#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using Proto.Promises.Async.CompilerServices;
using System;

namespace Proto.Promises.Linq
{
#if CSHARP_7_3_OR_NEWER
    partial class AsyncEnumerable
    {
        /// <summary>
        /// Projects each element of an async-enumerable sequence into a new form.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TResult">The type of the elements in the result sequence, obtained by running the selector function for each element in the source sequence.</typeparam>
        /// <param name="source">A sequence of elements to invoke a transform function on.</param>
        /// <param name="selector">A transform function to apply to each source element.</param>
        /// <returns>An async-enumerable sequence whose elements are the result of invoking the transform function on each element of source.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="selector"/> is null.</exception>
        public static AsyncEnumerable<TResult> Select<TSource, TResult>(this AsyncEnumerable<TSource> source, Func<TSource, TResult> selector)
        {
            ValidateArgument(selector, nameof(selector), 1);

            return Internal.SelectHelper<TResult>.Select(source.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(selector));
        }

        /// <summary>
        /// Projects each element of an async-enumerable sequence into a new form.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TResult">The type of the elements in the result sequence, obtained by running the selector function for each element in the source sequence.</typeparam>
        /// <typeparam name="TCapture">The type of the captured value that will be passed to <paramref name="selector"/>.</typeparam>
        /// <param name="source">A sequence of elements to invoke a transform function on.</param>
        /// <param name="captureValue">The extra value that will be passed to <paramref name="selector"/>.</param>
        /// <param name="selector">A transform function to apply to each source element.</param>
        /// <returns>An async-enumerable sequence whose elements are the result of invoking the transform function on each element of source.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="selector"/> is null.</exception>
        public static AsyncEnumerable<TResult> Select<TSource, TResult, TCapture>(this AsyncEnumerable<TSource> source, TCapture captureValue, Func<TCapture, TSource, TResult> selector)
        {
            ValidateArgument(selector, nameof(selector), 1);

            return Internal.SelectHelper<TResult>.Select(source.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(captureValue, selector));
        }

        /// <summary>
        /// Projects each element of an async-enumerable sequence into a new form.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TResult">The type of the elements in the result sequence, obtained by running the selector function for each element in the source sequence.</typeparam>
        /// <param name="source">A sequence of elements to invoke a transform function on.</param>
        /// <param name="selector">An asynchronous transform function to apply to each source element.</param>
        /// <returns>An async-enumerable sequence whose elements are the result of invoking the transform function on each element of source.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="selector"/> is null.</exception>
        public static AsyncEnumerable<TResult> Select<TSource, TResult>(this AsyncEnumerable<TSource> source, Func<TSource, Promise<TResult>> selector)
        {
            ValidateArgument(selector, nameof(selector), 1);

            return Internal.SelectHelper<TResult>.SelectAwait(source.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(selector));
        }

        /// <summary>
        /// Projects each element of an async-enumerable sequence into a new form.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TResult">The type of the elements in the result sequence, obtained by running the selector function for each element in the source sequence.</typeparam>
        /// <typeparam name="TCapture">The type of the captured value that will be passed to <paramref name="selector"/>.</typeparam>
        /// <param name="source">A sequence of elements to invoke a transform function on.</param>
        /// <param name="captureValue">The extra value that will be passed to <paramref name="selector"/>.</param>
        /// <param name="selector">An asynchronous transform to apply to each source element.</param>
        /// <returns>An async-enumerable sequence whose elements are the result of invoking the transform function on each element of source.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="selector"/> is null.</exception>
        public static AsyncEnumerable<TResult> Select<TSource, TResult, TCapture>(this AsyncEnumerable<TSource> source, TCapture captureValue, Func<TCapture, TSource, Promise<TResult>> selector)
        {
            ValidateArgument(selector, nameof(selector), 1);

            return Internal.SelectHelper<TResult>.SelectAwait(source.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(captureValue, selector));
        }

