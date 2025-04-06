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
        internal static class TakeWhileHelper
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private readonly struct TakeWhileIterator<TSource, TPredicate> : IAsyncIterator<TSource>
                where TPredicate : IFunc<TSource, CancelationToken, Promise<bool>>
            {
                private readonly AsyncEnumerator<TSource> _source;
                private readonly TPredicate _predicate;

                internal TakeWhileIterator(AsyncEnumerator<TSource> source, TPredicate predicate)
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
                            if (!await _predicate.Invoke(element, cancelationToken))
                            {
                                break;
                            }
                            await writer.YieldAsync(element);
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

            internal static AsyncEnumerable<TSource> TakeWhile<TSource, TPredicate>(AsyncEnumerator<TSource> source, TPredicate predicate)
                where TPredicate : IFunc<TSource, CancelationToken, Promise<bool>>
                => AsyncEnumerable<TSource>.Create(new TakeWhileIterator<TSource, TPredicate>(source, predicate));

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private readonly struct TakeWhileWithIndexIterator<TSource, TPredicate> : IAsyncIterator<TSource>
                where TPredicate : IFunc<TSource, int, CancelationToken, Promise<bool>>
            {
                private readonly AsyncEnumerator<TSource> _source;
                private readonly TPredicate _predicate;

                internal TakeWhileWithIndexIterator(AsyncEnumerator<TSource> source, TPredicate predicate)
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
                            if (!await _predicate.Invoke(element, checked(++index), cancelationToken))
                            {
                                break;
                            }
                            await writer.YieldAsync(element);
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

            internal static AsyncEnumerable<TSource> TakeWhileWithIndex<TSource, TPredicate>(AsyncEnumerator<TSource> source, TPredicate predicate)
                where TPredicate : IFunc<TSource, int, CancelationToken, Promise<bool>>
                => AsyncEnumerable<TSource>.Create(new TakeWhileWithIndexIterator<TSource, TPredicate>(source, predicate));
            
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private readonly struct ConfiguredTakeWhileIterator<TSource, TPredicate> : IAsyncIterator<TSource>
                where TPredicate : IFunc<TSource, CancelationToken, Promise<bool>>
            {
                private readonly ConfiguredAsyncEnumerable<TSource>.Enumerator _configuredAsyncEnumerator;
                private readonly TPredicate _predicate;

                internal ConfiguredTakeWhileIterator(ConfiguredAsyncEnumerable<TSource>.Enumerator configuredAsyncEnumerator, TPredicate predicate)
                {
                    _configuredAsyncEnumerator = configuredAsyncEnumerator;
                    _predicate = predicate;
                }

                public Promise DisposeAsyncWithoutStart()
                    => _configuredAsyncEnumerator.DisposeAsync();

                public async AsyncIteratorMethod Start(AsyncStreamWriter<TSource> writer, CancelationToken cancelationToken)
                {
                    // The enumerator may have been configured with a cancelation token. We need to join the passed in token before starting iteration.
                    var maybeJoinedCancelationSource = MaybeJoinCancelationTokens(ref cancelationToken, ref _configuredAsyncEnumerator._enumerator._target._cancelationToken);

                    try
                    {
                        while (await _configuredAsyncEnumerator.MoveNextAsync())
                        {
                            var element = _configuredAsyncEnumerator.Current;
                            if (!await _predicate.Invoke(element, cancelationToken))
                            {
                                break;
                            }
                            await writer.YieldAsync(element);
                        }

                        // We yield and wait for the enumerator to be disposed, but only if there were no exceptions.
                        await writer.YieldAsync(default).ForLinqExtension();
                    }
                    finally
                    {
                        maybeJoinedCancelationSource.Dispose();
                        await _configuredAsyncEnumerator.DisposeAsync();
                    }
                }
            }

            internal static AsyncEnumerable<TSource> TakeWhile<TSource, TPredicate>(ConfiguredAsyncEnumerable<TSource>.Enumerator configuredAsyncEnumerator, TPredicate predicate)
                where TPredicate : IFunc<TSource, CancelationToken, Promise<bool>>
                => AsyncEnumerable<TSource>.Create(new ConfiguredTakeWhileIterator<TSource, TPredicate>(configuredAsyncEnumerator, predicate));

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private readonly struct ConfiguredTakeWhileWithIndexIterator<TSource, TPredicate> : IAsyncIterator<TSource>
                where TPredicate : IFunc<TSource, int, CancelationToken, Promise<bool>>
            {
                private readonly ConfiguredAsyncEnumerable<TSource>.Enumerator _configuredAsyncEnumerator;
                private readonly TPredicate _predicate;

                internal ConfiguredTakeWhileWithIndexIterator(ConfiguredAsyncEnumerable<TSource>.Enumerator configuredAsyncEnumerator, TPredicate predicate)
                {
                    _configuredAsyncEnumerator = configuredAsyncEnumerator;
                    _predicate = predicate;
                }

                public Promise DisposeAsyncWithoutStart()
                    => _configuredAsyncEnumerator.DisposeAsync();

                public async AsyncIteratorMethod Start(AsyncStreamWriter<TSource> writer, CancelationToken cancelationToken)
                {
                    // The enumerator may have been configured with a cancelation token. We need to join the passed in token before starting iteration.
                    var maybeJoinedCancelationSource = MaybeJoinCancelationTokens(ref cancelationToken, ref _configuredAsyncEnumerator._enumerator._target._cancelationToken);

                    try
                    {
                        int index = -1;
                        while (await _configuredAsyncEnumerator.MoveNextAsync())
                        {
                            var element = _configuredAsyncEnumerator.Current;
                            if (!await _predicate.Invoke(element, checked(++index), cancelationToken))
                            {
                                break;
                            }
                            await writer.YieldAsync(element);
                        }

                        // We yield and wait for the enumerator to be disposed, but only if there were no exceptions.
                        await writer.YieldAsync(default).ForLinqExtension();
                    }
                    finally
                    {
                        maybeJoinedCancelationSource.Dispose();
                        await _configuredAsyncEnumerator.DisposeAsync();
                    }
                }
            }

            internal static AsyncEnumerable<TSource> TakeWhileWithIndex<TSource, TPredicate>(ConfiguredAsyncEnumerable<TSource>.Enumerator configuredAsyncEnumerator, TPredicate predicate)
                where TPredicate : IFunc<TSource, int, CancelationToken, Promise<bool>>
                => AsyncEnumerable<TSource>.Create(new ConfiguredTakeWhileWithIndexIterator<TSource, TPredicate>(configuredAsyncEnumerator, predicate));
        } // class TakeWhileHelper
    } // class Internal
} // namespace Proto.Promises