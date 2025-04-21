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
using System.Threading;

namespace Proto.Promises
{
    partial class Internal
    {
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal static class TakeWhileHelper
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private readonly struct TakeWhileSyncIterator<TSource, TPredicate> : IAsyncIterator<TSource>
                where TPredicate : IFunc<TSource, bool>
            {
                private readonly AsyncEnumerator<TSource> _source;
                private readonly TPredicate _predicate;

                internal TakeWhileSyncIterator(AsyncEnumerator<TSource> source, TPredicate predicate)
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
                                break;
                            }
                            await writer.YieldAsync(element);
                        }

                        await AsyncEnumerableSourceHelpers.WaitForDisposeAsync(writer);
                    }
                    finally
                    {
                        await _source.DisposeAsync();
                    }
                }
            }

            internal static AsyncEnumerable<TSource> TakeWhile<TSource, TPredicate>(AsyncEnumerator<TSource> source, TPredicate predicate)
                where TPredicate : IFunc<TSource, bool>
                => AsyncEnumerable<TSource>.Create(new TakeWhileSyncIterator<TSource, TPredicate>(source, predicate));

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private readonly struct TakeWhileAyncIterator<TSource, TPredicate> : IAsyncIterator<TSource>
                where TPredicate : IFunc<TSource, Promise<bool>>
            {
                private readonly AsyncEnumerator<TSource> _source;
                private readonly TPredicate _predicate;

                internal TakeWhileAyncIterator(AsyncEnumerator<TSource> source, TPredicate predicate)
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
                                break;
                            }
                            await writer.YieldAsync(element);
                        }

                        await AsyncEnumerableSourceHelpers.WaitForDisposeAsync(writer);
                    }
                    finally
                    {
                        await _source.DisposeAsync();
                    }
                }
            }

            internal static AsyncEnumerable<TSource> TakeWhileAwait<TSource, TPredicate>(AsyncEnumerator<TSource> source, TPredicate predicate)
                where TPredicate : IFunc<TSource, Promise<bool>>
                => AsyncEnumerable<TSource>.Create(new TakeWhileAyncIterator<TSource, TPredicate>(source, predicate));

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private readonly struct TakeWhileWithIndexSyncIterator<TSource, TPredicate> : IAsyncIterator<TSource>
                where TPredicate : IFunc<TSource, int, bool>
            {
                private readonly AsyncEnumerator<TSource> _source;
                private readonly TPredicate _predicate;

                internal TakeWhileWithIndexSyncIterator(AsyncEnumerator<TSource> source, TPredicate predicate)
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
                                break;
                            }
                            await writer.YieldAsync(element);
                        }

                        await AsyncEnumerableSourceHelpers.WaitForDisposeAsync(writer);
                    }
                    finally
                    {
                        await _source.DisposeAsync();
                    }
                }
            }

            internal static AsyncEnumerable<TSource> TakeWhileWithIndex<TSource, TPredicate>(AsyncEnumerator<TSource> source, TPredicate predicate)
                where TPredicate : IFunc<TSource, int, bool>
                => AsyncEnumerable<TSource>.Create(new TakeWhileWithIndexSyncIterator<TSource, TPredicate>(source, predicate));

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private readonly struct TakeWhileWithIndexAyncIterator<TSource, TPredicate> : IAsyncIterator<TSource>
                where TPredicate : IFunc<TSource, int, Promise<bool>>
            {
                private readonly AsyncEnumerator<TSource> _source;
                private readonly TPredicate _predicate;

                internal TakeWhileWithIndexAyncIterator(AsyncEnumerator<TSource> source, TPredicate predicate)
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
                                break;
                            }
                            await writer.YieldAsync(element);
                        }

                        await AsyncEnumerableSourceHelpers.WaitForDisposeAsync(writer);
                    }
                    finally
                    {
                        await _source.DisposeAsync();
                    }
                }
            }

            internal static AsyncEnumerable<TSource> TakeWhileWithIndexAwait<TSource, TPredicate>(AsyncEnumerator<TSource> source, TPredicate predicate)
                where TPredicate : IFunc<TSource, int, Promise<bool>>
                => AsyncEnumerable<TSource>.Create(new TakeWhileWithIndexAyncIterator<TSource, TPredicate>(source, predicate));
            
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private readonly struct ConfiguredTakeWhileSyncIterator<TSource, TPredicate> : IAsyncIterator<TSource>
                where TPredicate : IFunc<TSource, bool>
            {
                private readonly ConfiguredAsyncEnumerable<TSource>.Enumerator _configuredSource;
                private readonly TPredicate _predicate;

                internal ConfiguredTakeWhileSyncIterator(ConfiguredAsyncEnumerable<TSource>.Enumerator configuredSource, TPredicate predicate)
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
                    var maybeJoinedCancelationSource = MaybeJoinCancelationTokens(enumerableRef._cancelationToken, cancelationToken, out enumerableRef._cancelationToken);

                    try
                    {
                        while (await _configuredSource.MoveNextAsync())
                        {
                            var element = _configuredSource.Current;
                            if (!_predicate.Invoke(element))
                            {
                                break;
                            }
                            await writer.YieldAsync(element);
                        }

                        await AsyncEnumerableSourceHelpers.WaitForDisposeAsync(writer);
                    }
                    finally
                    {
                        maybeJoinedCancelationSource.Dispose();
                        await _configuredSource.DisposeAsync();
                    }
                }
            }

            internal static AsyncEnumerable<TSource> TakeWhile<TSource, TPredicate>(ConfiguredAsyncEnumerable<TSource>.Enumerator configuredSource, TPredicate predicate)
                where TPredicate : IFunc<TSource, bool>
                => AsyncEnumerable<TSource>.Create(new ConfiguredTakeWhileSyncIterator<TSource, TPredicate>(configuredSource, predicate));

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private readonly struct ConfiguredTakeWhileAyncIterator<TSource, TPredicate> : IAsyncIterator<TSource>
                where TPredicate : IFunc<TSource, Promise<bool>>
            {
                private readonly ConfiguredAsyncEnumerable<TSource>.Enumerator _configuredSource;
                private readonly TPredicate _predicate;

                internal ConfiguredTakeWhileAyncIterator(ConfiguredAsyncEnumerable<TSource>.Enumerator configuredSource, TPredicate predicate)
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
                    var maybeJoinedCancelationSource = MaybeJoinCancelationTokens(enumerableRef._cancelationToken, cancelationToken, out enumerableRef._cancelationToken);

                    try
                    {
                        while (await _configuredSource.MoveNextAsync())
                        {
                            var element = _configuredSource.Current;
                            if (!await _predicate.Invoke(element).ConfigureAwait(_configuredSource.ContinuationOptions))
                            {
                                break;
                            }
                            await writer.YieldAsync(element);
                        }

                        await AsyncEnumerableSourceHelpers.WaitForDisposeAsync(writer);
                    }
                    finally
                    {
                        maybeJoinedCancelationSource.Dispose();
                        await _configuredSource.DisposeAsync();
                    }
                }
            }

            internal static AsyncEnumerable<TSource> TakeWhileAwait<TSource, TPredicate>(ConfiguredAsyncEnumerable<TSource>.Enumerator configuredSource, TPredicate predicate)
                where TPredicate : IFunc<TSource, Promise<bool>>
                => AsyncEnumerable<TSource>.Create(new ConfiguredTakeWhileAyncIterator<TSource, TPredicate>(configuredSource, predicate));

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private readonly struct ConfiguredTakeWhileWithIndexSyncIterator<TSource, TPredicate> : IAsyncIterator<TSource>
                where TPredicate : IFunc<TSource, int, bool>
            {
                private readonly ConfiguredAsyncEnumerable<TSource>.Enumerator _configuredSource;
                private readonly TPredicate _predicate;

                internal ConfiguredTakeWhileWithIndexSyncIterator(ConfiguredAsyncEnumerable<TSource>.Enumerator configuredSource, TPredicate predicate)
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
                    var maybeJoinedCancelationSource = MaybeJoinCancelationTokens(enumerableRef._cancelationToken, cancelationToken, out enumerableRef._cancelationToken);

                    try
                    {
                        int index = -1;
                        while (await _configuredSource.MoveNextAsync())
                        {
                            var element = _configuredSource.Current;
                            if (!_predicate.Invoke(element, checked(++index)))
                            {
                                break;
                            }
                            await writer.YieldAsync(element);
                        }

                        await AsyncEnumerableSourceHelpers.WaitForDisposeAsync(writer);
                    }
                    finally
                    {
                        maybeJoinedCancelationSource.Dispose();
                        await _configuredSource.DisposeAsync();
                    }
                }
            }

            internal static AsyncEnumerable<TSource> TakeWhileWithIndex<TSource, TPredicate>(ConfiguredAsyncEnumerable<TSource>.Enumerator configuredSource, TPredicate predicate)
                where TPredicate : IFunc<TSource, int, bool>
                => AsyncEnumerable<TSource>.Create(new ConfiguredTakeWhileWithIndexSyncIterator<TSource, TPredicate>(configuredSource, predicate));

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private readonly struct ConfiguredTakeWhileWithIndexAyncIterator<TSource, TPredicate> : IAsyncIterator<TSource>
                where TPredicate : IFunc<TSource, int, Promise<bool>>
            {
                private readonly ConfiguredAsyncEnumerable<TSource>.Enumerator _configuredSource;
                private readonly TPredicate _predicate;

                internal ConfiguredTakeWhileWithIndexAyncIterator(ConfiguredAsyncEnumerable<TSource>.Enumerator configuredSource, TPredicate predicate)
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
                    var maybeJoinedCancelationSource = MaybeJoinCancelationTokens(enumerableRef._cancelationToken, cancelationToken, out enumerableRef._cancelationToken);

                    try
                    {
                        int index = -1;
                        while (await _configuredSource.MoveNextAsync())
                        {
                            var element = _configuredSource.Current;
                            if (!await _predicate.Invoke(element, checked(++index)).ConfigureAwait(_configuredSource.ContinuationOptions))
                            {
                                break;
                            }
                            await writer.YieldAsync(element);
                        }

                        await AsyncEnumerableSourceHelpers.WaitForDisposeAsync(writer);
                    }
                    finally
                    {
                        maybeJoinedCancelationSource.Dispose();
                        await _configuredSource.DisposeAsync();
                    }
                }
            }

            internal static AsyncEnumerable<TSource> TakeWhileWithIndexAwait<TSource, TPredicate>(ConfiguredAsyncEnumerable<TSource>.Enumerator configuredSource, TPredicate predicate)
                where TPredicate : IFunc<TSource, int, Promise<bool>>
                => AsyncEnumerable<TSource>.Create(new ConfiguredTakeWhileWithIndexAyncIterator<TSource, TPredicate>(configuredSource, predicate));
        } // class TakeWhileHelper
    } // class Internal
} // namespace Proto.Promises