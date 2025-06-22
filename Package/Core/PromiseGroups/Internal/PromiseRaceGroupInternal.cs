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
                protected void Complete()
                {
                    HandleNextInternal(CompleteAndGetState());
                }

                private Promise.State CompleteAndGetState()
                {
                    ThrowIfInPool(this);

                    var state = _completeState;
                    // Race promise group ignores rejections if any promise was resolved,
                    // unless a cancelation token or cleanup callback threw.
                    if ((state != Promise.State.Resolved | _cancelationOrCleanupThrew) & _exceptions != null)
                    {
                        state = Promise.State.Rejected;
                        RejectContainer = CreateRejectContainer(new AggregateException(_exceptions), int.MinValue, null, this);
                    }
                    _exceptions = null;
                    return state;
                }

                [MethodImpl(InlineOption)]
                internal void SetResolved()
                {
                    ThrowIfInPool(this);

                    // We don't need to branch for VoidResult.
                    _isResolved = 1;
                    _completeState = Promise.State.Resolved;
                    CancelGroup();
                }

                [MethodImpl(InlineOption)]
                internal void SetResolved(in TResult result)
                {
                    ThrowIfInPool(this);

                    if (Interlocked.Exchange(ref _isResolved, 1) == 0)
                    {
                        _completeState = Promise.State.Resolved;
                        _result = result;
                    }
                    CancelGroup();
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
                internal void MarkReady(uint totalPromises)
                    => MarkReady(unchecked((int) totalPromises));

                internal void MarkReady(int totalPromises)
                {
                    // This method is called after all promises have been hooked up to this.
                    if (MarkReadyAndGetIsComplete(totalPromises))
                    {
                        // All promises already completed.
                        _next = PromiseCompletionSentinel.s_instance;
                        SetCompletionState(CompleteAndGetState());
                    }
                }

                [MethodImpl(InlineOption)]
                protected void Reset(CancelationRef cancelationSource, bool cancelOnNonResolved)
                {
                    Reset(cancelationSource);
                    _completeState = Promise.State.Canceled; // Default to Canceled state. If the promise is actually resolved or rejected, the state will be overwritten.
                    _isResolved = 0;
                    _cancelOnNonResolved = cancelOnNonResolved;
                    _cancelationOrCleanupThrew = false;
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
                internal static RacePromiseGroupVoid GetOrCreate(CancelationRef cancelationSource, bool cancelOnNonResolved)
                {
                    var promise = GetOrCreate();
                    promise.Reset(cancelationSource, cancelOnNonResolved);
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
                            _completeState = Promise.State.Resolved;
                        }
                        CancelGroup();
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
                internal static RacePromiseGroup<TResult> GetOrCreate(CancelationRef cancelationSource, bool cancelOnNonResolved, RaceCleanupCallback<TResult> cleanupCallback)
                {
                    var promise = GetOrCreate();
                    promise.Reset(cancelationSource, cancelOnNonResolved);
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
                            _completeState = Promise.State.Resolved;
                            _result = handler.GetResult<TResult>();
                        }
                        else
                        {
                            MaybeInvokeCleanupCallback(ref releaseCount, handler.GetResult<TResult>());
                        }
                        CancelGroup();
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
                        RecordException(e is InvalidReturnException ? e : new InvalidReturnException("onCleanup returned an invalid promise.", string.Empty));
                        if (TryComplete())
                        {
                            CompleteAndCleanup();
                        }
                    }
                }

                new internal void MarkReady(uint totalPromises)
                {
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

                    // If a cancelation or cleanup callback threw, and this is otherwise resolved, we need to cleanup the resolved value.
                    var cleanupCallback = _cleanupCallback;
                    _cleanupCallback = null;
                    if (_completeState != Promise.State.Resolved | !_cancelationOrCleanupThrew | cleanupCallback == null)
                    {
                        cleanupCallback?.Dispose();
                        Complete();
                        return;
                    }

                    var cleanupPromise = cleanupCallback.Invoke(_result);
                    cleanupCallback.Dispose();
                    if (cleanupPromise._ref == null)
                    {
                        Complete();
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
                                    _this.RecordException(result._target._rejectContainer.GetValueAsException());
                                }
                                _this.Complete();
                            })
                            .Forget();
                    }
                    catch (Exception e)
                    {
#if PROMISE_DEBUG
                        RemoveForCircularAwaitDetection(cleanupPromise._ref);
#endif
                        RecordException(e is InvalidReturnException ? e : new InvalidReturnException("onCleanup returned an invalid promise.", string.Empty));
                        if (TryComplete())
                        {
                            CompleteAndCleanup();
                        }
                    }
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal sealed partial class RacePromiseWithIndexGroupVoid : RacePromiseGroupBase<int>
            {
                [MethodImpl(InlineOption)]
                private static RacePromiseWithIndexGroupVoid GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<RacePromiseWithIndexGroupVoid>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new RacePromiseWithIndexGroupVoid()
                        : obj.UnsafeAs<RacePromiseWithIndexGroupVoid>();
                }

                [MethodImpl(InlineOption)]
                internal static RacePromiseWithIndexGroupVoid GetOrCreate(CancelationRef cancelationSource, bool cancelOnNonResolved)
                {
                    var promise = GetOrCreate();
                    promise.Reset(cancelationSource, cancelOnNonResolved);
                    return promise;
                }

                internal override void MaybeDispose()
                {
                    Dispose();
                    ObjectPool.MaybeRepool(this);
                }

                internal override void Handle(PromiseRefBase handler, Promise.State state, int index)
                {
                    RemovePromiseAndSetCompletionState(handler, state);
                    if (state == Promise.State.Resolved)
                    {
                        if (Interlocked.Exchange(ref _isResolved, 1) == 0)
                        {
                            _completeState = Promise.State.Resolved;
                            _result = index;
                        }
                        CancelGroup();
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
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal sealed partial class RacePromiseWithIndexGroup<TResult> : RacePromiseGroupBase<(int, TResult)>
            {
                [MethodImpl(InlineOption)]
                private static RacePromiseWithIndexGroup<TResult> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<RacePromiseWithIndexGroup<TResult>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new RacePromiseWithIndexGroup<TResult>()
                        : obj.UnsafeAs<RacePromiseWithIndexGroup<TResult>>();
                }

                [MethodImpl(InlineOption)]
                internal static RacePromiseWithIndexGroup<TResult> GetOrCreate(CancelationRef cancelationSource, bool cancelOnNonResolved)
                {
                    var promise = GetOrCreate();
                    promise.Reset(cancelationSource, cancelOnNonResolved);
                    return promise;
                }

                internal override void MaybeDispose()
                {
                    Dispose();
                    ObjectPool.MaybeRepool(this);
                }

                internal override void Handle(PromiseRefBase handler, Promise.State state, int index)
                {
                    RemovePromiseAndSetCompletionState(handler, state);
                    if (state == Promise.State.Resolved)
                    {
                        if (Interlocked.Exchange(ref _isResolved, 1) == 0)
                        {
                            _completeState = Promise.State.Resolved;
                            _result = (index, handler.GetResult<TResult>());
                        }
                        CancelGroup();
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
            }
        } // class PromiseRefBase

        [MethodImpl(InlineOption)]
        internal static PromiseRefBase.RacePromiseGroupVoid GetOrCreateRacePromiseGroup(CancelationRef cancelationSource, bool cancelOnNonResolved)
            => PromiseRefBase.RacePromiseGroupVoid.GetOrCreate(cancelationSource, cancelOnNonResolved);

        [MethodImpl(InlineOption)]
        internal static PromiseRefBase.RacePromiseGroup<T> GetOrCreateRacePromiseGroup<T>(CancelationRef cancelationSource, bool cancelOnNonResolved, RaceCleanupCallback<T> cleanupCallback)
            => PromiseRefBase.RacePromiseGroup<T>.GetOrCreate(cancelationSource, cancelOnNonResolved, cleanupCallback);

        [MethodImpl(InlineOption)]
        internal static PromiseRefBase.RacePromiseWithIndexGroupVoid GetOrCreateRacePromiseWithIndexGroupVoid(CancelationRef cancelationSource, bool cancelOnNonResolved)
            => PromiseRefBase.RacePromiseWithIndexGroupVoid.GetOrCreate(cancelationSource, cancelOnNonResolved);

        [MethodImpl(InlineOption)]
        internal static PromiseRefBase.RacePromiseWithIndexGroup<T> GetOrCreateRacePromiseWithIndexGroup<T>(CancelationRef cancelationSource, bool cancelOnNonResolved)
            => PromiseRefBase.RacePromiseWithIndexGroup<T>.GetOrCreate(cancelationSource, cancelOnNonResolved);

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowInvalidRaceGroup(int skipFrames)
            => throw new InvalidOperationException("The promise race group is invalid.", GetFormattedStacktrace(skipFrames + 1));

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowAtLeastOneRaceGroup(int skipFrames)
            => throw new InvalidOperationException("The promise race group must have at least one promise added to it.", GetFormattedStacktrace(skipFrames + 1));
    } // class Internal
}