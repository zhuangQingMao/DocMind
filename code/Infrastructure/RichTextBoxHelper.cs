using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace DocMind
{
    public static class RichTextBoxHelper
    {
        // 定义附加属性BindableDocument
        public static readonly DependencyProperty BindableDocumentProperty =
            DependencyProperty.RegisterAttached(
                "BindableDocument",
                typeof(FlowDocument),
                typeof(RichTextBoxHelper),
                new PropertyMetadata(null, OnBindableDocumentChanged));

        // 获取附加属性
        public static FlowDocument GetBindableDocument(DependencyObject obj)
        {
            return (FlowDocument)obj.GetValue(BindableDocumentProperty);
        }

        // 设置附加属性
        public static void SetBindableDocument(DependencyObject obj, FlowDocument value)
        {
            obj.SetValue(BindableDocumentProperty, value);
        }

        // 属性变化时，同步到RichTextBox的Document
        private static void OnBindableDocumentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is RichTextBox richTextBox && e.NewValue != null)
            {
                richTextBox.Dispatcher.Invoke(() =>
                {
                    richTextBox.Document = e.NewValue as FlowDocument;
                });
            }

        }
    }
}
