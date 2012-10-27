using System.Windows.Input;
using Windows.UI.Xaml.Media;

namespace Drash.Models
{
    public class ViewModel
    {
        public string Location { get; set; }
        public string Chance { get; set; }
        public string Precipitation { get; set; }
        public ImageSource IntensityImage { get; set; }

        public ICommand RefreshCommand { get; set; }
    }
}