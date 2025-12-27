using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace DocMind
{
    public static class RichTextBoxHighlighter
    {
        /// <summary>
        /// 根据「要高亮的目标文本内容」直接高亮（自动匹配位置，无视格式）
        /// </summary>
        /// <param name="richTextBox">目标RichTextBox</param>
        /// <param name="targetText">要高亮的文本内容</param>
        /// <param name="highlightColor">高亮颜色</param>
        public static void HighlightByTextContent(RichTextBox richTextBox, string targetText, Brush highlightColor = null)
        {
            if (richTextBox == null || string.IsNullOrEmpty(targetText))
                return;

            highlightColor ??= Brushes.Yellow;
            //ClearAllHighlights(richTextBox); // 可选：清除旧高亮

            // 获取FlowDocument的完整渲染文本（带格式结构的纯文本）
            string fullText = GetFullRenderedText(richTextBox.Document);
            int startIndex = fullText.IndexOf(targetText, StringComparison.Ordinal);
            if (startIndex == -1) return;

            // 定位起始和结束位置
            TextPointer startPos = FindPositionByCharacterIndex(richTextBox.Document, startIndex);
            TextPointer endPos = FindPositionByCharacterIndex(richTextBox.Document, startIndex + targetText.Length);

            if (startPos != null && endPos != null)
            {
                new TextRange(startPos, endPos).ApplyPropertyValue(TextElement.BackgroundProperty, highlightColor);

                // 1. 获取RichTextBox的内置ScrollViewer
                ScrollViewer scrollViewer = FindVisualChild<ScrollViewer>(richTextBox);
                if (scrollViewer == null) return;

                // 2. 获取目标位置的文档坐标（关键：使用RelativeToViewport）
                Rect rect = startPos.GetCharacterRect(LogicalDirection.Forward);

                // 3. 直接滚动到目标位置的顶部（不做居中，避免越界）
                double targetOffset = rect.Top;
                // 确保偏移不超过滚动范围
                targetOffset = Math.Max(0, Math.Min(targetOffset, scrollViewer.ScrollableHeight));

                // 4. 强制滚动（立即生效）
                scrollViewer.ScrollToVerticalOffset(targetOffset);

                //// 备选：强制刷新布局（解决滚动不生效问题）
                //scrollViewer.UpdateLayout();
                //richTextBox.UpdateLayout();
            }
        }

        private static T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typedChild)
                    return typedChild;

                T childOfChild = FindVisualChild<T>(child);
                if (childOfChild != null)
                    return childOfChild;
            }
            return null;
        }

        /// <summary>
        /// 根据「字符索引」精准定位TextPointer（适配所有格式）
        /// </summary>
        private static TextPointer FindPositionByCharacterIndex(FlowDocument doc, int targetIndex)
        {
            if (targetIndex < 0) return null;

            TextPointer current = doc.ContentStart;
            int currentIndex = 0;

            while (current != null)
            {
                if (current.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.Text)
                {
                    string textRun = current.GetTextInRun(LogicalDirection.Forward);
                    if (textRun == null) break;

                    // 遍历文本Run的每个字符，仅统计“可见字符”（跳过换行符）
                    for (int i = 0; i < textRun.Length; i++)
                    {
                        char c = textRun[i];
                        // 跳过换行/回车符（这些是格式符号，不计入可见字符索引）
                        if (c == '\r' || c == '\n') continue;

                        // 匹配目标索引，返回精确位置
                        if (currentIndex == targetIndex)
                        {
                            return current.GetPositionAtOffset(i);
                        }
                        currentIndex++;
                    }
                }

                // 移动到下一个内容位置
                current = current.GetNextContextPosition(LogicalDirection.Forward);
            }

            // 处理目标索引等于最后一个可见字符的情况
            return currentIndex == targetIndex ? current : null;
        }

        /// <summary>
        /// 获取FlowDocument中「用户可见的完整纯文本」（包含换行，匹配显示顺序）
        /// </summary>
        public static string GetFullRenderedText(FlowDocument doc)
        {
            StringBuilder sb = new StringBuilder();
            TextPointer current = doc.ContentStart;

            while (current != null && current.CompareTo(doc.ContentEnd) < 0)
            {
                if (current.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.Text)
                {
                    string textRun = current.GetTextInRun(LogicalDirection.Forward);
                    foreach (char c in textRun)
                    {
                        // 跳过换行/回车符
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

        /// <summary>
        /// 遍历所有Block元素，拼接可见文本
        /// </summary>
        private static void TraverseBlocks(BlockCollection blocks, StringBuilder sb)
        {
            foreach (Block block in blocks)
            {
                if (block is Paragraph para)
                {
                    // 拼接段落内文本
                    foreach (Inline inline in para.Inlines)
                    {
                        if (inline is Run run) sb.Append(run.Text);
                        else if (inline is Span span) TraverseInlines(span.Inlines, sb);
                    }
                    sb.AppendLine(); // 段落结束换行
                }
                else if (block is System.Windows.Documents.List list)
                {
                    // 遍历列表项
                    foreach (ListItem item in list.ListItems)
                    {
                        TraverseBlocks(item.Blocks, sb); // 列表项内的Blocks（通常是Paragraph）
                    }
                }
            }
        }

        /// <summary>
        /// 遍历Inline元素（处理Span嵌套）
        /// </summary>
        private static void TraverseInlines(InlineCollection inlines, StringBuilder sb)
        {
            foreach (Inline inline in inlines)
            {
                if (inline is Run run) sb.Append(run.Text);
                else if (inline is Span span) TraverseInlines(span.Inlines, sb);
            }
        }

        /// <summary>
        /// 清除所有高亮
        /// </summary>
        public static void ClearAllHighlights(RichTextBox richTextBox)
        {
            if (richTextBox == null) return;
            TextRange fullRange = new TextRange(richTextBox.Document.ContentStart, richTextBox.Document.ContentEnd);
            fullRange.ApplyPropertyValue(TextElement.BackgroundProperty, Brushes.Transparent);
        }
    }
}