        /// <summary>
        /// Projects each element of a configured async-enumerable sequence into a new form.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TResult">The type of the elements in the result sequence, obtained by running the selector function for each element in the source sequence.</typeparam>
        /// <param name="configuredSource">A configured sequence of elements to invoke a transform function on.</param>
        /// <param name="selector">A transform function to apply to each source element.</param>
        /// <returns>An async-enumerable sequence whose elements are the result of invoking the transform function on each element of source.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="selector"/> is null.</exception>
        public static AsyncEnumerable<TResult> Select<TSource, TResult>(this ConfiguredAsyncEnumerable<TSource> configuredSource, Func<TSource, TResult> selector)
        {
            ValidateArgument(selector, nameof(selector), 1);

            return Internal.SelectHelper<TResult>.Select(configuredSource.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(selector));
        }

        /// <summary>
        /// Projects each element of a configured async-enumerable sequence into a new form.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TResult">The type of the elements in the result sequence, obtained by running the selector function for each element in the source sequence.</typeparam>
        /// <typeparam name="TCapture">The type of the captured value that will be passed to <paramref name="selector"/>.</typeparam>
        /// <param name="configuredSource">A configured sequence of elements to invoke a transform function on.</param>
        /// <param name="captureValue">The extra value that will be passed to <paramref name="selector"/>.</param>
        /// <param name="selector">A transform function to apply to each source element.</param>
        /// <returns>An async-enumerable sequence whose elements are the result of invoking the transform function on each element of source.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="selector"/> is null.</exception>
        public static AsyncEnumerable<TResult> Select<TSource, TResult, TCapture>(this ConfiguredAsyncEnumerable<TSource> configuredSource, TCapture captureValue, Func<TCapture, TSource, TResult> selector)
        {
            ValidateArgument(selector, nameof(selector), 1);

            return Internal.SelectHelper<TResult>.Select(configuredSource.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(captureValue, selector));
        }

        /// <summary>
        /// Projects each element of a configured async-enumerable sequence into a new form.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TResult">The type of the elements in the result sequence, obtained by running the selector function for each element in the source sequence.</typeparam>
        /// <param name="configuredSource">A configured sequence of elements to invoke a transform function on.</param>
        /// <param name="selector">An asynchronous transform function to apply to each source element.</param>
        /// <returns>An async-enumerable sequence whose elements are the result of invoking the transform function on each element of source.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="selector"/> is null.</exception>
        public static AsyncEnumerable<TResult> Select<TSource, TResult>(this ConfiguredAsyncEnumerable<TSource> configuredSource, Func<TSource, Promise<TResult>> selector)
        {
            ValidateArgument(selector, nameof(selector), 1);

            return Internal.SelectHelper<TResult>.SelectAwait(configuredSource.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(selector));
        }

        /// <summary>
        /// Projects each element of a configured async-enumerable sequence into a new form.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TResult">The type of the elements in the result sequence, obtained by running the selector function for each element in the source sequence.</typeparam>
        /// <typeparam name="TCapture">The type of the captured value that will be passed to <paramref name="selector"/>.</typeparam>
        /// <param name="configuredSource">A configured sequence of elements to invoke a transform function on.</param>
        /// <param name="captureValue">The extra value that will be passed to <paramref name="selector"/>.</param>
        /// <param name="selector">An asynchronous transform to apply to each source element.</param>
        /// <returns>An async-enumerable sequence whose elements are the result of invoking the transform function on each element of source.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="selector"/> is null.</exception>
        public static AsyncEnumerable<TResult> Select<TSource, TResult, TCapture>(this ConfiguredAsyncEnumerable<TSource> configuredSource, TCapture captureValue, Func<TCapture, TSource, Promise<TResult>> selector)
        {
            ValidateArgument(selector, nameof(selector), 1);

            return Internal.SelectHelper<TResult>.SelectAwait(configuredSource.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(captureValue, selector));
        }

