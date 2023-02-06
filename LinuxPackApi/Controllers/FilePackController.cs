using LinuxPackApi.Model;
using LinuxPackApi.Service;
using Microsoft.AspNetCore.Mvc;
using System.IO;

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
            return _packService!.GetAllPermitProducts();
        }

        [HttpPost(Name = "StartPack")]
        public async Task<IEnumerable<FileContentResult>> StartPack(PackRequestModel packRequestModel)
        {
            var productModes = _packService!.GetAllPermitProducts();
            var productMode = productModes.FirstOrDefault(item => item.ProductIdentity == packRequestModel.ProductIdentity);
            if (productMode == null)
            {
                throw new ArgumentException($"Could not find Product : {packRequestModel.ProductIdentity}");
            }
            var debRecords = await _packService!.Packing(packRequestModel.ProductIdentity, "", packRequestModel.Version, packRequestModel.IsPreRelease);
            var result = new List<FileContentResult>();
            foreach (var debRecord in debRecords)
            {
                var fileContentResult = File(debRecord.FileBytes, "application/octet-stream", debRecord.FileName);
                result.Add(fileContentResult);
            }
            return result;
        }
    }
}
