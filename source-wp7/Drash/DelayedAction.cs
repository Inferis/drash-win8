using System;
using System.Threading;

namespace Drash
{
    internal class DelayedAction : IDisposable
    {
        private ManualResetEvent waiter = new ManualResetEvent(false);

        public void Run(Action action, int delay)
        {
            Cancel();
            var canceled = waiter.WaitOne(delay);
            if (!canceled)
                action();
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