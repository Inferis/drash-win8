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

        private void BuienRadar_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            new WebBrowserTask {Uri = new Uri("http://gratisweerdata.buienradar.nl", UriKind.RelativeOrAbsolute)}.Show();
        }

        private void Website_Tap(object sender, GestureEventArgs e)
        {
            new WebBrowserTask { Uri = new Uri("http://interfaceimplementation.be", UriKind.RelativeOrAbsolute) }.Show();
        }
    }
}