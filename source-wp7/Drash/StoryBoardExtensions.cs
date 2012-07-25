using System;
using System.Windows;
using System.Windows.Media.Animation;

namespace Drash
{
    public static class StoryBoardExtensions
    {
        public static void Begin(this Storyboard storyboard, Action callback)
        {
            EventHandler handler = null;
            handler = (s, e) => {
                callback();
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
            EventHandler handler = null;
            handler = (s, e) => {
                callback();
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