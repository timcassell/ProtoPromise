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
        /// Bypasses a specified number of elements in an async-enumerable sequence as long as a specified condition is true.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the <paramref name="source"/> sequence.</typeparam>
        /// <param name="source">The async-enumerable sequence to take elements from.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <returns>An <see cref="AsyncEnumerable{T}"/> that contains the elements from the input sequence that occur before the element at which the test no longer passes.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
        public static AsyncEnumerable<TSource> TakeWhile<TSource>(this AsyncEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return Internal.TakeWhileHelper.TakeWhile(source.GetAsyncEnumerator(), DelegateWrapper.Create(predicate));
        }

        /// <summary>
        /// Bypasses a specified number of elements in an async-enumerable sequence as long as a specified condition is true.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the <paramref name="source"/> sequence.</typeparam>
        /// <typeparam name="TCapture">The type of the captured value that will be passed to <paramref name="predicate"/>.</typeparam>
        /// <param name="source">The async-enumerable sequence to take elements from.</param>
        /// <param name="captureValue">The extra value that will be passed to <paramref name="predicate"/>.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <returns>An <see cref="AsyncEnumerable{T}"/> that contains the elements from the input sequence that occur before the element at which the test no longer passes.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
        public static AsyncEnumerable<TSource> TakeWhile<TSource, TCapture>(this AsyncEnumerable<TSource> source, TCapture captureValue, Func<TCapture, TSource, bool> predicate)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return Internal.TakeWhileHelper.TakeWhile(source.GetAsyncEnumerator(), DelegateWrapper.Create(captureValue, predicate));
        }

        /// <summary>
        /// Bypasses a specified number of elements in an async-enumerable sequence as long as a specified condition is true.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the <paramref name="source"/> sequence.</typeparam>
        /// <param name="source">The async-enumerable sequence to take elements from.</param>
        /// <param name="predicate">An async function to test each element for a condition.</param>
        /// <returns>An <see cref="AsyncEnumerable{T}"/> that contains the elements from the input sequence that occur before the element at which the test no longer passes.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
        public static AsyncEnumerable<TSource> TakeWhile<TSource>(this AsyncEnumerable<TSource> source, Func<TSource, CancelationToken, Promise<bool>> predicate)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return Internal.TakeWhileHelper.TakeWhile(source.GetAsyncEnumerator(), DelegateWrapper.Create(predicate));
        }

        /// <summary>
        /// Bypasses a specified number of elements in an async-enumerable sequence as long as a specified condition is true.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the <paramref name="source"/> sequence.</typeparam>
        /// <typeparam name="TCapture">The type of the captured value that will be passed to <paramref name="predicate"/>.</typeparam>
        /// <param name="source">The async-enumerable sequence to take elements from.</param>
        /// <param name="captureValue">The extra value that will be passed to <paramref name="predicate"/>.</param>
        /// <param name="predicate">An async function to test each element for a condition.</param>
        /// <returns>An <see cref="AsyncEnumerable{T}"/> that contains the elements from the input sequence that occur before the element at which the test no longer passes.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
        public static AsyncEnumerable<TSource> TakeWhile<TSource, TCapture>(this AsyncEnumerable<TSource> source, TCapture captureValue, Func<TCapture, TSource, CancelationToken, Promise<bool>> predicate)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return Internal.TakeWhileHelper.TakeWhile(source.GetAsyncEnumerator(), DelegateWrapper.Create(captureValue, predicate));
        }

        /// <summary>
        /// Bypasses a specified number of elements in an async-enumerable sequence as long as a specified condition is true.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the <paramref name="configuredSource"/> sequence.</typeparam>
        /// <param name="configuredSource">The configured async-enumerable sequence to take elements from.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <returns>An <see cref="AsyncEnumerable{T}"/> that contains the elements from the input sequence that occur before the element at which the test no longer passes.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
        public static AsyncEnumerable<TSource> TakeWhile<TSource>(this in ConfiguredAsyncEnumerable<TSource> configuredSource, Func<TSource, bool> predicate)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return Internal.TakeWhileHelper.TakeWhile(configuredSource.GetAsyncEnumerator(), DelegateWrapper.Create(predicate));
        }

        /// <summary>
        /// Bypasses a specified number of elements in an async-enumerable sequence as long as a specified condition is true.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the <paramref name="configuredSource"/> sequence.</typeparam>
        /// <typeparam name="TCapture">The type of the captured value that will be passed to <paramref name="predicate"/>.</typeparam>
        /// <param name="configuredSource">The configured async-enumerable sequence to take elements from.</param>
        /// <param name="captureValue">The extra value that will be passed to <paramref name="predicate"/>.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <returns>An <see cref="AsyncEnumerable{T}"/> that contains the elements from the input sequence that occur before the element at which the test no longer passes.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
        public static AsyncEnumerable<TSource> TakeWhile<TSource, TCapture>(this in ConfiguredAsyncEnumerable<TSource> configuredSource, TCapture captureValue, Func<TCapture, TSource, bool> predicate)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return Internal.TakeWhileHelper.TakeWhile(configuredSource.GetAsyncEnumerator(), DelegateWrapper.Create(captureValue, predicate));
        }

        /// <summary>
        /// Bypasses a specified number of elements in an async-enumerable sequence as long as a specified condition is true.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the <paramref name="configuredSource"/> sequence.</typeparam>
        /// <param name="configuredSource">The configured async-enumerable sequence to take elements from.</param>
        /// <param name="predicate">An async function to test each element for a condition.</param>
        /// <returns>An <see cref="AsyncEnumerable{T}"/> that contains the elements from the input sequence that occur before the element at which the test no longer passes.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
        public static AsyncEnumerable<TSource> TakeWhile<TSource>(this in ConfiguredAsyncEnumerable<TSource> configuredSource, Func<TSource, CancelationToken, Promise<bool>> predicate)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return Internal.TakeWhileHelper.TakeWhile(configuredSource.GetAsyncEnumerator(), DelegateWrapper.Create(predicate));
        }

        /// <summary>
        /// Bypasses a specified number of elements in an async-enumerable sequence as long as a specified condition is true.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the <paramref name="configuredSource"/> sequence.</typeparam>
        /// <typeparam name="TCapture">The type of the captured value that will be passed to <paramref name="predicate"/>.</typeparam>
        /// <param name="configuredSource">The configured async-enumerable sequence to take elements from.</param>
        /// <param name="captureValue">The extra value that will be passed to <paramref name="predicate"/>.</param>
        /// <param name="predicate">An async function to test each element for a condition.</param>
        /// <returns>An <see cref="AsyncEnumerable{T}"/> that contains the elements from the input sequence that occur before the element at which the test no longer passes.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
        public static AsyncEnumerable<TSource> TakeWhile<TSource, TCapture>(this in ConfiguredAsyncEnumerable<TSource> configuredSource, TCapture captureValue, Func<TCapture, TSource, CancelationToken, Promise<bool>> predicate)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return Internal.TakeWhileHelper.TakeWhile(configuredSource.GetAsyncEnumerator(), DelegateWrapper.Create(captureValue, predicate));
        }
    }
}