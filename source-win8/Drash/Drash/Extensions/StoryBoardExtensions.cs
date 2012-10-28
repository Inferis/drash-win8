using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Animation;

namespace Drash
{
    internal static class StoryBoardExtensions
    {
        public static void Begin(this Storyboard storyboard, Action callback)
        {
            EventHandler<object> handler = null;
            handler = (s, e) => {
                if (callback != null) callback();
                storyboard.Completed -= handler;
            };
            storyboard.Completed += handler;

            storyboard.Begin();
        }

        public static void Begin(this Storyboard storyboard, int duration)
        {
            var d = new Duration(TimeSpan.FromMilliseconds(duration));
            foreach (var child in storyboard.Children) {
                child.Duration = d;
            }
            storyboard.Begin();
        }

        public static void Begin(this Storyboard storyboard, int duration, Action callback)
        {
            EventHandler<object> handler = null;
            handler = (s, e) => {
                if (callback != null) callback();
                storyboard.Completed -= handler;
            };
            storyboard.Completed += handler;

            var d = new Duration(TimeSpan.FromMilliseconds(duration));
            foreach (var child in storyboard.Children) {
                child.Duration = d;
            }
            storyboard.Begin();
        }
    }
}