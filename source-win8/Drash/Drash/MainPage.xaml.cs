using System;
using System.Globalization;
using System.Net.Http;
using System.Net.NetworkInformation;
using Drash.Models;
using Windows.Devices.Geolocation;
using Windows.UI;
using Windows.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Drash
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private Geolocator watcher;
        private readonly DelayedAction updateLocation;
        private readonly DelayedAction updateLocationName;
        private readonly DelayedAction updateRain;
        private bool fetchingRain;
        private bool firstFetch;
        private readonly Color graphStrokeColor;
        private readonly Color graphFillFrom;
        private bool loaded;

        public MainPage()
        {
            InitializeComponent();

            updateLocationName = new DelayedAction(Dispatcher);
            updateLocation = new DelayedAction(Dispatcher);
            updateRain = new DelayedAction(Dispatcher, 3 * 60 * 1000);

            UpdateStateFromModel();
            NetworkChange.NetworkAddressChanged += (sender, args) => UpdateState();

            //graphStrokeColor = ((SolidColorBrush)Graph.Stroke).Color;
            //graphFillFrom = ((LinearGradientBrush)Graph.Fill).GradientStops[0].Color;

            Loaded += MainPageLoaded;
        }

        public Model Model
        {
            get { return null; } // return ((App)Application.Current).Model; }
        }

        public void UpdateStateFromModel()
        {
            //Location.Opacity = Model.LocationShown ? 1 : 0;
            //DataRoot.Opacity = Model.DataRootShown ? 1 : 0;
            //ErrorImage.Opacity = Model.ErrorImageShown ? 1 : 0;
            //Chance.Opacity = Model.ChanceShown ? 1 : 0;
            //IntensityImage.Opacity = Model.IntensityValueShown ? 0 : 1;
            //IntensityValue.Opacity = Model.IntensityValueShown ? 1 : 0;
            //updateLocation.Cancel();
            //updateLocationName.Cancel();

            //UpdateState();
            //updateRain.Run(FetchRain);
        }

        private void MainPageLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            var asked = DrashSettings.LocationAllowedAsked;
            var allowed = DrashSettings.LocationAllowed;

            SplashFadeout.Begin(async () => {
                loaded = true;

                var recog = new GestureRecognizer();
                recog.Holding += (o, args) => { if (args.HoldingState == HoldingState.Started) FetchRain(); };
                if (!asked) {
                    var result = await MessageBox.ShowAsync("Would you like to display the rain forecast based on your current location? This will send your location to buienradar.nl to retrieve the forecast. You can disable location services in the About screen.", "Enable location services?", MessageBoxButton.OKCancel);
                    allowed = result == MessageBoxResult.OK;
                    DrashSettings.LocationAllowed = allowed;
                }

                if (allowed) {
                    watcher = new Geolocator() { MovementThreshold = 500 };
                    watcher.PositionChanged += (s, a) => {
                        if (Model.Location != null && Model.Location.GetDistanceTo(a.Position.Coordinate) < 20)
                            return;
                        UpdateLocation(a.Position);
                    };
                    watcher.StatusChanged += async (o, args) => {
                                                           try {
                                                               var pos = await watcher.GetGeopositionAsync();
                                                               UpdateLocation(pos);
                                                           }
                                                           catch (Exception) {}
                                                       };

                    firstFetch = await watcher.GetGeopositionAsync() != null;
                }
            });

            //TransitionService.SetNavigationInTransition(this, new NavigationInTransition()
            //{
            //    Backward = new TurnstileTransition() { Mode = TurnstileTransitionMode.BackwardIn },
            //    Forward = new TurnstileTransition() { Mode = TurnstileTransitionMode.ForwardIn }
            //});

        }

        private bool UpdateLocation(Geoposition newLocation)
        {
            if (newLocation == null) {
                Model.LocationName = "";
                Model.GoodLocationName = false;
                UpdateState();
                return false;
            }

            var delay = firstFetch || Model.Location == null ||
                        Model.Location.GetDistanceTo(newLocation.Coordinate) > 500
                            ? 0
                            : 2000;

            Action<Geoposition> setName = loc => {
            };
            updateLocation.Run(async () => {
                Model.Location = newLocation.Coordinate;
                if (newLocation.CivicAddress == null || string.IsNullOrEmpty(newLocation.CivicAddress.City) || string.IsNullOrEmpty(newLocation.CivicAddress.Country))
                {
                    var glocator = new GoogleAddressResolver();
                    var addr = await glocator.ResolveAddressAsync(newLocation.Coordinate);
                    Model.LocationName = addr == null || addr.IsUnknown || string.IsNullOrEmpty(addr.City) || string.IsNullOrEmpty(addr.CountryRegion)
                        ? string.Format("{0:0.000000}, {1:0.000000}", newLocation.Coordinate.Latitude, newLocation.Coordinate.Longitude)
                        : string.Format("{0}, {1}", addr.City, addr.CountryRegion);
                }
                else
                    Model.LocationName = newLocation.CivicAddress == null || string.IsNullOrEmpty(newLocation.CivicAddress.City) || string.IsNullOrEmpty(newLocation.CivicAddress.Country)
                        ? string.Format("{0:0.000000}, {1:0.000000}", newLocation.Coordinate.Latitude, newLocation.Coordinate.Longitude)
                        : string.Format("{0}, {1}", newLocation.CivicAddress.City, newLocation.CivicAddress.Country);

                UpdateState();
                FetchRain();
            }, delay);
            return true;
        }

        private async void FetchRain()
        {
            if (fetchingRain) return;

            updateRain.Cancel();
            if (Model.Location == null) {
                // no location, schedule new fetch
                if (!UpdateLocation(await watcher.GetGeopositionAsync()))
                    updateRain.Run(FetchRain);
                return;
            }

            if (!NetworkInterface.GetIsNetworkAvailable()) {
                // no network, schedule new fetch
                UpdateState();
                updateRain.Run(FetchRain);
                return;
            }

            UpdateSpinner();
            fetchingRain = true;
            firstFetch = false;
            var uri = string.Format("http://gps.buienradar.nl/getrr.php?lat={0:0.000000}&lon={1:0.000000}&stamp={2}", Model.Location.Latitude, Model.Location.Longitude, DateTime.UtcNow.Ticks);

            var wc = new HttpClient();
            var result = await wc.GetAsync(uri);
            if (result.IsSuccessStatusCode) {
                Model.Rain = await RainData.TryParseAsync(await result.Content.ReadAsStringAsync());
                fetchingRain = false;
            }
            UpdateSpinner();
            Model.RainWasUpdated = true;
            UpdateState();
            updateRain.Run(FetchRain);
        }

        private void UpdateSpinner()
        {
            spinner.Visibility = fetchingRain ? Visibility.Visible : Visibility.Collapsed;
        }

        private void UpdateState()
        {
            try {
                if (!NetworkInterface.GetIsNetworkAvailable()) {
                    Model.State = DrashState.NoNetwork;
                    return;
                }

                if (Model.Location == null) {
                    Model.State = DrashState.NoLocation;
                    return;
                }

                Model.State = DrashState.Good;
            }
            finally {
                UpdateVisuals();
            }
        }

        private void UpdateVisuals()
        {
            if (Model.State != DrashState.Good) {
                VisualizeError(Model.State);
                return;
            }

            if (Model.ErrorImageShown) {
                ErrorImage.FadeOut(UpdateVisuals);
                Model.ErrorImageShown = false;
                return;
            }

            if (string.IsNullOrEmpty(Model.LocationName) && Model.Rain == null) {
                if (Model.DataRootShown) {
                    DataRoot.FadeOut();
                    Model.DataRootShown = false;
                }
                return;
            }

            VisualizeRain(Model.Rain);
            VisualizeLocation(Model.LocationName);
        }

        private void VisualizeError(DrashState drashState)
        {
        }

        private void VisualizeLocation(string name)
        {
            ShowData(animated => {
                if (!Model.DataRootShown) {
                    Location.Opacity = 1;
                    Model.LocationShown = true;
                    Location.Text = name;
                    return;
                }

                if (!Model.LocationShown) {
                    Location.Text = name;
                    Location.FadeIn();
                    Model.LocationShown = true;
                    return;
                }

                if (Location.Text == name)
                    return;

                Location.FadeOutThenIn(between: () => {
                    Location.Text = name;
                });

            });
        }

        private void VisualizeRain(RainData rainData)
        {
            var animate = Model.RainWasUpdated;
            Model.RainWasUpdated = false;
            ShowData(animated => {
                string chanceText;
                Color chanceColor;
                string mmImage;
                string mmText;

                animated = animated && animate;
                if (rainData != null && rainData.Chance >= 0) {
                    chanceText = string.Format("{0}%", rainData.Chance);
                    chanceColor = Colors.White;
                }
                else {
                    chanceText = "?";
                    chanceColor = Colors.DarkGray;
                }

                var intensity = 0;
                var mm = 0.0;
                if (rainData != null) {
                    mm = rainData.Precipitation;
                    intensity = rainData.Intensity;
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
                mmImage = string.Format("Resources/intensity{0}.png", mmImage);

                Action setter = () => {
                    Chance.Text = chanceText;
                    Chance.Foreground = new SolidColorBrush(chanceColor);
                    IntensityValueNumber.Text = mmText;
                    IntensityImage.Source = new BitmapImage(new Uri("ms-appx://"+ mmImage, UriKind.Absolute));
                    VisualizeGraph(rainData, animated);
                };

                if (!Model.DataRootShown || !animated) {
                    Chance.Opacity = 1;
                    Model.ChanceShown = true;
                    setter();
                    return;
                }

                DataGrid.FadeOutThenIn(between: setter);
            });
        }

        private void VisualizeGraph(RainData rainData, bool animated)
        {
        }

        private void ShowData(Action<bool> action)
        {
            if (Model.ErrorImageShown) {
                ErrorImage.FadeOut(() => ShowData(action));
                return;
            }

            if (!Model.DataRootShown) {
                action(false);
                DataRoot.FadeIn();
                Model.DataRootShown = true;
                return;
            }

            action(true);
        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.  The Parameter
        /// property is typically used to configure the page.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs routedEventArgs)
        {
            FetchRain();
        }

        private void InfoButton_Click(object sender, RoutedEventArgs routedEventArgs)
        {
            Model.IsAboutOpen = true;
            //this.Frame.Navigate();
        }

    }
}
