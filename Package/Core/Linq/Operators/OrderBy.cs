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

            return Internal.OrderedAsyncEnumerableHead<TSource, TKey>.GetOrCreate(source.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(keySelector), comparer);
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

            return Internal.OrderedAsyncEnumerableHead<TSource, TKey>.GetOrCreate(source.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(keyCaptureValue, keySelector), comparer);
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

            return Internal.OrderedAsyncEnumerableHead<TSource, TKey>.GetOrCreate(configuredSource.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(keySelector), comparer);
        }

        /// <summary>
        /// Sorts the elements of a configured async-enumerable sequence in ascending order according to a specified key selector function, and a comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to the key selector.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by the <paramref name="keySelector"/>.</typeparam>
        /// <param name="configuredSource">A configured async-enumerable sequence to sort.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">A function to extract a key from an element.</param>
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

            return Internal.OrderedAsyncEnumerableHead<TSource, TKey>.GetOrCreate(configuredSource.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(keyCaptureValue, keySelector), comparer);
        }
    }
#endif // CSHARP_7_3_OR_NEWER
}