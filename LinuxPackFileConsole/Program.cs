using HtmlAgilityPack;
using LinuxPackFile;
using System.Net;
using System.Text;
using System.Xml;

namespace LinuxPackFileConsole
{
    class Program
    {
        public static void Main(string[] args)
        {
            var task = FindLinuxPackFileDownloadUrl(new Version("2.3.16517.11"));
            var result = task.Result;
            
        }
        private static async Task<string> FindLinuxPackFileDownloadUrl(Version targetVersion)
        {
            var hevoClientUrl = @"https://www.baidu.com";
            var packLinkRouter = @"/html/body/table/tbody/tr/td/a";
            var linuxPackHeader = "result_thshq-b2b-client-linux";
            var href = "href";

            HttpClient httpClient = new HttpClient();
            var response = await httpClient.GetAsync(hevoClientUrl);
            var responseStream = await response.Content.ReadAsStreamAsync();
            string html = string.Empty;
            using (var sr = new StreamReader(responseStream, Encoding.UTF8))
            {
                html = sr.ReadToEnd();
            }
            //var html = await httpClient.GetStringAsync(hevoClientUrl);


            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);
            var nodes = doc.DocumentNode.SelectNodes(packLinkRouter);
            if (nodes == null)
            {
                throw new Exception($"无法从{hevoClientUrl}读取指定节点信息,节点为:{packLinkRouter}");
            }
            var packageUrls = new List<string>();
            foreach (var node in nodes)
            {
                var url = node.GetAttributeValue(href, string.Empty);
                packageUrls.Add(url);
            }
            var linuxPackageUrls = packageUrls.Where(item => item.Contains(linuxPackHeader));
            var linuxPackageUrl = linuxPackageUrls.FirstOrDefault(item => targetVersion == GetVersionFromUrls(item));
            if (string.IsNullOrEmpty(linuxPackageUrl))
            {
                throw new Exception("没有找到对应版本号的Linux包");
            }
            var result = Path.Combine(hevoClientUrl, linuxPackageUrl, $"{targetVersion.ToString()}.zip");
            return result;

            Version GetVersionFromUrls(string url)
            {
                var toyear = DateTime.Now.Year.ToString();
                var startStr = "linux-";
                var endStr = $"_{toyear}";

                var startIndex = url.IndexOf(startStr) + startStr.Length;
                var endIndex = url.IndexOf(endStr);
                var versionStr = url.Substring(startIndex, endIndex - startIndex);

                return new Version(versionStr);
            }
        }
        public static void Main1(string[] args)
        {
            if (args == null || args.Length < 4)
            {
                Console.WriteLine("请输入参数 1. 券商标识（eg: ZheShang）2. 版本号（eg: 2.3.0.1）3. 是发是预发布版本（eg: true）4. 输出路径");
                return;
            }

            var productType = args[0];
            var productName = GetProductName(productType);
            var productVersion = args[1];
            var isPreRelease = args[2];
            var outputPath = args[3];

            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory.ToString();
            string packTemplateFolderPathTemp = Path.Combine(baseDirectory, "Temp");
            ClearTemp(packTemplateFolderPathTemp);

            PackTemplateManager packFileManager = new PackTemplateManager();
            try
            {
                packFileManager.DoWork(packTemplateFolderPathTemp, productType, productName);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Console.ReadKey();
                return;
            }

            bool.TryParse(isPreRelease, out bool isPre);
            PackPublishAppManager packDebManager = new PackPublishAppManager(baseDirectory);
            try
            {
                packDebManager.DoWork(packTemplateFolderPathTemp, productType, productVersion, isPre);
                packDebManager.Complete(outputPath, packTemplateFolderPathTemp);
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
            string productXml = "ProductConfig.xml";
            string productNode = $"Product/{productType}";
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.Load(productXml);
            var node = xmlDocument.SelectSingleNode(productNode);
            if (node == null)
            {
                throw new ArgumentException("productType 类型错误，无此券商");
            }
            return node.InnerText;
        }
        private static void ClearTemp(string tempPath)
        {
            if (Directory.Exists(tempPath))
            {
                Directory.Delete(tempPath, true);
            }
        }
    }
}