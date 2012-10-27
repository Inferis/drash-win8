using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.Devices.Geolocation;

namespace Drash
{
    public class GoogleAddressResolver
    {
        private static GoogleCivicAddress ParseResult(string xml)
        {
            try {
                if (!string.IsNullOrEmpty(xml)) {
                    var doc = XDocument.Parse(xml);
                    if (doc.Root == null)
                        return new GoogleCivicAddress();

                    var status = doc.Root.Descendants("status").Select(x => x.Value).FirstOrDefault();
                    if (status != "OK")
                        return new GoogleCivicAddress();

                    var result = doc.Root.Descendants("result").WithTypeValue("street_address").FirstOrDefault();
                    if (result == null) {
                        result = doc.Root.Descendants("result").WithTypeValue("route").FirstOrDefault();
                        if (result == null)
                            return new GoogleCivicAddress();
                    }

                    var components = result.Elements("address_component").ToArray();
                    var addressLine1 = string.Format("{0} {1}",
                        components.WithTypeValue("route").LongNameValue(),
                        components.WithTypeValue("street_number").LongNameValue());
                    var city = string.Format("{0}", components.WithTypeValue("locality").LongNameValue());
                    var stateProvince = string.Format("{0}", components.WithTypeValue("administrative_area_level_1").LongNameValue());
                    var postalCode = string.Format("{0}", components.WithTypeValue("postal_code").LongNameValue());
                    var country = string.Format("{0}", components.WithTypeValue("country").LongNameValue());

                    return new GoogleCivicAddress(addressLine1, "", "", city, country, "", postalCode, stateProvince);
                }
            }
            catch (Exception) {
                throw;
            }

            return new GoogleCivicAddress();
        }

        public async Task<GoogleCivicAddress> ResolveAddressAsync(Geocoordinate coordinate)
        {
            if (coordinate == null)
                throw new ArgumentNullException("coordinate");

            var url = string.Format(CultureInfo.InvariantCulture, "http://maps.googleapis.com/maps/api/geocode/xml?latlng={0},{1}&sensor=true", coordinate.Latitude, coordinate.Longitude);
            var wc = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            var result = await wc.GetStringAsync(url);
            return ParseResult(result);
        }
    }

    public class GoogleCivicAddress
    {
        public GoogleCivicAddress()
        {
            IsUnknown = true;
        }

        public GoogleCivicAddress(string addressLine1, string addressLine2, string building, string city, string country, string floorLevel, string postalCode, string stateProvince)
        {
            AddressLine1 = addressLine1;
            AddressLine2 = addressLine2;
            Building = building;
            City = city;
            CountryRegion = country;
            FloorLevel = floorLevel;
            PostalCode = postalCode;
            StateProvince = stateProvince;
            IsUnknown = false;
        }

        public bool IsUnknown { get; private set; }
        public string AddressLine1 { get; private set; }
        public string AddressLine2 { get; private set; }
        public string Building { get; private set; }
        public string City { get; private set; }
        public string CountryRegion { get; private set; }
        public string FloorLevel { get; private set; }
        public string PostalCode { get; private set; }
        public string StateProvince { get; private set; }
    }

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
