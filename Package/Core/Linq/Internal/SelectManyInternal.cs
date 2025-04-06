#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using Proto.Promises.Collections;
using Proto.Promises.CompilerServices;
using Proto.Promises.Linq;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;

namespace Proto.Promises
{
    partial class Internal
    {
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal static class SelectManyHelper<TResult>
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private readonly struct SelectManyAsyncIterator<TSource, TSelector> : IAsyncIterator<TResult>
                where TSelector : IFunc<TSource, CancelationToken, Promise<AsyncEnumerable<TResult>>>
            {
                private readonly AsyncEnumerator<TSource> _asyncEnumerator;
                private readonly TSelector _selector;

                internal SelectManyAsyncIterator(AsyncEnumerator<TSource> asyncEnumerator, TSelector selector)
                {
                    _asyncEnumerator = asyncEnumerator;
                    _selector = selector;
                }

                public async AsyncIteratorMethod Start(AsyncStreamWriter<TResult> writer, CancelationToken cancelationToken)
                {
                    // The enumerator was retrieved without a cancelation token when the original function was called.
                    // We need to propagate the token that was passed in, so we assign it before starting iteration.
                    _asyncEnumerator._target._cancelationToken = cancelationToken;

                    // We don't dispose the enumerators until the owner is disposed.
                    // This is in case any enumerator contains TempCollections that they will still be valid until the owner is disposed.
                    var enumerators = new TempCollectionBuilder<AsyncEnumerator<TResult>>(0);
                    try
                    {
                        while (await _asyncEnumerator.MoveNextAsync())
                        {
                            var innerEnumerator = (await _selector.Invoke(_asyncEnumerator.Current, cancelationToken)).GetAsyncEnumerator(cancelationToken);
                            enumerators.Add(innerEnumerator);
                            while (await innerEnumerator.MoveNextAsync())
                            {
                                await writer.YieldAsync(innerEnumerator.Current);
                            }
                        }

                        // We yield and wait for the enumerator to be disposed, but only if there were no exceptions.
                        await writer.YieldAsync(default).ForLinqExtension();
                    }
                    finally
                    {
                        try
                        {
                            await _asyncEnumerator.DisposeAsync();
                        }
                        finally
                        {
                            // We can't do a try/finally loop to dispose all of the enumerators,
                            // so we simulate the behavior by only capturing the last exception.
                            Exception ex = null;
                            for (int i = 0; i < enumerators._count; ++i)
                            {
                                try
                                {
                                    await enumerators[i].DisposeAsync();
                                }
                                catch (Exception e)
                                {
                                    ex = e;
                                }
                            }
                            enumerators.Dispose();
                            if (ex != null)
                            {
                                ExceptionDispatchInfo.Capture(ex).Throw();
                            }
                        }
                    }
                }

                [MethodImpl(InlineOption)]
                public Promise DisposeAsyncWithoutStart()
                    => _asyncEnumerator.DisposeAsync();
            }

