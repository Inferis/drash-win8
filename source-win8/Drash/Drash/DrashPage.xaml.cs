using System;
using System.ComponentModel;
using System.Diagnostics;
using Drash.Common;
using Drash.Models;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.UI.Input;
using Windows.UI.Xaml.Navigation;

namespace Drash
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class DrashPage : LayoutAwarePage, INotifyPropertyChanged
    {
        private ViewModel model;

        public DrashPage()
        {
            this.InitializeComponent();

            Loaded += (sender, args) => {
                if (Model != null) {
                    Model.View = this;
                    Model.GraphView = Graph;
                    Model.GraphContainer = GraphView;
                }

                AddGestureRecognizer();
            };


        }

        private void AddGestureRecognizer()
        {
            var gesture = new GestureRecognizer() {
                AutoProcessInertia = true,
                GestureSettings = GestureSettings.Drag | GestureSettings.CrossSlide,
                CrossSlideHorizontally = true,
                CrossSlideThresholds = new CrossSlideThresholds { SpeedBumpEnd = 30 }
            };

            Point lastDragLocation;
            var lastDragTime = DateTime.Now;
            gesture.Dragging += (sender, args) => {
                switch (args.DraggingState) {
                    case DraggingState.Started:
                        lastDragLocation = args.Position;
                        lastDragTime = DateTime.Now;
                        break;

                    case DraggingState.Continuing: {
                            var delta = args.Position.X - lastDragLocation.X;
                            var time = DateTime.Now.Subtract(lastDragTime).TotalSeconds;
                            if (Math.Abs(delta) > 30 && time > 0) {
                                var velocity = delta / time;

                                lastDragLocation = args.Position;
                                lastDragTime = DateTime.Now;

                                Model.Zoomed(velocity);
                            }
                            break;
                        }
                }
            };
            gesture.CrossSliding += (sender, args) => {
                switch (args.CrossSlidingState) {
                    case CrossSlidingState.Started:
                        lastDragLocation = args.Position;
                        lastDragTime = DateTime.Now;
                        break;

                    case CrossSlidingState.Dragging: {
                            var delta = args.Position.X - lastDragLocation.X;
                            var time = DateTime.Now.Subtract(lastDragTime).TotalSeconds;
                            if (Math.Abs(delta) > 30 && time > 0) {
                                var velocity = delta / time;

                                lastDragLocation = args.Position;
                                lastDragTime = DateTime.Now;

                                Model.Zoomed(velocity);
                            }
                            break;
                        }
                }
            };

            GraphView.PointerPressed += (sender, args) => {
                GraphView.CapturePointer(args.Pointer);
                gesture.ProcessDownEvent(args.GetCurrentPoint(GraphView));
            };
            GraphView.PointerMoved += (sender, args) => {
                gesture.ProcessMoveEvents(args.GetIntermediatePoints(GraphView));
            };
            GraphView.PointerReleased += (sender, args) => {
                gesture.ProcessUpEvent(args.GetCurrentPoint(GraphView));
                GraphView.ReleasePointerCapture(args.Pointer);
            };
        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.  The Parameter
        /// property is typically used to configure the page.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
        }

        public ViewModel Model
        {
            get { return model; }
            set
            {
                model = value;
                OnPropertyChanged("Model");
            }
        }

        public bool TouchEnabled
        {
            get { return new TouchCapabilities().TouchPresent != 0; }
        }

        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
