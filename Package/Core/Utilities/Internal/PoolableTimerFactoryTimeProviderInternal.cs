#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using Proto.Promises.Threading;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable IDE0090 // Use 'new(...)'
#pragma warning disable IDE0290 // Use primary constructor

namespace Proto.Promises
{
    partial class PoolableTimerFactory
    {
        // TimeProvider that implements ITimers by wrapping the IPoolableTimers returned from PoolableTimerFactory.CreateTimer.
        // Other methods use the default implementation (same as TimeProvider.System).
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        private sealed class PoolableTimerFactoryTimeProvider : TimeProvider
        {
            internal readonly PoolableTimerFactory _poolableTimerFactory;

            internal PoolableTimerFactoryTimeProvider(PoolableTimerFactory poolableTimerFactory)
            {
                _poolableTimerFactory = poolableTimerFactory;
            }

            public override ITimer CreateTimer(TimerCallback callback, object state, TimeSpan dueTime, TimeSpan period)
                => new PoolableTimerFactoryTimeProviderTimer(_poolableTimerFactory.CreateTimer(callback, state, dueTime, period));

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private sealed class PoolableTimerFactoryTimeProviderTimer : ITimer
            {
                private IPoolableTimer _poolableTimer;
                private readonly TaskCompletionSource<bool> _completionSource = new TaskCompletionSource<bool>();
                // ReaderWriterLock used to ensure that Change will not be called after DisposeAsync.
                private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();

                internal PoolableTimerFactoryTimeProviderTimer(IPoolableTimer poolableTimer)
                {
                    _poolableTimer = poolableTimer;
                }

                public bool Change(TimeSpan dueTime, TimeSpan period)
                {
                    _lock.EnterReadLock();
                    var timer = _poolableTimer;
                    if (timer is null)
                    {
                        _lock.ExitReadLock();
                        return false;
                    }
                    timer.Change(dueTime, period);
                    _lock.ExitReadLock();
                    return true;
                }

                private Task<bool> MaybeDisposeAsync()
                {
                    // Check if this was already disposed before entering the write lock.
                    if (_poolableTimer is null)
                    {
                        return _completionSource.Task;
                    }

                    _lock.EnterWriteLock();
                    // Cache the timer to dispose if it was not already disposed.
                    var timer = _poolableTimer;
                    _poolableTimer = null;
                    _lock.ExitWriteLock();

                    timer?.DisposeAsync()
                        .Then(_completionSource, cs => cs.SetResult(true))
                        .Forget();
                    return _completionSource.Task;
                }

                public void Dispose()
                    => _ = MaybeDisposeAsync();

#if UNITY_2021_2_OR_NEWER || !UNITY_2018_3_OR_NEWER
                public ValueTask DisposeAsync()
                    => new ValueTask(MaybeDisposeAsync());
#endif
            }
        }
    }
}