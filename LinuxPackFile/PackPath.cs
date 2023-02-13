using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace LinuxPackFile
{
    internal static class PackPath
    {
        private static string BaseDirectory = AppDomain.CurrentDomain.BaseDirectory.ToString();

        public const string LINUX_TEMPLATE = "LinuxPublish";
        public const string LINUX_PACK_TEMPLATE = "Linux_Pack";
        public const string LINUX_X64_APPLICATION = "linux-x64";
        public const string LINUX_ARM_APPLICATION = "linux-arm64";
        public const string LINUX_RES = "res";
        public const string LINUX_X64_TRANSACTION = "transaction_x64";
        public const string LINUX_ARM_TRANSACTION = "transaction_arm";
        public const string LINUX_X64_LIB = "lib_x64";
        public const string LINUX_ARM_LIB = "lib_arm";

        public static string PackTemplateFolderPath { get; } = Path.Combine(BaseDirectory, "LinuxPublish");
        public static string PackResourcePath { get; } = Path.Combine(BaseDirectory, "LinuxPackResource");
    }
}
