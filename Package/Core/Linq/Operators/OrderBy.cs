#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using Proto.Promises.Async.CompilerServices;
using System;
using System.Collections.Generic;

namespace Proto.Promises.Linq
{
#if CSHARP_7_3_OR_NEWER
    partial class AsyncEnumerable
    {
        #region OrderBy
        /// <summary>
        /// Sorts the elements of an async-enumerable sequence in ascending order according to a specified key selector function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by the <paramref name="keySelector"/>.</typeparam>
        /// <param name="source">An async-enumerable sequence to sort.</param>
        /// <param name="keySelector">A function to extract a key from an element.</param>
        /// <returns>An <see cref="OrderedAsyncEnumerable{T}"/> whose elements are sorted according to a key.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        public static OrderedAsyncEnumerable<TSource> OrderBy<TSource, TKey>(
            this AsyncEnumerable<TSource> source,
            Func<TSource, TKey> keySelector)
            => OrderBy(source, keySelector, comparer: null);

        /// <summary>
        /// Sorts the elements of an async-enumerable sequence in ascending order according to a specified key selector function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to the key selector.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by the <paramref name="keySelector"/>.</typeparam>
        /// <param name="source">An async-enumerable sequence to sort.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">A function to extract a key from an element.</param>
        /// <returns>An <see cref="OrderedAsyncEnumerable{T}"/> whose elements are sorted according to a key.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        public static OrderedAsyncEnumerable<TSource> OrderBy<TSource, TCaptureKey, TKey>(
            this AsyncEnumerable<TSource> source,
            TCaptureKey keyCaptureValue,
            Func<TCaptureKey, TSource, TKey> keySelector)
            => OrderBy(source, keyCaptureValue, keySelector, comparer: null);

        /// <summary>
        /// Sorts the elements of an async-enumerable sequence in ascending order according to a specified key selector function, and a comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by the <paramref name="keySelector"/>.</typeparam>
        /// <param name="source">An async-enumerable sequence to sort.</param>
        /// <param name="keySelector">A function to extract a key from an element.</param>
        /// <param name="comparer">A comparer to compare keys. If null, the default comparer will be used.</param>
        /// <returns>An <see cref="OrderedAsyncEnumerable{T}"/> whose elements are sorted according to a key.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        public static OrderedAsyncEnumerable<TSource> OrderBy<TSource, TKey>(
            this AsyncEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            IComparer<TKey> comparer)
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);

            return Internal.OrderHelper<TSource, TKey>.OrderBy(source.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(keySelector), comparer ?? Comparer<TKey>.Default);
        }

        /// <summary>
        /// Sorts the elements of an async-enumerable sequence in ascending order according to a specified key selector function, and a comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to the key selector.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by the <paramref name="keySelector"/>.</typeparam>
        /// <param name="source">An async-enumerable sequence to sort.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">A function to extract a key from an element.</param>
        /// <param name="comparer">A comparer to compare keys. If null, the default comparer will be used.</param>
        /// <returns>An <see cref="OrderedAsyncEnumerable{T}"/> whose elements are sorted according to a key.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        public static OrderedAsyncEnumerable<TSource> OrderBy<TSource, TCaptureKey, TKey>(
            this AsyncEnumerable<TSource> source,
            TCaptureKey keyCaptureValue,
            Func<TCaptureKey, TSource, TKey> keySelector,
            IComparer<TKey> comparer)
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);

            return Internal.OrderHelper<TSource, TKey>.OrderBy(source.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(keyCaptureValue, keySelector), comparer ?? Comparer<TKey>.Default);
        }

        /// <summary>
        /// Sorts the elements of an async-enumerable sequence in ascending order according to a specified key selector function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by the <paramref name="keySelector"/>.</typeparam>
        /// <param name="source">An async-enumerable sequence to sort.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from an element.</param>
        /// <returns>An <see cref="OrderedAsyncEnumerable{T}"/> whose elements are sorted according to a key.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        public static OrderedAsyncEnumerable<TSource> OrderBy<TSource, TKey>(
            this AsyncEnumerable<TSource> source,
            Func<TSource, Promise<TKey>> keySelector)
            => OrderBy<TSource, TKey>(source, keySelector, comparer: null);

        /// <summary>
        /// Sorts the elements of an async-enumerable sequence in ascending order according to a specified key selector function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to the key selector.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by the <paramref name="keySelector"/>.</typeparam>
        /// <param name="source">An async-enumerable sequence to sort.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from an element.</param>
        /// <returns>An <see cref="OrderedAsyncEnumerable{T}"/> whose elements are sorted according to a key.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        public static OrderedAsyncEnumerable<TSource> OrderBy<TSource, TCaptureKey, TKey>(
            this AsyncEnumerable<TSource> source,
            TCaptureKey keyCaptureValue,
            Func<TCaptureKey, TSource, Promise<TKey>> keySelector)
            => OrderBy<TSource, TCaptureKey, TKey>(source, keyCaptureValue, keySelector, comparer: null);

        /// <summary>
        /// Sorts the elements of an async-enumerable sequence in ascending order according to a specified key selector function, and a comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by the <paramref name="keySelector"/>.</typeparam>
        /// <param name="source">An async-enumerable sequence to sort.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from an element.</param>
        /// <param name="comparer">A comparer to compare keys. If null, the default comparer will be used.</param>
        /// <returns>An <see cref="OrderedAsyncEnumerable{T}"/> whose elements are sorted according to a key.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        public static OrderedAsyncEnumerable<TSource> OrderBy<TSource, TKey>(
            this AsyncEnumerable<TSource> source,
            Func<TSource, Promise<TKey>> keySelector,
            IComparer<TKey> comparer)
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);

            return Internal.OrderHelper<TSource, TKey>.OrderByAwait(source.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(keySelector), comparer ?? Comparer<TKey>.Default);
        }

        /// <summary>
        /// Sorts the elements of an async-enumerable sequence in ascending order according to a specified key selector function, and a comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to the key selector.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by the <paramref name="keySelector"/>.</typeparam>
        /// <param name="source">An async-enumerable sequence to sort.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from an element.</param>
        /// <param name="comparer">A comparer to compare keys. If null, the default comparer will be used.</param>
        /// <returns>An <see cref="OrderedAsyncEnumerable{T}"/> whose elements are sorted according to a key.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        public static OrderedAsyncEnumerable<TSource> OrderBy<TSource, TCaptureKey, TKey>(
            this AsyncEnumerable<TSource> source,
            TCaptureKey keyCaptureValue,
            Func<TCaptureKey, TSource, Promise<TKey>> keySelector,
            IComparer<TKey> comparer)
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);

            return Internal.OrderHelper<TSource, TKey>.OrderByAwait(source.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(keyCaptureValue, keySelector), comparer ?? Comparer<TKey>.Default);
        }

        /// <summary>
        /// Sorts the elements of a configured async-enumerable sequence in ascending order according to a specified key selector function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by the <paramref name="keySelector"/>.</typeparam>
        /// <param name="configuredSource">A configured async-enumerable sequence to sort.</param>
        /// <param name="keySelector">A function to extract a key from an element.</param>
        /// <returns>An <see cref="OrderedAsyncEnumerable{T}"/> whose elements are sorted according to a key.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        public static OrderedAsyncEnumerable<TSource> OrderBy<TSource, TKey>(
            this ConfiguredAsyncEnumerable<TSource> configuredSource,
            Func<TSource, TKey> keySelector)
            => OrderBy(configuredSource, keySelector, comparer: null);

        /// <summary>
        /// Sorts the elements of a configured async-enumerable sequence in ascending order according to a specified key selector function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to the key selector.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by the <paramref name="keySelector"/>.</typeparam>
        /// <param name="configuredSource">A configured async-enumerable sequence to sort.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">A function to extract a key from an element.</param>
        /// <returns>An <see cref="OrderedAsyncEnumerable{T}"/> whose elements are sorted according to a key.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        public static OrderedAsyncEnumerable<TSource> OrderBy<TSource, TCaptureKey, TKey>(
            this ConfiguredAsyncEnumerable<TSource> configuredSource,
            TCaptureKey keyCaptureValue,
            Func<TCaptureKey, TSource, TKey> keySelector)
            => OrderBy(configuredSource, keyCaptureValue, keySelector, comparer: null);

        /// <summary>
        /// Sorts the elements of a configured async-enumerable sequence in ascending order according to a specified key selector function, and a comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by the <paramref name="keySelector"/>.</typeparam>
        /// <param name="configuredSource">A configured async-enumerable sequence to sort.</param>
        /// <param name="keySelector">A function to extract a key from an element.</param>
        /// <param name="comparer">A comparer to compare keys. If null, the default comparer will be used.</param>
        /// <returns>An <see cref="OrderedAsyncEnumerable{T}"/> whose elements are sorted according to a key.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        public static OrderedAsyncEnumerable<TSource> OrderBy<TSource, TKey>(
            this ConfiguredAsyncEnumerable<TSource> configuredSource,
            Func<TSource, TKey> keySelector,
            IComparer<TKey> comparer)
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);

            return Internal.OrderHelper<TSource, TKey>.OrderBy(configuredSource.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(keySelector), comparer ?? Comparer<TKey>.Default);
        }

        /// <summary>
        /// Sorts the elements of a configured async-enumerable sequence in ascending order according to a specified key selector function, and a comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to the key selector.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by the <paramref name="keySelector"/>.</typeparam>
        /// <param name="configuredSource">A configured async-enumerable sequence to sort.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from an element.</param>
        /// <param name="comparer">A comparer to compare keys. If null, the default comparer will be used.</param>
        /// <returns>An <see cref="OrderedAsyncEnumerable{T}"/> whose elements are sorted according to a key.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        public static OrderedAsyncEnumerable<TSource> OrderBy<TSource, TCaptureKey, TKey>(
            this ConfiguredAsyncEnumerable<TSource> configuredSource,
            TCaptureKey keyCaptureValue,
            Func<TCaptureKey, TSource, TKey> keySelector,
            IComparer<TKey> comparer)
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);

            return Internal.OrderHelper<TSource, TKey>.OrderBy(configuredSource.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(keyCaptureValue, keySelector), comparer ?? Comparer<TKey>.Default);
        }

        /// <summary>
        /// Sorts the elements of a configured async-enumerable sequence in ascending order according to a specified key selector function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by the <paramref name="keySelector"/>.</typeparam>
        /// <param name="configuredSource">A configured async-enumerable sequence to sort.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from an element.</param>
        /// <returns>An <see cref="OrderedAsyncEnumerable{T}"/> whose elements are sorted according to a key.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        public static OrderedAsyncEnumerable<TSource> OrderBy<TSource, TKey>(
            this ConfiguredAsyncEnumerable<TSource> configuredSource,
            Func<TSource, Promise<TKey>> keySelector)
            => OrderBy<TSource, TKey>(configuredSource, keySelector, comparer: null);

        /// <summary>
        /// Sorts the elements of a configured async-enumerable sequence in ascending order according to a specified key selector function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to the key selector.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by the <paramref name="keySelector"/>.</typeparam>
        /// <param name="configuredSource">A configured async-enumerable sequence to sort.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from an element.</param>
        /// <returns>An <see cref="OrderedAsyncEnumerable{T}"/> whose elements are sorted according to a key.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        public static OrderedAsyncEnumerable<TSource> OrderBy<TSource, TCaptureKey, TKey>(
            this ConfiguredAsyncEnumerable<TSource> configuredSource,
            TCaptureKey keyCaptureValue,
            Func<TCaptureKey, TSource, Promise<TKey>> keySelector)
            => OrderBy<TSource, TCaptureKey, TKey>(configuredSource, keyCaptureValue, keySelector, comparer: null);

        /// <summary>
        /// Sorts the elements of a configured async-enumerable sequence in ascending order according to a specified key selector function, and a comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by the <paramref name="keySelector"/>.</typeparam>
        /// <param name="configuredSource">A configured async-enumerable sequence to sort.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from an element.</param>
        /// <param name="comparer">A comparer to compare keys. If null, the default comparer will be used.</param>
        /// <returns>An <see cref="OrderedAsyncEnumerable{T}"/> whose elements are sorted according to a key.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        public static OrderedAsyncEnumerable<TSource> OrderBy<TSource, TKey>(
            this ConfiguredAsyncEnumerable<TSource> configuredSource,
            Func<TSource, Promise<TKey>> keySelector,
            IComparer<TKey> comparer)
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);

            return Internal.OrderHelper<TSource, TKey>.OrderByAwait(configuredSource.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(keySelector), comparer ?? Comparer<TKey>.Default);
        }

        /// <summary>
        /// Sorts the elements of a configured async-enumerable sequence in ascending order according to a specified key selector function, and a comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to the key selector.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by the <paramref name="keySelector"/>.</typeparam>
        /// <param name="configuredSource">A configured async-enumerable sequence to sort.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from an element.</param>
        /// <param name="comparer">A comparer to compare keys. If null, the default comparer will be used.</param>
        /// <returns>An <see cref="OrderedAsyncEnumerable{T}"/> whose elements are sorted according to a key.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        public static OrderedAsyncEnumerable<TSource> OrderBy<TSource, TCaptureKey, TKey>(
            this ConfiguredAsyncEnumerable<TSource> configuredSource,
            TCaptureKey keyCaptureValue,
            Func<TCaptureKey, TSource, Promise<TKey>> keySelector,
            IComparer<TKey> comparer)
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);

            return Internal.OrderHelper<TSource, TKey>.OrderByAwait(configuredSource.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(keyCaptureValue, keySelector), comparer ?? Comparer<TKey>.Default);
        }

        /// <summary>
        /// Sorts the elements of an async-enumerable sequence in descending order according to a specified key selector function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by the <paramref name="keySelector"/>.</typeparam>
        /// <param name="source">An async-enumerable sequence to sort.</param>
        /// <param name="keySelector">A function to extract a key from an element.</param>
        /// <returns>An <see cref="OrderedAsyncEnumerable{T}"/> whose elements are sorted according to a key.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        public static OrderedAsyncEnumerable<TSource> OrderByDescending<TSource, TKey>(
            this AsyncEnumerable<TSource> source,
            Func<TSource, TKey> keySelector)
            => OrderByDescending(source, keySelector, comparer: null);

        /// <summary>
        /// Sorts the elements of an async-enumerable sequence in descending order according to a specified key selector function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to the key selector.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by the <paramref name="keySelector"/>.</typeparam>
        /// <param name="source">An async-enumerable sequence to sort.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">A function to extract a key from an element.</param>
        /// <returns>An <see cref="OrderedAsyncEnumerable{T}"/> whose elements are sorted according to a key.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        public static OrderedAsyncEnumerable<TSource> OrderByDescending<TSource, TCaptureKey, TKey>(
            this AsyncEnumerable<TSource> source,
            TCaptureKey keyCaptureValue,
            Func<TCaptureKey, TSource, TKey> keySelector)
            => OrderByDescending(source, keyCaptureValue, keySelector, comparer: null);

        /// <summary>
        /// Sorts the elements of an async-enumerable sequence in descending order according to a specified key selector function, and a comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by the <paramref name="keySelector"/>.</typeparam>
        /// <param name="source">An async-enumerable sequence to sort.</param>
        /// <param name="keySelector">A function to extract a key from an element.</param>
        /// <param name="comparer">A comparer to compare keys. If null, the default comparer will be used.</param>
        /// <returns>An <see cref="OrderedAsyncEnumerable{T}"/> whose elements are sorted according to a key.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        public static OrderedAsyncEnumerable<TSource> OrderByDescending<TSource, TKey>(
            this AsyncEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            IComparer<TKey> comparer)
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);

            return Internal.OrderHelper<TSource, TKey>.OrderBy(source.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(keySelector), new Internal.ReverseComparer<TKey>(comparer ?? Comparer<TKey>.Default));
        }

        /// <summary>
        /// Sorts the elements of an async-enumerable sequence in descending order according to a specified key selector function, and a comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to the key selector.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by the <paramref name="keySelector"/>.</typeparam>
        /// <param name="source">An async-enumerable sequence to sort.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">A function to extract a key from an element.</param>
        /// <param name="comparer">A comparer to compare keys. If null, the default comparer will be used.</param>
        /// <returns>An <see cref="OrderedAsyncEnumerable{T}"/> whose elements are sorted according to a key.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        public static OrderedAsyncEnumerable<TSource> OrderByDescending<TSource, TCaptureKey, TKey>(
            this AsyncEnumerable<TSource> source,
            TCaptureKey keyCaptureValue,
            Func<TCaptureKey, TSource, TKey> keySelector,
            IComparer<TKey> comparer)
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);

            return Internal.OrderHelper<TSource, TKey>.OrderBy(source.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(keyCaptureValue, keySelector), new Internal.ReverseComparer<TKey>(comparer ?? Comparer<TKey>.Default));
        }

        /// <summary>
        /// Sorts the elements of an async-enumerable sequence in descending order according to a specified key selector function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by the <paramref name="keySelector"/>.</typeparam>
        /// <param name="source">An async-enumerable sequence to sort.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from an element.</param>
        /// <returns>An <see cref="OrderedAsyncEnumerable{T}"/> whose elements are sorted according to a key.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        public static OrderedAsyncEnumerable<TSource> OrderByDescending<TSource, TKey>(
            this AsyncEnumerable<TSource> source,
            Func<TSource, Promise<TKey>> keySelector)
            => OrderByDescending<TSource, TKey>(source, keySelector, comparer: null);

        /// <summary>
        /// Sorts the elements of an async-enumerable sequence in descending order according to a specified key selector function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to the key selector.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by the <paramref name="keySelector"/>.</typeparam>
        /// <param name="source">An async-enumerable sequence to sort.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from an element.</param>
        /// <returns>An <see cref="OrderedAsyncEnumerable{T}"/> whose elements are sorted according to a key.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        public static OrderedAsyncEnumerable<TSource> OrderByDescending<TSource, TCaptureKey, TKey>(
            this AsyncEnumerable<TSource> source,
            TCaptureKey keyCaptureValue,
            Func<TCaptureKey, TSource, Promise<TKey>> keySelector)
            => OrderByDescending<TSource, TCaptureKey, TKey>(source, keyCaptureValue, keySelector, comparer: null);

        /// <summary>
        /// Sorts the elements of an async-enumerable sequence in descending order according to a specified key selector function, and a comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by the <paramref name="keySelector"/>.</typeparam>
        /// <param name="source">An async-enumerable sequence to sort.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from an element.</param>
        /// <param name="comparer">A comparer to compare keys. If null, the default comparer will be used.</param>
        /// <returns>An <see cref="OrderedAsyncEnumerable{T}"/> whose elements are sorted according to a key.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        public static OrderedAsyncEnumerable<TSource> OrderByDescending<TSource, TKey>(
            this AsyncEnumerable<TSource> source,
            Func<TSource, Promise<TKey>> keySelector,
            IComparer<TKey> comparer)
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);

            return Internal.OrderHelper<TSource, TKey>.OrderByAwait(source.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(keySelector), new Internal.ReverseComparer<TKey>(comparer ?? Comparer<TKey>.Default));
        }

        /// <summary>
        /// Sorts the elements of an async-enumerable sequence in descending order according to a specified key selector function, and a comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to the key selector.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by the <paramref name="keySelector"/>.</typeparam>
        /// <param name="source">An async-enumerable sequence to sort.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from an element.</param>
        /// <param name="comparer">A comparer to compare keys. If null, the default comparer will be used.</param>
        /// <returns>An <see cref="OrderedAsyncEnumerable{T}"/> whose elements are sorted according to a key.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        public static OrderedAsyncEnumerable<TSource> OrderByDescending<TSource, TCaptureKey, TKey>(
            this AsyncEnumerable<TSource> source,
            TCaptureKey keyCaptureValue,
            Func<TCaptureKey, TSource, Promise<TKey>> keySelector,
            IComparer<TKey> comparer)
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);

            return Internal.OrderHelper<TSource, TKey>.OrderByAwait(source.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(keyCaptureValue, keySelector), new Internal.ReverseComparer<TKey>(comparer ?? Comparer<TKey>.Default));
        }

        /// <summary>
        /// Sorts the elements of a configured async-enumerable sequence in descending order according to a specified key selector function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by the <paramref name="keySelector"/>.</typeparam>
        /// <param name="configuredSource">A configured async-enumerable sequence to sort.</param>
        /// <param name="keySelector">A function to extract a key from an element.</param>
        /// <returns>An <see cref="OrderedAsyncEnumerable{T}"/> whose elements are sorted according to a key.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        public static OrderedAsyncEnumerable<TSource> OrderByDescending<TSource, TKey>(
            this ConfiguredAsyncEnumerable<TSource> configuredSource,
            Func<TSource, TKey> keySelector)
            => OrderByDescending(configuredSource, keySelector, comparer: null);

        /// <summary>
        /// Sorts the elements of a configured async-enumerable sequence in descending order according to a specified key selector function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to the key selector.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by the <paramref name="keySelector"/>.</typeparam>
        /// <param name="configuredSource">A configured async-enumerable sequence to sort.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">A function to extract a key from an element.</param>
        /// <returns>An <see cref="OrderedAsyncEnumerable{T}"/> whose elements are sorted according to a key.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        public static OrderedAsyncEnumerable<TSource> OrderByDescending<TSource, TCaptureKey, TKey>(
            this ConfiguredAsyncEnumerable<TSource> configuredSource,
            TCaptureKey keyCaptureValue,
            Func<TCaptureKey, TSource, TKey> keySelector)
            => OrderByDescending(configuredSource, keyCaptureValue, keySelector, comparer: null);

        /// <summary>
        /// Sorts the elements of a configured async-enumerable sequence in descending order according to a specified key selector function, and a comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by the <paramref name="keySelector"/>.</typeparam>
        /// <param name="configuredSource">A configured async-enumerable sequence to sort.</param>
        /// <param name="keySelector">A function to extract a key from an element.</param>
        /// <param name="comparer">A comparer to compare keys. If null, the default comparer will be used.</param>
        /// <returns>An <see cref="OrderedAsyncEnumerable{T}"/> whose elements are sorted according to a key.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        public static OrderedAsyncEnumerable<TSource> OrderByDescending<TSource, TKey>(
            this ConfiguredAsyncEnumerable<TSource> configuredSource,
            Func<TSource, TKey> keySelector,
            IComparer<TKey> comparer)
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);

            return Internal.OrderHelper<TSource, TKey>.OrderBy(configuredSource.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(keySelector), new Internal.ReverseComparer<TKey>(comparer ?? Comparer<TKey>.Default));
        }

        /// <summary>
        /// Sorts the elements of a configured async-enumerable sequence in descending order according to a specified key selector function, and a comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to the key selector.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by the <paramref name="keySelector"/>.</typeparam>
        /// <param name="configuredSource">A configured async-enumerable sequence to sort.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from an element.</param>
        /// <param name="comparer">A comparer to compare keys. If null, the default comparer will be used.</param>
        /// <returns>An <see cref="OrderedAsyncEnumerable{T}"/> whose elements are sorted according to a key.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        public static OrderedAsyncEnumerable<TSource> OrderByDescending<TSource, TCaptureKey, TKey>(
            this ConfiguredAsyncEnumerable<TSource> configuredSource,
            TCaptureKey keyCaptureValue,
            Func<TCaptureKey, TSource, TKey> keySelector,
            IComparer<TKey> comparer)
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);

            return Internal.OrderHelper<TSource, TKey>.OrderBy(configuredSource.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(keyCaptureValue, keySelector), new Internal.ReverseComparer<TKey>(comparer ?? Comparer<TKey>.Default));
        }

        /// <summary>
        /// Sorts the elements of a configured async-enumerable sequence in descending order according to a specified key selector function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by the <paramref name="keySelector"/>.</typeparam>
        /// <param name="configuredSource">A configured async-enumerable sequence to sort.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from an element.</param>
        /// <returns>An <see cref="OrderedAsyncEnumerable{T}"/> whose elements are sorted according to a key.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        public static OrderedAsyncEnumerable<TSource> OrderByDescending<TSource, TKey>(
            this ConfiguredAsyncEnumerable<TSource> configuredSource,
            Func<TSource, Promise<TKey>> keySelector)
            => OrderByDescending<TSource, TKey>(configuredSource, keySelector, comparer: null);

        /// <summary>
        /// Sorts the elements of a configured async-enumerable sequence in descending order according to a specified key selector function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to the key selector.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by the <paramref name="keySelector"/>.</typeparam>
        /// <param name="configuredSource">A configured async-enumerable sequence to sort.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from an element.</param>
        /// <returns>An <see cref="OrderedAsyncEnumerable{T}"/> whose elements are sorted according to a key.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        public static OrderedAsyncEnumerable<TSource> OrderByDescending<TSource, TCaptureKey, TKey>(
            this ConfiguredAsyncEnumerable<TSource> configuredSource,
            TCaptureKey keyCaptureValue,
            Func<TCaptureKey, TSource, Promise<TKey>> keySelector)
            => OrderByDescending<TSource, TCaptureKey, TKey>(configuredSource, keyCaptureValue, keySelector, comparer: null);

        /// <summary>
        /// Sorts the elements of a configured async-enumerable sequence in descending order according to a specified key selector function, and a comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by the <paramref name="keySelector"/>.</typeparam>
        /// <param name="configuredSource">A configured async-enumerable sequence to sort.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from an element.</param>
        /// <param name="comparer">A comparer to compare keys. If null, the default comparer will be used.</param>
        /// <returns>An <see cref="OrderedAsyncEnumerable{T}"/> whose elements are sorted according to a key.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        public static OrderedAsyncEnumerable<TSource> OrderByDescending<TSource, TKey>(
            this ConfiguredAsyncEnumerable<TSource> configuredSource,
            Func<TSource, Promise<TKey>> keySelector,
            IComparer<TKey> comparer)
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);

            return Internal.OrderHelper<TSource, TKey>.OrderByAwait(configuredSource.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(keySelector), new Internal.ReverseComparer<TKey>(comparer ?? Comparer<TKey>.Default));
        }

        /// <summary>
        /// Sorts the elements of a configured async-enumerable sequence in descending order according to a specified key selector function, and a comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to the key selector.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by the <paramref name="keySelector"/>.</typeparam>
        /// <param name="configuredSource">A configured async-enumerable sequence to sort.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from an element.</param>
        /// <param name="comparer">A comparer to compare keys. If null, the default comparer will be used.</param>
        /// <returns>An <see cref="OrderedAsyncEnumerable{T}"/> whose elements are sorted according to a key.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        public static OrderedAsyncEnumerable<TSource> OrderByDescending<TSource, TCaptureKey, TKey>(
            this ConfiguredAsyncEnumerable<TSource> configuredSource,
            TCaptureKey keyCaptureValue,
            Func<TCaptureKey, TSource, Promise<TKey>> keySelector,
            IComparer<TKey> comparer)
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);

            return Internal.OrderHelper<TSource, TKey>.OrderByAwait(configuredSource.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(keyCaptureValue, keySelector), new Internal.ReverseComparer<TKey>(comparer ?? Comparer<TKey>.Default));
        }
        #endregion OrderBy

        #region Thenby
        /// <summary>
        /// Performs a subsequent ordering of the elements in an async-enumerable sequence in ascending order according to a specified key selector function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by the <paramref name="keySelector"/>.</typeparam>
        /// <param name="source">An ordered async-enumerable sequence that contains elements to sort.</param>
        /// <param name="keySelector">A function to extract a key from an element.</param>
        /// <returns>An <see cref="OrderedAsyncEnumerable{T}"/> whose elements are sorted according to a key.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        public static OrderedAsyncEnumerable<TSource> ThenBy<TSource, TKey>(
            this OrderedAsyncEnumerable<TSource> source,
            Func<TSource, TKey> keySelector)
            => ThenBy(source, keySelector, comparer: null);

        /// <summary>
        /// Performs a subsequent ordering of the elements in an async-enumerable sequence in ascending order according to a specified key selector function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to the key selector.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by the <paramref name="keySelector"/>.</typeparam>
        /// <param name="source">An ordered async-enumerable sequence that contains elements to sort.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">A function to extract a key from an element.</param>
        /// <returns>An <see cref="OrderedAsyncEnumerable{T}"/> whose elements are sorted according to a key.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        public static OrderedAsyncEnumerable<TSource> ThenBy<TSource, TCaptureKey, TKey>(
            this OrderedAsyncEnumerable<TSource> source,
            TCaptureKey keyCaptureValue,
            Func<TCaptureKey, TSource, TKey> keySelector)
            => ThenBy(source, keyCaptureValue, keySelector, comparer: null);

        /// <summary>
        /// Performs a subsequent ordering of the elements in an async-enumerable sequence in ascending order according to a specified key selector function, and a comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by the <paramref name="keySelector"/>.</typeparam>
        /// <param name="source">An ordered async-enumerable sequence that contains elements to sort.</param>
        /// <param name="keySelector">A function to extract a key from an element.</param>
        /// <param name="comparer">A comparer to compare keys. If null, the default comparer will be used.</param>
        /// <returns>An <see cref="OrderedAsyncEnumerable{T}"/> whose elements are sorted according to a key.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        public static OrderedAsyncEnumerable<TSource> ThenBy<TSource, TKey>(
            this OrderedAsyncEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            IComparer<TKey> comparer)
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);

            return Internal.OrderHelper<TSource, TKey>.ThenBy(source, Internal.PromiseRefBase.DelegateWrapper.Create(keySelector), comparer ?? Comparer<TKey>.Default);
        }

        /// <summary>
        /// Performs a subsequent ordering of the elements in an async-enumerable sequence in ascending order according to a specified key selector function, and a comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to the key selector.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by the <paramref name="keySelector"/>.</typeparam>
        /// <param name="source">An ordered async-enumerable sequence that contains elements to sort.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">A function to extract a key from an element.</param>
        /// <param name="comparer">A comparer to compare keys. If null, the default comparer will be used.</param>
        /// <returns>An <see cref="OrderedAsyncEnumerable{T}"/> whose elements are sorted according to a key.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        public static OrderedAsyncEnumerable<TSource> ThenBy<TSource, TCaptureKey, TKey>(
            this OrderedAsyncEnumerable<TSource> source,
            TCaptureKey keyCaptureValue,
            Func<TCaptureKey, TSource, TKey> keySelector,
            IComparer<TKey> comparer)
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);

            return Internal.OrderHelper<TSource, TKey>.ThenBy(source, Internal.PromiseRefBase.DelegateWrapper.Create(keyCaptureValue, keySelector), comparer ?? Comparer<TKey>.Default);
        }

        /// <summary>
        /// Performs a subsequent ordering of the elements in an async-enumerable sequence in ascending order according to a specified key selector function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by the <paramref name="keySelector"/>.</typeparam>
        /// <param name="source">An ordered async-enumerable sequence that contains elements to sort.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from an element.</param>
        /// <returns>An <see cref="OrderedAsyncEnumerable{T}"/> whose elements are sorted according to a key.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        public static OrderedAsyncEnumerable<TSource> ThenBy<TSource, TKey>(
            this OrderedAsyncEnumerable<TSource> source,
            Func<TSource, Promise<TKey>> keySelector)
            => ThenBy<TSource, TKey>(source, keySelector, comparer: null);

        /// <summary>
        /// Performs a subsequent ordering of the elements in an async-enumerable sequence in ascending order according to a specified key selector function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to the key selector.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by the <paramref name="keySelector"/>.</typeparam>
        /// <param name="source">An ordered async-enumerable sequence that contains elements to sort.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from an element.</param>
        /// <returns>An <see cref="OrderedAsyncEnumerable{T}"/> whose elements are sorted according to a key.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        public static OrderedAsyncEnumerable<TSource> ThenBy<TSource, TCaptureKey, TKey>(
            this OrderedAsyncEnumerable<TSource> source,
            TCaptureKey keyCaptureValue,
            Func<TCaptureKey, TSource, Promise<TKey>> keySelector)
            => ThenBy<TSource, TCaptureKey, TKey>(source, keyCaptureValue, keySelector, comparer: null);

        /// <summary>
        /// Performs a subsequent ordering of the elements in an async-enumerable sequence in ascending order according to a specified key selector function, and a comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by the <paramref name="keySelector"/>.</typeparam>
        /// <param name="source">An ordered async-enumerable sequence that contains elements to sort.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from an element.</param>
        /// <param name="comparer">A comparer to compare keys. If null, the default comparer will be used.</param>
        /// <returns>An <see cref="OrderedAsyncEnumerable{T}"/> whose elements are sorted according to a key.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        public static OrderedAsyncEnumerable<TSource> ThenBy<TSource, TKey>(
            this OrderedAsyncEnumerable<TSource> source,
            Func<TSource, Promise<TKey>> keySelector,
            IComparer<TKey> comparer)
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);

            return Internal.OrderHelper<TSource, TKey>.ThenByAwait(source, Internal.PromiseRefBase.DelegateWrapper.Create(keySelector), comparer ?? Comparer<TKey>.Default);
        }

        /// <summary>
        /// Performs a subsequent ordering of the elements in an async-enumerable sequence in ascending order according to a specified key selector function, and a comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to the key selector.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by the <paramref name="keySelector"/>.</typeparam>
        /// <param name="source">An ordered async-enumerable sequence that contains elements to sort.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from an element.</param>
        /// <param name="comparer">A comparer to compare keys. If null, the default comparer will be used.</param>
        /// <returns>An <see cref="OrderedAsyncEnumerable{T}"/> whose elements are sorted according to a key.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        public static OrderedAsyncEnumerable<TSource> ThenBy<TSource, TCaptureKey, TKey>(
            this OrderedAsyncEnumerable<TSource> source,
            TCaptureKey keyCaptureValue,
            Func<TCaptureKey, TSource, Promise<TKey>> keySelector,
            IComparer<TKey> comparer)
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);

            return Internal.OrderHelper<TSource, TKey>.ThenByAwait(source, Internal.PromiseRefBase.DelegateWrapper.Create(keyCaptureValue, keySelector), comparer ?? Comparer<TKey>.Default);
        }

        /// <summary>
        /// Performs a subsequent ordering of the elements in an async-enumerable sequence in descending order according to a specified key selector function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by the <paramref name="keySelector"/>.</typeparam>
        /// <param name="source">An ordered async-enumerable sequence that contains elements to sort.</param>
        /// <param name="keySelector">A function to extract a key from an element.</param>
        /// <returns>An <see cref="OrderedAsyncEnumerable{T}"/> whose elements are sorted according to a key.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        public static OrderedAsyncEnumerable<TSource> ThenByDescending<TSource, TKey>(
            this OrderedAsyncEnumerable<TSource> source,
            Func<TSource, TKey> keySelector)
            => ThenByDescending(source, keySelector, comparer: null);

        /// <summary>
        /// Performs a subsequent ordering of the elements in an async-enumerable sequence in descending order according to a specified key selector function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to the key selector.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by the <paramref name="keySelector"/>.</typeparam>
        /// <param name="source">An ordered async-enumerable sequence that contains elements to sort.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">A function to extract a key from an element.</param>
        /// <returns>An <see cref="OrderedAsyncEnumerable{T}"/> whose elements are sorted according to a key.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        public static OrderedAsyncEnumerable<TSource> ThenByDescending<TSource, TCaptureKey, TKey>(
            this OrderedAsyncEnumerable<TSource> source,
            TCaptureKey keyCaptureValue,
            Func<TCaptureKey, TSource, TKey> keySelector)
            => ThenByDescending(source, keyCaptureValue, keySelector, comparer: null);

        /// <summary>
        /// Performs a subsequent ordering of the elements in an async-enumerable sequence in descending order according to a specified key selector function, and a comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by the <paramref name="keySelector"/>.</typeparam>
        /// <param name="source">An ordered async-enumerable sequence that contains elements to sort.</param>
        /// <param name="keySelector">A function to extract a key from an element.</param>
        /// <param name="comparer">A comparer to compare keys. If null, the default comparer will be used.</param>
        /// <returns>An <see cref="OrderedAsyncEnumerable{T}"/> whose elements are sorted according to a key.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        public static OrderedAsyncEnumerable<TSource> ThenByDescending<TSource, TKey>(
            this OrderedAsyncEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            IComparer<TKey> comparer)
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);

            return Internal.OrderHelper<TSource, TKey>.ThenBy(source, Internal.PromiseRefBase.DelegateWrapper.Create(keySelector), new Internal.ReverseComparer<TKey>(comparer ?? Comparer<TKey>.Default));
        }

        /// <summary>
        /// Performs a subsequent ordering of the elements in an async-enumerable sequence in descending order according to a specified key selector function, and a comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to the key selector.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by the <paramref name="keySelector"/>.</typeparam>
        /// <param name="source">An ordered async-enumerable sequence that contains elements to sort.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">A function to extract a key from an element.</param>
        /// <param name="comparer">A comparer to compare keys. If null, the default comparer will be used.</param>
        /// <returns>An <see cref="OrderedAsyncEnumerable{T}"/> whose elements are sorted according to a key.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        public static OrderedAsyncEnumerable<TSource> ThenByDescending<TSource, TCaptureKey, TKey>(
            this OrderedAsyncEnumerable<TSource> source,
            TCaptureKey keyCaptureValue,
            Func<TCaptureKey, TSource, TKey> keySelector,
            IComparer<TKey> comparer)
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);

            return Internal.OrderHelper<TSource, TKey>.ThenBy(source, Internal.PromiseRefBase.DelegateWrapper.Create(keyCaptureValue, keySelector), new Internal.ReverseComparer<TKey>(comparer ?? Comparer<TKey>.Default));
        }

        /// <summary>
        /// Performs a subsequent ordering of the elements in an async-enumerable sequence in descending order according to a specified key selector function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by the <paramref name="keySelector"/>.</typeparam>
        /// <param name="source">An ordered async-enumerable sequence that contains elements to sort.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from an element.</param>
        /// <returns>An <see cref="OrderedAsyncEnumerable{T}"/> whose elements are sorted according to a key.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        public static OrderedAsyncEnumerable<TSource> ThenByDescending<TSource, TKey>(
            this OrderedAsyncEnumerable<TSource> source,
            Func<TSource, Promise<TKey>> keySelector)
            => ThenByDescending<TSource, TKey>(source, keySelector, comparer: null);

        /// <summary>
        /// Performs a subsequent ordering of the elements in an async-enumerable sequence in descending order according to a specified key selector function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to the key selector.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by the <paramref name="keySelector"/>.</typeparam>
        /// <param name="source">An ordered async-enumerable sequence that contains elements to sort.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from an element.</param>
        /// <returns>An <see cref="OrderedAsyncEnumerable{T}"/> whose elements are sorted according to a key.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        public static OrderedAsyncEnumerable<TSource> ThenByDescending<TSource, TCaptureKey, TKey>(
            this OrderedAsyncEnumerable<TSource> source,
            TCaptureKey keyCaptureValue,
            Func<TCaptureKey, TSource, Promise<TKey>> keySelector)
            => ThenByDescending<TSource, TCaptureKey, TKey>(source, keyCaptureValue, keySelector, comparer: null);

        /// <summary>
        /// Performs a subsequent ordering of the elements in an async-enumerable sequence in descending order according to a specified key selector function, and a comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by the <paramref name="keySelector"/>.</typeparam>
        /// <param name="source">An ordered async-enumerable sequence that contains elements to sort.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from an element.</param>
        /// <param name="comparer">A comparer to compare keys. If null, the default comparer will be used.</param>
        /// <returns>An <see cref="OrderedAsyncEnumerable{T}"/> whose elements are sorted according to a key.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        public static OrderedAsyncEnumerable<TSource> ThenByDescending<TSource, TKey>(
            this OrderedAsyncEnumerable<TSource> source,
            Func<TSource, Promise<TKey>> keySelector,
            IComparer<TKey> comparer)
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);

            return Internal.OrderHelper<TSource, TKey>.ThenByAwait(source, Internal.PromiseRefBase.DelegateWrapper.Create(keySelector), new Internal.ReverseComparer<TKey>(comparer ?? Comparer<TKey>.Default));
        }

        /// <summary>
        /// Performs a subsequent ordering of the elements in an async-enumerable sequence in descending order according to a specified key selector function, and a comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to the key selector.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by the <paramref name="keySelector"/>.</typeparam>
        /// <param name="source">An ordered async-enumerable sequence that contains elements to sort.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from an element.</param>
        /// <param name="comparer">A comparer to compare keys. If null, the default comparer will be used.</param>
        /// <returns>An <see cref="OrderedAsyncEnumerable{T}"/> whose elements are sorted according to a key.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        public static OrderedAsyncEnumerable<TSource> ThenByDescending<TSource, TCaptureKey, TKey>(
            this OrderedAsyncEnumerable<TSource> source,
            TCaptureKey keyCaptureValue,
            Func<TCaptureKey, TSource, Promise<TKey>> keySelector,
            IComparer<TKey> comparer)
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);

            return Internal.OrderHelper<TSource, TKey>.ThenByAwait(source, Internal.PromiseRefBase.DelegateWrapper.Create(keyCaptureValue, keySelector), new Internal.ReverseComparer<TKey>(comparer ?? Comparer<TKey>.Default));
        }
        #endregion ThenBy
    }
#endif // CSHARP_7_3_OR_NEWER
}