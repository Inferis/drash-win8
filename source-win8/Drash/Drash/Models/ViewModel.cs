using System;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace Drash.Models
{
    public class ViewModel
    {
        public string Location { get; set; }
        public string Chance { get; set; }
        public string Precipitation { get; set; }
        public ImageSource IntensityImage { get; set; }
    }
}