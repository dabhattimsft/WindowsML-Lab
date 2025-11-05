using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace WinMLLabDemo
{
    public class MessageBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isUser = (bool)value;
            return new SolidColorBrush(isUser ? Colors.LightBlue : Colors.LightGray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}