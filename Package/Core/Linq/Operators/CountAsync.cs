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

        private static async Promise<int> CountCore<TSource>(AsyncEnumerator<TSource> asyncEnumerator)
        {
            try
            {
                int count = 0;
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

        private static async Promise<int> CountCore<TSource, TPredicate>(AsyncEnumerator<TSource> asyncEnumerator, TPredicate predicate, CancelationToken cancelationToken)
            where TPredicate : IFunc<TSource, CancelationToken, Promise<bool>>
        {
            try
            {
                int count = 0;
                while (await asyncEnumerator.MoveNextAsync())
                {
                    if (await predicate.Invoke(asyncEnumerator.Current, cancelationToken))
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

        private static async Promise<int> CountCore<TSource, TPredicate>(ConfiguredAsyncEnumerable<TSource>.Enumerator asyncEnumerator, TPredicate predicate, CancelationToken cancelationToken)
            where TPredicate : IFunc<TSource, CancelationToken, Promise<bool>>
        {
            try
            {
                int count = 0;
                while (await asyncEnumerator.MoveNextAsync())
                {
                    if (await predicate.Invoke(asyncEnumerator.Current, cancelationToken))
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
        /// Returns a <see cref="Promise{T}"/> resulting in the number of elements in an async-enumerable sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">The sequence that contains elements to be counted.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>A <see cref="Promise{T}"/> resulting in number of elements in the input sequence.</returns>
        /// <exception cref="OverflowException">The number of elements in the source sequence is larger than <see cref="int.MaxValue"/>.</exception>
        public static Promise<int> CountAsync<TSource>(this AsyncEnumerable<TSource> source, CancelationToken cancelationToken = default)
            => CountCore(source.GetAsyncEnumerator(cancelationToken));

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> resulting in how many elements in the specified async-enumerable sequence satisfy a condition.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">The sequence that contains elements to be counted.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>A <see cref="Promise{T}"/> resulting in how many elements in the sequence satisfy the condition in the predicate function.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
        /// <exception cref="OverflowException">The number of elements in the source sequence is larger than <see cref="int.MaxValue"/>.</exception>
        public static Promise<int> CountAsync<TSource>(this AsyncEnumerable<TSource> source, Func<TSource, bool> predicate, CancelationToken cancelationToken = default)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return CountCore(source.GetAsyncEnumerator(cancelationToken), DelegateWrapper.Create(predicate), cancelationToken);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> resulting in how many elements in the specified async-enumerable sequence satisfy a condition.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TCapture">The type of the <paramref name="captureValue"/>.</typeparam>
        /// <param name="source">The sequence that contains elements to be counted.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <param name="captureValue">The extra value that will be passed to the <paramref name="predicate"/>.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>A <see cref="Promise{T}"/> resulting in how many elements in the sequence satisfy the condition in the predicate function.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
        /// <exception cref="OverflowException">The number of elements in the source sequence is larger than <see cref="int.MaxValue"/>.</exception>
        public static Promise<int> CountAsync<TSource, TCapture>(this AsyncEnumerable<TSource> source, TCapture captureValue, Func<TCapture, TSource, bool> predicate, CancelationToken cancelationToken = default)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return CountCore(source.GetAsyncEnumerator(cancelationToken), DelegateWrapper.Create(captureValue, predicate), cancelationToken);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> resulting in how many elements in the specified async-enumerable sequence satisfy a condition.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">The sequence that contains elements to be counted.</param>
        /// <param name="predicate">An async function to test each element for a condition.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>A <see cref="Promise{T}"/> resulting in how many elements in the sequence satisfy the condition in the predicate function.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
        /// <exception cref="OverflowException">The number of elements in the source sequence is larger than <see cref="int.MaxValue"/>.</exception>
        public static Promise<int> CountAsync<TSource>(this AsyncEnumerable<TSource> source, Func<TSource, CancelationToken, Promise<bool>> predicate, CancelationToken cancelationToken = default)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return CountCore(source.GetAsyncEnumerator(cancelationToken), DelegateWrapper.Create(predicate), cancelationToken);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> resulting in how many elements in the specified async-enumerable sequence satisfy a condition.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TCapture">The type of the <paramref name="captureValue"/>.</typeparam>
        /// <param name="source">The sequence that contains elements to be counted.</param>
        /// <param name="predicate">An async function to test each element for a condition.</param>
        /// <param name="captureValue">The extra value that will be passed to the <paramref name="predicate"/>.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>A <see cref="Promise{T}"/> resulting in how many elements in the sequence satisfy the condition in the predicate function.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
        /// <exception cref="OverflowException">The number of elements in the source sequence is larger than <see cref="int.MaxValue"/>.</exception>
        public static Promise<int> CountAsync<TSource, TCapture>(this AsyncEnumerable<TSource> source, TCapture captureValue, Func<TCapture, TSource, CancelationToken, Promise<bool>> predicate, CancelationToken cancelationToken = default)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return CountCore(source.GetAsyncEnumerator(cancelationToken), DelegateWrapper.Create(captureValue, predicate), cancelationToken);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> resulting in how many elements in the specified configured async-enumerable sequence satisfy a condition.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="configuredSource"/>.</typeparam>
        /// <param name="configuredSource">The configured async-enumerable sequence that contains elements to be counted.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <returns>A <see cref="Promise{T}"/> resulting in how many elements in the sequence satisfy the condition in the predicate function.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="configuredSource"/> is null.</exception>
        /// <exception cref="OverflowException">The number of elements in the source sequence is larger than <see cref="int.MaxValue"/>.</exception>
        public static Promise<int> CountAsync<TSource>(this in ConfiguredAsyncEnumerable<TSource> configuredSource, Func<TSource, bool> predicate)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return CountCore(configuredSource.GetAsyncEnumerator(), DelegateWrapper.Create(predicate), configuredSource.CancelationToken);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> resulting in how many elements in the specified configured async-enumerable sequence satisfy a condition.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="configuredSource"/>.</typeparam>
        /// <typeparam name="TCapture">The type of the <paramref name="captureValue"/>.</typeparam>
        /// <param name="configuredSource">The configured async-enumerable sequence that contains elements to be counted.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <param name="captureValue">The extra value that will be passed to the <paramref name="predicate"/>.</param>
        /// <returns>A <see cref="Promise{T}"/> resulting in how many elements in the sequence satisfy the condition in the predicate function.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="configuredSource"/> is null.</exception>
        /// <exception cref="OverflowException">The number of elements in the source sequence is larger than <see cref="int.MaxValue"/>.</exception>
        public static Promise<int> CountAsync<TSource, TCapture>(this in ConfiguredAsyncEnumerable<TSource> configuredSource, TCapture captureValue, Func<TCapture, TSource, bool> predicate)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return CountCore(configuredSource.GetAsyncEnumerator(), DelegateWrapper.Create(captureValue, predicate), configuredSource.CancelationToken);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> resulting in how many elements in the specified configured async-enumerable sequence satisfy a condition.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="configuredSource"/>.</typeparam>
        /// <param name="configuredSource">The configured async-enumerable sequence that contains elements to be counted.</param>
        /// <param name="predicate">An async function to test each element for a condition.</param>
        /// <returns>A <see cref="Promise{T}"/> resulting in how many elements in the sequence satisfy the condition in the predicate function.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="configuredSource"/> is null.</exception>
        /// <exception cref="OverflowException">The number of elements in the source sequence is larger than <see cref="int.MaxValue"/>.</exception>
        public static Promise<int> CountAsync<TSource>(this in ConfiguredAsyncEnumerable<TSource> configuredSource, Func<TSource, CancelationToken, Promise<bool>> predicate)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return CountCore(configuredSource.GetAsyncEnumerator(), DelegateWrapper.Create(predicate), configuredSource.CancelationToken);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> resulting in how many elements in the specified configured async-enumerable sequence satisfy a condition.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="configuredSource"/>.</typeparam>
        /// <typeparam name="TCapture">The type of the <paramref name="captureValue"/>.</typeparam>
        /// <param name="configuredSource">The configured async-enumerable sequence that contains elements to be counted.</param>
        /// <param name="predicate">An async function to test each element for a condition.</param>
        /// <param name="captureValue">The extra value that will be passed to the <paramref name="predicate"/>.</param>
        /// <returns>A <see cref="Promise{T}"/> resulting in how many elements in the sequence satisfy the condition in the predicate function.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="configuredSource"/> is null.</exception>
        /// <exception cref="OverflowException">The number of elements in the source sequence is larger than <see cref="int.MaxValue"/>.</exception>
        public static Promise<int> CountAsync<TSource, TCapture>(this in ConfiguredAsyncEnumerable<TSource> configuredSource, TCapture captureValue, Func<TCapture, TSource, CancelationToken, Promise<bool>> predicate)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return CountCore(configuredSource.GetAsyncEnumerator(), DelegateWrapper.Create(captureValue, predicate), configuredSource.CancelationToken);
        }
    }
}