#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using Proto.Promises.Threading;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Proto.Promises
{
    /// <summary>
    /// A <see cref="PoolableTimerFactory"/> that provides pooled timers based on <see cref="UnityEngine.Time.time"/>.
    /// </summary>
    /// <remarks>
    /// Timers created from this factory are safe to change and dispose on background threads, however calls to
    /// <see cref="IPoolableTimer.Change(TimeSpan, TimeSpan)"/> will be marshalled to the main thread, which may cause delays.
    /// </remarks>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public sealed class PoolableUnitySimulatedTimerFactory : PoolableTimerFactory
    {
        /// <summary>
        /// Gets the singleton instance of the <see cref="PoolableUnitySimulatedTimerFactory"/>.
        /// </summary>
        public static PoolableUnitySimulatedTimerFactory Instance { get; } = new PoolableUnitySimulatedTimerFactory();

        private PoolableUnitySimulatedTimerFactory() { }

        /// <summary>
        /// Gets a timer from the object pool, or creates a new timer, based on <see cref="UnityEngine.Time.time"/>, which will be returned to the object pool when it is disposed.
        /// </summary>
        /// <remarks>
        /// If this or <see cref="IPoolableTimer.Change(TimeSpan, TimeSpan)"/> are called from background threads, the calls will be marshalled to the main thread, which may cause delays.
        /// </remarks>
        /// <inheritdoc/>
        public override IPoolableTimer CreateTimer(TimerCallback callback, object state, TimeSpan dueTime, TimeSpan period)
        {
            var timer = PoolableUnitySimulatedTimerFactoryTimer.GetOrCreate(callback, state);
            timer.Change(dueTime, period);
            return timer;
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        // We inherit from PromiseSingleAwait<> to support DisposeAsync.
        private sealed class PoolableUnitySimulatedTimerFactoryTimer : Internal.PromiseRefBase.PromiseSingleAwait<Internal.VoidResult>, IPoolableTimer
        {
            private TimerCallback _callback;
            private object _state;
            private float _dueTime;
            private float _period;
            private int _retainCounter;
            private bool _isTicking;
            volatile private int _isDisposingFlag;

            private PoolableUnitySimulatedTimerFactoryTimer() { }

            [MethodImpl(Internal.InlineOption)]
            private static PoolableUnitySimulatedTimerFactoryTimer GetOrCreate()
            {
                var obj = Internal.ObjectPool.TryTakeOrInvalid<PoolableUnitySimulatedTimerFactoryTimer>();
                return obj == InvalidAwaitSentinel.s_instance
                    ? new PoolableUnitySimulatedTimerFactoryTimer()
                    : obj.UnsafeAs<PoolableUnitySimulatedTimerFactoryTimer>();
            }

            [MethodImpl(Internal.InlineOption)]
            internal static PoolableUnitySimulatedTimerFactoryTimer GetOrCreate(TimerCallback callback, object state)
            {
                var timer = GetOrCreate();
                timer.Reset();
                // System timers always capture ExecutionContext (unless flow is suppressed).
                // For performance reasons, we only capture if it's enabled in the config.
                if (Promise.Config.AsyncFlowExecutionContextEnabled)
                {
                    timer.ContinuationContext = ExecutionContext.Capture();
                }
                timer._callback = callback;
                timer._state = state;
                timer._retainCounter = 1;
                timer._isDisposingFlag = 0;
                return timer;
            }

            internal override void MaybeDispose()
            {
                Dispose();
                _callback = null;
                _state = null;
                Internal.ObjectPool.MaybeRepool(this);
            }

            [MethodImpl(Internal.InlineOption)]
            private void Retain()
                => Internal.InterlockedAddWithUnsignedOverflowCheck(ref _retainCounter, 1);

            [MethodImpl(Internal.InlineOption)]
            private void Release()
            {
                if (Internal.InterlockedAddWithUnsignedOverflowCheck(ref _retainCounter, -1) == 0)
                {
                    HandleNextInternal(Promise.State.Resolved);
                }
            }

            // This is called once per frame. Returns `true` to stop the ticking.
            private bool OnTick()
            {
                if (_isDisposingFlag == 1 || float.IsInfinity(_dueTime))
                {
                    _isTicking = false;
                    Release();
                    return true;
                }

                if (InternalHelper.PromiseBehaviour.s_time < _dueTime)
                {
                    return false;
                }

                try
                {
                    Invoke();
                }
                catch (Exception e)
                {
                    Internal.ReportRejection(e, this);
                }

                if (float.IsInfinity(_period))
                {
                    _isTicking = false;
                    Release();
                    return true;
                }

                _dueTime += _period;
                return false;
            }

            [MethodImpl(Internal.InlineOption)]
            private void Invoke()
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
                        obj => obj.UnsafeAs<PoolableUnitySimulatedTimerFactoryTimer>().InvokeDirect(),
                        this
                    );
                }
            }

            [MethodImpl(Internal.InlineOption)]
            private void InvokeDirect()
                => _callback.Invoke(_state);

            public void Change(TimeSpan dueTime, TimeSpan period)
            {
                if (InternalHelper.IsOnMainThread())
                {
                    ChangeImpl(dueTime, period);
                }
                else
                {
                    InternalHelper.PromiseBehaviour.Instance._syncContext.Send(
                        t => t.UnsafeAs<PoolableUnitySimulatedTimerFactoryTimer>().ChangeImpl(dueTime, period),
                        this
                    );
                }
            }

            private void ChangeImpl(TimeSpan dueTime, TimeSpan period)
            {
                Retain();
                if (_isDisposingFlag == 1)
                {
                    Release();
                    throw new ObjectDisposedException(nameof(PoolableUnitySimulatedTimerFactoryTimer));
                }

                if (dueTime == Timeout.InfiniteTimeSpan)
                {
                    _dueTime = _period = float.PositiveInfinity;
                    Release();
                    return;
                }

                // We use `UnityEngine.Time.time` instead of `InternalHelper.PromiseBehaviour.s_time`
                // because we don't know if this is called before or after the processor is executed this frame.
                _dueTime = (float) (UnityEngine.Time.time + dueTime.TotalSeconds);
                _period = period == Timeout.InfiniteTimeSpan ? float.PositiveInfinity : (float) period.TotalSeconds;
                if (_isTicking)
                {
                    Release();
                    return;
                }

                _isTicking = true;
                // TODO: Optimize this to just add the await instruction to the processor directly without using a Promise.
                new AwaitInstruction(this).ToPromise().Forget();
            }

            public Promise DisposeAsync()
            {
                if (Interlocked.Exchange(ref _isDisposingFlag, 1) == 1)
                {
                    throw new ObjectDisposedException(nameof(PoolableUnitySimulatedTimerFactoryTimer));
                }

                Release();
                return new Promise(this, Id);
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private readonly struct AwaitInstruction : IAwaitInstruction
            {
                private readonly PoolableUnitySimulatedTimerFactoryTimer _target;

                internal AwaitInstruction(PoolableUnitySimulatedTimerFactoryTimer target)
                {
                    _target = target;
                }

                public bool IsCompleted()
                    => _target.OnTick();
            }
        }
    }
}