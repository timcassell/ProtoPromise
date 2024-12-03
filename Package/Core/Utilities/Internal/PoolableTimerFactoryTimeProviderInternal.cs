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

                internal PoolableTimerFactoryTimeProviderTimer(IPoolableTimer poolableTimer)
                {
                    _poolableTimer = poolableTimer;
                }

                public bool Change(TimeSpan dueTime, TimeSpan period)
                {
                    IPoolableTimer timer;
                    lock (this)
                    {
                        timer = _poolableTimer;
                        if (timer is null)
                        {
                            return false;
                        }
                        _poolableTimer = null;
                    }
                    timer.Change(dueTime, period);
                    return true;
                }

                private Task<bool> MaybeDisposeAsync()
                {
                    IPoolableTimer timer;
                    lock (this)
                    {
                        timer = _poolableTimer;
                        if (timer is null)
                        {
                            return _completionSource.Task;
                        }

                        _poolableTimer = null;
                    }
                    timer.DisposeAsync()
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