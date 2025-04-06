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
        private static async Promise<bool> SequenceEqualAsyncCore<TSource, TComparer>(AsyncEnumerator<TSource> first, AsyncEnumerator<TSource> second, TComparer comparer)
            where TComparer : IEqualityComparer<TSource>
        {
            try
            {
                while (await first.MoveNextAsync())
                {
                    if (!(await second.MoveNextAsync() && comparer.Equals(first.Current, second.Current)))
                    {
                        return false;
                    }
                }
                return !await second.MoveNextAsync();
            }
            finally
            {
                try
                {
                    await first.DisposeAsync();
                }
                finally
                {
                    await second.DisposeAsync();
                }
            }
        }

        private static async Promise<bool> SequenceEqualAsyncCore<TSource, TComparer>(ConfiguredAsyncEnumerable<TSource>.Enumerator first, AsyncEnumerator<TSource> second, TComparer comparer)
            where TComparer : IEqualityComparer<TSource>
        {
            try
            {
                while (await first.MoveNextAsync())
                {
                    // Switch to the configured context before invoking the comparer.
                    if (!await second.MoveNextAsync().ConfigureAwait(first.ContinuationOptions))
                    {
                        return false;
                    }
                    if (!comparer.Equals(first.Current, second.Current))
                    {
                        return false;
                    }
                }
                return !await second.MoveNextAsync();
            }
            finally
            {
                try
                {
                    await first.DisposeAsync();
                }
                finally
                {
                    await second.DisposeAsync();
                }
            }
        }

        /// <summary>
        /// Asynchronously determines whether two async-enumerable sequences are equal by comparing the elements by using the default equality comparer for their type.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the input sequences.</typeparam>
        /// <param name="first">An <see cref="AsyncEnumerable{T}"/> to compare to <paramref name="second"/>.</param>
        /// <param name="second">An <see cref="AsyncEnumerable{T}"/> to compare to <paramref name="first"/>.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the comparison at any time.</param>
        /// <returns>A <see cref="Promise{T}"/> resulting in <see langword="true"/> if the two source sequences are of equal length and their corresponding elements are equal; otherwise, <see langword="true"/>.</returns>
        public static Promise<bool> SequenceEqualAsync<TSource>(this AsyncEnumerable<TSource> first, AsyncEnumerable<TSource> second, CancelationToken cancelationToken = default)
            => SequenceEqualAsync(first, second, EqualityComparer<TSource>.Default, cancelationToken);

        /// <summary>
        /// Asynchronously determines whether two async-enumerable sequences are equal by comparing the elements by using the specified equality comparer for their type.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the input sequences.</typeparam>
        /// <typeparam name="TComparer">The type of <paramref name="comparer"/>.</typeparam>
        /// <param name="first">An <see cref="AsyncEnumerable{T}"/> to compare to <paramref name="second"/>.</param>
        /// <param name="second">An <see cref="AsyncEnumerable{T}"/> to compare to <paramref name="first"/>.</param>
        /// <param name="comparer">An equality comparer to use to compare elements.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the comparison at any time.</param>
        /// <returns>A <see cref="Promise{T}"/> resulting in <see langword="true"/> if the two source sequences are of equal length and their corresponding elements are equal; otherwise, <see langword="true"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="comparer"/> is <see langword="null"/>.</exception>
        public static Promise<bool> SequenceEqualAsync<TSource, TComparer>(this AsyncEnumerable<TSource> first, AsyncEnumerable<TSource> second, TComparer comparer, CancelationToken cancelationToken = default)
            where TComparer : IEqualityComparer<TSource>
        {
            ValidateArgument(comparer, nameof(comparer), 1);

            return SequenceEqualAsyncCore(first.GetAsyncEnumerator(cancelationToken), second.GetAsyncEnumerator(cancelationToken), comparer);
        }

        /// <summary>
        /// Asynchronously determines whether two async-enumerable sequences are equal by comparing the elements by using the default equality comparer for their type.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the input sequences.</typeparam>
        /// <param name="configuredFirst">A <see cref="ConfiguredAsyncEnumerable{T}"/> to compare to <paramref name="second"/>.</param>
        /// <param name="second">An <see cref="AsyncEnumerable{T}"/> to compare to <paramref name="configuredFirst"/>.</param>
        /// <returns>A <see cref="Promise{T}"/> resulting in <see langword="true"/> if the two source sequences are of equal length and their corresponding elements are equal; otherwise, <see langword="true"/>.</returns>
        public static Promise<bool> SequenceEqualAsync<TSource>(this in ConfiguredAsyncEnumerable<TSource> configuredFirst, AsyncEnumerable<TSource> second)
            => SequenceEqualAsync(configuredFirst, second, EqualityComparer<TSource>.Default);

        /// <summary>
        /// Asynchronously determines whether two async-enumerable sequences are equal by comparing the elements by using the specified equality comparer for their type.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the input sequences.</typeparam>
        /// <typeparam name="TComparer">The type of <paramref name="comparer"/>.</typeparam>
        /// <param name="configuredFirst">A <see cref="ConfiguredAsyncEnumerable{T}"/> to compare to <paramref name="second"/>.</param>
        /// <param name="second">An <see cref="AsyncEnumerable{T}"/> to compare to <paramref name="configuredFirst"/>.</param>
        /// <param name="comparer">An equality comparer to use to compare elements.</param>
        /// <returns>A <see cref="Promise{T}"/> resulting in <see langword="true"/> if the two source sequences are of equal length and their corresponding elements are equal; otherwise, <see langword="true"/>.</returns>
        public static Promise<bool> SequenceEqualAsync<TSource, TComparer>(this in ConfiguredAsyncEnumerable<TSource> configuredFirst, AsyncEnumerable<TSource> second, TComparer comparer)
            where TComparer : IEqualityComparer<TSource>
        {
            ValidateArgument(comparer, nameof(comparer), 1);

            return SequenceEqualAsyncCore(configuredFirst.GetAsyncEnumerator(), second.GetAsyncEnumerator(configuredFirst.CancelationToken), comparer);
        }
    }
}