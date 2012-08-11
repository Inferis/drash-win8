using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Drash
{
    internal static class FadeExtensions
    {
        public static void FadeIn(this FrameworkElement element, Action callback = null, int duration = 300)
        {
            var storyboard = new Storyboard();
            var durationSpan = TimeSpan.FromMilliseconds(duration);

            element.RenderTransformOrigin = new Point(0.5, 0.5);
            var transform = element.RenderTransform as CompositeTransform;
            if (transform == null)
                element.RenderTransform = transform = new CompositeTransform();
            transform.ScaleX = 0.9;
            transform.ScaleY = 0.9;
            element.Opacity = 0;

            // opacity
            var anim = new DoubleAnimation() { Duration = durationSpan, To = 1 };
            Storyboard.SetTarget(anim, element);
            Storyboard.SetTargetProperty(anim, new PropertyPath(UIElement.OpacityProperty));
            storyboard.Children.Add(anim);

            // scalex
            anim = new DoubleAnimation() { Duration = durationSpan, To = 1 };
            Storyboard.SetTarget(anim, element);
            Storyboard.SetTargetProperty(anim, new PropertyPath("(UIElement.RenderTransform).(CompositeTransform.ScaleX)"));
            storyboard.Children.Add(anim);

            // scaley
            anim = new DoubleAnimation() { Duration = durationSpan, To = 1 };
            Storyboard.SetTarget(anim, element);
            Storyboard.SetTargetProperty(anim, new PropertyPath("(UIElement.RenderTransform).(CompositeTransform.ScaleY)"));
            storyboard.Children.Add(anim);

            storyboard.Begin(callback);
        }

        public static void FadeOut(this FrameworkElement element, Action callback = null, int duration = 300)
        {
            var storyboard = new Storyboard();
            var ms300 = TimeSpan.FromMilliseconds(duration);

            element.RenderTransformOrigin = new Point(0.5, 0.5);
            var transform = element.RenderTransform as CompositeTransform;
            if (transform == null)
                element.RenderTransform = transform = new CompositeTransform();
            transform.ScaleX = 1;
            transform.ScaleY = 1;
            element.Opacity = 1;

            // opacity
            var anim = new DoubleAnimation() { Duration = ms300, To = 0 };
            Storyboard.SetTarget(anim, element);
            Storyboard.SetTargetProperty(anim, new PropertyPath(UIElement.OpacityProperty));
            storyboard.Children.Add(anim);

            // scalex
            anim = new DoubleAnimation() { Duration = ms300, To = 0.9 };
            Storyboard.SetTarget(anim, element);
            Storyboard.SetTargetProperty(anim, new PropertyPath("(UIElement.RenderTransform).(CompositeTransform.ScaleX)"));
            storyboard.Children.Add(anim);

            // scaley
            anim = new DoubleAnimation() { Duration = ms300, To = 0.9 };
            Storyboard.SetTarget(anim, element);
            Storyboard.SetTargetProperty(anim, new PropertyPath("(UIElement.RenderTransform).(CompositeTransform.ScaleY)"));
            storyboard.Children.Add(anim);

            storyboard.Begin(callback);
        }

        public static void FadeOutThenIn(this FrameworkElement element, Action between = null, Action callback = null, int duration = 300)
        {
            element.FadeOut(callback: () => {
                                                if (between != null) between();
                                                element.FadeIn(callback, duration/2);
            }, duration:duration/2);
        }
    }
}