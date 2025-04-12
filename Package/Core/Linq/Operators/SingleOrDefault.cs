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
        private static async Promise<TSource> SingleOrDefaultAsyncCore<TSource, TPredicate>(AsyncEnumerator<TSource> asyncEnumerator, TPredicate predicate, TSource defaultValue, CancelationToken cancelationToken)
            where TPredicate : IFunc<TSource, CancelationToken, Promise<bool>>
        {
            try
            {
                while (await asyncEnumerator.MoveNextAsync())
                {
                    var result = asyncEnumerator.Current;
                    if (await predicate.Invoke(result, cancelationToken))
                    {
                        while (await asyncEnumerator.MoveNextAsync())
                        {
                            if (await predicate.Invoke(asyncEnumerator.Current, cancelationToken))
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

            return defaultValue;
        }

        private static async Promise<TSource> SingleOrDefaultAsyncCore<TSource, TPredicate>(ConfiguredAsyncEnumerable<TSource>.Enumerator asyncEnumerator, TPredicate predicate, TSource defaultValue, CancelationToken cancelationToken)
            where TPredicate : IFunc<TSource, CancelationToken, Promise<bool>>
        {
            try
            {
                while (await asyncEnumerator.MoveNextAsync())
                {
                    var result = asyncEnumerator.Current;
                    if (await predicate.Invoke(result, cancelationToken))
                    {
                        while (await asyncEnumerator.MoveNextAsync())
                        {
                            if (await predicate.Invoke(asyncEnumerator.Current, cancelationToken))
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

            return defaultValue;
        }

        /// <summary>
        /// Asynchronously returns the only element of an async-enumerable sequence, or a default value if the sequence is empty.
        /// This method reports an exception if there is more than one element in the async-enumerable sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">The sequence to return the single element of.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>A <see cref="Promise{T}"/> resulting in the single element, or a default value if <paramref name="source"/> is empty.</returns>
        public static Promise<TSource> SingleOrDefaultAsync<TSource>(this AsyncEnumerable<TSource> source, CancelationToken cancelationToken = default)
        {
            return Core(source.GetAsyncEnumerator(cancelationToken));

            async Promise<TSource> Core(AsyncEnumerator<TSource> asyncEnumerator)
            {
                try
                {
                    if (!await asyncEnumerator.MoveNextAsync())
                    {
                        return default;
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
        /// Asynchronously returns the only element of an async-enumerable sequence, or the specified default value if the sequence is empty.
        /// This method reports an exception if there is more than one element in the async-enumerable sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">The sequence to return the single element of.</param>
        /// <param name="defaultValue">The default value to return if the sequence is empty.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>A <see cref="Promise{T}"/> resulting in the single element, or <paramref name="defaultValue"/> if <paramref name="source"/> is empty.</returns>
        public static Promise<TSource> SingleOrDefaultAsync<TSource>(this AsyncEnumerable<TSource> source, TSource defaultValue, CancelationToken cancelationToken = default)
        {
            return Core(source.GetAsyncEnumerator(cancelationToken), defaultValue);

            async Promise<TSource> Core(AsyncEnumerator<TSource> asyncEnumerator, TSource d)
            {
                try
                {
                    if (!await asyncEnumerator.MoveNextAsync())
                    {
                        return d;
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
        /// Asynchronously returns the only element of an async-enumerable sequence that satisfies the specified condition, or a default value if no such element is found.
        /// This method reports an exception if more than one element satisfies the condition.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">The sequence to return the single element of.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>
        /// A <see cref="Promise{T}"/> resulting in the single element in the sequence that passes the test in the specified predicate function,
        /// or a default value if <paramref name="source"/> is empty or no element passes the test in the specified predicate function.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
        public static Promise<TSource> SingleOrDefaultAsync<TSource>(this AsyncEnumerable<TSource> source, Func<TSource, bool> predicate, CancelationToken cancelationToken = default)
            => SingleOrDefaultAsync(source, predicate, default(TSource), cancelationToken);

        /// <summary>
        /// Asynchronously returns the only element of an async-enumerable sequence that satisfies the specified condition, or a default value if no such element is found.
        /// This method reports an exception if more than one element satisfies the condition.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TCapture">The type of the <paramref name="captureValue"/>.</typeparam>
        /// <param name="source">The sequence to return the single element of.</param>
        /// <param name="captureValue">The extra value that will be passed to the <paramref name="predicate"/>.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>
        /// A <see cref="Promise{T}"/> resulting in the single element in the sequence that passes the test in the specified predicate function,
        /// or a default value if <paramref name="source"/> is empty or no element passes the test in the specified predicate function.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
        public static Promise<TSource> SingleOrDefaultAsync<TSource, TCapture>(this AsyncEnumerable<TSource> source, TCapture captureValue, Func<TCapture, TSource, bool> predicate, CancelationToken cancelationToken = default)
            => SingleOrDefaultAsync(source, captureValue, predicate, default(TSource), cancelationToken);

        /// <summary>
        /// Asynchronously returns the only element of an async-enumerable sequence that satisfies the specified condition, or a default value if no such element is found.
        /// This method reports an exception if more than one element satisfies the condition.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">The sequence to return the single element of.</param>
        /// <param name="predicate">An async function to test each element for a condition.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>
        /// A <see cref="Promise{T}"/> resulting in the single element in the sequence that passes the test in the specified predicate function,
        /// or a default value if <paramref name="source"/> is empty or no element passes the test in the specified predicate function.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
        public static Promise<TSource> SingleOrDefaultAsync<TSource>(this AsyncEnumerable<TSource> source, Func<TSource, CancelationToken, Promise<bool>> predicate, CancelationToken cancelationToken = default)
            => SingleOrDefaultAsync(source, predicate, default(TSource), cancelationToken);

        /// <summary>
        /// Asynchronously returns the only element of an async-enumerable sequence that satisfies the specified condition, or a default value if no such element is found.
        /// This method reports an exception if more than one element satisfies the condition.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TCapture">The type of the <paramref name="captureValue"/>.</typeparam>
        /// <param name="source">The sequence to return the single element of.</param>
        /// <param name="captureValue">The extra value that will be passed to the <paramref name="predicate"/>.</param>
        /// <param name="predicate">An async function to test each element for a condition.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>
        /// A <see cref="Promise{T}"/> resulting in the single element in the sequence that passes the test in the specified predicate function,
        /// or a default value if <paramref name="source"/> is empty or no element passes the test in the specified predicate function.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
        public static Promise<TSource> SingleOrDefaultAsync<TSource, TCapture>(this AsyncEnumerable<TSource> source, TCapture captureValue, Func<TCapture, TSource, CancelationToken, Promise<bool>> predicate, CancelationToken cancelationToken = default)
            => SingleOrDefaultAsync(source, captureValue, predicate, default(TSource), cancelationToken);

        /// <summary>
        /// Asynchronously returns the only element of an async-enumerable sequence that satisfies the specified condition, or a default value if no such element is found.
        /// This method reports an exception if more than one element satisfies the condition.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="configuredSource"/>.</typeparam>
        /// <param name="configuredSource">The sequence to return the single element of.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <returns>
        /// A <see cref="Promise{T}"/> resulting in the single element in the sequence that passes the test in the specified predicate function,
        /// or a default value if <paramref name="configuredSource"/> is empty or no element passes the test in the specified predicate function.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
        public static Promise<TSource> SingleOrDefaultAsync<TSource>(this in ConfiguredAsyncEnumerable<TSource> configuredSource, Func<TSource, bool> predicate)
            => SingleOrDefaultAsync(configuredSource, predicate, default(TSource));

        /// <summary>
        /// Asynchronously returns the only element of an async-enumerable sequence that satisfies the specified condition, or a default value if no such element is found.
        /// This method reports an exception if more than one element satisfies the condition.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="configuredSource"/>.</typeparam>
        /// <typeparam name="TCapture">The type of the <paramref name="captureValue"/>.</typeparam>
        /// <param name="configuredSource">The sequence to return the single element of.</param>
        /// <param name="captureValue">The extra value that will be passed to the <paramref name="predicate"/>.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <returns>
        /// A <see cref="Promise{T}"/> resulting in the single element in the sequence that passes the test in the specified predicate function,
        /// or a default value if <paramref name="configuredSource"/> is empty or no element passes the test in the specified predicate function.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
        public static Promise<TSource> SingleOrDefaultAsync<TSource, TCapture>(this in ConfiguredAsyncEnumerable<TSource> configuredSource, TCapture captureValue, Func<TCapture, TSource, bool> predicate)
            => SingleOrDefaultAsync(configuredSource, captureValue, predicate, default(TSource));

        /// <summary>
        /// Asynchronously returns the only element of an async-enumerable sequence that satisfies the specified condition, or a default value if no such element is found.
        /// This method reports an exception if more than one element satisfies the condition.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="configuredSource"/>.</typeparam>
        /// <param name="configuredSource">The sequence to return the single element of.</param>
        /// <param name="predicate">An async function to test each element for a condition.</param>
        /// <returns>
        /// A <see cref="Promise{T}"/> resulting in the single element in the sequence that passes the test in the specified predicate function,
        /// or a default value if <paramref name="configuredSource"/> is empty or no element passes the test in the specified predicate function.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
        public static Promise<TSource> SingleOrDefaultAsync<TSource>(this in ConfiguredAsyncEnumerable<TSource> configuredSource, Func<TSource, CancelationToken, Promise<bool>> predicate)
            => SingleOrDefaultAsync(configuredSource, predicate, default(TSource));

        /// <summary>
        /// Asynchronously returns the only element of an async-enumerable sequence that satisfies the specified condition, or a default value if no such element is found.
        /// This method reports an exception if more than one element satisfies the condition.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="configuredSource"/>.</typeparam>
        /// <typeparam name="TCapture">The type of the <paramref name="captureValue"/>.</typeparam>
        /// <param name="configuredSource">The sequence to return the single element of.</param>
        /// <param name="captureValue">The extra value that will be passed to the <paramref name="predicate"/>.</param>
        /// <param name="predicate">An async function to test each element for a condition.</param>
        /// <returns>
        /// A <see cref="Promise{T}"/> resulting in the single element in the sequence that passes the test in the specified predicate function,
        /// or a default value if <paramref name="configuredSource"/> is empty or no element passes the test in the specified predicate function.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
        public static Promise<TSource> SingleOrDefaultAsync<TSource, TCapture>(this in ConfiguredAsyncEnumerable<TSource> configuredSource, TCapture captureValue, Func<TCapture, TSource, CancelationToken, Promise<bool>> predicate)
            => SingleOrDefaultAsync(configuredSource, captureValue, predicate, default(TSource));

        /// <summary>
        /// Asynchronously returns the only element of an async-enumerable sequence that satisfies the specified condition, or the specified default value if no such element is found.
        /// This method reports an exception if more than one element satisfies the condition.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">The sequence to return the single element of.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <param name="defaultValue">The default value to return if the sequence is empty or no element passes the test specified by <paramref name="predicate"/>.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>
        /// A <see cref="Promise{T}"/> resulting in the single element in the sequence that passes the test in the specified predicate function,
        /// or the specified default value if <paramref name="source"/> is empty or no element passes the test in the specified predicate function.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
        public static Promise<TSource> SingleOrDefaultAsync<TSource>(this AsyncEnumerable<TSource> source, Func<TSource, bool> predicate, TSource defaultValue, CancelationToken cancelationToken = default)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return SingleOrDefaultAsyncCore(source.GetAsyncEnumerator(cancelationToken), DelegateWrapper.Create(predicate), defaultValue, cancelationToken);
        }

        /// <summary>
        /// Asynchronously returns the only element of an async-enumerable sequence that satisfies the specified condition, or the specified default value if no such element is found.
        /// This method reports an exception if more than one element satisfies the condition.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TCapture">The type of the <paramref name="captureValue"/>.</typeparam>
        /// <param name="source">The sequence to return the single element of.</param>
        /// <param name="captureValue">The extra value that will be passed to the <paramref name="predicate"/>.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <param name="defaultValue">The default value to return if the sequence is empty or no element passes the test specified by <paramref name="predicate"/>.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>
        /// A <see cref="Promise{T}"/> resulting in the single element in the sequence that passes the test in the specified predicate function,
        /// or the specified default value if <paramref name="source"/> is empty or no element passes the test in the specified predicate function.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
        public static Promise<TSource> SingleOrDefaultAsync<TSource, TCapture>(this AsyncEnumerable<TSource> source, TCapture captureValue, Func<TCapture, TSource, bool> predicate, TSource defaultValue, CancelationToken cancelationToken = default)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return SingleOrDefaultAsyncCore(source.GetAsyncEnumerator(cancelationToken), DelegateWrapper.Create(captureValue, predicate), defaultValue, cancelationToken);
        }

        /// <summary>
        /// Asynchronously returns the only element of an async-enumerable sequence that satisfies the specified condition, or the specified default value if no such element is found.
        /// This method reports an exception if more than one element satisfies the condition.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">The sequence to return the single element of.</param>
        /// <param name="predicate">An async function to test each element for a condition.</param>
        /// <param name="defaultValue">The default value to return if the sequence is empty or no element passes the test specified by <paramref name="predicate"/>.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>
        /// A <see cref="Promise{T}"/> resulting in the single element in the sequence that passes the test in the specified predicate function,
        /// or the specified default value if <paramref name="source"/> is empty or no element passes the test in the specified predicate function.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
        public static Promise<TSource> SingleOrDefaultAsync<TSource>(this AsyncEnumerable<TSource> source, Func<TSource, CancelationToken, Promise<bool>> predicate, TSource defaultValue, CancelationToken cancelationToken = default)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return SingleOrDefaultAsyncCore(source.GetAsyncEnumerator(cancelationToken), DelegateWrapper.Create(predicate), defaultValue, cancelationToken);
        }

        /// <summary>
        /// Asynchronously returns the only element of an async-enumerable sequence that satisfies the specified condition, or the specified default value if no such element is found.
        /// This method reports an exception if more than one element satisfies the condition.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TCapture">The type of the <paramref name="captureValue"/>.</typeparam>
        /// <param name="source">The sequence to return the single element of.</param>
        /// <param name="captureValue">The extra value that will be passed to the <paramref name="predicate"/>.</param>
        /// <param name="predicate">An async function to test each element for a condition.</param>
        /// <param name="defaultValue">The default value to return if the sequence is empty or no element passes the test specified by <paramref name="predicate"/>.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>
        /// A <see cref="Promise{T}"/> resulting in the single element in the sequence that passes the test in the specified predicate function,
        /// or the specified default value if <paramref name="source"/> is empty or no element passes the test in the specified predicate function.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
        public static Promise<TSource> SingleOrDefaultAsync<TSource, TCapture>(this AsyncEnumerable<TSource> source, TCapture captureValue, Func<TCapture, TSource, CancelationToken, Promise<bool>> predicate, TSource defaultValue, CancelationToken cancelationToken = default)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return SingleOrDefaultAsyncCore(source.GetAsyncEnumerator(cancelationToken), DelegateWrapper.Create(captureValue, predicate), defaultValue, cancelationToken);
        }

        /// <summary>
        /// Asynchronously returns the only element of an async-enumerable sequence that satisfies the specified condition, or the specified default value if no such element is found.
        /// This method reports an exception if more than one element satisfies the condition.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="configuredSource"/>.</typeparam>
        /// <param name="configuredSource">The sequence to return the single element of.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <param name="defaultValue">The default value to return if the sequence is empty or no element passes the test specified by <paramref name="predicate"/>.</param>
        /// <returns>
        /// A <see cref="Promise{T}"/> resulting in the single element in the sequence that passes the test in the specified predicate function,
        /// or the specified default value if <paramref name="configuredSource"/> is empty or no element passes the test in the specified predicate function.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
        public static Promise<TSource> SingleOrDefaultAsync<TSource>(this in ConfiguredAsyncEnumerable<TSource> configuredSource, Func<TSource, bool> predicate, TSource defaultValue)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return SingleOrDefaultAsyncCore(configuredSource.GetAsyncEnumerator(), DelegateWrapper.Create(predicate), defaultValue, configuredSource.CancelationToken);
        }

        /// <summary>
        /// Asynchronously returns the only element of an async-enumerable sequence that satisfies the specified condition, or the specified default value if no such element is found.
        /// This method reports an exception if more than one element satisfies the condition.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="configuredSource"/>.</typeparam>
        /// <typeparam name="TCapture">The type of the <paramref name="captureValue"/>.</typeparam>
        /// <param name="configuredSource">The sequence to return the single element of.</param>
        /// <param name="captureValue">The extra value that will be passed to the <paramref name="predicate"/>.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <param name="defaultValue">The default value to return if the sequence is empty or no element passes the test specified by <paramref name="predicate"/>.</param>
        /// <returns>
        /// A <see cref="Promise{T}"/> resulting in the single element in the sequence that passes the test in the specified predicate function,
        /// or the specified default value if <paramref name="configuredSource"/> is empty or no element passes the test in the specified predicate function.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
        public static Promise<TSource> SingleOrDefaultAsync<TSource, TCapture>(this in ConfiguredAsyncEnumerable<TSource> configuredSource, TCapture captureValue, Func<TCapture, TSource, bool> predicate, TSource defaultValue)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return SingleOrDefaultAsyncCore(configuredSource.GetAsyncEnumerator(), DelegateWrapper.Create(captureValue, predicate), defaultValue, configuredSource.CancelationToken);
        }

        /// <summary>
        /// Asynchronously returns the only element of an async-enumerable sequence that satisfies the specified condition, or the specified default value if no such element is found.
        /// This method reports an exception if more than one element satisfies the condition.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="configuredSource"/>.</typeparam>
        /// <param name="configuredSource">The sequence to return the single element of.</param>
        /// <param name="predicate">An async function to test each element for a condition.</param>
        /// <param name="defaultValue">The default value to return if the sequence is empty or no element passes the test specified by <paramref name="predicate"/>.</param>
        /// <returns>
        /// A <see cref="Promise{T}"/> resulting in the single element in the sequence that passes the test in the specified predicate function,
        /// or the specified default value if <paramref name="configuredSource"/> is empty or no element passes the test in the specified predicate function.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
        public static Promise<TSource> SingleOrDefaultAsync<TSource>(this in ConfiguredAsyncEnumerable<TSource> configuredSource, Func<TSource, CancelationToken, Promise<bool>> predicate, TSource defaultValue)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return SingleOrDefaultAsyncCore(configuredSource.GetAsyncEnumerator(), DelegateWrapper.Create(predicate), defaultValue, configuredSource.CancelationToken);
        }

        /// <summary>
        /// Asynchronously returns the only element of an async-enumerable sequence that satisfies the specified condition, or the specified default value if no such element is found.
        /// This method reports an exception if more than one element satisfies the condition.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="configuredSource"/>.</typeparam>
        /// <typeparam name="TCapture">The type of the <paramref name="captureValue"/>.</typeparam>
        /// <param name="configuredSource">The sequence to return the single element of.</param>
        /// <param name="captureValue">The extra value that will be passed to the <paramref name="predicate"/>.</param>
        /// <param name="predicate">An async function to test each element for a condition.</param>
        /// <param name="defaultValue">The default value to return if the sequence is empty or no element passes the test specified by <paramref name="predicate"/>.</param>
        /// <returns>
        /// A <see cref="Promise{T}"/> resulting in the single element in the sequence that passes the test in the specified predicate function,
        /// or the specified default value if <paramref name="configuredSource"/> is empty or no element passes the test in the specified predicate function.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
        public static Promise<TSource> SingleOrDefaultAsync<TSource, TCapture>(this in ConfiguredAsyncEnumerable<TSource> configuredSource, TCapture captureValue, Func<TCapture, TSource, CancelationToken, Promise<bool>> predicate, TSource defaultValue)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return SingleOrDefaultAsyncCore(configuredSource.GetAsyncEnumerator(), DelegateWrapper.Create(captureValue, predicate), defaultValue, configuredSource.CancelationToken);
        }
    }
}