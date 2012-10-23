using System.Device.Location;

namespace Drash
{
    public class Model
    {
        public Model()
        {
            Rain = null;
            Error = DrashError.None;
            Entries = 6;
        }

        public RainData Rain;
        public GeoCoordinate Location;
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
        public int Entries;
    }
}