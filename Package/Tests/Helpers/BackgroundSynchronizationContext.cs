﻿using Proto.Promises;
using System;
using System.Collections.Generic;
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

        volatile private int _runningActionCount;

        private sealed class ThreadRunner
        {
            private Stack<ThreadRunner> _pool;
            private BackgroundSynchronizationContext _owner;
            private readonly object _locker = new object();
            private SendOrPostCallback _callback;
            private object _state;

            public static void Run(BackgroundSynchronizationContext owner, SendOrPostCallback callback, object state)
            {
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
                    BackgroundSynchronizationContext owner = _owner;
                    SendOrPostCallback callback = _callback;
                    object state = _state;
                    // Allow GC to reclaim memory.
                    _owner = null;
                    _callback = null;
                    _state = null;
                    SetSynchronizationContext(owner);
                    callback.Invoke(state);
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