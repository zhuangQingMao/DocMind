using CommunityToolkit.Mvvm.Messaging;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace DocMind
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IRecipient<HighlightMessage>, IRecipient<JumpPageMessage>
    {
        public MainWindow(ChatViewModel viewModel)
        {
            InitializeComponent();

            // 1. 注册消息接收（关键步骤！）
            WeakReferenceMessenger.Default.Register<HighlightMessage>(this);
            WeakReferenceMessenger.Default.Register<JumpPageMessage>(this);

            // 监听消息集合变化，自动滚动到底部

            viewModel.Messages.CollectionChanged += (s, e) =>
            {
                ChatScrollViewer.ScrollToBottom();
            };

            this.DataContext = viewModel;
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            base.OnClosing(e);
        }


        // 按Enter发送消息，Shift+Enter换行
        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                var viewModel = DataContext as ChatViewModel;
                if (viewModel?.SendMessageCommand.CanExecute(null) == true)
                {
                    viewModel.SendMessageCommand.Execute(null);
                }
            }
        }

        // 获得焦点时清除占位文本
        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            var textBox = sender as System.Windows.Controls.TextBox;
            if (textBox.Text == "输入消息...")
            {
                textBox.Text = "";
            }
        }

        // 失去焦点时显示占位文本
        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var textBox = sender as System.Windows.Controls.TextBox;
            if (string.IsNullOrWhiteSpace(textBox.Text))
            {
                textBox.Text = "输入消息...";
            }
        }

        // 完善的 FindTextBoxInTemplate 应该能穿透 ContentPresenter
        public RichTextBox FindTextBoxInTemplate(DependencyObject parent)
        {
            // 递归查找逻辑
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                // 目标：找到 TextBox
                if (child is RichTextBox textBox)
                {
                    return textBox;
                }

                // 递归：继续深入查找
                var result = FindTextBoxInTemplate(child);
                if (result != null)
                {
                    return result;
                }
            }
            return null;
        }

        public DocumentViewer FindDocumentViewer(DependencyObject parent)
        {
            // 递归查找逻辑
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                // 目标：找到 TextBox
                if (child is DocumentViewer dv)
                {
                    return dv;
                }

                // 递归：继续深入查找
                var result = FindDocumentViewer(child);
                if (result != null)
                {
                    return result;
                }
            }
            return null;
        }


        // 1. 获取 RichTextBox 的纯文本 (黄金标准)
        public static string GetRichTextBoxContent(RichTextBox richTextBox)
        {
            // 使用 TextRange 获取整个 FlowDocument 的纯文本内容
            TextRange textRange = new TextRange(
                richTextBox.Document.ContentStart,
                richTextBox.Document.ContentEnd
            );
            return textRange.Text;
        }

        // 2. 最终执行高亮的方法 (将你之前的方法改造成接收 List<HighlightSpan>)
        private void ExecuteRichTextHighlight(RichTextBox richTextBox, List<HighlightSpan> spans)
        {
            // 清除旧高亮 (可选)
            // ClearPreviousHighlight(richTextBox); 

            foreach (var span in spans)
            {
                // 调用你之前写好的 TextPointer 高亮逻辑
                //HighlightTextRange(richTextBox, span.StartCharIndex, span.EndCharIndex);

                //RichTextBoxHighlighter.HighlightByTextContent(richTextBox, "早期的 Applicant Tracking Systems (ATS) 仅执行关键词匹配");
                RichTextBoxHighlighter.HighlightByTextContent(richTextBox, span.targetText);
            }
        }

        private List<HighlightSpan> FindHighlightSpans(string extractedSentences, string normalizedDocumentText)
        {
            var spans = new List<HighlightSpan>();

            // 1. 分割句子（去除可能的前后空格）
            var sentences = extractedSentences.Split(new[] { "|||" }, StringSplitOptions.RemoveEmptyEntries)
                                              .Select(s => s.Trim())
                                              .Where(s => !string.IsNullOrEmpty(s));

            // 2. 遍历句子并查找索引
            foreach (var sentence in sentences)
            {
                // 查找句子在整个原始文档中的起始位置
                int startIndex = normalizedDocumentText.IndexOf(sentence, StringComparison.Ordinal);

                if (startIndex != -1) // 找到句子
                {
                    spans.Add(new HighlightSpan
                    {
                        StartCharIndex = startIndex,
                        EndCharIndex = startIndex + sentence.Length,
                        targetText = sentence
                    });
                }
                // else: 如果句子没找到 (可能是 LLM 稍作修改)，则忽略此句子
            }

            return spans;
        }

        // 3. 实现 IRecipient 接口的 Receive 方法
        public void Receive(HighlightMessage message)
        {
            var originalTextBox = FindTextBoxInTemplate(this.previewContentControl);

            // 1. 获取 RichTextBox 自己的纯文本 (黄金标准)
            string preciseText = GetRichTextBoxContent(originalTextBox);

            if (originalTextBox == null) return;

            var highlightSpans = FindHighlightSpans(message.SecondAnswer, preciseText);
            //highlightSpans = new List<HighlightSpan>() { highlightSpans.FirstOrDefault() };

            ExecuteRichTextHighlight(originalTextBox, highlightSpans);

            //// 2. 遍历 Metadata 并执行高亮 (核心 UI 逻辑)
            //foreach (var metadata in message.MetadataToHighlight)
            //{
            //    // 确保你只在高亮当前显示的文件
            //    if (metadata.FileName == message.TargetFileName)
            //    {
            //        HighlightTextRange(originalTextBox, metadata.StartCharIndex, metadata.EndCharIndex);
            //    }
            //}
        }

        private void tiaozhuan(object sender, RoutedEventArgs e)
        {
            // 1. 获取并校验用户输入的页码
            if (!int.TryParse(message.Text, out int targetPage))
            {
                MessageBox.Show("请输入有效的页码数字。", "输入错误");
                return;
            }

            var documentViewer = FindDocumentViewer(this.previewContentControl);

            // 2. 检查 DocumentViewer 是否已加载文档
            // DocumentViewer.Document 返回的是 IDocumentPaginatorSource，通常是 FixedDocumentSequence
            if (documentViewer.Document is IDocumentPaginatorSource documentSource)
            {
                // 获取文档的实际总页数
                int maxPages = documentSource.DocumentPaginator.PageCount;

                // 3. 校验页码范围 (DocumentViewer 页码从 1 开始)
                if (targetPage >= 1 && targetPage <= maxPages)
                {
                    // 4. 执行跳转：这是核心方法
                    // GoToPage() 确保 DocumentViewer 滚动到指定页面的起始位置
                    documentViewer.GoToPage(targetPage);
                }
                else
                {
                    MessageBox.Show($"页码超出范围，请输入 1 到 {maxPages} 之间的页码。", "校验错误");
                }
            }
            else
            {
                MessageBox.Show("请先加载文档。", "错误");
            }
        }

        public void Receive(JumpPageMessage message)
        {
            var targetPage = message.PageNumber;

            var documentViewer = FindDocumentViewer(this.previewContentControl);

            // 2. 检查 DocumentViewer 是否已加载文档
            // DocumentViewer.Document 返回的是 IDocumentPaginatorSource，通常是 FixedDocumentSequence
            if (documentViewer.Document is IDocumentPaginatorSource documentSource)
            {
                // 获取文档的实际总页数
                int maxPages = documentSource.DocumentPaginator.PageCount;

                // 3. 校验页码范围 (DocumentViewer 页码从 1 开始)
                if (targetPage >= 1 && targetPage <= maxPages)
                {
                    // 4. 执行跳转：这是核心方法
                    // GoToPage() 确保 DocumentViewer 滚动到指定页面的起始位置
                    documentViewer.GoToPage(targetPage);
                }
                else
                {
                    MessageBox.Show($"页码超出范围，请输入 1 到 {maxPages} 之间的页码。", "校验错误");
                }
            }
            else
            {
                MessageBox.Show("请先加载文档。", "错误");
            }
        }
    }
}