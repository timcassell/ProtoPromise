#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using Proto.Promises.CompilerServices;
using Proto.Promises.Collections;
using System;
using System.Collections.Generic;

namespace Proto.Promises.Linq
{
#if CSHARP_7_3_OR_NEWER
    partial class AsyncEnumerable
    {
        #region AsyncEnumerable
        /// <summary>
        /// Correlates the elements of two async-enumerable sequences based on equality of keys and groups the results. The default equality comparer is used to compare keys.
        /// </summary>
        /// <typeparam name="TOuter">The type of the elements of the first async-enumerable sequence.</typeparam>
        /// <typeparam name="TInner">The type of the elements of the second async-enumerable sequence.</typeparam>
        /// <typeparam name="TKey">The type of the keys returned by the key selector functions.</typeparam>
        /// <param name="outer">The first async-enumerable sequence to join.</param>
        /// <param name="inner">The async-enumerable sequence to join to the first sequence.</param>
        /// <param name="outerKeySelector">A function to extract the join key from each element of the first sequence.</param>
        /// <param name="innerKeySelector">A function to extract the join key from each element of the second sequence.</param>
        /// <returns>An async-enumerable sequence that has elements of <see cref="ValueTuple{T1, T2}"/> of each outer element and the inner elements of the same key, that are obtained by performing a grouped join on two sequences.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="outerKeySelector"/> or <paramref name="innerKeySelector"/> is null.</exception>
        public static AsyncEnumerable<(TOuter Outer, TempCollection<TInner> InnerElements)> GroupJoin<TOuter, TInner, TKey>(this AsyncEnumerable<TOuter> outer,
            AsyncEnumerable<TInner> inner,
            Func<TOuter, TKey> outerKeySelector,
            Func<TInner, TKey> innerKeySelector)
            => GroupJoin(outer, inner, outerKeySelector, innerKeySelector, EqualityComparer<TKey>.Default);

        /// <summary>
        /// Correlates the elements of two async-enumerable sequences based on equality of keys and groups the results. The default equality comparer is used to compare keys.
        /// </summary>
        /// <typeparam name="TOuter">The type of the elements of the first async-enumerable sequence.</typeparam>
        /// <typeparam name="TInner">The type of the elements of the second async-enumerable sequence.</typeparam>
        /// <typeparam name="TKey">The type of the keys returned by the key selector functions.</typeparam>
        /// <typeparam name="TOuterCapture">The type of the captured value that will be passed to <paramref name="outerKeySelector"/>.</typeparam>
        /// <param name="outer">The first async-enumerable sequence to join.</param>
        /// <param name="inner">The async-enumerable sequence to join to the first sequence.</param>
        /// <param name="outerCaptureValue">The extra value that will be passed to <paramref name="outerKeySelector"/>.</param>
        /// <param name="outerKeySelector">A function to extract the join key from each element of the first sequence.</param>
        /// <param name="innerKeySelector">A function to extract the join key from each element of the second sequence.</param>
        /// <returns>An async-enumerable sequence that has elements of <see cref="ValueTuple{T1, T2}"/> of each outer element and the inner elements of the same key, that are obtained by performing a grouped join on two sequences.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="outerKeySelector"/> or <paramref name="innerKeySelector"/> is null.</exception>
        public static AsyncEnumerable<(TOuter Outer, TempCollection<TInner> InnerElements)> GroupJoin<TOuter, TInner, TKey, TOuterCapture>(this AsyncEnumerable<TOuter> outer,
            AsyncEnumerable<TInner> inner,
            TOuterCapture outerCaptureValue,
            Func<TOuterCapture, TOuter, TKey> outerKeySelector,
            Func<TInner, TKey> innerKeySelector)
            => GroupJoin(outer, inner, outerCaptureValue, outerKeySelector, innerKeySelector, EqualityComparer<TKey>.Default);

        /// <summary>
        /// Correlates the elements of two async-enumerable sequences based on equality of keys and groups the results. The default equality comparer is used to compare keys.
        /// </summary>
        /// <typeparam name="TOuter">The type of the elements of the first async-enumerable sequence.</typeparam>
        /// <typeparam name="TInner">The type of the elements of the second async-enumerable sequence.</typeparam>
        /// <typeparam name="TKey">The type of the keys returned by the key selector functions.</typeparam>
        /// <typeparam name="TInnerCapture">The type of the captured value that will be passed to <paramref name="innerKeySelector"/>.</typeparam>
        /// <param name="outer">The first async-enumerable sequence to join.</param>
        /// <param name="inner">The async-enumerable sequence to join to the first sequence.</param>
        /// <param name="outerKeySelector">A function to extract the join key from each element of the first sequence.</param>
        /// <param name="innerCaptureValue">The extra value that will be passed to <paramref name="innerKeySelector"/>.</param>
        /// <param name="innerKeySelector">A function to extract the join key from each element of the second sequence.</param>
        /// <returns>An async-enumerable sequence that has elements of <see cref="ValueTuple{T1, T2}"/> of each outer element and the inner elements of the same key, that are obtained by performing a grouped join on two sequences.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="outerKeySelector"/> or <paramref name="innerKeySelector"/> is null.</exception>
        public static AsyncEnumerable<(TOuter Outer, TempCollection<TInner> InnerElements)> GroupJoin<TOuter, TInner, TKey, TInnerCapture>(this AsyncEnumerable<TOuter> outer,
            AsyncEnumerable<TInner> inner,
            Func<TOuter, TKey> outerKeySelector,
            TInnerCapture innerCaptureValue,
            Func<TInnerCapture, TInner, TKey> innerKeySelector)
            => GroupJoin(outer, inner, outerKeySelector, innerCaptureValue, innerKeySelector, EqualityComparer<TKey>.Default);

        /// <summary>
        /// Correlates the elements of two async-enumerable sequences based on equality of keys and groups the results. The default equality comparer is used to compare keys.
        /// </summary>
        /// <typeparam name="TOuter">The type of the elements of the first async-enumerable sequence.</typeparam>
        /// <typeparam name="TInner">The type of the elements of the second async-enumerable sequence.</typeparam>
        /// <typeparam name="TKey">The type of the keys returned by the key selector functions.</typeparam>
        /// <typeparam name="TOuterCapture">The type of the captured value that will be passed to <paramref name="outerKeySelector"/>.</typeparam>
        /// <typeparam name="TInnerCapture">The type of the captured value that will be passed to <paramref name="innerKeySelector"/>.</typeparam>
        /// <param name="outer">The first async-enumerable sequence to join.</param>
        /// <param name="inner">The async-enumerable sequence to join to the first sequence.</param>
        /// <param name="outerCaptureValue">The extra value that will be passed to <paramref name="outerKeySelector"/>.</param>
        /// <param name="outerKeySelector">A function to extract the join key from each element of the first sequence.</param>
        /// <param name="innerCaptureValue">The extra value that will be passed to <paramref name="innerKeySelector"/>.</param>
        /// <param name="innerKeySelector">A function to extract the join key from each element of the second sequence.</param>
        /// <returns>An async-enumerable sequence that has elements of <see cref="ValueTuple{T1, T2}"/> of each outer element and the inner elements of the same key, that are obtained by performing a grouped join on two sequences.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="outerKeySelector"/> or <paramref name="innerKeySelector"/> is null.</exception>
        public static AsyncEnumerable<(TOuter Outer, TempCollection<TInner> InnerElements)> GroupJoin<TOuter, TInner, TKey, TOuterCapture, TInnerCapture>(this AsyncEnumerable<TOuter> outer,
            AsyncEnumerable<TInner> inner,
            TOuterCapture outerCaptureValue,
            Func<TOuterCapture, TOuter, TKey> outerKeySelector,
            TInnerCapture innerCaptureValue,
            Func<TInnerCapture, TInner, TKey> innerKeySelector)
            => GroupJoin(outer, inner, outerCaptureValue, outerKeySelector, innerCaptureValue, innerKeySelector, EqualityComparer<TKey>.Default);

        /// <summary>
        /// Correlates the elements of two async-enumerable sequences based on equality of keys and groups the results.
        /// </summary>
        /// <typeparam name="TOuter">The type of the elements of the first async-enumerable sequence.</typeparam>
        /// <typeparam name="TInner">The type of the elements of the second async-enumerable sequence.</typeparam>
        /// <typeparam name="TKey">The type of the keys returned by the key selector functions.</typeparam>
        /// <typeparam name="TEqualityComparer">The type of the comparer.</typeparam>
        /// <param name="outer">The first async-enumerable sequence to join.</param>
        /// <param name="inner">The async-enumerable sequence to join to the first sequence.</param>
        /// <param name="outerKeySelector">A function to extract the join key from each element of the first sequence.</param>
        /// <param name="innerKeySelector">A function to extract the join key from each element of the second sequence.</param>
        /// <param name="comparer">A comparer to compare keys.</param>
        /// <returns>An async-enumerable sequence that has elements of <see cref="ValueTuple{T1, T2}"/> of each outer element and the inner elements of the same key, that are obtained by performing a grouped join on two sequences.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="outerKeySelector"/> or <paramref name="innerKeySelector"/> or <paramref name="comparer"/> is null.</exception>
        public static AsyncEnumerable<(TOuter Outer, TempCollection<TInner> InnerElements)> GroupJoin<TOuter, TInner, TKey, TEqualityComparer>(this AsyncEnumerable<TOuter> outer,
            AsyncEnumerable<TInner> inner,
            Func<TOuter, TKey> outerKeySelector,
            Func<TInner, TKey> innerKeySelector,
            TEqualityComparer comparer)
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            ValidateArgument(outerKeySelector, nameof(outerKeySelector), 1);
            ValidateArgument(innerKeySelector, nameof(innerKeySelector), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.GroupJoinHelper<TKey>.GroupJoin(outer.GetAsyncEnumerator(),
                inner.GetAsyncEnumerator(),
                Internal.PromiseRefBase.DelegateWrapper.Create(outerKeySelector),
                Internal.PromiseRefBase.DelegateWrapper.Create(innerKeySelector),
                comparer);
        }

