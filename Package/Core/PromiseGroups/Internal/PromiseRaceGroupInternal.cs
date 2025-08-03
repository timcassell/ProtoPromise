#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

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
            internal abstract partial class RacePromiseGroupBase<TResult> : PromiseGroupBase<TResult>
            {
                protected void Complete(Promise.State state)
                {
                    ThrowIfInPool(this);

                    // Race promise group ignores rejections and cancelations if any promise was resolved,
                    // unless the source cancelation token was canceled, or a cancelation token or cleanup callback threw.
                    if (state != Promise.State.Resolved & RejectContainer != null)
                    {
                        state = Promise.State.Rejected;
                    }
                    HandleNextInternal(state);
                }

                [MethodImpl(InlineOption)]
                internal void SetResolved()
                {
                    ThrowIfInPool(this);

                    // We don't need to branch for VoidResult.
                    _isResolved = 1;
                    CancelGroup();
                }

                [MethodImpl(InlineOption)]
                internal void SetResolved(in TResult result)
                {
                    ThrowIfInPool(this);

                    _isResolved = 1;
                    _result = result;
                    CancelGroup();
                }

                [MethodImpl(InlineOption)]
                internal bool TrySetResolved(in TResult result)
                {
                    ThrowIfInPool(this);

                    if (Interlocked.Exchange(ref _isResolved, 1) == 0)
                    {
                        _result = result;
                        CancelGroup();
                        return true;
                    }
                    return false;
                }

                new protected void CancelGroup()
                {
                    // This may be called multiple times. It's fine because it checks internally if it's already canceled.
                    try
                    {
                        _cancelationRef.CancelUnsafe();
                    }
                    catch (Exception e)
                    {
                        _cancelationOrCleanupThrew = true;
                        RecordException(e);
                    }
                }

                [MethodImpl(InlineOption)]
                protected void Reset(CancelationRef sourceCancelationRef, CancelationRef groupCancelationRef, bool cancelOnNonResolved)
                {
                    _sourceCancelationRef = sourceCancelationRef;
                    _isResolved = 0;
                    _cancelOnNonResolved = cancelOnNonResolved;
                    _cancelationOrCleanupThrew = false;
                    Reset(groupCancelationRef);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal sealed partial class RacePromiseGroupVoid : RacePromiseGroupBase<VoidResult>
            {
                [MethodImpl(InlineOption)]
                private static RacePromiseGroupVoid GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<RacePromiseGroupVoid>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new RacePromiseGroupVoid()
                        : obj.UnsafeAs<RacePromiseGroupVoid>();
                }

                [MethodImpl(InlineOption)]
                internal static RacePromiseGroupVoid GetOrCreate(CancelationRef sourceCancelationRef, CancelationRef groupCancelationRef, bool cancelOnNonResolved)
                {
                    var promise = GetOrCreate();
                    promise.Reset(sourceCancelationRef, groupCancelationRef, cancelOnNonResolved);
                    return promise;
                }

                internal override void MaybeDispose()
                {
                    Dispose();
                    ObjectPool.MaybeRepool(this);
                }

                internal override void Handle(PromiseRefBase handler, Promise.State state)
                {
                    ThrowIfInPool(this);

                    RemovePromiseAndSetCompletionState(handler, state);
                    if (state == Promise.State.Resolved)
                    {
                        if (Interlocked.Exchange(ref _isResolved, 1) == 0)
                        {
                            CancelGroup();
                        }
                    }
                    else
                    {
                        if (state == Promise.State.Rejected)
                        {
                            RecordException(handler.RejectContainer.GetValueAsException());
                        }
                        if (_cancelOnNonResolved)
                        {
                            CancelGroup();
                        }
                    }
                    handler.MaybeDispose();
                    if (TryComplete())
                    {
                        // All promises are complete.
                        Complete();
                    }
                }

                private void Complete()
                {
                    var state = _isResolved == 1 & !_cancelationOrCleanupThrew ? Promise.State.Resolved : Promise.State.Canceled;
                    var sourceCancelationRef = _sourceCancelationRef;
                    if (sourceCancelationRef != null)
                    {
                        _sourceCancelationRef = null;
                        // If the source cancelation token was canceled before this is complete, it's canceled.
                        if (sourceCancelationRef.IsCanceledUnsafe())
                        {
                            state = Promise.State.Canceled;
                        }
                        sourceCancelationRef.ReleaseUserUnsafe();
                    }

                    Complete(state);
                }

                internal void MarkReady(uint totalPromises)
                {
                    ThrowIfInPool(this);

                    // This method is called after all promises have been hooked up to this.
                    if (MarkReadyAndGetIsComplete(unchecked((int) totalPromises)))
                    {
                        // All promises already completed.
                        Complete();
                    }
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal sealed partial class RacePromiseGroup<TResult> : RacePromiseGroupBase<TResult>
            {
                [MethodImpl(InlineOption)]
                private static RacePromiseGroup<TResult> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<RacePromiseGroup<TResult>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new RacePromiseGroup<TResult>()
                        : obj.UnsafeAs<RacePromiseGroup<TResult>>();
                }

                [MethodImpl(InlineOption)]
                internal static RacePromiseGroup<TResult> GetOrCreate(CancelationRef sourceCancelationRef, CancelationRef groupCancelationRef, bool cancelOnNonResolved, RaceCleanupCallback<TResult> cleanupCallback)
                {
                    var promise = GetOrCreate();
                    promise.Reset(sourceCancelationRef, groupCancelationRef, cancelOnNonResolved);
                    promise._cleanupCallback = cleanupCallback;
                    return promise;
                }

                internal override void MaybeDispose()
                {
                    Dispose();
                    ObjectPool.MaybeRepool(this);
                }

                [MethodImpl(InlineOption)]
                private bool TryComplete(int releaseCount)
                {
                    Debug.Assert(releaseCount < 0);
                    // We don't do an overflow check here, because it starts at zero
                    // and a promise may complete and decrement it before MarkReady() is called.
                    return Interlocked.Add(ref _waitCount, releaseCount) == 0;
                }

                internal override void Handle(PromiseRefBase handler, Promise.State state)
                {
                    ThrowIfInPool(this);

                    RemovePromiseAndSetCompletionState(handler, state);
                    int releaseCount = -1;
                    if (state == Promise.State.Resolved)
                    {
                        if (Interlocked.Exchange(ref _isResolved, 1) == 0)
                        {
                            releaseCount = -2;
                            _result = handler.GetResult<TResult>();
                            CancelGroup();
                        }
                        else
                        {
                            MaybeInvokeCleanupCallback(ref releaseCount, handler.GetResult<TResult>());
                        }
                    }
                    else
                    {
                        releaseCount = -2;
                        if (state == Promise.State.Rejected)
                        {
                            RecordException(handler.RejectContainer.GetValueAsException());
                        }
                        if (_cancelOnNonResolved)
                        {
                            CancelGroup();
                        }
                    }
                    handler.MaybeDispose();
                    if (TryComplete(releaseCount))
                    {
                        // All promises are complete.
                        CompleteAndCleanup();
                    }
                }

                private void MaybeInvokeCleanupCallback(ref int releaseCount, in TResult arg)
                {
                    var cleanupCallback = _cleanupCallback;
                    if (cleanupCallback == null)
                    {
                        releaseCount = -2;
                        return;
                    }

                    var cleanupPromise = cleanupCallback.Invoke(arg);
                    if (cleanupPromise._ref == null)
                    {
                        releaseCount = -2;
                        return;
                    }

                    HookupCleanupPromise(cleanupPromise);
                }

                internal void HookupCleanupPromise(Promise cleanupPromise)
                {
                    ThrowIfInPool(this);

                    try
                    {
                        ValidateReturn(cleanupPromise);
#if PROMISE_DEBUG
                        AddForCircularAwaitDetection(cleanupPromise._ref);
                        cleanupPromise
                            .ContinueWith((this, cleanupPromise._ref), (cv, result) =>
                            {
                                var (_this, cleanupRef) = cv;
                                RemoveForCircularAwaitDetection(cleanupRef);
#else
                        cleanupPromise
                            .ContinueWith(this, (_this, result) =>
                            {
#endif
                                if (result.State == Promise.State.Rejected)
                                {
                                    _this._cancelationOrCleanupThrew = true;
                                    _this.RecordException(result._target._rejectContainer.GetValueAsException());
                                }
                                if (_this.TryComplete())
                                {
                                    _this.CompleteAndCleanup();
                                }
                            })
                            .Forget();
                    }
                    catch (Exception e)
                    {
#if PROMISE_DEBUG
                        RemoveForCircularAwaitDetection(cleanupPromise._ref);
#endif
                        _cancelationOrCleanupThrew = true;
                        RecordException(e is InvalidReturnException ? e : new InvalidReturnException("onCleanup returned an invalid promise.", string.Empty));
                        if (TryComplete())
                        {
                            CompleteAndCleanup();
                        }
                    }
                }

                internal void MarkReady(uint totalPromises)
                {
                    ThrowIfInPool(this);

                    // This method is called after all promises have been hooked up to this.
                    if (MarkReadyAndGetIsComplete(unchecked((int) totalPromises)))
                    {
                        // All promises already completed.
                        CompleteAndCleanup();
                    }
                }

                private void CompleteAndCleanup()
                {
                    ThrowIfInPool(this);

                    // If the source cancelation token is canceled, or a cancelation or cleanup callback threw,
                    // and this was otherwise resolved, we need to cleanup the resolved value.
                    var cleanupCallback = _cleanupCallback;
                    _cleanupCallback = null;
                    bool wasResolved = _isResolved == 1;
                    var state = wasResolved & !_cancelationOrCleanupThrew ? Promise.State.Resolved : Promise.State.Canceled;
                    var sourceCancelationRef = _sourceCancelationRef;
                    if (sourceCancelationRef != null)
                    {
                        _sourceCancelationRef = null;
                        // If the source cancelation token was canceled before this is complete, it's canceled.
                        if (sourceCancelationRef.IsCanceledUnsafe())
                        {
                            state = Promise.State.Canceled;
                        }
                        sourceCancelationRef.ReleaseUserUnsafe();
                    }

                    if (state == Promise.State.Resolved | !wasResolved | cleanupCallback == null)
                    {
                        cleanupCallback?.Dispose();
                        Complete(state);
                        return;
                    }

                    var cleanupPromise = cleanupCallback.Invoke(_result);
                    cleanupCallback.Dispose();
                    if (cleanupPromise._ref == null)
                    {
                        Complete(state);
                        return;
                    }

                    try
                    {
                        ValidateReturn(cleanupPromise);
#if PROMISE_DEBUG
                        AddForCircularAwaitDetection(cleanupPromise._ref);
                        cleanupPromise
                            .ContinueWith((this, cleanupPromise._ref), (cv, result) =>
                            {
                                var (_this, cleanupRef) = cv;
                                RemoveForCircularAwaitDetection(cleanupRef);
#else
                        cleanupPromise
                            .ContinueWith(this, (_this, result) =>
                            {
#endif
                                if (result.State == Promise.State.Rejected)
                                {
                                    _this._cancelationOrCleanupThrew = true;
                                    _this.RecordException(result._target._rejectContainer.GetValueAsException());
                                }
                                _this.Complete(Promise.State.Canceled);
                            })
                            .Forget();
                    }
                    catch (Exception e)
                    {
#if PROMISE_DEBUG
                        RemoveForCircularAwaitDetection(cleanupPromise._ref);
#endif
                        _cancelationOrCleanupThrew = true;
                        RecordException(e is InvalidReturnException ? e : new InvalidReturnException("onCleanup returned an invalid promise.", string.Empty));
                        Complete(Promise.State.Rejected);
                    }
                }
            }
        } // class PromiseRefBase

        [MethodImpl(InlineOption)]
        internal static PromiseRefBase.RacePromiseGroupVoid GetOrCreateRacePromiseGroup(CancelationRef sourceCancelationRef, CancelationRef groupCancelationRef, bool cancelOnNonResolved)
            => PromiseRefBase.RacePromiseGroupVoid.GetOrCreate(sourceCancelationRef, groupCancelationRef, cancelOnNonResolved);

        [MethodImpl(InlineOption)]
        internal static PromiseRefBase.RacePromiseGroup<T> GetOrCreateRacePromiseGroup<T>(CancelationRef sourceCancelationRef, CancelationRef groupCancelationRef,
            bool cancelOnNonResolved, RaceCleanupCallback<T> cleanupCallback)
            => PromiseRefBase.RacePromiseGroup<T>.GetOrCreate(sourceCancelationRef, groupCancelationRef, cancelOnNonResolved, cleanupCallback);

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowInvalidRaceGroup(int skipFrames)
            => throw new InvalidOperationException("The promise race group is invalid.", GetFormattedStacktrace(skipFrames + 1));

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowAtLeastOneRaceGroup(int skipFrames)
            => throw new InvalidOperationException("The promise race group must have at least one promise added to it.", GetFormattedStacktrace(skipFrames + 1));
    } // class Internal
}