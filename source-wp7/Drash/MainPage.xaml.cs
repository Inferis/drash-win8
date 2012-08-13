using System;
using System.Collections.Generic;
using System.Device.Location;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.Serialization;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using Microsoft.Phone.Controls;
using NetworkInterface = System.Net.NetworkInformation.NetworkInterface;

namespace Drash
{
    public partial class MainPage : PhoneApplicationPage
    {
        private readonly GeoCoordinateWatcher watcher;
        private bool fetchingRain;
        private readonly DelayedAction updateLocation;
        private readonly DelayedAction updateLocationName;
        private readonly DelayedAction updateRain;
        private bool firstFetch;
        private readonly Color graphStrokeColor;
        private readonly Color graphFillFrom;

        private bool loaded;

        public Model Model
        {
            get { return ((App)Application.Current).Model; }
        }

        public void UpdateStateFromModel()
        {
            Location.Opacity = Model.LocationShown ? 1 : 0;
            DataRoot.Opacity = Model.DataRootShown ? 1 : 0;
            ErrorImage.Opacity = Model.ErrorImageShown ? 1 : 0;
            Chance.Opacity = Model.ChanceShown ? 1 : 0;
            IntensityImage.Opacity = Model.IntensityValueShown ? 0 : 1;
            IntensityValue.Opacity = Model.IntensityValueShown ? 1 : 0;
            updateLocation.Cancel();
            updateLocationName.Cancel();

            UpdateState();
            updateRain.Run(FetchRain);
        }

        // Constructor
        public MainPage()
        {
            InitializeComponent();

            updateLocationName = new DelayedAction(Dispatcher);
            updateLocation = new DelayedAction(Dispatcher);
            updateRain = new DelayedAction(Dispatcher, 3 * 60 * 1000);

            UpdateStateFromModel();
            NetworkChange.NetworkAddressChanged += (sender, args) => UpdateState();

            watcher = new GeoCoordinateWatcher { MovementThreshold = 500 };
            watcher.PositionChanged += (s, a) => {
                if (Model.Location != null && Model.Location.GetDistanceTo(a.Position.Location) < 20)
                    return;
                UpdateLocation(a.Position.Location);
            };
            watcher.StatusChanged += (o, args) => UpdateLocation(watcher.Position.Location);

            graphStrokeColor = ((SolidColorBrush)Graph.Stroke).Color;
            graphFillFrom = ((LinearGradientBrush)Graph.Fill).GradientStops[0].Color;

            Loaded += MainPageLoaded;

            ThreadPool.QueueUserWorkItem(o => {
                watcher.Start();
                firstFetch = watcher.Position != null;
            });
        }

        private void MainPageLoaded(object sender, System.Windows.RoutedEventArgs e)
        {
            loaded = true;

            SplashFadeout.Begin(() => {
                GestureService.GetGestureListener(this).Hold += (o, args) => FetchRain();
            });

            TransitionService.SetNavigationInTransition(this, new NavigationInTransition() {
                Backward = new TurnstileTransition() { Mode = TurnstileTransitionMode.BackwardIn },
                Forward = new TurnstileTransition() { Mode = TurnstileTransitionMode.ForwardIn }
            });

        }

        private void UpdateLocation(GeoCoordinate newLocation)
        {
            if (newLocation == null || newLocation.IsUnknown) {
                Model.LocationName = "";
                Model.GoodLocationName = false;
                UpdateState();
                return;
            }

            var delay = firstFetch || (Model.Location == null || Model.Location.IsUnknown) ||
                        Model.Location.GetDistanceTo(newLocation) > 500
                            ? 0
                            : 2000;
            updateLocation.Run(() => {
                Model.Location = newLocation;
                UpdateState();
                FetchRain();

                if (Model.Location != null && !Model.Location.IsUnknown) {
                    updateLocationName.Run(ResolveCurrentLocation, 1000);
                }
            }, delay);
        }

        private void ResolveCurrentLocation()
        {
            if (Model.Location == null || Model.Location.IsUnknown)
                return;

            var resolver = new CivicAddressResolver();
            resolver.ResolveAddressCompleted += (o, args) => {
                if (args.Address == null || args.Address.IsUnknown) {
                    ResolveCurrentLocationThroughGoogle();
                }
                else {
                    Model.LocationName = string.Format("{0}, {1}", args.Address.City, args.Address.CountryRegion);
                    Model.GoodLocationName = true;
                    UpdateState();
                }
            };
            resolver.ResolveAddressAsync(Model.Location);
        }

