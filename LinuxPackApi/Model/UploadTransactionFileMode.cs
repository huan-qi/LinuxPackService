using LinuxPackFile.Model;

namespace LinuxPackApi.Model
{
    public class UploadTransactionFileMode
    {
        public CPU_TYPE CpuType { get; set; }
        public string ProductIdentity { get; set; }
        public IFormFile FormFile { get; set; }
    }
}
