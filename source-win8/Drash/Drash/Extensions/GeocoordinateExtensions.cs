using System;
using Windows.Devices.Geolocation;

namespace Drash
{
    static class GeocoordinateExtensions
    {
        public const double EarthRadiusInMiles = 3956.0;
        public const double EarthRadiusInKilometers = 6367.0;
        public const double EarthRadiusInMeters = EarthRadiusInKilometers * 1000;

        public static double GetDistanceTo(this Geocoordinate a, Geocoordinate b)
        {
            return EarthRadiusInMeters * 2 * Math.Asin(Math.Min(1, Math.Sqrt((Math.Pow(Math.Sin((DiffRadian(a.Latitude, b.Latitude)) / 2.0), 2.0)
                + Math.Cos(ToRadian(a.Latitude)) * Math.Cos(ToRadian(b.Latitude)) * Math.Pow(Math.Sin((DiffRadian(a.Longitude, b.Longitude)) / 2.0), 2.0)))));
        }

        public static double ToRadian(double val) { return val * (Math.PI / 180); }
        public static double ToDegree(double val) { return val * 180 / Math.PI; }
        public static double DiffRadian(double val1, double val2) { return ToRadian(val2) - ToRadian(val1); }
    }
}
