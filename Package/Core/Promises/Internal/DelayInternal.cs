#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using Proto.Timers;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Proto.Promises
{
    partial class Internal
    {
        partial class PromiseRefBase
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal sealed partial class DelayPromise : PromiseSingleAwait<VoidResult>
            {
                // Use ITimerSource and int directly instead of the Timer struct
                // so that the fields can be packed efficiently without extra padding.
                private ITimerSource _timerSource;
                private int _timerToken;
                // The timer callback can be invoked before the fields are actually assigned,
                // so we use an Interlocked counter to ensure it is disposed properly.
                private int _timerUseCounter;

                private void MaybeDisposeTimer()
                {
                    ThrowIfInPool(this);
                    if (InterlockedAddWithUnsignedOverflowCheck(ref _timerUseCounter, -1) == 0)
                    {
                        _timerSource.DisposeAsync(_timerToken).Forget();
                    }
                }

                internal override void MaybeDispose()
                {
                    Dispose();
                    _timerSource = null;
                    ObjectPool.MaybeRepool(this);
                }

                [MethodImpl(InlineOption)]
                private static DelayPromise GetFromPoolOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<DelayPromise>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new DelayPromise()
                        : obj.UnsafeAs<DelayPromise>();
                }

                [MethodImpl(InlineOption)]
                internal static DelayPromise GetOrCreate(TimeSpan delay, TimerFactory timerFactory)
                {
                    var promise = GetFromPoolOrCreate();
                    promise.Reset();
                    promise._timerUseCounter = 2;
                    using (SuppressExecutionContextFlow())
                    {
                        var timer = timerFactory.CreateTimer(obj => obj.UnsafeAs<DelayPromise>().OnTimerCallback(), promise, delay, Timeout.InfiniteTimeSpan);
                        promise._timerToken = timer._token;
                        promise._timerSource = timer._timerSource;
                    }
                    promise.MaybeDisposeTimer();
                    return promise;
                }

                private void OnTimerCallback()
                {
                    MaybeDisposeTimer();
                    HandleNextInternal(Promise.State.Resolved);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal sealed partial class DelayWithCancelationPromise : PromiseSingleAwait<VoidResult>, ICancelable
            {
                private CancelationRegistration _cancelationRegistration;
                private Timers.Timer _timer;
                // The timer and cancelation callbacks can be invoked before the fields are actually assigned,
                // so we use an Interlocked counter to ensure they are disposed properly.
                private int _fieldUseCounter;
                // The timer and cancelation callbacks can race on different threads,
                // so we use an int flag for Interlocked to determine which one was invoked first.
                private int _isCompletedFlag;

                private void MaybeDisposeFields()
                {
                    ThrowIfInPool(this);
                    if (InterlockedAddWithUnsignedOverflowCheck(ref _fieldUseCounter, -1) == 0)
                    {
                        _cancelationRegistration.Dispose();
                        _timer.DisposeAsync().Forget();
                    }
                }

                internal override void MaybeDispose()
                {
                    Dispose();
                    _cancelationRegistration = default;
                    _timer = default;
                    ObjectPool.MaybeRepool(this);
                }

                [MethodImpl(InlineOption)]
                private static DelayWithCancelationPromise GetFromPoolOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<DelayWithCancelationPromise>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new DelayWithCancelationPromise()
                        : obj.UnsafeAs<DelayWithCancelationPromise>();
                }

                [MethodImpl(InlineOption)]
                internal static DelayWithCancelationPromise GetOrCreate(TimeSpan delay, TimerFactory timerFactory, CancelationToken cancelationToken)
                {
                    var promise = GetFromPoolOrCreate();
                    promise.Reset();
                    promise._isCompletedFlag = 0;
                    promise._fieldUseCounter = 2;
                    using (SuppressExecutionContextFlow())
                    {
                        promise._timer = timerFactory.CreateTimer(obj => obj.UnsafeAs<DelayWithCancelationPromise>().OnTimerCallback(), promise, delay, Timeout.InfiniteTimeSpan);
                    }
                    cancelationToken.TryRegister<ICancelable>(promise, out promise._cancelationRegistration);
                    promise.MaybeDisposeFields();
                    return promise;
                }

                private void OnTimerCallback()
                {
                    ThrowIfInPool(this);
                    if (Interlocked.Exchange(ref _isCompletedFlag, 1) != 0)
                    {
                        return;
                    }

                    MaybeDisposeFields();
                    HandleNextInternal(Promise.State.Resolved);
                }

                void ICancelable.Cancel()
                {
                    ThrowIfInPool(this);
                    if (Interlocked.Exchange(ref _isCompletedFlag, 1) != 0)
                    {
                        return;
                    }

                    // The _timer field is assigned before the cancelation registration is hooked up, so we know it's valid here.
                    var timerDisposePromise = _timer.DisposeAsync();
                    if (timerDisposePromise._ref == null)
                    {
                        HandleNextInternal(Promise.State.Canceled);
                    }
                    else
                    {
                        timerDisposePromise._ref.HookupExistingWaiter(timerDisposePromise._id, this);
                    }
                }

                internal override void Handle(PromiseRefBase handler, Promise.State state)
                {
                    ThrowIfInPool(this);
                    handler.SetCompletionState(state);
                    handler.MaybeReportUnhandledAndDispose(state);
                    HandleNextInternal(Promise.State.Canceled);
                }
            }
        } // class PromiseRef
    } // class Internal
} // namespace Proto.Promises