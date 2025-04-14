#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using Proto.Promises.CompilerServices;
using Proto.Promises.Linq;
using Proto.Promises.Linq.Sources;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Proto.Promises
{
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

                public async AsyncIteratorMethod Start(AsyncStreamWriter<TResult> writer, CancelationToken cancelationToken)
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

                        await AsyncEnumerableSourceHelpers.WaitForDisposeAsync(writer);
                    }
                    finally
                    {
                        await _asyncEnumerator.DisposeAsync();
                    }
                }

                [MethodImpl(InlineOption)]
                public Promise DisposeAsyncWithoutStart()
                    => _asyncEnumerator.DisposeAsync();
            }

            internal static AsyncEnumerable<TResult> Select<TSource, TSelector>(AsyncEnumerator<TSource> source, TSelector selector)
                where TSelector : IFunc<TSource, TResult>
            {
                return AsyncEnumerable<TResult>.Create(new SelectSyncIterator<TSource, TSelector>(source, selector));
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

                public async AsyncIteratorMethod Start(AsyncStreamWriter<TResult> writer, CancelationToken cancelationToken)
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

                        await AsyncEnumerableSourceHelpers.WaitForDisposeAsync(writer);
                    }
                    finally
                    {
                        await _asyncEnumerator.DisposeAsync();
                    }
                }

                [MethodImpl(InlineOption)]
                public Promise DisposeAsyncWithoutStart()
                    => _asyncEnumerator.DisposeAsync();
            }

            internal static AsyncEnumerable<TResult> SelectAwait<TSource, TSelector>(AsyncEnumerator<TSource> source, TSelector selector)
                where TSelector : IFunc<TSource, Promise<TResult>>
            {
                return AsyncEnumerable<TResult>.Create(new SelectAsyncIterator<TSource, TSelector>(source, selector));
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

                public async AsyncIteratorMethod Start(AsyncStreamWriter<TResult> writer, CancelationToken cancelationToken)
                {
                    // The enumerator may have been configured with a cancelation token. We need to join the passed in token before starting iteration.
                    var enumerableRef = _configuredAsyncEnumerator._enumerator._target;
                    var maybeJoinedCancelationSource = MaybeJoinCancelationTokens(enumerableRef._cancelationToken, cancelationToken, out enumerableRef._cancelationToken);

                    try
                    {
                        while (await _configuredAsyncEnumerator.MoveNextAsync())
                        {
                            await writer.YieldAsync(_selector.Invoke(_configuredAsyncEnumerator.Current));
                        }

                        await AsyncEnumerableSourceHelpers.WaitForDisposeAsync(writer);
                    }
                    finally
                    {
                        maybeJoinedCancelationSource.Dispose();
                        await _configuredAsyncEnumerator.DisposeAsync();
                    }
                }

                [MethodImpl(InlineOption)]
                public Promise DisposeAsyncWithoutStart()
                    => _configuredAsyncEnumerator.DisposeAsync();
            }

            internal static AsyncEnumerable<TResult> Select<TSource, TSelector>(ConfiguredAsyncEnumerable<TSource>.Enumerator configuredSource, TSelector selector)
                where TSelector : IFunc<TSource, TResult>
            {
                return AsyncEnumerable<TResult>.Create(new ConfiguredSelectSyncIterator<TSource, TSelector>(configuredSource, selector));
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

                public async AsyncIteratorMethod Start(AsyncStreamWriter<TResult> writer, CancelationToken cancelationToken)
                {
                    // The enumerator may have been configured with a cancelation token. We need to join the passed in token before starting iteration.
                    var enumerableRef = _configuredAsyncEnumerator._enumerator._target;
                    var maybeJoinedCancelationSource = MaybeJoinCancelationTokens(enumerableRef._cancelationToken, cancelationToken, out enumerableRef._cancelationToken);

                    try
                    {
                        while (await _configuredAsyncEnumerator.MoveNextAsync())
                        {
                            await writer.YieldAsync(await _selector.Invoke(_configuredAsyncEnumerator.Current));
                        }

                        await AsyncEnumerableSourceHelpers.WaitForDisposeAsync(writer);
                    }
                    finally
                    {
                        maybeJoinedCancelationSource.Dispose();
                        await _configuredAsyncEnumerator.DisposeAsync();
                    }
                }

                [MethodImpl(InlineOption)]
                public Promise DisposeAsyncWithoutStart()
                    => _configuredAsyncEnumerator.DisposeAsync();
            }

            internal static AsyncEnumerable<TResult> SelectAwait<TSource, TSelector>(ConfiguredAsyncEnumerable<TSource>.Enumerator configuredSource, TSelector selector)
                where TSelector : IFunc<TSource, Promise<TResult>>
            {
                return AsyncEnumerable<TResult>.Create(new ConfiguredSelectAsyncIterator<TSource, TSelector>(configuredSource, selector));
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

                public async AsyncIteratorMethod Start(AsyncStreamWriter<TResult> writer, CancelationToken cancelationToken)
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

                        await AsyncEnumerableSourceHelpers.WaitForDisposeAsync(writer);
                    }
                    finally
                    {
                        await _asyncEnumerator.DisposeAsync();
                    }
                }

                [MethodImpl(InlineOption)]
                public Promise DisposeAsyncWithoutStart()
                    => _asyncEnumerator.DisposeAsync();
            }

            internal static AsyncEnumerable<TResult> SelectWithIndex<TSource, TSelector>(AsyncEnumerator<TSource> source, TSelector selector)
                where TSelector : IFunc<TSource, int, TResult>
            {
                return AsyncEnumerable<TResult>.Create(new SelectWithIndexSyncIterator<TSource, TSelector>(source, selector));
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

                public async AsyncIteratorMethod Start(AsyncStreamWriter<TResult> writer, CancelationToken cancelationToken)
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

                        await AsyncEnumerableSourceHelpers.WaitForDisposeAsync(writer);
                    }
                    finally
                    {
                        await _asyncEnumerator.DisposeAsync();
                    }
                }

                [MethodImpl(InlineOption)]
                public Promise DisposeAsyncWithoutStart()
                    => _asyncEnumerator.DisposeAsync();
            }

            internal static AsyncEnumerable<TResult> SelectWithIndexAwait<TSource, TSelector>(AsyncEnumerator<TSource> source, TSelector selector)
                where TSelector : IFunc<TSource, int, Promise<TResult>>
            {
                return AsyncEnumerable<TResult>.Create(new SelectWithIndexAsyncIterator<TSource, TSelector>(source, selector));
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

                public async AsyncIteratorMethod Start(AsyncStreamWriter<TResult> writer, CancelationToken cancelationToken)
                {
                    // The enumerator may have been configured with a cancelation token. We need to join the passed in token before starting iteration.
                    var enumerableRef = _configuredAsyncEnumerator._enumerator._target;
                    var maybeJoinedCancelationSource = MaybeJoinCancelationTokens(enumerableRef._cancelationToken, cancelationToken, out enumerableRef._cancelationToken);

                    try
                    {
                        int i = 0;
                        while (await _configuredAsyncEnumerator.MoveNextAsync())
                        {
                            await writer.YieldAsync(_selector.Invoke(_configuredAsyncEnumerator.Current, checked(i++)));
                        }

                        await AsyncEnumerableSourceHelpers.WaitForDisposeAsync(writer);
                    }
                    finally
                    {
                        maybeJoinedCancelationSource.Dispose();
                        await _configuredAsyncEnumerator.DisposeAsync();
                    }
                }

                [MethodImpl(InlineOption)]
                public Promise DisposeAsyncWithoutStart()
                    => _configuredAsyncEnumerator.DisposeAsync();
            }

            internal static AsyncEnumerable<TResult> SelectWithIndex<TSource, TSelector>(ConfiguredAsyncEnumerable<TSource>.Enumerator configuredSource, TSelector selector)
                where TSelector : IFunc<TSource, int, TResult>
            {
                return AsyncEnumerable<TResult>.Create(new ConfiguredSelectWithIndexSyncIterator<TSource, TSelector>(configuredSource, selector));
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

                public async AsyncIteratorMethod Start(AsyncStreamWriter<TResult> writer, CancelationToken cancelationToken)
                {
                    // The enumerator may have been configured with a cancelation token. We need to join the passed in token before starting iteration.
                    var enumerableRef = _configuredAsyncEnumerator._enumerator._target;
                    var maybeJoinedCancelationSource = MaybeJoinCancelationTokens(enumerableRef._cancelationToken, cancelationToken, out enumerableRef._cancelationToken);

                    try
                    {
                        int i = 0;
                        while (await _configuredAsyncEnumerator.MoveNextAsync())
                        {
                            await writer.YieldAsync(await _selector.Invoke(_configuredAsyncEnumerator.Current, checked(i++)));
                        }

                        await AsyncEnumerableSourceHelpers.WaitForDisposeAsync(writer);
                    }
                    finally
                    {
                        maybeJoinedCancelationSource.Dispose();
                        await _configuredAsyncEnumerator.DisposeAsync();
                    }
                }

                [MethodImpl(InlineOption)]
                public Promise DisposeAsyncWithoutStart()
                    => _configuredAsyncEnumerator.DisposeAsync();
            }

            internal static AsyncEnumerable<TResult> SelectWithIndexAwait<TSource, TSelector>(ConfiguredAsyncEnumerable<TSource>.Enumerator configuredSource, TSelector selector)
                where TSelector : IFunc<TSource, int, Promise<TResult>>
            {
                return AsyncEnumerable<TResult>.Create(new ConfiguredSelectWithIndexAsyncIterator<TSource, TSelector>(configuredSource, selector));
            }
        }
    } // class Internal
} // namespace Proto.Promises