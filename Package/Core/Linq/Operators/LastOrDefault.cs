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
        private static async Promise<TSource> LastOrDefaultAsyncCore<TSource, TPredicate>(AsyncEnumerator<TSource> asyncEnumerator, TPredicate predicate, TSource defaultValue, CancelationToken cancelationToken)
            where TPredicate : IFunc<TSource, CancelationToken, Promise<bool>>
        {
            try
            {
                while (await asyncEnumerator.MoveNextAsync())
                {
                    var item = asyncEnumerator.Current;
                    if (await predicate.Invoke(item, cancelationToken))
                    {
                        defaultValue = item;
                    }
                }

                return defaultValue;
            }
            finally
            {
                await asyncEnumerator.DisposeAsync();
            }
        }

        private static async Promise<TSource> LastOrDefaultAsyncCore<TSource, TPredicate>(ConfiguredAsyncEnumerable<TSource>.Enumerator asyncEnumerator, TPredicate predicate, TSource defaultValue, CancelationToken cancelationToken)
            where TPredicate : IFunc<TSource, CancelationToken, Promise<bool>>
        {
            try
            {
                while (await asyncEnumerator.MoveNextAsync())
                {
                    var item = asyncEnumerator.Current;
                    if (await predicate.Invoke(item, cancelationToken))
                    {
                        defaultValue = item;
                    }
                }

                return defaultValue;
            }
            finally
            {
                await asyncEnumerator.DisposeAsync();
            }
        }

        /// <summary>
        /// Asynchronously returns the last element of an async-enumerable sequence, or a default value if no element is found.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">The sequence to return the last element of.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>A <see cref="Promise{T}"/> resulting in the last element, or a default value if <paramref name="source"/> is empty.</returns>
        /// <exception cref="InvalidOperationException"><paramref name="source"/> is empty.</exception>
        public static Promise<TSource> LastOrDefaultAsync<TSource>(this AsyncEnumerable<TSource> source, CancelationToken cancelationToken = default)
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

                    TSource last;
                    do
                    {
                        last = asyncEnumerator.Current;
                    } while (await asyncEnumerator.MoveNextAsync());
                    return last;
                }
                finally
                {
                    await asyncEnumerator.DisposeAsync();
                }
            }
        }

        /// <summary>
        /// Asynchronously returns the last element of an async-enumerable sequence, or a specified default value if no element is found.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">The sequence to return the last element of.</param>
        /// <param name="defaultValue">The default value to return if the sequence is empty.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>A <see cref="Promise{T}"/> resulting in the last element, or <paramref name="defaultValue"/> if <paramref name="source"/> is empty.</returns>
        /// <exception cref="InvalidOperationException"><paramref name="source"/> is empty.</exception>
        public static Promise<TSource> LastOrDefaultAsync<TSource>(this AsyncEnumerable<TSource> source, TSource defaultValue, CancelationToken cancelationToken = default)
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

                    TSource last;
                    do
                    {
                        last = asyncEnumerator.Current;
                    } while (await asyncEnumerator.MoveNextAsync());
                    return last;
                }
                finally
                {
                    await asyncEnumerator.DisposeAsync();
                }
            }
        }

        /// <summary>
        /// Asynchronously returns the last element of an async-enumerable sequence that satisfies a specified condition, or a default value if no such element is found.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">The sequence to return the last element of.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>
        /// A <see cref="Promise{T}"/> resulting in the last element in the sequence that passes the test in the specified predicate function,
        /// or a default value if <paramref name="source"/> is empty or no element passes the test in the specified predicate function.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
        public static Promise<TSource> LastOrDefaultAsync<TSource>(this AsyncEnumerable<TSource> source, Func<TSource, bool> predicate, CancelationToken cancelationToken = default)
            => LastOrDefaultAsync(source, predicate, default(TSource), cancelationToken);

        /// <summary>
        /// Asynchronously returns the last element of an async-enumerable sequence that satisfies a specified condition, or a default value if no such element is found.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TCapture">The type of the <paramref name="captureValue"/>.</typeparam>
        /// <param name="source">The sequence to return the last element of.</param>
        /// <param name="captureValue">The extra value that will be passed to the <paramref name="predicate"/>.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>
        /// A <see cref="Promise{T}"/> resulting in the last element in the sequence that passes the test in the specified predicate function,
        /// or a default value if <paramref name="source"/> is empty or no element passes the test in the specified predicate function.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
        public static Promise<TSource> LastOrDefaultAsync<TSource, TCapture>(this AsyncEnumerable<TSource> source, TCapture captureValue, Func<TCapture, TSource, bool> predicate, CancelationToken cancelationToken = default)
            => LastOrDefaultAsync(source, captureValue, predicate, default(TSource), cancelationToken);

        /// <summary>
        /// Asynchronously returns the last element of an async-enumerable sequence that satisfies a specified condition, or a default value if no such element is found.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">The sequence to return the last element of.</param>
        /// <param name="predicate">An async function to test each element for a condition.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>
        /// A <see cref="Promise{T}"/> resulting in the last element in the sequence that passes the test in the specified predicate function,
        /// or a default value if <paramref name="source"/> is empty or no element passes the test in the specified predicate function.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
        public static Promise<TSource> LastOrDefaultAsync<TSource>(this AsyncEnumerable<TSource> source, Func<TSource, Promise<bool>> predicate, CancelationToken cancelationToken = default)
            => LastOrDefaultAsync(source, predicate, default(TSource), cancelationToken);

        /// <summary>
        /// Asynchronously returns the last element of an async-enumerable sequence that satisfies a specified condition, or a default value if no such element is found.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TCapture">The type of the <paramref name="captureValue"/>.</typeparam>
        /// <param name="source">The sequence to return the last element of.</param>
        /// <param name="captureValue">The extra value that will be passed to the <paramref name="predicate"/>.</param>
        /// <param name="predicate">An async function to test each element for a condition.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>
        /// A <see cref="Promise{T}"/> resulting in the last element in the sequence that passes the test in the specified predicate function,
        /// or a default value if <paramref name="source"/> is empty or no element passes the test in the specified predicate function.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
        public static Promise<TSource> LastOrDefaultAsync<TSource, TCapture>(this AsyncEnumerable<TSource> source, TCapture captureValue, Func<TCapture, TSource, Promise<bool>> predicate, CancelationToken cancelationToken = default)
            => LastOrDefaultAsync(source, captureValue, predicate, default(TSource), cancelationToken);

        /// <summary>
        /// Asynchronously returns the last element of an async-enumerable sequence that satisfies a specified condition, or a default value if no such element is found.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="configuredSource"/>.</typeparam>
        /// <param name="configuredSource">The sequence to return the last element of.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <returns>
        /// A <see cref="Promise{T}"/> resulting in the last element in the sequence that passes the test in the specified predicate function,
        /// or a default value if <paramref name="configuredSource"/> is empty or no element passes the test in the specified predicate function.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
        public static Promise<TSource> LastOrDefaultAsync<TSource>(this in ConfiguredAsyncEnumerable<TSource> configuredSource, Func<TSource, bool> predicate)
            => LastOrDefaultAsync(configuredSource, predicate, default(TSource));

        /// <summary>
        /// Asynchronously returns the last element of an async-enumerable sequence that satisfies a specified condition, or a default value if no such element is found.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="configuredSource"/>.</typeparam>
        /// <typeparam name="TCapture">The type of the <paramref name="captureValue"/>.</typeparam>
        /// <param name="configuredSource">The sequence to return the last element of.</param>
        /// <param name="captureValue">The extra value that will be passed to the <paramref name="predicate"/>.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <returns>
        /// A <see cref="Promise{T}"/> resulting in the last element in the sequence that passes the test in the specified predicate function,
        /// or a default value if <paramref name="configuredSource"/> is empty or no element passes the test in the specified predicate function.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
        public static Promise<TSource> LastOrDefaultAsync<TSource, TCapture>(this in ConfiguredAsyncEnumerable<TSource> configuredSource, TCapture captureValue, Func<TCapture, TSource, bool> predicate)
            => LastOrDefaultAsync(configuredSource, captureValue, predicate, default(TSource));

        /// <summary>
        /// Asynchronously returns the last element of an async-enumerable sequence that satisfies a specified condition, or a default value if no such element is found.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="configuredSource"/>.</typeparam>
        /// <param name="configuredSource">The sequence to return the last element of.</param>
        /// <param name="predicate">An async function to test each element for a condition.</param>
        /// <returns>
        /// A <see cref="Promise{T}"/> resulting in the last element in the sequence that passes the test in the specified predicate function,
        /// or a default value if <paramref name="configuredSource"/> is empty or no element passes the test in the specified predicate function.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
        public static Promise<TSource> LastOrDefaultAsync<TSource>(this in ConfiguredAsyncEnumerable<TSource> configuredSource, Func<TSource, Promise<bool>> predicate)
            => LastOrDefaultAsync(configuredSource, predicate, default(TSource));

        /// <summary>
        /// Asynchronously returns the last element of an async-enumerable sequence that satisfies a specified condition, or a default value if no such element is found.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="configuredSource"/>.</typeparam>
        /// <typeparam name="TCapture">The type of the <paramref name="captureValue"/>.</typeparam>
        /// <param name="configuredSource">The sequence to return the last element of.</param>
        /// <param name="captureValue">The extra value that will be passed to the <paramref name="predicate"/>.</param>
        /// <param name="predicate">An async function to test each element for a condition.</param>
        /// <returns>
        /// A <see cref="Promise{T}"/> resulting in the last element in the sequence that passes the test in the specified predicate function,
        /// or a default value if <paramref name="configuredSource"/> is empty or no element passes the test in the specified predicate function.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
        public static Promise<TSource> LastOrDefaultAsync<TSource, TCapture>(this in ConfiguredAsyncEnumerable<TSource> configuredSource, TCapture captureValue, Func<TCapture, TSource, Promise<bool>> predicate)
            => LastOrDefaultAsync(configuredSource, captureValue, predicate, default(TSource));

        /// <summary>
        /// Asynchronously returns the last element of an async-enumerable sequence that satisfies a specified condition, or a specified default value if no such element is found.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">The sequence to return the last element of.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <param name="defaultValue">The default value to return if the sequence is empty or no element passes the test specified by <paramref name="predicate"/>.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>
        /// A <see cref="Promise{T}"/> resulting in the last element in the sequence that passes the test in the specified predicate function,
        /// or the specified default value if <paramref name="source"/> is empty or no element passes the test in the specified predicate function.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
        public static Promise<TSource> LastOrDefaultAsync<TSource>(this AsyncEnumerable<TSource> source, Func<TSource, bool> predicate, TSource defaultValue, CancelationToken cancelationToken = default)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return LastOrDefaultAsyncCore(source.GetAsyncEnumerator(cancelationToken), DelegateWrapper.Create(predicate), defaultValue, cancelationToken);
        }

        /// <summary>
        /// Asynchronously returns the last element of an async-enumerable sequence that satisfies a specified condition, or a specified default value if no such element is found.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TCapture">The type of the <paramref name="captureValue"/>.</typeparam>
        /// <param name="source">The sequence to return the last element of.</param>
        /// <param name="captureValue">The extra value that will be passed to the <paramref name="predicate"/>.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <param name="defaultValue">The default value to return if the sequence is empty or no element passes the test specified by <paramref name="predicate"/>.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>
        /// A <see cref="Promise{T}"/> resulting in the last element in the sequence that passes the test in the specified predicate function,
        /// or the specified default value if <paramref name="source"/> is empty or no element passes the test in the specified predicate function.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
        public static Promise<TSource> LastOrDefaultAsync<TSource, TCapture>(this AsyncEnumerable<TSource> source, TCapture captureValue, Func<TCapture, TSource, bool> predicate, TSource defaultValue, CancelationToken cancelationToken = default)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return LastOrDefaultAsyncCore(source.GetAsyncEnumerator(cancelationToken), DelegateWrapper.Create(captureValue, predicate), defaultValue, cancelationToken);
        }

        /// <summary>
        /// Asynchronously returns the last element of an async-enumerable sequence that satisfies a specified condition, or a specified default value if no such element is found.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">The sequence to return the last element of.</param>
        /// <param name="predicate">An async function to test each element for a condition.</param>
        /// <param name="defaultValue">The default value to return if the sequence is empty or no element passes the test specified by <paramref name="predicate"/>.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>
        /// A <see cref="Promise{T}"/> resulting in the last element in the sequence that passes the test in the specified predicate function,
        /// or the specified default value if <paramref name="source"/> is empty or no element passes the test in the specified predicate function.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
        public static Promise<TSource> LastOrDefaultAsync<TSource>(this AsyncEnumerable<TSource> source, Func<TSource, Promise<bool>> predicate, TSource defaultValue, CancelationToken cancelationToken = default)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return LastOrDefaultAsyncCore(source.GetAsyncEnumerator(cancelationToken), DelegateWrapper.Create(predicate), defaultValue, cancelationToken);
        }

        /// <summary>
        /// Asynchronously returns the last element of an async-enumerable sequence that satisfies a specified condition, or a specified default value if no such element is found.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TCapture">The type of the <paramref name="captureValue"/>.</typeparam>
        /// <param name="source">The sequence to return the last element of.</param>
        /// <param name="captureValue">The extra value that will be passed to the <paramref name="predicate"/>.</param>
        /// <param name="predicate">An async function to test each element for a condition.</param>
        /// <param name="defaultValue">The default value to return if the sequence is empty or no element passes the test specified by <paramref name="predicate"/>.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>
        /// A <see cref="Promise{T}"/> resulting in the last element in the sequence that passes the test in the specified predicate function,
        /// or the specified default value if <paramref name="source"/> is empty or no element passes the test in the specified predicate function.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
        public static Promise<TSource> LastOrDefaultAsync<TSource, TCapture>(this AsyncEnumerable<TSource> source, TCapture captureValue, Func<TCapture, TSource, Promise<bool>> predicate, TSource defaultValue, CancelationToken cancelationToken = default)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return LastOrDefaultAsyncCore(source.GetAsyncEnumerator(cancelationToken), DelegateWrapper.Create(captureValue, predicate), defaultValue, cancelationToken);
        }

        /// <summary>
        /// Asynchronously returns the last element of an async-enumerable sequence that satisfies a specified condition, or a specified default value if no such element is found.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="configuredSource"/>.</typeparam>
        /// <param name="configuredSource">The sequence to return the last element of.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <param name="defaultValue">The default value to return if the sequence is empty or no element passes the test specified by <paramref name="predicate"/>.</param>
        /// <returns>
        /// A <see cref="Promise{T}"/> resulting in the last element in the sequence that passes the test in the specified predicate function,
        /// or the specified default value if <paramref name="configuredSource"/> is empty or no element passes the test in the specified predicate function.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
        public static Promise<TSource> LastOrDefaultAsync<TSource>(this in ConfiguredAsyncEnumerable<TSource> configuredSource, Func<TSource, bool> predicate, TSource defaultValue)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return LastOrDefaultAsyncCore(configuredSource.GetAsyncEnumerator(), DelegateWrapper.Create(predicate), defaultValue, configuredSource.CancelationToken);
        }

        /// <summary>
        /// Asynchronously returns the last element of an async-enumerable sequence that satisfies a specified condition, or a specified default value if no such element is found.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="configuredSource"/>.</typeparam>
        /// <typeparam name="TCapture">The type of the <paramref name="captureValue"/>.</typeparam>
        /// <param name="configuredSource">The sequence to return the last element of.</param>
        /// <param name="captureValue">The extra value that will be passed to the <paramref name="predicate"/>.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <param name="defaultValue">The default value to return if the sequence is empty or no element passes the test specified by <paramref name="predicate"/>.</param>
        /// <returns>
        /// A <see cref="Promise{T}"/> resulting in the last element in the sequence that passes the test in the specified predicate function,
        /// or the specified default value if <paramref name="configuredSource"/> is empty or no element passes the test in the specified predicate function.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
        public static Promise<TSource> LastOrDefaultAsync<TSource, TCapture>(this in ConfiguredAsyncEnumerable<TSource> configuredSource, TCapture captureValue, Func<TCapture, TSource, bool> predicate, TSource defaultValue)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return LastOrDefaultAsyncCore(configuredSource.GetAsyncEnumerator(), DelegateWrapper.Create(captureValue, predicate), defaultValue, configuredSource.CancelationToken);
        }

        /// <summary>
        /// Asynchronously returns the last element of an async-enumerable sequence that satisfies a specified condition, or a specified default value if no such element is found.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="configuredSource"/>.</typeparam>
        /// <param name="configuredSource">The sequence to return the last element of.</param>
        /// <param name="predicate">An async function to test each element for a condition.</param>
        /// <param name="defaultValue">The default value to return if the sequence is empty or no element passes the test specified by <paramref name="predicate"/>.</param>
        /// <returns>
        /// A <see cref="Promise{T}"/> resulting in the last element in the sequence that passes the test in the specified predicate function,
        /// or the specified default value if <paramref name="configuredSource"/> is empty or no element passes the test in the specified predicate function.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
        public static Promise<TSource> LastOrDefaultAsync<TSource>(this in ConfiguredAsyncEnumerable<TSource> configuredSource, Func<TSource, Promise<bool>> predicate, TSource defaultValue)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return LastOrDefaultAsyncCore(configuredSource.GetAsyncEnumerator(), DelegateWrapper.Create(predicate), defaultValue, configuredSource.CancelationToken);
        }

        /// <summary>
        /// Asynchronously returns the last element of an async-enumerable sequence that satisfies a specified condition, or a specified default value if no such element is found.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="configuredSource"/>.</typeparam>
        /// <typeparam name="TCapture">The type of the <paramref name="captureValue"/>.</typeparam>
        /// <param name="configuredSource">The sequence to return the last element of.</param>
        /// <param name="captureValue">The extra value that will be passed to the <paramref name="predicate"/>.</param>
        /// <param name="predicate">An async function to test each element for a condition.</param>
        /// <param name="defaultValue">The default value to return if the sequence is empty or no element passes the test specified by <paramref name="predicate"/>.</param>
        /// <returns>
        /// A <see cref="Promise{T}"/> resulting in the last element in the sequence that passes the test in the specified predicate function,
        /// or the specified default value if <paramref name="configuredSource"/> is empty or no element passes the test in the specified predicate function.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
        public static Promise<TSource> LastOrDefaultAsync<TSource, TCapture>(this in ConfiguredAsyncEnumerable<TSource> configuredSource, TCapture captureValue, Func<TCapture, TSource, Promise<bool>> predicate, TSource defaultValue)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return LastOrDefaultAsyncCore(configuredSource.GetAsyncEnumerator(), DelegateWrapper.Create(captureValue, predicate), defaultValue, configuredSource.CancelationToken);
        }
    }
}