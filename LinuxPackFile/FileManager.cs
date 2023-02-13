using ICSharpCode.SharpZipLib.Zip;
using LinuxPackFile.Model;

namespace LinuxPackFile
{
    public class FileManager
    {
        private readonly string _baseDirectory;

        public FileManager(string baseDirectory)
        {
            _baseDirectory = baseDirectory;
        }

        /// <summary>
        /// 上传交易
        /// </summary>
        /// <param name="productIdentity">券商标识</param>
        /// <param name="cpuType">Cpu类型</param>
        /// <param name="zipFileStream">zip压缩包文件流</param>
        /// <returns></returns>
        public IEnumerable<string> UploadTransaction(string productIdentity, CPU_TYPE cpuType, Stream zipFileStream)
        {
            if (cpuType == CPU_TYPE.None)
            {
                return new List<string>();
            }
            var unZipPath = cpuType == CPU_TYPE.X64 ? "transaction_x64" : "transaction_arm";
            var targetPath = Path.Combine(PackPath.PackResourcePath, productIdentity, unZipPath);
            return UnZip(zipFileStream, targetPath);
        }
        /// <summary>
        /// 上传行情应用
        /// </summary>
        /// <param name="cpuType">Cpu类型</param>
        /// <param name="zipFileStream">zip压缩包文件流</param>
        /// <returns></returns>
        public IEnumerable<string> UploadLinuxApplication(Stream zipFileStream, string folderName)
        {
            var targetDirectory = Path.Combine(_baseDirectory, folderName);
            return UnZip(zipFileStream, targetDirectory);
        }


        /// <summary>  
        /// 解压缩zip文件(压缩文件中含有子目录)  
        /// </summary>  
        /// <param name="zipFilePath">待解压缩的文件路径</param>  
        /// <param name="unzipPath">解压缩到指定目录</param>  
        /// <returns>解压后的文件列表</returns>  
        private List<string> UnZip(string zipFilePath, string unzipPath)
        {
            using var zipfileStream = File.OpenRead(zipFilePath);
            return UnZip(zipfileStream, unzipPath);
        }
        /// <summary>
        /// 解压缩zip文件流
        /// </summary>
        /// <param name="zipFileStream">待解压缩的文件流</param>
        /// <param name="unzipPath">解压缩到指定目录</param>
        /// <returns>解压后的文件列表</returns>
        private List<string> UnZip(Stream zipFileStream, string unzipPath)
        {
            //解压出来的文件列表  
            List<string> unzipFiles = new List<string>();
            using ZipInputStream zipInputStream = new ZipInputStream(zipFileStream);
            ZipEntry theEntry;
            while ((theEntry = zipInputStream.GetNextEntry()) != null)
            {
                string? directoryName = Path.GetDirectoryName(unzipPath);
                string fileName = Path.GetFileName(theEntry.Name);

                //生成解压目录【用户解压到硬盘根目录时，不需要创建】  
                if (!string.IsNullOrEmpty(directoryName))
                {
                    Directory.CreateDirectory(directoryName);
                }
                if (string.IsNullOrEmpty(fileName))
                {
                    continue;
                }
                if (theEntry.CompressedSize == 0)
                {
                    continue;
                }
                var upzipFilePath = Path.Combine(unzipPath, theEntry.Name);
                //解压文件到指定的目录  
                directoryName = Path.GetDirectoryName(upzipFilePath);
                if (string.IsNullOrEmpty(directoryName))
                {
                    continue;
                }
                //建立下面的目录和子目录  
                Directory.CreateDirectory(directoryName);
                //记录导出的文件  
                unzipFiles.Add(upzipFilePath);

                using (var streamWriter = File.Create(upzipFilePath))
                {
                    int size = 2048;
                    byte[] data = new byte[2048];
                    while (true)
                    {
                        size = zipInputStream.Read(data, 0, data.Length);
                        if (size > 0)
                        {
                            streamWriter.Write(data, 0, size);
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }
            return unzipFiles;
        }
    }
}
