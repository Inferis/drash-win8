using System;
using System.Collections.Generic;
using System.Device.Location;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using NetworkInterface = System.Net.NetworkInformation.NetworkInterface;

namespace Drash
{
    public partial class MainPage : PhoneApplicationPage
    {
        private readonly GeoCoordinateWatcher watcher;
        private bool fetchingRain;
        private RainData rain;
        private GeoCoordinate location;
        private string locationName;
        private readonly DelayedAction updateLocation;
        private readonly DelayedAction updateLocationName;
        private bool firstFetch;
        private DrashError error = DrashError.None;
        private bool rainWasUpdated = false;
        private readonly Color graphStrokeColor;
        private readonly Color graphFillFrom;
        private readonly Color graphFillTo;

        // Constructor
        public MainPage()
        {
            InitializeComponent();

            updateLocationName = new DelayedAction(Dispatcher);
            updateLocation = new DelayedAction(Dispatcher);

            watcher = new GeoCoordinateWatcher { MovementThreshold = 500 };
            watcher.PositionChanged += (s, a) => {
                if (location != null && location.GetDistanceTo(a.Position.Location) < 20)
                    return;
                UpdateLocation(a.Position.Location);
            };
            watcher.StatusChanged += (o, args) => {
                UpdateLocation(watcher.Position.Location);
            };

            graphStrokeColor = ((SolidColorBrush)Graph.Stroke).Color;
            graphFillFrom = ((LinearGradientBrush)Graph.Fill).GradientStops[0].Color;
            graphFillTo = ((LinearGradientBrush)Graph.Fill).GradientStops[1].Color;

            Loaded += MainPageLoaded;

            ThreadPool.QueueUserWorkItem(o => {
                watcher.Start();
                firstFetch = watcher.Position != null;
            });
        }

        private void MainPageLoaded(object sender, System.Windows.RoutedEventArgs e)
        {
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
                locationName = "";
                UpdateState();
                return;
            }

            var delay = firstFetch || (location == null || location.IsUnknown) ||
                        location.GetDistanceTo(newLocation) > 500
                            ? 0
                            : 2000;
            updateLocation.Run(() => {
                location = newLocation;
                UpdateState();
                FetchRain();

                if (location != null && !location.IsUnknown) {
                    updateLocationName.Run(ResolveCurrentLocation, 1000);
                }
            }, delay);
        }

        private void ResolveCurrentLocation()
        {
            if (location == null || location.IsUnknown)
                return;

            var resolver = new CivicAddressResolver();
            resolver.ResolveAddressCompleted += (o, args) => {
                if (args.Address == null || args.Address.IsUnknown) {
                    ResolveCurrentLocationThroughGoogle();
                }
                else {
                    locationName = string.Format("{0}, {1}", args.Address.City,
                                                 args.Address.CountryRegion);
                    UpdateState();
                }
            };
            resolver.ResolveAddressAsync(location);
        }

        private void ResolveCurrentLocationThroughGoogle()
        {
            if (location == null || location.IsUnknown)
                return;

            var resolver = new GoogleAddressResolver();
            resolver.ResolveAddressCompleted += (o, args) => {
                if (args.Address == null || args.Address.IsUnknown) {
                    locationName = string.Format("{0:0.00000},{1:0.00000}",
                                                 watcher.Position.Location.
                                                     Latitude,
                                                 watcher.Position.Location.
                                                     Longitude);
                }
                else {
                    locationName = string.Format("{0}, {1}", args.Address.City,
                                                 args.Address.CountryRegion);
                }
                UpdateState();
            };
            resolver.ResolveAddressAsync(location);
        }

        private void FetchRain()
        {
            if (fetchingRain) return;
            if (location == null || location.IsUnknown)
                return;

            spinner.IsVisible = true;
            fetchingRain = true;
            firstFetch = false;
            var uri = string.Format("http://gps.buienradar.nl/getrr.php?lat={0}&lon={1}", location.Latitude, location.Longitude);

            var wc = new WebClient();
            wc.DownloadStringCompleted += (sender, args) => {
                if (args.Error != null) return;
                RainData.TryParse(args.Result, out rain);
                fetchingRain = false;
                spinner.IsVisible = false;
                rainWasUpdated = true;
                UpdateState();
            };
            wc.DownloadStringAsync(new Uri(uri));
        }

