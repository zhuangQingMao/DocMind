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
        // 缓存 Dispatcher 和 TextBlock，避免在后台线程访问 Behavior 属性
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

            // 【关键修复 step 1】在 UI 线程提前缓存对象
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
                    // 使用命名方法订阅
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

            // 清理引用
            _cachedTextBlock = null;
            _cachedDispatcher = null;

            base.OnDetaching();
        }

        private void OnMessagePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // 仅处理流式结束信号
            if (e.PropertyName == nameof(Message.IsStreamingFinished))
            {
                // 【关键修复 step 2】绝对不要在这里使用 "AssociatedObject" 或 "this"
                // 直接使用提前缓存的 _cachedDispatcher
                if (_cachedDispatcher != null && sender is Message message && message.IsStreamingFinished)
                {
                    _cachedDispatcher.Invoke(() =>
                    {
                        // 此时已回到 UI 线程，可以安全操作 _cachedTextBlock
                        if (_cachedTextBlock == null) return;

                        // 移除订阅
                        ((INotifyPropertyChanged)message).PropertyChanged -= OnMessagePropertyChanged;

                        // 渲染
                        RenderFinalContent(message);
                    });
                }
            }
        }

        /// <summary>
        /// 从给定字符串中，提取从“【引用页码汇总】”标记开始到字符串末尾的所有内容。
        /// </summary>
        /// <param name="fullText">包含页码引用的完整字符串。</param>
        /// <returns>如果找到标记，则返回从标记开始到末尾的子字符串；否则返回空字符串。</returns>
        private string ExtractReferencePages(string fullText)
        {
            // 定义要查找的起始标记
            const string startTag = "【引用页码汇总】";

            if (string.IsNullOrEmpty(fullText))
            {
                return string.Empty;
            }

            // 1. 查找起始标记在字符串中的位置
            // StringComparison.Ordinal ensures a fast, case-sensitive search.
            int startIndex = fullText.IndexOf(startTag, StringComparison.Ordinal);

            // 2. 检查是否找到了标记
            if (startIndex == -1)
            {
                // 未找到标记，返回空字符串
                return string.Empty;
            }

            // 3. 计算要提取的子字符串的起始位置：标记本身的起始位置
            // 例如：如果 fullText 是 "文本【引用页码汇总】2,3,4"，startIndex 是 2
            // 如果返回的字符串需要包含标记本身，直接使用 startIndex

            // 如果返回的字符串需要包含标记本身：
            return fullText.Substring(startIndex);

            /*
            // 如果您只需要标记后面（不含标记本身）的内容，使用以下代码：
            // int contentStartIndex = startIndex + startTag.Length;
            // return fullText.Substring(contentStartIndex);
            */
        }

        private void RenderFinalContent(Message message)
        {
            // 双重检查
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
                        ToolTip = $"跳转到第 {pageNumber} 页" // 可选：添加提示
                    };

                    _cachedTextBlock.Inlines.Add(hyperlink);
                }
            }

        }

    }
}
