#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using Proto.Promises.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

#pragma warning disable IDE0083 // Use pattern matching
#pragma warning disable IDE0090 // Use 'new(...)'

namespace Proto.Promises
{
#if CSHARP_7_3_OR_NEWER
    partial class Internal
    {
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal abstract class AsyncEnumerableMergerBase<TValue> : PromiseRefBase.AsyncEnumerableWithIterator<TValue>
        {
            // TODO: optimize these collections.
            protected readonly List<AsyncEnumerator<TValue>> _enumerators = new List<AsyncEnumerator<TValue>>();
            protected readonly List<(object rejectContainer, Promise disposePromise)> _disposePromises = new List<(object rejectContainer, Promise disposePromise)>();
            // We queue the successful MoveNextAsync results instead of using Promise.RaceWithIndex, to avoid having to preserve each promise.
            // This must not be readonly.
            protected SingleConsumerAsyncQueueInternal<int> _readyQueue = new SingleConsumerAsyncQueueInternal<int>(0);
            protected int _streamWriterId;

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

                    // TODO: We could use a PromisePassThrough instead of ContinueWith to reduce memory.
                    moveNextPromise
                        .ContinueWith((this, index), (cv, r) => cv.Item1.HandleFromMoveNextAsync(cv.index, r))
                        .Forget();
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

            private void HandleFromMoveNextAsync(int index, Promise<bool>.ResultContainer resultContainer)
            {
                bool hasValue = resultContainer.Value & resultContainer.State == Promise.State.Resolved;
                if (hasValue)
                {
                    _readyQueue.Enqueue(index);
                }
                else
                {
                    if (resultContainer.State != Promise.State.Resolved)
                    {
                        // The async enumerator was canceled or rejected, notify all enumerators that they don't need to continue executing.
                        _cancelationToken._ref.Cancel();
                    }
                    DisposeEnumerator(_enumerators[index], resultContainer._rejectContainer);
                }
            }

            protected void DisposeEnumerator(AsyncEnumerator<TValue> enumerator, object rejectContainer)
            {
                var tuple = (rejectContainer, enumerator.DisposeAsync());
                lock (_disposePromises)
                {
                    _disposePromises.Add(tuple);
                }
                _readyQueue.RemoveProducer();
            }
        } // class AsyncEnumerableMerger<TValue>

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
                DisposeAndReturnToPool();
                // We can't be sure if the _sourcesEnumerator is from a collection with already existing AsyncEnumerables (like array.ToAsyncEnumerable()),
                // or a lazy iterator, so we have to iterate it and dispose every AsyncEnumerable.

