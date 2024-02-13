#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using Proto.Promises.CompilerServices;
using Proto.Promises.Linq;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Proto.Promises
{
    partial class Internal
    {
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal static class WhereHelper
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private readonly struct WhereSyncIterator<TSource, TPredicate> : IAsyncIterator<TSource>
                where TPredicate : IFunc<TSource, bool>
            {
                private readonly AsyncEnumerator<TSource> _asyncEnumerator;
                private readonly TPredicate _predicate;

                internal WhereSyncIterator(AsyncEnumerator<TSource> asyncEnumerator, TPredicate predicate)
                {
                    _asyncEnumerator = asyncEnumerator;
                    _predicate = predicate;
                }

                public async AsyncIteratorMethod Start(AsyncStreamWriter<TSource> writer, CancelationToken cancelationToken)
                {
                    // The enumerator was retrieved without a cancelation token when the original function was called.
                    // We need to propagate the token that was passed in, so we assign it before starting iteration.
                    _asyncEnumerator._target._cancelationToken = cancelationToken;

                    try
                    {
                        while (await _asyncEnumerator.MoveNextAsync())
                        {
                            var element = _asyncEnumerator.Current;
                            if (_predicate.Invoke(element))
                            {
                                await writer.YieldAsync(element);
                            }
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

                [MethodImpl(InlineOption)]
                public Promise DisposeAsyncWithoutStart()
                    => _asyncEnumerator.DisposeAsync();
            }

            internal static AsyncEnumerable<TSource> Where<TSource, TPredicate>(AsyncEnumerator<TSource> source, TPredicate predicate)
                where TPredicate : IFunc<TSource, bool>
            {
                return AsyncEnumerable<TSource>.Create(new WhereSyncIterator<TSource, TPredicate>(source, predicate));
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private readonly struct WhereAsyncIterator<TSource, TPredicate> : IAsyncIterator<TSource>
                where TPredicate : IFunc<TSource, Promise<bool>>
            {
                private readonly AsyncEnumerator<TSource> _asyncEnumerator;
                private readonly TPredicate _predicate;

                internal WhereAsyncIterator(AsyncEnumerator<TSource> asyncEnumerator, TPredicate predicate)
                {
                    _asyncEnumerator = asyncEnumerator;
                    _predicate = predicate;
                }

                public async AsyncIteratorMethod Start(AsyncStreamWriter<TSource> writer, CancelationToken cancelationToken)
                {
                    // The enumerator was retrieved without a cancelation token when the original function was called.
                    // We need to propagate the token that was passed in, so we assign it before starting iteration.
                    _asyncEnumerator._target._cancelationToken = cancelationToken;

                    try
                    {
                        while (await _asyncEnumerator.MoveNextAsync())
                        {
                            var element = _asyncEnumerator.Current;
                            if (await _predicate.Invoke(element))
                            {
                                await writer.YieldAsync(element);
                            }
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

                [MethodImpl(InlineOption)]
                public Promise DisposeAsyncWithoutStart()
                    => _asyncEnumerator.DisposeAsync();
            }

            internal static AsyncEnumerable<TSource> WhereAwait<TSource, TPredicate>(AsyncEnumerator<TSource> source, TPredicate predicate)
                where TPredicate : IFunc<TSource, Promise<bool>>
            {
                return AsyncEnumerable<TSource>.Create(new WhereAsyncIterator<TSource, TPredicate>(source, predicate));
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private readonly struct ConfiguredWhereSyncIterator<TSource, TPredicate> : IAsyncIterator<TSource>
                where TPredicate : IFunc<TSource, bool>
            {
                private readonly ConfiguredAsyncEnumerable<TSource>.Enumerator _configuredAsyncEnumerator;
                private readonly TPredicate _predicate;

                internal ConfiguredWhereSyncIterator(ConfiguredAsyncEnumerable<TSource>.Enumerator configuredAsyncEnumerator, TPredicate predicate)
                {
                    _configuredAsyncEnumerator = configuredAsyncEnumerator;
                    _predicate = predicate;
                }

                public async AsyncIteratorMethod Start(AsyncStreamWriter<TSource> writer, CancelationToken cancelationToken)
                {
                    // The enumerator may have been configured with a cancelation token. We need to join the passed in token before starting iteration.
                    var enumerableRef = _configuredAsyncEnumerator._enumerator._target;
                    var joinedCancelationSource = MaybeJoinCancelationTokens(enumerableRef._cancelationToken, cancelationToken, out enumerableRef._cancelationToken);

                    try
                    {
                        while (await _configuredAsyncEnumerator.MoveNextAsync())
                        {
                            var element = _configuredAsyncEnumerator.Current;
                            if (_predicate.Invoke(element))
                            {
                                await writer.YieldAsync(element);
                            }
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

                [MethodImpl(InlineOption)]
                public Promise DisposeAsyncWithoutStart()
                    => _configuredAsyncEnumerator.DisposeAsync();
            }

            internal static AsyncEnumerable<TSource> Where<TSource, TPredicate>(ConfiguredAsyncEnumerable<TSource>.Enumerator configuredSource, TPredicate predicate)
                where TPredicate : IFunc<TSource, bool>
            {
                return AsyncEnumerable<TSource>.Create(new ConfiguredWhereSyncIterator<TSource, TPredicate>(configuredSource, predicate));
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private readonly struct ConfiguredWhereAsyncIterator<TSource, TPredicate> : IAsyncIterator<TSource>
                where TPredicate : IFunc<TSource, Promise<bool>>
            {
                private readonly ConfiguredAsyncEnumerable<TSource>.Enumerator _configuredAsyncEnumerator;
                private readonly TPredicate _predicate;

                internal ConfiguredWhereAsyncIterator(ConfiguredAsyncEnumerable<TSource>.Enumerator configuredAsyncEnumerator, TPredicate predicate)
                {
                    _configuredAsyncEnumerator = configuredAsyncEnumerator;
                    _predicate = predicate;
                }

                public async AsyncIteratorMethod Start(AsyncStreamWriter<TSource> writer, CancelationToken cancelationToken)
                {
                    // The enumerator may have been configured with a cancelation token. We need to join the passed in token before starting iteration.
                    var enumerableRef = _configuredAsyncEnumerator._enumerator._target;
                    var joinedCancelationSource = MaybeJoinCancelationTokens(enumerableRef._cancelationToken, cancelationToken, out enumerableRef._cancelationToken);

                    try
                    {
                        while (await _configuredAsyncEnumerator.MoveNextAsync())
                        {
                            var element = _configuredAsyncEnumerator.Current;
                            if (await _predicate.Invoke(element))
                            {
                                await writer.YieldAsync(element);
                            }
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

                [MethodImpl(InlineOption)]
                public Promise DisposeAsyncWithoutStart()
                    => _configuredAsyncEnumerator.DisposeAsync();
            }

            internal static AsyncEnumerable<TSource> WhereAwait<TSource, TPredicate>(ConfiguredAsyncEnumerable<TSource>.Enumerator configuredSource, TPredicate predicate)
                where TPredicate : IFunc<TSource, Promise<bool>>
            {
                return AsyncEnumerable<TSource>.Create(new ConfiguredWhereAsyncIterator<TSource, TPredicate>(configuredSource, predicate));
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private readonly struct WhereWithIndexSyncIterator<TSource, TPredicate> : IAsyncIterator<TSource>
                where TPredicate : IFunc<TSource, int, bool>
            {
                private readonly AsyncEnumerator<TSource> _asyncEnumerator;
                private readonly TPredicate _predicate;

                internal WhereWithIndexSyncIterator(AsyncEnumerator<TSource> asyncEnumerator, TPredicate predicate)
                {
                    _asyncEnumerator = asyncEnumerator;
                    _predicate = predicate;
                }

                public async AsyncIteratorMethod Start(AsyncStreamWriter<TSource> writer, CancelationToken cancelationToken)
                {
                    // The enumerator was retrieved without a cancelation token when the original function was called.
                    // We need to propagate the token that was passed in, so we assign it before starting iteration.
                    _asyncEnumerator._target._cancelationToken = cancelationToken;

                    try
                    {
                        int i = 0;
                        while (await _asyncEnumerator.MoveNextAsync())
                        {
                            var element = _asyncEnumerator.Current;
                            if (_predicate.Invoke(element, checked(i++)))
                            {
                                await writer.YieldAsync(element);
                            }
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

                [MethodImpl(InlineOption)]
                public Promise DisposeAsyncWithoutStart()
                    => _asyncEnumerator.DisposeAsync();
            }

            internal static AsyncEnumerable<TSource> WhereWithIndex<TSource, TPredicate>(AsyncEnumerator<TSource> source, TPredicate predicate)
                where TPredicate : IFunc<TSource, int, bool>
            {
                return AsyncEnumerable<TSource>.Create(new WhereWithIndexSyncIterator<TSource, TPredicate>(source, predicate));
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private readonly struct WhereWithIndexAsyncIterator<TSource, TPredicate> : IAsyncIterator<TSource>
                where TPredicate : IFunc<TSource, int, Promise<bool>>
            {
                private readonly AsyncEnumerator<TSource> _asyncEnumerator;
                private readonly TPredicate _predicate;

                internal WhereWithIndexAsyncIterator(AsyncEnumerator<TSource> asyncEnumerator, TPredicate predicate)
                {
                    _asyncEnumerator = asyncEnumerator;
                    _predicate = predicate;
                }

                public async AsyncIteratorMethod Start(AsyncStreamWriter<TSource> writer, CancelationToken cancelationToken)
                {
                    // The enumerator was retrieved without a cancelation token when the original function was called.
                    // We need to propagate the token that was passed in, so we assign it before starting iteration.
                    _asyncEnumerator._target._cancelationToken = cancelationToken;
                    try
                    {
                        int i = 0;
                        while (await _asyncEnumerator.MoveNextAsync())
                        {
                            var element = _asyncEnumerator.Current;
                            if (await _predicate.Invoke(element, checked(i++)))
                            {
                                await writer.YieldAsync(element);
                            }
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

                [MethodImpl(InlineOption)]
                public Promise DisposeAsyncWithoutStart()
                    => _asyncEnumerator.DisposeAsync();
            }

            internal static AsyncEnumerable<TSource> WhereWithIndexAwait<TSource, TPredicate>(AsyncEnumerator<TSource> source, TPredicate predicate)
                where TPredicate : IFunc<TSource, int, Promise<bool>>
            {
                return AsyncEnumerable<TSource>.Create(new WhereWithIndexAsyncIterator<TSource, TPredicate>(source, predicate));
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private readonly struct ConfiguredWhereWithIndexSyncIterator<TSource, TPredicate> : IAsyncIterator<TSource>
                where TPredicate : IFunc<TSource, int, bool>
            {
                private readonly ConfiguredAsyncEnumerable<TSource>.Enumerator _configuredAsyncEnumerator;
                private readonly TPredicate _predicate;

                internal ConfiguredWhereWithIndexSyncIterator(ConfiguredAsyncEnumerable<TSource>.Enumerator configuredAsyncEnumerator, TPredicate predicate)
                {
                    _configuredAsyncEnumerator = configuredAsyncEnumerator;
                    _predicate = predicate;
                }

                public async AsyncIteratorMethod Start(AsyncStreamWriter<TSource> writer, CancelationToken cancelationToken)
                {
                    // The enumerator may have been configured with a cancelation token. We need to join the passed in token before starting iteration.
                    var enumerableRef = _configuredAsyncEnumerator._enumerator._target;
                    var joinedCancelationSource = MaybeJoinCancelationTokens(enumerableRef._cancelationToken, cancelationToken, out enumerableRef._cancelationToken);

                    try
                    {
                        int i = 0;
                        while (await _configuredAsyncEnumerator.MoveNextAsync())
                        {
                            var element = _configuredAsyncEnumerator.Current;
                            if (_predicate.Invoke(element, checked(i++)))
                            {
                                await writer.YieldAsync(element);
                            }
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

                [MethodImpl(InlineOption)]
                public Promise DisposeAsyncWithoutStart()
                    => _configuredAsyncEnumerator.DisposeAsync();
            }

            internal static AsyncEnumerable<TSource> WhereWithIndex<TSource, TPredicate>(ConfiguredAsyncEnumerable<TSource>.Enumerator configuredSource, TPredicate predicate)
                where TPredicate : IFunc<TSource, int, bool>
            {
                return AsyncEnumerable<TSource>.Create(new ConfiguredWhereWithIndexSyncIterator<TSource, TPredicate>(configuredSource, predicate));
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private readonly struct ConfiguredWhereWithIndexAsyncIterator<TSource, TPredicate> : IAsyncIterator<TSource>
                where TPredicate : IFunc<TSource, int, Promise<bool>>
            {
                private readonly ConfiguredAsyncEnumerable<TSource>.Enumerator _configuredAsyncEnumerator;
                private readonly TPredicate _predicate;

                internal ConfiguredWhereWithIndexAsyncIterator(ConfiguredAsyncEnumerable<TSource>.Enumerator configuredAsyncEnumerator, TPredicate predicate)
                {
                    _configuredAsyncEnumerator = configuredAsyncEnumerator;
                    _predicate = predicate;
                }

                public async AsyncIteratorMethod Start(AsyncStreamWriter<TSource> writer, CancelationToken cancelationToken)
                {
                    // The enumerator may have been configured with a cancelation token. We need to join the passed in token before starting iteration.
                    var enumerableRef = _configuredAsyncEnumerator._enumerator._target;
                    var joinedCancelationSource = MaybeJoinCancelationTokens(enumerableRef._cancelationToken, cancelationToken, out enumerableRef._cancelationToken);

                    try
                    {
                        int i = 0;
                        while (await _configuredAsyncEnumerator.MoveNextAsync())
                        {
                            var element = _configuredAsyncEnumerator.Current;
                            if (await _predicate.Invoke(element, checked(i++)))
                            {
                                await writer.YieldAsync(element);
                            }
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

                [MethodImpl(InlineOption)]
                public Promise DisposeAsyncWithoutStart()
                    => _configuredAsyncEnumerator.DisposeAsync();
            }

            internal static AsyncEnumerable<TSource> WhereWithIndexAwait<TSource, TPredicate>(ConfiguredAsyncEnumerable<TSource>.Enumerator configuredSource, TPredicate predicate)
                where TPredicate : IFunc<TSource, int, Promise<bool>>
            {
                return AsyncEnumerable<TSource>.Create(new ConfiguredWhereWithIndexAsyncIterator<TSource, TPredicate>(configuredSource, predicate));
            }
        }
    } // class Internal
} // namespace Proto.Promises