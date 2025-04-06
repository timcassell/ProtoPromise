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
        internal static class SelectHelper<TResult>
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private readonly struct SelectIterator<TSource, TSelector> : IAsyncIterator<TResult>
                where TSelector : IFunc<TSource, CancelationToken, Promise<TResult>>
            {
                private readonly AsyncEnumerator<TSource> _asyncEnumerator;
                private readonly TSelector _selector;

                internal SelectIterator(AsyncEnumerator<TSource> asyncEnumerator, TSelector selector)
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
                            await writer.YieldAsync(await _selector.Invoke(_asyncEnumerator.Current, cancelationToken));
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

            internal static AsyncEnumerable<TResult> Select<TSource, TSelector>(AsyncEnumerator<TSource> source, TSelector selector)
                where TSelector : IFunc<TSource, CancelationToken, Promise<TResult>>
                => AsyncEnumerable<TResult>.Create(new SelectIterator<TSource, TSelector>(source, selector));

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private readonly struct ConfiguredSelectIterator<TSource, TSelector> : IAsyncIterator<TResult>
                where TSelector : IFunc<TSource, CancelationToken, Promise<TResult>>
            {
                private readonly ConfiguredAsyncEnumerable<TSource>.Enumerator _configuredAsyncEnumerator;
                private readonly TSelector _selector;

                internal ConfiguredSelectIterator(ConfiguredAsyncEnumerable<TSource>.Enumerator configuredAsyncEnumerator, TSelector selector)
                {
                    _configuredAsyncEnumerator = configuredAsyncEnumerator;
                    _selector = selector;
                }

                public async AsyncIteratorMethod Start(AsyncStreamWriter<TResult> writer, CancelationToken cancelationToken)
                {
                    // The enumerator may have been configured with a cancelation token. We need to join the passed in token before starting iteration.
                    var maybeJoinedCancelationSource = MaybeJoinCancelationTokens(ref cancelationToken, ref _configuredAsyncEnumerator._enumerator._target._cancelationToken);

                    try
                    {
                        while (await _configuredAsyncEnumerator.MoveNextAsync())
                        {
                            await writer.YieldAsync(await _selector.Invoke(_configuredAsyncEnumerator.Current, cancelationToken));
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

            internal static AsyncEnumerable<TResult> Select<TSource, TSelector>(ConfiguredAsyncEnumerable<TSource>.Enumerator configuredAsyncEnumerator, TSelector selector)
                where TSelector : IFunc<TSource, CancelationToken, Promise<TResult>>
                => AsyncEnumerable<TResult>.Create(new ConfiguredSelectIterator<TSource, TSelector>(configuredAsyncEnumerator, selector));

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private readonly struct SelectWithIndexIterator<TSource, TSelector> : IAsyncIterator<TResult>
                where TSelector : IFunc<TSource, int, CancelationToken, Promise<TResult>>
            {
                private readonly AsyncEnumerator<TSource> _asyncEnumerator;
                private readonly TSelector _selector;

                internal SelectWithIndexIterator(AsyncEnumerator<TSource> asyncEnumerator, TSelector selector)
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
                            await writer.YieldAsync(await _selector.Invoke(_asyncEnumerator.Current, checked(i++), cancelationToken));
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

            internal static AsyncEnumerable<TResult> SelectWithIndex<TSource, TSelector>(AsyncEnumerator<TSource> source, TSelector selector)
                where TSelector : IFunc<TSource, int, CancelationToken, Promise<TResult>>
                => AsyncEnumerable<TResult>.Create(new SelectWithIndexIterator<TSource, TSelector>(source, selector));

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private readonly struct ConfiguredSelectWithIndexIterator<TSource, TSelector> : IAsyncIterator<TResult>
                where TSelector : IFunc<TSource, int, CancelationToken, Promise<TResult>>
            {
                private readonly ConfiguredAsyncEnumerable<TSource>.Enumerator _configuredAsyncEnumerator;
                private readonly TSelector _selector;

                internal ConfiguredSelectWithIndexIterator(ConfiguredAsyncEnumerable<TSource>.Enumerator configuredAsyncEnumerator, TSelector selector)
                {
                    _configuredAsyncEnumerator = configuredAsyncEnumerator;
                    _selector = selector;
                }

                public async AsyncIteratorMethod Start(AsyncStreamWriter<TResult> writer, CancelationToken cancelationToken)
                {
                    // The enumerator may have been configured with a cancelation token. We need to join the passed in token before starting iteration.
                    var maybeJoinedCancelationSource = MaybeJoinCancelationTokens(ref cancelationToken, ref _configuredAsyncEnumerator._enumerator._target._cancelationToken);

                    try
                    {
                        int i = 0;
                        while (await _configuredAsyncEnumerator.MoveNextAsync())
                        {
                            await writer.YieldAsync(await _selector.Invoke(_configuredAsyncEnumerator.Current, checked(i++), cancelationToken));
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

            internal static AsyncEnumerable<TResult> SelectWithIndex<TSource, TSelector>(ConfiguredAsyncEnumerable<TSource>.Enumerator configuredAsyncEnumerator, TSelector selector)
                where TSelector : IFunc<TSource, int, CancelationToken, Promise<TResult>>
                => AsyncEnumerable<TResult>.Create(new ConfiguredSelectWithIndexIterator<TSource, TSelector>(configuredAsyncEnumerator, selector));
        }
    } // class Internal
} // namespace Proto.Promises