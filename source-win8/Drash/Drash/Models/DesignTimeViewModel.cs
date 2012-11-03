using System;
using System.Linq;
using Drash.Models.Api;
using Windows.UI.Xaml.Media.Imaging;

namespace Drash.Models
{
    public class DesignTimeViewModel : ViewModel
    {
        public DesignTimeViewModel() : base(null)
        {
            var random = new Random();
            var points = Enumerable.Range(0, 20)
                .Select(x => new RainPoint(DateTime.Now.AddMinutes(5 * x), random.Next(50 + x * 7, 255)))
                .ToList();

            Location = "Someplace, Somewhere";
            Chance = "75%";
            IntensityImage = new BitmapImage(new Uri("ms-appx:/Assets/Intensity3.png", UriKind.Absolute));
            Precipitation = "3.314\nmm";
            EntriesImage = new BitmapImage(new Uri("ms-appx:/Assets/dial30.png", UriKind.Absolute));
            EntriesDescription = "30 min";
        }

    }
}