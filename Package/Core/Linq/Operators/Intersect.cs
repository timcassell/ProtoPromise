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
        /// Produces the set intersection of two async-enumerable sequences by using the default equality comparer to compare values.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of the input sequences.</typeparam>
        /// <param name="first">An async-enumerable sequence whose distinct elements that also appear in <paramref name="second"/> will be returned.</param>
        /// <param name="second">An async-enumerable sequence whose distinct elements that also appear in <paramref name="first"/> will be returned.</param>
        /// <returns>An async-enumerable sequence that contains the set intersection of the elements of two async-enumerable sequences.</returns>
        public static AsyncEnumerable<TSource> Intersect<TSource>(this AsyncEnumerable<TSource> first, AsyncEnumerable<TSource> second)
            => Intersect(first, second, EqualityComparer<TSource>.Default);

        /// <summary>
        /// Produces the set intersection of two async-enumerable sequences by using the specified equality comparer to compare values.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of the input sequences.</typeparam>
        /// <typeparam name="TEqualityComparer">The type of the <paramref name="comparer"/>.</typeparam>
        /// <param name="first">An async-enumerable sequence whose distinct elements that also appear in <paramref name="second"/> will be returned.</param>
        /// <param name="second">An async-enumerable sequence whose distinct elements that also appear in <paramref name="first"/> will be returned.</param>
        /// <param name="comparer">An equality comparer to compare values.</param>
        /// <returns>An async-enumerable sequence that contains the set intersection of the elements of two async-enumerable sequences.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="comparer"/> is null.</exception>
        public static AsyncEnumerable<TSource> Intersect<TSource, TEqualityComparer>(this AsyncEnumerable<TSource> first, AsyncEnumerable<TSource> second, TEqualityComparer comparer)
            where TEqualityComparer : IEqualityComparer<TSource>
        {
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.IntersectHelper.Intersect(first.GetAsyncEnumerator(), second.GetAsyncEnumerator(), comparer);
        }

        /// <summary>
        /// Produces the set intersection of two async-enumerable sequences by using the default equality comparer to compare values.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of the input sequences.</typeparam>
        /// <param name="configuredFirst">A configured async-enumerable sequence whose distinct elements that also appear in <paramref name="second"/> will be returned.</param>
        /// <param name="second">An async-enumerable sequence whose distinct elements that also appear in <paramref name="configuredFirst"/> will be returned.</param>
        /// <returns>An async-enumerable sequence that contains the set intersection of the elements of two async-enumerable sequences.</returns>
        public static AsyncEnumerable<TSource> Intersect<TSource>(this in ConfiguredAsyncEnumerable<TSource> configuredFirst, AsyncEnumerable<TSource> second)
            => Intersect(configuredFirst, second, EqualityComparer<TSource>.Default);

        /// <summary>
        /// Produces the set intersection of two async-enumerable sequences by using the specified equality comparer to compare values.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of the input sequences.</typeparam>
        /// <typeparam name="TEqualityComparer">The type of the <paramref name="comparer"/>.</typeparam>
        /// <param name="configuredFirst">A configured async-enumerable sequence whose distinct elements that also appear in <paramref name="second"/> will be returned.</param>
        /// <param name="second">An async-enumerable sequence whose distinct elements that also appear in <paramref name="configuredFirst"/> will be returned.</param>
        /// <param name="comparer">An equality comparer to compare values.</param>
        /// <returns>An async-enumerable sequence that contains the set intersection of the elements of two async-enumerable sequences.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="comparer"/> is null.</exception>
        public static AsyncEnumerable<TSource> Intersect<TSource, TEqualityComparer>(this in ConfiguredAsyncEnumerable<TSource> configuredFirst, AsyncEnumerable<TSource> second, TEqualityComparer comparer)
            where TEqualityComparer : IEqualityComparer<TSource>
        {
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.IntersectHelper.Intersect(configuredFirst.GetAsyncEnumerator(), second.GetAsyncEnumerator(), comparer);
        }

        /// <summary>
        /// Produces the set intersection of two async-enumerable sequences by using the default equality comparer to compare values.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of the input sequences.</typeparam>
        /// <typeparam name="TKey">The type of key to identify elements by.</typeparam>
        /// <param name="first">An async-enumerable sequence whose distinct elements that also appear in <paramref name="second"/> will be returned.</param>
        /// <param name="second">An async-enumerable sequence whose distinct elements that also appear in <paramref name="first"/> will be returned.</param>
        /// <param name="keySelector">A function to extract the key for each element.</param>
        /// <returns>An async-enumerable sequence that contains the set intersection of the elements of two async-enumerable sequences.</returns>
        public static AsyncEnumerable<TSource> IntersectBy<TSource, TKey>(this AsyncEnumerable<TSource> first, AsyncEnumerable<TSource> second, Func<TSource, TKey> keySelector)
            => IntersectBy(first, second, keySelector, EqualityComparer<TKey>.Default);

        /// <summary>
        /// Produces the set intersection of two async-enumerable sequences by using the specified equality comparer to compare values.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of the input sequences.</typeparam>
        /// <typeparam name="TKey">The type of key to identify elements by.</typeparam>
        /// <typeparam name="TEqualityComparer">The type of the <paramref name="comparer"/>.</typeparam>
        /// <param name="first">An async-enumerable sequence whose distinct elements that also appear in <paramref name="second"/> will be returned.</param>
        /// <param name="second">An async-enumerable sequence whose distinct elements that also appear in <paramref name="first"/> will be returned.</param>
        /// <param name="keySelector">A function to extract the key for each element.</param>
        /// <param name="comparer">An equality comparer to compare values.</param>
        /// <returns>An async-enumerable sequence that contains the set intersection of the elements of two async-enumerable sequences.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="comparer"/> is null.</exception>
        public static AsyncEnumerable<TSource> IntersectBy<TSource, TKey, TEqualityComparer>(this AsyncEnumerable<TSource> first, AsyncEnumerable<TSource> second, Func<TSource, TKey> keySelector, TEqualityComparer comparer)
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.IntersectByHelper<TKey>.IntersectBy(first.GetAsyncEnumerator(), second.GetAsyncEnumerator(), DelegateWrapper.Create(keySelector), comparer);
        }

        /// <summary>
        /// Produces the set intersection of two async-enumerable sequences by using the default equality comparer to compare values.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of the input sequences.</typeparam>
        /// <typeparam name="TKey">The type of key to identify elements by.</typeparam>
        /// <typeparam name="TCapture">The type of the <paramref name="captureValue"/>.</typeparam>
        /// <param name="first">An async-enumerable sequence whose distinct elements that also appear in <paramref name="second"/> will be returned.</param>
        /// <param name="second">An async-enumerable sequence whose distinct elements that also appear in <paramref name="first"/> will be returned.</param>
        /// <param name="captureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">A function to extract the key for each element.</param>
        /// <returns>An async-enumerable sequence that contains the set intersection of the elements of two async-enumerable sequences.</returns>
        public static AsyncEnumerable<TSource> IntersectBy<TSource, TKey, TCapture>(this AsyncEnumerable<TSource> first, AsyncEnumerable<TSource> second, TCapture captureValue, Func<TCapture, TSource, TKey> keySelector)
            => IntersectBy(first, second, captureValue, keySelector, EqualityComparer<TKey>.Default);

        /// <summary>
        /// Produces the set intersection of two async-enumerable sequences by using the specified equality comparer to compare values.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of the input sequences.</typeparam>
        /// <typeparam name="TKey">The type of key to identify elements by.</typeparam>
        /// <typeparam name="TCapture">The type of the <paramref name="captureValue"/>.</typeparam>
        /// <typeparam name="TEqualityComparer">The type of the <paramref name="comparer"/>.</typeparam>
        /// <param name="first">An async-enumerable sequence whose distinct elements that also appear in <paramref name="second"/> will be returned.</param>
        /// <param name="second">An async-enumerable sequence whose distinct elements that also appear in <paramref name="first"/> will be returned.</param>
        /// <param name="captureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">A function to extract the key for each element.</param>
        /// <param name="comparer">An equality comparer to compare values.</param>
        /// <returns>An async-enumerable sequence that contains the set intersection of the elements of two async-enumerable sequences.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="comparer"/> is null.</exception>
        public static AsyncEnumerable<TSource> IntersectBy<TSource, TKey, TCapture, TEqualityComparer>(this AsyncEnumerable<TSource> first, AsyncEnumerable<TSource> second, TCapture captureValue, Func<TCapture, TSource, TKey> keySelector, TEqualityComparer comparer)
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.IntersectByHelper<TKey>.IntersectBy(first.GetAsyncEnumerator(), second.GetAsyncEnumerator(), DelegateWrapper.Create(captureValue, keySelector), comparer);
        }

        /// <summary>
        /// Produces the set intersection of two async-enumerable sequences by using the default equality comparer to compare values.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of the input sequences.</typeparam>
        /// <typeparam name="TKey">The type of key to identify elements by.</typeparam>
        /// <param name="first">An async-enumerable sequence whose distinct elements that also appear in <paramref name="second"/> will be returned.</param>
        /// <param name="second">An async-enumerable sequence whose distinct elements that also appear in <paramref name="first"/> will be returned.</param>
        /// <param name="keySelector">An async function to extract the key for each element.</param>
        /// <returns>An async-enumerable sequence that contains the set intersection of the elements of two async-enumerable sequences.</returns>
        public static AsyncEnumerable<TSource> IntersectBy<TSource, TKey>(this AsyncEnumerable<TSource> first, AsyncEnumerable<TSource> second, Func<TSource, Promise<TKey>> keySelector)
            => IntersectBy(first, second, keySelector, EqualityComparer<TKey>.Default);

        /// <summary>
        /// Produces the set intersection of two async-enumerable sequences by using the specified equality comparer to compare values.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of the input sequences.</typeparam>
        /// <typeparam name="TKey">The type of key to identify elements by.</typeparam>
        /// <typeparam name="TEqualityComparer">The type of the <paramref name="comparer"/>.</typeparam>
        /// <param name="first">An async-enumerable sequence whose distinct elements that also appear in <paramref name="second"/> will be returned.</param>
        /// <param name="second">An async-enumerable sequence whose distinct elements that also appear in <paramref name="first"/> will be returned.</param>
        /// <param name="keySelector">An async function to extract the key for each element.</param>
        /// <param name="comparer">An equality comparer to compare values.</param>
        /// <returns>An async-enumerable sequence that contains the set intersection of the elements of two async-enumerable sequences.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="comparer"/> is null.</exception>
        public static AsyncEnumerable<TSource> IntersectBy<TSource, TKey, TEqualityComparer>(this AsyncEnumerable<TSource> first, AsyncEnumerable<TSource> second, Func<TSource, Promise<TKey>> keySelector, TEqualityComparer comparer)
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.IntersectByHelper<TKey>.IntersectBy(first.GetAsyncEnumerator(), second.GetAsyncEnumerator(), DelegateWrapper.Create(keySelector), comparer);
        }

        /// <summary>
        /// Produces the set intersection of two async-enumerable sequences by using the default equality comparer to compare values.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of the input sequences.</typeparam>
        /// <typeparam name="TKey">The type of key to identify elements by.</typeparam>
        /// <typeparam name="TCapture">The type of the <paramref name="captureValue"/>.</typeparam>
        /// <param name="first">An async-enumerable sequence whose distinct elements that also appear in <paramref name="second"/> will be returned.</param>
        /// <param name="second">An async-enumerable sequence whose distinct elements that also appear in <paramref name="first"/> will be returned.</param>
        /// <param name="captureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">An async function to extract the key for each element.</param>
        /// <returns>An async-enumerable sequence that contains the set intersection of the elements of two async-enumerable sequences.</returns>
        public static AsyncEnumerable<TSource> IntersectBy<TSource, TKey, TCapture>(this AsyncEnumerable<TSource> first, AsyncEnumerable<TSource> second, TCapture captureValue, Func<TCapture, TSource, Promise<TKey>> keySelector)
            => IntersectBy(first, second, captureValue, keySelector, EqualityComparer<TKey>.Default);

        /// <summary>
        /// Produces the set intersection of two async-enumerable sequences by using the specified equality comparer to compare values.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of the input sequences.</typeparam>
        /// <typeparam name="TKey">The type of key to identify elements by.</typeparam>
        /// <typeparam name="TCapture">The type of the <paramref name="captureValue"/>.</typeparam>
        /// <typeparam name="TEqualityComparer">The type of the <paramref name="comparer"/>.</typeparam>
        /// <param name="first">An async-enumerable sequence whose distinct elements that also appear in <paramref name="second"/> will be returned.</param>
        /// <param name="second">An async-enumerable sequence whose distinct elements that also appear in <paramref name="first"/> will be returned.</param>
        /// <param name="captureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">An async function to extract the key for each element.</param>
        /// <param name="comparer">An equality comparer to compare values.</param>
        /// <returns>An async-enumerable sequence that contains the set intersection of the elements of two async-enumerable sequences.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="comparer"/> is null.</exception>
        public static AsyncEnumerable<TSource> IntersectBy<TSource, TKey, TCapture, TEqualityComparer>(this AsyncEnumerable<TSource> first, AsyncEnumerable<TSource> second, TCapture captureValue, Func<TCapture, TSource, Promise<TKey>> keySelector, TEqualityComparer comparer)
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.IntersectByHelper<TKey>.IntersectBy(first.GetAsyncEnumerator(), second.GetAsyncEnumerator(), DelegateWrapper.Create(captureValue, keySelector), comparer);
        }

        /// <summary>
        /// Produces the set intersection of two async-enumerable sequences by using the default equality comparer to compare values.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of the input sequences.</typeparam>
        /// <typeparam name="TKey">The type of key to identify elements by.</typeparam>
        /// <param name="configuredFirst">A configured async-enumerable sequence whose distinct elements that also appear in second will be returned.</param>
        /// <param name="second">An async-enumerable sequence whose distinct elements that also appear in <paramref name="configuredFirst"/> will be returned.</param>
        /// <param name="keySelector">A function to extract the key for each element.</param>
        /// <returns>An async-enumerable sequence that contains the set intersection of the elements of two async-enumerable sequences.</returns>
        public static AsyncEnumerable<TSource> IntersectBy<TSource, TKey>(this in ConfiguredAsyncEnumerable<TSource> configuredFirst, AsyncEnumerable<TSource> second, Func<TSource, TKey> keySelector)
            => IntersectBy(configuredFirst, second, keySelector, EqualityComparer<TKey>.Default);

        /// <summary>
        /// Produces the set intersection of two async-enumerable sequences by using the specified equality comparer to compare values.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of the input sequences.</typeparam>
        /// <typeparam name="TKey">The type of key to identify elements by.</typeparam>
        /// <typeparam name="TEqualityComparer">The type of the <paramref name="comparer"/>.</typeparam>
        /// <param name="configuredFirst">A configured async-enumerable sequence whose distinct elements that also appear in second will be returned.</param>
        /// <param name="second">An async-enumerable sequence whose distinct elements that also appear in <paramref name="configuredFirst"/> will be returned.</param>
        /// <param name="keySelector">A function to extract the key for each element.</param>
        /// <param name="comparer">An equality comparer to compare values.</param>
        /// <returns>An async-enumerable sequence that contains the set intersection of the elements of two async-enumerable sequences.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="comparer"/> is null.</exception>
        public static AsyncEnumerable<TSource> IntersectBy<TSource, TKey, TEqualityComparer>(this in ConfiguredAsyncEnumerable<TSource> configuredFirst, AsyncEnumerable<TSource> second, Func<TSource, TKey> keySelector, TEqualityComparer comparer)
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.IntersectByHelper<TKey>.IntersectBy(configuredFirst.GetAsyncEnumerator(), second.GetAsyncEnumerator(), DelegateWrapper.Create(keySelector), comparer);
        }

        /// <summary>
        /// Produces the set intersection of two async-enumerable sequences by using the default equality comparer to compare values.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of the input sequences.</typeparam>
        /// <typeparam name="TKey">The type of key to identify elements by.</typeparam>
        /// <typeparam name="TCapture">The type of the <paramref name="captureValue"/>.</typeparam>
        /// <param name="configuredFirst">A configured async-enumerable sequence whose distinct elements that also appear in second will be returned.</param>
        /// <param name="second">An async-enumerable sequence whose distinct elements that also appear in <paramref name="configuredFirst"/> will be returned.</param>
        /// <param name="captureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">A function to extract the key for each element.</param>
        /// <returns>An async-enumerable sequence that contains the set intersection of the elements of two async-enumerable sequences.</returns>
        public static AsyncEnumerable<TSource> IntersectBy<TSource, TKey, TCapture>(this in ConfiguredAsyncEnumerable<TSource> configuredFirst, AsyncEnumerable<TSource> second, TCapture captureValue, Func<TCapture, TSource, TKey> keySelector)
            => IntersectBy(configuredFirst, second, captureValue, keySelector, EqualityComparer<TKey>.Default);

        /// <summary>
        /// Produces the set intersection of two async-enumerable sequences by using the specified equality comparer to compare values.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of the input sequences.</typeparam>
        /// <typeparam name="TKey">The type of key to identify elements by.</typeparam>
        /// <typeparam name="TCapture">The type of the <paramref name="captureValue"/>.</typeparam>
        /// <typeparam name="TEqualityComparer">The type of the <paramref name="comparer"/>.</typeparam>
        /// <param name="configuredFirst">A configured async-enumerable sequence whose distinct elements that also appear in second will be returned.</param>
        /// <param name="second">An async-enumerable sequence whose distinct elements that also appear in <paramref name="configuredFirst"/> will be returned.</param>
        /// <param name="captureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">A function to extract the key for each element.</param>
        /// <param name="comparer">An equality comparer to compare values.</param>
        /// <returns>An async-enumerable sequence that contains the set intersection of the elements of two async-enumerable sequences.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="comparer"/> is null.</exception>
        public static AsyncEnumerable<TSource> IntersectBy<TSource, TKey, TCapture, TEqualityComparer>(this in ConfiguredAsyncEnumerable<TSource> configuredFirst, AsyncEnumerable<TSource> second, TCapture captureValue, Func<TCapture, TSource, TKey> keySelector, TEqualityComparer comparer)
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.IntersectByHelper<TKey>.IntersectBy(configuredFirst.GetAsyncEnumerator(), second.GetAsyncEnumerator(), DelegateWrapper.Create(captureValue, keySelector), comparer);
        }

        /// <summary>
        /// Produces the set intersection of two async-enumerable sequences by using the default equality comparer to compare values.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of the input sequences.</typeparam>
        /// <typeparam name="TKey">The type of key to identify elements by.</typeparam>
        /// <param name="configuredFirst">A configured async-enumerable sequence whose distinct elements that also appear in second will be returned.</param>
        /// <param name="second">An async-enumerable sequence whose distinct elements that also appear in <paramref name="configuredFirst"/> will be returned.</param>
        /// <param name="keySelector">An async function to extract the key for each element.</param>
        /// <returns>An async-enumerable sequence that contains the set intersection of the elements of two async-enumerable sequences.</returns>
        public static AsyncEnumerable<TSource> IntersectBy<TSource, TKey>(this in ConfiguredAsyncEnumerable<TSource> configuredFirst, AsyncEnumerable<TSource> second, Func<TSource, Promise<TKey>> keySelector)
            => IntersectBy(configuredFirst, second, keySelector, EqualityComparer<TKey>.Default);

        /// <summary>
        /// Produces the set intersection of two async-enumerable sequences by using the specified equality comparer to compare values.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of the input sequences.</typeparam>
        /// <typeparam name="TKey">The type of key to identify elements by.</typeparam>
        /// <typeparam name="TEqualityComparer">The type of the <paramref name="comparer"/>.</typeparam>
        /// <param name="configuredFirst">A configured async-enumerable sequence whose distinct elements that also appear in second will be returned.</param>
        /// <param name="second">An async-enumerable sequence whose distinct elements that also appear in <paramref name="configuredFirst"/> will be returned.</param>
        /// <param name="keySelector">An async function to extract the key for each element.</param>
        /// <param name="comparer">An equality comparer to compare values.</param>
        /// <returns>An async-enumerable sequence that contains the set intersection of the elements of two async-enumerable sequences.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="comparer"/> is null.</exception>
        public static AsyncEnumerable<TSource> IntersectBy<TSource, TKey, TEqualityComparer>(this in ConfiguredAsyncEnumerable<TSource> configuredFirst, AsyncEnumerable<TSource> second, Func<TSource, Promise<TKey>> keySelector, TEqualityComparer comparer)
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.IntersectByHelper<TKey>.IntersectBy(configuredFirst.GetAsyncEnumerator(), second.GetAsyncEnumerator(), DelegateWrapper.Create(keySelector), comparer);
        }

        /// <summary>
        /// Produces the set intersection of two async-enumerable sequences by using the default equality comparer to compare values.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of the input sequences.</typeparam>
        /// <typeparam name="TKey">The type of key to identify elements by.</typeparam>
        /// <typeparam name="TCapture">The type of the <paramref name="captureValue"/>.</typeparam>
        /// <param name="configuredFirst">A configured async-enumerable sequence whose distinct elements that also appear in second will be returned.</param>
        /// <param name="second">An async-enumerable sequence whose distinct elements that also appear in <paramref name="configuredFirst"/> will be returned.</param>
        /// <param name="captureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">An async function to extract the key for each element.</param>
        /// <returns>An async-enumerable sequence that contains the set intersection of the elements of two async-enumerable sequences.</returns>
        public static AsyncEnumerable<TSource> IntersectBy<TSource, TKey, TCapture>(this in ConfiguredAsyncEnumerable<TSource> configuredFirst, AsyncEnumerable<TSource> second, TCapture captureValue, Func<TCapture, TSource, Promise<TKey>> keySelector)
            => IntersectBy(configuredFirst, second, captureValue, keySelector, EqualityComparer<TKey>.Default);

        /// <summary>
        /// Produces the set intersection of two async-enumerable sequences by using the specified equality comparer to compare values.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of the input sequences.</typeparam>
        /// <typeparam name="TKey">The type of key to identify elements by.</typeparam>
        /// <typeparam name="TCapture">The type of the <paramref name="captureValue"/>.</typeparam>
        /// <typeparam name="TEqualityComparer">The type of the <paramref name="comparer"/>.</typeparam>
        /// <param name="configuredFirst">A configured async-enumerable sequence whose distinct elements that also appear in second will be returned.</param>
        /// <param name="second">An async-enumerable sequence whose distinct elements that also appear in <paramref name="configuredFirst"/> will be returned.</param>
        /// <param name="captureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">An async function to extract the key for each element.</param>
        /// <param name="comparer">An equality comparer to compare values.</param>
        /// <returns>An async-enumerable sequence that contains the set intersection of the elements of two async-enumerable sequences.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="comparer"/> is null.</exception>
        public static AsyncEnumerable<TSource> IntersectBy<TSource, TKey, TCapture, TEqualityComparer>(this in ConfiguredAsyncEnumerable<TSource> configuredFirst, AsyncEnumerable<TSource> second, TCapture captureValue, Func<TCapture, TSource, Promise<TKey>> keySelector, TEqualityComparer comparer)
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.IntersectByHelper<TKey>.IntersectBy(configuredFirst.GetAsyncEnumerator(), second.GetAsyncEnumerator(), DelegateWrapper.Create(captureValue, keySelector), comparer);
        }
    }
}