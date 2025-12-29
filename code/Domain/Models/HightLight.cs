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
}
