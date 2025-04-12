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
        #region SingleSeed
        /// <summary>
        /// Applies an accumulator function over each group of elements in an async-enumerable sequence according to a key selector function, using the default equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence and the result of the aggregation.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TAccumulate">The type of the result of the aggregation.</typeparam>
        /// <param name="source">The source sequence.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="seed">The initial accumulator value for each group.</param>
        /// <param name="accumulator">An accumulator function to be invoked on each element.</param>
        /// <returns>An async-enumerable sequence containing the aggregates corresponding to each key deriving from <paramref name="source"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="accumulator"/> is null.</exception>
        public static AsyncEnumerable<KeyValuePair<TKey, TAccumulate>> AggregateBy<TSource, TKey, TAccumulate>(this AsyncEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            TAccumulate seed,
            Func<TAccumulate, TSource, TAccumulate> accumulator)
            => AggregateBy(source, keySelector, seed, accumulator, EqualityComparer<TKey>.Default);

        /// <summary>
        /// Applies an accumulator function over each group of elements in an async-enumerable sequence according to a key selector function, using the specified equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence and the result of the aggregation.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TAccumulate">The type of the result of the aggregation.</typeparam>
        /// <typeparam name="TEqualityComparer">The type of the equality comparer.</typeparam>
        /// <param name="source">The source sequence.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="seed">The initial accumulator value for each group.</param>
        /// <param name="accumulator">An accumulator function to be invoked on each element.</param>
        /// <param name="comparer">An equality comparer used to compare keys for equality.</param>
        /// <returns>An async-enumerable sequence containing the aggregates corresponding to each key deriving from <paramref name="source"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="accumulator"/> or <paramref name="comparer"/> is null.</exception>
        public static AsyncEnumerable<KeyValuePair<TKey, TAccumulate>> AggregateBy<TSource, TKey, TAccumulate, TEqualityComparer>(this AsyncEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            TAccumulate seed,
            Func<TAccumulate, TSource, TAccumulate> accumulator,
            TEqualityComparer comparer)
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(accumulator, nameof(accumulator), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.AggregateByHelper<TKey>.AggregateBy(source.GetAsyncEnumerator(),
                DelegateWrapper.Create(keySelector),
                seed,
                DelegateWrapper.Create(accumulator),
                comparer);
        }

        /// <summary>
        /// Applies an accumulator function over each group of elements in an async-enumerable sequence according to a key selector function, using the default equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence and the result of the aggregation.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TAccumulate">The type of the result of the aggregation.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to <paramref name="keySelector"/>.</typeparam>
        /// <param name="source">The source sequence.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="seed">The initial accumulator value for each group.</param>
        /// <param name="accumulator">An accumulator function to be invoked on each element.</param>
        /// <returns>An async-enumerable sequence containing the aggregates corresponding to each key deriving from <paramref name="source"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="accumulator"/> is null.</exception>
        public static AsyncEnumerable<KeyValuePair<TKey, TAccumulate>> AggregateBy<TSource, TKey, TAccumulate, TCaptureKey>(this AsyncEnumerable<TSource> source,
            TCaptureKey keyCaptureValue, Func<TCaptureKey, TSource, TKey> keySelector,
            TAccumulate seed,
            Func<TAccumulate, TSource, TAccumulate> accumulator)
            => AggregateBy(source, keyCaptureValue, keySelector, seed, accumulator, EqualityComparer<TKey>.Default);

        /// <summary>
        /// Applies an accumulator function over each group of elements in an async-enumerable sequence according to a key selector function, using the specified equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence and the result of the aggregation.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TAccumulate">The type of the result of the aggregation.</typeparam>
        /// <typeparam name="TEqualityComparer">The type of the equality comparer.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to <paramref name="keySelector"/>.</typeparam>
        /// <param name="source">The source sequence.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="seed">The initial accumulator value for each group.</param>
        /// <param name="accumulator">An accumulator function to be invoked on each element.</param>
        /// <param name="comparer">An equality comparer used to compare keys for equality.</param>
        /// <returns>An async-enumerable sequence containing the aggregates corresponding to each key deriving from <paramref name="source"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="accumulator"/> or <paramref name="comparer"/> is null.</exception>
        public static AsyncEnumerable<KeyValuePair<TKey, TAccumulate>> AggregateBy<TSource, TKey, TAccumulate, TEqualityComparer, TCaptureKey>(this AsyncEnumerable<TSource> source,
            TCaptureKey keyCaptureValue, Func<TCaptureKey, TSource, TKey> keySelector,
            TAccumulate seed,
            Func<TAccumulate, TSource, TAccumulate> accumulator,
            TEqualityComparer comparer)
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(accumulator, nameof(accumulator), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.AggregateByHelper<TKey>.AggregateBy(source.GetAsyncEnumerator(),
                DelegateWrapper.Create(keyCaptureValue, keySelector),
                seed,
                DelegateWrapper.Create(accumulator),
                comparer);
        }

        /// <summary>
        /// Applies an accumulator function over each group of elements in an async-enumerable sequence according to a key selector function, using the default equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence and the result of the aggregation.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TAccumulate">The type of the result of the aggregation.</typeparam>
        /// <typeparam name="TCaptureAccumulate">The type of the captured value that will be passed to <paramref name="accumulator"/>.</typeparam>
        /// <param name="source">The source sequence.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="seed">The initial accumulator value for each group.</param>
        /// <param name="accumulateCaptureValue">The extra value that will be passed to <paramref name="accumulator"/>.</param>
        /// <param name="accumulator">An accumulator function to be invoked on each element.</param>
        /// <returns>An async-enumerable sequence containing the aggregates corresponding to each key deriving from <paramref name="source"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="accumulator"/> is null.</exception>
        public static AsyncEnumerable<KeyValuePair<TKey, TAccumulate>> AggregateBy<TSource, TKey, TAccumulate, TCaptureAccumulate>(this AsyncEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            TAccumulate seed,
            TCaptureAccumulate accumulateCaptureValue, Func<TCaptureAccumulate, TAccumulate, TSource, TAccumulate> accumulator)
            => AggregateBy(source, keySelector, seed, accumulateCaptureValue, accumulator, EqualityComparer<TKey>.Default);

        /// <summary>
        /// Applies an accumulator function over each group of elements in an async-enumerable sequence according to a key selector function, using the specified equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence and the result of the aggregation.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TAccumulate">The type of the result of the aggregation.</typeparam>
        /// <typeparam name="TEqualityComparer">The type of the equality comparer.</typeparam>
        /// <typeparam name="TCaptureAccumulate">The type of the captured value that will be passed to <paramref name="accumulator"/>.</typeparam>
        /// <param name="source">The source sequence.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="seed">The initial accumulator value for each group.</param>
        /// <param name="accumulateCaptureValue">The extra value that will be passed to <paramref name="accumulator"/>.</param>
        /// <param name="accumulator">An accumulator function to be invoked on each element.</param>
        /// <param name="comparer">An equality comparer used to compare keys for equality.</param>
        /// <returns>An async-enumerable sequence containing the aggregates corresponding to each key deriving from <paramref name="source"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="accumulator"/> or <paramref name="comparer"/> is null.</exception>
        public static AsyncEnumerable<KeyValuePair<TKey, TAccumulate>> AggregateBy<TSource, TKey, TAccumulate, TEqualityComparer, TCaptureAccumulate>(this AsyncEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            TAccumulate seed,
            TCaptureAccumulate accumulateCaptureValue, Func<TCaptureAccumulate, TAccumulate, TSource, TAccumulate> accumulator,
            TEqualityComparer comparer)
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(accumulator, nameof(accumulator), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.AggregateByHelper<TKey>.AggregateBy(source.GetAsyncEnumerator(),
                DelegateWrapper.Create(keySelector),
                seed,
                DelegateWrapper.Create(accumulateCaptureValue, accumulator),
                comparer);
        }

        /// <summary>
        /// Applies an accumulator function over each group of elements in an async-enumerable sequence according to a key selector function, using the default equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence and the result of the aggregation.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TAccumulate">The type of the result of the aggregation.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TCaptureAccumulate">The type of the captured value that will be passed to <paramref name="accumulator"/>.</typeparam>
        /// <param name="source">The source sequence.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="seed">The initial accumulator value for each group.</param>
        /// <param name="accumulateCaptureValue">The extra value that will be passed to <paramref name="accumulator"/>.</param>
        /// <param name="accumulator">An accumulator function to be invoked on each element.</param>
        /// <returns>An async-enumerable sequence containing the aggregates corresponding to each key deriving from <paramref name="source"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="accumulator"/> is null.</exception>
        public static AsyncEnumerable<KeyValuePair<TKey, TAccumulate>> AggregateBy<TSource, TKey, TAccumulate, TCaptureKey, TCaptureAccumulate>(this AsyncEnumerable<TSource> source,
            TCaptureKey keyCaptureValue, Func<TCaptureKey, TSource, TKey> keySelector,
            TAccumulate seed,
            TCaptureAccumulate accumulateCaptureValue, Func<TCaptureAccumulate, TAccumulate, TSource, TAccumulate> accumulator)
            => AggregateBy(source, keyCaptureValue, keySelector, seed, accumulateCaptureValue, accumulator, EqualityComparer<TKey>.Default);

        /// <summary>
        /// Applies an accumulator function over each group of elements in an async-enumerable sequence according to a key selector function, using the specified equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence and the result of the aggregation.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TAccumulate">The type of the result of the aggregation.</typeparam>
        /// <typeparam name="TEqualityComparer">The type of the equality comparer.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TCaptureAccumulate">The type of the captured value that will be passed to <paramref name="accumulator"/>.</typeparam>
        /// <param name="source">The source sequence.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="seed">The initial accumulator value for each group.</param>
        /// <param name="accumulateCaptureValue">The extra value that will be passed to <paramref name="accumulator"/>.</param>
        /// <param name="accumulator">An accumulator function to be invoked on each element.</param>
        /// <param name="comparer">An equality comparer used to compare keys for equality.</param>
        /// <returns>An async-enumerable sequence containing the aggregates corresponding to each key deriving from <paramref name="source"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="accumulator"/> or <paramref name="comparer"/> is null.</exception>
        public static AsyncEnumerable<KeyValuePair<TKey, TAccumulate>> AggregateBy<TSource, TKey, TAccumulate, TEqualityComparer, TCaptureKey, TCaptureAccumulate>(this AsyncEnumerable<TSource> source,
            TCaptureKey keyCaptureValue, Func<TCaptureKey, TSource, TKey> keySelector,
            TAccumulate seed,
            TCaptureAccumulate accumulateCaptureValue, Func<TCaptureAccumulate, TAccumulate, TSource, TAccumulate> accumulator,
            TEqualityComparer comparer)
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(accumulator, nameof(accumulator), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.AggregateByHelper<TKey>.AggregateBy(source.GetAsyncEnumerator(),
                DelegateWrapper.Create(keyCaptureValue, keySelector),
                seed,
                DelegateWrapper.Create(accumulateCaptureValue, accumulator),
                comparer);
        }

        /// <summary>
        /// Applies an accumulator function over each group of elements in an async-enumerable sequence according to a key selector function, using the default equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence and the result of the aggregation.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TAccumulate">The type of the result of the aggregation.</typeparam>
        /// <param name="source">The source sequence.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from each element.</param>
        /// <param name="seed">The initial accumulator value for each group.</param>
        /// <param name="accumulator">An asynchronous accumulator function to be invoked on each element.</param>
        /// <returns>An async-enumerable sequence containing the aggregates corresponding to each key deriving from <paramref name="source"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="accumulator"/> is null.</exception>
        public static AsyncEnumerable<KeyValuePair<TKey, TAccumulate>> AggregateBy<TSource, TKey, TAccumulate>(this AsyncEnumerable<TSource> source,
            Func<TSource, CancelationToken, Promise<TKey>> keySelector,
            TAccumulate seed,
            Func<TAccumulate, TSource, CancelationToken, Promise<TAccumulate>> accumulator)
            => AggregateBy(source, keySelector, seed, accumulator, EqualityComparer<TKey>.Default);

        /// <summary>
        /// Applies an accumulator function over each group of elements in an async-enumerable sequence according to a key selector function, using the specified equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence and the result of the aggregation.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TAccumulate">The type of the result of the aggregation.</typeparam>
        /// <typeparam name="TEqualityComparer">The type of the equality comparer.</typeparam>
        /// <param name="source">The source sequence.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from each element.</param>
        /// <param name="seed">The initial accumulator value for each group.</param>
        /// <param name="accumulator">An asynchronous accumulator function to be invoked on each element.</param>
        /// <param name="comparer">An equality comparer used to compare keys for equality.</param>
        /// <returns>An async-enumerable sequence containing the aggregates corresponding to each key deriving from <paramref name="source"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="accumulator"/> or <paramref name="comparer"/> is null.</exception>
        public static AsyncEnumerable<KeyValuePair<TKey, TAccumulate>> AggregateBy<TSource, TKey, TAccumulate, TEqualityComparer>(this AsyncEnumerable<TSource> source,
            Func<TSource, CancelationToken, Promise<TKey>> keySelector,
            TAccumulate seed,
            Func<TAccumulate, TSource, CancelationToken, Promise<TAccumulate>> accumulator,
            TEqualityComparer comparer)
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(accumulator, nameof(accumulator), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.AggregateByHelper<TKey>.AggregateBy(source.GetAsyncEnumerator(),
                DelegateWrapper.Create(keySelector),
                seed,
                DelegateWrapper.Create(accumulator),
                comparer);
        }

        /// <summary>
        /// Applies an accumulator function over each group of elements in an async-enumerable sequence according to a key selector function, using the default equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence and the result of the aggregation.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TAccumulate">The type of the result of the aggregation.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to <paramref name="keySelector"/>.</typeparam>
        /// <param name="source">The source sequence.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from each element.</param>
        /// <param name="seed">The initial accumulator value for each group.</param>
        /// <param name="accumulator">An asynchronous accumulator function to be invoked on each element.</param>
        /// <returns>An async-enumerable sequence containing the aggregates corresponding to each key deriving from <paramref name="source"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="accumulator"/> is null.</exception>
        public static AsyncEnumerable<KeyValuePair<TKey, TAccumulate>> AggregateBy<TSource, TKey, TAccumulate, TCaptureKey>(this AsyncEnumerable<TSource> source,
            TCaptureKey keyCaptureValue, Func<TCaptureKey, TSource, CancelationToken, Promise<TKey>> keySelector,
            TAccumulate seed,
            Func<TAccumulate, TSource, CancelationToken, Promise<TAccumulate>> accumulator)
            => AggregateBy(source, keyCaptureValue, keySelector, seed, accumulator, EqualityComparer<TKey>.Default);

        /// <summary>
        /// Applies an accumulator function over each group of elements in an async-enumerable sequence according to a key selector function, using the specified equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence and the result of the aggregation.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TAccumulate">The type of the result of the aggregation.</typeparam>
        /// <typeparam name="TEqualityComparer">The type of the equality comparer.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to <paramref name="keySelector"/>.</typeparam>
        /// <param name="source">The source sequence.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from each element.</param>
        /// <param name="seed">The initial accumulator value for each group.</param>
        /// <param name="accumulator">An asynchronous accumulator function to be invoked on each element.</param>
        /// <param name="comparer">An equality comparer used to compare keys for equality.</param>
        /// <returns>An async-enumerable sequence containing the aggregates corresponding to each key deriving from <paramref name="source"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="accumulator"/> or <paramref name="comparer"/> is null.</exception>
        public static AsyncEnumerable<KeyValuePair<TKey, TAccumulate>> AggregateBy<TSource, TKey, TAccumulate, TEqualityComparer, TCaptureKey>(this AsyncEnumerable<TSource> source,
            TCaptureKey keyCaptureValue, Func<TCaptureKey, TSource, CancelationToken, Promise<TKey>> keySelector,
            TAccumulate seed,
            Func<TAccumulate, TSource, CancelationToken, Promise<TAccumulate>> accumulator,
            TEqualityComparer comparer)
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(accumulator, nameof(accumulator), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.AggregateByHelper<TKey>.AggregateBy(source.GetAsyncEnumerator(),
                DelegateWrapper.Create(keyCaptureValue, keySelector),
                seed,
                DelegateWrapper.Create(accumulator),
                comparer);
        }

        /// <summary>
        /// Applies an accumulator function over each group of elements in an async-enumerable sequence according to a key selector function, using the default equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence and the result of the aggregation.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TAccumulate">The type of the result of the aggregation.</typeparam>
        /// <typeparam name="TCaptureAccumulate">The type of the captured value that will be passed to <paramref name="accumulator"/>.</typeparam>
        /// <param name="source">The source sequence.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from each element.</param>
        /// <param name="seed">The initial accumulator value for each group.</param>
        /// <param name="accumulateCaptureValue">The extra value that will be passed to <paramref name="accumulator"/>.</param>
        /// <param name="accumulator">An asynchronous accumulator function to be invoked on each element.</param>
        /// <returns>An async-enumerable sequence containing the aggregates corresponding to each key deriving from <paramref name="source"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="accumulator"/> is null.</exception>
        public static AsyncEnumerable<KeyValuePair<TKey, TAccumulate>> AggregateBy<TSource, TKey, TAccumulate, TCaptureAccumulate>(this AsyncEnumerable<TSource> source,
            Func<TSource, CancelationToken, Promise<TKey>> keySelector,
            TAccumulate seed,
            TCaptureAccumulate accumulateCaptureValue, Func<TCaptureAccumulate, TAccumulate, TSource, CancelationToken, Promise<TAccumulate>> accumulator)
            => AggregateBy(source, keySelector, seed, accumulateCaptureValue, accumulator, EqualityComparer<TKey>.Default);

        /// <summary>
        /// Applies an accumulator function over each group of elements in an async-enumerable sequence according to a key selector function, using the specified equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence and the result of the aggregation.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TAccumulate">The type of the result of the aggregation.</typeparam>
        /// <typeparam name="TEqualityComparer">The type of the equality comparer.</typeparam>
        /// <typeparam name="TCaptureAccumulate">The type of the captured value that will be passed to <paramref name="accumulator"/>.</typeparam>
        /// <param name="source">The source sequence.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from each element.</param>
        /// <param name="seed">The initial accumulator value for each group.</param>
        /// <param name="accumulateCaptureValue">The extra value that will be passed to <paramref name="accumulator"/>.</param>
        /// <param name="accumulator">An asynchronous accumulator function to be invoked on each element.</param>
        /// <param name="comparer">An equality comparer used to compare keys for equality.</param>
        /// <returns>An async-enumerable sequence containing the aggregates corresponding to each key deriving from <paramref name="source"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="accumulator"/> or <paramref name="comparer"/> is null.</exception>
        public static AsyncEnumerable<KeyValuePair<TKey, TAccumulate>> AggregateBy<TSource, TKey, TAccumulate, TEqualityComparer, TCaptureAccumulate>(this AsyncEnumerable<TSource> source,
            Func<TSource, CancelationToken, Promise<TKey>> keySelector,
            TAccumulate seed,
            TCaptureAccumulate accumulateCaptureValue, Func<TCaptureAccumulate, TAccumulate, TSource, CancelationToken, Promise<TAccumulate>> accumulator,
            TEqualityComparer comparer)
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(accumulator, nameof(accumulator), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.AggregateByHelper<TKey>.AggregateBy(source.GetAsyncEnumerator(),
                DelegateWrapper.Create(keySelector),
                seed,
                DelegateWrapper.Create(accumulateCaptureValue, accumulator),
                comparer);
        }

        /// <summary>
        /// Applies an accumulator function over each group of elements in an async-enumerable sequence according to a key selector function, using the default equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence and the result of the aggregation.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TAccumulate">The type of the result of the aggregation.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TCaptureAccumulate">The type of the captured value that will be passed to <paramref name="accumulator"/>.</typeparam>
        /// <param name="source">The source sequence.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from each element.</param>
        /// <param name="seed">The initial accumulator value for each group.</param>
        /// <param name="accumulateCaptureValue">The extra value that will be passed to <paramref name="accumulator"/>.</param>
        /// <param name="accumulator">An asynchronous accumulator function to be invoked on each element.</param>
        /// <returns>An async-enumerable sequence containing the aggregates corresponding to each key deriving from <paramref name="source"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="accumulator"/> is null.</exception>
        public static AsyncEnumerable<KeyValuePair<TKey, TAccumulate>> AggregateBy<TSource, TKey, TAccumulate, TCaptureKey, TCaptureAccumulate>(this AsyncEnumerable<TSource> source,
            TCaptureKey keyCaptureValue, Func<TCaptureKey, TSource, CancelationToken, Promise<TKey>> keySelector,
            TAccumulate seed,
            TCaptureAccumulate accumulateCaptureValue, Func<TCaptureAccumulate, TAccumulate, TSource, CancelationToken, Promise<TAccumulate>> accumulator)
            => AggregateBy(source, keyCaptureValue, keySelector, seed, accumulateCaptureValue, accumulator, EqualityComparer<TKey>.Default);

        /// <summary>
        /// Applies an accumulator function over each group of elements in an async-enumerable sequence according to a key selector function, using the specified equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence and the result of the aggregation.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TAccumulate">The type of the result of the aggregation.</typeparam>
        /// <typeparam name="TEqualityComparer">The type of the equality comparer.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TCaptureAccumulate">The type of the captured value that will be passed to <paramref name="accumulator"/>.</typeparam>
        /// <param name="source">The source sequence.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from each element.</param>
        /// <param name="seed">The initial accumulator value for each group.</param>
        /// <param name="accumulateCaptureValue">The extra value that will be passed to <paramref name="accumulator"/>.</param>
        /// <param name="accumulator">An asynchronous accumulator function to be invoked on each element.</param>
        /// <param name="comparer">An equality comparer used to compare keys for equality.</param>
        /// <returns>An async-enumerable sequence containing the aggregates corresponding to each key deriving from <paramref name="source"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="accumulator"/> or <paramref name="comparer"/> is null.</exception>
        public static AsyncEnumerable<KeyValuePair<TKey, TAccumulate>> AggregateBy<TSource, TKey, TAccumulate, TEqualityComparer, TCaptureKey, TCaptureAccumulate>(this AsyncEnumerable<TSource> source,
            TCaptureKey keyCaptureValue, Func<TCaptureKey, TSource, CancelationToken, Promise<TKey>> keySelector,
            TAccumulate seed,
            TCaptureAccumulate accumulateCaptureValue, Func<TCaptureAccumulate, TAccumulate, TSource, CancelationToken, Promise<TAccumulate>> accumulator,
            TEqualityComparer comparer)
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(accumulator, nameof(accumulator), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.AggregateByHelper<TKey>.AggregateBy(source.GetAsyncEnumerator(),
                DelegateWrapper.Create(keyCaptureValue, keySelector),
                seed,
                DelegateWrapper.Create(accumulateCaptureValue, accumulator),
                comparer);
        }

        /// <summary>
        /// Applies an accumulator function over each group of elements in a configured async-enumerable sequence according to a key selector function, using the default equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence and the result of the aggregation.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TAccumulate">The type of the result of the aggregation.</typeparam>
        /// <param name="configuredSource">The configured source sequence.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="seed">The initial accumulator value for each group.</param>
        /// <param name="accumulator">An accumulator function to be invoked on each element.</param>
        /// <returns>An async-enumerable sequence containing the aggregates corresponding to each key deriving from <paramref name="configuredSource"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="accumulator"/> is null.</exception>
        public static AsyncEnumerable<KeyValuePair<TKey, TAccumulate>> AggregateBy<TSource, TKey, TAccumulate>(
            this in ConfiguredAsyncEnumerable<TSource> configuredSource,
            Func<TSource, TKey> keySelector,
            TAccumulate seed,
            Func<TAccumulate, TSource, TAccumulate> accumulator)
            => AggregateBy(configuredSource, keySelector, seed, accumulator, EqualityComparer<TKey>.Default);

        /// <summary>
        /// Applies an accumulator function over each group of elements in a configured async-enumerable sequence according to a key selector function, using the specified equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence and the result of the aggregation.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TAccumulate">The type of the result of the aggregation.</typeparam>
        /// <typeparam name="TEqualityComparer">The type of the equality comparer.</typeparam>
        /// <param name="configuredSource">The configured source sequence.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="seed">The initial accumulator value for each group.</param>
        /// <param name="accumulator">An accumulator function to be invoked on each element.</param>
        /// <param name="comparer">An equality comparer used to compare keys for equality.</param>
        /// <returns>An async-enumerable sequence containing the aggregates corresponding to each key deriving from <paramref name="configuredSource"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="accumulator"/> or <paramref name="comparer"/> is null.</exception>
        public static AsyncEnumerable<KeyValuePair<TKey, TAccumulate>> AggregateBy<TSource, TKey, TAccumulate, TEqualityComparer>(
            this in ConfiguredAsyncEnumerable<TSource> configuredSource,
            Func<TSource, TKey> keySelector,
            TAccumulate seed,
            Func<TAccumulate, TSource, TAccumulate> accumulator,
            TEqualityComparer comparer)
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(accumulator, nameof(accumulator), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.AggregateByHelper<TKey>.AggregateBy(configuredSource.GetAsyncEnumerator(),
                DelegateWrapper.Create(keySelector),
                seed,
                DelegateWrapper.Create(accumulator),
                comparer);
        }

        /// <summary>
        /// Applies an accumulator function over each group of elements in a configured async-enumerable sequence according to a key selector function, using the default equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence and the result of the aggregation.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TAccumulate">The type of the result of the aggregation.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to <paramref name="keySelector"/>.</typeparam>
        /// <param name="configuredSource">The configured source sequence.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="seed">The initial accumulator value for each group.</param>
        /// <param name="accumulator">An accumulator function to be invoked on each element.</param>
        /// <returns>An async-enumerable sequence containing the aggregates corresponding to each key deriving from <paramref name="configuredSource"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="accumulator"/> is null.</exception>
        public static AsyncEnumerable<KeyValuePair<TKey, TAccumulate>> AggregateBy<TSource, TKey, TAccumulate, TCaptureKey>(
            this in ConfiguredAsyncEnumerable<TSource> configuredSource,
            TCaptureKey keyCaptureValue, Func<TCaptureKey, TSource, TKey> keySelector,
            TAccumulate seed,
            Func<TAccumulate, TSource, TAccumulate> accumulator)
            => AggregateBy(configuredSource, keyCaptureValue, keySelector, seed, accumulator, EqualityComparer<TKey>.Default);

        /// <summary>
        /// Applies an accumulator function over each group of elements in a configured async-enumerable sequence according to a key selector function, using the specified equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence and the result of the aggregation.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TAccumulate">The type of the result of the aggregation.</typeparam>
        /// <typeparam name="TEqualityComparer">The type of the equality comparer.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to <paramref name="keySelector"/>.</typeparam>
        /// <param name="configuredSource">The configured source sequence.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="seed">The initial accumulator value for each group.</param>
        /// <param name="accumulator">An accumulator function to be invoked on each element.</param>
        /// <param name="comparer">An equality comparer used to compare keys for equality.</param>
        /// <returns>An async-enumerable sequence containing the aggregates corresponding to each key deriving from <paramref name="configuredSource"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="accumulator"/> or <paramref name="comparer"/> is null.</exception>
        public static AsyncEnumerable<KeyValuePair<TKey, TAccumulate>> AggregateBy<TSource, TKey, TAccumulate, TEqualityComparer, TCaptureKey>(
            this in ConfiguredAsyncEnumerable<TSource> configuredSource,
            TCaptureKey keyCaptureValue, Func<TCaptureKey, TSource, TKey> keySelector,
            TAccumulate seed,
            Func<TAccumulate, TSource, TAccumulate> accumulator,
            TEqualityComparer comparer)
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(accumulator, nameof(accumulator), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.AggregateByHelper<TKey>.AggregateBy(configuredSource.GetAsyncEnumerator(),
                DelegateWrapper.Create(keyCaptureValue, keySelector),
                seed,
                DelegateWrapper.Create(accumulator),
                comparer);
        }

        /// <summary>
        /// Applies an accumulator function over each group of elements in a configured async-enumerable sequence according to a key selector function, using the default equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence and the result of the aggregation.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TAccumulate">The type of the result of the aggregation.</typeparam>
        /// <typeparam name="TCaptureAccumulate">The type of the captured value that will be passed to <paramref name="accumulator"/>.</typeparam>
        /// <param name="configuredSource">The configured source sequence.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="seed">The initial accumulator value for each group.</param>
        /// <param name="accumulateCaptureValue">The extra value that will be passed to <paramref name="accumulator"/>.</param>
        /// <param name="accumulator">An accumulator function to be invoked on each element.</param>
        /// <returns>An async-enumerable sequence containing the aggregates corresponding to each key deriving from <paramref name="configuredSource"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="accumulator"/> is null.</exception>
        public static AsyncEnumerable<KeyValuePair<TKey, TAccumulate>> AggregateBy<TSource, TKey, TAccumulate, TCaptureAccumulate>(
            this in ConfiguredAsyncEnumerable<TSource> configuredSource,
            Func<TSource, TKey> keySelector,
            TAccumulate seed,
            TCaptureAccumulate accumulateCaptureValue, Func<TCaptureAccumulate, TAccumulate, TSource, TAccumulate> accumulator)
            => AggregateBy(configuredSource, keySelector, seed, accumulateCaptureValue, accumulator, EqualityComparer<TKey>.Default);

        /// <summary>
        /// Applies an accumulator function over each group of elements in a configured async-enumerable sequence according to a key selector function, using the specified equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence and the result of the aggregation.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TAccumulate">The type of the result of the aggregation.</typeparam>
        /// <typeparam name="TEqualityComparer">The type of the equality comparer.</typeparam>
        /// <typeparam name="TCaptureAccumulate">The type of the captured value that will be passed to <paramref name="accumulator"/>.</typeparam>
        /// <param name="configuredSource">The configured source sequence.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="seed">The initial accumulator value for each group.</param>
        /// <param name="accumulateCaptureValue">The extra value that will be passed to <paramref name="accumulator"/>.</param>
        /// <param name="accumulator">An accumulator function to be invoked on each element.</param>
        /// <param name="comparer">An equality comparer used to compare keys for equality.</param>
        /// <returns>An async-enumerable sequence containing the aggregates corresponding to each key deriving from <paramref name="configuredSource"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="accumulator"/> or <paramref name="comparer"/> is null.</exception>
        public static AsyncEnumerable<KeyValuePair<TKey, TAccumulate>> AggregateBy<TSource, TKey, TAccumulate, TEqualityComparer, TCaptureAccumulate>(
            this in ConfiguredAsyncEnumerable<TSource> configuredSource,
            Func<TSource, TKey> keySelector,
            TAccumulate seed,
            TCaptureAccumulate accumulateCaptureValue, Func<TCaptureAccumulate, TAccumulate, TSource, TAccumulate> accumulator,
            TEqualityComparer comparer)
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(accumulator, nameof(accumulator), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.AggregateByHelper<TKey>.AggregateBy(configuredSource.GetAsyncEnumerator(),
                DelegateWrapper.Create(keySelector),
                seed,
                DelegateWrapper.Create(accumulateCaptureValue, accumulator),
                comparer);
        }

        /// <summary>
        /// Applies an accumulator function over each group of elements in a configured async-enumerable sequence according to a key selector function, using the default equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence and the result of the aggregation.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TAccumulate">The type of the result of the aggregation.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TCaptureAccumulate">The type of the captured value that will be passed to <paramref name="accumulator"/>.</typeparam>
        /// <param name="configuredSource">The configured source sequence.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="seed">The initial accumulator value for each group.</param>
        /// <param name="accumulateCaptureValue">The extra value that will be passed to <paramref name="accumulator"/>.</param>
        /// <param name="accumulator">An accumulator function to be invoked on each element.</param>
        /// <returns>An async-enumerable sequence containing the aggregates corresponding to each key deriving from <paramref name="configuredSource"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="accumulator"/> is null.</exception>
        public static AsyncEnumerable<KeyValuePair<TKey, TAccumulate>> AggregateBy<TSource, TKey, TAccumulate, TCaptureKey, TCaptureAccumulate>(
            this in ConfiguredAsyncEnumerable<TSource> configuredSource,
            TCaptureKey keyCaptureValue, Func<TCaptureKey, TSource, TKey> keySelector,
            TAccumulate seed,
            TCaptureAccumulate accumulateCaptureValue, Func<TCaptureAccumulate, TAccumulate, TSource, TAccumulate> accumulator)
            => AggregateBy(configuredSource, keyCaptureValue, keySelector, seed, accumulateCaptureValue, accumulator, EqualityComparer<TKey>.Default);

        /// <summary>
        /// Applies an accumulator function over each group of elements in a configured async-enumerable sequence according to a key selector function, using the specified equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence and the result of the aggregation.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TAccumulate">The type of the result of the aggregation.</typeparam>
        /// <typeparam name="TEqualityComparer">The type of the equality comparer.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TCaptureAccumulate">The type of the captured value that will be passed to <paramref name="accumulator"/>.</typeparam>
        /// <param name="configuredSource">The configured source sequence.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="seed">The initial accumulator value for each group.</param>
        /// <param name="accumulateCaptureValue">The extra value that will be passed to <paramref name="accumulator"/>.</param>
        /// <param name="accumulator">An accumulator function to be invoked on each element.</param>
        /// <param name="comparer">An equality comparer used to compare keys for equality.</param>
        /// <returns>An async-enumerable sequence containing the aggregates corresponding to each key deriving from <paramref name="configuredSource"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="accumulator"/> or <paramref name="comparer"/> is null.</exception>
        public static AsyncEnumerable<KeyValuePair<TKey, TAccumulate>> AggregateBy<TSource, TKey, TAccumulate, TEqualityComparer, TCaptureKey, TCaptureAccumulate>(
            this in ConfiguredAsyncEnumerable<TSource> configuredSource,
            TCaptureKey keyCaptureValue, Func<TCaptureKey, TSource, TKey> keySelector,
            TAccumulate seed,
            TCaptureAccumulate accumulateCaptureValue, Func<TCaptureAccumulate, TAccumulate, TSource, TAccumulate> accumulator,
            TEqualityComparer comparer)
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(accumulator, nameof(accumulator), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.AggregateByHelper<TKey>.AggregateBy(configuredSource.GetAsyncEnumerator(),
                DelegateWrapper.Create(keyCaptureValue, keySelector),
                seed,
                DelegateWrapper.Create(accumulateCaptureValue, accumulator),
                comparer);
        }

        /// <summary>
        /// Applies an accumulator function over each group of elements in a configured async-enumerable sequence according to a key selector function, using the default equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence and the result of the aggregation.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TAccumulate">The type of the result of the aggregation.</typeparam>
        /// <param name="configuredSource">The configured source sequence.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from each element.</param>
        /// <param name="seed">The initial accumulator value for each group.</param>
        /// <param name="accumulator">An asynchronous accumulator function to be invoked on each element.</param>
        /// <returns>An async-enumerable sequence containing the aggregates corresponding to each key deriving from <paramref name="configuredSource"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="accumulator"/> is null.</exception>
        public static AsyncEnumerable<KeyValuePair<TKey, TAccumulate>> AggregateBy<TSource, TKey, TAccumulate>(
            this in ConfiguredAsyncEnumerable<TSource> configuredSource,
            Func<TSource, CancelationToken, Promise<TKey>> keySelector,
            TAccumulate seed,
            Func<TAccumulate, TSource, CancelationToken, Promise<TAccumulate>> accumulator)
            => AggregateBy(configuredSource, keySelector, seed, accumulator, EqualityComparer<TKey>.Default);

        /// <summary>
        /// Applies an accumulator function over each group of elements in a configured async-enumerable sequence according to a key selector function, using the specified equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence and the result of the aggregation.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TAccumulate">The type of the result of the aggregation.</typeparam>
        /// <typeparam name="TEqualityComparer">The type of the equality comparer.</typeparam>
        /// <param name="configuredSource">The configured source sequence.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from each element.</param>
        /// <param name="seed">The initial accumulator value for each group.</param>
        /// <param name="accumulator">An asynchronous accumulator function to be invoked on each element.</param>
        /// <param name="comparer">An equality comparer used to compare keys for equality.</param>
        /// <returns>An async-enumerable sequence containing the aggregates corresponding to each key deriving from <paramref name="configuredSource"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="accumulator"/> or <paramref name="comparer"/> is null.</exception>
        public static AsyncEnumerable<KeyValuePair<TKey, TAccumulate>> AggregateBy<TSource, TKey, TAccumulate, TEqualityComparer>(
            this in ConfiguredAsyncEnumerable<TSource> configuredSource,
            Func<TSource, CancelationToken, Promise<TKey>> keySelector,
            TAccumulate seed,
            Func<TAccumulate, TSource, CancelationToken, Promise<TAccumulate>> accumulator,
            TEqualityComparer comparer)
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(accumulator, nameof(accumulator), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.AggregateByHelper<TKey>.AggregateBy(configuredSource.GetAsyncEnumerator(),
                DelegateWrapper.Create(keySelector),
                seed,
                DelegateWrapper.Create(accumulator),
                comparer);
        }

        /// <summary>
        /// Applies an accumulator function over each group of elements in a configured async-enumerable sequence according to a key selector function, using the default equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence and the result of the aggregation.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TAccumulate">The type of the result of the aggregation.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to <paramref name="keySelector"/>.</typeparam>
        /// <param name="configuredSource">The configured source sequence.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from each element.</param>
        /// <param name="seed">The initial accumulator value for each group.</param>
        /// <param name="accumulator">An asynchronous accumulator function to be invoked on each element.</param>
        /// <returns>An async-enumerable sequence containing the aggregates corresponding to each key deriving from <paramref name="configuredSource"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="accumulator"/> is null.</exception>
        public static AsyncEnumerable<KeyValuePair<TKey, TAccumulate>> AggregateBy<TSource, TKey, TAccumulate, TCaptureKey>(
            this in ConfiguredAsyncEnumerable<TSource> configuredSource,
            TCaptureKey keyCaptureValue, Func<TCaptureKey, TSource, CancelationToken, Promise<TKey>> keySelector,
            TAccumulate seed,
            Func<TAccumulate, TSource, CancelationToken, Promise<TAccumulate>> accumulator)
            => AggregateBy(configuredSource, keyCaptureValue, keySelector, seed, accumulator, EqualityComparer<TKey>.Default);

        /// <summary>
        /// Applies an accumulator function over each group of elements in a configured async-enumerable sequence according to a key selector function, using the specified equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence and the result of the aggregation.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TAccumulate">The type of the result of the aggregation.</typeparam>
        /// <typeparam name="TEqualityComparer">The type of the equality comparer.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to <paramref name="keySelector"/>.</typeparam>
        /// <param name="configuredSource">The configured source sequence.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from each element.</param>
        /// <param name="seed">The initial accumulator value for each group.</param>
        /// <param name="accumulator">An asynchronous accumulator function to be invoked on each element.</param>
        /// <param name="comparer">An equality comparer used to compare keys for equality.</param>
        /// <returns>An async-enumerable sequence containing the aggregates corresponding to each key deriving from <paramref name="configuredSource"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="accumulator"/> or <paramref name="comparer"/> is null.</exception>
        public static AsyncEnumerable<KeyValuePair<TKey, TAccumulate>> AggregateBy<TSource, TKey, TAccumulate, TEqualityComparer, TCaptureKey>(
            this in ConfiguredAsyncEnumerable<TSource> configuredSource,
            TCaptureKey keyCaptureValue, Func<TCaptureKey, TSource, CancelationToken, Promise<TKey>> keySelector,
            TAccumulate seed,
            Func<TAccumulate, TSource, CancelationToken, Promise<TAccumulate>> accumulator,
            TEqualityComparer comparer)
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(accumulator, nameof(accumulator), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.AggregateByHelper<TKey>.AggregateBy(configuredSource.GetAsyncEnumerator(),
                DelegateWrapper.Create(keyCaptureValue, keySelector),
                seed,
                DelegateWrapper.Create(accumulator),
                comparer);
        }

        /// <summary>
        /// Applies an accumulator function over each group of elements in a configured async-enumerable sequence according to a key selector function, using the default equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence and the result of the aggregation.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TAccumulate">The type of the result of the aggregation.</typeparam>
        /// <typeparam name="TCaptureAccumulate">The type of the captured value that will be passed to <paramref name="accumulator"/>.</typeparam>
        /// <param name="configuredSource">The configured source sequence.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from each element.</param>
        /// <param name="seed">The initial accumulator value for each group.</param>
        /// <param name="accumulateCaptureValue">The extra value that will be passed to <paramref name="accumulator"/>.</param>
        /// <param name="accumulator">An asynchronous accumulator function to be invoked on each element.</param>
        /// <returns>An async-enumerable sequence containing the aggregates corresponding to each key deriving from <paramref name="configuredSource"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="accumulator"/> is null.</exception>
        public static AsyncEnumerable<KeyValuePair<TKey, TAccumulate>> AggregateBy<TSource, TKey, TAccumulate, TCaptureAccumulate>(
            this in ConfiguredAsyncEnumerable<TSource> configuredSource,
            Func<TSource, CancelationToken, Promise<TKey>> keySelector,
            TAccumulate seed,
            TCaptureAccumulate accumulateCaptureValue, Func<TCaptureAccumulate, TAccumulate, TSource, CancelationToken, Promise<TAccumulate>> accumulator)
            => AggregateBy(configuredSource, keySelector, seed, accumulateCaptureValue, accumulator, EqualityComparer<TKey>.Default);

        /// <summary>
        /// Applies an accumulator function over each group of elements in a configured async-enumerable sequence according to a key selector function, using the specified equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence and the result of the aggregation.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TAccumulate">The type of the result of the aggregation.</typeparam>
        /// <typeparam name="TEqualityComparer">The type of the equality comparer.</typeparam>
        /// <typeparam name="TCaptureAccumulate">The type of the captured value that will be passed to <paramref name="accumulator"/>.</typeparam>
        /// <param name="configuredSource">The configured source sequence.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from each element.</param>
        /// <param name="seed">The initial accumulator value for each group.</param>
        /// <param name="accumulateCaptureValue">The extra value that will be passed to <paramref name="accumulator"/>.</param>
        /// <param name="accumulator">An asynchronous accumulator function to be invoked on each element.</param>
        /// <param name="comparer">An equality comparer used to compare keys for equality.</param>
        /// <returns>An async-enumerable sequence containing the aggregates corresponding to each key deriving from <paramref name="configuredSource"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="accumulator"/> or <paramref name="comparer"/> is null.</exception>
        public static AsyncEnumerable<KeyValuePair<TKey, TAccumulate>> AggregateBy<TSource, TKey, TAccumulate, TEqualityComparer, TCaptureAccumulate>(
            this in ConfiguredAsyncEnumerable<TSource> configuredSource,
            Func<TSource, CancelationToken, Promise<TKey>> keySelector,
            TAccumulate seed,
            TCaptureAccumulate accumulateCaptureValue, Func<TCaptureAccumulate, TAccumulate, TSource, CancelationToken, Promise<TAccumulate>> accumulator,
            TEqualityComparer comparer)
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(accumulator, nameof(accumulator), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.AggregateByHelper<TKey>.AggregateBy(configuredSource.GetAsyncEnumerator(),
                DelegateWrapper.Create(keySelector),
                seed,
                DelegateWrapper.Create(accumulateCaptureValue, accumulator),
                comparer);
        }

        /// <summary>
        /// Applies an accumulator function over each group of elements in a configured async-enumerable sequence according to a key selector function, using the default equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence and the result of the aggregation.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TAccumulate">The type of the result of the aggregation.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TCaptureAccumulate">The type of the captured value that will be passed to <paramref name="accumulator"/>.</typeparam>
        /// <param name="configuredSource">The configured source sequence.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from each element.</param>
        /// <param name="seed">The initial accumulator value for each group.</param>
        /// <param name="accumulateCaptureValue">The extra value that will be passed to <paramref name="accumulator"/>.</param>
        /// <param name="accumulator">An asynchronous accumulator function to be invoked on each element.</param>
        /// <returns>An async-enumerable sequence containing the aggregates corresponding to each key deriving from <paramref name="configuredSource"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="accumulator"/> is null.</exception>
        public static AsyncEnumerable<KeyValuePair<TKey, TAccumulate>> AggregateBy<TSource, TKey, TAccumulate, TCaptureKey, TCaptureAccumulate>(
            this in ConfiguredAsyncEnumerable<TSource> configuredSource,
            TCaptureKey keyCaptureValue, Func<TCaptureKey, TSource, CancelationToken, Promise<TKey>> keySelector,
            TAccumulate seed,
            TCaptureAccumulate accumulateCaptureValue, Func<TCaptureAccumulate, TAccumulate, TSource, CancelationToken, Promise<TAccumulate>> accumulator)
            => AggregateBy(configuredSource, keyCaptureValue, keySelector, seed, accumulateCaptureValue, accumulator, EqualityComparer<TKey>.Default);

        /// <summary>
        /// Applies an accumulator function over each group of elements in a configured async-enumerable sequence according to a key selector function, using the specified equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence and the result of the aggregation.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TAccumulate">The type of the result of the aggregation.</typeparam>
        /// <typeparam name="TEqualityComparer">The type of the equality comparer.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TCaptureAccumulate">The type of the captured value that will be passed to <paramref name="accumulator"/>.</typeparam>
        /// <param name="configuredSource">The configured source sequence.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from each element.</param>
        /// <param name="seed">The initial accumulator value for each group.</param>
        /// <param name="accumulateCaptureValue">The extra value that will be passed to <paramref name="accumulator"/>.</param>
        /// <param name="accumulator">An asynchronous accumulator function to be invoked on each element.</param>
        /// <param name="comparer">An equality comparer used to compare keys for equality.</param>
        /// <returns>An async-enumerable sequence containing the aggregates corresponding to each key deriving from <paramref name="configuredSource"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="accumulator"/> or <paramref name="comparer"/> is null.</exception>
        public static AsyncEnumerable<KeyValuePair<TKey, TAccumulate>> AggregateBy<TSource, TKey, TAccumulate, TEqualityComparer, TCaptureKey, TCaptureAccumulate>(
            this in ConfiguredAsyncEnumerable<TSource> configuredSource,
            TCaptureKey keyCaptureValue, Func<TCaptureKey, TSource, CancelationToken, Promise<TKey>> keySelector,
            TAccumulate seed,
            TCaptureAccumulate accumulateCaptureValue, Func<TCaptureAccumulate, TAccumulate, TSource, CancelationToken, Promise<TAccumulate>> accumulator,
            TEqualityComparer comparer)
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(accumulator, nameof(accumulator), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.AggregateByHelper<TKey>.AggregateBy(configuredSource.GetAsyncEnumerator(),
                DelegateWrapper.Create(keyCaptureValue, keySelector),
                seed,
                DelegateWrapper.Create(accumulateCaptureValue, accumulator),
                comparer);
        }
        #endregion SingleSeed

        #region SeedSelector
        /// <summary>
        /// Applies an accumulator function over each group of elements in an async-enumerable sequence according to a key selector function, using the default equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence and the result of the aggregation.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TAccumulate">The type of the result of the aggregation.</typeparam>
        /// <param name="source">The source sequence.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="seedSelector">A function to retrieve the initial accumulator value for each group.</param>
        /// <param name="accumulator">An accumulator function to be invoked on each element.</param>
        /// <returns>An async-enumerable sequence containing the aggregates corresponding to each key deriving from <paramref name="source"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="seedSelector"/> or <paramref name="accumulator"/> is null.</exception>
        public static AsyncEnumerable<KeyValuePair<TKey, TAccumulate>> AggregateBy<TSource, TKey, TAccumulate>(this AsyncEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            Func<TKey, TAccumulate> seedSelector,
            Func<TAccumulate, TSource, TAccumulate> accumulator)
            => AggregateBy(source, keySelector, seedSelector, accumulator, EqualityComparer<TKey>.Default);

        /// <summary>
        /// Applies an accumulator function over each group of elements in an async-enumerable sequence according to a key selector function, using the specified equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence and the result of the aggregation.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TAccumulate">The type of the result of the aggregation.</typeparam>
        /// <typeparam name="TEqualityComparer">The type of the equality comparer.</typeparam>
        /// <param name="source">The source sequence.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="seedSelector">A function to retrieve the initial accumulator value for each group.</param>
        /// <param name="accumulator">An accumulator function to be invoked on each element.</param>
        /// <param name="comparer">An equality comparer used to compare keys for equality.</param>
        /// <returns>An async-enumerable sequence containing the aggregates corresponding to each key deriving from <paramref name="source"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="seedSelector"/> or <paramref name="accumulator"/> or <paramref name="comparer"/> is null.</exception>
        public static AsyncEnumerable<KeyValuePair<TKey, TAccumulate>> AggregateBy<TSource, TKey, TAccumulate, TEqualityComparer>(this AsyncEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            Func<TKey, TAccumulate> seedSelector,
            Func<TAccumulate, TSource, TAccumulate> accumulator,
            TEqualityComparer comparer)
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(seedSelector, nameof(seedSelector), 1);
            ValidateArgument(accumulator, nameof(accumulator), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.AggregateByHelper<TKey, TAccumulate>.AggregateBy(source.GetAsyncEnumerator(),
                DelegateWrapper.Create(keySelector),
                DelegateWrapper.Create(seedSelector),
                DelegateWrapper.Create(accumulator),
                comparer);
        }

        /// <summary>
        /// Applies an accumulator function over each group of elements in an async-enumerable sequence according to a key selector function, using the default equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence and the result of the aggregation.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TAccumulate">The type of the result of the aggregation.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to <paramref name="keySelector"/>.</typeparam>
        /// <param name="source">The source sequence.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="seedSelector">A function to retrieve the initial accumulator value for each group.</param>
        /// <param name="accumulator">An accumulator function to be invoked on each element.</param>
        /// <returns>An async-enumerable sequence containing the aggregates corresponding to each key deriving from <paramref name="source"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="seedSelector"/> or <paramref name="accumulator"/> is null.</exception>
        public static AsyncEnumerable<KeyValuePair<TKey, TAccumulate>> AggregateBy<TSource, TKey, TAccumulate, TCaptureKey>(this AsyncEnumerable<TSource> source,
            TCaptureKey keyCaptureValue, Func<TCaptureKey, TSource, TKey> keySelector,
            Func<TKey, TAccumulate> seedSelector,
            Func<TAccumulate, TSource, TAccumulate> accumulator)
            => AggregateBy(source, keyCaptureValue, keySelector, seedSelector, accumulator, EqualityComparer<TKey>.Default);

        /// <summary>
        /// Applies an accumulator function over each group of elements in an async-enumerable sequence according to a key selector function, using the specified equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence and the result of the aggregation.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TAccumulate">The type of the result of the aggregation.</typeparam>
        /// <typeparam name="TEqualityComparer">The type of the equality comparer.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to <paramref name="keySelector"/>.</typeparam>
        /// <param name="source">The source sequence.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="seedSelector">A function to retrieve the initial accumulator value for each group.</param>
        /// <param name="accumulator">An accumulator function to be invoked on each element.</param>
        /// <param name="comparer">An equality comparer used to compare keys for equality.</param>
        /// <returns>An async-enumerable sequence containing the aggregates corresponding to each key deriving from <paramref name="source"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="seedSelector"/> or <paramref name="accumulator"/> or <paramref name="comparer"/> is null.</exception>
        public static AsyncEnumerable<KeyValuePair<TKey, TAccumulate>> AggregateBy<TSource, TKey, TAccumulate, TEqualityComparer, TCaptureKey>(this AsyncEnumerable<TSource> source,
            TCaptureKey keyCaptureValue, Func<TCaptureKey, TSource, TKey> keySelector,
            Func<TKey, TAccumulate> seedSelector,
            Func<TAccumulate, TSource, TAccumulate> accumulator,
            TEqualityComparer comparer)
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(seedSelector, nameof(seedSelector), 1);
            ValidateArgument(accumulator, nameof(accumulator), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.AggregateByHelper<TKey, TAccumulate>.AggregateBy(source.GetAsyncEnumerator(),
                DelegateWrapper.Create(keyCaptureValue, keySelector),
                DelegateWrapper.Create(seedSelector),
                DelegateWrapper.Create(accumulator),
                comparer);
        }

        /// <summary>
        /// Applies an accumulator function over each group of elements in an async-enumerable sequence according to a key selector function, using the default equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence and the result of the aggregation.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TAccumulate">The type of the result of the aggregation.</typeparam>
        /// <typeparam name="TCaptureAccumulate">The type of the captured value that will be passed to <paramref name="accumulator"/>.</typeparam>
        /// <param name="source">The source sequence.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="seedSelector">A function to retrieve the initial accumulator value for each group.</param>
        /// <param name="accumulateCaptureValue">The extra value that will be passed to <paramref name="accumulator"/>.</param>
        /// <param name="accumulator">An accumulator function to be invoked on each element.</param>
        /// <returns>An async-enumerable sequence containing the aggregates corresponding to each key deriving from <paramref name="source"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="seedSelector"/> or <paramref name="accumulator"/> is null.</exception>
        public static AsyncEnumerable<KeyValuePair<TKey, TAccumulate>> AggregateBy<TSource, TKey, TAccumulate, TCaptureAccumulate>(this AsyncEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            Func<TKey, TAccumulate> seedSelector,
            TCaptureAccumulate accumulateCaptureValue, Func<TCaptureAccumulate, TAccumulate, TSource, TAccumulate> accumulator)
            => AggregateBy(source, keySelector, seedSelector, accumulateCaptureValue, accumulator, EqualityComparer<TKey>.Default);

        /// <summary>
        /// Applies an accumulator function over each group of elements in an async-enumerable sequence according to a key selector function, using the specified equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence and the result of the aggregation.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TAccumulate">The type of the result of the aggregation.</typeparam>
        /// <typeparam name="TEqualityComparer">The type of the equality comparer.</typeparam>
        /// <typeparam name="TCaptureAccumulate">The type of the captured value that will be passed to <paramref name="accumulator"/>.</typeparam>
        /// <param name="source">The source sequence.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="seedSelector">A function to retrieve the initial accumulator value for each group.</param>
        /// <param name="accumulateCaptureValue">The extra value that will be passed to <paramref name="accumulator"/>.</param>
        /// <param name="accumulator">An accumulator function to be invoked on each element.</param>
        /// <param name="comparer">An equality comparer used to compare keys for equality.</param>
        /// <returns>An async-enumerable sequence containing the aggregates corresponding to each key deriving from <paramref name="source"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="seedSelector"/> or <paramref name="accumulator"/> or <paramref name="comparer"/> is null.</exception>
        public static AsyncEnumerable<KeyValuePair<TKey, TAccumulate>> AggregateBy<TSource, TKey, TAccumulate, TEqualityComparer, TCaptureAccumulate>(this AsyncEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            Func<TKey, TAccumulate> seedSelector,
            TCaptureAccumulate accumulateCaptureValue, Func<TCaptureAccumulate, TAccumulate, TSource, TAccumulate> accumulator,
            TEqualityComparer comparer)
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(seedSelector, nameof(seedSelector), 1);
            ValidateArgument(accumulator, nameof(accumulator), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.AggregateByHelper<TKey, TAccumulate>.AggregateBy(source.GetAsyncEnumerator(),
                DelegateWrapper.Create(keySelector),
                DelegateWrapper.Create(seedSelector),
                DelegateWrapper.Create(accumulateCaptureValue, accumulator),
                comparer);
        }

        /// <summary>
        /// Applies an accumulator function over each group of elements in an async-enumerable sequence according to a key selector function, using the default equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence and the result of the aggregation.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TAccumulate">The type of the result of the aggregation.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TCaptureAccumulate">The type of the captured value that will be passed to <paramref name="accumulator"/>.</typeparam>
        /// <param name="source">The source sequence.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="seedSelector">A function to retrieve the initial accumulator value for each group.</param>
        /// <param name="accumulateCaptureValue">The extra value that will be passed to <paramref name="accumulator"/>.</param>
        /// <param name="accumulator">An accumulator function to be invoked on each element.</param>
        /// <returns>An async-enumerable sequence containing the aggregates corresponding to each key deriving from <paramref name="source"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="seedSelector"/> or <paramref name="accumulator"/> is null.</exception>
        public static AsyncEnumerable<KeyValuePair<TKey, TAccumulate>> AggregateBy<TSource, TKey, TAccumulate, TCaptureKey, TCaptureAccumulate>(this AsyncEnumerable<TSource> source,
            TCaptureKey keyCaptureValue, Func<TCaptureKey, TSource, TKey> keySelector,
            Func<TKey, TAccumulate> seedSelector,
            TCaptureAccumulate accumulateCaptureValue, Func<TCaptureAccumulate, TAccumulate, TSource, TAccumulate> accumulator)
            => AggregateBy(source, keyCaptureValue, keySelector, seedSelector, accumulateCaptureValue, accumulator, EqualityComparer<TKey>.Default);

        /// <summary>
        /// Applies an accumulator function over each group of elements in an async-enumerable sequence according to a key selector function, using the specified equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence and the result of the aggregation.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TAccumulate">The type of the result of the aggregation.</typeparam>
        /// <typeparam name="TEqualityComparer">The type of the equality comparer.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TCaptureAccumulate">The type of the captured value that will be passed to <paramref name="accumulator"/>.</typeparam>
        /// <param name="source">The source sequence.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="seedSelector">A function to retrieve the initial accumulator value for each group.</param>
        /// <param name="accumulateCaptureValue">The extra value that will be passed to <paramref name="accumulator"/>.</param>
        /// <param name="accumulator">An accumulator function to be invoked on each element.</param>
        /// <param name="comparer">An equality comparer used to compare keys for equality.</param>
        /// <returns>An async-enumerable sequence containing the aggregates corresponding to each key deriving from <paramref name="source"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="seedSelector"/> or <paramref name="accumulator"/> or <paramref name="comparer"/> is null.</exception>
        public static AsyncEnumerable<KeyValuePair<TKey, TAccumulate>> AggregateBy<TSource, TKey, TAccumulate, TEqualityComparer, TCaptureKey, TCaptureAccumulate>(this AsyncEnumerable<TSource> source,
            TCaptureKey keyCaptureValue, Func<TCaptureKey, TSource, TKey> keySelector,
            Func<TKey, TAccumulate> seedSelector,
            TCaptureAccumulate accumulateCaptureValue, Func<TCaptureAccumulate, TAccumulate, TSource, TAccumulate> accumulator,
            TEqualityComparer comparer)
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(seedSelector, nameof(seedSelector), 1);
            ValidateArgument(accumulator, nameof(accumulator), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.AggregateByHelper<TKey, TAccumulate>.AggregateBy(source.GetAsyncEnumerator(),
                DelegateWrapper.Create(keyCaptureValue, keySelector),
                DelegateWrapper.Create(seedSelector),
                DelegateWrapper.Create(accumulateCaptureValue, accumulator),
                comparer);
        }

        /// <summary>
        /// Applies an accumulator function over each group of elements in an async-enumerable sequence according to a key selector function, using the default equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence and the result of the aggregation.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TAccumulate">The type of the result of the aggregation.</typeparam>
        /// <param name="source">The source sequence.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from each element.</param>
        /// <param name="seedSelector">An asynchronous function to retrieve the initial accumulator value for each group.</param>
        /// <param name="accumulator">An asynchronous accumulator function to be invoked on each element.</param>
        /// <returns>An async-enumerable sequence containing the aggregates corresponding to each key deriving from <paramref name="source"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="seedSelector"/> or <paramref name="accumulator"/> is null.</exception>
        public static AsyncEnumerable<KeyValuePair<TKey, TAccumulate>> AggregateBy<TSource, TKey, TAccumulate>(this AsyncEnumerable<TSource> source,
            Func<TSource, CancelationToken, Promise<TKey>> keySelector,
            Func<TKey, CancelationToken, Promise<TAccumulate>> seedSelector,
            Func<TAccumulate, TSource, CancelationToken, Promise<TAccumulate>> accumulator)
            => AggregateBy(source, keySelector, seedSelector, accumulator, EqualityComparer<TKey>.Default);

        /// <summary>
        /// Applies an accumulator function over each group of elements in an async-enumerable sequence according to a key selector function, using the specified equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence and the result of the aggregation.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TAccumulate">The type of the result of the aggregation.</typeparam>
        /// <typeparam name="TEqualityComparer">The type of the equality comparer.</typeparam>
        /// <param name="source">The source sequence.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from each element.</param>
        /// <param name="seedSelector">An asynchronous function to retrieve the initial accumulator value for each group.</param>
        /// <param name="accumulator">An asynchronous accumulator function to be invoked on each element.</param>
        /// <param name="comparer">An equality comparer used to compare keys for equality.</param>
        /// <returns>An async-enumerable sequence containing the aggregates corresponding to each key deriving from <paramref name="source"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="seedSelector"/> or <paramref name="accumulator"/> or <paramref name="comparer"/> is null.</exception>
        public static AsyncEnumerable<KeyValuePair<TKey, TAccumulate>> AggregateBy<TSource, TKey, TAccumulate, TEqualityComparer>(this AsyncEnumerable<TSource> source,
            Func<TSource, CancelationToken, Promise<TKey>> keySelector,
            Func<TKey, CancelationToken, Promise<TAccumulate>> seedSelector,
            Func<TAccumulate, TSource, CancelationToken, Promise<TAccumulate>> accumulator,
            TEqualityComparer comparer)
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(seedSelector, nameof(seedSelector), 1);
            ValidateArgument(accumulator, nameof(accumulator), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.AggregateByHelper<TKey, TAccumulate>.AggregateBy(source.GetAsyncEnumerator(),
                DelegateWrapper.Create(keySelector),
                DelegateWrapper.Create(seedSelector),
                DelegateWrapper.Create(accumulator),
                comparer);
        }

        /// <summary>
        /// Applies an accumulator function over each group of elements in an async-enumerable sequence according to a key selector function, using the default equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence and the result of the aggregation.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TAccumulate">The type of the result of the aggregation.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to <paramref name="keySelector"/>.</typeparam>
        /// <param name="source">The source sequence.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from each element.</param>
        /// <param name="seedSelector">An asynchronous function to retrieve the initial accumulator value for each group.</param>
        /// <param name="accumulator">An asynchronous accumulator function to be invoked on each element.</param>
        /// <returns>An async-enumerable sequence containing the aggregates corresponding to each key deriving from <paramref name="source"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="seedSelector"/> or <paramref name="accumulator"/> is null.</exception>
        public static AsyncEnumerable<KeyValuePair<TKey, TAccumulate>> AggregateBy<TSource, TKey, TAccumulate, TCaptureKey>(this AsyncEnumerable<TSource> source,
            TCaptureKey keyCaptureValue, Func<TCaptureKey, TSource, CancelationToken, Promise<TKey>> keySelector,
            Func<TKey, CancelationToken, Promise<TAccumulate>> seedSelector,
            Func<TAccumulate, TSource, CancelationToken, Promise<TAccumulate>> accumulator)
            => AggregateBy(source, keyCaptureValue, keySelector, seedSelector, accumulator, EqualityComparer<TKey>.Default);

        /// <summary>
        /// Applies an accumulator function over each group of elements in an async-enumerable sequence according to a key selector function, using the specified equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence and the result of the aggregation.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TAccumulate">The type of the result of the aggregation.</typeparam>
        /// <typeparam name="TEqualityComparer">The type of the equality comparer.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to <paramref name="keySelector"/>.</typeparam>
        /// <param name="source">The source sequence.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from each element.</param>
        /// <param name="seedSelector">An asynchronous function to retrieve the initial accumulator value for each group.</param>
        /// <param name="accumulator">An asynchronous accumulator function to be invoked on each element.</param>
        /// <param name="comparer">An equality comparer used to compare keys for equality.</param>
        /// <returns>An async-enumerable sequence containing the aggregates corresponding to each key deriving from <paramref name="source"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="seedSelector"/> or <paramref name="accumulator"/> or <paramref name="comparer"/> is null.</exception>
        public static AsyncEnumerable<KeyValuePair<TKey, TAccumulate>> AggregateBy<TSource, TKey, TAccumulate, TEqualityComparer, TCaptureKey>(this AsyncEnumerable<TSource> source,
            TCaptureKey keyCaptureValue, Func<TCaptureKey, TSource, CancelationToken, Promise<TKey>> keySelector,
            Func<TKey, CancelationToken, Promise<TAccumulate>> seedSelector,
            Func<TAccumulate, TSource, CancelationToken, Promise<TAccumulate>> accumulator,
            TEqualityComparer comparer)
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(seedSelector, nameof(seedSelector), 1);
            ValidateArgument(accumulator, nameof(accumulator), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.AggregateByHelper<TKey, TAccumulate>.AggregateBy(source.GetAsyncEnumerator(),
                DelegateWrapper.Create(keyCaptureValue, keySelector),
                DelegateWrapper.Create(seedSelector),
                DelegateWrapper.Create(accumulator),
                comparer);
        }

        /// <summary>
        /// Applies an accumulator function over each group of elements in an async-enumerable sequence according to a key selector function, using the default equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence and the result of the aggregation.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TAccumulate">The type of the result of the aggregation.</typeparam>
        /// <typeparam name="TCaptureAccumulate">The type of the captured value that will be passed to <paramref name="accumulator"/>.</typeparam>
        /// <param name="source">The source sequence.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from each element.</param>
        /// <param name="seedSelector">An asynchronous function to retrieve the initial accumulator value for each group.</param>
        /// <param name="accumulateCaptureValue">The extra value that will be passed to <paramref name="accumulator"/>.</param>
        /// <param name="accumulator">An asynchronous accumulator function to be invoked on each element.</param>
        /// <returns>An async-enumerable sequence containing the aggregates corresponding to each key deriving from <paramref name="source"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="seedSelector"/> or <paramref name="accumulator"/> is null.</exception>
        public static AsyncEnumerable<KeyValuePair<TKey, TAccumulate>> AggregateBy<TSource, TKey, TAccumulate, TCaptureAccumulate>(this AsyncEnumerable<TSource> source,
            Func<TSource, CancelationToken, Promise<TKey>> keySelector,
            Func<TKey, CancelationToken, Promise<TAccumulate>> seedSelector,
            TCaptureAccumulate accumulateCaptureValue, Func<TCaptureAccumulate, TAccumulate, TSource, CancelationToken, Promise<TAccumulate>> accumulator)
            => AggregateBy(source, keySelector, seedSelector, accumulateCaptureValue, accumulator, EqualityComparer<TKey>.Default);

        /// <summary>
        /// Applies an accumulator function over each group of elements in an async-enumerable sequence according to a key selector function, using the specified equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence and the result of the aggregation.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TAccumulate">The type of the result of the aggregation.</typeparam>
        /// <typeparam name="TEqualityComparer">The type of the equality comparer.</typeparam>
        /// <typeparam name="TCaptureAccumulate">The type of the captured value that will be passed to <paramref name="accumulator"/>.</typeparam>
        /// <param name="source">The source sequence.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from each element.</param>
        /// <param name="seedSelector">An asynchronous function to retrieve the initial accumulator value for each group.</param>
        /// <param name="accumulateCaptureValue">The extra value that will be passed to <paramref name="accumulator"/>.</param>
        /// <param name="accumulator">An asynchronous accumulator function to be invoked on each element.</param>
        /// <param name="comparer">An equality comparer used to compare keys for equality.</param>
        /// <returns>An async-enumerable sequence containing the aggregates corresponding to each key deriving from <paramref name="source"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="seedSelector"/> or <paramref name="accumulator"/> or <paramref name="comparer"/> is null.</exception>
        public static AsyncEnumerable<KeyValuePair<TKey, TAccumulate>> AggregateBy<TSource, TKey, TAccumulate, TEqualityComparer, TCaptureAccumulate>(this AsyncEnumerable<TSource> source,
            Func<TSource, CancelationToken, Promise<TKey>> keySelector,
            Func<TKey, CancelationToken, Promise<TAccumulate>> seedSelector,
            TCaptureAccumulate accumulateCaptureValue, Func<TCaptureAccumulate, TAccumulate, TSource, CancelationToken, Promise<TAccumulate>> accumulator,
            TEqualityComparer comparer)
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(seedSelector, nameof(seedSelector), 1);
            ValidateArgument(accumulator, nameof(accumulator), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.AggregateByHelper<TKey, TAccumulate>.AggregateBy(source.GetAsyncEnumerator(),
                DelegateWrapper.Create(keySelector),
                DelegateWrapper.Create(seedSelector),
                DelegateWrapper.Create(accumulateCaptureValue, accumulator),
                comparer);
        }

        /// <summary>
        /// Applies an accumulator function over each group of elements in an async-enumerable sequence according to a key selector function, using the default equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence and the result of the aggregation.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TAccumulate">The type of the result of the aggregation.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TCaptureAccumulate">The type of the captured value that will be passed to <paramref name="accumulator"/>.</typeparam>
        /// <param name="source">The source sequence.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from each element.</param>
        /// <param name="seedSelector">An asynchronous function to retrieve the initial accumulator value for each group.</param>
        /// <param name="accumulateCaptureValue">The extra value that will be passed to <paramref name="accumulator"/>.</param>
        /// <param name="accumulator">An asynchronous accumulator function to be invoked on each element.</param>
        /// <returns>An async-enumerable sequence containing the aggregates corresponding to each key deriving from <paramref name="source"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="seedSelector"/> or <paramref name="accumulator"/> is null.</exception>
        public static AsyncEnumerable<KeyValuePair<TKey, TAccumulate>> AggregateBy<TSource, TKey, TAccumulate, TCaptureKey, TCaptureAccumulate>(this AsyncEnumerable<TSource> source,
            TCaptureKey keyCaptureValue, Func<TCaptureKey, TSource, CancelationToken, Promise<TKey>> keySelector,
            Func<TKey, CancelationToken, Promise<TAccumulate>> seedSelector,
            TCaptureAccumulate accumulateCaptureValue, Func<TCaptureAccumulate, TAccumulate, TSource, CancelationToken, Promise<TAccumulate>> accumulator)
            => AggregateBy(source, keyCaptureValue, keySelector, seedSelector, accumulateCaptureValue, accumulator, EqualityComparer<TKey>.Default);

        /// <summary>
        /// Applies an accumulator function over each group of elements in an async-enumerable sequence according to a key selector function, using the specified equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence and the result of the aggregation.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TAccumulate">The type of the result of the aggregation.</typeparam>
        /// <typeparam name="TEqualityComparer">The type of the equality comparer.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TCaptureAccumulate">The type of the captured value that will be passed to <paramref name="accumulator"/>.</typeparam>
        /// <param name="source">The source sequence.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from each element.</param>
        /// <param name="seedSelector">An asynchronous function to retrieve the initial accumulator value for each group.</param>
        /// <param name="accumulateCaptureValue">The extra value that will be passed to <paramref name="accumulator"/>.</param>
        /// <param name="accumulator">An asynchronous accumulator function to be invoked on each element.</param>
        /// <param name="comparer">An equality comparer used to compare keys for equality.</param>
        /// <returns>An async-enumerable sequence containing the aggregates corresponding to each key deriving from <paramref name="source"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="seedSelector"/> or <paramref name="accumulator"/> or <paramref name="comparer"/> is null.</exception>
        public static AsyncEnumerable<KeyValuePair<TKey, TAccumulate>> AggregateBy<TSource, TKey, TAccumulate, TEqualityComparer, TCaptureKey, TCaptureAccumulate>(this AsyncEnumerable<TSource> source,
            TCaptureKey keyCaptureValue, Func<TCaptureKey, TSource, CancelationToken, Promise<TKey>> keySelector,
            Func<TKey, CancelationToken, Promise<TAccumulate>> seedSelector,
            TCaptureAccumulate accumulateCaptureValue, Func<TCaptureAccumulate, TAccumulate, TSource, CancelationToken, Promise<TAccumulate>> accumulator,
            TEqualityComparer comparer)
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(seedSelector, nameof(seedSelector), 1);
            ValidateArgument(accumulator, nameof(accumulator), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.AggregateByHelper<TKey, TAccumulate>.AggregateBy(source.GetAsyncEnumerator(),
                DelegateWrapper.Create(keyCaptureValue, keySelector),
                DelegateWrapper.Create(seedSelector),
                DelegateWrapper.Create(accumulateCaptureValue, accumulator),
                comparer);
        }

        /// <summary>
        /// Applies an accumulator function over each group of elements in a configured async-enumerable sequence according to a key selector function, using the default equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence and the result of the aggregation.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TAccumulate">The type of the result of the aggregation.</typeparam>
        /// <param name="configuredSource">The configured source sequence.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="seedSelector">A function to retrieve the initial accumulator value for each group.</param>
        /// <param name="accumulator">An accumulator function to be invoked on each element.</param>
        /// <returns>An async-enumerable sequence containing the aggregates corresponding to each key deriving from <paramref name="configuredSource"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="seedSelector"/> or <paramref name="accumulator"/> is null.</exception>
        public static AsyncEnumerable<KeyValuePair<TKey, TAccumulate>> AggregateBy<TSource, TKey, TAccumulate>(
            this in ConfiguredAsyncEnumerable<TSource> configuredSource,
            Func<TSource, TKey> keySelector,
            Func<TKey, TAccumulate> seedSelector,
            Func<TAccumulate, TSource, TAccumulate> accumulator)
            => AggregateBy(configuredSource, keySelector, seedSelector, accumulator, EqualityComparer<TKey>.Default);

        /// <summary>
        /// Applies an accumulator function over each group of elements in a configured async-enumerable sequence according to a key selector function, using the specified equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence and the result of the aggregation.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TAccumulate">The type of the result of the aggregation.</typeparam>
        /// <typeparam name="TEqualityComparer">The type of the equality comparer.</typeparam>
        /// <param name="configuredSource">The configured source sequence.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="seedSelector">A function to retrieve the initial accumulator value for each group.</param>
        /// <param name="accumulator">An accumulator function to be invoked on each element.</param>
        /// <param name="comparer">An equality comparer used to compare keys for equality.</param>
        /// <returns>An async-enumerable sequence containing the aggregates corresponding to each key deriving from <paramref name="configuredSource"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="seedSelector"/> or <paramref name="accumulator"/> or <paramref name="comparer"/> is null.</exception>
        public static AsyncEnumerable<KeyValuePair<TKey, TAccumulate>> AggregateBy<TSource, TKey, TAccumulate, TEqualityComparer>(
            this in ConfiguredAsyncEnumerable<TSource> configuredSource,
            Func<TSource, TKey> keySelector,
            Func<TKey, TAccumulate> seedSelector,
            Func<TAccumulate, TSource, TAccumulate> accumulator,
            TEqualityComparer comparer)
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(seedSelector, nameof(seedSelector), 1);
            ValidateArgument(accumulator, nameof(accumulator), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.AggregateByHelper<TKey, TAccumulate>.AggregateBy(configuredSource.GetAsyncEnumerator(),
                DelegateWrapper.Create(keySelector),
                DelegateWrapper.Create(seedSelector),
                DelegateWrapper.Create(accumulator),
                comparer);
        }

        /// <summary>
        /// Applies an accumulator function over each group of elements in a configured async-enumerable sequence according to a key selector function, using the default equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence and the result of the aggregation.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TAccumulate">The type of the result of the aggregation.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to <paramref name="keySelector"/>.</typeparam>
        /// <param name="configuredSource">The configured source sequence.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="seedSelector">A function to retrieve the initial accumulator value for each group.</param>
        /// <param name="accumulator">An accumulator function to be invoked on each element.</param>
        /// <returns>An async-enumerable sequence containing the aggregates corresponding to each key deriving from <paramref name="configuredSource"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="seedSelector"/> or <paramref name="accumulator"/> is null.</exception>
        public static AsyncEnumerable<KeyValuePair<TKey, TAccumulate>> AggregateBy<TSource, TKey, TAccumulate, TCaptureKey>(
            this in ConfiguredAsyncEnumerable<TSource> configuredSource,
            TCaptureKey keyCaptureValue, Func<TCaptureKey, TSource, TKey> keySelector,
            Func<TKey, TAccumulate> seedSelector,
            Func<TAccumulate, TSource, TAccumulate> accumulator)
            => AggregateBy(configuredSource, keyCaptureValue, keySelector, seedSelector, accumulator, EqualityComparer<TKey>.Default);

        /// <summary>
        /// Applies an accumulator function over each group of elements in a configured async-enumerable sequence according to a key selector function, using the specified equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence and the result of the aggregation.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TAccumulate">The type of the result of the aggregation.</typeparam>
        /// <typeparam name="TEqualityComparer">The type of the equality comparer.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to <paramref name="keySelector"/>.</typeparam>
        /// <param name="configuredSource">The configured source sequence.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="seedSelector">A function to retrieve the initial accumulator value for each group.</param>
        /// <param name="accumulator">An accumulator function to be invoked on each element.</param>
        /// <param name="comparer">An equality comparer used to compare keys for equality.</param>
        /// <returns>An async-enumerable sequence containing the aggregates corresponding to each key deriving from <paramref name="configuredSource"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="seedSelector"/> or <paramref name="accumulator"/> or <paramref name="comparer"/> is null.</exception>
        public static AsyncEnumerable<KeyValuePair<TKey, TAccumulate>> AggregateBy<TSource, TKey, TAccumulate, TEqualityComparer, TCaptureKey>(
            this in ConfiguredAsyncEnumerable<TSource> configuredSource,
            TCaptureKey keyCaptureValue, Func<TCaptureKey, TSource, TKey> keySelector,
            Func<TKey, TAccumulate> seedSelector,
            Func<TAccumulate, TSource, TAccumulate> accumulator,
            TEqualityComparer comparer)
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(seedSelector, nameof(seedSelector), 1);
            ValidateArgument(accumulator, nameof(accumulator), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.AggregateByHelper<TKey, TAccumulate>.AggregateBy(configuredSource.GetAsyncEnumerator(),
                DelegateWrapper.Create(keyCaptureValue, keySelector),
                DelegateWrapper.Create(seedSelector),
                DelegateWrapper.Create(accumulator),
                comparer);
        }

        /// <summary>
        /// Applies an accumulator function over each group of elements in a configured async-enumerable sequence according to a key selector function, using the default equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence and the result of the aggregation.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TAccumulate">The type of the result of the aggregation.</typeparam>
        /// <typeparam name="TCaptureAccumulate">The type of the captured value that will be passed to <paramref name="accumulator"/>.</typeparam>
        /// <param name="configuredSource">The configured source sequence.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="seedSelector">A function to retrieve the initial accumulator value for each group.</param>
        /// <param name="accumulateCaptureValue">The extra value that will be passed to <paramref name="accumulator"/>.</param>
        /// <param name="accumulator">An accumulator function to be invoked on each element.</param>
        /// <returns>An async-enumerable sequence containing the aggregates corresponding to each key deriving from <paramref name="configuredSource"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="seedSelector"/> or <paramref name="accumulator"/> is null.</exception>
        public static AsyncEnumerable<KeyValuePair<TKey, TAccumulate>> AggregateBy<TSource, TKey, TAccumulate, TCaptureAccumulate>(
            this in ConfiguredAsyncEnumerable<TSource> configuredSource,
            Func<TSource, TKey> keySelector,
            Func<TKey, TAccumulate> seedSelector,
            TCaptureAccumulate accumulateCaptureValue, Func<TCaptureAccumulate, TAccumulate, TSource, TAccumulate> accumulator)
            => AggregateBy(configuredSource, keySelector, seedSelector, accumulateCaptureValue, accumulator, EqualityComparer<TKey>.Default);

        /// <summary>
        /// Applies an accumulator function over each group of elements in a configured async-enumerable sequence according to a key selector function, using the specified equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence and the result of the aggregation.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TAccumulate">The type of the result of the aggregation.</typeparam>
        /// <typeparam name="TEqualityComparer">The type of the equality comparer.</typeparam>
        /// <typeparam name="TCaptureAccumulate">The type of the captured value that will be passed to <paramref name="accumulator"/>.</typeparam>
        /// <param name="configuredSource">The configured source sequence.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="seedSelector">A function to retrieve the initial accumulator value for each group.</param>
        /// <param name="accumulateCaptureValue">The extra value that will be passed to <paramref name="accumulator"/>.</param>
        /// <param name="accumulator">An accumulator function to be invoked on each element.</param>
        /// <param name="comparer">An equality comparer used to compare keys for equality.</param>
        /// <returns>An async-enumerable sequence containing the aggregates corresponding to each key deriving from <paramref name="configuredSource"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="seedSelector"/> or <paramref name="accumulator"/> or <paramref name="comparer"/> is null.</exception>
        public static AsyncEnumerable<KeyValuePair<TKey, TAccumulate>> AggregateBy<TSource, TKey, TAccumulate, TEqualityComparer, TCaptureAccumulate>(
            this in ConfiguredAsyncEnumerable<TSource> configuredSource,
            Func<TSource, TKey> keySelector,
            Func<TKey, TAccumulate> seedSelector,
            TCaptureAccumulate accumulateCaptureValue, Func<TCaptureAccumulate, TAccumulate, TSource, TAccumulate> accumulator,
            TEqualityComparer comparer)
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(seedSelector, nameof(seedSelector), 1);
            ValidateArgument(accumulator, nameof(accumulator), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.AggregateByHelper<TKey, TAccumulate>.AggregateBy(configuredSource.GetAsyncEnumerator(),
                DelegateWrapper.Create(keySelector),
                DelegateWrapper.Create(seedSelector),
                DelegateWrapper.Create(accumulateCaptureValue, accumulator),
                comparer);
        }

        /// <summary>
        /// Applies an accumulator function over each group of elements in a configured async-enumerable sequence according to a key selector function, using the default equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence and the result of the aggregation.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TAccumulate">The type of the result of the aggregation.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TCaptureAccumulate">The type of the captured value that will be passed to <paramref name="accumulator"/>.</typeparam>
        /// <param name="configuredSource">The configured source sequence.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="seedSelector">A function to retrieve the initial accumulator value for each group.</param>
        /// <param name="accumulateCaptureValue">The extra value that will be passed to <paramref name="accumulator"/>.</param>
        /// <param name="accumulator">An accumulator function to be invoked on each element.</param>
        /// <returns>An async-enumerable sequence containing the aggregates corresponding to each key deriving from <paramref name="configuredSource"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="seedSelector"/> or <paramref name="accumulator"/> is null.</exception>
        public static AsyncEnumerable<KeyValuePair<TKey, TAccumulate>> AggregateBy<TSource, TKey, TAccumulate, TCaptureKey, TCaptureAccumulate>(
            this in ConfiguredAsyncEnumerable<TSource> configuredSource,
            TCaptureKey keyCaptureValue, Func<TCaptureKey, TSource, TKey> keySelector,
            Func<TKey, TAccumulate> seedSelector,
            TCaptureAccumulate accumulateCaptureValue, Func<TCaptureAccumulate, TAccumulate, TSource, TAccumulate> accumulator)
            => AggregateBy(configuredSource, keyCaptureValue, keySelector, seedSelector, accumulateCaptureValue, accumulator, EqualityComparer<TKey>.Default);

        /// <summary>
        /// Applies an accumulator function over each group of elements in a configured async-enumerable sequence according to a key selector function, using the specified equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence and the result of the aggregation.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TAccumulate">The type of the result of the aggregation.</typeparam>
        /// <typeparam name="TEqualityComparer">The type of the equality comparer.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TCaptureAccumulate">The type of the captured value that will be passed to <paramref name="accumulator"/>.</typeparam>
        /// <param name="configuredSource">The configured source sequence.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="seedSelector">A function to retrieve the initial accumulator value for each group.</param>
        /// <param name="accumulateCaptureValue">The extra value that will be passed to <paramref name="accumulator"/>.</param>
        /// <param name="accumulator">An accumulator function to be invoked on each element.</param>
        /// <param name="comparer">An equality comparer used to compare keys for equality.</param>
        /// <returns>An async-enumerable sequence containing the aggregates corresponding to each key deriving from <paramref name="configuredSource"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="seedSelector"/> or <paramref name="accumulator"/> or <paramref name="comparer"/> is null.</exception>
        public static AsyncEnumerable<KeyValuePair<TKey, TAccumulate>> AggregateBy<TSource, TKey, TAccumulate, TEqualityComparer, TCaptureKey, TCaptureAccumulate>(
            this in ConfiguredAsyncEnumerable<TSource> configuredSource,
            TCaptureKey keyCaptureValue, Func<TCaptureKey, TSource, TKey> keySelector,
            Func<TKey, TAccumulate> seedSelector,
            TCaptureAccumulate accumulateCaptureValue, Func<TCaptureAccumulate, TAccumulate, TSource, TAccumulate> accumulator,
            TEqualityComparer comparer)
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(seedSelector, nameof(seedSelector), 1);
            ValidateArgument(accumulator, nameof(accumulator), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.AggregateByHelper<TKey, TAccumulate>.AggregateBy(configuredSource.GetAsyncEnumerator(),
                DelegateWrapper.Create(keyCaptureValue, keySelector),
                DelegateWrapper.Create(seedSelector),
                DelegateWrapper.Create(accumulateCaptureValue, accumulator),
                comparer);
        }

        /// <summary>
        /// Applies an accumulator function over each group of elements in a configured async-enumerable sequence according to a key selector function, using the default equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence and the result of the aggregation.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TAccumulate">The type of the result of the aggregation.</typeparam>
        /// <param name="configuredSource">The configured source sequence.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from each element.</param>
        /// <param name="seedSelector">An asynchronous function to retrieve the initial accumulator value for each group.</param>
        /// <param name="accumulator">An asynchronous accumulator function to be invoked on each element.</param>
        /// <returns>An async-enumerable sequence containing the aggregates corresponding to each key deriving from <paramref name="configuredSource"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="seedSelector"/> or <paramref name="accumulator"/> is null.</exception>
        public static AsyncEnumerable<KeyValuePair<TKey, TAccumulate>> AggregateBy<TSource, TKey, TAccumulate>(
            this in ConfiguredAsyncEnumerable<TSource> configuredSource,
            Func<TSource, CancelationToken, Promise<TKey>> keySelector,
            Func<TKey, CancelationToken, Promise<TAccumulate>> seedSelector,
            Func<TAccumulate, TSource, CancelationToken, Promise<TAccumulate>> accumulator)
            => AggregateBy(configuredSource, keySelector, seedSelector, accumulator, EqualityComparer<TKey>.Default);

        /// <summary>
        /// Applies an accumulator function over each group of elements in a configured async-enumerable sequence according to a key selector function, using the specified equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence and the result of the aggregation.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TAccumulate">The type of the result of the aggregation.</typeparam>
        /// <typeparam name="TEqualityComparer">The type of the equality comparer.</typeparam>
        /// <param name="configuredSource">The configured source sequence.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from each element.</param>
        /// <param name="seedSelector">An asynchronous function to retrieve the initial accumulator value for each group.</param>
        /// <param name="accumulator">An asynchronous accumulator function to be invoked on each element.</param>
        /// <param name="comparer">An equality comparer used to compare keys for equality.</param>
        /// <returns>An async-enumerable sequence containing the aggregates corresponding to each key deriving from <paramref name="configuredSource"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="seedSelector"/> or <paramref name="accumulator"/> or <paramref name="comparer"/> is null.</exception>
        public static AsyncEnumerable<KeyValuePair<TKey, TAccumulate>> AggregateBy<TSource, TKey, TAccumulate, TEqualityComparer>(
            this in ConfiguredAsyncEnumerable<TSource> configuredSource,
            Func<TSource, CancelationToken, Promise<TKey>> keySelector,
            Func<TKey, CancelationToken, Promise<TAccumulate>> seedSelector,
            Func<TAccumulate, TSource, CancelationToken, Promise<TAccumulate>> accumulator,
            TEqualityComparer comparer)
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(seedSelector, nameof(seedSelector), 1);
            ValidateArgument(accumulator, nameof(accumulator), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.AggregateByHelper<TKey, TAccumulate>.AggregateBy(configuredSource.GetAsyncEnumerator(),
                DelegateWrapper.Create(keySelector),
                DelegateWrapper.Create(seedSelector),
                DelegateWrapper.Create(accumulator),
                comparer);
        }

        /// <summary>
        /// Applies an accumulator function over each group of elements in a configured async-enumerable sequence according to a key selector function, using the default equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence and the result of the aggregation.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TAccumulate">The type of the result of the aggregation.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to <paramref name="keySelector"/>.</typeparam>
        /// <param name="configuredSource">The configured source sequence.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from each element.</param>
        /// <param name="seedSelector">An asynchronous function to retrieve the initial accumulator value for each group.</param>
        /// <param name="accumulator">An asynchronous accumulator function to be invoked on each element.</param>
        /// <returns>An async-enumerable sequence containing the aggregates corresponding to each key deriving from <paramref name="configuredSource"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="seedSelector"/> or <paramref name="accumulator"/> is null.</exception>
        public static AsyncEnumerable<KeyValuePair<TKey, TAccumulate>> AggregateBy<TSource, TKey, TAccumulate, TCaptureKey>(
            this in ConfiguredAsyncEnumerable<TSource> configuredSource,
            TCaptureKey keyCaptureValue, Func<TCaptureKey, TSource, CancelationToken, Promise<TKey>> keySelector,
            Func<TKey, CancelationToken, Promise<TAccumulate>> seedSelector,
            Func<TAccumulate, TSource, CancelationToken, Promise<TAccumulate>> accumulator)
            => AggregateBy(configuredSource, keyCaptureValue, keySelector, seedSelector, accumulator, EqualityComparer<TKey>.Default);

        /// <summary>
        /// Applies an accumulator function over each group of elements in a configured async-enumerable sequence according to a key selector function, using the specified equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence and the result of the aggregation.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TAccumulate">The type of the result of the aggregation.</typeparam>
        /// <typeparam name="TEqualityComparer">The type of the equality comparer.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to <paramref name="keySelector"/>.</typeparam>
        /// <param name="configuredSource">The configured source sequence.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from each element.</param>
        /// <param name="seedSelector">An asynchronous function to retrieve the initial accumulator value for each group.</param>
        /// <param name="accumulator">An asynchronous accumulator function to be invoked on each element.</param>
        /// <param name="comparer">An equality comparer used to compare keys for equality.</param>
        /// <returns>An async-enumerable sequence containing the aggregates corresponding to each key deriving from <paramref name="configuredSource"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="seedSelector"/> or <paramref name="accumulator"/> or <paramref name="comparer"/> is null.</exception>
        public static AsyncEnumerable<KeyValuePair<TKey, TAccumulate>> AggregateBy<TSource, TKey, TAccumulate, TEqualityComparer, TCaptureKey>(
            this in ConfiguredAsyncEnumerable<TSource> configuredSource,
            TCaptureKey keyCaptureValue, Func<TCaptureKey, TSource, CancelationToken, Promise<TKey>> keySelector,
            Func<TKey, CancelationToken, Promise<TAccumulate>> seedSelector,
            Func<TAccumulate, TSource, CancelationToken, Promise<TAccumulate>> accumulator,
            TEqualityComparer comparer)
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(seedSelector, nameof(seedSelector), 1);
            ValidateArgument(accumulator, nameof(accumulator), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.AggregateByHelper<TKey, TAccumulate>.AggregateBy(configuredSource.GetAsyncEnumerator(),
                DelegateWrapper.Create(keyCaptureValue, keySelector),
                DelegateWrapper.Create(seedSelector),
                DelegateWrapper.Create(accumulator),
                comparer);
        }

        /// <summary>
        /// Applies an accumulator function over each group of elements in a configured async-enumerable sequence according to a key selector function, using the default equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence and the result of the aggregation.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TAccumulate">The type of the result of the aggregation.</typeparam>
        /// <typeparam name="TCaptureAccumulate">The type of the captured value that will be passed to <paramref name="accumulator"/>.</typeparam>
        /// <param name="configuredSource">The configured source sequence.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from each element.</param>
        /// <param name="seedSelector">An asynchronous function to retrieve the initial accumulator value for each group.</param>
        /// <param name="accumulateCaptureValue">The extra value that will be passed to <paramref name="accumulator"/>.</param>
        /// <param name="accumulator">An asynchronous accumulator function to be invoked on each element.</param>
        /// <returns>An async-enumerable sequence containing the aggregates corresponding to each key deriving from <paramref name="configuredSource"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="seedSelector"/> or <paramref name="accumulator"/> is null.</exception>
        public static AsyncEnumerable<KeyValuePair<TKey, TAccumulate>> AggregateBy<TSource, TKey, TAccumulate, TCaptureAccumulate>(
            this in ConfiguredAsyncEnumerable<TSource> configuredSource,
            Func<TSource, CancelationToken, Promise<TKey>> keySelector,
            Func<TKey, CancelationToken, Promise<TAccumulate>> seedSelector,
            TCaptureAccumulate accumulateCaptureValue, Func<TCaptureAccumulate, TAccumulate, TSource, CancelationToken, Promise<TAccumulate>> accumulator)
            => AggregateBy(configuredSource, keySelector, seedSelector, accumulateCaptureValue, accumulator, EqualityComparer<TKey>.Default);

        /// <summary>
        /// Applies an accumulator function over each group of elements in a configured async-enumerable sequence according to a key selector function, using the specified equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence and the result of the aggregation.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TAccumulate">The type of the result of the aggregation.</typeparam>
        /// <typeparam name="TEqualityComparer">The type of the equality comparer.</typeparam>
        /// <typeparam name="TCaptureAccumulate">The type of the captured value that will be passed to <paramref name="accumulator"/>.</typeparam>
        /// <param name="configuredSource">The configured source sequence.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from each element.</param>
        /// <param name="seedSelector">An asynchronous function to retrieve the initial accumulator value for each group.</param>
        /// <param name="accumulateCaptureValue">The extra value that will be passed to <paramref name="accumulator"/>.</param>
        /// <param name="accumulator">An asynchronous accumulator function to be invoked on each element.</param>
        /// <param name="comparer">An equality comparer used to compare keys for equality.</param>
        /// <returns>An async-enumerable sequence containing the aggregates corresponding to each key deriving from <paramref name="configuredSource"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="seedSelector"/> or <paramref name="accumulator"/> or <paramref name="comparer"/> is null.</exception>
        public static AsyncEnumerable<KeyValuePair<TKey, TAccumulate>> AggregateBy<TSource, TKey, TAccumulate, TEqualityComparer, TCaptureAccumulate>(
            this in ConfiguredAsyncEnumerable<TSource> configuredSource,
            Func<TSource, CancelationToken, Promise<TKey>> keySelector,
            Func<TKey, CancelationToken, Promise<TAccumulate>> seedSelector,
            TCaptureAccumulate accumulateCaptureValue, Func<TCaptureAccumulate, TAccumulate, TSource, CancelationToken, Promise<TAccumulate>> accumulator,
            TEqualityComparer comparer)
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(seedSelector, nameof(seedSelector), 1);
            ValidateArgument(accumulator, nameof(accumulator), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.AggregateByHelper<TKey, TAccumulate>.AggregateBy(configuredSource.GetAsyncEnumerator(),
                DelegateWrapper.Create(keySelector),
                DelegateWrapper.Create(seedSelector),
                DelegateWrapper.Create(accumulateCaptureValue, accumulator),
                comparer);
        }

        /// <summary>
        /// Applies an accumulator function over each group of elements in a configured async-enumerable sequence according to a key selector function, using the default equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence and the result of the aggregation.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TAccumulate">The type of the result of the aggregation.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TCaptureAccumulate">The type of the captured value that will be passed to <paramref name="accumulator"/>.</typeparam>
        /// <param name="configuredSource">The configured source sequence.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from each element.</param>
        /// <param name="seedSelector">An asynchronous function to retrieve the initial accumulator value for each group.</param>
        /// <param name="accumulateCaptureValue">The extra value that will be passed to <paramref name="accumulator"/>.</param>
        /// <param name="accumulator">An asynchronous accumulator function to be invoked on each element.</param>
        /// <returns>An async-enumerable sequence containing the aggregates corresponding to each key deriving from <paramref name="configuredSource"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="seedSelector"/> or <paramref name="accumulator"/> is null.</exception>
        public static AsyncEnumerable<KeyValuePair<TKey, TAccumulate>> AggregateBy<TSource, TKey, TAccumulate, TCaptureKey, TCaptureAccumulate>(
            this in ConfiguredAsyncEnumerable<TSource> configuredSource,
            TCaptureKey keyCaptureValue, Func<TCaptureKey, TSource, CancelationToken, Promise<TKey>> keySelector,
            Func<TKey, CancelationToken, Promise<TAccumulate>> seedSelector,
            TCaptureAccumulate accumulateCaptureValue, Func<TCaptureAccumulate, TAccumulate, TSource, CancelationToken, Promise<TAccumulate>> accumulator)
            => AggregateBy(configuredSource, keyCaptureValue, keySelector, seedSelector, accumulateCaptureValue, accumulator, EqualityComparer<TKey>.Default);

        /// <summary>
        /// Applies an accumulator function over each group of elements in a configured async-enumerable sequence according to a key selector function, using the specified equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence and the result of the aggregation.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TAccumulate">The type of the result of the aggregation.</typeparam>
        /// <typeparam name="TEqualityComparer">The type of the equality comparer.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TCaptureAccumulate">The type of the captured value that will be passed to <paramref name="accumulator"/>.</typeparam>
        /// <param name="configuredSource">The configured source sequence.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from each element.</param>
        /// <param name="seedSelector">An asynchronous function to retrieve the initial accumulator value for each group.</param>
        /// <param name="accumulateCaptureValue">The extra value that will be passed to <paramref name="accumulator"/>.</param>
        /// <param name="accumulator">An asynchronous accumulator function to be invoked on each element.</param>
        /// <param name="comparer">An equality comparer used to compare keys for equality.</param>
        /// <returns>An async-enumerable sequence containing the aggregates corresponding to each key deriving from <paramref name="configuredSource"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="seedSelector"/> or <paramref name="accumulator"/> or <paramref name="comparer"/> is null.</exception>
        public static AsyncEnumerable<KeyValuePair<TKey, TAccumulate>> AggregateBy<TSource, TKey, TAccumulate, TEqualityComparer, TCaptureKey, TCaptureAccumulate>(
            this in ConfiguredAsyncEnumerable<TSource> configuredSource,
            TCaptureKey keyCaptureValue, Func<TCaptureKey, TSource, CancelationToken, Promise<TKey>> keySelector,
            Func<TKey, CancelationToken, Promise<TAccumulate>> seedSelector,
            TCaptureAccumulate accumulateCaptureValue, Func<TCaptureAccumulate, TAccumulate, TSource, CancelationToken, Promise<TAccumulate>> accumulator,
            TEqualityComparer comparer)
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(seedSelector, nameof(seedSelector), 1);
            ValidateArgument(accumulator, nameof(accumulator), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.AggregateByHelper<TKey, TAccumulate>.AggregateBy(configuredSource.GetAsyncEnumerator(),
                DelegateWrapper.Create(keyCaptureValue, keySelector),
                DelegateWrapper.Create(seedSelector),
                DelegateWrapper.Create(accumulateCaptureValue, accumulator),
                comparer);
        }

        /// <summary>
        /// Applies an accumulator function over each group of elements in an async-enumerable sequence according to a key selector function, using the default equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence and the result of the aggregation.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TAccumulate">The type of the result of the aggregation.</typeparam>
        /// <typeparam name="TCaptureSeed">The type of the captured value that will be passed to <paramref name="seedSelector"/>.</typeparam>
        /// <param name="source">The source sequence.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="seedCaptureValue">The extra value that will be passed to <paramref name="seedSelector"/>.</param>
        /// <param name="seedSelector">A function to retrieve the initial accumulator value for each group.</param>
        /// <param name="accumulator">An accumulator function to be invoked on each element.</param>
        /// <returns>An async-enumerable sequence containing the aggregates corresponding to each key deriving from <paramref name="source"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="seedSelector"/> or <paramref name="accumulator"/> is null.</exception>
        public static AsyncEnumerable<KeyValuePair<TKey, TAccumulate>> AggregateBy<TSource, TKey, TAccumulate, TCaptureSeed>(this AsyncEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            TCaptureSeed seedCaptureValue, Func<TCaptureSeed, TKey, TAccumulate> seedSelector,
            Func<TAccumulate, TSource, TAccumulate> accumulator)
            => AggregateBy(source, keySelector, seedCaptureValue, seedSelector, accumulator, EqualityComparer<TKey>.Default);

        /// <summary>
        /// Applies an accumulator function over each group of elements in an async-enumerable sequence according to a key selector function, using the specified equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence and the result of the aggregation.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TAccumulate">The type of the result of the aggregation.</typeparam>
        /// <typeparam name="TEqualityComparer">The type of the equality comparer.</typeparam>
        /// <typeparam name="TCaptureSeed">The type of the captured value that will be passed to <paramref name="seedSelector"/>.</typeparam>
        /// <param name="source">The source sequence.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="seedCaptureValue">The extra value that will be passed to <paramref name="seedSelector"/>.</param>
        /// <param name="seedSelector">A function to retrieve the initial accumulator value for each group.</param>
        /// <param name="accumulator">An accumulator function to be invoked on each element.</param>
        /// <param name="comparer">An equality comparer used to compare keys for equality.</param>
        /// <returns>An async-enumerable sequence containing the aggregates corresponding to each key deriving from <paramref name="source"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="seedSelector"/> or <paramref name="accumulator"/> or <paramref name="comparer"/> is null.</exception>
        public static AsyncEnumerable<KeyValuePair<TKey, TAccumulate>> AggregateBy<TSource, TKey, TAccumulate, TEqualityComparer, TCaptureSeed>(this AsyncEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            TCaptureSeed seedCaptureValue, Func<TCaptureSeed, TKey, TAccumulate> seedSelector,
            Func<TAccumulate, TSource, TAccumulate> accumulator,
            TEqualityComparer comparer)
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(seedSelector, nameof(seedSelector), 1);
            ValidateArgument(accumulator, nameof(accumulator), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.AggregateByHelper<TKey, TAccumulate>.AggregateBy(source.GetAsyncEnumerator(),
                DelegateWrapper.Create(keySelector),
                DelegateWrapper.Create(seedCaptureValue, seedSelector),
                DelegateWrapper.Create(accumulator),
                comparer);
        }

        /// <summary>
        /// Applies an accumulator function over each group of elements in an async-enumerable sequence according to a key selector function, using the default equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence and the result of the aggregation.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TAccumulate">The type of the result of the aggregation.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TCaptureSeed">The type of the captured value that will be passed to <paramref name="seedSelector"/>.</typeparam>
        /// <param name="source">The source sequence.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="seedCaptureValue">The extra value that will be passed to <paramref name="seedSelector"/>.</param>
        /// <param name="seedSelector">A function to retrieve the initial accumulator value for each group.</param>
        /// <param name="accumulator">An accumulator function to be invoked on each element.</param>
        /// <returns>An async-enumerable sequence containing the aggregates corresponding to each key deriving from <paramref name="source"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="seedSelector"/> or <paramref name="accumulator"/> is null.</exception>
        public static AsyncEnumerable<KeyValuePair<TKey, TAccumulate>> AggregateBy<TSource, TKey, TAccumulate, TCaptureKey, TCaptureSeed>(this AsyncEnumerable<TSource> source,
            TCaptureKey keyCaptureValue, Func<TCaptureKey, TSource, TKey> keySelector,
            TCaptureSeed seedCaptureValue, Func<TCaptureSeed, TKey, TAccumulate> seedSelector,
            Func<TAccumulate, TSource, TAccumulate> accumulator)
            => AggregateBy(source, keyCaptureValue, keySelector, seedCaptureValue, seedSelector, accumulator, EqualityComparer<TKey>.Default);

        /// <summary>
        /// Applies an accumulator function over each group of elements in an async-enumerable sequence according to a key selector function, using the specified equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence and the result of the aggregation.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TAccumulate">The type of the result of the aggregation.</typeparam>
        /// <typeparam name="TEqualityComparer">The type of the equality comparer.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TCaptureSeed">The type of the captured value that will be passed to <paramref name="seedSelector"/>.</typeparam>
        /// <param name="source">The source sequence.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="seedCaptureValue">The extra value that will be passed to <paramref name="seedSelector"/>.</param>
        /// <param name="seedSelector">A function to retrieve the initial accumulator value for each group.</param>
        /// <param name="accumulator">An accumulator function to be invoked on each element.</param>
        /// <param name="comparer">An equality comparer used to compare keys for equality.</param>
        /// <returns>An async-enumerable sequence containing the aggregates corresponding to each key deriving from <paramref name="source"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="seedSelector"/> or <paramref name="accumulator"/> or <paramref name="comparer"/> is null.</exception>
        public static AsyncEnumerable<KeyValuePair<TKey, TAccumulate>> AggregateBy<TSource, TKey, TAccumulate, TEqualityComparer, TCaptureKey, TCaptureSeed>(this AsyncEnumerable<TSource> source,
            TCaptureKey keyCaptureValue, Func<TCaptureKey, TSource, TKey> keySelector,
            TCaptureSeed seedCaptureValue, Func<TCaptureSeed, TKey, TAccumulate> seedSelector,
            Func<TAccumulate, TSource, TAccumulate> accumulator,
            TEqualityComparer comparer)
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(seedSelector, nameof(seedSelector), 1);
            ValidateArgument(accumulator, nameof(accumulator), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.AggregateByHelper<TKey, TAccumulate>.AggregateBy(source.GetAsyncEnumerator(),
                DelegateWrapper.Create(keyCaptureValue, keySelector),
                DelegateWrapper.Create(seedCaptureValue, seedSelector),
                DelegateWrapper.Create(accumulator),
                comparer);
        }

        /// <summary>
        /// Applies an accumulator function over each group of elements in an async-enumerable sequence according to a key selector function, using the default equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence and the result of the aggregation.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TAccumulate">The type of the result of the aggregation.</typeparam>
        /// <typeparam name="TCaptureSeed">The type of the captured value that will be passed to <paramref name="seedSelector"/>.</typeparam>
        /// <typeparam name="TCaptureAccumulate">The type of the captured value that will be passed to <paramref name="accumulator"/>.</typeparam>
        /// <param name="source">The source sequence.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="seedCaptureValue">The extra value that will be passed to <paramref name="seedSelector"/>.</param>
        /// <param name="seedSelector">A function to retrieve the initial accumulator value for each group.</param>
        /// <param name="accumulateCaptureValue">The extra value that will be passed to <paramref name="accumulator"/>.</param>
        /// <param name="accumulator">An accumulator function to be invoked on each element.</param>
        /// <returns>An async-enumerable sequence containing the aggregates corresponding to each key deriving from <paramref name="source"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="seedSelector"/> or <paramref name="accumulator"/> is null.</exception>
        public static AsyncEnumerable<KeyValuePair<TKey, TAccumulate>> AggregateBy<TSource, TKey, TAccumulate, TCaptureSeed, TCaptureAccumulate>(this AsyncEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            TCaptureSeed seedCaptureValue, Func<TCaptureSeed, TKey, TAccumulate> seedSelector,
            TCaptureAccumulate accumulateCaptureValue, Func<TCaptureAccumulate, TAccumulate, TSource, TAccumulate> accumulator)
            => AggregateBy(source, keySelector, seedCaptureValue, seedSelector, accumulateCaptureValue, accumulator, EqualityComparer<TKey>.Default);

        /// <summary>
        /// Applies an accumulator function over each group of elements in an async-enumerable sequence according to a key selector function, using the specified equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence and the result of the aggregation.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TAccumulate">The type of the result of the aggregation.</typeparam>
        /// <typeparam name="TEqualityComparer">The type of the equality comparer.</typeparam>
        /// <typeparam name="TCaptureSeed">The type of the captured value that will be passed to <paramref name="seedSelector"/>.</typeparam>
        /// <typeparam name="TCaptureAccumulate">The type of the captured value that will be passed to <paramref name="accumulator"/>.</typeparam>
        /// <param name="source">The source sequence.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="seedCaptureValue">The extra value that will be passed to <paramref name="seedSelector"/>.</param>
        /// <param name="seedSelector">A function to retrieve the initial accumulator value for each group.</param>
        /// <param name="accumulateCaptureValue">The extra value that will be passed to <paramref name="accumulator"/>.</param>
        /// <param name="accumulator">An accumulator function to be invoked on each element.</param>
        /// <param name="comparer">An equality comparer used to compare keys for equality.</param>
        /// <returns>An async-enumerable sequence containing the aggregates corresponding to each key deriving from <paramref name="source"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="seedSelector"/> or <paramref name="accumulator"/> or <paramref name="comparer"/> is null.</exception>
        public static AsyncEnumerable<KeyValuePair<TKey, TAccumulate>> AggregateBy<TSource, TKey, TAccumulate, TEqualityComparer, TCaptureSeed, TCaptureAccumulate>(this AsyncEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            TCaptureSeed seedCaptureValue, Func<TCaptureSeed, TKey, TAccumulate> seedSelector,
            TCaptureAccumulate accumulateCaptureValue, Func<TCaptureAccumulate, TAccumulate, TSource, TAccumulate> accumulator,
            TEqualityComparer comparer)
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(seedSelector, nameof(seedSelector), 1);
            ValidateArgument(accumulator, nameof(accumulator), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.AggregateByHelper<TKey, TAccumulate>.AggregateBy(source.GetAsyncEnumerator(),
                DelegateWrapper.Create(keySelector),
                DelegateWrapper.Create(seedCaptureValue, seedSelector),
                DelegateWrapper.Create(accumulateCaptureValue, accumulator),
                comparer);
        }

        /// <summary>
        /// Applies an accumulator function over each group of elements in an async-enumerable sequence according to a key selector function, using the default equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence and the result of the aggregation.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TAccumulate">The type of the result of the aggregation.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TCaptureSeed">The type of the captured value that will be passed to <paramref name="seedSelector"/>.</typeparam>
        /// <typeparam name="TCaptureAccumulate">The type of the captured value that will be passed to <paramref name="accumulator"/>.</typeparam>
        /// <param name="source">The source sequence.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="seedCaptureValue">The extra value that will be passed to <paramref name="seedSelector"/>.</param>
        /// <param name="seedSelector">A function to retrieve the initial accumulator value for each group.</param>
        /// <param name="accumulateCaptureValue">The extra value that will be passed to <paramref name="accumulator"/>.</param>
        /// <param name="accumulator">An accumulator function to be invoked on each element.</param>
        /// <returns>An async-enumerable sequence containing the aggregates corresponding to each key deriving from <paramref name="source"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="seedSelector"/> or <paramref name="accumulator"/> is null.</exception>
        public static AsyncEnumerable<KeyValuePair<TKey, TAccumulate>> AggregateBy<TSource, TKey, TAccumulate, TCaptureKey, TCaptureSeed, TCaptureAccumulate>(this AsyncEnumerable<TSource> source,
            TCaptureKey keyCaptureValue, Func<TCaptureKey, TSource, TKey> keySelector,
            TCaptureSeed seedCaptureValue, Func<TCaptureSeed, TKey, TAccumulate> seedSelector,
            TCaptureAccumulate accumulateCaptureValue, Func<TCaptureAccumulate, TAccumulate, TSource, TAccumulate> accumulator)
            => AggregateBy(source, keyCaptureValue, keySelector, seedCaptureValue, seedSelector, accumulateCaptureValue, accumulator, EqualityComparer<TKey>.Default);

        /// <summary>
        /// Applies an accumulator function over each group of elements in an async-enumerable sequence according to a key selector function, using the specified equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence and the result of the aggregation.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TAccumulate">The type of the result of the aggregation.</typeparam>
        /// <typeparam name="TEqualityComparer">The type of the equality comparer.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TCaptureSeed">The type of the captured value that will be passed to <paramref name="seedSelector"/>.</typeparam>
        /// <typeparam name="TCaptureAccumulate">The type of the captured value that will be passed to <paramref name="accumulator"/>.</typeparam>
        /// <param name="source">The source sequence.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="seedCaptureValue">The extra value that will be passed to <paramref name="seedSelector"/>.</param>
        /// <param name="seedSelector">A function to retrieve the initial accumulator value for each group.</param>
        /// <param name="accumulateCaptureValue">The extra value that will be passed to <paramref name="accumulator"/>.</param>
        /// <param name="accumulator">An accumulator function to be invoked on each element.</param>
        /// <param name="comparer">An equality comparer used to compare keys for equality.</param>
        /// <returns>An async-enumerable sequence containing the aggregates corresponding to each key deriving from <paramref name="source"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="seedSelector"/> or <paramref name="accumulator"/> or <paramref name="comparer"/> is null.</exception>
        public static AsyncEnumerable<KeyValuePair<TKey, TAccumulate>> AggregateBy<TSource, TKey, TAccumulate, TEqualityComparer, TCaptureKey, TCaptureSeed, TCaptureAccumulate>(this AsyncEnumerable<TSource> source,
            TCaptureKey keyCaptureValue, Func<TCaptureKey, TSource, TKey> keySelector,
            TCaptureSeed seedCaptureValue, Func<TCaptureSeed, TKey, TAccumulate> seedSelector,
            TCaptureAccumulate accumulateCaptureValue, Func<TCaptureAccumulate, TAccumulate, TSource, TAccumulate> accumulator,
            TEqualityComparer comparer)
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(seedSelector, nameof(seedSelector), 1);
            ValidateArgument(accumulator, nameof(accumulator), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.AggregateByHelper<TKey, TAccumulate>.AggregateBy(source.GetAsyncEnumerator(),
                DelegateWrapper.Create(keyCaptureValue, keySelector),
                DelegateWrapper.Create(seedCaptureValue, seedSelector),
                DelegateWrapper.Create(accumulateCaptureValue, accumulator),
                comparer);
        }

        /// <summary>
        /// Applies an accumulator function over each group of elements in an async-enumerable sequence according to a key selector function, using the default equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence and the result of the aggregation.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TAccumulate">The type of the result of the aggregation.</typeparam>
        /// <typeparam name="TCaptureSeed">The type of the captured value that will be passed to <paramref name="seedSelector"/>.</typeparam>
        /// <param name="source">The source sequence.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from each element.</param>
        /// <param name="seedCaptureValue">The extra value that will be passed to <paramref name="seedSelector"/>.</param>
        /// <param name="seedSelector">An asynchronous function to retrieve the initial accumulator value for each group.</param>
        /// <param name="accumulator">An asynchronous accumulator function to be invoked on each element.</param>
        /// <returns>An async-enumerable sequence containing the aggregates corresponding to each key deriving from <paramref name="source"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="seedSelector"/> or <paramref name="accumulator"/> is null.</exception>
        public static AsyncEnumerable<KeyValuePair<TKey, TAccumulate>> AggregateBy<TSource, TKey, TAccumulate, TCaptureSeed>(this AsyncEnumerable<TSource> source,
            Func<TSource, CancelationToken, Promise<TKey>> keySelector,
            TCaptureSeed seedCaptureValue, Func<TCaptureSeed, TKey, CancelationToken, Promise<TAccumulate>> seedSelector,
            Func<TAccumulate, TSource, CancelationToken, Promise<TAccumulate>> accumulator)
            => AggregateBy(source, keySelector, seedCaptureValue, seedSelector, accumulator, EqualityComparer<TKey>.Default);

        /// <summary>
        /// Applies an accumulator function over each group of elements in an async-enumerable sequence according to a key selector function, using the specified equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence and the result of the aggregation.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TAccumulate">The type of the result of the aggregation.</typeparam>
        /// <typeparam name="TEqualityComparer">The type of the equality comparer.</typeparam>
        /// <typeparam name="TCaptureSeed">The type of the captured value that will be passed to <paramref name="seedSelector"/>.</typeparam>
        /// <param name="source">The source sequence.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from each element.</param>
        /// <param name="seedCaptureValue">The extra value that will be passed to <paramref name="seedSelector"/>.</param>
        /// <param name="seedSelector">An asynchronous function to retrieve the initial accumulator value for each group.</param>
        /// <param name="accumulator">An asynchronous accumulator function to be invoked on each element.</param>
        /// <param name="comparer">An equality comparer used to compare keys for equality.</param>
        /// <returns>An async-enumerable sequence containing the aggregates corresponding to each key deriving from <paramref name="source"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="seedSelector"/> or <paramref name="accumulator"/> or <paramref name="comparer"/> is null.</exception>
        public static AsyncEnumerable<KeyValuePair<TKey, TAccumulate>> AggregateBy<TSource, TKey, TAccumulate, TEqualityComparer, TCaptureSeed>(this AsyncEnumerable<TSource> source,
            Func<TSource, CancelationToken, Promise<TKey>> keySelector,
            TCaptureSeed seedCaptureValue, Func<TCaptureSeed, TKey, CancelationToken, Promise<TAccumulate>> seedSelector,
            Func<TAccumulate, TSource, CancelationToken, Promise<TAccumulate>> accumulator,
            TEqualityComparer comparer)
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(seedSelector, nameof(seedSelector), 1);
            ValidateArgument(accumulator, nameof(accumulator), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.AggregateByHelper<TKey, TAccumulate>.AggregateBy(source.GetAsyncEnumerator(),
                DelegateWrapper.Create(keySelector),
                DelegateWrapper.Create(seedCaptureValue, seedSelector),
                DelegateWrapper.Create(accumulator),
                comparer);
        }

        /// <summary>
        /// Applies an accumulator function over each group of elements in an async-enumerable sequence according to a key selector function, using the default equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence and the result of the aggregation.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TAccumulate">The type of the result of the aggregation.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TCaptureSeed">The type of the captured value that will be passed to <paramref name="seedSelector"/>.</typeparam>
        /// <param name="source">The source sequence.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from each element.</param>
        /// <param name="seedCaptureValue">The extra value that will be passed to <paramref name="seedSelector"/>.</param>
        /// <param name="seedSelector">An asynchronous function to retrieve the initial accumulator value for each group.</param>
        /// <param name="accumulator">An asynchronous accumulator function to be invoked on each element.</param>
        /// <returns>An async-enumerable sequence containing the aggregates corresponding to each key deriving from <paramref name="source"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="seedSelector"/> or <paramref name="accumulator"/> is null.</exception>
        public static AsyncEnumerable<KeyValuePair<TKey, TAccumulate>> AggregateBy<TSource, TKey, TAccumulate, TCaptureKey, TCaptureSeed>(this AsyncEnumerable<TSource> source,
            TCaptureKey keyCaptureValue, Func<TCaptureKey, TSource, CancelationToken, Promise<TKey>> keySelector,
            TCaptureSeed seedCaptureValue, Func<TCaptureSeed, TKey, CancelationToken, Promise<TAccumulate>> seedSelector,
            Func<TAccumulate, TSource, CancelationToken, Promise<TAccumulate>> accumulator)
            => AggregateBy(source, keyCaptureValue, keySelector, seedCaptureValue, seedSelector, accumulator, EqualityComparer<TKey>.Default);

        /// <summary>
        /// Applies an accumulator function over each group of elements in an async-enumerable sequence according to a key selector function, using the specified equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence and the result of the aggregation.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TAccumulate">The type of the result of the aggregation.</typeparam>
        /// <typeparam name="TEqualityComparer">The type of the equality comparer.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TCaptureSeed">The type of the captured value that will be passed to <paramref name="seedSelector"/>.</typeparam>
        /// <param name="source">The source sequence.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from each element.</param>
        /// <param name="seedCaptureValue">The extra value that will be passed to <paramref name="seedSelector"/>.</param>
        /// <param name="seedSelector">An asynchronous function to retrieve the initial accumulator value for each group.</param>
        /// <param name="accumulator">An asynchronous accumulator function to be invoked on each element.</param>
        /// <param name="comparer">An equality comparer used to compare keys for equality.</param>
        /// <returns>An async-enumerable sequence containing the aggregates corresponding to each key deriving from <paramref name="source"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="seedSelector"/> or <paramref name="accumulator"/> or <paramref name="comparer"/> is null.</exception>
        public static AsyncEnumerable<KeyValuePair<TKey, TAccumulate>> AggregateBy<TSource, TKey, TAccumulate, TEqualityComparer, TCaptureKey, TCaptureSeed>(this AsyncEnumerable<TSource> source,
            TCaptureKey keyCaptureValue, Func<TCaptureKey, TSource, CancelationToken, Promise<TKey>> keySelector,
            TCaptureSeed seedCaptureValue, Func<TCaptureSeed, TKey, CancelationToken, Promise<TAccumulate>> seedSelector,
            Func<TAccumulate, TSource, CancelationToken, Promise<TAccumulate>> accumulator,
            TEqualityComparer comparer)
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(seedSelector, nameof(seedSelector), 1);
            ValidateArgument(accumulator, nameof(accumulator), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.AggregateByHelper<TKey, TAccumulate>.AggregateBy(source.GetAsyncEnumerator(),
                DelegateWrapper.Create(keyCaptureValue, keySelector),
                DelegateWrapper.Create(seedCaptureValue, seedSelector),
                DelegateWrapper.Create(accumulator),
                comparer);
        }

        /// <summary>
        /// Applies an accumulator function over each group of elements in an async-enumerable sequence according to a key selector function, using the default equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence and the result of the aggregation.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TAccumulate">The type of the result of the aggregation.</typeparam>
        /// <typeparam name="TCaptureSeed">The type of the captured value that will be passed to <paramref name="seedSelector"/>.</typeparam>
        /// <typeparam name="TCaptureAccumulate">The type of the captured value that will be passed to <paramref name="accumulator"/>.</typeparam>
        /// <param name="source">The source sequence.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from each element.</param>
        /// <param name="seedCaptureValue">The extra value that will be passed to <paramref name="seedSelector"/>.</param>
        /// <param name="seedSelector">An asynchronous function to retrieve the initial accumulator value for each group.</param>
        /// <param name="accumulateCaptureValue">The extra value that will be passed to <paramref name="accumulator"/>.</param>
        /// <param name="accumulator">An asynchronous accumulator function to be invoked on each element.</param>
        /// <returns>An async-enumerable sequence containing the aggregates corresponding to each key deriving from <paramref name="source"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="seedSelector"/> or <paramref name="accumulator"/> is null.</exception>
        public static AsyncEnumerable<KeyValuePair<TKey, TAccumulate>> AggregateBy<TSource, TKey, TAccumulate, TCaptureSeed, TCaptureAccumulate>(this AsyncEnumerable<TSource> source,
            Func<TSource, CancelationToken, Promise<TKey>> keySelector,
            TCaptureSeed seedCaptureValue, Func<TCaptureSeed, TKey, CancelationToken, Promise<TAccumulate>> seedSelector,
            TCaptureAccumulate accumulateCaptureValue, Func<TCaptureAccumulate, TAccumulate, TSource, CancelationToken, Promise<TAccumulate>> accumulator)
            => AggregateBy(source, keySelector, seedCaptureValue, seedSelector, accumulateCaptureValue, accumulator, EqualityComparer<TKey>.Default);

        /// <summary>
        /// Applies an accumulator function over each group of elements in an async-enumerable sequence according to a key selector function, using the specified equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence and the result of the aggregation.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TAccumulate">The type of the result of the aggregation.</typeparam>
        /// <typeparam name="TEqualityComparer">The type of the equality comparer.</typeparam>
        /// <typeparam name="TCaptureSeed">The type of the captured value that will be passed to <paramref name="seedSelector"/>.</typeparam>
        /// <typeparam name="TCaptureAccumulate">The type of the captured value that will be passed to <paramref name="accumulator"/>.</typeparam>
        /// <param name="source">The source sequence.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from each element.</param>
        /// <param name="seedCaptureValue">The extra value that will be passed to <paramref name="seedSelector"/>.</param>
        /// <param name="seedSelector">An asynchronous function to retrieve the initial accumulator value for each group.</param>
        /// <param name="accumulateCaptureValue">The extra value that will be passed to <paramref name="accumulator"/>.</param>
        /// <param name="accumulator">An asynchronous accumulator function to be invoked on each element.</param>
        /// <param name="comparer">An equality comparer used to compare keys for equality.</param>
        /// <returns>An async-enumerable sequence containing the aggregates corresponding to each key deriving from <paramref name="source"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="seedSelector"/> or <paramref name="accumulator"/> or <paramref name="comparer"/> is null.</exception>
        public static AsyncEnumerable<KeyValuePair<TKey, TAccumulate>> AggregateBy<TSource, TKey, TAccumulate, TEqualityComparer, TCaptureSeed, TCaptureAccumulate>(this AsyncEnumerable<TSource> source,
            Func<TSource, CancelationToken, Promise<TKey>> keySelector,
            TCaptureSeed seedCaptureValue, Func<TCaptureSeed, TKey, CancelationToken, Promise<TAccumulate>> seedSelector,
            TCaptureAccumulate accumulateCaptureValue, Func<TCaptureAccumulate, TAccumulate, TSource, CancelationToken, Promise<TAccumulate>> accumulator,
            TEqualityComparer comparer)
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(seedSelector, nameof(seedSelector), 1);
            ValidateArgument(accumulator, nameof(accumulator), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.AggregateByHelper<TKey, TAccumulate>.AggregateBy(source.GetAsyncEnumerator(),
                DelegateWrapper.Create(keySelector),
                DelegateWrapper.Create(seedCaptureValue, seedSelector),
                DelegateWrapper.Create(accumulateCaptureValue, accumulator),
                comparer);
        }

        /// <summary>
        /// Applies an accumulator function over each group of elements in an async-enumerable sequence according to a key selector function, using the default equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence and the result of the aggregation.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TAccumulate">The type of the result of the aggregation.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TCaptureSeed">The type of the captured value that will be passed to <paramref name="seedSelector"/>.</typeparam>
        /// <typeparam name="TCaptureAccumulate">The type of the captured value that will be passed to <paramref name="accumulator"/>.</typeparam>
        /// <param name="source">The source sequence.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from each element.</param>
        /// <param name="seedCaptureValue">The extra value that will be passed to <paramref name="seedSelector"/>.</param>
        /// <param name="seedSelector">An asynchronous function to retrieve the initial accumulator value for each group.</param>
        /// <param name="accumulateCaptureValue">The extra value that will be passed to <paramref name="accumulator"/>.</param>
        /// <param name="accumulator">An asynchronous accumulator function to be invoked on each element.</param>
        /// <returns>An async-enumerable sequence containing the aggregates corresponding to each key deriving from <paramref name="source"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="seedSelector"/> or <paramref name="accumulator"/> is null.</exception>
        public static AsyncEnumerable<KeyValuePair<TKey, TAccumulate>> AggregateBy<TSource, TKey, TAccumulate, TCaptureKey, TCaptureSeed, TCaptureAccumulate>(this AsyncEnumerable<TSource> source,
            TCaptureKey keyCaptureValue, Func<TCaptureKey, TSource, CancelationToken, Promise<TKey>> keySelector,
            TCaptureSeed seedCaptureValue, Func<TCaptureSeed, TKey, CancelationToken, Promise<TAccumulate>> seedSelector,
            TCaptureAccumulate accumulateCaptureValue, Func<TCaptureAccumulate, TAccumulate, TSource, CancelationToken, Promise<TAccumulate>> accumulator)
            => AggregateBy(source, keyCaptureValue, keySelector, seedCaptureValue, seedSelector, accumulateCaptureValue, accumulator, EqualityComparer<TKey>.Default);

        /// <summary>
        /// Applies an accumulator function over each group of elements in an async-enumerable sequence according to a key selector function, using the specified equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence and the result of the aggregation.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TAccumulate">The type of the result of the aggregation.</typeparam>
        /// <typeparam name="TEqualityComparer">The type of the equality comparer.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TCaptureSeed">The type of the captured value that will be passed to <paramref name="seedSelector"/>.</typeparam>
        /// <typeparam name="TCaptureAccumulate">The type of the captured value that will be passed to <paramref name="accumulator"/>.</typeparam>
        /// <param name="source">The source sequence.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from each element.</param>
        /// <param name="seedCaptureValue">The extra value that will be passed to <paramref name="seedSelector"/>.</param>
        /// <param name="seedSelector">An asynchronous function to retrieve the initial accumulator value for each group.</param>
        /// <param name="accumulateCaptureValue">The extra value that will be passed to <paramref name="accumulator"/>.</param>
        /// <param name="accumulator">An asynchronous accumulator function to be invoked on each element.</param>
        /// <param name="comparer">An equality comparer used to compare keys for equality.</param>
        /// <returns>An async-enumerable sequence containing the aggregates corresponding to each key deriving from <paramref name="source"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="seedSelector"/> or <paramref name="accumulator"/> or <paramref name="comparer"/> is null.</exception>
        public static AsyncEnumerable<KeyValuePair<TKey, TAccumulate>> AggregateBy<TSource, TKey, TAccumulate, TEqualityComparer, TCaptureKey, TCaptureSeed, TCaptureAccumulate>(this AsyncEnumerable<TSource> source,
            TCaptureKey keyCaptureValue, Func<TCaptureKey, TSource, CancelationToken, Promise<TKey>> keySelector,
            TCaptureSeed seedCaptureValue, Func<TCaptureSeed, TKey, CancelationToken, Promise<TAccumulate>> seedSelector,
            TCaptureAccumulate accumulateCaptureValue, Func<TCaptureAccumulate, TAccumulate, TSource, CancelationToken, Promise<TAccumulate>> accumulator,
            TEqualityComparer comparer)
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(seedSelector, nameof(seedSelector), 1);
            ValidateArgument(accumulator, nameof(accumulator), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.AggregateByHelper<TKey, TAccumulate>.AggregateBy(source.GetAsyncEnumerator(),
                DelegateWrapper.Create(keyCaptureValue, keySelector),
                DelegateWrapper.Create(seedCaptureValue, seedSelector),
                DelegateWrapper.Create(accumulateCaptureValue, accumulator),
                comparer);
        }

        /// <summary>
        /// Applies an accumulator function over each group of elements in a configured async-enumerable sequence according to a key selector function, using the default equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence and the result of the aggregation.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TAccumulate">The type of the result of the aggregation.</typeparam>
        /// <typeparam name="TCaptureSeed">The type of the captured value that will be passed to <paramref name="seedSelector"/>.</typeparam>
        /// <param name="configuredSource">The configured source sequence.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="seedCaptureValue">The extra value that will be passed to <paramref name="seedSelector"/>.</param>
        /// <param name="seedSelector">A function to retrieve the initial accumulator value for each group.</param>
        /// <param name="accumulator">An accumulator function to be invoked on each element.</param>
        /// <returns>An async-enumerable sequence containing the aggregates corresponding to each key deriving from <paramref name="configuredSource"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="seedSelector"/> or <paramref name="accumulator"/> is null.</exception>
        public static AsyncEnumerable<KeyValuePair<TKey, TAccumulate>> AggregateBy<TSource, TKey, TAccumulate, TCaptureSeed>(
            this in ConfiguredAsyncEnumerable<TSource> configuredSource,
            Func<TSource, TKey> keySelector,
            TCaptureSeed seedCaptureValue, Func<TCaptureSeed, TKey, TAccumulate> seedSelector,
            Func<TAccumulate, TSource, TAccumulate> accumulator)
            => AggregateBy(configuredSource, keySelector, seedCaptureValue, seedSelector, accumulator, EqualityComparer<TKey>.Default);

        /// <summary>
        /// Applies an accumulator function over each group of elements in a configured async-enumerable sequence according to a key selector function, using the specified equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence and the result of the aggregation.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TAccumulate">The type of the result of the aggregation.</typeparam>
        /// <typeparam name="TEqualityComparer">The type of the equality comparer.</typeparam>
        /// <typeparam name="TCaptureSeed">The type of the captured value that will be passed to <paramref name="seedSelector"/>.</typeparam>
        /// <param name="configuredSource">The configured source sequence.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="seedCaptureValue">The extra value that will be passed to <paramref name="seedSelector"/>.</param>
        /// <param name="seedSelector">A function to retrieve the initial accumulator value for each group.</param>
        /// <param name="accumulator">An accumulator function to be invoked on each element.</param>
        /// <param name="comparer">An equality comparer used to compare keys for equality.</param>
        /// <returns>An async-enumerable sequence containing the aggregates corresponding to each key deriving from <paramref name="configuredSource"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="seedSelector"/> or <paramref name="accumulator"/> or <paramref name="comparer"/> is null.</exception>
        public static AsyncEnumerable<KeyValuePair<TKey, TAccumulate>> AggregateBy<TSource, TKey, TAccumulate, TEqualityComparer, TCaptureSeed>(
            this in ConfiguredAsyncEnumerable<TSource> configuredSource,
            Func<TSource, TKey> keySelector,
            TCaptureSeed seedCaptureValue, Func<TCaptureSeed, TKey, TAccumulate> seedSelector,
            Func<TAccumulate, TSource, TAccumulate> accumulator,
            TEqualityComparer comparer)
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(seedSelector, nameof(seedSelector), 1);
            ValidateArgument(accumulator, nameof(accumulator), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.AggregateByHelper<TKey, TAccumulate>.AggregateBy(configuredSource.GetAsyncEnumerator(),
                DelegateWrapper.Create(keySelector),
                DelegateWrapper.Create(seedCaptureValue, seedSelector),
                DelegateWrapper.Create(accumulator),
                comparer);
        }

        /// <summary>
        /// Applies an accumulator function over each group of elements in a configured async-enumerable sequence according to a key selector function, using the default equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence and the result of the aggregation.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TAccumulate">The type of the result of the aggregation.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TCaptureSeed">The type of the captured value that will be passed to <paramref name="seedSelector"/>.</typeparam>
        /// <param name="configuredSource">The configured source sequence.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="seedCaptureValue">The extra value that will be passed to <paramref name="seedSelector"/>.</param>
        /// <param name="seedSelector">A function to retrieve the initial accumulator value for each group.</param>
        /// <param name="accumulator">An accumulator function to be invoked on each element.</param>
        /// <returns>An async-enumerable sequence containing the aggregates corresponding to each key deriving from <paramref name="configuredSource"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="seedSelector"/> or <paramref name="accumulator"/> is null.</exception>
        public static AsyncEnumerable<KeyValuePair<TKey, TAccumulate>> AggregateBy<TSource, TKey, TAccumulate, TCaptureKey, TCaptureSeed>(
            this in ConfiguredAsyncEnumerable<TSource> configuredSource,
            TCaptureKey keyCaptureValue, Func<TCaptureKey, TSource, TKey> keySelector,
            TCaptureSeed seedCaptureValue, Func<TCaptureSeed, TKey, TAccumulate> seedSelector,
            Func<TAccumulate, TSource, TAccumulate> accumulator)
            => AggregateBy(configuredSource, keyCaptureValue, keySelector, seedCaptureValue, seedSelector, accumulator, EqualityComparer<TKey>.Default);

        /// <summary>
        /// Applies an accumulator function over each group of elements in a configured async-enumerable sequence according to a key selector function, using the specified equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence and the result of the aggregation.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TAccumulate">The type of the result of the aggregation.</typeparam>
        /// <typeparam name="TEqualityComparer">The type of the equality comparer.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TCaptureSeed">The type of the captured value that will be passed to <paramref name="seedSelector"/>.</typeparam>
        /// <param name="configuredSource">The configured source sequence.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="seedCaptureValue">The extra value that will be passed to <paramref name="seedSelector"/>.</param>
        /// <param name="seedSelector">A function to retrieve the initial accumulator value for each group.</param>
        /// <param name="accumulator">An accumulator function to be invoked on each element.</param>
        /// <param name="comparer">An equality comparer used to compare keys for equality.</param>
        /// <returns>An async-enumerable sequence containing the aggregates corresponding to each key deriving from <paramref name="configuredSource"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="seedSelector"/> or <paramref name="accumulator"/> or <paramref name="comparer"/> is null.</exception>
        public static AsyncEnumerable<KeyValuePair<TKey, TAccumulate>> AggregateBy<TSource, TKey, TAccumulate, TEqualityComparer, TCaptureKey, TCaptureSeed>(
            this in ConfiguredAsyncEnumerable<TSource> configuredSource,
            TCaptureKey keyCaptureValue, Func<TCaptureKey, TSource, TKey> keySelector,
            TCaptureSeed seedCaptureValue, Func<TCaptureSeed, TKey, TAccumulate> seedSelector,
            Func<TAccumulate, TSource, TAccumulate> accumulator,
            TEqualityComparer comparer)
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(seedSelector, nameof(seedSelector), 1);
            ValidateArgument(accumulator, nameof(accumulator), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.AggregateByHelper<TKey, TAccumulate>.AggregateBy(configuredSource.GetAsyncEnumerator(),
                DelegateWrapper.Create(keyCaptureValue, keySelector),
                DelegateWrapper.Create(seedCaptureValue, seedSelector),
                DelegateWrapper.Create(accumulator),
                comparer);
        }

        /// <summary>
        /// Applies an accumulator function over each group of elements in a configured async-enumerable sequence according to a key selector function, using the default equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence and the result of the aggregation.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TAccumulate">The type of the result of the aggregation.</typeparam>
        /// <typeparam name="TCaptureSeed">The type of the captured value that will be passed to <paramref name="seedSelector"/>.</typeparam>
        /// <typeparam name="TCaptureAccumulate">The type of the captured value that will be passed to <paramref name="accumulator"/>.</typeparam>
        /// <param name="configuredSource">The configured source sequence.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="seedCaptureValue">The extra value that will be passed to <paramref name="seedSelector"/>.</param>
        /// <param name="seedSelector">A function to retrieve the initial accumulator value for each group.</param>
        /// <param name="accumulateCaptureValue">The extra value that will be passed to <paramref name="accumulator"/>.</param>
        /// <param name="accumulator">An accumulator function to be invoked on each element.</param>
        /// <returns>An async-enumerable sequence containing the aggregates corresponding to each key deriving from <paramref name="configuredSource"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="seedSelector"/> or <paramref name="accumulator"/> is null.</exception>
        public static AsyncEnumerable<KeyValuePair<TKey, TAccumulate>> AggregateBy<TSource, TKey, TAccumulate, TCaptureSeed, TCaptureAccumulate>(
            this in ConfiguredAsyncEnumerable<TSource> configuredSource,
            Func<TSource, TKey> keySelector,
            TCaptureSeed seedCaptureValue, Func<TCaptureSeed, TKey, TAccumulate> seedSelector,
            TCaptureAccumulate accumulateCaptureValue, Func<TCaptureAccumulate, TAccumulate, TSource, TAccumulate> accumulator)
            => AggregateBy(configuredSource, keySelector, seedCaptureValue, seedSelector, accumulateCaptureValue, accumulator, EqualityComparer<TKey>.Default);

        /// <summary>
        /// Applies an accumulator function over each group of elements in a configured async-enumerable sequence according to a key selector function, using the specified equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence and the result of the aggregation.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TAccumulate">The type of the result of the aggregation.</typeparam>
        /// <typeparam name="TEqualityComparer">The type of the equality comparer.</typeparam>
        /// <typeparam name="TCaptureSeed">The type of the captured value that will be passed to <paramref name="seedSelector"/>.</typeparam>
        /// <typeparam name="TCaptureAccumulate">The type of the captured value that will be passed to <paramref name="accumulator"/>.</typeparam>
        /// <param name="configuredSource">The configured source sequence.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="seedCaptureValue">The extra value that will be passed to <paramref name="seedSelector"/>.</param>
        /// <param name="seedSelector">A function to retrieve the initial accumulator value for each group.</param>
        /// <param name="accumulateCaptureValue">The extra value that will be passed to <paramref name="accumulator"/>.</param>
        /// <param name="accumulator">An accumulator function to be invoked on each element.</param>
        /// <param name="comparer">An equality comparer used to compare keys for equality.</param>
        /// <returns>An async-enumerable sequence containing the aggregates corresponding to each key deriving from <paramref name="configuredSource"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="seedSelector"/> or <paramref name="accumulator"/> or <paramref name="comparer"/> is null.</exception>
        public static AsyncEnumerable<KeyValuePair<TKey, TAccumulate>> AggregateBy<TSource, TKey, TAccumulate, TEqualityComparer, TCaptureSeed, TCaptureAccumulate>(
            this in ConfiguredAsyncEnumerable<TSource> configuredSource,
            Func<TSource, TKey> keySelector,
            TCaptureSeed seedCaptureValue, Func<TCaptureSeed, TKey, TAccumulate> seedSelector,
            TCaptureAccumulate accumulateCaptureValue, Func<TCaptureAccumulate, TAccumulate, TSource, TAccumulate> accumulator,
            TEqualityComparer comparer)
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(seedSelector, nameof(seedSelector), 1);
            ValidateArgument(accumulator, nameof(accumulator), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.AggregateByHelper<TKey, TAccumulate>.AggregateBy(configuredSource.GetAsyncEnumerator(),
                DelegateWrapper.Create(keySelector),
                DelegateWrapper.Create(seedCaptureValue, seedSelector),
                DelegateWrapper.Create(accumulateCaptureValue, accumulator),
                comparer);
        }

        /// <summary>
        /// Applies an accumulator function over each group of elements in a configured async-enumerable sequence according to a key selector function, using the default equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence and the result of the aggregation.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TAccumulate">The type of the result of the aggregation.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TCaptureSeed">The type of the captured value that will be passed to <paramref name="seedSelector"/>.</typeparam>
        /// <typeparam name="TCaptureAccumulate">The type of the captured value that will be passed to <paramref name="accumulator"/>.</typeparam>
        /// <param name="configuredSource">The configured source sequence.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="seedCaptureValue">The extra value that will be passed to <paramref name="seedSelector"/>.</param>
        /// <param name="seedSelector">A function to retrieve the initial accumulator value for each group.</param>
        /// <param name="accumulateCaptureValue">The extra value that will be passed to <paramref name="accumulator"/>.</param>
        /// <param name="accumulator">An accumulator function to be invoked on each element.</param>
        /// <returns>An async-enumerable sequence containing the aggregates corresponding to each key deriving from <paramref name="configuredSource"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="seedSelector"/> or <paramref name="accumulator"/> is null.</exception>
        public static AsyncEnumerable<KeyValuePair<TKey, TAccumulate>> AggregateBy<TSource, TKey, TAccumulate, TCaptureKey, TCaptureSeed, TCaptureAccumulate>(
            this in ConfiguredAsyncEnumerable<TSource> configuredSource,
            TCaptureKey keyCaptureValue, Func<TCaptureKey, TSource, TKey> keySelector,
            TCaptureSeed seedCaptureValue, Func<TCaptureSeed, TKey, TAccumulate> seedSelector,
            TCaptureAccumulate accumulateCaptureValue, Func<TCaptureAccumulate, TAccumulate, TSource, TAccumulate> accumulator)
            => AggregateBy(configuredSource, keyCaptureValue, keySelector, seedCaptureValue, seedSelector, accumulateCaptureValue, accumulator, EqualityComparer<TKey>.Default);

        /// <summary>
        /// Applies an accumulator function over each group of elements in a configured async-enumerable sequence according to a key selector function, using the specified equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence and the result of the aggregation.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TAccumulate">The type of the result of the aggregation.</typeparam>
        /// <typeparam name="TEqualityComparer">The type of the equality comparer.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TCaptureSeed">The type of the captured value that will be passed to <paramref name="seedSelector"/>.</typeparam>
        /// <typeparam name="TCaptureAccumulate">The type of the captured value that will be passed to <paramref name="accumulator"/>.</typeparam>
        /// <param name="configuredSource">The configured source sequence.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="seedCaptureValue">The extra value that will be passed to <paramref name="seedSelector"/>.</param>
        /// <param name="seedSelector">A function to retrieve the initial accumulator value for each group.</param>
        /// <param name="accumulateCaptureValue">The extra value that will be passed to <paramref name="accumulator"/>.</param>
        /// <param name="accumulator">An accumulator function to be invoked on each element.</param>
        /// <param name="comparer">An equality comparer used to compare keys for equality.</param>
        /// <returns>An async-enumerable sequence containing the aggregates corresponding to each key deriving from <paramref name="configuredSource"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="seedSelector"/> or <paramref name="accumulator"/> or <paramref name="comparer"/> is null.</exception>
        public static AsyncEnumerable<KeyValuePair<TKey, TAccumulate>> AggregateBy<TSource, TKey, TAccumulate, TEqualityComparer, TCaptureKey, TCaptureSeed, TCaptureAccumulate>(
            this in ConfiguredAsyncEnumerable<TSource> configuredSource,
            TCaptureKey keyCaptureValue, Func<TCaptureKey, TSource, TKey> keySelector,
            TCaptureSeed seedCaptureValue, Func<TCaptureSeed, TKey, TAccumulate> seedSelector,
            TCaptureAccumulate accumulateCaptureValue, Func<TCaptureAccumulate, TAccumulate, TSource, TAccumulate> accumulator,
            TEqualityComparer comparer)
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(seedSelector, nameof(seedSelector), 1);
            ValidateArgument(accumulator, nameof(accumulator), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.AggregateByHelper<TKey, TAccumulate>.AggregateBy(configuredSource.GetAsyncEnumerator(),
                DelegateWrapper.Create(keyCaptureValue, keySelector),
                DelegateWrapper.Create(seedCaptureValue, seedSelector),
                DelegateWrapper.Create(accumulateCaptureValue, accumulator),
                comparer);
        }

        /// <summary>
        /// Applies an accumulator function over each group of elements in a configured async-enumerable sequence according to a key selector function, using the default equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence and the result of the aggregation.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TAccumulate">The type of the result of the aggregation.</typeparam>
        /// <typeparam name="TCaptureSeed">The type of the captured value that will be passed to <paramref name="seedSelector"/>.</typeparam>
        /// <param name="configuredSource">The configured source sequence.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from each element.</param>
        /// <param name="seedCaptureValue">The extra value that will be passed to <paramref name="seedSelector"/>.</param>
        /// <param name="seedSelector">An asynchronous function to retrieve the initial accumulator value for each group.</param>
        /// <param name="accumulator">An asynchronous accumulator function to be invoked on each element.</param>
        /// <returns>An async-enumerable sequence containing the aggregates corresponding to each key deriving from <paramref name="configuredSource"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="seedSelector"/> or <paramref name="accumulator"/> is null.</exception>
        public static AsyncEnumerable<KeyValuePair<TKey, TAccumulate>> AggregateBy<TSource, TKey, TAccumulate, TCaptureSeed>(
            this in ConfiguredAsyncEnumerable<TSource> configuredSource,
            Func<TSource, CancelationToken, Promise<TKey>> keySelector,
            TCaptureSeed seedCaptureValue, Func<TCaptureSeed, TKey, CancelationToken, Promise<TAccumulate>> seedSelector,
            Func<TAccumulate, TSource, CancelationToken, Promise<TAccumulate>> accumulator)
            => AggregateBy(configuredSource, keySelector, seedCaptureValue, seedSelector, accumulator, EqualityComparer<TKey>.Default);

        /// <summary>
        /// Applies an accumulator function over each group of elements in a configured async-enumerable sequence according to a key selector function, using the specified equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence and the result of the aggregation.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TAccumulate">The type of the result of the aggregation.</typeparam>
        /// <typeparam name="TEqualityComparer">The type of the equality comparer.</typeparam>
        /// <typeparam name="TCaptureSeed">The type of the captured value that will be passed to <paramref name="seedSelector"/>.</typeparam>
        /// <param name="configuredSource">The configured source sequence.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from each element.</param>
        /// <param name="seedCaptureValue">The extra value that will be passed to <paramref name="seedSelector"/>.</param>
        /// <param name="seedSelector">An asynchronous function to retrieve the initial accumulator value for each group.</param>
        /// <param name="accumulator">An asynchronous accumulator function to be invoked on each element.</param>
        /// <param name="comparer">An equality comparer used to compare keys for equality.</param>
        /// <returns>An async-enumerable sequence containing the aggregates corresponding to each key deriving from <paramref name="configuredSource"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="seedSelector"/> or <paramref name="accumulator"/> or <paramref name="comparer"/> is null.</exception>
        public static AsyncEnumerable<KeyValuePair<TKey, TAccumulate>> AggregateBy<TSource, TKey, TAccumulate, TEqualityComparer, TCaptureSeed>(
            this in ConfiguredAsyncEnumerable<TSource> configuredSource,
            Func<TSource, CancelationToken, Promise<TKey>> keySelector,
            TCaptureSeed seedCaptureValue, Func<TCaptureSeed, TKey, CancelationToken, Promise<TAccumulate>> seedSelector,
            Func<TAccumulate, TSource, CancelationToken, Promise<TAccumulate>> accumulator,
            TEqualityComparer comparer)
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(seedSelector, nameof(seedSelector), 1);
            ValidateArgument(accumulator, nameof(accumulator), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.AggregateByHelper<TKey, TAccumulate>.AggregateBy(configuredSource.GetAsyncEnumerator(),
                DelegateWrapper.Create(keySelector),
                DelegateWrapper.Create(seedCaptureValue, seedSelector),
                DelegateWrapper.Create(accumulator),
                comparer);
        }

        /// <summary>
        /// Applies an accumulator function over each group of elements in a configured async-enumerable sequence according to a key selector function, using the default equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence and the result of the aggregation.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TAccumulate">The type of the result of the aggregation.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TCaptureSeed">The type of the captured value that will be passed to <paramref name="seedSelector"/>.</typeparam>
        /// <param name="configuredSource">The configured source sequence.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from each element.</param>
        /// <param name="seedCaptureValue">The extra value that will be passed to <paramref name="seedSelector"/>.</param>
        /// <param name="seedSelector">An asynchronous function to retrieve the initial accumulator value for each group.</param>
        /// <param name="accumulator">An asynchronous accumulator function to be invoked on each element.</param>
        /// <returns>An async-enumerable sequence containing the aggregates corresponding to each key deriving from <paramref name="configuredSource"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="seedSelector"/> or <paramref name="accumulator"/> is null.</exception>
        public static AsyncEnumerable<KeyValuePair<TKey, TAccumulate>> AggregateBy<TSource, TKey, TAccumulate, TCaptureKey, TCaptureSeed>(
            this in ConfiguredAsyncEnumerable<TSource> configuredSource,
            TCaptureKey keyCaptureValue, Func<TCaptureKey, TSource, CancelationToken, Promise<TKey>> keySelector,
            TCaptureSeed seedCaptureValue, Func<TCaptureSeed, TKey, CancelationToken, Promise<TAccumulate>> seedSelector,
            Func<TAccumulate, TSource, CancelationToken, Promise<TAccumulate>> accumulator)
            => AggregateBy(configuredSource, keyCaptureValue, keySelector, seedCaptureValue, seedSelector, accumulator, EqualityComparer<TKey>.Default);

        /// <summary>
        /// Applies an accumulator function over each group of elements in a configured async-enumerable sequence according to a key selector function, using the specified equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence and the result of the aggregation.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TAccumulate">The type of the result of the aggregation.</typeparam>
        /// <typeparam name="TEqualityComparer">The type of the equality comparer.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TCaptureSeed">The type of the captured value that will be passed to <paramref name="seedSelector"/>.</typeparam>
        /// <param name="configuredSource">The configured source sequence.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from each element.</param>
        /// <param name="seedCaptureValue">The extra value that will be passed to <paramref name="seedSelector"/>.</param>
        /// <param name="seedSelector">An asynchronous function to retrieve the initial accumulator value for each group.</param>
        /// <param name="accumulator">An asynchronous accumulator function to be invoked on each element.</param>
        /// <param name="comparer">An equality comparer used to compare keys for equality.</param>
        /// <returns>An async-enumerable sequence containing the aggregates corresponding to each key deriving from <paramref name="configuredSource"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="seedSelector"/> or <paramref name="accumulator"/> or <paramref name="comparer"/> is null.</exception>
        public static AsyncEnumerable<KeyValuePair<TKey, TAccumulate>> AggregateBy<TSource, TKey, TAccumulate, TEqualityComparer, TCaptureKey, TCaptureSeed>(
            this in ConfiguredAsyncEnumerable<TSource> configuredSource,
            TCaptureKey keyCaptureValue, Func<TCaptureKey, TSource, CancelationToken, Promise<TKey>> keySelector,
            TCaptureSeed seedCaptureValue, Func<TCaptureSeed, TKey, CancelationToken, Promise<TAccumulate>> seedSelector,
            Func<TAccumulate, TSource, CancelationToken, Promise<TAccumulate>> accumulator,
            TEqualityComparer comparer)
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(seedSelector, nameof(seedSelector), 1);
            ValidateArgument(accumulator, nameof(accumulator), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.AggregateByHelper<TKey, TAccumulate>.AggregateBy(configuredSource.GetAsyncEnumerator(),
                DelegateWrapper.Create(keyCaptureValue, keySelector),
                DelegateWrapper.Create(seedCaptureValue, seedSelector),
                DelegateWrapper.Create(accumulator),
                comparer);
        }

        /// <summary>
        /// Applies an accumulator function over each group of elements in a configured async-enumerable sequence according to a key selector function, using the default equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence and the result of the aggregation.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TAccumulate">The type of the result of the aggregation.</typeparam>
        /// <typeparam name="TCaptureSeed">The type of the captured value that will be passed to <paramref name="seedSelector"/>.</typeparam>
        /// <typeparam name="TCaptureAccumulate">The type of the captured value that will be passed to <paramref name="accumulator"/>.</typeparam>
        /// <param name="configuredSource">The configured source sequence.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from each element.</param>
        /// <param name="seedCaptureValue">The extra value that will be passed to <paramref name="seedSelector"/>.</param>
        /// <param name="seedSelector">An asynchronous function to retrieve the initial accumulator value for each group.</param>
        /// <param name="accumulateCaptureValue">The extra value that will be passed to <paramref name="accumulator"/>.</param>
        /// <param name="accumulator">An asynchronous accumulator function to be invoked on each element.</param>
        /// <returns>An async-enumerable sequence containing the aggregates corresponding to each key deriving from <paramref name="configuredSource"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="seedSelector"/> or <paramref name="accumulator"/> is null.</exception>
        public static AsyncEnumerable<KeyValuePair<TKey, TAccumulate>> AggregateBy<TSource, TKey, TAccumulate, TCaptureSeed, TCaptureAccumulate>(
            this in ConfiguredAsyncEnumerable<TSource> configuredSource,
            Func<TSource, CancelationToken, Promise<TKey>> keySelector,
            TCaptureSeed seedCaptureValue, Func<TCaptureSeed, TKey, CancelationToken, Promise<TAccumulate>> seedSelector,
            TCaptureAccumulate accumulateCaptureValue, Func<TCaptureAccumulate, TAccumulate, TSource, CancelationToken, Promise<TAccumulate>> accumulator)
            => AggregateBy(configuredSource, keySelector, seedCaptureValue, seedSelector, accumulateCaptureValue, accumulator, EqualityComparer<TKey>.Default);

        /// <summary>
        /// Applies an accumulator function over each group of elements in a configured async-enumerable sequence according to a key selector function, using the specified equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence and the result of the aggregation.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TAccumulate">The type of the result of the aggregation.</typeparam>
        /// <typeparam name="TEqualityComparer">The type of the equality comparer.</typeparam>
        /// <typeparam name="TCaptureSeed">The type of the captured value that will be passed to <paramref name="seedSelector"/>.</typeparam>
        /// <typeparam name="TCaptureAccumulate">The type of the captured value that will be passed to <paramref name="accumulator"/>.</typeparam>
        /// <param name="configuredSource">The configured source sequence.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from each element.</param>
        /// <param name="seedCaptureValue">The extra value that will be passed to <paramref name="seedSelector"/>.</param>
        /// <param name="seedSelector">An asynchronous function to retrieve the initial accumulator value for each group.</param>
        /// <param name="accumulateCaptureValue">The extra value that will be passed to <paramref name="accumulator"/>.</param>
        /// <param name="accumulator">An asynchronous accumulator function to be invoked on each element.</param>
        /// <param name="comparer">An equality comparer used to compare keys for equality.</param>
        /// <returns>An async-enumerable sequence containing the aggregates corresponding to each key deriving from <paramref name="configuredSource"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="seedSelector"/> or <paramref name="accumulator"/> or <paramref name="comparer"/> is null.</exception>
        public static AsyncEnumerable<KeyValuePair<TKey, TAccumulate>> AggregateBy<TSource, TKey, TAccumulate, TEqualityComparer, TCaptureSeed, TCaptureAccumulate>(
            this in ConfiguredAsyncEnumerable<TSource> configuredSource,
            Func<TSource, CancelationToken, Promise<TKey>> keySelector,
            TCaptureSeed seedCaptureValue, Func<TCaptureSeed, TKey, CancelationToken, Promise<TAccumulate>> seedSelector,
            TCaptureAccumulate accumulateCaptureValue, Func<TCaptureAccumulate, TAccumulate, TSource, CancelationToken, Promise<TAccumulate>> accumulator,
            TEqualityComparer comparer)
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(seedSelector, nameof(seedSelector), 1);
            ValidateArgument(accumulator, nameof(accumulator), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.AggregateByHelper<TKey, TAccumulate>.AggregateBy(configuredSource.GetAsyncEnumerator(),
                DelegateWrapper.Create(keySelector),
                DelegateWrapper.Create(seedCaptureValue, seedSelector),
                DelegateWrapper.Create(accumulateCaptureValue, accumulator),
                comparer);
        }

        /// <summary>
        /// Applies an accumulator function over each group of elements in a configured async-enumerable sequence according to a key selector function, using the default equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence and the result of the aggregation.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TAccumulate">The type of the result of the aggregation.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TCaptureSeed">The type of the captured value that will be passed to <paramref name="seedSelector"/>.</typeparam>
        /// <typeparam name="TCaptureAccumulate">The type of the captured value that will be passed to <paramref name="accumulator"/>.</typeparam>
        /// <param name="configuredSource">The configured source sequence.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from each element.</param>
        /// <param name="seedCaptureValue">The extra value that will be passed to <paramref name="seedSelector"/>.</param>
        /// <param name="seedSelector">An asynchronous function to retrieve the initial accumulator value for each group.</param>
        /// <param name="accumulateCaptureValue">The extra value that will be passed to <paramref name="accumulator"/>.</param>
        /// <param name="accumulator">An asynchronous accumulator function to be invoked on each element.</param>
        /// <returns>An async-enumerable sequence containing the aggregates corresponding to each key deriving from <paramref name="configuredSource"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="seedSelector"/> or <paramref name="accumulator"/> is null.</exception>
        public static AsyncEnumerable<KeyValuePair<TKey, TAccumulate>> AggregateBy<TSource, TKey, TAccumulate, TCaptureKey, TCaptureSeed, TCaptureAccumulate>(
            this in ConfiguredAsyncEnumerable<TSource> configuredSource,
            TCaptureKey keyCaptureValue, Func<TCaptureKey, TSource, CancelationToken, Promise<TKey>> keySelector,
            TCaptureSeed seedCaptureValue, Func<TCaptureSeed, TKey, CancelationToken, Promise<TAccumulate>> seedSelector,
            TCaptureAccumulate accumulateCaptureValue, Func<TCaptureAccumulate, TAccumulate, TSource, CancelationToken, Promise<TAccumulate>> accumulator)
            => AggregateBy(configuredSource, keyCaptureValue, keySelector, seedCaptureValue, seedSelector, accumulateCaptureValue, accumulator, EqualityComparer<TKey>.Default);

        /// <summary>
        /// Applies an accumulator function over each group of elements in a configured async-enumerable sequence according to a key selector function, using the specified equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence and the result of the aggregation.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TAccumulate">The type of the result of the aggregation.</typeparam>
        /// <typeparam name="TEqualityComparer">The type of the equality comparer.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TCaptureSeed">The type of the captured value that will be passed to <paramref name="seedSelector"/>.</typeparam>
        /// <typeparam name="TCaptureAccumulate">The type of the captured value that will be passed to <paramref name="accumulator"/>.</typeparam>
        /// <param name="configuredSource">The configured source sequence.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from each element.</param>
        /// <param name="seedCaptureValue">The extra value that will be passed to <paramref name="seedSelector"/>.</param>
        /// <param name="seedSelector">An asynchronous function to retrieve the initial accumulator value for each group.</param>
        /// <param name="accumulateCaptureValue">The extra value that will be passed to <paramref name="accumulator"/>.</param>
        /// <param name="accumulator">An asynchronous accumulator function to be invoked on each element.</param>
        /// <param name="comparer">An equality comparer used to compare keys for equality.</param>
        /// <returns>An async-enumerable sequence containing the aggregates corresponding to each key deriving from <paramref name="configuredSource"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="seedSelector"/> or <paramref name="accumulator"/> or <paramref name="comparer"/> is null.</exception>
        public static AsyncEnumerable<KeyValuePair<TKey, TAccumulate>> AggregateBy<TSource, TKey, TAccumulate, TEqualityComparer, TCaptureKey, TCaptureSeed, TCaptureAccumulate>(
            this in ConfiguredAsyncEnumerable<TSource> configuredSource,
            TCaptureKey keyCaptureValue, Func<TCaptureKey, TSource, CancelationToken, Promise<TKey>> keySelector,
            TCaptureSeed seedCaptureValue, Func<TCaptureSeed, TKey, CancelationToken, Promise<TAccumulate>> seedSelector,
            TCaptureAccumulate accumulateCaptureValue, Func<TCaptureAccumulate, TAccumulate, TSource, CancelationToken, Promise<TAccumulate>> accumulator,
            TEqualityComparer comparer)
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(seedSelector, nameof(seedSelector), 1);
            ValidateArgument(accumulator, nameof(accumulator), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.AggregateByHelper<TKey, TAccumulate>.AggregateBy(configuredSource.GetAsyncEnumerator(),
                DelegateWrapper.Create(keyCaptureValue, keySelector),
                DelegateWrapper.Create(seedCaptureValue, seedSelector),
                DelegateWrapper.Create(accumulateCaptureValue, accumulator),
                comparer);
        }
        #endregion SeedSelector
    }
}