#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using Proto.Promises.CompilerServices;
using System.Collections.Generic;

namespace Proto.Promises.Linq
{
    partial class AsyncEnumerable
    {
        private static async Promise<TSource> MaxAsyncCore<TSource, TComparer>(AsyncEnumerator<TSource> asyncEnumerator, TComparer comparer)
            where TComparer : IComparer<TSource>
        {
            try
            {
                // Check if nullable type. This check is eliminated by the JIT.
                if (default(TSource) == null)
                {
                    var max = default(TSource);
                    do
                    {
                        if (!await asyncEnumerator.MoveNextAsync())
                        {
                            return max;
                        }
                        max = asyncEnumerator.Current;
                    } while (max == null);

                    while (await asyncEnumerator.MoveNextAsync())
                    {
                        var element = asyncEnumerator.Current;
                        if (element != null && comparer.Compare(element, max) > 0)
                        {
                            max = element;
                        }
                    }
                    return max;
                }
                else
                {
                    if (!await asyncEnumerator.MoveNextAsync())
                    {
                        throw new InvalidOperationException("source must contain at least 1 element.", Internal.GetFormattedStacktrace(1));
                    }

                    var max = asyncEnumerator.Current;
                    while (await asyncEnumerator.MoveNextAsync())
                    {
                        var element = asyncEnumerator.Current;
                        if (comparer.Compare(element, max) > 0)
                        {
                            max = element;
                        }
                    }
                    return max;
                }
            }
            finally
            {
                await asyncEnumerator.DisposeAsync();
            }
        }

        private static async Promise<TSource> MaxAsyncCore<TSource, TComparer>(ConfiguredAsyncEnumerable<TSource>.Enumerator asyncEnumerator, TComparer comparer)
            where TComparer : IComparer<TSource>
        {
            try
            {
                // Check if nullable type. This check is eliminated by the JIT.
                if (default(TSource) == null)
                {
                    var max = default(TSource);
                    do
                    {
                        if (!await asyncEnumerator.MoveNextAsync())
                        {
                            return max;
                        }
                        max = asyncEnumerator.Current;
                    } while (max == null);

                    while (await asyncEnumerator.MoveNextAsync())
                    {
                        var element = asyncEnumerator.Current;
                        if (element != null && comparer.Compare(element, max) > 0)
                        {
                            max = element;
                        }
                    }
                    return max;
                }
                else
                {
                    if (!await asyncEnumerator.MoveNextAsync())
                    {
                        throw new InvalidOperationException("source must contain at least 1 element.", Internal.GetFormattedStacktrace(1));
                    }

                    var max = asyncEnumerator.Current;
                    while (await asyncEnumerator.MoveNextAsync())
                    {
                        var element = asyncEnumerator.Current;
                        if (comparer.Compare(element, max) > 0)
                        {
                            max = element;
                        }
                    }
                    return max;
                }
            }
            finally
            {
                await asyncEnumerator.DisposeAsync();
            }
        }

        /// <summary>
        /// Asynchronously returns the maximum element of an async-enumerable sequence using the default comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">The sequence to return the maximum element of.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>A <see cref="Promise{T}"/> resulting in the maximum element.</returns>
        /// <exception cref="InvalidOperationException"><paramref name="source"/> is empty and <typeparamref name="TSource"/> is not nullable.</exception>
        /// <remarks>If <typeparamref name="TSource"/> is a nullable type and the source sequence is empty or contains only values that are <see langword="null"/>, this method yields <see langword="null"/>.</remarks>
        public static Promise<TSource> MaxAsync<TSource>(this AsyncEnumerable<TSource> source, CancelationToken cancelationToken = default)
            => MaxAsyncCore(source.GetAsyncEnumerator(cancelationToken), Comparer<TSource>.Default);

        /// <summary>
        /// Asynchronously returns the maximum element of an async-enumerable sequence using the specified comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TComparer">The type of the comparer.</typeparam>
        /// <param name="source">The sequence to return the maximum element of.</param>
        /// <param name="comparer">A comparer to compare values.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>A <see cref="Promise{T}"/> resulting in the maximum element.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="comparer"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="source"/> is empty and <typeparamref name="TSource"/> is not nullable.</exception>
        /// <remarks>If <typeparamref name="TSource"/> is a nullable type and the source sequence is empty or contains only values that are <see langword="null"/>, this method yields <see langword="null"/>.</remarks>
        public static Promise<TSource> MaxAsync<TSource, TComparer>(this AsyncEnumerable<TSource> source, TComparer comparer, CancelationToken cancelationToken = default)
            where TComparer : IComparer<TSource>
        {
            ValidateArgument(comparer, nameof(comparer), 1);

            return MaxAsyncCore(source.GetAsyncEnumerator(cancelationToken), comparer);
        }

        /// <summary>
        /// Asynchronously returns the maximum element of a configured async-enumerable sequence using the default comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">The configured async-enumerable sequence to return the maximum element of.</param>
        /// <returns>A <see cref="Promise{T}"/> resulting in the maximum element.</returns>
        /// <exception cref="InvalidOperationException"><paramref name="source"/> is empty and <typeparamref name="TSource"/> is not nullable.</exception>
        /// <remarks>If <typeparamref name="TSource"/> is a nullable type and the source sequence is empty or contains only values that are <see langword="null"/>, this method yields <see langword="null"/>.</remarks>
        public static Promise<TSource> MaxAsync<TSource>(this in ConfiguredAsyncEnumerable<TSource> source)
            => MaxAsyncCore(source.GetAsyncEnumerator(), Comparer<TSource>.Default);

        /// <summary>
        /// Asynchronously returns the maximum element of a configured async-enumerable sequence using the specified comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TComparer">The type of the comparer.</typeparam>
        /// <param name="source">The configured async-enumerable sequence to return the maximum element of.</param>
        /// <param name="comparer">A comparer to compare values.</param>
        /// <returns>A <see cref="Promise{T}"/> resulting in the maximum element.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="comparer"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="source"/> is empty and <typeparamref name="TSource"/> is not nullable.</exception>
        /// <remarks>If <typeparamref name="TSource"/> is a nullable type and the source sequence is empty or contains only values that are <see langword="null"/>, this method yields <see langword="null"/>.</remarks>
        public static Promise<TSource> MaxAsync<TSource, TComparer>(this in ConfiguredAsyncEnumerable<TSource> source, TComparer comparer)
            where TComparer : IComparer<TSource>
        {
            ValidateArgument(comparer, nameof(comparer), 1);

            return MaxAsyncCore(source.GetAsyncEnumerator(), comparer);
        }
    }
}