using LinuxPackApi.Model;
using LinuxPackApi.Service;
using Microsoft.AspNetCore.Mvc;

namespace LinuxPackApi.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class FilePackController : ControllerBase
    {
        private readonly PackService _packService;
        public FilePackController()
        {
            _packService = new PackService();
        }

        /// <summary>
        /// 获取所有支持的券商
        /// </summary>
        /// <returns></returns>
        [HttpGet(Name = "GetPermitProducts")]
        public List<ProductModel> GetPermitProducts()
        {
            var result = _packService.GetAllPermitProducts();
            return result;
        }
        /// <summary>
        /// 获取所有包信息
        /// </summary>
        /// <returns></returns>
        [HttpGet(Name = "GetProductZipPackageList")]
        public IEnumerable<ZipPackageMode> GetProductZipPackageList()
        {
            var result = new List<ZipPackageMode>();
            var zipPackageFileInfos = _packService.GetAllPackages();

            foreach (var zipPackageFileInfo in zipPackageFileInfos)
            {
                if (zipPackageFileInfo.Directory == null)
                {
                    continue;
                }
                string guid = string.Empty;
                string version = string.Empty;
                string productIdentity = string.Empty;
                string downLoadUrl = string.Empty;
                string status = string.Empty;

                if (zipPackageFileInfo.Extension == ".ini")
                {
                    var startIndex = zipPackageFileInfo.Name.IndexOf("_");
                    var endIndex = zipPackageFileInfo.Name.IndexOf(".ini");
                    guid = zipPackageFileInfo.Directory.Name;
                    version = zipPackageFileInfo.Name.Substring(startIndex + 1, endIndex - startIndex - 1);
                    productIdentity = zipPackageFileInfo.Name.Substring(0, startIndex);
                    downLoadUrl = string.Empty;
                    status = PACKING_STATUS.PACKING.ToString();
                }
                else
                {
                    var startIndex = zipPackageFileInfo.Name.IndexOf("_");
                    var endIndex = zipPackageFileInfo.Name.IndexOf(".zip");

                    guid = zipPackageFileInfo.Directory.Name;
                    version = zipPackageFileInfo.Name.Substring(startIndex + 1, endIndex - startIndex - 1);
                    productIdentity = zipPackageFileInfo.Name.Substring(0, startIndex);
                    if (IsOccupled(zipPackageFileInfo.FullName))
                    {
                        downLoadUrl = string.Empty;
                        status = PACKING_STATUS.COMPRESSION.ToString();
                    }
                    else
                    {
                        downLoadUrl = $"{this.Request.Host.ToString()}/ShareZipPackages/{guid}/{zipPackageFileInfo.Name}";
                        status = PACKING_STATUS.COMPLATE.ToString();
                    }
                }
                var zipPackageMode = new ZipPackageMode()
                {
                    ProducZipGuid = guid,
                    ProductVersion = version,
                    ProductIdentity = productIdentity,
                    ProductDownLoadPath = downLoadUrl,
                    Status = status
                };
                result.Add(zipPackageMode);
            }
            return result;
        }
        [HttpGet(Name = "GetBasisPackageVersions")]
        public async Task<IEnumerable<string>> GetBasisPackageVersions()
        {
            return await _packService.GetAllPublicPackFileVersions();
        }
        /// <summary>
        /// 开始打包返回下载文件流
        /// </summary>
        /// <param name="packRequestModel"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        [HttpPost(Name = "StartPackWidthDownload")]
        public async Task<FileStreamResult> StartPackWidthDownload(PackRequestModel packRequestModel)
        {
            var productMode = CheckProductModel(packRequestModel.ProductIdentity);
            var zipPackagePath = await _packService.PackingWidthDownLoad(packRequestModel.ProductIdentity,
                                                                          productMode.ProductName,
                                                                          packRequestModel.BasisPackageVersion,
                                                                          packRequestModel.ProductVersion,
                                                                          packRequestModel.ProductIsPreRelease);
            var zipPackageFileInfo = new FileInfo(zipPackagePath);
            if (!zipPackageFileInfo.Exists)
            {
                throw new Exception($"打包失败未找到文件");
            }
            var zipFileStream = new FileStream(zipPackagePath, FileMode.Open);
            var result = File(zipFileStream, "application/octet-stream", zipPackageFileInfo.Name);
            return result;
        }
        /// <summary>
        /// 开始打包返回包的Guid
        /// </summary>
        /// <param name="packRequestModel"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        [HttpPost(Name = "StartPackWidthProductPackageGuid")]
        public string StartPackWidthProductPackageGuid(PackRequestModel packRequestModel)
        {
            var productMode = CheckProductModel(packRequestModel.ProductIdentity);
            return _packService.PackingWidthGuid(packRequestModel.ProductIdentity,
                                                 productMode.ProductName,
                                                 packRequestModel.BasisPackageVersion,
                                                 packRequestModel.ProductVersion,
                                                 packRequestModel.ProductIsPreRelease);
        }
        /// <summary>
        /// 根据包的Guid返回文件下载流
        /// </summary>
        /// <param name="guid"></param>
        /// <returns></returns>
        [HttpPost(Name = "DownloadProductPackageFromGuid")]
        public FileStreamResult DownloadPackage(string guid)
        {
            var zipPackageFileInfo = _packService.PackageFile(guid);
            var zipFileStream = new FileStream(zipPackageFileInfo.FullName, FileMode.Open);
            var result = File(zipFileStream, "application/octet-stream", zipPackageFileInfo.Name);
            return result;
        }
        /// <summary>
        /// 上传应用Zip包
        /// </summary>
        /// <param name="uploadApplicationFileMode"></param>
        [HttpPost(Name = "UploadApplicationFile")]
        [RequestSizeLimit(int.MaxValue)]
        public void UploadApplicationFile([FromForm]UploadApplicationFileMode uploadApplicationFileMode)
        {
            _packService.UploadApplicationFile(uploadApplicationFileMode.BasisPackageVersion, uploadApplicationFileMode.FormFile);
        }
        /// <summary>
        /// 上传交易Zip包
        /// </summary>
        /// <param name="uploadTransactionFileMode"></param>
        [HttpPost(Name = "UploadTransactionFile")]
        [RequestSizeLimit(int.MaxValue)]
        public void UploadTransactionFile([FromForm]UploadTransactionFileMode uploadTransactionFileMode)
        {
            _packService.UploadTransactionFile(uploadTransactionFileMode.CpuType, uploadTransactionFileMode.ProductIdentity, uploadTransactionFileMode.FormFile);
        }

        private ProductModel CheckProductModel(string productIdentity)
        {
            var productModes = _packService.GetAllPermitProducts();
            var productMode = productModes.FirstOrDefault(item => item.ProductIdentity == productIdentity);
            if (productMode == null)
            {
                throw new Exception($"未找到对应券商 : {productIdentity}");
            }
            return productMode;
        }
        private bool IsOccupled(string filePath)
        {
            try
            {
                using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite);
                return false;
            }
            catch (Exception)
            {
                return true;
            }
        }
    }
}
