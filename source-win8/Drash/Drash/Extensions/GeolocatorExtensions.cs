using System;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;

namespace Drash.Extensions
{
    public static class GeolocatorExtensions
    {
        public static Task<Geoposition> GetDrashGeopositionAsync(this Geolocator locator)
        {
            return locator.GetGeopositionAsync(TimeSpan.FromMinutes(5), TimeSpan.FromSeconds(5)).AsTask();
        }
        
    }
}