            internal static AsyncEnumerable<TResult> SelectMany<TSource, TSelector>(AsyncEnumerator<TSource> source, TSelector selector)
                where TSelector : IFunc<TSource, CancelationToken, Promise<AsyncEnumerable<TResult>>>
                => AsyncEnumerable<TResult>.Create(new SelectManyAsyncIterator<TSource, TSelector>(source, selector));

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private readonly struct ConfiguredSelectManyAsyncIterator<TSource, TSelector> : IAsyncIterator<TResult>
                where TSelector : IFunc<TSource, CancelationToken, Promise<AsyncEnumerable<TResult>>>
            {
                private readonly ConfiguredAsyncEnumerable<TSource>.Enumerator _configuredAsyncEnumerator;
                private readonly TSelector _selector;

                internal ConfiguredSelectManyAsyncIterator(ConfiguredAsyncEnumerable<TSource>.Enumerator configuredAsyncEnumerator, TSelector selector)
                {
                    _configuredAsyncEnumerator = configuredAsyncEnumerator;
                    _selector = selector;
                }

                public async AsyncIteratorMethod Start(AsyncStreamWriter<TResult> writer, CancelationToken cancelationToken)
                {
                    // The enumerator may have been configured with a cancelation token. We need to join the passed in token before starting iteration.
                    var maybeJoinedCancelationSource = MaybeJoinCancelationTokens(ref cancelationToken, ref _configuredAsyncEnumerator._enumerator._target._cancelationToken);

                    // We don't dispose the enumerators until the owner is disposed.
                    // This is in case any enumerator contains TempCollections that they will still be valid until the owner is disposed.
                    var enumerators = new TempCollectionBuilder<AsyncEnumerator<TResult>>(0);
                    try
                    {
                        while (await _configuredAsyncEnumerator.MoveNextAsync())
                        {
                            var innerEnumerator = (await _selector.Invoke(_configuredAsyncEnumerator.Current, cancelationToken)).GetAsyncEnumerator(_configuredAsyncEnumerator._enumerator._target._cancelationToken);
                            enumerators.Add(innerEnumerator);
                            while (await innerEnumerator.MoveNextAsync())
                            {
                                await writer.YieldAsync(innerEnumerator.Current);
                            }
                        }

                        // We yield and wait for the enumerator to be disposed, but only if there were no exceptions.
                        await writer.YieldAsync(default).ForLinqExtension();
                    }
                    finally
                    {
                        maybeJoinedCancelationSource.Dispose();
                        try
                        {
                            await _configuredAsyncEnumerator.DisposeAsync();
                        }
                        finally
                        {
                            // We can't do a try/finally loop to dispose all of the enumerators,
                            // so we simulate the behavior by only capturing the last exception.
                            Exception ex = null;
                            for (int i = 0; i < enumerators._count; ++i)
                            {
                                try
                                {
                                    await enumerators[i].DisposeAsync();
                                }
                                catch (Exception e)
                                {
                                    ex = e;
                                }
                            }
                            enumerators.Dispose();
                            if (ex != null)
                            {
                                ExceptionDispatchInfo.Capture(ex).Throw();
                            }
                        }
                    }
                }

                [MethodImpl(InlineOption)]
                public Promise DisposeAsyncWithoutStart()
                    => _configuredAsyncEnumerator.DisposeAsync();
            }

            internal static AsyncEnumerable<TResult> SelectMany<TSource, TSelector>(ConfiguredAsyncEnumerable<TSource>.Enumerator configuredAsyncEnumerator, TSelector selector)
                where TSelector : IFunc<TSource, CancelationToken, Promise<AsyncEnumerable<TResult>>>
                => AsyncEnumerable<TResult>.Create(new ConfiguredSelectManyAsyncIterator<TSource, TSelector>(configuredAsyncEnumerator, selector));

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private readonly struct SelectManyWithIndexAsyncIterator<TSource, TSelector> : IAsyncIterator<TResult>
                where TSelector : IFunc<TSource, int, CancelationToken, Promise<AsyncEnumerable<TResult>>>
            {
                private readonly AsyncEnumerator<TSource> _asyncEnumerator;
                private readonly TSelector _selector;

                internal SelectManyWithIndexAsyncIterator(AsyncEnumerator<TSource> asyncEnumerator, TSelector selector)
                {
                    _asyncEnumerator = asyncEnumerator;
                    _selector = selector;
                }

                public async AsyncIteratorMethod Start(AsyncStreamWriter<TResult> writer, CancelationToken cancelationToken)
                {
                    // The enumerator was retrieved without a cancelation token when the original function was called.
                    // We need to propagate the token that was passed in, so we assign it before starting iteration.
                    _asyncEnumerator._target._cancelationToken = cancelationToken;

                    // We don't dispose the enumerators until the owner is disposed.
                    // This is in case any enumerator contains TempCollections that they will still be valid until the owner is disposed.
                    var enumerators = new TempCollectionBuilder<AsyncEnumerator<TResult>>(0);
                    try
                    {
                        int i = 0;
                        while (await _asyncEnumerator.MoveNextAsync())
                        {
                            var innerEnumerator = (await _selector.Invoke(_asyncEnumerator.Current, checked(i++), cancelationToken)).GetAsyncEnumerator(cancelationToken);
                            enumerators.Add(innerEnumerator);
                            while (await innerEnumerator.MoveNextAsync())
                            {
                                await writer.YieldAsync(innerEnumerator.Current);
                            }
                        }

                        // We yield and wait for the enumerator to be disposed, but only if there were no exceptions.
                        await writer.YieldAsync(default).ForLinqExtension();
                    }
                    finally
                    {
                        try
                        {
                            await _asyncEnumerator.DisposeAsync();
                        }
                        finally
                        {
                            // We can't do a try/finally loop to dispose all of the enumerators,
                            // so we simulate the behavior by only capturing the last exception.
                            Exception ex = null;
                            for (int i = 0; i < enumerators._count; ++i)
                            {
                                try
                                {
                                    await enumerators[i].DisposeAsync();
                                }
                                catch (Exception e)
                                {
                                    ex = e;
                                }
                            }
                            enumerators.Dispose();
                            if (ex != null)
                            {
                                ExceptionDispatchInfo.Capture(ex).Throw();
                            }
                        }
                    }
                }

                [MethodImpl(InlineOption)]
                public Promise DisposeAsyncWithoutStart()
                    => _asyncEnumerator.DisposeAsync();
            }