        /// <summary>
        /// Projects each element of a sequence into a new form by incorporating the element's index.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TResult">The type of the elements in the result sequence, obtained by running the selector function for each element in the source sequence.</typeparam>
        /// <param name="source">A sequence of elements to invoke a transform function on.</param>
        /// <param name="selector">A transform function to apply to each source element.</param>
        /// <returns>An async-enumerable sequence whose elements are the result of invoking the transform function on each element of source.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="selector"/> is null.</exception>
        public static AsyncEnumerable<TResult> Select<TSource, TResult>(this AsyncEnumerable<TSource> source, Func<TSource, int, TResult> selector)
        {
            ValidateArgument(selector, nameof(selector), 1);

            return Internal.SelectHelper<TResult>.SelectWithIndex(source.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(selector));
        }

        /// <summary>
        /// Projects each element of a sequence into a new form by incorporating the element's index.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TResult">The type of the elements in the result sequence, obtained by running the selector function for each element in the source sequence.</typeparam>
        /// <typeparam name="TCapture">The type of the captured value that will be passed to <paramref name="selector"/>.</typeparam>
        /// <param name="source">A sequence of elements to invoke a transform function on.</param>
        /// <param name="captureValue">The extra value that will be passed to <paramref name="selector"/>.</param>
        /// <param name="selector">A transform function to apply to each source element.</param>
        /// <returns>An async-enumerable sequence whose elements are the result of invoking the transform function on each element of source.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="selector"/> is null.</exception>
        public static AsyncEnumerable<TResult> Select<TSource, TResult, TCapture>(this AsyncEnumerable<TSource> source, TCapture captureValue, Func<TCapture, TSource, int, TResult> selector)
        {
            ValidateArgument(selector, nameof(selector), 1);

            return Internal.SelectHelper<TResult>.SelectWithIndex(source.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(captureValue, selector));
        }

        /// <summary>
        /// Projects each element of a sequence into a new form by incorporating the element's index.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TResult">The type of the elements in the result sequence, obtained by running the selector function for each element in the source sequence.</typeparam>
        /// <param name="source">A sequence of elements to invoke a transform function on.</param>
        /// <param name="selector">An asynchronous transform function to apply to each source element.</param>
        /// <returns>An async-enumerable sequence whose elements are the result of invoking the transform function on each element of source.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="selector"/> is null.</exception>
        public static AsyncEnumerable<TResult> Select<TSource, TResult>(this AsyncEnumerable<TSource> source, Func<TSource, int, Promise<TResult>> selector)
        {
            ValidateArgument(selector, nameof(selector), 1);

            return Internal.SelectHelper<TResult>.SelectWithIndexAwait(source.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(selector));
        }

        /// <summary>
        /// Projects each element of a sequence into a new form by incorporating the element's index.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TResult">The type of the elements in the result sequence, obtained by running the selector function for each element in the source sequence.</typeparam>
        /// <typeparam name="TCapture">The type of the captured value that will be passed to <paramref name="selector"/>.</typeparam>
        /// <param name="source">A sequence of elements to invoke a transform function on.</param>
        /// <param name="captureValue">The extra value that will be passed to <paramref name="selector"/>.</param>
        /// <param name="selector">An asynchronous transform to apply to each source element.</param>
        /// <returns>An async-enumerable sequence whose elements are the result of invoking the transform function on each element of source.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="selector"/> is null.</exception>
        public static AsyncEnumerable<TResult> Select<TSource, TResult, TCapture>(this AsyncEnumerable<TSource> source, TCapture captureValue, Func<TCapture, TSource, int, Promise<TResult>> selector)
        {
            ValidateArgument(selector, nameof(selector), 1);

            return Internal.SelectHelper<TResult>.SelectWithIndexAwait(source.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(captureValue, selector));
        }

