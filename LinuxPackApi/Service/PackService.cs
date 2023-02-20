using HtmlAgilityPack;
using LinuxPackApi.Model;
using LinuxPackFile;
using LinuxPackFile.Model;
using LinuxPackFileConsole;
using System.Text;
using System.Xml;

namespace LinuxPackApi.Service
{
    internal class PackService
    {
        private const string DEB_PACKAGE = "DebPackage";
        private const string ZIP_PACKAGE = "ZipPackage";
        private const string HEVO_CLIENT_URL = @"http://172.19.80.141:81/hevo_client/";

        private readonly string _productXml = string.Empty;
        private readonly string _baseDirectory = string.Empty;
        private readonly PackPublishAppManager _packPublishAppManager;
        private readonly PackTemplateManager _packTemplateManager;
        private readonly FileManager _fileManager;
        private readonly HttpClient _httpClient;
 
        public PackService()
        {
            _productXml = "ProductConfig.xml";
            _baseDirectory = AppDomain.CurrentDomain.BaseDirectory.ToString();
            _packPublishAppManager = new PackPublishAppManager(_baseDirectory); //开始进行打包
            _packTemplateManager = new PackTemplateManager(); //打包文件生成器
            _fileManager = new FileManager(_baseDirectory); //打包需要的文件管理器
            _httpClient = new HttpClient() { Timeout = new TimeSpan(0, 0, 5) };
        }

        /// <summary>
        /// 获取所有支持券商
        /// </summary>
        /// <returns></returns>
        public List<ProductModel> GetAllPermitProducts()
        {
            var productNode = GetProductConfig();
            var result = new List<ProductModel>();
            foreach (XmlNode childNode in productNode.ChildNodes)
            {
                var productModel = new ProductModel();
                productModel.ProductIdentity = childNode.Name;
                productModel.ProductName = childNode.InnerText;
                result.Add(productModel);
            }
            return result;
        }
        /// <summary>
        /// 获取所有的包下载路径
        /// </summary>
        /// <returns></returns>
        public IEnumerable<FileInfo> GetAllPackages()
        {
            var result = new List<FileInfo>();
            var compressInfos = GetCompressionInfo();
            var packingInfos = GetPackingInfo(compressInfos);
            result.AddRange(packingInfos);
            result.AddRange(compressInfos);
            return result;

            IEnumerable<FileInfo> GetPackingInfo(IEnumerable<FileInfo> compressionInfos)
            {
                List<FileInfo> result = new List<FileInfo>();
                var baseDirectory = new DirectoryInfo(_baseDirectory);
                baseDirectory.GetDirectories().Where(item => item.Name.StartsWith("Temp_"));
                foreach (var directory in baseDirectory.GetDirectories())
                {
                    if (directory.Name.StartsWith("Temp_"))
                    {
                        var iniFileInfo = directory.GetFiles("*.ini").FirstOrDefault();
                        if (iniFileInfo == null)
                        {
                            continue;
                        }
                        var alreadyPacking = compressionInfos.Any(item => item.Directory!.Name == iniFileInfo.Directory!.Name);
                        if (alreadyPacking)
                        {
                            continue;
                        }
                        result.Add(iniFileInfo);
                    }
                }
                return result;
            }
            IEnumerable<FileInfo> GetCompressionInfo()
            {
                var zipPackagePath = Path.Combine(_baseDirectory, ZIP_PACKAGE);
                DirectoryInfo zipPackageDirectoryInfo = new DirectoryInfo(zipPackagePath);
                var zipPackageFileInfos = zipPackageDirectoryInfo.GetFiles("*.zip", SearchOption.AllDirectories);
                return zipPackageFileInfos;
            }
        }
        public async Task<IEnumerable<string>> GetAllPublicPackFileVersions()
        {
            var result = new List<string>();
            var baseDirectoryInfo = new DirectoryInfo(_baseDirectory);
            foreach (var directory in baseDirectoryInfo.GetDirectories())
            {
                if (Version.TryParse(directory.Name, out _))
                {
                    result.Add(directory.Name);
                }
            }

            try
            {
                var versions = await FindBasisPackageVersions();
                result.AddRange(versions);
            }
            catch { }

            return result.Distinct();
        }
        /// <summary>
        /// 打包返回下载流
        /// </summary>
        /// <param name="productIdentity">券商标识</param>
        /// <param name="productName">券商名</param>
        /// <param name="packageVersion">公办包版本</param>
        /// <param name="productVersion">生成包版本</param>
        /// <param name="isPreRelease">是否预发布</param>
        /// <returns>包路径</returns>
        public async Task<string> PackingWidthDownLoad(string productIdentity, 
                                                       string productName, 
                                                       string basisPackageVersion, 
                                                       string productVersion, 
                                                       bool productIsPreRelease)
        {
            var tempFolderName = GetTempFolder();
            return await PackingHandle(tempFolderName, productIdentity, productName, basisPackageVersion, productVersion, productIsPreRelease);
        }
        /// <summary>
        /// 打包返回包GUID
        /// </summary>
        /// <param name="productIdentity">券商标识</param>
        /// <param name="productName">券商名</param>
        /// <param name="packageVersion">公办包版本</param>
        /// <param name="productVersion">生成包版本</param>
        /// <param name="isPreRelease">是否预发布</param>
        /// <returns></returns>
        public string PackingWidthGuid(string productIdentity,
                                       string productName,
                                       string basisPackageVersion,
                                       string productVersion,
                                       bool productIsPreRelease)
        {
            var tempFolderName = GetTempFolder();
            var backgroundTask = new Task(async () => 
            {
                try
                {
                    await PackingHandle(tempFolderName, productIdentity, productName, basisPackageVersion, productVersion, productIsPreRelease);
                }
                catch { }
            });
            backgroundTask.Start();
            return tempFolderName;
        }
        /// <summary>
        /// 根据打包时的GUID获取包
        /// </summary>
        /// <param name="folderName">打包时生成的GUID</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public FileInfo PackageFile(string folderName)
        {
            var zipPackagePath = Path.Combine(_baseDirectory, ZIP_PACKAGE);
            var tempZipOutputPath = Path.Combine(zipPackagePath, folderName);   //zip包临时文件夹
            DirectoryInfo zipDirectoryInfo = new DirectoryInfo(tempZipOutputPath);
            if (!zipDirectoryInfo.Exists)
            {
                throw new Exception("没有找到对应的Linux包");
            }
            var zipFileInfo = zipDirectoryInfo.GetFiles("*.zip").FirstOrDefault();
            if (zipFileInfo == null || !zipFileInfo.Exists)
            {
                throw new Exception("没有找到对应的Linux包");
            }
            return zipFileInfo;
        }
        /// <summary>
        /// 上传交易
        /// </summary>
        /// <param name="cpuType"></param>
        /// <param name="productIdentity"></param>
        /// <param name="formFile"></param>
        public void UploadTransactionFile(CPU_TYPE cpuType, string productIdentity, IFormFile formFile)
        {
            using var stream = formFile.OpenReadStream();
            _fileManager.UploadTransaction(productIdentity, cpuType, stream);
        }
        /// <summary>
        /// 上传公版应用
        /// </summary>
        /// <param name="version">公版版本</param>
        /// <param name="formFile">文件流</param>
        public void UploadApplicationFile(string basisPackageVersion, IFormFile formFile)
        {
            using var stream = formFile.OpenReadStream();
            _fileManager.UploadLinuxApplication(stream, basisPackageVersion);
        }


