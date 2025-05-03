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
        /// Asynchronously returns the minimum element of an async-enumerable sequence according to the specified key selector function using the default comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TKey">The type of key to compare elements by.</typeparam>
        /// <param name="source">The sequence to return the minimum element of.</param>
        /// <param name="keySelector">A function to extract the key for each element.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>A <see cref="Promise{T}"/> resulting in the minimum element.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="source"/> is empty and <typeparamref name="TSource"/> is not nullable.</exception>
        /// <remarks>
        /// If <typeparamref name="TSource"/> is a nullable type and the source sequence is empty, this method yields <see langword="null"/>.
        /// <para/> If <paramref name="keySelector"/> results in <see langword="null"/> for all elements, the first element will be returned.
        /// </remarks>
        public static Promise<TSource> MinByAsync<TSource, TKey>(this AsyncEnumerable<TSource> source, Func<TSource, TKey> keySelector, CancelationToken cancelationToken = default)
            => MinByAsync(source, keySelector, Comparer<TKey>.Default, cancelationToken);

        /// <summary>
        /// Asynchronously returns the minimum element of an async-enumerable sequence according to the specified key selector function using the specified comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TKey">The type of key to compare elements by.</typeparam>
        /// <typeparam name="TComparer">The type of the comparer.</typeparam>
        /// <param name="source">The sequence to return the minimum element of.</param>
        /// <param name="keySelector">A function to extract the key for each element.</param>
        /// <param name="comparer">A comparer to compare values.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>A <see cref="Promise{T}"/> resulting in the minimum element.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="comparer"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="source"/> is empty and <typeparamref name="TSource"/> is not nullable.</exception>
        /// <remarks>
        /// If <typeparamref name="TSource"/> is a nullable type and the source sequence is empty, this method yields <see langword="null"/>.
        /// <para/> If <paramref name="keySelector"/> results in <see langword="null"/> for all elements, the first element will be returned.
        /// </remarks>
        public static Promise<TSource> MinByAsync<TSource, TKey, TComparer>(this AsyncEnumerable<TSource> source, Func<TSource, TKey> keySelector, TComparer comparer, CancelationToken cancelationToken = default)
            where TComparer : IComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return MinByHelper<TKey>.MinByAsync(source.GetAsyncEnumerator(cancelationToken), DelegateWrapper.Create(keySelector), comparer);
        }

        /// <summary>
        /// Asynchronously returns the minimum element of an async-enumerable sequence according to the specified key selector function using the default comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TKey">The type of key to compare elements by.</typeparam>
        /// <typeparam name="TCapture">The type of the captured value that will be passed to <paramref name="keySelector"/>.</typeparam>
        /// <param name="source">The sequence to return the minimum element of.</param>
        /// <param name="captureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">A function to extract the key for each element.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>A <see cref="Promise{T}"/> resulting in the minimum element.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="source"/> is empty and <typeparamref name="TSource"/> is not nullable.</exception>
        /// <remarks>
        /// If <typeparamref name="TSource"/> is a nullable type and the source sequence is empty, this method yields <see langword="null"/>.
        /// <para/> If <paramref name="keySelector"/> results in <see langword="null"/> for all elements, the first element will be returned.
        /// </remarks>
        public static Promise<TSource> MinByAsync<TSource, TKey, TCapture>(this AsyncEnumerable<TSource> source, TCapture captureValue, Func<TCapture, TSource, TKey> keySelector, CancelationToken cancelationToken = default)
            => MinByAsync(source, captureValue, keySelector, Comparer<TKey>.Default, cancelationToken);

        /// <summary>
        /// Asynchronously returns the minimum element of an async-enumerable sequence according to the specified key selector function using the specified comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TKey">The type of key to compare elements by.</typeparam>
        /// <typeparam name="TCapture">The type of the captured value that will be passed to <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TComparer">The type of the comparer.</typeparam>
        /// <param name="source">The sequence to return the minimum element of.</param>
        /// <param name="captureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">A function to extract the key for each element.</param>
        /// <param name="comparer">A comparer to compare values.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>A <see cref="Promise{T}"/> resulting in the minimum element.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="comparer"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="source"/> is empty and <typeparamref name="TSource"/> is not nullable.</exception>
        /// <remarks>
        /// If <typeparamref name="TSource"/> is a nullable type and the source sequence is empty, this method yields <see langword="null"/>.
        /// <para/> If <paramref name="keySelector"/> results in <see langword="null"/> for all elements, the first element will be returned.
        /// </remarks>
        public static Promise<TSource> MinByAsync<TSource, TKey, TCapture, TComparer>(this AsyncEnumerable<TSource> source, TCapture captureValue, Func<TCapture, TSource, TKey> keySelector, TComparer comparer, CancelationToken cancelationToken = default)
            where TComparer : IComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return MinByHelper<TKey>.MinByAsync(source.GetAsyncEnumerator(cancelationToken), DelegateWrapper.Create(captureValue, keySelector), comparer);
        }

        /// <summary>
        /// Asynchronously returns the minimum element of an async-enumerable sequence according to the specified key selector function using the default comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TKey">The type of key to compare elements by.</typeparam>
        /// <param name="source">The sequence to return the minimum element of.</param>
        /// <param name="keySelector">An async function to extract the key for each element.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>A <see cref="Promise{T}"/> resulting in the minimum element.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="source"/> is empty and <typeparamref name="TSource"/> is not nullable.</exception>
        /// <remarks>
        /// If <typeparamref name="TSource"/> is a nullable type and the source sequence is empty, this method yields <see langword="null"/>.
        /// <para/> If <paramref name="keySelector"/> results in <see langword="null"/> for all elements, the first element will be returned.
        /// </remarks>
        public static Promise<TSource> MinByAsync<TSource, TKey>(this AsyncEnumerable<TSource> source, Func<TSource, Promise<TKey>> keySelector, CancelationToken cancelationToken = default)
            => MinByAsync(source, keySelector, Comparer<TKey>.Default, cancelationToken);

        /// <summary>
        /// Asynchronously returns the minimum element of an async-enumerable sequence according to the specified key selector function using the specified comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TKey">The type of key to compare elements by.</typeparam>
        /// <typeparam name="TComparer">The type of the comparer.</typeparam>
        /// <param name="source">The sequence to return the minimum element of.</param>
        /// <param name="keySelector">An async function to extract the key for each element.</param>
        /// <param name="comparer">A comparer to compare values.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>A <see cref="Promise{T}"/> resulting in the minimum element.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="comparer"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="source"/> is empty and <typeparamref name="TSource"/> is not nullable.</exception>
        /// <remarks>
        /// If <typeparamref name="TSource"/> is a nullable type and the source sequence is empty, this method yields <see langword="null"/>.
        /// <para/> If <paramref name="keySelector"/> results in <see langword="null"/> for all elements, the first element will be returned.
        /// </remarks>
        public static Promise<TSource> MinByAsync<TSource, TKey, TComparer>(this AsyncEnumerable<TSource> source, Func<TSource, Promise<TKey>> keySelector, TComparer comparer, CancelationToken cancelationToken = default)
            where TComparer : IComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return MinByHelper<TKey>.MinByAwaitAsync(source.GetAsyncEnumerator(cancelationToken), DelegateWrapper.Create(keySelector), comparer);
        }

        /// <summary>
        /// Asynchronously returns the minimum element of an async-enumerable sequence according to the specified key selector function using the default comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TKey">The type of key to compare elements by.</typeparam>
        /// <typeparam name="TCapture">The type of the captured value that will be passed to <paramref name="keySelector"/>.</typeparam>
        /// <param name="source">The sequence to return the minimum element of.</param>
        /// <param name="captureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">An async function to extract the key for each element.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>A <see cref="Promise{T}"/> resulting in the minimum element.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="source"/> is empty and <typeparamref name="TSource"/> is not nullable.</exception>
        /// <remarks>
        /// If <typeparamref name="TSource"/> is a nullable type and the source sequence is empty, this method yields <see langword="null"/>.
        /// <para/> If <paramref name="keySelector"/> results in <see langword="null"/> for all elements, the first element will be returned.
        /// </remarks>
        public static Promise<TSource> MinByAsync<TSource, TKey, TCapture>(this AsyncEnumerable<TSource> source, TCapture captureValue, Func<TCapture, TSource, Promise<TKey>> keySelector, CancelationToken cancelationToken = default)
            => MinByAsync(source, captureValue, keySelector, Comparer<TKey>.Default, cancelationToken);

        /// <summary>
        /// Asynchronously returns the minimum element of an async-enumerable sequence according to the specified key selector function using the specified comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TKey">The type of key to compare elements by.</typeparam>
        /// <typeparam name="TCapture">The type of the captured value that will be passed to <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TComparer">The type of the comparer.</typeparam>
        /// <param name="source">The sequence to return the minimum element of.</param>
        /// <param name="captureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">An async function to extract the key for each element.</param>
        /// <param name="comparer">A comparer to compare values.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>A <see cref="Promise{T}"/> resulting in the minimum element.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="comparer"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="source"/> is empty and <typeparamref name="TSource"/> is not nullable.</exception>
        /// <remarks>
        /// If <typeparamref name="TSource"/> is a nullable type and the source sequence is empty, this method yields <see langword="null"/>.
        /// <para/> If <paramref name="keySelector"/> results in <see langword="null"/> for all elements, the first element will be returned.
        /// </remarks>
        public static Promise<TSource> MinByAsync<TSource, TKey, TCapture, TComparer>(this AsyncEnumerable<TSource> source, TCapture captureValue, Func<TCapture, TSource, Promise<TKey>> keySelector, TComparer comparer, CancelationToken cancelationToken = default)
            where TComparer : IComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return MinByHelper<TKey>.MinByAwaitAsync(source.GetAsyncEnumerator(cancelationToken), DelegateWrapper.Create(captureValue, keySelector), comparer);
        }

        /// <summary>
        /// Asynchronously returns the minimum element of a configured async-enumerable sequence according to the specified key selector function using the default comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="configuredSource"/>.</typeparam>
        /// <typeparam name="TKey">The type of key to compare elements by.</typeparam>
        /// <param name="configuredSource">The configured async-enumerable sequence to return the minimum element of.</param>
        /// <param name="keySelector">A function to extract the key for each element.</param>
        /// <returns>A <see cref="Promise{T}"/> resulting in the minimum element.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="configuredSource"/> is empty and <typeparamref name="TSource"/> is not nullable.</exception>
        /// <remarks>
        /// If <typeparamref name="TSource"/> is a nullable type and the source sequence is empty, this method yields <see langword="null"/>.
        /// <para/> If <paramref name="keySelector"/> results in <see langword="null"/> for all elements, the first element will be returned.
        /// </remarks>
        public static Promise<TSource> MinByAsync<TSource, TKey>(this in ConfiguredAsyncEnumerable<TSource> configuredSource, Func<TSource, TKey> keySelector)
            => MinByAsync(configuredSource, keySelector, Comparer<TKey>.Default);

        /// <summary>
        /// Asynchronously returns the minimum element of a configured async-enumerable sequence according to the specified key selector function using the specified comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="configuredSource"/>.</typeparam>
        /// <typeparam name="TKey">The type of key to compare elements by.</typeparam>
        /// <typeparam name="TComparer">The type of the comparer.</typeparam>
        /// <param name="configuredSource">The configured async-enumerable sequence to return the minimum element of.</param>
        /// <param name="keySelector">A function to extract the key for each element.</param>
        /// <param name="comparer">A comparer to compare values.</param>
        /// <returns>A <see cref="Promise{T}"/> resulting in the minimum element.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="comparer"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="configuredSource"/> is empty and <typeparamref name="TSource"/> is not nullable.</exception>
        /// <remarks>
        /// If <typeparamref name="TSource"/> is a nullable type and the source sequence is empty, this method yields <see langword="null"/>.
        /// <para/> If <paramref name="keySelector"/> results in <see langword="null"/> for all elements, the first element will be returned.
        /// </remarks>
        public static Promise<TSource> MinByAsync<TSource, TKey, TComparer>(this in ConfiguredAsyncEnumerable<TSource> configuredSource, Func<TSource, TKey> keySelector, TComparer comparer)
            where TComparer : IComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return MinByHelper<TKey>.MinByAsync(configuredSource.GetAsyncEnumerator(), DelegateWrapper.Create(keySelector), comparer);
        }

        /// <summary>
        /// Asynchronously returns the minimum element of a configured async-enumerable sequence according to the specified key selector function using the default comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="configuredSource"/>.</typeparam>
        /// <typeparam name="TKey">The type of key to compare elements by.</typeparam>
        /// <typeparam name="TCapture">The type of the captured value that will be passed to <paramref name="keySelector"/>.</typeparam>
        /// <param name="configuredSource">The configured async-enumerable sequence to return the minimum element of.</param>
        /// <param name="captureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">A function to extract the key for each element.</param>
        /// <returns>A <see cref="Promise{T}"/> resulting in the minimum element.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="configuredSource"/> is empty and <typeparamref name="TSource"/> is not nullable.</exception>
        /// <remarks>
        /// If <typeparamref name="TSource"/> is a nullable type and the source sequence is empty, this method yields <see langword="null"/>.
        /// <para/> If <paramref name="keySelector"/> results in <see langword="null"/> for all elements, the first element will be returned.
        /// </remarks>
        public static Promise<TSource> MinByAsync<TSource, TKey, TCapture>(this in ConfiguredAsyncEnumerable<TSource> configuredSource, TCapture captureValue, Func<TCapture, TSource, TKey> keySelector)
            => MinByAsync(configuredSource, captureValue, keySelector, Comparer<TKey>.Default);

        /// <summary>
        /// Asynchronously returns the minimum element of a configured async-enumerable sequence according to the specified key selector function using the specified comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="configuredSource"/>.</typeparam>
        /// <typeparam name="TKey">The type of key to compare elements by.</typeparam>
        /// <typeparam name="TCapture">The type of the captured value that will be passed to <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TComparer">The type of the comparer.</typeparam>
        /// <param name="configuredSource">The configured async-enumerable sequence to return the minimum element of.</param>
        /// <param name="captureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">A function to extract the key for each element.</param>
        /// <param name="comparer">A comparer to compare values.</param>
        /// <returns>A <see cref="Promise{T}"/> resulting in the minimum element.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="comparer"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="configuredSource"/> is empty and <typeparamref name="TSource"/> is not nullable.</exception>
        /// <remarks>
        /// If <typeparamref name="TSource"/> is a nullable type and the source sequence is empty, this method yields <see langword="null"/>.
        /// <para/> If <paramref name="keySelector"/> results in <see langword="null"/> for all elements, the first element will be returned.
        /// </remarks>
        public static Promise<TSource> MinByAsync<TSource, TKey, TCapture, TComparer>(this in ConfiguredAsyncEnumerable<TSource> configuredSource, TCapture captureValue, Func<TCapture, TSource, TKey> keySelector, TComparer comparer)
            where TComparer : IComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return MinByHelper<TKey>.MinByAsync(configuredSource.GetAsyncEnumerator(), DelegateWrapper.Create(captureValue, keySelector), comparer);
        }

        /// <summary>
        /// Asynchronously returns the minimum element of a configured async-enumerable sequence according to the specified key selector function using the default comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="configuredSource"/>.</typeparam>
        /// <typeparam name="TKey">The type of key to compare elements by.</typeparam>
        /// <param name="configuredSource">The configured async-enumerable sequence to return the minimum element of.</param>
        /// <param name="keySelector">An async function to extract the key for each element.</param>
        /// <returns>A <see cref="Promise{T}"/> resulting in the minimum element.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="configuredSource"/> is empty and <typeparamref name="TSource"/> is not nullable.</exception>
        /// <remarks>
        /// If <typeparamref name="TSource"/> is a nullable type and the source sequence is empty, this method yields <see langword="null"/>.
        /// <para/> If <paramref name="keySelector"/> results in <see langword="null"/> for all elements, the first element will be returned.
        /// </remarks>
        public static Promise<TSource> MinByAsync<TSource, TKey>(this in ConfiguredAsyncEnumerable<TSource> configuredSource, Func<TSource, Promise<TKey>> keySelector)
            => MinByAsync(configuredSource, keySelector, Comparer<TKey>.Default);

        /// <summary>
        /// Asynchronously returns the minimum element of a configured async-enumerable sequence according to the specified key selector function using the specified comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="configuredSource"/>.</typeparam>
        /// <typeparam name="TKey">The type of key to compare elements by.</typeparam>
        /// <typeparam name="TComparer">The type of the comparer.</typeparam>
        /// <param name="configuredSource">The configured async-enumerable sequence to return the minimum element of.</param>
        /// <param name="keySelector">An async function to extract the key for each element.</param>
        /// <param name="comparer">A comparer to compare values.</param>
        /// <returns>A <see cref="Promise{T}"/> resulting in the minimum element.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="comparer"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="configuredSource"/> is empty and <typeparamref name="TSource"/> is not nullable.</exception>
        /// <remarks>
        /// If <typeparamref name="TSource"/> is a nullable type and the source sequence is empty, this method yields <see langword="null"/>.
        /// <para/> If <paramref name="keySelector"/> results in <see langword="null"/> for all elements, the first element will be returned.
        /// </remarks>
        public static Promise<TSource> MinByAsync<TSource, TKey, TComparer>(this in ConfiguredAsyncEnumerable<TSource> configuredSource, Func<TSource, Promise<TKey>> keySelector, TComparer comparer)
            where TComparer : IComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return MinByHelper<TKey>.MinByAwaitAsync(configuredSource.GetAsyncEnumerator(), DelegateWrapper.Create(keySelector), comparer);
        }

        /// <summary>
        /// Asynchronously returns the minimum element of a configured async-enumerable sequence according to the specified key selector function using the default comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="configuredSource"/>.</typeparam>
        /// <typeparam name="TKey">The type of key to compare elements by.</typeparam>
        /// <typeparam name="TCapture">The type of the captured value that will be passed to <paramref name="keySelector"/>.</typeparam>
        /// <param name="configuredSource">The configured async-enumerable sequence to return the minimum element of.</param>
        /// <param name="captureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">An async function to extract the key for each element.</param>
        /// <returns>A <see cref="Promise{T}"/> resulting in the minimum element.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="configuredSource"/> is empty and <typeparamref name="TSource"/> is not nullable.</exception>
        /// <remarks>
        /// If <typeparamref name="TSource"/> is a nullable type and the source sequence is empty, this method yields <see langword="null"/>.
        /// <para/> If <paramref name="keySelector"/> results in <see langword="null"/> for all elements, the first element will be returned.
        /// </remarks>
        public static Promise<TSource> MinByAsync<TSource, TKey, TCapture>(this in ConfiguredAsyncEnumerable<TSource> configuredSource, TCapture captureValue, Func<TCapture, TSource, Promise<TKey>> keySelector)
            => MinByAsync(configuredSource, captureValue, keySelector, Comparer<TKey>.Default);

        /// <summary>
        /// Asynchronously returns the minimum element of a configured async-enumerable sequence according to the specified key selector function using the specified comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="configuredSource"/>.</typeparam>
        /// <typeparam name="TKey">The type of key to compare elements by.</typeparam>
        /// <typeparam name="TCapture">The type of the captured value that will be passed to <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TComparer">The type of the comparer.</typeparam>
        /// <param name="configuredSource">The configured async-enumerable sequence to return the minimum element of.</param>
        /// <param name="captureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">An async function to extract the key for each element.</param>
        /// <param name="comparer">A comparer to compare values.</param>
        /// <returns>A <see cref="Promise{T}"/> resulting in the minimum element.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="comparer"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="configuredSource"/> is empty and <typeparamref name="TSource"/> is not nullable.</exception>
        /// <remarks>
        /// If <typeparamref name="TSource"/> is a nullable type and the source sequence is empty, this method yields <see langword="null"/>.
        /// <para/> If <paramref name="keySelector"/> results in <see langword="null"/> for all elements, the first element will be returned.
        /// </remarks>
        public static Promise<TSource> MinByAsync<TSource, TKey, TCapture, TComparer>(this in ConfiguredAsyncEnumerable<TSource> configuredSource, TCapture captureValue, Func<TCapture, TSource, Promise<TKey>> keySelector, TComparer comparer)
            where TComparer : IComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return MinByHelper<TKey>.MinByAwaitAsync(configuredSource.GetAsyncEnumerator(), DelegateWrapper.Create(captureValue, keySelector), comparer);
        }

        private static class MinByHelper<TKey>
        {
            internal static async Promise<TSource> MinByAsync<TSource, TKeySelector, TComparer>(AsyncEnumerator<TSource> asyncEnumerator, TKeySelector keySelector, TComparer comparer)
                where TComparer : IComparer<TKey>
                where TKeySelector : IFunc<TSource, TKey>
            {
                try
                {
                    if (!await asyncEnumerator.MoveNextAsync())
                    {
                        // Check if nullable type. This check is eliminated by the JIT.
                        if (default(TSource) == null)
                        {
                            return default;
                        }
                        else
                        {
                            throw new InvalidOperationException("source must contain at least 1 element.", Internal.GetFormattedStacktrace(1));
                        }
                    }

                    TSource value = asyncEnumerator.Current;
                    TKey key = keySelector.Invoke(value);

                    // Check if nullable type. This check is eliminated by the JIT.
                    if (default(TKey) == null)
                    {
                        if (key == null)
                        {
                            TSource firstValue = value;

                            do
                            {
                                if (!await asyncEnumerator.MoveNextAsync())
                                {
                                    // All keys are null, surface the first element.
                                    return firstValue;
                                }

                                value = asyncEnumerator.Current;
                                key = keySelector.Invoke(value);
                            }
                            while (key == null);
                        }

                        while (await asyncEnumerator.MoveNextAsync())
                        {
                            TSource nextValue = asyncEnumerator.Current;
                            TKey nextKey = keySelector.Invoke(nextValue);
                            if (nextKey != null && comparer.Compare(nextKey, key) < 0)
                            {
                                key = nextKey;
                                value = nextValue;
                            }
                        }
                    }
                    else
                    {
                        while (await asyncEnumerator.MoveNextAsync())
                        {
                            TSource nextValue = asyncEnumerator.Current;
                            TKey nextKey = keySelector.Invoke(nextValue);
                            if (comparer.Compare(nextKey, key) < 0)
                            {
                                key = nextKey;
                                value = nextValue;
                            }
                        }
                    }

                    return value;
                }
                finally
                {
                    await asyncEnumerator.DisposeAsync();
                }
            }

            internal static async Promise<TSource> MinByAwaitAsync<TSource, TKeySelector, TComparer>(AsyncEnumerator<TSource> asyncEnumerator, TKeySelector keySelector, TComparer comparer)
                where TComparer : IComparer<TKey>
                where TKeySelector : IFunc<TSource, Promise<TKey>>
            {
                try
                {
                    if (!await asyncEnumerator.MoveNextAsync())
                    {
                        // Check if nullable type. This check is eliminated by the JIT.
                        if (default(TSource) == null)
                        {
                            return default;
                        }
                        else
                        {
                            throw new InvalidOperationException("source must contain at least 1 element.", Internal.GetFormattedStacktrace(1));
                        }
                    }

                    TSource value = asyncEnumerator.Current;
                    TKey key = await keySelector.Invoke(value);

                    // Check if nullable type. This check is eliminated by the JIT.
                    if (default(TKey) == null)
                    {
                        if (key == null)
                        {
                            TSource firstValue = value;

                            do
                            {
                                if (!await asyncEnumerator.MoveNextAsync())
                                {
                                    // All keys are null, surface the first element.
                                    return firstValue;
                                }

                                value = asyncEnumerator.Current;
                                key = await keySelector.Invoke(value);
                            }
                            while (key == null);
                        }

                        while (await asyncEnumerator.MoveNextAsync())
                        {
                            TSource nextValue = asyncEnumerator.Current;
                            TKey nextKey = await keySelector.Invoke(nextValue);
                            if (nextKey != null && comparer.Compare(nextKey, key) < 0)
                            {
                                key = nextKey;
                                value = nextValue;
                            }
                        }
                    }
                    else
                    {
                        while (await asyncEnumerator.MoveNextAsync())
                        {
                            TSource nextValue = asyncEnumerator.Current;
                            TKey nextKey = await keySelector.Invoke(nextValue);
                            if (comparer.Compare(nextKey, key) < 0)
                            {
                                key = nextKey;
                                value = nextValue;
                            }
                        }
                    }

                    return value;
                }
                finally
                {
                    await asyncEnumerator.DisposeAsync();
                }
            }

            internal static async Promise<TSource> MinByAsync<TSource, TKeySelector, TComparer>(ConfiguredAsyncEnumerable<TSource>.Enumerator asyncEnumerator, TKeySelector keySelector, TComparer comparer)
                where TComparer : IComparer<TKey>
                where TKeySelector : IFunc<TSource, TKey>
            {
                try
                {
                    if (!await asyncEnumerator.MoveNextAsync())
                    {
                        // Check if nullable type. This check is eliminated by the JIT.
                        if (default(TSource) == null)
                        {
                            return default;
                        }
                        else
                        {
                            throw new InvalidOperationException("source must contain at least 1 element.", Internal.GetFormattedStacktrace(1));
                        }
                    }

                    TSource value = asyncEnumerator.Current;
                    TKey key = keySelector.Invoke(value);

                    // Check if nullable type. This check is eliminated by the JIT.
                    if (default(TKey) == null)
                    {
                        if (key == null)
                        {
                            TSource firstValue = value;

                            do
                            {
                                if (!await asyncEnumerator.MoveNextAsync())
                                {
                                    // All keys are null, surface the first element.
                                    return firstValue;
                                }

                                value = asyncEnumerator.Current;
                                key = keySelector.Invoke(value);
                            }
                            while (key == null);
                        }

                        while (await asyncEnumerator.MoveNextAsync())
                        {
                            TSource nextValue = asyncEnumerator.Current;
                            TKey nextKey = keySelector.Invoke(nextValue);
                            if (nextKey != null && comparer.Compare(nextKey, key) < 0)
                            {
                                key = nextKey;
                                value = nextValue;
                            }
                        }
                    }
                    else
                    {
                        while (await asyncEnumerator.MoveNextAsync())
                        {
                            TSource nextValue = asyncEnumerator.Current;
                            TKey nextKey = keySelector.Invoke(nextValue);
                            if (comparer.Compare(nextKey, key) < 0)
                            {
                                key = nextKey;
                                value = nextValue;
                            }
                        }
                    }

                    return value;
                }
                finally
                {
                    await asyncEnumerator.DisposeAsync();
                }
            }

            internal static async Promise<TSource> MinByAwaitAsync<TSource, TKeySelector, TComparer>(ConfiguredAsyncEnumerable<TSource>.Enumerator asyncEnumerator, TKeySelector keySelector, TComparer comparer)
                where TComparer : IComparer<TKey>
                where TKeySelector : IFunc<TSource, Promise<TKey>>
            {
                try
                {
                    if (!await asyncEnumerator.MoveNextAsync())
                    {
                        // Check if nullable type. This check is eliminated by the JIT.
                        if (default(TSource) == null)
                        {
                            return default;
                        }
                        else
                        {
                            throw new InvalidOperationException("source must contain at least 1 element.", Internal.GetFormattedStacktrace(1));
                        }
                    }

                    TSource value = asyncEnumerator.Current;
                    TKey key = await keySelector.Invoke(value).ConfigureAwait(asyncEnumerator.ContinuationOptions);

                    // Check if nullable type. This check is eliminated by the JIT.
                    if (default(TKey) == null)
                    {
                        if (key == null)
                        {
                            TSource firstValue = value;

                            do
                            {
                                if (!await asyncEnumerator.MoveNextAsync())
                                {
                                    // All keys are null, surface the first element.
                                    return firstValue;
                                }

                                value = asyncEnumerator.Current;
                                key = await keySelector.Invoke(value).ConfigureAwait(asyncEnumerator.ContinuationOptions);
                            }
                            while (key == null);
                        }

                        while (await asyncEnumerator.MoveNextAsync())
                        {
                            TSource nextValue = asyncEnumerator.Current;
                            TKey nextKey = await keySelector.Invoke(nextValue).ConfigureAwait(asyncEnumerator.ContinuationOptions);
                            if (nextKey != null && comparer.Compare(nextKey, key) < 0)
                            {
                                key = nextKey;
                                value = nextValue;
                            }
                        }
                    }
                    else
                    {
                        while (await asyncEnumerator.MoveNextAsync())
                        {
                            TSource nextValue = asyncEnumerator.Current;
                            TKey nextKey = await keySelector.Invoke(nextValue).ConfigureAwait(asyncEnumerator.ContinuationOptions);
                            if (comparer.Compare(nextKey, key) < 0)
                            {
                                key = nextKey;
                                value = nextValue;
                            }
                        }
                    }

                    return value;
                }
                finally
                {
                    await asyncEnumerator.DisposeAsync();
                }
            }
        }
    }
}