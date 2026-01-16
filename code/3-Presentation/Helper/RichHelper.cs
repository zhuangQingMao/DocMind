using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace DocMind
{
    public static class RichTextBoxHighlighter
    {
        private static TextPointer? _start;
        private static TextPointer? _end;
        private static string _defaultHighLightColor = "#D6E4FF";

        public static void HighlightByTextContent(RichTextBox richTextBox, List<string> spans, bool isSingleSencente = false)
        {
            if (richTextBox == null)
                return;

            if (!isSingleSencente)
                ClearAllHighlights(richTextBox);

            var fullText = GetFullRenderedText(richTextBox.Document);
            var scrollViewer = FindVisualChild<ScrollViewer>(richTextBox);
            TextPointer? firstHighlightPosition = null;

            string colorCode = isSingleSencente ? "#FFFF00" : _defaultHighLightColor;

            foreach (var targetText in spans)
            {
                int startIndex = fullText.IndexOf(targetText, StringComparison.Ordinal);
                if (startIndex == -1)
                    continue;

                var startPos = FindPositionByCharacterIndex(richTextBox.Document, startIndex);
                var endPos = FindPositionByCharacterIndex(richTextBox.Document, startIndex + targetText.Length);

                if (startPos != null && endPos != null)
                {
                    firstHighlightPosition ??= startPos;

                    var bc = new BrushConverter();

                    var brush = (Brush?)bc.ConvertFromString(colorCode);

                    if (brush != null)
                        new TextRange(startPos, endPos).ApplyPropertyValue(TextElement.BackgroundProperty, brush);

                    if (isSingleSencente)
                    {
                        if (_start != null && _end != null)
                            ClearHighlight(richTextBox, _start, _end);

                        _start = startPos;
                        _end = endPos;
                    }
                }
            }

            if (firstHighlightPosition != null && scrollViewer != null)
            {
                Rect rect = firstHighlightPosition.GetCharacterRect(LogicalDirection.Forward);

                double targetOffset = rect.Top + scrollViewer.VerticalOffset;

                targetOffset = Math.Max(0, Math.Min(targetOffset, scrollViewer.ScrollableHeight));

                scrollViewer.ScrollToVerticalOffset(targetOffset);
            }
        }

        private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typedChild)
                    return typedChild;

                T? childOfChild = FindVisualChild<T>(child);
                if (childOfChild != null)
                    return childOfChild;
            }
            return null;
        }

        private static TextPointer? FindPositionByCharacterIndex(FlowDocument doc, int targetIndex)
        {
            if (targetIndex < 0)
                return null;

            TextPointer current = doc.ContentStart;
            int currentIndex = 0;

            while (current != null)
            {
                if (current.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.Text)
                {
                    var textRun = current.GetTextInRun(LogicalDirection.Forward);
                    if (textRun == null)
                        break;

                    for (int i = 0; i < textRun.Length; i++)
                    {
                        char c = textRun[i];
                        if (c == '\r' || c == '\n')
                            continue;

                        if (currentIndex == targetIndex)
                        {
                            return current.GetPositionAtOffset(i);
                        }
                        currentIndex++;
                    }
                }

                current = current.GetNextContextPosition(LogicalDirection.Forward);
            }

            return currentIndex == targetIndex ? current : null;
        }

        public static string GetFullRenderedText(FlowDocument doc)
        {
            var sb = new StringBuilder();
            var current = doc.ContentStart;

            while (current != null && current.CompareTo(doc.ContentEnd) < 0)
            {
                if (current.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.Text)
                {
                    string textRun = current.GetTextInRun(LogicalDirection.Forward);
                    foreach (char c in textRun)
                    {
                        if (c != '\r' && c != '\n')
                        {
                            sb.Append(c);
                        }
                    }
                }
                current = current.GetNextContextPosition(LogicalDirection.Forward);
            }
            return sb.ToString();
        }

        public static void ClearAllHighlights(RichTextBox richTextBox)
        {
            if (richTextBox == null)
                return;

            var fullRange = new TextRange(richTextBox.Document.ContentStart, richTextBox.Document.ContentEnd);

            fullRange.ApplyPropertyValue(TextElement.BackgroundProperty, Brushes.Transparent);
        }

        public static void ClearHighlight(RichTextBox richTextBox, TextPointer start, TextPointer end)
        {
            if (richTextBox == null)
                return;

            var range = new TextRange(start, end);

            var bc = new BrushConverter();

            var brush = (Brush?)bc.ConvertFromString(_defaultHighLightColor);

            if (brush != null)
                range.ApplyPropertyValue(TextElement.BackgroundProperty, brush);
        }
    }
}
