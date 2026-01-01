using Microsoft.Xaml.Behaviors;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace DocMind
{
    public class FlowDocumentBehavior : Behavior<RichTextBox>
    {
        public static readonly DependencyProperty BoundDocumentProperty =
            DependencyProperty.Register(
                nameof(BoundDocument),
                typeof(FlowDocument),
                typeof(FlowDocumentBehavior),
                new PropertyMetadata(null, OnBoundDocumentChanged));

        public FlowDocument BoundDocument
        {
            get => (FlowDocument)GetValue(BoundDocumentProperty);
            set => SetValue(BoundDocumentProperty, value);
        }

        private static void OnBoundDocumentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FlowDocumentBehavior behavior)
            {
                if (behavior.AssociatedObject != null && e.NewValue is FlowDocument newDocument)
                {
                    behavior.AssociatedObject.Document = newDocument;
                }
            }
        }

        protected override void OnAttached()
        {
            base.OnAttached();

            if (BoundDocument != null)
            {
                AssociatedObject.Document = BoundDocument;
            }
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.Document = new FlowDocument();
        }
    }
}
