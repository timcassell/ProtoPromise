#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using Proto.Promises.Async.CompilerServices;
using Proto.Promises.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

#pragma warning disable IDE0063 // Use simple 'using' statement
#pragma warning disable IDE0090 // Use 'new(...)'

namespace Proto.Promises
{
#if CSHARP_7_3_OR_NEWER
    partial class Internal
    {
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal abstract class AsyncEnumerableMergerBase<TValue> : PromiseRefBase.AsyncEnumerableBase<TValue>
        {
            // TODO: optimize these collections.
            protected readonly List<AsyncEnumerator<TValue>> _enumerators = new List<AsyncEnumerator<TValue>>();
            // We queue the successful MoveNextAsync results instead of using Promise.RaceWithIndex, to avoid having to preserve each promise.
            protected readonly Queue<int> _readyQueue = new Queue<int>();
            protected readonly List<(object rejectContainer, Promise disposePromise)> _disposePromises = new List<(object rejectContainer, Promise disposePromise)>();
            // Used to pause the iterator function until a value is retrieved.
            // TODO: use a more optimized event with synchronous continuation.
            protected readonly AsyncAutoResetEventInternal _continueEvent = new AsyncAutoResetEventInternal(false);
            protected int _enumeratorCount;
            protected int _streamWriterId;

            protected void ContinueMerge(AsyncEnumerator<TValue> enumerator, int index)
            {
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
                    lock (_readyQueue)
                    {
                        _readyQueue.Enqueue(index);
                    }
                    _continueEvent.Set();
                }
                else
                {
                    DisposeEnumerator(enumerator, null);
                    if (Interlocked.Decrement(ref _enumeratorCount) == 0)
                    {
                        _continueEvent.Set();
                    }
                }
            }

            private void HandleFromMoveNextAsync(int index, Promise<bool>.ResultContainer resultContainer)
            {
                bool hasValue = resultContainer.Value & resultContainer.State == Promise.State.Resolved;
                if (hasValue)
                {
                    lock (_readyQueue)
                    {
                        _readyQueue.Enqueue(index);
                    }
                    _continueEvent.Set();
                }
                else
                {
                    if (resultContainer.State != Promise.State.Resolved)
                    {
                        // The async enumerator was canceled or rejected, notify all enumerators that they don't need to continue executing.
                        _cancelationToken._ref.Cancel();
                    }
                    DisposeEnumerator(_enumerators[index], resultContainer._rejectContainer);
                    if (Interlocked.Decrement(ref _enumeratorCount) == 0)
                    {
                        _continueEvent.Set();
                    }
                }
            }

