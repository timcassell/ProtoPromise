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
        #region Order
        /// <summary>
        /// Sorts the elements of an async-enumerable sequence in ascending order.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <param name="source">An async-enumerable sequence to sort.</param>
        /// <returns>An <see cref="OrderedAsyncEnumerable{T}"/> whose elements are sorted.</returns>
        public static OrderedAsyncEnumerable<TSource> Order<TSource>(this AsyncEnumerable<TSource> source)
            => Order(source, Comparer<TSource>.Default);

        /// <summary>
        /// Sorts the elements of an async-enumerable sequence in ascending order according to a specified comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TComparer">The type of the comparer.</typeparam>
        /// <param name="source">An async-enumerable sequence to sort.</param>
        /// <param name="comparer">A comparer to compare keys.</param>
        /// <returns>An <see cref="OrderedAsyncEnumerable{T}"/> whose elements are sorted.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="comparer"/> is null.</exception>
        public static OrderedAsyncEnumerable<TSource> Order<TSource, TComparer>(this AsyncEnumerable<TSource> source, TComparer comparer)
            where TComparer : IComparer<TSource>
        {
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.OrderHelper<TSource>.Order(source.GetAsyncEnumerator(), comparer);
        }

        /// <summary>
        /// Sorts the elements of a configured async-enumerable sequence in ascending order.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <param name="configuredSource">A configured async-enumerable sequence to sort.</param>
        /// <returns>An <see cref="OrderedAsyncEnumerable{T}"/> whose elements are sorted.</returns>
        public static OrderedAsyncEnumerable<TSource> Order<TSource>(this in ConfiguredAsyncEnumerable<TSource> configuredSource)
            => Order(configuredSource, Comparer<TSource>.Default);

        /// <summary>
        /// Sorts the elements of a configured async-enumerable sequence in ascending order according to a specified comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TComparer">The type of the comparer.</typeparam>
        /// <param name="configuredSource">A configured async-enumerable sequence to sort.</param>
        /// <param name="comparer">A comparer to compare keys.</param>
        /// <returns>An <see cref="OrderedAsyncEnumerable{T}"/> whose elements are sorted.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="comparer"/> is null.</exception>
        public static OrderedAsyncEnumerable<TSource> Order<TSource, TComparer>(this in ConfiguredAsyncEnumerable<TSource> configuredSource, TComparer comparer)
            where TComparer : IComparer<TSource>
        {
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.OrderHelper<TSource>.Order(configuredSource.GetAsyncEnumerator(), comparer);
        }

        /// <summary>
        /// Sorts the elements of an async-enumerable sequence in descending order.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <param name="source">An async-enumerable sequence to sort.</param>
        /// <returns>An <see cref="OrderedAsyncEnumerable{T}"/> whose elements are sorted.</returns>
        public static OrderedAsyncEnumerable<TSource> OrderDescending<TSource>(this AsyncEnumerable<TSource> source)
            => OrderDescending(source, Comparer<TSource>.Default);

        /// <summary>
        /// Sorts the elements of an async-enumerable sequence in descending order according to a specified comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TComparer">The type of the comparer.</typeparam>
        /// <param name="source">An async-enumerable sequence to sort.</param>
        /// <param name="comparer">A comparer to compare keys.</param>
        /// <returns>An <see cref="OrderedAsyncEnumerable{T}"/> whose elements are sorted.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="comparer"/> is null.</exception>
        public static OrderedAsyncEnumerable<TSource> OrderDescending<TSource, TComparer>(this AsyncEnumerable<TSource> source, TComparer comparer)
            where TComparer : IComparer<TSource>
        {
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.OrderHelper<TSource>.Order(source.GetAsyncEnumerator(), new Internal.ReverseComparer<TSource, TComparer>(comparer));
        }

        /// <summary>
        /// Sorts the elements of a configured async-enumerable sequence in descending order.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <param name="configuredSource">A configured async-enumerable sequence to sort.</param>
        /// <returns>An <see cref="OrderedAsyncEnumerable{T}"/> whose elements are sorted.</returns>
        public static OrderedAsyncEnumerable<TSource> OrderDescending<TSource>(this in ConfiguredAsyncEnumerable<TSource> configuredSource)
            => OrderDescending(configuredSource, Comparer<TSource>.Default);

        /// <summary>
        /// Sorts the elements of a configured async-enumerable sequence in descending order according to a specified comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TComparer">The type of the comparer.</typeparam>
        /// <param name="configuredSource">A configured async-enumerable sequence to sort.</param>
        /// <param name="comparer">A comparer to compare keys.</param>
        /// <returns>An <see cref="OrderedAsyncEnumerable{T}"/> whose elements are sorted.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="comparer"/> is null.</exception>
        public static OrderedAsyncEnumerable<TSource> OrderDescending<TSource, TComparer>(this in ConfiguredAsyncEnumerable<TSource> configuredSource, TComparer comparer)
            where TComparer : IComparer<TSource>
        {
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.OrderHelper<TSource>.Order(configuredSource.GetAsyncEnumerator(), new Internal.ReverseComparer<TSource, TComparer>(comparer));
        }
        #endregion Order

        #region OrderBy
        /// <summary>
        /// Sorts the elements of an async-enumerable sequence in ascending order according to a specified key selector function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by the <paramref name="keySelector"/>.</typeparam>
        /// <param name="source">An async-enumerable sequence to sort.</param>
        /// <param name="keySelector">A function to extract a key from an element.</param>
        /// <returns>An <see cref="OrderedAsyncEnumerable{T}"/> whose elements are sorted.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        public static OrderedAsyncEnumerable<TSource> OrderBy<TSource, TKey>(
            this AsyncEnumerable<TSource> source,
            Func<TSource, TKey> keySelector)
            => OrderBy(source, keySelector, Comparer<TKey>.Default);

        /// <summary>
        /// Sorts the elements of an async-enumerable sequence in ascending order according to a specified key selector function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to the key selector.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by the <paramref name="keySelector"/>.</typeparam>
        /// <param name="source">An async-enumerable sequence to sort.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">A function to extract a key from an element.</param>
        /// <returns>An <see cref="OrderedAsyncEnumerable{T}"/> whose elements are sorted.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        public static OrderedAsyncEnumerable<TSource> OrderBy<TSource, TCaptureKey, TKey>(
            this AsyncEnumerable<TSource> source,
            TCaptureKey keyCaptureValue,
            Func<TCaptureKey, TSource, TKey> keySelector)
            => OrderBy(source, keyCaptureValue, keySelector, Comparer<TKey>.Default);

        /// <summary>
        /// Sorts the elements of an async-enumerable sequence in ascending order according to a specified key selector function, and a comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by the <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TComparer">The type of the comparer.</typeparam>
        /// <param name="source">An async-enumerable sequence to sort.</param>
        /// <param name="keySelector">A function to extract a key from an element.</param>
        /// <param name="comparer">A comparer to compare keys.</param>
        /// <returns>An <see cref="OrderedAsyncEnumerable{T}"/> whose elements are sorted.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="comparer"/> is null.</exception>
        public static OrderedAsyncEnumerable<TSource> OrderBy<TSource, TKey, TComparer>(
            this AsyncEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            TComparer comparer)
            where TComparer : IComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.OrderHelper<TSource, TKey>.OrderBy(source.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(keySelector), comparer);
        }

        /// <summary>
        /// Sorts the elements of an async-enumerable sequence in ascending order according to a specified key selector function, and a comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to the key selector.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by the <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TComparer">The type of the comparer.</typeparam>
        /// <param name="source">An async-enumerable sequence to sort.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">A function to extract a key from an element.</param>
        /// <param name="comparer">A comparer to compare keys.</param>
        /// <returns>An <see cref="OrderedAsyncEnumerable{T}"/> whose elements are sorted.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="comparer"/> is null.</exception>
        public static OrderedAsyncEnumerable<TSource> OrderBy<TSource, TCaptureKey, TKey, TComparer>(
            this AsyncEnumerable<TSource> source,
            TCaptureKey keyCaptureValue,
            Func<TCaptureKey, TSource, TKey> keySelector,
            TComparer comparer)
            where TComparer : IComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.OrderHelper<TSource, TKey>.OrderBy(source.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(keyCaptureValue, keySelector), comparer);
        }

        /// <summary>
        /// Sorts the elements of an async-enumerable sequence in ascending order according to a specified key selector function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by the <paramref name="keySelector"/>.</typeparam>
        /// <param name="source">An async-enumerable sequence to sort.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from an element.</param>
        /// <returns>An <see cref="OrderedAsyncEnumerable{T}"/> whose elements are sorted.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        public static OrderedAsyncEnumerable<TSource> OrderBy<TSource, TKey>(
            this AsyncEnumerable<TSource> source,
            Func<TSource, Promise<TKey>> keySelector)
            => OrderBy<TSource, TKey, IComparer<TKey>>(source, keySelector, Comparer<TKey>.Default);

        /// <summary>
        /// Sorts the elements of an async-enumerable sequence in ascending order according to a specified key selector function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to the key selector.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by the <paramref name="keySelector"/>.</typeparam>
        /// <param name="source">An async-enumerable sequence to sort.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from an element.</param>
        /// <returns>An <see cref="OrderedAsyncEnumerable{T}"/> whose elements are sorted.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        public static OrderedAsyncEnumerable<TSource> OrderBy<TSource, TCaptureKey, TKey>(
            this AsyncEnumerable<TSource> source,
            TCaptureKey keyCaptureValue,
            Func<TCaptureKey, TSource, Promise<TKey>> keySelector)
            => OrderBy<TSource, TCaptureKey, TKey, IComparer<TKey>>(source, keyCaptureValue, keySelector, Comparer<TKey>.Default);

        /// <summary>
        /// Sorts the elements of an async-enumerable sequence in ascending order according to a specified key selector function, and a comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by the <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TComparer">The type of the comparer.</typeparam>
        /// <param name="source">An async-enumerable sequence to sort.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from an element.</param>
        /// <param name="comparer">A comparer to compare keys.</param>
        /// <returns>An <see cref="OrderedAsyncEnumerable{T}"/> whose elements are sorted.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="comparer"/> is null.</exception>
        public static OrderedAsyncEnumerable<TSource> OrderBy<TSource, TKey, TComparer>(
            this AsyncEnumerable<TSource> source,
            Func<TSource, Promise<TKey>> keySelector,
            TComparer comparer)
            where TComparer : IComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.OrderHelper<TSource, TKey>.OrderByAwait(source.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(keySelector), comparer);
        }

        /// <summary>
        /// Sorts the elements of an async-enumerable sequence in ascending order according to a specified key selector function, and a comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to the key selector.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by the <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TComparer">The type of the comparer.</typeparam>
        /// <param name="source">An async-enumerable sequence to sort.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from an element.</param>
        /// <param name="comparer">A comparer to compare keys.</param>
        /// <returns>An <see cref="OrderedAsyncEnumerable{T}"/> whose elements are sorted.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="comparer"/> is null.</exception>
        public static OrderedAsyncEnumerable<TSource> OrderBy<TSource, TCaptureKey, TKey, TComparer>(
            this AsyncEnumerable<TSource> source,
            TCaptureKey keyCaptureValue,
            Func<TCaptureKey, TSource, Promise<TKey>> keySelector,
            TComparer comparer)
            where TComparer : IComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.OrderHelper<TSource, TKey>.OrderByAwait(source.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(keyCaptureValue, keySelector), comparer);
        }

        /// <summary>
        /// Sorts the elements of a configured async-enumerable sequence in ascending order according to a specified key selector function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by the <paramref name="keySelector"/>.</typeparam>
        /// <param name="configuredSource">A configured async-enumerable sequence to sort.</param>
        /// <param name="keySelector">A function to extract a key from an element.</param>
        /// <returns>An <see cref="OrderedAsyncEnumerable{T}"/> whose elements are sorted.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        public static OrderedAsyncEnumerable<TSource> OrderBy<TSource, TKey>(
            this in ConfiguredAsyncEnumerable<TSource> configuredSource,
            Func<TSource, TKey> keySelector)
            => OrderBy(configuredSource, keySelector, Comparer<TKey>.Default);

        /// <summary>
        /// Sorts the elements of a configured async-enumerable sequence in ascending order according to a specified key selector function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to the key selector.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by the <paramref name="keySelector"/>.</typeparam>
        /// <param name="configuredSource">A configured async-enumerable sequence to sort.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">A function to extract a key from an element.</param>
        /// <returns>An <see cref="OrderedAsyncEnumerable{T}"/> whose elements are sorted.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        public static OrderedAsyncEnumerable<TSource> OrderBy<TSource, TCaptureKey, TKey>(
            this in ConfiguredAsyncEnumerable<TSource> configuredSource,
            TCaptureKey keyCaptureValue,
            Func<TCaptureKey, TSource, TKey> keySelector)
            => OrderBy(configuredSource, keyCaptureValue, keySelector, Comparer<TKey>.Default);

        /// <summary>
        /// Sorts the elements of a configured async-enumerable sequence in ascending order according to a specified key selector function, and a comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by the <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TComparer">The type of the comparer.</typeparam>
        /// <param name="configuredSource">A configured async-enumerable sequence to sort.</param>
        /// <param name="keySelector">A function to extract a key from an element.</param>
        /// <param name="comparer">A comparer to compare keys.</param>
        /// <returns>An <see cref="OrderedAsyncEnumerable{T}"/> whose elements are sorted.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="comparer"/> is null.</exception>
        public static OrderedAsyncEnumerable<TSource> OrderBy<TSource, TKey, TComparer>(
            this in ConfiguredAsyncEnumerable<TSource> configuredSource,
            Func<TSource, TKey> keySelector,
            TComparer comparer)
            where TComparer : IComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.OrderHelper<TSource, TKey>.OrderBy(configuredSource.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(keySelector), comparer);
        }

        /// <summary>
        /// Sorts the elements of a configured async-enumerable sequence in ascending order according to a specified key selector function, and a comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to the key selector.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by the <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TComparer">The type of the comparer.</typeparam>
        /// <param name="configuredSource">A configured async-enumerable sequence to sort.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from an element.</param>
        /// <param name="comparer">A comparer to compare keys.</param>
        /// <returns>An <see cref="OrderedAsyncEnumerable{T}"/> whose elements are sorted.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="comparer"/> is null.</exception>
        public static OrderedAsyncEnumerable<TSource> OrderBy<TSource, TCaptureKey, TKey, TComparer>(
            this in ConfiguredAsyncEnumerable<TSource> configuredSource,
            TCaptureKey keyCaptureValue,
            Func<TCaptureKey, TSource, TKey> keySelector,
            TComparer comparer)
            where TComparer : IComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.OrderHelper<TSource, TKey>.OrderBy(configuredSource.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(keyCaptureValue, keySelector), comparer);
        }

        /// <summary>
        /// Sorts the elements of a configured async-enumerable sequence in ascending order according to a specified key selector function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by the <paramref name="keySelector"/>.</typeparam>
        /// <param name="configuredSource">A configured async-enumerable sequence to sort.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from an element.</param>
        /// <returns>An <see cref="OrderedAsyncEnumerable{T}"/> whose elements are sorted.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        public static OrderedAsyncEnumerable<TSource> OrderBy<TSource, TKey>(
            this in ConfiguredAsyncEnumerable<TSource> configuredSource,
            Func<TSource, Promise<TKey>> keySelector)
            => OrderBy<TSource, TKey, IComparer<TKey>>(configuredSource, keySelector, Comparer<TKey>.Default);

        /// <summary>
        /// Sorts the elements of a configured async-enumerable sequence in ascending order according to a specified key selector function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to the key selector.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by the <paramref name="keySelector"/>.</typeparam>
        /// <param name="configuredSource">A configured async-enumerable sequence to sort.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from an element.</param>
        /// <returns>An <see cref="OrderedAsyncEnumerable{T}"/> whose elements are sorted.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        public static OrderedAsyncEnumerable<TSource> OrderBy<TSource, TCaptureKey, TKey>(
            this in ConfiguredAsyncEnumerable<TSource> configuredSource,
            TCaptureKey keyCaptureValue,
            Func<TCaptureKey, TSource, Promise<TKey>> keySelector)
            => OrderBy<TSource, TCaptureKey, TKey, IComparer<TKey>>(configuredSource, keyCaptureValue, keySelector, Comparer<TKey>.Default);

        /// <summary>
        /// Sorts the elements of a configured async-enumerable sequence in ascending order according to a specified key selector function, and a comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by the <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TComparer">The type of the comparer.</typeparam>
        /// <param name="configuredSource">A configured async-enumerable sequence to sort.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from an element.</param>
        /// <param name="comparer">A comparer to compare keys.</param>
        /// <returns>An <see cref="OrderedAsyncEnumerable{T}"/> whose elements are sorted.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="comparer"/> is null.</exception>
        public static OrderedAsyncEnumerable<TSource> OrderBy<TSource, TKey, TComparer>(
            this in ConfiguredAsyncEnumerable<TSource> configuredSource,
            Func<TSource, Promise<TKey>> keySelector,
            TComparer comparer)
            where TComparer : IComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.OrderHelper<TSource, TKey>.OrderByAwait(configuredSource.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(keySelector), comparer);
        }

        /// <summary>
        /// Sorts the elements of a configured async-enumerable sequence in ascending order according to a specified key selector function, and a comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to the key selector.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by the <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TComparer">The type of the comparer.</typeparam>
        /// <param name="configuredSource">A configured async-enumerable sequence to sort.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from an element.</param>
        /// <param name="comparer">A comparer to compare keys.</param>
        /// <returns>An <see cref="OrderedAsyncEnumerable{T}"/> whose elements are sorted.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="comparer"/> is null.</exception>
        public static OrderedAsyncEnumerable<TSource> OrderBy<TSource, TCaptureKey, TKey, TComparer>(
            this in ConfiguredAsyncEnumerable<TSource> configuredSource,
            TCaptureKey keyCaptureValue,
            Func<TCaptureKey, TSource, Promise<TKey>> keySelector,
            TComparer comparer)
            where TComparer : IComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.OrderHelper<TSource, TKey>.OrderByAwait(configuredSource.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(keyCaptureValue, keySelector), comparer);
        }

        /// <summary>
        /// Sorts the elements of an async-enumerable sequence in descending order according to a specified key selector function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by the <paramref name="keySelector"/>.</typeparam>
        /// <param name="source">An async-enumerable sequence to sort.</param>
        /// <param name="keySelector">A function to extract a key from an element.</param>
        /// <returns>An <see cref="OrderedAsyncEnumerable{T}"/> whose elements are sorted.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        public static OrderedAsyncEnumerable<TSource> OrderByDescending<TSource, TKey>(
            this AsyncEnumerable<TSource> source,
            Func<TSource, TKey> keySelector)
            => OrderByDescending(source, keySelector, Comparer<TKey>.Default);

        /// <summary>
        /// Sorts the elements of an async-enumerable sequence in descending order according to a specified key selector function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to the key selector.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by the <paramref name="keySelector"/>.</typeparam>
        /// <param name="source">An async-enumerable sequence to sort.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">A function to extract a key from an element.</param>
        /// <returns>An <see cref="OrderedAsyncEnumerable{T}"/> whose elements are sorted.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        public static OrderedAsyncEnumerable<TSource> OrderByDescending<TSource, TCaptureKey, TKey>(
            this AsyncEnumerable<TSource> source,
            TCaptureKey keyCaptureValue,
            Func<TCaptureKey, TSource, TKey> keySelector)
            => OrderByDescending(source, keyCaptureValue, keySelector, Comparer<TKey>.Default);

        /// <summary>
        /// Sorts the elements of an async-enumerable sequence in descending order according to a specified key selector function, and a comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by the <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TComparer">The type of the comparer.</typeparam>
        /// <param name="source">An async-enumerable sequence to sort.</param>
        /// <param name="keySelector">A function to extract a key from an element.</param>
        /// <param name="comparer">A comparer to compare keys.</param>
        /// <returns>An <see cref="OrderedAsyncEnumerable{T}"/> whose elements are sorted.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="comparer"/> is null.</exception>
        public static OrderedAsyncEnumerable<TSource> OrderByDescending<TSource, TKey, TComparer>(
            this AsyncEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            TComparer comparer)
            where TComparer : IComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.OrderHelper<TSource, TKey>.OrderBy(source.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(keySelector), new Internal.ReverseComparer<TKey, TComparer>(comparer));
        }

        /// <summary>
        /// Sorts the elements of an async-enumerable sequence in descending order according to a specified key selector function, and a comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to the key selector.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by the <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TComparer">The type of the comparer.</typeparam>
        /// <param name="source">An async-enumerable sequence to sort.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">A function to extract a key from an element.</param>
        /// <param name="comparer">A comparer to compare keys.</param>
        /// <returns>An <see cref="OrderedAsyncEnumerable{T}"/> whose elements are sorted.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="comparer"/> is null.</exception>
        public static OrderedAsyncEnumerable<TSource> OrderByDescending<TSource, TCaptureKey, TKey, TComparer>(
            this AsyncEnumerable<TSource> source,
            TCaptureKey keyCaptureValue,
            Func<TCaptureKey, TSource, TKey> keySelector,
            TComparer comparer)
            where TComparer : IComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.OrderHelper<TSource, TKey>.OrderBy(source.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(keyCaptureValue, keySelector), new Internal.ReverseComparer<TKey, TComparer>(comparer));
        }

        /// <summary>
        /// Sorts the elements of an async-enumerable sequence in descending order according to a specified key selector function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by the <paramref name="keySelector"/>.</typeparam>
        /// <param name="source">An async-enumerable sequence to sort.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from an element.</param>
        /// <returns>An <see cref="OrderedAsyncEnumerable{T}"/> whose elements are sorted.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        public static OrderedAsyncEnumerable<TSource> OrderByDescending<TSource, TKey>(
            this AsyncEnumerable<TSource> source,
            Func<TSource, Promise<TKey>> keySelector)
            => OrderByDescending<TSource, TKey, IComparer<TKey>>(source, keySelector, Comparer<TKey>.Default);

        /// <summary>
        /// Sorts the elements of an async-enumerable sequence in descending order according to a specified key selector function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to the key selector.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by the <paramref name="keySelector"/>.</typeparam>
        /// <param name="source">An async-enumerable sequence to sort.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from an element.</param>
        /// <returns>An <see cref="OrderedAsyncEnumerable{T}"/> whose elements are sorted.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        public static OrderedAsyncEnumerable<TSource> OrderByDescending<TSource, TCaptureKey, TKey>(
            this AsyncEnumerable<TSource> source,
            TCaptureKey keyCaptureValue,
            Func<TCaptureKey, TSource, Promise<TKey>> keySelector)
            => OrderByDescending<TSource, TCaptureKey, TKey, IComparer<TKey>>(source, keyCaptureValue, keySelector, Comparer<TKey>.Default);

        /// <summary>
        /// Sorts the elements of an async-enumerable sequence in descending order according to a specified key selector function, and a comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by the <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TComparer">The type of the comparer.</typeparam>
        /// <param name="source">An async-enumerable sequence to sort.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from an element.</param>
        /// <param name="comparer">A comparer to compare keys.</param>
        /// <returns>An <see cref="OrderedAsyncEnumerable{T}"/> whose elements are sorted.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="comparer"/> is null.</exception>
        public static OrderedAsyncEnumerable<TSource> OrderByDescending<TSource, TKey, TComparer>(
            this AsyncEnumerable<TSource> source,
            Func<TSource, Promise<TKey>> keySelector,
            TComparer comparer)
            where TComparer : IComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.OrderHelper<TSource, TKey>.OrderByAwait(source.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(keySelector), new Internal.ReverseComparer<TKey, TComparer>(comparer));
        }

        /// <summary>
        /// Sorts the elements of an async-enumerable sequence in descending order according to a specified key selector function, and a comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to the key selector.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by the <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TComparer">The type of the comparer.</typeparam>
        /// <param name="source">An async-enumerable sequence to sort.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from an element.</param>
        /// <param name="comparer">A comparer to compare keys.</param>
        /// <returns>An <see cref="OrderedAsyncEnumerable{T}"/> whose elements are sorted.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="comparer"/> is null.</exception>
        public static OrderedAsyncEnumerable<TSource> OrderByDescending<TSource, TCaptureKey, TKey, TComparer>(
            this AsyncEnumerable<TSource> source,
            TCaptureKey keyCaptureValue,
            Func<TCaptureKey, TSource, Promise<TKey>> keySelector,
            TComparer comparer)
            where TComparer : IComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.OrderHelper<TSource, TKey>.OrderByAwait(source.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(keyCaptureValue, keySelector), new Internal.ReverseComparer<TKey, TComparer>(comparer));
        }

        /// <summary>
        /// Sorts the elements of a configured async-enumerable sequence in descending order according to a specified key selector function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by the <paramref name="keySelector"/>.</typeparam>
        /// <param name="configuredSource">A configured async-enumerable sequence to sort.</param>
        /// <param name="keySelector">A function to extract a key from an element.</param>
        /// <returns>An <see cref="OrderedAsyncEnumerable{T}"/> whose elements are sorted.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        public static OrderedAsyncEnumerable<TSource> OrderByDescending<TSource, TKey>(
            this in ConfiguredAsyncEnumerable<TSource> configuredSource,
            Func<TSource, TKey> keySelector)
            => OrderByDescending(configuredSource, keySelector, Comparer<TKey>.Default);

        /// <summary>
        /// Sorts the elements of a configured async-enumerable sequence in descending order according to a specified key selector function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to the key selector.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by the <paramref name="keySelector"/>.</typeparam>
        /// <param name="configuredSource">A configured async-enumerable sequence to sort.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">A function to extract a key from an element.</param>
        /// <returns>An <see cref="OrderedAsyncEnumerable{T}"/> whose elements are sorted.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        public static OrderedAsyncEnumerable<TSource> OrderByDescending<TSource, TCaptureKey, TKey>(
            this in ConfiguredAsyncEnumerable<TSource> configuredSource,
            TCaptureKey keyCaptureValue,
            Func<TCaptureKey, TSource, TKey> keySelector)
            => OrderByDescending(configuredSource, keyCaptureValue, keySelector, Comparer<TKey>.Default);

        /// <summary>
        /// Sorts the elements of a configured async-enumerable sequence in descending order according to a specified key selector function, and a comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by the <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TComparer">The type of the comparer.</typeparam>
        /// <param name="configuredSource">A configured async-enumerable sequence to sort.</param>
        /// <param name="keySelector">A function to extract a key from an element.</param>
        /// <param name="comparer">A comparer to compare keys.</param>
        /// <returns>An <see cref="OrderedAsyncEnumerable{T}"/> whose elements are sorted.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="comparer"/> is null.</exception>
        public static OrderedAsyncEnumerable<TSource> OrderByDescending<TSource, TKey, TComparer>(
            this in ConfiguredAsyncEnumerable<TSource> configuredSource,
            Func<TSource, TKey> keySelector,
            TComparer comparer)
            where TComparer : IComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.OrderHelper<TSource, TKey>.OrderBy(configuredSource.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(keySelector), new Internal.ReverseComparer<TKey, TComparer>(comparer));
        }

        /// <summary>
        /// Sorts the elements of a configured async-enumerable sequence in descending order according to a specified key selector function, and a comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to the key selector.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by the <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TComparer">The type of the comparer.</typeparam>
        /// <param name="configuredSource">A configured async-enumerable sequence to sort.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from an element.</param>
        /// <param name="comparer">A comparer to compare keys.</param>
        /// <returns>An <see cref="OrderedAsyncEnumerable{T}"/> whose elements are sorted.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="comparer"/> is null.</exception>
        public static OrderedAsyncEnumerable<TSource> OrderByDescending<TSource, TCaptureKey, TKey, TComparer>(
            this in ConfiguredAsyncEnumerable<TSource> configuredSource,
            TCaptureKey keyCaptureValue,
            Func<TCaptureKey, TSource, TKey> keySelector,
            TComparer comparer)
            where TComparer : IComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.OrderHelper<TSource, TKey>.OrderBy(configuredSource.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(keyCaptureValue, keySelector), new Internal.ReverseComparer<TKey, TComparer>(comparer));
        }

        /// <summary>
        /// Sorts the elements of a configured async-enumerable sequence in descending order according to a specified key selector function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by the <paramref name="keySelector"/>.</typeparam>
        /// <param name="configuredSource">A configured async-enumerable sequence to sort.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from an element.</param>
        /// <returns>An <see cref="OrderedAsyncEnumerable{T}"/> whose elements are sorted.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        public static OrderedAsyncEnumerable<TSource> OrderByDescending<TSource, TKey>(
            this in ConfiguredAsyncEnumerable<TSource> configuredSource,
            Func<TSource, Promise<TKey>> keySelector)
            => OrderByDescending<TSource, TKey, IComparer<TKey>>(configuredSource, keySelector, Comparer<TKey>.Default);

        /// <summary>
        /// Sorts the elements of a configured async-enumerable sequence in descending order according to a specified key selector function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to the key selector.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by the <paramref name="keySelector"/>.</typeparam>
        /// <param name="configuredSource">A configured async-enumerable sequence to sort.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from an element.</param>
        /// <returns>An <see cref="OrderedAsyncEnumerable{T}"/> whose elements are sorted.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        public static OrderedAsyncEnumerable<TSource> OrderByDescending<TSource, TCaptureKey, TKey>(
            this in ConfiguredAsyncEnumerable<TSource> configuredSource,
            TCaptureKey keyCaptureValue,
            Func<TCaptureKey, TSource, Promise<TKey>> keySelector)
            => OrderByDescending<TSource, TCaptureKey, TKey, IComparer<TKey>>(configuredSource, keyCaptureValue, keySelector, Comparer<TKey>.Default);

        /// <summary>
        /// Sorts the elements of a configured async-enumerable sequence in descending order according to a specified key selector function, and a comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by the <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TComparer">The type of the comparer.</typeparam>
        /// <param name="configuredSource">A configured async-enumerable sequence to sort.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from an element.</param>
        /// <param name="comparer">A comparer to compare keys.</param>
        /// <returns>An <see cref="OrderedAsyncEnumerable{T}"/> whose elements are sorted.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="comparer"/> is null.</exception>
        public static OrderedAsyncEnumerable<TSource> OrderByDescending<TSource, TKey, TComparer>(
            this in ConfiguredAsyncEnumerable<TSource> configuredSource,
            Func<TSource, Promise<TKey>> keySelector,
            TComparer comparer)
            where TComparer : IComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.OrderHelper<TSource, TKey>.OrderByAwait(configuredSource.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(keySelector), new Internal.ReverseComparer<TKey, TComparer>(comparer));
        }

        /// <summary>
        /// Sorts the elements of a configured async-enumerable sequence in descending order according to a specified key selector function, and a comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to the key selector.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by the <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TComparer">The type of the comparer.</typeparam>
        /// <param name="configuredSource">A configured async-enumerable sequence to sort.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from an element.</param>
        /// <param name="comparer">A comparer to compare keys.</param>
        /// <returns>An <see cref="OrderedAsyncEnumerable{T}"/> whose elements are sorted.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="comparer"/> is null.</exception>
        public static OrderedAsyncEnumerable<TSource> OrderByDescending<TSource, TCaptureKey, TKey, TComparer>(
            this in ConfiguredAsyncEnumerable<TSource> configuredSource,
            TCaptureKey keyCaptureValue,
            Func<TCaptureKey, TSource, Promise<TKey>> keySelector,
            TComparer comparer)
            where TComparer : IComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.OrderHelper<TSource, TKey>.OrderByAwait(configuredSource.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(keyCaptureValue, keySelector), new Internal.ReverseComparer<TKey, TComparer>(comparer));
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
        /// <returns>An <see cref="OrderedAsyncEnumerable{T}"/> whose elements are sorted.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        public static OrderedAsyncEnumerable<TSource> ThenBy<TSource, TKey>(
            this OrderedAsyncEnumerable<TSource> source,
            Func<TSource, TKey> keySelector)
            => ThenBy(source, keySelector, Comparer<TKey>.Default);

        /// <summary>
        /// Performs a subsequent ordering of the elements in an async-enumerable sequence in ascending order according to a specified key selector function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to the key selector.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by the <paramref name="keySelector"/>.</typeparam>
        /// <param name="source">An ordered async-enumerable sequence that contains elements to sort.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">A function to extract a key from an element.</param>
        /// <returns>An <see cref="OrderedAsyncEnumerable{T}"/> whose elements are sorted.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        public static OrderedAsyncEnumerable<TSource> ThenBy<TSource, TCaptureKey, TKey>(
            this OrderedAsyncEnumerable<TSource> source,
            TCaptureKey keyCaptureValue,
            Func<TCaptureKey, TSource, TKey> keySelector)
            => ThenBy(source, keyCaptureValue, keySelector, Comparer<TKey>.Default);

        /// <summary>
        /// Performs a subsequent ordering of the elements in an async-enumerable sequence in ascending order according to a specified key selector function, and a comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by the <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TComparer">The type of the comparer.</typeparam>
        /// <param name="source">An ordered async-enumerable sequence that contains elements to sort.</param>
        /// <param name="keySelector">A function to extract a key from an element.</param>
        /// <param name="comparer">A comparer to compare keys.</param>
        /// <returns>An <see cref="OrderedAsyncEnumerable{T}"/> whose elements are sorted.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="comparer"/> is null.</exception>
        public static OrderedAsyncEnumerable<TSource> ThenBy<TSource, TKey, TComparer>(
            this OrderedAsyncEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            TComparer comparer)
            where TComparer : IComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.OrderHelper<TSource, TKey>.ThenBy(source, Internal.PromiseRefBase.DelegateWrapper.Create(keySelector), comparer);
        }

        /// <summary>
        /// Performs a subsequent ordering of the elements in an async-enumerable sequence in ascending order according to a specified key selector function, and a comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to the key selector.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by the <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TComparer">The type of the comparer.</typeparam>
        /// <param name="source">An ordered async-enumerable sequence that contains elements to sort.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">A function to extract a key from an element.</param>
        /// <param name="comparer">A comparer to compare keys.</param>
        /// <returns>An <see cref="OrderedAsyncEnumerable{T}"/> whose elements are sorted.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="comparer"/> is null.</exception>
        public static OrderedAsyncEnumerable<TSource> ThenBy<TSource, TCaptureKey, TKey, TComparer>(
            this OrderedAsyncEnumerable<TSource> source,
            TCaptureKey keyCaptureValue,
            Func<TCaptureKey, TSource, TKey> keySelector,
            TComparer comparer)
            where TComparer : IComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.OrderHelper<TSource, TKey>.ThenBy(source, Internal.PromiseRefBase.DelegateWrapper.Create(keyCaptureValue, keySelector), comparer);
        }

        /// <summary>
        /// Performs a subsequent ordering of the elements in an async-enumerable sequence in ascending order according to a specified key selector function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by the <paramref name="keySelector"/>.</typeparam>
        /// <param name="source">An ordered async-enumerable sequence that contains elements to sort.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from an element.</param>
        /// <returns>An <see cref="OrderedAsyncEnumerable{T}"/> whose elements are sorted.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        public static OrderedAsyncEnumerable<TSource> ThenBy<TSource, TKey>(
            this OrderedAsyncEnumerable<TSource> source,
            Func<TSource, Promise<TKey>> keySelector)
            => ThenBy<TSource, TKey, IComparer<TKey>>(source, keySelector, Comparer<TKey>.Default);

        /// <summary>
        /// Performs a subsequent ordering of the elements in an async-enumerable sequence in ascending order according to a specified key selector function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to the key selector.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by the <paramref name="keySelector"/>.</typeparam>
        /// <param name="source">An ordered async-enumerable sequence that contains elements to sort.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from an element.</param>
        /// <returns>An <see cref="OrderedAsyncEnumerable{T}"/> whose elements are sorted.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        public static OrderedAsyncEnumerable<TSource> ThenBy<TSource, TCaptureKey, TKey>(
            this OrderedAsyncEnumerable<TSource> source,
            TCaptureKey keyCaptureValue,
            Func<TCaptureKey, TSource, Promise<TKey>> keySelector)
            => ThenBy<TSource, TCaptureKey, TKey, IComparer<TKey>>(source, keyCaptureValue, keySelector, Comparer<TKey>.Default);

        /// <summary>
        /// Performs a subsequent ordering of the elements in an async-enumerable sequence in ascending order according to a specified key selector function, and a comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by the <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TComparer">The type of the comparer.</typeparam>
        /// <param name="source">An ordered async-enumerable sequence that contains elements to sort.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from an element.</param>
        /// <param name="comparer">A comparer to compare keys.</param>
        /// <returns>An <see cref="OrderedAsyncEnumerable{T}"/> whose elements are sorted.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="comparer"/> is null.</exception>
        public static OrderedAsyncEnumerable<TSource> ThenBy<TSource, TKey, TComparer>(
            this OrderedAsyncEnumerable<TSource> source,
            Func<TSource, Promise<TKey>> keySelector,
            TComparer comparer)
            where TComparer : IComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.OrderHelper<TSource, TKey>.ThenByAwait(source, Internal.PromiseRefBase.DelegateWrapper.Create(keySelector), comparer);
        }

        /// <summary>
        /// Performs a subsequent ordering of the elements in an async-enumerable sequence in ascending order according to a specified key selector function, and a comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to the key selector.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by the <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TComparer">The type of the comparer.</typeparam>
        /// <param name="source">An ordered async-enumerable sequence that contains elements to sort.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from an element.</param>
        /// <param name="comparer">A comparer to compare keys.</param>
        /// <returns>An <see cref="OrderedAsyncEnumerable{T}"/> whose elements are sorted.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="comparer"/> is null.</exception>
        public static OrderedAsyncEnumerable<TSource> ThenBy<TSource, TCaptureKey, TKey, TComparer>(
            this OrderedAsyncEnumerable<TSource> source,
            TCaptureKey keyCaptureValue,
            Func<TCaptureKey, TSource, Promise<TKey>> keySelector,
            TComparer comparer)
            where TComparer : IComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.OrderHelper<TSource, TKey>.ThenByAwait(source, Internal.PromiseRefBase.DelegateWrapper.Create(keyCaptureValue, keySelector), comparer);
        }

        /// <summary>
        /// Performs a subsequent ordering of the elements in an async-enumerable sequence in descending order according to a specified key selector function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by the <paramref name="keySelector"/>.</typeparam>
        /// <param name="source">An ordered async-enumerable sequence that contains elements to sort.</param>
        /// <param name="keySelector">A function to extract a key from an element.</param>
        /// <returns>An <see cref="OrderedAsyncEnumerable{T}"/> whose elements are sorted.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        public static OrderedAsyncEnumerable<TSource> ThenByDescending<TSource, TKey>(
            this OrderedAsyncEnumerable<TSource> source,
            Func<TSource, TKey> keySelector)
            => ThenByDescending(source, keySelector, Comparer<TKey>.Default);

        /// <summary>
        /// Performs a subsequent ordering of the elements in an async-enumerable sequence in descending order according to a specified key selector function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to the key selector.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by the <paramref name="keySelector"/>.</typeparam>
        /// <param name="source">An ordered async-enumerable sequence that contains elements to sort.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">A function to extract a key from an element.</param>
        /// <returns>An <see cref="OrderedAsyncEnumerable{T}"/> whose elements are sorted.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        public static OrderedAsyncEnumerable<TSource> ThenByDescending<TSource, TCaptureKey, TKey>(
            this OrderedAsyncEnumerable<TSource> source,
            TCaptureKey keyCaptureValue,
            Func<TCaptureKey, TSource, TKey> keySelector)
            => ThenByDescending(source, keyCaptureValue, keySelector, Comparer<TKey>.Default);

        /// <summary>
        /// Performs a subsequent ordering of the elements in an async-enumerable sequence in descending order according to a specified key selector function, and a comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by the <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TComparer">The type of the comparer.</typeparam>
        /// <param name="source">An ordered async-enumerable sequence that contains elements to sort.</param>
        /// <param name="keySelector">A function to extract a key from an element.</param>
        /// <param name="comparer">A comparer to compare keys.</param>
        /// <returns>An <see cref="OrderedAsyncEnumerable{T}"/> whose elements are sorted.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="comparer"/> is null.</exception>
        public static OrderedAsyncEnumerable<TSource> ThenByDescending<TSource, TKey, TComparer>(
            this OrderedAsyncEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            TComparer comparer)
            where TComparer : IComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.OrderHelper<TSource, TKey>.ThenBy(source, Internal.PromiseRefBase.DelegateWrapper.Create(keySelector), new Internal.ReverseComparer<TKey, TComparer>(comparer));
        }

        /// <summary>
        /// Performs a subsequent ordering of the elements in an async-enumerable sequence in descending order according to a specified key selector function, and a comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to the key selector.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by the <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TComparer">The type of the comparer.</typeparam>
        /// <param name="source">An ordered async-enumerable sequence that contains elements to sort.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">A function to extract a key from an element.</param>
        /// <param name="comparer">A comparer to compare keys.</param>
        /// <returns>An <see cref="OrderedAsyncEnumerable{T}"/> whose elements are sorted.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="comparer"/> is null.</exception>
        public static OrderedAsyncEnumerable<TSource> ThenByDescending<TSource, TCaptureKey, TKey, TComparer>(
            this OrderedAsyncEnumerable<TSource> source,
            TCaptureKey keyCaptureValue,
            Func<TCaptureKey, TSource, TKey> keySelector,
            TComparer comparer)
            where TComparer : IComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.OrderHelper<TSource, TKey>.ThenBy(source, Internal.PromiseRefBase.DelegateWrapper.Create(keyCaptureValue, keySelector), new Internal.ReverseComparer<TKey, TComparer>(comparer));
        }

        /// <summary>
        /// Performs a subsequent ordering of the elements in an async-enumerable sequence in descending order according to a specified key selector function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by the <paramref name="keySelector"/>.</typeparam>
        /// <param name="source">An ordered async-enumerable sequence that contains elements to sort.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from an element.</param>
        /// <returns>An <see cref="OrderedAsyncEnumerable{T}"/> whose elements are sorted.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        public static OrderedAsyncEnumerable<TSource> ThenByDescending<TSource, TKey>(
            this OrderedAsyncEnumerable<TSource> source,
            Func<TSource, Promise<TKey>> keySelector)
            => ThenByDescending<TSource, TKey, IComparer<TKey>>(source, keySelector, Comparer<TKey>.Default);

        /// <summary>
        /// Performs a subsequent ordering of the elements in an async-enumerable sequence in descending order according to a specified key selector function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to the key selector.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by the <paramref name="keySelector"/>.</typeparam>
        /// <param name="source">An ordered async-enumerable sequence that contains elements to sort.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from an element.</param>
        /// <returns>An <see cref="OrderedAsyncEnumerable{T}"/> whose elements are sorted.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        public static OrderedAsyncEnumerable<TSource> ThenByDescending<TSource, TCaptureKey, TKey>(
            this OrderedAsyncEnumerable<TSource> source,
            TCaptureKey keyCaptureValue,
            Func<TCaptureKey, TSource, Promise<TKey>> keySelector)
            => ThenByDescending<TSource, TCaptureKey, TKey, IComparer<TKey>>(source, keyCaptureValue, keySelector, Comparer<TKey>.Default);

        /// <summary>
        /// Performs a subsequent ordering of the elements in an async-enumerable sequence in descending order according to a specified key selector function, and a comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by the <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TComparer">The type of the comparer.</typeparam>
        /// <param name="source">An ordered async-enumerable sequence that contains elements to sort.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from an element.</param>
        /// <param name="comparer">A comparer to compare keys.</param>
        /// <returns>An <see cref="OrderedAsyncEnumerable{T}"/> whose elements are sorted.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="comparer"/> is null.</exception>
        public static OrderedAsyncEnumerable<TSource> ThenByDescending<TSource, TKey, TComparer>(
            this OrderedAsyncEnumerable<TSource> source,
            Func<TSource, Promise<TKey>> keySelector,
            TComparer comparer)
            where TComparer : IComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.OrderHelper<TSource, TKey>.ThenByAwait(source, Internal.PromiseRefBase.DelegateWrapper.Create(keySelector), new Internal.ReverseComparer<TKey, TComparer>(comparer));
        }

        /// <summary>
        /// Performs a subsequent ordering of the elements in an async-enumerable sequence in descending order according to a specified key selector function, and a comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to the key selector.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by the <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TComparer">The type of the comparer.</typeparam>
        /// <param name="source">An ordered async-enumerable sequence that contains elements to sort.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from an element.</param>
        /// <param name="comparer">A comparer to compare keys.</param>
        /// <returns>An <see cref="OrderedAsyncEnumerable{T}"/> whose elements are sorted.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="comparer"/> is null.</exception>
        public static OrderedAsyncEnumerable<TSource> ThenByDescending<TSource, TCaptureKey, TKey, TComparer>(
            this OrderedAsyncEnumerable<TSource> source,
            TCaptureKey keyCaptureValue,
            Func<TCaptureKey, TSource, Promise<TKey>> keySelector,
            TComparer comparer)
            where TComparer : IComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.OrderHelper<TSource, TKey>.ThenByAwait(source, Internal.PromiseRefBase.DelegateWrapper.Create(keyCaptureValue, keySelector), new Internal.ReverseComparer<TKey, TComparer>(comparer));
        }
        #endregion ThenBy
    }
#endif // CSHARP_7_3_OR_NEWER
}