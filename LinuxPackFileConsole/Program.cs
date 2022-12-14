using LinuxPackFile;

namespace LinuxPackFileConsole
{
    class Program
    {
        public static void Main(string[] args)
        {
            if (args == null || args.Length < 5)
            {
                Console.WriteLine("请输入参数 1. 券商标识（eg: ZheShang） 2. 券商名称（eg: 浙商金融终端）3. 版本号（eg: 2.3.0.1） 4. 是发是预发布版本（eg: true）5. 输出路径");
                return;
            }

            var productType = args[0];
            var productName = args[1];
            var productVersion = args[2];
            var isPreRelease = args[3];
            var outputPath = args[4];

            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory.ToString();
            string packTemplateFolderPathTemp = Path.Combine(baseDirectory, "Temp");     //定制打包模板生产位置
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
    }
}