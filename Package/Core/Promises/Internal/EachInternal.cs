#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#pragma warning disable IDE0090 // Use 'new(...)'

using Proto.Promises.Linq;
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
            internal sealed partial class PromiseEachAsyncEnumerable<TResult> : AsyncEnumerableWithIterator<TResult>
            {
                private static GetResultDelegate<TResult> s_getResult;

                // These must not be readonly.
                private DeferredPromise<VoidResult> _queuePromise;
                private PoolBackedQueue<TResult> _queue;
                private int _remaining;
                private int _retainCount;

                private PromiseEachAsyncEnumerable() { }

                [MethodImpl(InlineOption)]
                private static PromiseEachAsyncEnumerable<TResult> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<PromiseEachAsyncEnumerable<TResult>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new PromiseEachAsyncEnumerable<TResult>()
                        : obj.UnsafeAs<PromiseEachAsyncEnumerable<TResult>>();
                }

                [MethodImpl(InlineOption)]
                internal static PromiseEachAsyncEnumerable<TResult> GetOrCreate(GetResultDelegate<TResult> getResultDelegate)
                {
                    s_getResult = getResultDelegate;

                    var enumerable = GetOrCreate();
                    enumerable.Reset();
                    enumerable._remaining = 0;
                    enumerable._retainCount = 1;
                    enumerable._queue = new PoolBackedQueue<TResult>(0);
                    return enumerable;
                }

                internal void AddPromise(PromiseRefBase promise, short id)
                {
                    AddPending(promise);

                    ++_remaining;
                    InterlockedAddWithUnsignedOverflowCheck(ref _retainCount, 1);
                    // The base AsyncEnumerableWithIterator type has a sealed Handle implementation for the async iterator,
                    // so we have to create a passthrough to hook the promise up to this, even though the index isn't used.
                    var passthrough = PromisePassThrough.GetOrCreate(promise, this, 0);
                    promise.HookupNewWaiter(id, passthrough);
                }

                [MethodImpl(InlineOption)]
                internal void AddResult(in TResult result)
                {
                    ++_remaining;
                    lock (this)
                    {
                        _queue.Enqueue(result);
                    }
                }

                internal override void Handle(PromiseRefBase handler, Promise.State state, int index)
                {
                    RemoveComplete(handler);

                    handler.SetCompletionState(state);
                    TResult result = default;
                    s_getResult.Invoke(handler, 0, ref result);
                    handler.MaybeDispose();

                    DeferredPromise<VoidResult> queuePromise;
                    lock (this)
                    {
                        _queue.Enqueue(result);
                        queuePromise = _queuePromise;
                        _queuePromise = null;
                    }
                    DisposeAndReturnToPool();
                    queuePromise?.ResolveDirectVoid();
                }

                protected override void DisposeAndReturnToPool()
                {
                    if (InterlockedAddWithUnsignedOverflowCheck(ref _retainCount, -1) == 0)
                    {
                        ValidateNoPending();

                        Dispose();
                        _queue.Dispose();
                        ObjectPool.MaybeRepool(this);
                    }
                }

                protected override Promise DisposeAsyncWithoutStart()
                {
                    DisposeAndReturnToPool();
                    return Promise.Resolved();
                }

                internal override void MaybeDispose()
                {
                    // This is called on every MoveNextAsync, we only fully dispose and return to pool after DisposeAsync is called.
                    if (_disposed)
                    {
                        DisposeAndReturnToPool();
                    }
                }

                protected override void Start(int enumerableId)
                {
                    var iteratorPromise = Iterate(enumerableId)._promise;
                    if (iteratorPromise._ref == null)
                    {
                        // Already complete.
                        HandleFromSynchronouslyCompletedIterator();
                        return;
                    }

                    this.SetPrevious(iteratorPromise._ref);
                    // We hook this up directly to the returned promise so we can know when the iteration is complete, and use this for the DisposeAsync promise.
                    iteratorPromise._ref.HookupExistingWaiter(iteratorPromise._id, this);
                }

                // TODO: we may be able to optimize this by handling the logic directly in MoveNextAsync instead of using an async function and queuePromise.
                private async AsyncIteratorMethod Iterate(int streamWriterId)
                {
                    do
                    {
                        _cancelationToken.ThrowIfCancelationRequested();

                        DeferredPromise<VoidResult> queuePromise;
                        TResult result;
                        lock (this)
                        {
                            if (_queue.TryDequeue(out result))
                            {
                                goto YieldResult;
                            }
                            _queuePromise = queuePromise = DeferredPromise<VoidResult>.GetOrCreate();
                        }
                        await new Promise(queuePromise, queuePromise.Id).WaitAsync(_cancelationToken);
                        lock (this)
                        {
                            result = _queue.Dequeue();
                        }

                    YieldResult:
                        await YieldAsync(result, streamWriterId);
                    } while (--_remaining != 0);
                }

                partial void AddPending(PromiseRefBase pendingPromise);
                partial void RemoveComplete(PromiseRefBase completePromise);
                partial void ValidateNoPending();
            }

#if PROMISE_DEBUG
            partial class PromiseEachAsyncEnumerable<TResult>
            {
                private readonly HashSet<PromiseRefBase> _pendingPromises = new HashSet<PromiseRefBase>();

                protected override void BorrowPreviousPromises(Stack<PromiseRefBase> borrower)
                {
                    lock (_pendingPromises)
                    {
                        foreach (var promiseRef in _pendingPromises)
                        {
                            borrower.Push(promiseRef);
                        }
                    }
                }

                partial void ValidateNoPending()
                {
                    lock (_pendingPromises)
                    {
                        if (_pendingPromises.Count != 0)
                        {
                            throw new System.InvalidOperationException("PromiseEachAsyncEnumerable disposed with pending promises.");
                        }
                    }
                }

                partial void AddPending(PromiseRefBase pendingPromise)
                {
                    lock (_pendingPromises)
                    {
                        _pendingPromises.Add(pendingPromise);
                    }
                }

                partial void RemoveComplete(PromiseRefBase completePromise)
                {
                    lock (_pendingPromises)
                    {
                        _pendingPromises.Remove(completePromise);
                    }
                }
            }
#endif
        } // class PromiseRefBase

        [MethodImpl(InlineOption)]
        internal static PromiseRefBase.PromiseEachAsyncEnumerable<Promise<T>.ResultContainer> GetOrCreatePromiseEachAsyncEnumerable<T>()
            => PromiseRefBase.PromiseEachAsyncEnumerable<Promise<T>.ResultContainer>.GetOrCreate(Promise.MergeResultFuncs.GetMergeResult<T>());

        [MethodImpl(InlineOption)]
        internal static PromiseRefBase.PromiseEachAsyncEnumerable<Promise.ResultContainer> GetOrCreatePromiseEachAsyncEnumerableVoid()
            => PromiseRefBase.PromiseEachAsyncEnumerable<Promise.ResultContainer>.GetOrCreate(Promise.MergeResultFuncs.GetMergeResultVoid());
    } // class Internal
}