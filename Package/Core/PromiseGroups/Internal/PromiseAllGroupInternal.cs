﻿#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Proto.Promises
{
    partial class Internal
    {
        partial class PromiseRefBase
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal sealed partial class AllPromiseGroup<T> : MergePromiseGroupBase<IList<T>>
            {
                [MethodImpl(InlineOption)]
                private static AllPromiseGroup<T> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<AllPromiseGroup<T>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new AllPromiseGroup<T>()
                        : obj.UnsafeAs<AllPromiseGroup<T>>();
                }

                [MethodImpl(InlineOption)]
                internal static AllPromiseGroup<T> GetOrCreate(CancelationRef cancelationSource, IList<T> value)
                {
                    var promise = GetOrCreate();
                    promise.Reset();
                    promise._result = value;
                    promise._completeState = Promise.State.Resolved; // Default to Resolved state. If the promise is actually canceled or rejected, the state will be overwritten.
                    promise._cancelationRef = cancelationSource;
                    return promise;
                }

                internal override void MaybeDispose()
                {
                    Dispose();
                    _exceptions = null;
                    _cancelationRef.Dispose();
                    _cancelationRef = null;
                    ObjectPool.MaybeRepool(this);
                }

                internal override void Handle(PromisePassThroughForMergeGroup passthrough, PromiseRefBase handler, Promise.State state)
                {
                    // We store the passthrough until all promises are complete,
                    // so that items won't be written to the list while it's being expanded on another thread.
                    RemovePromiseAndSetCompletionState(handler, state);
                    _completedPassThroughs.PushInterlocked(passthrough);
                    if (state != Promise.State.Resolved)
                    {
                        CancelGroup();
                    }
                    if (TryComplete())
                    {
                        // All promises are complete.
                        Complete(_completeState);
                    }
                }

                private void Complete(Promise.State state)
                {
                    if (_exceptions != null)
                    {
                        state = Promise.State.Rejected;
                        _rejectContainer = CreateRejectContainer(new AggregateException(_exceptions), int.MinValue, null, this);
                        _exceptions = null;
                    }

                    var passthroughs = _completedPassThroughs;
                    _completedPassThroughs = default;
                    do
                    {
                        var passthrough = passthroughs.Pop();
                        _result[passthrough.Index] = passthrough.Owner.GetResult<T>();
                        passthrough.Dispose();
                    } while (passthroughs.IsNotEmpty);
                    HandleNextInternal(state);
                }
            }

            internal sealed partial class AllPromiseResultsGroupVoid : MergePromiseGroupBase<IList<Promise.ResultContainer>>
            {
                [MethodImpl(InlineOption)]
                private static AllPromiseResultsGroupVoid GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<AllPromiseResultsGroupVoid>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new AllPromiseResultsGroupVoid()
                        : obj.UnsafeAs<AllPromiseResultsGroupVoid>();
                }

                [MethodImpl(InlineOption)]
                internal static AllPromiseResultsGroupVoid GetOrCreate(CancelationRef cancelationSource, IList<Promise.ResultContainer> value)
                {
                    var promise = GetOrCreate();
                    promise.Reset();
                    promise._result = value;
                    promise._completeState = Promise.State.Resolved; // Default to Resolved state. If the promise is actually canceled or rejected, the state will be overwritten.
                    promise._cancelationRef = cancelationSource;
                    return promise;
                }

                internal override void MaybeDispose()
                {
                    Dispose();
                    ObjectPool.MaybeRepool(this);
                }

                internal override void Handle(PromisePassThroughForMergeGroup passthrough, PromiseRefBase handler, Promise.State state)
                {
                    // We store the passthrough until all promises are complete,
                    // so that items won't be written to the list while it's being expanded on another thread.
                    RemovePromiseAndSetCompletionState(handler, state);
                    _completedPassThroughs.PushInterlocked(passthrough);
                    if (state != Promise.State.Resolved)
                    {
                        CancelGroup();
                    }
                    if (TryComplete())
                    {
                        // All promises are complete.
                        Complete(_completeState);
                    }
                }

                private void Complete(Promise.State state)
                {
                    if (_exceptions != null)
                    {
                        state = Promise.State.Rejected;
                        _rejectContainer = CreateRejectContainer(new AggregateException(_exceptions), int.MinValue, null, this);
                        _exceptions = null;
                    }

                    var passthroughs = _completedPassThroughs;
                    _completedPassThroughs = default;
                    do
                    {
                        var passthrough = passthroughs.Pop();
                        var owner = passthrough.Owner;
                        _result[passthrough.Index] = new Promise.ResultContainer(owner._rejectContainer, owner.State);
                        passthrough.Dispose();
                    } while (passthroughs.IsNotEmpty);
                    HandleNextInternal(state);
                }
            }

            internal sealed partial class AllPromiseResultsGroup<T> : MergePromiseGroupBase<IList<Promise<T>.ResultContainer>>
            {
                [MethodImpl(InlineOption)]
                private static AllPromiseResultsGroup<T> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<AllPromiseResultsGroup<T>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new AllPromiseResultsGroup<T>()
                        : obj.UnsafeAs<AllPromiseResultsGroup<T>>();
                }

                [MethodImpl(InlineOption)]
                internal static AllPromiseResultsGroup<T> GetOrCreate(CancelationRef cancelationSource, IList<Promise<T>.ResultContainer> value)
                {
                    var promise = GetOrCreate();
                    promise.Reset();
                    promise._result = value;
                    promise._completeState = Promise.State.Resolved; // Default to Resolved state. If the promise is actually canceled or rejected, the state will be overwritten.
                    promise._cancelationRef = cancelationSource;
                    return promise;
                }

                internal override void MaybeDispose()
                {
                    Dispose();
                    ObjectPool.MaybeRepool(this);
                }

                internal override void Handle(PromisePassThroughForMergeGroup passthrough, PromiseRefBase handler, Promise.State state)
                {
                    // We store the passthrough until all promises are complete,
                    // so that items won't be written to the list while it's being expanded on another thread.
                    RemovePromiseAndSetCompletionState(handler, state);
                    _completedPassThroughs.PushInterlocked(passthrough);
                    if (state != Promise.State.Resolved)
                    {
                        CancelGroup();
                    }
                    if (TryComplete())
                    {
                        // All promises are complete.
                        Complete(_completeState);
                    }
                }

                private void Complete(Promise.State state)
                {
                    if (_exceptions != null)
                    {
                        state = Promise.State.Rejected;
                        _rejectContainer = CreateRejectContainer(new AggregateException(_exceptions), int.MinValue, null, this);
                        _exceptions = null;
                    }

                    var passthroughs = _completedPassThroughs;
                    _completedPassThroughs = default;
                    do
                    {
                        var passthrough = passthroughs.Pop();
                        var owner = passthrough.Owner;
                        _result[passthrough.Index] = new Promise<T>.ResultContainer(owner.GetResult<T>(), owner._rejectContainer, owner.State);
                        passthrough.Dispose();
                    } while (passthroughs.IsNotEmpty);
                    HandleNextInternal(state);
                }
            }
        } // class PromiseRefBase

        [MethodImpl(InlineOption)]
        internal static PromiseRefBase.AllPromiseGroup<T> GetOrCreateAllPromiseGroup<T>(CancelationRef cancelationSource, IList<T> value)
            => PromiseRefBase.AllPromiseGroup<T>.GetOrCreate(cancelationSource, value);

        [MethodImpl(InlineOption)]
        internal static PromiseRefBase.AllPromiseResultsGroupVoid GetOrCreateAllPromiseResultsGroup(CancelationRef cancelationSource, IList<Promise.ResultContainer> value)
            => PromiseRefBase.AllPromiseResultsGroupVoid.GetOrCreate(cancelationSource, value);

        [MethodImpl(InlineOption)]
        internal static PromiseRefBase.AllPromiseResultsGroup<T> GetOrCreateAllPromiseResultsGroup<T>(CancelationRef cancelationSource, IList<Promise<T>.ResultContainer> value)
            => PromiseRefBase.AllPromiseResultsGroup<T>.GetOrCreate(cancelationSource, value);

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowInvalidAllGroup()
            => throw new InvalidOperationException("The promise all group is invalid.");
    } // class Internal
}