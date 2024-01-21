#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using Proto.Promises.Async.CompilerServices;
using System.Collections.Generic;

namespace Proto.Promises.Linq
{
#if CSHARP_7_3_OR_NEWER
    partial class AsyncEnumerable
    {
        /// <summary>
        /// Determines whether an async-enumerable sequence contains a specified element by using the default equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">The sequence in which to locate a value.</param>
        /// <param name="value">The value to locate in the sequence.</param>
        /// <returns>A <see cref="Promise{T}"/> resulting in whether the source sequence contains an element that has the specified value.</returns>
        public static Promise<bool> ContainsAsync<TSource>(this AsyncEnumerable<TSource> source, TSource value)
            => ContainsCore(source.GetAsyncEnumerator(), value, EqualityComparer<TSource>.Default);

        /// <summary>
        /// Determines whether an async-enumerable sequence contains a specified element by using a specified equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TEqualityComparer">The type of the elements of <paramref name="equalityComparer"/>.</typeparam>
        /// <param name="source">The sequence in which to locate a value.</param>
        /// <param name="value">The value to locate in the sequence.</param>
        /// <param name="equalityComparer">An equality comparer to compare values.</param>
        /// <returns>A <see cref="Promise{T}"/> resulting in whether the source sequence contains an element that has the specified value.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="equalityComparer"/> is null.</exception>
        public static Promise<bool> ContainsAsync<TSource, TEqualityComparer>(this AsyncEnumerable<TSource> source, TSource value, TEqualityComparer equalityComparer)
            where TEqualityComparer : IEqualityComparer<TSource>
        {
            ValidateArgument(equalityComparer, nameof(equalityComparer), 1);

            return ContainsCore(source.GetAsyncEnumerator(), value, equalityComparer);
        }

        private static async Promise<bool> ContainsCore<TSource, TEqualityComparer>(AsyncEnumerator<TSource> asyncEnumerator, TSource value, TEqualityComparer equalityComparer)
            where TEqualityComparer : IEqualityComparer<TSource>
        {
            try
            {
                while (await asyncEnumerator.MoveNextAsync())
                {
                    if (equalityComparer.Equals(asyncEnumerator.Current, value))
                    {
                        return true;
                    }
                }
                return false;
            }
            finally
            {
                await asyncEnumerator.DisposeAsync();
            }
        }

        /// <summary>
        /// Determines whether a configured async-enumerable sequence contains a specified element by using the default equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="configuredSource"/>.</typeparam>
        /// <param name="configuredSource">The sequence in which to locate a value.</param>
        /// <param name="value">The value to locate in the sequence.</param>
        /// <returns>A <see cref="Promise{T}"/> resulting in whether the source sequence contains an element that has the specified value.</returns>
        public static Promise<bool> ContainsAsync<TSource>(this ConfiguredAsyncEnumerable<TSource> configuredSource, TSource value)
            => ContainsCore(configuredSource.GetAsyncEnumerator(), value, EqualityComparer<TSource>.Default);

        /// <summary>
        /// Determines whether a configured async-enumerable sequence contains a specified element by using a specified equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="configuredSource"/>.</typeparam>
        /// <typeparam name="TEqualityComparer">The type of the elements of <paramref name="equalityComparer"/>.</typeparam>
        /// <param name="configuredSource">The sequence in which to locate a value.</param>
        /// <param name="value">The value to locate in the sequence.</param>
        /// <param name="equalityComparer">An equality comparer to compare values.</param>
        /// <returns>A <see cref="Promise{T}"/> resulting in whether the source sequence contains an element that has the specified value.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="equalityComparer"/> is null.</exception>
        public static Promise<bool> ContainsAsync<TSource, TEqualityComparer>(this ConfiguredAsyncEnumerable<TSource> configuredSource, TSource value, TEqualityComparer equalityComparer)
            where TEqualityComparer : IEqualityComparer<TSource>
        {
            ValidateArgument(equalityComparer, nameof(equalityComparer), 1);

            return ContainsCore(configuredSource.GetAsyncEnumerator(), value, equalityComparer);
        }

        private static async Promise<bool> ContainsCore<TSource, TEqualityComparer>(ConfiguredAsyncEnumerable<TSource>.Enumerator asyncEnumerator, TSource value, TEqualityComparer equalityComparer)
            where TEqualityComparer : IEqualityComparer<TSource>
        {
            try
            {
                while (await asyncEnumerator.MoveNextAsync())
                {
                    if (equalityComparer.Equals(asyncEnumerator.Current, value))
                    {
                        return true;
                    }
                }
                return false;
            }
            finally
            {
                await asyncEnumerator.DisposeAsync();
            }
        }
    }
#endif // CSHARP_7_3_OR_NEWER
}