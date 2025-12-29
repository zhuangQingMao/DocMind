namespace DocMind
{
    public interface IDocumentChunker
    {
        List<ChunkData> ChunkAndGenerateMetadata(string fileName, object fileMeta);
    }

    public class DocumentChunker : IDocumentChunker
    {
        private const int MaxChunkSize = 500;
        private const int OverlapSize = 50;

        public List<ChunkData> ChunkAndGenerateMetadata(string fileName, object fileMeta)
        {
            string fileExtension = System.IO.Path.GetExtension(fileName).ToLower();

            return fileExtension switch
            {
                ".txt" => ChunkTxtDocument(fileMeta),
                ".docx" => ChunkWordDocument(fileMeta),
                _ => throw new Exception($"不支持的文件类型：{fileExtension}"),
            };
        }

        private static List<ChunkData> ChunkWordDocument(object fileMeta)
        {
            var res = new List<ChunkData>();
            var fileContent = fileMeta as List<WordChunk> ?? throw new Exception("fileMeta convert to content failed");

            if (fileContent.Count == 0)
                return res;

            foreach (var item in fileContent)
            {
                res.Add(new ChunkData()
                {
                    Text = item.Content,
                    ChunkIndex = item.PageNumber
                });
            }

            return res;
        }

        private static List<ChunkData> ChunkTxtDocument(object fileMeta)
        {
            var fileContent = fileMeta as string;
            if (string.IsNullOrWhiteSpace(fileContent))
                throw new Exception("fileMeta convert to content failed");

            var chunks = new List<ChunkData>();
            var currentIndex = 0;
            var chunkIndex = 0;
            var totalLenth = fileContent.Length;

            while (currentIndex < totalLenth)
            {
                var length = Math.Min(MaxChunkSize, totalLenth - currentIndex);
                var chunkText = fileContent.Substring(currentIndex, length);

                chunks.Add(new ChunkData
                {
                    Text = chunkText,
                    ChunkIndex = chunkIndex
                });

                var endIndex = currentIndex + length;
                currentIndex = endIndex - OverlapSize;

                if (currentIndex < 0)
                    currentIndex = 0;

                chunkIndex++;

                if (endIndex == totalLenth)
                    break;
            }

            return chunks;
        }
    }

    public class ChunkData
    {
        public int ChunkIndex { get; set; }
        public string Text { get; set; } = string.Empty;
    }
}
