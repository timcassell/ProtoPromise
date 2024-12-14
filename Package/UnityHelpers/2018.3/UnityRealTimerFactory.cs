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

namespace Proto.Timers
{
    /// <summary>
    /// A <see cref="TimerFactory"/> that provides pooled timers based on <see cref="UnityEngine.Time.realtimeSinceStartup"/>.
    /// </summary>
    /// <remarks>
    /// Timers created from this factory are safe to change and dispose on background threads, however calls to
    /// <see cref="Timer.Change(TimeSpan, TimeSpan)"/> will be marshalled to the main thread, which may cause delays.
    /// </remarks>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public sealed class UnityRealTimerFactory : TimerFactory
    {
        /// <summary>
        /// Gets the singleton instance of the <see cref="UnityRealTimerFactory"/>.
        /// </summary>
        public static UnityRealTimerFactory Instance { get; } = new UnityRealTimerFactory();

        private UnityRealTimerFactory() { }

        /// <summary>
        /// Gets a timer from the object pool, or creates a new timer, based on <see cref="UnityEngine.Time.realtimeSinceStartup"/>, which will be returned to the object pool when it is disposed.
        /// </summary>
        /// <remarks>
        /// If this or <see cref="Timer.Change(TimeSpan, TimeSpan)"/> are called from background threads, the calls will be marshalled to the main thread, which may cause delays.
        /// </remarks>
        /// <inheritdoc/>
        public override Timer CreateTimer(TimerCallback callback, object state, TimeSpan dueTime, TimeSpan period)
        {
            var timer = UnityRealTimerFactoryTimer.GetOrCreate(callback, state);
            timer.Change(dueTime, period, timer.Version);
            return new Timer(timer, timer.Version);
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        // We inherit from PromiseSingleAwait<> to support DisposeAsync.
        private sealed class UnityRealTimerFactoryTimer : Internal.PromiseRefBase.PromiseSingleAwait<Internal.VoidResult>, ITimerSource
        {
            private TimerCallback _callback;
            private object _state;
            private float _dueTime;
            private float _period;
            // Start with 1 instead of 0 to reduce risk of false positives.
            private int _version = 1;
            private int _retainCounter;
            private bool _isTicking;
            volatile private bool _isDisposed;

            internal int Version => _version;

            private UnityRealTimerFactoryTimer() { }

            ~UnityRealTimerFactoryTimer()
            {
                if (!_isDisposed)
                {
                    WasAwaitedOrForgotten = true; // Stop base finalizer from adding an extra exception.
                    Internal.ReportRejection(new UnreleasedObjectException($"A timer's resources were garbage collected without being disposed. {this}"), this);
                }
            }

            [MethodImpl(Internal.InlineOption)]
            private static UnityRealTimerFactoryTimer GetOrCreate()
            {
                var obj = Internal.ObjectPool.TryTakeOrInvalid<UnityRealTimerFactoryTimer>();
                return obj == InvalidAwaitSentinel.s_instance
                    ? new UnityRealTimerFactoryTimer()
                    : obj.UnsafeAs<UnityRealTimerFactoryTimer>();
            }

            [MethodImpl(Internal.InlineOption)]
            internal static UnityRealTimerFactoryTimer GetOrCreate(TimerCallback callback, object state)
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
                timer._isDisposed = false;
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
                if (_isDisposed || float.IsInfinity(_dueTime))
                {
                    _isTicking = false;
                    Release();
                    return true;
                }

                if (UnityEngine.Time.realtimeSinceStartup < _dueTime)
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
                        obj => obj.UnsafeAs<UnityRealTimerFactoryTimer>().InvokeDirect(),
                        this
                    );
                }
            }

            [MethodImpl(Internal.InlineOption)]
            private void InvokeDirect()
                => _callback.Invoke(_state);

            public void Change(TimeSpan dueTime, TimeSpan period, int token)
            {
                Retain();
                if (Volatile.Read(ref _version) != token)
                {
                    Release();
                    throw new ObjectDisposedException(nameof(UnityRealTimerFactoryTimer));
                }

                if (InternalHelper.IsOnMainThread())
                {
                    ChangeImpl(dueTime, period);
                }
                else
                {
                    InternalHelper.PromiseBehaviour.Instance._syncContext.Send(
                        t => t.UnsafeAs<UnityRealTimerFactoryTimer>().ChangeImpl(dueTime, period),
                        this
                    );
                }
            }

            private void ChangeImpl(TimeSpan dueTime, TimeSpan period)
            {
                if (dueTime == Timeout.InfiniteTimeSpan)
                {
                    _dueTime = _period = float.PositiveInfinity;
                    Release();
                    return;
                }

                _dueTime = (float) (UnityEngine.Time.realtimeSinceStartup + dueTime.TotalSeconds);
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

            public Promise DisposeAsync(int token)
            {
                if (Interlocked.CompareExchange(ref _version, unchecked(token + 1), token) != token)
                {
                    throw new ObjectDisposedException(nameof(UnityRealTimerFactoryTimer));
                }
                _isDisposed = true;

                Release();
                return new Promise(this, Id);
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private readonly struct AwaitInstruction : IAwaitInstruction
            {
                private readonly UnityRealTimerFactoryTimer _target;

                internal AwaitInstruction(UnityRealTimerFactoryTimer target)
                {
                    _target = target;
                }

                public bool IsCompleted()
                    => _target.OnTick();
            }
        } // class UnityRealTimerFactoryTimer
    } // class UnityRealTimerFactory
} // namespace Proto.Timers