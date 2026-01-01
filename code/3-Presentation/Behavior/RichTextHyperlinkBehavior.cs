using Microsoft.Xaml.Behaviors;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Threading;

namespace DocMind
{
    public class RichTextHyperlinkBehavior : Behavior<TextBlock>
    {
        private Dispatcher _cachedDispatcher;
        private TextBlock _cachedTextBlock;

        public static readonly DependencyProperty JumpToPageCommandProperty =
            DependencyProperty.Register(nameof(JumpToPageCommand), typeof(ICommand), typeof(RichTextHyperlinkBehavior));

        public ICommand JumpToPageCommand
        {
            get => (ICommand)GetValue(JumpToPageCommandProperty);
            set => SetValue(JumpToPageCommandProperty, value);
        }

        protected override void OnAttached()
        {
            base.OnAttached();

            _cachedTextBlock = AssociatedObject;
            _cachedDispatcher = AssociatedObject.Dispatcher;

            if (AssociatedObject.DataContext is Message message)
            {
                if (message.IsStreamingFinished)
                {
                    RenderFinalContent(message);
                }
                else if (message is INotifyPropertyChanged npc)
                {
                    npc.PropertyChanged += OnMessagePropertyChanged;
                }
            }
        }

        protected override void OnDetaching()
        {
            if (_cachedTextBlock != null && _cachedTextBlock.DataContext is Message message && message is INotifyPropertyChanged npc)
            {
                npc.PropertyChanged -= OnMessagePropertyChanged;
            }

            _cachedTextBlock = null;
            _cachedDispatcher = null;

            base.OnDetaching();
        }

        private void OnMessagePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Message.IsStreamingFinished))
            {
                if (_cachedDispatcher != null && sender is Message message && message.IsStreamingFinished)
                {
                    _cachedDispatcher.Invoke(() =>
                    {
                        if (_cachedTextBlock == null) return;

                        ((INotifyPropertyChanged)message).PropertyChanged -= OnMessagePropertyChanged;

                        RenderFinalContent(message);
                    });
                }
            }
        }

        private string ExtractReferencePages(string fullText)
        {
            const string startTag = "【引用页码汇总】";

            if (string.IsNullOrEmpty(fullText))
            {
                return string.Empty;
            }

            int startIndex = fullText.IndexOf(startTag, StringComparison.Ordinal);

            if (startIndex == -1)
            {
                return string.Empty;
            }

            return fullText.Substring(startIndex);
        }

        private void RenderFinalContent(Message message)
        {
            if (_cachedTextBlock == null) return;

            string response = message.Text;

            var pageStr = ExtractReferencePages(response);

            message.Text = message.Text.Replace(pageStr, "【引用页码汇总】");

            pageStr = pageStr.Replace("【引用页码汇总】", "");
            var pageArr = pageStr.Split(",");

            foreach (var item in pageArr)
            {
                if (int.TryParse(item, out int pageNumber))
                {
                    var str = $"[{item}]";

                    Hyperlink hyperlink = new Hyperlink(new Run(str))
                    {
                        Command = JumpToPageCommand,
                        CommandParameter = pageNumber,
                        NavigateUri = null,
                        ToolTip = $"跳转到第 {pageNumber} 页" 
                    };

                    _cachedTextBlock.Inlines.Add(hyperlink);
                }
            }
        }
    }
}
