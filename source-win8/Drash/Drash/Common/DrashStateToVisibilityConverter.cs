using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace Drash.Common
{
    /// <summary>
    /// Value converter that translates true to false and vice versa.
    /// </summary>
    public sealed class DrashStateToVisibilityConverter : IValueConverter
    {
        public DrashState VisibleForState { get; set; }

        public DrashStateToVisibilityConverter(DrashState state)
        {
            VisibleForState = state;
        }

        public DrashStateToVisibilityConverter()
        {
            VisibleForState = DrashState.Good;
        }

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is DrashState) {
                return (DrashState) value == VisibleForState ? Visibility.Visible : Visibility.Collapsed;
            }

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotSupportedException();
        }
    }
}
