using System;
using Windows.UI.Xaml.Data;

namespace Drash
{
    public class StringFormatConverter : IValueConverter
    {
        public StringFormatConverter()
        {
            Format = "";
        }

        public string Format { get; set; }

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return string.Format(Format, value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotSupportedException();
        }
    }
}