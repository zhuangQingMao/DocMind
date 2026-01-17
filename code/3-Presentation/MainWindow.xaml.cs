using CommunityToolkit.Mvvm.Messaging;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Wpf.Ui.Controls;
using MessageBox = System.Windows.MessageBox;
using RichTextBox = System.Windows.Controls.RichTextBox;

namespace DocMind
{
    public partial class MainWindow : FluentWindow, IRecipient<HighlightMessage>, IRecipient<JumpPageMessage>, IRecipient<JumpParagraphMessage>
    {
        public MainWindow(ChatViewModel viewModel)
        {
            InitializeComponent();

            WeakReferenceMessenger.Default.Register<HighlightMessage>(this);
            WeakReferenceMessenger.Default.Register<JumpPageMessage>(this);
            WeakReferenceMessenger.Default.Register<JumpParagraphMessage>(this);

            viewModel.Messages.CollectionChanged += (s, e) =>
            {
                ChatScrollViewer.ScrollToBottom();
            };

            this.DataContext = viewModel;
        }

        #region Txt HighLight

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

        public void Receive(JumpParagraphMessage message)
        {
            var textTextBox = FindTextBoxInTemplate(previewContentControl);
            if (textTextBox == null)
                return;

            if (!string.IsNullOrWhiteSpace(message.Sentence))
                RichTextBoxHighlighter.HighlightByTextContent(textTextBox, [message.Sentence], true);
        }

        #endregion

        #region word page

        //word跳转界面
        public void Receive(JumpPageMessage message)
        {
            var targetPage = message.PageNumber;

            var documentViewer = FindDocumentViewer(this.previewContentControl);

            if (documentViewer == null)
                return;

            if (documentViewer.Document is IDocumentPaginatorSource documentSource)
            {
                int maxPages = documentSource.DocumentPaginator.PageCount;

                if (targetPage >= 1 && targetPage <= maxPages)
                    documentViewer.GoToPage(targetPage);
                else
                    MessageBox.Show($"页码超出范围，请输入 1 到 {maxPages} 之间的页码。", "校验错误");
            }
            else
                MessageBox.Show("请先加载文档。", "错误");
        }

        #endregion

        private void OnCloseApp(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        // 按Enter发送消息
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

        //递归查找控件
        private static DocumentViewer? FindDocumentViewer(DependencyObject parent)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is DocumentViewer dv)
                {
                    return dv;
                }

                var result = FindDocumentViewer(child);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        //拖动穿窗体
        private void OnDragWindow(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }
    }
}