        private void UpdateState()
        {
            try {
                if (!NetworkInterface.GetIsNetworkAvailable()) {
                    error = DrashError.NoNetwork;
                    return;
                }

                if (location == null || location.IsUnknown) {
                    error = DrashError.NoLocation;
                    return;
                }

                error = DrashError.None;
            }
            finally {
                UpdateVisuals();
            }
        }

        private void UpdateVisuals()
        {
            if (error != DrashError.None) {
                VisualizeError(error);
                return;
            }

            if (ErrorImage.Opacity > 0) {
                ErrorImage.FadeOut(UpdateVisuals);
                return;
            }

            if (string.IsNullOrEmpty(locationName) && rain == null) {
                if (DataRoot.Opacity > 0)
                    DataRoot.FadeOut();
                return;
            }

            VisualizeRain(rain);
            VisualizeLocation(locationName);
        }

        private void VisualizeError(DrashError drashError)
        {
            if (DataRoot.Opacity > 0) {
                DataRoot.FadeIn(() => VisualizeError(drashError));
                return;
            }

            if (ErrorImage.Opacity == 0) {
                var uri = new Uri(drashError == DrashError.NoLocation ? "nolocation.png" : "nonetwork.png");
                ErrorImage.Source = new BitmapImage(uri);
                ErrorImage.FadeIn();
                return;
            }

            ErrorImage.FadeOutThenIn(between: () => {
                var uri = new Uri(drashError == DrashError.NoLocation ? "nolocation.png" : "nonetwork.png");
                ErrorImage.Source = new BitmapImage(uri);
            });
        }

        private void VisualizeLocation(string name)
        {
            ShowData(animated => {
                if (DataRoot.Opacity < 1) {
                    Location.Opacity = 1;
                    Location.Text = name;
                    return;
                }

                if (Location.Opacity < 1) {
                    Location.Text = name;
                    Location.FadeIn();
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
            var animate = rainWasUpdated;
            rainWasUpdated = false;
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
                    var mm = Math.Max(rainData.Precipitation, 0);
                    mmText = Math.Floor(mm) == mm ? string.Format("{0}mm", (int)mm) : string.Format("{0:0.00}mm", mm);
                    mmImage = ((int)Math.Max(1, Math.Min(1 + rainData.Intensity / 25.0, 4))).ToString(CultureInfo.InvariantCulture);
                }
                else {
                    mmText = "0mm";
                    mmImage = "0";
                }
                mmImage = string.Format("Resources/intensity{0}.png", mmImage);

                Action setter = () => {
                    Chance.Text = chanceText;
                    Chance.Foreground = new SolidColorBrush(chanceColor);
                    IntensityValue.Text = mmText;
                    IntensityImage.Source = new BitmapImage(new Uri(mmImage, UriKind.Relative));
                    VisualizeGraph(rainData, animated);
                };

                if (DataRoot.Opacity < 1 || !animated) {
                    Chance.Opacity = 1;
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

            //var fillBottomAnim = new ColorAnimation() { Duration = ms300, FillBehavior = FillBehavior.HoldEnd, To = allZeros ?  : graphFillTo };
            //Storyboard.SetTarget(fillBottomAnim, ((LinearGradientBrush)Graph.Fill).GradientStops[0]);
            //Storyboard.SetTargetProperty(fillBottomAnim, new PropertyPath(GradientStop.ColorProperty));
            //storyboard.Children.Add(fillBottomAnim);

            storyboard.Begin(() => Graph.Data = path);
        }

        private void ShowData(Action<bool> action)
        {
            if (ErrorImage.Opacity > 0) {
                ErrorImage.FadeIn(() => ShowData(action));
                return;
            }

            if (DataRoot.Opacity < 1) {
                var transform = (CompositeTransform)DataRoot.RenderTransform;
                transform.ScaleX = 0.9;
                transform.ScaleY = 0.9;
                action(false);
                DataRoot.FadeIn();
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
            NavigationService.Navigate(new Uri("/AboutPage.xaml", UriKind.Relative));
        }

        private void Intensity_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (IntensityImage.Opacity > 0) {
                IntensityImage.FadeOut(() => IntensityValue.FadeIn(duration: 150), duration: 150);
            }
            else {
                IntensityValue.FadeOut(() => IntensityImage.FadeIn(duration: 150), duration: 150);
            }

        }
    }
}