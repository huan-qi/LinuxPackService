using System.Text.RegularExpressions;

namespace LinuxPackFileConsole
{
    public class PackFileManager
    {
        private readonly string _sourceFolderPath;
        private readonly string _resourceFolderPath;
        private readonly string _outputFolderPath;
        private readonly string _productName;

        public List<string> ModifyFileNameList { get; private set; }
        public Dictionary<string, string> ReplaceFolderNameDic { get; private set; }
        public Dictionary<string, string> ReplaceFileNameDic { get; private set; }
        public Dictionary<string, string> ReplaceStrDic { get; private set; }

        public PackFileManager(
            string sourceFolderPath,
            string resourceFolderPath,
            string outputFolderPath,
            string productName)
        {
            _sourceFolderPath = sourceFolderPath;
            _resourceFolderPath = resourceFolderPath;
            _outputFolderPath = outputFolderPath;
            _productName = productName;

            ModifyFileNameList = new List<string>();
            ReplaceFolderNameDic = new Dictionary<string, string>();
            ReplaceFileNameDic = new Dictionary<string, string>();
            ReplaceStrDic = new Dictionary<string, string>();
        }
        public PackFileManager(
            string sourceFolderPath,
            string resourceFolderPath,
            string outputFolderPath,
            string productName,
            string chineseName) : this(sourceFolderPath, resourceFolderPath, outputFolderPath, productName)
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
            CopyToTarget(_sourceFolderPath, _outputFolderPath);
            foreach (var keyPair in ReplaceFolderNameDic)
            {
                ChildFolderMoveTo(_outputFolderPath, keyPair.Key, keyPair.Value);
            }
            foreach (var keyPair in ReplaceFileNameDic)
            {
                FileMoveTo(_outputFolderPath, keyPair.Key, keyPair.Value);
            }
            ReplaceAllFiles(_outputFolderPath, ModifyFileNameList, ReplaceStrDic);
            PackResourceMoveTo(_outputFolderPath, _resourceFolderPath, _productName);
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
        private void ChildFolderMoveTo(string rootPath, string sourceName, string destName)
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
                ChildFolderMoveTo(childFolder, sourceName, destName);
            }
        }
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
