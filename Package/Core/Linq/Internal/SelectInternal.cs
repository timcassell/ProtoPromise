#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using Proto.Promises.CompilerServices;
using Proto.Promises.Linq;
using System.Diagnostics;

namespace Proto.Promises
{
#if CSHARP_7_3_OR_NEWER
    partial class Internal
    {
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal static class SelectHelper<TResult>
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private readonly struct SelectSyncIterator<TSource, TSelector> : IAsyncIterator<TResult>
                where TSelector : IFunc<TSource, TResult>
            {
                private readonly AsyncEnumerator<TSource> _asyncEnumerator;
                private readonly TSelector _selector;

                internal SelectSyncIterator(AsyncEnumerator<TSource> asyncEnumerator, TSelector selector)
                {
                    _asyncEnumerator = asyncEnumerator;
                    _selector = selector;
                }

                public async AsyncEnumerableMethod Start(AsyncStreamWriter<TResult> writer, CancelationToken cancelationToken)
                {
                    // The enumerator was retrieved without a cancelation token when the original function was called.
                    // We need to propagate the token that was passed in, so we assign it before starting iteration.
                    _asyncEnumerator._target._cancelationToken = cancelationToken;
                    try
                    {
                        while (await _asyncEnumerator.MoveNextAsync())
                        {
                            await writer.YieldAsync(_selector.Invoke(_asyncEnumerator.Current));
                        }
                        // We don't dispose the source enumerator until the owner is disposed.
                        // This is in case the source enumerator contains TempCollection that they will still be valid until the owner is disposed.

                        // We yield and wait for the enumerator to be disposed, but only if there were no exceptions.
                        await writer.YieldAsync(default).ForLinqExtension();
                    }
                    finally
                    {
                        await _asyncEnumerator.DisposeAsync();
                    }
                }
            }

