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
        /// <summary>
        /// Filters an async-enumerable sequence of values based on a predicate.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <param name="source">A sequence of elements to filter.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <returns>An async-enumerable sequence that contains elements from the input sequence that satisfy the condition.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
        public static AsyncEnumerable<TSource> Where<TSource>(this AsyncEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return Internal.WhereHelper.Where(source.GetAsyncEnumerator(), DelegateWrapper.Create(predicate));
        }

        /// <summary>
        /// Filters an async-enumerable sequence of values based on a predicate.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCapture">The type of the captured value that will be passed to <paramref name="predicate"/>.</typeparam>
        /// <param name="source">A sequence of elements to filter.</param>
        /// <param name="captureValue">The extra value that will be passed to <paramref name="predicate"/>.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <returns>An async-enumerable sequence that contains elements from the input sequence that satisfy the condition.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
        public static AsyncEnumerable<TSource> Where<TSource, TCapture>(this AsyncEnumerable<TSource> source, TCapture captureValue, Func<TCapture, TSource, bool> predicate)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return Internal.WhereHelper.Where(source.GetAsyncEnumerator(), DelegateWrapper.Create(captureValue, predicate));
        }

        /// <summary>
        /// Filters an async-enumerable sequence of values based on a predicate.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <param name="source">A sequence of elements to filter.</param>
        /// <param name="predicate">An asynchronous function to test each element for a condition.</param>
        /// <returns>An async-enumerable sequence that contains elements from the input sequence that satisfy the condition.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
        public static AsyncEnumerable<TSource> Where<TSource>(this AsyncEnumerable<TSource> source, Func<TSource, Promise<bool>> predicate)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return Internal.WhereHelper.Where(source.GetAsyncEnumerator(), DelegateWrapper.Create(predicate));
        }

        /// <summary>
        /// Filters an async-enumerable sequence of values based on a predicate.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCapture">The type of the captured value that will be passed to <paramref name="predicate"/>.</typeparam>
        /// <param name="source">A sequence of elements to filter.</param>
        /// <param name="captureValue">The extra value that will be passed to <paramref name="predicate"/>.</param>
        /// <param name="predicate">An asynchronous function to test each element for a condition.</param>
        /// <returns>An async-enumerable sequence that contains elements from the input sequence that satisfy the condition.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
        public static AsyncEnumerable<TSource> Where<TSource, TCapture>(this AsyncEnumerable<TSource> source, TCapture captureValue, Func<TCapture, TSource, Promise<bool>> predicate)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return Internal.WhereHelper.Where(source.GetAsyncEnumerator(), DelegateWrapper.Create(captureValue, predicate));
        }

        /// <summary>
        /// Filters a configured async-enumerable sequence of values based on a predicate.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <param name="configuredSource">A configured sequence of elements to filter.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <returns>An async-enumerable sequence that contains elements from the input sequence that satisfy the condition.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
        public static AsyncEnumerable<TSource> Where<TSource>(this in ConfiguredAsyncEnumerable<TSource> configuredSource, Func<TSource, bool> predicate)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return Internal.WhereHelper.Where(configuredSource.GetAsyncEnumerator(), DelegateWrapper.Create(predicate));
        }

        /// <summary>
        /// Filters a configured async-enumerable sequence of values based on a predicate.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCapture">The type of the captured value that will be passed to <paramref name="predicate"/>.</typeparam>
        /// <param name="configuredSource">A configured sequence of elements to filter.</param>
        /// <param name="captureValue">The extra value that will be passed to <paramref name="predicate"/>.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <returns>An async-enumerable sequence that contains elements from the input sequence that satisfy the condition.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
        public static AsyncEnumerable<TSource> Where<TSource, TCapture>(this in ConfiguredAsyncEnumerable<TSource> configuredSource, TCapture captureValue, Func<TCapture, TSource, bool> predicate)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return Internal.WhereHelper.Where(configuredSource.GetAsyncEnumerator(), DelegateWrapper.Create(captureValue, predicate));
        }

        /// <summary>
        /// Filters a configured async-enumerable sequence of values based on a predicate.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <param name="configuredSource">A configured sequence of elements to filter.</param>
        /// <param name="predicate">An asynchronous function to test each element for a condition.</param>
        /// <returns>An async-enumerable sequence that contains elements from the input sequence that satisfy the condition.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
        public static AsyncEnumerable<TSource> Where<TSource>(this in ConfiguredAsyncEnumerable<TSource> configuredSource, Func<TSource, Promise<bool>> predicate)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return Internal.WhereHelper.Where(configuredSource.GetAsyncEnumerator(), DelegateWrapper.Create(predicate));
        }

        /// <summary>
        /// Filters a configured async-enumerable sequence of values based on a predicate.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCapture">The type of the captured value that will be passed to <paramref name="predicate"/>.</typeparam>
        /// <param name="configuredSource">A configured sequence of elements to filter.</param>
        /// <param name="captureValue">The extra value that will be passed to <paramref name="predicate"/>.</param>
        /// <param name="predicate">An asynchronous function to test each element for a condition.</param>
        /// <returns>An async-enumerable sequence that contains elements from the input sequence that satisfy the condition.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
        public static AsyncEnumerable<TSource> Where<TSource, TCapture>(this in ConfiguredAsyncEnumerable<TSource> configuredSource, TCapture captureValue, Func<TCapture, TSource, Promise<bool>> predicate)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return Internal.WhereHelper.Where(configuredSource.GetAsyncEnumerator(), DelegateWrapper.Create(captureValue, predicate));
        }

        /// <summary>
        /// Filters an async-enumerable sequence of values based on a predicate. Each element's index is used in the logic of the predicate function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <param name="source">A sequence of elements to filter.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <returns>An async-enumerable sequence that contains elements from the input sequence that satisfy the condition.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
        public static AsyncEnumerable<TSource> Where<TSource>(this AsyncEnumerable<TSource> source, Func<TSource, int, bool> predicate)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return Internal.WhereHelper.WhereWithIndex(source.GetAsyncEnumerator(), DelegateWrapper.Create(predicate));
        }

        /// <summary>
        /// Filters an async-enumerable sequence of values based on a predicate. Each element's index is used in the logic of the predicate function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCapture">The type of the captured value that will be passed to <paramref name="predicate"/>.</typeparam>
        /// <param name="source">A sequence of elements to filter.</param>
        /// <param name="captureValue">The extra value that will be passed to <paramref name="predicate"/>.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <returns>An async-enumerable sequence that contains elements from the input sequence that satisfy the condition.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
        public static AsyncEnumerable<TSource> Where<TSource, TCapture>(this AsyncEnumerable<TSource> source, TCapture captureValue, Func<TCapture, TSource, int, bool> predicate)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return Internal.WhereHelper.WhereWithIndex(source.GetAsyncEnumerator(), DelegateWrapper.Create(captureValue, predicate));
        }

        /// <summary>
        /// Filters an async-enumerable sequence of values based on a predicate. Each element's index is used in the logic of the predicate function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <param name="source">A sequence of elements to filter.</param>
        /// <param name="predicate">An asynchronous function to test each element for a condition.</param>
        /// <returns>An async-enumerable sequence that contains elements from the input sequence that satisfy the condition.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
        public static AsyncEnumerable<TSource> Where<TSource>(this AsyncEnumerable<TSource> source, Func<TSource, int, Promise<bool>> predicate)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return Internal.WhereHelper.WhereWithIndex(source.GetAsyncEnumerator(), DelegateWrapper.Create(predicate));
        }

        /// <summary>
        /// Filters an async-enumerable sequence of values based on a predicate. Each element's index is used in the logic of the predicate function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCapture">The type of the captured value that will be passed to <paramref name="predicate"/>.</typeparam>
        /// <param name="source">A sequence of elements to filter.</param>
        /// <param name="captureValue">The extra value that will be passed to <paramref name="predicate"/>.</param>
        /// <param name="predicate">An asynchronous function to test each element for a condition.</param>
        /// <returns>An async-enumerable sequence that contains elements from the input sequence that satisfy the condition.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
        public static AsyncEnumerable<TSource> Where<TSource, TCapture>(this AsyncEnumerable<TSource> source, TCapture captureValue, Func<TCapture, TSource, int, Promise<bool>> predicate)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return Internal.WhereHelper.WhereWithIndex(source.GetAsyncEnumerator(), DelegateWrapper.Create(captureValue, predicate));
        }

        /// <summary>
        /// Filters a configured async-enumerable sequence of values based on a predicate. Each element's index is used in the logic of the predicate function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <param name="configuredSource">A configured sequence of elements to filter.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <returns>An async-enumerable sequence that contains elements from the input sequence that satisfy the condition.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
        public static AsyncEnumerable<TSource> Where<TSource>(this in ConfiguredAsyncEnumerable<TSource> configuredSource, Func<TSource, int, bool> predicate)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return Internal.WhereHelper.WhereWithIndex(configuredSource.GetAsyncEnumerator(), DelegateWrapper.Create(predicate));
        }

        /// <summary>
        /// Filters a configured async-enumerable sequence of values based on a predicate. Each element's index is used in the logic of the predicate function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCapture">The type of the captured value that will be passed to <paramref name="predicate"/>.</typeparam>
        /// <param name="configuredSource">A configured sequence of elements to filter.</param>
        /// <param name="captureValue">The extra value that will be passed to <paramref name="predicate"/>.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <returns>An async-enumerable sequence that contains elements from the input sequence that satisfy the condition.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
        public static AsyncEnumerable<TSource> Where<TSource, TCapture>(this in ConfiguredAsyncEnumerable<TSource> configuredSource, TCapture captureValue, Func<TCapture, TSource, int, bool> predicate)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return Internal.WhereHelper.WhereWithIndex(configuredSource.GetAsyncEnumerator(), DelegateWrapper.Create(captureValue, predicate));
        }

        /// <summary>
        /// Filters a configured async-enumerable sequence of values based on a predicate. Each element's index is used in the logic of the predicate function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <param name="configuredSource">A configured sequence of elements to filter.</param>
        /// <param name="predicate">An asynchronous function to test each element for a condition.</param>
        /// <returns>An async-enumerable sequence that contains elements from the input sequence that satisfy the condition.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
        public static AsyncEnumerable<TSource> Where<TSource>(this in ConfiguredAsyncEnumerable<TSource> configuredSource, Func<TSource, int, Promise<bool>> predicate)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return Internal.WhereHelper.WhereWithIndex(configuredSource.GetAsyncEnumerator(), DelegateWrapper.Create(predicate));
        }

        /// <summary>
        /// Filters a configured async-enumerable sequence of values based on a predicate. Each element's index is used in the logic of the predicate function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCapture">The type of the captured value that will be passed to <paramref name="predicate"/>.</typeparam>
        /// <param name="configuredSource">A configured sequence of elements to filter.</param>
        /// <param name="captureValue">The extra value that will be passed to <paramref name="predicate"/>.</param>
        /// <param name="predicate">An asynchronous function to test each element for a condition.</param>
        /// <returns>An async-enumerable sequence that contains elements from the input sequence that satisfy the condition.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
        public static AsyncEnumerable<TSource> Where<TSource, TCapture>(this in ConfiguredAsyncEnumerable<TSource> configuredSource, TCapture captureValue, Func<TCapture, TSource, int, Promise<bool>> predicate)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return Internal.WhereHelper.WhereWithIndex(configuredSource.GetAsyncEnumerator(), DelegateWrapper.Create(captureValue, predicate));
        }
    }
}