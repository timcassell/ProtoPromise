#if UNITY_5_5 || NET_2_0 || NET_2_0_SUBSET
#define NET_LEGACY
#endif

#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif
#if !PROTO_PROMISE_PROGRESS_DISABLE
#define PROMISE_PROGRESS
#else
#undef PROMISE_PROGRESS
#endif

#pragma warning disable 0420 // A reference to a volatile field will not be treated as volatile

using System;
using System.Collections.Generic;
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
            internal abstract partial class MultiHandleablePromiseBase<TResult> : PromiseSingleAwait<TResult>
            {
                internal override void Handle(PromiseRefBase handler, object rejectContainer, Promise.State state) { throw new System.InvalidOperationException(); }

                // When each promise is completed, we decrement the wait count until it reaches zero before we handle the next waiter.
                // If a promise completes with a state that should complete this promise before all the other promises were complete,
                // we instead swap the count to 0 so that it will be marked complete early, and future completions will decrement into negative and never read 0 again.
                // (Merge reject/cancel, or First resolve, Race always tries to set complete).
                [MethodImpl(InlineOption)]
                protected bool TrySetComplete()
                {
                    return InterlockedExchange(ref _waitCount, 0) > 0;
                }

                [MethodImpl(InlineOption)]
                protected bool RemoveWaiterAndGetIsComplete()
                {
                    // No overflow check as we expect the count to be able to go negative.
                    return Interlocked.Add(ref _waitCount, -1) == 0;
                }

                protected void ReleaseAndHandleNext(object rejectContainer, Promise.State state)
                {
                    // Decrement retain counter instead of calling MaybeDispose, since we know the next handler will call MaybeDispose.
                    InterlockedAddWithUnsignedOverflowCheck(ref _retainCounter, -1);
                    HandleNextInternal(rejectContainer, state);
                }

                protected void Setup(ValueLinkedStack<PromisePassThrough> promisePassThroughs, int pendingAwaits, ushort depth)
                {
                    _waitCount = pendingAwaits;
                    unchecked
                    {
                        _retainCounter = pendingAwaits + 1;
                    }
                    Reset(depth);

                    _passThroughs = promisePassThroughs;
                    foreach (var passThrough in promisePassThroughs)
                    {
#if PROMISE_DEBUG
                        lock (_previousPromises)
                        {
                            _previousPromises.Push(passThrough.Owner);
                        }
#endif
                        passThrough.SetTargetAndAddToOwner(this);
                    }
                }

                new protected void Dispose()
                {
                    base.Dispose();
                    while (_passThroughs.IsNotEmpty)
                    {
                        _passThroughs.Pop().Dispose();
                    }
#if PROMISE_DEBUG
                    lock (_previousPromises)
                    {
                        _previousPromises.Clear();
                    }
#endif
                }
            }

            internal abstract partial class MergePromise<TResult> : MultiHandleablePromiseBase<TResult>
            {
                internal void Setup(ValueLinkedStack<PromisePassThrough> promisePassThroughs, int pendingAwaits, ulong completedProgress, ushort depth)
                {
#if PROMISE_PROGRESS
                    _completeProgress = completedProgress;
#endif
                    Setup(promisePassThroughs, pendingAwaits, depth);
                }
            }

            internal static MergePromise<VoidResult> GetOrCreateAllPromiseVoid(ValueLinkedStack<PromisePassThrough> promisePassThroughs, int pendingAwaits, ulong completedProgress, ushort depth)
            {
                var promise = MergePromiseVoid.GetOrCreate();
                promise.Setup(promisePassThroughs, pendingAwaits, completedProgress, depth);
                return promise;
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private sealed class MergePromiseVoid : MergePromise<VoidResult>
            {
                [MethodImpl(InlineOption)]
                internal static MergePromiseVoid GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<MergePromiseVoid>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new MergePromiseVoid()
                        : obj.UnsafeAs<MergePromiseVoid>();
                }

                internal override void MaybeDispose()
                {
                    if (InterlockedAddWithUnsignedOverflowCheck(ref _retainCounter, -1) == 0)
                    {
                        Dispose();
                        ObjectPool.MaybeRepool(this);
                    }
                }

                internal override void Handle(PromiseRefBase handler, object rejectContainer, Promise.State state, int index)
                {
                    handler.SuppressRejection = true;
                    handler.MaybeDispose();
                    bool isComplete = state == Promise.State.Resolved
                        ? RemoveWaiterAndGetIsComplete()
                        : TrySetComplete();
                    if (isComplete)
                    {
                        ReleaseAndHandleNext(rejectContainer, state);
                        return;
                    }
                    MaybeDispose();
                }
            }

            private abstract class MergePromiseT<TResult> : MergePromise<TResult>
            {
                internal override sealed void MaybeDispose()
                {
                    if (InterlockedAddWithUnsignedOverflowCheck(ref _retainCounter, -1) == 0)
                    {
                        Dispose();
                        // We use this base type to repool so that we don't have to override MaybeDispose() on every merge promise, and this can be called directly instead of virtually.
                        // The merge promises then try to take this base type from the pool, and create their concrete type if it doesn't exist.
                        // Each merge promise type will be unique thanks to the differing <TResult> type (i.e. <List<T>>, <ValueTuple<T1, T2>>, etc).
                        ObjectPool.MaybeRepool(this);
                    }
                }

                internal override sealed void Handle(PromiseRefBase handler, object rejectContainer, Promise.State state, int index)
                {
                    bool isComplete;
                    if (state == Promise.State.Resolved)
                    {
                        ReadResult(handler, index);
                        isComplete = RemoveWaiterAndGetIsComplete();
                    }
                    else
                    {
                        isComplete = TrySetComplete();
                    }
                    handler.SuppressRejection = true;
                    handler.MaybeDispose();
                    if (isComplete)
                    {
                        ReleaseAndHandleNext(rejectContainer, state);
                        return;
                    }
                    MaybeDispose();
                }

                protected abstract void ReadResult(PromiseRefBase handler, int index);
            }

            internal static MergePromise<IList<TResult>> GetOrCreateAllPromise<TResult>(
                ValueLinkedStack<PromisePassThrough> promisePassThroughs,
#if CSHARP_7_3_OR_NEWER
                in
#endif
                IList<TResult> value,
                int pendingAwaits, ulong completedProgress, ushort depth)
            {
                var promise = AllPromise<TResult>.GetOrCreate();
                promise._result = value;
                promise.Setup(promisePassThroughs, pendingAwaits, completedProgress, depth);
                return promise;
            }

            private sealed class AllPromise<TResult> : MergePromiseT<IList<TResult>>
            {
                [MethodImpl(InlineOption)]
                internal static AllPromise<TResult> GetOrCreate()
                {
                    // We take the base type instead of the concrete type because the base type re-pools with its type.
                    var obj = ObjectPool.TryTakeOrInvalid<MergePromiseT<IList<TResult>>, AllPromise<TResult>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new AllPromise<TResult>()
                        : obj.UnsafeAs<AllPromise<TResult>>();
                }

                protected override void ReadResult(PromiseRefBase handler, int index)
                {
                    _result[index] = handler.GetResult<TResult>();
                }
            }

            internal static MergePromise<T1> GetOrCreateMergePromise<T1>(
                ValueLinkedStack<PromisePassThrough> promisePassThroughs,
#if CSHARP_7_3_OR_NEWER
                in
#endif
                T1 value,
                int pendingAwaits, ulong completedProgress, ushort depth)
            {
                var promise = MergePromiseTuple<T1>.GetOrCreate();
                promise._result = value;
                promise.Setup(promisePassThroughs, pendingAwaits, completedProgress, depth);
                return promise;
            }

            private sealed class MergePromiseTuple<T1> : MergePromiseT<T1>
            {
                [MethodImpl(InlineOption)]
                internal static MergePromiseTuple<T1> GetOrCreate()
                {
                    // We take the base type instead of the concrete type because the base type re-pools with its type.
                    var obj = ObjectPool.TryTakeOrInvalid<MergePromiseT<T1>, MergePromiseTuple<T1>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new MergePromiseTuple<T1>()
                        : obj.UnsafeAs<MergePromiseTuple<T1>>();
                }

                protected override void ReadResult(PromiseRefBase handler, int index)
                {
                    if (index == 0)
                    {
                        _result = handler.GetResult<T1>();
                    }
                }
            }

            internal static MergePromise<ValueTuple<T1, T2>> GetOrCreateMergePromise<T1, T2>(
                ValueLinkedStack<PromisePassThrough> promisePassThroughs,
#if CSHARP_7_3_OR_NEWER
                in
#endif
                ValueTuple<T1, T2> value,
                int pendingAwaits, ulong completedProgress, ushort depth)
            {
                var promise = MergePromiseTuple<T1, T2>.GetOrCreate();
                promise._result = value;
                promise.Setup(promisePassThroughs, pendingAwaits, completedProgress, depth);
                return promise;
            }

            private sealed class MergePromiseTuple<T1, T2> : MergePromiseT<ValueTuple<T1, T2>>
            {
                [MethodImpl(InlineOption)]
                internal static MergePromiseTuple<T1, T2> GetOrCreate()
                {
                    // We take the base type instead of the concrete type because the base type re-pools with its type.
                    var obj = ObjectPool.TryTakeOrInvalid<MergePromiseT<ValueTuple<T1, T2>>, MergePromiseTuple<T1, T2>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new MergePromiseTuple<T1, T2>()
                        : obj.UnsafeAs<MergePromiseTuple<T1, T2>>();
                }

                protected override void ReadResult(PromiseRefBase handler, int index)
                {
                    switch (index)
                    {
                        case 0:
                            _result.Item1 = handler.GetResult<T1>();
                            break;
                        case 1:
                            _result.Item2 = handler.GetResult<T2>();
                            break;
                    }
                }
            }

            internal static MergePromise<ValueTuple<T1, T2, T3>> GetOrCreateMergePromise<T1, T2, T3>(
                ValueLinkedStack<PromisePassThrough> promisePassThroughs,
#if CSHARP_7_3_OR_NEWER
                in
#endif
                ValueTuple<T1, T2, T3> value,
                int pendingAwaits, ulong completedProgress, ushort depth)
            {
                var promise = MergePromiseTuple<T1, T2, T3>.GetOrCreate();
                promise._result = value;
                promise.Setup(promisePassThroughs, pendingAwaits, completedProgress, depth);
                return promise;
            }

            private sealed class MergePromiseTuple<T1, T2, T3> : MergePromiseT<ValueTuple<T1, T2, T3>>
            {
                [MethodImpl(InlineOption)]
                internal static MergePromiseTuple<T1, T2, T3> GetOrCreate()
                {
                    // We take the base type instead of the concrete type because the base type re-pools with its type.
                    var obj = ObjectPool.TryTakeOrInvalid<MergePromiseT<ValueTuple<T1, T2, T3>>, MergePromiseTuple<T1, T2, T3>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new MergePromiseTuple<T1, T2, T3>()
                        : obj.UnsafeAs<MergePromiseTuple<T1, T2, T3>>();
                }

                protected override void ReadResult(PromiseRefBase handler, int index)
                {
                    switch (index)
                    {
                        case 0:
                            _result.Item1 = handler.GetResult<T1>();
                            break;
                        case 1:
                            _result.Item2 = handler.GetResult<T2>();
                            break;
                        case 2:
                            _result.Item3 = handler.GetResult<T3>();
                            break;
                    }
                }
            }

            internal static MergePromise<ValueTuple<T1, T2, T3, T4>> GetOrCreateMergePromise<T1, T2, T3, T4>(
                ValueLinkedStack<PromisePassThrough> promisePassThroughs,
#if CSHARP_7_3_OR_NEWER
                in
#endif
                ValueTuple<T1, T2, T3, T4> value,
                int pendingAwaits, ulong completedProgress, ushort depth)
            {
                var promise = MergePromiseTuple<T1, T2, T3, T4>.GetOrCreate();
                promise._result = value;
                promise.Setup(promisePassThroughs, pendingAwaits, completedProgress, depth);
                return promise;
            }

            private sealed class MergePromiseTuple<T1, T2, T3, T4> : MergePromiseT<ValueTuple<T1, T2, T3, T4>>
            {
                [MethodImpl(InlineOption)]
                internal static MergePromiseTuple<T1, T2, T3, T4> GetOrCreate()
                {
                    // We take the base type instead of the concrete type because the base type re-pools with its type.
                    var obj = ObjectPool.TryTakeOrInvalid<MergePromiseT<ValueTuple<T1, T2, T3, T4>>, MergePromiseTuple<T1, T2, T3, T4>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new MergePromiseTuple<T1, T2, T3, T4>()
                        : obj.UnsafeAs<MergePromiseTuple<T1, T2, T3, T4>>();
                }

                protected override void ReadResult(PromiseRefBase handler, int index)
                {
                    switch (index)
                    {
                        case 0:
                            _result.Item1 = handler.GetResult<T1>();
                            break;
                        case 1:
                            _result.Item2 = handler.GetResult<T2>();
                            break;
                        case 2:
                            _result.Item3 = handler.GetResult<T3>();
                            break;
                        case 3:
                            _result.Item4 = handler.GetResult<T4>();
                            break;
                    }
                }
            }

            internal static MergePromise<ValueTuple<T1, T2, T3, T4, T5>> GetOrCreateMergePromise<T1, T2, T3, T4, T5>(
                ValueLinkedStack<PromisePassThrough> promisePassThroughs,
#if CSHARP_7_3_OR_NEWER
                in
#endif
                ValueTuple<T1, T2, T3, T4, T5> value,
                int pendingAwaits, ulong completedProgress, ushort depth)
            {
                var promise = MergePromiseTuple<T1, T2, T3, T4, T5>.GetOrCreate();
                promise._result = value;
                promise.Setup(promisePassThroughs, pendingAwaits, completedProgress, depth);
                return promise;
            }

            private sealed class MergePromiseTuple<T1, T2, T3, T4, T5> : MergePromiseT<ValueTuple<T1, T2, T3, T4, T5>>
            {
                [MethodImpl(InlineOption)]
                internal static MergePromiseTuple<T1, T2, T3, T4, T5> GetOrCreate()
                {
                    // We take the base type instead of the concrete type because the base type re-pools with its type.
                    var obj = ObjectPool.TryTakeOrInvalid<MergePromiseT<ValueTuple<T1, T2, T3, T4, T5>>, MergePromiseTuple<T1, T2, T3, T4, T5>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new MergePromiseTuple<T1, T2, T3, T4, T5>()
                        : obj.UnsafeAs<MergePromiseTuple<T1, T2, T3, T4, T5>>();
                }

                protected override void ReadResult(PromiseRefBase handler, int index)
                {
                    switch (index)
                    {
                        case 0:
                            _result.Item1 = handler.GetResult<T1>();
                            break;
                        case 1:
                            _result.Item2 = handler.GetResult<T2>();
                            break;
                        case 2:
                            _result.Item3 = handler.GetResult<T3>();
                            break;
                        case 3:
                            _result.Item4 = handler.GetResult<T4>();
                            break;
                        case 4:
                            _result.Item5 = handler.GetResult<T5>();
                            break;
                    }
                }
            }

            internal static MergePromise<ValueTuple<T1, T2, T3, T4, T5, T6>> GetOrCreateMergePromise<T1, T2, T3, T4, T5, T6>(
                ValueLinkedStack<PromisePassThrough> promisePassThroughs,
#if CSHARP_7_3_OR_NEWER
                in
#endif
                ValueTuple<T1, T2, T3, T4, T5, T6> value,
                int pendingAwaits, ulong completedProgress, ushort depth)
            {
                var promise = MergePromiseTuple<T1, T2, T3, T4, T5, T6>.GetOrCreate();
                promise._result = value;
                promise.Setup(promisePassThroughs, pendingAwaits, completedProgress, depth);
                return promise;
            }

            private sealed class MergePromiseTuple<T1, T2, T3, T4, T5, T6> : MergePromiseT<ValueTuple<T1, T2, T3, T4, T5, T6>>
            {
                [MethodImpl(InlineOption)]
                internal static MergePromiseTuple<T1, T2, T3, T4, T5, T6> GetOrCreate()
                {
                    // We take the base type instead of the concrete type because the base type re-pools with its type.
                    var obj = ObjectPool.TryTakeOrInvalid<MergePromiseT<ValueTuple<T1, T2, T3, T4, T5, T6>>, MergePromiseTuple<T1, T2, T3, T4, T5, T6>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new MergePromiseTuple<T1, T2, T3, T4, T5, T6>()
                        : obj.UnsafeAs<MergePromiseTuple<T1, T2, T3, T4, T5, T6>>();
                }

                protected override void ReadResult(PromiseRefBase handler, int index)
                {
                    switch (index)
                    {
                        case 0:
                            _result.Item1 = handler.GetResult<T1>();
                            break;
                        case 1:
                            _result.Item2 = handler.GetResult<T2>();
                            break;
                        case 2:
                            _result.Item3 = handler.GetResult<T3>();
                            break;
                        case 3:
                            _result.Item4 = handler.GetResult<T4>();
                            break;
                        case 4:
                            _result.Item5 = handler.GetResult<T5>();
                            break;
                        case 5:
                            _result.Item6 = handler.GetResult<T6>();
                            break;
                    }
                }
            }

            internal static MergePromise<ValueTuple<T1, T2, T3, T4, T5, T6, T7>> GetOrCreateMergePromise<T1, T2, T3, T4, T5, T6, T7>(
                ValueLinkedStack<PromisePassThrough> promisePassThroughs,
#if CSHARP_7_3_OR_NEWER
                in
#endif
                ValueTuple<T1, T2, T3, T4, T5, T6, T7> value,
                int pendingAwaits, ulong completedProgress, ushort depth)
            {
                var promise = MergePromiseTuple<T1, T2, T3, T4, T5, T6, T7>.GetOrCreate();
                promise._result = value;
                promise.Setup(promisePassThroughs, pendingAwaits, completedProgress, depth);
                return promise;
            }

            private sealed class MergePromiseTuple<T1, T2, T3, T4, T5, T6, T7> : MergePromiseT<ValueTuple<T1, T2, T3, T4, T5, T6, T7>>
            {
                [MethodImpl(InlineOption)]
                internal static MergePromiseTuple<T1, T2, T3, T4, T5, T6, T7> GetOrCreate()
                {
                    // We take the base type instead of the concrete type because the base type re-pools with its type.
                    var obj = ObjectPool.TryTakeOrInvalid<MergePromiseT<ValueTuple<T1, T2, T3, T4, T5, T6, T7>>, MergePromiseTuple<T1, T2, T3, T4, T5, T6, T7>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new MergePromiseTuple<T1, T2, T3, T4, T5, T6, T7>()
                        : obj.UnsafeAs<MergePromiseTuple<T1, T2, T3, T4, T5, T6, T7>>();
                }

                protected override void ReadResult(PromiseRefBase handler, int index)
                {
                    switch (index)
                    {
                        case 0:
                            _result.Item1 = handler.GetResult<T1>();
                            break;
                        case 1:
                            _result.Item2 = handler.GetResult<T2>();
                            break;
                        case 2:
                            _result.Item3 = handler.GetResult<T3>();
                            break;
                        case 3:
                            _result.Item4 = handler.GetResult<T4>();
                            break;
                        case 4:
                            _result.Item5 = handler.GetResult<T5>();
                            break;
                        case 5:
                            _result.Item6 = handler.GetResult<T6>();
                            break;
                        case 6:
                            _result.Item7 = handler.GetResult<T7>();
                            break;
                    }
                }
            }
        } // class PromiseRefBase
    } // class Internal
}