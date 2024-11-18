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
    /// A <see cref="TimeProvider"/> that provides pooled timers based on based on <see cref="UnityEngine.Time.realtimeSinceStartup"/>.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public sealed class PooledUnityRealTimeProvider : TimeProvider
    {
        /// <summary>
        /// Gets the singleton instance of the <see cref="PooledUnityRealTimeProvider"/>.
        /// </summary>
        public static PooledUnityRealTimeProvider Instance { get; } = new PooledUnityRealTimeProvider();

        private PooledUnityRealTimeProvider() { }

        /// <summary>
        /// Gets a timer from the object pool, or creates a new timer, based on <see cref="UnityEngine.Time.realtimeSinceStartup"/>, which will be returned to the object pool when it is disposed.
        /// </summary>
        /// <remarks>
        /// This method and the returned timer are only safe to use on the main thread.
        /// </remarks>
        /// <inheritdoc/>
        public override ITimer CreateTimer(TimerCallback callback, object state, TimeSpan dueTime, TimeSpan period)
            => PooledUnityRealTimeProviderTimer.GetOrCreate(callback, state, dueTime, period);

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        private sealed class PooledUnityRealTimeProviderTimer : Internal.HandleablePromiseBase, ITimer
        {
            private ExecutionContext _executionContext;
            private TimerCallback _callback;
            private object _state;
            private float _period;
            private float _dueTime;
            private int _retainCounter;
            private bool _isTicking;

            private PooledUnityRealTimeProviderTimer() { }

            [MethodImpl(Internal.InlineOption)]
            private static PooledUnityRealTimeProviderTimer GetOrCreate()
            {
                var obj = Internal.ObjectPool.TryTakeOrInvalid<PooledUnityRealTimeProviderTimer>();
                return obj == Internal.PromiseRefBase.InvalidAwaitSentinel.s_instance
                    ? new PooledUnityRealTimeProviderTimer()
                    : obj.UnsafeAs<PooledUnityRealTimeProviderTimer>();
            }

            [MethodImpl(Internal.InlineOption)]
            internal static PooledUnityRealTimeProviderTimer GetOrCreate(TimerCallback callback, object state, TimeSpan dueTime, TimeSpan period)
            {
                var timer = GetOrCreate();
                timer._callback = callback;
                timer._state = state;
                timer._retainCounter = 1;
                // System timers always capture ExecutionContext (unless flow is suppressed).
                // For performance reasons, we only capture if it's enabled in the config.
                if (Promise.Config.AsyncFlowExecutionContextEnabled)
                {
                    timer._executionContext = ExecutionContext.Capture();
                }
                timer.Change(dueTime, period);
                return timer;
            }

            // Returns `true` to stop the ticking.
            private bool OnTick()
            {
                if (_callback == null || float.IsNegativeInfinity(_dueTime))
                {
                    _isTicking = false;
                    MaybeDispose();
                    return true;
                }

                float realtimeSinceStartup = UnityEngine.Time.realtimeSinceStartup;
                if (realtimeSinceStartup < _dueTime)
                {
                    return false;
                }

                Invoke();

                if (float.IsNegativeInfinity(_period))
                {
                    _isTicking = false;
                    MaybeDispose();
                    return true;
                }

                _dueTime = realtimeSinceStartup + _period;
                return false;
            }

            [MethodImpl(Internal.InlineOption)]
            private void Invoke()
            {
                var executionContext = _executionContext;
                if (executionContext == null)
                {
                    InvokeDirect();
                }
                else
                {
                    ExecutionContext.Run(executionContext, obj => obj.UnsafeAs<PooledUnityRealTimeProviderTimer>().InvokeDirect(), this);
                }
            }

            [MethodImpl(Internal.InlineOption)]
            private void InvokeDirect()
                => _callback.Invoke(_state);

            public bool Change(TimeSpan dueTime, TimeSpan period)
            {
                if (_callback == null)
                {
                    return false;
                }
                _dueTime = dueTime < TimeSpan.Zero
                    ? float.NegativeInfinity
                    : (float) (UnityEngine.Time.realtimeSinceStartup + dueTime.TotalSeconds);
                _period = period < TimeSpan.Zero
                    ? float.NegativeInfinity
                    : (float) period.TotalSeconds;
                if (!_isTicking && dueTime != Timeout.InfiniteTimeSpan)
                {
                    ++_retainCounter;
                    _isTicking = true;
                    // TODO: Optimize this to just add the await instruction to the processor directly without using a Promise.
                    new AwaitInstruction(this).ToPromise().Forget();
                }
                return true;
            }

            public void Dispose()
            {
                if (_callback == null)
                {
                    throw new ObjectDisposedException(nameof(PooledUnityRealTimeProviderTimer));
                }

                _executionContext = null;
                _callback = null;
                _state = null;
                _dueTime = float.NegativeInfinity;
                _period = float.NegativeInfinity;

                MaybeDispose();
            }

            private void MaybeDispose()
            {
                if (--_retainCounter != 0)
                {
                    return;
                }

                Internal.ObjectPool.MaybeRepool(this);
            }

#if UNITY_2021_2_OR_NEWER || !UNITY_2018_3_OR_NEWER
            public ValueTask DisposeAsync()
            {
                Dispose();
                return default;
            }
#endif

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private readonly struct AwaitInstruction : IAwaitInstruction
            {
                private readonly PooledUnityRealTimeProviderTimer _target;

                internal AwaitInstruction(PooledUnityRealTimeProviderTimer target)
                {
                    _target = target;
                }

                public bool IsCompleted()
                    => _target.OnTick();
            }
        }
    }
}