using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DocMind
{
    public class HighlightMessage
    {
        public string TargetFileName { get; }

        //public List<ChunkMetadata> MetadataToHighlight { get; set; }

        public string SecondAnswer { get; set; }

        public HighlightMessage(string targetFileName)//, List<ChunkMetadata> metadata
        {
            TargetFileName = targetFileName;
            //MetadataToHighlight = metadata;
        }
    }

    public class HighlightSpan
    {
        // 句子在整个原始文档文本中的起始字符索引
        public int StartCharIndex { get; set; }

        // 句子在整个原始文档文本中的结束字符索引
        public int EndCharIndex { get; set; }

        public string targetText { get; set; }


    }
}