        /// <summary>
        /// Correlates the elements of two async-enumerable sequences based on equality of keys and groups the results.
        /// </summary>
        /// <typeparam name="TOuter">The type of the elements of the first async-enumerable sequence.</typeparam>
        /// <typeparam name="TInner">The type of the elements of the second async-enumerable sequence.</typeparam>
        /// <typeparam name="TKey">The type of the keys returned by the key selector functions.</typeparam>
        /// <typeparam name="TOuterCapture">The type of the captured value that will be passed to <paramref name="outerKeySelector"/>.</typeparam>
        /// <typeparam name="TEqualityComparer">The type of the comparer.</typeparam>
        /// <param name="outer">The first async-enumerable sequence to join.</param>
        /// <param name="inner">The async-enumerable sequence to join to the first sequence.</param>
        /// <param name="outerCaptureValue">The extra value that will be passed to <paramref name="outerKeySelector"/>.</param>
        /// <param name="outerKeySelector">A function to extract the join key from each element of the first sequence.</param>
        /// <param name="innerKeySelector">A function to extract the join key from each element of the second sequence.</param>
        /// <param name="comparer">A comparer to compare keys.</param>
        /// <returns>An async-enumerable sequence that has elements of <see cref="ValueTuple{T1, T2}"/> of each outer element and the inner elements of the same key, that are obtained by performing a grouped join on two sequences.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="outerKeySelector"/> or <paramref name="innerKeySelector"/> is null.</exception>
        public static AsyncEnumerable<(TOuter Outer, TempCollection<TInner> InnerElements)> GroupJoin<TOuter, TInner, TKey, TOuterCapture, TEqualityComparer>(this AsyncEnumerable<TOuter> outer,
            AsyncEnumerable<TInner> inner,
            TOuterCapture outerCaptureValue,
            Func<TOuterCapture, TOuter, TKey> outerKeySelector,
            Func<TInner, TKey> innerKeySelector,
            TEqualityComparer comparer)
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            ValidateArgument(outerKeySelector, nameof(outerKeySelector), 1);
            ValidateArgument(innerKeySelector, nameof(innerKeySelector), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.GroupJoinHelper<TKey>.GroupJoin(outer.GetAsyncEnumerator(),
                inner.GetAsyncEnumerator(),
                Internal.PromiseRefBase.DelegateWrapper.Create(outerCaptureValue, outerKeySelector),
                Internal.PromiseRefBase.DelegateWrapper.Create(innerKeySelector),
                comparer);
        }

        /// <summary>
        /// Correlates the elements of two async-enumerable sequences based on equality of keys and groups the results.
        /// </summary>
        /// <typeparam name="TOuter">The type of the elements of the first async-enumerable sequence.</typeparam>
        /// <typeparam name="TInner">The type of the elements of the second async-enumerable sequence.</typeparam>
        /// <typeparam name="TKey">The type of the keys returned by the key selector functions.</typeparam>
        /// <typeparam name="TInnerCapture">The type of the captured value that will be passed to <paramref name="innerKeySelector"/>.</typeparam>
        /// <typeparam name="TEqualityComparer">The type of the comparer.</typeparam>
        /// <param name="outer">The first async-enumerable sequence to join.</param>
        /// <param name="inner">The async-enumerable sequence to join to the first sequence.</param>
        /// <param name="outerKeySelector">A function to extract the join key from each element of the first sequence.</param>
        /// <param name="innerCaptureValue">The extra value that will be passed to <paramref name="innerKeySelector"/>.</param>
        /// <param name="innerKeySelector">A function to extract the join key from each element of the second sequence.</param>
        /// <param name="comparer">A comparer to compare keys.</param>
        /// <returns>An async-enumerable sequence that has elements of <see cref="ValueTuple{T1, T2}"/> of each outer element and the inner elements of the same key, that are obtained by performing a grouped join on two sequences.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="outerKeySelector"/> or <paramref name="innerKeySelector"/> is null.</exception>
        public static AsyncEnumerable<(TOuter Outer, TempCollection<TInner> InnerElements)> GroupJoin<TOuter, TInner, TKey, TInnerCapture, TEqualityComparer>(this AsyncEnumerable<TOuter> outer,
            AsyncEnumerable<TInner> inner,
            Func<TOuter, TKey> outerKeySelector,
            TInnerCapture innerCaptureValue,
            Func<TInnerCapture, TInner, TKey> innerKeySelector,
            TEqualityComparer comparer)
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            ValidateArgument(outerKeySelector, nameof(outerKeySelector), 1);
            ValidateArgument(innerKeySelector, nameof(innerKeySelector), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.GroupJoinHelper<TKey>.GroupJoin(outer.GetAsyncEnumerator(),
                inner.GetAsyncEnumerator(),
                Internal.PromiseRefBase.DelegateWrapper.Create(outerKeySelector),
                Internal.PromiseRefBase.DelegateWrapper.Create(innerCaptureValue, innerKeySelector),
                comparer);
        }

        /// <summary>
        /// Correlates the elements of two async-enumerable sequences based on equality of keys and groups the results.
        /// </summary>
        /// <typeparam name="TOuter">The type of the elements of the first async-enumerable sequence.</typeparam>
        /// <typeparam name="TInner">The type of the elements of the second async-enumerable sequence.</typeparam>
        /// <typeparam name="TKey">The type of the keys returned by the key selector functions.</typeparam>
        /// <typeparam name="TOuterCapture">The type of the captured value that will be passed to <paramref name="outerKeySelector"/>.</typeparam>
        /// <typeparam name="TInnerCapture">The type of the captured value that will be passed to <paramref name="innerKeySelector"/>.</typeparam>
        /// <typeparam name="TEqualityComparer">The type of the comparer.</typeparam>
        /// <param name="outer">The first async-enumerable sequence to join.</param>
        /// <param name="inner">The async-enumerable sequence to join to the first sequence.</param>
        /// <param name="outerCaptureValue">The extra value that will be passed to <paramref name="outerKeySelector"/>.</param>
        /// <param name="outerKeySelector">A function to extract the join key from each element of the first sequence.</param>
        /// <param name="innerCaptureValue">The extra value that will be passed to <paramref name="innerKeySelector"/>.</param>
        /// <param name="innerKeySelector">A function to extract the join key from each element of the second sequence.</param>
        /// <param name="comparer">A comparer to compare keys.</param>
        /// <returns>An async-enumerable sequence that has elements of <see cref="ValueTuple{T1, T2}"/> of each outer element and the inner elements of the same key, that are obtained by performing a grouped join on two sequences.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="outerKeySelector"/> or <paramref name="innerKeySelector"/> is null.</exception>
        public static AsyncEnumerable<(TOuter Outer, TempCollection<TInner> InnerElements)> GroupJoin<TOuter, TInner, TKey, TOuterCapture, TInnerCapture, TEqualityComparer>(this AsyncEnumerable<TOuter> outer,
            AsyncEnumerable<TInner> inner,
            TOuterCapture outerCaptureValue,
            Func<TOuterCapture, TOuter, TKey> outerKeySelector,
            TInnerCapture innerCaptureValue,
            Func<TInnerCapture, TInner, TKey> innerKeySelector,
            TEqualityComparer comparer)
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            ValidateArgument(outerKeySelector, nameof(outerKeySelector), 1);
            ValidateArgument(innerKeySelector, nameof(innerKeySelector), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.GroupJoinHelper<TKey>.GroupJoin(outer.GetAsyncEnumerator(),
                inner.GetAsyncEnumerator(),
                Internal.PromiseRefBase.DelegateWrapper.Create(outerCaptureValue, outerKeySelector),
                Internal.PromiseRefBase.DelegateWrapper.Create(innerCaptureValue, innerKeySelector),
                comparer);
        }

