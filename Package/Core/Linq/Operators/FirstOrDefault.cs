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
        /// Asynchronously returns the first element of an async-enumerable sequence, or a default value if no element is found.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">The sequence to return the first element of.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>A <see cref="Promise{T}"/> resulting in the first element, or a default value if <paramref name="source"/> is empty.</returns>
        public static Promise<TSource> FirstOrDefaultAsync<TSource>(this AsyncEnumerable<TSource> source, CancelationToken cancelationToken = default)
        {
            return Core(source.GetAsyncEnumerator(cancelationToken));

            async Promise<TSource> Core(AsyncEnumerator<TSource> asyncEnumerator)
            {
                try
                {
                    return await asyncEnumerator.MoveNextAsync()
                        ? asyncEnumerator.Current
                        : default;
                }
                finally
                {
                    await asyncEnumerator.DisposeAsync();
                }
            }
        }

        /// <summary>
        /// Asynchronously returns the first element of an async-enumerable sequence, or a specified default value if no element is found.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">The sequence to return the first element of.</param>
        /// <param name="defaultValue">The default value to return if the sequence is empty.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>A <see cref="Promise{T}"/> resulting in the first element, or <paramref name="defaultValue"/> if <paramref name="source"/> is empty.</returns>
        public static Promise<TSource> FirstOrDefaultAsync<TSource>(this AsyncEnumerable<TSource> source, TSource defaultValue, CancelationToken cancelationToken = default)
        {
            return Core(source.GetAsyncEnumerator(cancelationToken), defaultValue);

            async Promise<TSource> Core(AsyncEnumerator<TSource> asyncEnumerator, TSource d)
            {
                try
                {
                    return await asyncEnumerator.MoveNextAsync()
                        ? asyncEnumerator.Current
                        : d;
                }
                finally
                {
                    await asyncEnumerator.DisposeAsync();
                }
            }
        }

        /// <summary>
        /// Asynchronously returns the first element of an async-enumerable sequence that satisfies a specified condition, or a default value if no such element is found.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">The sequence to return the first element of.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>
        /// A <see cref="Promise{T}"/> resulting in the first element in the sequence that passes the test in the specified predicate function,
        /// or a default value if <paramref name="source"/> is empty or no element passes the test in the specified predicate function.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
        public static Promise<TSource> FirstOrDefaultAsync<TSource>(this AsyncEnumerable<TSource> source, Func<TSource, bool> predicate, CancelationToken cancelationToken = default)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return FirstOrDefaultAsyncCore(source.GetAsyncEnumerator(cancelationToken), DelegateWrapper.Create(predicate));
        }

        /// <summary>
        /// Asynchronously returns the first element of an async-enumerable sequence that satisfies a specified condition, or a default value if no such element is found.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TCapture">The type of the <paramref name="captureValue"/>.</typeparam>
        /// <param name="source">The sequence to return the first element of.</param>
        /// <param name="captureValue">The extra value that will be passed to the <paramref name="predicate"/>.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>
        /// A <see cref="Promise{T}"/> resulting in the first element in the sequence that passes the test in the specified predicate function,
        /// or a default value if <paramref name="source"/> is empty or no element passes the test in the specified predicate function.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
        public static Promise<TSource> FirstOrDefaultAsync<TSource, TCapture>(this AsyncEnumerable<TSource> source, TCapture captureValue, Func<TCapture, TSource, bool> predicate, CancelationToken cancelationToken = default)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return FirstOrDefaultAsyncCore(source.GetAsyncEnumerator(cancelationToken), DelegateWrapper.Create(captureValue, predicate));
        }

        private static async Promise<TSource> FirstOrDefaultAsyncCore<TSource, TPredicate>(AsyncEnumerator<TSource> asyncEnumerator, TPredicate predicate)
            where TPredicate : IFunc<TSource, bool>
        {
            try
            {
                while (await asyncEnumerator.MoveNextAsync())
                {
                    var item = asyncEnumerator.Current;
                    if (predicate.Invoke(item))
                    {
                        return item;
                    }
                }
                return default;
            }
            finally
            {
                await asyncEnumerator.DisposeAsync();
            }
        }

        /// <summary>
        /// Asynchronously returns the first element of an async-enumerable sequence that satisfies a specified condition, or a default value if no such element is found.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">The sequence to return the first element of.</param>
        /// <param name="predicate">An async function to test each element for a condition.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>
        /// A <see cref="Promise{T}"/> resulting in the first element in the sequence that passes the test in the specified predicate function,
        /// or a default value if <paramref name="source"/> is empty or no element passes the test in the specified predicate function.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
        public static Promise<TSource> FirstOrDefaultAsync<TSource>(this AsyncEnumerable<TSource> source, Func<TSource, Promise<bool>> predicate, CancelationToken cancelationToken = default)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return FirstOrDefaultAsyncCoreAwait(source.GetAsyncEnumerator(cancelationToken), DelegateWrapper.Create(predicate));
        }

        /// <summary>
        /// Asynchronously returns the first element of an async-enumerable sequence that satisfies a specified condition, or a default value if no such element is found.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TCapture">The type of the <paramref name="captureValue"/>.</typeparam>
        /// <param name="source">The sequence to return the first element of.</param>
        /// <param name="captureValue">The extra value that will be passed to the <paramref name="predicate"/>.</param>
        /// <param name="predicate">An async function to test each element for a condition.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>
        /// A <see cref="Promise{T}"/> resulting in the first element in the sequence that passes the test in the specified predicate function,
        /// or a default value if <paramref name="source"/> is empty or no element passes the test in the specified predicate function.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
        public static Promise<TSource> FirstOrDefaultAsync<TSource, TCapture>(this AsyncEnumerable<TSource> source, TCapture captureValue, Func<TCapture, TSource, Promise<bool>> predicate, CancelationToken cancelationToken = default)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return FirstOrDefaultAsyncCoreAwait(source.GetAsyncEnumerator(cancelationToken), DelegateWrapper.Create(captureValue, predicate));
        }

        private static async Promise<TSource> FirstOrDefaultAsyncCoreAwait<TSource, TPredicate>(AsyncEnumerator<TSource> asyncEnumerator, TPredicate predicate)
            where TPredicate : IFunc<TSource, Promise<bool>>
        {
            try
            {
                while (await asyncEnumerator.MoveNextAsync())
                {
                    var item = asyncEnumerator.Current;
                    if (await predicate.Invoke(item))
                    {
                        return item;
                    }
                }
                return default;
            }
            finally
            {
                await asyncEnumerator.DisposeAsync();
            }
        }

        /// <summary>
        /// Asynchronously returns the first element of an async-enumerable sequence that satisfies a specified condition, or a default value if no such element is found.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="configuredSource"/>.</typeparam>
        /// <param name="configuredSource">The sequence to return the first element of.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <returns>
        /// A <see cref="Promise{T}"/> resulting in the first element in the sequence that passes the test in the specified predicate function,
        /// or a default value if <paramref name="configuredSource"/> is empty or no element passes the test in the specified predicate function.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
        public static Promise<TSource> FirstOrDefaultAsync<TSource>(this in ConfiguredAsyncEnumerable<TSource> configuredSource, Func<TSource, bool> predicate)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return FirstOrDefaultAsyncCore(configuredSource.GetAsyncEnumerator(), DelegateWrapper.Create(predicate));
        }

        /// <summary>
        /// Asynchronously returns the first element of an async-enumerable sequence that satisfies a specified condition, or a default value if no such element is found.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="configuredSource"/>.</typeparam>
        /// <typeparam name="TCapture">The type of the <paramref name="captureValue"/>.</typeparam>
        /// <param name="configuredSource">The sequence to return the first element of.</param>
        /// <param name="captureValue">The extra value that will be passed to the <paramref name="predicate"/>.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <returns>
        /// A <see cref="Promise{T}"/> resulting in the first element in the sequence that passes the test in the specified predicate function,
        /// or a default value if <paramref name="configuredSource"/> is empty or no element passes the test in the specified predicate function.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
        public static Promise<TSource> FirstOrDefaultAsync<TSource, TCapture>(this in ConfiguredAsyncEnumerable<TSource> configuredSource, TCapture captureValue, Func<TCapture, TSource, bool> predicate)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return FirstOrDefaultAsyncCore(configuredSource.GetAsyncEnumerator(), DelegateWrapper.Create(captureValue, predicate));
        }

        private static async Promise<TSource> FirstOrDefaultAsyncCore<TSource, TPredicate>(ConfiguredAsyncEnumerable<TSource>.Enumerator asyncEnumerator, TPredicate predicate)
            where TPredicate : IFunc<TSource, bool>
        {
            try
            {
                while (await asyncEnumerator.MoveNextAsync())
                {
                    var item = asyncEnumerator.Current;
                    if (predicate.Invoke(item))
                    {
                        return item;
                    }
                }
                return default;
            }
            finally
            {
                await asyncEnumerator.DisposeAsync();
            }
        }

        /// <summary>
        /// Asynchronously returns the first element of an async-enumerable sequence that satisfies a specified condition, or a default value if no such element is found.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="configuredSource"/>.</typeparam>
        /// <param name="configuredSource">The sequence to return the first element of.</param>
        /// <param name="predicate">An async function to test each element for a condition.</param>
        /// <returns>
        /// A <see cref="Promise{T}"/> resulting in the first element in the sequence that passes the test in the specified predicate function,
        /// or a default value if <paramref name="configuredSource"/> is empty or no element passes the test in the specified predicate function.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
        public static Promise<TSource> FirstOrDefaultAsync<TSource>(this in ConfiguredAsyncEnumerable<TSource> configuredSource, Func<TSource, Promise<bool>> predicate)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return FirstOrDefaultAsyncCoreAwait(configuredSource.GetAsyncEnumerator(), DelegateWrapper.Create(predicate));
        }

        /// <summary>
        /// Asynchronously returns the first element of an async-enumerable sequence that satisfies a specified condition, or a default value if no such element is found.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="configuredSource"/>.</typeparam>
        /// <typeparam name="TCapture">The type of the <paramref name="captureValue"/>.</typeparam>
        /// <param name="configuredSource">The sequence to return the first element of.</param>
        /// <param name="captureValue">The extra value that will be passed to the <paramref name="predicate"/>.</param>
        /// <param name="predicate">An async function to test each element for a condition.</param>
        /// <returns>
        /// A <see cref="Promise{T}"/> resulting in the first element in the sequence that passes the test in the specified predicate function,
        /// or a default value if <paramref name="configuredSource"/> is empty or no element passes the test in the specified predicate function.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
        public static Promise<TSource> FirstOrDefaultAsync<TSource, TCapture>(this in ConfiguredAsyncEnumerable<TSource> configuredSource, TCapture captureValue, Func<TCapture, TSource, Promise<bool>> predicate)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return FirstOrDefaultAsyncCoreAwait(configuredSource.GetAsyncEnumerator(), DelegateWrapper.Create(captureValue, predicate));
        }

        private static async Promise<TSource> FirstOrDefaultAsyncCoreAwait<TSource, TPredicate>(ConfiguredAsyncEnumerable<TSource>.Enumerator asyncEnumerator, TPredicate predicate)
            where TPredicate : IFunc<TSource, Promise<bool>>
        {
            try
            {
                while (await asyncEnumerator.MoveNextAsync())
                {
                    var item = asyncEnumerator.Current;
                    if (await predicate.Invoke(item).ConfigureAwait(asyncEnumerator.ContinuationOptions))
                    {
                        return item;
                    }
                }
                return default;
            }
            finally
            {
                await asyncEnumerator.DisposeAsync();
            }
        }

        /// <summary>
        /// Asynchronously returns the first element of an async-enumerable sequence that satisfies a specified condition, or a specified default value if no such element is found.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">The sequence to return the first element of.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <param name="defaultValue">The default value to return if the sequence is empty or no element passes the test specified by <paramref name="predicate"/>.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>
        /// A <see cref="Promise{T}"/> resulting in the first element in the sequence that passes the test in the specified predicate function,
        /// or the specified default value if <paramref name="source"/> is empty or no element passes the test in the specified predicate function.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
        public static Promise<TSource> FirstOrDefaultAsync<TSource>(this AsyncEnumerable<TSource> source, Func<TSource, bool> predicate, TSource defaultValue, CancelationToken cancelationToken = default)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return FirstOrDefaultAsyncCore(source.GetAsyncEnumerator(cancelationToken), DelegateWrapper.Create(predicate), defaultValue);
        }

        /// <summary>
        /// Asynchronously returns the first element of an async-enumerable sequence that satisfies a specified condition, or a specified default value if no such element is found.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TCapture">The type of the <paramref name="captureValue"/>.</typeparam>
        /// <param name="source">The sequence to return the first element of.</param>
        /// <param name="captureValue">The extra value that will be passed to the <paramref name="predicate"/>.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <param name="defaultValue">The default value to return if the sequence is empty or no element passes the test specified by <paramref name="predicate"/>.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>
        /// A <see cref="Promise{T}"/> resulting in the first element in the sequence that passes the test in the specified predicate function,
        /// or the specified default value if <paramref name="source"/> is empty or no element passes the test in the specified predicate function.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
        public static Promise<TSource> FirstOrDefaultAsync<TSource, TCapture>(this AsyncEnumerable<TSource> source, TCapture captureValue, Func<TCapture, TSource, bool> predicate, TSource defaultValue, CancelationToken cancelationToken = default)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return FirstOrDefaultAsyncCore(source.GetAsyncEnumerator(cancelationToken), DelegateWrapper.Create(captureValue, predicate), defaultValue);
        }

        private static async Promise<TSource> FirstOrDefaultAsyncCore<TSource, TPredicate>(AsyncEnumerator<TSource> asyncEnumerator, TPredicate predicate, TSource d)
            where TPredicate : IFunc<TSource, bool>
        {
            try
            {
                while (await asyncEnumerator.MoveNextAsync())
                {
                    var item = asyncEnumerator.Current;
                    if (predicate.Invoke(item))
                    {
                        return item;
                    }
                }
                return d;
            }
            finally
            {
                await asyncEnumerator.DisposeAsync();
            }
        }

        /// <summary>
        /// Asynchronously returns the first element of an async-enumerable sequence that satisfies a specified condition, or a specified default value if no such element is found.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">The sequence to return the first element of.</param>
        /// <param name="predicate">An async function to test each element for a condition.</param>
        /// <param name="defaultValue">The default value to return if the sequence is empty or no element passes the test specified by <paramref name="predicate"/>.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>
        /// A <see cref="Promise{T}"/> resulting in the first element in the sequence that passes the test in the specified predicate function,
        /// or the specified default value if <paramref name="source"/> is empty or no element passes the test in the specified predicate function.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
        public static Promise<TSource> FirstOrDefaultAsync<TSource>(this AsyncEnumerable<TSource> source, Func<TSource, Promise<bool>> predicate, TSource defaultValue, CancelationToken cancelationToken = default)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return FirstOrDefaultAsyncCoreAwait(source.GetAsyncEnumerator(cancelationToken), DelegateWrapper.Create(predicate), defaultValue);
        }

        /// <summary>
        /// Asynchronously returns the first element of an async-enumerable sequence that satisfies a specified condition, or a specified default value if no such element is found.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TCapture">The type of the <paramref name="captureValue"/>.</typeparam>
        /// <param name="source">The sequence to return the first element of.</param>
        /// <param name="captureValue">The extra value that will be passed to the <paramref name="predicate"/>.</param>
        /// <param name="predicate">An async function to test each element for a condition.</param>
        /// <param name="defaultValue">The default value to return if the sequence is empty or no element passes the test specified by <paramref name="predicate"/>.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>
        /// A <see cref="Promise{T}"/> resulting in the first element in the sequence that passes the test in the specified predicate function,
        /// or the specified default value if <paramref name="source"/> is empty or no element passes the test in the specified predicate function.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
        public static Promise<TSource> FirstOrDefaultAsync<TSource, TCapture>(this AsyncEnumerable<TSource> source, TCapture captureValue, Func<TCapture, TSource, Promise<bool>> predicate, TSource defaultValue, CancelationToken cancelationToken = default)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return FirstOrDefaultAsyncCoreAwait(source.GetAsyncEnumerator(cancelationToken), DelegateWrapper.Create(captureValue, predicate), defaultValue);
        }

        private static async Promise<TSource> FirstOrDefaultAsyncCoreAwait<TSource, TPredicate>(AsyncEnumerator<TSource> asyncEnumerator, TPredicate predicate, TSource d)
            where TPredicate : IFunc<TSource, Promise<bool>>
        {
            try
            {
                while (await asyncEnumerator.MoveNextAsync())
                {
                    var item = asyncEnumerator.Current;
                    if (await predicate.Invoke(item))
                    {
                        return item;
                    }
                }
                return d;
            }
            finally
            {
                await asyncEnumerator.DisposeAsync();
            }
        }

        /// <summary>
        /// Asynchronously returns the first element of an async-enumerable sequence that satisfies a specified condition, or a specified default value if no such element is found.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="configuredSource"/>.</typeparam>
        /// <param name="configuredSource">The sequence to return the first element of.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <param name="defaultValue">The default value to return if the sequence is empty or no element passes the test specified by <paramref name="predicate"/>.</param>
        /// <returns>
        /// A <see cref="Promise{T}"/> resulting in the first element in the sequence that passes the test in the specified predicate function,
        /// or the specified default value if <paramref name="configuredSource"/> is empty or no element passes the test in the specified predicate function.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
        public static Promise<TSource> FirstOrDefaultAsync<TSource>(this in ConfiguredAsyncEnumerable<TSource> configuredSource, Func<TSource, bool> predicate, TSource defaultValue)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return FirstOrDefaultAsyncCore(configuredSource.GetAsyncEnumerator(), DelegateWrapper.Create(predicate), defaultValue);
        }

        /// <summary>
        /// Asynchronously returns the first element of an async-enumerable sequence that satisfies a specified condition, or a specified default value if no such element is found.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="configuredSource"/>.</typeparam>
        /// <typeparam name="TCapture">The type of the <paramref name="captureValue"/>.</typeparam>
        /// <param name="configuredSource">The sequence to return the first element of.</param>
        /// <param name="captureValue">The extra value that will be passed to the <paramref name="predicate"/>.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <param name="defaultValue">The default value to return if the sequence is empty or no element passes the test specified by <paramref name="predicate"/>.</param>
        /// <returns>
        /// A <see cref="Promise{T}"/> resulting in the first element in the sequence that passes the test in the specified predicate function,
        /// or the specified default value if <paramref name="configuredSource"/> is empty or no element passes the test in the specified predicate function.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
        public static Promise<TSource> FirstOrDefaultAsync<TSource, TCapture>(this in ConfiguredAsyncEnumerable<TSource> configuredSource, TCapture captureValue, Func<TCapture, TSource, bool> predicate, TSource defaultValue)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return FirstOrDefaultAsyncCore(configuredSource.GetAsyncEnumerator(), DelegateWrapper.Create(captureValue, predicate), defaultValue);
        }

        private static async Promise<TSource> FirstOrDefaultAsyncCore<TSource, TPredicate>(ConfiguredAsyncEnumerable<TSource>.Enumerator asyncEnumerator, TPredicate predicate, TSource d)
            where TPredicate : IFunc<TSource, bool>
        {
            try
            {
                while (await asyncEnumerator.MoveNextAsync())
                {
                    var item = asyncEnumerator.Current;
                    if (predicate.Invoke(item))
                    {
                        return item;
                    }
                }
                return d;
            }
            finally
            {
                await asyncEnumerator.DisposeAsync();
            }
        }

        /// <summary>
        /// Asynchronously returns the first element of an async-enumerable sequence that satisfies a specified condition, or a specified default value if no such element is found.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="configuredSource"/>.</typeparam>
        /// <param name="configuredSource">The sequence to return the first element of.</param>
        /// <param name="predicate">An async function to test each element for a condition.</param>
        /// <param name="defaultValue">The default value to return if the sequence is empty or no element passes the test specified by <paramref name="predicate"/>.</param>
        /// <returns>
        /// A <see cref="Promise{T}"/> resulting in the first element in the sequence that passes the test in the specified predicate function,
        /// or the specified default value if <paramref name="configuredSource"/> is empty or no element passes the test in the specified predicate function.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
        public static Promise<TSource> FirstOrDefaultAsync<TSource>(this in ConfiguredAsyncEnumerable<TSource> configuredSource, Func<TSource, Promise<bool>> predicate, TSource defaultValue)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return FirstOrDefaultAsyncCoreAwait(configuredSource.GetAsyncEnumerator(), DelegateWrapper.Create(predicate), defaultValue);
        }

        /// <summary>
        /// Asynchronously returns the first element of an async-enumerable sequence that satisfies a specified condition, or a specified default value if no such element is found.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="configuredSource"/>.</typeparam>
        /// <typeparam name="TCapture">The type of the <paramref name="captureValue"/>.</typeparam>
        /// <param name="configuredSource">The sequence to return the first element of.</param>
        /// <param name="captureValue">The extra value that will be passed to the <paramref name="predicate"/>.</param>
        /// <param name="predicate">An async function to test each element for a condition.</param>
        /// <param name="defaultValue">The default value to return if the sequence is empty or no element passes the test specified by <paramref name="predicate"/>.</param>
        /// <returns>
        /// A <see cref="Promise{T}"/> resulting in the first element in the sequence that passes the test in the specified predicate function,
        /// or the specified default value if <paramref name="configuredSource"/> is empty or no element passes the test in the specified predicate function.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
        public static Promise<TSource> FirstOrDefaultAsync<TSource, TCapture>(this in ConfiguredAsyncEnumerable<TSource> configuredSource, TCapture captureValue, Func<TCapture, TSource, Promise<bool>> predicate, TSource defaultValue)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return FirstOrDefaultAsyncCoreAwait(configuredSource.GetAsyncEnumerator(), DelegateWrapper.Create(captureValue, predicate), defaultValue);
        }

        private static async Promise<TSource> FirstOrDefaultAsyncCoreAwait<TSource, TPredicate>(ConfiguredAsyncEnumerable<TSource>.Enumerator asyncEnumerator, TPredicate predicate, TSource d)
            where TPredicate : IFunc<TSource, Promise<bool>>
        {
            try
            {
                while (await asyncEnumerator.MoveNextAsync())
                {
                    var item = asyncEnumerator.Current;
                    if (await predicate.Invoke(item).ConfigureAwait(asyncEnumerator.ContinuationOptions))
                    {
                        return item;
                    }
                }
                return d;
            }
            finally
            {
                await asyncEnumerator.DisposeAsync();
            }
        }
    }
}