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
#if CSHARP_7_3_OR_NEWER
    partial class AsyncEnumerable
    {
        /// <summary>
        /// Produces the set union of two async-enumerable sequences by using the default equality comparer to compare values.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of the input sequences.</typeparam>
        /// <param name="first">An async-enumerable sequence whose distinct elements form the first set for the union.</param>
        /// <param name="second">An async-enumerable sequence whose distinct elements form the second set for the union.</param>
        /// <returns>An async-enumerable sequence that contains the elements from both input sequences, excluding duplicates.</returns>
        public static AsyncEnumerable<TSource> Union<TSource>(this AsyncEnumerable<TSource> first, AsyncEnumerable<TSource> second)
            => Union(first, second, EqualityComparer<TSource>.Default);

        /// <summary>
        /// Produces the set union of two async-enumerable sequences by using the specified equality comparer to compare values.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of the input sequences.</typeparam>
        /// <typeparam name="TEqualityComparer">The type of the <paramref name="comparer"/>.</typeparam>
        /// <param name="first">An async-enumerable sequence whose distinct elements form the first set for the union.</param>
        /// <param name="second">An async-enumerable sequence whose distinct elements form the second set for the union.</param>
        /// <param name="comparer">An equality comparer to compare values.</param>
        /// <returns>An async-enumerable sequence that contains the elements from both input sequences, excluding duplicates.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="comparer"/> is null.</exception>
        public static AsyncEnumerable<TSource> Union<TSource, TEqualityComparer>(this AsyncEnumerable<TSource> first, AsyncEnumerable<TSource> second, TEqualityComparer comparer)
            where TEqualityComparer : IEqualityComparer<TSource>
        {
            ValidateArgument(comparer, nameof(comparer), 1);

            // If the TEqualityComparer is a reference type, we force it to use IEqualityComparer<TSource> instead of the concrete type for future type checks.
            // This check gets eliminated by the JIT.
            return null == default(TEqualityComparer)
                ? UnionImpl<TSource, IEqualityComparer<TSource>>(first, second, comparer)
                : UnionImpl(first, second, comparer);
        }
        
        private static AsyncEnumerable<TSource> UnionImpl<TSource, TEqualityComparer>(this AsyncEnumerable<TSource> first, AsyncEnumerable<TSource> second, TEqualityComparer comparer)
            where TEqualityComparer : IEqualityComparer<TSource>
        {
            // If the first or second were already unioned with the same comparer, we flatten the unions instead of calling into them recursively.
            if (first._target is Internal.UnionAsyncEnumerableBase<TSource, TEqualityComparer> unionEnumerable1
                && EqualityComparer<TEqualityComparer>.Default.Equals(comparer, unionEnumerable1._comparer))
            {
                return unionEnumerable1.Union(first._id, second);
            }
            if (second._target is Internal.UnionAsyncEnumerableBase<TSource, TEqualityComparer> unionEnumerable2
                && EqualityComparer<TEqualityComparer>.Default.Equals(comparer, unionEnumerable2._comparer))
            {
                return unionEnumerable2.Union(second._id, first, comparer);
            }
            var enumerable = Internal.Union2AsyncEnumerable<TSource, TEqualityComparer>.GetOrCreate(first.GetAsyncEnumerator(), second.GetAsyncEnumerator(), comparer);
            return new AsyncEnumerable<TSource>(enumerable, enumerable._id);
        }

        /// <summary>
        /// Produces the set union of two async-enumerable sequences by using the default equality comparer to compare values.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of the input sequences.</typeparam>
        /// <param name="configuredFirst">A configured async-enumerable sequence whose distinct elements that also appear in <paramref name="second"/> will be returned.</param>
        /// <param name="second">An async-enumerable sequence whose distinct elements form the second set for the union.</param>
        /// <returns>An async-enumerable sequence that contains the elements from both input sequences, excluding duplicates.</returns>
        public static AsyncEnumerable<TSource> Union<TSource>(this in ConfiguredAsyncEnumerable<TSource> configuredFirst, AsyncEnumerable<TSource> second)
            => Union(configuredFirst, second, EqualityComparer<TSource>.Default);

        /// <summary>
        /// Produces the set union of two async-enumerable sequences by using the specified equality comparer to compare values.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of the input sequences.</typeparam>
        /// <typeparam name="TEqualityComparer">The type of the <paramref name="comparer"/>.</typeparam>
        /// <param name="configuredFirst">A configured async-enumerable sequence whose distinct elements that also appear in <paramref name="second"/> will be returned.</param>
        /// <param name="second">An async-enumerable sequence whose distinct elements form the second set for the union.</param>
        /// <param name="comparer">An equality comparer to compare values.</param>
        /// <returns>An async-enumerable sequence that contains the elements from both input sequences, excluding duplicates.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="comparer"/> is null.</exception>
        public static AsyncEnumerable<TSource> Union<TSource, TEqualityComparer>(this in ConfiguredAsyncEnumerable<TSource> configuredFirst, AsyncEnumerable<TSource> second, TEqualityComparer comparer)
            where TEqualityComparer : IEqualityComparer<TSource>
        {
            ValidateArgument(comparer, nameof(comparer), 1);

            // If the TEqualityComparer is a reference type, we force it to use IEqualityComparer<TSource> instead of the concrete type for future type checks.
            // This check gets eliminated by the JIT.
            return null == default(TEqualityComparer)
                ? UnionImpl<TSource, IEqualityComparer<TSource>>(configuredFirst, second, comparer)
                : UnionImpl(configuredFirst, second, comparer);
        }

        private static AsyncEnumerable<TSource> UnionImpl<TSource, TEqualityComparer>(in ConfiguredAsyncEnumerable<TSource> configuredFirst, AsyncEnumerable<TSource> second, TEqualityComparer comparer)
            where TEqualityComparer : IEqualityComparer<TSource>
        {
            // We can't flatten configured sequences, so we just use the naive union.
            var enumerable = Internal.ConfiguredUnion2AsyncEnumerable<TSource, TEqualityComparer>.GetOrCreate(configuredFirst.GetAsyncEnumerator(), second.GetAsyncEnumerator(), comparer);
            return new AsyncEnumerable<TSource>(enumerable, enumerable._id);
        }

        /// <summary>
        /// Produces the set union of two async-enumerable sequences by using the default equality comparer to compare values.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of the input sequences.</typeparam>
        /// <typeparam name="TKey">The type of key to identify elements by.</typeparam>
        /// <param name="first">An async-enumerable sequence whose distinct elements form the first set for the union.</param>
        /// <param name="second">An async-enumerable sequence whose distinct elements form the second set for the union.</param>
        /// <param name="keySelector">A function to extract the key for each element.</param>
        /// <returns>An async-enumerable sequence that contains the elements from both input sequences, excluding duplicates.</returns>
        public static AsyncEnumerable<TSource> UnionBy<TSource, TKey>(this AsyncEnumerable<TSource> first, AsyncEnumerable<TSource> second, Func<TSource, TKey> keySelector)
            => UnionBy(first, second, keySelector, EqualityComparer<TKey>.Default);

        /// <summary>
        /// Produces the set union of two async-enumerable sequences by using the specified equality comparer to compare values.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of the input sequences.</typeparam>
        /// <typeparam name="TKey">The type of key to identify elements by.</typeparam>
        /// <typeparam name="TEqualityComparer">The type of the <paramref name="comparer"/>.</typeparam>
        /// <param name="first">An async-enumerable sequence whose distinct elements form the first set for the union.</param>
        /// <param name="second">An async-enumerable sequence whose distinct elements form the second set for the union.</param>
        /// <param name="keySelector">A function to extract the key for each element.</param>
        /// <param name="comparer">An equality comparer to compare values.</param>
        /// <returns>An async-enumerable sequence that contains the elements from both input sequences, excluding duplicates.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="comparer"/> is null.</exception>
        public static AsyncEnumerable<TSource> UnionBy<TSource, TKey, TEqualityComparer>(this AsyncEnumerable<TSource> first, AsyncEnumerable<TSource> second, Func<TSource, TKey> keySelector, TEqualityComparer comparer)
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.UnionByHelper<TKey>.UnionBy(first.GetAsyncEnumerator(), second.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(keySelector), comparer);
        }

        /// <summary>
        /// Produces the set union of two async-enumerable sequences by using the default equality comparer to compare values.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of the input sequences.</typeparam>
        /// <typeparam name="TKey">The type of key to identify elements by.</typeparam>
        /// <typeparam name="TCapture">The type of the <paramref name="captureValue"/>.</typeparam>
        /// <param name="first">An async-enumerable sequence whose distinct elements form the first set for the union.</param>
        /// <param name="second">An async-enumerable sequence whose distinct elements form the second set for the union.</param>
        /// <param name="captureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">A function to extract the key for each element.</param>
        /// <returns>An async-enumerable sequence that contains the elements from both input sequences, excluding duplicates.</returns>
        public static AsyncEnumerable<TSource> UnionBy<TSource, TKey, TCapture>(this AsyncEnumerable<TSource> first, AsyncEnumerable<TSource> second, TCapture captureValue, Func<TCapture, TSource, TKey> keySelector)
            => UnionBy(first, second, captureValue, keySelector, EqualityComparer<TKey>.Default);

        /// <summary>
        /// Produces the set union of two async-enumerable sequences by using the specified equality comparer to compare values.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of the input sequences.</typeparam>
        /// <typeparam name="TKey">The type of key to identify elements by.</typeparam>
        /// <typeparam name="TCapture">The type of the <paramref name="captureValue"/>.</typeparam>
        /// <typeparam name="TEqualityComparer">The type of the <paramref name="comparer"/>.</typeparam>
        /// <param name="first">An async-enumerable sequence whose distinct elements form the first set for the union.</param>
        /// <param name="second">An async-enumerable sequence whose distinct elements form the second set for the union.</param>
        /// <param name="captureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">A function to extract the key for each element.</param>
        /// <param name="comparer">An equality comparer to compare values.</param>
        /// <returns>An async-enumerable sequence that contains the elements from both input sequences, excluding duplicates.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="comparer"/> is null.</exception>
        public static AsyncEnumerable<TSource> UnionBy<TSource, TKey, TCapture, TEqualityComparer>(this AsyncEnumerable<TSource> first, AsyncEnumerable<TSource> second, TCapture captureValue, Func<TCapture, TSource, TKey> keySelector, TEqualityComparer comparer)
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.UnionByHelper<TKey>.UnionBy(first.GetAsyncEnumerator(), second.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(captureValue, keySelector), comparer);
        }

        /// <summary>
        /// Produces the set union of two async-enumerable sequences by using the default equality comparer to compare values.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of the input sequences.</typeparam>
        /// <typeparam name="TKey">The type of key to identify elements by.</typeparam>
        /// <param name="first">An async-enumerable sequence whose distinct elements form the first set for the union.</param>
        /// <param name="second">An async-enumerable sequence whose distinct elements form the second set for the union.</param>
        /// <param name="keySelector">An async function to extract the key for each element.</param>
        /// <returns>An async-enumerable sequence that contains the elements from both input sequences, excluding duplicates.</returns>
        public static AsyncEnumerable<TSource> UnionBy<TSource, TKey>(this AsyncEnumerable<TSource> first, AsyncEnumerable<TSource> second, Func<TSource, Promise<TKey>> keySelector)
            => UnionBy(first, second, keySelector, EqualityComparer<TKey>.Default);

        /// <summary>
        /// Produces the set union of two async-enumerable sequences by using the specified equality comparer to compare values.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of the input sequences.</typeparam>
        /// <typeparam name="TKey">The type of key to identify elements by.</typeparam>
        /// <typeparam name="TEqualityComparer">The type of the <paramref name="comparer"/>.</typeparam>
        /// <param name="first">An async-enumerable sequence whose distinct elements form the first set for the union.</param>
        /// <param name="second">An async-enumerable sequence whose distinct elements form the second set for the union.</param>
        /// <param name="keySelector">An async function to extract the key for each element.</param>
        /// <param name="comparer">An equality comparer to compare values.</param>
        /// <returns>An async-enumerable sequence that contains the elements from both input sequences, excluding duplicates.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="comparer"/> is null.</exception>
        public static AsyncEnumerable<TSource> UnionBy<TSource, TKey, TEqualityComparer>(this AsyncEnumerable<TSource> first, AsyncEnumerable<TSource> second, Func<TSource, Promise<TKey>> keySelector, TEqualityComparer comparer)
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.UnionByHelper<TKey>.UnionByAwait(first.GetAsyncEnumerator(), second.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(keySelector), comparer);
        }

        /// <summary>
        /// Produces the set union of two async-enumerable sequences by using the default equality comparer to compare values.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of the input sequences.</typeparam>
        /// <typeparam name="TKey">The type of key to identify elements by.</typeparam>
        /// <typeparam name="TCapture">The type of the <paramref name="captureValue"/>.</typeparam>
        /// <param name="first">An async-enumerable sequence whose distinct elements form the first set for the union.</param>
        /// <param name="second">An async-enumerable sequence whose distinct elements form the second set for the union.</param>
        /// <param name="captureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">An async function to extract the key for each element.</param>
        /// <returns>An async-enumerable sequence that contains the elements from both input sequences, excluding duplicates.</returns>
        public static AsyncEnumerable<TSource> UnionBy<TSource, TKey, TCapture>(this AsyncEnumerable<TSource> first, AsyncEnumerable<TSource> second, TCapture captureValue, Func<TCapture, TSource, Promise<TKey>> keySelector)
            => UnionBy(first, second, captureValue, keySelector, EqualityComparer<TKey>.Default);

        /// <summary>
        /// Produces the set union of two async-enumerable sequences by using the specified equality comparer to compare values.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of the input sequences.</typeparam>
        /// <typeparam name="TKey">The type of key to identify elements by.</typeparam>
        /// <typeparam name="TCapture">The type of the <paramref name="captureValue"/>.</typeparam>
        /// <typeparam name="TEqualityComparer">The type of the <paramref name="comparer"/>.</typeparam>
        /// <param name="first">An async-enumerable sequence whose distinct elements form the first set for the union.</param>
        /// <param name="second">An async-enumerable sequence whose distinct elements form the second set for the union.</param>
        /// <param name="captureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">An async function to extract the key for each element.</param>
        /// <param name="comparer">An equality comparer to compare values.</param>
        /// <returns>An async-enumerable sequence that contains the elements from both input sequences, excluding duplicates.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="comparer"/> is null.</exception>
        public static AsyncEnumerable<TSource> UnionBy<TSource, TKey, TCapture, TEqualityComparer>(this AsyncEnumerable<TSource> first, AsyncEnumerable<TSource> second, TCapture captureValue, Func<TCapture, TSource, Promise<TKey>> keySelector, TEqualityComparer comparer)
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.UnionByHelper<TKey>.UnionByAwait(first.GetAsyncEnumerator(), second.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(captureValue, keySelector), comparer);
        }

        /// <summary>
        /// Produces the set union of two async-enumerable sequences by using the default equality comparer to compare values.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of the input sequences.</typeparam>
        /// <typeparam name="TKey">The type of key to identify elements by.</typeparam>
        /// <param name="configuredFirst">A configured async-enumerable sequence whose distinct elements form the first set for the union.</param>
        /// <param name="second">An async-enumerable sequence whose distinct elements form the second set for the union.</param>
        /// <param name="keySelector">A function to extract the key for each element.</param>
        /// <returns>An async-enumerable sequence that contains the elements from both input sequences, excluding duplicates.</returns>
        public static AsyncEnumerable<TSource> UnionBy<TSource, TKey>(this in ConfiguredAsyncEnumerable<TSource> configuredFirst, AsyncEnumerable<TSource> second, Func<TSource, TKey> keySelector)
            => UnionBy(configuredFirst, second, keySelector, EqualityComparer<TKey>.Default);

        /// <summary>
        /// Produces the set union of two async-enumerable sequences by using the specified equality comparer to compare values.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of the input sequences.</typeparam>
        /// <typeparam name="TKey">The type of key to identify elements by.</typeparam>
        /// <typeparam name="TEqualityComparer">The type of the <paramref name="comparer"/>.</typeparam>
        /// <param name="configuredFirst">A configured async-enumerable sequence whose distinct elements form the first set for the union.</param>
        /// <param name="second">An async-enumerable sequence whose distinct elements form the second set for the union.</param>
        /// <param name="keySelector">A function to extract the key for each element.</param>
        /// <param name="comparer">An equality comparer to compare values.</param>
        /// <returns>An async-enumerable sequence that contains the elements from both input sequences, excluding duplicates.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="comparer"/> is null.</exception>
        public static AsyncEnumerable<TSource> UnionBy<TSource, TKey, TEqualityComparer>(this in ConfiguredAsyncEnumerable<TSource> configuredFirst, AsyncEnumerable<TSource> second, Func<TSource, TKey> keySelector, TEqualityComparer comparer)
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.UnionByHelper<TKey>.UnionBy(configuredFirst.GetAsyncEnumerator(), second.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(keySelector), comparer);
        }

        /// <summary>
        /// Produces the set union of two async-enumerable sequences by using the default equality comparer to compare values.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of the input sequences.</typeparam>
        /// <typeparam name="TKey">The type of key to identify elements by.</typeparam>
        /// <typeparam name="TCapture">The type of the <paramref name="captureValue"/>.</typeparam>
        /// <param name="configuredFirst">A configured async-enumerable sequence whose distinct elements form the first set for the union.</param>
        /// <param name="second">An async-enumerable sequence whose distinct elements form the second set for the union.</param>
        /// <param name="captureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">A function to extract the key for each element.</param>
        /// <returns>An async-enumerable sequence that contains the elements from both input sequences, excluding duplicates.</returns>
        public static AsyncEnumerable<TSource> UnionBy<TSource, TKey, TCapture>(this in ConfiguredAsyncEnumerable<TSource> configuredFirst, AsyncEnumerable<TSource> second, TCapture captureValue, Func<TCapture, TSource, TKey> keySelector)
            => UnionBy(configuredFirst, second, captureValue, keySelector, EqualityComparer<TKey>.Default);

        /// <summary>
        /// Produces the set union of two async-enumerable sequences by using the specified equality comparer to compare values.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of the input sequences.</typeparam>
        /// <typeparam name="TKey">The type of key to identify elements by.</typeparam>
        /// <typeparam name="TCapture">The type of the <paramref name="captureValue"/>.</typeparam>
        /// <typeparam name="TEqualityComparer">The type of the <paramref name="comparer"/>.</typeparam>
        /// <param name="configuredFirst">A configured async-enumerable sequence whose distinct elements form the first set for the union.</param>
        /// <param name="second">An async-enumerable sequence whose distinct elements form the second set for the union.</param>
        /// <param name="captureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">A function to extract the key for each element.</param>
        /// <param name="comparer">An equality comparer to compare values.</param>
        /// <returns>An async-enumerable sequence that contains the elements from both input sequences, excluding duplicates.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="comparer"/> is null.</exception>
        public static AsyncEnumerable<TSource> UnionBy<TSource, TKey, TCapture, TEqualityComparer>(this in ConfiguredAsyncEnumerable<TSource> configuredFirst, AsyncEnumerable<TSource> second, TCapture captureValue, Func<TCapture, TSource, TKey> keySelector, TEqualityComparer comparer)
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.UnionByHelper<TKey>.UnionBy(configuredFirst.GetAsyncEnumerator(), second.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(captureValue, keySelector), comparer);
        }

        /// <summary>
        /// Produces the set union of two async-enumerable sequences by using the default equality comparer to compare values.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of the input sequences.</typeparam>
        /// <typeparam name="TKey">The type of key to identify elements by.</typeparam>
        /// <param name="configuredFirst">A configured async-enumerable sequence whose distinct elements form the first set for the union.</param>
        /// <param name="second">An async-enumerable sequence whose distinct elements form the second set for the union.</param>
        /// <param name="keySelector">An async function to extract the key for each element.</param>
        /// <returns>An async-enumerable sequence that contains the elements from both input sequences, excluding duplicates.</returns>
        public static AsyncEnumerable<TSource> UnionBy<TSource, TKey>(this in ConfiguredAsyncEnumerable<TSource> configuredFirst, AsyncEnumerable<TSource> second, Func<TSource, Promise<TKey>> keySelector)
            => UnionBy(configuredFirst, second, keySelector, EqualityComparer<TKey>.Default);

        /// <summary>
        /// Produces the set union of two async-enumerable sequences by using the specified equality comparer to compare values.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of the input sequences.</typeparam>
        /// <typeparam name="TKey">The type of key to identify elements by.</typeparam>
        /// <typeparam name="TEqualityComparer">The type of the <paramref name="comparer"/>.</typeparam>
        /// <param name="configuredFirst">A configured async-enumerable sequence whose distinct elements form the first set for the union.</param>
        /// <param name="second">An async-enumerable sequence whose distinct elements form the second set for the union.</param>
        /// <param name="keySelector">An async function to extract the key for each element.</param>
        /// <param name="comparer">An equality comparer to compare values.</param>
        /// <returns>An async-enumerable sequence that contains the elements from both input sequences, excluding duplicates.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="comparer"/> is null.</exception>
        public static AsyncEnumerable<TSource> UnionBy<TSource, TKey, TEqualityComparer>(this in ConfiguredAsyncEnumerable<TSource> configuredFirst, AsyncEnumerable<TSource> second, Func<TSource, Promise<TKey>> keySelector, TEqualityComparer comparer)
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.UnionByHelper<TKey>.UnionByAwait(configuredFirst.GetAsyncEnumerator(), second.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(keySelector), comparer);
        }

        /// <summary>
        /// Produces the set union of two async-enumerable sequences by using the default equality comparer to compare values.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of the input sequences.</typeparam>
        /// <typeparam name="TKey">The type of key to identify elements by.</typeparam>
        /// <typeparam name="TCapture">The type of the <paramref name="captureValue"/>.</typeparam>
        /// <param name="configuredFirst">A configured async-enumerable sequence whose distinct elements form the first set for the union.</param>
        /// <param name="second">An async-enumerable sequence whose distinct elements form the second set for the union.</param>
        /// <param name="captureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">An async function to extract the key for each element.</param>
        /// <returns>An async-enumerable sequence that contains the elements from both input sequences, excluding duplicates.</returns>
        public static AsyncEnumerable<TSource> UnionBy<TSource, TKey, TCapture>(this in ConfiguredAsyncEnumerable<TSource> configuredFirst, AsyncEnumerable<TSource> second, TCapture captureValue, Func<TCapture, TSource, Promise<TKey>> keySelector)
            => UnionBy(configuredFirst, second, captureValue, keySelector, EqualityComparer<TKey>.Default);

        /// <summary>
        /// Produces the set union of two async-enumerable sequences by using the specified equality comparer to compare values.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of the input sequences.</typeparam>
        /// <typeparam name="TKey">The type of key to identify elements by.</typeparam>
        /// <typeparam name="TCapture">The type of the <paramref name="captureValue"/>.</typeparam>
        /// <typeparam name="TEqualityComparer">The type of the <paramref name="comparer"/>.</typeparam>
        /// <param name="configuredFirst">A configured async-enumerable sequence whose distinct elements form the first set for the union.</param>
        /// <param name="second">An async-enumerable sequence whose distinct elements form the second set for the union.</param>
        /// <param name="captureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">An async function to extract the key for each element.</param>
        /// <param name="comparer">An equality comparer to compare values.</param>
        /// <returns>An async-enumerable sequence that contains the elements from both input sequences, excluding duplicates.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="comparer"/> is null.</exception>
        public static AsyncEnumerable<TSource> UnionBy<TSource, TKey, TCapture, TEqualityComparer>(this in ConfiguredAsyncEnumerable<TSource> configuredFirst, AsyncEnumerable<TSource> second, TCapture captureValue, Func<TCapture, TSource, Promise<TKey>> keySelector, TEqualityComparer comparer)
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.UnionByHelper<TKey>.UnionByAwait(configuredFirst.GetAsyncEnumerator(), second.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(captureValue, keySelector), comparer);
        }
    }
#endif // CSHARP_7_3_OR_NEWER
}