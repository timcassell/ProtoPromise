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
        #region KeySelector
        /// <summary>
        /// Creates a <see cref="Dictionary{TKey, TValue}"/> from an async-enumerable sequence using the specified <paramref name="comparer"/> to compare keys.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
        /// <param name="source">An async-enumerable sequence to create a <see cref="Dictionary{TKey, TValue}"/> for.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="comparer">An <see cref="IEqualityComparer{T}"/> to compare keys.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>A <see cref="Promise{T}"/> whose result will be a <see cref="Dictionary{TKey, TValue}"/> that contains values of type <typeparamref name="TSource"/> selected from the source sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="keySelector"/> produces duplicate keys for two elements.</exception>
        public static Promise<Dictionary<TKey, TSource>> ToDictionaryAsync<TSource, TKey>(this AsyncEnumerable<TSource> source,
            Func<TSource, TKey> keySelector, IEqualityComparer<TKey> comparer = null, CancelationToken cancelationToken = default)
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);

            return ToDictionaryHelper<TKey>.ToDictionaryAsyncCore(
                source.GetAsyncEnumerator(cancelationToken),
                Internal.PromiseRefBase.DelegateWrapper.Create(keySelector),
                comparer);
        }

        /// <summary>
        /// Creates a <see cref="Dictionary{TKey, TValue}"/> from an async-enumerable sequence using the specified <paramref name="comparer"/> to compare keys.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TCapture">The type of the captured value that will be passed to <paramref name="keySelector"/>.</typeparam>
        /// <param name="source">An async-enumerable sequence to create a <see cref="Dictionary{TKey, TValue}"/> for.</param>
        /// <param name="captureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="comparer">An <see cref="IEqualityComparer{T}"/> to compare keys.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>A <see cref="Promise{T}"/> whose result will be a <see cref="Dictionary{TKey, TValue}"/> that contains values of type <typeparamref name="TSource"/> selected from the source sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="keySelector"/> produces duplicate keys for two elements.</exception>
        public static Promise<Dictionary<TKey, TSource>> ToDictionaryAsync<TSource, TKey, TCapture>(this AsyncEnumerable<TSource> source,
            TCapture captureValue, Func<TCapture, TSource, TKey> keySelector, IEqualityComparer<TKey> comparer = null, CancelationToken cancelationToken = default)
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);

            return ToDictionaryHelper<TKey>.ToDictionaryAsyncCore(
                source.GetAsyncEnumerator(cancelationToken),
                Internal.PromiseRefBase.DelegateWrapper.Create(captureValue, keySelector),
                comparer);
        }

        /// <summary>
        /// Creates a <see cref="Dictionary{TKey, TValue}"/> from an async-enumerable sequence using the specified <paramref name="comparer"/> to compare keys.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
        /// <param name="source">An async-enumerable sequence to create a <see cref="Dictionary{TKey, TValue}"/> for.</param>
        /// <param name="keySelector">An async function  to extract a key from each element.</param>
        /// <param name="comparer">An <see cref="IEqualityComparer{T}"/> to compare keys.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>A <see cref="Promise{T}"/> whose result will be a <see cref="Dictionary{TKey, TValue}"/> that contains values of type <typeparamref name="TSource"/> selected from the source sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="keySelector"/> produces duplicate keys for two elements.</exception>
        public static Promise<Dictionary<TKey, TSource>> ToDictionaryAsync<TSource, TKey>(this AsyncEnumerable<TSource> source,
            Func<TSource, Promise<TKey>> keySelector, IEqualityComparer<TKey> comparer = null, CancelationToken cancelationToken = default)
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);

            return ToDictionaryHelper<TKey>.ToDictionaryAsyncCoreAwait(
                source.GetAsyncEnumerator(cancelationToken),
                Internal.PromiseRefBase.DelegateWrapper.Create(keySelector),
                comparer);
        }

        /// <summary>
        /// Creates a <see cref="Dictionary{TKey, TValue}"/> from an async-enumerable sequence using the specified <paramref name="comparer"/> to compare keys.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TCapture">The type of the captured value that will be passed to <paramref name="keySelector"/>.</typeparam>
        /// <param name="source">An async-enumerable sequence to create a <see cref="Dictionary{TKey, TValue}"/> for.</param>
        /// <param name="captureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">An async function  to extract a key from each element.</param>
        /// <param name="comparer">An <see cref="IEqualityComparer{T}"/> to compare keys.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>A <see cref="Promise{T}"/> whose result will be a <see cref="Dictionary{TKey, TValue}"/> that contains values of type <typeparamref name="TSource"/> selected from the source sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="keySelector"/> produces duplicate keys for two elements.</exception>
        public static Promise<Dictionary<TKey, TSource>> ToDictionaryAsync<TSource, TKey, TCapture>(this AsyncEnumerable<TSource> source,
            TCapture captureValue, Func<TCapture, TSource, Promise<TKey>> keySelector, IEqualityComparer<TKey> comparer = null, CancelationToken cancelationToken = default)
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);

            return ToDictionaryHelper<TKey>.ToDictionaryAsyncCoreAwait(
                source.GetAsyncEnumerator(cancelationToken),
                Internal.PromiseRefBase.DelegateWrapper.Create(captureValue, keySelector),
                comparer);
        }

        /// <summary>
        /// Creates a <see cref="Dictionary{TKey, TValue}"/> from a configured async-enumerable sequence using the specified <paramref name="comparer"/> to compare keys.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
        /// <param name="configuredSource">A configured async-enumerable sequence to create a <see cref="Dictionary{TKey, TValue}"/> for.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="comparer">An <see cref="IEqualityComparer{T}"/> to compare keys.</param>
        /// <returns>A <see cref="Promise{T}"/> whose result will be a <see cref="Dictionary{TKey, TValue}"/> that contains values of type <typeparamref name="TSource"/> selected from the source sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="keySelector"/> produces duplicate keys for two elements.</exception>
        public static Promise<Dictionary<TKey, TSource>> ToDictionaryAsync<TSource, TKey>(this in ConfiguredAsyncEnumerable<TSource> configuredSource,
            Func<TSource, TKey> keySelector, IEqualityComparer<TKey> comparer = null)
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);

            return ToDictionaryHelper<TKey>.ToDictionaryAsyncCore(
                configuredSource.GetAsyncEnumerator(),
                Internal.PromiseRefBase.DelegateWrapper.Create(keySelector),
                comparer);
        }

        /// <summary>
        /// Creates a <see cref="Dictionary{TKey, TValue}"/> from a configured async-enumerable sequence using the specified <paramref name="comparer"/> to compare keys.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TCapture">The type of the captured value that will be passed to <paramref name="keySelector"/>.</typeparam>
        /// <param name="configuredSource">A configured async-enumerable sequence to create a <see cref="Dictionary{TKey, TValue}"/> for.</param>
        /// <param name="captureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="comparer">An <see cref="IEqualityComparer{T}"/> to compare keys.</param>
        /// <returns>A <see cref="Promise{T}"/> whose result will be a <see cref="Dictionary{TKey, TValue}"/> that contains values of type <typeparamref name="TSource"/> selected from the source sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="keySelector"/> produces duplicate keys for two elements.</exception>
        public static Promise<Dictionary<TKey, TSource>> ToDictionaryAsync<TSource, TKey, TCapture>(this in ConfiguredAsyncEnumerable<TSource> configuredSource,
            TCapture captureValue, Func<TCapture, TSource, TKey> keySelector, IEqualityComparer<TKey> comparer = null)
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);

            return ToDictionaryHelper<TKey>.ToDictionaryAsyncCore(
                configuredSource.GetAsyncEnumerator(),
                Internal.PromiseRefBase.DelegateWrapper.Create(captureValue, keySelector),
                comparer);
        }

        /// <summary>
        /// Creates a <see cref="Dictionary{TKey, TValue}"/> from a configured async-enumerable sequence using the specified <paramref name="comparer"/> to compare keys.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
        /// <param name="configuredSource">A configured async-enumerable sequence to create a <see cref="Dictionary{TKey, TValue}"/> for.</param>
        /// <param name="keySelector">An async function  to extract a key from each element.</param>
        /// <param name="comparer">An <see cref="IEqualityComparer{T}"/> to compare keys.</param>
        /// <returns>A <see cref="Promise{T}"/> whose result will be a <see cref="Dictionary{TKey, TValue}"/> that contains values of type <typeparamref name="TSource"/> selected from the source sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="keySelector"/> produces duplicate keys for two elements.</exception>
        public static Promise<Dictionary<TKey, TSource>> ToDictionaryAsync<TSource, TKey>(this in ConfiguredAsyncEnumerable<TSource> configuredSource,
            Func<TSource, Promise<TKey>> keySelector, IEqualityComparer<TKey> comparer = null)
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);

            return ToDictionaryHelper<TKey>.ToDictionaryAsyncCoreAwait(
                configuredSource.GetAsyncEnumerator(),
                Internal.PromiseRefBase.DelegateWrapper.Create(keySelector),
                comparer);
        }

        /// <summary>
        /// Creates a <see cref="Dictionary{TKey, TValue}"/> from a configured async-enumerable sequence using the specified <paramref name="comparer"/> to compare keys.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TCapture">The type of the captured value that will be passed to <paramref name="keySelector"/>.</typeparam>
        /// <param name="configuredSource">A configured async-enumerable sequence to create a <see cref="Dictionary{TKey, TValue}"/> for.</param>
        /// <param name="captureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">An async function  to extract a key from each element.</param>
        /// <param name="comparer">An <see cref="IEqualityComparer{T}"/> to compare keys.</param>
        /// <returns>A <see cref="Promise{T}"/> whose result will be a <see cref="Dictionary{TKey, TValue}"/> that contains values of type <typeparamref name="TSource"/> selected from the source sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="keySelector"/> produces duplicate keys for two elements.</exception>
        public static Promise<Dictionary<TKey, TSource>> ToDictionaryAsync<TSource, TKey, TCapture>(this in ConfiguredAsyncEnumerable<TSource> configuredSource,
            TCapture captureValue, Func<TCapture, TSource, Promise<TKey>> keySelector, IEqualityComparer<TKey> comparer = null)
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);

            return ToDictionaryHelper<TKey>.ToDictionaryAsyncCoreAwait(
                configuredSource.GetAsyncEnumerator(),
                Internal.PromiseRefBase.DelegateWrapper.Create(captureValue, keySelector),
                comparer);
        }

        private static class ToDictionaryHelper<TKey>
        {
            internal static async Promise<Dictionary<TKey, TSource>> ToDictionaryAsyncCore<TSource, TKeySelector>(
                AsyncEnumerator<TSource> asyncEnumerator,
                TKeySelector keySelector,
                IEqualityComparer<TKey> comparer)
                where TKeySelector : Internal.IFunc<TSource, TKey>
            {
                try
                {
                    var dictionary = new Dictionary<TKey, TSource>(comparer);
                    while (await asyncEnumerator.MoveNextAsync())
                    {
                        var element = asyncEnumerator.Current;
                        dictionary.Add(keySelector.Invoke(element), element);
                    }
                    return dictionary;
                }
                finally
                {
                    await asyncEnumerator.DisposeAsync();
                }
            }

            internal static async Promise<Dictionary<TKey, TSource>> ToDictionaryAsyncCoreAwait<TSource, TKeySelector>(
                AsyncEnumerator<TSource> asyncEnumerator,
                TKeySelector keySelector,
                IEqualityComparer<TKey> comparer)
                where TKeySelector : Internal.IFunc<TSource, Promise<TKey>>
            {
                try
                {
                    var dictionary = new Dictionary<TKey, TSource>(comparer);
                    while (await asyncEnumerator.MoveNextAsync())
                    {
                        var element = asyncEnumerator.Current;
                        dictionary.Add(await keySelector.Invoke(element), element);
                    }
                    return dictionary;
                }
                finally
                {
                    await asyncEnumerator.DisposeAsync();
                }
            }

            internal static async Promise<Dictionary<TKey, TSource>> ToDictionaryAsyncCore<TSource, TKeySelector>(
                ConfiguredAsyncEnumerable<TSource>.Enumerator asyncEnumerator,
                TKeySelector keySelector,
                IEqualityComparer<TKey> comparer)
                where TKeySelector : Internal.IFunc<TSource, TKey>
            {
                try
                {
                    var dictionary = new Dictionary<TKey, TSource>(comparer);
                    while (await asyncEnumerator.MoveNextAsync())
                    {
                        var element = asyncEnumerator.Current;
                        dictionary.Add(keySelector.Invoke(element), element);
                    }
                    return dictionary;
                }
                finally
                {
                    await asyncEnumerator.DisposeAsync();
                }
            }

            internal static async Promise<Dictionary<TKey, TSource>> ToDictionaryAsyncCoreAwait<TSource, TKeySelector>(
                ConfiguredAsyncEnumerable<TSource>.Enumerator asyncEnumerator,
                TKeySelector keySelector,
                IEqualityComparer<TKey> comparer)
                where TKeySelector : Internal.IFunc<TSource, Promise<TKey>>
            {
                try
                {
                    var dictionary = new Dictionary<TKey, TSource>(comparer);
                    while (await asyncEnumerator.MoveNextAsync())
                    {
                        var element = asyncEnumerator.Current;
                        dictionary.Add(await keySelector.Invoke(element), element);
                    }
                    return dictionary;
                }
                finally
                {
                    await asyncEnumerator.DisposeAsync();
                }
            }
        }
        #endregion KeySelector

        #region KeyElementSelector
        /// <summary>
        /// Creates a <see cref="Dictionary{TKey, TValue}"/> from an async-enumerable sequence using the specified <paramref name="comparer"/> to compare keys.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TElement">The type of the key returned by <paramref name="elementSelector"/>.</typeparam>
        /// <param name="source">An async-enumerable sequence to create a <see cref="Dictionary{TKey, TValue}"/> for.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="elementSelector">A transform function to produce a result element value from each element.</param>
        /// <param name="comparer">An <see cref="IEqualityComparer{T}"/> to compare keys.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>A <see cref="Promise{T}"/> whose result will be a <see cref="Dictionary{TKey, TValue}"/> that contains values of type <typeparamref name="TSource"/> selected from the source sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="elementSelector"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="keySelector"/> produces duplicate keys for two elements.</exception>
        public static Promise<Dictionary<TKey, TElement>> ToDictionaryAsync<TSource, TKey, TElement>(this AsyncEnumerable<TSource> source,
            Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, IEqualityComparer<TKey> comparer = null, CancelationToken cancelationToken = default)
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(elementSelector, nameof(elementSelector), 1);

            return ToDictionaryHelper<TKey, TElement>.ToDictionaryAsyncCore(
                source.GetAsyncEnumerator(cancelationToken),
                Internal.PromiseRefBase.DelegateWrapper.Create(keySelector),
                Internal.PromiseRefBase.DelegateWrapper.Create(elementSelector),
                comparer);
        }

        /// <summary>
        /// Creates a <see cref="Dictionary{TKey, TValue}"/> from an async-enumerable sequence using the specified <paramref name="comparer"/> to compare keys.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TElement">The type of the key returned by <paramref name="elementSelector"/>.</typeparam>
        /// <typeparam name="TKeyCapture">The type of the captured value that will be passed to <paramref name="keySelector"/> that will be passed to <paramref name="keySelector"/>.</typeparam>
        /// <param name="source">An async-enumerable sequence to create a <see cref="Dictionary{TKey, TValue}"/> for.</param>
        /// <param name="captureKeyValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="elementSelector">A transform function to produce a result element value from each element.</param>
        /// <param name="comparer">An <see cref="IEqualityComparer{T}"/> to compare keys.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>A <see cref="Promise{T}"/> whose result will be a <see cref="Dictionary{TKey, TValue}"/> that contains values of type <typeparamref name="TSource"/> selected from the source sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="elementSelector"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="keySelector"/> produces duplicate keys for two elements.</exception>
        public static Promise<Dictionary<TKey, TElement>> ToDictionaryAsync<TSource, TKey, TElement, TKeyCapture>(this AsyncEnumerable<TSource> source,
            TKeyCapture captureKeyValue, Func<TKeyCapture, TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, IEqualityComparer<TKey> comparer = null, CancelationToken cancelationToken = default)
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(elementSelector, nameof(elementSelector), 1);

            return ToDictionaryHelper<TKey, TElement>.ToDictionaryAsyncCore(
                source.GetAsyncEnumerator(cancelationToken),
                Internal.PromiseRefBase.DelegateWrapper.Create(captureKeyValue, keySelector),
                Internal.PromiseRefBase.DelegateWrapper.Create(elementSelector),
                comparer);
        }

        /// <summary>
        /// Creates a <see cref="Dictionary{TKey, TValue}"/> from an async-enumerable sequence using the specified <paramref name="comparer"/> to compare keys.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TElement">The type of the key returned by <paramref name="elementSelector"/>.</typeparam>
        /// <param name="source">An async-enumerable sequence to create a <see cref="Dictionary{TKey, TValue}"/> for.</param>
        /// <param name="keySelector">An async function  to extract a key from each element.</param>
        /// <param name="elementSelector">A transform function to produce a result element value from each element.</param>
        /// <param name="comparer">An <see cref="IEqualityComparer{T}"/> to compare keys.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>A <see cref="Promise{T}"/> whose result will be a <see cref="Dictionary{TKey, TValue}"/> that contains values of type <typeparamref name="TSource"/> selected from the source sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="elementSelector"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="keySelector"/> produces duplicate keys for two elements.</exception>
        public static Promise<Dictionary<TKey, TElement>> ToDictionaryAsync<TSource, TKey, TElement>(this AsyncEnumerable<TSource> source,
            Func<TSource, Promise<TKey>> keySelector, Func<TSource, TElement> elementSelector, IEqualityComparer<TKey> comparer = null, CancelationToken cancelationToken = default)
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(elementSelector, nameof(elementSelector), 1);

            return ToDictionaryHelper<TKey, TElement>.ToDictionaryAsyncCoreAwaitKey(
                source.GetAsyncEnumerator(cancelationToken),
                Internal.PromiseRefBase.DelegateWrapper.Create(keySelector),
                Internal.PromiseRefBase.DelegateWrapper.Create(elementSelector),
                comparer);
        }

        /// <summary>
        /// Creates a <see cref="Dictionary{TKey, TValue}"/> from an async-enumerable sequence using the specified <paramref name="comparer"/> to compare keys.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TElement">The type of the key returned by <paramref name="elementSelector"/>.</typeparam>
        /// <typeparam name="TKeyCapture">The type of the captured value that will be passed to <paramref name="keySelector"/>.</typeparam>
        /// <param name="source">An async-enumerable sequence to create a <see cref="Dictionary{TKey, TValue}"/> for.</param>
        /// <param name="captureKeyValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">An async function  to extract a key from each element.</param>
        /// <param name="elementSelector">A transform function to produce a result element value from each element.</param>
        /// <param name="comparer">An <see cref="IEqualityComparer{T}"/> to compare keys.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>A <see cref="Promise{T}"/> whose result will be a <see cref="Dictionary{TKey, TValue}"/> that contains values of type <typeparamref name="TSource"/> selected from the source sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="elementSelector"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="keySelector"/> produces duplicate keys for two elements.</exception>
        public static Promise<Dictionary<TKey, TElement>> ToDictionaryAsync<TSource, TKey, TElement, TKeyCapture>(this AsyncEnumerable<TSource> source,
            TKeyCapture captureKeyValue, Func<TKeyCapture, TSource, Promise<TKey>> keySelector, Func<TSource, TElement> elementSelector, IEqualityComparer<TKey> comparer = null, CancelationToken cancelationToken = default)
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(elementSelector, nameof(elementSelector), 1);

            return ToDictionaryHelper<TKey, TElement>.ToDictionaryAsyncCoreAwaitKey(
                source.GetAsyncEnumerator(cancelationToken),
                Internal.PromiseRefBase.DelegateWrapper.Create(captureKeyValue, keySelector),
                Internal.PromiseRefBase.DelegateWrapper.Create(elementSelector),
                comparer);
        }

        /// <summary>
        /// Creates a <see cref="Dictionary{TKey, TValue}"/> from a configured async-enumerable sequence using the specified <paramref name="comparer"/> to compare keys.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TElement">The type of the key returned by <paramref name="elementSelector"/>.</typeparam>
        /// <param name="configuredSource">A configured async-enumerable sequence to create a <see cref="Dictionary{TKey, TValue}"/> for.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="elementSelector">A transform function to produce a result element value from each element.</param>
        /// <param name="comparer">An <see cref="IEqualityComparer{T}"/> to compare keys.</param>
        /// <returns>A <see cref="Promise{T}"/> whose result will be a <see cref="Dictionary{TKey, TValue}"/> that contains values of type <typeparamref name="TSource"/> selected from the source sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="elementSelector"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="keySelector"/> produces duplicate keys for two elements.</exception>
        public static Promise<Dictionary<TKey, TElement>> ToDictionaryAsync<TSource, TKey, TElement>(this in ConfiguredAsyncEnumerable<TSource> configuredSource,
            Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, IEqualityComparer<TKey> comparer = null)
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(elementSelector, nameof(elementSelector), 1);

            return ToDictionaryHelper<TKey, TElement>.ToDictionaryAsyncCore(
                configuredSource.GetAsyncEnumerator(),
                Internal.PromiseRefBase.DelegateWrapper.Create(keySelector),
                Internal.PromiseRefBase.DelegateWrapper.Create(elementSelector),
                comparer);
        }

        /// <summary>
        /// Creates a <see cref="Dictionary{TKey, TValue}"/> from a configured async-enumerable sequence using the specified <paramref name="comparer"/> to compare keys.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TElement">The type of the key returned by <paramref name="elementSelector"/>.</typeparam>
        /// <typeparam name="TKeyCapture">The type of the captured value that will be passed to <paramref name="keySelector"/>.</typeparam>
        /// <param name="configuredSource">A configured async-enumerable sequence to create a <see cref="Dictionary{TKey, TValue}"/> for.</param>
        /// <param name="captureKeyValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="elementSelector">A transform function to produce a result element value from each element.</param>
        /// <param name="comparer">An <see cref="IEqualityComparer{T}"/> to compare keys.</param>
        /// <returns>A <see cref="Promise{T}"/> whose result will be a <see cref="Dictionary{TKey, TValue}"/> that contains values of type <typeparamref name="TSource"/> selected from the source sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="elementSelector"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="keySelector"/> produces duplicate keys for two elements.</exception>
        public static Promise<Dictionary<TKey, TElement>> ToDictionaryAsync<TSource, TKey, TElement, TKeyCapture>(this in ConfiguredAsyncEnumerable<TSource> configuredSource,
            TKeyCapture captureKeyValue, Func<TKeyCapture, TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, IEqualityComparer<TKey> comparer = null)
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(elementSelector, nameof(elementSelector), 1);

            return ToDictionaryHelper<TKey, TElement>.ToDictionaryAsyncCore(
                configuredSource.GetAsyncEnumerator(),
                Internal.PromiseRefBase.DelegateWrapper.Create(captureKeyValue, keySelector),
                Internal.PromiseRefBase.DelegateWrapper.Create(elementSelector),
                comparer);
        }

        /// <summary>
        /// Creates a <see cref="Dictionary{TKey, TValue}"/> from a configured async-enumerable sequence using the specified <paramref name="comparer"/> to compare keys.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TElement">The type of the key returned by <paramref name="elementSelector"/>.</typeparam>
        /// <param name="configuredSource">A configured async-enumerable sequence to create a <see cref="Dictionary{TKey, TValue}"/> for.</param>
        /// <param name="keySelector">An async function  to extract a key from each element.</param>
        /// <param name="elementSelector">A transform function to produce a result element value from each element.</param>
        /// <param name="comparer">An <see cref="IEqualityComparer{T}"/> to compare keys.</param>
        /// <returns>A <see cref="Promise{T}"/> whose result will be a <see cref="Dictionary{TKey, TValue}"/> that contains values of type <typeparamref name="TSource"/> selected from the source sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="elementSelector"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="keySelector"/> produces duplicate keys for two elements.</exception>
        public static Promise<Dictionary<TKey, TElement>> ToDictionaryAsync<TSource, TKey, TElement>(this in ConfiguredAsyncEnumerable<TSource> configuredSource,
            Func<TSource, Promise<TKey>> keySelector, Func<TSource, TElement> elementSelector, IEqualityComparer<TKey> comparer = null)
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(elementSelector, nameof(elementSelector), 1);

            return ToDictionaryHelper<TKey, TElement>.ToDictionaryAsyncCoreAwaitKey(
                configuredSource.GetAsyncEnumerator(),
                Internal.PromiseRefBase.DelegateWrapper.Create(keySelector),
                Internal.PromiseRefBase.DelegateWrapper.Create(elementSelector),
                comparer);
        }

        /// <summary>
        /// Creates a <see cref="Dictionary{TKey, TValue}"/> from a configured async-enumerable sequence using the specified <paramref name="comparer"/> to compare keys.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TElement">The type of the key returned by <paramref name="elementSelector"/>.</typeparam>
        /// <typeparam name="TKeyCapture">The type of the captured value that will be passed to <paramref name="keySelector"/>.</typeparam>
        /// <param name="configuredSource">A configured async-enumerable sequence to create a <see cref="Dictionary{TKey, TValue}"/> for.</param>
        /// <param name="captureKeyValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">An async function  to extract a key from each element.</param>
        /// <param name="elementSelector">A transform function to produce a result element value from each element.</param>
        /// <param name="comparer">An <see cref="IEqualityComparer{T}"/> to compare keys.</param>
        /// <returns>A <see cref="Promise{T}"/> whose result will be a <see cref="Dictionary{TKey, TValue}"/> that contains values of type <typeparamref name="TSource"/> selected from the source sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="elementSelector"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="keySelector"/> produces duplicate keys for two elements.</exception>
        public static Promise<Dictionary<TKey, TElement>> ToDictionaryAsync<TSource, TKey, TElement, TKeyCapture>(this in ConfiguredAsyncEnumerable<TSource> configuredSource,
            TKeyCapture captureKeyValue, Func<TKeyCapture, TSource, Promise<TKey>> keySelector, Func<TSource, TElement> elementSelector, IEqualityComparer<TKey> comparer = null)
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(elementSelector, nameof(elementSelector), 1);

            return ToDictionaryHelper<TKey, TElement>.ToDictionaryAsyncCoreAwaitKey(
                configuredSource.GetAsyncEnumerator(),
                Internal.PromiseRefBase.DelegateWrapper.Create(captureKeyValue, keySelector),
                Internal.PromiseRefBase.DelegateWrapper.Create(elementSelector),
                comparer);
        }

        /// <summary>
        /// Creates a <see cref="Dictionary{TKey, TValue}"/> from an async-enumerable sequence using the specified <paramref name="comparer"/> to compare keys.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TElement">The type of the key returned by <paramref name="elementSelector"/>.</typeparam>
        /// <typeparam name="TElementCapture">The type of the captured value that will be passed to <paramref name="elementSelector"/>.</typeparam>
        /// <param name="source">An async-enumerable sequence to create a <see cref="Dictionary{TKey, TValue}"/> for.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="captureElementValue">The extra value that will be passed to <paramref name="elementSelector"/>.</param>
        /// <param name="elementSelector">A transform function to produce a result element value from each element.</param>
        /// <param name="comparer">An <see cref="IEqualityComparer{T}"/> to compare keys.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>A <see cref="Promise{T}"/> whose result will be a <see cref="Dictionary{TKey, TValue}"/> that contains values of type <typeparamref name="TSource"/> selected from the source sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="elementSelector"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="keySelector"/> produces duplicate keys for two elements.</exception>
        public static Promise<Dictionary<TKey, TElement>> ToDictionaryAsync<TSource, TKey, TElement, TElementCapture>(this AsyncEnumerable<TSource> source,
            Func<TSource, TKey> keySelector, TElementCapture captureElementValue, Func<TElementCapture, TSource, TElement> elementSelector, IEqualityComparer<TKey> comparer = null, CancelationToken cancelationToken = default)
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(elementSelector, nameof(elementSelector), 1);

            return ToDictionaryHelper<TKey, TElement>.ToDictionaryAsyncCore(
                source.GetAsyncEnumerator(cancelationToken),
                Internal.PromiseRefBase.DelegateWrapper.Create(keySelector),
                Internal.PromiseRefBase.DelegateWrapper.Create(captureElementValue, elementSelector),
                comparer);
        }

        /// <summary>
        /// Creates a <see cref="Dictionary{TKey, TValue}"/> from an async-enumerable sequence using the specified <paramref name="comparer"/> to compare keys.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TElement">The type of the key returned by <paramref name="elementSelector"/>.</typeparam>
        /// <typeparam name="TKeyCapture">The type of the captured value that will be passed to <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TElementCapture">The type of the captured value that will be passed to <paramref name="elementSelector"/>.</typeparam>
        /// <param name="source">An async-enumerable sequence to create a <see cref="Dictionary{TKey, TValue}"/> for.</param>
        /// <param name="captureKeyValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="captureElementValue">The extra value that will be passed to <paramref name="elementSelector"/>.</param>
        /// <param name="elementSelector">A transform function to produce a result element value from each element.</param>
        /// <param name="comparer">An <see cref="IEqualityComparer{T}"/> to compare keys.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>A <see cref="Promise{T}"/> whose result will be a <see cref="Dictionary{TKey, TValue}"/> that contains values of type <typeparamref name="TSource"/> selected from the source sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="elementSelector"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="keySelector"/> produces duplicate keys for two elements.</exception>
        public static Promise<Dictionary<TKey, TElement>> ToDictionaryAsync<TSource, TKey, TElement, TKeyCapture, TElementCapture>(this AsyncEnumerable<TSource> source,
            TKeyCapture captureKeyValue, Func<TKeyCapture, TSource, TKey> keySelector, TElementCapture captureElementValue, Func<TElementCapture, TSource, TElement> elementSelector, IEqualityComparer<TKey> comparer = null, CancelationToken cancelationToken = default)
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(elementSelector, nameof(elementSelector), 1);

            return ToDictionaryHelper<TKey, TElement>.ToDictionaryAsyncCore(
                source.GetAsyncEnumerator(cancelationToken),
                Internal.PromiseRefBase.DelegateWrapper.Create(captureKeyValue, keySelector),
                Internal.PromiseRefBase.DelegateWrapper.Create(captureElementValue, elementSelector),
                comparer);
        }

        /// <summary>
        /// Creates a <see cref="Dictionary{TKey, TValue}"/> from an async-enumerable sequence using the specified <paramref name="comparer"/> to compare keys.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TElement">The type of the key returned by <paramref name="elementSelector"/>.</typeparam>
        /// <typeparam name="TElementCapture">The type of the captured value that will be passed to <paramref name="elementSelector"/>.</typeparam>
        /// <param name="source">An async-enumerable sequence to create a <see cref="Dictionary{TKey, TValue}"/> for.</param>
        /// <param name="keySelector">An async function  to extract a key from each element.</param>
        /// <param name="captureElementValue">The extra value that will be passed to <paramref name="elementSelector"/>.</param>
        /// <param name="elementSelector">A transform function to produce a result element value from each element.</param>
        /// <param name="comparer">An <see cref="IEqualityComparer{T}"/> to compare keys.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>A <see cref="Promise{T}"/> whose result will be a <see cref="Dictionary{TKey, TValue}"/> that contains values of type <typeparamref name="TSource"/> selected from the source sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="elementSelector"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="keySelector"/> produces duplicate keys for two elements.</exception>
        public static Promise<Dictionary<TKey, TElement>> ToDictionaryAsync<TSource, TKey, TElement, TElementCapture>(this AsyncEnumerable<TSource> source,
            Func<TSource, Promise<TKey>> keySelector, TElementCapture captureElementValue, Func<TElementCapture, TSource, TElement> elementSelector, IEqualityComparer<TKey> comparer = null, CancelationToken cancelationToken = default)
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(elementSelector, nameof(elementSelector), 1);

            return ToDictionaryHelper<TKey, TElement>.ToDictionaryAsyncCoreAwaitKey(
                source.GetAsyncEnumerator(cancelationToken),
                Internal.PromiseRefBase.DelegateWrapper.Create(keySelector),
                Internal.PromiseRefBase.DelegateWrapper.Create(captureElementValue, elementSelector),
                comparer);
        }

        /// <summary>
        /// Creates a <see cref="Dictionary{TKey, TValue}"/> from an async-enumerable sequence using the specified <paramref name="comparer"/> to compare keys.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TElement">The type of the key returned by <paramref name="elementSelector"/>.</typeparam>
        /// <typeparam name="TKeyCapture">The type of the captured value that will be passed to <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TElementCapture">The type of the captured value that will be passed to <paramref name="elementSelector"/>.</typeparam>
        /// <param name="source">An async-enumerable sequence to create a <see cref="Dictionary{TKey, TValue}"/> for.</param>
        /// <param name="captureKeyValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">An async function  to extract a key from each element.</param>
        /// <param name="captureElementValue">The extra value that will be passed to <paramref name="elementSelector"/>.</param>
        /// <param name="elementSelector">A transform function to produce a result element value from each element.</param>
        /// <param name="comparer">An <see cref="IEqualityComparer{T}"/> to compare keys.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>A <see cref="Promise{T}"/> whose result will be a <see cref="Dictionary{TKey, TValue}"/> that contains values of type <typeparamref name="TSource"/> selected from the source sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="elementSelector"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="keySelector"/> produces duplicate keys for two elements.</exception>
        public static Promise<Dictionary<TKey, TElement>> ToDictionaryAsync<TSource, TKey, TElement, TKeyCapture, TElementCapture>(this AsyncEnumerable<TSource> source,
            TKeyCapture captureKeyValue, Func<TKeyCapture, TSource, Promise<TKey>> keySelector, TElementCapture captureElementValue, Func<TElementCapture, TSource, TElement> elementSelector, IEqualityComparer<TKey> comparer = null, CancelationToken cancelationToken = default)
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(elementSelector, nameof(elementSelector), 1);

            return ToDictionaryHelper<TKey, TElement>.ToDictionaryAsyncCoreAwaitKey(
                source.GetAsyncEnumerator(cancelationToken),
                Internal.PromiseRefBase.DelegateWrapper.Create(captureKeyValue, keySelector),
                Internal.PromiseRefBase.DelegateWrapper.Create(captureElementValue, elementSelector),
                comparer);
        }

        /// <summary>
        /// Creates a <see cref="Dictionary{TKey, TValue}"/> from a configured async-enumerable sequence using the specified <paramref name="comparer"/> to compare keys.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TElement">The type of the key returned by <paramref name="elementSelector"/>.</typeparam>
        /// <typeparam name="TElementCapture">The type of the captured value that will be passed to <paramref name="elementSelector"/>.</typeparam>
        /// <param name="configuredSource">A configured async-enumerable sequence to create a <see cref="Dictionary{TKey, TValue}"/> for.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="captureElementValue">The extra value that will be passed to <paramref name="elementSelector"/>.</param>
        /// <param name="elementSelector">A transform function to produce a result element value from each element.</param>
        /// <param name="comparer">An <see cref="IEqualityComparer{T}"/> to compare keys.</param>
        /// <returns>A <see cref="Promise{T}"/> whose result will be a <see cref="Dictionary{TKey, TValue}"/> that contains values of type <typeparamref name="TSource"/> selected from the source sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="elementSelector"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="keySelector"/> produces duplicate keys for two elements.</exception>
        public static Promise<Dictionary<TKey, TElement>> ToDictionaryAsync<TSource, TKey, TElement, TElementCapture>(this in ConfiguredAsyncEnumerable<TSource> configuredSource,
            Func<TSource, TKey> keySelector, TElementCapture captureElementValue, Func<TElementCapture, TSource, TElement> elementSelector, IEqualityComparer<TKey> comparer = null)
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(elementSelector, nameof(elementSelector), 1);

            return ToDictionaryHelper<TKey, TElement>.ToDictionaryAsyncCore(
                configuredSource.GetAsyncEnumerator(),
                Internal.PromiseRefBase.DelegateWrapper.Create(keySelector),
                Internal.PromiseRefBase.DelegateWrapper.Create(captureElementValue, elementSelector),
                comparer);
        }

        /// <summary>
        /// Creates a <see cref="Dictionary{TKey, TValue}"/> from a configured async-enumerable sequence using the specified <paramref name="comparer"/> to compare keys.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TElement">The type of the key returned by <paramref name="elementSelector"/>.</typeparam>
        /// <typeparam name="TKeyCapture">The type of the captured value that will be passed to <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TElementCapture">The type of the captured value that will be passed to <paramref name="elementSelector"/>.</typeparam>
        /// <param name="configuredSource">A configured async-enumerable sequence to create a <see cref="Dictionary{TKey, TValue}"/> for.</param>
        /// <param name="captureKeyValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="captureElementValue">The extra value that will be passed to <paramref name="elementSelector"/>.</param>
        /// <param name="elementSelector">A transform function to produce a result element value from each element.</param>
        /// <param name="comparer">An <see cref="IEqualityComparer{T}"/> to compare keys.</param>
        /// <returns>A <see cref="Promise{T}"/> whose result will be a <see cref="Dictionary{TKey, TValue}"/> that contains values of type <typeparamref name="TSource"/> selected from the source sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="elementSelector"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="keySelector"/> produces duplicate keys for two elements.</exception>
        public static Promise<Dictionary<TKey, TElement>> ToDictionaryAsync<TSource, TKey, TElement, TKeyCapture, TElementCapture>(this in ConfiguredAsyncEnumerable<TSource> configuredSource,
            TKeyCapture captureKeyValue, Func<TKeyCapture, TSource, TKey> keySelector, TElementCapture captureElementValue, Func<TElementCapture, TSource, TElement> elementSelector, IEqualityComparer<TKey> comparer = null)
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(elementSelector, nameof(elementSelector), 1);

            return ToDictionaryHelper<TKey, TElement>.ToDictionaryAsyncCore(
                configuredSource.GetAsyncEnumerator(),
                Internal.PromiseRefBase.DelegateWrapper.Create(captureKeyValue, keySelector),
                Internal.PromiseRefBase.DelegateWrapper.Create(captureElementValue, elementSelector),
                comparer);
        }

        /// <summary>
        /// Creates a <see cref="Dictionary{TKey, TValue}"/> from a configured async-enumerable sequence using the specified <paramref name="comparer"/> to compare keys.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TElement">The type of the key returned by <paramref name="elementSelector"/>.</typeparam>
        /// <typeparam name="TElementCapture">The type of the captured value that will be passed to <paramref name="elementSelector"/>.</typeparam>
        /// <param name="configuredSource">A configured async-enumerable sequence to create a <see cref="Dictionary{TKey, TValue}"/> for.</param>
        /// <param name="keySelector">An async function  to extract a key from each element.</param>
        /// <param name="captureElementValue">The extra value that will be passed to <paramref name="elementSelector"/>.</param>
        /// <param name="elementSelector">A transform function to produce a result element value from each element.</param>
        /// <param name="comparer">An <see cref="IEqualityComparer{T}"/> to compare keys.</param>
        /// <returns>A <see cref="Promise{T}"/> whose result will be a <see cref="Dictionary{TKey, TValue}"/> that contains values of type <typeparamref name="TSource"/> selected from the source sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="elementSelector"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="keySelector"/> produces duplicate keys for two elements.</exception>
        public static Promise<Dictionary<TKey, TElement>> ToDictionaryAsync<TSource, TKey, TElement, TElementCapture>(this in ConfiguredAsyncEnumerable<TSource> configuredSource,
            Func<TSource, Promise<TKey>> keySelector, TElementCapture captureElementValue, Func<TElementCapture, TSource, TElement> elementSelector, IEqualityComparer<TKey> comparer = null)
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(elementSelector, nameof(elementSelector), 1);

            return ToDictionaryHelper<TKey, TElement>.ToDictionaryAsyncCoreAwaitKey(
                configuredSource.GetAsyncEnumerator(),
                Internal.PromiseRefBase.DelegateWrapper.Create(keySelector),
                Internal.PromiseRefBase.DelegateWrapper.Create(captureElementValue, elementSelector),
                comparer);
        }

        /// <summary>
        /// Creates a <see cref="Dictionary{TKey, TValue}"/> from a configured async-enumerable sequence using the specified <paramref name="comparer"/> to compare keys.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TElement">The type of the key returned by <paramref name="elementSelector"/>.</typeparam>
        /// <typeparam name="TKeyCapture">The type of the captured value that will be passed to <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TElementCapture">The type of the captured value that will be passed to <paramref name="elementSelector"/>.</typeparam>
        /// <param name="configuredSource">A configured async-enumerable sequence to create a <see cref="Dictionary{TKey, TValue}"/> for.</param>
        /// <param name="captureKeyValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">An async function  to extract a key from each element.</param>
        /// <param name="captureElementValue">The extra value that will be passed to <paramref name="elementSelector"/>.</param>
        /// <param name="elementSelector">A transform function to produce a result element value from each element.</param>
        /// <param name="comparer">An <see cref="IEqualityComparer{T}"/> to compare keys.</param>
        /// <returns>A <see cref="Promise{T}"/> whose result will be a <see cref="Dictionary{TKey, TValue}"/> that contains values of type <typeparamref name="TSource"/> selected from the source sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="elementSelector"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="keySelector"/> produces duplicate keys for two elements.</exception>
        public static Promise<Dictionary<TKey, TElement>> ToDictionaryAsync<TSource, TKey, TElement, TKeyCapture, TElementCapture>(this in ConfiguredAsyncEnumerable<TSource> configuredSource,
            TKeyCapture captureKeyValue, Func<TKeyCapture, TSource, Promise<TKey>> keySelector, TElementCapture captureElementValue, Func<TElementCapture, TSource, TElement> elementSelector, IEqualityComparer<TKey> comparer = null)
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(elementSelector, nameof(elementSelector), 1);

            return ToDictionaryHelper<TKey, TElement>.ToDictionaryAsyncCoreAwaitKey(
                configuredSource.GetAsyncEnumerator(),
                Internal.PromiseRefBase.DelegateWrapper.Create(captureKeyValue, keySelector),
                Internal.PromiseRefBase.DelegateWrapper.Create(captureElementValue, elementSelector),
                comparer);
        }

        /// <summary>
        /// Creates a <see cref="Dictionary{TKey, TValue}"/> from an async-enumerable sequence using the specified <paramref name="comparer"/> to compare keys.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TElement">The type of the key returned by <paramref name="elementSelector"/>.</typeparam>
        /// <param name="source">An async-enumerable sequence to create a <see cref="Dictionary{TKey, TValue}"/> for.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="elementSelector">An async transform function to produce a result element value from each element.</param>
        /// <param name="comparer">An <see cref="IEqualityComparer{T}"/> to compare keys.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>A <see cref="Promise{T}"/> whose result will be a <see cref="Dictionary{TKey, TValue}"/> that contains values of type <typeparamref name="TSource"/> selected from the source sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="elementSelector"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="keySelector"/> produces duplicate keys for two elements.</exception>
        public static Promise<Dictionary<TKey, TElement>> ToDictionaryAsync<TSource, TKey, TElement>(this AsyncEnumerable<TSource> source,
            Func<TSource, TKey> keySelector, Func<TSource, Promise<TElement>> elementSelector, IEqualityComparer<TKey> comparer = null, CancelationToken cancelationToken = default)
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(elementSelector, nameof(elementSelector), 1);

            return ToDictionaryHelper<TKey, TElement>.ToDictionaryAsyncCoreAwaitElement(
                source.GetAsyncEnumerator(cancelationToken),
                Internal.PromiseRefBase.DelegateWrapper.Create(keySelector),
                Internal.PromiseRefBase.DelegateWrapper.Create(elementSelector),
                comparer);
        }

        /// <summary>
        /// Creates a <see cref="Dictionary{TKey, TValue}"/> from an async-enumerable sequence using the specified <paramref name="comparer"/> to compare keys.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TElement">The type of the key returned by <paramref name="elementSelector"/>.</typeparam>
        /// <typeparam name="TKeyCapture">The type of the captured value that will be passed to <paramref name="keySelector"/>.</typeparam>
        /// <param name="source">An async-enumerable sequence to create a <see cref="Dictionary{TKey, TValue}"/> for.</param>
        /// <param name="captureKeyValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="elementSelector">An async transform function to produce a result element value from each element.</param>
        /// <param name="comparer">An <see cref="IEqualityComparer{T}"/> to compare keys.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>A <see cref="Promise{T}"/> whose result will be a <see cref="Dictionary{TKey, TValue}"/> that contains values of type <typeparamref name="TSource"/> selected from the source sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="elementSelector"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="keySelector"/> produces duplicate keys for two elements.</exception>
        public static Promise<Dictionary<TKey, TElement>> ToDictionaryAsync<TSource, TKey, TElement, TKeyCapture>(this AsyncEnumerable<TSource> source,
            TKeyCapture captureKeyValue, Func<TKeyCapture, TSource, TKey> keySelector, Func<TSource, Promise<TElement>> elementSelector, IEqualityComparer<TKey> comparer = null, CancelationToken cancelationToken = default)
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(elementSelector, nameof(elementSelector), 1);

            return ToDictionaryHelper<TKey, TElement>.ToDictionaryAsyncCoreAwaitElement(
                source.GetAsyncEnumerator(cancelationToken),
                Internal.PromiseRefBase.DelegateWrapper.Create(captureKeyValue, keySelector),
                Internal.PromiseRefBase.DelegateWrapper.Create(elementSelector),
                comparer);
        }

        /// <summary>
        /// Creates a <see cref="Dictionary{TKey, TValue}"/> from an async-enumerable sequence using the specified <paramref name="comparer"/> to compare keys.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TElement">The type of the key returned by <paramref name="elementSelector"/>.</typeparam>
        /// <param name="source">An async-enumerable sequence to create a <see cref="Dictionary{TKey, TValue}"/> for.</param>
        /// <param name="keySelector">An async function  to extract a key from each element.</param>
        /// <param name="elementSelector">An async transform function to produce a result element value from each element.</param>
        /// <param name="comparer">An <see cref="IEqualityComparer{T}"/> to compare keys.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>A <see cref="Promise{T}"/> whose result will be a <see cref="Dictionary{TKey, TValue}"/> that contains values of type <typeparamref name="TSource"/> selected from the source sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="elementSelector"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="keySelector"/> produces duplicate keys for two elements.</exception>
        public static Promise<Dictionary<TKey, TElement>> ToDictionaryAsync<TSource, TKey, TElement>(this AsyncEnumerable<TSource> source,
            Func<TSource, Promise<TKey>> keySelector, Func<TSource, Promise<TElement>> elementSelector, IEqualityComparer<TKey> comparer = null, CancelationToken cancelationToken = default)
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(elementSelector, nameof(elementSelector), 1);

            return ToDictionaryHelper<TKey, TElement>.ToDictionaryAsyncCoreAwaitKeyElement(
                source.GetAsyncEnumerator(cancelationToken),
                Internal.PromiseRefBase.DelegateWrapper.Create(keySelector),
                Internal.PromiseRefBase.DelegateWrapper.Create(elementSelector),
                comparer);
        }

        /// <summary>
        /// Creates a <see cref="Dictionary{TKey, TValue}"/> from an async-enumerable sequence using the specified <paramref name="comparer"/> to compare keys.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TElement">The type of the key returned by <paramref name="elementSelector"/>.</typeparam>
        /// <typeparam name="TKeyCapture">The type of the captured value that will be passed to <paramref name="keySelector"/>.</typeparam>
        /// <param name="source">An async-enumerable sequence to create a <see cref="Dictionary{TKey, TValue}"/> for.</param>
        /// <param name="captureKeyValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">An async function  to extract a key from each element.</param>
        /// <param name="elementSelector">An async transform function to produce a result element value from each element.</param>
        /// <param name="comparer">An <see cref="IEqualityComparer{T}"/> to compare keys.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>A <see cref="Promise{T}"/> whose result will be a <see cref="Dictionary{TKey, TValue}"/> that contains values of type <typeparamref name="TSource"/> selected from the source sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="elementSelector"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="keySelector"/> produces duplicate keys for two elements.</exception>
        public static Promise<Dictionary<TKey, TElement>> ToDictionaryAsync<TSource, TKey, TElement, TKeyCapture>(this AsyncEnumerable<TSource> source,
            TKeyCapture captureKeyValue, Func<TKeyCapture, TSource, Promise<TKey>> keySelector, Func<TSource, Promise<TElement>> elementSelector, IEqualityComparer<TKey> comparer = null, CancelationToken cancelationToken = default)
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(elementSelector, nameof(elementSelector), 1);

            return ToDictionaryHelper<TKey, TElement>.ToDictionaryAsyncCoreAwaitKeyElement(
                source.GetAsyncEnumerator(cancelationToken),
                Internal.PromiseRefBase.DelegateWrapper.Create(captureKeyValue, keySelector),
                Internal.PromiseRefBase.DelegateWrapper.Create(elementSelector),
                comparer);
        }

        /// <summary>
        /// Creates a <see cref="Dictionary{TKey, TValue}"/> from a configured async-enumerable sequence using the specified <paramref name="comparer"/> to compare keys.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TElement">The type of the key returned by <paramref name="elementSelector"/>.</typeparam>
        /// <param name="configuredSource">A configured async-enumerable sequence to create a <see cref="Dictionary{TKey, TValue}"/> for.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="elementSelector">An async transform function to produce a result element value from each element.</param>
        /// <param name="comparer">An <see cref="IEqualityComparer{T}"/> to compare keys.</param>
        /// <returns>A <see cref="Promise{T}"/> whose result will be a <see cref="Dictionary{TKey, TValue}"/> that contains values of type <typeparamref name="TSource"/> selected from the source sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="elementSelector"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="keySelector"/> produces duplicate keys for two elements.</exception>
        public static Promise<Dictionary<TKey, TElement>> ToDictionaryAsync<TSource, TKey, TElement>(this in ConfiguredAsyncEnumerable<TSource> configuredSource,
            Func<TSource, TKey> keySelector, Func<TSource, Promise<TElement>> elementSelector, IEqualityComparer<TKey> comparer = null)
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(elementSelector, nameof(elementSelector), 1);

            return ToDictionaryHelper<TKey, TElement>.ToDictionaryAsyncCoreAwaitElement(
                configuredSource.GetAsyncEnumerator(),
                Internal.PromiseRefBase.DelegateWrapper.Create(keySelector),
                Internal.PromiseRefBase.DelegateWrapper.Create(elementSelector),
                comparer);
        }

        /// <summary>
        /// Creates a <see cref="Dictionary{TKey, TValue}"/> from a configured async-enumerable sequence using the specified <paramref name="comparer"/> to compare keys.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TElement">The type of the key returned by <paramref name="elementSelector"/>.</typeparam>
        /// <typeparam name="TKeyCapture">The type of the captured value that will be passed to <paramref name="keySelector"/>.</typeparam>
        /// <param name="configuredSource">A configured async-enumerable sequence to create a <see cref="Dictionary{TKey, TValue}"/> for.</param>
        /// <param name="captureKeyValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="elementSelector">An async transform function to produce a result element value from each element.</param>
        /// <param name="comparer">An <see cref="IEqualityComparer{T}"/> to compare keys.</param>
        /// <returns>A <see cref="Promise{T}"/> whose result will be a <see cref="Dictionary{TKey, TValue}"/> that contains values of type <typeparamref name="TSource"/> selected from the source sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="elementSelector"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="keySelector"/> produces duplicate keys for two elements.</exception>
        public static Promise<Dictionary<TKey, TElement>> ToDictionaryAsync<TSource, TKey, TElement, TKeyCapture>(this in ConfiguredAsyncEnumerable<TSource> configuredSource,
            TKeyCapture captureKeyValue, Func<TKeyCapture, TSource, TKey> keySelector, Func<TSource, Promise<TElement>> elementSelector, IEqualityComparer<TKey> comparer = null)
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(elementSelector, nameof(elementSelector), 1);

            return ToDictionaryHelper<TKey, TElement>.ToDictionaryAsyncCoreAwaitElement(
                configuredSource.GetAsyncEnumerator(),
                Internal.PromiseRefBase.DelegateWrapper.Create(captureKeyValue, keySelector),
                Internal.PromiseRefBase.DelegateWrapper.Create(elementSelector),
                comparer);
        }

        /// <summary>
        /// Creates a <see cref="Dictionary{TKey, TValue}"/> from a configured async-enumerable sequence using the specified <paramref name="comparer"/> to compare keys.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TElement">The type of the key returned by <paramref name="elementSelector"/>.</typeparam>
        /// <param name="configuredSource">A configured async-enumerable sequence to create a <see cref="Dictionary{TKey, TValue}"/> for.</param>
        /// <param name="keySelector">An async function  to extract a key from each element.</param>
        /// <param name="elementSelector">An async transform function to produce a result element value from each element.</param>
        /// <param name="comparer">An <see cref="IEqualityComparer{T}"/> to compare keys.</param>
        /// <returns>A <see cref="Promise{T}"/> whose result will be a <see cref="Dictionary{TKey, TValue}"/> that contains values of type <typeparamref name="TSource"/> selected from the source sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="elementSelector"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="keySelector"/> produces duplicate keys for two elements.</exception>
        public static Promise<Dictionary<TKey, TElement>> ToDictionaryAsync<TSource, TKey, TElement>(this in ConfiguredAsyncEnumerable<TSource> configuredSource,
            Func<TSource, Promise<TKey>> keySelector, Func<TSource, Promise<TElement>> elementSelector, IEqualityComparer<TKey> comparer = null)
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(elementSelector, nameof(elementSelector), 1);

            return ToDictionaryHelper<TKey, TElement>.ToDictionaryAsyncCoreAwaitKeyElement(
                configuredSource.GetAsyncEnumerator(),
                Internal.PromiseRefBase.DelegateWrapper.Create(keySelector),
                Internal.PromiseRefBase.DelegateWrapper.Create(elementSelector),
                comparer);
        }

        /// <summary>
        /// Creates a <see cref="Dictionary{TKey, TValue}"/> from a configured async-enumerable sequence using the specified <paramref name="comparer"/> to compare keys.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TElement">The type of the key returned by <paramref name="elementSelector"/>.</typeparam>
        /// <typeparam name="TKeyCapture">The type of the captured value that will be passed to <paramref name="keySelector"/>.</typeparam>
        /// <param name="configuredSource">A configured async-enumerable sequence to create a <see cref="Dictionary{TKey, TValue}"/> for.</param>
        /// <param name="captureKeyValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">An async function  to extract a key from each element.</param>
        /// <param name="elementSelector">An async transform function to produce a result element value from each element.</param>
        /// <param name="comparer">An <see cref="IEqualityComparer{T}"/> to compare keys.</param>
        /// <returns>A <see cref="Promise{T}"/> whose result will be a <see cref="Dictionary{TKey, TValue}"/> that contains values of type <typeparamref name="TSource"/> selected from the source sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="elementSelector"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="keySelector"/> produces duplicate keys for two elements.</exception>
        public static Promise<Dictionary<TKey, TElement>> ToDictionaryAsync<TSource, TKey, TElement, TKeyCapture>(this in ConfiguredAsyncEnumerable<TSource> configuredSource,
            TKeyCapture captureKeyValue, Func<TKeyCapture, TSource, Promise<TKey>> keySelector, Func<TSource, Promise<TElement>> elementSelector, IEqualityComparer<TKey> comparer = null)
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(elementSelector, nameof(elementSelector), 1);

            return ToDictionaryHelper<TKey, TElement>.ToDictionaryAsyncCoreAwaitKeyElement(
                configuredSource.GetAsyncEnumerator(),
                Internal.PromiseRefBase.DelegateWrapper.Create(captureKeyValue, keySelector),
                Internal.PromiseRefBase.DelegateWrapper.Create(elementSelector),
                comparer);
        }

        /// <summary>
        /// Creates a <see cref="Dictionary{TKey, TValue}"/> from an async-enumerable sequence using the specified <paramref name="comparer"/> to compare keys.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TElement">The type of the key returned by <paramref name="elementSelector"/>.</typeparam>
        /// <typeparam name="TElementCapture">The type of the captured value that will be passed to <paramref name="elementSelector"/>.</typeparam>
        /// <param name="source">An async-enumerable sequence to create a <see cref="Dictionary{TKey, TValue}"/> for.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="captureElementValue">The extra value that will be passed to <paramref name="elementSelector"/>.</param>
        /// <param name="elementSelector">An async transform function to produce a result element value from each element.</param>
        /// <param name="comparer">An <see cref="IEqualityComparer{T}"/> to compare keys.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>A <see cref="Promise{T}"/> whose result will be a <see cref="Dictionary{TKey, TValue}"/> that contains values of type <typeparamref name="TSource"/> selected from the source sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="elementSelector"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="keySelector"/> produces duplicate keys for two elements.</exception>
        public static Promise<Dictionary<TKey, TElement>> ToDictionaryAsync<TSource, TKey, TElement, TElementCapture>(this AsyncEnumerable<TSource> source,
            Func<TSource, TKey> keySelector, TElementCapture captureElementValue, Func<TElementCapture, TSource, Promise<TElement>> elementSelector, IEqualityComparer<TKey> comparer = null, CancelationToken cancelationToken = default)
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(elementSelector, nameof(elementSelector), 1);

            return ToDictionaryHelper<TKey, TElement>.ToDictionaryAsyncCoreAwaitElement(
                source.GetAsyncEnumerator(cancelationToken),
                Internal.PromiseRefBase.DelegateWrapper.Create(keySelector),
                Internal.PromiseRefBase.DelegateWrapper.Create(captureElementValue, elementSelector),
                comparer);
        }

        /// <summary>
        /// Creates a <see cref="Dictionary{TKey, TValue}"/> from an async-enumerable sequence using the specified <paramref name="comparer"/> to compare keys.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TElement">The type of the key returned by <paramref name="elementSelector"/>.</typeparam>
        /// <typeparam name="TKeyCapture">The type of the captured value that will be passed to <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TElementCapture">The type of the captured value that will be passed to <paramref name="elementSelector"/>.</typeparam>
        /// <param name="source">An async-enumerable sequence to create a <see cref="Dictionary{TKey, TValue}"/> for.</param>
        /// <param name="captureKeyValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="captureElementValue">The extra value that will be passed to <paramref name="elementSelector"/>.</param>
        /// <param name="elementSelector">An async transform function to produce a result element value from each element.</param>
        /// <param name="comparer">An <see cref="IEqualityComparer{T}"/> to compare keys.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>A <see cref="Promise{T}"/> whose result will be a <see cref="Dictionary{TKey, TValue}"/> that contains values of type <typeparamref name="TSource"/> selected from the source sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="elementSelector"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="keySelector"/> produces duplicate keys for two elements.</exception>
        public static Promise<Dictionary<TKey, TElement>> ToDictionaryAsync<TSource, TKey, TElement, TKeyCapture, TElementCapture>(this AsyncEnumerable<TSource> source,
            TKeyCapture captureKeyValue, Func<TKeyCapture, TSource, TKey> keySelector, TElementCapture captureElementValue, Func<TElementCapture, TSource, Promise<TElement>> elementSelector, IEqualityComparer<TKey> comparer = null, CancelationToken cancelationToken = default)
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(elementSelector, nameof(elementSelector), 1);

            return ToDictionaryHelper<TKey, TElement>.ToDictionaryAsyncCoreAwaitElement(
                source.GetAsyncEnumerator(cancelationToken),
                Internal.PromiseRefBase.DelegateWrapper.Create(captureKeyValue, keySelector),
                Internal.PromiseRefBase.DelegateWrapper.Create(captureElementValue, elementSelector),
                comparer);
        }

        /// <summary>
        /// Creates a <see cref="Dictionary{TKey, TValue}"/> from an async-enumerable sequence using the specified <paramref name="comparer"/> to compare keys.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TElement">The type of the key returned by <paramref name="elementSelector"/>.</typeparam>
        /// <typeparam name="TElementCapture">The type of the captured value that will be passed to <paramref name="elementSelector"/>.</typeparam>
        /// <param name="source">An async-enumerable sequence to create a <see cref="Dictionary{TKey, TValue}"/> for.</param>
        /// <param name="keySelector">An async function  to extract a key from each element.</param>
        /// <param name="captureElementValue">The extra value that will be passed to <paramref name="elementSelector"/>.</param>
        /// <param name="elementSelector">An async transform function to produce a result element value from each element.</param>
        /// <param name="comparer">An <see cref="IEqualityComparer{T}"/> to compare keys.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>A <see cref="Promise{T}"/> whose result will be a <see cref="Dictionary{TKey, TValue}"/> that contains values of type <typeparamref name="TSource"/> selected from the source sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="elementSelector"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="keySelector"/> produces duplicate keys for two elements.</exception>
        public static Promise<Dictionary<TKey, TElement>> ToDictionaryAsync<TSource, TKey, TElement, TElementCapture>(this AsyncEnumerable<TSource> source,
            Func<TSource, Promise<TKey>> keySelector, TElementCapture captureElementValue, Func<TElementCapture, TSource, Promise<TElement>> elementSelector, IEqualityComparer<TKey> comparer = null, CancelationToken cancelationToken = default)
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(elementSelector, nameof(elementSelector), 1);

            return ToDictionaryHelper<TKey, TElement>.ToDictionaryAsyncCoreAwaitKeyElement(
                source.GetAsyncEnumerator(cancelationToken),
                Internal.PromiseRefBase.DelegateWrapper.Create(keySelector),
                Internal.PromiseRefBase.DelegateWrapper.Create(captureElementValue, elementSelector),
                comparer);
        }

        /// <summary>
        /// Creates a <see cref="Dictionary{TKey, TValue}"/> from an async-enumerable sequence using the specified <paramref name="comparer"/> to compare keys.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TElement">The type of the key returned by <paramref name="elementSelector"/>.</typeparam>
        /// <typeparam name="TKeyCapture">The type of the captured value that will be passed to <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TElementCapture">The type of the captured value that will be passed to <paramref name="elementSelector"/>.</typeparam>
        /// <param name="source">An async-enumerable sequence to create a <see cref="Dictionary{TKey, TValue}"/> for.</param>
        /// <param name="captureKeyValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">An async function  to extract a key from each element.</param>
        /// <param name="captureElementValue">The extra value that will be passed to <paramref name="elementSelector"/>.</param>
        /// <param name="elementSelector">An async transform function to produce a result element value from each element.</param>
        /// <param name="comparer">An <see cref="IEqualityComparer{T}"/> to compare keys.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>A <see cref="Promise{T}"/> whose result will be a <see cref="Dictionary{TKey, TValue}"/> that contains values of type <typeparamref name="TSource"/> selected from the source sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="elementSelector"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="keySelector"/> produces duplicate keys for two elements.</exception>
        public static Promise<Dictionary<TKey, TElement>> ToDictionaryAsync<TSource, TKey, TElement, TKeyCapture, TElementCapture>(this AsyncEnumerable<TSource> source,
            TKeyCapture captureKeyValue, Func<TKeyCapture, TSource, Promise<TKey>> keySelector, TElementCapture captureElementValue, Func<TElementCapture, TSource, Promise<TElement>> elementSelector, IEqualityComparer<TKey> comparer = null, CancelationToken cancelationToken = default)
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(elementSelector, nameof(elementSelector), 1);

            return ToDictionaryHelper<TKey, TElement>.ToDictionaryAsyncCoreAwaitKeyElement(
                source.GetAsyncEnumerator(cancelationToken),
                Internal.PromiseRefBase.DelegateWrapper.Create(captureKeyValue, keySelector),
                Internal.PromiseRefBase.DelegateWrapper.Create(captureElementValue, elementSelector),
                comparer);
        }

        /// <summary>
        /// Creates a <see cref="Dictionary{TKey, TValue}"/> from a configured async-enumerable sequence using the specified <paramref name="comparer"/> to compare keys.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TElement">The type of the key returned by <paramref name="elementSelector"/>.</typeparam>
        /// <typeparam name="TElementCapture">The type of the captured value that will be passed to <paramref name="elementSelector"/>.</typeparam>
        /// <param name="configuredSource">A configured async-enumerable sequence to create a <see cref="Dictionary{TKey, TValue}"/> for.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="captureElementValue">The extra value that will be passed to <paramref name="elementSelector"/>.</param>
        /// <param name="elementSelector">An async transform function to produce a result element value from each element.</param>
        /// <param name="comparer">An <see cref="IEqualityComparer{T}"/> to compare keys.</param>
        /// <returns>A <see cref="Promise{T}"/> whose result will be a <see cref="Dictionary{TKey, TValue}"/> that contains values of type <typeparamref name="TSource"/> selected from the source sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="elementSelector"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="keySelector"/> produces duplicate keys for two elements.</exception>
        public static Promise<Dictionary<TKey, TElement>> ToDictionaryAsync<TSource, TKey, TElement, TElementCapture>(this in ConfiguredAsyncEnumerable<TSource> configuredSource,
            Func<TSource, TKey> keySelector, TElementCapture captureElementValue, Func<TElementCapture, TSource, Promise<TElement>> elementSelector, IEqualityComparer<TKey> comparer = null)
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(elementSelector, nameof(elementSelector), 1);

            return ToDictionaryHelper<TKey, TElement>.ToDictionaryAsyncCoreAwaitElement(
                configuredSource.GetAsyncEnumerator(),
                Internal.PromiseRefBase.DelegateWrapper.Create(keySelector),
                Internal.PromiseRefBase.DelegateWrapper.Create(captureElementValue, elementSelector),
                comparer);
        }

        /// <summary>
        /// Creates a <see cref="Dictionary{TKey, TValue}"/> from a configured async-enumerable sequence using the specified <paramref name="comparer"/> to compare keys.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TElement">The type of the key returned by <paramref name="elementSelector"/>.</typeparam>
        /// <typeparam name="TKeyCapture">The type of the captured value that will be passed to <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TElementCapture">The type of the captured value that will be passed to <paramref name="elementSelector"/>.</typeparam>
        /// <param name="configuredSource">A configured async-enumerable sequence to create a <see cref="Dictionary{TKey, TValue}"/> for.</param>
        /// <param name="captureKeyValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="captureElementValue">The extra value that will be passed to <paramref name="elementSelector"/>.</param>
        /// <param name="elementSelector">An async transform function to produce a result element value from each element.</param>
        /// <param name="comparer">An <see cref="IEqualityComparer{T}"/> to compare keys.</param>
        /// <returns>A <see cref="Promise{T}"/> whose result will be a <see cref="Dictionary{TKey, TValue}"/> that contains values of type <typeparamref name="TSource"/> selected from the source sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="elementSelector"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="keySelector"/> produces duplicate keys for two elements.</exception>
        public static Promise<Dictionary<TKey, TElement>> ToDictionaryAsync<TSource, TKey, TElement, TKeyCapture, TElementCapture>(this in ConfiguredAsyncEnumerable<TSource> configuredSource,
            TKeyCapture captureKeyValue, Func<TKeyCapture, TSource, TKey> keySelector, TElementCapture captureElementValue, Func<TElementCapture, TSource, Promise<TElement>> elementSelector, IEqualityComparer<TKey> comparer = null)
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(elementSelector, nameof(elementSelector), 1);

            return ToDictionaryHelper<TKey, TElement>.ToDictionaryAsyncCoreAwaitElement(
                configuredSource.GetAsyncEnumerator(),
                Internal.PromiseRefBase.DelegateWrapper.Create(captureKeyValue, keySelector),
                Internal.PromiseRefBase.DelegateWrapper.Create(captureElementValue, elementSelector),
                comparer);
        }

        /// <summary>
        /// Creates a <see cref="Dictionary{TKey, TValue}"/> from a configured async-enumerable sequence using the specified <paramref name="comparer"/> to compare keys.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TElement">The type of the key returned by <paramref name="elementSelector"/>.</typeparam>
        /// <typeparam name="TElementCapture">The type of the captured value that will be passed to <paramref name="elementSelector"/>.</typeparam>
        /// <param name="configuredSource">A configured async-enumerable sequence to create a <see cref="Dictionary{TKey, TValue}"/> for.</param>
        /// <param name="keySelector">An async function  to extract a key from each element.</param>
        /// <param name="captureElementValue">The extra value that will be passed to <paramref name="elementSelector"/>.</param>
        /// <param name="elementSelector">An async transform function to produce a result element value from each element.</param>
        /// <param name="comparer">An <see cref="IEqualityComparer{T}"/> to compare keys.</param>
        /// <returns>A <see cref="Promise{T}"/> whose result will be a <see cref="Dictionary{TKey, TValue}"/> that contains values of type <typeparamref name="TSource"/> selected from the source sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="elementSelector"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="keySelector"/> produces duplicate keys for two elements.</exception>
        public static Promise<Dictionary<TKey, TElement>> ToDictionaryAsync<TSource, TKey, TElement, TElementCapture>(this in ConfiguredAsyncEnumerable<TSource> configuredSource,
            Func<TSource, Promise<TKey>> keySelector, TElementCapture captureElementValue, Func<TElementCapture, TSource, Promise<TElement>> elementSelector, IEqualityComparer<TKey> comparer = null)
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(elementSelector, nameof(elementSelector), 1);

            return ToDictionaryHelper<TKey, TElement>.ToDictionaryAsyncCoreAwaitKeyElement(
                configuredSource.GetAsyncEnumerator(),
                Internal.PromiseRefBase.DelegateWrapper.Create(keySelector),
                Internal.PromiseRefBase.DelegateWrapper.Create(captureElementValue, elementSelector),
                comparer);
        }

        /// <summary>
        /// Creates a <see cref="Dictionary{TKey, TValue}"/> from a configured async-enumerable sequence using the specified <paramref name="comparer"/> to compare keys.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TElement">The type of the key returned by <paramref name="elementSelector"/>.</typeparam>
        /// <typeparam name="TKeyCapture">The type of the captured value that will be passed to <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TElementCapture">The type of the captured value that will be passed to <paramref name="elementSelector"/>.</typeparam>
        /// <param name="configuredSource">A configured async-enumerable sequence to create a <see cref="Dictionary{TKey, TValue}"/> for.</param>
        /// <param name="captureKeyValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">An async function  to extract a key from each element.</param>
        /// <param name="captureElementValue">The extra value that will be passed to <paramref name="elementSelector"/>.</param>
        /// <param name="elementSelector">An async transform function to produce a result element value from each element.</param>
        /// <param name="comparer">An <see cref="IEqualityComparer{T}"/> to compare keys.</param>
        /// <returns>A <see cref="Promise{T}"/> whose result will be a <see cref="Dictionary{TKey, TValue}"/> that contains values of type <typeparamref name="TSource"/> selected from the source sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="elementSelector"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="keySelector"/> produces duplicate keys for two elements.</exception>
        public static Promise<Dictionary<TKey, TElement>> ToDictionaryAsync<TSource, TKey, TElement, TKeyCapture, TElementCapture>(this in ConfiguredAsyncEnumerable<TSource> configuredSource,
            TKeyCapture captureKeyValue, Func<TKeyCapture, TSource, Promise<TKey>> keySelector, TElementCapture captureElementValue, Func<TElementCapture, TSource, Promise<TElement>> elementSelector, IEqualityComparer<TKey> comparer = null)
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(elementSelector, nameof(elementSelector), 1);

            return ToDictionaryHelper<TKey, TElement>.ToDictionaryAsyncCoreAwaitKeyElement(
                configuredSource.GetAsyncEnumerator(),
                Internal.PromiseRefBase.DelegateWrapper.Create(captureKeyValue, keySelector),
                Internal.PromiseRefBase.DelegateWrapper.Create(captureElementValue, elementSelector),
                comparer);
        }

        private static class ToDictionaryHelper<TKey, TElement>
        {
            internal static async Promise<Dictionary<TKey, TElement>> ToDictionaryAsyncCore<TSource, TKeySelector, TElementSelector>(
                AsyncEnumerator<TSource> asyncEnumerator,
                TKeySelector keySelector,
                TElementSelector elementSelector,
                IEqualityComparer<TKey> comparer)
                where TKeySelector : Internal.IFunc<TSource, TKey>
                where TElementSelector : Internal.IFunc<TSource, TElement>
            {
                try
                {
                    var dictionary = new Dictionary<TKey, TElement>(comparer);
                    while (await asyncEnumerator.MoveNextAsync())
                    {
                        var element = asyncEnumerator.Current;
                        dictionary.Add(keySelector.Invoke(element), elementSelector.Invoke(element));
                    }
                    return dictionary;
                }
                finally
                {
                    await asyncEnumerator.DisposeAsync();
                }
            }

            internal static async Promise<Dictionary<TKey, TElement>> ToDictionaryAsyncCoreAwaitKey<TSource, TKeySelector, TElementSelector>(
                AsyncEnumerator<TSource> asyncEnumerator,
                TKeySelector keySelector,
                TElementSelector elementSelector,
                IEqualityComparer<TKey> comparer)
                where TKeySelector : Internal.IFunc<TSource, Promise<TKey>>
                where TElementSelector : Internal.IFunc<TSource, TElement>
            {
                try
                {
                    var dictionary = new Dictionary<TKey, TElement>(comparer);
                    while (await asyncEnumerator.MoveNextAsync())
                    {
                        var element = asyncEnumerator.Current;
                        dictionary.Add(await keySelector.Invoke(element), elementSelector.Invoke(element));
                    }
                    return dictionary;
                }
                finally
                {
                    await asyncEnumerator.DisposeAsync();
                }
            }

            internal static async Promise<Dictionary<TKey, TElement>> ToDictionaryAsyncCore<TSource, TKeySelector, TElementSelector>(
                ConfiguredAsyncEnumerable<TSource>.Enumerator asyncEnumerator,
                TKeySelector keySelector,
                TElementSelector elementSelector,
                IEqualityComparer<TKey> comparer)
                where TKeySelector : Internal.IFunc<TSource, TKey>
                where TElementSelector : Internal.IFunc<TSource, TElement>
            {
                try
                {
                    var dictionary = new Dictionary<TKey, TElement>(comparer);
                    while (await asyncEnumerator.MoveNextAsync())
                    {
                        var element = asyncEnumerator.Current;
                        dictionary.Add(keySelector.Invoke(element), elementSelector.Invoke(element));
                    }
                    return dictionary;
                }
                finally
                {
                    await asyncEnumerator.DisposeAsync();
                }
            }

            internal static async Promise<Dictionary<TKey, TElement>> ToDictionaryAsyncCoreAwaitKey<TSource, TKeySelector, TElementSelector>(
                ConfiguredAsyncEnumerable<TSource>.Enumerator asyncEnumerator,
                TKeySelector keySelector,
                TElementSelector elementSelector,
                IEqualityComparer<TKey> comparer)
                where TKeySelector : Internal.IFunc<TSource, Promise<TKey>>
                where TElementSelector : Internal.IFunc<TSource, TElement>
            {
                try
                {
                    var dictionary = new Dictionary<TKey, TElement>(comparer);
                    while (await asyncEnumerator.MoveNextAsync())
                    {
                        var element = asyncEnumerator.Current;
                        dictionary.Add(await keySelector.Invoke(element), elementSelector.Invoke(element));
                    }
                    return dictionary;
                }
                finally
                {
                    await asyncEnumerator.DisposeAsync();
                }
            }

            internal static async Promise<Dictionary<TKey, TElement>> ToDictionaryAsyncCoreAwaitElement<TSource, TKeySelector, TElementSelector>(
                AsyncEnumerator<TSource> asyncEnumerator,
                TKeySelector keySelector,
                TElementSelector elementSelector,
                IEqualityComparer<TKey> comparer)
                where TKeySelector : Internal.IFunc<TSource, TKey>
                where TElementSelector : Internal.IFunc<TSource, Promise<TElement>>
            {
                try
                {
                    var dictionary = new Dictionary<TKey, TElement>(comparer);
                    while (await asyncEnumerator.MoveNextAsync())
                    {
                        var element = asyncEnumerator.Current;
                        dictionary.Add(keySelector.Invoke(element), await elementSelector.Invoke(element));
                    }
                    return dictionary;
                }
                finally
                {
                    await asyncEnumerator.DisposeAsync();
                }
            }

            internal static async Promise<Dictionary<TKey, TElement>> ToDictionaryAsyncCoreAwaitKeyElement<TSource, TKeySelector, TElementSelector>(
                AsyncEnumerator<TSource> asyncEnumerator,
                TKeySelector keySelector,
                TElementSelector elementSelector,
                IEqualityComparer<TKey> comparer)
                where TKeySelector : Internal.IFunc<TSource, Promise<TKey>>
                where TElementSelector : Internal.IFunc<TSource, Promise<TElement>>
            {
                try
                {
                    var dictionary = new Dictionary<TKey, TElement>(comparer);
                    while (await asyncEnumerator.MoveNextAsync())
                    {
                        var element = asyncEnumerator.Current;
                        dictionary.Add(await keySelector.Invoke(element), await elementSelector.Invoke(element));
                    }
                    return dictionary;
                }
                finally
                {
                    await asyncEnumerator.DisposeAsync();
                }
            }

            internal static async Promise<Dictionary<TKey, TElement>> ToDictionaryAsyncCoreAwaitElement<TSource, TKeySelector, TElementSelector>(
                ConfiguredAsyncEnumerable<TSource>.Enumerator asyncEnumerator,
                TKeySelector keySelector,
                TElementSelector elementSelector,
                IEqualityComparer<TKey> comparer)
                where TKeySelector : Internal.IFunc<TSource, TKey>
                where TElementSelector : Internal.IFunc<TSource, Promise<TElement>>
            {
                try
                {
                    var dictionary = new Dictionary<TKey, TElement>(comparer);
                    while (await asyncEnumerator.MoveNextAsync())
                    {
                        var element = asyncEnumerator.Current;
                        dictionary.Add(keySelector.Invoke(element), await elementSelector.Invoke(element));
                    }
                    return dictionary;
                }
                finally
                {
                    await asyncEnumerator.DisposeAsync();
                }
            }

            internal static async Promise<Dictionary<TKey, TElement>> ToDictionaryAsyncCoreAwaitKeyElement<TSource, TKeySelector, TElementSelector>(
                ConfiguredAsyncEnumerable<TSource>.Enumerator asyncEnumerator,
                TKeySelector keySelector,
                TElementSelector elementSelector,
                IEqualityComparer<TKey> comparer)
                where TKeySelector : Internal.IFunc<TSource, Promise<TKey>>
                where TElementSelector : Internal.IFunc<TSource, Promise<TElement>>
            {
                try
                {
                    var dictionary = new Dictionary<TKey, TElement>(comparer);
                    while (await asyncEnumerator.MoveNextAsync())
                    {
                        var element = asyncEnumerator.Current;
                        dictionary.Add(await keySelector.Invoke(element), await elementSelector.Invoke(element));
                    }
                    return dictionary;
                }
                finally
                {
                    await asyncEnumerator.DisposeAsync();
                }
            }
        }
        #endregion KeyElementSelector
    }
#endif // CSHARP_7_3_OR_NEWER
}