            internal static AsyncEnumerable<TResult> Select<TSource, TSelector>(AsyncEnumerator<TSource> source, TSelector selector)
                where TSelector : IFunc<TSource, TResult>
            {
                var enumerable = AsyncEnumerableCreate<TResult, SelectSyncIterator<TSource, TSelector>>.GetOrCreate(
                    new SelectSyncIterator<TSource, TSelector>(source, selector));
                return new AsyncEnumerable<TResult>(enumerable);
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private readonly struct SelectAsyncIterator<TSource, TSelector> : IAsyncIterator<TResult>
                where TSelector : IFunc<TSource, Promise<TResult>>
            {
                private readonly AsyncEnumerator<TSource> _asyncEnumerator;
                private readonly TSelector _selector;

                internal SelectAsyncIterator(AsyncEnumerator<TSource> asyncEnumerator, TSelector selector)
                {
                    _asyncEnumerator = asyncEnumerator;
                    _selector = selector;
                }

                public async AsyncEnumerableMethod Start(AsyncStreamWriter<TResult> writer, CancelationToken cancelationToken)
                {
                    // The enumerator was retrieved without a cancelation token when the original function was called.
                    // We need to propagate the token that was passed in, so we assign it before starting iteration.
                    _asyncEnumerator._target._cancelationToken = cancelationToken;
                    try
                    {
                        while (await _asyncEnumerator.MoveNextAsync())
                        {
                            await writer.YieldAsync(await _selector.Invoke(_asyncEnumerator.Current));
                        }
                        // We don't dispose the source enumerator until the owner is disposed.
                        // This is in case the source enumerator contains TempCollection that they will still be valid until the owner is disposed.

                        // We yield and wait for the enumerator to be disposed, but only if there were no exceptions.
                        await writer.YieldAsync(default).ForLinqExtension();
                    }
                    finally
                    {
                        await _asyncEnumerator.DisposeAsync();
                    }
                }
            }

            internal static AsyncEnumerable<TResult> SelectAwait<TSource, TSelector>(AsyncEnumerator<TSource> source, TSelector selector)
                where TSelector : IFunc<TSource, Promise<TResult>>
            {
                var enumerable = AsyncEnumerableCreate<TResult, SelectAsyncIterator<TSource, TSelector>>.GetOrCreate(
                    new SelectAsyncIterator<TSource, TSelector>(source, selector));
                return new AsyncEnumerable<TResult>(enumerable);
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private readonly struct ConfiguredSelectSyncIterator<TSource, TSelector> : IAsyncIterator<TResult>
                where TSelector : IFunc<TSource, TResult>
            {
                private readonly ConfiguredAsyncEnumerable<TSource>.Enumerator _configuredAsyncEnumerator;
                private readonly TSelector _selector;

                internal ConfiguredSelectSyncIterator(ConfiguredAsyncEnumerable<TSource>.Enumerator configuredAsyncEnumerator, TSelector selector)
                {
                    _configuredAsyncEnumerator = configuredAsyncEnumerator;
                    _selector = selector;
                }

                public async AsyncEnumerableMethod Start(AsyncStreamWriter<TResult> writer, CancelationToken cancelationToken)
                {
                    // The enumerator may have been configured with a cancelation token. We need to join the passed in token before starting iteration.
                    var enumerableRef = _configuredAsyncEnumerator._enumerator._target;
                    var joinedCancelationSource = MaybeJoinCancelationTokens(enumerableRef._cancelationToken, cancelationToken, out enumerableRef._cancelationToken);

                    try
                    {
                        while (await _configuredAsyncEnumerator.MoveNextAsync())
                        {
                            await writer.YieldAsync(_selector.Invoke(_configuredAsyncEnumerator.Current));
                        }
                        // We don't dispose the source enumerator until the owner is disposed.
                        // This is in case the source enumerator contains TempCollection that they will still be valid until the owner is disposed.

                        // We yield and wait for the enumerator to be disposed, but only if there were no exceptions.
                        await writer.YieldAsync(default).ForLinqExtension();
                    }
                    finally
                    {
                        joinedCancelationSource.TryDispose();
                        await _configuredAsyncEnumerator.DisposeAsync();
                    }
                }
            }

            internal static AsyncEnumerable<TResult> Select<TSource, TSelector>(ConfiguredAsyncEnumerable<TSource>.Enumerator configuredSource, TSelector selector)
                where TSelector : IFunc<TSource, TResult>
            {
                var enumerable = AsyncEnumerableCreate<TResult, ConfiguredSelectSyncIterator<TSource, TSelector>>.GetOrCreate(
                    new ConfiguredSelectSyncIterator<TSource, TSelector>(configuredSource, selector));
                return new AsyncEnumerable<TResult>(enumerable);
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private readonly struct ConfiguredSelectAsyncIterator<TSource, TSelector> : IAsyncIterator<TResult>
                where TSelector : IFunc<TSource, Promise<TResult>>
            {
                private readonly ConfiguredAsyncEnumerable<TSource>.Enumerator _configuredAsyncEnumerator;
                private readonly TSelector _selector;

                internal ConfiguredSelectAsyncIterator(ConfiguredAsyncEnumerable<TSource>.Enumerator configuredAsyncEnumerator, TSelector selector)
                {
                    _configuredAsyncEnumerator = configuredAsyncEnumerator;
                    _selector = selector;
                }

                public async AsyncEnumerableMethod Start(AsyncStreamWriter<TResult> writer, CancelationToken cancelationToken)
                {
                    // The enumerator may have been configured with a cancelation token. We need to join the passed in token before starting iteration.
                    var enumerableRef = _configuredAsyncEnumerator._enumerator._target;
                    var joinedCancelationSource = MaybeJoinCancelationTokens(enumerableRef._cancelationToken, cancelationToken, out enumerableRef._cancelationToken);

                    try
                    {
                        while (await _configuredAsyncEnumerator.MoveNextAsync())
                        {
                            await writer.YieldAsync(await _selector.Invoke(_configuredAsyncEnumerator.Current));
                        }
                        // We don't dispose the source enumerator until the owner is disposed.
                        // This is in case the source enumerator contains TempCollection that they will still be valid until the owner is disposed.

                        // We yield and wait for the enumerator to be disposed, but only if there were no exceptions.
                        await writer.YieldAsync(default).ForLinqExtension();
                    }
                    finally
                    {
                        joinedCancelationSource.TryDispose();
                        await _configuredAsyncEnumerator.DisposeAsync();
                    }
                }
            }

            internal static AsyncEnumerable<TResult> SelectAwait<TSource, TSelector>(ConfiguredAsyncEnumerable<TSource>.Enumerator configuredSource, TSelector selector)
                where TSelector : IFunc<TSource, Promise<TResult>>
            {
                var enumerable = AsyncEnumerableCreate<TResult, ConfiguredSelectAsyncIterator<TSource, TSelector>>.GetOrCreate(
                    new ConfiguredSelectAsyncIterator<TSource, TSelector>(configuredSource, selector));
                return new AsyncEnumerable<TResult>(enumerable);
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private readonly struct SelectWithIndexSyncIterator<TSource, TSelector> : IAsyncIterator<TResult>
                where TSelector : IFunc<TSource, int, TResult>
            {
                private readonly AsyncEnumerator<TSource> _asyncEnumerator;
                private readonly TSelector _selector;

                internal SelectWithIndexSyncIterator(AsyncEnumerator<TSource> asyncEnumerator, TSelector selector)
                {
                    _asyncEnumerator = asyncEnumerator;
                    _selector = selector;
                }

                public async AsyncEnumerableMethod Start(AsyncStreamWriter<TResult> writer, CancelationToken cancelationToken)
                {
                    // The enumerator was retrieved without a cancelation token when the original function was called.
                    // We need to propagate the token that was passed in, so we assign it before starting iteration.
                    _asyncEnumerator._target._cancelationToken = cancelationToken;
                    try
                    {
                        int i = 0;
                        while (await _asyncEnumerator.MoveNextAsync())
                        {
                            await writer.YieldAsync(_selector.Invoke(_asyncEnumerator.Current, checked(i++)));
                        }
                        // We don't dispose the source enumerator until the owner is disposed.
                        // This is in case the source enumerator contains TempCollection that they will still be valid until the owner is disposed.

                        // We yield and wait for the enumerator to be disposed, but only if there were no exceptions.
                        await writer.YieldAsync(default).ForLinqExtension();
                    }
                    finally
                    {
                        await _asyncEnumerator.DisposeAsync();
                    }
                }
            }

            internal static AsyncEnumerable<TResult> SelectWithIndex<TSource, TSelector>(AsyncEnumerator<TSource> source, TSelector selector)
                where TSelector : IFunc<TSource, int, TResult>
            {
                var enumerable = AsyncEnumerableCreate<TResult, SelectWithIndexSyncIterator<TSource, TSelector>>.GetOrCreate(
                    new SelectWithIndexSyncIterator<TSource, TSelector>(source, selector));
                return new AsyncEnumerable<TResult>(enumerable);
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private readonly struct SelectWithIndexAsyncIterator<TSource, TSelector> : IAsyncIterator<TResult>
                where TSelector : IFunc<TSource, int, Promise<TResult>>
            {
                private readonly AsyncEnumerator<TSource> _asyncEnumerator;
                private readonly TSelector _selector;

                internal SelectWithIndexAsyncIterator(AsyncEnumerator<TSource> asyncEnumerator, TSelector selector)
                {
                    _asyncEnumerator = asyncEnumerator;
                    _selector = selector;
                }

                public async AsyncEnumerableMethod Start(AsyncStreamWriter<TResult> writer, CancelationToken cancelationToken)
                {
                    // The enumerator was retrieved without a cancelation token when the original function was called.
                    // We need to propagate the token that was passed in, so we assign it before starting iteration.
                    _asyncEnumerator._target._cancelationToken = cancelationToken;
                    try
                    {
                        int i = 0;
                        while (await _asyncEnumerator.MoveNextAsync())
                        {
                            await writer.YieldAsync(await _selector.Invoke(_asyncEnumerator.Current, checked(i++)));
                        }
                        // We don't dispose the source enumerator until the owner is disposed.
                        // This is in case the source enumerator contains TempCollection that they will still be valid until the owner is disposed.

                        // We yield and wait for the enumerator to be disposed, but only if there were no exceptions.
                        await writer.YieldAsync(default).ForLinqExtension();
                    }
                    finally
                    {
                        await _asyncEnumerator.DisposeAsync();
                    }
                }
            }

            internal static AsyncEnumerable<TResult> SelectWithIndexAwait<TSource, TSelector>(AsyncEnumerator<TSource> source, TSelector selector)
                where TSelector : IFunc<TSource, int, Promise<TResult>>
            {
                var enumerable = AsyncEnumerableCreate<TResult, SelectWithIndexAsyncIterator<TSource, TSelector>>.GetOrCreate(
                    new SelectWithIndexAsyncIterator<TSource, TSelector>(source, selector));
                return new AsyncEnumerable<TResult>(enumerable);
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private readonly struct ConfiguredSelectWithIndexSyncIterator<TSource, TSelector> : IAsyncIterator<TResult>
                where TSelector : IFunc<TSource, int, TResult>
            {
                private readonly ConfiguredAsyncEnumerable<TSource>.Enumerator _configuredAsyncEnumerator;
                private readonly TSelector _selector;

                internal ConfiguredSelectWithIndexSyncIterator(ConfiguredAsyncEnumerable<TSource>.Enumerator configuredAsyncEnumerator, TSelector selector)
                {
                    _configuredAsyncEnumerator = configuredAsyncEnumerator;
                    _selector = selector;
                }

                public async AsyncEnumerableMethod Start(AsyncStreamWriter<TResult> writer, CancelationToken cancelationToken)
                {
                    // The enumerator may have been configured with a cancelation token. We need to join the passed in token before starting iteration.
                    var enumerableRef = _configuredAsyncEnumerator._enumerator._target;
                    var joinedCancelationSource = MaybeJoinCancelationTokens(enumerableRef._cancelationToken, cancelationToken, out enumerableRef._cancelationToken);

                    try
                    {
                        int i = 0;
                        while (await _configuredAsyncEnumerator.MoveNextAsync())
                        {
                            await writer.YieldAsync(_selector.Invoke(_configuredAsyncEnumerator.Current, checked(i++)));
                        }
                        // We don't dispose the source enumerator until the owner is disposed.
                        // This is in case the source enumerator contains TempCollection that they will still be valid until the owner is disposed.

                        // We yield and wait for the enumerator to be disposed, but only if there were no exceptions.
                        await writer.YieldAsync(default).ForLinqExtension();
                    }
                    finally
                    {
                        joinedCancelationSource.TryDispose();
                        await _configuredAsyncEnumerator.DisposeAsync();
                    }
                }
            }

            internal static AsyncEnumerable<TResult> SelectWithIndex<TSource, TSelector>(ConfiguredAsyncEnumerable<TSource>.Enumerator configuredSource, TSelector selector)
                where TSelector : IFunc<TSource, int, TResult>
            {
                var enumerable = AsyncEnumerableCreate<TResult, ConfiguredSelectWithIndexSyncIterator<TSource, TSelector>>.GetOrCreate(
                    new ConfiguredSelectWithIndexSyncIterator<TSource, TSelector>(configuredSource, selector));
                return new AsyncEnumerable<TResult>(enumerable);
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private readonly struct ConfiguredSelectWithIndexAsyncIterator<TSource, TSelector> : IAsyncIterator<TResult>
                where TSelector : IFunc<TSource, int, Promise<TResult>>
            {
                private readonly ConfiguredAsyncEnumerable<TSource>.Enumerator _configuredAsyncEnumerator;
                private readonly TSelector _selector;

                internal ConfiguredSelectWithIndexAsyncIterator(ConfiguredAsyncEnumerable<TSource>.Enumerator configuredAsyncEnumerator, TSelector selector)
                {
                    _configuredAsyncEnumerator = configuredAsyncEnumerator;
                    _selector = selector;
                }

                public async AsyncEnumerableMethod Start(AsyncStreamWriter<TResult> writer, CancelationToken cancelationToken)
                {
                    // The enumerator may have been configured with a cancelation token. We need to join the passed in token before starting iteration.
                    var enumerableRef = _configuredAsyncEnumerator._enumerator._target;
                    var joinedCancelationSource = MaybeJoinCancelationTokens(enumerableRef._cancelationToken, cancelationToken, out enumerableRef._cancelationToken);

                    try
                    {
                        int i = 0;
                        while (await _configuredAsyncEnumerator.MoveNextAsync())
                        {
                            await writer.YieldAsync(await _selector.Invoke(_configuredAsyncEnumerator.Current, checked(i++)));
                        }
                        // We don't dispose the source enumerator until the owner is disposed.
                        // This is in case the source enumerator contains TempCollection that they will still be valid until the owner is disposed.

                        // We yield and wait for the enumerator to be disposed, but only if there were no exceptions.
                        await writer.YieldAsync(default).ForLinqExtension();
                    }
                    finally
                    {
                        joinedCancelationSource.TryDispose();
                        await _configuredAsyncEnumerator.DisposeAsync();
                    }
                }
            }

            internal static AsyncEnumerable<TResult> SelectWithIndexAwait<TSource, TSelector>(ConfiguredAsyncEnumerable<TSource>.Enumerator configuredSource, TSelector selector)
                where TSelector : IFunc<TSource, int, Promise<TResult>>
            {
                var enumerable = AsyncEnumerableCreate<TResult, ConfiguredSelectWithIndexAsyncIterator<TSource, TSelector>>.GetOrCreate(
                    new ConfiguredSelectWithIndexAsyncIterator<TSource, TSelector>(configuredSource, selector));
                return new AsyncEnumerable<TResult>(enumerable);
            }
        }
    } // class Internal
#endif
} // namespace Proto.Promises