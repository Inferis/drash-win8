using Windows.UI.Xaml;

namespace Drash.Common
{
    public enum SquaredSizeTracked
    {
        Width, Height
    }

    public class SquaredSize : DependencyObject
    {
        public static DependencyProperty TrackedProperty = DependencyProperty.RegisterAttached("Tracked", typeof(SquaredSizeTracked),
                                                                                        typeof(FrameworkElement),
                                                                                        new PropertyMetadata(SquaredSizeTracked.Width));

        public static void SetTracked(DependencyObject target, SquaredSizeTracked tracked)
        {
            target.SetValue(TrackedProperty, tracked);

            var fe = target as FrameworkElement;
            if (fe == null) return;

            fe.SizeChanged += OnSizeChanged;
        }

        public static SquaredSizeTracked GetTracked(DependencyObject target)
        {
            return (SquaredSizeTracked)target.GetValue(TrackedProperty);
        }

        static void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            var fe = (FrameworkElement)sender;
            if (GetTracked(fe) == SquaredSizeTracked.Width)
                fe.Height = e.NewSize.Width;
            else
                fe.Width = e.NewSize.Height;
        }

    }
}