        private void ResolveCurrentLocationThroughGoogle()
        {
            if (Model.Location == null || Model.Location.IsUnknown)
                return;

            var resolver = new GoogleAddressResolver();
            resolver.ResolveAddressCompleted += (o, args) => {
                if (args.Address == null || args.Address.IsUnknown) {
                    if (Model.Location != null && !Model.Location.IsUnknown) {
                        Model.LocationName = string.Format("{0:0.000000}, {1:0.000000}", Model.Location.Latitude, Model.Location.Longitude);
                        Model.GoodLocationName = false;
                    }
                }
                else {
                    Model.LocationName = string.Format("{0}, {1}", args.Address.City,
                                                 args.Address.CountryRegion);
                    Model.GoodLocationName = true;
                }
                UpdateState();
            };
            resolver.ResolveAddressAsync(Model.Location);
        }

        private void FetchRain()
        {
            if (fetchingRain) return;

            updateRain.Cancel();
            if (Model.Location == null || Model.Location.IsUnknown) {
                updateRain.Run(FetchRain);
                return;
            }

            if (!NetworkInterface.GetIsNetworkAvailable()) {
                UpdateState();
                updateRain.Run(FetchRain);
                return;
            }

            // if we don't have a good location name, try to update it
            if (!Model.GoodLocationName && Model.Location != null && !Model.Location.IsUnknown) {
                updateLocationName.Run(ResolveCurrentLocation, 1000);
            }

            spinner.IsVisible = true;
            fetchingRain = true;
            firstFetch = false;
            var uri = string.Format("http://gps.buienradar.nl/getrr.php?lat={0:0.000000}&lon={1:0.000000}", Model.Location.Latitude, Model.Location.Longitude);

            var wc = new WebClient();
            wc.DownloadStringCompleted += (sender, args) => {
                if (args.Error == null) {
                    RainData.TryParse(args.Result, out Model.Rain);
                    fetchingRain = false;
                }
                spinner.IsVisible = false;
                Model.RainWasUpdated = true;
                UpdateState();
                updateRain.Run(FetchRain);
            };
            wc.DownloadStringAsync(new Uri(uri));
        }

        private void UpdateState()
        {
            if (!loaded)
                return;

            try {
                if (!NetworkInterface.GetIsNetworkAvailable()) {
                    Model.Error = DrashError.NoNetwork;
                    return;
                }

                if (Model.Location == null || Model.Location.IsUnknown) {
                    Model.Error = DrashError.NoLocation;
                    return;
                }

                Model.Error = DrashError.None;
            }
            finally {
                UpdateVisuals();
            }
        }