        /// <summary>
        /// Correlates the elements of two async-enumerable sequences based on equality of keys and groups the results. The default equality comparer is used to compare keys.
        /// </summary>
        /// <typeparam name="TOuter">The type of the elements of the first async-enumerable sequence.</typeparam>
        /// <typeparam name="TInner">The type of the elements of the second async-enumerable sequence.</typeparam>
        /// <typeparam name="TKey">The type of the keys returned by the key selector functions.</typeparam>
        /// <param name="outer">The first async-enumerable sequence to join.</param>
        /// <param name="inner">The async-enumerable sequence to join to the first sequence.</param>
        /// <param name="outerKeySelector">An asynchronous function to extract the join key from each element of the first sequence.</param>
        /// <param name="innerKeySelector">An asynchronous function to extract the join key from each element of the second sequence.</param>
        /// <returns>An async-enumerable sequence that has elements of <see cref="ValueTuple{T1, T2}"/> of each outer element and the inner elements of the same key, that are obtained by performing a grouped join on two sequences.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="outerKeySelector"/> or <paramref name="innerKeySelector"/> is null.</exception>
        public static AsyncEnumerable<(TOuter Outer, TempCollection<TInner> InnerElements)> GroupJoin<TOuter, TInner, TKey>(this AsyncEnumerable<TOuter> outer,
            AsyncEnumerable<TInner> inner,
            Func<TOuter, Promise<TKey>> outerKeySelector,
            Func<TInner, Promise<TKey>> innerKeySelector)
            => GroupJoin(outer, inner, outerKeySelector, innerKeySelector, EqualityComparer<TKey>.Default);

        /// <summary>
        /// Correlates the elements of two async-enumerable sequences based on equality of keys and groups the results. The default equality comparer is used to compare keys.
        /// </summary>
        /// <typeparam name="TOuter">The type of the elements of the first async-enumerable sequence.</typeparam>
        /// <typeparam name="TInner">The type of the elements of the second async-enumerable sequence.</typeparam>
        /// <typeparam name="TKey">The type of the keys returned by the key selector functions.</typeparam>
        /// <typeparam name="TOuterCapture">The type of the captured value that will be passed to <paramref name="outerKeySelector"/>.</typeparam>
        /// <param name="outer">The first async-enumerable sequence to join.</param>
        /// <param name="inner">The async-enumerable sequence to join to the first sequence.</param>
        /// <param name="outerCaptureValue">The extra value that will be passed to <paramref name="outerKeySelector"/>.</param>
        /// <param name="outerKeySelector">An asynchronous function to extract the join key from each element of the first sequence.</param>
        /// <param name="innerKeySelector">An asynchronous function to extract the join key from each element of the second sequence.</param>
        /// <returns>An async-enumerable sequence that has elements of <see cref="ValueTuple{T1, T2}"/> of each outer element and the inner elements of the same key, that are obtained by performing a grouped join on two sequences.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="outerKeySelector"/> or <paramref name="innerKeySelector"/> is null.</exception>
        public static AsyncEnumerable<(TOuter Outer, TempCollection<TInner> InnerElements)> GroupJoin<TOuter, TInner, TKey, TOuterCapture>(this AsyncEnumerable<TOuter> outer,
            AsyncEnumerable<TInner> inner,
            TOuterCapture outerCaptureValue,
            Func<TOuterCapture, TOuter, Promise<TKey>> outerKeySelector,
            Func<TInner, Promise<TKey>> innerKeySelector)
            => GroupJoin(outer, inner, outerCaptureValue, outerKeySelector, innerKeySelector, EqualityComparer<TKey>.Default);

        /// <summary>
        /// Correlates the elements of two async-enumerable sequences based on equality of keys and groups the results. The default equality comparer is used to compare keys.
        /// </summary>
        /// <typeparam name="TOuter">The type of the elements of the first async-enumerable sequence.</typeparam>
        /// <typeparam name="TInner">The type of the elements of the second async-enumerable sequence.</typeparam>
        /// <typeparam name="TKey">The type of the keys returned by the key selector functions.</typeparam>
        /// <typeparam name="TInnerCapture">The type of the captured value that will be passed to <paramref name="innerKeySelector"/>.</typeparam>
        /// <param name="outer">The first async-enumerable sequence to join.</param>
        /// <param name="inner">The async-enumerable sequence to join to the first sequence.</param>
        /// <param name="outerKeySelector">An asynchronous function to extract the join key from each element of the first sequence.</param>
        /// <param name="innerCaptureValue">The extra value that will be passed to <paramref name="innerKeySelector"/>.</param>
        /// <param name="innerKeySelector">An asynchronous function to extract the join key from each element of the second sequence.</param>
        /// <returns>An async-enumerable sequence that has elements of <see cref="ValueTuple{T1, T2}"/> of each outer element and the inner elements of the same key, that are obtained by performing a grouped join on two sequences.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="outerKeySelector"/> or <paramref name="innerKeySelector"/> is null.</exception>
        public static AsyncEnumerable<(TOuter Outer, TempCollection<TInner> InnerElements)> GroupJoin<TOuter, TInner, TKey, TInnerCapture>(this AsyncEnumerable<TOuter> outer,
            AsyncEnumerable<TInner> inner,
            Func<TOuter, Promise<TKey>> outerKeySelector,
            TInnerCapture innerCaptureValue,
            Func<TInnerCapture, TInner, Promise<TKey>> innerKeySelector)
            => GroupJoin(outer, inner, outerKeySelector, innerCaptureValue, innerKeySelector, EqualityComparer<TKey>.Default);

        /// <summary>
        /// Correlates the elements of two async-enumerable sequences based on equality of keys and groups the results. The default equality comparer is used to compare keys.
        /// </summary>
        /// <typeparam name="TOuter">The type of the elements of the first async-enumerable sequence.</typeparam>
        /// <typeparam name="TInner">The type of the elements of the second async-enumerable sequence.</typeparam>
        /// <typeparam name="TKey">The type of the keys returned by the key selector functions.</typeparam>
        /// <typeparam name="TOuterCapture">The type of the captured value that will be passed to <paramref name="outerKeySelector"/>.</typeparam>
        /// <typeparam name="TInnerCapture">The type of the captured value that will be passed to <paramref name="innerKeySelector"/>.</typeparam>
        /// <param name="outer">The first async-enumerable sequence to join.</param>
        /// <param name="inner">The async-enumerable sequence to join to the first sequence.</param>
        /// <param name="outerCaptureValue">The extra value that will be passed to <paramref name="outerKeySelector"/>.</param>
        /// <param name="outerKeySelector">An asynchronous function to extract the join key from each element of the first sequence.</param>
        /// <param name="innerCaptureValue">The extra value that will be passed to <paramref name="innerKeySelector"/>.</param>
        /// <param name="innerKeySelector">An asynchronous function to extract the join key from each element of the second sequence.</param>
        /// <returns>An async-enumerable sequence that has elements of <see cref="ValueTuple{T1, T2}"/> of each outer element and the inner elements of the same key, that are obtained by performing a grouped join on two sequences.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="outerKeySelector"/> or <paramref name="innerKeySelector"/> is null.</exception>
        public static AsyncEnumerable<(TOuter Outer, TempCollection<TInner> InnerElements)> GroupJoin<TOuter, TInner, TKey, TOuterCapture, TInnerCapture>(this AsyncEnumerable<TOuter> outer,
            AsyncEnumerable<TInner> inner,
            TOuterCapture outerCaptureValue,
            Func<TOuterCapture, TOuter, Promise<TKey>> outerKeySelector,
            TInnerCapture innerCaptureValue,
            Func<TInnerCapture, TInner, Promise<TKey>> innerKeySelector)
            => GroupJoin(outer, inner, outerCaptureValue, outerKeySelector, innerCaptureValue, innerKeySelector, EqualityComparer<TKey>.Default);

        /// <summary>
        /// Correlates the elements of two async-enumerable sequences based on equality of keys and groups the results.
        /// </summary>
        /// <typeparam name="TOuter">The type of the elements of the first async-enumerable sequence.</typeparam>
        /// <typeparam name="TInner">The type of the elements of the second async-enumerable sequence.</typeparam>
        /// <typeparam name="TKey">The type of the keys returned by the key selector functions.</typeparam>
        /// <typeparam name="TEqualityComparer">The type of the comparer.</typeparam>
        /// <param name="outer">The first async-enumerable sequence to join.</param>
        /// <param name="inner">The async-enumerable sequence to join to the first sequence.</param>
        /// <param name="outerKeySelector">An asynchronous function to extract the join key from each element of the first sequence.</param>
        /// <param name="innerKeySelector">An asynchronous function to extract the join key from each element of the second sequence.</param>
        /// <param name="comparer">A comparer to compare keys.</param>
        /// <returns>An async-enumerable sequence that has elements of <see cref="ValueTuple{T1, T2}"/> of each outer element and the inner elements of the same key, that are obtained by performing a grouped join on two sequences.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="outerKeySelector"/> or <paramref name="innerKeySelector"/> or <paramref name="comparer"/> is null.</exception>
        public static AsyncEnumerable<(TOuter Outer, TempCollection<TInner> InnerElements)> GroupJoin<TOuter, TInner, TKey, TEqualityComparer>(this AsyncEnumerable<TOuter> outer,
            AsyncEnumerable<TInner> inner,
            Func<TOuter, Promise<TKey>> outerKeySelector,
            Func<TInner, Promise<TKey>> innerKeySelector,
            TEqualityComparer comparer)
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            ValidateArgument(outerKeySelector, nameof(outerKeySelector), 1);
            ValidateArgument(innerKeySelector, nameof(innerKeySelector), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.GroupJoinHelper<TKey>.GroupJoinAwait(outer.GetAsyncEnumerator(),
                inner.GetAsyncEnumerator(),
                Internal.PromiseRefBase.DelegateWrapper.Create(outerKeySelector),
                Internal.PromiseRefBase.DelegateWrapper.Create(innerKeySelector),
                comparer);
        }