            private void DisposeEnumerator(AsyncEnumerator<TValue> enumerator, object rejectContainer)
            {
                var tuple = (rejectContainer, enumerator.DisposeAsync());
                lock (_disposePromises)
                {
                    _disposePromises.Add(tuple);
                }
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
                _cancelationToken = default;
                ObjectPool.MaybeRepool(this);
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
                if (_sourcesEnumerator._target == null)
                {
                    MoveNext();
                    return;
                }

                var sourcesEnumerator = _sourcesEnumerator;
                _sourcesEnumerator = default;
                // We got the enumerator without a token when this was created, now we need to hook it up before we start moving next.
                // However, before we do so, we need to hook up our own cancelation source to notify all enumerators when 1 of them has been aborted.
                // We don't store the source directly, to reduce memory, we just store it in the _cancelationToken field and use the _ref directly.
                sourcesEnumerator._target._cancelationToken = _cancelationToken = CancelationSource.New(_cancelationToken).Token;
                _streamWriterId = enumerableId;
                _continueEvent.Reset();
                var iteratorPromise = Iterate(sourcesEnumerator)._promise;
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

            private async AsyncEnumerableMethod Iterate(AsyncEnumerator<AsyncEnumerable<TValue>> sourcesEnumerator)
            {
                try
                {
                    MergeSources(sourcesEnumerator).Forget();

                    while (true)
                    {
                        // Wait until a value is available, or all enumerators have completed.
                        await _continueEvent.WaitAsync();

                        // Read the count via Interlocked without changing it.
                        if (Interlocked.CompareExchange(ref _enumeratorCount, 0, 0) == 0)
                        {
                            break;
                        }

                        while (true)
                        {
                            int index;
                            lock (_readyQueue)
                            {
                                if (_readyQueue.Count == 0)
                                {
                                    break;
                                }
                                index = _readyQueue.Dequeue();
                            }

                            // Yield the value to the consumer.
                            // Only store the index past the `await` to keep the async state machine as small as possible.
                            await YieldAsync(_enumerators[index].Current, _streamWriterId);

                            ContinueMerge(_enumerators[index], index);
                        }
                    }

                    // All enumerators are complete, the only thing left to do is wait for DisposeAsyncs.
                    // If any rejections occurred, we capture them all and throw them in an AggregateException.
                    List<Exception> exceptions = null;
                    for (int i = 0, max = _disposePromises.Count; i < max; ++i)
                    {
                        var (rejectContainer, disposePromise) = _disposePromises[i];
                        try
                        {
                            await disposePromise;
                        }
                        catch (Exception e)
                        {
                            if (e is OperationCanceledException)
                            {
                                _cancelationToken._ref.Cancel();
                            }
                            else
                            {
                                RecordException(e, ref exceptions);
                            }
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

                    if (exceptions != null)
                    {
                        throw new AggregateException(exceptions);
                    }
                    _cancelationToken.ThrowIfCancelationRequested();
                }
                finally
                {
                    // We stored the CancelationRef we created in the token field, so we extract it to dispose here.
                    _cancelationToken._ref.TryDispose(_cancelationToken._ref.SourceId);
                    _enumerators.Clear();
                    _disposePromises.Clear();
                }
            }

            private async Promise MergeSources(AsyncEnumerator<AsyncEnumerable<TValue>> sourcesEnumerator)
            {
                _enumeratorCount = 1;
                int index = 0;
                object rejectContainer = null;
                try
                {
                    while (await sourcesEnumerator.MoveNextAsync())
                    {
                        int i = index;
                        checked
                        {
                            ++index;
                        }
                        Interlocked.Increment(ref _enumeratorCount);
                        var enumerator = sourcesEnumerator.Current.GetAsyncEnumerator(_cancelationToken);
                        _enumerators.Add(enumerator);
                        ContinueMerge(enumerator, i);
                    }
                }
                catch (Exception e)
                {
                    if (e is OperationCanceledException)
                    {
                        _cancelationToken._ref.Cancel();
                    }
                    else
                    {
                        rejectContainer = CreateRejectContainer(e, int.MinValue, null, null);
                    }
                }
                finally
                {

                    var tuple = (rejectContainer, sourcesEnumerator.DisposeAsync());
                    // Add to the list so it can be awaited the same as all the other dispose promises.
                    lock (_disposePromises)
                    {
                        _disposePromises.Add(tuple);
                    }
                    if (Interlocked.Decrement(ref _enumeratorCount) == 0)
                    {
                        _continueEvent.Set();
                    }
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
                _cancelationToken = default;
                ObjectPool.MaybeRepool(this);
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
                _continueEvent.Reset();
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

            private async AsyncEnumerableMethod Iterate()
            {
                try
                {
                    // If any rejections or exceptions occurred, we capture them all and throw them in an AggregateException.
                    List<Exception> exceptions = null;
                    MergeSources(ref exceptions);

                    while (true)
                    {
                        // Wait until a value is available, or all enumerators have completed.
                        await _continueEvent.WaitAsync();

                        // Read the count via Interlocked without changing it.
                        if (Interlocked.CompareExchange(ref _enumeratorCount, 0, 0) == 0)
                        {
                            break;
                        }

                        while (true)
                        {
                            int index;
                            lock (_readyQueue)
                            {
                                if (_readyQueue.Count == 0)
                                {
                                    break;
                                }
                                index = _readyQueue.Dequeue();
                            }

                            // Yield the value to the consumer.
                            // Only store the index past the `await` to keep the async state machine as small as possible.
                            await YieldAsync(_enumerators[index].Current, _streamWriterId);

                            ContinueMerge(_enumerators[index], index);
                        }
                    }

                    // All enumerators are complete, the only thing left to do is wait for DisposeAsyncs.
                    for (int i = 0, max = _disposePromises.Count; i < max; ++i)
                    {
                        var (rejectContainer, disposePromise) = _disposePromises[i];
                        try
                        {
                            await disposePromise;
                        }
                        catch (Exception e)
                        {
                            if (e is OperationCanceledException)
                            {
                                _cancelationToken._ref.Cancel();
                            }
                            else
                            {
                                RecordException(e, ref exceptions);
                            }
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

                    if (exceptions != null)
                    {
                        throw new AggregateException(exceptions);
                    }
                    _cancelationToken.ThrowIfCancelationRequested();
                }
                finally
                {
                    // We stored the CancelationRef we created in the token field, so we extract it to dispose here.
                    _cancelationToken._ref.TryDispose(_cancelationToken._ref.SourceId);
                    _enumerators.Clear();
                    _disposePromises.Clear();
                }
            }

            private void MergeSources(ref List<Exception> exceptions)
            {
                try
                {
                    _enumeratorCount = 1;
                    int index = 0;
                    using (var sources = _sourcesEnumerator)
                    {
                        _sourcesEnumerator = default;
                        try
                        {
                            while (sources.MoveNext())
                            {
                                int i = index;
                                checked
                                {
                                    ++index;
                                }
                                Interlocked.Increment(ref _enumeratorCount);
                                var enumerator = sources.Current.GetAsyncEnumerator(_cancelationToken);
                                _enumerators.Add(enumerator);
                                ContinueMerge(enumerator, i);
                            }
                        }
                        finally
                        {
                            if (Interlocked.Decrement(ref _enumeratorCount) == 0)
                            {
                                _continueEvent.Set();
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    RecordException(e, ref exceptions);
                }
            }
        }
    } // class Internal
#endif
} // namespace Proto.Promises