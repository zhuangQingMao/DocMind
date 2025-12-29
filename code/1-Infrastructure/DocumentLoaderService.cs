using System.IO;

namespace DocMind
{
    public interface IDocumentLoaderService
    {
        Task<DocumentFile> LoadFromFileAsync(string path);
    }

    public class DocumentLoaderService(IFileManagerFactory factory) : IDocumentLoaderService
    {
        private readonly IFileManagerFactory _fileFactory = factory;

        public async Task<DocumentFile> LoadFromFileAsync(string path)
        {
            var fileInfo = new FileInfo(path);

            var fileManager = _fileFactory.CreateManager(path);

            object fileMeta;

            if (fileManager is IFileManager<string> txtManager)
                fileMeta = await txtManager.GetFileInfo();
            else if (fileManager is IFileManager<List<WordChunk>> wordManager)
                fileMeta = await wordManager.GetFileInfo();
            else
                throw new NotSupportedException($"文件管理器类型不支持: {fileManager.GetType().Name}");

            return new DocumentFile
            {
                FileSizeKB = (double)fileInfo.Length / 1024,
                FileName = fileInfo.Name,
                FilePath = path,
                FileSize = fileInfo.Length,
                FileMeta = fileMeta,
                Content = await fileManager.GetContent(),
                Display = await fileManager.GetDisplay(),
                FileType = GetFileType(fileInfo.Name)
            };
        }

        private static FileType GetFileType(string fileName)
        {
            if (fileName.EndsWith(".txt", comparisonType: StringComparison.OrdinalIgnoreCase))
                return FileType.Txt;

            if (fileName.EndsWith(".docx", comparisonType: StringComparison.OrdinalIgnoreCase))
                return FileType.Word;

            throw new Exception($"无法识别文件类型：{fileName}");
        }
    }
}