            internal static AsyncEnumerable<TResult> SelectManyWithIndex<TSource, TSelector>(AsyncEnumerator<TSource> source, TSelector selector)
                where TSelector : IFunc<TSource, int, CancelationToken, Promise<AsyncEnumerable<TResult>>>
                => AsyncEnumerable<TResult>.Create(new SelectManyWithIndexAsyncIterator<TSource, TSelector>(source, selector));

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private readonly struct ConfiguredSelectManyWithIndexAsyncIterator<TSource, TSelector> : IAsyncIterator<TResult>
                where TSelector : IFunc<TSource, int, CancelationToken, Promise<AsyncEnumerable<TResult>>>
            {
                private readonly ConfiguredAsyncEnumerable<TSource>.Enumerator _configuredAsyncEnumerator;
                private readonly TSelector _selector;

                internal ConfiguredSelectManyWithIndexAsyncIterator(ConfiguredAsyncEnumerable<TSource>.Enumerator configuredAsyncEnumerator, TSelector selector)
                {
                    _configuredAsyncEnumerator = configuredAsyncEnumerator;
                    _selector = selector;
                }

                public async AsyncIteratorMethod Start(AsyncStreamWriter<TResult> writer, CancelationToken cancelationToken)
                {
                    // The enumerator may have been configured with a cancelation token. We need to join the passed in token before starting iteration.
                    var maybeJoinedCancelationSource = MaybeJoinCancelationTokens(ref cancelationToken, ref _configuredAsyncEnumerator._enumerator._target._cancelationToken);

                    // We don't dispose the enumerators until the owner is disposed.
                    // This is in case any enumerator contains TempCollections that they will still be valid until the owner is disposed.
                    var enumerators = new TempCollectionBuilder<AsyncEnumerator<TResult>>(0);
                    try
                    {
                        int i = 0;
                        while (await _configuredAsyncEnumerator.MoveNextAsync())
                        {
                            var innerEnumerator = (await _selector.Invoke(_configuredAsyncEnumerator.Current, checked(i++), cancelationToken)).GetAsyncEnumerator(cancelationToken);
                            enumerators.Add(innerEnumerator);
                            while (await innerEnumerator.MoveNextAsync())
                            {
                                await writer.YieldAsync(innerEnumerator.Current);
                            }
                        }

                        // We yield and wait for the enumerator to be disposed, but only if there were no exceptions.
                        await writer.YieldAsync(default).ForLinqExtension();
                    }
                    finally
                    {
                        maybeJoinedCancelationSource.Dispose();
                        try
                        {
                            await _configuredAsyncEnumerator.DisposeAsync();
                        }
                        finally
                        {
                            // We can't do a try/finally loop to dispose all of the enumerators,
                            // so we simulate the behavior by only capturing the last exception.
                            Exception ex = null;
                            for (int i = 0; i < enumerators._count; ++i)
                            {
                                try
                                {
                                    await enumerators[i].DisposeAsync();
                                }
                                catch (Exception e)
                                {
                                    ex = e;
                                }
                            }
                            enumerators.Dispose();
                            if (ex != null)
                            {
                                ExceptionDispatchInfo.Capture(ex).Throw();
                            }
                        }
                    }
                }

                [MethodImpl(InlineOption)]
                public Promise DisposeAsyncWithoutStart()
                    => _configuredAsyncEnumerator.DisposeAsync();
            }

