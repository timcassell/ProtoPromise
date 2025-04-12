#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using Proto.Promises.CompilerServices;
using System;
using System.Collections.Generic;

namespace Proto.Promises.Linq
{
    partial class AsyncEnumerable
    {
        /// <summary>
        /// Applies a key-generating function to each element of an async-enumerable sequence and returns an async-enumerable sequence of
        /// unique keys and their number of occurrences in the original sequence, using the default equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the keys.</typeparam>
        /// <param name="source">The source async-enumerable sequence.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <returns>An <see cref="AsyncEnumerable{T}"/> of unique keys and their number of occurrences in <paramref name="source"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        public static AsyncEnumerable<KeyValuePair<TKey, int>> CountBy<TSource, TKey>(this AsyncEnumerable<TSource> source,
            Func<TSource, TKey> keySelector)
            => CountBy(source, keySelector, EqualityComparer<TKey>.Default);

        /// <summary>
        /// Applies a key-generating function to each element of an async-enumerable sequence and returns an async-enumerable sequence of
        /// unique keys and their number of occurrences in the original sequence, using the specified equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the keys.</typeparam>
        /// <typeparam name="TEqualityComparer">The type of the comparer used to compare keys for equality.</typeparam>
        /// <param name="source">The source async-enumerable sequence.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="comparer">The comparer used to compare keys for equality.</param>
        /// <returns>An <see cref="AsyncEnumerable{T}"/> of unique keys and their number of occurrences in <paramref name="source"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="comparer"/> is null.</exception>
        public static AsyncEnumerable<KeyValuePair<TKey, int>> CountBy<TSource, TKey, TEqualityComparer>(this AsyncEnumerable<TSource> source,
            Func<TSource, TKey> keySelector, TEqualityComparer comparer)
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.CountByHelper<TKey>.CountBy(source.GetAsyncEnumerator(), DelegateWrapper.Create(keySelector), comparer);
        }

        /// <summary>
        /// Applies a key-generating function to each element of an async-enumerable sequence and returns an async-enumerable sequence of
        /// unique keys and their number of occurrences in the original sequence, using the default equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the keys.</typeparam>
        /// <typeparam name="TCapture">The type of the extra value that will be passed to <paramref name="keySelector"/>.</typeparam>
        /// <param name="source">The source async-enumerable sequence.</param>
        /// <param name="captureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <returns>An <see cref="AsyncEnumerable{T}"/> of unique keys and their number of occurrences in <paramref name="source"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        public static AsyncEnumerable<KeyValuePair<TKey, int>> CountBy<TSource, TKey, TCapture>(this AsyncEnumerable<TSource> source,
            TCapture captureValue, Func<TCapture, TSource, TKey> keySelector)
            => CountBy(source, captureValue, keySelector, EqualityComparer<TKey>.Default);

        /// <summary>
        /// Applies a key-generating function to each element of an async-enumerable sequence and returns an async-enumerable sequence of
        /// unique keys and their number of occurrences in the original sequence, using the specified equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the keys.</typeparam>
        /// <typeparam name="TCapture">The type of the extra value that will be passed to <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TEqualityComparer">The type of the comparer used to compare keys for equality.</typeparam>
        /// <param name="source">The source async-enumerable sequence.</param>
        /// <param name="captureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="comparer">The comparer used to compare keys for equality.</param>
        /// <returns>An <see cref="AsyncEnumerable{T}"/> of unique keys and their number of occurrences in <paramref name="source"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="comparer"/> is null.</exception>
        public static AsyncEnumerable<KeyValuePair<TKey, int>> CountBy<TSource, TKey, TCapture, TEqualityComparer>(this AsyncEnumerable<TSource> source,
            TCapture captureValue, Func<TCapture, TSource, TKey> keySelector, TEqualityComparer comparer)
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.CountByHelper<TKey>.CountBy(source.GetAsyncEnumerator(), DelegateWrapper.Create(captureValue, keySelector), comparer);
        }
        
        /// <summary>
        /// Applies a key-generating function to each element of an async-enumerable sequence and returns an async-enumerable sequence of
        /// unique keys and their number of occurrences in the original sequence, using the default equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the keys.</typeparam>
        /// <param name="source">The source async-enumerable sequence.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from each element.</param>
        /// <returns>An <see cref="AsyncEnumerable{T}"/> of unique keys and their number of occurrences in <paramref name="source"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        public static AsyncEnumerable<KeyValuePair<TKey, int>> CountBy<TSource, TKey>(this AsyncEnumerable<TSource> source,
            Func<TSource, CancelationToken, Promise<TKey>> keySelector)
            => CountBy(source, keySelector, EqualityComparer<TKey>.Default);

        /// <summary>
        /// Applies a key-generating function to each element of an async-enumerable sequence and returns an async-enumerable sequence of
        /// unique keys and their number of occurrences in the original sequence, using the specified equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the keys.</typeparam>
        /// <typeparam name="TEqualityComparer">The type of the comparer used to compare keys for equality.</typeparam>
        /// <param name="source">The source async-enumerable sequence.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from each element.</param>
        /// <param name="comparer">The comparer used to compare keys for equality.</param>
        /// <returns>An <see cref="AsyncEnumerable{T}"/> of unique keys and their number of occurrences in <paramref name="source"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="comparer"/> is null.</exception>
        public static AsyncEnumerable<KeyValuePair<TKey, int>> CountBy<TSource, TKey, TEqualityComparer>(this AsyncEnumerable<TSource> source,
            Func<TSource, CancelationToken, Promise<TKey>> keySelector, TEqualityComparer comparer)
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.CountByHelper<TKey>.CountBy(source.GetAsyncEnumerator(), DelegateWrapper.Create(keySelector), comparer);
        }

        /// <summary>
        /// Applies a key-generating function to each element of an async-enumerable sequence and returns an async-enumerable sequence of
        /// unique keys and their number of occurrences in the original sequence, using the default equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the keys.</typeparam>
        /// <typeparam name="TCapture">The type of the extra value that will be passed to <paramref name="keySelector"/>.</typeparam>
        /// <param name="source">The source async-enumerable sequence.</param>
        /// <param name="captureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from each element.</param>
        /// <returns>An <see cref="AsyncEnumerable{T}"/> of unique keys and their number of occurrences in <paramref name="source"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        public static AsyncEnumerable<KeyValuePair<TKey, int>> CountBy<TSource, TKey, TCapture>(this AsyncEnumerable<TSource> source,
            TCapture captureValue, Func<TCapture, TSource, CancelationToken, Promise<TKey>> keySelector)
            => CountBy(source, captureValue, keySelector, EqualityComparer<TKey>.Default);

        /// <summary>
        /// Applies a key-generating function to each element of an async-enumerable sequence and returns an async-enumerable sequence of
        /// unique keys and their number of occurrences in the original sequence, using the specified equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the keys.</typeparam>
        /// <typeparam name="TCapture">The type of the extra value that will be passed to <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TEqualityComparer">The type of the comparer used to compare keys for equality.</typeparam>
        /// <param name="source">The configured source async-enumerable sequence.</param>
        /// <param name="captureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from each element.</param>
        /// <param name="comparer">The comparer used to compare keys for equality.</param>
        /// <returns>An <see cref="AsyncEnumerable{T}"/> of unique keys and their number of occurrences in <paramref name="source"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="comparer"/> is null.</exception>
        public static AsyncEnumerable<KeyValuePair<TKey, int>> CountBy<TSource, TKey, TCapture, TEqualityComparer>(this AsyncEnumerable<TSource> source,
            TCapture captureValue, Func<TCapture, TSource, CancelationToken, Promise<TKey>> keySelector, TEqualityComparer comparer)
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.CountByHelper<TKey>.CountBy(source.GetAsyncEnumerator(), DelegateWrapper.Create(captureValue, keySelector), comparer);
        }
        
        /// <summary>
        /// Applies a key-generating function to each element of a configured async-enumerable sequence and returns an async-enumerable sequence of
        /// unique keys and their number of occurrences in the original sequence, using the default equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the keys.</typeparam>
        /// <param name="source">The configured source async-enumerable sequence.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <returns>An <see cref="AsyncEnumerable{T}"/> of unique keys and their number of occurrences in <paramref name="source"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        public static AsyncEnumerable<KeyValuePair<TKey, int>> CountBy<TSource, TKey>(this in ConfiguredAsyncEnumerable<TSource> source,
            Func<TSource, TKey> keySelector)
            => CountBy(source, keySelector, EqualityComparer<TKey>.Default);

        /// <summary>
        /// Applies a key-generating function to each element of a configured async-enumerable sequence and returns an async-enumerable sequence of
        /// unique keys and their number of occurrences in the original sequence, using the specified equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the keys.</typeparam>
        /// <typeparam name="TEqualityComparer">The type of the comparer used to compare keys for equality.</typeparam>
        /// <param name="source">The configured source async-enumerable sequence.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="comparer">The comparer used to compare keys for equality.</param>
        /// <returns>An <see cref="AsyncEnumerable{T}"/> of unique keys and their number of occurrences in <paramref name="source"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="comparer"/> is null.</exception>
        public static AsyncEnumerable<KeyValuePair<TKey, int>> CountBy<TSource, TKey, TEqualityComparer>(this in ConfiguredAsyncEnumerable<TSource> source,
            Func<TSource, TKey> keySelector, TEqualityComparer comparer)
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.CountByHelper<TKey>.CountBy(source.GetAsyncEnumerator(), DelegateWrapper.Create(keySelector), comparer);
        }

        /// <summary>
        /// Applies a key-generating function to each element of a configured async-enumerable sequence and returns an async-enumerable sequence of
        /// unique keys and their number of occurrences in the original sequence, using the default equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the keys.</typeparam>
        /// <typeparam name="TCapture">The type of the extra value that will be passed to <paramref name="keySelector"/>.</typeparam>
        /// <param name="source">The configured source async-enumerable sequence.</param>
        /// <param name="captureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <returns>An <see cref="AsyncEnumerable{T}"/> of unique keys and their number of occurrences in <paramref name="source"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        public static AsyncEnumerable<KeyValuePair<TKey, int>> CountBy<TSource, TKey, TCapture>(this in ConfiguredAsyncEnumerable<TSource> source,
            TCapture captureValue, Func<TCapture, TSource, TKey> keySelector)
            => CountBy(source, captureValue, keySelector, EqualityComparer<TKey>.Default);

        /// <summary>
        /// Applies a key-generating function to each element of a configured async-enumerable sequence and returns an async-enumerable sequence of
        /// unique keys and their number of occurrences in the original sequence, using the specified equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the keys.</typeparam>
        /// <typeparam name="TCapture">The type of the extra value that will be passed to <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TEqualityComparer">The type of the comparer used to compare keys for equality.</typeparam>
        /// <param name="source">The configured source async-enumerable sequence.</param>
        /// <param name="captureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="comparer">The comparer used to compare keys for equality.</param>
        /// <returns>An <see cref="AsyncEnumerable{T}"/> of unique keys and their number of occurrences in <paramref name="source"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="comparer"/> is null.</exception>
        public static AsyncEnumerable<KeyValuePair<TKey, int>> CountBy<TSource, TKey, TCapture, TEqualityComparer>(this in ConfiguredAsyncEnumerable<TSource> source,
            TCapture captureValue, Func<TCapture, TSource, TKey> keySelector, TEqualityComparer comparer)
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.CountByHelper<TKey>.CountBy(source.GetAsyncEnumerator(), DelegateWrapper.Create(captureValue, keySelector), comparer);
        }
        
        /// <summary>
        /// Applies a key-generating function to each element of a configured async-enumerable sequence and returns an async-enumerable sequence of
        /// unique keys and their number of occurrences in the original sequence, using the default equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the keys.</typeparam>
        /// <param name="source">The configured source async-enumerable sequence.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from each element.</param>
        /// <returns>An <see cref="AsyncEnumerable{T}"/> of unique keys and their number of occurrences in <paramref name="source"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        public static AsyncEnumerable<KeyValuePair<TKey, int>> CountBy<TSource, TKey>(this in ConfiguredAsyncEnumerable<TSource> source,
            Func<TSource, CancelationToken, Promise<TKey>> keySelector)
            => CountBy(source, keySelector, EqualityComparer<TKey>.Default);

        /// <summary>
        /// Applies a key-generating function to each element of a configured async-enumerable sequence and returns an async-enumerable sequence of
        /// unique keys and their number of occurrences in the original sequence, using the specified equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the keys.</typeparam>
        /// <typeparam name="TEqualityComparer">The type of the comparer used to compare keys for equality.</typeparam>
        /// <param name="source">The configured source async-enumerable sequence.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from each element.</param>
        /// <param name="comparer">The comparer used to compare keys for equality.</param>
        /// <returns>An <see cref="AsyncEnumerable{T}"/> of unique keys and their number of occurrences in <paramref name="source"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="comparer"/> is null.</exception>
        public static AsyncEnumerable<KeyValuePair<TKey, int>> CountBy<TSource, TKey, TEqualityComparer>(this in ConfiguredAsyncEnumerable<TSource> source,
            Func<TSource, CancelationToken, Promise<TKey>> keySelector, TEqualityComparer comparer)
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.CountByHelper<TKey>.CountBy(source.GetAsyncEnumerator(), DelegateWrapper.Create(keySelector), comparer);
        }

        /// <summary>
        /// Applies a key-generating function to each element of a configured async-enumerable sequence and returns an async-enumerable sequence of
        /// unique keys and their number of occurrences in the original sequence, using the default equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the keys.</typeparam>
        /// <typeparam name="TCapture">The type of the extra value that will be passed to <paramref name="keySelector"/>.</typeparam>
        /// <param name="source">The configured source async-enumerable sequence.</param>
        /// <param name="captureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from each element.</param>
        /// <returns>An <see cref="AsyncEnumerable{T}"/> of unique keys and their number of occurrences in <paramref name="source"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        public static AsyncEnumerable<KeyValuePair<TKey, int>> CountBy<TSource, TKey, TCapture>(this in ConfiguredAsyncEnumerable<TSource> source,
            TCapture captureValue, Func<TCapture, TSource, CancelationToken, Promise<TKey>> keySelector)
            => CountBy(source, captureValue, keySelector, EqualityComparer<TKey>.Default);

        /// <summary>
        /// Applies a key-generating function to each element of a configured async-enumerable sequence and returns an async-enumerable sequence of
        /// unique keys and their number of occurrences in the original sequence, using the specified equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the keys.</typeparam>
        /// <typeparam name="TCapture">The type of the extra value that will be passed to <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TEqualityComparer">The type of the comparer used to compare keys for equality.</typeparam>
        /// <param name="source">The configured source async-enumerable sequence.</param>
        /// <param name="captureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from each element.</param>
        /// <param name="comparer">The comparer used to compare keys for equality.</param>
        /// <returns>An <see cref="AsyncEnumerable{T}"/> of unique keys and their number of occurrences in <paramref name="source"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="comparer"/> is null.</exception>
        public static AsyncEnumerable<KeyValuePair<TKey, int>> CountBy<TSource, TKey, TCapture, TEqualityComparer>(this in ConfiguredAsyncEnumerable<TSource> source,
            TCapture captureValue, Func<TCapture, TSource, CancelationToken, Promise<TKey>> keySelector, TEqualityComparer comparer)
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.CountByHelper<TKey>.CountBy(source.GetAsyncEnumerator(), DelegateWrapper.Create(captureValue, keySelector), comparer);
        }
    }
}