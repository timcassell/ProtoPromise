﻿#if UNITY_5_5 || NET_2_0 || NET_2_0_SUBSET
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

#pragma warning disable IDE0034 // Simplify 'default' expression
#pragma warning disable IDE0074 // Use compound assignment
#pragma warning disable IDE0250 // Make struct 'readonly'
#pragma warning disable CA1507 // Use nameof to express symbol names

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Proto.Promises
{
    partial class Internal
    {
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal struct ForLoopEnumerator : IEnumerator<int>
        {
            private int _current;
            private readonly int _toIndex;

            [MethodImpl(InlineOption)]
            internal ForLoopEnumerator(int fromIndex, int toIndex)
            {
                _current = fromIndex;
                _toIndex = toIndex;
            }

            public int Current
            {
                [MethodImpl(InlineOption)]
                get
                {
                    unchecked
                    {
                        return _current++;
                    }
                }
            }

            object IEnumerator.Current { get { return Current; } }

            [MethodImpl(InlineOption)]
            public bool MoveNext()
            {
                // We just check if the index can be incremented.
                // We don't do the actual MoveNext until Current is called to avoid an extra branch.
                return _current < _toIndex;
            }

            [MethodImpl(InlineOption)]
            void IDisposable.Dispose() { }

            void IEnumerator.Reset()
            {
                throw new NotImplementedException();
            }
        }

        internal interface IParallelBody<TSource>
        {
            Promise Invoke(TSource source, CancelationToken cancelationToken);
        }

        internal struct ParallelBody<TSource> : IParallelBody<TSource>
        {
            private readonly Func<TSource, CancelationToken, Promise> _body;

            [MethodImpl(InlineOption)]
            internal ParallelBody(Func<TSource, CancelationToken, Promise> body)
            {
                _body = body;
            }

            [MethodImpl(InlineOption)]
            Promise IParallelBody<TSource>.Invoke(TSource source, CancelationToken cancelationToken)
            {
                return _body.Invoke(source, cancelationToken);
            }
        }

        internal struct ParallelCaptureBody<TSource, TCapture> : IParallelBody<TSource>
        {
            private readonly Func<TSource, TCapture, CancelationToken, Promise> _body;
            private readonly TCapture _capturedValue;

            [MethodImpl(InlineOption)]
            internal ParallelCaptureBody(TCapture capturedValue, Func<TSource, TCapture, CancelationToken, Promise> body)
            {
                _capturedValue = capturedValue;
                _body = body;
            }

            [MethodImpl(InlineOption)]
            Promise IParallelBody<TSource>.Invoke(TSource source, CancelationToken cancelationToken)
            {
                return _body.Invoke(source, _capturedValue, cancelationToken);
            }
        }

        internal static Promise ParallelForEach<TEnumerator, TParallelBody, TSource>(TEnumerator enumerator, TParallelBody body, CancelationToken cancelationToken, SynchronizationContext synchronizationContext, int maxDegreeOfParallelism)
            where TEnumerator : IEnumerator<TSource>
            where TParallelBody : IParallelBody<TSource>
        {
            if (maxDegreeOfParallelism == -1)
            {
                maxDegreeOfParallelism = Environment.ProcessorCount;
            }
            else if (maxDegreeOfParallelism < 1)
            {
                enumerator.Dispose();
                throw new ArgumentOutOfRangeException("maxDegreeOfParallelism", "maxDegreeOfParallelism must be positive, or -1 for default (Environment.ProcessorCount). Actual: " + maxDegreeOfParallelism, GetFormattedStacktrace(2));
            }

            // One fast up-front check for cancelation before we start the whole operation.
            if (cancelationToken.IsCancelationRequested)
            {
                enumerator.Dispose();
                return Promise.Canceled();
            }

            try
            {
                var promise = PromiseRefBase.PromiseParallelForEach<TEnumerator, TParallelBody, TSource>.GetOrCreate(enumerator, body, cancelationToken, synchronizationContext, maxDegreeOfParallelism);
                promise.MaybeLaunchWorker(true);
                return new Promise(promise, promise.Id, 0);
            }
            catch (Exception e)
            {
                return Promise.Rejected(e);
            }
        }

        partial class PromiseRefBase
        {
            // Inheriting PromiseSingleAwait<TEnumerator> instead of PromiseRefBase so we can take advantage of the already implemented methods.
            // We store the enumerator in the _result field to save space, because this type is only used in `Promise`, not `Promise<T>`, so the result doesn't matter.
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal sealed class PromiseParallelForEach<TEnumerator, TParallelBody, TSource> : PromiseSingleAwait<TEnumerator>, ICancelable
                where TEnumerator : IEnumerator<TSource>
                where TParallelBody : IParallelBody<TSource>
            {
                private TParallelBody _body;
                private CancelationRegistration _externalCancelationRegistration;
                // Use the CancelationRef directly instead of CancelationSource struct to save memory.
                private Internal.CancelationRef _cancelationRef;
                private SynchronizationContext _synchronizationContext;
                private int _remainingAvailableWorkers;
                private int _waitCounter;
                private List<Exception> _exceptions;
                private Promise.State _completionState;

                private PromiseParallelForEach() { }

                [MethodImpl(InlineOption)]
                private static PromiseParallelForEach<TEnumerator, TParallelBody, TSource> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<PromiseParallelForEach<TEnumerator, TParallelBody, TSource>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new PromiseParallelForEach<TEnumerator, TParallelBody, TSource>()
                        : obj.UnsafeAs<PromiseParallelForEach<TEnumerator, TParallelBody, TSource>>();
                }

                internal static PromiseParallelForEach<TEnumerator, TParallelBody, TSource> GetOrCreate(TEnumerator enumerator, TParallelBody body, CancelationToken cancelationToken, SynchronizationContext synchronizationContext, int maxDegreeOfParallelism)
                {
                    var promise = GetOrCreate();
                    promise.Reset();
                    promise._result = enumerator;
                    promise._body = body;
                    promise._synchronizationContext = synchronizationContext ?? BackgroundSynchronizationContextSentinel.s_instance;
                    promise._remainingAvailableWorkers = maxDegreeOfParallelism;
                    promise._completionState = Promise.State.Resolved;
                    promise._cancelationRef = CancelationRef.GetOrCreate();
                    cancelationToken.TryRegister(promise, out promise._externalCancelationRegistration);
                    return promise;
                }

                internal override void MaybeDispose()
                {
                    Dispose();
                    _body = default(TParallelBody);
                    _synchronizationContext = null;
                    ObjectPool.MaybeRepool(this);
                }

                public void Cancel()
                {
                    _completionState = Promise.State.Canceled;
                    _cancelationRef.Cancel();
                }

                internal void MaybeLaunchWorker(bool launchWorker)
                {
                    if (launchWorker & _remainingAvailableWorkers > 0)
                    {
                        --_remainingAvailableWorkers;
                        // We add to the wait counter before we run the worker to resolve a race condition where the counter could hit zero prematurely.
                        InterlockedAddWithUnsignedOverflowCheck(ref _waitCounter, 1);

                        ScheduleContextCallback(_synchronizationContext, this,
                            obj => obj.UnsafeAs<PromiseParallelForEach<TEnumerator, TParallelBody, TSource>>().ExecuteWorker(true),
                            obj => obj.UnsafeAs<PromiseParallelForEach<TEnumerator, TParallelBody, TSource>>().ExecuteWorker(true)
                        );
                    }
                }

                private void ExecuteWorker(bool launchNext)
                {
                    var currentContext = ts_currentContext;
                    ts_currentContext = _synchronizationContext;
                    
                    SetCurrentInvoker(this);
                    try
                    {
                        WorkerBody(launchNext);
                    }
                    catch (OperationCanceledException)
                    {
                        Cancel();
                        MaybeComplete();
                    }
                    catch (Exception e)
                    {
                        // Record the failure and then don't let the exception propagate. The last worker to complete
                        // will propagate exceptions as is appropriate to the top-level promise.
                        RecordException(e);
                        MaybeComplete();
                    }
                    ClearCurrentInvoker();

                    ts_currentContext = currentContext;
                }

                private void WorkerBody(bool launchNext)
                {
                    // The worker body. Each worker will execute this same body.
                    while (true)
                    {
                        if (_cancelationRef._state >= CancelationRef.State.Canceled)
                        {
                            MaybeComplete();
                            return;
                        }

                        // Get the next element from the enumerator. This requires locking around MoveNext/Current.
                        TSource element;
                        lock (this)
                        {
                            if (!_result.MoveNext())
                            {
                                // We cancel the source to notify completion.
                                _cancelationRef.Cancel();
                                MaybeComplete();
                                return;
                            }

                            element = _result.Current;
                        }

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
                            promise._ref.HookupExistingWaiter(promise._id, this);
                            return;
                        }

                        // The promise was already complete successfully. Rerun the loop synchronously without launching another worker.
                        launchNext = false;
                    }
                }

                internal override void Handle(PromiseRefBase handler, object rejectContainer, Promise.State state)
                {
                    handler.SetCompletionState(rejectContainer, state);
                    handler.MaybeDispose();

                    if (state == Promise.State.Resolved)
                    {
                        // Schedule the worker body to run again on the context, but without launching another worker.
                        ScheduleContextCallback(_synchronizationContext, this,
                            obj => obj.UnsafeAs<PromiseParallelForEach<TEnumerator, TParallelBody, TSource>>().ExecuteWorker(false),
                            obj => obj.UnsafeAs<PromiseParallelForEach<TEnumerator, TParallelBody, TSource>>().ExecuteWorker(false)
                        );
                    }
                    else if (state == Promise.State.Canceled)
                    {
                        Cancel();
                        MaybeComplete();
                    }
                    else
                    {
                        // Record the failure. The last worker to complete will propagate exceptions as is appropriate to the top-level promise.
                        var container = rejectContainer.UnsafeAs<IRejectContainer>();
                        var exception = container.Value as Exception
                            // If the reason was not an exception, get the reason wrapped in an exception.
                            ?? container.GetExceptionDispatchInfo().SourceException;
                        RecordException(exception);
                        MaybeComplete();
                    }
                }

                private void RecordException(Exception e)
                {
                    _cancelationRef.Cancel();

                    lock (this)
                    {
                        if (_exceptions == null)
                        {
                            _exceptions = new List<Exception>();
                        }
                        _exceptions.Add(e);
                    }
                }

                private void MaybeComplete()
                {
                    // If we're the last worker to complete, clean up and complete the operation.
                    if (InterlockedAddWithUnsignedOverflowCheck(ref _waitCounter, -1) == 0)
                    {
                        _externalCancelationRegistration.Dispose();
                        _externalCancelationRegistration = default(CancelationRegistration);
                        _cancelationRef.TryDispose(_cancelationRef.SourceId);
                        _cancelationRef = null;

                        try
                        {
                            _result.Dispose();
                        }
                        catch (Exception e)
                        {
                            RecordException(e);
                        }

                        // Finally, complete the promise returned to the ParallelForEach caller.
                        // This must be the very last thing done.
                        if (_exceptions != null)
                        {
                            var rejectContainer = CreateRejectContainer(new AggregateException(_exceptions), int.MinValue, null, this);
                            _exceptions = null;
                            HandleNextInternal(rejectContainer, Promise.State.Rejected);
                        }
                        else
                        {
                            HandleNextInternal(null, _completionState);
                        }
                    }
                }
            } // class PromiseParallelForEach
        } // class PromiseRefBase
    } // class Internal
}