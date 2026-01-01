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

            WeakReferenceMessenger.Default.Register<HighlightMessage>(this);
            WeakReferenceMessenger.Default.Register<JumpPageMessage>(this);

            viewModel.Messages.CollectionChanged += (s, e) =>
            {
                ChatScrollViewer.ScrollToBottom();
            };

            this.DataContext = viewModel;
        }

        #region HighLight

        public void Receive(HighlightMessage message)
        {
            var textTextBox = FindTextBoxInTemplate(previewContentControl);
            if (textTextBox == null)
                return;

            RichTextBoxHighlighter.HighlightByTextContent(textTextBox, message.Spans);
        }

        private static RichTextBox? FindTextBoxInTemplate(DependencyObject parent)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is RichTextBox textBox)
                    return textBox;

                var result = FindTextBoxInTemplate(child);
                if (result != null)
                    return result;
            }

            return null;
        }

        #endregion

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


        // 2. 最终执行高亮的方法 (将你之前的方法改造成接收 List<HighlightSpan>)






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