        /// <summary>
        /// Correlates the elements of two async-enumerable sequences based on equality of keys and groups the results.
        /// </summary>
        /// <typeparam name="TOuter">The type of the elements of the first async-enumerable sequence.</typeparam>
        /// <typeparam name="TInner">The type of the elements of the second async-enumerable sequence.</typeparam>
        /// <typeparam name="TKey">The type of the keys returned by the key selector functions.</typeparam>
        /// <typeparam name="TOuterCapture">The type of the captured value that will be passed to <paramref name="outerKeySelector"/>.</typeparam>
        /// <typeparam name="TEqualityComparer">The type of the comparer.</typeparam>
        /// <param name="outer">The first async-enumerable sequence to join.</param>
        /// <param name="inner">The async-enumerable sequence to join to the first sequence.</param>
        /// <param name="outerCaptureValue">The extra value that will be passed to <paramref name="outerKeySelector"/>.</param>
        /// <param name="outerKeySelector">An asynchronous function to extract the join key from each element of the first sequence.</param>
        /// <param name="innerKeySelector">An asynchronous function to extract the join key from each element of the second sequence.</param>
        /// <param name="comparer">A comparer to compare keys.</param>
        /// <returns>An async-enumerable sequence that has elements of <see cref="ValueTuple{T1, T2}"/> of each outer element and the inner elements of the same key, that are obtained by performing a grouped join on two sequences.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="outerKeySelector"/> or <paramref name="innerKeySelector"/> is null.</exception>
        public static AsyncEnumerable<(TOuter Outer, TempCollection<TInner> InnerElements)> GroupJoin<TOuter, TInner, TKey, TOuterCapture, TEqualityComparer>(this AsyncEnumerable<TOuter> outer,
            AsyncEnumerable<TInner> inner,
            TOuterCapture outerCaptureValue,
            Func<TOuterCapture, TOuter, Promise<TKey>> outerKeySelector,
            Func<TInner, Promise<TKey>> innerKeySelector,
            TEqualityComparer comparer)
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            ValidateArgument(outerKeySelector, nameof(outerKeySelector), 1);
            ValidateArgument(innerKeySelector, nameof(innerKeySelector), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.GroupJoinHelper<TKey>.GroupJoinAwait(outer.GetAsyncEnumerator(),
                inner.GetAsyncEnumerator(),
                Internal.PromiseRefBase.DelegateWrapper.Create(outerCaptureValue, outerKeySelector),
                Internal.PromiseRefBase.DelegateWrapper.Create(innerKeySelector),
                comparer);
        }

        /// <summary>
        /// Correlates the elements of two async-enumerable sequences based on equality of keys and groups the results.
        /// </summary>
        /// <typeparam name="TOuter">The type of the elements of the first async-enumerable sequence.</typeparam>
        /// <typeparam name="TInner">The type of the elements of the second async-enumerable sequence.</typeparam>
        /// <typeparam name="TKey">The type of the keys returned by the key selector functions.</typeparam>
        /// <typeparam name="TInnerCapture">The type of the captured value that will be passed to <paramref name="innerKeySelector"/>.</typeparam>
        /// <typeparam name="TEqualityComparer">The type of the comparer.</typeparam>
        /// <param name="outer">The first async-enumerable sequence to join.</param>
        /// <param name="inner">The async-enumerable sequence to join to the first sequence.</param>
        /// <param name="outerKeySelector">An asynchronous function to extract the join key from each element of the first sequence.</param>
        /// <param name="innerCaptureValue">The extra value that will be passed to <paramref name="innerKeySelector"/>.</param>
        /// <param name="innerKeySelector">An asynchronous function to extract the join key from each element of the second sequence.</param>
        /// <param name="comparer">A comparer to compare keys.</param>
        /// <returns>An async-enumerable sequence that has elements of <see cref="ValueTuple{T1, T2}"/> of each outer element and the inner elements of the same key, that are obtained by performing a grouped join on two sequences.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="outerKeySelector"/> or <paramref name="innerKeySelector"/> is null.</exception>
        public static AsyncEnumerable<(TOuter Outer, TempCollection<TInner> InnerElements)> GroupJoin<TOuter, TInner, TKey, TInnerCapture, TEqualityComparer>(this AsyncEnumerable<TOuter> outer,
            AsyncEnumerable<TInner> inner,
            Func<TOuter, Promise<TKey>> outerKeySelector,
            TInnerCapture innerCaptureValue,
            Func<TInnerCapture, TInner, Promise<TKey>> innerKeySelector,
            TEqualityComparer comparer)
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            ValidateArgument(outerKeySelector, nameof(outerKeySelector), 1);
            ValidateArgument(innerKeySelector, nameof(innerKeySelector), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.GroupJoinHelper<TKey>.GroupJoinAwait(outer.GetAsyncEnumerator(),
                inner.GetAsyncEnumerator(),
                Internal.PromiseRefBase.DelegateWrapper.Create(outerKeySelector),
                Internal.PromiseRefBase.DelegateWrapper.Create(innerCaptureValue, innerKeySelector),
                comparer);
        }

        /// <summary>
        /// Correlates the elements of two async-enumerable sequences based on equality of keys and groups the results.
        /// </summary>
        /// <typeparam name="TOuter">The type of the elements of the first async-enumerable sequence.</typeparam>
        /// <typeparam name="TInner">The type of the elements of the second async-enumerable sequence.</typeparam>
        /// <typeparam name="TKey">The type of the keys returned by the key selector functions.</typeparam>
        /// <typeparam name="TOuterCapture">The type of the captured value that will be passed to <paramref name="outerKeySelector"/>.</typeparam>
        /// <typeparam name="TInnerCapture">The type of the captured value that will be passed to <paramref name="innerKeySelector"/>.</typeparam>
        /// <typeparam name="TEqualityComparer">The type of the comparer.</typeparam>
        /// <param name="outer">The first async-enumerable sequence to join.</param>
        /// <param name="inner">The async-enumerable sequence to join to the first sequence.</param>
        /// <param name="outerCaptureValue">The extra value that will be passed to <paramref name="outerKeySelector"/>.</param>
        /// <param name="outerKeySelector">An asynchronous function to extract the join key from each element of the first sequence.</param>
        /// <param name="innerCaptureValue">The extra value that will be passed to <paramref name="innerKeySelector"/>.</param>
        /// <param name="innerKeySelector">An asynchronous function to extract the join key from each element of the second sequence.</param>
        /// <param name="comparer">A comparer to compare keys.</param>
        /// <returns>An async-enumerable sequence that has elements of <see cref="ValueTuple{T1, T2}"/> of each outer element and the inner elements of the same key, that are obtained by performing a grouped join on two sequences.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="outerKeySelector"/> or <paramref name="innerKeySelector"/> is null.</exception>
        public static AsyncEnumerable<(TOuter Outer, TempCollection<TInner> InnerElements)> GroupJoin<TOuter, TInner, TKey, TOuterCapture, TInnerCapture, TEqualityComparer>(this AsyncEnumerable<TOuter> outer,
            AsyncEnumerable<TInner> inner,
            TOuterCapture outerCaptureValue,
            Func<TOuterCapture, TOuter, Promise<TKey>> outerKeySelector,
            TInnerCapture innerCaptureValue,
            Func<TInnerCapture, TInner, Promise<TKey>> innerKeySelector,
            TEqualityComparer comparer)
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            ValidateArgument(outerKeySelector, nameof(outerKeySelector), 1);
            ValidateArgument(innerKeySelector, nameof(innerKeySelector), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.GroupJoinHelper<TKey>.GroupJoinAwait(outer.GetAsyncEnumerator(),
                inner.GetAsyncEnumerator(),
                Internal.PromiseRefBase.DelegateWrapper.Create(outerCaptureValue, outerKeySelector),
                Internal.PromiseRefBase.DelegateWrapper.Create(innerCaptureValue, innerKeySelector),
                comparer);
        }
        #endregion AsyncEnumerable

