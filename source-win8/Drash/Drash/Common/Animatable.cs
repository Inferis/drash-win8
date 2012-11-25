using System;
using System.Diagnostics;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace Drash.Common
{
    enum AnimatableMode
    {
        Enabled,
        Disabled,
        Forced
    }
    
    class Animatable : DependencyObject
    {
        public static AnimatableMode Mode { get; set; }
        static Animatable()
        {
            Mode = AnimatableMode.Enabled;
        }

        #region Source

        public static DependencyProperty
            SourceProperty = DependencyProperty.RegisterAttached(
            "Source", typeof(ImageSource), typeof(DependencyObject), new PropertyMetadata(null, OnSourceChanged));

        private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var target = d as Image;
            if (target != null) {
                var oldSource = e.OldValue as BitmapImage != null ? ((BitmapImage)e.OldValue).UriSource : null;
                var newSource = e.NewValue as BitmapImage != null ? ((BitmapImage)e.NewValue).UriSource : null;
                Animate(oldSource, newSource, target, t => t.SetValue(Image.SourceProperty, e.NewValue));
            }

        }

        public static void SetSource(DependencyObject target, string source)
        {
        }

        public static string GetSource(DependencyObject target)
        {
            return (string)target.GetValue(Image.SourceProperty);
        }
        #endregion

        #region Text
        public static DependencyProperty
            TextProperty = DependencyProperty.RegisterAttached(
            "Text", typeof(string), typeof(DependencyObject), new PropertyMetadata(null, OnTextChanged));

        private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var target = d as TextBlock;
            if (target != null) {
                Animate(e.OldValue, e.NewValue, target, t => t.SetValue(TextBlock.TextProperty, e.NewValue));
            }

        }

        private static void Animate(object oldValue, object newValue, FrameworkElement target, Action<DependencyObject> action)
        {
            if (Equals(oldValue, newValue) && Mode != AnimatableMode.Forced)
                return;

            if (Mode != AnimatableMode.Disabled)
                target.FadeOutThenIn(() => action(target));
            else 
                action(target);
        }

        public static void SetText(DependencyObject target, string text)
        {
        }

        public static string GetText(DependencyObject target)
        {
            return (string)target.GetValue(TextBlock.TextProperty);
        }
        #endregion
    }
}
