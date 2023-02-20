namespace LinuxPackApi.Model
{
    public class PackRequestModel
    {
        public string ProductIdentity { get; set; }
        public string BasisPackageVersion { get; set; }
        public string ProductVersion { get; set; }
        public bool ProductIsPreRelease { get; set; }
    }
}
