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
                    // In a rare race condition, the timer callback could be invoked before the fields are assigned.
                    // To avoid an invalid timer disposal, we decrement the counter after all fields are definitely assigned,
                    // and only dispose when the counter reaches 0.
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
                private Timers.Timer _timer;
                // The timer and cancelation callbacks can race on different threads,
                // and can be invoked before the fields are actually assigned;
                // we use CancelationHelper to make sure they are used and disposed properly.
                private CancelationHelper _cancelationHelper;

                private void MaybeDisposeFields()
                {
                    ThrowIfInPool(this);
                    if (_cancelationHelper.TryRelease())
                    {
                        _cancelationHelper.UnregisterAndWait();
                        _timer.DisposeAsync().Forget();
                    }
                }

                internal override void MaybeDispose()
                {
                    Dispose();
                    _cancelationHelper = default;
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
                    promise._cancelationHelper.Reset();
                    // IMPORTANT - must hookup callbacks after promise is fully setup.
                    using (SuppressExecutionContextFlow())
                    {
                        promise._timer = timerFactory.CreateTimer(obj => obj.UnsafeAs<DelayWithCancelationPromise>().OnTimerCallback(), promise, delay, Timeout.InfiniteTimeSpan);
                    }
                    // IMPORTANT - must register cancelation callback after everything else.
                    promise._cancelationHelper.Register(cancelationToken, promise);
                    // In a rare race condition, either callback could be invoked before the fields are assigned.
                    // To avoid invalid disposal, we release the cancelation helper after all fields are definitely assigned,
                    // and only dispose when it is completely released.
                    promise.MaybeDisposeFields();
                    return promise;
                }

                private void OnTimerCallback()
                {
                    ThrowIfInPool(this);
                    if (_cancelationHelper.TrySetCompleted())
                    {
                        MaybeDisposeFields();
                        HandleNextInternal(Promise.State.Resolved);
                    }
                }

                void ICancelable.Cancel()
                {
                    ThrowIfInPool(this);
                    if (!_cancelationHelper.TrySetCompleted())
                    {
                        return;
                    }

                    // We don't call MaybeDisposeFields in this path. There is no need,
                    // as disposing the cancelation registration is pointless because it's being invoked now,
                    // and we are explicitly disposing the timer here.

                    // The _timer field is assigned before the cancelation registration is hooked up, so we know it's valid here.
                    var timerDisposePromise = _timer.DisposeAsync();
                    if (timerDisposePromise._ref == null || timerDisposePromise._ref.State != Promise.State.Pending)
                    {
                        timerDisposePromise._ref?.MaybeMarkAwaitedAndDispose(timerDisposePromise._id);
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