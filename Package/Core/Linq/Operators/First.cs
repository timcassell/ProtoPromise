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
#if CSHARP_7_3_OR_NEWER
    partial class AsyncEnumerable
    {
        /// <summary>
        /// Asynchronously returns the first element of an async-enumerable sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">The sequence to return the first element of.</param>
        /// <returns>A <see cref="Promise{T}"/> resulting in the first element.</returns>
        /// <exception cref="InvalidOperationException"><paramref name="source"/> is empty.</exception>
        public static Promise<TSource> FirstAsync<TSource>(this AsyncEnumerable<TSource> source)
        {
            return Core(source.GetAsyncEnumerator());

            async Promise<TSource> Core(AsyncEnumerator<TSource> asyncEnumerator)
            {
                try
                {
                    if (await asyncEnumerator.MoveNextAsync())
                    {
                        return asyncEnumerator.Current;
                    }
                }
                finally
                {
                    await asyncEnumerator.DisposeAsync();
                }

                throw new InvalidOperationException("source must contain at least 1 element.", Internal.GetFormattedStacktrace(1));
            }
        }

        /// <summary>
        /// Asynchronously returns the first element of an async-enumerable sequence that satisfies a specified condition.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">The sequence to return the first element of.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <returns>A <see cref="Promise{T}"/> resulting in the first element in the sequence that passes the test in the specified predicate function.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="source"/> is empty, or no element satisfies the condition in <paramref name="predicate"/>.</exception>
        public static Promise<TSource> FirstAsync<TSource>(this AsyncEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return FirstAsyncCore(source.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(predicate));
        }

        /// <summary>
        /// Asynchronously returns the first element of an async-enumerable sequence that satisfies a specified condition.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TCapture">The type of the <paramref name="captureValue"/>.</typeparam>
        /// <param name="source">The sequence to return the first element of.</param>
        /// <param name="captureValue">The extra value that will be passed to the <paramref name="predicate"/>.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <returns>A <see cref="Promise{T}"/> resulting in the first element in the sequence that passes the test in the specified predicate function.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="source"/> is empty, or no element satisfies the condition in <paramref name="predicate"/>.</exception>
        public static Promise<TSource> FirstAsync<TSource, TCapture>(this AsyncEnumerable<TSource> source, TCapture captureValue, Func<TCapture, TSource, bool> predicate)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return FirstAsyncCore(source.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(captureValue, predicate));
        }

        private static async Promise<TSource> FirstAsyncCore<TSource, TPredicate>(AsyncEnumerator<TSource> asyncEnumerator, TPredicate predicate)
            where TPredicate : Internal.IFunc<TSource, bool>
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
            }
            finally
            {
                await asyncEnumerator.DisposeAsync();
            }

            throw new InvalidOperationException("source must contain at least 1 element.", Internal.GetFormattedStacktrace(1));
        }

        /// <summary>
        /// Asynchronously returns the first element of an async-enumerable sequence that satisfies a specified condition.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">The sequence to return the first element of.</param>
        /// <param name="predicate">An async function to test each element for a condition.</param>
        /// <returns>A <see cref="Promise{T}"/> resulting in the first element in the sequence that passes the test in the specified predicate function.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="source"/> is empty, or no element satisfies the condition in <paramref name="predicate"/>.</exception>
        public static Promise<TSource> FirstAsync<TSource>(this AsyncEnumerable<TSource> source, Func<TSource, Promise<bool>> predicate)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return FirstAsyncCoreAwait(source.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(predicate));
        }

        /// <summary>
        /// Asynchronously returns the first element of an async-enumerable sequence that satisfies a specified condition.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TCapture">The type of the <paramref name="captureValue"/>.</typeparam>
        /// <param name="source">The sequence to return the first element of.</param>
        /// <param name="captureValue">The extra value that will be passed to the <paramref name="predicate"/>.</param>
        /// <param name="predicate">An async function to test each element for a condition.</param>
        /// <returns>A <see cref="Promise{T}"/> resulting in the first element in the sequence that passes the test in the specified predicate function.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="source"/> is empty, or no element satisfies the condition in <paramref name="predicate"/>.</exception>
        public static Promise<TSource> FirstAsync<TSource, TCapture>(this AsyncEnumerable<TSource> source, TCapture captureValue, Func<TCapture, TSource, Promise<bool>> predicate)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return FirstAsyncCoreAwait(source.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(captureValue, predicate));
        }

        private static async Promise<TSource> FirstAsyncCoreAwait<TSource, TPredicate>(AsyncEnumerator<TSource> asyncEnumerator, TPredicate predicate)
            where TPredicate : Internal.IFunc<TSource, Promise<bool>>
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
            }
            finally
            {
                await asyncEnumerator.DisposeAsync();
            }

            throw new InvalidOperationException("source must contain at least 1 element.", Internal.GetFormattedStacktrace(1));
        }

        /// <summary>
        /// Asynchronously returns the first element of an async-enumerable sequence that satisfies a specified condition.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="configuredSource"/>.</typeparam>
        /// <param name="configuredSource">The sequence to return the first element of.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <returns>A <see cref="Promise{T}"/> resulting in the first element in the sequence that passes the test in the specified predicate function.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="configuredSource"/> is empty, or no element satisfies the condition in <paramref name="predicate"/>.</exception>
        public static Promise<TSource> FirstAsync<TSource>(this ConfiguredAsyncEnumerable<TSource> configuredSource, Func<TSource, bool> predicate)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return FirstAsyncCore(configuredSource.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(predicate));
        }

        /// <summary>
        /// Asynchronously returns the first element of an async-enumerable sequence that satisfies a specified condition.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="configuredSource"/>.</typeparam>
        /// <typeparam name="TCapture">The type of the <paramref name="captureValue"/>.</typeparam>
        /// <param name="configuredSource">The sequence to return the first element of.</param>
        /// <param name="captureValue">The extra value that will be passed to the <paramref name="predicate"/>.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <returns>A <see cref="Promise{T}"/> resulting in the first element in the sequence that passes the test in the specified predicate function.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="configuredSource"/> is empty, or no element satisfies the condition in <paramref name="predicate"/>.</exception>
        public static Promise<TSource> FirstAsync<TSource, TCapture>(this ConfiguredAsyncEnumerable<TSource> configuredSource, TCapture captureValue, Func<TCapture, TSource, bool> predicate)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return FirstAsyncCore(configuredSource.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(captureValue, predicate));
        }

        private static async Promise<TSource> FirstAsyncCore<TSource, TPredicate>(ConfiguredAsyncEnumerable<TSource>.Enumerator asyncEnumerator, TPredicate predicate)
            where TPredicate : Internal.IFunc<TSource, bool>
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
            }
            finally
            {
                await asyncEnumerator.DisposeAsync();
            }

            throw new InvalidOperationException("source must contain at least 1 element.", Internal.GetFormattedStacktrace(1));
        }

        /// <summary>
        /// Asynchronously returns the first element of an async-enumerable sequence that satisfies a specified condition.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="configuredSource"/>.</typeparam>
        /// <param name="configuredSource">The sequence to return the first element of.</param>
        /// <param name="predicate">An async function to test each element for a condition.</param>
        /// <returns>A <see cref="Promise{T}"/> resulting in the first element in the sequence that passes the test in the specified predicate function.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="configuredSource"/> is empty, or no element satisfies the condition in <paramref name="predicate"/>.</exception>
        public static Promise<TSource> FirstAsync<TSource>(this ConfiguredAsyncEnumerable<TSource> configuredSource, Func<TSource, Promise<bool>> predicate)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return FirstAsyncCoreAwait(configuredSource.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(predicate));
        }

        /// <summary>
        /// Asynchronously returns the first element of an async-enumerable sequence that satisfies a specified condition.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="configuredSource"/>.</typeparam>
        /// <typeparam name="TCapture">The type of the <paramref name="captureValue"/>.</typeparam>
        /// <param name="configuredSource">The sequence to return the first element of.</param>
        /// <param name="captureValue">The extra value that will be passed to the <paramref name="predicate"/>.</param>
        /// <param name="predicate">An async function to test each element for a condition.</param>
        /// <returns>A <see cref="Promise{T}"/> resulting in the first element in the sequence that passes the test in the specified predicate function.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="configuredSource"/> is empty, or no element satisfies the condition in <paramref name="predicate"/>.</exception>
        public static Promise<TSource> FirstAsync<TSource, TCapture>(this ConfiguredAsyncEnumerable<TSource> configuredSource, TCapture captureValue, Func<TCapture, TSource, Promise<bool>> predicate)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return FirstAsyncCoreAwait(configuredSource.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(captureValue, predicate));
        }

        private static async Promise<TSource> FirstAsyncCoreAwait<TSource, TPredicate>(ConfiguredAsyncEnumerable<TSource>.Enumerator asyncEnumerator, TPredicate predicate)
            where TPredicate : Internal.IFunc<TSource, Promise<bool>>
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
            }
            finally
            {
                await asyncEnumerator.DisposeAsync();
            }

            throw new InvalidOperationException("source must contain at least 1 element.", Internal.GetFormattedStacktrace(1));
        }
    }
#endif // CSHARP_7_3_OR_NEWER
}