                // If any rejections occurred, we capture them all and throw them in an AggregateException.
                List<Exception> exceptions = null;
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
                        RecordException(e, ref exceptions);
                    }
                }

                try
                {
                    await sources.DisposeAsync();
                }
                catch (Exception e) when (!(e is OperationCanceledException))
                {
                    RecordException(e, ref exceptions);
                }

                if (exceptions != null)
                {
                    throw new AggregateException(exceptions);
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
                _streamWriterId = enumerableId;
                var iteratorPromise = Iterate()._promise;
                if (iteratorPromise._ref == null)
                {
                    // Already complete.
                    HandleFromSynchronouslyCompletedIterator();
                    return;
                }

                // We only set _previous to support circular await detection.
                // We don't set _rejectContainerOrPreviousOrLink to prevent progress subscriptions from going down the chain, because progress is meaningless for AsyncEnumerable.
#if PROMISE_DEBUG
                _previous = iteratorPromise._ref;
#endif
                // We hook this up directly to the returned promise so we can know when the iteration is complete, and use this for the DisposeAsync promise.
                iteratorPromise._ref.HookupExistingWaiter(iteratorPromise._id, this);
            }

            private async AsyncIteratorMethod Iterate()
            {
                var mergeSourcesPromise = MergeSources();
                int enumeratorIndex = -1;
                try
                {
                    while (true)
                    {
                        var (hasValue, index) = await _readyQueue.TryDequeueAsync();
                        if (!hasValue)
                        {
                            break;
                        }
                        // We store the index in case the user disposes early.
                        enumeratorIndex = index;

                        // Yield the value to the consumer.
                        await YieldAsync(_enumerators[enumeratorIndex].Current, _streamWriterId);

                        ContinueMerge(enumeratorIndex);
                        enumeratorIndex = -1;
                    }
                }
                finally
                {
                    bool canceled;
                    if (enumeratorIndex >= 0)
                    {
                        // The operation was stopped early (`break` keyword in `foreach` loop).
                        // Notify all enumerators that they don't need to continue executing.
                        _cancelationToken._ref.Cancel();
                        // Break is different from cancelation, we don't cancel the iteration in this case.
                        canceled = false;
                        DisposeEnumerator(_enumerators[enumeratorIndex], null);

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
                    }
                    else
                    {
                        canceled = _cancelationToken.IsCancelationRequested;
                    }

                    // Wait for all DisposeAsyncs.
                    // If any rejections occurred, we capture them all and throw them in an AggregateException.
                    List<Exception> exceptions = null;
                    try
                    {
                        await mergeSourcesPromise;
                    }
                    catch (Exception e) when (!(e is OperationCanceledException))
                    {
                        RecordException(e, ref exceptions);
                    }

                    for (int i = 0, max = _disposePromises.Count; i < max; ++i)
                    {
                        var (rejectContainer, disposePromise) = _disposePromises[i];
                        try
                        {
                            await disposePromise;
                        }
                        catch (Exception e) when (!(e is OperationCanceledException))
                        {
                            RecordException(e, ref exceptions);
                            // If the dispose threw, we ignore any rejections from MoveNextAsync.
                            // This matches the behavior of the disposal in a sequential async function.
                            continue;
                        }
                        if (rejectContainer != null)
                        {
                            var container = rejectContainer.UnsafeAs<IRejectContainer>();
                            var exception = container.Value as Exception
                                // If the reason was not an exception, get the reason wrapped in an exception.
                                ?? container.GetExceptionDispatchInfo().SourceException;
                            RecordException(exception, ref exceptions);
                        }
                    }

                    _enumerators.Clear();
                    _disposePromises.Clear();
                    // We stored the CancelationRef we created in the token field, so we extract it to dispose here.
                    _cancelationToken._ref.TryDispose(_cancelationToken._ref.SourceId);

#pragma warning disable CA2219 // Do not raise exceptions in finally clauses
                    if (exceptions != null)
                    {
                        throw new AggregateException(exceptions);
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
                _readyQueue.AddProducer();
                try
                {
                    while (await _sourcesEnumerator.MoveNextAsync())
                    {
                        int index = _enumerators.Count;
                        _readyQueue.AddProducer();
                        _enumerators.Add(_sourcesEnumerator.Current.GetAsyncEnumerator(_cancelationToken));
                        ContinueMerge(index);
                    }
                }
                catch
                {
                    // The async enumerator was canceled or rejected, notify all enumerators that they don't need to continue executing.
                    _cancelationToken._ref.Cancel();
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
                DisposeAndReturnToPool();
                // We can't be sure if the _sourcesEnumerator is from a collection with already existing AsyncEnumerables (like an array or list),
                // or a lazy iterator, so we have to iterate it and dispose every AsyncEnumerable.

                // If any rejections occurred, we capture them all and throw them in an AggregateException.
                List<Exception> exceptions = null;
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
                        RecordException(e, ref exceptions);
                    }
                }

                try
                {
                    sources.Dispose();
                }
                catch (Exception e)
                {
                    RecordException(e, ref exceptions);
                }

                if (exceptions != null)
                {
                    throw new AggregateException(exceptions);
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
                _streamWriterId = enumerableId;
                var iteratorPromise = Iterate()._promise;
                if (iteratorPromise._ref == null)
                {
                    // Already complete.
                    HandleFromSynchronouslyCompletedIterator();
                    return;
                }

                // We only set _previous to support circular await detection.
                // We don't set _rejectContainerOrPreviousOrLink to prevent progress subscriptions from going down the chain, because progress is meaningless for AsyncEnumerable.
#if PROMISE_DEBUG
                _previous = iteratorPromise._ref;
#endif
                // We hook this up directly to the returned promise so we can know when the iteration is complete, and use this for the DisposeAsync promise.
                iteratorPromise._ref.HookupExistingWaiter(iteratorPromise._id, this);
            }

            private async AsyncIteratorMethod Iterate()
            {
                // If any rejections or exceptions occurred, we capture them all and throw them in an AggregateException.
                List<Exception> exceptions = null;
                int enumeratorIndex = -1;
                try
                {
                    MergeSources(ref exceptions);

                    while (true)
                    {
                        var (hasValue, index) = await _readyQueue.TryDequeueAsync();
                        if (!hasValue)
                        {
                            break;
                        }
                        // We store the index in case the user disposes early.
                        enumeratorIndex = index;

                        // Yield the value to the consumer.
                        await YieldAsync(_enumerators[enumeratorIndex].Current, _streamWriterId);

                        ContinueMerge(enumeratorIndex);
                        enumeratorIndex = -1;
                    }
                }
                finally
                {
                    bool canceled;
                    if (enumeratorIndex >= 0)
                    {
                        // The operation was stopped early (`break` keyword in `foreach` loop).
                        // Notify all enumerators that they don't need to continue executing.
                        _cancelationToken._ref.Cancel();
                        // Break is different from cancelation, we don't cancel the iteration in this case.
                        canceled = false;
                        DisposeEnumerator(_enumerators[enumeratorIndex], null);

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
                    }
                    else
                    {
                        canceled = _cancelationToken.IsCancelationRequested;
                    }

                    // Wait for all DisposeAsyncs.
                    // If any rejections occurred, we capture them all and throw them in an AggregateException.
                    for (int i = 0, max = _disposePromises.Count; i < max; ++i)
                    {
                        var (rejectContainer, disposePromise) = _disposePromises[i];
                        try
                        {
                            await disposePromise;
                        }
                        catch (Exception e) when (!(e is OperationCanceledException))
                        {
                            RecordException(e, ref exceptions);
                            // If the dispose threw, we ignore any rejections from MoveNextAsync.
                            // This matches the behavior of the disposal in a sequential async function.
                            continue;
                        }
                        if (rejectContainer != null)
                        {
                            var container = rejectContainer.UnsafeAs<IRejectContainer>();
                            var exception = container.Value as Exception
                                // If the reason was not an exception, get the reason wrapped in an exception.
                                ?? container.GetExceptionDispatchInfo().SourceException;
                            RecordException(exception, ref exceptions);
                        }
                    }

                    _enumerators.Clear();
                    _disposePromises.Clear();
                    // We stored the CancelationRef we created in the token field, so we extract it to dispose here.
                    _cancelationToken._ref.TryDispose(_cancelationToken._ref.SourceId);

#pragma warning disable CA2219 // Do not raise exceptions in finally clauses
                    if (exceptions != null)
                    {
                        throw new AggregateException(exceptions);
                    }
                    if (canceled)
                    {
                        throw Promise.CancelException();
                    }
#pragma warning restore CA2219 // Do not raise exceptions in finally clauses
                }
            }

            private void MergeSources(ref List<Exception> exceptions)
            {
                try
                {
                    _readyQueue.AddProducer();
                    using (_sourcesEnumerator)
                    {
                        while (_sourcesEnumerator.MoveNext())
                        {
                            int index = _enumerators.Count;
                            _readyQueue.AddProducer();
                            _enumerators.Add(_sourcesEnumerator.Current.GetAsyncEnumerator(_cancelationToken));
                            ContinueMerge(index);
                        }
                    }
                }
                catch (Exception e)
                {
                    // The enumerator threw, notify all enumerators that they don't need to continue executing.
                    _cancelationToken._ref.Cancel();
                    RecordException(e, ref exceptions);
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
#endif
} // namespace Proto.Promises