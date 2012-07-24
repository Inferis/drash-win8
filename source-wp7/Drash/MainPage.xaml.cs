using System;
using System.Device.Location;
using System.Linq;
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

            this.Loaded += new System.Windows.RoutedEventHandler(MainPage_Loaded);
            SetLocation = text => {
                LocationFadeout.Begin();
                LocationFadeout.Completed += (sender, args) => {
                    Location.Text = text;
                    LocationFadein.Begin();
                };
            };

        }

        public Action<string> SetLocation { get; private set; }



        void MainPage_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            watcher = new GeoCoordinateWatcher();
            watcher.MovementThreshold = 500;
            watcher.PositionChanged += (s, a) => {
                ResolveCurrentLocation();
                FetchRain();
            };
            watcher.Start();

            if (watcher.Position != null) {
                FetchRain();
            }
        }

        private void ResolveCurrentLocation()
        {
            var resolver = new CivicAddressResolver();
            resolver.ResolveAddressCompleted += (o, args) => {
                if (args.Address == null || args.Address.IsUnknown) {
                    ResolveCurrentLocationThroughGoogle();
                }
                else {
                    SetLocation(string.Format("{0}, {1}", args.Address.City, args.Address.CountryRegion));
                }
            };
            resolver.ResolveAddressAsync(watcher.Position.Location);
        }

        private void ResolveCurrentLocationThroughGoogle()
        {
            var resolver = new GoogleAddressResolver();
            resolver.ResolveAddressCompleted += (o, args) => {
                if (args.Address == null || args.Address.IsUnknown) {
                    SetLocation(string.Format("{0:0.00000},{1:0.00000}", watcher.Position.Location.Latitude, watcher.Position.Location.Longitude));
                }
                else {
                    SetLocation(string.Format("{0}, {1}", args.Address.City, args.Address.CountryRegion));
                }
            };
            resolver.ResolveAddressAsync(watcher.Position.Location);
        }

        private void FetchRain()
        {
            if (fetchingRain) return;

            spinner.IsVisible = true;
            fetchingRain = true;
            var uri = string.Format("http://gps.buienradar.nl/getrr.php?lat={0}&lon={1}", watcher.Position.Location.Latitude, watcher.Position.Location.Longitude);

            var wc = new WebClient();
            wc.DownloadStringCompleted += (sender, args) => {
                if (args.Error == null) return;
                RainData.TryParse(args.Result, out rain);
                fetchingRain = false;
                spinner.IsVisible = false;
                UpdateState();
            };
            wc.DownloadStringAsync(new Uri(uri));
        }

        private void UpdateState()
        {
        }
    }
}