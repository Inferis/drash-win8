using System;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace Drash
{
    internal class DelayedAction : IDisposable
    {
        private readonly Dispatcher dispatcher;
        private readonly int standardDelay;
        private ManualResetEvent waiter = new ManualResetEvent(false);

        public DelayedAction(Dispatcher dispatcher)
            : this(dispatcher, 0)
        {
        }

        public DelayedAction(Dispatcher dispatcher, int standardDelay)
        {
            this.dispatcher = dispatcher;
            this.standardDelay = standardDelay;
        }

        public void Run(Action action)
        {
            Run(action, standardDelay);
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