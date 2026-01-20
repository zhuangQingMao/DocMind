namespace DocMind
{
    public interface IVectorRepository
    {
        Task<(bool, List<ChunkSortResult>)> FindRelevantChunks(string fileId, float[] queryVector);
        Task SaveVectorAsync(string id, string fileName, int chunkIndex, string text, float[] vector);
        Task ClearDocAsync(string id);
    }
}
