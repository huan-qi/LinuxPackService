using System.Diagnostics;
using System.Xml;

namespace LinuxPackFile
{
    public class PackDebManager
    {
        private string _productTypeName;
        private string _version;
        private bool _isPreRelease;
        private string _amdAppSourcePath;
        private string _armAppSourcePath;
        private string _amdAppTargetPath;
        private string _armAppTargetPath;
        private string _packTemplatePathTemp;

        public PackDebManager(
            string productTypeName,
            string version,
            bool isPreRelease,
            string amdAppSourcePath, 
            string armAppSourcePath, 
            string packTemplatePathTemp)
        {
            _productTypeName = productTypeName;
            _version = version;
            _isPreRelease = isPreRelease;
            _amdAppSourcePath = amdAppSourcePath;
            _armAppSourcePath = armAppSourcePath;
            _packTemplatePathTemp = packTemplatePathTemp;
            _amdAppTargetPath = Path.Combine(packTemplatePathTemp, "LinuxPublish", "linux-x64", "quotes");
            _armAppTargetPath = Path.Combine(packTemplatePathTemp, "LinuxPublish", "linux-arm64", "quotes");
        }

        public void DoWork()
        {
            var baseResPath = Path.Combine(PackPath.PackResourcePath, _productTypeName.ToLower(), "res");

            var amdLibSourcePath = Path.Combine(PackPath.PackResourcePath, "base", "lib_x64");
            var armLibSourcePath = Path.Combine(PackPath.PackResourcePath, "base", "lib_arm");
            var amdLibTargetPath = Path.Combine(_amdAppTargetPath, "lib");
            var armLibTargetPath = Path.Combine(_armAppTargetPath, "lib");

            var amdTransactionSourcePath = Path.Combine(PackPath.PackResourcePath, _productTypeName.ToLower(), "transaction_x64");
            var armTransactionSourcePath = Path.Combine(PackPath.PackResourcePath, _productTypeName.ToLower(), "transaction_arm");
            var amdTransactionTargetPath = Path.Combine(_amdAppTargetPath, "transaction");
            var armTransactionTargetPath = Path.Combine(_armAppTargetPath, "transaction");

            CopyFilesTo(_amdAppSourcePath, _armAppSourcePath, _amdAppTargetPath, _armAppTargetPath);
            CopyLibFilesTo(amdLibSourcePath, armLibSourcePath, amdLibTargetPath, armLibTargetPath);
            CopyResFilesTo(baseResPath, _amdAppTargetPath, _armAppTargetPath);
            CopyFilesTo(amdTransactionSourcePath, armTransactionSourcePath, amdTransactionTargetPath, armTransactionTargetPath);

            ModifyProperties();
            BuildDeb();
        }
        public void Complete(string outputPath)
        {
            var linuxPackPath = Path.Combine(_packTemplatePathTemp, "LinuxPublish", "Linux_Pack");
            DirectoryInfo linuxPackDirectory = new DirectoryInfo(linuxPackPath);
            foreach (var debFile in linuxPackDirectory.GetFiles("*.deb"))
            {
                var targetPath = Path.Combine(outputPath, debFile.Name);
                File.Copy(debFile.FullName, targetPath, true);
            }
            DeleteDirectory(_packTemplatePathTemp);
        }

