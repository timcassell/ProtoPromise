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
        #region NoResultSelector
        /// <summary>
        /// Projects each element of an async-enumerable sequence to an <see cref="AsyncEnumerable{T}"/> and flattens the resulting sequences into one sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TResult">The type of the elements in the sequence returned by <paramref name="selector"/>.</typeparam>
        /// <param name="source">A sequence of elements to invoke a transform function on.</param>
        /// <param name="selector">A one-to-many transform function to apply to each source element.</param>
        /// <returns>An async-enumerable sequence whose elements are the result of invoking the one-to-many transform function on each element of <paramref name="source"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="selector"/> is null.</exception>
        public static AsyncEnumerable<TResult> SelectMany<TSource, TResult>(this AsyncEnumerable<TSource> source, Func<TSource, AsyncEnumerable<TResult>> selector)
        {
            ValidateArgument(selector, nameof(selector), 1);

            return Internal.SelectManyHelper<TResult>.SelectMany(source.GetAsyncEnumerator(), DelegateWrapper.Create(selector));
        }

        /// <summary>
        /// Projects each element of an async-enumerable sequence to an <see cref="AsyncEnumerable{T}"/> and flattens the resulting sequences into one sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TResult">The type of the elements in the sequence returned by <paramref name="selector"/>.</typeparam>
        /// <typeparam name="TCapture">The type of the captured value that will be passed to <paramref name="selector"/>.</typeparam>
        /// <param name="source">A sequence of elements to invoke a transform function on.</param>
        /// <param name="captureValue">The extra value that will be passed to <paramref name="selector"/>.</param>
        /// <param name="selector">A one-to-many transform function to apply to each source element.</param>
        /// <returns>An async-enumerable sequence whose elements are the result of invoking the one-to-many transform function on each element of <paramref name="source"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="selector"/> is null.</exception>
        public static AsyncEnumerable<TResult> SelectMany<TSource, TResult, TCapture>(this AsyncEnumerable<TSource> source, TCapture captureValue, Func<TCapture, TSource, AsyncEnumerable<TResult>> selector)
        {
            ValidateArgument(selector, nameof(selector), 1);

            return Internal.SelectManyHelper<TResult>.SelectMany(source.GetAsyncEnumerator(), DelegateWrapper.Create(captureValue, selector));
        }

        /// <summary>
        /// Projects each element of an async-enumerable sequence to an <see cref="AsyncEnumerable{T}"/> and flattens the resulting sequences into one sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TResult">The type of the elements in the sequence returned by <paramref name="selector"/>.</typeparam>
        /// <param name="source">A sequence of elements to invoke a transform function on.</param>
        /// <param name="selector">An asynchronous one-to-many transform function to apply to each source element.</param>
        /// <returns>An async-enumerable sequence whose elements are the result of invoking the one-to-many transform function on each element of <paramref name="source"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="selector"/> is null.</exception>
        public static AsyncEnumerable<TResult> SelectMany<TSource, TResult>(this AsyncEnumerable<TSource> source, Func<TSource, CancelationToken, Promise<AsyncEnumerable<TResult>>> selector)
        {
            ValidateArgument(selector, nameof(selector), 1);

            return Internal.SelectManyHelper<TResult>.SelectMany(source.GetAsyncEnumerator(), DelegateWrapper.Create(selector));
        }

        /// <summary>
        /// Projects each element of an async-enumerable sequence to an <see cref="AsyncEnumerable{T}"/> and flattens the resulting sequences into one sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TResult">The type of the elements in the sequence returned by <paramref name="selector"/>.</typeparam>
        /// <typeparam name="TCapture">The type of the captured value that will be passed to <paramref name="selector"/>.</typeparam>
        /// <param name="source">A sequence of elements to invoke a transform function on.</param>
        /// <param name="captureValue">The extra value that will be passed to <paramref name="selector"/>.</param>
        /// <param name="selector">An asynchronous one-to-many transform function to apply to each source element.</param>
        /// <returns>An async-enumerable sequence whose elements are the result of invoking the one-to-many transform function on each element of <paramref name="source"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="selector"/> is null.</exception>
        public static AsyncEnumerable<TResult> SelectMany<TSource, TResult, TCapture>(this AsyncEnumerable<TSource> source, TCapture captureValue, Func<TCapture, TSource, CancelationToken, Promise<AsyncEnumerable<TResult>>> selector)
        {
            ValidateArgument(selector, nameof(selector), 1);

            return Internal.SelectManyHelper<TResult>.SelectMany(source.GetAsyncEnumerator(), DelegateWrapper.Create(captureValue, selector));
        }

        /// <summary>
        /// Projects each element of a configured async-enumerable sequence to an <see cref="AsyncEnumerable{T}"/> and flattens the resulting sequences into one sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TResult">The type of the elements in the sequence returned by <paramref name="selector"/>.</typeparam>
        /// <param name="configuredSource">A configured sequence of elements to invoke a transform function on.</param>
        /// <param name="selector">A one-to-many transform function to apply to each source element.</param>
        /// <returns>An async-enumerable sequence whose elements are the result of invoking the one-to-many transform function on each element of <paramref name="configuredSource"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="selector"/> is null.</exception>
        public static AsyncEnumerable<TResult> SelectMany<TSource, TResult>(this in ConfiguredAsyncEnumerable<TSource> configuredSource, Func<TSource, AsyncEnumerable<TResult>> selector)
        {
            ValidateArgument(selector, nameof(selector), 1);

            return Internal.SelectManyHelper<TResult>.SelectMany(configuredSource.GetAsyncEnumerator(), DelegateWrapper.Create(selector));
        }

        /// <summary>
        /// Projects each element of a configured async-enumerable sequence to an <see cref="AsyncEnumerable{T}"/> and flattens the resulting sequences into one sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TResult">The type of the elements in the sequence returned by <paramref name="selector"/>.</typeparam>
        /// <typeparam name="TCapture">The type of the captured value that will be passed to <paramref name="selector"/>.</typeparam>
        /// <param name="configuredSource">A configured sequence of elements to invoke a transform function on.</param>
        /// <param name="captureValue">The extra value that will be passed to <paramref name="selector"/>.</param>
        /// <param name="selector">A one-to-many transform function to apply to each source element.</param>
        /// <returns>An async-enumerable sequence whose elements are the result of invoking the one-to-many transform function on each element of <paramref name="configuredSource"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="selector"/> is null.</exception>
        public static AsyncEnumerable<TResult> SelectMany<TSource, TResult, TCapture>(this in ConfiguredAsyncEnumerable<TSource> configuredSource, TCapture captureValue, Func<TCapture, TSource, AsyncEnumerable<TResult>> selector)
        {
            ValidateArgument(selector, nameof(selector), 1);

            return Internal.SelectManyHelper<TResult>.SelectMany(configuredSource.GetAsyncEnumerator(), DelegateWrapper.Create(captureValue, selector));
        }

        /// <summary>
        /// Projects each element of a configured async-enumerable sequence to an <see cref="AsyncEnumerable{T}"/> and flattens the resulting sequences into one sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TResult">The type of the elements in the sequence returned by <paramref name="selector"/>.</typeparam>
        /// <param name="configuredSource">A configured sequence of elements to invoke a transform function on.</param>
        /// <param name="selector">An asynchronous one-to-many transform function to apply to each source element.</param>
        /// <returns>An async-enumerable sequence whose elements are the result of invoking the one-to-many transform function on each element of <paramref name="configuredSource"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="selector"/> is null.</exception>
        public static AsyncEnumerable<TResult> SelectMany<TSource, TResult>(this in ConfiguredAsyncEnumerable<TSource> configuredSource, Func<TSource, CancelationToken, Promise<AsyncEnumerable<TResult>>> selector)
        {
            ValidateArgument(selector, nameof(selector), 1);

            return Internal.SelectManyHelper<TResult>.SelectMany(configuredSource.GetAsyncEnumerator(), DelegateWrapper.Create(selector));
        }

        /// <summary>
        /// Projects each element of a configured async-enumerable sequence to an <see cref="AsyncEnumerable{T}"/> and flattens the resulting sequences into one sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TResult">The type of the elements in the sequence returned by <paramref name="selector"/>.</typeparam>
        /// <typeparam name="TCapture">The type of the captured value that will be passed to <paramref name="selector"/>.</typeparam>
        /// <param name="configuredSource">A configured sequence of elements to invoke a transform function on.</param>
        /// <param name="captureValue">The extra value that will be passed to <paramref name="selector"/>.</param>
        /// <param name="selector">An asynchronous one-to-many transform function to apply to each source element.</param>
        /// <returns>An async-enumerable sequence whose elements are the result of invoking the one-to-many transform function on each element of <paramref name="configuredSource"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="selector"/> is null.</exception>
        public static AsyncEnumerable<TResult> SelectMany<TSource, TResult, TCapture>(this in ConfiguredAsyncEnumerable<TSource> configuredSource, TCapture captureValue, Func<TCapture, TSource, CancelationToken, Promise<AsyncEnumerable<TResult>>> selector)
        {
            ValidateArgument(selector, nameof(selector), 1);

            return Internal.SelectManyHelper<TResult>.SelectMany(configuredSource.GetAsyncEnumerator(), DelegateWrapper.Create(captureValue, selector));
        }
        #endregion NoResultSelector

        #region WithResultSelector
        /// <summary>
        /// Projects each element of an async-enumerable sequence to an <see cref="AsyncEnumerable{T}"/>, flattens the resulting sequences into one sequence, and invokes a result selector function on each element therein.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCollection">The type of the intermediate elements collected by <paramref name="collectionSelector"/>.</typeparam>
        /// <typeparam name="TResult">The type of the elements of the resulting sequence.</typeparam>
        /// <param name="source">A sequence of elements to invoke a transform function on.</param>
        /// <param name="collectionSelector">A one-to-many transform function to apply to each source element.</param>
        /// <param name="resultSelector">A transform function to apply to each element of the intermediate sequence.</param>
        /// <returns>
        /// An async-enumerable sequence whose elements are the result of invoking the one-to-many transform function on each element of <paramref name="source"/>
        /// and then mapping each of those sequence elements and their corresponding source element to a result element.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="collectionSelector"/> or <paramref name="resultSelector"/> is null.</exception>
        public static AsyncEnumerable<TResult> SelectMany<TSource, TCollection, TResult>(this AsyncEnumerable<TSource> source,
            Func<TSource, AsyncEnumerable<TCollection>> collectionSelector,
            Func<TSource, TCollection, TResult> resultSelector)
        {
            ValidateArgument(collectionSelector, nameof(collectionSelector), 1);
            ValidateArgument(resultSelector, nameof(resultSelector), 1);

            return Internal.SelectManyHelper<TCollection, TResult>.SelectMany(source.GetAsyncEnumerator(),
                DelegateWrapper.Create(collectionSelector),
                DelegateWrapper.Create(resultSelector));
        }

        /// <summary>
        /// Projects each element of an async-enumerable sequence to an <see cref="AsyncEnumerable{T}"/>, flattens the resulting sequences into one sequence, and invokes a result selector function on each element therein.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCollection">The type of the intermediate elements collected by <paramref name="collectionSelector"/>.</typeparam>
        /// <typeparam name="TResult">The type of the elements of the resulting sequence.</typeparam>
        /// <typeparam name="TCaptureCollection">The type of the captured value that will be passed to <paramref name="collectionSelector"/>.</typeparam>
        /// <param name="source">A sequence of elements to invoke a transform function on.</param>
        /// <param name="collectionCaptureValue">The extra value that will be passed to <paramref name="collectionSelector"/>.</param>
        /// <param name="collectionSelector">A one-to-many transform function to apply to each source element.</param>
        /// <param name="resultSelector">A transform function to apply to each element of the intermediate sequence.</param>
        /// <returns>
        /// An async-enumerable sequence whose elements are the result of invoking the one-to-many transform function on each element of <paramref name="source"/>
        /// and then mapping each of those sequence elements and their corresponding source element to a result element.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="collectionSelector"/> or <paramref name="resultSelector"/> is null.</exception>
        public static AsyncEnumerable<TResult> SelectMany<TSource, TCollection, TResult, TCaptureCollection>(this AsyncEnumerable<TSource> source,
            TCaptureCollection collectionCaptureValue, Func<TCaptureCollection, TSource, AsyncEnumerable<TCollection>> collectionSelector,
            Func<TSource, TCollection, TResult> resultSelector)
        {
            ValidateArgument(collectionSelector, nameof(collectionSelector), 1);
            ValidateArgument(resultSelector, nameof(resultSelector), 1);

            return Internal.SelectManyHelper<TCollection, TResult>.SelectMany(source.GetAsyncEnumerator(),
                DelegateWrapper.Create(collectionCaptureValue, collectionSelector),
                DelegateWrapper.Create(resultSelector));
        }

        /// <summary>
        /// Projects each element of an async-enumerable sequence to an <see cref="AsyncEnumerable{T}"/>, flattens the resulting sequences into one sequence, and invokes a result selector function on each element therein.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCollection">The type of the intermediate elements collected by <paramref name="collectionSelector"/>.</typeparam>
        /// <typeparam name="TResult">The type of the elements of the resulting sequence.</typeparam>
        /// <param name="source">A sequence of elements to invoke a transform function on.</param>
        /// <param name="collectionSelector">An asynchronous one-to-many transform function to apply to each source element.</param>
        /// <param name="resultSelector">An asynchronous transform function to apply to each element of the intermediate sequence.</param>
        /// <returns>
        /// An async-enumerable sequence whose elements are the result of invoking the one-to-many transform function on each element of <paramref name="source"/>
        /// and then mapping each of those sequence elements and their corresponding source element to a result element.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="collectionSelector"/> or <paramref name="resultSelector"/> is null.</exception>
        public static AsyncEnumerable<TResult> SelectMany<TSource, TCollection, TResult>(this AsyncEnumerable<TSource> source,
            Func<TSource, CancelationToken, Promise<AsyncEnumerable<TCollection>>> collectionSelector,
            Func<TSource, TCollection, CancelationToken, Promise<TResult>> resultSelector)
        {
            ValidateArgument(collectionSelector, nameof(collectionSelector), 1);
            ValidateArgument(resultSelector, nameof(resultSelector), 1);

            return Internal.SelectManyHelper<TCollection, TResult>.SelectMany(source.GetAsyncEnumerator(),
                DelegateWrapper.Create(collectionSelector),
                DelegateWrapper.Create(resultSelector));
        }

        /// <summary>
        /// Projects each element of an async-enumerable sequence to an <see cref="AsyncEnumerable{T}"/>, flattens the resulting sequences into one sequence, and invokes a result selector function on each element therein.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCollection">The type of the intermediate elements collected by <paramref name="collectionSelector"/>.</typeparam>
        /// <typeparam name="TResult">The type of the elements of the resulting sequence.</typeparam>
        /// <typeparam name="TCaptureCollection">The type of the captured value that will be passed to <paramref name="collectionSelector"/>.</typeparam>
        /// <param name="source">A sequence of elements to invoke a transform function on.</param>
        /// <param name="collectionCaptureValue">The extra value that will be passed to <paramref name="collectionSelector"/>.</param>
        /// <param name="collectionSelector">An asynchronous one-to-many transform function to apply to each source element.</param>
        /// <param name="resultSelector">An asynchronous transform function to apply to each element of the intermediate sequence.</param>
        /// <returns>
        /// An async-enumerable sequence whose elements are the result of invoking the one-to-many transform function on each element of <paramref name="source"/>
        /// and then mapping each of those sequence elements and their corresponding source element to a result element.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="collectionSelector"/> or <paramref name="resultSelector"/> is null.</exception>
        public static AsyncEnumerable<TResult> SelectMany<TSource, TCollection, TResult, TCaptureCollection>(this AsyncEnumerable<TSource> source,
            TCaptureCollection collectionCaptureValue, Func<TCaptureCollection, TSource, CancelationToken, Promise<AsyncEnumerable<TCollection>>> collectionSelector,
            Func<TSource, TCollection, CancelationToken, Promise<TResult>> resultSelector)
        {
            ValidateArgument(collectionSelector, nameof(collectionSelector), 1);
            ValidateArgument(resultSelector, nameof(resultSelector), 1);

            return Internal.SelectManyHelper<TCollection, TResult>.SelectMany(source.GetAsyncEnumerator(),
                DelegateWrapper.Create(collectionCaptureValue, collectionSelector),
                DelegateWrapper.Create(resultSelector));
        }

        /// <summary>
        /// Projects each element of a configured async-enumerable sequence to an <see cref="AsyncEnumerable{T}"/>, flattens the resulting sequences into one sequence, and invokes a result selector function on each element therein.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCollection">The type of the intermediate elements collected by <paramref name="collectionSelector"/>.</typeparam>
        /// <typeparam name="TResult">The type of the elements of the resulting sequence.</typeparam>
        /// <param name="configuredSource">A configured sequence of elements to invoke a transform function on.</param>
        /// <param name="collectionSelector">A one-to-many transform function to apply to each source element.</param>
        /// <param name="resultSelector">A transform function to apply to each element of the intermediate sequence.</param>
        /// <returns>
        /// An async-enumerable sequence whose elements are the result of invoking the one-to-many transform function on each element of <paramref name="configuredSource"/>
        /// and then mapping each of those sequence elements and their corresponding source element to a result element.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="collectionSelector"/> or <paramref name="resultSelector"/> is null.</exception>
        public static AsyncEnumerable<TResult> SelectMany<TSource, TCollection, TResult>(this in ConfiguredAsyncEnumerable<TSource> configuredSource,
            Func<TSource, AsyncEnumerable<TCollection>> collectionSelector,
            Func<TSource, TCollection, TResult> resultSelector)
        {
            ValidateArgument(collectionSelector, nameof(collectionSelector), 1);
            ValidateArgument(resultSelector, nameof(resultSelector), 1);

            return Internal.SelectManyHelper<TCollection, TResult>.SelectMany(configuredSource.GetAsyncEnumerator(),
                DelegateWrapper.Create(collectionSelector),
                DelegateWrapper.Create(resultSelector));
        }

        /// <summary>
        /// Projects each element of a configured async-enumerable sequence to an <see cref="AsyncEnumerable{T}"/>, flattens the resulting sequences into one sequence, and invokes a result selector function on each element therein.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCollection">The type of the intermediate elements collected by <paramref name="collectionSelector"/>.</typeparam>
        /// <typeparam name="TResult">The type of the elements of the resulting sequence.</typeparam>
        /// <typeparam name="TCaptureCollection">The type of the captured value that will be passed to <paramref name="collectionSelector"/>.</typeparam>
        /// <param name="configuredSource">A configured sequence of elements to invoke a transform function on.</param>
        /// <param name="collectionCaptureValue">The extra value that will be passed to <paramref name="collectionSelector"/>.</param>
        /// <param name="collectionSelector">A one-to-many transform function to apply to each source element.</param>
        /// <param name="resultSelector">A transform function to apply to each element of the intermediate sequence.</param>
        /// <returns>
        /// An async-enumerable sequence whose elements are the result of invoking the one-to-many transform function on each element of <paramref name="configuredSource"/>
        /// and then mapping each of those sequence elements and their corresponding source element to a result element.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="collectionSelector"/> or <paramref name="resultSelector"/> is null.</exception>
        public static AsyncEnumerable<TResult> SelectMany<TSource, TCollection, TResult, TCaptureCollection>(this in ConfiguredAsyncEnumerable<TSource> configuredSource,
            TCaptureCollection collectionCaptureValue, Func<TCaptureCollection, TSource, AsyncEnumerable<TCollection>> collectionSelector,
            Func<TSource, TCollection, TResult> resultSelector)
        {
            ValidateArgument(collectionSelector, nameof(collectionSelector), 1);
            ValidateArgument(resultSelector, nameof(resultSelector), 1);

            return Internal.SelectManyHelper<TCollection, TResult>.SelectMany(configuredSource.GetAsyncEnumerator(),
                DelegateWrapper.Create(collectionCaptureValue, collectionSelector),
                DelegateWrapper.Create(resultSelector));
        }

        /// <summary>
        /// Projects each element of a configured async-enumerable sequence to an <see cref="AsyncEnumerable{T}"/>, flattens the resulting sequences into one sequence, and invokes a result selector function on each element therein.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCollection">The type of the intermediate elements collected by <paramref name="collectionSelector"/>.</typeparam>
        /// <typeparam name="TResult">The type of the elements of the resulting sequence.</typeparam>
        /// <param name="configuredSource">A configured sequence of elements to invoke a transform function on.</param>
        /// <param name="collectionSelector">An asynchronous one-to-many transform function to apply to each source element.</param>
        /// <param name="resultSelector">An asynchronous transform function to apply to each element of the intermediate sequence.</param>
        /// <returns>
        /// An async-enumerable sequence whose elements are the result of invoking the one-to-many transform function on each element of <paramref name="configuredSource"/>
        /// and then mapping each of those sequence elements and their corresponding source element to a result element.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="collectionSelector"/> or <paramref name="resultSelector"/> is null.</exception>
        public static AsyncEnumerable<TResult> SelectMany<TSource, TCollection, TResult>(this in ConfiguredAsyncEnumerable<TSource> configuredSource,
            Func<TSource, CancelationToken, Promise<AsyncEnumerable<TCollection>>> collectionSelector,
            Func<TSource, TCollection, CancelationToken, Promise<TResult>> resultSelector)
        {
            ValidateArgument(collectionSelector, nameof(collectionSelector), 1);
            ValidateArgument(resultSelector, nameof(resultSelector), 1);

            return Internal.SelectManyHelper<TCollection, TResult>.SelectMany(configuredSource.GetAsyncEnumerator(),
                DelegateWrapper.Create(collectionSelector),
                DelegateWrapper.Create(resultSelector));
        }

        /// <summary>
        /// Projects each element of a configured async-enumerable sequence to an <see cref="AsyncEnumerable{T}"/>, flattens the resulting sequences into one sequence, and invokes a result selector function on each element therein.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCollection">The type of the intermediate elements collected by <paramref name="collectionSelector"/>.</typeparam>
        /// <typeparam name="TResult">The type of the elements of the resulting sequence.</typeparam>
        /// <typeparam name="TCaptureCollection">The type of the captured value that will be passed to <paramref name="collectionSelector"/>.</typeparam>
        /// <param name="configuredSource">A configured sequence of elements to invoke a transform function on.</param>
        /// <param name="collectionCaptureValue">The extra value that will be passed to <paramref name="collectionSelector"/>.</param>
        /// <param name="collectionSelector">An asynchronous one-to-many transform function to apply to each source element.</param>
        /// <param name="resultSelector">An asynchronous transform function to apply to each element of the intermediate sequence.</param>
        /// <returns>
        /// An async-enumerable sequence whose elements are the result of invoking the one-to-many transform function on each element of <paramref name="configuredSource"/>
        /// and then mapping each of those sequence elements and their corresponding source element to a result element.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="collectionSelector"/> or <paramref name="resultSelector"/> is null.</exception>
        public static AsyncEnumerable<TResult> SelectMany<TSource, TCollection, TResult, TCaptureCollection>(this in ConfiguredAsyncEnumerable<TSource> configuredSource,
            TCaptureCollection collectionCaptureValue, Func<TCaptureCollection, TSource, CancelationToken, Promise<AsyncEnumerable<TCollection>>> collectionSelector,
            Func<TSource, TCollection, CancelationToken, Promise<TResult>> resultSelector)
        {
            ValidateArgument(collectionSelector, nameof(collectionSelector), 1);
            ValidateArgument(resultSelector, nameof(resultSelector), 1);

            return Internal.SelectManyHelper<TCollection, TResult>.SelectMany(configuredSource.GetAsyncEnumerator(),
                DelegateWrapper.Create(collectionCaptureValue, collectionSelector),
                DelegateWrapper.Create(resultSelector));
        }

        /// <summary>
        /// Projects each element of an async-enumerable sequence to an <see cref="AsyncEnumerable{T}"/>, flattens the resulting sequences into one sequence, and invokes a result selector function on each element therein.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCollection">The type of the intermediate elements collected by <paramref name="collectionSelector"/>.</typeparam>
        /// <typeparam name="TResult">The type of the elements of the resulting sequence.</typeparam>
        /// <typeparam name="TCaptureResult">The type of the captured value that will be passed to <paramref name="resultSelector"/>.</typeparam>
        /// <param name="source">A sequence of elements to invoke a transform function on.</param>
        /// <param name="collectionSelector">A one-to-many transform function to apply to each source element.</param>
        /// <param name="resultCaptureValue">The extra value that will be passed to <paramref name="resultSelector"/>.</param>
        /// <param name="resultSelector">A transform function to apply to each element of the intermediate sequence.</param>
        /// <returns>
        /// An async-enumerable sequence whose elements are the result of invoking the one-to-many transform function on each element of <paramref name="source"/>
        /// and then mapping each of those sequence elements and their corresponding source element to a result element.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="collectionSelector"/> or <paramref name="resultSelector"/> is null.</exception>
        public static AsyncEnumerable<TResult> SelectMany<TSource, TCollection, TResult, TCaptureResult>(this AsyncEnumerable<TSource> source,
            Func<TSource, AsyncEnumerable<TCollection>> collectionSelector,
            TCaptureResult resultCaptureValue, Func<TCaptureResult, TSource, TCollection, TResult> resultSelector)
        {
            ValidateArgument(collectionSelector, nameof(collectionSelector), 1);
            ValidateArgument(resultSelector, nameof(resultSelector), 1);

            return Internal.SelectManyHelper<TCollection, TResult>.SelectMany(source.GetAsyncEnumerator(),
                DelegateWrapper.Create(collectionSelector),
                DelegateWrapper.Create(resultCaptureValue, resultSelector));
        }

        /// <summary>
        /// Projects each element of an async-enumerable sequence to an <see cref="AsyncEnumerable{T}"/>, flattens the resulting sequences into one sequence, and invokes a result selector function on each element therein.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCollection">The type of the intermediate elements collected by <paramref name="collectionSelector"/>.</typeparam>
        /// <typeparam name="TResult">The type of the elements of the resulting sequence.</typeparam>
        /// <typeparam name="TCaptureCollection">The type of the captured value that will be passed to <paramref name="collectionSelector"/>.</typeparam>
        /// <typeparam name="TCaptureResult">The type of the captured value that will be passed to <paramref name="resultSelector"/>.</typeparam>
        /// <param name="source">A sequence of elements to invoke a transform function on.</param>
        /// <param name="collectionCaptureValue">The extra value that will be passed to <paramref name="collectionSelector"/>.</param>
        /// <param name="collectionSelector">A one-to-many transform function to apply to each source element.</param>
        /// <param name="resultCaptureValue">The extra value that will be passed to <paramref name="resultSelector"/>.</param>
        /// <param name="resultSelector">A transform function to apply to each element of the intermediate sequence.</param>
        /// <returns>
        /// An async-enumerable sequence whose elements are the result of invoking the one-to-many transform function on each element of <paramref name="source"/>
        /// and then mapping each of those sequence elements and their corresponding source element to a result element.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="collectionSelector"/> or <paramref name="resultSelector"/> is null.</exception>
        public static AsyncEnumerable<TResult> SelectMany<TSource, TCollection, TResult, TCaptureCollection, TCaptureResult>(this AsyncEnumerable<TSource> source,
            TCaptureCollection collectionCaptureValue, Func<TCaptureCollection, TSource, AsyncEnumerable<TCollection>> collectionSelector,
            TCaptureResult resultCaptureValue, Func<TCaptureResult, TSource, TCollection, TResult> resultSelector)
        {
            ValidateArgument(collectionSelector, nameof(collectionSelector), 1);
            ValidateArgument(resultSelector, nameof(resultSelector), 1);

            return Internal.SelectManyHelper<TCollection, TResult>.SelectMany(source.GetAsyncEnumerator(),
                DelegateWrapper.Create(collectionCaptureValue, collectionSelector),
                DelegateWrapper.Create(resultCaptureValue, resultSelector));
        }

        /// <summary>
        /// Projects each element of an async-enumerable sequence to an <see cref="AsyncEnumerable{T}"/>, flattens the resulting sequences into one sequence, and invokes a result selector function on each element therein.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCollection">The type of the intermediate elements collected by <paramref name="collectionSelector"/>.</typeparam>
        /// <typeparam name="TResult">The type of the elements of the resulting sequence.</typeparam>
        /// <typeparam name="TCaptureResult">The type of the captured value that will be passed to <paramref name="resultSelector"/>.</typeparam>
        /// <param name="source">A sequence of elements to invoke a transform function on.</param>
        /// <param name="collectionSelector">An asynchronous one-to-many transform function to apply to each source element.</param>
        /// <param name="resultCaptureValue">The extra value that will be passed to <paramref name="resultSelector"/>.</param>
        /// <param name="resultSelector">An asynchronous transform function to apply to each element of the intermediate sequence.</param>
        /// <returns>
        /// An async-enumerable sequence whose elements are the result of invoking the one-to-many transform function on each element of <paramref name="source"/>
        /// and then mapping each of those sequence elements and their corresponding source element to a result element.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="collectionSelector"/> or <paramref name="resultSelector"/> is null.</exception>
        public static AsyncEnumerable<TResult> SelectMany<TSource, TCollection, TResult, TCaptureResult>(this AsyncEnumerable<TSource> source,
            Func<TSource, CancelationToken, Promise<AsyncEnumerable<TCollection>>> collectionSelector,
            TCaptureResult resultCaptureValue, Func<TCaptureResult, TSource, TCollection, CancelationToken, Promise<TResult>> resultSelector)
        {
            ValidateArgument(collectionSelector, nameof(collectionSelector), 1);
            ValidateArgument(resultSelector, nameof(resultSelector), 1);

            return Internal.SelectManyHelper<TCollection, TResult>.SelectMany(source.GetAsyncEnumerator(),
                DelegateWrapper.Create(collectionSelector),
                DelegateWrapper.Create(resultCaptureValue, resultSelector));
        }

        /// <summary>
        /// Projects each element of an async-enumerable sequence to an <see cref="AsyncEnumerable{T}"/>, flattens the resulting sequences into one sequence, and invokes a result selector function on each element therein.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCollection">The type of the intermediate elements collected by <paramref name="collectionSelector"/>.</typeparam>
        /// <typeparam name="TResult">The type of the elements of the resulting sequence.</typeparam>
        /// <typeparam name="TCaptureCollection">The type of the captured value that will be passed to <paramref name="collectionSelector"/>.</typeparam>
        /// <typeparam name="TCaptureResult">The type of the captured value that will be passed to <paramref name="resultSelector"/>.</typeparam>
        /// <param name="source">A sequence of elements to invoke a transform function on.</param>
        /// <param name="collectionCaptureValue">The extra value that will be passed to <paramref name="collectionSelector"/>.</param>
        /// <param name="collectionSelector">An asynchronous one-to-many transform function to apply to each source element.</param>
        /// <param name="resultCaptureValue">The extra value that will be passed to <paramref name="resultSelector"/>.</param>
        /// <param name="resultSelector">An asynchronous transform function to apply to each element of the intermediate sequence.</param>
        /// <returns>
        /// An async-enumerable sequence whose elements are the result of invoking the one-to-many transform function on each element of <paramref name="source"/>
        /// and then mapping each of those sequence elements and their corresponding source element to a result element.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="collectionSelector"/> or <paramref name="resultSelector"/> is null.</exception>
        public static AsyncEnumerable<TResult> SelectMany<TSource, TCollection, TResult, TCaptureCollection, TCaptureResult>(this AsyncEnumerable<TSource> source,
            TCaptureCollection collectionCaptureValue, Func<TCaptureCollection, TSource, CancelationToken, Promise<AsyncEnumerable<TCollection>>> collectionSelector,
            TCaptureResult resultCaptureValue, Func<TCaptureResult, TSource, TCollection, CancelationToken, Promise<TResult>> resultSelector)
        {
            ValidateArgument(collectionSelector, nameof(collectionSelector), 1);
            ValidateArgument(resultSelector, nameof(resultSelector), 1);

            return Internal.SelectManyHelper<TCollection, TResult>.SelectMany(source.GetAsyncEnumerator(),
                DelegateWrapper.Create(collectionCaptureValue, collectionSelector),
                DelegateWrapper.Create(resultCaptureValue, resultSelector));
        }

        /// <summary>
        /// Projects each element of a configured async-enumerable sequence to an <see cref="AsyncEnumerable{T}"/>, flattens the resulting sequences into one sequence, and invokes a result selector function on each element therein.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCollection">The type of the intermediate elements collected by <paramref name="collectionSelector"/>.</typeparam>
        /// <typeparam name="TResult">The type of the elements of the resulting sequence.</typeparam>
        /// <typeparam name="TCaptureResult">The type of the captured value that will be passed to <paramref name="resultSelector"/>.</typeparam>
        /// <param name="configuredSource">A configured sequence of elements to invoke a transform function on.</param>
        /// <param name="collectionSelector">A one-to-many transform function to apply to each source element.</param>
        /// <param name="resultCaptureValue">The extra value that will be passed to <paramref name="resultSelector"/>.</param>
        /// <param name="resultSelector">A transform function to apply to each element of the intermediate sequence.</param>
        /// <returns>
        /// An async-enumerable sequence whose elements are the result of invoking the one-to-many transform function on each element of <paramref name="configuredSource"/>
        /// and then mapping each of those sequence elements and their corresponding source element to a result element.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="collectionSelector"/> or <paramref name="resultSelector"/> is null.</exception>
        public static AsyncEnumerable<TResult> SelectMany<TSource, TCollection, TResult, TCaptureResult>(this in ConfiguredAsyncEnumerable<TSource> configuredSource,
            Func<TSource, AsyncEnumerable<TCollection>> collectionSelector,
            TCaptureResult resultCaptureValue, Func<TCaptureResult, TSource, TCollection, TResult> resultSelector)
        {
            ValidateArgument(collectionSelector, nameof(collectionSelector), 1);
            ValidateArgument(resultSelector, nameof(resultSelector), 1);

            return Internal.SelectManyHelper<TCollection, TResult>.SelectMany(configuredSource.GetAsyncEnumerator(),
                DelegateWrapper.Create(collectionSelector),
                DelegateWrapper.Create(resultCaptureValue, resultSelector));
        }

        /// <summary>
        /// Projects each element of a configured async-enumerable sequence to an <see cref="AsyncEnumerable{T}"/>, flattens the resulting sequences into one sequence, and invokes a result selector function on each element therein.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCollection">The type of the intermediate elements collected by <paramref name="collectionSelector"/>.</typeparam>
        /// <typeparam name="TResult">The type of the elements of the resulting sequence.</typeparam>
        /// <typeparam name="TCaptureCollection">The type of the captured value that will be passed to <paramref name="collectionSelector"/>.</typeparam>
        /// <typeparam name="TCaptureResult">The type of the captured value that will be passed to <paramref name="resultSelector"/>.</typeparam>
        /// <param name="configuredSource">A configured sequence of elements to invoke a transform function on.</param>
        /// <param name="collectionCaptureValue">The extra value that will be passed to <paramref name="collectionSelector"/>.</param>
        /// <param name="collectionSelector">A one-to-many transform function to apply to each source element.</param>
        /// <param name="resultCaptureValue">The extra value that will be passed to <paramref name="resultSelector"/>.</param>
        /// <param name="resultSelector">A transform function to apply to each element of the intermediate sequence.</param>
        /// <returns>
        /// An async-enumerable sequence whose elements are the result of invoking the one-to-many transform function on each element of <paramref name="configuredSource"/>
        /// and then mapping each of those sequence elements and their corresponding source element to a result element.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="collectionSelector"/> or <paramref name="resultSelector"/> is null.</exception>
        public static AsyncEnumerable<TResult> SelectMany<TSource, TCollection, TResult, TCaptureCollection, TCaptureResult>(this in ConfiguredAsyncEnumerable<TSource> configuredSource,
            TCaptureCollection collectionCaptureValue, Func<TCaptureCollection, TSource, AsyncEnumerable<TCollection>> collectionSelector,
            TCaptureResult resultCaptureValue, Func<TCaptureResult, TSource, TCollection, TResult> resultSelector)
        {
            ValidateArgument(collectionSelector, nameof(collectionSelector), 1);
            ValidateArgument(resultSelector, nameof(resultSelector), 1);

            return Internal.SelectManyHelper<TCollection, TResult>.SelectMany(configuredSource.GetAsyncEnumerator(),
                DelegateWrapper.Create(collectionCaptureValue, collectionSelector),
                DelegateWrapper.Create(resultCaptureValue, resultSelector));
        }

        /// <summary>
        /// Projects each element of a configured async-enumerable sequence to an <see cref="AsyncEnumerable{T}"/>, flattens the resulting sequences into one sequence, and invokes a result selector function on each element therein.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCollection">The type of the intermediate elements collected by <paramref name="collectionSelector"/>.</typeparam>
        /// <typeparam name="TResult">The type of the elements of the resulting sequence.</typeparam>
        /// <typeparam name="TCaptureResult">The type of the captured value that will be passed to <paramref name="resultSelector"/>.</typeparam>
        /// <param name="configuredSource">A configured sequence of elements to invoke a transform function on.</param>
        /// <param name="collectionSelector">An asynchronous one-to-many transform function to apply to each source element.</param>
        /// <param name="resultCaptureValue">The extra value that will be passed to <paramref name="resultSelector"/>.</param>
        /// <param name="resultSelector">An asynchronous transform function to apply to each element of the intermediate sequence.</param>
        /// <returns>
        /// An async-enumerable sequence whose elements are the result of invoking the one-to-many transform function on each element of <paramref name="configuredSource"/>
        /// and then mapping each of those sequence elements and their corresponding source element to a result element.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="collectionSelector"/> or <paramref name="resultSelector"/> is null.</exception>
        public static AsyncEnumerable<TResult> SelectMany<TSource, TCollection, TResult, TCaptureResult>(this in ConfiguredAsyncEnumerable<TSource> configuredSource,
            Func<TSource, CancelationToken, Promise<AsyncEnumerable<TCollection>>> collectionSelector,
            TCaptureResult resultCaptureValue, Func<TCaptureResult, TSource, TCollection, CancelationToken, Promise<TResult>> resultSelector)
        {
            ValidateArgument(collectionSelector, nameof(collectionSelector), 1);
            ValidateArgument(resultSelector, nameof(resultSelector), 1);

            return Internal.SelectManyHelper<TCollection, TResult>.SelectMany(configuredSource.GetAsyncEnumerator(),
                DelegateWrapper.Create(collectionSelector),
                DelegateWrapper.Create(resultCaptureValue, resultSelector));
        }

        /// <summary>
        /// Projects each element of a configured async-enumerable sequence to an <see cref="AsyncEnumerable{T}"/>, flattens the resulting sequences into one sequence, and invokes a result selector function on each element therein.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCollection">The type of the intermediate elements collected by <paramref name="collectionSelector"/>.</typeparam>
        /// <typeparam name="TResult">The type of the elements of the resulting sequence.</typeparam>
        /// <typeparam name="TCaptureCollection">The type of the captured value that will be passed to <paramref name="collectionSelector"/>.</typeparam>
        /// <typeparam name="TCaptureResult">The type of the captured value that will be passed to <paramref name="resultSelector"/>.</typeparam>
        /// <param name="configuredSource">A configured sequence of elements to invoke a transform function on.</param>
        /// <param name="collectionCaptureValue">The extra value that will be passed to <paramref name="collectionSelector"/>.</param>
        /// <param name="collectionSelector">An asynchronous one-to-many transform function to apply to each source element.</param>
        /// <param name="resultCaptureValue">The extra value that will be passed to <paramref name="resultSelector"/>.</param>
        /// <param name="resultSelector">An asynchronous transform function to apply to each element of the intermediate sequence.</param>
        /// <returns>
        /// An async-enumerable sequence whose elements are the result of invoking the one-to-many transform function on each element of <paramref name="configuredSource"/>
        /// and then mapping each of those sequence elements and their corresponding source element to a result element.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="collectionSelector"/> or <paramref name="resultSelector"/> is null.</exception>
        public static AsyncEnumerable<TResult> SelectMany<TSource, TCollection, TResult, TCaptureCollection, TCaptureResult>(this in ConfiguredAsyncEnumerable<TSource> configuredSource,
            TCaptureCollection collectionCaptureValue, Func<TCaptureCollection, TSource, CancelationToken, Promise<AsyncEnumerable<TCollection>>> collectionSelector,
            TCaptureResult resultCaptureValue, Func<TCaptureResult, TSource, TCollection, CancelationToken, Promise<TResult>> resultSelector)
        {
            ValidateArgument(collectionSelector, nameof(collectionSelector), 1);
            ValidateArgument(resultSelector, nameof(resultSelector), 1);

            return Internal.SelectManyHelper<TCollection, TResult>.SelectMany(configuredSource.GetAsyncEnumerator(),
                DelegateWrapper.Create(collectionCaptureValue, collectionSelector),
                DelegateWrapper.Create(resultCaptureValue, resultSelector));
        }
        #endregion WithResultSelector
    }
}