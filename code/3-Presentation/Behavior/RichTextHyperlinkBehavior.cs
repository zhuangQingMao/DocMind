using Microsoft.Xaml.Behaviors;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;

namespace DocMind
{
    public class RichTextHyperlinkBehavior : Behavior<TextBlock>
    {
        private static readonly Regex PageReferenceRegex = new Regex(@"\[(\s*\d+(\s*,\s*\d+)*\s*)\]", RegexOptions.Compiled);

        private static readonly DependencyProperty JumpToPageCommandProperty =
            DependencyProperty.Register(
                nameof(JumpToPageCommand),
                typeof(ICommand),
                typeof(RichTextHyperlinkBehavior));

        public ICommand JumpToPageCommand
        {
            get => (ICommand)GetValue(JumpToPageCommandProperty);
            set => SetValue(JumpToPageCommandProperty, value);
        }

        protected override void OnAttached()
        {
            base.OnAttached();

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

        private void OnMessagePropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Message.IsStreamingFinished))
            {
                if (AssociatedObject != null && AssociatedObject.Dispatcher != null && sender is Message message && message.IsStreamingFinished)
                {
                    ((INotifyPropertyChanged)message).PropertyChanged -= OnMessagePropertyChanged;

                    AssociatedObject.Dispatcher.Invoke(() => RenderFinalContent(message));
                }
            }
        }

        protected override void OnDetaching()
        {
            if (AssociatedObject != null && AssociatedObject.DataContext is Message message && message is INotifyPropertyChanged npc)
            {
                npc.PropertyChanged -= OnMessagePropertyChanged;
            }

            base.OnDetaching();
        }

        private void RenderFinalContent(Message message)
        {
            if (AssociatedObject == null)
                return;

            AssociatedObject.Inlines.Clear();

            var response = message.Text;
            int currentPosition = 0;

            var matches = PageReferenceRegex.Matches(response);

            foreach (Match match in matches)
            {
                if (match.Index > currentPosition)
                {
                    var precedingText = response[currentPosition..match.Index];
                    AssociatedObject.Inlines.Add(new Run(precedingText));
                }

                var fullMatchText = match.Value;
                var pageNumbersStr = match.Groups[1].Value;
                var pageNumbers = pageNumbersStr.Split(',');

                foreach (var pageStr in pageNumbers)
                {
                    var combinedHyperlink = new Hyperlink()
                    {
                        Command = JumpToPageCommand,
                        NavigateUri = null,
                        ToolTip = $"跳转到引用页：{pageStr}"
                    };

                    if (int.TryParse(pageStr.Trim(), out int pageNumber))
                    {
                        combinedHyperlink.CommandParameter = pageNumber;
                        var run = new Run($"[{pageNumber}]");
                        combinedHyperlink.Inlines.Add(run);
                    }

                    AssociatedObject.Inlines.Add(combinedHyperlink);
                }

                currentPosition = match.Index + match.Length;
            }

            if (currentPosition < response.Length)
            {
                string remainingText = response[currentPosition..];
                AssociatedObject.Inlines.Add(new Run(remainingText));
            }
        }
    }
}