            internal static AsyncEnumerable<TResult> SelectManyWithIndex<TSource, TSelector>(ConfiguredAsyncEnumerable<TSource>.Enumerator configuredAsyncEnumerator, TSelector selector)
                where TSelector : IFunc<TSource, int, CancelationToken, Promise<AsyncEnumerable<TResult>>>
                => AsyncEnumerable<TResult>.Create(new ConfiguredSelectManyWithIndexAsyncIterator<TSource, TSelector>(configuredAsyncEnumerator, selector));
        } // class SelectManyHelper<TResult>

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal static class SelectManyHelper<TCollection, TResult>
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private readonly struct SelectManyAsyncIterator<TSource, TCollectionSelector, TResultSelector> : IAsyncIterator<TResult>
                where TCollectionSelector : IFunc<TSource, CancelationToken, Promise<AsyncEnumerable<TCollection>>>
                where TResultSelector : IFunc<TSource, TCollection, CancelationToken, Promise<TResult>>
            {
                private readonly AsyncEnumerator<TSource> _asyncEnumerator;
                private readonly TCollectionSelector _collectionSelector;
                private readonly TResultSelector _resultSelector;

                internal SelectManyAsyncIterator(AsyncEnumerator<TSource> asyncEnumerator, TCollectionSelector collectionSelector, TResultSelector resultSelector)
                {
                    _asyncEnumerator = asyncEnumerator;
                    _collectionSelector = collectionSelector;
                    _resultSelector = resultSelector;
                }

                public async AsyncIteratorMethod Start(AsyncStreamWriter<TResult> writer, CancelationToken cancelationToken)
                {
                    // The enumerator was retrieved without a cancelation token when the original function was called.
                    // We need to propagate the token that was passed in, so we assign it before starting iteration.
                    _asyncEnumerator._target._cancelationToken = cancelationToken;

                    // We don't dispose the enumerators until the owner is disposed.
                    // This is in case any enumerator contains TempCollections that they will still be valid until the owner is disposed.
                    var enumerators = new TempCollectionBuilder<AsyncEnumerator<TCollection>>(0);
                    try
                    {
                        while (await _asyncEnumerator.MoveNextAsync())
                        {
                            var outerResult = _asyncEnumerator.Current;
                            var innerEnumerator = (await _collectionSelector.Invoke(outerResult, cancelationToken)).GetAsyncEnumerator(cancelationToken);
                            enumerators.Add(innerEnumerator);
                            while (await innerEnumerator.MoveNextAsync())
                            {
                                await writer.YieldAsync(await _resultSelector.Invoke(outerResult, innerEnumerator.Current, cancelationToken));
                            }
                        }

                        // We yield and wait for the enumerator to be disposed, but only if there were no exceptions.
                        await writer.YieldAsync(default).ForLinqExtension();
                    }
                    finally
                    {
                        try
                        {
                            await _asyncEnumerator.DisposeAsync();
                        }
                        finally
                        {
                            // We can't do a try/finally loop to dispose all of the enumerators,
                            // so we simulate the behavior by only capturing the last exception.
                            Exception ex = null;
                            for (int i = 0; i < enumerators._count; ++i)
                            {
                                try
                                {
                                    await enumerators[i].DisposeAsync();
                                }
                                catch (Exception e)
                                {
                                    ex = e;
                                }
                            }
                            enumerators.Dispose();
                            if (ex != null)
                            {
                                ExceptionDispatchInfo.Capture(ex).Throw();
                            }
                        }
                    }
                }

                [MethodImpl(InlineOption)]
                public Promise DisposeAsyncWithoutStart()
                    => _asyncEnumerator.DisposeAsync();
            }

