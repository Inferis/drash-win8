using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Windows.Input;
using Drash.Common;
using Drash.Common.Api;
using Drash.Extensions;
using Windows.Devices.Geolocation;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Shapes;

namespace Drash.Models
{
    public class ViewModel : NotifyPropertyChangedBase
    {
        private LayoutAwarePage view;
        private Geolocator geolocator;
        private DelayedAction delayedLocationUpdate, nextRainUpdate;
        private bool firstFetch = true, noLocationData = false;
        private bool fetchingRain = false;
        private bool isBusy;
        private string location;
        private string chance;
        private string precipitation;
        private ImageSource intensityImage, entriesImage;
        private int graphEntries = 0;
        private Color graphStrokeColor;
        private Color graphFillFrom;
        private Path graphView;
        private FrameworkElement graphContainer;
        private string entriesDescription;

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

        public ImageSource EntriesImage
        {
            get { return entriesImage; }
            set
            {
                entriesImage = value;
                OnPropertyChanged(() => EntriesImage);
            }
        }

        public string EntriesDescription
        {
            get { return entriesDescription; }
            set
            {
                entriesDescription = value;
                OnPropertyChanged(() => EntriesDescription);
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

        public DrashState State
        {
            get { return Model != null ? Model.State : DrashState.Starting; }
        }

        #endregion

        protected Model Model { get; set; }

        public Path GraphView
        {
            get { return graphView; }
            set
            {
                graphView = value;
                graphStrokeColor = ((SolidColorBrush)graphView.Stroke).Color;
                graphFillFrom = ((LinearGradientBrush)graphView.Fill).GradientStops[0].Color;
            }
        }

        public FrameworkElement GraphContainer
        {
            get { return graphContainer; }
            set
            {
                if (graphContainer != null)
                    graphContainer.SizeChanged -= GraphContainerOnSizeChanged;
                graphContainer = value;
                if (graphContainer != null)
                    graphContainer.SizeChanged += GraphContainerOnSizeChanged;
            }
        }

        private void GraphContainerOnSizeChanged(object sender, SizeChangedEventArgs sizeChangedEventArgs)
        {
            VisualizeGraph(Model.Rain, false);
        }

        public ViewModel()
        {
            RefreshCommand = new ActionCommand(FetchRain);
        }

        private void RegisterLiveTile()
        {
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

            UpdateEntriesImage();
            UpdateState();
            InitializeGeolocator();
        }

        private void InitializeGeolocator()
        {
            GoToState(DrashState.FindingLocation);
            geolocator = new Geolocator() { MovementThreshold = 500, DesiredAccuracy = PositionAccuracy.High };
            geolocator.PositionChanged += (s, a) => {
                if (Model.Location != null && Model.Location.GetDistanceTo(a.Position.Coordinate) < 20)
                    return;
                UpdateLocation(a.Position);
            };
            geolocator.StatusChanged += async (o, args) => {
                if (args.Status == PositionStatus.Disabled || args.Status == PositionStatus.NotAvailable || args.Status == PositionStatus.NoData) {
                    noLocationData = true;
                    UpdateLocation(null);
                    geolocator = null;
                    return;
                }

                noLocationData = false;
                if (args.Status == PositionStatus.Ready) {
                    try {
                        var pos = await geolocator.GetDrashGeopositionAsync();
                        UpdateLocation(pos);
                    }
                    catch (Exception) {
                        Debug.WriteLine("status Failed");
                    }
                }
                else {
                    UpdateState();
                }
            };

            if (firstFetch)
                geolocator.GetDrashGeopositionAsync();
        }

        private bool UpdateLocation(Geoposition newLocation)
        {
            if (newLocation == null || newLocation.Coordinate == null) {
                Model.LocationName = "";
                Model.GoodLocationName = false;
                Model.Location = null;
                UpdateState();
                return false;
            }

            var delay = firstFetch || Model.Location == null ||
                        Model.Location.GetDistanceTo(newLocation.Coordinate) > 500
                            ? 0
                            : 2000;
            delayedLocationUpdate.Run(async () => {
                await Task.Delay(2000);
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

            nextRainUpdate.Cancel();
            if (Model.Location == null) {
                // no location, schedule new fetch
                if (geolocator.LocationStatus != PositionStatus.Ready || !UpdateLocation(await geolocator.GetDrashGeopositionAsync()))
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

            try {
                MessageDialog dialog = null;

                try {
                    var uri = string.Format(CultureInfo.InvariantCulture, "http://gps.buienradar.nl/getrr.php?lat={0:0.000000}&lon={1:0.000000}&stamp={2}", Model.Location.Latitude, Model.Location.Longitude, DateTime.UtcNow.Ticks);

                    var wc = new HttpClient();
                    await Task.Delay(3000);
                    var result = await wc.GetAsync(uri);
                    if (result.IsSuccessStatusCode) {
                        Model.Rain = await RainData.TryParseAsync(await result.Content.ReadAsStringAsync());
                        if (Model.Rain != null) {
                            DrashTile.Update(Model.Rain.ChanceForEntries(6), Model.LocationName);
                        }
                    }
                    Model.RainWasUpdated = true;
                    firstFetch = false;
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
                    GoToState(noLocationData ? DrashState.NoLocation : DrashState.FindingLocation);
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
                    VisualizeGraph(Model.Rain);
                    Location = Model.LocationName;
                });

        }

        private async void VisualizeRain(RainData rainData)
        {
            if (Model.RainWasUpdated)
                Animatable.Mode = AnimatableMode.Forced;
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
            mmText = mmText + "\nmm";

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

            // we have to jump through silly hoops to have the text change
            if (Animatable.Mode == AnimatableMode.Forced) {
                Animatable.Mode = AnimatableMode.Disabled;
                if (Chance == chanceText) Chance = null;
                if (Precipitation == mmText) Precipitation = null;
                Animatable.Mode = AnimatableMode.Forced;
            }
            Chance = chanceText;
            Precipitation = mmText;

            IntensityImage = new BitmapImage(new Uri(mmImage));

            await Task.Delay(500);
            Animatable.Mode = AnimatableMode.Enabled;
        }

        private void VisualizeGraph(RainData rainData, bool animated = true)
        {
            List<int> pointValues;
            if (rainData == null || rainData.Points == null)
                pointValues = new List<int>();
            else
                pointValues = rainData.Points.Take(25).Select(p => p.AdjustedValue).ToList();

            while (pointValues.Count < 25)
                pointValues.Add(pointValues.LastOrDefault());

            var path = GraphView.Data as PathGeometry;
            var graphSize = new Size(GraphContainer.ActualWidth, GraphContainer.ActualHeight);
            var step = graphSize.Width / Model.Entries;
            Func<int, double> xForIndex = idx => idx == 0 ? -2 : idx * step;

            var x = 0;
            var max = (graphSize.Height / 12.0 * 11.0) - 10;
            var allZeros = pointValues.Take(Model.Entries + 1).All(p => p == 0);
            var points = pointValues.Select(v => {
                var y = allZeros ? max - 20 : Math.Max(1, max - (v * max / 100));
                var p = new Point(xForIndex(x), y);
                x++;
                return p;
            }).ToList();
            points.Add(new Point(graphSize.Width + 2, points.Last().Y));
            points.Add(new Point(graphSize.Width + 2, graphSize.Height + 2));

            PathFigure figure;
            var startPoint = new Point(-2.0, graphSize.Height + 2);
            if (path == null) {
                path = new PathGeometry();
                figure = new PathFigure() { StartPoint = startPoint, IsClosed = true };
                path.Figures.Add(figure);
                foreach (var p in points) {
                    figure.Segments.Add(new LineSegment() { Point = p });
                }
                GraphView.Data = path;
            }

            var entriesAnimated = graphEntries != Model.Entries && graphEntries > 0 && Model.Entries > 0;
            if (entriesAnimated)
                UpdateEntriesImage();

            var ms300 = TimeSpan.FromMilliseconds(entriesAnimated ? 450 / Math.Abs(Model.Entries - graphEntries) : animated ? 300 : 0);
            graphEntries = Model.Entries;
            var storyboard = new Storyboard() { Duration = ms300 };


            figure = path.Figures[0];
            var anim = new PointAnimation() { Duration = ms300, To = startPoint, FillBehavior = FillBehavior.HoldEnd, EnableDependentAnimation = true };
            Storyboard.SetTarget(anim, figure);
            Storyboard.SetTargetProperty(anim, "StartPoint");
            storyboard.Children.Add(anim);

            for (var i = 0; i < points.Count; ++i) {
                anim = new PointAnimation() { Duration = ms300, To = points[i], FillBehavior = FillBehavior.HoldEnd, EnableDependentAnimation = true };
                Storyboard.SetTarget(anim, figure.Segments[i]);
                Storyboard.SetTargetProperty(anim, "Point");
                storyboard.Children.Add(anim);
            }

            var strokeAnim = new ColorAnimation() { Duration = ms300, FillBehavior = FillBehavior.HoldEnd, To = allZeros ? Colors.Transparent : graphStrokeColor, EnableDependentAnimation = true };
            Storyboard.SetTarget(strokeAnim, GraphView.Stroke);
            Storyboard.SetTargetProperty(strokeAnim, "Color");
            storyboard.Children.Add(strokeAnim);

            var fillTopAnim = new ColorAnimation() { Duration = ms300, FillBehavior = FillBehavior.HoldEnd, To = allZeros ? Colors.Black : graphFillFrom, EnableDependentAnimation = true };
            Storyboard.SetTarget(fillTopAnim, ((LinearGradientBrush)GraphView.Fill).GradientStops[0]);
            Storyboard.SetTargetProperty(fillTopAnim, "Color");
            storyboard.Children.Add(fillTopAnim);

            storyboard.Begin(() => GraphView.Data = path);
        }

        private void UpdateEntriesImage()
        {
            var uri = string.Format("ms-appx:/Assets/dial{0}.png", Model.Entries * 5);
            EntriesImage = new BitmapImage(new Uri(uri));
            EntriesDescription = string.Format("{0} min", Model.Entries * 5);
        }

        private void GoToState(DrashState state)
        {
            Model.State = state;

            View.Dispatcher.RunAsync(
                CoreDispatcherPriority.Normal,
                () => OnPropertyChanged(() => State));
        }

        public async void Zoomed(double velocity)
        {
            var factor = 1 + (int)Math.Floor(Math.Abs(velocity / 1000.0));
            var entries = Model.Entries + (velocity < 0 ? 3 : -3) * factor;
            entries = Math.Min(Math.Max(6, entries), 24);

            if (entries != Model.Entries) {
                Model.Entries = entries;
                Animatable.Mode = AnimatableMode.Disabled;
                UpdateVisuals();
                await Task.Delay(250);
                Animatable.Mode = AnimatableMode.Enabled;
            }
        }

        public async void RestartLocation()
        {
            await Task.Delay(500);
            if (geolocator == null)
                InitializeGeolocator();
        }
    }

    public class Entry
    {
        public Entry(int entries)
        {
            Description = string.Format("{0} min", entries * 5);
            NumEntries = entries;
        }

        public string Description { get; set; }
        public int NumEntries { get; set; }
    }
}