        #region ConfiguredAsyncEnumerable
        /// <summary>
        /// Correlates the elements of two async-enumerable sequences based on equality of keys and groups the results, using the outer async-enumerable as the await configuration. The default equality comparer is used to compare keys.
        /// </summary>
        /// <typeparam name="TOuter">The type of the elements of the first async-enumerable sequence.</typeparam>
        /// <typeparam name="TInner">The type of the elements of the second async-enumerable sequence.</typeparam>
        /// <typeparam name="TKey">The type of the keys returned by the key selector functions.</typeparam>
        /// <param name="configuredOuter">The first async-enumerable sequence to join with await configuration.</param>
        /// <param name="inner">The async-enumerable sequence to join to the first sequence.</param>
        /// <param name="outerKeySelector">A function to extract the join key from each element of the first sequence.</param>
        /// <param name="innerKeySelector">A function to extract the join key from each element of the second sequence.</param>
        /// <returns>An async-enumerable sequence that has elements of <see cref="ValueTuple{T1, T2}"/> of each outer element and the inner elements of the same key, that are obtained by performing a grouped join on two sequences.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="outerKeySelector"/> or <paramref name="innerKeySelector"/> is null.</exception>
        public static AsyncEnumerable<(TOuter Outer, TempCollection<TInner> InnerElements)> GroupJoin<TOuter, TInner, TKey>(this in ConfiguredAsyncEnumerable<TOuter> configuredOuter,
            AsyncEnumerable<TInner> inner,
            Func<TOuter, TKey> outerKeySelector,
            Func<TInner, TKey> innerKeySelector)
            => GroupJoin(configuredOuter, inner, outerKeySelector, innerKeySelector, EqualityComparer<TKey>.Default);

        /// <summary>
        /// Correlates the elements of two async-enumerable sequences based on equality of keys and groups the results, using the outer async-enumerable as the await configuration. The default equality comparer is used to compare keys.
        /// </summary>
        /// <typeparam name="TOuter">The type of the elements of the first async-enumerable sequence.</typeparam>
        /// <typeparam name="TInner">The type of the elements of the second async-enumerable sequence.</typeparam>
        /// <typeparam name="TKey">The type of the keys returned by the key selector functions.</typeparam>
        /// <typeparam name="TOuterCapture">The type of the captured value that will be passed to <paramref name="outerKeySelector"/>.</typeparam>
        /// <param name="configuredOuter">The first async-enumerable sequence to join with await configuration.</param>
        /// <param name="inner">The async-enumerable sequence to join to the first sequence.</param>
        /// <param name="outerCaptureValue">The extra value that will be passed to <paramref name="outerKeySelector"/>.</param>
        /// <param name="outerKeySelector">A function to extract the join key from each element of the first sequence.</param>
        /// <param name="innerKeySelector">A function to extract the join key from each element of the second sequence.</param>
        /// <returns>An async-enumerable sequence that has elements of <see cref="ValueTuple{T1, T2}"/> of each outer element and the inner elements of the same key, that are obtained by performing a grouped join on two sequences.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="outerKeySelector"/> or <paramref name="innerKeySelector"/> is null.</exception>
        public static AsyncEnumerable<(TOuter Outer, TempCollection<TInner> InnerElements)> GroupJoin<TOuter, TInner, TKey, TOuterCapture>(this in ConfiguredAsyncEnumerable<TOuter> configuredOuter,
            AsyncEnumerable<TInner> inner,
            TOuterCapture outerCaptureValue,
            Func<TOuterCapture, TOuter, TKey> outerKeySelector,
            Func<TInner, TKey> innerKeySelector)
            => GroupJoin(configuredOuter, inner, outerCaptureValue, outerKeySelector, innerKeySelector, EqualityComparer<TKey>.Default);

        /// <summary>
        /// Correlates the elements of two async-enumerable sequences based on equality of keys and groups the results, using the outer async-enumerable as the await configuration. The default equality comparer is used to compare keys.
        /// </summary>
        /// <typeparam name="TOuter">The type of the elements of the first async-enumerable sequence.</typeparam>
        /// <typeparam name="TInner">The type of the elements of the second async-enumerable sequence.</typeparam>
        /// <typeparam name="TKey">The type of the keys returned by the key selector functions.</typeparam>
        /// <typeparam name="TInnerCapture">The type of the captured value that will be passed to <paramref name="innerKeySelector"/>.</typeparam>
        /// <param name="configuredOuter">The first async-enumerable sequence to join with await configuration.</param>
        /// <param name="inner">The async-enumerable sequence to join to the first sequence.</param>
        /// <param name="outerKeySelector">A function to extract the join key from each element of the first sequence.</param>
        /// <param name="innerCaptureValue">The extra value that will be passed to <paramref name="innerKeySelector"/>.</param>
        /// <param name="innerKeySelector">A function to extract the join key from each element of the second sequence.</param>
        /// <returns>An async-enumerable sequence that has elements of <see cref="ValueTuple{T1, T2}"/> of each outer element and the inner elements of the same key, that are obtained by performing a grouped join on two sequences.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="outerKeySelector"/> or <paramref name="innerKeySelector"/> is null.</exception>
        public static AsyncEnumerable<(TOuter Outer, TempCollection<TInner> InnerElements)> GroupJoin<TOuter, TInner, TKey, TInnerCapture>(this in ConfiguredAsyncEnumerable<TOuter> configuredOuter,
            AsyncEnumerable<TInner> inner,
            Func<TOuter, TKey> outerKeySelector,
            TInnerCapture innerCaptureValue,
            Func<TInnerCapture, TInner, TKey> innerKeySelector)
            => GroupJoin(configuredOuter, inner, outerKeySelector, innerCaptureValue, innerKeySelector, EqualityComparer<TKey>.Default);

        /// <summary>
        /// Correlates the elements of two async-enumerable sequences based on equality of keys and groups the results, using the outer async-enumerable as the await configuration. The default equality comparer is used to compare keys.
        /// </summary>
        /// <typeparam name="TOuter">The type of the elements of the first async-enumerable sequence.</typeparam>
        /// <typeparam name="TInner">The type of the elements of the second async-enumerable sequence.</typeparam>
        /// <typeparam name="TKey">The type of the keys returned by the key selector functions.</typeparam>
        /// <typeparam name="TOuterCapture">The type of the captured value that will be passed to <paramref name="outerKeySelector"/>.</typeparam>
        /// <typeparam name="TInnerCapture">The type of the captured value that will be passed to <paramref name="innerKeySelector"/>.</typeparam>
        /// <param name="configuredOuter">The first async-enumerable sequence to join with await configuration.</param>
        /// <param name="inner">The async-enumerable sequence to join to the first sequence.</param>
        /// <param name="outerCaptureValue">The extra value that will be passed to <paramref name="outerKeySelector"/>.</param>
        /// <param name="outerKeySelector">A function to extract the join key from each element of the first sequence.</param>
        /// <param name="innerCaptureValue">The extra value that will be passed to <paramref name="innerKeySelector"/>.</param>
        /// <param name="innerKeySelector">A function to extract the join key from each element of the second sequence.</param>
        /// <returns>An async-enumerable sequence that has elements of <see cref="ValueTuple{T1, T2}"/> of each outer element and the inner elements of the same key, that are obtained by performing a grouped join on two sequences.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="outerKeySelector"/> or <paramref name="innerKeySelector"/> is null.</exception>
        public static AsyncEnumerable<(TOuter Outer, TempCollection<TInner> InnerElements)> GroupJoin<TOuter, TInner, TKey, TOuterCapture, TInnerCapture>(this in ConfiguredAsyncEnumerable<TOuter> configuredOuter,
            AsyncEnumerable<TInner> inner,
            TOuterCapture outerCaptureValue,
            Func<TOuterCapture, TOuter, TKey> outerKeySelector,
            TInnerCapture innerCaptureValue,
            Func<TInnerCapture, TInner, TKey> innerKeySelector)
            => GroupJoin(configuredOuter, inner, outerCaptureValue, outerKeySelector, innerCaptureValue, innerKeySelector, EqualityComparer<TKey>.Default);

        /// <summary>
        /// Correlates the elements of two async-enumerable sequences based on equality of keys and groups the results, using the outer async-enumerable as the await configuration.
        /// </summary>
        /// <typeparam name="TOuter">The type of the elements of the first async-enumerable sequence.</typeparam>
        /// <typeparam name="TInner">The type of the elements of the second async-enumerable sequence.</typeparam>
        /// <typeparam name="TKey">The type of the keys returned by the key selector functions.</typeparam>
        /// <typeparam name="TEqualityComparer">The type of the comparer.</typeparam>
        /// <param name="configuredOuter">The first async-enumerable sequence to join with await configuration.</param>
        /// <param name="inner">The async-enumerable sequence to join to the first sequence.</param>
        /// <param name="outerKeySelector">A function to extract the join key from each element of the first sequence.</param>
        /// <param name="innerKeySelector">A function to extract the join key from each element of the second sequence.</param>
        /// <param name="comparer">A comparer to compare keys.</param>
        /// <returns>An async-enumerable sequence that has elements of <see cref="ValueTuple{T1, T2}"/> of each outer element and the inner elements of the same key, that are obtained by performing a grouped join on two sequences.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="outerKeySelector"/> or <paramref name="innerKeySelector"/> or <paramref name="comparer"/> is null.</exception>
        public static AsyncEnumerable<(TOuter Outer, TempCollection<TInner> InnerElements)> GroupJoin<TOuter, TInner, TKey, TEqualityComparer>(this in ConfiguredAsyncEnumerable<TOuter> configuredOuter,
            AsyncEnumerable<TInner> inner,
            Func<TOuter, TKey> outerKeySelector,
            Func<TInner, TKey> innerKeySelector,
            TEqualityComparer comparer)
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            ValidateArgument(outerKeySelector, nameof(outerKeySelector), 1);
            ValidateArgument(innerKeySelector, nameof(innerKeySelector), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.GroupJoinHelper<TKey>.GroupJoin(configuredOuter.GetAsyncEnumerator(),
                inner.GetAsyncEnumerator(),
                Internal.PromiseRefBase.DelegateWrapper.Create(outerKeySelector),
                Internal.PromiseRefBase.DelegateWrapper.Create(innerKeySelector),
                comparer);
        }