            internal static AsyncEnumerable<TResult> SelectMany<TSource, TCollectionSelector, TResultSelector>(AsyncEnumerator<TSource> source, TCollectionSelector collectionSelector, TResultSelector resultSelector)
                where TCollectionSelector : IFunc<TSource, CancelationToken, Promise<AsyncEnumerable<TCollection>>>
                where TResultSelector : IFunc<TSource, TCollection, CancelationToken, Promise<TResult>>
                => AsyncEnumerable<TResult>.Create(new SelectManyAsyncIterator<TSource, TCollectionSelector, TResultSelector>(source, collectionSelector, resultSelector));

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private readonly struct ConfiguredSelectManyAsyncIterator<TSource, TCollectionSelector, TResultSelector> : IAsyncIterator<TResult>
                where TCollectionSelector : IFunc<TSource, CancelationToken, Promise<AsyncEnumerable<TCollection>>>
                where TResultSelector : IFunc<TSource, TCollection, CancelationToken, Promise<TResult>>
            {
                private readonly ConfiguredAsyncEnumerable<TSource>.Enumerator _configuredAsyncEnumerator;
                private readonly TCollectionSelector _collectionSelector;
                private readonly TResultSelector _resultSelector;

                internal ConfiguredSelectManyAsyncIterator(ConfiguredAsyncEnumerable<TSource>.Enumerator configuredAsyncEnumerator, TCollectionSelector collectionSelector, TResultSelector resultSelector)
                {
                    _configuredAsyncEnumerator = configuredAsyncEnumerator;
                    _collectionSelector = collectionSelector;
                    _resultSelector = resultSelector;
                }

                public async AsyncIteratorMethod Start(AsyncStreamWriter<TResult> writer, CancelationToken cancelationToken)
                {
                    // The enumerator may have been configured with a cancelation token. We need to join the passed in token before starting iteration.
                    var maybeJoinedCancelationSource = MaybeJoinCancelationTokens(ref cancelationToken, ref _configuredAsyncEnumerator._enumerator._target._cancelationToken);

                    // We don't dispose the enumerators until the owner is disposed.
                    // This is in case any enumerator contains TempCollections that they will still be valid until the owner is disposed.
                    var enumerators = new TempCollectionBuilder<AsyncEnumerator<TCollection>>(0);
                    try
                    {
                        while (await _configuredAsyncEnumerator.MoveNextAsync())
                        {
                            var outerResult = _configuredAsyncEnumerator.Current;
                            var innerEnumerator = (await _collectionSelector.Invoke(outerResult, cancelationToken)).GetAsyncEnumerator(_configuredAsyncEnumerator._enumerator._target._cancelationToken);
                            enumerators.Add(innerEnumerator);
                            while (await innerEnumerator.MoveNextAsync())
                            {
                                await writer.YieldAsync(await _resultSelector.Invoke(outerResult, innerEnumerator.Current, cancelationToken));
                            }
                        }

                        // We yield and wait for the enumerator to be disposed, but only if there were no exceptions.
                        await writer.YieldAsync(default).ForLinqExtension();
                    }
                    finally
                    {
                        maybeJoinedCancelationSource.Dispose();
                        try
                        {
                            await _configuredAsyncEnumerator.DisposeAsync();
                        }
                        finally
                        {
                            // We can't do a try/finally loop to dispose all of the enumerators,
                            // so we simulate the behavior by only capturing the last exception.
                            Exception ex = null;
                            for (int i = 0; i < enumerators._count; ++i)
                            {
                                try
                                {
                                    await enumerators[i].DisposeAsync();
                                }
                                catch (Exception e)
                                {
                                    ex = e;
                                }
                            }
                            enumerators.Dispose();
                            if (ex != null)
                            {
                                ExceptionDispatchInfo.Capture(ex).Throw();
                            }
                        }
                    }
                }

                [MethodImpl(InlineOption)]
                public Promise DisposeAsyncWithoutStart()
                    => _configuredAsyncEnumerator.DisposeAsync();
            }