        /// <summary>
        /// 打包处理逻辑
        /// </summary>
        /// <param name="tempFolderName"></param>
        /// <param name="productIdentity"></param>
        /// <param name="productName"></param>
        /// <param name="basisPackageVersion"></param>
        /// <param name="productVersion"></param>
        /// <param name="isPreRelease"></param>
        /// <returns></returns>
        private async Task<string> PackingHandle(string tempFolderName,
                                                 string productIdentity,
                                                 string productName,
                                                 string basisPackageVersion,
                                                 string productVersion,
                                                 bool productIsPreRelease)
        {
            var tempPath = Path.Combine(_baseDirectory, tempFolderName);
            var debPackPath = Path.Combine(_baseDirectory, DEB_PACKAGE);
            var zipPackagePath = Path.Combine(_baseDirectory, ZIP_PACKAGE);
            var tempDebOutputPath = Path.Combine(debPackPath, tempFolderName);      //deb包临时文件夹
            var tempZipOutputPath = Path.Combine(zipPackagePath, tempFolderName);   //zip包临时文件夹

            DirectoryInfo tempDirectory = new DirectoryInfo(tempPath);
            DirectoryInfo debPackageDirectory = new DirectoryInfo(debPackPath);     //deb包文件夹
            DirectoryInfo zipPackageDirectory = new DirectoryInfo(zipPackagePath);  //zip包文件夹
            if (!debPackageDirectory.Exists)
            {
                debPackageDirectory.Create();
            }
            if (!zipPackageDirectory.Exists)
            {
                zipPackageDirectory.Create();
            }
            if (!tempDirectory.Exists)
            {
                tempDirectory.Create();
                var infoFilePath = Path.Combine(tempPath, $"{productIdentity}_{productVersion}.ini");
                File.Create(infoFilePath).Close();
            }

            try
            {
                var appFilePath = Path.Combine(_baseDirectory, basisPackageVersion);
                if (!Directory.Exists(appFilePath))
                {
                    //从包的发布网址下载包
                    var downloadUrl = await FindLinuxPackFileDownloadUrl(new Version(basisPackageVersion));
                    using (var fileStream = await DownloadFileStream(downloadUrl))
                    {
                        _fileManager.UploadLinuxApplication(fileStream, basisPackageVersion);
                    }
                }

                _packTemplateManager.DoWork(tempPath, productIdentity, productName); //开始生成打包模板
                _packPublishAppManager.DoWork(tempPath, productIdentity, basisPackageVersion, productVersion, productIsPreRelease); //开始打包
                _packPublishAppManager.Complete(tempDebOutputPath, tempPath);

                DirectoryInfo tempDebOutputDirectory = new DirectoryInfo(tempDebOutputPath);
                DirectoryInfo tempZipOutputDirectory = new DirectoryInfo(tempZipOutputPath);
                if (!tempDebOutputDirectory.Exists)
                {
                    tempDebOutputDirectory.Create();
                }
                if (!tempZipOutputDirectory.Exists)
                {
                    tempZipOutputDirectory.Create();
                }

                var tempZipFilePath = Path.Combine(tempZipOutputPath, $"{productIdentity}_{productVersion}.zip");  //zip压缩包
                using var zipFileStream = new FileStream(tempZipFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite);
                _fileManager.ZipDirectory(tempDebOutputPath, zipFileStream);
                return tempZipFilePath;
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                _packPublishAppManager.DeleteTempDirectory(tempDebOutputPath);
                _packPublishAppManager.DeleteTempDirectory(tempPath);
            }
        }
        /// <summary>
        /// 生成临时文件夹
        /// </summary>
        /// <returns></returns>
        private string GetTempFolder()
        {
            var guidStr = Guid.NewGuid().ToString();
            var tempName = $"Temp_{guidStr}";
            return tempName;
        }
        /// <summary>
        /// 获取券商配置
        /// </summary>
        /// <returns></returns>
        private XmlNode? GetProductConfig()
        {
            string productNode = "Product";
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.Load(_productXml);
            var nodes = xmlDocument.SelectSingleNode(productNode);
            return nodes;
        }

