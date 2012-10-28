using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Windows.Input;
using Drash.Common;
using Windows.Devices.Geolocation;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace Drash.Models
{
    public class ViewModel : NotifyPropertyChangedBase
    {
        private LayoutAwarePage view;
        private Geolocator geolocator;
        private DelayedAction delayedLocationUpdate, nextRainUpdate;
        private bool firstFetch = true;
        private bool fetchingRain = false;
        private bool isBusy;
        private string location;

        public LayoutAwarePage View
        {
            private get { return view; }
            set
            {
                view = value;
                if (view != null) ViewLoaded();
            }
        }

        #region Binding Properties

        public string Location
        {
            get { return location; }
            set
            {
                location = value;
                OnPropertyChanged(() => Location);
            }
        }

        public string Chance { get; set; }
        public string Precipitation { get; set; }
        public ImageSource IntensityImage { get; set; }
        public ICommand RefreshCommand { get; set; }

        public bool IsBusy
        {
            get { return isBusy; }
            set
            {
                isBusy = value;
                OnPropertyChanged(() => IsBusy);
            }
        }

        #endregion

        private Model Model { get; set; }

        public ViewModel(Model model)
        {
            Model = model;
        }

        public void ViewLoaded()
        {
            delayedLocationUpdate = new DelayedAction(View.Dispatcher);
            nextRainUpdate = new DelayedAction(View.Dispatcher, 3 * 60 * 1000);

            NetworkChange.NetworkAddressChanged += (sender, args) => UpdateState();

            UpdateState();
            InitializeGeolocator();
        }

        private void InitializeGeolocator()
        {
            geolocator = new Geolocator() { MovementThreshold = 500, DesiredAccuracy = PositionAccuracy.Default };
            geolocator.PositionChanged += (s, a) => {
                Debug.WriteLine("Position changed");
                if (Model.Location != null && Model.Location.GetDistanceTo(a.Position.Coordinate) < 20)
                    return;
                UpdateLocation(a.Position);
            };
            geolocator.StatusChanged += async (o, args) => {
                Debug.WriteLine("status changed " + args.Status);
                if (args.Status == PositionStatus.Ready) {
                    try {
                        Debug.WriteLine("getting pos");
                        var pos = await geolocator.GetGeopositionAsync(TimeSpan.FromMinutes(30), TimeSpan.FromSeconds(5));
                        Debug.WriteLine("got pos after status " + args.Status);
                        UpdateLocation(pos);
                    }
                    catch (Exception) {
                        Debug.WriteLine("status Failed");
                    }
                }
                else {
                    UpdateLocation(null);
                }
            };
        }

        private bool UpdateLocation(Geoposition newLocation)
        {
            if (newLocation == null || newLocation.Coordinate == null) {
                Debug.WriteLine("Update location null");
                Model.LocationName = "";
                Model.GoodLocationName = false;
                UpdateState();
                return false;
            }

            Debug.WriteLine("Update location");
            Debug.WriteLine(Model.Location != null ? Model.Location.GetDistanceTo(newLocation.Coordinate).ToString() : "000");
            var delay = firstFetch || Model.Location == null ||
                        Model.Location.GetDistanceTo(newLocation.Coordinate) > 500
                            ? 0
                            : 2000;
            Debug.WriteLine("Delay = ", delay);
            delayedLocationUpdate.Run(async () => {
                Debug.WriteLine("Actually updating");
                Model.Location = newLocation.Coordinate;
                if (newLocation.CivicAddress == null || string.IsNullOrEmpty(newLocation.CivicAddress.City) || string.IsNullOrEmpty(newLocation.CivicAddress.Country)) {
                    var glocator = new GoogleAddressResolver();
                    var addr = await glocator.ResolveAddressAsync(newLocation.Coordinate);
                    Model.GoodLocationName = addr == null || addr.IsUnknown || string.IsNullOrEmpty(addr.City) || string.IsNullOrEmpty(addr.CountryRegion);
                    Model.LocationName = Model.GoodLocationName
                        ? string.Format("{0:0.000000}, {1:0.000000}", newLocation.Coordinate.Latitude, newLocation.Coordinate.Longitude)
                        : string.Format("{0}, {1}", addr.City, addr.CountryRegion);
                }
                else {
                    Model.GoodLocationName = newLocation.CivicAddress == null || string.IsNullOrEmpty(newLocation.CivicAddress.City) || string.IsNullOrEmpty(newLocation.CivicAddress.Country);
                    Model.LocationName = Model.GoodLocationName
                        ? string.Format("{0:0.000000}, {1:0.000000}", newLocation.Coordinate.Latitude, newLocation.Coordinate.Longitude)
                        : string.Format("{0}, {1}", newLocation.CivicAddress.City, newLocation.CivicAddress.Country);
                }

                UpdateState();
                FetchRain();
            }, delay);
            return true;
        }

        private async void FetchRain()
        {
            if (fetchingRain) return;

            Debug.WriteLine("fetching rain");
            nextRainUpdate.Cancel();
            if (Model.Location == null) {
                // no location, schedule new fetch
                if (!UpdateLocation(await geolocator.GetGeopositionAsync()))
                    nextRainUpdate.Run(FetchRain);
                return;
            }

            if (!NetworkInterface.GetIsNetworkAvailable()) {
                // no network, schedule new fetch
                UpdateState();
                nextRainUpdate.Run(FetchRain);
                return;
            }

            fetchingRain = true;
            UpdateBusy();
            firstFetch = false;
            try {
                var uri = string.Format("http://gps.buienradar.nl/getrr.php?lat={0:0.000000}&lon={1:0.000000}&stamp={2}", Model.Location.Latitude, Model.Location.Longitude, DateTime.UtcNow.Ticks);

                var wc = new HttpClient();
                var result = await wc.GetAsync(uri);
                if (result.IsSuccessStatusCode) {
                    Model.Rain = await RainData.TryParseAsync(await result.Content.ReadAsStringAsync());
                }
                Model.RainWasUpdated = true;
            }
            finally {
                fetchingRain = false;
                UpdateBusy();
                UpdateState();
                nextRainUpdate.Run(FetchRain);
            }
        }

        private void UpdateBusy()
        {
            Debug.WriteLine("Update busy " + fetchingRain);
            IsBusy = fetchingRain;
        }

        private void UpdateState()
        {
            try {
                if (!NetworkInterface.GetIsNetworkAvailable()) {
                    GoToState(DrashState.NoNetwork);
                    return;
                }

                if (Model.Location == null) {
                    GoToState(DrashState.NoLocation);
                    return;
                }

                GoToState(DrashState.Good);
            }
            finally {
                UpdateVisuals();
            }
        }

        private void UpdateVisuals()
        {
            View.Dispatcher.RunAsync(
                CoreDispatcherPriority.Normal,
                () => {
                    Location = Model.LocationName;
                });

        }

        private void GoToState(DrashState state)
        {
            Model.State = state;

            Debug.WriteLine("state " + state.ToString());
            View.Dispatcher.RunAsync(
                CoreDispatcherPriority.Normal,
                () => VisualStateManager.GoToState(View, state.ToString(), true));
        }
    }
}