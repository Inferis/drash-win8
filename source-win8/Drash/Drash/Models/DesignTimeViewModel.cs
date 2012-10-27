using System;
using System.Linq;
using Windows.UI.Xaml.Media.Imaging;

namespace Drash.Models
{
    public class DesignTimeViewModel : ViewModel
    {
        public DesignTimeViewModel()
        {
            var random = new Random();
            var points = Enumerable.Range(0, 20)
                .Select(x => new RainPoint(DateTime.Now.AddMinutes(5 * x), random.Next(50 + x * 7, 255)))
                .ToList();

            Location = "Someplace, Somewhere";
            Chance = "75%";
            IntensityImage = new BitmapImage(new Uri("ms-appx:/Assets/Intensity3.png", UriKind.Absolute));
            Precipitation = "3.314\nmm";
        }

    }
}