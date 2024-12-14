#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using Proto.Promises;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

#pragma warning disable IDE0090 // Use 'new(...)'

namespace Proto.Timers
{
    partial class TimerFactory
    {
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        private sealed class TimeProviderTimerFactory : TimerFactory
        {
            // We have to use per-instance object pooling instead of global, because TimerFactory is abstract, and can have many different implementations.
            internal readonly Internal.LocalObjectPool<TimeProviderTimerFactoryTimer> _timerPool;

            internal TimeProviderTimerFactory(TimeProvider timeProvider)
            {
                _timeProvider = timeProvider;
                _timerPool = new Internal.LocalObjectPool<TimeProviderTimerFactoryTimer>(() => new TimeProviderTimerFactoryTimer(this));
            }

            public override Timer CreateTimer(TimerCallback callback, object state, TimeSpan dueTime, TimeSpan period)
            {
                if (callback == null)
                {
                    throw new Promises.ArgumentNullException(nameof(callback), "callback may not be null", Internal.GetFormattedStacktrace(1));
                }
                var timer = TimeProviderTimerFactoryTimer.GetOrCreate(this, callback, state, dueTime, period);
                return new Timer(timer, timer.Version);
            }

            public override TimeProvider ToTimeProvider()
                => _timeProvider;
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        private sealed class TimeProviderTimerFactoryTimer : Internal.HandleablePromiseBase, ITimerSource
        {
            private readonly TimeProviderTimerFactory _factory;
            // Timer doubles as the sync lock.
            private readonly ITimer _timer;
            private CallbackInvoker _callbackInvoker;
            // Start with 1 instead of 0 to reduce risk of false positives.
            private int _version = 1;

            internal int Version => _version;

            internal TimeProviderTimerFactoryTimer(TimeProviderTimerFactory factory)
            {
                _factory = factory;
                // We don't need the extra overhead of the timer capturing the execution context.
                // We capture it manually per usage of this instance.
                using (ExecutionContext.SuppressFlow())
                {
                    _timer = factory._timeProvider.CreateTimer(OnTimerCallback, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
                }
            }

            ~TimeProviderTimerFactoryTimer()
            {
                if (_callbackInvoker != null)
                {
                    Internal.Discard(_callbackInvoker); // Prevent the invoker's base finalizer from adding an extra exception.
                    Internal.ReportRejection(new UnreleasedObjectException($"A timer's resources were garbage collected without being disposed. {this}"), _callbackInvoker);
                }
            }

            [MethodImpl(Internal.InlineOption)]
            private static TimeProviderTimerFactoryTimer GetOrCreate(TimeProviderTimerFactory factory)
            {
                var obj = factory._timerPool.TryTakeOrInvalid();
                return obj == Internal.PromiseRefBase.InvalidAwaitSentinel.s_instance
                    ? new TimeProviderTimerFactoryTimer(factory)
                    : obj.UnsafeAs<TimeProviderTimerFactoryTimer>();
            }

            [MethodImpl(Internal.InlineOption)]
            internal static TimeProviderTimerFactoryTimer GetOrCreate(TimeProviderTimerFactory factory, TimerCallback callback, object state, TimeSpan dueTime, TimeSpan period)
            {
                var timer = GetOrCreate(factory);
                var callbackInvoker = CallbackInvoker.GetOrCreate(factory._timeProvider, callback, state);
                callbackInvoker.PrepareChange(dueTime, period);
                Volatile.Write(ref timer._callbackInvoker, callbackInvoker);
                timer._timer.Change(dueTime, period);
                return timer;
            }

            private void OnTimerCallback(object _)
            {
                CallbackInvoker callbackInvoker = null;
                try
                {
                    lock (_timer)
                    {
                        callbackInvoker = _callbackInvoker;
                        // If the timer callback was invoked on a background thread after this was disposed and returned to the pool,
                        // the invoker will be null, or TryPrepareInvoke() will return false.
                        if (callbackInvoker?.TryPrepareInvoke(_timer) != true)
                        {
                            return;
                        }
                    }

                    // Invoke outside of the lock!
                    callbackInvoker.Invoke();
                }
                catch (Exception e)
                {
                    Internal.ReportRejection(e, callbackInvoker);
                }
            }

            public void Change(TimeSpan dueTime, TimeSpan period, int token)
            {
                lock (_timer)
                {
                    var callbackInvoker = _callbackInvoker;
                    if (callbackInvoker is null | _version != token)
                    {
                        throw new ObjectDisposedException(nameof(TimeProviderTimerFactoryTimer));
                    }
                    callbackInvoker.PrepareChange(dueTime, period);
                }

                _timer.Change(dueTime, period);
            }

            public Promise DisposeAsync(int token)
            {
                CallbackInvoker callbackInvoker;
                lock (_timer)
                {
                    callbackInvoker = _callbackInvoker;
                    if (callbackInvoker is null | _version != token)
                    {
                        throw new ObjectDisposedException(nameof(TimeProviderTimerFactoryTimer));
                    }
                    _callbackInvoker = null;
                    unchecked { ++_version; }
                    callbackInvoker.PrepareChange(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
                }

                _timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);

                _factory._timerPool.MaybeRepool(this);
                return callbackInvoker.DisposeAsync();
            }

            // System timers run on background threads, which may cause the timer callback to be invoked unexpectedly.
            // In order to ensure the correct values are used to invoke user callbacks, and user callbacks are not invoked prematurely or after the timer is disposed,
            // we wrap the fields in a separate class so that we can read and update them atomically without invoking the callback while holding a lock.
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            // We inherit from PromiseSingleAwait<> to support DisposeAsync.
            private sealed class CallbackInvoker : Internal.PromiseRefBase.PromiseSingleAwait<Internal.VoidResult>
            {
                private TimeProvider _timeProvider;
                private TimerCallback _callback;
                private object _state;
                private long _changedTimestamp;
                private TimeSpan _dueTime;
                private TimeSpan _period;
                private int _retainCounter;

                [MethodImpl(Internal.InlineOption)]
                internal static CallbackInvoker GetOrCreate()
                {
                    var obj = Internal.ObjectPool.TryTakeOrInvalid<CallbackInvoker>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new CallbackInvoker()
                        : obj.UnsafeAs<CallbackInvoker>();
                }

                [MethodImpl(Internal.InlineOption)]
                internal static CallbackInvoker GetOrCreate(TimeProvider timeProvider, TimerCallback callback, object state)
                {
                    var callbackInvoker = GetOrCreate();
                    callbackInvoker.Reset();
                    // System timers always capture ExecutionContext (unless flow is suppressed).
                    // For performance reasons, we only capture if it's enabled in the config.
                    if (Promise.Config.AsyncFlowExecutionContextEnabled)
                    {
                        callbackInvoker.ContinuationContext = ExecutionContext.Capture();
                    }
                    callbackInvoker._timeProvider = timeProvider;
                    callbackInvoker._callback = callback;
                    callbackInvoker._state = state;
                    callbackInvoker._retainCounter = 1;
                    return callbackInvoker;
                }

                internal override void MaybeDispose()
                {
                    Dispose();
                    _callback = null;
                    _state = null;
                    Internal.ObjectPool.MaybeRepool(this);
                }

                [MethodImpl(Internal.InlineOption)]
                internal void PrepareChange(TimeSpan dueTime, TimeSpan period)
                {
                    _changedTimestamp = _timeProvider.GetTimestamp();
                    _dueTime = dueTime;
                    _period = period;
                }

                [MethodImpl(Internal.InlineOption)]
                internal bool TryPrepareInvoke(ITimer timer)
                {
                    // If the dueTime is infinite, this should not be invoked.
                    if (_dueTime == Timeout.InfiniteTimeSpan)
                    {
                        return false;
                    }

                    // If the dueTime has not elapsed, this should not be invoked.
                    // System timers may sometimes fire a bit early. https://github.com/dotnet/runtime/issues/87112
                    // Also, due to object pooling, the system timer callback could be invoked "early", or multiple times on background threads simultaneously.
                    // To protect against both cases, we simply Change() the underlying timer to the remaining time, and don't invoke now.
                    var elapsed = _timeProvider.GetElapsedTime(_changedTimestamp);
                    if (elapsed < _dueTime)
                    {
                        timer.Change(_dueTime - elapsed, _period);
                        return false;
                    }

                    // If the period is infinite, this should be invoked exactly once.
                    if (_period == Timeout.InfiniteTimeSpan)
                    {
                        _dueTime = Timeout.InfiniteTimeSpan;
                    }
                    else
                    {
                        _dueTime += _period;
                    }

                    Internal.InterlockedAddWithUnsignedOverflowCheck(ref _retainCounter, 1);
                    return true;
                }

                [MethodImpl(Internal.InlineOption)]
                internal void Invoke()
                {
                    var executionContext = ContinuationContext;
                    if (executionContext == null)
                    {
                        InvokeDirect();
                    }
                    else
                    {
                        ExecutionContext.Run(
                            // .Net Framework doesn't allow us to re-use a captured context, so we have to copy it for each invocation.
                            // .Net Core's implementation of CreateCopy returns itself, so this is always as efficient as it can be.
                            executionContext.UnsafeAs<ExecutionContext>().CreateCopy(),
                            obj => obj.UnsafeAs<CallbackInvoker>().InvokeDirect(),
                            this
                        );
                    }
                }

                [MethodImpl(Internal.InlineOption)]
                private void InvokeDirect()
                {
                    _callback.Invoke(_state);
                    Release();
                }

                [MethodImpl(Internal.InlineOption)]
                private void Release()
                {
                    if (Internal.InterlockedAddWithUnsignedOverflowCheck(ref _retainCounter, -1) == 0)
                    {
                        HandleNextInternal(Promise.State.Resolved);
                    }
                }

                [MethodImpl(Internal.InlineOption)]
                internal Promise DisposeAsync()
                {
                    Release();
                    return new Promise(this, Id);
                }
            } // class CallbackInvoker
        } // class TimeProviderTimerFactoryTimer
    } // class TimerFactory
} // namespace Proto.Timers