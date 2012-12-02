using System;
using Windows.System;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Drash
{
    public sealed partial class AboutFlyout : UserControl
    {
        public AboutFlyout()
        {
            this.InitializeComponent();
        }

        private void Buienradar_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            Launcher.LaunchUriAsync(new Uri("http://gratisweerdata.buienradar.nl"));
        }

        private void Website_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            Launcher.LaunchUriAsync(new Uri("http://dra.sh"));
        }
    }
}