        /// <summary>
        /// Correlates the elements of two async-enumerable sequences based on equality of keys and groups the results, using the outer async-enumerable as the await configuration.
        /// </summary>
        /// <typeparam name="TOuter">The type of the elements of the first async-enumerable sequence.</typeparam>
        /// <typeparam name="TInner">The type of the elements of the second async-enumerable sequence.</typeparam>
        /// <typeparam name="TKey">The type of the keys returned by the key selector functions.</typeparam>
        /// <typeparam name="TOuterCapture">The type of the captured value that will be passed to <paramref name="outerKeySelector"/>.</typeparam>
        /// <typeparam name="TEqualityComparer">The type of the comparer.</typeparam>
        /// <param name="configuredOuter">The first async-enumerable sequence to join with await configuration.</param>
        /// <param name="inner">The async-enumerable sequence to join to the first sequence.</param>
        /// <param name="outerCaptureValue">The extra value that will be passed to <paramref name="outerKeySelector"/>.</param>
        /// <param name="outerKeySelector">A function to extract the join key from each element of the first sequence.</param>
        /// <param name="innerKeySelector">A function to extract the join key from each element of the second sequence.</param>
        /// <param name="comparer">A comparer to compare keys.</param>
        /// <returns>An async-enumerable sequence that has elements of <see cref="ValueTuple{T1, T2}"/> of each outer element and the inner elements of the same key, that are obtained by performing a grouped join on two sequences.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="outerKeySelector"/> or <paramref name="innerKeySelector"/> is null.</exception>
        public static AsyncEnumerable<(TOuter Outer, TempCollection<TInner> InnerElements)> GroupJoin<TOuter, TInner, TKey, TOuterCapture, TEqualityComparer>(this in ConfiguredAsyncEnumerable<TOuter> configuredOuter,
            AsyncEnumerable<TInner> inner,
            TOuterCapture outerCaptureValue,
            Func<TOuterCapture, TOuter, TKey> outerKeySelector,
            Func<TInner, TKey> innerKeySelector,
            TEqualityComparer comparer)
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            ValidateArgument(outerKeySelector, nameof(outerKeySelector), 1);
            ValidateArgument(innerKeySelector, nameof(innerKeySelector), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.GroupJoinHelper<TKey>.GroupJoin(configuredOuter.GetAsyncEnumerator(),
                inner.GetAsyncEnumerator(),
                Internal.PromiseRefBase.DelegateWrapper.Create(outerCaptureValue, outerKeySelector),
                Internal.PromiseRefBase.DelegateWrapper.Create(innerKeySelector),
                comparer);
        }

        /// <summary>
        /// Correlates the elements of two async-enumerable sequences based on equality of keys and groups the results, using the outer async-enumerable as the await configuration.
        /// </summary>
        /// <typeparam name="TOuter">The type of the elements of the first async-enumerable sequence.</typeparam>
        /// <typeparam name="TInner">The type of the elements of the second async-enumerable sequence.</typeparam>
        /// <typeparam name="TKey">The type of the keys returned by the key selector functions.</typeparam>
        /// <typeparam name="TInnerCapture">The type of the captured value that will be passed to <paramref name="innerKeySelector"/>.</typeparam>
        /// <typeparam name="TEqualityComparer">The type of the comparer.</typeparam>
        /// <param name="configuredOuter">The first async-enumerable sequence to join with await configuration.</param>
        /// <param name="inner">The async-enumerable sequence to join to the first sequence.</param>
        /// <param name="outerKeySelector">A function to extract the join key from each element of the first sequence.</param>
        /// <param name="innerCaptureValue">The extra value that will be passed to <paramref name="innerKeySelector"/>.</param>
        /// <param name="innerKeySelector">A function to extract the join key from each element of the second sequence.</param>
        /// <param name="comparer">A comparer to compare keys.</param>
        /// <returns>An async-enumerable sequence that has elements of <see cref="ValueTuple{T1, T2}"/> of each outer element and the inner elements of the same key, that are obtained by performing a grouped join on two sequences.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="outerKeySelector"/> or <paramref name="innerKeySelector"/> is null.</exception>
        public static AsyncEnumerable<(TOuter Outer, TempCollection<TInner> InnerElements)> GroupJoin<TOuter, TInner, TKey, TInnerCapture, TEqualityComparer>(this in ConfiguredAsyncEnumerable<TOuter> configuredOuter,
            AsyncEnumerable<TInner> inner,
            Func<TOuter, TKey> outerKeySelector,
            TInnerCapture innerCaptureValue,
            Func<TInnerCapture, TInner, TKey> innerKeySelector,
            TEqualityComparer comparer)
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            ValidateArgument(outerKeySelector, nameof(outerKeySelector), 1);
            ValidateArgument(innerKeySelector, nameof(innerKeySelector), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.GroupJoinHelper<TKey>.GroupJoin(configuredOuter.GetAsyncEnumerator(),
                inner.GetAsyncEnumerator(),
                Internal.PromiseRefBase.DelegateWrapper.Create(outerKeySelector),
                Internal.PromiseRefBase.DelegateWrapper.Create(innerCaptureValue, innerKeySelector),
                comparer);
        }

        /// <summary>
        /// Correlates the elements of two async-enumerable sequences based on equality of keys and groups the results, using the outer async-enumerable as the await configuration.
        /// </summary>
        /// <typeparam name="TOuter">The type of the elements of the first async-enumerable sequence.</typeparam>
        /// <typeparam name="TInner">The type of the elements of the second async-enumerable sequence.</typeparam>
        /// <typeparam name="TKey">The type of the keys returned by the key selector functions.</typeparam>
        /// <typeparam name="TOuterCapture">The type of the captured value that will be passed to <paramref name="outerKeySelector"/>.</typeparam>
        /// <typeparam name="TInnerCapture">The type of the captured value that will be passed to <paramref name="innerKeySelector"/>.</typeparam>
        /// <typeparam name="TEqualityComparer">The type of the comparer.</typeparam>
        /// <param name="configuredOuter">The first async-enumerable sequence to join with await configuration.</param>
        /// <param name="inner">The async-enumerable sequence to join to the first sequence.</param>
        /// <param name="outerCaptureValue">The extra value that will be passed to <paramref name="outerKeySelector"/>.</param>
        /// <param name="outerKeySelector">A function to extract the join key from each element of the first sequence.</param>
        /// <param name="innerCaptureValue">The extra value that will be passed to <paramref name="innerKeySelector"/>.</param>
        /// <param name="innerKeySelector">A function to extract the join key from each element of the second sequence.</param>
        /// <param name="comparer">A comparer to compare keys.</param>
        /// <returns>An async-enumerable sequence that has elements of <see cref="ValueTuple{T1, T2}"/> of each outer element and the inner elements of the same key, that are obtained by performing a grouped join on two sequences.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="outerKeySelector"/> or <paramref name="innerKeySelector"/> is null.</exception>
        public static AsyncEnumerable<(TOuter Outer, TempCollection<TInner> InnerElements)> GroupJoin<TOuter, TInner, TKey, TOuterCapture, TInnerCapture, TEqualityComparer>(this in ConfiguredAsyncEnumerable<TOuter> configuredOuter,
            AsyncEnumerable<TInner> inner,
            TOuterCapture outerCaptureValue,
            Func<TOuterCapture, TOuter, TKey> outerKeySelector,
            TInnerCapture innerCaptureValue,
            Func<TInnerCapture, TInner, TKey> innerKeySelector,
            TEqualityComparer comparer)
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            ValidateArgument(outerKeySelector, nameof(outerKeySelector), 1);
            ValidateArgument(innerKeySelector, nameof(innerKeySelector), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.GroupJoinHelper<TKey>.GroupJoin(configuredOuter.GetAsyncEnumerator(),
                inner.GetAsyncEnumerator(),
                Internal.PromiseRefBase.DelegateWrapper.Create(outerCaptureValue, outerKeySelector),
                Internal.PromiseRefBase.DelegateWrapper.Create(innerCaptureValue, innerKeySelector),
                comparer);
        }

