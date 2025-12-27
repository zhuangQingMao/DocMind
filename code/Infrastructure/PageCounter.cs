using Aspose.Words;

namespace DocMind
{
    public class WordChunk
    {
        public int PageNumber { get; set; }
        public string Content { get; set; } = string.Empty;
    }

    public class PageContentExtractor
    {
        private const int MAX_CHUNK_SIZE = 500;

        public static List<WordChunk> SplitLongChunks(Dictionary<int, string> inputDictionary)
        {
            var outputChunks = new List<WordChunk>();

            foreach (var entry in inputDictionary)
            {
                int pageNumber = entry.Key;
                string content = entry.Value;

                int currentIndex = 0;

                while (currentIndex < content.Length)
                {
                    int length = Math.Min(MAX_CHUNK_SIZE, content.Length - currentIndex);

                    string chunkContent = content.Substring(currentIndex, length);

                    outputChunks.Add(new WordChunk { PageNumber = pageNumber, Content = chunkContent });

                    currentIndex += length; 
                }
            }

            return outputChunks;
        }

        public static List<WordChunk> ExtractPages(string filePath)
        {
            try
            {
                var pageContents = new Dictionary<int, string>();
                var doc = new Document(filePath);
                doc.UpdatePageLayout();
                var totalPages = doc.PageCount;

                for (int pageIndex = 0; pageIndex < totalPages; pageIndex++)
                {
                    int pageNumber = pageIndex + 1;

                    Document onePageDoc = doc.ExtractPages(pageIndex, 1);

                    string pageText = onePageDoc.GetText();

                    pageText = CleanText(pageText);

                    pageContents.Add(pageNumber, pageText);
                }

                return SplitLongChunks(pageContents);
            }
            catch (Exception ex)
            {
                throw new Exception($"处理文档时发生错误: {ex.Message}");
            }
        }

        private static string CleanText(string text)
        {
            return text.Replace("\x000c", "")
                       .Replace(ControlChar.SectionBreak, "")
                       .Replace(@"Created with an evaluation copy of Aspose.Words. To remove all limitations, you can use Free Temporary License  HYPERLINK ""https://products.aspose.com/words/temporary-license/"" https://products.aspose.com/words/temporary-license/", "")
                       .Replace(@"Evaluation Only. Created with Aspose.Words. Copyright 2003-2025 Aspose Pty Ltd.", "")
                       .Trim();
        }
    }
}
