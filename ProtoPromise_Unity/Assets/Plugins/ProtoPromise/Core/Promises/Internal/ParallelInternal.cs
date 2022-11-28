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

#pragma warning disable IDE0034 // Simplify 'default' expression
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
            private int _currentForIterator;
            private int _currentForIncrement;
            private readonly int _toIndex;

            internal ForLoopEnumerator(int fromIndex, int toIndex)
            {
                _currentForIterator = 0;
                _currentForIncrement = fromIndex;
                _toIndex = toIndex;
            }

            int IEnumerator<int>.Current { get { return _currentForIterator; } }

            object IEnumerator.Current { get { return _currentForIterator; } }

            bool IEnumerator.MoveNext()
            {
                // We have to check if it's already complete before incrementing to prevent overflow.
                if (_currentForIncrement >= _toIndex)
                {
                    return false;
                }
                _currentForIterator = _currentForIncrement;
                unchecked
                {
                    ++_currentForIncrement;
                }
                return _currentForIterator < _toIndex;
            }

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

            internal ParallelBody(Func<TSource, CancelationToken, Promise> body)
            {
                _body = body;
            }

            Promise IParallelBody<TSource>.Invoke(TSource source, CancelationToken cancelationToken)
            {
                return _body.Invoke(source, cancelationToken);
            }
        }

        internal struct ParallelCaptureBody<TSource, TCapture> : IParallelBody<TSource>
        {
            private readonly Func<TSource, TCapture, CancelationToken, Promise> _body;
            private readonly TCapture _capturedValue;

            internal ParallelCaptureBody(TCapture capturedValue, Func<TSource, TCapture, CancelationToken, Promise> body)
            {
                _capturedValue = capturedValue;
                _body = body;
            }

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
                private CancelationSource _cancelationSource;
                private SynchronizationContext _synchronizationContext;
                private int _remainingAvailableWorkers;
                private int _waitCounter;
                private List<Exception> _exceptions;
                private bool _wasCanceled;

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
                    promise._wasCanceled = false;
                    promise._cancelationSource = CancelationSource.New();
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
                    _wasCanceled = true;
                    _cancelationSource.TryCancel();
                }

                internal void MaybeLaunchWorker(bool launchWorker)
                {
                    if (launchWorker & _remainingAvailableWorkers > 0)
                    {
                        --_remainingAvailableWorkers;
                        // We add to the wait counter before we run the worker to resolve a race condition where the counter could hit zero prematurely.
                        InterlockedAddWithUnsignedOverflowCheck(ref _waitCounter, 1);
                        // We run the worker on the synchronization context, and we force async so that it will not block the current thread.
                        Promise.Run(this, _this => _this.ExecuteWorker(true), _synchronizationContext, forceAsync: true)
                            .Forget();
                    }
                }

                private void ExecuteWorker(bool launchNext)
                {
                    // We do it this way instead of using async/await with a loop, because old language versions do not support async/await.
                    SetCurrentInvoker(this);
                    Promise.Resolved()
                        .Then(ValueTuple.Create(this, launchNext), cv => cv.Item1.WorkerBody(cv.Item2))
                        .ContinueWith(this, (_this, resultContainer) => _this.AfterWorkerBody(resultContainer))
                        .Forget();
                    ClearCurrentInvoker();
                }

                private Promise WorkerBody(bool launchNext)
                {
                    // The worker body. Each worker will execute this same body.
                    if (_cancelationSource.IsCancelationRequested)
                    {
                        return Promise.Resolved();
                    }

                    // Get the next element from the enumerator. This requires locking around MoveNext/Current.
                    TSource element;
                    lock (this)
                    {
                        if (!_result.MoveNext())
                        {
                            // We cancel the source to notify completion, and return resolved instead of canceled
                            // so that the ultimate promise will be resolved instead of canceled.
                            _cancelationSource.TryCancel();
                            return Promise.Resolved();
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
                    return _body.Invoke(element, _cancelationSource.Token);
                }

                private void AfterWorkerBody(Promise.ResultContainer resultContainer)
                {
                    var state = resultContainer.State;
                    bool isWorkerComplete = state != Promise.State.Resolved | _cancelationSource.IsCancelationRequested;
                    if (state == Promise.State.Rejected)
                    {
                        // Record the failure and then don't let the exception propagate.  The last worker to complete
                        // will propagate exceptions as is appropriate to the top-level promise.
                        Exception e = resultContainer.RejectReason as Exception
                            // If the reason was not an exception, get the reason wrapped in an exception.
                            ?? resultContainer._target._rejectContainer.UnsafeAs<IRejectContainer>().GetExceptionDispatchInfo().SourceException;
                        RecordException(e);
                    }
                    else if (state == Promise.State.Canceled)
                    {
                        Cancel();
                    }

                    if (isWorkerComplete)
                    {
                        MaybeComplete();
                    }
                    else
                    {
                        // Run the worker body again, but without launching another worker.
                        // We run it on the synchronization context and force async,
                        // because this is a recursive call and we don't want to cause a StackOverflowException.
                        Promise.Run(this, _this => _this.ExecuteWorker(false), _synchronizationContext, forceAsync: true)
                            .Forget();
                    }
                }

                private void RecordException(Exception e)
                {
                    _cancelationSource.TryCancel();

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
                        try
                        {
                            _externalCancelationRegistration.Dispose();
                            _result.Dispose();
                        }
                        catch (Exception e)
                        {
                            RecordException(e);
                        }

                        _cancelationSource.Dispose();
                        _cancelationSource = default(CancelationSource);
                        _externalCancelationRegistration = default(CancelationRegistration);

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
                            HandleNextInternal(null, _wasCanceled ? Promise.State.Canceled : Promise.State.Resolved);
                        }
                    }
                }

                internal override void Handle(PromiseRefBase handler, object rejectContainer, Promise.State state) { throw new System.InvalidOperationException(); }
            } // class PromiseParallelForEach
        } // class PromiseRefBase
    } // class Internal
}