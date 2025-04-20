#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using Proto.Promises.CompilerServices;
using System;

namespace Proto.Promises.Linq
{
    partial class AsyncEnumerable
    {
        // Implementation note: unlike System.Linq which does a type-check for ICollection<T> before iterating, we don't do it.
        // This is mostly because async enumerable values are expected to be evaluated lazily rather than all at once.
        // There are a few cases where the optimization would help (like array.ToAsyncEnumerable()), but it's not worth the added complexity to support those.

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> resulting in a <see cref="long"/> of the number of elements in an async-enumerable sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">The sequence that contains elements to be counted.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>A <see cref="Promise{T}"/> resulting in number of elements in the input sequence.</returns>
        /// <exception cref="OverflowException">The number of elements in the source sequence is larger than <see cref="long.MaxValue"/>.</exception>
        public static Promise<long> LongCountAsync<TSource>(this AsyncEnumerable<TSource> source, CancelationToken cancelationToken = default)
            => LongCountCore(source.GetAsyncEnumerator(cancelationToken));

        private static async Promise<long> LongCountCore<TSource>(AsyncEnumerator<TSource> asyncEnumerator)
        {
            try
            {
                long count = 0;
                while (await asyncEnumerator.MoveNextAsync())
                {
                    checked
                    {
                        ++count;
                    }
                }
                return count;
            }
            finally
            {
                await asyncEnumerator.DisposeAsync();
            }
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> resulting in a <see cref="long"/> of how many elements in the specified async-enumerable sequence satisfy a condition.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">The sequence that contains elements to be counted.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>A <see cref="Promise{T}"/> resulting in how many elements in the sequence satisfy the condition in the predicate function.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
        /// <exception cref="OverflowException">The number of elements in the source sequence is larger than <see cref="long.MaxValue"/>.</exception>
        public static Promise<long> LongCountAsync<TSource>(this AsyncEnumerable<TSource> source, Func<TSource, bool> predicate, CancelationToken cancelationToken = default)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return LongCountCore(source.GetAsyncEnumerator(cancelationToken), DelegateWrapper.Create(predicate));
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> resulting in a <see cref="long"/> of how many elements in the specified async-enumerable sequence satisfy a condition.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TCapture">The type of the <paramref name="captureValue"/>.</typeparam>
        /// <param name="source">The sequence that contains elements to be counted.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <param name="captureValue">The extra value that will be passed to the <paramref name="predicate"/>.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>A <see cref="Promise{T}"/> resulting in how many elements in the sequence satisfy the condition in the predicate function.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
        /// <exception cref="OverflowException">The number of elements in the source sequence is larger than <see cref="long.MaxValue"/>.</exception>
        public static Promise<long> LongCountAsync<TSource, TCapture>(this AsyncEnumerable<TSource> source, TCapture captureValue, Func<TCapture, TSource, bool> predicate, CancelationToken cancelationToken = default)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return LongCountCore(source.GetAsyncEnumerator(cancelationToken), DelegateWrapper.Create(captureValue, predicate));
        }

        private static async Promise<long> LongCountCore<TSource, TPredicate>(AsyncEnumerator<TSource> asyncEnumerator, TPredicate predicate)
            where TPredicate : IFunc<TSource, bool>
        {
            try
            {
                long count = 0;
                while (await asyncEnumerator.MoveNextAsync())
                {
                    if (predicate.Invoke(asyncEnumerator.Current))
                    {
                        checked
                        {
                            ++count;
                        }
                    }
                }
                return count;
            }
            finally
            {
                await asyncEnumerator.DisposeAsync();
            }
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> resulting in a <see cref="long"/> of how many elements in the specified async-enumerable sequence satisfy a condition.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">The sequence that contains elements to be counted.</param>
        /// <param name="predicate">An async function to test each element for a condition.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>A <see cref="Promise{T}"/> resulting in how many elements in the sequence satisfy the condition in the predicate function.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
        /// <exception cref="OverflowException">The number of elements in the source sequence is larger than <see cref="long.MaxValue"/>.</exception>
        public static Promise<long> LongCountAsync<TSource>(this AsyncEnumerable<TSource> source, Func<TSource, Promise<bool>> predicate, CancelationToken cancelationToken = default)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return LongCountAwaitCore(source.GetAsyncEnumerator(cancelationToken), DelegateWrapper.Create(predicate));
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> resulting in a <see cref="long"/> of how many elements in the specified async-enumerable sequence satisfy a condition.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TCapture">The type of the <paramref name="captureValue"/>.</typeparam>
        /// <param name="source">The sequence that contains elements to be counted.</param>
        /// <param name="predicate">An async function to test each element for a condition.</param>
        /// <param name="captureValue">The extra value that will be passed to the <paramref name="predicate"/>.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>A <see cref="Promise{T}"/> resulting in how many elements in the sequence satisfy the condition in the predicate function.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
        /// <exception cref="OverflowException">The number of elements in the source sequence is larger than <see cref="long.MaxValue"/>.</exception>
        public static Promise<long> LongCountAsync<TSource, TCapture>(this AsyncEnumerable<TSource> source, TCapture captureValue, Func<TCapture, TSource, Promise<bool>> predicate, CancelationToken cancelationToken = default)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return LongCountAwaitCore(source.GetAsyncEnumerator(cancelationToken), DelegateWrapper.Create(captureValue, predicate));
        }

