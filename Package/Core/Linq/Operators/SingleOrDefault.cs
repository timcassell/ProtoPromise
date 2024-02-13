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
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return SingleOrDefaultAsyncCore(source.GetAsyncEnumerator(cancelationToken), Internal.PromiseRefBase.DelegateWrapper.Create(predicate));
        }

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
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return SingleOrDefaultAsyncCore(source.GetAsyncEnumerator(cancelationToken), Internal.PromiseRefBase.DelegateWrapper.Create(captureValue, predicate));
        }

        private static async Promise<TSource> SingleOrDefaultAsyncCore<TSource, TPredicate>(AsyncEnumerator<TSource> asyncEnumerator, TPredicate predicate)
            where TPredicate : Internal.IFunc<TSource, bool>
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

            return default;
        }

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
        public static Promise<TSource> SingleOrDefaultAsync<TSource>(this AsyncEnumerable<TSource> source, Func<TSource, Promise<bool>> predicate, CancelationToken cancelationToken = default)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return SingleOrDefaultAsyncCoreAwait(source.GetAsyncEnumerator(cancelationToken), Internal.PromiseRefBase.DelegateWrapper.Create(predicate));
        }

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
        public static Promise<TSource> SingleOrDefaultAsync<TSource, TCapture>(this AsyncEnumerable<TSource> source, TCapture captureValue, Func<TCapture, TSource, Promise<bool>> predicate, CancelationToken cancelationToken = default)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return SingleOrDefaultAsyncCoreAwait(source.GetAsyncEnumerator(cancelationToken), Internal.PromiseRefBase.DelegateWrapper.Create(captureValue, predicate));
        }

        private static async Promise<TSource> SingleOrDefaultAsyncCoreAwait<TSource, TPredicate>(AsyncEnumerator<TSource> asyncEnumerator, TPredicate predicate)
            where TPredicate : Internal.IFunc<TSource, Promise<bool>>
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

            return default;
        }

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
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return SingleOrDefaultAsyncCore(configuredSource.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(predicate));
        }

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
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return SingleOrDefaultAsyncCore(configuredSource.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(captureValue, predicate));
        }

        private static async Promise<TSource> SingleOrDefaultAsyncCore<TSource, TPredicate>(ConfiguredAsyncEnumerable<TSource>.Enumerator asyncEnumerator, TPredicate predicate)
            where TPredicate : Internal.IFunc<TSource, bool>
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

            return default;
        }

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
        public static Promise<TSource> SingleOrDefaultAsync<TSource>(this in ConfiguredAsyncEnumerable<TSource> configuredSource, Func<TSource, Promise<bool>> predicate)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return SingleOrDefaultAsyncCoreAwait(configuredSource.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(predicate));
        }

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
        public static Promise<TSource> SingleOrDefaultAsync<TSource, TCapture>(this in ConfiguredAsyncEnumerable<TSource> configuredSource, TCapture captureValue, Func<TCapture, TSource, Promise<bool>> predicate)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return SingleOrDefaultAsyncCoreAwait(configuredSource.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(captureValue, predicate));
        }

        private static async Promise<TSource> SingleOrDefaultAsyncCoreAwait<TSource, TPredicate>(ConfiguredAsyncEnumerable<TSource>.Enumerator asyncEnumerator, TPredicate predicate)
            where TPredicate : Internal.IFunc<TSource, Promise<bool>>
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

            return default;
        }

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

            return SingleOrDefaultAsyncCore(source.GetAsyncEnumerator(cancelationToken), Internal.PromiseRefBase.DelegateWrapper.Create(predicate), defaultValue);
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

            return SingleOrDefaultAsyncCore(source.GetAsyncEnumerator(cancelationToken), Internal.PromiseRefBase.DelegateWrapper.Create(captureValue, predicate), defaultValue);
        }

        private static async Promise<TSource> SingleOrDefaultAsyncCore<TSource, TPredicate>(AsyncEnumerator<TSource> asyncEnumerator, TPredicate predicate, TSource d)
            where TPredicate : Internal.IFunc<TSource, bool>
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

            return d;
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
        public static Promise<TSource> SingleOrDefaultAsync<TSource>(this AsyncEnumerable<TSource> source, Func<TSource, Promise<bool>> predicate, TSource defaultValue, CancelationToken cancelationToken = default)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return SingleOrDefaultAsyncCoreAwait(source.GetAsyncEnumerator(cancelationToken), Internal.PromiseRefBase.DelegateWrapper.Create(predicate), defaultValue);
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
        public static Promise<TSource> SingleOrDefaultAsync<TSource, TCapture>(this AsyncEnumerable<TSource> source, TCapture captureValue, Func<TCapture, TSource, Promise<bool>> predicate, TSource defaultValue, CancelationToken cancelationToken = default)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return SingleOrDefaultAsyncCoreAwait(source.GetAsyncEnumerator(cancelationToken), Internal.PromiseRefBase.DelegateWrapper.Create(captureValue, predicate), defaultValue);
        }

        private static async Promise<TSource> SingleOrDefaultAsyncCoreAwait<TSource, TPredicate>(AsyncEnumerator<TSource> asyncEnumerator, TPredicate predicate, TSource d)
            where TPredicate : Internal.IFunc<TSource, Promise<bool>>
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

            return d;
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

            return SingleOrDefaultAsyncCore(configuredSource.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(predicate), defaultValue);
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

            return SingleOrDefaultAsyncCore(configuredSource.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(captureValue, predicate), defaultValue);
        }

        private static async Promise<TSource> SingleOrDefaultAsyncCore<TSource, TPredicate>(ConfiguredAsyncEnumerable<TSource>.Enumerator asyncEnumerator, TPredicate predicate, TSource d)
            where TPredicate : Internal.IFunc<TSource, bool>
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

            return d;
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
        public static Promise<TSource> SingleOrDefaultAsync<TSource>(this in ConfiguredAsyncEnumerable<TSource> configuredSource, Func<TSource, Promise<bool>> predicate, TSource defaultValue)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return SingleOrDefaultAsyncCoreAwait(configuredSource.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(predicate), defaultValue);
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
        public static Promise<TSource> SingleOrDefaultAsync<TSource, TCapture>(this in ConfiguredAsyncEnumerable<TSource> configuredSource, TCapture captureValue, Func<TCapture, TSource, Promise<bool>> predicate, TSource defaultValue)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return SingleOrDefaultAsyncCoreAwait(configuredSource.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(captureValue, predicate), defaultValue);
        }

        private static async Promise<TSource> SingleOrDefaultAsyncCoreAwait<TSource, TPredicate>(ConfiguredAsyncEnumerable<TSource>.Enumerator asyncEnumerator, TPredicate predicate, TSource d)
            where TPredicate : Internal.IFunc<TSource, Promise<bool>>
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

            return d;
        }
    }
}