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
        /// Bypasses a specified number of elements in an async-enumerable sequence as long as a specified condition is true and then returns the remaining elements.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the <paramref name="source"/> sequence.</typeparam>
        /// <param name="source">The async-enumerable sequence to take elements from.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <returns>An <see cref="AsyncEnumerable{T}"/> that contains the elements from the input sequence starting at the first element in the linear series that does not pass the test specified by <paramref name="predicate"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
        public static AsyncEnumerable<TSource> SkipWhile<TSource>(this AsyncEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return Internal.SkipWhileHelper.SkipWhile(source.GetAsyncEnumerator(), DelegateWrapper.Create(predicate));
        }

        /// <summary>
        /// Bypasses a specified number of elements in an async-enumerable sequence as long as a specified condition is true and then returns the remaining elements.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the <paramref name="source"/> sequence.</typeparam>
        /// <typeparam name="TCapture">The type of the captured value that will be passed to <paramref name="predicate"/>.</typeparam>
        /// <param name="source">The async-enumerable sequence to take elements from.</param>
        /// <param name="captureValue">The extra value that will be passed to <paramref name="predicate"/>.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <returns>An <see cref="AsyncEnumerable{T}"/> that contains the elements from the input sequence starting at the first element in the linear series that does not pass the test specified by <paramref name="predicate"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
        public static AsyncEnumerable<TSource> SkipWhile<TSource, TCapture>(this AsyncEnumerable<TSource> source, TCapture captureValue, Func<TCapture, TSource, bool> predicate)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return Internal.SkipWhileHelper.SkipWhile(source.GetAsyncEnumerator(), DelegateWrapper.Create(captureValue, predicate));
        }

        /// <summary>
        /// Bypasses a specified number of elements in an async-enumerable sequence as long as a specified condition is true and then returns the remaining elements.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the <paramref name="source"/> sequence.</typeparam>
        /// <param name="source">The async-enumerable sequence to take elements from.</param>
        /// <param name="predicate">An async function to test each element for a condition.</param>
        /// <returns>An <see cref="AsyncEnumerable{T}"/> that contains the elements from the input sequence starting at the first element in the linear series that does not pass the test specified by <paramref name="predicate"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
        public static AsyncEnumerable<TSource> SkipWhile<TSource>(this AsyncEnumerable<TSource> source, Func<TSource, Promise<bool>> predicate)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return Internal.SkipWhileHelper.SkipWhile(source.GetAsyncEnumerator(), DelegateWrapper.Create(predicate));
        }

        /// <summary>
        /// Bypasses a specified number of elements in an async-enumerable sequence as long as a specified condition is true and then returns the remaining elements.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the <paramref name="source"/> sequence.</typeparam>
        /// <typeparam name="TCapture">The type of the captured value that will be passed to <paramref name="predicate"/>.</typeparam>
        /// <param name="source">The async-enumerable sequence to take elements from.</param>
        /// <param name="captureValue">The extra value that will be passed to <paramref name="predicate"/>.</param>
        /// <param name="predicate">An async function to test each element for a condition.</param>
        /// <returns>An <see cref="AsyncEnumerable{T}"/> that contains the elements from the input sequence starting at the first element in the linear series that does not pass the test specified by <paramref name="predicate"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
        public static AsyncEnumerable<TSource> SkipWhile<TSource, TCapture>(this AsyncEnumerable<TSource> source, TCapture captureValue, Func<TCapture, TSource, Promise<bool>> predicate)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return Internal.SkipWhileHelper.SkipWhile(source.GetAsyncEnumerator(), DelegateWrapper.Create(captureValue, predicate));
        }

        /// <summary>
        /// Bypasses a specified number of elements in an async-enumerable sequence as long as a specified condition is true and then returns the remaining elements.
        /// The element's index is used in the logic of the predicate function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the <paramref name="source"/> sequence.</typeparam>
        /// <param name="source">The async-enumerable sequence to take elements from.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <returns>An <see cref="AsyncEnumerable{T}"/> that contains the elements from the input sequence starting at the first element in the linear series that does not pass the test specified by <paramref name="predicate"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
        public static AsyncEnumerable<TSource> SkipWhile<TSource>(this AsyncEnumerable<TSource> source, Func<TSource, int, bool> predicate)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return Internal.SkipWhileHelper.SkipWhileWithIndex(source.GetAsyncEnumerator(), DelegateWrapper.Create(predicate));
        }

        /// <summary>
        /// Bypasses a specified number of elements in an async-enumerable sequence as long as a specified condition is true and then returns the remaining elements.
        /// The element's index is used in the logic of the predicate function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the <paramref name="source"/> sequence.</typeparam>
        /// <typeparam name="TCapture">The type of the captured value that will be passed to <paramref name="predicate"/>.</typeparam>
        /// <param name="source">The async-enumerable sequence to take elements from.</param>
        /// <param name="captureValue">The extra value that will be passed to <paramref name="predicate"/>.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <returns>An <see cref="AsyncEnumerable{T}"/> that contains the elements from the input sequence starting at the first element in the linear series that does not pass the test specified by <paramref name="predicate"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
        public static AsyncEnumerable<TSource> SkipWhile<TSource, TCapture>(this AsyncEnumerable<TSource> source, TCapture captureValue, Func<TCapture, TSource, int, bool> predicate)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return Internal.SkipWhileHelper.SkipWhileWithIndex(source.GetAsyncEnumerator(), DelegateWrapper.Create(captureValue, predicate));
        }

        /// <summary>
        /// Bypasses a specified number of elements in an async-enumerable sequence as long as a specified condition is true and then returns the remaining elements.
        /// The element's index is used in the logic of the predicate function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the <paramref name="source"/> sequence.</typeparam>
        /// <param name="source">The async-enumerable sequence to take elements from.</param>
        /// <param name="predicate">An async function to test each element for a condition.</param>
        /// <returns>An <see cref="AsyncEnumerable{T}"/> that contains the elements from the input sequence starting at the first element in the linear series that does not pass the test specified by <paramref name="predicate"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
        public static AsyncEnumerable<TSource> SkipWhile<TSource>(this AsyncEnumerable<TSource> source, Func<TSource, int, Promise<bool>> predicate)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return Internal.SkipWhileHelper.SkipWhileWithIndex(source.GetAsyncEnumerator(), DelegateWrapper.Create(predicate));
        }

        /// <summary>
        /// Bypasses a specified number of elements in an async-enumerable sequence as long as a specified condition is true and then returns the remaining elements.
        /// The element's index is used in the logic of the predicate function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the <paramref name="source"/> sequence.</typeparam>
        /// <typeparam name="TCapture">The type of the captured value that will be passed to <paramref name="predicate"/>.</typeparam>
        /// <param name="source">The async-enumerable sequence to take elements from.</param>
        /// <param name="captureValue">The extra value that will be passed to <paramref name="predicate"/>.</param>
        /// <param name="predicate">An async function to test each element for a condition.</param>
        /// <returns>An <see cref="AsyncEnumerable{T}"/> that contains the elements from the input sequence starting at the first element in the linear series that does not pass the test specified by <paramref name="predicate"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
        public static AsyncEnumerable<TSource> SkipWhile<TSource, TCapture>(this AsyncEnumerable<TSource> source, TCapture captureValue, Func<TCapture, TSource, int, Promise<bool>> predicate)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return Internal.SkipWhileHelper.SkipWhileWithIndex(source.GetAsyncEnumerator(), DelegateWrapper.Create(captureValue, predicate));
        }

        /// <summary>
        /// Bypasses a specified number of elements in an async-enumerable sequence as long as a specified condition is true and then returns the remaining elements.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the <paramref name="configuredSource"/> sequence.</typeparam>
        /// <param name="configuredSource">The configured async-enumerable sequence to take elements from.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <returns>An <see cref="AsyncEnumerable{T}"/> that contains the elements from the input sequence starting at the first element in the linear series that does not pass the test specified by <paramref name="predicate"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
        public static AsyncEnumerable<TSource> SkipWhile<TSource>(this in ConfiguredAsyncEnumerable<TSource> configuredSource, Func<TSource, bool> predicate)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return Internal.SkipWhileHelper.SkipWhile(configuredSource.GetAsyncEnumerator(), DelegateWrapper.Create(predicate));
        }

        /// <summary>
        /// Bypasses a specified number of elements in an async-enumerable sequence as long as a specified condition is true and then returns the remaining elements.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the <paramref name="configuredSource"/> sequence.</typeparam>
        /// <typeparam name="TCapture">The type of the captured value that will be passed to <paramref name="predicate"/>.</typeparam>
        /// <param name="configuredSource">The configured async-enumerable sequence to take elements from.</param>
        /// <param name="captureValue">The extra value that will be passed to <paramref name="predicate"/>.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <returns>An <see cref="AsyncEnumerable{T}"/> that contains the elements from the input sequence starting at the first element in the linear series that does not pass the test specified by <paramref name="predicate"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
        public static AsyncEnumerable<TSource> SkipWhile<TSource, TCapture>(this in ConfiguredAsyncEnumerable<TSource> configuredSource, TCapture captureValue, Func<TCapture, TSource, bool> predicate)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return Internal.SkipWhileHelper.SkipWhile(configuredSource.GetAsyncEnumerator(), DelegateWrapper.Create(captureValue, predicate));
        }

        /// <summary>
        /// Bypasses a specified number of elements in an async-enumerable sequence as long as a specified condition is true and then returns the remaining elements.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the <paramref name="configuredSource"/> sequence.</typeparam>
        /// <param name="configuredSource">The configured async-enumerable sequence to take elements from.</param>
        /// <param name="predicate">An async function to test each element for a condition.</param>
        /// <returns>An <see cref="AsyncEnumerable{T}"/> that contains the elements from the input sequence starting at the first element in the linear series that does not pass the test specified by <paramref name="predicate"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
        public static AsyncEnumerable<TSource> SkipWhile<TSource>(this in ConfiguredAsyncEnumerable<TSource> configuredSource, Func<TSource, Promise<bool>> predicate)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return Internal.SkipWhileHelper.SkipWhile(configuredSource.GetAsyncEnumerator(), DelegateWrapper.Create(predicate));
        }

        /// <summary>
        /// Bypasses a specified number of elements in an async-enumerable sequence as long as a specified condition is true and then returns the remaining elements.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the <paramref name="configuredSource"/> sequence.</typeparam>
        /// <typeparam name="TCapture">The type of the captured value that will be passed to <paramref name="predicate"/>.</typeparam>
        /// <param name="configuredSource">The configured async-enumerable sequence to take elements from.</param>
        /// <param name="captureValue">The extra value that will be passed to <paramref name="predicate"/>.</param>
        /// <param name="predicate">An async function to test each element for a condition.</param>
        /// <returns>An <see cref="AsyncEnumerable{T}"/> that contains the elements from the input sequence starting at the first element in the linear series that does not pass the test specified by <paramref name="predicate"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
        public static AsyncEnumerable<TSource> SkipWhile<TSource, TCapture>(this in ConfiguredAsyncEnumerable<TSource> configuredSource, TCapture captureValue, Func<TCapture, TSource, Promise<bool>> predicate)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return Internal.SkipWhileHelper.SkipWhile(configuredSource.GetAsyncEnumerator(), DelegateWrapper.Create(captureValue, predicate));
        }

        /// <summary>
        /// Bypasses a specified number of elements in an async-enumerable sequence as long as a specified condition is true and then returns the remaining elements.
        /// The element's index is used in the logic of the predicate function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the <paramref name="configuredSource"/> sequence.</typeparam>
        /// <param name="configuredSource">The configured async-enumerable sequence to take elements from.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <returns>An <see cref="AsyncEnumerable{T}"/> that contains the elements from the input sequence starting at the first element in the linear series that does not pass the test specified by <paramref name="predicate"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
        public static AsyncEnumerable<TSource> SkipWhile<TSource>(this in ConfiguredAsyncEnumerable<TSource> configuredSource, Func<TSource, int, bool> predicate)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return Internal.SkipWhileHelper.SkipWhileWithIndex(configuredSource.GetAsyncEnumerator(), DelegateWrapper.Create(predicate));
        }

        /// <summary>
        /// Bypasses a specified number of elements in an async-enumerable sequence as long as a specified condition is true and then returns the remaining elements.
        /// The element's index is used in the logic of the predicate function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the <paramref name="configuredSource"/> sequence.</typeparam>
        /// <typeparam name="TCapture">The type of the captured value that will be passed to <paramref name="predicate"/>.</typeparam>
        /// <param name="configuredSource">The configured async-enumerable sequence to take elements from.</param>
        /// <param name="captureValue">The extra value that will be passed to <paramref name="predicate"/>.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <returns>An <see cref="AsyncEnumerable{T}"/> that contains the elements from the input sequence starting at the first element in the linear series that does not pass the test specified by <paramref name="predicate"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
        public static AsyncEnumerable<TSource> SkipWhile<TSource, TCapture>(this in ConfiguredAsyncEnumerable<TSource> configuredSource, TCapture captureValue, Func<TCapture, TSource, int, bool> predicate)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return Internal.SkipWhileHelper.SkipWhileWithIndex(configuredSource.GetAsyncEnumerator(), DelegateWrapper.Create(captureValue, predicate));
        }

        /// <summary>
        /// Bypasses a specified number of elements in an async-enumerable sequence as long as a specified condition is true and then returns the remaining elements.
        /// The element's index is used in the logic of the predicate function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the <paramref name="configuredSource"/> sequence.</typeparam>
        /// <param name="configuredSource">The configured async-enumerable sequence to take elements from.</param>
        /// <param name="predicate">An async function to test each element for a condition.</param>
        /// <returns>An <see cref="AsyncEnumerable{T}"/> that contains the elements from the input sequence starting at the first element in the linear series that does not pass the test specified by <paramref name="predicate"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
        public static AsyncEnumerable<TSource> SkipWhile<TSource>(this in ConfiguredAsyncEnumerable<TSource> configuredSource, Func<TSource, int, Promise<bool>> predicate)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return Internal.SkipWhileHelper.SkipWhileWithIndex(configuredSource.GetAsyncEnumerator(), DelegateWrapper.Create(predicate));
        }

        /// <summary>
        /// Bypasses a specified number of elements in an async-enumerable sequence as long as a specified condition is true and then returns the remaining elements.
        /// The element's index is used in the logic of the predicate function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the <paramref name="configuredSource"/> sequence.</typeparam>
        /// <typeparam name="TCapture">The type of the captured value that will be passed to <paramref name="predicate"/>.</typeparam>
        /// <param name="configuredSource">The configured async-enumerable sequence to take elements from.</param>
        /// <param name="captureValue">The extra value that will be passed to <paramref name="predicate"/>.</param>
        /// <param name="predicate">An async function to test each element for a condition.</param>
        /// <returns>An <see cref="AsyncEnumerable{T}"/> that contains the elements from the input sequence starting at the first element in the linear series that does not pass the test specified by <paramref name="predicate"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
        public static AsyncEnumerable<TSource> SkipWhile<TSource, TCapture>(this in ConfiguredAsyncEnumerable<TSource> configuredSource, TCapture captureValue, Func<TCapture, TSource, int, Promise<bool>> predicate)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return Internal.SkipWhileHelper.SkipWhileWithIndex(configuredSource.GetAsyncEnumerator(), DelegateWrapper.Create(captureValue, predicate));
        }
    }
}