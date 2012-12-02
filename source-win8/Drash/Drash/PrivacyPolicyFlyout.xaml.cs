using System;
using Windows.System;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace Drash
{
    public sealed partial class PrivacyPolicyFlyout : UserControl
    {
        public PrivacyPolicyFlyout()
        {
            this.InitializeComponent();
        }

        private void Buienradar_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            Launcher.LaunchUriAsync(new Uri("http://gratisweerdata.buienradar.nl"));
        }
    }
}
