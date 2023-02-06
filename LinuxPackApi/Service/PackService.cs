using LinuxPackApi.Model;
using LinuxPackFile;
using LinuxPackFileConsole;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;
using System.IO;
using System.Xml;
using System.Xml.Linq;

namespace LinuxPackApi.Service
{
    internal class PackService
    {
        private readonly string _productXml = string.Empty;
        private readonly string _baseDirectory = string.Empty;

        public PackService() 
        {
            _productXml = "ProductConfig.xml";
            _baseDirectory = AppDomain.CurrentDomain.BaseDirectory.ToString();
        }

        public List<ProductModel> GetAllPermitProducts()
        {
            var productNode = GetProductConfig();
            var result = new List<ProductModel>();
            foreach (XmlNode childNode in productNode.ChildNodes)
            {
                var productModel = new ProductModel();
                productModel.ProductIdentity = childNode.Name;
                productModel.ProductName = childNode.InnerText;
                result.Add(productModel);
            }
            return result;
        }
        public async Task<IEnumerable<FileRecordMode>> Packing(string productIdentity, string productName, string productVersion, bool isPreRelease)
        {
            var amdAppPath = Path.Combine(_baseDirectory, "linux-x64");
            var armAppPath = Path.Combine(_baseDirectory, "linux-arm64");
            var tempPath = GetTempFolderPath();
            var packFileManager = new PackFileManager(tempPath, productIdentity, productName); //打包文件生成器
            var packDebManager = new PackDebManager(productIdentity, productVersion, isPreRelease, amdAppPath, armAppPath, tempPath); //开始进行打包
            var resultFileRecordes = new List<FileRecordMode>();

            packFileManager.DoWork(); //开始生成打包模板
            packDebManager.DoWork(); //开始打包
            var debFileInfos = packDebManager.Complete();
            var outputPath = Path.Combine("/home", "hexin", "Desktop", productIdentity);
            Console.WriteLine($"OutPutPath: {outputPath}");
            packDebManager.Complete(outputPath);

            foreach (var debFileInfo in debFileInfos)
            {
                byte[] fileBytes;
                using (var fs = debFileInfo.OpenRead()) 
                {
                    fileBytes = new byte[debFileInfo.Length];
                    await fs.ReadAsync(fileBytes);
                }
                resultFileRecordes.Add(new FileRecordMode() 
                {
                    FileName = debFileInfo.Name,
                    FileBytes= fileBytes,
                });
            }
            return resultFileRecordes;
        }

        private string GetTempFolderPath()
        {
            var guidStr = Guid.NewGuid().ToString();
            var tempName = $"Temp_{guidStr}";
            return Path.Combine(_baseDirectory, tempName);
        }
        private XmlNode? GetProductConfig()
        {
            string productNode = "Product";
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.Load(_productXml);
            var nodes = xmlDocument.SelectSingleNode(productNode);
            return nodes;
        }
    }
}
