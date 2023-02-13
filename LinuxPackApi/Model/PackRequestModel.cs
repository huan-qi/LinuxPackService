namespace LinuxPackApi.Model
{
    public class PackRequestModel
    {
        public string ProductIdentity { get; set; }
        public string PackageVersion { get; set; }
        public string Version { get; set; }
        public bool IsPreRelease { get; set; }
    }
}
