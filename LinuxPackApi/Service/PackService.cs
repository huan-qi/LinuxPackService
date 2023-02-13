using HtmlAgilityPack;
using LinuxPackApi.Model;
using LinuxPackFile;
using LinuxPackFile.Model;
using LinuxPackFileConsole;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml;

namespace LinuxPackApi.Service
{
    internal class PackService
    {
        private readonly string _productXml = string.Empty;
        private readonly string _baseDirectory = string.Empty;
        private readonly PackPublishAppManager _packPublishAppManager = null;
        private readonly PackTemplateManager _packTemplateManager = null;
        private readonly FileManager _fileManager = null;
        private readonly HttpClient _httpClient = null;

        public PackService()
        {
            _productXml = "ProductConfig.xml";
            _baseDirectory = AppDomain.CurrentDomain.BaseDirectory.ToString();
            _packPublishAppManager = new PackPublishAppManager(_baseDirectory); //开始进行打包
            _packTemplateManager = new PackTemplateManager(); //打包文件生成器
            _fileManager = new FileManager(_baseDirectory); //打包需要的文件管理器
            _httpClient = new HttpClient();
        }

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
        public async Task<IEnumerable<FileRecordMode>> Packing(string productIdentity, 
                                                               string productName, 
                                                               string packageVersion, 
                                                               string productVersion, 
                                                               bool isPreRelease)
        {
            var tempPath = GetTempFolderPath();
            var outputPath = Path.Combine("/home", "hexin", "Desktop", productIdentity);
            var resultFileRecordes = new List<FileRecordMode>();

            try
            {
                var appFilePath = Path.Combine(_baseDirectory, packageVersion);
                if (!Directory.Exists(appFilePath))
                {
                    //从包的发布网址下载包
                    var downloadUrl = await FindLinuxPackFileDownloadUrl(new Version(packageVersion));
                    using (var fileStream = await DownloadFileStream(downloadUrl))
                    {
                        _fileManager.UploadLinuxApplication(fileStream, packageVersion);
                    }
                }

                _packTemplateManager.DoWork(tempPath, productIdentity, productName); //开始生成打包模板
                _packPublishAppManager.DoWork(tempPath, productIdentity, productVersion, isPreRelease); //开始打包
                var debFileInfos = _packPublishAppManager.Complete(tempPath);

                foreach (var debFileInfo in debFileInfos)
                {
                    byte[] fileBytes;
                    using (var fs = debFileInfo.OpenRead())
                    {
                        fileBytes = new byte[debFileInfo.Length];
                        await fs.ReadAsync(fileBytes);
                    }
                    resultFileRecordes.Add(new FileRecordMode()
                    {
                        FileName = debFileInfo.Name,
                        FileBytes = fileBytes,
                    });
                }

                _packPublishAppManager.Complete(outputPath, tempPath);
            }
            catch (Exception)
            {
                _packPublishAppManager.DeleteTempDirectory(tempPath);
                throw;
            }

            return resultFileRecordes;
        }
        public void UploadTransactionFile(CPU_TYPE cpuType, string productIdentity, IFormFile formFile)
        {
            using var stream = formFile.OpenReadStream();
            _fileManager.UploadTransaction(productIdentity, cpuType, stream);
        }
        public void UploadApplicationFile(string version, IFormFile formFile)
        {
            using var stream = formFile.OpenReadStream();
            _fileManager.UploadLinuxApplication(stream, version);
        }

        /// <summary>
        /// 生成临时文件夹
        /// </summary>
        /// <returns></returns>
        private string GetTempFolderPath()
        {
            var guidStr = Guid.NewGuid().ToString();
            var tempName = $"Temp_{guidStr}";
            return Path.Combine(_baseDirectory, tempName);
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
        /// <summary>
        /// 查找对应版本号的Linux App包
        /// </summary>
        /// <param name="targetVersion">目标版本号</param>
        /// <returns>包的下载路径</returns>
        /// <exception cref="Exception"></exception>
        private async Task<string> FindLinuxPackFileDownloadUrl(Version targetVersion)
        {
            var hevoClientUrl = @"http://172.19.80.141:81/hevo_client/";
            var packLinkRouter = @"/html/body/table/tbody/tr/td/a";
            var linuxPackHeader = "result_thshq-b2b-client-linux";
            var href = "href";

            var response = await _httpClient.GetAsync(hevoClientUrl);
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
                throw new Exception($"无法从{hevoClientUrl}读取指定节点信息,节点为:{packLinkRouter}");
            }
            var packageUrls = new List<string>();
            foreach (var node in nodes)
            {
                var url = node.GetAttributeValue(href, string.Empty);
                packageUrls.Add(url);
            }
            var linuxPackageUrls = packageUrls.Where(item => item.Contains(linuxPackHeader));
            var linuxPackageUrl = linuxPackageUrls.FirstOrDefault(item => targetVersion == GetVersionFromUrls(item));
            if (string.IsNullOrEmpty(linuxPackageUrl))
            {
                throw new Exception("没有找到对应版本号的Linux包");
            }
            var result = Path.Combine(hevoClientUrl, linuxPackageUrl, $"{targetVersion.ToString()}.zip");
            return result;

            Version GetVersionFromUrls(string url)
            {
                var toyear = DateTime.Now.Year.ToString();
                var startStr = "linux-";
                var endStr = $"_{toyear}";

                var startIndex = url.IndexOf(startStr) + startStr.Length;
                var endIndex = url.IndexOf(endStr);
                var versionStr = url.Substring(startIndex, endIndex - startIndex);

                return new Version(versionStr);
            }
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
