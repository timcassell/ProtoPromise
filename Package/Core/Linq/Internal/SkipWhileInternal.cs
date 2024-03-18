#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using Proto.Promises.CompilerServices;
using Proto.Promises.Linq;
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
        internal static class SkipWhileHelper
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private readonly struct SkipWhileSyncIterator<TSource, TPredicate> : IAsyncIterator<TSource>
                where TPredicate : IFunc<TSource, bool>
            {
                private readonly AsyncEnumerator<TSource> _source;
                private readonly TPredicate _predicate;

                internal SkipWhileSyncIterator(AsyncEnumerator<TSource> source, TPredicate predicate)
                {
                    _source = source;
                    _predicate = predicate;
                }

                public Promise DisposeAsyncWithoutStart()
                    => _source.DisposeAsync();

                public async AsyncIteratorMethod Start(AsyncStreamWriter<TSource> writer, CancelationToken cancelationToken)
                {
                    // The enumerator was retrieved without a cancelation token when the original function was called.
                    // We need to propagate the token that was passed in, so we assign it before starting iteration.
                    _source._target._cancelationToken = cancelationToken;

                    try
                    {
                        while (await _source.MoveNextAsync())
                        {
                            var element = _source.Current;
                            if (!_predicate.Invoke(element))
                            {
                                await writer.YieldAsync(element);
                                while (await _source.MoveNextAsync())
                                {
                                    await writer.YieldAsync(_source.Current);
                                }
                                return;
                            }
                        }

                        // We yield and wait for the enumerator to be disposed, but only if there were no exceptions.
                        await writer.YieldAsync(default).ForLinqExtension();
                    }
                    finally
                    {
                        await _source.DisposeAsync();
                    }
                }
            }

            internal static AsyncEnumerable<TSource> SkipWhile<TSource, TPredicate>(AsyncEnumerator<TSource> source, TPredicate predicate)
                where TPredicate : IFunc<TSource, bool>
                => AsyncEnumerable<TSource>.Create(new SkipWhileSyncIterator<TSource, TPredicate>(source, predicate));

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private readonly struct SkipWhileAyncIterator<TSource, TPredicate> : IAsyncIterator<TSource>
                where TPredicate : IFunc<TSource, Promise<bool>>
            {
                private readonly AsyncEnumerator<TSource> _source;
                private readonly TPredicate _predicate;

                internal SkipWhileAyncIterator(AsyncEnumerator<TSource> source, TPredicate predicate)
                {
                    _source = source;
                    _predicate = predicate;
                }

                public Promise DisposeAsyncWithoutStart()
                    => _source.DisposeAsync();

                public async AsyncIteratorMethod Start(AsyncStreamWriter<TSource> writer, CancelationToken cancelationToken)
                {
                    // The enumerator was retrieved without a cancelation token when the original function was called.
                    // We need to propagate the token that was passed in, so we assign it before starting iteration.
                    _source._target._cancelationToken = cancelationToken;

                    try
                    {
                        while (await _source.MoveNextAsync())
                        {
                            var element = _source.Current;
                            if (!await _predicate.Invoke(element))
                            {
                                await writer.YieldAsync(element);
                                while (await _source.MoveNextAsync())
                                {
                                    await writer.YieldAsync(_source.Current);
                                }
                                return;
                            }
                        }

                        // We yield and wait for the enumerator to be disposed, but only if there were no exceptions.
                        await writer.YieldAsync(default).ForLinqExtension();
                    }
                    finally
                    {
                        await _source.DisposeAsync();
                    }
                }
            }

            internal static AsyncEnumerable<TSource> SkipWhileAwait<TSource, TPredicate>(AsyncEnumerator<TSource> source, TPredicate predicate)
                where TPredicate : IFunc<TSource, Promise<bool>>
                => AsyncEnumerable<TSource>.Create(new SkipWhileAyncIterator<TSource, TPredicate>(source, predicate));

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private readonly struct SkipWhileWithIndexSyncIterator<TSource, TPredicate> : IAsyncIterator<TSource>
                where TPredicate : IFunc<TSource, int, bool>
            {
                private readonly AsyncEnumerator<TSource> _source;
                private readonly TPredicate _predicate;

                internal SkipWhileWithIndexSyncIterator(AsyncEnumerator<TSource> source, TPredicate predicate)
                {
                    _source = source;
                    _predicate = predicate;
                }

                public Promise DisposeAsyncWithoutStart()
                    => _source.DisposeAsync();

                public async AsyncIteratorMethod Start(AsyncStreamWriter<TSource> writer, CancelationToken cancelationToken)
                {
                    // The enumerator was retrieved without a cancelation token when the original function was called.
                    // We need to propagate the token that was passed in, so we assign it before starting iteration.
                    _source._target._cancelationToken = cancelationToken;

                    try
                    {
                        int index = -1;
                        while (await _source.MoveNextAsync())
                        {
                            var element = _source.Current;
                            if (!_predicate.Invoke(element, checked(++index)))
                            {
                                await writer.YieldAsync(element);
                                while (await _source.MoveNextAsync())
                                {
                                    await writer.YieldAsync(_source.Current);
                                }
                                return;
                            }
                        }

                        // We yield and wait for the enumerator to be disposed, but only if there were no exceptions.
                        await writer.YieldAsync(default).ForLinqExtension();
                    }
                    finally
                    {
                        await _source.DisposeAsync();
                    }
                }
            }

            internal static AsyncEnumerable<TSource> SkipWhileWithIndex<TSource, TPredicate>(AsyncEnumerator<TSource> source, TPredicate predicate)
                where TPredicate : IFunc<TSource, int, bool>
                => AsyncEnumerable<TSource>.Create(new SkipWhileWithIndexSyncIterator<TSource, TPredicate>(source, predicate));

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private readonly struct SkipWhileWithIndexAyncIterator<TSource, TPredicate> : IAsyncIterator<TSource>
                where TPredicate : IFunc<TSource, int, Promise<bool>>
            {
                private readonly AsyncEnumerator<TSource> _source;
                private readonly TPredicate _predicate;

                internal SkipWhileWithIndexAyncIterator(AsyncEnumerator<TSource> source, TPredicate predicate)
                {
                    _source = source;
                    _predicate = predicate;
                }

                public Promise DisposeAsyncWithoutStart()
                    => _source.DisposeAsync();

                public async AsyncIteratorMethod Start(AsyncStreamWriter<TSource> writer, CancelationToken cancelationToken)
                {
                    // The enumerator was retrieved without a cancelation token when the original function was called.
                    // We need to propagate the token that was passed in, so we assign it before starting iteration.
                    _source._target._cancelationToken = cancelationToken;

                    try
                    {
                        int index = -1;
                        while (await _source.MoveNextAsync())
                        {
                            var element = _source.Current;
                            if (!await _predicate.Invoke(element, checked(++index)))
                            {
                                await writer.YieldAsync(element);
                                while (await _source.MoveNextAsync())
                                {
                                    await writer.YieldAsync(_source.Current);
                                }
                                return;
                            }
                        }

                        // We yield and wait for the enumerator to be disposed, but only if there were no exceptions.
                        await writer.YieldAsync(default).ForLinqExtension();
                    }
                    finally
                    {
                        await _source.DisposeAsync();
                    }
                }
            }

            internal static AsyncEnumerable<TSource> SkipWhileWithIndexAwait<TSource, TPredicate>(AsyncEnumerator<TSource> source, TPredicate predicate)
                where TPredicate : IFunc<TSource, int, Promise<bool>>
                => AsyncEnumerable<TSource>.Create(new SkipWhileWithIndexAyncIterator<TSource, TPredicate>(source, predicate));
            
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private readonly struct ConfiguredSkipWhileSyncIterator<TSource, TPredicate> : IAsyncIterator<TSource>
                where TPredicate : IFunc<TSource, bool>
            {
                private readonly ConfiguredAsyncEnumerable<TSource>.Enumerator _configuredSource;
                private readonly TPredicate _predicate;

                internal ConfiguredSkipWhileSyncIterator(ConfiguredAsyncEnumerable<TSource>.Enumerator configuredSource, TPredicate predicate)
                {
                    _configuredSource = configuredSource;
                    _predicate = predicate;
                }

                public Promise DisposeAsyncWithoutStart()
                    => _configuredSource.DisposeAsync();

                public async AsyncIteratorMethod Start(AsyncStreamWriter<TSource> writer, CancelationToken cancelationToken)
                {
                    // The enumerator may have been configured with a cancelation token. We need to join the passed in token before starting iteration.
                    var enumerableRef = _configuredSource._enumerator._target;
                    var joinedCancelationSource = MaybeJoinCancelationTokens(enumerableRef._cancelationToken, cancelationToken, out enumerableRef._cancelationToken);

                    try
                    {
                        while (await _configuredSource.MoveNextAsync())
                        {
                            var element = _configuredSource.Current;
                            if (!_predicate.Invoke(element))
                            {
                                await writer.YieldAsync(element);
                                while (await _configuredSource.MoveNextAsync())
                                {
                                    await writer.YieldAsync(_configuredSource.Current);
                                }
                                return;
                            }
                        }

                        // We yield and wait for the enumerator to be disposed, but only if there were no exceptions.
                        await writer.YieldAsync(default).ForLinqExtension();
                    }
                    finally
                    {
                        joinedCancelationSource.TryDispose();
                        await _configuredSource.DisposeAsync();
                    }
                }
            }

            internal static AsyncEnumerable<TSource> SkipWhile<TSource, TPredicate>(ConfiguredAsyncEnumerable<TSource>.Enumerator configuredSource, TPredicate predicate)
                where TPredicate : IFunc<TSource, bool>
                => AsyncEnumerable<TSource>.Create(new ConfiguredSkipWhileSyncIterator<TSource, TPredicate>(configuredSource, predicate));

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private readonly struct ConfiguredSkipWhileAyncIterator<TSource, TPredicate> : IAsyncIterator<TSource>
                where TPredicate : IFunc<TSource, Promise<bool>>
            {
                private readonly ConfiguredAsyncEnumerable<TSource>.Enumerator _configuredSource;
                private readonly TPredicate _predicate;

                internal ConfiguredSkipWhileAyncIterator(ConfiguredAsyncEnumerable<TSource>.Enumerator configuredSource, TPredicate predicate)
                {
                    _configuredSource = configuredSource;
                    _predicate = predicate;
                }

                public Promise DisposeAsyncWithoutStart()
                    => _configuredSource.DisposeAsync();

                public async AsyncIteratorMethod Start(AsyncStreamWriter<TSource> writer, CancelationToken cancelationToken)
                {
                    // The enumerator may have been configured with a cancelation token. We need to join the passed in token before starting iteration.
                    var enumerableRef = _configuredSource._enumerator._target;
                    var joinedCancelationSource = MaybeJoinCancelationTokens(enumerableRef._cancelationToken, cancelationToken, out enumerableRef._cancelationToken);

                    try
                    {
                        while (await _configuredSource.MoveNextAsync())
                        {
                            var element = _configuredSource.Current;
                            if (!await _predicate.Invoke(element))
                            {
                                await writer.YieldAsync(element);
                                while (await _configuredSource.MoveNextAsync())
                                {
                                    await writer.YieldAsync(_configuredSource.Current);
                                }
                                return;
                            }
                        }

                        // We yield and wait for the enumerator to be disposed, but only if there were no exceptions.
                        await writer.YieldAsync(default).ForLinqExtension();
                    }
                    finally
                    {
                        joinedCancelationSource.TryDispose();
                        await _configuredSource.DisposeAsync();
                    }
                }
            }

            internal static AsyncEnumerable<TSource> SkipWhileAwait<TSource, TPredicate>(ConfiguredAsyncEnumerable<TSource>.Enumerator configuredSource, TPredicate predicate)
                where TPredicate : IFunc<TSource, Promise<bool>>
                => AsyncEnumerable<TSource>.Create(new ConfiguredSkipWhileAyncIterator<TSource, TPredicate>(configuredSource, predicate));

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private readonly struct ConfiguredSkipWhileWithIndexSyncIterator<TSource, TPredicate> : IAsyncIterator<TSource>
                where TPredicate : IFunc<TSource, int, bool>
            {
                private readonly ConfiguredAsyncEnumerable<TSource>.Enumerator _configuredSource;
                private readonly TPredicate _predicate;

                internal ConfiguredSkipWhileWithIndexSyncIterator(ConfiguredAsyncEnumerable<TSource>.Enumerator configuredSource, TPredicate predicate)
                {
                    _configuredSource = configuredSource;
                    _predicate = predicate;
                }

                public Promise DisposeAsyncWithoutStart()
                    => _configuredSource.DisposeAsync();

                public async AsyncIteratorMethod Start(AsyncStreamWriter<TSource> writer, CancelationToken cancelationToken)
                {
                    // The enumerator may have been configured with a cancelation token. We need to join the passed in token before starting iteration.
                    var enumerableRef = _configuredSource._enumerator._target;
                    var joinedCancelationSource = MaybeJoinCancelationTokens(enumerableRef._cancelationToken, cancelationToken, out enumerableRef._cancelationToken);

                    try
                    {
                        int index = -1;
                        while (await _configuredSource.MoveNextAsync())
                        {
                            var element = _configuredSource.Current;
                            if (!_predicate.Invoke(element, checked(++index)))
                            {
                                await writer.YieldAsync(element);
                                while (await _configuredSource.MoveNextAsync())
                                {
                                    await writer.YieldAsync(_configuredSource.Current);
                                }
                                return;
                            }
                        }

                        // We yield and wait for the enumerator to be disposed, but only if there were no exceptions.
                        await writer.YieldAsync(default).ForLinqExtension();
                    }
                    finally
                    {
                        joinedCancelationSource.TryDispose();
                        await _configuredSource.DisposeAsync();
                    }
                }
            }

            internal static AsyncEnumerable<TSource> SkipWhileWithIndex<TSource, TPredicate>(ConfiguredAsyncEnumerable<TSource>.Enumerator configuredSource, TPredicate predicate)
                where TPredicate : IFunc<TSource, int, bool>
                => AsyncEnumerable<TSource>.Create(new ConfiguredSkipWhileWithIndexSyncIterator<TSource, TPredicate>(configuredSource, predicate));

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private readonly struct ConfiguredSkipWhileWithIndexAyncIterator<TSource, TPredicate> : IAsyncIterator<TSource>
                where TPredicate : IFunc<TSource, int, Promise<bool>>
            {
                private readonly ConfiguredAsyncEnumerable<TSource>.Enumerator _configuredSource;
                private readonly TPredicate _predicate;

                internal ConfiguredSkipWhileWithIndexAyncIterator(ConfiguredAsyncEnumerable<TSource>.Enumerator configuredSource, TPredicate predicate)
                {
                    _configuredSource = configuredSource;
                    _predicate = predicate;
                }

                public Promise DisposeAsyncWithoutStart()
                    => _configuredSource.DisposeAsync();

                public async AsyncIteratorMethod Start(AsyncStreamWriter<TSource> writer, CancelationToken cancelationToken)
                {
                    // The enumerator may have been configured with a cancelation token. We need to join the passed in token before starting iteration.
                    var enumerableRef = _configuredSource._enumerator._target;
                    var joinedCancelationSource = MaybeJoinCancelationTokens(enumerableRef._cancelationToken, cancelationToken, out enumerableRef._cancelationToken);

                    try
                    {
                        int index = -1;
                        while (await _configuredSource.MoveNextAsync())
                        {
                            var element = _configuredSource.Current;
                            if (!await _predicate.Invoke(element, checked(++index)))
                            {
                                await writer.YieldAsync(element);
                                while (await _configuredSource.MoveNextAsync())
                                {
                                    await writer.YieldAsync(_configuredSource.Current);
                                }
                                return;
                            }
                        }

                        // We yield and wait for the enumerator to be disposed, but only if there were no exceptions.
                        await writer.YieldAsync(default).ForLinqExtension();
                    }
                    finally
                    {
                        joinedCancelationSource.TryDispose();
                        await _configuredSource.DisposeAsync();
                    }
                }
            }

            internal static AsyncEnumerable<TSource> SkipWhileWithIndexAwait<TSource, TPredicate>(ConfiguredAsyncEnumerable<TSource>.Enumerator configuredSource, TPredicate predicate)
                where TPredicate : IFunc<TSource, int, Promise<bool>>
                => AsyncEnumerable<TSource>.Create(new ConfiguredSkipWhileWithIndexAyncIterator<TSource, TPredicate>(configuredSource, predicate));
        } // class SkipWhileHelper
    } // class Internal
} // namespace Proto.Promises