        /// <summary>
        /// Correlates the elements of two async-enumerable sequences based on equality of keys and groups the results, using the outer async-enumerable as the await configuration. The default equality comparer is used to compare keys.
        /// </summary>
        /// <typeparam name="TOuter">The type of the elements of the first async-enumerable sequence.</typeparam>
        /// <typeparam name="TInner">The type of the elements of the second async-enumerable sequence.</typeparam>
        /// <typeparam name="TKey">The type of the keys returned by the key selector functions.</typeparam>
        /// <param name="configuredOuter">The first async-enumerable sequence to join with await configuration.</param>
        /// <param name="inner">The async-enumerable sequence to join to the first sequence.</param>
        /// <param name="outerKeySelector">An asynchronous function to extract the join key from each element of the first sequence.</param>
        /// <param name="innerKeySelector">An asynchronous function to extract the join key from each element of the second sequence.</param>
        /// <returns>An async-enumerable sequence that has elements of <see cref="ValueTuple{T1, T2}"/> of each outer element and the inner elements of the same key, that are obtained by performing a grouped join on two sequences.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="outerKeySelector"/> or <paramref name="innerKeySelector"/> is null.</exception>
        public static AsyncEnumerable<(TOuter Outer, TempCollection<TInner> InnerElements)> GroupJoin<TOuter, TInner, TKey>(this in ConfiguredAsyncEnumerable<TOuter> configuredOuter,
            AsyncEnumerable<TInner> inner,
            Func<TOuter, Promise<TKey>> outerKeySelector,
            Func<TInner, Promise<TKey>> innerKeySelector)
            => GroupJoin(configuredOuter, inner, outerKeySelector, innerKeySelector, EqualityComparer<TKey>.Default);

        /// <summary>
        /// Correlates the elements of two async-enumerable sequences based on equality of keys and groups the results, using the outer async-enumerable as the await configuration. The default equality comparer is used to compare keys.
        /// </summary>
        /// <typeparam name="TOuter">The type of the elements of the first async-enumerable sequence.</typeparam>
        /// <typeparam name="TInner">The type of the elements of the second async-enumerable sequence.</typeparam>
        /// <typeparam name="TKey">The type of the keys returned by the key selector functions.</typeparam>
        /// <typeparam name="TOuterCapture">The type of the captured value that will be passed to <paramref name="outerKeySelector"/>.</typeparam>
        /// <param name="configuredOuter">The first async-enumerable sequence to join with await configuration.</param>
        /// <param name="inner">The async-enumerable sequence to join to the first sequence.</param>
        /// <param name="outerCaptureValue">The extra value that will be passed to <paramref name="outerKeySelector"/>.</param>
        /// <param name="outerKeySelector">An asynchronous function to extract the join key from each element of the first sequence.</param>
        /// <param name="innerKeySelector">An asynchronous function to extract the join key from each element of the second sequence.</param>
        /// <returns>An async-enumerable sequence that has elements of <see cref="ValueTuple{T1, T2}"/> of each outer element and the inner elements of the same key, that are obtained by performing a grouped join on two sequences.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="outerKeySelector"/> or <paramref name="innerKeySelector"/> is null.</exception>
        public static AsyncEnumerable<(TOuter Outer, TempCollection<TInner> InnerElements)> GroupJoin<TOuter, TInner, TKey, TOuterCapture>(this in ConfiguredAsyncEnumerable<TOuter> configuredOuter,
            AsyncEnumerable<TInner> inner,
            TOuterCapture outerCaptureValue,
            Func<TOuterCapture, TOuter, Promise<TKey>> outerKeySelector,
            Func<TInner, Promise<TKey>> innerKeySelector)
            => GroupJoin(configuredOuter, inner, outerCaptureValue, outerKeySelector, innerKeySelector, EqualityComparer<TKey>.Default);

        /// <summary>
        /// Correlates the elements of two async-enumerable sequences based on equality of keys and groups the results, using the outer async-enumerable as the await configuration. The default equality comparer is used to compare keys.
        /// </summary>
        /// <typeparam name="TOuter">The type of the elements of the first async-enumerable sequence.</typeparam>
        /// <typeparam name="TInner">The type of the elements of the second async-enumerable sequence.</typeparam>
        /// <typeparam name="TKey">The type of the keys returned by the key selector functions.</typeparam>
        /// <typeparam name="TInnerCapture">The type of the captured value that will be passed to <paramref name="innerKeySelector"/>.</typeparam>
        /// <param name="configuredOuter">The first async-enumerable sequence to join with await configuration.</param>
        /// <param name="inner">The async-enumerable sequence to join to the first sequence.</param>
        /// <param name="outerKeySelector">An asynchronous function to extract the join key from each element of the first sequence.</param>
        /// <param name="innerCaptureValue">The extra value that will be passed to <paramref name="innerKeySelector"/>.</param>
        /// <param name="innerKeySelector">An asynchronous function to extract the join key from each element of the second sequence.</param>
        /// <returns>An async-enumerable sequence that has elements of <see cref="ValueTuple{T1, T2}"/> of each outer element and the inner elements of the same key, that are obtained by performing a grouped join on two sequences.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="outerKeySelector"/> or <paramref name="innerKeySelector"/> is null.</exception>
        public static AsyncEnumerable<(TOuter Outer, TempCollection<TInner> InnerElements)> GroupJoin<TOuter, TInner, TKey, TInnerCapture>(this in ConfiguredAsyncEnumerable<TOuter> configuredOuter,
            AsyncEnumerable<TInner> inner,
            Func<TOuter, Promise<TKey>> outerKeySelector,
            TInnerCapture innerCaptureValue,
            Func<TInnerCapture, TInner, Promise<TKey>> innerKeySelector)
            => GroupJoin(configuredOuter, inner, outerKeySelector, innerCaptureValue, innerKeySelector, EqualityComparer<TKey>.Default);

        /// <summary>
        /// Correlates the elements of two async-enumerable sequences based on equality of keys and groups the results, using the outer async-enumerable as the await configuration. The default equality comparer is used to compare keys.
        /// </summary>
        /// <typeparam name="TOuter">The type of the elements of the first async-enumerable sequence.</typeparam>
        /// <typeparam name="TInner">The type of the elements of the second async-enumerable sequence.</typeparam>
        /// <typeparam name="TKey">The type of the keys returned by the key selector functions.</typeparam>
        /// <typeparam name="TOuterCapture">The type of the captured value that will be passed to <paramref name="outerKeySelector"/>.</typeparam>
        /// <typeparam name="TInnerCapture">The type of the captured value that will be passed to <paramref name="innerKeySelector"/>.</typeparam>
        /// <param name="configuredOuter">The first async-enumerable sequence to join with await configuration.</param>
        /// <param name="inner">The async-enumerable sequence to join to the first sequence.</param>
        /// <param name="outerCaptureValue">The extra value that will be passed to <paramref name="outerKeySelector"/>.</param>
        /// <param name="outerKeySelector">An asynchronous function to extract the join key from each element of the first sequence.</param>
        /// <param name="innerCaptureValue">The extra value that will be passed to <paramref name="innerKeySelector"/>.</param>
        /// <param name="innerKeySelector">An asynchronous function to extract the join key from each element of the second sequence.</param>
        /// <returns>An async-enumerable sequence that has elements of <see cref="ValueTuple{T1, T2}"/> of each outer element and the inner elements of the same key, that are obtained by performing a grouped join on two sequences.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="outerKeySelector"/> or <paramref name="innerKeySelector"/> is null.</exception>
        public static AsyncEnumerable<(TOuter Outer, TempCollection<TInner> InnerElements)> GroupJoin<TOuter, TInner, TKey, TOuterCapture, TInnerCapture>(this in ConfiguredAsyncEnumerable<TOuter> configuredOuter,
            AsyncEnumerable<TInner> inner,
            TOuterCapture outerCaptureValue,
            Func<TOuterCapture, TOuter, Promise<TKey>> outerKeySelector,
            TInnerCapture innerCaptureValue,
            Func<TInnerCapture, TInner, Promise<TKey>> innerKeySelector)
            => GroupJoin(configuredOuter, inner, outerCaptureValue, outerKeySelector, innerCaptureValue, innerKeySelector, EqualityComparer<TKey>.Default);

        /// <summary>
        /// Correlates the elements of two async-enumerable sequences based on equality of keys and groups the results, using the outer async-enumerable as the await configuration.
        /// </summary>
        /// <typeparam name="TOuter">The type of the elements of the first async-enumerable sequence.</typeparam>
        /// <typeparam name="TInner">The type of the elements of the second async-enumerable sequence.</typeparam>
        /// <typeparam name="TKey">The type of the keys returned by the key selector functions.</typeparam>
        /// <typeparam name="TEqualityComparer">The type of the comparer.</typeparam>
        /// <param name="configuredOuter">The first async-enumerable sequence to join with await configuration.</param>
        /// <param name="inner">The async-enumerable sequence to join to the first sequence.</param>
        /// <param name="outerKeySelector">An asynchronous function to extract the join key from each element of the first sequence.</param>
        /// <param name="innerKeySelector">An asynchronous function to extract the join key from each element of the second sequence.</param>
        /// <param name="comparer">A comparer to compare keys.</param>
        /// <returns>An async-enumerable sequence that has elements of <see cref="ValueTuple{T1, T2}"/> of each outer element and the inner elements of the same key, that are obtained by performing a grouped join on two sequences.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="outerKeySelector"/> or <paramref name="innerKeySelector"/> or <paramref name="comparer"/> is null.</exception>
        public static AsyncEnumerable<(TOuter Outer, TempCollection<TInner> InnerElements)> GroupJoin<TOuter, TInner, TKey, TEqualityComparer>(this in ConfiguredAsyncEnumerable<TOuter> configuredOuter,
            AsyncEnumerable<TInner> inner,
            Func<TOuter, Promise<TKey>> outerKeySelector,
            Func<TInner, Promise<TKey>> innerKeySelector,
            TEqualityComparer comparer)
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            ValidateArgument(outerKeySelector, nameof(outerKeySelector), 1);
            ValidateArgument(innerKeySelector, nameof(innerKeySelector), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.GroupJoinHelper<TKey>.GroupJoinAwait(configuredOuter.GetAsyncEnumerator(),
                inner.GetAsyncEnumerator(),
                Internal.PromiseRefBase.DelegateWrapper.Create(outerKeySelector),
                Internal.PromiseRefBase.DelegateWrapper.Create(innerKeySelector),
                comparer);
        }

