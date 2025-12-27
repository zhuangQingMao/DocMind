namespace DocMind
{
    public interface IRagCoreService
    {
        IAsyncEnumerable<string> firstChat_Stream(string userQuestion, string context, FileType type);
        Task<string> GetOriginContext(string userQuestion, FileType type);
        Task ImportDocumentAsync(string fileName, object fileMeta);
        Task<string> secondChat(string context, string firstAnswer, FileType type);
    }
}