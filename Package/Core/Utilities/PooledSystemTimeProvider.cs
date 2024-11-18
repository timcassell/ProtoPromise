#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Proto.Promises
{
    /// <summary>
    /// A <see cref="TimeProvider"/> that provides pooled timers based on system time.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public sealed class PooledSystemTimeProvider : TimeProvider
    {
        /// <summary>
        /// Gets the singleton instance of the <see cref="PooledSystemTimeProvider"/>.
        /// </summary>
        public static PooledSystemTimeProvider Instance { get; } = new PooledSystemTimeProvider();

        private PooledSystemTimeProvider() { }

        /// <summary>
        /// Gets a timer from the object pool, or creates a new timer, based on system time, which will be returned to the object pool when it is disposed.
        /// </summary>
        /// <inheritdoc/>
        public override ITimer CreateTimer(TimerCallback callback, object state, TimeSpan dueTime, TimeSpan period)
        {
            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback), "callback may not be null", Internal.GetFormattedStacktrace(1));
            }
            return PooledSystemTimeProviderTimer.GetOrCreate(callback, state, dueTime, period);
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        private sealed class PooledSystemTimeProviderTimer : Internal.HandleablePromiseBase, ITimer
        {
            // Timer doubles as the sync lock.
            private readonly ITimer _timer;
            private CallbackInvoker _callbackInvoker;

            private PooledSystemTimeProviderTimer()
            {
                // We don't need the extra overhead of the system timer capturing the execution context.
                // We capture it manually per usage of this instance.
                using (ExecutionContext.SuppressFlow())
                {
                    _timer = System.CreateTimer(OnTimerCallback, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
                }
            }

            [MethodImpl(Internal.InlineOption)]
            private static PooledSystemTimeProviderTimer GetOrCreate()
            {
                var obj = Internal.ObjectPool.TryTakeOrInvalid<PooledSystemTimeProviderTimer>();
                return obj == Internal.PromiseRefBase.InvalidAwaitSentinel.s_instance
                    ? new PooledSystemTimeProviderTimer()
                    : obj.UnsafeAs<PooledSystemTimeProviderTimer>();
            }

            [MethodImpl(Internal.InlineOption)]
            internal static PooledSystemTimeProviderTimer GetOrCreate(TimerCallback callback, object state, TimeSpan dueTime, TimeSpan period)
            {
                var timer = GetOrCreate();
                timer._callbackInvoker = CallbackInvoker.GetOrCreate(callback, state, dueTime, period);
                timer._timer.Change(dueTime, period);
                return timer;
            }

            private void OnTimerCallback(object _)
            {
                CallbackInvoker callbackInvoker;
                lock (_timer)
                {
                    callbackInvoker = _callbackInvoker;
                    // If the timer callback was invoked on a background thread after this was disposed and returned to the pool,
                    // the invoker will be null, or GetShouldInvoke() will return false.
                    if (callbackInvoker?.GetShouldInvoke() != true)
                    {
                        return;
                    }
                    callbackInvoker.PrepareInvoke();
                }

                // Invoke outside of the lock!
                callbackInvoker.Invoke();
            }

            public bool Change(TimeSpan dueTime, TimeSpan period)
            {
                lock (_timer)
                {
                    var callbackInvoker = _callbackInvoker;
                    if (callbackInvoker is null)
                    {
                        return false;
                    }
                    callbackInvoker.PrepareChange(dueTime, period);
                }

                return _timer.Change(dueTime, period);
            }

            private Promise DisposePromise()
            {
                CallbackInvoker callbackInvoker;
                lock (_timer)
                {
                    callbackInvoker = _callbackInvoker;
                    if (callbackInvoker is null)
                    {
                        throw new ObjectDisposedException(nameof(PooledSystemTimeProviderTimer));
                    }
                    _callbackInvoker = null;
                }

                _timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);

                Internal.ObjectPool.MaybeRepool(this);
                return callbackInvoker.DisposeAsync();
            }

            public void Dispose()
                => DisposePromise().Wait();

#if UNITY_2021_2_OR_NEWER || !UNITY_2018_3_OR_NEWER
            public ValueTask DisposeAsync()
                => DisposePromise();
#endif
            // System timers run on background threads, which may cause the timer callback to be invoked unexpectedly.
            // In order to ensure the correct values are used to invoke user callbacks, and user callbacks are not invoked prematurely or after the timer is disposed,
            // we wrap the fields in a separate class so that we can read and update them atomically without invoking the callback inside a lock.
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            // We inherit from PromiseSingleAwait<> to support DisposeAsync.
            private sealed class CallbackInvoker : Internal.PromiseRefBase.PromiseSingleAwait<Internal.VoidResult>
            {
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
                internal static CallbackInvoker GetOrCreate(TimerCallback callback, object state, TimeSpan dueTime, TimeSpan period)
                {
                    var callbackInvoker = GetOrCreate();
                    // System timers always capture ExecutionContext (unless flow is suppressed).
                    // For performance reasons, we only capture if it's enabled in the config.
                    if (Promise.Config.AsyncFlowExecutionContextEnabled)
                    {
                        callbackInvoker.ContinuationContext = ExecutionContext.Capture();
                    }
                    callbackInvoker._callback = callback;
                    callbackInvoker._state = state;
                    callbackInvoker._retainCounter = 1;
                    callbackInvoker.PrepareChange(dueTime, period);
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
                    _changedTimestamp = System.GetTimestamp();
                    _dueTime = dueTime;
                    _period = period;
                }

                [MethodImpl(Internal.InlineOption)]
                internal bool GetShouldInvoke()
                    // Ensure the dueTime has elapsed.
                    => _dueTime != Timeout.InfiniteTimeSpan && System.GetElapsedTime(_changedTimestamp) <= _dueTime;

                [MethodImpl(Internal.InlineOption)]
                internal void PrepareInvoke()
                {
                    Internal.InterlockedAddWithUnsignedOverflowCheck(ref _retainCounter, 1);
                    // The system timer callback could be invoked multiple times on background threads simultaneously,
                    // we ensure that it is only invoked once per period.
                    if (_period == Timeout.InfiniteTimeSpan)
                    {
                        _dueTime = Timeout.InfiniteTimeSpan;
                    }
                    else
                    {
                        _dueTime += _period;
                    }
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
                        ExecutionContext.Run(executionContext.UnsafeAs<ExecutionContext>(), obj => obj.UnsafeAs<CallbackInvoker>().InvokeDirect(), this);
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
        } // class PooledSystemTimer
    } // class PooledSystemTimeProvider
} // namespace Proto.Promises