        private async Task<IEnumerable<string>> FindBasisPackageVersions()
        {
            var basisPackageUrls = await GetBasisPackageDownloadUrls();
            var versions = basisPackageUrls.Select(item => GetBasisPackageVersionFromUrl(item).ToString());
            return versions;
        }
        /// <summary>
        /// 查找对应版本号的Linux App包
        /// </summary>
        /// <param name="targetVersion">目标版本号</param>
        /// <returns>包的下载路径</returns>
        /// <exception cref="Exception"></exception>
        private async Task<string> FindLinuxPackFileDownloadUrl(Version targetVersion)
        {
            var basisPackageUrls = await GetBasisPackageDownloadUrls();
             var basisPackageUrl = basisPackageUrls.FirstOrDefault(item => targetVersion == GetBasisPackageVersionFromUrl(item));
            if (string.IsNullOrEmpty(basisPackageUrl))
            {
                throw new Exception("没有找到对应版本号的Linux包");
            }
            var result = Path.Combine(HEVO_CLIENT_URL, basisPackageUrl, $"{targetVersion.ToString()}.zip");
            return result;
        }
        /// <summary>
        /// 重URL中获取版本信息
        /// </summary>
        /// <param name="url">url</param>
        /// <returns></returns>
        private Version GetBasisPackageVersionFromUrl(string url)
        {
            var toyear = DateTime.Now.Year.ToString();
            var startStr = "linux-";
            var endStr = $"_{toyear}";

            var startIndex = url.IndexOf(startStr) + startStr.Length;
            var endIndex = url.IndexOf(endStr);
            var versionStr = url.Substring(startIndex, endIndex - startIndex);

            return new Version(versionStr);
        }
        /// <summary>
        /// 获取Linux包的路径
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private async Task<IEnumerable<string>> GetBasisPackageDownloadUrls()
        {
            var packLinkRouter = @"/html/body/table/tbody/tr/td/a";
            var linuxPackHeader = "result_thshq-b2b-client-linux";
            var href = "href";

            var response = await _httpClient.GetAsync(HEVO_CLIENT_URL);
            var responseStream = await response.Content.ReadAsStreamAsync();
            string html = string.Empty;
            using (var sr = new StreamReader(responseStream, Encoding.UTF8))
            {
                html = sr.ReadToEnd();
            }

            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);
            var nodes = doc.DocumentNode.SelectNodes(packLinkRouter);
            if (nodes == null)
            {
                throw new Exception($"无法从{HEVO_CLIENT_URL}读取指定节点信息,节点为:{packLinkRouter}");
            }
            var packageUrls = new List<string>();
            foreach (var node in nodes)
            {
                var url = node.GetAttributeValue(href, string.Empty);
                packageUrls.Add(url);
            }
            var linuxPackageUrls = packageUrls.Where(item => item.Contains(linuxPackHeader));

            return linuxPackageUrls;
        }
        /// <summary>
        /// 下载文件流
        /// </summary>
        /// <param name="downloadUrl">下载路径</param>
        /// <returns>文件流</returns>
        private async Task<Stream> DownloadFileStream(string downloadUrl)
        {
            var stream = await _httpClient.GetStreamAsync(downloadUrl);
            return stream;
        }
    }
}