            internal static AsyncEnumerable<TResult> SelectMany<TSource, TCollectionSelector, TResultSelector>(ConfiguredAsyncEnumerable<TSource>.Enumerator configuredAsyncEnumerator, TCollectionSelector collectionSelector, TResultSelector resultSelector)
                where TCollectionSelector : IFunc<TSource, CancelationToken, Promise<AsyncEnumerable<TCollection>>>
                where TResultSelector : IFunc<TSource, TCollection, CancelationToken, Promise<TResult>>
                => AsyncEnumerable<TResult>.Create(new ConfiguredSelectManyAsyncIterator<TSource, TCollectionSelector, TResultSelector>(configuredAsyncEnumerator, collectionSelector, resultSelector));

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private readonly struct SelectManyWithIndexAsyncIterator<TSource, TCollectionSelector, TResultSelector> : IAsyncIterator<TResult>
                where TCollectionSelector : IFunc<TSource, int, CancelationToken, Promise<AsyncEnumerable<TCollection>>>
                where TResultSelector : IFunc<TSource, TCollection, CancelationToken, Promise<TResult>>
            {
                private readonly AsyncEnumerator<TSource> _asyncEnumerator;
                private readonly TCollectionSelector _collectionSelector;
                private readonly TResultSelector _resultSelector;

                internal SelectManyWithIndexAsyncIterator(AsyncEnumerator<TSource> asyncEnumerator, TCollectionSelector collectionSelector, TResultSelector resultSelector)
                {
                    _asyncEnumerator = asyncEnumerator;
                    _collectionSelector = collectionSelector;
                    _resultSelector = resultSelector;
                }

                public async AsyncIteratorMethod Start(AsyncStreamWriter<TResult> writer, CancelationToken cancelationToken)
                {
                    // The enumerator was retrieved without a cancelation token when the original function was called.
                    // We need to propagate the token that was passed in, so we assign it before starting iteration.
                    _asyncEnumerator._target._cancelationToken = cancelationToken;

                    // We don't dispose the enumerators until the owner is disposed.
                    // This is in case any enumerator contains TempCollections that they will still be valid until the owner is disposed.
                    var enumerators = new TempCollectionBuilder<AsyncEnumerator<TCollection>>(0);
                    try
                    {
                        int i = 0;
                        while (await _asyncEnumerator.MoveNextAsync())
                        {
                            var outerResult = _asyncEnumerator.Current;
                            var innerEnumerator = (await _collectionSelector.Invoke(outerResult, checked(i++), cancelationToken)).GetAsyncEnumerator(cancelationToken);
                            enumerators.Add(innerEnumerator);
                            while (await innerEnumerator.MoveNextAsync())
                            {
                                await writer.YieldAsync(await _resultSelector.Invoke(outerResult, innerEnumerator.Current, cancelationToken));
                            }
                        }

                        // We yield and wait for the enumerator to be disposed, but only if there were no exceptions.
                        await writer.YieldAsync(default).ForLinqExtension();
                    }
                    finally
                    {
                        try
                        {
                            await _asyncEnumerator.DisposeAsync();
                        }
                        finally
                        {
                            // We can't do a try/finally loop to dispose all of the enumerators,
                            // so we simulate the behavior by only capturing the last exception.
                            Exception ex = null;
                            for (int i = 0; i < enumerators._count; ++i)
                            {
                                try
                                {
                                    await enumerators[i].DisposeAsync();
                                }
                                catch (Exception e)
                                {
                                    ex = e;
                                }
                            }
                            enumerators.Dispose();
                            if (ex != null)
                            {
                                ExceptionDispatchInfo.Capture(ex).Throw();
                            }
                        }
                    }
                }

                [MethodImpl(InlineOption)]
                public Promise DisposeAsyncWithoutStart()
                    => _asyncEnumerator.DisposeAsync();
            }

