using Microsoft.Xaml.Behaviors;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using TextBlock = System.Windows.Controls.TextBlock;

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

        private static readonly DependencyProperty JumpToParagraphCommandProperty =
            DependencyProperty.Register(
                nameof(JumpToParagraphCommand),
                typeof(ICommand),
                typeof(RichTextHyperlinkBehavior));

        public ICommand JumpToParagraphCommand
        {
            get => (ICommand)GetValue(JumpToParagraphCommandProperty);
            set => SetValue(JumpToParagraphCommandProperty, value);
        }

        protected override void OnAttached()
        {
            base.OnAttached();

            if (AssociatedObject.DataContext is Message message)
            {
                if (message.IsStreamingFinished)
                {
                    AssociatedObject.Dispatcher.Invoke(() => RenderFinalContent(message));
                }
                else if (message is INotifyPropertyChanged npc)
                {
                    npc.PropertyChanged += OnMessagePropertyChanged;
                }
            }
        }

        private void OnMessagePropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (AssociatedObject == null)
                return;

            if (e.PropertyName == nameof(Message.IsStreamingFinished))
            {
                if (AssociatedObject != null && AssociatedObject.Dispatcher != null && sender is Message message && message.IsStreamingFinished)
                {
                    ((INotifyPropertyChanged)message).PropertyChanged -= OnMessagePropertyChanged;

                    AssociatedObject.Dispatcher.Invoke(() => RenderFinalContent(message));
                }
            }

            if (e.PropertyName == nameof(Message.IsTxtTrackingFinished))
            {
                if (AssociatedObject != null && AssociatedObject.Dispatcher != null && sender is Message message && message.IsTxtTrackingFinished)
                {
                    ((INotifyPropertyChanged)message).PropertyChanged -= OnMessagePropertyChanged;

                    AssociatedObject.Dispatcher.Invoke(() => RenderCitation(message));
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

        private void RenderCitation(Message message)
        {
            if (JumpToParagraphCommand == null)
                return;

            if (message != null && message.Sentences != null && message.Sentences.Count != 0)
            {
                int no = 1;

                AssociatedObject.Inlines.Add(new LineBreak());
                AssociatedObject.Inlines.Add(new LineBreak());
                AssociatedObject.Inlines.Add("引用：");

                foreach (var sentence in message.Sentences)
                {
                    var link = new Hyperlink
                    {
                        ToolTip = $"转到引用：{no}"
                    };

                    var currentSentence = sentence;

                    link.Click += (s, e) =>
                    {
                        if (JumpToParagraphCommand != null && JumpToParagraphCommand.CanExecute(currentSentence))
                        {
                            JumpToParagraphCommand.Execute(currentSentence);
                        }
                    };

                    link.Inlines.Add(new Run($"[{no}]"));
                    AssociatedObject.Inlines.Add(link);

                    no++;
                }
            }
        }

        private void RenderFinalContent(Message message)
        {
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
