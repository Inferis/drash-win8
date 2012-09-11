using Windows.Devices.Geolocation;

namespace Drash
{
    public class Model
    {
        public Model()
        {
            Rain = null;
            Error = DrashError.None;
        }

        public RainData Rain;
        public Geocoordinate Location;
        public string LocationName;
        public bool GoodLocationName;
        public DrashError Error = DrashError.None;
        public bool RainWasUpdated = false;
        public bool IsAboutOpen;
        public bool IntensityValueShown;
        public bool ErrorImageShown;
        public bool DataRootShown;
        public bool LocationShown;
        public bool ChanceShown;
    }
}