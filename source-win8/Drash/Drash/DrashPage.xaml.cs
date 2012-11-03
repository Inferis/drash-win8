using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using Drash.Common;
using Drash.Models;
using Windows.Foundation;
using Windows.UI.Input;
using Windows.UI.Xaml.Media;
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
            var gesture = new GestureRecognizer() { GestureSettings = GestureSettings.Drag, ShowGestureFeedback = true };
            bool dragging = false;
            int dragDelta = 0;
            Point dragReference;
            gesture.ManipulationUpdated += (sender, args) =>
                                               {
                                                   Debug.WriteLine("mu " + args.Position);
                                               };
            gesture.Dragging += (recognizer, eventArgs) => {
                if (eventArgs.DraggingState == DraggingState.Started) {
                    dragging = true;
                    dragReference = eventArgs.Position;
                    dragDelta = 0;
                }
                else if (eventArgs.DraggingState == DraggingState.Continuing) {
                    if (dragging) {
                        var vertical = Math.Abs(eventArgs.Position.Y - dragReference.Y);
                        var horizontal = eventArgs.Position.X - dragReference.X;

                        Debug.WriteLine(eventArgs.Position + " v=" + vertical + " h=" + horizontal);
                        //if (dragDelta != 0 && Math.Sign(eventArgs.HorizontalChange) != Math.Sign(dragDelta))
                        //    dragDelta = 0;
                        //Zoomed(args.HorizontalChange);
                    }
                }
                else {
                    dragging = false;
                }
            };

            GraphView.PointerPressed += (sender, args) => { gesture.ProcessDownEvent(args.GetCurrentPoint(GraphView)); };
            GraphView.PointerMoved += (sender, args) => { gesture.ProcessMoveEvents(args.GetIntermediatePoints(GraphView)); };
            GraphView.PointerReleased += (sender, args) => { gesture.ProcessUpEvent(args.GetCurrentPoint(GraphView)); };
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

        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
