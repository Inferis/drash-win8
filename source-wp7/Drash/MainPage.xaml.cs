using System;
using System.Device.Location;
using System.Net;
using Microsoft.Phone.Controls;

namespace Drash
{
    public partial class MainPage : PhoneApplicationPage
    {
        private GeoCoordinateWatcher watcher;
        private bool fetchingRain;
        private RainData rain;

        // Constructor
        public MainPage()
        {
            InitializeComponent();

            watcher = new GeoCoordinateWatcher();
            watcher.MovementThreshold = 500;
            watcher.PositionChanged += (sender, args) => {
                FetchRain();
            };
            watcher.Start();
        }

        private void FetchRain()
        {
            if (fetchingRain) return;

            fetchingRain = true;
            var uri = string.Format("http://gps.buienradar.nl/getrr.php?lat={0}&lon={1}", watcher.Position.Location.Latitude, watcher.Position.Location.Longitude);

            var wc = new WebClient();
            wc.DownloadStringCompleted += (sender, args) => {
                RainData.TryParse(args.Result, out rain);
                fetchingRain = false;
            };
            wc.DownloadStringAsync(new Uri(uri));
        }
    }
}