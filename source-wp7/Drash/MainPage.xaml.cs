using System;
using System.Collections.Generic;
using System.Device.Location;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Net.NetworkInformation;
using NetworkInterface = System.Net.NetworkInformation.NetworkInterface;

namespace Drash
{
    public partial class MainPage : PhoneApplicationPage
    {
        private GeoCoordinateWatcher watcher;
        private bool fetchingRain;
        private RainData rain;
        private GeoCoordinate location;
        private string locationName;
        private readonly DelayedAction updateLocation = new DelayedAction();
        private readonly DelayedAction updateLocationName = new DelayedAction();
        private bool firstFetch;
        private DrashError error = DrashError.None;
        private bool rainWasUpdated = false;

        // Constructor
        public MainPage()
        {
            InitializeComponent();

            watcher = new GeoCoordinateWatcher { MovementThreshold = 500 };
            watcher.PositionChanged += (s, a) => {
                if (location != null && location.GetDistanceTo(a.Position.Location) < 20)
                    return;
                UpdateLocation(a.Position.Location);
            };
            watcher.StatusChanged += (o, args) => {
                UpdateLocation(watcher.Position.Location);
            };

            this.Loaded += MainPageLoaded;
        }

        private void MainPageLoaded(object sender, System.Windows.RoutedEventArgs e)
        {
            SplashFadeout.Completed += (s, a) => {
                watcher.Start();
                firstFetch = watcher.Position != null;
            };
            SplashFadeout.Begin();
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
                ErrorFadeout.Begin(300, UpdateVisuals);
                return;
            }

            if (string.IsNullOrEmpty(locationName) && rain == null) {
                if (DataRoot.Opacity > 0)
                    DataRootFadeout.Begin();
                return;
            }

            VisualizeRain(rain);
            VisualizeLocation(locationName);
        }

        private void VisualizeError(DrashError drashError)
        {
            if (DataRoot.Opacity > 0) {
                DataRootFadeout.Begin(300, () => VisualizeError(drashError));
                return;
            }

            if (ErrorImage.Opacity < 1) {
                var uri = new Uri(drashError == DrashError.NoLocation ? "nolocation.png" : "nonetwork.png");
                ErrorImage.Source = new BitmapImage(uri);
                var transform = (CompositeTransform)ErrorImage.RenderTransform;
                transform.ScaleX = 0.9;
                transform.ScaleY = 0.9;
                ErrorFadein.Begin();
                return;
            }

            ErrorFadeout.Begin(150, () => {
                var uri = new Uri(drashError == DrashError.NoLocation ? "nolocation.png" : "nonetwork.png");
                ErrorImage.Source = new BitmapImage(uri);
                ErrorFadein.Begin(150);
            });
        }

        private void VisualizeLocation(string name)
        {
            ShowData(() => {
                if (DataRoot.Opacity < 1) {
                    Location.Opacity = 1;
                    Location.Text = name;
                    return;
                }

                if (Location.Opacity < 1) {
                    Location.Text = name;
                    LocationFadein.Begin(300);
                    return;
                }

                if (Location.Text == name)
                    return;

                LocationFadeout.Begin(150, () => {
                    Location.Text = name;
                    LocationFadein.Begin(150);
                });

            });
        }

        private void VisualizeRain(RainData rainData)
        {
            var animate = rainWasUpdated;
            rainWasUpdated = false;
            ShowData(() => {
                string chanceText;
                Color chanceColor;

                if (rainData != null && rainData.Chance >= 0) {
                    chanceText = string.Format("{0}%", rainData.Chance);
                    chanceColor = Colors.White;
                }
                else {
                    chanceText = "?";
                    chanceColor = Colors.DarkGray;
                }

                Action setter = () => {
                    Chance.Text = chanceText;
                    Chance.Foreground = new SolidColorBrush(chanceColor);
                    VisualizeGraph(rainData);
                };

                if (DataRoot.Opacity < 1 || !animate) {
                    Chance.Opacity = 1;
                    setter();
                    return;
                }

                DataGridFadeout.Begin(150, () => {
                    setter();
                    DataGridFadein.Begin(150);
                });
            });
        }

        private void VisualizeGraph(RainData rainData)
        {
            List<int> pointValues;
            if (rainData == null || rainData.Points == null)
                pointValues = new List<int>();
            else
                pointValues = rainData.Points.Select(p => p.AdjustedValue).ToList();

            while (pointValues.Count < 7)
                pointValues.Add(0);

            //M-2,246 L-2,100 L80,120 L160,180 L240,190 L320,190 L400,170 L482,160 L482,246
            var existing1 = ((PathGeometry)Graph.Data);

            var x = 0;
            var points = pointValues.Select(v => {
                var y = 220 - (v * 220 / 100);
                var p = new Point(x == 0 ? -2 : x == 480 ? 482 : x, y);
                x += 480 / pointValues.Count;
                return p;
            }).ToList();

            var path = Graph.Data as PathGeometry;
            PathFigure figure;
            if (path == null) {
                path = new PathGeometry();
                figure = new PathFigure() { StartPoint = new Point(-2.0, 246.0), IsClosed = true };
                path.Figures.Add(figure);
                var zx = 0;
                foreach (var value in pointValues) {
                    figure.Segments.Add(new LineSegment() { Point = new Point(zx == 0 ? -2 : zx == 480 ? 482 : zx, 200) });
                    zx += 480 / pointValues.Count;
                }
                figure.Segments.Add(new LineSegment() { Point = new Point(482, 246) });
                Graph.Data = path;
            }

            var ms300 = TimeSpan.FromMilliseconds(300);
            var storyboard = new Storyboard() { Duration = ms300 };

            figure = path.Figures[0];
            for (var i = 0; i < points.Count; ++i) {
                var anim = new PointAnimation() { Duration= ms300, To = points[i], FillBehavior = FillBehavior.HoldEnd };
                Storyboard.SetTarget(anim, figure.Segments[i]);
                Storyboard.SetTargetProperty(anim, new PropertyPath(LineSegment.PointProperty));
                storyboard.Children.Add(anim);

                //((LineSegment)figure.Segments[i]).Point = points[i];
            }
            LayoutRoot.Resources.Add(Guid.NewGuid().ToString("N"), storyboard);
            storyboard.Begin(() => Graph.Data = path);
        }

        private void ShowData(Action action)
        {
            if (ErrorImage.Opacity > 0) {
                ErrorFadein.Begin(300, () => ShowData(action));
                return;
            }

            if (DataRoot.Opacity < 1) {
                var transform = (CompositeTransform)DataRoot.RenderTransform;
                transform.ScaleX = 0.9;
                transform.ScaleY = 0.9;
                action();
                DataRootFadein.Begin();
                return;
            }

            action();
        }

        private void RefreshButton_Click(object sender, EventArgs e)
        {
            FetchRain();
        }

        private void InfoButton_Click(object sender, EventArgs e)
        {
        }
    }
}