using System;
using Windows.UI.Xaml.Data;

namespace Drash.Common
{
    /// <summary>
    /// Value converter that translates true to false and vice versa.
    /// </summary>
    public sealed class PercentageConverter : IValueConverter
    {
        public double Percentage { get; set; }

        public PercentageConverter(double percentage)
        {
            Percentage = percentage;
        }

        public PercentageConverter()
        {
            Percentage = 100;
        }

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            try {
                var v = System.Convert.ToDouble(value);
                return v * Percentage / 100.0;
            }
            catch (Exception) {
                return value;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            try {
                var v = System.Convert.ToDouble(value);
                return v / Percentage * 100.0;
            }
            catch (Exception) {
                return value;
            }
        }
    }
}
