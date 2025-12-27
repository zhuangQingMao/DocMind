namespace DocMind
{
    public interface IVectorRepository
    {
        Task<List<ChunkSortResult>> FindRelevantChunks(float[] queryVector, int topK);
        Task SaveVectorAsync(string fileName, int chunkIndex, string text, float[] vector);
    }
}