        private void UpdateVisuals()
        {
            if (Model.Error != DrashError.None) {
                VisualizeError(Model.Error);
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

        private void VisualizeError(DrashError drashError)
        {
            if (Model.DataRootShown) {
                DataRoot.FadeOut(() => VisualizeError(drashError));
                Model.DataRootShown = false;
                return;
            }

            var uri = new Uri(drashError == DrashError.NoLocation ? "Resources/nolocation.png" : "Resources/nonetwork.png", UriKind.Relative);
            if (!Model.ErrorImageShown) {
                ErrorImage.Source = new BitmapImage(uri);
                ErrorImage.FadeIn();
                Model.ErrorImageShown = true;
                return;
            }

            var current = ErrorImage.Source as BitmapImage;
            if (current != null && uri == current.UriSource)
                return;

            ErrorImage.FadeOutThenIn(between: () => {
                ErrorImage.Source = new BitmapImage(uri);
            });
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
                if (rainData != null && (rainData.Intensity > 0 || rainData.Precipitation > 0)) {
                    var mm = Math.Max(rainData.Precipitation, 0.01);
                    mmText = Math.Floor(mm) == mm ? string.Format("{0}", (int)mm) : string.Format("{0:0.00}", mm);
                    mmImage = ((int)Math.Max(1, Math.Min(1 + rainData.Intensity / 25.0, 4))).ToString(CultureInfo.InvariantCulture);
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
                    IntensityImage.Source = new BitmapImage(new Uri(mmImage, UriKind.Relative));
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

            List<int> pointValues;
            if (rainData == null || rainData.Points == null)
                pointValues = new List<int>();
            else
                pointValues = rainData.Points.Select(p => p.AdjustedValue).ToList();

            while (pointValues.Count < 7)
                pointValues.Add(0);

            var step = Graph.ActualWidth / pointValues.Count;
            Func<int, double> xForIndex = idx => idx == 0 ? -2 : idx == pointValues.Count - 1 ? Graph.ActualWidth + 2 : idx * step;

            var x = 0;
            var max = Graph.ActualHeight + 2 - 10;
            var allZeros = pointValues.All(p => p == 0);
            var points = pointValues.Select(v => {
                var y = allZeros ? max - 40 : Math.Max(1, max - (v * max / 100));
                var p = new Point(xForIndex(x), y);
                x++;
                return p;
            }).ToList();

            var path = Graph.Data as PathGeometry;
            PathFigure figure;
            if (path == null) {
                path = new PathGeometry();
                figure = new PathFigure() { StartPoint = new Point(-2.0, Graph.ActualHeight + 2), IsClosed = true };
                path.Figures.Add(figure);
                foreach (var p in points) {
                    figure.Segments.Add(new LineSegment() { Point = p });
                }
                figure.Segments.Add(new LineSegment() { Point = new Point(Graph.ActualWidth + 2, Graph.ActualHeight + 2) });
                Graph.Data = path;
            }

            var ms300 = TimeSpan.FromMilliseconds(animated ? 300 : 0);
            var storyboard = new Storyboard() { Duration = ms300 };

            figure = path.Figures[0];
            for (var i = 0; i < points.Count; ++i) {
                var anim = new PointAnimation() { Duration = ms300, To = points[i], FillBehavior = FillBehavior.HoldEnd };
                Storyboard.SetTarget(anim, figure.Segments[i]);
                Storyboard.SetTargetProperty(anim, new PropertyPath(LineSegment.PointProperty));
                storyboard.Children.Add(anim);
            }

            var strokeAnim = new ColorAnimation() { Duration = ms300, FillBehavior = FillBehavior.HoldEnd, To = allZeros ? Colors.Transparent : graphStrokeColor };
            Storyboard.SetTarget(strokeAnim, Graph.Stroke);
            Storyboard.SetTargetProperty(strokeAnim, new PropertyPath(SolidColorBrush.ColorProperty));
            storyboard.Children.Add(strokeAnim);

            var fillTopAnim = new ColorAnimation() { Duration = ms300, FillBehavior = FillBehavior.HoldEnd, To = allZeros ? Colors.Black : graphFillFrom };
            Storyboard.SetTarget(fillTopAnim, ((LinearGradientBrush)Graph.Fill).GradientStops[0]);
            Storyboard.SetTargetProperty(fillTopAnim, new PropertyPath(GradientStop.ColorProperty));
            storyboard.Children.Add(fillTopAnim);

            storyboard.Begin(() => Graph.Data = path);
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

        private void RefreshButton_Click(object sender, EventArgs e)
        {
            FetchRain();
        }

        private void InfoButton_Click(object sender, EventArgs e)
        {
            Model.IsAboutOpen = true;
            NavigationService.Navigate(new Uri("/AboutPage.xaml", UriKind.Relative));
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            Model.IsAboutOpen = false;
        }

        private void Intensity_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (Model.IntensityValueShown) {
                IntensityValue.FadeOut(() => IntensityImage.FadeIn(duration: 150), duration: 150);
                Model.IntensityValueShown = false;
            }
            else {
                IntensityImage.FadeOut(() => IntensityValue.FadeIn(duration: 150), duration: 150);
                Model.IntensityValueShown = true;
            }

        }

        public void RestoreState(IDictionary<string, object> state)
        {
            var mustUpdate = false;
            var mustLocate = false;

            if (state.ContainsKey("lat") && state.ContainsKey("lng")) {
                Model.Location = new GeoCoordinate((double)state["lat"], (double)state["lng"]);
                mustLocate = true;
                mustUpdate = true;
            }

            if (state.ContainsKey("locationName")) {
                Model.LocationName = state["locationName"] as string;
                if (!string.IsNullOrEmpty(Model.LocationName))
                    mustLocate = false;
            }

            if (state.ContainsKey("rain")) {
                using (var buffer = new MemoryStream((byte[])state["rain"])) {
                    var serializer = new DataContractSerializer(typeof(RainData), new[] { typeof(RainPoint) });
                    Model.Rain = (RainData)serializer.ReadObject(buffer);
                }

                Model.RainWasUpdated = true;
            }

            if (!mustUpdate) return;

            if (mustLocate)
                UpdateLocation(Model.Location);
            UpdateState();
        }

        public void StoreState(IDictionary<string, object> state)
        {
            if (Model.Location == null || Model.Location.IsUnknown) {
                if (state.ContainsKey("lat")) state.Remove("lat");
                if (state.ContainsKey("lng")) state.Remove("lng");
            }
            else {
                state["lat"] = Model.Location.Latitude;
                state["lng"] = Model.Location.Longitude;
            }


            state["name"] = Model.LocationName;
            if (Model.Rain == null) {
                if (state.ContainsKey("rain")) state.Remove("rain");
            }
            else {
                var serializer = new DataContractSerializer(typeof(RainData), new[] { typeof(RainPoint) });
                using (var buffer = new MemoryStream()) {
                    serializer.WriteObject(buffer, Model.Rain);
                    buffer.Flush();
                    state["rain"] = buffer.GetBuffer();
                }
            }
        }
    }
}