using System.Globalization;
using System.Windows.Data;

namespace DocMind
{
    public class StringIsNotNullOrEmptyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string? str = value as string;

            bool isNotNullOrEmpty = !string.IsNullOrEmpty(str);

            if (parameter != null && parameter.ToString().Equals("Invert", StringComparison.OrdinalIgnoreCase))
            {
                return !isNotNullOrEmpty;
            }

            return isNotNullOrEmpty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
