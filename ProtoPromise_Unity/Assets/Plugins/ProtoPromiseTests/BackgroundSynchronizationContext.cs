using System;
using System.Collections.Generic;
using System.Threading;

namespace ProtoPromiseTests
{
    // Used instead of ThreadPool, because ThreadPool has issues in old runtime, causing tests to fail.
    public sealed class BackgroundSynchronizationContext : SynchronizationContext
    {
        private sealed class ThreadRunner
        {
            private static readonly Stack<ThreadRunner> _pool = new Stack<ThreadRunner>();

            private readonly object _locker = new object();
            private SendOrPostCallback _callback;
            private object _state;

            public static void Run(SendOrPostCallback callback, object state)
            {
                bool reused = false;
                ThreadRunner threadRunner = null;
                lock (_pool)
                {
                    if (_pool.Count > 0)
                    {
                        reused = true;
                        threadRunner = _pool.Pop();
                    }
                }
                if (!reused)
                {
                    threadRunner = new ThreadRunner();
                }
                lock (threadRunner._locker)
                {
                    threadRunner._callback = callback;
                    threadRunner._state = state;
                    if (reused)
                    {
                        Monitor.Pulse(threadRunner._locker);
                    }
                    else
                    {
                        // Thread will never be garbage collected until the application terminates.
                        new Thread(threadRunner.ThreadAction) { IsBackground = true }.Start();
                    }
                }
            }

            private void ThreadAction()
            {
                while (true)
                {
                    SendOrPostCallback callback = _callback;
                    object state = _state;
                    // Allow GC to reclaim memory.
                    _callback = null;
                    _state = null;
                    callback.Invoke(state);
                    lock (_locker)
                    {
                        lock (_pool)
                        {
                            _pool.Push(this);
                        }
                        Monitor.Wait(_locker);
                    }
                }
            }
        }

        public override SynchronizationContext CreateCopy()
        {
            return this;
        }

        public override void Post(SendOrPostCallback d, object state)
        {
            if (d == null)
            {
                throw new ArgumentNullException("d", "SendOrPostCallback may not be null.");
            }

            ThreadRunner.Run(d, state);
        }

        public override void Send(SendOrPostCallback d, object state)
        {
            throw new InvalidOperationException("BackgroundSynchronizationContext.Send is not supported.");
        }
    }
}