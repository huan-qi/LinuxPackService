using System;
using System.Text.RegularExpressions;

namespace LinuxPackFileConsole
{
    class Program
    {
        public static void Main(string[] args)
        {
            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory.ToString();
            string packTemplateFolderPath = Path.Combine(baseDirectory, "LinuxPublish"); //公版打包模板位置
            string packTemplateFolderPathTemp = Path.Combine(baseDirectory, "Temp");     //定制打包模板生产位置
            string resourcePath = Path.Combine(baseDirectory, "LinuxPackResource");      //打包模板相关资源
#if DEBUG
            PackFileManager packFileManager = new PackFileManager(
            packTemplateFolderPath,
            resourcePath,
            packTemplateFolderPathTemp,
            "CaiXinHxyhb",
            "财信证券");
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
            Console.WriteLine("Successful!!!");
            Console.ReadKey();
#else
            if (args == null || args.Length != 3)
            {
                Console.WriteLine("No Parames!!!");
                return;
            }
            string outputPath = args[0];
            string product = args[1];
            string chineseName = args[2];
            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory.ToString();
            string folderPath = Path.Combine(baseDirectory, "Publish"); 
            string resourcePath = Path.Combine(baseDirectory, "LinuxPackResource");

            PackFileManager packFileManager = new PackFileManager(
            folderPath,
            resourcePath,
            outputPath,   //@"C:\Users\91688\Desktop\ZheShang",
            product,      //"ZheShang",
            chineseName); //"浙商金融终端"
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
            Console.WriteLine("Successful!!!");
            Console.ReadKey();
#endif
            //修改build.sh dos2unix
            //chmod a+x build.sh
        }
    }
}