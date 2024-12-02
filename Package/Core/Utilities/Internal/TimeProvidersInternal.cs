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
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        private sealed class DualTimeProvider : TimeProvider
        {
            private readonly TimeProvider _createTimerTimeProvider;
            private readonly TimeProvider _otherTimeProvider;

            internal DualTimeProvider(TimeProvider createTimerTimeProvider, TimeProvider otherTimeProvider)
            {
                _createTimerTimeProvider = createTimerTimeProvider;
                _otherTimeProvider = otherTimeProvider;
            }

            public override ITimer CreateTimer(TimerCallback callback, object state, TimeSpan dueTime, TimeSpan period)
                => _createTimerTimeProvider.CreateTimer(callback, state, dueTime, period);

            public override TimeZoneInfo LocalTimeZone
                => _otherTimeProvider.LocalTimeZone;

            public override long TimestampFrequency
                => _otherTimeProvider.TimestampFrequency;

            public override long GetTimestamp()
                => _otherTimeProvider.GetTimestamp();

            public override DateTimeOffset GetUtcNow()
                => _otherTimeProvider.GetUtcNow();
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        private sealed class PoolableTimerFactoryTimeProvider : TimeProvider
        {
            private readonly PoolableTimerFactory _poolableTimerFactory;
            private readonly TimeProvider _otherTimeProvider;

            internal PoolableTimerFactoryTimeProvider(PoolableTimerFactory poolableTimerFactory, TimeProvider otherTimeProvider)
            {
                _poolableTimerFactory = poolableTimerFactory;
                _otherTimeProvider = otherTimeProvider;
            }

            public override TimeZoneInfo LocalTimeZone
                => _otherTimeProvider.LocalTimeZone;

            public override long TimestampFrequency
                => _otherTimeProvider.TimestampFrequency;

            public override long GetTimestamp()
                => _otherTimeProvider.GetTimestamp();

            public override DateTimeOffset GetUtcNow()
                => _otherTimeProvider.GetUtcNow();

            public override ITimer CreateTimer(TimerCallback callback, object state, TimeSpan dueTime, TimeSpan period)
                => new PoolableTimerWrapper(_poolableTimerFactory.CreateTimer(callback, state, dueTime, period));

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private sealed class PoolableTimerWrapper : ITimer
            {
                private IPoolableTimer _poolableTimer;
                private TaskCompletionSource<bool> _completionSource;

                internal PoolableTimerWrapper(IPoolableTimer poolableTimer)
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
                    TaskCompletionSource<bool> completionSource;
                    lock (this)
                    {
                        timer = _poolableTimer;
                        completionSource = _completionSource;
                        if (completionSource != null)
                        {
                            return completionSource.Task;
                        }

                        _poolableTimer = null;
                        _completionSource = completionSource = new TaskCompletionSource<bool>();
                    }
                    timer.DisposeAsync()
                        .Then(completionSource, cs => cs.SetResult(true))
                        .Forget();
                    return completionSource.Task;
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