        /// <summary>
        /// Projects each element of a configured async-enumerable sequence into a new form by incorporating the element's index.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TResult">The type of the elements in the result sequence, obtained by running the selector function for each element in the source sequence.</typeparam>
        /// <param name="configuredSource">A configured sequence of elements to invoke a transform function on.</param>
        /// <param name="selector">A transform function to apply to each source element.</param>
        /// <returns>An async-enumerable sequence whose elements are the result of invoking the transform function on each element of source.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="selector"/> is null.</exception>
        public static AsyncEnumerable<TResult> Select<TSource, TResult>(this ConfiguredAsyncEnumerable<TSource> configuredSource, Func<TSource, int, TResult> selector)
        {
            ValidateArgument(selector, nameof(selector), 1);

            return Internal.SelectHelper<TResult>.SelectWithIndex(configuredSource.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(selector));
        }

        /// <summary>
        /// Projects each element of a configured async-enumerable sequence into a new form by incorporating the element's index.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TResult">The type of the elements in the result sequence, obtained by running the selector function for each element in the source sequence.</typeparam>
        /// <typeparam name="TCapture">The type of the captured value that will be passed to <paramref name="selector"/>.</typeparam>
        /// <param name="configuredSource">A configured sequence of elements to invoke a transform function on.</param>
        /// <param name="captureValue">The extra value that will be passed to <paramref name="selector"/>.</param>
        /// <param name="selector">A transform function to apply to each source element.</param>
        /// <returns>An async-enumerable sequence whose elements are the result of invoking the transform function on each element of source.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="selector"/> is null.</exception>
        public static AsyncEnumerable<TResult> Select<TSource, TResult, TCapture>(this ConfiguredAsyncEnumerable<TSource> configuredSource, TCapture captureValue, Func<TCapture, TSource, int, TResult> selector)
        {
            ValidateArgument(selector, nameof(selector), 1);

            return Internal.SelectHelper<TResult>.SelectWithIndex(configuredSource.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(captureValue, selector));
        }

        /// <summary>
        /// Projects each element of a configured async-enumerable sequence into a new form by incorporating the element's index.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TResult">The type of the elements in the result sequence, obtained by running the selector function for each element in the source sequence.</typeparam>
        /// <param name="configuredSource">A configured sequence of elements to invoke a transform function on.</param>
        /// <param name="selector">An asynchronous transform function to apply to each source element.</param>
        /// <returns>An async-enumerable sequence whose elements are the result of invoking the transform function on each element of source.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="selector"/> is null.</exception>
        public static AsyncEnumerable<TResult> Select<TSource, TResult>(this ConfiguredAsyncEnumerable<TSource> configuredSource, Func<TSource, int, Promise<TResult>> selector)
        {
            ValidateArgument(selector, nameof(selector), 1);

            return Internal.SelectHelper<TResult>.SelectWithIndexAwait(configuredSource.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(selector));
        }

        /// <summary>
        /// Projects each element of a configured async-enumerable sequence into a new form by incorporating the element's index.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TResult">The type of the elements in the result sequence, obtained by running the selector function for each element in the source sequence.</typeparam>
        /// <typeparam name="TCapture">The type of the captured value that will be passed to <paramref name="selector"/>.</typeparam>
        /// <param name="configuredSource">A configured sequence of elements to invoke a transform function on.</param>
        /// <param name="captureValue">The extra value that will be passed to <paramref name="selector"/>.</param>
        /// <param name="selector">An asynchronous transform to apply to each source element.</param>
        /// <returns>An async-enumerable sequence whose elements are the result of invoking the transform function on each element of source.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="selector"/> is null.</exception>
        public static AsyncEnumerable<TResult> Select<TSource, TResult, TCapture>(this ConfiguredAsyncEnumerable<TSource> configuredSource, TCapture captureValue, Func<TCapture, TSource, int, Promise<TResult>> selector)
        {
            ValidateArgument(selector, nameof(selector), 1);

            return Internal.SelectHelper<TResult>.SelectWithIndexAwait(configuredSource.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(captureValue, selector));
        }
    }
#endif // CSHARP_7_3_OR_NEWER
}