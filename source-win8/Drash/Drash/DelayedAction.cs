using System;
using System.Threading;
using Windows.System.Threading;
using Windows.UI.Core;

namespace Drash
{
    internal class DelayedAction : IDisposable
    {
        private readonly CoreDispatcher dispatcher;
        private readonly int standardDelay;
        private readonly ManualResetEvent waiter = new ManualResetEvent(false);

        public DelayedAction(CoreDispatcher dispatcher)
            : this(dispatcher, 0)
        {
        }

        public DelayedAction(CoreDispatcher dispatcher, int standardDelay)
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
            ThreadPool.RunAsync(o => {
                var canceled = waiter.WaitOne(delay);
                if (!canceled)
                    dispatcher.RunIdleAsync(e => action());
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