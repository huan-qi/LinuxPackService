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
        public static string PackTemplateFolderPath { get; } = Path.Combine(BaseDirectory, "LinuxPublish");
        public static string PackResourcePath { get; } = Path.Combine(BaseDirectory, "LinuxPackResource");
    }
}
