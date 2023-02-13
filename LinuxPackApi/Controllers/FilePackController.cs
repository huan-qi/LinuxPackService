using LinuxPackApi.Model;
using LinuxPackApi.Service;
using LinuxPackFile.Model;
using Microsoft.AspNetCore.Mvc;

namespace LinuxPackApi.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class FilePackController : ControllerBase
    {
        private readonly PackService? _packService = null;
        public FilePackController()
        {
            _packService = new PackService();
        }

        [HttpGet(Name = "GetPermitProducts")]
        public List<ProductModel> GetPermitProducts()
        {
            var result = _packService!.GetAllPermitProducts();
            return result;
        }

        [HttpPost(Name = "StartPack")]
        public async Task<IEnumerable<FileContentResult>> StartPack(PackRequestModel packRequestModel)
        {
            var productModes = _packService!.GetAllPermitProducts();
            var productMode = productModes.FirstOrDefault(item => item.ProductIdentity == packRequestModel.ProductIdentity);
            if (productMode == null)
            {
                throw new Exception($"未找到对应券商 : {packRequestModel.ProductIdentity}");
            }
            var debRecords = await _packService!.Packing(packRequestModel.ProductIdentity, 
                                                         productMode.ProductName, 
                                                         packRequestModel.PackageVersion,
                                                         packRequestModel.Version, 
                                                         packRequestModel.IsPreRelease);
          
            var result = new List<FileContentResult>();
            foreach (var debRecord in debRecords)
            {
                var fileContentResult = File(debRecord.FileBytes, "application/octet-stream", debRecord.FileName);
                result.Add(fileContentResult);
            }

            return result;
        }

        [HttpPost(Name = "UploadApplicationFile")]
        public void UploadApplicationFile([FromForm]UploadApplicationFileMode uploadApplicationFileMode)
        {
            _packService!.UploadApplicationFile(uploadApplicationFileMode.Version, uploadApplicationFileMode.FormFile);
        }

        [HttpPost(Name = "UploadTransactionFile")]
        public void UploadTransactionFile([FromForm]UploadTransactionFileMode uploadTransactionFileMode)
        {
            _packService!.UploadTransactionFile(uploadTransactionFileMode.CpuType, uploadTransactionFileMode.ProductIdentity, uploadTransactionFileMode.FormFile);
        }


        public class UploadFileMode
        {
            public string Version { get; set; }
            public IFormFile FromFile { get; set; }
        }
    }
}
