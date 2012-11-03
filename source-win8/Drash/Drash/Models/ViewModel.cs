using System;
using System.Diagnostics;
using System.Globalization;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Windows.Input;
using Drash.Common;
using Windows.Devices.Geolocation;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

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
        private string chance;
        private string precipitation;
        private ImageSource intensityImage;

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

        public string Chance
        {
            get { return chance; }
            set
            {
                chance = value;
                OnPropertyChanged(() => Chance);
            }
        }

        public string Precipitation
        {
            get { return precipitation; }
            set
            {
                precipitation = value;
                OnPropertyChanged(() => Precipitation);
            }
        }

        public ImageSource IntensityImage
        {
            get { return intensityImage; }
            set
            {
                intensityImage = value;
                OnPropertyChanged(() => IntensityImage);
            }
        }

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

        public ViewModel()
        {
            RefreshCommand = new ActionCommand(FetchRain);
        }

        public ViewModel(Model model)
            : this()
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
            geolocator = new Geolocator() { MovementThreshold = 500, DesiredAccuracy = PositionAccuracy.High };
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
                MessageDialog dialog = null;

                try {
                    var uri = string.Format(CultureInfo.InvariantCulture, "http://gps.buienradar.nl/getrr.php?lat={0:0.000000}&lon={1:0.000000}&stamp={2}", Model.Location.Latitude, Model.Location.Longitude, DateTime.UtcNow.Ticks);

                    var wc = new HttpClient();
                    var result = await wc.GetAsync(uri);
                    if (result.IsSuccessStatusCode) {
                        Model.Rain = await RainData.TryParseAsync(await result.Content.ReadAsStringAsync());
                    }
                    Model.RainWasUpdated = true;
                }
                catch (Exception ex) {
                    dialog = new MessageDialog("Could not fetch rain data. An error occured.") {
                        Options = MessageDialogOptions.AcceptUserInputAfterDelay
                    };
                }

                if (dialog != null) {
                    await dialog.ShowAsync();
                }
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
                    VisualizeRain(Model.Rain);
                    Location = Model.LocationName;
                });

        }

        private void VisualizeRain(RainData rainData)
        {
            var animated = Model.RainWasUpdated;
            Model.RainWasUpdated = false;

            string chanceText;
            Color chanceColor;
            string mmImage;
            string mmText;

            var chance = rainData != null ? rainData.ChanceForEntries(Model.Entries) : -1;
            if (chance >= 0) {
                chanceText = string.Format("{0}%", chance);
                chanceColor = Colors.White;
            }
            else {
                chanceText = "?";
                chanceColor = Colors.DarkGray;
            }

            var intensity = 0;
            var mm = 0.0;
            if (rainData != null) {
                mm = rainData.PrecipitationForEntries(Model.Entries);
                intensity = rainData.IntensityForEntries(Model.Entries);
            }

            if (intensity > 0 || mm > 0) {
                mm = Math.Max(mm, 0.001);
                intensity = ((int)Math.Max(1, Math.Min(1 + intensity / 25.0, 4)));

                var format = mm < 0.01 ? "{0:0.000}" : "{0:0.00}";
                mmText = Math.Floor(mm) == mm ? string.Format("{0}", (int)mm) : string.Format(format, mm);
                mmImage = intensity.ToString(CultureInfo.InvariantCulture);
            }
            else {
                mmText = "0";
                mmImage = "0";
            }

            string night;
            if (intensity == 0 && Model.Location != null) {
                var solarinfo = SolarInfo.ForDate(Model.Location.Latitude, Model.Location.Longitude, DateTime.Now);
                var sunrisen = solarinfo.Sunrise < DateTime.UtcNow && DateTime.UtcNow < solarinfo.Sunset;
                night = !sunrisen ? "n" : "d";
            }
            else {
                night = "";
            }
            mmImage = string.Format("ms-appx:/Assets/intensity{0}{1}.png", mmImage, night);

            Chance = chanceText;
            Precipitation = mmText + "\nmm";
            IntensityImage = new BitmapImage(new Uri(mmImage));

            //VisualizeGraph(rainData, animated);
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