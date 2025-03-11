#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#pragma warning disable IDE0062 // Make local function 'static'

using Proto.Promises.CompilerServices;
using System;

namespace Proto.Promises.Linq
{
    partial class AsyncEnumerable
    {
        /// <summary>
        /// Asynchronously returns the only element of an async-enumerable sequence, and reports an exception if there is not exactly one element in the async-enumerable sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">The sequence to return the single element of.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>A <see cref="Promise{T}"/> resulting in the single element.</returns>
        /// <exception cref="InvalidOperationException"><paramref name="source"/> is empty.</exception>
        public static Promise<TSource> SingleAsync<TSource>(this AsyncEnumerable<TSource> source, CancelationToken cancelationToken = default)
        {
            return Core(source.GetAsyncEnumerator(cancelationToken));

            async Promise<TSource> Core(AsyncEnumerator<TSource> asyncEnumerator)
            {
                try
                {
                    if (!await asyncEnumerator.MoveNextAsync())
                    {
                        throw new InvalidOperationException("source is empty.", Internal.GetFormattedStacktrace(1));
                    }
                    var result = asyncEnumerator.Current;
                    if (await asyncEnumerator.MoveNextAsync())
                    {
                        throw new InvalidOperationException("source contains more than 1 element.", Internal.GetFormattedStacktrace(1));
                    }
                    return result;
                }
                finally
                {
                    await asyncEnumerator.DisposeAsync();
                }
            }
        }

        /// <summary>
        /// Asynchronously returns the only element of an async-enumerable sequence that satisfies the specified condition, and reports an exception if there is not exactly one element in the async-enumerable sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">The sequence to return the single element of.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>A <see cref="Promise{T}"/> resulting in the single element in the sequence that passes the test in the specified predicate function.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="source"/> is empty, or no element satisfies the condition in <paramref name="predicate"/>.</exception>
        public static Promise<TSource> SingleAsync<TSource>(this AsyncEnumerable<TSource> source, Func<TSource, bool> predicate, CancelationToken cancelationToken = default)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return SingleAsyncCore(source.GetAsyncEnumerator(cancelationToken), Internal.PromiseRefBase.DelegateWrapper.Create(predicate));
        }

