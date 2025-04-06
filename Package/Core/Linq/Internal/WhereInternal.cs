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
            private readonly struct WhereAsyncIterator<TSource, TPredicate> : IAsyncIterator<TSource>
                where TPredicate : IFunc<TSource, CancelationToken, Promise<bool>>
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
                            if (await _predicate.Invoke(element, cancelationToken))
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
                where TPredicate : IFunc<TSource, CancelationToken, Promise<bool>>
                => AsyncEnumerable<TSource>.Create(new WhereAsyncIterator<TSource, TPredicate>(source, predicate));

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private readonly struct ConfiguredWhereAsyncIterator<TSource, TPredicate> : IAsyncIterator<TSource>
                where TPredicate : IFunc<TSource, CancelationToken, Promise<bool>>
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
                    var maybeJoinedCancelationSource = MaybeJoinCancelationTokens(ref cancelationToken, ref _configuredAsyncEnumerator._enumerator._target._cancelationToken);

                    try
                    {
                        while (await _configuredAsyncEnumerator.MoveNextAsync())
                        {
                            var element = _configuredAsyncEnumerator.Current;
                            if (await _predicate.Invoke(element, cancelationToken))
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
                        maybeJoinedCancelationSource.Dispose();
                        await _configuredAsyncEnumerator.DisposeAsync();
                    }
                }

                [MethodImpl(InlineOption)]
                public Promise DisposeAsyncWithoutStart()
                    => _configuredAsyncEnumerator.DisposeAsync();
            }

            internal static AsyncEnumerable<TSource> Where<TSource, TPredicate>(ConfiguredAsyncEnumerable<TSource>.Enumerator configuredAsyncEnumerator, TPredicate predicate)
                where TPredicate : IFunc<TSource, CancelationToken, Promise<bool>>
                => AsyncEnumerable<TSource>.Create(new ConfiguredWhereAsyncIterator<TSource, TPredicate>(configuredAsyncEnumerator, predicate));

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private readonly struct WhereWithIndexAsyncIterator<TSource, TPredicate> : IAsyncIterator<TSource>
                where TPredicate : IFunc<TSource, int, CancelationToken, Promise<bool>>
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
                            if (await _predicate.Invoke(element, checked(i++), cancelationToken))
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
                where TPredicate : IFunc<TSource, int, CancelationToken, Promise<bool>>
                => AsyncEnumerable<TSource>.Create(new WhereWithIndexAsyncIterator<TSource, TPredicate>(source, predicate));

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private readonly struct ConfiguredWhereWithIndexAsyncIterator<TSource, TPredicate> : IAsyncIterator<TSource>
                where TPredicate : IFunc<TSource, int, CancelationToken, Promise<bool>>
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
                    var maybeJoinedCancelationSource = MaybeJoinCancelationTokens(ref cancelationToken, ref _configuredAsyncEnumerator._enumerator._target._cancelationToken);

                    try
                    {
                        int i = 0;
                        while (await _configuredAsyncEnumerator.MoveNextAsync())
                        {
                            var element = _configuredAsyncEnumerator.Current;
                            if (await _predicate.Invoke(element, checked(i++), cancelationToken))
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
                        maybeJoinedCancelationSource.Dispose();
                        await _configuredAsyncEnumerator.DisposeAsync();
                    }
                }

                [MethodImpl(InlineOption)]
                public Promise DisposeAsyncWithoutStart()
                    => _configuredAsyncEnumerator.DisposeAsync();
            }

            internal static AsyncEnumerable<TSource> WhereWithIndex<TSource, TPredicate>(ConfiguredAsyncEnumerable<TSource>.Enumerator configuredAsyncEnumerator, TPredicate predicate)
                where TPredicate : IFunc<TSource, int, CancelationToken, Promise<bool>>
                => AsyncEnumerable<TSource>.Create(new ConfiguredWhereWithIndexAsyncIterator<TSource, TPredicate>(configuredAsyncEnumerator, predicate));
        }
    } // class Internal
} // namespace Proto.Promises