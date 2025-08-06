﻿#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using Proto.Promises.Linq;
using Proto.Promises.Threading;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Proto.Promises
{
    internal static partial class ParallelAsyncHelper<TSource>
    {
        internal static Promise ForEach<TParallelBody>(AsyncEnumerable<TSource> source, TParallelBody body, ParallelAsyncOptions parallelAsyncOptions)
            where TParallelBody : IFunc<TSource, CancelationToken, Promise>
        {
            var cancelationToken = parallelAsyncOptions.CancelationToken;
            // One fast up-front check for cancelation before we start the whole operation.
            if (cancelationToken.IsCancelationRequested)
            {
                return source.GetAsyncEnumerator().DisposeAsync()
                    .Then(() => Promise.Canceled());
            }

            var promise = Internal.PromiseRefBase.PromiseParallelForEachAsync<TParallelBody, TSource>.GetOrCreate(
                source, body, cancelationToken, parallelAsyncOptions.EffectiveSynchronizationContext, parallelAsyncOptions.EffectiveMaxDegreeOfParallelism);
            promise.MaybeLaunchWorker(true);
            return new Promise(promise);
        }
    }

    partial class Internal
    {
        internal static Promise ParallelForEachAsync<TParallelBody, TSource>(AsyncEnumerable<TSource> enumerable, TParallelBody body, CancelationToken cancelationToken, SynchronizationContext synchronizationContext, int maxDegreeOfParallelism)
            where TParallelBody : IFunc<TSource, CancelationToken, Promise>
        {
            if (maxDegreeOfParallelism == -1)
            {
                maxDegreeOfParallelism = Environment.ProcessorCount;
            }
            else if (maxDegreeOfParallelism < 1)
            {
                enumerable.GetAsyncEnumerator().DisposeAsync().Forget();
                throw new ArgumentOutOfRangeException(nameof(maxDegreeOfParallelism), "maxDegreeOfParallelism must be positive, or -1 for default (Environment.ProcessorCount). Actual: " + maxDegreeOfParallelism, GetFormattedStacktrace(2));
            }

            // One fast up-front check for cancelation before we start the whole operation.
            if (cancelationToken.IsCancelationRequested)
            {
                return enumerable.GetAsyncEnumerator().DisposeAsync()
                    .Then(() => Promise.Canceled());
            }

            var promise = PromiseRefBase.PromiseParallelForEachAsync<TParallelBody, TSource>.GetOrCreate(
                enumerable, body, cancelationToken, synchronizationContext, maxDegreeOfParallelism);
            promise.MaybeLaunchWorker(true);
            return new Promise(promise, promise.Id);
        }

        partial class PromiseRefBase
        {
            // Inheriting PromiseSingleAwait<VoidResult> instead of PromiseRefBase so we can take advantage of the already implemented methods.
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal sealed partial class PromiseParallelForEachAsync<TParallelBody, TSource> : SingleAwaitPromise<VoidResult>, ICancelable
                where TParallelBody : IFunc<TSource, CancelationToken, Promise>
            {
                private TParallelBody _body;
                private AsyncEnumerator<TSource> _asyncEnumerator;
                private CancelationRegistration _externalCancelationRegistration;
                // Use the CancelationRef directly instead of CancelationSource struct to save memory.
                private CancelationRef _cancelationRef;
                private ExecutionContext _executionContext;
                private int _remainingAvailableWorkers;
                private int _waitCounter;
                private List<Exception> _exceptions;
                private Promise.State _completionState;
                private bool _stopExecuting;
                // We need an async lock to lock around MoveNextAsync.
                // We just use a counter instead of AsyncLock, and implement the locking algorithm directly.
                // This is possible because we only use the lock once in a single code path.
                // We also need to store whether another worker needs to be launched when a worker is waiting to continue asynchronously from MoveNextAsync.
                // These both need to be updated atomically, so we store 2 Int32s in a single Int64 for use with Interlocked.
                private long _lockAndLaunchNext;

                private PromiseParallelForEachAsync() { }

                [MethodImpl(InlineOption)]
                private static PromiseParallelForEachAsync<TParallelBody, TSource> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<PromiseParallelForEachAsync<TParallelBody, TSource>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new PromiseParallelForEachAsync<TParallelBody, TSource>()
                        : obj.UnsafeAs<PromiseParallelForEachAsync<TParallelBody, TSource>>();
                }

                internal static PromiseParallelForEachAsync<TParallelBody, TSource> GetOrCreate(
                    AsyncEnumerable<TSource> enumerable, TParallelBody body, CancelationToken cancelationToken, SynchronizationContext synchronizationContext, int maxDegreeOfParallelism)
                {
                    var promise = GetOrCreate();
                    promise.Reset();
                    promise._body = body;
                    promise.ContinuationContext = synchronizationContext ?? BackgroundSynchronizationContextSentinel.s_instance;
                    promise._remainingAvailableWorkers = maxDegreeOfParallelism;
                    promise._completionState = Promise.State.Resolved;
                    promise._stopExecuting = false;
                    promise._lockAndLaunchNext = 0;
                    var cancelRef = CancelationRef.GetOrCreate();
                    promise._cancelationRef = cancelRef;
                    promise._externalCancelationRegistration = cancelationToken.Register<ICancelable>(promise);
                    promise._asyncEnumerator = enumerable.GetAsyncEnumerator(new CancelationToken(cancelRef, cancelRef.TokenId));
                    if (Promise.Config.AsyncFlowExecutionContextEnabled)
                    {
                        promise._executionContext = ExecutionContext.Capture();
                    }
                    return promise;
                }

                internal override void MaybeDispose()
                {
                    ValidateNoPending();
                    Dispose();
                    _body = default;
                    _executionContext = null;
                    ObjectPool.MaybeRepool(this);
                }

                public void Cancel()
                {
                    _completionState = Promise.State.Canceled;
                    CancelWorkers();
                }

                private bool TryEnterLockAndStoreLaunchNext()
                {
                    long current = Volatile.Read(ref _lockAndLaunchNext);
                    while (true)
                    {
                        unchecked
                        {
                            int oldLockCount = (int) current;
                            // We increment the lock count and store the count in both the lower and higher 32 bits.
                            // This way when the lock is exited, it will know if the next worker needs to be launched.
                            // Only 1 worker will call this at a time, so we don't have to worry about 2 workers overwriting the stored value.
                            long newLockCount = oldLockCount + 1L;
                            long newValue = newLockCount | (newLockCount << 32);
                            long oldValue = Interlocked.CompareExchange(ref _lockAndLaunchNext, newValue, current);
                            if (oldValue == current)
                            {
                                return oldLockCount == 0;
                            }
                            current = oldValue;
                        }
                    }
                }

                [MethodImpl(InlineOption)]
                private bool TryEnterLock()
                {
                    unchecked
                    {
                        return ((int) Interlocked.Increment(ref _lockAndLaunchNext)) == 1;
                    }
                }

                [MethodImpl(InlineOption)]
                private bool ExitLockAndGetLaunchNext()
                {
                    unchecked
                    {
                        // We subtract 1 from both the lower and higher 32 bits.
                        const long decrement = -((1L << 32) + 1L);
                        long newValue = Interlocked.Add(ref _lockAndLaunchNext, decrement);

                        if ((int) newValue > 0)
                        {
                            // Another worker is waiting on the lock, schedule it on the context and jump to inside lock.
                            ScheduleContextCallback(ContinuationContext.UnsafeAs<SynchronizationContext>(), this,
                                obj => obj.UnsafeAs<PromiseParallelForEachAsync<TParallelBody, TSource>>().ExecuteWorkerInsideLock(),
                                obj => obj.UnsafeAs<PromiseParallelForEachAsync<TParallelBody, TSource>>().ExecuteWorkerInsideLock()
                            );
                        }
                        // If the new value of the higher 32 bits is 0, it's time to launch the next worker.
                        return (newValue >> 32) == 0L;
                    }
                }

                private void ExitLockComplete()
                {
                    _stopExecuting = true;
                    // There are no more iterations to execute. We have full control of the lock,
                    // so we don't need to schedule continuations, and we simply subtract all waiting workers.
                    unchecked
                    {
                        int completions = (int) Interlocked.Exchange(ref _lockAndLaunchNext, 0L);
                        MaybeComplete(completions);
                    }
                }

                internal void MaybeLaunchWorker(bool launchWorker)
                {
                    if (launchWorker & _remainingAvailableWorkers > 0)
                    {
                        --_remainingAvailableWorkers;
                        // We add to the wait counter before we run the worker to resolve a race condition where the counter could hit zero prematurely.
                        InterlockedAddWithUnsignedOverflowCheck(ref _waitCounter, 1);

                        ScheduleContextCallback(ContinuationContext.UnsafeAs<SynchronizationContext>(), this,
                            obj => obj.UnsafeAs<PromiseParallelForEachAsync<TParallelBody, TSource>>().ExecuteWorkerAndLaunchNext(),
                            obj => obj.UnsafeAs<PromiseParallelForEachAsync<TParallelBody, TSource>>().ExecuteWorkerAndLaunchNext()
                        );
                    }
                }

                private void ExecuteWorkerAndLaunchNext()
                {
                    // We do the more expensive lock enter here where we know it's necessary, without adding an extra branch in the worker body.
                    // This also helps us avoid the cost of writing the contexts when the lock is unavailable.
                    if (TryEnterLockAndStoreLaunchNext())
                    {
                        ExecuteWorkerInsideLock();
                    }
                    // If the TryEnter failed, when the other worker exits the lock, it will schedule this worker to continue.
                }

                private void ExecuteWorkerWithoutLaunchNext()
                {
                    // We enter the lock here before calling the main worker body, so we can avoid adding an extra branch in the worker body,
                    // and avoid the cost of writing the contexts when the lock is unavailable.
                    if (TryEnterLock())
                    {
                        ExecuteWorkerInsideLock();
                    }
                }

                private void ExecuteWorkerInsideLock()
                {
                    if (_executionContext == null)
                    {
                        ExecuteWorker(false);
                    }
                    else
                    {
                        ExecutionContext.Run(
                            // .Net Framework doesn't allow us to re-use a captured context, so we have to copy it for each invocation.
                            // .Net Core's implementation of CreateCopy returns itself, so this is always as efficient as it can be.
                            _executionContext.CreateCopy(),
                            obj => obj.UnsafeAs<PromiseParallelForEachAsync<TParallelBody, TSource>>().ExecuteWorker(false),
                            this
                        );
                    }
                }

                private void ExecuteWorkerAfterMoveNext()
                {
                    if (_executionContext == null)
                    {
                        ExecuteWorker(true);
                    }
                    else
                    {
                        ExecutionContext.Run(
                            // .Net Framework doesn't allow us to re-use a captured context, so we have to copy it for each invocation.
                            // .Net Core's implementation of CreateCopy returns itself, so this is always as efficient as it can be.
                            _executionContext.CreateCopy(),
                            obj => obj.UnsafeAs<PromiseParallelForEachAsync<TParallelBody, TSource>>().ExecuteWorker(true),
                            this
                        );
                    }
                }

                private void ExecuteWorker(bool fromMoveNext)
                {
                    SetCurrentInvoker(this);
                    try
                    {
                        WorkerBody(fromMoveNext);
                    }
                    catch (OperationCanceledException)
                    {
                        _completionState = Promise.State.Canceled;
                        CancelWorkersAndMaybeComplete(1);
                    }
                    catch (Exception e)
                    {
                        // Record the failure and then don't let the exception propagate. The last worker to complete
                        // will propagate exceptions as is appropriate to the top-level promise.
                        RecordException(e);
                        CancelWorkersAndMaybeComplete(1);
                    }
                    ClearCurrentInvoker();
                }

                private void WorkerBody(bool fromMoveNext)
                {
                    // The worker body. Each worker will execute this same body.

                    if (fromMoveNext)
                    {
                        goto AfterMoveNext;
                    }

                    // We use goto LoopStart instead of while(true) so that we can jump to the proper place before the loop starts.
                LoopStart:
                    // Get the next element from the enumerator. This requires locking around MoveNextAsync/Current.
                    // We already have acquired the lock at this point, either from the caller, or from the end of the loop.
                    if (_stopExecuting)
                    {
                        ExitLockComplete();
                        return;
                    }

                    var moveNextPromise = _asyncEnumerator.MoveNextAsync();
                    bool hasValue;
                    if (moveNextPromise._ref == null)
                    {
                        hasValue = moveNextPromise._result;
                    }
                    else
                    {
                        // We check for resolved state because the implementation always uses a backing reference
                        // (except when the iteration is complete) that may complete synchronously.
                        if (moveNextPromise._ref.State != Promise.State.Resolved)
                        {
                            // The promise may still be pending, hook this up to continue when it completes.
                            AddPending(moveNextPromise._ref);
                            moveNextPromise._ref.HookupExistingWaiter(moveNextPromise._id, this);
                            return;
                        }
                        hasValue = moveNextPromise._ref._result;
                        moveNextPromise.Forget();
                    }

                    if (!hasValue)
                    {
                        ExitLockComplete();
                        return;
                    }

                AfterMoveNext:
                    var element = _asyncEnumerator.Current;
                    bool launchNext = ExitLockAndGetLaunchNext();

                    // If the available workers allows it and we've not yet queued the next worker, do so now.  We wait
                    // until after we've grabbed an item from the enumerator to a) avoid unnecessary contention on the
                    // serialized resource, and b) avoid queueing another work if there aren't any more items.  Each worker
                    // is responsible only for creating the next worker, which in turn means there can't be any contention
                    // on creating workers (though it's possible one worker could be executing while we're creating the next).
                    MaybeLaunchWorker(launchNext);

                    // Process the loop body.
                    var promise = _body.Invoke(element, new CancelationToken(_cancelationRef, _cancelationRef.TokenId));
                    ValidateReturn(promise);

                    if (promise._ref != null)
                    {
                        // The promise may still be pending, hook this up to rerun the loop when it completes.
                        AddPending(promise._ref);
                        promise._ref.HookupExistingWaiter(promise._id, this);
                        return;
                    }

                    // The promise was already complete successfully. Rerun the loop synchronously.

                    // We enter the lock at the end of the loop instead of the start so we can avoid
                    // ExecutionContext copy in case the lock is unavailable, and so we can avoid
                    // an extra branch at the start of the method.
                    if (!TryEnterLock())
                    {
                        // When the other worker exits the lock, it will schedule this worker to continue.
                        return;
                    }
                    goto LoopStart;
                }

                internal override void Handle(PromiseRefBase handler, Promise.State state)
                {
                    RemoveComplete(handler);
                    var rejectContainer = handler.RejectContainer;
                    handler.SuppressRejection = true;
                    handler.SetCompletionState(state);
                    handler.MaybeDispose();

                    // We hook this up to the MoveNextAsync promise and the parallel body promise,
                    // so we need to check which one completed.
                    bool isMoveNextAsyncContinuation = handler == _asyncEnumerator._target;

                    if (state == Promise.State.Resolved)
                    {
                        if (!isMoveNextAsyncContinuation)
                        {
                            // Schedule the worker body to run again on the context, but without launching another worker.
                            ScheduleContextCallback(ContinuationContext.UnsafeAs<SynchronizationContext>(), this,
                                obj => obj.UnsafeAs<PromiseParallelForEachAsync<TParallelBody, TSource>>().ExecuteWorkerWithoutLaunchNext(),
                                obj => obj.UnsafeAs<PromiseParallelForEachAsync<TParallelBody, TSource>>().ExecuteWorkerWithoutLaunchNext()
                            );
                            return;
                        }
                        if (!handler.GetResult<bool>())
                        {
                            ExitLockComplete();
                            return;
                        }
                        // Schedule the worker body to jump to after move next.
                        ScheduleContextCallback(ContinuationContext.UnsafeAs<SynchronizationContext>(), this,
                                obj => obj.UnsafeAs<PromiseParallelForEachAsync<TParallelBody, TSource>>().ExecuteWorkerAfterMoveNext(),
                                obj => obj.UnsafeAs<PromiseParallelForEachAsync<TParallelBody, TSource>>().ExecuteWorkerAfterMoveNext()
                        );
                        return;
                    }

                    _completionState = state;
                    CancelWorkers();
                    if (state == Promise.State.Rejected)
                    {
                        // Record the failure. The last worker to complete will propagate exceptions as is appropriate to the top-level promise.
                        RecordException(rejectContainer.GetValueAsException());
                    }
                    if (isMoveNextAsyncContinuation)
                    {
                        ExitLockComplete();
                    }
                    else
                    {
                        MaybeComplete(1);
                    }
                }

                new private void RecordException(Exception e)
                {
                    lock (this)
                    {
                        Internal.RecordException(e, ref _exceptions);
                    }
                }

                private void CancelWorkers()
                {
                    _stopExecuting = true;
                    // We cancel the source to notify the workers that they don't need to continue processing.
                    // This may be called multiple times. It's fine because it checks internally if it's already canceled.
                    try
                    {
                        _cancelationRef.CancelUnsafe();
                    }
                    catch (Exception e)
                    {
                        RecordException(e);
                    }
                }

                private void CancelWorkersAndMaybeComplete(int completeCount)
                {
                    CancelWorkers();
                    MaybeComplete(completeCount);
                }

                private void MaybeComplete(int completeCount)
                {
                    // If we're the last worker to complete, clean up and complete the operation.
                    if (InterlockedAddWithUnsignedOverflowCheck(ref _waitCounter, -completeCount) != 0)
                    {
                        return;
                    }

                    Promise disposePromise;
                    try
                    {
                        disposePromise = _asyncEnumerator.DisposeAsync();
                    }
                    catch (Exception e)
                    {
                        RecordException(e);
                        OnComplete();
                        return;
                    }

                    HookUpDisposeAsync(disposePromise);
                }

                private void OnComplete()
                {
                    _externalCancelationRegistration.Dispose();
                    _externalCancelationRegistration = default;
                    _cancelationRef.DisposeUnsafe();
                    _cancelationRef = null;

                    // Finally, complete the promise returned to the ParallelForEachAsync caller.
                    // This must be the very last thing done.
                    if (_exceptions != null)
                    {
                        RejectContainer = CreateRejectContainer(new AggregateException(_exceptions), int.MinValue, null, this);
                        _exceptions = null;
                        HandleNextInternal(Promise.State.Rejected);
                    }
                    else
                    {
                        HandleNextInternal(_completionState);
                    }
                }

                [MethodImpl(InlineOption)]
                private void HookUpDisposeAsync(Promise disposePromise)
                {
                    if (disposePromise._ref == null)
                    {
                        OnComplete();
                        return;
                    }
                    var state = disposePromise._ref.State;
                    if (state == Promise.State.Resolved | state == Promise.State.Canceled)
                    {
                        disposePromise.Forget();
                        // Canceled = 3 and Resolved = 1, this happens to work with | to not overwrite the canceled state if this state is resolved.
                        _completionState |= state;
                        OnComplete();
                        return;
                    }
                    // Pending or Rejected

                    // We're already hooking this up directly to the MoveNextAsync promise and the loop body promise,
                    // Adding a 3rd direct hookup for DisposeAsync which is only called once would add extra overhead to the others that are called multiple times.
                    // Instead, we use a PromisePassThrough.

                    AddPending(disposePromise._ref);
                    var passthrough = PromisePassThrough.GetOrCreate(disposePromise._ref, this, 0);
                    disposePromise._ref.HookupNewWaiter(disposePromise._id, passthrough);
                }

                internal override void Handle(PromiseRefBase handler, Promise.State state, int index)
                {
                    RemoveComplete(handler);
                    handler.SetCompletionState(state);
                    if (state == Promise.State.Rejected)
                    {
                        RecordException(handler.RejectContainer.GetValueAsException());
                        handler.SuppressRejection = true;
                    }
                    handler.MaybeDispose();
                    // Canceled = 3 and Resolved = 1, this happens to work with | to not overwrite the canceled state if this state is resolved.
                    _completionState |= state;
                    OnComplete();
                }

                partial void ValidateNoPending();
                partial void AddPending(PromiseRefBase pendingPromise);
                partial void RemoveComplete(PromiseRefBase completePromise);
            } // class PromiseParallelForEach
        } // class PromiseRefBase
    } // class Internal
}