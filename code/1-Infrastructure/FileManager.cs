using Aspose.Words;
using DocMind.code.Infrastructure;
using System.Data;
using System.IO;
using System.Windows.Xps.Packaging;

namespace DocMind
{
    #region 工厂

    //抽象工厂
    public interface IFileManagerFactory
    {
        IFileManager CreateManager(string filePath);
    }

    //工厂
    public class FileManagerFactory : IFileManagerFactory
    {
        public IFileManager CreateManager(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentNullException(nameof(filePath), "文件路径不能为空。");

            var ext = Path.GetExtension(filePath).ToLower();

            if (ext.EndsWith("txt"))
                return new TxtManager(filePath);

            if (ext.EndsWith("docx"))
                return new WordManager(filePath);

            throw new NotSupportedException($"不支持的文件后缀: {ext}");
        }
    }

    #endregion

    #region 具体

    //最上层的抽象
    public interface IFileManager
    {
        Task<string> GetContent();
        Task<object> GetDisplay();
    }

    //泛型接口，实现多个不同类型的具体实现
    public interface IFileManager<TFileMeta> : IFileManager
    {
        Task<TFileMeta> GetFileInfo();
    }

    //抽象类，把冗余部分抽象到此类
    public abstract class FileManager<TFileMeta>(string filePath) : IFileManager<TFileMeta>
    {
        public string FilePath { get; } = filePath;
        private TFileMeta? FileCache { get; set; }
        public virtual async Task<TFileMeta> GetFileInfo()
        {
            FileCache ??= await LoadFileMetaInternalAsync();

            return FileCache;
        }
        public abstract Task<string> GetContent();
        public abstract Task<object> GetDisplay();
        protected abstract Task<TFileMeta> LoadFileMetaInternalAsync();
    }

    public class TxtManager(string filePath) : FileManager<string>(filePath)
    {
        protected override async Task<string> LoadFileMetaInternalAsync()
            => await File.ReadAllTextAsync(FilePath);

        public override async Task<string> GetContent()
            => await GetFileInfo();

        public override async Task<object> GetDisplay()
        {
            var str = await GetContent();
            return FlowDocumentHelper.ConvertToFlowDocument(str);
        }
    }

    public class WordManager(string filePath) : FileManager<List<WordChunk>>(filePath)
    {
        protected override Task<List<WordChunk>> LoadFileMetaInternalAsync()
        {
            var chunk = PageContentExtractor.ExtractPages(FilePath);
            return Task.FromResult(chunk);
        }

        public override async Task<string> GetContent()
        {
            var fileMeta = await GetFileInfo();

            if (fileMeta == null || fileMeta.Count == 0)
                return "";

            return string.Join("", fileMeta.Select(a => a.Content));
        }

        public override Task<object> GetDisplay()
        {
            var appPath = AppDomain.CurrentDomain.BaseDirectory;
            appPath = Path.Combine(appPath, "WORD");

            if (Directory.Exists(appPath) == false)
                Directory.CreateDirectory(appPath);

            var xpsPath = Path.Combine(appPath, $"{Path.GetFileNameWithoutExtension(FilePath)}.xps");

            var doc = new Document(FilePath);
            doc.Save(xpsPath, SaveFormat.Xps);

            using XpsDocument xpsDoc = new(xpsPath, FileAccess.Read);
            var fixedDocSeq = xpsDoc.GetFixedDocumentSequence() ?? throw new Exception("空的xps内容");

            return Task.FromResult<object>(fixedDocSeq);
        }
    }

    #endregion
}
