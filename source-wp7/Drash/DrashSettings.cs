using System.IO.IsolatedStorage;

namespace Drash
{
    internal class DrashSettings
    {
        private static bool asked;
        private static bool allowed;
        private static bool ensured = false;

        static void EnsureAllowed()
        {
            if (ensured) return;

            var userSettings = IsolatedStorageSettings.ApplicationSettings;
            allowed = false;
            asked = userSettings.TryGetValue("LocationAllowed", out allowed);
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
                var userSettings = IsolatedStorageSettings.ApplicationSettings;
                userSettings["LocationAllowed"] = value;
                userSettings.Save();
            }
        }
    }
}