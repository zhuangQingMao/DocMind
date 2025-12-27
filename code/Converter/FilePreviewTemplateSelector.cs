using System.Windows;
using System.Windows.Controls;

namespace DocMind
{
    public class FilePreviewTemplateSelector : DataTemplateSelector
    {
        public DataTemplate TxtTemplate { get; set; }
        public DataTemplate WordTemplate { get; set; }
        public DataTemplate ExcelTemplate { get; set; }
        public DataTemplate DefaultTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is DocumentFile file)
            {
                var extension = System.IO.Path.GetExtension(file.FileName)?.ToLower();
                return extension switch
                {
                    ".txt" => TxtTemplate,
                    ".doc" or ".docx" => WordTemplate,
                    ".xls" or ".xlsx" => ExcelTemplate,
                    _ => DefaultTemplate,
                };
            }
            return DefaultTemplate;
        }
    }
}
