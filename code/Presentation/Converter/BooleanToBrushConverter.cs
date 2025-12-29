using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace DocMind
{
    public class BooleanToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isUser = (bool)value;
            var colors = parameter.ToString()?.Split(',') ?? ["yellow", "red"];
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString(isUser ? colors[0] : colors[1]));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}
