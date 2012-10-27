using Windows.Devices.Geolocation;

namespace Drash.Models
{
    public class Model
    {
        public Model()
        {
            Rain = null;
            Error = DrashError.None;
            RainWasUpdated = false;
        }

        public RainData Rain { get; set; }
        public Geocoordinate Location { get; set; }
        public string LocationName { get; set; }
        public bool GoodLocationName { get; set; }
        public DrashError Error { get; set; }
        public bool RainWasUpdated { get; set; }
        public bool IsAboutOpen { get; set; }
        public bool IntensityValueShown { get; set; }
        public bool ErrorImageShown { get; set; }
        public bool DataRootShown { get; set; }
        public bool LocationShown { get; set; }
        public bool ChanceShown { get; set; }
    }
}