        private void CopyFilesTo(string amdSourcePath, string armSourcePath, string amdTargetPath, string armTargetPath)
        {
            DirectoryInfo amdSourceDirectory = new DirectoryInfo(amdSourcePath);
            DirectoryInfo armSourceDirectory = new DirectoryInfo(armSourcePath);
            if (amdSourceDirectory.Exists)
            {
                CopyTo(amdSourcePath, amdTargetPath);
            }
            else
            {
                Console.WriteLine($"未找到文件夹 {amdSourcePath}");
            }
            if (armSourceDirectory.Exists)
            {
                CopyTo(armSourcePath, armTargetPath);
            }
            else
            {
                Console.WriteLine($"未找到文件夹 {armSourcePath}");
            }
        }
        private void CopyLibFilesTo(string amdLibSourcePath, string armLibSroucePath, string amdLibTargetPath, string armLibTargetPath)
        {
            DirectoryInfo amdLibTargetDirectory = new DirectoryInfo(amdLibTargetPath);
            DirectoryInfo armLibTargetDirectory = new DirectoryInfo(armLibTargetPath);
            if (amdLibTargetDirectory.Exists)
            {
                DeleteDirectory(amdLibTargetPath);
            }
            if (armLibTargetDirectory.Exists)
            {
                DeleteDirectory(armLibTargetPath);
            }
            CopyFilesTo(amdLibSourcePath, armLibSroucePath, amdLibTargetPath, armLibTargetPath);
        }
        private void CopyResFilesTo(string resourcePath, string amdResTargetPath, string armResTargetPath)
        {
            DirectoryInfo resourceDiectory = new DirectoryInfo(resourcePath);
            if (resourceDiectory.Exists)
            {
                CopyToTarget(resourcePath, amdResTargetPath);
                CopyToTarget(resourcePath, armResTargetPath);
            }
            else
            {
                Console.WriteLine($"未找到文件夹 {resourcePath}");
            }
        }
        /// <summary>
        /// 修改Resources.xml
        /// 修改版本号
        /// 修改是否是预发布版本
        /// </summary>
        /// <returns></returns>
        private void ModifyProperties()
        {
            const string properties = "Properties";
            const string resourcesFile = "Resources.xml";

            var amdFilePath = Path.Combine(_amdAppTargetPath, properties, resourcesFile);
            var armFilePath = Path.Combine(_armAppTargetPath, properties, resourcesFile);
            var ret = ModifyProperty(amdFilePath);
            if (!ret)
            {
                throw new Exception("修改amd Properties Resources.xml 报错");
            }
            ret = ModifyProperty(armFilePath);
            if (!ret)
            {
                throw new Exception("修改arm Properties Resources.xml 报错");
            }
        }
        /// <summary>
        /// 编译Deb包
        /// </summary>
        private void BuildDeb()
        {
            ExecuteCommand(@"chmod a+x build.sh");
            ExecuteCommand($@"./build.sh amd64 {_version}");
            ExecuteCommand($@"./build.sh arm64 {_version}");
        }
        /// <summary>
        /// 执行命令行
        /// </summary>
        /// <param name="command"></param>
        private void ExecuteCommand(string command)
        {
            var workingDriectory = Path.Combine(_packTemplatePathTemp, "LinuxPublish", "Linux_Pack");
            Process process = new Process();
            process.StartInfo.FileName = "/bin/bash";
            process.StartInfo.Arguments = command;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.WorkingDirectory = workingDriectory;
            process.Start();
            process.WaitForExit();
        }
        /// <summary>
        /// 将文件夹中的内容复制到指定位置
        /// </summary>
        /// <param name="sourcePath"></param>
        /// <param name="destPath"></param>
        private void CopyTo(string sourcePath, string destPath)
        {
            DirectoryInfo di = Directory.CreateDirectory(destPath);
            string[] files = Directory.GetFileSystemEntries(sourcePath);

            foreach (string file in files)
            {
                if (Directory.Exists(file))
                {
                    var filePath = Path.GetFileName(file);
                    var targetPath = Path.Combine(destPath, filePath);
                    CopyTo(file, targetPath);
                }
                else
                {
                    File.Copy(file, Path.Combine(di.FullName, Path.GetFileName(file)), true);
                }
            }
        }
        /// <summary>
        /// 将文件夹复制到指定位置
        /// </summary>
        /// <param name="sourcePath"></param>
        /// <param name="destPath"></param>
        private void CopyToTarget(string sourcePath, string destPath)
        {
            string floderName = Path.GetFileName(sourcePath);
            DirectoryInfo di = Directory.CreateDirectory(Path.Combine(destPath, floderName));
            string[] files = Directory.GetFileSystemEntries(sourcePath);

            foreach (string file in files)
            {
                if (Directory.Exists(file))
                {
                    CopyToTarget(file, di.FullName);
                }
                else
                {
                    File.Copy(file, Path.Combine(di.FullName, Path.GetFileName(file)), true);
                }
            }
        }
        /// <summary>
        /// 删除文件夹
        /// </summary>
        /// <param name="path"></param>
        private void DeleteDirectory(string path)
        {
            DirectoryInfo dir = new DirectoryInfo(path);
            if (dir.Exists)
            {
                DirectoryInfo[] childs = dir.GetDirectories();
                foreach (DirectoryInfo child in childs)
                {
                    child.Delete(true);
                }
                dir.Delete(true);
            }
        }
        /// <summary>
        /// 修改Resource.xml文件内容
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        private bool ModifyProperty(string filePath)
        {
            const string ptnXmlPath = "ApplicationConfig/ProductTypeName";
            const string verXmlPath = "ApplicationConfig/Version";
            const string isPreXmlPath = "ApplicationConfig/IsPreRelease";

            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.Load(filePath);
            var productTypeName = xmlDocument.SelectSingleNode(ptnXmlPath);
            var version = xmlDocument.SelectSingleNode(verXmlPath);
            var isPreRelease = xmlDocument.SelectSingleNode(isPreXmlPath);

            if (productTypeName == null || version == null || isPreRelease == null)
            {
                return false;
            }
            productTypeName.InnerText = _productTypeName;
            version.InnerText = _version;
            isPreRelease.InnerText = _isPreRelease.ToString().ToLower();
            xmlDocument.Save(filePath);
            return true;
        }
    }
}
