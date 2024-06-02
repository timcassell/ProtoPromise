#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using Proto.Promises.Collections;
using Proto.Promises.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

#pragma warning disable IDE0083 // Use pattern matching
#pragma warning disable IDE0090 // Use 'new(...)'

namespace Proto.Promises
{
    partial class Internal
    {
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal abstract class AsyncEnumerableMergerBase<TValue> : PromiseRefBase.AsyncEnumerableWithIterator<TValue>
        {
            // These must not be readonly.
            // We queue the successful MoveNextAsync results instead of using Promise.RaceWithIndex, to avoid having to retain each promise.
            protected SingleConsumerAsyncQueueInternal<int> _readyQueue;
            protected TempCollectionBuilder<AsyncEnumerator<TValue>> _enumerators;
            protected TempCollectionBuilder<(IRejectContainer rejectContainer, Promise disposePromise)> _disposePromises;
            protected SpinLocker _locker = new SpinLocker();
            // If any rejections or exceptions occur, we capture them all and throw them in an AggregateException.
            protected List<Exception> _exceptions;

            protected void ContinueMerge(int index)
            {
                var enumerator = _enumerators[index];
                var moveNextPromise = enumerator.MoveNextAsync();
                bool hasValue;
                if (moveNextPromise._ref == null)
                {
                    hasValue = moveNextPromise._result;
                }
                // We check for resolved state because the implementation always uses a backing reference
                // (except when the iteration is complete) that may complete synchronously.
                else if (moveNextPromise._ref.State == Promise.State.Resolved)
                {
                    hasValue = moveNextPromise._ref._result;
                    moveNextPromise.Forget();
                }
                else
                {
                    // The promise may still be pending, hook this up to continue when it completes.
                    var passthrough = PromisePassThrough.GetOrCreate(moveNextPromise._ref, this, index);
                    moveNextPromise._ref.HookupNewWaiter(moveNextPromise._id, passthrough);
                    return;
                }

                if (hasValue)
                {
                    _readyQueue.Enqueue(index);
                }
                else
                {
                    DisposeEnumerator(enumerator, null);
                }
            }

            internal override void Handle(PromiseRefBase handler, Promise.State state, int index)
            {
                handler.SetCompletionState(state);
                bool hasValue = state == Promise.State.Resolved & handler.GetResult<bool>();
                if (hasValue)
                {
                    _readyQueue.Enqueue(index);
                }
                else
                {
                    if (state != Promise.State.Resolved)
                    {
                        // The async enumerator was canceled or rejected, notify all enumerators that they don't need to continue executing.
                        CancelEnumerators();
                    }
                    DisposeEnumerator(_enumerators[index], handler._rejectContainer);
                }
            }

            protected void DisposeEnumerator(AsyncEnumerator<TValue> enumerator, IRejectContainer rejectContainer)
            {
                var tuple = (rejectContainer, enumerator.DisposeAsync());
                
                _locker.Enter();
                _disposePromises.Add(tuple);
                _locker.Exit();

                _readyQueue.RemoveProducer();
            }

            protected void CancelEnumerators()
            {
                // This may be called multiple times. It's fine because it checks internally if it's already canceled.
                try
                {
                    _cancelationToken._ref.Cancel();
                }
                catch (Exception e)
                {
                    RecordException(e);
                }
            }

            protected void RecordException(Exception e)
            {
                lock (this)
                {
                    Internal.RecordException(e, ref _exceptions);
                }
            }

            [MethodImpl(InlineOption)]
            new protected void Dispose()
            {
                base.Dispose();
                _exceptions = null;
            }
        } // class AsyncEnumerableMergerBase<TValue>

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal sealed class AsyncEnumerableMergerAsync<TValue> : AsyncEnumerableMergerBase<TValue>
        {
            private AsyncEnumerator<AsyncEnumerable<TValue>> _sourcesEnumerator;

            private AsyncEnumerableMergerAsync() { }

            [MethodImpl(InlineOption)]
            private static AsyncEnumerableMergerAsync<TValue> GetOrCreate()
            {
                var obj = ObjectPool.TryTakeOrInvalid<AsyncEnumerableMergerAsync<TValue>>();
                return obj == InvalidAwaitSentinel.s_instance
                    ? new AsyncEnumerableMergerAsync<TValue>()
                    : obj.UnsafeAs<AsyncEnumerableMergerAsync<TValue>>();
            }

            [MethodImpl(InlineOption)]
            internal static AsyncEnumerableMergerAsync<TValue> GetOrCreate(AsyncEnumerator<AsyncEnumerable<TValue>> sources)
            {
                var enumerable = GetOrCreate();
                enumerable.Reset();
                enumerable._sourcesEnumerator = sources;
                return enumerable;
            }

            protected override void DisposeAndReturnToPool()
            {
                Dispose();
                _sourcesEnumerator = default;
                ObjectPool.MaybeRepool(this);
            }

            protected override async Promise DisposeAsyncWithoutStart()
            {
                var sources = _sourcesEnumerator;
                SetStateForDisposeWithoutStart();
                DisposeAndReturnToPool();
                // We can't be sure if the _sourcesEnumerator is from a collection with already existing AsyncEnumerables (like array.ToAsyncEnumerable()),
                // or a lazy iterator, so we have to iterate it and dispose every AsyncEnumerable.

                while (true)
                {
                    try
                    {
                        if (!await sources.MoveNextAsync())
                        {
                            break;
                        }
                        await sources.Current.GetAsyncEnumerator().DisposeAsync();
                    }
                    catch (Exception e) when (!(e is OperationCanceledException))
                    {
                        RecordException(e);
                    }
                }

                try
                {
                    await sources.DisposeAsync();
                }
                catch (Exception e) when (!(e is OperationCanceledException))
                {
                    RecordException(e);
                }

                if (_exceptions != null)
                {
                    throw new AggregateException(_exceptions);
                }
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
                // We got the enumerator without a token when this was created, now we need to hook it up before we start moving next.
                // However, before we do so, we need to hook up our own cancelation source to notify all enumerators when 1 of them has been aborted.
                // We don't store the source directly, to reduce memory, we just store it in the _cancelationToken field and use the _ref directly.
                _sourcesEnumerator._target._cancelationToken = _cancelationToken = CancelationSource.New(_cancelationToken).Token;
                _enumerators = new TempCollectionBuilder<AsyncEnumerator<TValue>>(0);
                _disposePromises = new TempCollectionBuilder<(IRejectContainer rejectContainer, Promise disposePromise)>(0);
                var iteratorPromise = Iterate(enumerableId)._promise;
                if (iteratorPromise._ref == null)
                {
                    // Already complete.
                    HandleFromSynchronouslyCompletedIterator();
                    return;
                }

                // We only set _previous to support circular await detection.
#if PROMISE_DEBUG
                _previous = iteratorPromise._ref;
#endif
                // We hook this up directly to the returned promise so we can know when the iteration is complete, and use this for the DisposeAsync promise.
                iteratorPromise._ref.HookupExistingWaiter(iteratorPromise._id, this);
            }

            private async AsyncIteratorMethod Iterate(int streamWriterId)
            {
                var mergeSourcesPromise = MergeSources();
                int enumeratorIndex = -1;
                try
                {
                    while (!_cancelationToken.IsCancelationRequested)
                    {
                        var (hasValue, index) = await _readyQueue.TryDequeueAsync();
                        if (!hasValue)
                        {
                            break;
                        }
                        // We store the index in case the user disposes early.
                        enumeratorIndex = index;

                        // Yield the value to the consumer.
                        await YieldAsync(_enumerators[enumeratorIndex].Current, streamWriterId);

                        ContinueMerge(enumeratorIndex);
                        enumeratorIndex = -1;
                    }

                    // We yield and wait for the enumerator to be disposed, but only if there were no exceptions, and cancelation was not requested.
                    if (!_cancelationToken.IsCancelationRequested)
                    {
                        await YieldAsync(default, streamWriterId).ForLinqExtension();
                    }
                }
                finally
                {
                    bool canceled;
                    if (enumeratorIndex >= 0)
                    {
                        // The operation was stopped early (`break` keyword in `foreach` loop).
                        // Notify all enumerators that they don't need to continue executing.
                        CancelEnumerators();
                        // Break is different from cancelation, we don't cancel the iteration in this case.
                        canceled = false;
                        DisposeEnumerator(_enumerators[enumeratorIndex], null);
                    }
                    else
                    {
                        canceled = _cancelationToken.IsCancelationRequested;
                    }

                    // Wait for all MoveNextAsync promises to complete.
                    while (true)
                    {
                        var (hasValue, index) = await _readyQueue.TryDequeueAsync();
                        if (!hasValue)
                        {
                            break;
                        }
                        DisposeEnumerator(_enumerators[index], null);
                    }

                    // Wait for all DisposeAsyncs.
                    try
                    {
                        await mergeSourcesPromise;
                    }
                    catch (Exception e) when (!(e is OperationCanceledException))
                    {
                        RecordException(e);
                    }

                    for (int i = 0, max = _disposePromises._count; i < max; ++i)
                    {
                        var (rejectContainer, disposePromise) = _disposePromises[i];
                        try
                        {
                            await disposePromise;
                        }
                        catch (Exception e) when (!(e is OperationCanceledException))
                        {
                            RecordException(e);
                            // If the dispose threw, we ignore any rejections from MoveNextAsync.
                            // This matches the behavior of the disposal in a sequential async function.
                            continue;
                        }
                        if (rejectContainer != null)
                        {
                            RecordException(rejectContainer.GetValueAsException());
                        }
                    }

                    _readyQueue.Dispose();
                    _enumerators.Dispose();
                    _disposePromises.Dispose();
                    // We stored the CancelationRef we created in the token field, so we extract it to dispose here.
                    _cancelationToken._ref.TryDispose(_cancelationToken._ref.SourceId);

#pragma warning disable CA2219 // Do not raise exceptions in finally clauses
                    if (_exceptions != null)
                    {
                        throw new AggregateException(_exceptions);
                    }
                    if (canceled)
                    {
                        throw Promise.CancelException();
                    }
#pragma warning restore CA2219 // Do not raise exceptions in finally clauses
                }
            }

            private async Promise MergeSources()
            {
                _readyQueue = new SingleConsumerAsyncQueueInternal<int>(0, 1);
                try
                {
                    while (await _sourcesEnumerator.MoveNextAsync())
                    {
                        int index = _enumerators._count;
                        _readyQueue.AddProducer();
                        _enumerators.Add(_sourcesEnumerator.Current.GetAsyncEnumerator(_cancelationToken));
                        ContinueMerge(index);
                    }
                }
                catch
                {
                    // The async enumerator was canceled or rejected, notify all enumerators that they don't need to continue executing.
                    CancelEnumerators();
                    throw;
                }
                finally
                {
                    _readyQueue.RemoveProducer();
                    await _sourcesEnumerator.DisposeAsync();
                }
            }
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal sealed class AsyncEnumerableMergerSync<TValue, TEnumerator> : AsyncEnumerableMergerBase<TValue>
            where TEnumerator : IEnumerator<AsyncEnumerable<TValue>>
        {
            private TEnumerator _sourcesEnumerator;

            private AsyncEnumerableMergerSync() { }

            [MethodImpl(InlineOption)]
            private static AsyncEnumerableMergerSync<TValue, TEnumerator> GetOrCreate()
            {
                var obj = ObjectPool.TryTakeOrInvalid<AsyncEnumerableMergerSync<TValue, TEnumerator>>();
                return obj == InvalidAwaitSentinel.s_instance
                    ? new AsyncEnumerableMergerSync<TValue, TEnumerator>()
                    : obj.UnsafeAs<AsyncEnumerableMergerSync<TValue, TEnumerator>>();
            }

            [MethodImpl(InlineOption)]
            internal static AsyncEnumerableMergerSync<TValue, TEnumerator> GetOrCreate(in TEnumerator sources)
            {
                var enumerable = GetOrCreate();
                enumerable.Reset();
                enumerable._sourcesEnumerator = sources;
                return enumerable;
            }

            protected override void DisposeAndReturnToPool()
            {
                Dispose();
                _sourcesEnumerator = default;
                ObjectPool.MaybeRepool(this);
            }

            protected override async Promise DisposeAsyncWithoutStart()
            {
                var sources = _sourcesEnumerator;
                SetStateForDisposeWithoutStart();
                DisposeAndReturnToPool();
                // We can't be sure if the _sourcesEnumerator is from a collection with already existing AsyncEnumerables (like an array or list),
                // or a lazy iterator, so we have to iterate it and dispose every AsyncEnumerable.

                while (true)
                {
                    try
                    {
                        if (!sources.MoveNext())
                        {
                            break;
                        }
                        await sources.Current.GetAsyncEnumerator().DisposeAsync();
                    }
                    catch (Exception e) when (!(e is OperationCanceledException))
                    {
                        RecordException(e);
                    }
                }

                try
                {
                    sources.Dispose();
                }
                catch (Exception e)
                {
                    RecordException(e);
                }

                if (_exceptions != null)
                {
                    throw new AggregateException(_exceptions);
                }
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
                // We need to hook up our own cancelation source to notify all enumerators when 1 of them has been aborted.
                // We don't store the source directly, to reduce memory, we just store it in the _cancelationToken field and use the _ref directly.
                _cancelationToken = CancelationSource.New(_cancelationToken).Token;
                _enumerators = new TempCollectionBuilder<AsyncEnumerator<TValue>>(0);
                _disposePromises = new TempCollectionBuilder<(IRejectContainer rejectContainer, Promise disposePromise)>(0);
                var iteratorPromise = Iterate(enumerableId)._promise;
                if (iteratorPromise._ref == null)
                {
                    // Already complete.
                    HandleFromSynchronouslyCompletedIterator();
                    return;
                }

                // We only set _previous to support circular await detection.
#if PROMISE_DEBUG
                _previous = iteratorPromise._ref;
#endif
                // We hook this up directly to the returned promise so we can know when the iteration is complete, and use this for the DisposeAsync promise.
                iteratorPromise._ref.HookupExistingWaiter(iteratorPromise._id, this);
            }

            private async AsyncIteratorMethod Iterate(int streamWriterId)
            {
                int enumeratorIndex = -1;
                try
                {
                    MergeSources();

                    while (!_cancelationToken.IsCancelationRequested)
                    {
                        var (hasValue, index) = await _readyQueue.TryDequeueAsync();
                        if (!hasValue)
                        {
                            break;
                        }
                        // We store the index in case the user disposes early.
                        enumeratorIndex = index;

                        // Yield the value to the consumer.
                        await YieldAsync(_enumerators[enumeratorIndex].Current, streamWriterId);

                        ContinueMerge(enumeratorIndex);
                        enumeratorIndex = -1;
                    }

                    // We yield and wait for the enumerator to be disposed, but only if there were no exceptions, and cancelation was not requested.
                    if (!_cancelationToken.IsCancelationRequested)
                    {
                        await YieldAsync(default, streamWriterId).ForLinqExtension();
                    }
                }
                finally
                {
                    bool canceled;
                    if (enumeratorIndex >= 0)
                    {
                        // The operation was stopped early (`break` keyword in `foreach` loop).
                        // Notify all enumerators that they don't need to continue executing.
                        CancelEnumerators();
                        // Break is different from cancelation, we don't cancel the iteration in this case.
                        canceled = false;
                        DisposeEnumerator(_enumerators[enumeratorIndex], null);
                    }
                    else
                    {
                        canceled = _cancelationToken.IsCancelationRequested;
                    }

                    // Wait for all MoveNextAsync promises to complete.
                    while (true)
                    {
                        var (hasValue, index) = await _readyQueue.TryDequeueAsync();
                        if (!hasValue)
                        {
                            break;
                        }
                        DisposeEnumerator(_enumerators[index], null);
                    }

                    // Wait for all DisposeAsyncs.
                    // If any rejections occurred, we capture them all and throw them in an AggregateException.
                    for (int i = 0, max = _disposePromises._count; i < max; ++i)
                    {
                        var (rejectContainer, disposePromise) = _disposePromises[i];
                        try
                        {
                            await disposePromise;
                        }
                        catch (Exception e) when (!(e is OperationCanceledException))
                        {
                            RecordException(e);
                            // If the dispose threw, we ignore any rejections from MoveNextAsync.
                            // This matches the behavior of the disposal in a sequential async function.
                            continue;
                        }
                        if (rejectContainer != null)
                        {
                            RecordException(rejectContainer.GetValueAsException());
                        }
                    }

                    _readyQueue.Dispose();
                    _enumerators.Dispose();
                    _disposePromises.Dispose();
                    // We stored the CancelationRef we created in the token field, so we extract it to dispose here.
                    _cancelationToken._ref.TryDispose(_cancelationToken._ref.SourceId);

#pragma warning disable CA2219 // Do not raise exceptions in finally clauses
                    if (_exceptions != null)
                    {
                        throw new AggregateException(_exceptions);
                    }
                    if (canceled)
                    {
                        throw Promise.CancelException();
                    }
#pragma warning restore CA2219 // Do not raise exceptions in finally clauses
                }
            }

            private void MergeSources()
            {
                try
                {
                    _readyQueue = new SingleConsumerAsyncQueueInternal<int>(0, 1);
                    using (_sourcesEnumerator)
                    {
                        while (_sourcesEnumerator.MoveNext())
                        {
                            int index = _enumerators._count;
                            _readyQueue.AddProducer();
                            _enumerators.Add(_sourcesEnumerator.Current.GetAsyncEnumerator(_cancelationToken));
                            ContinueMerge(index);
                        }
                    }
                }
                catch (Exception e)
                {
                    RecordException(e);
                    // The enumerator threw, notify all enumerators that they don't need to continue executing.
                    CancelEnumerators();
                }
                finally
                {
                    // Even though the enumerator is cleared in DisposeAndReturnToPool, we also do it here
                    // to allow GC to clean it up if it's eligible before the merge is complete.
                    _sourcesEnumerator = default;
                    _readyQueue.RemoveProducer();
                }
            }
        }
    } // class Internal
} // namespace Proto.Promises