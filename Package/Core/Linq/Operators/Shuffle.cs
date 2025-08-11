#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using Proto.Promises.Collections;
using Proto.Promises.Linq.Sources;
using System;
using System.Diagnostics;

namespace Proto.Promises.Linq
{
    partial class AsyncEnumerable
    {
#if !NET6_0_OR_GREATER
        [ThreadStatic]
        private static Random ts_random;
#endif

        /// <summary>Shuffles the order of the elements of an async-enumerable sequence.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">A sequence of values to shuffle.</param>
        /// <returns>An async-enumerable sequence whose elements correspond to those of the input sequence in randomized order.</returns>
        /// <remarks>Randomization is performed using a non-cryptographically-secure random number generator.</remarks>
        public static AsyncEnumerable<TSource> Shuffle<TSource>(this AsyncEnumerable<TSource> source)
            => AsyncEnumerable<TSource>.Create(new ShuffleIterator<TSource>(source.GetAsyncEnumerator()));

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        private readonly struct ShuffleIterator<TSource> : IAsyncIterator<TSource>
        {
            private readonly AsyncEnumerator<TSource> _asyncEnumerator;

            internal ShuffleIterator(AsyncEnumerator<TSource> asyncEnumerator)
            {
                _asyncEnumerator = asyncEnumerator;
            }

            public Promise DisposeAsyncWithoutStart()
                => _asyncEnumerator.DisposeAsync();

            public async AsyncIteratorMethod Start(AsyncStreamWriter<TSource> writer, CancelationToken cancelationToken)
            {
                // The enumerator was retrieved without a cancelation token when the original function was called.
                // We need to propagate the token that was passed in, so we assign it before starting iteration.
                _asyncEnumerator._target._cancelationToken = cancelationToken;

                try
                {
                    using (var tempCollectionBuilder = new TempCollectionBuilder<TSource>(0))
                    {
                        while (await _asyncEnumerator.MoveNextAsync())
                        {
                            tempCollectionBuilder.Add(_asyncEnumerator.Current);
                        }

                        Shuffle(tempCollectionBuilder.Span);

                        // Iterating backwards over shuffled elements is equivalent to iterating forwards, and it's cheaper.
                        for (int i = tempCollectionBuilder._count - 1; i >= 0; --i)
                        {
                            await writer.YieldAsync(tempCollectionBuilder[i]);
                        }
                    }

                    await AsyncEnumerableSourceHelpers.WaitForDisposeAsync(writer);
                }
                finally
                {
                    await _asyncEnumerator.DisposeAsync();
                }
            }

            private static void Shuffle(Span<TSource> span)
            {
#if NET8_0_OR_GREATER
                Random.Shared.Shuffle(span);
#else
#if NET6_0_OR_GREATER
                var random = Random.Shared;
#else
                var random = ts_random;
                if (random is null)
                {
                    ts_random = random = new Random();
                }
#endif
                int n = span.Length;
                for (int i = 0; i < n - 1; i++)
                {
                    int j = random.Next(i, n);
                    if (j != i)
                    {
                        var temp = span[i];
                        span[i] = span[j];
                        span[j] = temp;
                    }
                }
#endif
            }
        }
    }
}