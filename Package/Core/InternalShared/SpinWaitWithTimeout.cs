using System;
using System.Threading;

namespace Proto.Promises
{
    internal static partial class Internal
    {
        internal struct SpinWaitWithTimeout
        {
            private SpinWait _spinWait;
            private ValueStopwatch _stopwatch;
            private TimeSpan _timeout;

            internal bool NextSpinWillYield
            {
                get { return _spinWait.NextSpinWillYield; }
            }

            internal SpinWaitWithTimeout(TimeSpan timeout)
            {
                _timeout = timeout;
                _spinWait = new SpinWait();
                _stopwatch = ValueStopwatch.StartNew();
            }

            internal void SpinOnce()
            {
                if (NextSpinWillYield && _stopwatch.GetElapsedTime() > _timeout)
                {
                    throw new TimeoutException("SpinWait exceeded timeout " + _timeout);
                }
                _spinWait.SpinOnce();
            }
        }
    }
}