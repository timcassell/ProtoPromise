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

            return AsyncEnumerable<TSource>.Create((firstAsyncEnumerator: first.GetAsyncEnumerator(), secondAsyncEnumerator: second.GetAsyncEnumerator(), comparer), async (cv, writer, cancelationToken) =>
            {
                // The enumerators were retrieved without a cancelation token when the original function was called.
                // We need to propagate the token that was passed in, so we assign it before starting iteration.
                cv.firstAsyncEnumerator._target._cancelationToken = cancelationToken;
                cv.secondAsyncEnumerator._target._cancelationToken = cancelationToken;
                try
                {
                    using (var set = new Internal.PoolBackedSet<TSource, TEqualityComparer>(cv.comparer))
                    {
                        while (await cv.secondAsyncEnumerator.MoveNextAsync())
                        {
                            set.Add(cv.secondAsyncEnumerator.Current);
                        }

                        while (await cv.firstAsyncEnumerator.MoveNextAsync())
                        {
                            var element = cv.firstAsyncEnumerator.Current;
                            if (set.Remove(element))
                            {
                                await writer.YieldAsync(element);
                            }
                        }
                    }

                    // We yield and wait for the enumerator to be disposed, but only if there were no exceptions.
                    await writer.YieldAsync(default).ForLinqExtension();
                }
                finally
                {
                    try
                    {
                        await cv.secondAsyncEnumerator.DisposeAsync();
                    }
                    finally
                    {
                        await cv.firstAsyncEnumerator.DisposeAsync();
                    }
                }
            });
        }

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

            return AsyncEnumerable<TSource>.Create((firstAsyncEnumerator: configuredFirst.GetAsyncEnumerator(), secondAsyncEnumerator: second.GetAsyncEnumerator(), comparer), async (cv, writer, cancelationToken) =>
            {
                // The enumerator may have been configured with a cancelation token. We need to join the passed in token before starting iteration.
                var enumerableRef = cv.firstAsyncEnumerator._enumerator._target;
                var joinedCancelationSource = Internal.MaybeJoinCancelationTokens(enumerableRef._cancelationToken, cancelationToken, out enumerableRef._cancelationToken);
                // Use the same cancelation token for both enumerators.
                cv.secondAsyncEnumerator._target._cancelationToken = enumerableRef._cancelationToken;

                try
                {
                    using (var set = new Internal.PoolBackedSet<TSource, TEqualityComparer>(cv.comparer))
                    {
                        while (await cv.secondAsyncEnumerator.MoveNextAsync())
                        {
                            // We need to make sure we're on the configured context before invoking the comparer.
                            await cv.firstAsyncEnumerator.SwitchToContext();
                            set.Add(cv.secondAsyncEnumerator.Current);
                        }

                        while (await cv.firstAsyncEnumerator.MoveNextAsync())
                        {
                            var element = cv.firstAsyncEnumerator.Current;
                            if (set.Remove(element))
                            {
                                await writer.YieldAsync(element);
                            }
                        }
                    }

                    // We yield and wait for the enumerator to be disposed, but only if there were no exceptions.
                    await writer.YieldAsync(default).ForLinqExtension();
                }
                finally
                {
                    try
                    {
                        await cv.secondAsyncEnumerator.DisposeAsync();
                    }
                    finally
                    {
                        await cv.firstAsyncEnumerator.DisposeAsync();
                    }
                }
            });
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

            return IntersectByHelper<TKey>.IntersectBy(first.GetAsyncEnumerator(), second.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(keySelector), comparer);
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

            return IntersectByHelper<TKey>.IntersectBy(first.GetAsyncEnumerator(), second.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(captureValue, keySelector), comparer);
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

            return IntersectByHelper<TKey>.IntersectByAwait(first.GetAsyncEnumerator(), second.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(keySelector), comparer);
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

            return IntersectByHelper<TKey>.IntersectByAwait(first.GetAsyncEnumerator(), second.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(captureValue, keySelector), comparer);
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

            return IntersectByHelper<TKey>.IntersectBy(configuredFirst.GetAsyncEnumerator(), second.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(keySelector), comparer);
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

            return IntersectByHelper<TKey>.IntersectBy(configuredFirst.GetAsyncEnumerator(), second.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(captureValue, keySelector), comparer);
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

            return IntersectByHelper<TKey>.IntersectByAwait(configuredFirst.GetAsyncEnumerator(), second.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(keySelector), comparer);
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

            return IntersectByHelper<TKey>.IntersectByAwait(configuredFirst.GetAsyncEnumerator(), second.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(captureValue, keySelector), comparer);
        }

        private static class IntersectByHelper<TKey>
        {
            internal static AsyncEnumerable<TSource> IntersectBy<TSource, TKeySelector, TEqualityComparer>(
                AsyncEnumerator<TSource> firstAsyncEnumerator, AsyncEnumerator<TSource> secondAsyncEnumerator, TKeySelector keySelector, TEqualityComparer comparer)
                where TKeySelector : Internal.IFunc<TSource, TKey>
                where TEqualityComparer : IEqualityComparer<TKey>
                => AsyncEnumerable<TSource>.Create((firstAsyncEnumerator, secondAsyncEnumerator, keySelector, comparer), async (cv, writer, cancelationToken) =>
                {
                    // The enumerators were retrieved without a cancelation token when the original function was called.
                    // We need to propagate the token that was passed in, so we assign it before starting iteration.
                    cv.firstAsyncEnumerator._target._cancelationToken = cancelationToken;
                    cv.secondAsyncEnumerator._target._cancelationToken = cancelationToken;
                    try
                    {
                        using (var set = new Internal.PoolBackedSet<TKey, TEqualityComparer>(cv.comparer))
                        {
                            while (await cv.secondAsyncEnumerator.MoveNextAsync())
                            {
                                set.Add(cv.keySelector.Invoke(cv.secondAsyncEnumerator.Current));
                            }

                            while (await cv.firstAsyncEnumerator.MoveNextAsync())
                            {
                                var element = cv.firstAsyncEnumerator.Current;
                                if (set.Remove(cv.keySelector.Invoke(element)))
                                {
                                    await writer.YieldAsync(element);
                                }
                            }
                        }

                        // We yield and wait for the enumerator to be disposed, but only if there were no exceptions.
                        await writer.YieldAsync(default).ForLinqExtension();
                    }
                    finally
                    {
                        try
                        {
                            await cv.secondAsyncEnumerator.DisposeAsync();
                        }
                        finally
                        {
                            await cv.firstAsyncEnumerator.DisposeAsync();
                        }
                    }
                });

            internal static AsyncEnumerable<TSource> IntersectByAwait<TSource, TKeySelector, TEqualityComparer>(
                AsyncEnumerator<TSource> firstAsyncEnumerator, AsyncEnumerator<TSource> secondAsyncEnumerator, TKeySelector keySelector, TEqualityComparer comparer)
                where TKeySelector : Internal.IFunc<TSource, Promise<TKey>>
                where TEqualityComparer : IEqualityComparer<TKey>
                => AsyncEnumerable<TSource>.Create((firstAsyncEnumerator, secondAsyncEnumerator, keySelector, comparer), async (cv, writer, cancelationToken) =>
                {
                    // The enumerators were retrieved without a cancelation token when the original function was called.
                    // We need to propagate the token that was passed in, so we assign it before starting iteration.
                    cv.firstAsyncEnumerator._target._cancelationToken = cancelationToken;
                    cv.secondAsyncEnumerator._target._cancelationToken = cancelationToken;
                    try
                    {
                        using (var set = new Internal.PoolBackedSet<TKey, TEqualityComparer>(cv.comparer))
                        {
                            while (await cv.secondAsyncEnumerator.MoveNextAsync())
                            {
                                set.Add(await cv.keySelector.Invoke(cv.secondAsyncEnumerator.Current));
                            }

                            while (await cv.firstAsyncEnumerator.MoveNextAsync())
                            {
                                var element = cv.firstAsyncEnumerator.Current;
                                if (set.Remove(await cv.keySelector.Invoke(element)))
                                {
                                    await writer.YieldAsync(element);
                                }
                            }
                        }

                        // We yield and wait for the enumerator to be disposed, but only if there were no exceptions.
                        await writer.YieldAsync(default).ForLinqExtension();
                    }
                    finally
                    {
                        try
                        {
                            await cv.secondAsyncEnumerator.DisposeAsync();
                        }
                        finally
                        {
                            await cv.firstAsyncEnumerator.DisposeAsync();
                        }
                    }
                });

            internal static AsyncEnumerable<TSource> IntersectBy<TSource, TKeySelector, TEqualityComparer>(
                ConfiguredAsyncEnumerable<TSource>.Enumerator firstAsyncEnumerator, AsyncEnumerator<TSource> secondAsyncEnumerator, TKeySelector keySelector, TEqualityComparer comparer)
                where TKeySelector : Internal.IFunc<TSource, TKey>
                where TEqualityComparer : IEqualityComparer<TKey>
                => AsyncEnumerable<TSource>.Create((firstAsyncEnumerator, secondAsyncEnumerator, keySelector, comparer), async (cv, writer, cancelationToken) =>
                {
                    // The enumerator may have been configured with a cancelation token. We need to join the passed in token before starting iteration.
                    var enumerableRef = cv.firstAsyncEnumerator._enumerator._target;
                    var joinedCancelationSource = Internal.MaybeJoinCancelationTokens(enumerableRef._cancelationToken, cancelationToken, out enumerableRef._cancelationToken);
                    // Use the same cancelation token for both enumerators.
                    cv.secondAsyncEnumerator._target._cancelationToken = enumerableRef._cancelationToken;

                    try
                    {
                        using (var set = new Internal.PoolBackedSet<TKey, TEqualityComparer>(cv.comparer))
                        {
                            while (await cv.secondAsyncEnumerator.MoveNextAsync())
                            {
                                // We need to make sure we're on the configured context before invoking the key selector.
                                await cv.firstAsyncEnumerator.SwitchToContext();
                                set.Add(cv.keySelector.Invoke(cv.secondAsyncEnumerator.Current));
                            }

                            while (await cv.firstAsyncEnumerator.MoveNextAsync())
                            {
                                var element = cv.firstAsyncEnumerator.Current;
                                if (set.Remove(cv.keySelector.Invoke(element)))
                                {
                                    await writer.YieldAsync(element);
                                }
                            }
                        }

                        // We yield and wait for the enumerator to be disposed, but only if there were no exceptions.
                        await writer.YieldAsync(default).ForLinqExtension();
                    }
                    finally
                    {
                        try
                        {
                            await cv.secondAsyncEnumerator.DisposeAsync();
                        }
                        finally
                        {
                            await cv.firstAsyncEnumerator.DisposeAsync();
                        }
                    }
                });

            internal static AsyncEnumerable<TSource> IntersectByAwait<TSource, TKeySelector, TEqualityComparer>(
                ConfiguredAsyncEnumerable<TSource>.Enumerator firstAsyncEnumerator, AsyncEnumerator<TSource> secondAsyncEnumerator, TKeySelector keySelector, TEqualityComparer comparer)
                where TKeySelector : Internal.IFunc<TSource, Promise<TKey>>
                where TEqualityComparer : IEqualityComparer<TKey>
                => AsyncEnumerable<TSource>.Create((firstAsyncEnumerator, secondAsyncEnumerator, keySelector, comparer), async (cv, writer, cancelationToken) =>
                {
                    // The enumerator may have been configured with a cancelation token. We need to join the passed in token before starting iteration.
                    var enumerableRef = cv.firstAsyncEnumerator._enumerator._target;
                    var joinedCancelationSource = Internal.MaybeJoinCancelationTokens(enumerableRef._cancelationToken, cancelationToken, out enumerableRef._cancelationToken);
                    // Use the same cancelation token for both enumerators.
                    cv.secondAsyncEnumerator._target._cancelationToken = enumerableRef._cancelationToken;

                    try
                    {
                        using (var set = new Internal.PoolBackedSet<TKey, TEqualityComparer>(cv.comparer))
                        {
                            while (await cv.secondAsyncEnumerator.MoveNextAsync())
                            {
                                // We need to make sure we're on the configured context before invoking the key selector.
                                await cv.firstAsyncEnumerator.SwitchToContext();
                                var key = await cv.keySelector.Invoke(cv.secondAsyncEnumerator.Current);
                                // In case the key selector changed context, we need to make sure we're on the configured context before invoking the comparer.
                                await cv.firstAsyncEnumerator.SwitchToContext();
                                set.Add(key);
                            }

                            while (await cv.firstAsyncEnumerator.MoveNextAsync())
                            {
                                var element = cv.firstAsyncEnumerator.Current;
                                var key = await cv.keySelector.Invoke(element);
                                // In case the key selector changed context, we need to make sure we're on the configured context before invoking the comparer.
                                await cv.firstAsyncEnumerator.SwitchToContext();
                                if (set.Remove(key))
                                {
                                    await writer.YieldAsync(element);
                                }
                            }
                        }

                        // We yield and wait for the enumerator to be disposed, but only if there were no exceptions.
                        await writer.YieldAsync(default).ForLinqExtension();
                    }
                    finally
                    {
                        try
                        {
                            await cv.secondAsyncEnumerator.DisposeAsync();
                        }
                        finally
                        {
                            await cv.firstAsyncEnumerator.DisposeAsync();
                        }
                    }
                });
        }
    }
#endif // CSHARP_7_3_OR_NEWER
}