        /// <summary>
        /// Correlates the elements of two async-enumerable sequences based on equality of keys and groups the results, using the outer async-enumerable as the await configuration.
        /// </summary>
        /// <typeparam name="TOuter">The type of the elements of the first async-enumerable sequence.</typeparam>
        /// <typeparam name="TInner">The type of the elements of the second async-enumerable sequence.</typeparam>
        /// <typeparam name="TKey">The type of the keys returned by the key selector functions.</typeparam>
        /// <typeparam name="TOuterCapture">The type of the captured value that will be passed to <paramref name="outerKeySelector"/>.</typeparam>
        /// <typeparam name="TEqualityComparer">The type of the comparer.</typeparam>
        /// <param name="configuredOuter">The first async-enumerable sequence to join with await configuration.</param>
        /// <param name="inner">The async-enumerable sequence to join to the first sequence.</param>
        /// <param name="outerCaptureValue">The extra value that will be passed to <paramref name="outerKeySelector"/>.</param>
        /// <param name="outerKeySelector">An asynchronous function to extract the join key from each element of the first sequence.</param>
        /// <param name="innerKeySelector">An asynchronous function to extract the join key from each element of the second sequence.</param>
        /// <param name="comparer">A comparer to compare keys.</param>
        /// <returns>An async-enumerable sequence that has elements of <see cref="ValueTuple{T1, T2}"/> of each outer element and the inner elements of the same key, that are obtained by performing a grouped join on two sequences.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="outerKeySelector"/> or <paramref name="innerKeySelector"/> is null.</exception>
        public static AsyncEnumerable<(TOuter Outer, TempCollection<TInner> InnerElements)> GroupJoin<TOuter, TInner, TKey, TOuterCapture, TEqualityComparer>(this in ConfiguredAsyncEnumerable<TOuter> configuredOuter,
            AsyncEnumerable<TInner> inner,
            TOuterCapture outerCaptureValue,
            Func<TOuterCapture, TOuter, Promise<TKey>> outerKeySelector,
            Func<TInner, Promise<TKey>> innerKeySelector,
            TEqualityComparer comparer)
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            ValidateArgument(outerKeySelector, nameof(outerKeySelector), 1);
            ValidateArgument(innerKeySelector, nameof(innerKeySelector), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.GroupJoinHelper<TKey>.GroupJoinAwait(configuredOuter.GetAsyncEnumerator(),
                inner.GetAsyncEnumerator(),
                Internal.PromiseRefBase.DelegateWrapper.Create(outerCaptureValue, outerKeySelector),
                Internal.PromiseRefBase.DelegateWrapper.Create(innerKeySelector),
                comparer);
        }

        /// <summary>
        /// Correlates the elements of two async-enumerable sequences based on equality of keys and groups the results, using the outer async-enumerable as the await configuration.
        /// </summary>
        /// <typeparam name="TOuter">The type of the elements of the first async-enumerable sequence.</typeparam>
        /// <typeparam name="TInner">The type of the elements of the second async-enumerable sequence.</typeparam>
        /// <typeparam name="TKey">The type of the keys returned by the key selector functions.</typeparam>
        /// <typeparam name="TInnerCapture">The type of the captured value that will be passed to <paramref name="innerKeySelector"/>.</typeparam>
        /// <typeparam name="TEqualityComparer">The type of the comparer.</typeparam>
        /// <param name="configuredOuter">The first async-enumerable sequence to join with await configuration.</param>
        /// <param name="inner">The async-enumerable sequence to join to the first sequence.</param>
        /// <param name="outerKeySelector">An asynchronous function to extract the join key from each element of the first sequence.</param>
        /// <param name="innerCaptureValue">The extra value that will be passed to <paramref name="innerKeySelector"/>.</param>
        /// <param name="innerKeySelector">An asynchronous function to extract the join key from each element of the second sequence.</param>
        /// <param name="comparer">A comparer to compare keys.</param>
        /// <returns>An async-enumerable sequence that has elements of <see cref="ValueTuple{T1, T2}"/> of each outer element and the inner elements of the same key, that are obtained by performing a grouped join on two sequences.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="outerKeySelector"/> or <paramref name="innerKeySelector"/> is null.</exception>
        public static AsyncEnumerable<(TOuter Outer, TempCollection<TInner> InnerElements)> GroupJoin<TOuter, TInner, TKey, TInnerCapture, TEqualityComparer>(this in ConfiguredAsyncEnumerable<TOuter> configuredOuter,
            AsyncEnumerable<TInner> inner,
            Func<TOuter, Promise<TKey>> outerKeySelector,
            TInnerCapture innerCaptureValue,
            Func<TInnerCapture, TInner, Promise<TKey>> innerKeySelector,
            TEqualityComparer comparer)
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            ValidateArgument(outerKeySelector, nameof(outerKeySelector), 1);
            ValidateArgument(innerKeySelector, nameof(innerKeySelector), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.GroupJoinHelper<TKey>.GroupJoinAwait(configuredOuter.GetAsyncEnumerator(),
                inner.GetAsyncEnumerator(),
                Internal.PromiseRefBase.DelegateWrapper.Create(outerKeySelector),
                Internal.PromiseRefBase.DelegateWrapper.Create(innerCaptureValue, innerKeySelector),
                comparer);
        }

        /// <summary>
        /// Correlates the elements of two async-enumerable sequences based on equality of keys and groups the results, using the outer async-enumerable as the await configuration.
        /// </summary>
        /// <typeparam name="TOuter">The type of the elements of the first async-enumerable sequence.</typeparam>
        /// <typeparam name="TInner">The type of the elements of the second async-enumerable sequence.</typeparam>
        /// <typeparam name="TKey">The type of the keys returned by the key selector functions.</typeparam>
        /// <typeparam name="TOuterCapture">The type of the captured value that will be passed to <paramref name="outerKeySelector"/>.</typeparam>
        /// <typeparam name="TInnerCapture">The type of the captured value that will be passed to <paramref name="innerKeySelector"/>.</typeparam>
        /// <typeparam name="TEqualityComparer">The type of the comparer.</typeparam>
        /// <param name="configuredOuter">The first async-enumerable sequence to join with await configuration.</param>
        /// <param name="inner">The async-enumerable sequence to join to the first sequence.</param>
        /// <param name="outerCaptureValue">The extra value that will be passed to <paramref name="outerKeySelector"/>.</param>
        /// <param name="outerKeySelector">An asynchronous function to extract the join key from each element of the first sequence.</param>
        /// <param name="innerCaptureValue">The extra value that will be passed to <paramref name="innerKeySelector"/>.</param>
        /// <param name="innerKeySelector">An asynchronous function to extract the join key from each element of the second sequence.</param>
        /// <param name="comparer">A comparer to compare keys.</param>
        /// <returns>An async-enumerable sequence that has elements of <see cref="ValueTuple{T1, T2}"/> of each outer element and the inner elements of the same key, that are obtained by performing a grouped join on two sequences.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="outerKeySelector"/> or <paramref name="innerKeySelector"/> is null.</exception>
        public static AsyncEnumerable<(TOuter Outer, TempCollection<TInner> InnerElements)> GroupJoin<TOuter, TInner, TKey, TOuterCapture, TInnerCapture, TEqualityComparer>(this in ConfiguredAsyncEnumerable<TOuter> configuredOuter,
            AsyncEnumerable<TInner> inner,
            TOuterCapture outerCaptureValue,
            Func<TOuterCapture, TOuter, Promise<TKey>> outerKeySelector,
            TInnerCapture innerCaptureValue,
            Func<TInnerCapture, TInner, Promise<TKey>> innerKeySelector,
            TEqualityComparer comparer)
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            ValidateArgument(outerKeySelector, nameof(outerKeySelector), 1);
            ValidateArgument(innerKeySelector, nameof(innerKeySelector), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.GroupJoinHelper<TKey>.GroupJoinAwait(configuredOuter.GetAsyncEnumerator(),
                inner.GetAsyncEnumerator(),
                Internal.PromiseRefBase.DelegateWrapper.Create(outerCaptureValue, outerKeySelector),
                Internal.PromiseRefBase.DelegateWrapper.Create(innerCaptureValue, innerKeySelector),
                comparer);
        }
        #endregion ConfiguredAsyncEnumerable
    }
#endif // CSHARP_7_3_OR_NEWER
}