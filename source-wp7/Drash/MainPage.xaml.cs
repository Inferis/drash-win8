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
        private PageOrientation lastOrientation;

        private bool loaded;
        private bool updatingLocation = false;
        private Size graphSize;
        private bool dragging;
        private int dragDelta;
        private int graphEntries = 0;

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
                if (Model.Location != null && !Model.Location.IsUnknown && Model.Location.GetDistanceTo(a.Position.Location) < 20)
                    return;
                UpdateLocation(a.Position.Location);
            };
            watcher.StatusChanged += (o, args) => UpdateLocation(watcher.Position.Location);

            graphStrokeColor = ((SolidColorBrush)Graph.Stroke).Color;
            graphFillFrom = ((LinearGradientBrush)Graph.Fill).GradientStops[0].Color;

            Loaded += MainPageLoaded;
        }

        private void MainPageLoaded(object sender, System.Windows.RoutedEventArgs e)
        {
            var asked = DrashSettings.LocationAllowedAsked;
            var allowed = DrashSettings.LocationAllowed;
            lastOrientation = Orientation;

            VisualizeEntries();
            SplashFadeout.Begin(() => {
                loaded = true;

                if (!asked) {
                    var result = MessageBox.Show("Would you like to display the rain forecast based on your current location? This will send your location to buienradar.nl to retrieve the forecast. You can disable location services in the About screen.", "Enable location services?", MessageBoxButton.OKCancel);
                    allowed = result == MessageBoxResult.OK;
                    DrashSettings.LocationAllowed = allowed;
                }

                if (allowed) {
                    watcher.Start();
                    firstFetch = watcher.Position != null;
                }

                GestureService.GetGestureListener(this).Hold += (o, args) => FetchRain();
                GestureService.GetGestureListener(this).DragStarted += (o, args) => {
                    dragging = args.GetPosition(Graph).Y >= 0;
                    dragDelta = 0;
                };
                GestureService.GetGestureListener(this).DragDelta += (o, args) => {
                    if (dragging) {
                        if (dragDelta != 0 && Math.Sign(args.HorizontalChange) != Math.Sign(dragDelta))
                            dragDelta = 0;
                        Zoomed(args.HorizontalChange);
                    }
                };
            });

            TransitionService.SetNavigationInTransition(this, new NavigationInTransition() {
                Backward = new TurnstileTransition() { Mode = TurnstileTransitionMode.BackwardIn },
                Forward = new TurnstileTransition() { Mode = TurnstileTransitionMode.ForwardIn }
            });

        }

        private void Zoomed(double horizontalChange)
        {
            dragDelta = (int)Math.Round(dragDelta + horizontalChange);
            if (Math.Abs(dragDelta) < 30)
                return;

            var factor = 1 + (int)Math.Floor((Math.Abs(dragDelta) - 30) / 45.0);
            var entries = Model.Entries + (dragDelta < 0 ? 3 : -3) * factor;
            entries = Math.Min(Math.Max(6, entries), 24);
            dragDelta = 0;

            if (entries != Model.Entries) {
                Model.Entries = entries;
                VisualizeEntries();
                UpdateState();
            }
        }

        protected override void OnOrientationChanged(OrientationChangedEventArgs e)
        {
            var newOrientation = e.Orientation;
            var transitionElement = new RotateTransition();

            switch (newOrientation) {
                case PageOrientation.Landscape:
                case PageOrientation.LandscapeRight:
                    // Come here from PortraitUp (i.e. clockwise) or LandscapeLeft?
                    if (lastOrientation == PageOrientation.PortraitUp)
                        transitionElement.Mode = RotateTransitionMode.In90Counterclockwise;
                    else
                        transitionElement.Mode = RotateTransitionMode.In180Clockwise;
                    break;
                case PageOrientation.LandscapeLeft:
                    // Come here from LandscapeRight or PortraitUp?
                    if (lastOrientation == PageOrientation.LandscapeRight)
                        transitionElement.Mode = RotateTransitionMode.In180Counterclockwise;
                    else
                        transitionElement.Mode = RotateTransitionMode.In90Clockwise;
                    break;
                case PageOrientation.Portrait:
                case PageOrientation.PortraitUp:
                    // Come here from LandscapeLeft or LandscapeRight?
                    if (lastOrientation == PageOrientation.LandscapeLeft)
                        transitionElement.Mode = RotateTransitionMode.In90Counterclockwise;
                    else
                        transitionElement.Mode = RotateTransitionMode.In90Clockwise;
                    break;
                default:
                    break;
            }

            // Execute the transition
            var page = (PhoneApplicationPage)(((PhoneApplicationFrame)Application.Current.RootVisual)).Content;
            var transition = transitionElement.GetTransition(page);
            transition.Completed += (sender, args) => {
                transition.Stop();
                graphSize = new Size(0, 0);
                UpdateVisuals();
            };
            transition.Begin();

            if (newOrientation == PageOrientation.LandscapeLeft || newOrientation == PageOrientation.LandscapeRight) {
                DataRoot.Margin = new Thickness(70, 0, 70, 0);
            }
            else {
                DataRoot.Margin = new Thickness(0);
            }
            lastOrientation = newOrientation;
            base.OnOrientationChanged(e);
        }

        private bool UpdateLocation(GeoCoordinate newLocation)
        {
            if (newLocation == null || newLocation.IsUnknown) {
                Model.LocationName = "";
                Model.GoodLocationName = false;
                UpdateState();
                return false;
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
            return true;
        }

        private void ResolveCurrentLocation()
        {
            if (Model.Location == null || Model.Location.IsUnknown)
                return;

            updatingLocation = true;
            UpdateSpinner();
            var resolver = new CivicAddressResolver();
            resolver.ResolveAddressCompleted += (o, args) => {
                if (args.Address == null || args.Address.IsUnknown) {
                    ResolveCurrentLocationThroughGoogle();
                }
                else {
                    Model.LocationName = string.Format("{0}, {1}", args.Address.City, args.Address.CountryRegion);
                    Model.GoodLocationName = true;
                    updatingLocation = false;
                    UpdateSpinner();
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
                updatingLocation = false;
                if (args.Address == null || args.Address.IsUnknown) {
                    if (Model.Location != null && !Model.Location.IsUnknown) {
                        Model.LocationName = string.Format(CultureInfo.InvariantCulture, "{0:0.000000}, {1:0.000000}", Model.Location.Latitude, Model.Location.Longitude);
                        Model.GoodLocationName = false;
                    }
                }
                else {
                    Model.LocationName = string.Format("{0}, {1}", args.Address.City, args.Address.CountryRegion);
                    Model.GoodLocationName = true;
                }
                UpdateSpinner();
                UpdateState();
            };
            resolver.ResolveAddressAsync(Model.Location);
        }

        private void FetchRain()
        {
            if (fetchingRain) return;

            updateRain.Cancel();
            if (Model.Location == null || Model.Location.IsUnknown) {
                // no location, schedule new fetch
                if (!UpdateLocation(watcher.Position.Location))
                    updateRain.Run(FetchRain);
                return;
            }

            if (!NetworkInterface.GetIsNetworkAvailable()) {
                // no network, schedule new fetch
                UpdateState();
                updateRain.Run(FetchRain);
                return;
            }

            // if we don't have a good location name, try to update it
            if (!Model.GoodLocationName) {
                // also try to resolve location
                updateLocationName.Run(ResolveCurrentLocation, 1000);
            }

            UpdateSpinner();
            fetchingRain = true;
            firstFetch = false;
            var uri = string.Format(CultureInfo.InvariantCulture, "http://gps.buienradar.nl/getrr.php?lat={0:0.000000}&lon={1:0.000000}&stamp={2}", Model.Location.Latitude, Model.Location.Longitude, DateTime.UtcNow.Ticks);

            var wc = new WebClient();
            wc.DownloadStringCompleted += (sender, args) => {
                if (args.Error == null) {
                    RainData.TryParse(args.Result, out Model.Rain);
                    fetchingRain = false;
                }
                UpdateSpinner();
                Model.RainWasUpdated = true;
                UpdateState();
                updateRain.Run(FetchRain);
            };
            wc.DownloadStringAsync(new Uri(uri));
        }

        private void UpdateSpinner()
        {
            spinner.IsVisible = fetchingRain || updatingLocation;
        }

        private void UpdateState()
        {
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

        private void VisualizeEntries()
        {
            ZoomImage.Source = new BitmapImage(new Uri(string.Format("Resources/dial{0}.png", Model.Entries * 5), UriKind.Relative));
            ZoomText.Text = string.Format("{0}min", Model.Entries * 5);
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
                var chance = rainData.ChanceForEntries(Model.Entries);
                if (rainData != null && chance >= 0) {
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

                var solarinfo = SolarInfo.ForDate(Model.Location.Latitude, Model.Location.Longitude, DateTime.Now);
                var sunrisen = solarinfo.Sunrise < DateTime.UtcNow && DateTime.UtcNow < solarinfo.Sunset;
                var night = intensity == 0 && !sunrisen ? "n" : "";
                mmImage = string.Format("Resources/intensity{0}{1}.png", mmImage, night);

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
            if (Graph.ActualWidth == 0 || Graph.ActualHeight == 0)
                return;

            List<int> pointValues;
            if (rainData == null || rainData.Points == null)
                pointValues = new List<int>();
            else
                pointValues = rainData.Points.Take(25).Select(p => p.AdjustedValue).ToList();

            while (pointValues.Count < 25)
                pointValues.Add(0);

            var path = Graph.Data as PathGeometry;
            if (graphSize.Width == 0 && graphSize.Height == 0) {
                graphSize = new Size(GraphContainer.ActualWidth, GraphContainer.ActualHeight);
            }

            var step = graphSize.Width / Model.Entries;
            Func<int, double> xForIndex = idx => idx == 0 ? -2 : idx >= Model.Entries ? graphSize.Width + 2 : idx * step;

            var x = 0;
            var max = graphSize.Height + 2 - 80;
            var allZeros = pointValues.Take(Model.Entries + 1).All(p => p == 0);
            var points = pointValues.Select(v => {
                var y = allZeros ? max - 40 : Math.Max(1, max - (v * max / 100));
                var p = new Point(xForIndex(x), y);
                x++;
                return p;
            }).ToList();
            points.Add(new Point(graphSize.Width + 2, graphSize.Height + 2));
            //points.Add(new Point(-2, graphSize.Height + 2));

            PathFigure figure;
            if (path == null) {
                path = new PathGeometry();
                figure = new PathFigure() { StartPoint = new Point(-2.0, graphSize.Height + 2), IsClosed = true };
                path.Figures.Add(figure);
                foreach (var p in points) {
                    figure.Segments.Add(new LineSegment() { Point = p });
                }
                Graph.Data = path;
            }

            animated = animated || graphEntries != Model.Entries && graphEntries > 0 && Model.Entries > 0;
            graphEntries = Model.Entries;
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
            if (Model.IsAboutOpen) {
                Model.IsAboutOpen = false;
                if (DrashSettings.LocationAllowed) {
                    watcher.Start();
                }
                else {
                    watcher.Stop();
                    Model.Location = GeoCoordinate.Unknown;
                    UpdateState();
                }
            }
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