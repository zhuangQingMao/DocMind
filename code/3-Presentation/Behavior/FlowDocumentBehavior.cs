using Microsoft.Xaml.Behaviors;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace DocMind
{
    // 继承自 Behavior<RichTextBox>，指定该行为只能附加到 RichTextBox 控件
    public class FlowDocumentBehavior : Behavior<RichTextBox>
    {
        // 1. 定义依赖属性，用于接收 ViewModel 传入的 FlowDocument
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

        // 2. 依赖属性值变化时的回调函数
        private static void OnBoundDocumentChanged(
            DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // 确保 d 是 FlowDocumentBehavior 实例
            if (d is FlowDocumentBehavior behavior)
            {
                // AssociatedObject 是 Behavior 附加到的控件实例 (RichTextBox)
                if (behavior.AssociatedObject != null && e.NewValue is FlowDocument newDocument)
                {
                    // 在 UI 线程上设置 RichTextBox 的 Document 属性
                    // AssociatedObject 就是 RichTextBox
                    behavior.AssociatedObject.Document = newDocument;
                }
            }
        }

        // 3. 附加/分离逻辑（可选，但推荐）
        // 当 Behavior 被附加到 RichTextBox 时执行
        protected override void OnAttached()
        {
            base.OnAttached();
            // 初始加载时同步一次 Document
            if (BoundDocument != null)
            {
                AssociatedObject.Document = BoundDocument;
            }
        }

        // 当 Behavior 从 RichTextBox 分离时执行
        protected override void OnDetaching()
        {
            base.OnDetaching();
            // 清理，防止内存泄漏（可选）
            AssociatedObject.Document = new FlowDocument();
        }
    }
}
