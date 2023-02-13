using LinuxPackFile.Model;
using System.Diagnostics;
using System.IO.Compression;
using System.Xml;

namespace LinuxPackFile
{
    /// <summary>
    /// 拼接用于打包的文件并打包
    /// </summary>
    public class PackPublishAppManager
    {
        private readonly string _baseDirectory;

        public PackPublishAppManager(string baseDirectory)
        {
            _baseDirectory = baseDirectory;
        }

        /// <summary>
        /// 拼接文件，进行打包
        /// </summary>
        public void DoWork(string packTemplatePathTemp, string productIdentity, string version, bool isPreRelease)
        {
            var baseResPath = Path.Combine(PackPath.PackResourcePath, productIdentity.ToLower(), "res");

            var amdAppSourcePath = Path.Combine(_baseDirectory, version, PackPath.LINUX_X64_APPLICATION);
            var armAppSourcePath = Path.Combine(_baseDirectory, version, PackPath.LINUX_ARM_APPLICATION);
            var amdAppTargetPath = Path.Combine(packTemplatePathTemp, PackPath.LINUX_TEMPLATE, PackPath.LINUX_X64_APPLICATION, "quotes");
            var armAppTargetPath = Path.Combine(packTemplatePathTemp, PackPath.LINUX_TEMPLATE, PackPath.LINUX_ARM_APPLICATION, "quotes");

            var amdLibSourcePath = Path.Combine(PackPath.PackResourcePath, "base", "lib_x64");
            var armLibSourcePath = Path.Combine(PackPath.PackResourcePath, "base", "lib_arm");
            var amdLibTargetPath = Path.Combine(amdAppTargetPath, "lib");
            var armLibTargetPath = Path.Combine(armAppTargetPath, "lib");

            var amdTransactionSourcePath = Path.Combine(PackPath.PackResourcePath, productIdentity.ToLower(), "transaction_x64");
            var armTransactionSourcePath = Path.Combine(PackPath.PackResourcePath, productIdentity.ToLower(), "transaction_arm");
            var amdTransactionTargetPath = Path.Combine(amdAppTargetPath, "..", "transaction");
            var armTransactionTargetPath = Path.Combine(armAppTargetPath, "..", "transaction");

            if (!Directory.Exists(amdAppSourcePath))
            {
                throw new Exception($"拼接文件并打包：未找到x64应用{version}公版包");
            }
            if (!Directory.Exists(armAppSourcePath))
            {
                throw new Exception($"拼接文件并打包：未找到arm应用{version}公版包");
            }
            if (!Directory.Exists(amdLibSourcePath))
            {
                throw new Exception("拼接文件并打包：未找到lib x64依赖库");
            }
            if (!Directory.Exists(armLibSourcePath))
            {
                throw new Exception("拼接文件并打包：未找到lib arm依赖库");
            }

            CopyFilesTo(amdAppSourcePath, armAppSourcePath, amdAppTargetPath, armAppTargetPath);
            CopyLibFilesTo(amdLibSourcePath, armLibSourcePath, amdLibTargetPath, armLibTargetPath);
            CopyResFilesTo(baseResPath, amdAppTargetPath, armAppTargetPath);
            CopyFilesTo(amdTransactionSourcePath, armTransactionSourcePath, amdTransactionTargetPath, armTransactionTargetPath);

            ModifyProperties(amdAppTargetPath, armAppTargetPath, productIdentity, version, isPreRelease);
            BuildDeb(version, packTemplatePathTemp);
        }
        /// <summary>
        /// 完成后将生成的两个deb文件拷贝到指定位置
        /// </summary>
        /// <param name="outputPath">生成文件位置</param>
        public void Complete(string outputPath, string packTemplatePathTemp)
        {
            var linuxPackPath = Path.Combine(packTemplatePathTemp, PackPath.LINUX_TEMPLATE, PackPath.LINUX_PACK_TEMPLATE);
            DirectoryInfo linuxPackDirectory = new DirectoryInfo(linuxPackPath);
            if (!linuxPackDirectory.Exists)
            {
                return;
            }
            foreach (var debFile in linuxPackDirectory.GetFiles("*.deb"))
            {
                var targetPath = Path.Combine(outputPath, debFile.Name);
                File.Copy(debFile.FullName, targetPath, true);
            }
            DeleteDirectory(packTemplatePathTemp);
        }
        /// <summary>
        /// 完成后返回生成的两个deb文件流
        /// </summary>
        /// <returns>文件流</returns>
        public IEnumerable<FileInfo> Complete(string packTemplatePathTemp)
        {
            var debFiles = new List<FileInfo>();
            var linuxPackPath = Path.Combine(packTemplatePathTemp, PackPath.LINUX_TEMPLATE, PackPath.LINUX_PACK_TEMPLATE);
            DirectoryInfo linuxPackDirectory = new DirectoryInfo(linuxPackPath);
            if (!linuxPackDirectory.Exists)
            {
                return debFiles;
            }
            foreach (var debFile in linuxPackDirectory.GetFiles("*.deb"))
            {
                debFiles.Add(debFile);
            }
            return debFiles;
        }
        public void DeleteTempDirectory(string packTemplatePathTemp)
        {
            DeleteDirectory(packTemplatePathTemp);
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
        private void ModifyProperties(string amdAppTargetPath, string armAppTargetPath, string productIdentity, string versionParam, bool isPreRelesseParam)
        {
            const string properties = "Properties";
            const string resourcesFile = "Resources.xml";

            var amdFilePath = Path.Combine(amdAppTargetPath, properties, resourcesFile);
            var armFilePath = Path.Combine(armAppTargetPath, properties, resourcesFile);
            var ret = ModifyProperty(amdFilePath, productIdentity, versionParam, isPreRelesseParam);
            if (!ret)
            {
                throw new Exception("修改amd Properties Resources.xml 报错");
            }
            ret = ModifyProperty(armFilePath, productIdentity, versionParam, isPreRelesseParam);
            if (!ret)
            {
                throw new Exception("修改arm Properties Resources.xml 报错");
            }
        }
        /// <summary>
        /// 编译Deb包
        /// </summary>
        private void BuildDeb(string version, string packTemplatePathTemp)
        { 
            ExecuteCommand(@"chmod a+x build.sh", packTemplatePathTemp);
            ExecuteCommand($@"./build.sh amd64 {version}", packTemplatePathTemp);
            ExecuteCommand($@"./build.sh arm64 {version}", packTemplatePathTemp);
        }
        /// <summary>
        /// 执行命令行
        /// </summary>
        /// <param name="command"></param>
        private void ExecuteCommand(string command, string packTemplatePathTemp)
        {
            var workingDriectory = Path.Combine(packTemplatePathTemp, PackPath.LINUX_TEMPLATE, PackPath.LINUX_PACK_TEMPLATE);
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
        /// <param name="destPath"></param>13333333
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
        private bool ModifyProperty(string filePath, string productIdentity, string versionParam, bool isPreRelesseParam)
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
            productTypeName.InnerText = productIdentity;
            version.InnerText = versionParam;
            isPreRelease.InnerText = isPreRelesseParam.ToString().ToLower();
            xmlDocument.Save(filePath);
            return true;
        }
    }
}
