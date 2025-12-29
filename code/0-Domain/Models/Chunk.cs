namespace DocMind
{
    public class ChunkSortResult
    {
        public required ChunkRecord Record { get; set; }
        public float Score { get; set; }
        public int PageNumber { get; set; }
    }

    public class ChunkRecord
    {
        public string OriginalText { get; set; }
        public int ChunkIndex { get; set; }
        public float SimilarityScore { get; set; }
        public float[] Vector { get; set; } // 用于内部计算，不必暴露给 UI
    }
}
