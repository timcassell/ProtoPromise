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
using PromisesInternal = Proto.Promises.Internal;

namespace Proto.Timers
{
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    // We inherit from PromiseSingleAwait<> to support DisposeAsync.
    internal abstract class UnityTimerBase : PromisesInternal.PromiseRefBase.SingleAwaitPromise<PromisesInternal.VoidResult>, ITimerSource
    {
        private TimerCallback _callback;
        private object _state;
        protected float _dueTime;
        protected float _period;
        // Start with 1 instead of 0 to reduce risk of false positives.
        private int _version = 1;
        private byte _retainCounter;
        protected bool _isTicking;
        protected bool _isDisposed;

        internal int Version => _version;

        ~UnityTimerBase()
        {
            if (!_isDisposed)
            {
                WasAwaitedOrForgotten = true; // Stop base finalizer from adding an extra exception.
                PromisesInternal.ReportRejection(new UnreleasedObjectException($"A timer's resources were garbage collected without being disposed. {this}"), this);
            }
        }

        [MethodImpl(PromisesInternal.InlineOption)]
        protected void Reset(TimerCallback callback, object state)
        {
            Reset();
            // System timers always capture ExecutionContext (unless flow is suppressed).
            // For performance reasons, we only capture if it's enabled in the config.
            if (Promise.Config.AsyncFlowExecutionContextEnabled)
            {
                ContinuationContext = ExecutionContext.Capture();
            }
            _callback = callback;
            _state = state;
            _retainCounter = 1;
            _isDisposed = false;
        }

        new protected void Dispose()
        {
            base.Dispose();
            _callback = null;
            _state = null;
        }

        [MethodImpl(PromisesInternal.InlineOption)]
        protected void Retain()
        {
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
            checked
#endif
            {
                ++_retainCounter;
            }
        }

        [MethodImpl(PromisesInternal.InlineOption)]
        protected void Release()
        {
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
            checked
#endif
            {
                if (--_retainCounter == 0)
                {
                    HandleNextInternal(Promise.State.Resolved);
                }
            }
        }

        [MethodImpl(PromisesInternal.InlineOption)]
        protected void Invoke()
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
                    obj => obj.UnsafeAs<UnityTimerBase>().InvokeDirect(),
                    this
                );
            }
        }

        [MethodImpl(PromisesInternal.InlineOption)]
        private void InvokeDirect()
            => _callback.Invoke(_state);

        protected abstract void ChangeImpl(TimeSpan dueTime, TimeSpan period, int token);

        public void Change(TimeSpan dueTime, TimeSpan period, int token)
        {
            if (InternalHelper.IsOnMainThread())
            {
                ChangeImpl(dueTime, period, token);
                return;
            }

            Promise.Run((this, dueTime, period, token),
                cv => cv.Item1.ChangeImpl(cv.dueTime, cv.period, cv.token),
                InternalHelper.PromiseBehaviour.Instance._syncContext, forceAsync: true
            ).Wait();
        }

        protected static float GetSeconds(TimeSpan time, string paramName)
        {
            // Same as internal System.Threading.Timer.MaxSupportedTimeout.
            const uint MaxSupportedTimeout = 0xfffffffe;

            long tm = (long) time.TotalMilliseconds;
            if (tm < -1 || tm > MaxSupportedTimeout)
            {
                throw new Promises.ArgumentOutOfRangeException(paramName, PromisesInternal.GetFormattedStacktrace(3));
            }
            return (float) time.TotalSeconds;
        }

        public Promise DisposeAsync(int token)
        {
            if (InternalHelper.IsOnMainThread())
            {
                return DisposeAsyncImpl(token);
            }

            return Promise.Run((this, token),
                cv => cv.Item1.DisposeAsyncImpl(cv.token),
                InternalHelper.PromiseBehaviour.Instance._syncContext, forceAsync: true
            );
        }

        private Promise DisposeAsyncImpl(int token)
        {
            if (_isDisposed | _version != token)
            {
                throw new ObjectDisposedException(nameof(UnityTimerBase));
            }
            _isDisposed = true;
            unchecked
            {
                _version = token + 1;
            }

            Release();
            return new Promise(this, Id);
        }
    } // class UnityTimerFactoryBase
} // namespace Proto.Timers