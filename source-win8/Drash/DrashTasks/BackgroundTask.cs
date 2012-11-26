using System;
using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;
using Drash.Common;
using Drash.Common.Api;
using Windows.ApplicationModel.Background;
using Windows.Devices.Geolocation;

namespace Drash.Tasks
{
    public sealed class BackgroundTask : IBackgroundTask
    {
        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            var deferral = taskInstance.GetDeferral();

            try {
                var geolocator = new Geolocator() { DesiredAccuracy = PositionAccuracy.High };

                // get location
                var location = await geolocator.GetGeopositionAsync();

                if (location == null)
                    return;

                Task locationTask = null;
                var locationName = string.Format("{0:0.000000}, {1:0.000000}", location.Coordinate.Latitude, location.Coordinate.Longitude);
                if (location.CivicAddress == null || string.IsNullOrEmpty(location.CivicAddress.City) ||
                    string.IsNullOrEmpty(location.CivicAddress.Country)) {
                    var glocator = new GoogleAddressResolver();
                    locationTask = glocator.ResolveAddressAsync(location.Coordinate).ContinueWith(t => {
                        var addr = t.Result;
                        if (addr != null && !addr.IsUnknown && !string.IsNullOrEmpty(addr.City) && !string.IsNullOrEmpty(addr.CountryRegion)) {
                            locationName = string.Format("{0}, {1}", addr.City, addr.CountryRegion);
                        }
                    });
                }
                else {
                    locationTask = Task.Delay(0);
                }

                RainData rain = null;
                var rainTask = Task.Run(async () => {
                    var uri = string.Format(CultureInfo.InvariantCulture,
                                            "http://gps.buienradar.nl/getrr.php?lat={0:0.000000}&lon={1:0.000000}&stamp={2}",
                                            location.Coordinate.Latitude, location.Coordinate.Longitude,
                                            DateTime.UtcNow.Ticks);
                    var wc = new HttpClient();
                    var result = await wc.GetAsync(uri);
                    if (result != null && result.IsSuccessStatusCode) {
                        rain = await RainData.TryParseAsync(await result.Content.ReadAsStringAsync());
                    }
                });

                Task.WaitAll(locationTask, rainTask);

                if (rain != null) {
                    DrashTile.Update(rain.ChanceForEntries(6), locationName);
                }
            }
            catch (Exception) {
                // nom nom nom
            }
            finally {
                deferral.Complete();
            }
        }
    }
}
