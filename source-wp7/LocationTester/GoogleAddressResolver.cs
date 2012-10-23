using System;
using System.Collections.Generic;
using System.Device.Location;
using System.Linq;
using System.Net;
using System.Threading;
using System.Xml.Linq;

namespace Drash
{
    public class GoogleAddressResolver : ICivicAddressResolver
    {
#if DEBUG
        public event EventHandler<TraceEventArgs> Trace = delegate { };
#endif
        public CivicAddress ResolveAddress(GeoCoordinate coordinate)
        {
            OnTrace("ResolveAddress Sync wrapper");
            var signal = new ManualResetEvent(false);
            var address = new CivicAddress();
            Exception error = null;
            OnTrace("invoking ResolveAddressAsync");
            ResolveAddressAsync(coordinate, args => {
                OnTrace("processing");
                try {
                    if (args.Error != null) {
                        OnTrace("setting error");
                        error = args.Error;
                        return;
                    }

                    OnTrace("parsing");
                    address = ParseResult(args);
                }
                catch (Exception ex) {
                    OnTrace("exception caught, setting error");
                    error = ex;
                }
                finally {
                    OnTrace("setting done signal");
                    signal.Set();
                }
            });

            OnTrace("waiting for done signal");
            signal.WaitOne();
            OnTrace("done signal received");
            if (error != null) {
                OnTrace("got error");
                throw error;
            }

            OnTrace("returning address");
            return address;
        }

        private CivicAddress ParseResult(DownloadStringCompletedEventArgs args)
        {
            try {
                if (!string.IsNullOrEmpty(args.Result)) {
                    OnTrace("download result = " + args.Result);

                    var doc = XDocument.Parse(args.Result);
                    if (doc.Root == null) {
                        OnTrace("no root!");
                        return new CivicAddress();
                    }

                    var status = doc.Root.Descendants("status").Select(x => x.Value).FirstOrDefault();
                    if (status != "OK")
                    {
                        OnTrace("Invalid status OK = " + status);
                        return new CivicAddress();
                    }

                    var result = doc.Root.Descendants("result").WithTypeValue("street_address").FirstOrDefault();
                    if (result == null) {
                        OnTrace("no 'street_address' result. Trying 'route'.");
                        result = doc.Root.Descendants("result").WithTypeValue("route").FirstOrDefault();
                        if (result == null)
                        {
                            OnTrace("no 'street_address' or 'route' result. Failed!");
                            return new CivicAddress();
                        }
                    }

                    OnTrace("retrieving components");

                    var components = result.Elements("address_component").ToArray();
                    var addressLine1 = string.Format("{0} {1}",
                        components.WithTypeValue("route").LongNameValue(),
                        components.WithTypeValue("street_number").LongNameValue());
                    var city = string.Format("{0}", components.WithTypeValue("locality").LongNameValue());
                    var stateProvince = string.Format("{0}", components.WithTypeValue("administrative_area_level_1").LongNameValue());
                    var postalCode = string.Format("{0}", components.WithTypeValue("postal_code").LongNameValue());
                    var country = string.Format("{0}", components.WithTypeValue("country").LongNameValue());

                    OnTrace("returning result");
                    return new CivicAddress(addressLine1, "", "", city, country, "", postalCode, stateProvince);
                }
                else {
                    OnTrace("download result = empty");
                }
            }
            catch (Exception) {

                throw;
            }

            return new CivicAddress();
        }

        public void ResolveAddressAsync(GeoCoordinate coordinate)
        {
            ResolveAddressAsync(coordinate, args => {
                var error = args.Error;
                var address = new CivicAddress();
                try {
                    if (!args.Cancelled && args.Error == null) {
                        address = ParseResult(args);
                    }
                }
                catch (Exception ex) {
                    error = ex;
                }
                finally {
                    var handler = ResolveAddressCompleted;
                    if (handler != null) {
                        handler(this, new ResolveAddressCompletedEventArgs(address, error, args.Cancelled, null));
                    }
                }
            });
        }

        public void ResolveAddressAsync(GeoCoordinate coordinate, Action<DownloadStringCompletedEventArgs> handler)
        {
            if (coordinate == null)
                throw new ArgumentNullException("coordinate");

            var url = string.Format("http://maps.googleapis.com/maps/api/geocode/xml?latlng={0},{1}&sensor=true", coordinate.Latitude, coordinate.Longitude);
            OnTrace("ResolveAddressAsync with " + url);
            var wc = new WebClient();
            var timeout = new ManualResetEvent(false);
            wc.DownloadStringCompleted += (sender, args) => {
                OnTrace("Webrequest completed");
                timeout.Set();
                OnTrace("Invoking handler");
                handler(args);
            };
            OnTrace("DownloadStringAsync invoked");
            ThreadPool.QueueUserWorkItem(o => {
                OnTrace("Waiting for request");
                if (!timeout.WaitOne(30000)) {
                    OnTrace("Timeout expired, canceled");
                    wc.CancelAsync();
                }
                else
                    OnTrace("Request finished");
            });
            wc.DownloadStringAsync(new Uri(url));
        }

        private void OnTrace(string line)
        {
#if DEBUG
            Trace(this, new TraceEventArgs(line));
#endif
        }

        public event EventHandler<ResolveAddressCompletedEventArgs> ResolveAddressCompleted;
    }

#if DEBUG
    public class TraceEventArgs : EventArgs
    {
        public TraceEventArgs(string line)
        {
            Line = line;
        }
        public string Line { get; private set; }
    }
#endif

    internal static class GoogleAddressResolveXmlExtensions
    {
        public static IEnumerable<XElement> WithTypeValue(this IEnumerable<XElement> elements, string type)
        {
            return elements.Where(c => c.Elements("type").Any(x => x.Value == type));
        }

        public static string LongNameValue(this IEnumerable<XElement> elements)
        {
            return elements.Select(x => x.Element("long_name")).Where(x => x != null).Select(x => x.Value).FirstOrDefault();
        }
    }

}
