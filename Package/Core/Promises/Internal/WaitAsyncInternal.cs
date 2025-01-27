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
            internal sealed partial class WaitAsyncWithCancelationPromise<TResult> : PromiseSingleAwait<TResult>, ICancelable
            {
                private WaitAsyncWithCancelationPromise() { }

                [MethodImpl(InlineOption)]
                private static WaitAsyncWithCancelationPromise<TResult> GetOrCreateInstance()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<WaitAsyncWithCancelationPromise<TResult>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new WaitAsyncWithCancelationPromise<TResult>()
                        : obj.UnsafeAs<WaitAsyncWithCancelationPromise<TResult>>();
                }

                [MethodImpl(InlineOption)]
                internal static WaitAsyncWithCancelationPromise<TResult> GetOrCreate()
                {
                    var promise = GetOrCreateInstance();
                    promise.Reset();
                    promise._cancelationHelper.Reset();
                    return promise;
                }

                internal override void MaybeDispose()
                {
                    if (_cancelationHelper.TryRelease())
                    {
                        Dispose();
                        _cancelationHelper = default;
                        ObjectPool.MaybeRepool(this);
                    }
                }

                internal override void Handle(PromiseRefBase handler, Promise.State state)
                {
                    ThrowIfInPool(this);
                    handler.SetCompletionState(state);
                    if (_cancelationHelper.TrySetCompleted())
                    {
                        _cancelationHelper.UnregisterAndWait();
                        _cancelationHelper.ReleaseOne();
                        HandleSelf(handler, state);
                    }
                    else
                    {
                        MaybeDispose();
                        handler.MaybeReportUnhandledAndDispose(state);
                    }
                }

                void ICancelable.Cancel()
                {
                    ThrowIfInPool(this);
                    if (_cancelationHelper.TrySetCompleted())
                    {
                        HandleNextInternal(Promise.State.Canceled);
                    }
                }
            }

            // A non-generic class to hold the constants so that they won't need to be duplicated in the generic types in the runtime.
            // This also acts as a pseudo-enum (since old runtimes don't support Interlocked operations on enums).
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private static class WaitAsyncState
            {
                internal const int Initial = 0;
                internal const int Waiting = 1;
                internal const int Completed = 2;
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal sealed partial class WaitAsyncWithTimeoutPromise<TResult> : PromiseSingleAwait<TResult>
            {
                private WaitAsyncWithTimeoutPromise() { }

                [MethodImpl(InlineOption)]
                private static WaitAsyncWithTimeoutPromise<TResult> GetOrCreateInstance()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<WaitAsyncWithTimeoutPromise<TResult>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new WaitAsyncWithTimeoutPromise<TResult>()
                        : obj.UnsafeAs<WaitAsyncWithTimeoutPromise<TResult>>();
                }

                [MethodImpl(InlineOption)]
                internal static WaitAsyncWithTimeoutPromise<TResult> GetOrCreate(TimeSpan timeout, TimerFactory timerFactory)
                {
                    var promise = GetOrCreateInstance();
                    promise.Reset();
                    promise._retainCounter = 2;
                    promise._waitState = WaitAsyncState.Initial;
                    // IMPORTANT - must hookup callback after promise is fully setup.
                    using (SuppressExecutionContextFlow())
                    {
                        promise._timer = timerFactory.CreateTimer(obj => obj.UnsafeAs<WaitAsyncWithTimeoutPromise<TResult>>().OnTimerCallback(), promise, timeout, Timeout.InfiniteTimeSpan);
                        if (promise._timer == default)
                        {
                            Discard(promise);
                            throw new InvalidReturnException("timerFactory returned a default Timer.", GetFormattedStacktrace(3));
                        }
                    }
                    // In a rare race condition, the timer callback could be invoked before the field is assigned.
                    // To avoid an invalid timer disposal, we Interlocked.CompareExchange the wait state, and dispose if it was already invoked.
                    if (Interlocked.CompareExchange(ref promise._waitState, WaitAsyncState.Waiting, WaitAsyncState.Initial) != WaitAsyncState.Initial
                        && !promise.TryDisposeTimer(out var exception))
                    {
                        ReportRejection(exception, promise);
                    }
                    return promise;
                }

                internal override void MaybeDispose()
                {
                    if (InterlockedAddWithUnsignedOverflowCheck(ref _retainCounter, -1) == 0)
                    {
                        Dispose();
                        _timer = default;
                        ObjectPool.MaybeRepool(this);
                    }
                }

                internal override void Handle(PromiseRefBase handler, Promise.State state)
                {
                    ThrowIfInPool(this);
                    handler.SetCompletionState(state);
                    if (Interlocked.Exchange(ref _waitState, WaitAsyncState.Completed) != WaitAsyncState.Waiting)
                    {
                        // This already completed and this is being called from the timer's dispose promise,
                        // or this was timed out and this is being called from the awaited promise.
                        MaybeDispose();
                        handler.MaybeReportUnhandledAndDispose(state);
                        return;
                    }

                    // The _timer field is assigned before this is hooked up to the awaited promise, so we know it's valid here.
                    if (TryDisposeTimer(out var exception))
                    {
                        HandleSelf(handler, state);
                        return;
                    }
                 
                    RejectContainer = CreateRejectContainer(exception, int.MinValue, null, this);
                    handler.MaybeReportUnhandledAndDispose(state);
                    HandleNextInternal(Promise.State.Rejected);
                }

                private void OnTimerCallback()
                {
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                    // Due to object pooling, this is not a fool-proof check, but object pooling is disabled in DEBUG mode.
                    // It's good enough to protect against accidental non-compliant timer implementations.
                    if (_disposed)
                    {
                        throw new InvalidOperationException("Timer callback may not be invoked after its DisposeAsync Promise has completed.", GetFormattedStacktrace(1));
                    }
#endif
                    ThrowIfInPool(this);

                    int previousState = Interlocked.Exchange(ref _waitState, WaitAsyncState.Completed);
                    if (previousState == WaitAsyncState.Completed)
                    {
                        return;
                    }

                    // Handle will be called twice, once from the awaited promise, and again from the timer's dispose promise.
                    // We add another retain count here before attempting to dispose the timer and handling the next promise.
                    InterlockedAddWithUnsignedOverflowCheck(ref _retainCounter, 1);

                    if (previousState == WaitAsyncState.Waiting
                        && !TryDisposeTimer(out var exception))
                    {
                        RejectContainer = CreateRejectContainer(exception, int.MinValue, null, this);
                        HandleNextInternal(Promise.State.Rejected);
                        return;
                    }

                    RejectContainer = CreateRejectContainer(new TimeoutException(), int.MinValue, null, this);
                    HandleNextInternal(Promise.State.Rejected);
                }

                private bool TryDisposeTimer(out Exception exception)
                {
                    // Dispose the timer and hook it up to this. Handle will be called again (possibly recursively),
                    // and the second time it will enter the other branch to complete disposal.
                    Promise timerDisposePromise;
                    try
                    {
                        timerDisposePromise = _timer.DisposeAsync();
                    }
                    catch (Exception e)
                    {
                        exception = e;
                        return false;
                    }

                    try
                    {
                        if (timerDisposePromise._ref?.State != Promise.State.Pending)
                        {
                            timerDisposePromise._ref?.MaybeMarkAwaitedAndDispose(timerDisposePromise._id);
                            // Same as MaybeDispose, but without an extra branch, since we know this isn't done yet.
                            InterlockedAddWithUnsignedOverflowCheck(ref _retainCounter, -1);
                            exception = null;
                            return true;
                        }

                        timerDisposePromise._ref.HookupExistingWaiter(timerDisposePromise._id, this);
                        exception = null;
                        return true;
                    }
                    catch (InvalidOperationException)
                    {
                        exception = new InvalidReturnException("Timer.DisposeAsync() returned an invalid Promise.", string.Empty);
                        return false;
                    }
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal sealed partial class WaitAsyncWithTimeoutAndCancelationPromise<TResult> : PromiseSingleAwait<TResult>, ICancelable
            {
                private WaitAsyncWithTimeoutAndCancelationPromise() { }

                [MethodImpl(InlineOption)]
                private static WaitAsyncWithTimeoutAndCancelationPromise<TResult> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<WaitAsyncWithTimeoutAndCancelationPromise<TResult>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new WaitAsyncWithTimeoutAndCancelationPromise<TResult>()
                        : obj.UnsafeAs<WaitAsyncWithTimeoutAndCancelationPromise<TResult>>();
                }

                [MethodImpl(InlineOption)]
                internal static WaitAsyncWithTimeoutAndCancelationPromise<TResult> GetOrCreateAndHookup(
                    PromiseRefBase previous, short id, TimeSpan timeout, TimerFactory timerFactory, CancelationToken cancelationToken)
                {
                    var promise = GetOrCreate();
                    promise.Reset();
                    promise._retainCounter = 2;
                    promise._waitState = WaitAsyncState.Initial;
                    promise.SetPrevious(previous);
                    // IMPORTANT - must hookup callbacks after promise is fully setup.
                    using (SuppressExecutionContextFlow())
                    {
                        promise._timer = timerFactory.CreateTimer(obj => obj.UnsafeAs<WaitAsyncWithTimeoutAndCancelationPromise<TResult>>().OnTimerCallback(), promise, timeout, Timeout.InfiniteTimeSpan);
                        if (promise._timer == default)
                        {
                            Discard(promise);
                            throw new InvalidReturnException("timerFactory returned a default Timer.", GetFormattedStacktrace(3));
                        }
                    }
                    // IMPORTANT - must register cancelation callback after the timer.
                    promise._cancelationRegistration = cancelationToken.Register(promise);
                    // In a rare race condition, either callback could be invoked before the fields are assigned.
                    // To avoid invalid disposal, we Interlocked.CompareExchange the wait state, and dispose if it was already invoked.
                    if (Interlocked.CompareExchange(ref promise._waitState, WaitAsyncState.Waiting, WaitAsyncState.Initial) != WaitAsyncState.Initial)
                    {
                        promise._cancelationRegistration.Dispose();
                        if (!promise.TryDisposeTimer(out var exception))
                        {
                            ReportRejection(exception, promise);
                        }
                    }
                    // Finally, hook up to the awaited promise.
                    previous.HookupNewWaiter(id, promise);
                    return promise;
                }

                internal override void MaybeDispose()
                {
                    if (InterlockedAddWithUnsignedOverflowCheck(ref _retainCounter, -1) == 0)
                    {
                        Dispose();
                        _timer = default;
                        _cancelationRegistration = default;
                        ObjectPool.MaybeRepool(this);
                    }
                }

                internal override void Handle(PromiseRefBase handler, Promise.State state)
                {
                    ThrowIfInPool(this);
                    handler.SetCompletionState(state);
                    int previousState = Interlocked.Exchange(ref _waitState, WaitAsyncState.Completed);
                    if (previousState == WaitAsyncState.Completed)
                    {
                        // This already completed and this is being called from the timer's dispose promise,
                        // or this was timed out or canceled and this is being called from the awaited promise.
                        MaybeDispose();
                        handler.MaybeReportUnhandledAndDispose(state);
                        return;
                    }

                    if (previousState == WaitAsyncState.Waiting)
                    {
                        _cancelationRegistration.Dispose();
                    }

                    // The _timer field is assigned before this is hooked up to the awaited promise, so we know it's valid here.
                    if (TryDisposeTimer(out var exception))
                    {
                        HandleSelf(handler, state);
                        return;
                    }

                    RejectContainer = CreateRejectContainer(exception, int.MinValue, null, this);
                    handler.MaybeReportUnhandledAndDispose(state);
                    HandleNextInternal(Promise.State.Rejected);
                }

                private void OnTimerCallback()
                {
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                    // Due to object pooling, this is not a fool-proof check, but object pooling is disabled in DEBUG mode.
                    // It's good enough to protect against accidental non-compliant timer implementations.
                    if (_disposed)
                    {
                        throw new InvalidOperationException("Timer callback may not be invoked after its DisposeAsync Promise has completed.", GetFormattedStacktrace(1));
                    }
#endif
                    ThrowIfInPool(this);

                    int previousState = Interlocked.Exchange(ref _waitState, WaitAsyncState.Completed);
                    if (previousState == WaitAsyncState.Completed)
                    {
                        return;
                    }

                    // Handle will be called twice, once from the awaited promise, and again from the timer's dispose promise.
                    // We add another retain count here before attempting to dispose the timer and handling the next promise.
                    InterlockedAddWithUnsignedOverflowCheck(ref _retainCounter, 1);

                    if (previousState == WaitAsyncState.Waiting)
                    {
                        _cancelationRegistration.Dispose();
                        if (!TryDisposeTimer(out var exception))
                        {
                            RejectContainer = CreateRejectContainer(exception, int.MinValue, null, this);
                            HandleNextInternal(Promise.State.Rejected);
                            return;
                        }
                    }

                    RejectContainer = CreateRejectContainer(new TimeoutException(), int.MinValue, null, this);
                    HandleNextInternal(Promise.State.Rejected);
                }

                void ICancelable.Cancel()
                {
                    ThrowIfInPool(this);
                    int previousState = Interlocked.Exchange(ref _waitState, WaitAsyncState.Completed);
                    if (previousState == WaitAsyncState.Completed)
                    {
                        return;
                    }

                    // Handle will be called twice, once from the awaited promise, and again from the timer's dispose promise.
                    // We add another retain count here before attempting to dispose the timer and handling the next promise.
                    InterlockedAddWithUnsignedOverflowCheck(ref _retainCounter, 1);

                    // We don't dispose the cancelation registration here, as it's pointless because it's being invoked now.
                    if (previousState == WaitAsyncState.Waiting
                        && !TryDisposeTimer(out var exception))
                    {
                        RejectContainer = CreateRejectContainer(exception, int.MinValue, null, this);
                        HandleNextInternal(Promise.State.Rejected);
                        return;
                    }

                    HandleNextInternal(Promise.State.Canceled);
                }

                private bool TryDisposeTimer(out Exception exception)
                {
                    // Dispose the timer and hook it up to this. Handle will be called again (possibly recursively),
                    // and the second time it will enter the other branch to complete disposal.
                    Promise timerDisposePromise;
                    try
                    {
                        timerDisposePromise = _timer.DisposeAsync();
                    }
                    catch (Exception e)
                    {
                        exception = e;
                        return false;
                    }

                    try
                    {
                        if (timerDisposePromise._ref?.State != Promise.State.Pending)
                        {
                            timerDisposePromise._ref?.MaybeMarkAwaitedAndDispose(timerDisposePromise._id);
                            // Same as MaybeDispose, but without an extra branch, since we know this isn't done yet.
                            InterlockedAddWithUnsignedOverflowCheck(ref _retainCounter, -1);
                            exception = null;
                            return true;
                        }

                        timerDisposePromise._ref.HookupExistingWaiter(timerDisposePromise._id, this);
                        exception = null;
                        return true;
                    }
                    catch (InvalidOperationException)
                    {
                        exception = new InvalidReturnException("Timer.DisposeAsync() returned an invalid Promise.", string.Empty);
                        return false;
                    }
                }
            }
        } // class PromiseRefBase
    } // class Internal
} // namespace Proto.Promises