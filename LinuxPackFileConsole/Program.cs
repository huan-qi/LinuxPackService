using LinuxPackFile;
using System.Xml;
using System.Linq;

namespace LinuxPackFileConsole
{
    class Program
    {
        public static void Main(string[] args)
        {
            if (args == null || args.Length < 5)
            {
                Console.WriteLine("请输入参数 1. 券商标识（eg: ZheShang）2. 版本号（eg: 2.3.0.1）3. 是发是预发布版本（eg: true）4. 输出路径");
                return;
            }

            var productType = args[0];
            var productName = GetProductName(productType);
            var productVersion = args[2];
            var isPreRelease = args[3];
            var outputPath = args[4];

            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory.ToString();
            string packTemplateFolderPathTemp = Path.Combine(baseDirectory, "Temp");
            string amdAppPath = Path.Combine(baseDirectory, "linux-x64");
            string armAppPath = Path.Combine(baseDirectory, "linux-arm64");

            PackFileManager packFileManager = new PackFileManager(
            packTemplateFolderPathTemp,
            productType,
            productName);
            try
            {
                packFileManager.DoWork();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Console.ReadKey();
                return;
            }

            bool.TryParse(isPreRelease, out bool isPre);
            PackDebManager packDebManager = new PackDebManager(
                productType,
                productVersion,
                isPre, 
                amdAppPath, 
                armAppPath,
                packTemplateFolderPathTemp);

            try
            {
                packDebManager.DoWork();
                packDebManager.Complete(outputPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Console.ReadKey();
                return;
            }

            Console.WriteLine("Successful!!!");
            Console.ReadKey();

            //修改build.sh dos2unix
            //chmod a+x build.sh
        }

        private static string GetProductName(string productType)
        {
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.Load("ProductConfig.xml");
            var node = xmlDocument.SelectSingleNode($"Product/{productType}");
            return node.InnerText;
        }
    }
}