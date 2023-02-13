using LinuxPackFile.Model;

namespace LinuxPackApi.Model
{
    public class UploadFileMode
    {
        public FILE_MODE_ENUM FileMode { get; set; }
        public CPU_TYPE CpuType { get; set; }
        public string ProductIdentity { get; set; }
        public IFormFile FormFile { get; set; }
    }
}
