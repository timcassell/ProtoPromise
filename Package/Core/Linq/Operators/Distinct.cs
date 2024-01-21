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
        /// Returns an async-enumerable sequence that contains only distinct elements by using the default equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">The sequence to remove duplicate elements from.</param>
        /// <returns>An <see cref="AsyncEnumerable{T}"/> that contains distinct elements from the source sequence.</returns>
        public static AsyncEnumerable<TSource> Distinct<TSource>(this AsyncEnumerable<TSource> source)
            => Distinct(source, EqualityComparer<TSource>.Default);

        /// <summary>
        /// Returns an async-enumerable sequence that contains only distinct elements by using the specified equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TEqualityComparer">The type of the <paramref name="comparer"/>.</typeparam>
        /// <param name="source">The sequence to remove duplicate elements from.</param>
        /// <param name="comparer">An <see cref="IEqualityComparer{T}"/> to compare values.</param>
        /// <returns>An <see cref="AsyncEnumerable{T}"/> that contains distinct elements from the source sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="comparer"/> is null.</exception>
        public static AsyncEnumerable<TSource> Distinct<TSource, TEqualityComparer>(this AsyncEnumerable<TSource> source, TEqualityComparer comparer)
            where TEqualityComparer : IEqualityComparer<TSource>
        {
            ValidateArgument(comparer, nameof(comparer), 1);

            return AsyncEnumerable<TSource>.Create((asyncEnumerator: source.GetAsyncEnumerator(), comparer), async (cv, writer, cancelationToken) =>
            {
                // The enumerator was retrieved without a cancelation token when the original function was called.
                // We need to propagate the token that was passed in, so we assign it before starting iteration.
                cv.asyncEnumerator._target._cancelationToken = cancelationToken;

                try
                {
                    if (!await cv.asyncEnumerator.MoveNextAsync())
                    {
                        // Empty source.
                        return;
                    }

                    using (var set = new Internal.Set<TSource, TEqualityComparer>(cv.comparer))
                    {
                        var current = cv.asyncEnumerator.Current;
                        set.Add(current);
                        await writer.YieldAsync(current);
                        
                        while (await cv.asyncEnumerator.MoveNextAsync())
                        {
                            current = cv.asyncEnumerator.Current;
                            if (set.Add(current))
                            {
                                await writer.YieldAsync(current);
                            }
                        }
                    }
                }
                finally
                {
                    await cv.asyncEnumerator.DisposeAsync();
                }
            });
        }

        /// <summary>
        /// Returns an async-enumerable sequence that contains only distinct elements by using the specified equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TEqualityComparer">The type of the <paramref name="comparer"/>.</typeparam>
        /// <param name="source">The configured async-enumerable sequence to remove duplicate elements from.</param>
        /// <param name="comparer">An <see cref="IEqualityComparer{T}"/> to compare values.</param>
        /// <returns>An <see cref="AsyncEnumerable{T}"/> that contains distinct elements from the source sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="comparer"/> is null.</exception>
        public static AsyncEnumerable<TSource> Distinct<TSource, TEqualityComparer>(this ConfiguredAsyncEnumerable<TSource> source, TEqualityComparer comparer)
            where TEqualityComparer : IEqualityComparer<TSource>
        {
            ValidateArgument(comparer, nameof(comparer), 1);

            return AsyncEnumerable<TSource>.Create((configuredAsyncEnumerator: source.GetAsyncEnumerator(), comparer), async (cv, writer, cancelationToken) =>
            {
                // The enumerator may have been configured with a cancelation token. We need to join the passed in token before starting iteration.
                var enumerableRef = cv.configuredAsyncEnumerator._enumerator._target;
                var joinedCancelationSource = Internal.MaybeJoinCancelationTokens(enumerableRef._cancelationToken, cancelationToken, out enumerableRef._cancelationToken);
                
                try
                {
                    if (!await cv.configuredAsyncEnumerator.MoveNextAsync())
                    {
                        // Empty source.
                        return;
                    }

                    using (var set = new Internal.Set<TSource, TEqualityComparer>(cv.comparer))
                    {
                        var current = cv.configuredAsyncEnumerator.Current;
                        set.Add(current);
                        await writer.YieldAsync(current);

                        while (await cv.configuredAsyncEnumerator.MoveNextAsync())
                        {
                            current = cv.configuredAsyncEnumerator.Current;
                            if (set.Add(current))
                            {
                                await writer.YieldAsync(current);
                            }
                        }
                    }
                }
                finally
                {
                    joinedCancelationSource.TryDispose();
                    await cv.configuredAsyncEnumerator.DisposeAsync();
                }
            });
        }

        /// <summary>
        /// Returns an async-enumerable sequence that contains only distinct elements according to a specified key selector function by using the default equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TKey">The type of key to distinguish elements by.</typeparam>
        /// <param name="source">The sequence to remove duplicate elements from.</param>
        /// <param name="keySelector">A function to extract the key for each element.</param>
        /// <returns>An <see cref="AsyncEnumerable{T}"/> that contains distinct elements from the source sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        public static AsyncEnumerable<TSource> DistinctBy<TSource, TKey>(this AsyncEnumerable<TSource> source, Func<TSource, TKey> keySelector)
            => DistinctBy(source, keySelector, EqualityComparer<TKey>.Default);

        /// <summary>
        /// Returns an async-enumerable sequence that contains only distinct elements according to a specified key selector function by using the specified equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TKey">The type of key to distinguish elements by.</typeparam>
        /// <typeparam name="TEqualityComparer">The type of the <paramref name="comparer"/>.</typeparam>
        /// <param name="source">The sequence to remove duplicate elements from.</param>
        /// <param name="keySelector">A function to extract the key for each element.</param>
        /// <param name="comparer">An <see cref="IEqualityComparer{T}"/> to compare values.</param>
        /// <returns>An <see cref="AsyncEnumerable{T}"/> that contains distinct elements from the source sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="comparer"/> is null.</exception>
        public static AsyncEnumerable<TSource> DistinctBy<TSource, TKey, TEqualityComparer>(this AsyncEnumerable<TSource> source, Func<TSource, TKey> keySelector, TEqualityComparer comparer)
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return DistinctByHelper<TSource, TKey>.DistinctBy(source.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(keySelector), comparer);
        }

        /// <summary>
        /// Returns an async-enumerable sequence that contains only distinct elements according to a specified key selector function by using the default equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TKey">The type of key to distinguish elements by.</typeparam>
        /// <typeparam name="TCapture">The type of the <paramref name="captureValue"/>.</typeparam>
        /// <param name="source">The sequence to remove duplicate elements from.</param>
        /// <param name="captureValue">The extra value that will be passed to the <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">A function to extract the key for each element.</param>
        /// <returns>An <see cref="AsyncEnumerable{T}"/> that contains distinct elements from the source sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        public static AsyncEnumerable<TSource> DistinctBy<TSource, TKey, TCapture>(this AsyncEnumerable<TSource> source, TCapture captureValue, Func<TCapture, TSource, TKey> keySelector)
            => DistinctBy(source, captureValue, keySelector, EqualityComparer<TKey>.Default);

        /// <summary>
        /// Returns an async-enumerable sequence that contains only distinct elements according to a specified key selector function by using the specified equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TKey">The type of key to distinguish elements by.</typeparam>
        /// <typeparam name="TCapture">The type of the <paramref name="captureValue"/>.</typeparam>
        /// <typeparam name="TEqualityComparer">The type of the <paramref name="comparer"/>.</typeparam>
        /// <param name="source">The sequence to remove duplicate elements from.</param>
        /// <param name="keySelector">A function to extract the key for each element.</param>
        /// <param name="captureValue">The extra value that will be passed to the <paramref name="keySelector"/>.</param>
        /// <param name="comparer">An <see cref="IEqualityComparer{T}"/> to compare values.</param>
        /// <returns>An <see cref="AsyncEnumerable{T}"/> that contains distinct elements from the source sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="comparer"/> is null.</exception>
        public static AsyncEnumerable<TSource> DistinctBy<TSource, TKey, TCapture, TEqualityComparer>(this AsyncEnumerable<TSource> source, TCapture captureValue, Func<TCapture, TSource, TKey> keySelector, TEqualityComparer comparer)
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return DistinctByHelper<TSource, TKey>.DistinctBy(source.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(captureValue, keySelector), comparer);
        }

        /// <summary>
        /// Returns an async-enumerable sequence that contains only distinct elements according to a specified key selector function by using the default equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="configuredSource"/>.</typeparam>
        /// <typeparam name="TKey">The type of key to distinguish elements by.</typeparam>
        /// <param name="configuredSource">The configured async-enumerable sequence to remove duplicate elements from.</param>
        /// <param name="keySelector">A function to extract the key for each element.</param>
        /// <returns>An <see cref="AsyncEnumerable{T}"/> that contains distinct elements from the source sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        public static AsyncEnumerable<TSource> DistinctBy<TSource, TKey>(this ConfiguredAsyncEnumerable<TSource> configuredSource, Func<TSource, TKey> keySelector)
            => DistinctBy(configuredSource, keySelector, EqualityComparer<TKey>.Default);

        /// <summary>
        /// Returns an async-enumerable sequence that contains only distinct elements according to a specified key selector function by using the specified equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="configuredSource"/>.</typeparam>
        /// <typeparam name="TKey">The type of key to distinguish elements by.</typeparam>
        /// <typeparam name="TEqualityComparer">The type of the <paramref name="comparer"/>.</typeparam>
        /// <param name="configuredSource">The configured async-enumerable sequence to remove duplicate elements from.</param>
        /// <param name="keySelector">A function to extract the key for each element.</param>
        /// <param name="comparer">An <see cref="IEqualityComparer{T}"/> to compare values.</param>
        /// <returns>An <see cref="AsyncEnumerable{T}"/> that contains distinct elements from the source sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="comparer"/> is null.</exception>
        public static AsyncEnumerable<TSource> DistinctBy<TSource, TKey, TEqualityComparer>(this ConfiguredAsyncEnumerable<TSource> configuredSource, Func<TSource, TKey> keySelector, TEqualityComparer comparer)
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return DistinctByHelper<TSource, TKey>.DistinctBy(configuredSource.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(keySelector), comparer);
        }

        /// <summary>
        /// Returns an async-enumerable sequence that contains only distinct elements according to a specified key selector function by using the default equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="configuredSource"/>.</typeparam>
        /// <typeparam name="TKey">The type of key to distinguish elements by.</typeparam>
        /// <typeparam name="TCapture">The type of the <paramref name="captureValue"/>.</typeparam>
        /// <param name="configuredSource">The configured async-enumerable sequence to remove duplicate elements from.</param>
        /// <param name="captureValue">The extra value that will be passed to the <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">A function to extract the key for each element.</param>
        /// <returns>An <see cref="AsyncEnumerable{T}"/> that contains distinct elements from the source sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        public static AsyncEnumerable<TSource> DistinctBy<TSource, TKey, TCapture>(this ConfiguredAsyncEnumerable<TSource> configuredSource, TCapture captureValue, Func<TCapture, TSource, TKey> keySelector)
            => DistinctBy(configuredSource, captureValue, keySelector, EqualityComparer<TKey>.Default);

        /// <summary>
        /// Returns an async-enumerable sequence that contains only distinct elements according to a specified key selector function by using the specified equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="configuredSource"/>.</typeparam>
        /// <typeparam name="TKey">The type of key to distinguish elements by.</typeparam>
        /// <typeparam name="TCapture">The type of the <paramref name="captureValue"/>.</typeparam>
        /// <typeparam name="TEqualityComparer">The type of the <paramref name="comparer"/>.</typeparam>
        /// <param name="configuredSource">The configured async-enumerable sequence to remove duplicate elements from.</param>
        /// <param name="keySelector">A function to extract the key for each element.</param>
        /// <param name="captureValue">The extra value that will be passed to the <paramref name="keySelector"/>.</param>
        /// <param name="comparer">An <see cref="IEqualityComparer{T}"/> to compare values.</param>
        /// <returns>An <see cref="AsyncEnumerable{T}"/> that contains distinct elements from the source sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="comparer"/> is null.</exception>
        public static AsyncEnumerable<TSource> DistinctBy<TSource, TKey, TCapture, TEqualityComparer>(this ConfiguredAsyncEnumerable<TSource> configuredSource, TCapture captureValue, Func<TCapture, TSource, TKey> keySelector, TEqualityComparer comparer)
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return DistinctByHelper<TSource, TKey>.DistinctBy(configuredSource.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(captureValue, keySelector), comparer);
        }

        /// <summary>
        /// Returns an async-enumerable sequence that contains only distinct elements according to a specified key selector function by using the default equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TKey">The type of key to distinguish elements by.</typeparam>
        /// <param name="source">The sequence to remove duplicate elements from.</param>
        /// <param name="keySelector">An async function to extract the key for each element.</param>
        /// <returns>An <see cref="AsyncEnumerable{T}"/> that contains distinct elements from the source sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        public static AsyncEnumerable<TSource> DistinctBy<TSource, TKey>(this AsyncEnumerable<TSource> source, Func<TSource, Promise<TKey>> keySelector)
            => DistinctBy(source, keySelector, EqualityComparer<TKey>.Default);

        /// <summary>
        /// Returns an async-enumerable sequence that contains only distinct elements according to a specified key selector function by using the specified equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TKey">The type of key to distinguish elements by.</typeparam>
        /// <typeparam name="TEqualityComparer">The type of the <paramref name="comparer"/>.</typeparam>
        /// <param name="source">The sequence to remove duplicate elements from.</param>
        /// <param name="keySelector">An async function to extract the key for each element.</param>
        /// <param name="comparer">An <see cref="IEqualityComparer{T}"/> to compare values.</param>
        /// <returns>An <see cref="AsyncEnumerable{T}"/> that contains distinct elements from the source sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="comparer"/> is null.</exception>
        public static AsyncEnumerable<TSource> DistinctBy<TSource, TKey, TEqualityComparer>(this AsyncEnumerable<TSource> source, Func<TSource, Promise<TKey>> keySelector, TEqualityComparer comparer)
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return DistinctByHelper<TSource, TKey>.DistinctByAwait(source.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(keySelector), comparer);
        }

        /// <summary>
        /// Returns an async-enumerable sequence that contains only distinct elements according to a specified key selector function by using the default equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TKey">The type of key to distinguish elements by.</typeparam>
        /// <typeparam name="TCapture">The type of the <paramref name="captureValue"/>.</typeparam>
        /// <param name="source">The sequence to remove duplicate elements from.</param>
        /// <param name="captureValue">The extra value that will be passed to the <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">An async function to extract the key for each element.</param>
        /// <returns>An <see cref="AsyncEnumerable{T}"/> that contains distinct elements from the source sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        public static AsyncEnumerable<TSource> DistinctBy<TSource, TKey, TCapture>(this AsyncEnumerable<TSource> source, TCapture captureValue, Func<TCapture, TSource, Promise<TKey>> keySelector)
            => DistinctBy(source, captureValue, keySelector, EqualityComparer<TKey>.Default);

        /// <summary>
        /// Returns an async-enumerable sequence that contains only distinct elements according to a specified key selector function by using the specified equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TKey">The type of key to distinguish elements by.</typeparam>
        /// <typeparam name="TCapture">The type of the <paramref name="captureValue"/>.</typeparam>
        /// <typeparam name="TEqualityComparer">The type of the <paramref name="comparer"/>.</typeparam>
        /// <param name="source">The sequence to remove duplicate elements from.</param>
        /// <param name="keySelector">An async function to extract the key for each element.</param>
        /// <param name="captureValue">The extra value that will be passed to the <paramref name="keySelector"/>.</param>
        /// <param name="comparer">An <see cref="IEqualityComparer{T}"/> to compare values.</param>
        /// <returns>An <see cref="AsyncEnumerable{T}"/> that contains distinct elements from the source sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="comparer"/> is null.</exception>
        public static AsyncEnumerable<TSource> DistinctBy<TSource, TKey, TCapture, TEqualityComparer>(this AsyncEnumerable<TSource> source, TCapture captureValue, Func<TCapture, TSource, Promise<TKey>> keySelector, TEqualityComparer comparer)
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return DistinctByHelper<TSource, TKey>.DistinctByAwait(source.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(captureValue, keySelector), comparer);
        }

        /// <summary>
        /// Returns an async-enumerable sequence that contains only distinct elements according to a specified key selector function by using the default equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="configuredSource"/>.</typeparam>
        /// <typeparam name="TKey">The type of key to distinguish elements by.</typeparam>
        /// <param name="configuredSource">The configured async-enumerable sequence to remove duplicate elements from.</param>
        /// <param name="keySelector">An async function to extract the key for each element.</param>
        /// <returns>An <see cref="AsyncEnumerable{T}"/> that contains distinct elements from the source sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        public static AsyncEnumerable<TSource> DistinctBy<TSource, TKey>(this ConfiguredAsyncEnumerable<TSource> configuredSource, Func<TSource, Promise<TKey>> keySelector)
            => DistinctBy(configuredSource, keySelector, EqualityComparer<TKey>.Default);

        /// <summary>
        /// Returns an async-enumerable sequence that contains only distinct elements according to a specified key selector function by using the specified equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="configuredSource"/>.</typeparam>
        /// <typeparam name="TKey">The type of key to distinguish elements by.</typeparam>
        /// <typeparam name="TEqualityComparer">The type of the <paramref name="comparer"/>.</typeparam>
        /// <param name="configuredSource">The configured async-enumerable sequence to remove duplicate elements from.</param>
        /// <param name="keySelector">An async function to extract the key for each element.</param>
        /// <param name="comparer">An <see cref="IEqualityComparer{T}"/> to compare values.</param>
        /// <returns>An <see cref="AsyncEnumerable{T}"/> that contains distinct elements from the source sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="comparer"/> is null.</exception>
        public static AsyncEnumerable<TSource> DistinctBy<TSource, TKey, TEqualityComparer>(this ConfiguredAsyncEnumerable<TSource> configuredSource, Func<TSource, Promise<TKey>> keySelector, TEqualityComparer comparer)
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return DistinctByHelper<TSource, TKey>.DistinctByAwait(configuredSource.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(keySelector), comparer);
        }

        /// <summary>
        /// Returns an async-enumerable sequence that contains only distinct elements according to a specified key selector function by using the default equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="configuredSource"/>.</typeparam>
        /// <typeparam name="TKey">The type of key to distinguish elements by.</typeparam>
        /// <typeparam name="TCapture">The type of the <paramref name="captureValue"/>.</typeparam>
        /// <param name="configuredSource">The configured async-enumerable sequence to remove duplicate elements from.</param>
        /// <param name="captureValue">The extra value that will be passed to the <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">An async function to extract the key for each element.</param>
        /// <returns>An <see cref="AsyncEnumerable{T}"/> that contains distinct elements from the source sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        public static AsyncEnumerable<TSource> DistinctBy<TSource, TKey, TCapture>(this ConfiguredAsyncEnumerable<TSource> configuredSource, TCapture captureValue, Func<TCapture, TSource, Promise<TKey>> keySelector)
            => DistinctBy(configuredSource, captureValue, keySelector, EqualityComparer<TKey>.Default);

        /// <summary>
        /// Returns an async-enumerable sequence that contains only distinct elements according to a specified key selector function by using the specified equality comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="configuredSource"/>.</typeparam>
        /// <typeparam name="TKey">The type of key to distinguish elements by.</typeparam>
        /// <typeparam name="TCapture">The type of the <paramref name="captureValue"/>.</typeparam>
        /// <typeparam name="TEqualityComparer">The type of the <paramref name="comparer"/>.</typeparam>
        /// <param name="configuredSource">The configured async-enumerable sequence to remove duplicate elements from.</param>
        /// <param name="keySelector">An async function to extract the key for each element.</param>
        /// <param name="captureValue">The extra value that will be passed to the <paramref name="keySelector"/>.</param>
        /// <param name="comparer">An <see cref="IEqualityComparer{T}"/> to compare values.</param>
        /// <returns>An <see cref="AsyncEnumerable{T}"/> that contains distinct elements from the source sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="comparer"/> is null.</exception>
        public static AsyncEnumerable<TSource> DistinctBy<TSource, TKey, TCapture, TEqualityComparer>(this ConfiguredAsyncEnumerable<TSource> configuredSource, TCapture captureValue, Func<TCapture, TSource, Promise<TKey>> keySelector, TEqualityComparer comparer)
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return DistinctByHelper<TSource, TKey>.DistinctByAwait(configuredSource.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(captureValue, keySelector), comparer);
        }

        private static class DistinctByHelper<TSource, TKey>
        {
            internal static AsyncEnumerable<TSource> DistinctBy<TKeySelector, TEqualityComparer>(AsyncEnumerator<TSource> asyncEnumerator, TKeySelector keySelector, TEqualityComparer comparer)
                where TKeySelector : Internal.IFunc<TSource, TKey>
                where TEqualityComparer : IEqualityComparer<TKey>
                => AsyncEnumerable<TSource>.Create((asyncEnumerator, comparer), async (cv, writer, cancelationToken) =>
                {
                    // The enumerator was retrieved without a cancelation token when the original function was called.
                    // We need to propagate the token that was passed in, so we assign it before starting iteration.
                    cv.asyncEnumerator._target._cancelationToken = cancelationToken;

                    try
                    {
                        if (!await cv.asyncEnumerator.MoveNextAsync())
                        {
                            // Empty source.
                            return;
                        }

                        using (var set = new Internal.Set<TKey, TEqualityComparer>(cv.comparer))
                        {
                            var current = cv.asyncEnumerator.Current;
                            set.Add(keySelector.Invoke(current));
                            await writer.YieldAsync(current);

                            while (await cv.asyncEnumerator.MoveNextAsync())
                            {
                                current = cv.asyncEnumerator.Current;
                                if (set.Add(keySelector.Invoke(current)))
                                {
                                    await writer.YieldAsync(current);
                                }
                            }
                        }
                    }
                    finally
                    {
                        await cv.asyncEnumerator.DisposeAsync();
                    }
                });

            internal static AsyncEnumerable<TSource> DistinctBy<TKeySelector, TEqualityComparer>(ConfiguredAsyncEnumerable<TSource>.Enumerator configuredAsyncEnumerator, TKeySelector keySelector, TEqualityComparer comparer)
                where TKeySelector : Internal.IFunc<TSource, TKey>
                where TEqualityComparer : IEqualityComparer<TKey>
                => AsyncEnumerable<TSource>.Create((configuredAsyncEnumerator, comparer), async (cv, writer, cancelationToken) =>
                {
                    // The enumerator may have been configured with a cancelation token. We need to join the passed in token before starting iteration.
                    var enumerableRef = cv.configuredAsyncEnumerator._enumerator._target;
                    var joinedCancelationSource = Internal.MaybeJoinCancelationTokens(enumerableRef._cancelationToken, cancelationToken, out enumerableRef._cancelationToken);

                    try
                    {
                        if (!await cv.configuredAsyncEnumerator.MoveNextAsync())
                        {
                            // Empty source.
                            return;
                        }

                        using (var set = new Internal.Set<TKey, TEqualityComparer>(cv.comparer))
                        {
                            var current = cv.configuredAsyncEnumerator.Current;
                            set.Add(keySelector.Invoke(current));
                            await writer.YieldAsync(current);

                            while (await cv.configuredAsyncEnumerator.MoveNextAsync())
                            {
                                current = cv.configuredAsyncEnumerator.Current;
                                if (set.Add(keySelector.Invoke(current)))
                                {
                                    await writer.YieldAsync(current);
                                }
                            }
                        }
                    }
                    finally
                    {
                        joinedCancelationSource.TryDispose();
                        await cv.configuredAsyncEnumerator.DisposeAsync();
                    }
                });

            internal static AsyncEnumerable<TSource> DistinctByAwait<TKeySelector, TEqualityComparer>(AsyncEnumerator<TSource> asyncEnumerator, TKeySelector keySelector, TEqualityComparer comparer)
                where TKeySelector : Internal.IFunc<TSource, Promise<TKey>>
                where TEqualityComparer : IEqualityComparer<TKey>
                => AsyncEnumerable<TSource>.Create((asyncEnumerator, comparer), async (cv, writer, cancelationToken) =>
                {
                    // The enumerator was retrieved without a cancelation token when the original function was called.
                    // We need to propagate the token that was passed in, so we assign it before starting iteration.
                    cv.asyncEnumerator._target._cancelationToken = cancelationToken;

                    try
                    {
                        if (!await cv.asyncEnumerator.MoveNextAsync())
                        {
                            // Empty source.
                            return;
                        }

                        using (var set = new Internal.Set<TKey, TEqualityComparer>(cv.comparer))
                        {
                            var current = cv.asyncEnumerator.Current;
                            set.Add(await keySelector.Invoke(current));
                            await writer.YieldAsync(current);

                            while (await cv.asyncEnumerator.MoveNextAsync())
                            {
                                current = cv.asyncEnumerator.Current;
                                if (set.Add(await keySelector.Invoke(current)))
                                {
                                    await writer.YieldAsync(current);
                                }
                            }
                        }
                    }
                    finally
                    {
                        await cv.asyncEnumerator.DisposeAsync();
                    }
                });

            internal static AsyncEnumerable<TSource> DistinctByAwait<TKeySelector, TEqualityComparer>(ConfiguredAsyncEnumerable<TSource>.Enumerator configuredAsyncEnumerator, TKeySelector keySelector, TEqualityComparer comparer)
                where TKeySelector : Internal.IFunc<TSource, Promise<TKey>>
                where TEqualityComparer : IEqualityComparer<TKey>
                => AsyncEnumerable<TSource>.Create((configuredAsyncEnumerator, comparer), async (cv, writer, cancelationToken) =>
                {
                    // The enumerator may have been configured with a cancelation token. We need to join the passed in token before starting iteration.
                    var enumerableRef = cv.configuredAsyncEnumerator._enumerator._target;
                    var joinedCancelationSource = Internal.MaybeJoinCancelationTokens(enumerableRef._cancelationToken, cancelationToken, out enumerableRef._cancelationToken);

                    try
                    {
                        if (!await cv.configuredAsyncEnumerator.MoveNextAsync())
                        {
                            // Empty source.
                            return;
                        }

                        using (var set = new Internal.Set<TKey, TEqualityComparer>(cv.comparer))
                        {
                            var current = cv.configuredAsyncEnumerator.Current;
                            set.Add(await keySelector.Invoke(current));
                            await writer.YieldAsync(current);

                            while (await cv.configuredAsyncEnumerator.MoveNextAsync())
                            {
                                current = cv.configuredAsyncEnumerator.Current;
                                if (set.Add(await keySelector.Invoke(current)))
                                {
                                    await writer.YieldAsync(current);
                                }
                            }
                        }
                    }
                    finally
                    {
                        joinedCancelationSource.TryDispose();
                        await cv.configuredAsyncEnumerator.DisposeAsync();
                    }
                });
        }
    }
#endif // CSHARP_7_3_OR_NEWER
}