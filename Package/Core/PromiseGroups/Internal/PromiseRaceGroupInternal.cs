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
                    var state = _completeState;
                    // Race promise group ignores rejections if any promise was resolved,
                    // unless a cancelation token callback threw.
                    if ((state != Promise.State.Resolved | _cancelationThrew) & _exceptions != null)
                    {
                        state = Promise.State.Rejected;
                        _rejectContainer = CreateRejectContainer(new AggregateException(_exceptions), int.MinValue, null, this);
                    }
                    _exceptions = null;
                    return state;
                }

                [MethodImpl(InlineOption)]
                internal void SetResolved()
                {
                    // We don't need to branch for VoidResult.
                    _isResolved = 1;
                    _completeState = Promise.State.Resolved;
                    CancelGroup();
                }

                [MethodImpl(InlineOption)]
                internal void SetResolved(in TResult result)
                {
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
                        _cancelationRef.Cancel();
                    }
                    catch (Exception e)
                    {
                        _cancelationThrew = true;
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
                    base.Reset(cancelationSource);
                    _completeState = Promise.State.Canceled; // Default to Canceled state. If the promise is actually resolved or rejected, the state will be overwritten.
                    _isResolved = 0;
                    _cancelOnNonResolved = cancelOnNonResolved;
                    _cancelationThrew = false;
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
                internal static RacePromiseGroup<TResult> GetOrCreate(CancelationRef cancelationSource, bool cancelOnNonResolved)
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
                    RemovePromiseAndSetCompletionState(handler, state);
                    if (state == Promise.State.Resolved)
                    {
                        if (Interlocked.Exchange(ref _isResolved, 1) == 0)
                        {
                            _completeState = Promise.State.Resolved;
                            _result = handler.GetResult<TResult>();
                        }
                        CancelGroup();
                    }
                    else
                    {
                        if (state == Promise.State.Rejected)
                        {
                            RecordException(handler._rejectContainer.GetValueAsException());
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
                            RecordException(handler._rejectContainer.GetValueAsException());
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
                            RecordException(handler._rejectContainer.GetValueAsException());
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
        internal static PromiseRefBase.RacePromiseGroup<T> GetOrCreateRacePromiseGroup<T>(CancelationRef cancelationSource, bool cancelOnNonResolved)
            => PromiseRefBase.RacePromiseGroup<T>.GetOrCreate(cancelationSource, cancelOnNonResolved);

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