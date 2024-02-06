#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using Proto.Promises.CompilerServices;
using System.Collections.Generic;

namespace Proto.Promises.Linq
{
#if CSHARP_7_3_OR_NEWER
    partial class AsyncEnumerable
    {
        /// <summary>
        /// Asynchronously returns the minimum element of an async-enumerable sequence using the default comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">The sequence to return the minimum element of.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>A <see cref="Promise{T}"/> resulting in the minimum element.</returns>
        /// <exception cref="InvalidOperationException"><paramref name="source"/> is empty and <typeparamref name="TSource"/> is not nullable.</exception>
        /// <remarks>If <typeparamref name="TSource"/> is a nullable type and the source sequence is empty or contains only values that are <see langword="null"/>, this method yields <see langword="null"/>.</remarks>
        public static Promise<TSource> MinAsync<TSource>(this AsyncEnumerable<TSource> source, CancelationToken cancelationToken = default)
        {
            return MinAsyncCore(source.GetAsyncEnumerator(cancelationToken), Comparer<TSource>.Default);
        }

        /// <summary>
        /// Asynchronously returns the minimum element of an async-enumerable sequence using the specified comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TComparer">The type of the comparer.</typeparam>
        /// <param name="source">The sequence to return the minimum element of.</param>
        /// <param name="comparer">A comparer to compare values.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>A <see cref="Promise{T}"/> resulting in the minimum element.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="comparer"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="source"/> is empty and <typeparamref name="TSource"/> is not nullable.</exception>
        /// <remarks>If <typeparamref name="TSource"/> is a nullable type and the source sequence is empty or contains only values that are <see langword="null"/>, this method yields <see langword="null"/>.</remarks>
        public static Promise<TSource> MinAsync<TSource, TComparer>(this AsyncEnumerable<TSource> source, TComparer comparer, CancelationToken cancelationToken = default)
            where TComparer : IComparer<TSource>
        {
            ValidateArgument(comparer, nameof(comparer), 1);

            return MinAsyncCore(source.GetAsyncEnumerator(cancelationToken), comparer);
        }

        private static async Promise<TSource> MinAsyncCore<TSource, TComparer>(AsyncEnumerator<TSource> asyncEnumerator, TComparer comparer)
            where TComparer : IComparer<TSource>
        {
            try
            {
                // Check if nullable type. This check is eliminated by the JIT.
                if (default(TSource) == null)
                {
                    var min = default(TSource);
                    do
                    {
                        if (!await asyncEnumerator.MoveNextAsync())
                        {
                            return min;
                        }
                        min = asyncEnumerator.Current;
                    } while (min == null);

                    while (await asyncEnumerator.MoveNextAsync())
                    {
                        var element = asyncEnumerator.Current;
                        if (element != null && comparer.Compare(element, min) < 0)
                        {
                            min = element;
                        }
                    }
                    return min;
                }
                else
                {
                    if (!await asyncEnumerator.MoveNextAsync())
                    {
                        throw new InvalidOperationException("source must contain at least 1 element.", Internal.GetFormattedStacktrace(1));
                    }

                    var min = asyncEnumerator.Current;
                    while (await asyncEnumerator.MoveNextAsync())
                    {
                        var element = asyncEnumerator.Current;
                        if (comparer.Compare(element, min) < 0)
                        {
                            min = element;
                        }
                    }
                    return min;
                }
            }
            finally
            {
                await asyncEnumerator.DisposeAsync();
            }
        }

        /// <summary>
        /// Asynchronously returns the minimum element of a configured async-enumerable sequence using the default comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">The configured async-enumerable sequence to return the minimum element of.</param>
        /// <returns>A <see cref="Promise{T}"/> resulting in the minimum element.</returns>
        /// <exception cref="InvalidOperationException"><paramref name="source"/> is empty and <typeparamref name="TSource"/> is not nullable.</exception>
        /// <remarks>If <typeparamref name="TSource"/> is a nullable type and the source sequence is empty or contains only values that are <see langword="null"/>, this method yields <see langword="null"/>.</remarks>
        public static Promise<TSource> MinAsync<TSource>(this in ConfiguredAsyncEnumerable<TSource> source)
        {
            return MinAsyncCore(source.GetAsyncEnumerator(), Comparer<TSource>.Default);
        }

        /// <summary>
        /// Asynchronously returns the minimum element of a configured async-enumerable sequence using the specified comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TComparer">The type of the comparer.</typeparam>
        /// <param name="source">The configured async-enumerable sequence to return the minimum element of.</param>
        /// <param name="comparer">A comparer to compare values.</param>
        /// <returns>A <see cref="Promise{T}"/> resulting in the minimum element.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="comparer"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="source"/> is empty and <typeparamref name="TSource"/> is not nullable.</exception>
        /// <remarks>If <typeparamref name="TSource"/> is a nullable type and the source sequence is empty or contains only values that are <see langword="null"/>, this method yields <see langword="null"/>.</remarks>
        public static Promise<TSource> MinAsync<TSource, TComparer>(this in ConfiguredAsyncEnumerable<TSource> source, TComparer comparer)
            where TComparer : IComparer<TSource>
        {
            ValidateArgument(comparer, nameof(comparer), 1);

            return MinAsyncCore(source.GetAsyncEnumerator(), comparer);
        }

        private static async Promise<TSource> MinAsyncCore<TSource, TComparer>(ConfiguredAsyncEnumerable<TSource>.Enumerator asyncEnumerator, TComparer comparer)
            where TComparer : IComparer<TSource>
        {
            try
            {
                // Check if nullable type. This check is eliminated by the JIT.
                if (default(TSource) == null)
                {
                    var min = default(TSource);
                    do
                    {
                        if (!await asyncEnumerator.MoveNextAsync())
                        {
                            return min;
                        }
                        min = asyncEnumerator.Current;
                    } while (min == null);

                    while (await asyncEnumerator.MoveNextAsync())
                    {
                        var element = asyncEnumerator.Current;
                        if (element != null && comparer.Compare(element, min) < 0)
                        {
                            min = element;
                        }
                    }
                    return min;
                }
                else
                {
                    if (!await asyncEnumerator.MoveNextAsync())
                    {
                        throw new InvalidOperationException("source must contain at least 1 element.", Internal.GetFormattedStacktrace(1));
                    }

                    var min = asyncEnumerator.Current;
                    while (await asyncEnumerator.MoveNextAsync())
                    {
                        var element = asyncEnumerator.Current;
                        if (comparer.Compare(element, min) < 0)
                        {
                            min = element;
                        }
                    }
                    return min;
                }
            }
            finally
            {
                await asyncEnumerator.DisposeAsync();
            }
        }
    }
#endif // CSHARP_7_3_OR_NEWER
}