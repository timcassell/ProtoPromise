using Proto.Promises;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

#pragma warning disable 0420 // A reference to a volatile field will not be treated as volatile

namespace ProtoPromiseTests
{
    // Used instead of ThreadPool, because ThreadPool has issues in old runtime, causing tests to fail.
    // This also allows the test runner to wait for all background actions to complete.
    public sealed class BackgroundSynchronizationContext : SynchronizationContext
    {
        // Pool threads globally because creating new threads is expensive.
        private static Stack<ThreadRunner> s_pool = new Stack<ThreadRunner>();
        private static readonly HashSet<Thread> s_runningThreads = new HashSet<Thread>();

        volatile private int _runningActionCount;
        volatile private bool _neverCompleted;

        private sealed class ThreadRunner
        {
            private Stack<ThreadRunner> _pool;
            private BackgroundSynchronizationContext _owner;
            private readonly object _locker = new object();
            private SendOrPostCallback _callback;
            private object _state;

            public static void Run(BackgroundSynchronizationContext owner, SendOrPostCallback callback, object state)
            {
                if (owner._neverCompleted)
                {
                    throw new Exception("A previous thread never completed, not running action.");
                }
                Interlocked.Increment(ref owner._runningActionCount);
                bool reused = false;
                ThreadRunner threadRunner = null;
                var pool = s_pool;
                lock (pool)
                {
                    if (pool.Count > 0)
                    {
                        reused = true;
                        threadRunner = pool.Pop();
                    }
                }
                if (!reused)
                {
                    threadRunner = new ThreadRunner();
                }
                lock (threadRunner._locker)
                {
                    threadRunner._pool = pool;
                    threadRunner._owner = owner;
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
                    lock (s_runningThreads)
                    {
                        s_runningThreads.Add(Thread.CurrentThread);
                    }
                    BackgroundSynchronizationContext owner = _owner;
                    SendOrPostCallback callback = _callback;
                    object state = _state;
                    // Allow GC to reclaim memory.
                    _owner = null;
                    _callback = null;
                    _state = null;
                    SetSynchronizationContext(owner);
                    callback.Invoke(state);

                    lock (s_runningThreads)
                    {
                        s_runningThreads.Remove(Thread.CurrentThread);
                    }
                    Interlocked.Decrement(ref owner._runningActionCount);
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

        public void WaitForAllThreadsToComplete()
        {
            int runningActions = _runningActionCount;
            if (runningActions == 0)
            {
                return;
            }

            TimeSpan timeout = TimeSpan.FromSeconds(runningActions);
            if (!SpinWait.SpinUntil(() => _runningActionCount == 0, timeout))
            {
                s_pool = new Stack<ThreadRunner>();
                _runningActionCount = 0;
                _neverCompleted = true;

#if !NETCOREAPP
                List<Exception> exceptions = new List<Exception>();
                lock (s_runningThreads)
                {
                    foreach (var thread in s_runningThreads)
                    {
#pragma warning disable CS0618 // Type or member is obsolete
#pragma warning disable CS0612 // Type or member is obsolete
                        thread.Suspend();
                        var stackTrace = new StackTrace(thread, true);
                        exceptions.Add(new Proto.Promises.UnreleasedObjectException("Deadlocked thread", stackTrace.ToString()));
#pragma warning restore CS0612 // Type or member is obsolete
#pragma warning restore CS0618 // Type or member is obsolete
                    }
                    s_runningThreads.Clear();
                }
                if (exceptions.Count > 0)
                {
                    throw new Proto.Promises.AggregateException("WaitForAllThreadsToComplete timed out after " + timeout + ", _runningActionCount: " + _runningActionCount, exceptions);
                }
#endif
                throw new TimeoutException("WaitForAllThreadsToComplete timed out after " + timeout + ", _runningActionCount: " + _runningActionCount);
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
                throw new System.ArgumentNullException("d", "SendOrPostCallback may not be null.");
            }

            ThreadRunner.Run(this, d, state);
        }

        public override void Send(SendOrPostCallback d, object state)
        {
            throw new System.InvalidOperationException("BackgroundSynchronizationContext.Send is not supported.");
        }
    }
}