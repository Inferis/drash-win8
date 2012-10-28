using System;

namespace Drash
{
    internal class DrashSettings
    {
        private static bool asked;
        private static bool allowed;

        static void EnsureAllowed()
        {
            var userSettings = Windows.Storage.ApplicationData.Current.RoamingSettings.Values;
            object allowedValue;
            asked = userSettings.TryGetValue("LocationAllowed", out allowedValue);
            allowed = allowedValue != null && Convert.ToBoolean(allowedValue);
        }

        public static bool LocationAllowedAsked
        {
            get
            {
                EnsureAllowed();
                return asked;
            }
        }

        public static bool LocationAllowed
        {
            get
            {
                EnsureAllowed();
                return allowed;
            }
            set
            {
                allowed = value;
                asked = true;
                var userSettings = Windows.Storage.ApplicationData.Current.RoamingSettings.Values;
                userSettings["LocationAllowed"] = value;
            }
        }
    }
}