            internal static AsyncEnumerable<TResult> SelectManyWithIndex<TSource, TCollectionSelector, TResultSelector>(AsyncEnumerator<TSource> source, TCollectionSelector collectionSelector, TResultSelector resultSelector)
                where TCollectionSelector : IFunc<TSource, int, CancelationToken, Promise<AsyncEnumerable<TCollection>>>
                where TResultSelector : IFunc<TSource, TCollection, CancelationToken, Promise<TResult>>
                => AsyncEnumerable<TResult>.Create(new SelectManyWithIndexAsyncIterator<TSource, TCollectionSelector, TResultSelector>(source, collectionSelector, resultSelector));

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private readonly struct ConfiguredSelectManyWithIndexAsyncIterator<TSource, TCollectionSelector, TResultSelector> : IAsyncIterator<TResult>
                where TCollectionSelector : IFunc<TSource, int, CancelationToken, Promise<AsyncEnumerable<TCollection>>>
                where TResultSelector : IFunc<TSource, TCollection, CancelationToken, Promise<TResult>>
            {
                private readonly ConfiguredAsyncEnumerable<TSource>.Enumerator _configuredAsyncEnumerator;
                private readonly TCollectionSelector _collectionSelector;
                private readonly TResultSelector _resultSelector;

                internal ConfiguredSelectManyWithIndexAsyncIterator(ConfiguredAsyncEnumerable<TSource>.Enumerator configuredAsyncEnumerator, TCollectionSelector collectionSelector, TResultSelector resultSelector)
                {
                    _configuredAsyncEnumerator = configuredAsyncEnumerator;
                    _collectionSelector = collectionSelector;
                    _resultSelector = resultSelector;
                }

                public async AsyncIteratorMethod Start(AsyncStreamWriter<TResult> writer, CancelationToken cancelationToken)
                {
                    // The enumerator may have been configured with a cancelation token. We need to join the passed in token before starting iteration.
                    var maybeJoinedCancelationSource = MaybeJoinCancelationTokens(ref cancelationToken, ref _configuredAsyncEnumerator._enumerator._target._cancelationToken);

                    // We don't dispose the enumerators until the owner is disposed.
                    // This is in case any enumerator contains TempCollections that they will still be valid until the owner is disposed.
                    var enumerators = new TempCollectionBuilder<AsyncEnumerator<TCollection>>(0);
                    try
                    {
                        int i = 0;
                        while (await _configuredAsyncEnumerator.MoveNextAsync())
                        {
                            var outerResult = _configuredAsyncEnumerator.Current;
                            var innerEnumerator = (await _collectionSelector.Invoke(outerResult, checked(i++), cancelationToken)).GetAsyncEnumerator(cancelationToken);
                            enumerators.Add(innerEnumerator);
                            while (await innerEnumerator.MoveNextAsync())
                            {
                                await writer.YieldAsync(await _resultSelector.Invoke(outerResult, innerEnumerator.Current, cancelationToken));
                            }
                        }

                        // We yield and wait for the enumerator to be disposed, but only if there were no exceptions.
                        await writer.YieldAsync(default).ForLinqExtension();
                    }
                    finally
                    {
                        maybeJoinedCancelationSource.Dispose();
                        try
                        {
                            await _configuredAsyncEnumerator.DisposeAsync();
                        }
                        finally
                        {
                            // We can't do a try/finally loop to dispose all of the enumerators,
                            // so we simulate the behavior by only capturing the last exception.
                            Exception ex = null;
                            for (int i = 0; i < enumerators._count; ++i)
                            {
                                try
                                {
                                    await enumerators[i].DisposeAsync();
                                }
                                catch (Exception e)
                                {
                                    ex = e;
                                }
                            }
                            enumerators.Dispose();
                            if (ex != null)
                            {
                                ExceptionDispatchInfo.Capture(ex).Throw();
                            }
                        }
                    }
                }

                [MethodImpl(InlineOption)]
                public Promise DisposeAsyncWithoutStart()
                    => _configuredAsyncEnumerator.DisposeAsync();
            }

            internal static AsyncEnumerable<TResult> SelectManyWithIndex<TSource, TCollectionSelector, TResultSelector>(ConfiguredAsyncEnumerable<TSource>.Enumerator configuredAsyncEnumerator, TCollectionSelector collectionSelector, TResultSelector resultSelector)
                where TCollectionSelector : IFunc<TSource, int, CancelationToken, Promise<AsyncEnumerable<TCollection>>>
                where TResultSelector : IFunc<TSource, TCollection, CancelationToken, Promise<TResult>>
                => AsyncEnumerable<TResult>.Create(new ConfiguredSelectManyWithIndexAsyncIterator<TSource, TCollectionSelector, TResultSelector>(configuredAsyncEnumerator, collectionSelector, resultSelector));
        } // class SelectManyHelper<TCollection, TResult>
    } // class Internal
} // namespace Proto.Promises