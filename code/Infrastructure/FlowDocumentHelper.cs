using System.Windows;
using System.Windows.Documents;

namespace DocMind.code.Infrastructure
{
    public static class FlowDocumentHelper
    {
        public static FlowDocument ConvertToFlowDocument(string text)
        {
            var flowDoc = new FlowDocument();

            if (string.IsNullOrEmpty(text))
                return flowDoc;

            string[] lines = text.Split(['\n'], StringSplitOptions.None);

            foreach (string line in lines)
            {
                string cleanedLine = line.TrimEnd('\r');

                var paragraph = new Paragraph();

                paragraph.Inlines.Add(new Run(cleanedLine));

                paragraph.Margin = new Thickness(0);

                flowDoc.Blocks.Add(paragraph);
            }

            return flowDoc;
        }
    }
}
