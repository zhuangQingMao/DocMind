namespace DocMind
{
    public interface IVectorRepository
    {
        Task<List<ChunkSortResult>> FindRelevantChunks(string fileId, float[] queryVector, int topK);
        Task SaveVectorAsync(string id, string fileName, int chunkIndex, string text, float[] vector);
        Task ClearDocAsync(string id);
    }
}
