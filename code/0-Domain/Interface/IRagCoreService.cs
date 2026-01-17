namespace DocMind
{
    public interface IRagCoreService
    {
        IAsyncEnumerable<string> firstChat_Stream(string userQuestion, string context, FileType type);
        Task<string> GetOriginContext(string userQuestion, FileType type, string fileId);
        Task ImportDocumentAsync(string fileName, DocumentFile docFile);
        Task<string> secondChat(string context, string firstAnswer, FileType type);
        Task ClearDocAsync(string id);
    }
}