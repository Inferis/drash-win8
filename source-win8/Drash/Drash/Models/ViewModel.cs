using System.Net.NetworkInformation;
using System.Windows.Input;
using Drash.Common;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace Drash.Models
{
    public class ViewModel
    {
        private LayoutAwarePage view;

        public LayoutAwarePage View
        {
            private get { return view; }
            set
            {
                view = value;
                if (view != null) ViewLoaded();
            }
        }

        public string Location { get; set; }
        public string Chance { get; set; }
        public string Precipitation { get; set; }
        public ImageSource IntensityImage { get; set; }
        public ICommand RefreshCommand { get; set; }

        private Model Model { get; set; }

        public ViewModel(Model model)
        {
            Model = model;
        }

        public void ViewLoaded()
        {
            UpdateState();
            NetworkChange.NetworkAddressChanged += (sender, args) => UpdateState();
        }

        private void UpdateState()
        {
            try {
                if (!NetworkInterface.GetIsNetworkAvailable()) {
                    GoToState(DrashState.NoNetwork);
                    return;
                }

                if (Model.Location == null) {
                    GoToState(DrashState.NoLocation);
                    return;
                }
            }
            finally {
                UpdateVisuals();
            }
        }

        private void UpdateVisuals()
        {
        }

        private void GoToState(DrashState state)
        {
            Model.State = state;

            View.Dispatcher.RunAsync(
                CoreDispatcherPriority.Normal,
                () => {
                    var stateName = state == DrashState.Good
                                        ? View.DetermineVisualState(ApplicationView.Value)
                                        : state.ToString();
                    VisualStateManager.GoToState(View, stateName, true);
                });
        }
    }
}