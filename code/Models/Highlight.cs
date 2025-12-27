namespace DocMind
{
    public class HighlightSpan
    {
        // 句子在整个原始文档文本中的起始字符索引
        public int StartCharIndex { get; set; }

        // 句子在整个原始文档文本中的结束字符索引
        public int EndCharIndex { get; set; }

        public string targetText { get; set; }


    }

    public class RagResponse
    {
        public string Answer { get; set; } // LLM 生成的最终答案

        // 用于溯源和高亮的关键信息
        public List<HighlightSpan> SourceChunksMetadata { get; set; }

        // 原始 Chunk 文本，可选，用于调试
        // public List<string> ContextUsed { get; set; } 
    }
}
