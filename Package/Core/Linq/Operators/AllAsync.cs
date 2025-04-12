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
        private static async Promise<bool> AllCore<TSource, TPredicate>(AsyncEnumerator<TSource> asyncEnumerator, TPredicate predicate, CancelationToken cancelationToken)
            where TPredicate : IFunc<TSource, CancelationToken, Promise<bool>>
        {
            try
            {
                while (await asyncEnumerator.MoveNextAsync())
                {
                    if (!await predicate.Invoke(asyncEnumerator.Current, cancelationToken))
                    {
                        return false;
                    }
                }
                return true;
            }
            finally
            {
                await asyncEnumerator.DisposeAsync();
            }
        }

        private static async Promise<bool> AllCore<TSource, TPredicate>(ConfiguredAsyncEnumerable<TSource>.Enumerator asyncEnumerator, TPredicate predicate, CancelationToken cancelationToken)
            where TPredicate : IFunc<TSource, CancelationToken, Promise<bool>>
        {
            try
            {
                while (await asyncEnumerator.MoveNextAsync())
                {
                    if (!await predicate.Invoke(asyncEnumerator.Current, cancelationToken))
                    {
                        return false;
                    }
                }
                return true;
            }
            finally
            {
                await asyncEnumerator.DisposeAsync();
            }
        }

        /// <summary>
        /// Determines whether all elements of an async-enumerable sequence satisfy a condition.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <param name="source">Source sequence.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns><see cref="Promise{T}"/> containing the result of whether the source sequence is empty or all elements in the source sequence passed the test in the specified <paramref name="predicate"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
        public static Promise<bool> AllAsync<TSource>(this AsyncEnumerable<TSource> source, Func<TSource, bool> predicate, CancelationToken cancelationToken = default)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return AllCore(source.GetAsyncEnumerator(cancelationToken), DelegateWrapper.Create(predicate), cancelationToken);
        }

        /// <summary>
        /// Determines whether all elements of an async-enumerable sequence satisfy a condition.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCapture">The type of the captured value.</typeparam>
        /// <param name="source">Source sequence.</param>
        /// <param name="captureValue">The extra value that will be passed to <paramref name="predicate"/>.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns><see cref="Promise{T}"/> containing the result of whether the source sequence is empty or all elements in the source sequence passed the test in the specified <paramref name="predicate"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
        public static Promise<bool> AllAsync<TSource, TCapture>(this AsyncEnumerable<TSource> source, TCapture captureValue, Func<TCapture, TSource, bool> predicate, CancelationToken cancelationToken = default)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return AllCore(source.GetAsyncEnumerator(cancelationToken), DelegateWrapper.Create(captureValue, predicate), cancelationToken);
        }

        /// <summary>
        /// Determines whether all elements of an async-enumerable sequence satisfy a condition.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <param name="source">Source sequence.</param>
        /// <param name="predicate">An async function to test each element for a condition.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns><see cref="Promise{T}"/> containing the result of whether the source sequence is empty or all elements in the source sequence passed the test in the specified <paramref name="predicate"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
        public static Promise<bool> AllAsync<TSource>(this AsyncEnumerable<TSource> source, Func<TSource, CancelationToken, Promise<bool>> predicate, CancelationToken cancelationToken = default)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return AllCore(source.GetAsyncEnumerator(cancelationToken), DelegateWrapper.Create(predicate), cancelationToken);
        }

        /// <summary>
        /// Determines whether all elements of an async-enumerable sequence satisfy a condition.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCapture">The type of the captured value.</typeparam>
        /// <param name="source">Source sequence.</param>
        /// <param name="captureValue">The extra value that will be passed to <paramref name="predicate"/>.</param>
        /// <param name="predicate">An async function to test each element for a condition.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns><see cref="Promise{T}"/> containing the result of whether the source sequence is empty or all elements in the source sequence passed the test in the specified <paramref name="predicate"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
        public static Promise<bool> AllAsync<TSource, TCapture>(this AsyncEnumerable<TSource> source, TCapture captureValue, Func<TCapture, TSource, CancelationToken, Promise<bool>> predicate, CancelationToken cancelationToken = default)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return AllCore(source.GetAsyncEnumerator(cancelationToken), DelegateWrapper.Create(captureValue, predicate), cancelationToken);
        }

        /// <summary>
        /// Determines whether all elements of a configured async-enumerable sequence satisfy a condition.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <param name="configuredSource">Configured source sequence.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <returns><see cref="Promise{T}"/> containing the result of whether the source sequence is empty or all elements in the source sequence passed the test in the specified <paramref name="predicate"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
        public static Promise<bool> AllAsync<TSource>(this in ConfiguredAsyncEnumerable<TSource> configuredSource, Func<TSource, bool> predicate)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return AllCore(configuredSource.GetAsyncEnumerator(), DelegateWrapper.Create(predicate), configuredSource.CancelationToken);
        }

        /// <summary>
        /// Determines whether all elements of a configured async-enumerable sequence satisfy a condition.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCapture">The type of the captured value.</typeparam>
        /// <param name="configuredSource">Configured source sequence.</param>
        /// <param name="captureValue">The extra value that will be passed to <paramref name="predicate"/>.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <returns><see cref="Promise{T}"/> containing the result of whether the source sequence is empty or all elements in the source sequence passed the test in the specified <paramref name="predicate"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
        public static Promise<bool> AllAsync<TSource, TCapture>(this in ConfiguredAsyncEnumerable<TSource> configuredSource, TCapture captureValue, Func<TCapture, TSource, bool> predicate)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return AllCore(configuredSource.GetAsyncEnumerator(), DelegateWrapper.Create(captureValue, predicate), configuredSource.CancelationToken);
        }

        /// <summary>
        /// Determines whether all elements of a configured async-enumerable sequence satisfy a condition.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <param name="configuredSource">Configured source sequence.</param>
        /// <param name="predicate">An async function to test each element for a condition.</param>
        /// <returns><see cref="Promise{T}"/> containing the result of whether the source sequence is empty or all elements in the source sequence passed the test in the specified <paramref name="predicate"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
        public static Promise<bool> AllAsync<TSource>(this in ConfiguredAsyncEnumerable<TSource> configuredSource, Func<TSource, CancelationToken, Promise<bool>> predicate)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return AllCore(configuredSource.GetAsyncEnumerator(), DelegateWrapper.Create(predicate), configuredSource.CancelationToken);
        }

        /// <summary>
        /// Determines whether all elements of a configured async-enumerable sequence satisfy a condition.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCapture">The type of the captured value.</typeparam>
        /// <param name="configuredSource">Configured source sequence.</param>
        /// <param name="captureValue">The extra value that will be passed to <paramref name="predicate"/>.</param>
        /// <param name="predicate">An async function to test each element for a condition.</param>
        /// <returns><see cref="Promise{T}"/> containing the result of whether the source sequence is empty or all elements in the source sequence passed the test in the specified <paramref name="predicate"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
        public static Promise<bool> AllAsync<TSource, TCapture>(this in ConfiguredAsyncEnumerable<TSource> configuredSource, TCapture captureValue, Func<TCapture, TSource, CancelationToken, Promise<bool>> predicate)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return AllCore(configuredSource.GetAsyncEnumerator(), DelegateWrapper.Create(captureValue, predicate), configuredSource.CancelationToken);
        }
    }
}