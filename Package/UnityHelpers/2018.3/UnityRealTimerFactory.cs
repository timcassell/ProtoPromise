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
using static Proto.Promises.Internal;

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
        private sealed class UnityRealTimerFactoryTimer : UnityTimerBase
        {
            private UnityRealTimerFactoryTimer() { }

            [MethodImpl(InlineOption)]
            private static UnityRealTimerFactoryTimer GetOrCreate()
            {
                var obj = ObjectPool.TryTakeOrInvalid<UnityRealTimerFactoryTimer>();
                return obj == InvalidAwaitSentinel.s_instance
                    ? new UnityRealTimerFactoryTimer()
                    : obj.UnsafeAs<UnityRealTimerFactoryTimer>();
            }

            [MethodImpl(InlineOption)]
            internal static UnityRealTimerFactoryTimer GetOrCreate(TimerCallback callback, object state)
            {
                var timer = GetOrCreate();
                timer.Reset(callback, state);
                return timer;
            }

            internal override void MaybeDispose()
            {
                Dispose();
                ObjectPool.MaybeRepool(this);
            }

            protected override void ChangeImpl(TimeSpan dueTime, TimeSpan period, int token)
            {
                if (_isDisposed | Version != token)
                {
                    throw new ObjectDisposedException(nameof(UnityTimerBase));
                }

                if (dueTime == Timeout.InfiniteTimeSpan)
                {
                    _dueTime = _period = float.PositiveInfinity;
                    return;
                }

                _dueTime = UnityEngine.Time.realtimeSinceStartup + GetSeconds(dueTime, nameof(dueTime));
                _period = GetSeconds(period, nameof(period));
                if (_period <= 0)
                {
                    _period = float.PositiveInfinity;
                }

                if (_isTicking)
                {
                    return;
                }

                Retain();
                _isTicking = true;
                // TODO: Optimize this to just add the await instruction to the processor directly without using a Promise.
                // TODO: Optimize multiple timers by implementing an "EnsureTimerFiresBy" algorithm similar to how the BCL does it.
                new AwaitInstruction(this).ToPromise().Forget();
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
                    ReportRejection(e, this);
                }

                if (_period <= 0)
                {
                    _isTicking = false;
                    Release();
                    return true;
                }

                _dueTime += _period;
                return false;
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