using LinuxPackFile;
using System.Text.RegularExpressions;

namespace LinuxPackFileConsole
{
    public class PackFileManager
    {
        private readonly string _outputFolderPath;
        private readonly string _productName;

        /// <summary>
        /// 待进行内容修改的文件列表
        /// </summary>
        public List<string> ModifyFileNameList { get; private set; }
        /// <summary>
        /// 待进行重命名的文件夹字典
        /// </summary>
        public Dictionary<string, string> ReplaceFolderNameDic { get; private set; }
        /// <summary>
        /// 待进行重命名的文件字典
        /// </summary>
        public Dictionary<string, string> ReplaceFileNameDic { get; private set; }
        /// <summary>
        /// 待进行修改的内容字典
        /// </summary>
        public Dictionary<string, string> ReplaceStrDic { get; private set; }

        public PackFileManager(
            string outputFolderPath,
            string productName)
        {
            _outputFolderPath = outputFolderPath;
            _productName = productName;

            ModifyFileNameList = new List<string>();
            ReplaceFolderNameDic = new Dictionary<string, string>();
            ReplaceFileNameDic = new Dictionary<string, string>();
            ReplaceStrDic = new Dictionary<string, string>();
        }
        public PackFileManager(
            string outputFolderPath,
            string productName,
            string chineseName) : this(outputFolderPath, productName)
        {
            ModifyFileNameList.Add("control");
            ModifyFileNameList.Add("md5sums");
            ModifyFileNameList.Add("postinst");
            ModifyFileNameList.Add("postrm");
            ModifyFileNameList.Add($"cn.com.{productName}.desktop");
            ModifyFileNameList.Add("info");
            ModifyFileNameList.Add("build.sh");

            ReplaceFolderNameDic.Add("cn.com.10jqka", $"cn.com.{productName}");
            ReplaceFileNameDic.Add("cn.com.10jqka.desktop", $"cn.com.{productName}.desktop");

            ReplaceStrDic.Add("cn.com.10jqka", $"cn.com.{productName}");
            ReplaceStrDic.Add("HevoNext.10jqka.B2BApp", $"HevoNext.{productName}.B2BApp");
            ReplaceStrDic.Add("同花顺Beta", chineseName);
            ReplaceStrDic.Add("同花顺Linux", chineseName);
            ReplaceStrDic.Add("同花顺炒股软件", chineseName);
        }
        public void DoWork()
        {
            CopyToTarget(PackPath.PackTemplateFolderPath, _outputFolderPath);
            foreach (var keyPair in ReplaceFolderNameDic)
            {
                FolderMoveTo(_outputFolderPath, keyPair.Key, keyPair.Value);
            }
            foreach (var keyPair in ReplaceFileNameDic)
            {
                FileMoveTo(_outputFolderPath, keyPair.Key, keyPair.Value);
            }
            ReplaceAllFiles(_outputFolderPath, ModifyFileNameList, ReplaceStrDic);
            
            PackResourceMoveTo(_outputFolderPath, PackPath.PackResourcePath, _productName);
        }

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
        /// 文件夹重命名
        /// </summary>
        /// <param name="rootPath">根路径</param>
        /// <param name="sourceName">源文件夹名</param>
        /// <param name="destName">新文件夹名</param>
        private void FolderMoveTo(string rootPath, string sourceName, string destName)
        {
            var directoryInfo = new DirectoryInfo(rootPath);
            var children = directoryInfo.GetDirectories();

            foreach (var child in children)
            {
                var childFolder = child.FullName;
                if (child.Name.Equals(sourceName))
                {
                    var destFolder = child.FullName.Replace(sourceName, destName);
                    if (child.Exists)
                    {
                        child.MoveTo(destFolder);
                        childFolder = destFolder;
                    }
                }
                FolderMoveTo(childFolder, sourceName, destName);
            }
        }
        /// <summary>
        /// 文件重命名
        /// </summary>
        /// <param name="rootPath">根路径</param>
        /// <param name="sourceName">源文件名</param>
        /// <param name="destName">新文件名</param>
        private void FileMoveTo(string rootPath, string sourceName, string destName)
        {
            var directoryInfo = new DirectoryInfo(rootPath);
            var children = directoryInfo.GetFiles("*.*", SearchOption.AllDirectories);
            foreach (var child in children)
            {
                if (child.Name.Equals(sourceName))
                {
                    var destFile = child.FullName.Replace(sourceName, destName);
                    child.MoveTo(destFile);
                }
            }
        }
        /// <summary>
        /// 修改文件内容
        /// 修改根路径下所有replaceFile标识的文件内容
        /// 内容修改根据replaceContent
        /// </summary>
        /// <param name="rootPath">根路径</param>
        /// <param name="replaceFile">需要进行内容修改的文件集合</param>
        /// <param name="replaceContent">内容替换方案</param>
        private void ReplaceAllFiles(string rootPath, List<string> replaceFile, Dictionary<string, string> replaceContent)
        {
            var directoryInfo = new DirectoryInfo(rootPath);
            var fileInfos = directoryInfo.GetFiles("*.*", SearchOption.AllDirectories);
            foreach (var fileInfo in fileInfos)
            {
                if (replaceFile.Contains(fileInfo.Name) && fileInfo.Exists)
                {
                    ReplaceContent(fileInfo.FullName);
                }
            }

            void ReplaceContent(string filePath)
            {
                var sourceText = File.ReadAllText(filePath);
                foreach (var keyPair in replaceContent)
                {
                    sourceText = Regex.Replace(sourceText, keyPair.Key, keyPair.Value);
                }
                File.WriteAllText(filePath, sourceText);
            }
        }
        /// <summary>
        /// 将打包模板所需资源拷贝的目录中
        /// </summary>
        /// <param name="rootPath">根路径</param>
        /// <param name="resourcePath">资源所在位置</param>
        /// <param name="productName">项目名</param>
        private void PackResourceMoveTo(string rootPath, string resourcePath, string productName)
        {
            var targetFilePath = Path.Combine(resourcePath, productName, "HevoIcon.svg");
            if (!File.Exists(targetFilePath))
            {
                return;
            }
            var directoryInfo = new DirectoryInfo(rootPath);
            var fileInfos = directoryInfo.GetFiles("HevoIcon.svg", SearchOption.AllDirectories);

            foreach (var fileInfo in fileInfos)
            {
                File.Copy(targetFilePath, fileInfo.FullName, true);
            }
        }
    }
}
