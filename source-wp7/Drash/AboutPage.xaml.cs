using System;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Tasks;
using GestureEventArgs = System.Windows.Input.GestureEventArgs;

namespace Drash
{
    public partial class AboutPage : PhoneApplicationPage
    {
        public AboutPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            AllowLocation.IsChecked = DrashSettings.LocationAllowed;
        }

        private void BuienRadar_Tap(object sender, GestureEventArgs e)
        {
            new WebBrowserTask { Uri = new Uri("http://gratisweerdata.buienradar.nl", UriKind.RelativeOrAbsolute) }.Show();
        }

        private void Website_Tap(object sender, GestureEventArgs gestureEventArgs)
        {
            new WebBrowserTask { Uri = new Uri("http://interfaceimplementation.be", UriKind.RelativeOrAbsolute) }.Show();
        }

        private void AllowLocation_Changed(object sender, System.Windows.RoutedEventArgs e)
        {
            DrashSettings.LocationAllowed = AllowLocation.IsChecked.GetValueOrDefault(false);
        }
    }
}