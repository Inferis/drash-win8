using System;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace Drash
{
    internal class DelayedAction : IDisposable
    {
        private readonly Dispatcher dispatcher;
        private ManualResetEvent waiter = new ManualResetEvent(false);

        public DelayedAction(Dispatcher dispatcher)
        {
            this.dispatcher = dispatcher;
        }

        public void Run(Action action, int delay)
        {
            Cancel();
            ThreadPool.QueueUserWorkItem(o => {
                var canceled = waiter.WaitOne(delay);
                if (!canceled)
                    dispatcher.BeginInvoke(action);
            });
        }

        public void Cancel()
        {
            waiter.Set();
            waiter.Reset();
        }

        public void Dispose()
        {
            Cancel();
        }
    }
}