        private static async Promise<long> LongCountAwaitCore<TSource, TPredicate>(AsyncEnumerator<TSource> asyncEnumerator, TPredicate predicate)
            where TPredicate : IFunc<TSource, Promise<bool>>
        {
            try
            {
                long count = 0;
                while (await asyncEnumerator.MoveNextAsync())
                {
                    if (await predicate.Invoke(asyncEnumerator.Current))
                    {
                        checked
                        {
                            ++count;
                        }
                    }
                }
                return count;
            }
            finally
            {
                await asyncEnumerator.DisposeAsync();
            }
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> resulting in a <see cref="long"/> of how many elements in the specified configured async-enumerable sequence satisfy a condition.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="configuredSource"/>.</typeparam>
        /// <param name="configuredSource">The configured async-enumerable sequence that contains elements to be counted.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <returns>A <see cref="Promise{T}"/> resulting in how many elements in the sequence satisfy the condition in the predicate function.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="configuredSource"/> is null.</exception>
        /// <exception cref="OverflowException">The number of elements in the source sequence is larger than <see cref="long.MaxValue"/>.</exception>
        public static Promise<long> LongCountAsync<TSource>(this in ConfiguredAsyncEnumerable<TSource> configuredSource, Func<TSource, bool> predicate)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return LongCountCore(configuredSource.GetAsyncEnumerator(), DelegateWrapper.Create(predicate));
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> resulting in a <see cref="long"/> of how many elements in the specified configured async-enumerable sequence satisfy a condition.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="configuredSource"/>.</typeparam>
        /// <typeparam name="TCapture">The type of the <paramref name="captureValue"/>.</typeparam>
        /// <param name="configuredSource">The configured async-enumerable sequence that contains elements to be counted.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <param name="captureValue">The extra value that will be passed to the <paramref name="predicate"/>.</param>
        /// <returns>A <see cref="Promise{T}"/> resulting in how many elements in the sequence satisfy the condition in the predicate function.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="configuredSource"/> is null.</exception>
        /// <exception cref="OverflowException">The number of elements in the source sequence is larger than <see cref="long.MaxValue"/>.</exception>
        public static Promise<long> LongCountAsync<TSource, TCapture>(this in ConfiguredAsyncEnumerable<TSource> configuredSource, TCapture captureValue, Func<TCapture, TSource, bool> predicate)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return LongCountCore(configuredSource.GetAsyncEnumerator(), DelegateWrapper.Create(captureValue, predicate));
        }

        private static async Promise<long> LongCountCore<TSource, TPredicate>(ConfiguredAsyncEnumerable<TSource>.Enumerator asyncEnumerator, TPredicate predicate)
            where TPredicate : IFunc<TSource, bool>
        {
            try
            {
                long count = 0;
                while (await asyncEnumerator.MoveNextAsync())
                {
                    if (predicate.Invoke(asyncEnumerator.Current))
                    {
                        checked
                        {
                            ++count;
                        }
                    }
                }
                return count;
            }
            finally
            {
                await asyncEnumerator.DisposeAsync();
            }
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> resulting in a <see cref="long"/> of how many elements in the specified configured async-enumerable sequence satisfy a condition.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="configuredSource"/>.</typeparam>
        /// <param name="configuredSource">The configured async-enumerable sequence that contains elements to be counted.</param>
        /// <param name="predicate">An async function to test each element for a condition.</param>
        /// <returns>A <see cref="Promise{T}"/> resulting in how many elements in the sequence satisfy the condition in the predicate function.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="configuredSource"/> is null.</exception>
        /// <exception cref="OverflowException">The number of elements in the source sequence is larger than <see cref="long.MaxValue"/>.</exception>
        public static Promise<long> LongCountAsync<TSource>(this in ConfiguredAsyncEnumerable<TSource> configuredSource, Func<TSource, Promise<bool>> predicate)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return LongCountAwaitCore(configuredSource.GetAsyncEnumerator(), DelegateWrapper.Create(predicate));
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> resulting in a <see cref="long"/> of how many elements in the specified configured async-enumerable sequence satisfy a condition.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="configuredSource"/>.</typeparam>
        /// <typeparam name="TCapture">The type of the <paramref name="captureValue"/>.</typeparam>
        /// <param name="configuredSource">The configured async-enumerable sequence that contains elements to be counted.</param>
        /// <param name="predicate">An async function to test each element for a condition.</param>
        /// <param name="captureValue">The extra value that will be passed to the <paramref name="predicate"/>.</param>
        /// <returns>A <see cref="Promise{T}"/> resulting in how many elements in the sequence satisfy the condition in the predicate function.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="configuredSource"/> is null.</exception>
        /// <exception cref="OverflowException">The number of elements in the source sequence is larger than <see cref="long.MaxValue"/>.</exception>
        public static Promise<long> LongCountAsync<TSource, TCapture>(this in ConfiguredAsyncEnumerable<TSource> configuredSource, TCapture captureValue, Func<TCapture, TSource, Promise<bool>> predicate)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return LongCountAwaitCore(configuredSource.GetAsyncEnumerator(), DelegateWrapper.Create(captureValue, predicate));
        }

        private static async Promise<long> LongCountAwaitCore<TSource, TPredicate>(ConfiguredAsyncEnumerable<TSource>.Enumerator asyncEnumerator, TPredicate predicate)
            where TPredicate : IFunc<TSource, Promise<bool>>
        {
            try
            {
                long count = 0;
                while (await asyncEnumerator.MoveNextAsync())
                {
                    if (await predicate.Invoke(asyncEnumerator.Current).ConfigureAwait(asyncEnumerator.ContinuationOptions))
                    {
                        checked
                        {
                            ++count;
                        }
                    }
                }
                return count;
            }
            finally
            {
                await asyncEnumerator.DisposeAsync();
            }
        }
    }
}