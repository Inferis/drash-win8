using System.Device.Location;
using System.Diagnostics;
using System.Windows;
using Drash;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Tasks;

namespace LocationTester
{
    public partial class MainPage : PhoneApplicationPage
    {
        private GeoCoordinateWatcher watcher;
        private string fulllog;

        // Constructor
        public MainPage()
        {
            InitializeComponent();
            watcher = new GeoCoordinateWatcher(GeoPositionAccuracy.High);
            watcher.Start();
        }

        private void Test_Click(object sender, RoutedEventArgs e)
        {
            Log.Text = "";
            fulllog = "";
            Coordinate.Text = watcher.Position.Location.ToString();
            Result.Text = "<resolving>";

            var resolver = new GoogleAddressResolver();
            resolver.Trace += (o, args) => Dispatcher.BeginInvoke(() => DoLog(args.Line));
            resolver.ResolveAddressCompleted += (o, args) => {
                DoLog("Done.");

                if (args.Cancelled)
                    Result.Text = "<cancelled>";
                else if (args.Error != null) {
                    Result.Text = "<error>";
                    DoLog("");
                    DoLog(args.Error.ToString());
                }
                else if (args.Address == null)
                    Result.Text = "<no address>";
                else if (args.Address.IsUnknown)
                    Result.Text = "<unknown>";
                else {
                    Result.Text = args.Address.City + ", " + args.Address.CountryRegion;
                    DoLog(string.Format(
                        "al1: {0}\r\nal2: {1}\r\nbld: {2}\r\nflvl: {3}\r\nzip: {4}\r\n" +
                        "cty: {5}\r\nprov: {6}\r\ncrgn: {7}\r\n",
                        args.Address.AddressLine1,
                        args.Address.AddressLine2,
                        args.Address.Building,
                        args.Address.FloorLevel,
                        args.Address.PostalCode,
                        args.Address.City,
                        args.Address.StateProvince,
                        args.Address.CountryRegion));
                }

            };
            resolver.ResolveAddressAsync(watcher.Position.Location);
        }

        private void DoLog(string line)
        {
            fulllog += line + "\r\n";
            Log.Text += line + "\r\n";
        }

        private void Copy_Click(object sender, RoutedEventArgs e)
        {
            var log = string.Format("Location: {0}\r\nResult: {1}\r\nLog:\r\n{2}\r\n", Coordinate.Text, Result.Text, fulllog);
            Clipboard.SetText(log);
            Debug.WriteLine(log);

            new EmailComposeTask {
                Subject = "Drash Location Tester log",
                Body = log,
                To = "tom@interfaceimplementation.be",
            }.Show();
        }
    }
}