        /// <summary>
        /// Asynchronously returns the only element of an async-enumerable sequence that satisfies the specified condition, and reports an exception if there is not exactly one element in the async-enumerable sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TCapture">The type of the <paramref name="captureValue"/>.</typeparam>
        /// <param name="source">The sequence to return the single element of.</param>
        /// <param name="captureValue">The extra value that will be passed to the <paramref name="predicate"/>.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>A <see cref="Promise{T}"/> resulting in the single element in the sequence that passes the test in the specified predicate function.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="source"/> is empty, or no element satisfies the condition in <paramref name="predicate"/>.</exception>
        public static Promise<TSource> SingleAsync<TSource, TCapture>(this AsyncEnumerable<TSource> source, TCapture captureValue, Func<TCapture, TSource, bool> predicate, CancelationToken cancelationToken = default)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return SingleAsyncCore(source.GetAsyncEnumerator(cancelationToken), Internal.PromiseRefBase.DelegateWrapper.Create(captureValue, predicate));
        }

        private static async Promise<TSource> SingleAsyncCore<TSource, TPredicate>(AsyncEnumerator<TSource> asyncEnumerator, TPredicate predicate)
            where TPredicate : IFunc<TSource, bool>
        {
            try
            {
                while (await asyncEnumerator.MoveNextAsync())
                {
                    var result = asyncEnumerator.Current;
                    if (predicate.Invoke(result))
                    {
                        while (await asyncEnumerator.MoveNextAsync())
                        {
                            if (predicate.Invoke(asyncEnumerator.Current))
                            {
                                throw new InvalidOperationException("source contains more than 1 element that satisfies the condition.", Internal.GetFormattedStacktrace(1));
                            }
                        }
                        return result;
                    }
                }
            }
            finally
            {
                await asyncEnumerator.DisposeAsync();
            }

            throw new InvalidOperationException("source must contain 1 element that satisfies the condition.", Internal.GetFormattedStacktrace(1));
        }

        /// <summary>
        /// Asynchronously returns the only element of an async-enumerable sequence that satisfies the specified condition, and reports an exception if there is not exactly one element in the async-enumerable sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">The sequence to return the single element of.</param>
        /// <param name="predicate">An async function to test each element for a condition.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>A <see cref="Promise{T}"/> resulting in the single element in the sequence that passes the test in the specified predicate function.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="source"/> is empty, or no element satisfies the condition in <paramref name="predicate"/>.</exception>
        public static Promise<TSource> SingleAsync<TSource>(this AsyncEnumerable<TSource> source, Func<TSource, Promise<bool>> predicate, CancelationToken cancelationToken = default)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return SingleAsyncCoreAwait(source.GetAsyncEnumerator(cancelationToken), Internal.PromiseRefBase.DelegateWrapper.Create(predicate));
        }

        /// <summary>
        /// Asynchronously returns the only element of an async-enumerable sequence that satisfies the specified condition, and reports an exception if there is not exactly one element in the async-enumerable sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TCapture">The type of the <paramref name="captureValue"/>.</typeparam>
        /// <param name="source">The sequence to return the single element of.</param>
        /// <param name="captureValue">The extra value that will be passed to the <paramref name="predicate"/>.</param>
        /// <param name="predicate">An async function to test each element for a condition.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>A <see cref="Promise{T}"/> resulting in the single element in the sequence that passes the test in the specified predicate function.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="source"/> is empty, or no element satisfies the condition in <paramref name="predicate"/>.</exception>
        public static Promise<TSource> SingleAsync<TSource, TCapture>(this AsyncEnumerable<TSource> source, TCapture captureValue, Func<TCapture, TSource, Promise<bool>> predicate, CancelationToken cancelationToken = default)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return SingleAsyncCoreAwait(source.GetAsyncEnumerator(cancelationToken), Internal.PromiseRefBase.DelegateWrapper.Create(captureValue, predicate));
        }

        private static async Promise<TSource> SingleAsyncCoreAwait<TSource, TPredicate>(AsyncEnumerator<TSource> asyncEnumerator, TPredicate predicate)
            where TPredicate : IFunc<TSource, Promise<bool>>
        {
            try
            {
                while (await asyncEnumerator.MoveNextAsync())
                {
                    var result = asyncEnumerator.Current;
                    if (await predicate.Invoke(result))
                    {
                        while (await asyncEnumerator.MoveNextAsync())
                        {
                            if (await predicate.Invoke(asyncEnumerator.Current))
                            {
                                throw new InvalidOperationException("source contains more than 1 element that satisfies the condition.", Internal.GetFormattedStacktrace(1));
                            }
                        }
                        return result;
                    }
                }
            }
            finally
            {
                await asyncEnumerator.DisposeAsync();
            }

            throw new InvalidOperationException("source must contain 1 element that satisfies the condition.", Internal.GetFormattedStacktrace(1));
        }

        /// <summary>
        /// Asynchronously returns the only element of an async-enumerable sequence that satisfies the specified condition, and reports an exception if there is not exactly one element in the async-enumerable sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="configuredSource"/>.</typeparam>
        /// <param name="configuredSource">The sequence to return the single element of.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <returns>A <see cref="Promise{T}"/> resulting in the single element in the sequence that passes the test in the specified predicate function.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="configuredSource"/> is empty, or no element satisfies the condition in <paramref name="predicate"/>.</exception>
        public static Promise<TSource> SingleAsync<TSource>(this in ConfiguredAsyncEnumerable<TSource> configuredSource, Func<TSource, bool> predicate)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return SingleAsyncCore(configuredSource.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(predicate));
        }

        /// <summary>
        /// Asynchronously returns the only element of an async-enumerable sequence that satisfies the specified condition, and reports an exception if there is not exactly one element in the async-enumerable sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="configuredSource"/>.</typeparam>
        /// <typeparam name="TCapture">The type of the <paramref name="captureValue"/>.</typeparam>
        /// <param name="configuredSource">The sequence to return the single element of.</param>
        /// <param name="captureValue">The extra value that will be passed to the <paramref name="predicate"/>.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <returns>A <see cref="Promise{T}"/> resulting in the single element in the sequence that passes the test in the specified predicate function.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="configuredSource"/> is empty, or no element satisfies the condition in <paramref name="predicate"/>.</exception>
        public static Promise<TSource> SingleAsync<TSource, TCapture>(this in ConfiguredAsyncEnumerable<TSource> configuredSource, TCapture captureValue, Func<TCapture, TSource, bool> predicate)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return SingleAsyncCore(configuredSource.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(captureValue, predicate));
        }

        private static async Promise<TSource> SingleAsyncCore<TSource, TPredicate>(ConfiguredAsyncEnumerable<TSource>.Enumerator asyncEnumerator, TPredicate predicate)
            where TPredicate : IFunc<TSource, bool>
        {
            try
            {
                while (await asyncEnumerator.MoveNextAsync())
                {
                    var result = asyncEnumerator.Current;
                    if (predicate.Invoke(result))
                    {
                        while (await asyncEnumerator.MoveNextAsync())
                        {
                            if (predicate.Invoke(asyncEnumerator.Current))
                            {
                                throw new InvalidOperationException("source contains more than 1 element that satisfies the condition.", Internal.GetFormattedStacktrace(1));
                            }
                        }
                        return result;
                    }
                }
            }
            finally
            {
                await asyncEnumerator.DisposeAsync();
            }

            throw new InvalidOperationException("source must contain 1 element that satisfies the condition.", Internal.GetFormattedStacktrace(1));
        }

        /// <summary>
        /// Asynchronously returns the only element of an async-enumerable sequence that satisfies the specified condition, and reports an exception if there is not exactly one element in the async-enumerable sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="configuredSource"/>.</typeparam>
        /// <param name="configuredSource">The sequence to return the single element of.</param>
        /// <param name="predicate">An async function to test each element for a condition.</param>
        /// <returns>A <see cref="Promise{T}"/> resulting in the single element in the sequence that passes the test in the specified predicate function.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="configuredSource"/> is empty, or no element satisfies the condition in <paramref name="predicate"/>.</exception>
        public static Promise<TSource> SingleAsync<TSource>(this in ConfiguredAsyncEnumerable<TSource> configuredSource, Func<TSource, Promise<bool>> predicate)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return SingleAsyncCoreAwait(configuredSource.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(predicate));
        }

        /// <summary>
        /// Asynchronously returns the only element of an async-enumerable sequence that satisfies the specified condition, and reports an exception if there is not exactly one element in the async-enumerable sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="configuredSource"/>.</typeparam>
        /// <typeparam name="TCapture">The type of the <paramref name="captureValue"/>.</typeparam>
        /// <param name="configuredSource">The sequence to return the single element of.</param>
        /// <param name="captureValue">The extra value that will be passed to the <paramref name="predicate"/>.</param>
        /// <param name="predicate">An async function to test each element for a condition.</param>
        /// <returns>A <see cref="Promise{T}"/> resulting in the single element in the sequence that passes the test in the specified predicate function.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="configuredSource"/> is empty, or no element satisfies the condition in <paramref name="predicate"/>.</exception>
        public static Promise<TSource> SingleAsync<TSource, TCapture>(this in ConfiguredAsyncEnumerable<TSource> configuredSource, TCapture captureValue, Func<TCapture, TSource, Promise<bool>> predicate)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return SingleAsyncCoreAwait(configuredSource.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(captureValue, predicate));
        }

        private static async Promise<TSource> SingleAsyncCoreAwait<TSource, TPredicate>(ConfiguredAsyncEnumerable<TSource>.Enumerator asyncEnumerator, TPredicate predicate)
            where TPredicate : IFunc<TSource, Promise<bool>>
        {
            try
            {
                while (await asyncEnumerator.MoveNextAsync())
                {
                    var result = asyncEnumerator.Current;
                    if (await predicate.Invoke(result))
                    {
                        while (await asyncEnumerator.MoveNextAsync())
                        {
                            if (await predicate.Invoke(asyncEnumerator.Current))
                            {
                                throw new InvalidOperationException("source contains more than 1 element that satisfies the condition.", Internal.GetFormattedStacktrace(1));
                            }
                        }
                        return result;
                    }
                }
            }
            finally
            {
                await asyncEnumerator.DisposeAsync();
            }

            throw new InvalidOperationException("source must contain 1 element that satisfies the condition.", Internal.GetFormattedStacktrace(1));
        }
    }
}