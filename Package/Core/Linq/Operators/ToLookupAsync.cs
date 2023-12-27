using Proto.Promises.Async.CompilerServices;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Proto.Promises.Linq
{
#if CSHARP_7_3_OR_NEWER
    partial class AsyncEnumerable
    {
        #region AsyncEnumerable
        /// <summary>
        /// Creates a lookup from an async-enumerable sequence according to a specified key selector function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the lookup key computed for each element in the source sequence.</typeparam>
        /// <param name="source">An async-enumerable sequence to create a lookup for.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>A <see cref="Promise{T}"/> whose result will be a lookup mapping unique key values onto the corresponding source sequence's elements.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        public static Promise<ILookup<TKey, TSource>> ToLookupAsync<TSource, TKey>(
            this AsyncEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            CancelationToken cancelationToken = default)
            => ToLookupAsync(source, keySelector, comparer: null, cancelationToken);

        /// <summary>
        /// Creates a lookup from an async-enumerable sequence according to a specified key selector function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to the key selector.</typeparam>
        /// <typeparam name="TKey">The type of the lookup key computed for each element in the source sequence.</typeparam>
        /// <param name="source">An async-enumerable sequence to create a lookup for.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>A <see cref="Promise{T}"/> whose result will be a lookup mapping unique key values onto the corresponding source sequence's elements.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        public static Promise<ILookup<TKey, TSource>> ToLookupAsync<TSource, TCaptureKey, TKey>(
            this AsyncEnumerable<TSource> source,
            TCaptureKey keyCaptureValue,
            Func<TCaptureKey, TSource, TKey> keySelector,
            CancelationToken cancelationToken = default)
            => ToLookupAsync(source, keyCaptureValue, keySelector, comparer: null, cancelationToken);

        /// <summary>
        /// Creates a lookup from an async-enumerable sequence according to a specified key selector function, and a comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the lookup key computed for each element in the source sequence.</typeparam>
        /// <param name="source">An async-enumerable sequence to create a lookup for.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="comparer">An equality comparer to compare keys. If null, the default equality comparer will be used.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>A <see cref="Promise{T}"/> whose result will be a lookup mapping unique key values onto the corresponding source sequence's elements.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        public static Promise<ILookup<TKey, TSource>> ToLookupAsync<TSource, TKey>(
            this AsyncEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            IEqualityComparer<TKey> comparer,
            CancelationToken cancelationToken = default)
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);

            return Internal.Lookup<TKey, TSource>.CreateAsync(source.GetAsyncEnumerator(cancelationToken), Internal.PromiseRefBase.DelegateWrapper.Create(keySelector), comparer);
        }

        /// <summary>
        /// Creates a lookup from an async-enumerable sequence according to a specified key selector function, and a comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to the key selector.</typeparam>
        /// <typeparam name="TKey">The type of the lookup key computed for each element in the source sequence.</typeparam>
        /// <param name="source">An async-enumerable sequence to create a lookup for.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="comparer">An equality comparer to compare keys. If null, the default equality comparer will be used.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>A <see cref="Promise{T}"/> whose result will be a lookup mapping unique key values onto the corresponding source sequence's elements.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        public static Promise<ILookup<TKey, TSource>> ToLookupAsync<TSource, TCaptureKey, TKey>(
            this AsyncEnumerable<TSource> source,
            TCaptureKey keyCaptureValue,
            Func<TCaptureKey, TSource, TKey> keySelector,
            IEqualityComparer<TKey> comparer,
            CancelationToken cancelationToken = default)
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);

            return Internal.Lookup<TKey, TSource>.CreateAsync(source.GetAsyncEnumerator(cancelationToken), Internal.PromiseRefBase.DelegateWrapper.Create(keyCaptureValue, keySelector), comparer);
        }

        /// <summary>
        /// Creates a lookup from an async-enumerable sequence by invoking a key-selector function on each element and awaiting the result.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the lookup key computed for each element in the source sequence.</typeparam>
        /// <param name="source">An async-enumerable sequence to create a lookup for.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from each element.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>A <see cref="Promise{T}"/> whose result will be a lookup mapping unique key values onto the corresponding source sequence's elements.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        public static Promise<ILookup<TKey, TSource>> ToLookupAsync<TSource, TKey>(
            this AsyncEnumerable<TSource> source,
            Func<TSource, Promise<TKey>> keySelector,
            CancelationToken cancelationToken = default)
            => ToLookupAsync<TSource, TKey>(source, keySelector, comparer: null, cancelationToken);

        /// <summary>
        /// Creates a lookup from an async-enumerable sequence by invoking a key-selector function on each element and awaiting the result.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to the key selector.</typeparam>
        /// <typeparam name="TKey">The type of the lookup key computed for each element in the source sequence.</typeparam>
        /// <param name="source">An async-enumerable sequence to create a lookup for.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from each element.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>A <see cref="Promise{T}"/> whose result will be a lookup mapping unique key values onto the corresponding source sequence's elements.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        public static Promise<ILookup<TKey, TSource>> ToLookupAsync<TSource, TCaptureKey, TKey>(
            this AsyncEnumerable<TSource> source,
            TCaptureKey keyCaptureValue,
            Func<TCaptureKey, TSource, Promise<TKey>> keySelector,
            CancelationToken cancelationToken = default)
            => ToLookupAsync<TSource, TCaptureKey, TKey>(source, keyCaptureValue, keySelector, comparer: null, cancelationToken);

        /// <summary>
        /// Creates a lookup from an async-enumerable sequence by invoking a key-selector function on each element and awaiting the result.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the lookup key computed for each element in the source sequence.</typeparam>
        /// <param name="source">An async-enumerable sequence to create a lookup for.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from each element.</param>
        /// <param name="comparer">An equality comparer to compare keys. If null, the default equality comparer will be used.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>A <see cref="Promise{T}"/> whose result will be a lookup mapping unique key values onto the corresponding source sequence's elements.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        public static Promise<ILookup<TKey, TSource>> ToLookupAsync<TSource, TKey>(
            this AsyncEnumerable<TSource> source,
            Func<TSource, Promise<TKey>> keySelector,
            IEqualityComparer<TKey> comparer,
            CancelationToken cancelationToken = default)
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);

            return Internal.Lookup<TKey, TSource>.CreateAwaitAsync(source.GetAsyncEnumerator(cancelationToken), Internal.PromiseRefBase.DelegateWrapper.Create(keySelector), comparer);
        }

        /// <summary>
        /// Creates a lookup from an async-enumerable sequence by invoking a key-selector function on each element and awaiting the result.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to the key selector.</typeparam>
        /// <typeparam name="TKey">The type of the lookup key computed for each element in the source sequence.</typeparam>
        /// <param name="source">An async-enumerable sequence to create a lookup for.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from each element.</param>
        /// <param name="comparer">An equality comparer to compare keys. If null, the default equality comparer will be used.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>A <see cref="Promise{T}"/> whose result will be a lookup mapping unique key values onto the corresponding source sequence's elements.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        public static Promise<ILookup<TKey, TSource>> ToLookupAsync<TSource, TCaptureKey, TKey>(
            this AsyncEnumerable<TSource> source,
            TCaptureKey keyCaptureValue,
            Func<TCaptureKey, TSource, Promise<TKey>> keySelector,
            IEqualityComparer<TKey> comparer,
            CancelationToken cancelationToken = default)
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);

            return Internal.Lookup<TKey, TSource>.CreateAwaitAsync(source.GetAsyncEnumerator(cancelationToken), Internal.PromiseRefBase.DelegateWrapper.Create(keyCaptureValue, keySelector), comparer);
        }

        /// <summary>
        /// Creates a lookup from an async-enumerable sequence according to a specified key selector function, and an element selector function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the lookup key computed for each element in the source sequence.</typeparam>
        /// <typeparam name="TElement">The type of the lookup value computed for each element in the source sequence.</typeparam>
        /// <param name="source">An async-enumerable sequence to create a lookup for.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="elementSelector">A transform function to produce a result element value from each element.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>A <see cref="Promise{T}"/> whose result will be a lookup mapping unique key values onto the corresponding source sequence's elements.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="elementSelector"/> is null.</exception>
        public static Promise<ILookup<TKey, TElement>> ToLookupAsync<TSource, TKey, TElement>(
            this AsyncEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            Func<TSource, TElement> elementSelector,
            CancelationToken cancelationToken = default)
            => ToLookupAsync(source, keySelector, elementSelector, comparer: null, cancelationToken);

        /// <summary>
        /// Creates a lookup from an async-enumerable sequence according to a specified key selector function, and an element selector function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to the key selector.</typeparam>
        /// <typeparam name="TKey">The type of the lookup key computed for each element in the source sequence.</typeparam>
        /// <typeparam name="TElement">The type of the lookup value computed for each element in the source sequence.</typeparam>
        /// <param name="source">An async-enumerable sequence to create a lookup for.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="elementSelector">A transform function to produce a result element value from each element.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>A <see cref="Promise{T}"/> whose result will be a lookup mapping unique key values onto the corresponding source sequence's elements.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="elementSelector"/> is null.</exception>
        public static Promise<ILookup<TKey, TElement>> ToLookupAsync<TSource, TCaptureKey, TKey, TElement>(
            this AsyncEnumerable<TSource> source,
            TCaptureKey keyCaptureValue,
            Func<TCaptureKey, TSource, TKey> keySelector,
            Func<TSource, TElement> elementSelector,
            CancelationToken cancelationToken = default)
            => ToLookupAsync(source, keyCaptureValue, keySelector, elementSelector, comparer: null, cancelationToken);

        /// <summary>
        /// Creates a lookup from an async-enumerable sequence according to a specified key selector function, and an element selector function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the lookup key computed for each element in the source sequence.</typeparam>
        /// <typeparam name="TCaptureElement">The type of the captured value that will be passed to the element selector.</typeparam>
        /// <typeparam name="TElement">The type of the lookup value computed for each element in the source sequence.</typeparam>
        /// <param name="source">An async-enumerable sequence to create a lookup for.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="elementCaptureValue">The extra value that will be passed to <paramref name="elementSelector"/>.</param>
        /// <param name="elementSelector">A transform function to produce a result element value from each element.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>A <see cref="Promise{T}"/> whose result will be a lookup mapping unique key values onto the corresponding source sequence's elements.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="elementSelector"/> is null.</exception>
        public static Promise<ILookup<TKey, TElement>> ToLookupAsync<TSource, TKey, TCaptureElement, TElement>(
            this AsyncEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            TCaptureElement elementCaptureValue,
            Func<TCaptureElement, TSource, TElement> elementSelector,
            CancelationToken cancelationToken = default)
            => ToLookupAsync(source, keySelector, elementCaptureValue, elementSelector, comparer: null, cancelationToken);

        /// <summary>
        /// Creates a lookup from an async-enumerable sequence according to a specified key selector function, and an element selector function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to the key selector.</typeparam>
        /// <typeparam name="TKey">The type of the lookup key computed for each element in the source sequence.</typeparam>
        /// <typeparam name="TCaptureElement">The type of the captured value that will be passed to the element selector.</typeparam>
        /// <typeparam name="TElement">The type of the lookup value computed for each element in the source sequence.</typeparam>
        /// <param name="source">An async-enumerable sequence to create a lookup for.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="elementCaptureValue">The extra value that will be passed to <paramref name="elementSelector"/>.</param>
        /// <param name="elementSelector">A transform function to produce a result element value from each element.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>A <see cref="Promise{T}"/> whose result will be a lookup mapping unique key values onto the corresponding source sequence's elements.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="elementSelector"/> is null.</exception>
        public static Promise<ILookup<TKey, TElement>> ToLookupAsync<TSource, TCaptureKey, TKey, TCaptureElement, TElement>(
            this AsyncEnumerable<TSource> source,
            TCaptureKey keyCaptureValue,
            Func<TCaptureKey, TSource, TKey> keySelector,
            TCaptureElement elementCaptureValue,
            Func<TCaptureElement, TSource, TElement> elementSelector,
            CancelationToken cancelationToken = default)
            => ToLookupAsync(source, keyCaptureValue, keySelector, elementCaptureValue, elementSelector, comparer: null, cancelationToken);

        /// <summary>
        /// Creates a lookup from an async-enumerable sequence according to a specified key selector function, a comparer, and an element selector function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the lookup key computed for each element in the source sequence.</typeparam>
        /// <typeparam name="TElement">The type of the lookup value computed for each element in the source sequence.</typeparam>
        /// <param name="source">An async-enumerable sequence to create a lookup for.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="elementSelector">A transform function to produce a result element value from each element.</param>
        /// <param name="comparer">An equality comparer to compare keys. If null, the default equality comparer will be used.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>A <see cref="Promise{T}"/> whose result will be a lookup mapping unique key values onto the corresponding source sequence's elements.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="elementSelector"/> is null.</exception>
        public static Promise<ILookup<TKey, TElement>> ToLookupAsync<TSource, TKey, TElement>(
            this AsyncEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            Func<TSource, TElement> elementSelector,
            IEqualityComparer<TKey> comparer,
            CancelationToken cancelationToken = default)
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(elementSelector, nameof(elementSelector), 1);

            return Internal.Lookup<TKey, TElement>.CreateAsync(source.GetAsyncEnumerator(cancelationToken),
                Internal.PromiseRefBase.DelegateWrapper.Create(keySelector),
                Internal.PromiseRefBase.DelegateWrapper.Create(elementSelector),
                comparer);
        }

        /// <summary>
        /// Creates a lookup from an async-enumerable sequence according to a specified key selector function, a comparer, and an element selector function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to the key selector.</typeparam>
        /// <typeparam name="TKey">The type of the lookup key computed for each element in the source sequence.</typeparam>
        /// <typeparam name="TElement">The type of the lookup value computed for each element in the source sequence.</typeparam>
        /// <param name="source">An async-enumerable sequence to create a lookup for.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="elementSelector">A transform function to produce a result element value from each element.</param>
        /// <param name="comparer">An equality comparer to compare keys. If null, the default equality comparer will be used.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>A <see cref="Promise{T}"/> whose result will be a lookup mapping unique key values onto the corresponding source sequence's elements.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="elementSelector"/> is null.</exception>
        public static Promise<ILookup<TKey, TElement>> ToLookupAsync<TSource, TCaptureKey, TKey, TElement>(
            this AsyncEnumerable<TSource> source,
            TCaptureKey keyCaptureValue,
            Func<TCaptureKey, TSource, TKey> keySelector,
            Func<TSource, TElement> elementSelector,
            IEqualityComparer<TKey> comparer,
            CancelationToken cancelationToken = default)
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(elementSelector, nameof(elementSelector), 1);

            return Internal.Lookup<TKey, TElement>.CreateAsync(source.GetAsyncEnumerator(cancelationToken),
                Internal.PromiseRefBase.DelegateWrapper.Create(keyCaptureValue, keySelector),
                Internal.PromiseRefBase.DelegateWrapper.Create(elementSelector),
                comparer);
        }

        /// <summary>
        /// Creates a lookup from an async-enumerable sequence according to a specified key selector function, a comparer, and an element selector function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the lookup key computed for each element in the source sequence.</typeparam>
        /// <typeparam name="TCaptureElement">The type of the captured value that will be passed to the element selector.</typeparam>
        /// <typeparam name="TElement">The type of the lookup value computed for each element in the source sequence.</typeparam>
        /// <param name="source">An async-enumerable sequence to create a lookup for.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="elementCaptureValue">The extra value that will be passed to <paramref name="elementSelector"/>.</param>
        /// <param name="elementSelector">A transform function to produce a result element value from each element.</param>
        /// <param name="comparer">An equality comparer to compare keys. If null, the default equality comparer will be used.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>A <see cref="Promise{T}"/> whose result will be a lookup mapping unique key values onto the corresponding source sequence's elements.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="elementSelector"/> is null.</exception>
        public static Promise<ILookup<TKey, TElement>> ToLookupAsync<TSource, TKey, TCaptureElement, TElement>(
            this AsyncEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            TCaptureElement elementCaptureValue,
            Func<TCaptureElement, TSource, TElement> elementSelector,
            IEqualityComparer<TKey> comparer,
            CancelationToken cancelationToken = default)
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(elementSelector, nameof(elementSelector), 1);

            return Internal.Lookup<TKey, TElement>.CreateAsync(source.GetAsyncEnumerator(cancelationToken),
                Internal.PromiseRefBase.DelegateWrapper.Create(keySelector),
                Internal.PromiseRefBase.DelegateWrapper.Create(elementCaptureValue, elementSelector),
                comparer);
        }

        /// <summary>
        /// Creates a lookup from an async-enumerable sequence according to a specified key selector function, a comparer, and an element selector function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to the key selector.</typeparam>
        /// <typeparam name="TKey">The type of the lookup key computed for each element in the source sequence.</typeparam>
        /// <typeparam name="TCaptureElement">The type of the captured value that will be passed to the element selector.</typeparam>
        /// <typeparam name="TElement">The type of the lookup value computed for each element in the source sequence.</typeparam>
        /// <param name="source">An async-enumerable sequence to create a lookup for.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="elementCaptureValue">The extra value that will be passed to <paramref name="elementSelector"/>.</param>
        /// <param name="elementSelector">A transform function to produce a result element value from each element.</param>
        /// <param name="comparer">An equality comparer to compare keys. If null, the default equality comparer will be used.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>A <see cref="Promise{T}"/> whose result will be a lookup mapping unique key values onto the corresponding source sequence's elements.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="elementSelector"/> is null.</exception>
        public static Promise<ILookup<TKey, TElement>> ToLookupAsync<TSource, TCaptureKey, TKey, TCaptureElement, TElement>(
            this AsyncEnumerable<TSource> source,
            TCaptureKey keyCaptureValue,
            Func<TCaptureKey, TSource, TKey> keySelector,
            TCaptureElement elementCaptureValue,
            Func<TCaptureElement, TSource, TElement> elementSelector,
            IEqualityComparer<TKey> comparer,
            CancelationToken cancelationToken = default)
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(elementSelector, nameof(elementSelector), 1);

            return Internal.Lookup<TKey, TElement>.CreateAsync(source.GetAsyncEnumerator(cancelationToken),
                Internal.PromiseRefBase.DelegateWrapper.Create(keyCaptureValue, keySelector),
                Internal.PromiseRefBase.DelegateWrapper.Create(elementCaptureValue, elementSelector),
                comparer);
        }

        /// <summary>
        /// Creates a lookup from an async-enumerable sequence by invoking key and element selector functions on each source element and awaiting the results.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the lookup key computed for each element in the source sequence.</typeparam>
        /// <typeparam name="TElement">The type of the lookup value computed for each element in the source sequence.</typeparam>
        /// <param name="source">An async-enumerable sequence to create a lookup for.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from each element.</param>
        /// <param name="elementSelector">An asynchronous transform function to produce a result element value from each element.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>A <see cref="Promise{T}"/> whose result will be a lookup mapping unique key values onto the corresponding source sequence's elements.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="elementSelector"/> is null.</exception>
        public static Promise<ILookup<TKey, TElement>> ToLookupAsync<TSource, TKey, TElement>(
            this AsyncEnumerable<TSource> source,
            Func<TSource, Promise<TKey>> keySelector,
            Func<TSource, Promise<TElement>> elementSelector,
            CancelationToken cancelationToken = default)
            => ToLookupAsync<TSource, TKey, TElement>(source, keySelector, elementSelector, comparer: null, cancelationToken);

        /// <summary>
        /// Creates a lookup from an async-enumerable sequence by invoking key and element selector functions on each source element and awaiting the results.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to the key selector.</typeparam>
        /// <typeparam name="TKey">The type of the lookup key computed for each element in the source sequence.</typeparam>
        /// <typeparam name="TElement">The type of the lookup value computed for each element in the source sequence.</typeparam>
        /// <param name="source">An async-enumerable sequence to create a lookup for.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from each element.</param>
        /// <param name="elementSelector">An asynchronous transform function to produce a result element value from each element.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>A <see cref="Promise{T}"/> whose result will be a lookup mapping unique key values onto the corresponding source sequence's elements.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="elementSelector"/> is null.</exception>
        public static Promise<ILookup<TKey, TElement>> ToLookupAsync<TSource, TCaptureKey, TKey, TElement>(
            this AsyncEnumerable<TSource> source,
            TCaptureKey keyCaptureValue,
            Func<TCaptureKey, TSource, Promise<TKey>> keySelector,
            Func<TSource, Promise<TElement>> elementSelector,
            CancelationToken cancelationToken = default)
            => ToLookupAsync<TSource, TCaptureKey, TKey, TElement>(source, keyCaptureValue, keySelector, elementSelector, comparer: null, cancelationToken);

        /// <summary>
        /// Creates a lookup from an async-enumerable sequence by invoking key and element selector functions on each source element and awaiting the results.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the lookup key computed for each element in the source sequence.</typeparam>
        /// <typeparam name="TCaptureElement">The type of the captured value that will be passed to the element selector.</typeparam>
        /// <typeparam name="TElement">The type of the lookup value computed for each element in the source sequence.</typeparam>
        /// <param name="source">An async-enumerable sequence to create a lookup for.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from each element.</param>
        /// <param name="elementCaptureValue">The extra value that will be passed to <paramref name="elementSelector"/>.</param>
        /// <param name="elementSelector">An asynchronous transform function to produce a result element value from each element.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>A <see cref="Promise{T}"/> whose result will be a lookup mapping unique key values onto the corresponding source sequence's elements.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="elementSelector"/> is null.</exception>
        public static Promise<ILookup<TKey, TElement>> ToLookupAsync<TSource, TKey, TCaptureElement, TElement>(
            this AsyncEnumerable<TSource> source,
            Func<TSource, Promise<TKey>> keySelector,
            TCaptureElement elementCaptureValue,
            Func<TCaptureElement, TSource, Promise<TElement>> elementSelector,
            CancelationToken cancelationToken = default)
            => ToLookupAsync<TSource, TKey, TCaptureElement, TElement>(source, keySelector, elementCaptureValue, elementSelector, comparer: null, cancelationToken);

        /// <summary>
        /// Creates a lookup from an async-enumerable sequence by invoking key and element selector functions on each source element and awaiting the results.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to the key selector.</typeparam>
        /// <typeparam name="TKey">The type of the lookup key computed for each element in the source sequence.</typeparam>
        /// <typeparam name="TCaptureElement">The type of the captured value that will be passed to the element selector.</typeparam>
        /// <typeparam name="TElement">The type of the lookup value computed for each element in the source sequence.</typeparam>
        /// <param name="source">An async-enumerable sequence to create a lookup for.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from each element.</param>
        /// <param name="elementCaptureValue">The extra value that will be passed to <paramref name="elementSelector"/>.</param>
        /// <param name="elementSelector">An asynchronous transform function to produce a result element value from each element.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>A <see cref="Promise{T}"/> whose result will be a lookup mapping unique key values onto the corresponding source sequence's elements.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="elementSelector"/> is null.</exception>
        public static Promise<ILookup<TKey, TElement>> ToLookupAsync<TSource, TCaptureKey, TKey, TCaptureElement, TElement>(
            this AsyncEnumerable<TSource> source,
            TCaptureKey keyCaptureValue,
            Func<TCaptureKey, TSource, Promise<TKey>> keySelector,
            TCaptureElement elementCaptureValue,
            Func<TCaptureElement, TSource, Promise<TElement>> elementSelector,
            CancelationToken cancelationToken = default)
            => ToLookupAsync<TSource, TCaptureKey, TKey, TCaptureElement, TElement>(source, keyCaptureValue, keySelector, elementCaptureValue, elementSelector, comparer: null, cancelationToken);

        /// <summary>
        /// Creates a lookup from an async-enumerable sequence by invoking key and element selector functions on each source element and awaiting the results.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the lookup key computed for each element in the source sequence.</typeparam>
        /// <typeparam name="TElement">The type of the lookup value computed for each element in the source sequence.</typeparam>
        /// <param name="source">An async-enumerable sequence to create a lookup for.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from each element.</param>
        /// <param name="elementSelector">An asynchronous transform function to produce a result element value from each source element.</param>
        /// <param name="comparer">An equality comparer to compare keys. If null, the default equality comparer will be used.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>A <see cref="Promise{T}"/> whose result will be a lookup mapping unique key values onto the corresponding source sequence's elements.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="elementSelector"/> is null.</exception>
        public static Promise<ILookup<TKey, TElement>> ToLookupAsync<TSource, TKey, TElement>(
            this AsyncEnumerable<TSource> source,
            Func<TSource, Promise<TKey>> keySelector,
            Func<TSource, Promise<TElement>> elementSelector,
            IEqualityComparer<TKey> comparer,
            CancelationToken cancelationToken = default)
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(elementSelector, nameof(elementSelector), 1);

            return Internal.Lookup<TKey, TElement>.CreateAwaitAsync(source.GetAsyncEnumerator(cancelationToken),
                Internal.PromiseRefBase.DelegateWrapper.Create(keySelector),
                Internal.PromiseRefBase.DelegateWrapper.Create(elementSelector),
                comparer);
        }

        /// <summary>
        /// Creates a lookup from an async-enumerable sequence by invoking key and element selector functions on each source element and awaiting the results.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to the key selector.</typeparam>
        /// <typeparam name="TKey">The type of the lookup key computed for each element in the source sequence.</typeparam>
        /// <typeparam name="TElement">The type of the lookup value computed for each element in the source sequence.</typeparam>
        /// <param name="source">An async-enumerable sequence to create a lookup for.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from each element.</param>
        /// <param name="elementSelector">An asynchronous transform function to produce a result element value from each source element.</param>
        /// <param name="comparer">An equality comparer to compare keys. If null, the default equality comparer will be used.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>A <see cref="Promise{T}"/> whose result will be a lookup mapping unique key values onto the corresponding source sequence's elements.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="elementSelector"/> is null.</exception>
        public static Promise<ILookup<TKey, TElement>> ToLookupAsync<TSource, TCaptureKey, TKey, TElement>(
            this AsyncEnumerable<TSource> source,
            TCaptureKey keyCaptureValue,
            Func<TCaptureKey, TSource, Promise<TKey>> keySelector,
            Func<TSource, Promise<TElement>> elementSelector,
            IEqualityComparer<TKey> comparer,
            CancelationToken cancelationToken = default)
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(elementSelector, nameof(elementSelector), 1);

            return Internal.Lookup<TKey, TElement>.CreateAwaitAsync(source.GetAsyncEnumerator(cancelationToken),
                Internal.PromiseRefBase.DelegateWrapper.Create(keyCaptureValue, keySelector),
                Internal.PromiseRefBase.DelegateWrapper.Create(elementSelector),
                comparer);
        }

        /// <summary>
        /// Creates a lookup from an async-enumerable sequence by invoking key and element selector functions on each source element and awaiting the results.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the lookup key computed for each element in the source sequence.</typeparam>
        /// <typeparam name="TCaptureElement">The type of the captured value that will be passed to the element selector.</typeparam>
        /// <typeparam name="TElement">The type of the lookup value computed for each element in the source sequence.</typeparam>
        /// <param name="source">An async-enumerable sequence to create a lookup for.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from each element.</param>
        /// <param name="elementCaptureValue">The extra value that will be passed to <paramref name="elementSelector"/>.</param>
        /// <param name="elementSelector">An asynchronous transform function to produce a result element value from each source element.</param>
        /// <param name="comparer">An equality comparer to compare keys. If null, the default equality comparer will be used.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>A <see cref="Promise{T}"/> whose result will be a lookup mapping unique key values onto the corresponding source sequence's elements.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="elementSelector"/> is null.</exception>
        public static Promise<ILookup<TKey, TElement>> ToLookupAsync<TSource, TKey, TCaptureElement, TElement>(
            this AsyncEnumerable<TSource> source,
            Func<TSource, Promise<TKey>> keySelector,
            TCaptureElement elementCaptureValue,
            Func<TCaptureElement, TSource, Promise<TElement>> elementSelector,
            IEqualityComparer<TKey> comparer,
            CancelationToken cancelationToken = default)
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(elementSelector, nameof(elementSelector), 1);

            return Internal.Lookup<TKey, TElement>.CreateAwaitAsync(source.GetAsyncEnumerator(cancelationToken),
                Internal.PromiseRefBase.DelegateWrapper.Create(keySelector),
                Internal.PromiseRefBase.DelegateWrapper.Create(elementCaptureValue, elementSelector),
                comparer);
        }

        /// <summary>
        /// Creates a lookup from an async-enumerable sequence by invoking key and element selector functions on each source element and awaiting the results.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to the key selector.</typeparam>
        /// <typeparam name="TKey">The type of the lookup key computed for each element in the source sequence.</typeparam>
        /// <typeparam name="TCaptureElement">The type of the captured value that will be passed to the element selector.</typeparam>
        /// <typeparam name="TElement">The type of the lookup value computed for each element in the source sequence.</typeparam>
        /// <param name="source">An async-enumerable sequence to create a lookup for.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from each element.</param>
        /// <param name="elementCaptureValue">The extra value that will be passed to <paramref name="elementSelector"/>.</param>
        /// <param name="elementSelector">An asynchronous transform function to produce a result element value from each source element.</param>
        /// <param name="comparer">An equality comparer to compare keys. If null, the default equality comparer will be used.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>A <see cref="Promise{T}"/> whose result will be a lookup mapping unique key values onto the corresponding source sequence's elements.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="elementSelector"/> is null.</exception>
        public static Promise<ILookup<TKey, TElement>> ToLookupAsync<TSource, TCaptureKey, TKey, TCaptureElement, TElement>(
            this AsyncEnumerable<TSource> source,
            TCaptureKey keyCaptureValue,
            Func<TCaptureKey, TSource, Promise<TKey>> keySelector,
            TCaptureElement elementCaptureValue,
            Func<TCaptureElement, TSource, Promise<TElement>> elementSelector,
            IEqualityComparer<TKey> comparer,
            CancelationToken cancelationToken = default)
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(elementSelector, nameof(elementSelector), 1);

            return Internal.Lookup<TKey, TElement>.CreateAwaitAsync(source.GetAsyncEnumerator(cancelationToken),
                Internal.PromiseRefBase.DelegateWrapper.Create(keyCaptureValue, keySelector),
                Internal.PromiseRefBase.DelegateWrapper.Create(elementCaptureValue, elementSelector),
                comparer);
        }
        #endregion AsyncEnumerable

        #region ConfiguredAsyncEnumerable
        /// <summary>
        /// Creates a lookup from a configured async-enumerable sequence according to a specified key selector function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the lookup key computed for each element in the source sequence.</typeparam>
        /// <param name="configuredSource">A configured async-enumerable sequence to create a lookup for.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <returns>A <see cref="Promise{T}"/> whose result will be a lookup mapping unique key values onto the corresponding source sequence's elements.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        public static Promise<ILookup<TKey, TSource>> ToLookupAsync<TSource, TKey>(
            this ConfiguredAsyncEnumerable<TSource> configuredSource,
            Func<TSource, TKey> keySelector)
            => ToLookupAsync(configuredSource, keySelector, comparer: null);

        /// <summary>
        /// Creates a lookup from a configured async-enumerable sequence according to a specified key selector function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to the key selector.</typeparam>
        /// <typeparam name="TKey">The type of the lookup key computed for each element in the source sequence.</typeparam>
        /// <param name="configuredSource">A configured async-enumerable sequence to create a lookup for.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <returns>A <see cref="Promise{T}"/> whose result will be a lookup mapping unique key values onto the corresponding source sequence's elements.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        public static Promise<ILookup<TKey, TSource>> ToLookupAsync<TSource, TCaptureKey, TKey>(
            this ConfiguredAsyncEnumerable<TSource> configuredSource,
            TCaptureKey keyCaptureValue,
            Func<TCaptureKey, TSource, TKey> keySelector)
            => ToLookupAsync(configuredSource, keyCaptureValue, keySelector, comparer: null);

        /// <summary>
        /// Creates a lookup from a configured async-enumerable sequence according to a specified key selector function, and a comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the lookup key computed for each element in the source sequence.</typeparam>
        /// <param name="configuredSource">A configured async-enumerable sequence to create a lookup for.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="comparer">An equality comparer to compare keys. If null, the default equality comparer will be used.</param>
        /// <returns>A <see cref="Promise{T}"/> whose result will be a lookup mapping unique key values onto the corresponding source sequence's elements.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        public static Promise<ILookup<TKey, TSource>> ToLookupAsync<TSource, TKey>(
            this ConfiguredAsyncEnumerable<TSource> configuredSource,
            Func<TSource, TKey> keySelector,
            IEqualityComparer<TKey> comparer)
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);

            return Internal.Lookup<TKey, TSource>.CreateAsync(configuredSource.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(keySelector), comparer);
        }

        /// <summary>
        /// Creates a lookup from a configured async-enumerable sequence according to a specified key selector function, and a comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to the key selector.</typeparam>
        /// <typeparam name="TKey">The type of the lookup key computed for each element in the source sequence.</typeparam>
        /// <param name="configuredSource">A configured async-enumerable sequence to create a lookup for.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="comparer">An equality comparer to compare keys. If null, the default equality comparer will be used.</param>
        /// <returns>A <see cref="Promise{T}"/> whose result will be a lookup mapping unique key values onto the corresponding source sequence's elements.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        public static Promise<ILookup<TKey, TSource>> ToLookupAsync<TSource, TCaptureKey, TKey>(
            this ConfiguredAsyncEnumerable<TSource> configuredSource,
            TCaptureKey keyCaptureValue,
            Func<TCaptureKey, TSource, TKey> keySelector,
            IEqualityComparer<TKey> comparer)
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);

            return Internal.Lookup<TKey, TSource>.CreateAsync(configuredSource.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(keyCaptureValue, keySelector), comparer);
        }

        /// <summary>
        /// Creates a lookup from a configured async-enumerable sequence by invoking a key-selector function on each element and awaiting the result.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the lookup key computed for each element in the source sequence.</typeparam>
        /// <param name="configuredSource">A configured async-enumerable sequence to create a lookup for.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from each element.</param>
        /// <returns>A <see cref="Promise{T}"/> whose result will be a lookup mapping unique key values onto the corresponding source sequence's elements.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        public static Promise<ILookup<TKey, TSource>> ToLookupAsync<TSource, TKey>(
            this ConfiguredAsyncEnumerable<TSource> configuredSource,
            Func<TSource, Promise<TKey>> keySelector)
            => ToLookupAsync<TSource, TKey>(configuredSource, keySelector, comparer: null);

        /// <summary>
        /// Creates a lookup from a configured async-enumerable sequence by invoking a key-selector function on each element and awaiting the result.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to the key selector.</typeparam>
        /// <typeparam name="TKey">The type of the lookup key computed for each element in the source sequence.</typeparam>
        /// <param name="configuredSource">A configured async-enumerable sequence to create a lookup for.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from each element.</param>
        /// <returns>A <see cref="Promise{T}"/> whose result will be a lookup mapping unique key values onto the corresponding source sequence's elements.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        public static Promise<ILookup<TKey, TSource>> ToLookupAsync<TSource, TCaptureKey, TKey>(
            this ConfiguredAsyncEnumerable<TSource> configuredSource,
            TCaptureKey keyCaptureValue,
            Func<TCaptureKey, TSource, Promise<TKey>> keySelector)
            => ToLookupAsync<TSource, TCaptureKey, TKey>(configuredSource, keyCaptureValue, keySelector, comparer: null);

        /// <summary>
        /// Creates a lookup from a configured async-enumerable sequence by invoking a key-selector function on each element and awaiting the result.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the lookup key computed for each element in the source sequence.</typeparam>
        /// <param name="configuredSource">A configured async-enumerable sequence to create a lookup for.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from each element.</param>
        /// <param name="comparer">An equality comparer to compare keys. If null, the default equality comparer will be used.</param>
        /// <returns>A <see cref="Promise{T}"/> whose result will be a lookup mapping unique key values onto the corresponding source sequence's elements.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        public static Promise<ILookup<TKey, TSource>> ToLookupAsync<TSource, TKey>(
            this ConfiguredAsyncEnumerable<TSource> configuredSource,
            Func<TSource, Promise<TKey>> keySelector,
            IEqualityComparer<TKey> comparer)
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);

            return Internal.Lookup<TKey, TSource>.CreateAwaitAsync(configuredSource.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(keySelector), comparer);
        }

        /// <summary>
        /// Creates a lookup from a configured async-enumerable sequence by invoking a key-selector function on each element and awaiting the result.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to the key selector.</typeparam>
        /// <typeparam name="TKey">The type of the lookup key computed for each element in the source sequence.</typeparam>
        /// <param name="configuredSource">A configured async-enumerable sequence to create a lookup for.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from each element.</param>
        /// <param name="comparer">An equality comparer to compare keys. If null, the default equality comparer will be used.</param>
        /// <returns>A <see cref="Promise{T}"/> whose result will be a lookup mapping unique key values onto the corresponding source sequence's elements.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        public static Promise<ILookup<TKey, TSource>> ToLookupAsync<TSource, TCaptureKey, TKey>(
            this ConfiguredAsyncEnumerable<TSource> configuredSource,
            TCaptureKey keyCaptureValue,
            Func<TCaptureKey, TSource, Promise<TKey>> keySelector,
            IEqualityComparer<TKey> comparer)
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);

            return Internal.Lookup<TKey, TSource>.CreateAwaitAsync(configuredSource.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(keyCaptureValue, keySelector), comparer);
        }

        /// <summary>
        /// Creates a lookup from a configured async-enumerable sequence according to a specified key selector function, and an element selector function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the lookup key computed for each element in the source sequence.</typeparam>
        /// <typeparam name="TElement">The type of the lookup value computed for each element in the source sequence.</typeparam>
        /// <param name="configuredSource">A configured async-enumerable sequence to create a lookup for.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="elementSelector">A transform function to produce a result element value from each element.</param>
        /// <returns>A <see cref="Promise{T}"/> whose result will be a lookup mapping unique key values onto the corresponding source sequence's elements.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="elementSelector"/> is null.</exception>
        public static Promise<ILookup<TKey, TElement>> ToLookupAsync<TSource, TKey, TElement>(
            this ConfiguredAsyncEnumerable<TSource> configuredSource,
            Func<TSource, TKey> keySelector,
            Func<TSource, TElement> elementSelector)
            => ToLookupAsync(configuredSource, keySelector, elementSelector, comparer: null);

        /// <summary>
        /// Creates a lookup from a configured async-enumerable sequence according to a specified key selector function, and an element selector function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to the key selector.</typeparam>
        /// <typeparam name="TKey">The type of the lookup key computed for each element in the source sequence.</typeparam>
        /// <typeparam name="TElement">The type of the lookup value computed for each element in the source sequence.</typeparam>
        /// <param name="configuredSource">A configured async-enumerable sequence to create a lookup for.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="elementSelector">A transform function to produce a result element value from each element.</param>
        /// <returns>A <see cref="Promise{T}"/> whose result will be a lookup mapping unique key values onto the corresponding source sequence's elements.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="elementSelector"/> is null.</exception>
        public static Promise<ILookup<TKey, TElement>> ToLookupAsync<TSource, TCaptureKey, TKey, TElement>(
            this ConfiguredAsyncEnumerable<TSource> configuredSource,
            TCaptureKey keyCaptureValue,
            Func<TCaptureKey, TSource, TKey> keySelector,
            Func<TSource, TElement> elementSelector)
            => ToLookupAsync(configuredSource, keyCaptureValue, keySelector, elementSelector, comparer: null);

        /// <summary>
        /// Creates a lookup from a configured async-enumerable sequence according to a specified key selector function, and an element selector function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the lookup key computed for each element in the source sequence.</typeparam>
        /// <typeparam name="TCaptureElement">The type of the captured value that will be passed to the element selector.</typeparam>
        /// <typeparam name="TElement">The type of the lookup value computed for each element in the source sequence.</typeparam>
        /// <param name="configuredSource">A configured async-enumerable sequence to create a lookup for.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="elementCaptureValue">The extra value that will be passed to <paramref name="elementSelector"/>.</param>
        /// <param name="elementSelector">A transform function to produce a result element value from each element.</param>
        /// <returns>A <see cref="Promise{T}"/> whose result will be a lookup mapping unique key values onto the corresponding source sequence's elements.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="elementSelector"/> is null.</exception>
        public static Promise<ILookup<TKey, TElement>> ToLookupAsync<TSource, TKey, TCaptureElement, TElement>(
            this ConfiguredAsyncEnumerable<TSource> configuredSource,
            Func<TSource, TKey> keySelector,
            TCaptureElement elementCaptureValue,
            Func<TCaptureElement, TSource, TElement> elementSelector)
            => ToLookupAsync(configuredSource, keySelector, elementCaptureValue, elementSelector, comparer: null);

        /// <summary>
        /// Creates a lookup from a configured async-enumerable sequence according to a specified key selector function, and an element selector function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to the key selector.</typeparam>
        /// <typeparam name="TKey">The type of the lookup key computed for each element in the source sequence.</typeparam>
        /// <typeparam name="TCaptureElement">The type of the captured value that will be passed to the element selector.</typeparam>
        /// <typeparam name="TElement">The type of the lookup value computed for each element in the source sequence.</typeparam>
        /// <param name="configuredSource">A configured async-enumerable sequence to create a lookup for.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="elementCaptureValue">The extra value that will be passed to <paramref name="elementSelector"/>.</param>
        /// <param name="elementSelector">A transform function to produce a result element value from each element.</param>
        /// <returns>A <see cref="Promise{T}"/> whose result will be a lookup mapping unique key values onto the corresponding source sequence's elements.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="elementSelector"/> is null.</exception>
        public static Promise<ILookup<TKey, TElement>> ToLookupAsync<TSource, TCaptureKey, TKey, TCaptureElement, TElement>(
            this ConfiguredAsyncEnumerable<TSource> configuredSource,
            TCaptureKey keyCaptureValue,
            Func<TCaptureKey, TSource, TKey> keySelector,
            TCaptureElement elementCaptureValue,
            Func<TCaptureElement, TSource, TElement> elementSelector)
            => ToLookupAsync(configuredSource, keyCaptureValue, keySelector, elementCaptureValue, elementSelector, comparer: null);

        /// <summary>
        /// Creates a lookup from a configured async-enumerable sequence according to a specified key selector function, a comparer, and an element selector function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the lookup key computed for each element in the source sequence.</typeparam>
        /// <typeparam name="TElement">The type of the lookup value computed for each element in the source sequence.</typeparam>
        /// <param name="configuredSource">A configured async-enumerable sequence to create a lookup for.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="elementSelector">A transform function to produce a result element value from each element.</param>
        /// <param name="comparer">An equality comparer to compare keys. If null, the default equality comparer will be used.</param>
        /// <returns>A <see cref="Promise{T}"/> whose result will be a lookup mapping unique key values onto the corresponding source sequence's elements.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="elementSelector"/> is null.</exception>
        public static Promise<ILookup<TKey, TElement>> ToLookupAsync<TSource, TKey, TElement>(
            this ConfiguredAsyncEnumerable<TSource> configuredSource,
            Func<TSource, TKey> keySelector,
            Func<TSource, TElement> elementSelector,
            IEqualityComparer<TKey> comparer)
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(elementSelector, nameof(elementSelector), 1);

            return Internal.Lookup<TKey, TElement>.CreateAsync(configuredSource.GetAsyncEnumerator(),
                Internal.PromiseRefBase.DelegateWrapper.Create(keySelector),
                Internal.PromiseRefBase.DelegateWrapper.Create(elementSelector),
                comparer);
        }

        /// <summary>
        /// Creates a lookup from a configured async-enumerable sequence according to a specified key selector function, a comparer, and an element selector function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to the key selector.</typeparam>
        /// <typeparam name="TKey">The type of the lookup key computed for each element in the source sequence.</typeparam>
        /// <typeparam name="TElement">The type of the lookup value computed for each element in the source sequence.</typeparam>
        /// <param name="configuredSource">A configured async-enumerable sequence to create a lookup for.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="elementSelector">A transform function to produce a result element value from each element.</param>
        /// <param name="comparer">An equality comparer to compare keys. If null, the default equality comparer will be used.</param>
        /// <returns>A <see cref="Promise{T}"/> whose result will be a lookup mapping unique key values onto the corresponding source sequence's elements.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="elementSelector"/> is null.</exception>
        public static Promise<ILookup<TKey, TElement>> ToLookupAsync<TSource, TCaptureKey, TKey, TElement>(
            this ConfiguredAsyncEnumerable<TSource> configuredSource,
            TCaptureKey keyCaptureValue,
            Func<TCaptureKey, TSource, TKey> keySelector,
            Func<TSource, TElement> elementSelector,
            IEqualityComparer<TKey> comparer)
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(elementSelector, nameof(elementSelector), 1);

            return Internal.Lookup<TKey, TElement>.CreateAsync(configuredSource.GetAsyncEnumerator(),
                Internal.PromiseRefBase.DelegateWrapper.Create(keyCaptureValue, keySelector),
                Internal.PromiseRefBase.DelegateWrapper.Create(elementSelector),
                comparer);
        }

        /// <summary>
        /// Creates a lookup from a configured async-enumerable sequence according to a specified key selector function, a comparer, and an element selector function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the lookup key computed for each element in the source sequence.</typeparam>
        /// <typeparam name="TCaptureElement">The type of the captured value that will be passed to the element selector.</typeparam>
        /// <typeparam name="TElement">The type of the lookup value computed for each element in the source sequence.</typeparam>
        /// <param name="configuredSource">A configured async-enumerable sequence to create a lookup for.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="elementCaptureValue">The extra value that will be passed to <paramref name="elementSelector"/>.</param>
        /// <param name="elementSelector">A transform function to produce a result element value from each element.</param>
        /// <param name="comparer">An equality comparer to compare keys. If null, the default equality comparer will be used.</param>
        /// <returns>A <see cref="Promise{T}"/> whose result will be a lookup mapping unique key values onto the corresponding source sequence's elements.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="elementSelector"/> is null.</exception>
        public static Promise<ILookup<TKey, TElement>> ToLookupAsync<TSource, TKey, TCaptureElement, TElement>(
            this ConfiguredAsyncEnumerable<TSource> configuredSource,
            Func<TSource, TKey> keySelector,
            TCaptureElement elementCaptureValue,
            Func<TCaptureElement, TSource, TElement> elementSelector,
            IEqualityComparer<TKey> comparer)
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(elementSelector, nameof(elementSelector), 1);

            return Internal.Lookup<TKey, TElement>.CreateAsync(configuredSource.GetAsyncEnumerator(),
                Internal.PromiseRefBase.DelegateWrapper.Create(keySelector),
                Internal.PromiseRefBase.DelegateWrapper.Create(elementCaptureValue, elementSelector),
                comparer);
        }

        /// <summary>
        /// Creates a lookup from a configured async-enumerable sequence according to a specified key selector function, a comparer, and an element selector function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to the key selector.</typeparam>
        /// <typeparam name="TKey">The type of the lookup key computed for each element in the source sequence.</typeparam>
        /// <typeparam name="TCaptureElement">The type of the captured value that will be passed to the element selector.</typeparam>
        /// <typeparam name="TElement">The type of the lookup value computed for each element in the source sequence.</typeparam>
        /// <param name="configuredSource">A configured async-enumerable sequence to create a lookup for.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="elementCaptureValue">The extra value that will be passed to <paramref name="elementSelector"/>.</param>
        /// <param name="elementSelector">A transform function to produce a result element value from each element.</param>
        /// <param name="comparer">An equality comparer to compare keys. If null, the default equality comparer will be used.</param>
        /// <returns>A <see cref="Promise{T}"/> whose result will be a lookup mapping unique key values onto the corresponding source sequence's elements.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="elementSelector"/> is null.</exception>
        public static Promise<ILookup<TKey, TElement>> ToLookupAsync<TSource, TCaptureKey, TKey, TCaptureElement, TElement>(
            this ConfiguredAsyncEnumerable<TSource> configuredSource,
            TCaptureKey keyCaptureValue,
            Func<TCaptureKey, TSource, TKey> keySelector,
            TCaptureElement elementCaptureValue,
            Func<TCaptureElement, TSource, TElement> elementSelector,
            IEqualityComparer<TKey> comparer)
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(elementSelector, nameof(elementSelector), 1);

            return Internal.Lookup<TKey, TElement>.CreateAsync(configuredSource.GetAsyncEnumerator(),
                Internal.PromiseRefBase.DelegateWrapper.Create(keyCaptureValue, keySelector),
                Internal.PromiseRefBase.DelegateWrapper.Create(elementCaptureValue, elementSelector),
                comparer);
        }

        /// <summary>
        /// Creates a lookup from a configured async-enumerable sequence by invoking key and element selector functions on each source element and awaiting the results.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the lookup key computed for each element in the source sequence.</typeparam>
        /// <typeparam name="TElement">The type of the lookup value computed for each element in the source sequence.</typeparam>
        /// <param name="configuredSource">A configured async-enumerable sequence to create a lookup for.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from each element.</param>
        /// <param name="elementSelector">An asynchronous transform function to produce a result element value from each element.</param>
        /// <returns>A <see cref="Promise{T}"/> whose result will be a lookup mapping unique key values onto the corresponding source sequence's elements.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="elementSelector"/> is null.</exception>
        public static Promise<ILookup<TKey, TElement>> ToLookupAsync<TSource, TKey, TElement>(
            this ConfiguredAsyncEnumerable<TSource> configuredSource,
            Func<TSource, Promise<TKey>> keySelector,
            Func<TSource, Promise<TElement>> elementSelector)
            => ToLookupAsync<TSource, TKey, TElement>(configuredSource, keySelector, elementSelector, comparer: null);

        /// <summary>
        /// Creates a lookup from a configured async-enumerable sequence by invoking key and element selector functions on each source element and awaiting the results.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to the key selector.</typeparam>
        /// <typeparam name="TKey">The type of the lookup key computed for each element in the source sequence.</typeparam>
        /// <typeparam name="TElement">The type of the lookup value computed for each element in the source sequence.</typeparam>
        /// <param name="configuredSource">A configured async-enumerable sequence to create a lookup for.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from each element.</param>
        /// <param name="elementSelector">An asynchronous transform function to produce a result element value from each element.</param>
        /// <returns>A <see cref="Promise{T}"/> whose result will be a lookup mapping unique key values onto the corresponding source sequence's elements.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="elementSelector"/> is null.</exception>
        public static Promise<ILookup<TKey, TElement>> ToLookupAsync<TSource, TCaptureKey, TKey, TElement>(
            this ConfiguredAsyncEnumerable<TSource> configuredSource,
            TCaptureKey keyCaptureValue,
            Func<TCaptureKey, TSource, Promise<TKey>> keySelector,
            Func<TSource, Promise<TElement>> elementSelector)
            => ToLookupAsync<TSource, TCaptureKey, TKey, TElement>(configuredSource, keyCaptureValue, keySelector, elementSelector, comparer: null);

        /// <summary>
        /// Creates a lookup from a configured async-enumerable sequence by invoking key and element selector functions on each source element and awaiting the results.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the lookup key computed for each element in the source sequence.</typeparam>
        /// <typeparam name="TCaptureElement">The type of the captured value that will be passed to the element selector.</typeparam>
        /// <typeparam name="TElement">The type of the lookup value computed for each element in the source sequence.</typeparam>
        /// <param name="configuredSource">A configured async-enumerable sequence to create a lookup for.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from each element.</param>
        /// <param name="elementCaptureValue">The extra value that will be passed to <paramref name="elementSelector"/>.</param>
        /// <param name="elementSelector">An asynchronous transform function to produce a result element value from each element.</param>
        /// <returns>A <see cref="Promise{T}"/> whose result will be a lookup mapping unique key values onto the corresponding source sequence's elements.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="elementSelector"/> is null.</exception>
        public static Promise<ILookup<TKey, TElement>> ToLookupAsync<TSource, TKey, TCaptureElement, TElement>(
            this ConfiguredAsyncEnumerable<TSource> configuredSource,
            Func<TSource, Promise<TKey>> keySelector,
            TCaptureElement elementCaptureValue,
            Func<TCaptureElement, TSource, Promise<TElement>> elementSelector)
            => ToLookupAsync<TSource, TKey, TCaptureElement, TElement>(configuredSource, keySelector, elementCaptureValue, elementSelector, comparer: null);

        /// <summary>
        /// Creates a lookup from a configured async-enumerable sequence by invoking key and element selector functions on each source element and awaiting the results.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to the key selector.</typeparam>
        /// <typeparam name="TKey">The type of the lookup key computed for each element in the source sequence.</typeparam>
        /// <typeparam name="TCaptureElement">The type of the captured value that will be passed to the element selector.</typeparam>
        /// <typeparam name="TElement">The type of the lookup value computed for each element in the source sequence.</typeparam>
        /// <param name="configuredSource">A configured async-enumerable sequence to create a lookup for.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from each element.</param>
        /// <param name="elementCaptureValue">The extra value that will be passed to <paramref name="elementSelector"/>.</param>
        /// <param name="elementSelector">An asynchronous transform function to produce a result element value from each element.</param>
        /// <returns>A <see cref="Promise{T}"/> whose result will be a lookup mapping unique key values onto the corresponding source sequence's elements.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="elementSelector"/> is null.</exception>
        public static Promise<ILookup<TKey, TElement>> ToLookupAsync<TSource, TCaptureKey, TKey, TCaptureElement, TElement>(
            this ConfiguredAsyncEnumerable<TSource> configuredSource,
            TCaptureKey keyCaptureValue,
            Func<TCaptureKey, TSource, Promise<TKey>> keySelector,
            TCaptureElement elementCaptureValue,
            Func<TCaptureElement, TSource, Promise<TElement>> elementSelector)
            => ToLookupAsync<TSource, TCaptureKey, TKey, TCaptureElement, TElement>(configuredSource, keyCaptureValue, keySelector, elementCaptureValue, elementSelector, comparer: null);

        /// <summary>
        /// Creates a lookup from a configured async-enumerable sequence by invoking key and element selector functions on each source element and awaiting the results.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the lookup key computed for each element in the source sequence.</typeparam>
        /// <typeparam name="TElement">The type of the lookup value computed for each element in the source sequence.</typeparam>
        /// <param name="configuredSource">A configured async-enumerable sequence to create a lookup for.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from each element.</param>
        /// <param name="elementSelector">An asynchronous transform function to produce a result element value from each source element.</param>
        /// <param name="comparer">An equality comparer to compare keys. If null, the default equality comparer will be used.</param>
        /// <returns>A <see cref="Promise{T}"/> whose result will be a lookup mapping unique key values onto the corresponding source sequence's elements.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="elementSelector"/> is null.</exception>
        public static Promise<ILookup<TKey, TElement>> ToLookupAsync<TSource, TKey, TElement>(
            this ConfiguredAsyncEnumerable<TSource> configuredSource,
            Func<TSource, Promise<TKey>> keySelector,
            Func<TSource, Promise<TElement>> elementSelector,
            IEqualityComparer<TKey> comparer)
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(elementSelector, nameof(elementSelector), 1);

            return Internal.Lookup<TKey, TElement>.CreateAwaitAsync(configuredSource.GetAsyncEnumerator(),
                Internal.PromiseRefBase.DelegateWrapper.Create(keySelector),
                Internal.PromiseRefBase.DelegateWrapper.Create(elementSelector),
                comparer);
        }

        /// <summary>
        /// Creates a lookup from a configured async-enumerable sequence by invoking key and element selector functions on each source element and awaiting the results.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to the key selector.</typeparam>
        /// <typeparam name="TKey">The type of the lookup key computed for each element in the source sequence.</typeparam>
        /// <typeparam name="TElement">The type of the lookup value computed for each element in the source sequence.</typeparam>
        /// <param name="configuredSource">A configured async-enumerable sequence to create a lookup for.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from each element.</param>
        /// <param name="elementSelector">An asynchronous transform function to produce a result element value from each source element.</param>
        /// <param name="comparer">An equality comparer to compare keys. If null, the default equality comparer will be used.</param>
        /// <returns>A <see cref="Promise{T}"/> whose result will be a lookup mapping unique key values onto the corresponding source sequence's elements.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="elementSelector"/> is null.</exception>
        public static Promise<ILookup<TKey, TElement>> ToLookupAsync<TSource, TCaptureKey, TKey, TElement>(
            this ConfiguredAsyncEnumerable<TSource> configuredSource,
            TCaptureKey keyCaptureValue,
            Func<TCaptureKey, TSource, Promise<TKey>> keySelector,
            Func<TSource, Promise<TElement>> elementSelector,
            IEqualityComparer<TKey> comparer)
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(elementSelector, nameof(elementSelector), 1);

            return Internal.Lookup<TKey, TElement>.CreateAwaitAsync(configuredSource.GetAsyncEnumerator(),
                Internal.PromiseRefBase.DelegateWrapper.Create(keyCaptureValue, keySelector),
                Internal.PromiseRefBase.DelegateWrapper.Create(elementSelector),
                comparer);
        }

        /// <summary>
        /// Creates a lookup from a configured async-enumerable sequence by invoking key and element selector functions on each source element and awaiting the results.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the lookup key computed for each element in the source sequence.</typeparam>
        /// <typeparam name="TCaptureElement">The type of the captured value that will be passed to the element selector.</typeparam>
        /// <typeparam name="TElement">The type of the lookup value computed for each element in the source sequence.</typeparam>
        /// <param name="configuredSource">A configured async-enumerable sequence to create a lookup for.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from each element.</param>
        /// <param name="elementCaptureValue">The extra value that will be passed to <paramref name="elementSelector"/>.</param>
        /// <param name="elementSelector">An asynchronous transform function to produce a result element value from each source element.</param>
        /// <param name="comparer">An equality comparer to compare keys. If null, the default equality comparer will be used.</param>
        /// <returns>A <see cref="Promise{T}"/> whose result will be a lookup mapping unique key values onto the corresponding source sequence's elements.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="elementSelector"/> is null.</exception>
        public static Promise<ILookup<TKey, TElement>> ToLookupAsync<TSource, TKey, TCaptureElement, TElement>(
            this ConfiguredAsyncEnumerable<TSource> configuredSource,
            Func<TSource, Promise<TKey>> keySelector,
            TCaptureElement elementCaptureValue,
            Func<TCaptureElement, TSource, Promise<TElement>> elementSelector,
            IEqualityComparer<TKey> comparer)
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(elementSelector, nameof(elementSelector), 1);

            return Internal.Lookup<TKey, TElement>.CreateAwaitAsync(configuredSource.GetAsyncEnumerator(),
                Internal.PromiseRefBase.DelegateWrapper.Create(keySelector),
                Internal.PromiseRefBase.DelegateWrapper.Create(elementCaptureValue, elementSelector),
                comparer);
        }

        /// <summary>
        /// Creates a lookup from a configured async-enumerable sequence by invoking key and element selector functions on each source element and awaiting the results.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to the key selector.</typeparam>
        /// <typeparam name="TKey">The type of the lookup key computed for each element in the source sequence.</typeparam>
        /// <typeparam name="TCaptureElement">The type of the captured value that will be passed to the element selector.</typeparam>
        /// <typeparam name="TElement">The type of the lookup value computed for each element in the source sequence.</typeparam>
        /// <param name="configuredSource">A configured async-enumerable sequence to create a lookup for.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from each element.</param>
        /// <param name="elementCaptureValue">The extra value that will be passed to <paramref name="elementSelector"/>.</param>
        /// <param name="elementSelector">An asynchronous transform function to produce a result element value from each source element.</param>
        /// <param name="comparer">An equality comparer to compare keys. If null, the default equality comparer will be used.</param>
        /// <returns>A <see cref="Promise{T}"/> whose result will be a lookup mapping unique key values onto the corresponding source sequence's elements.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="elementSelector"/> is null.</exception>
        public static Promise<ILookup<TKey, TElement>> ToLookupAsync<TSource, TCaptureKey, TKey, TCaptureElement, TElement>(
            this ConfiguredAsyncEnumerable<TSource> configuredSource,
            TCaptureKey keyCaptureValue,
            Func<TCaptureKey, TSource, Promise<TKey>> keySelector,
            TCaptureElement elementCaptureValue,
            Func<TCaptureElement, TSource, Promise<TElement>> elementSelector,
            IEqualityComparer<TKey> comparer)
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(elementSelector, nameof(elementSelector), 1);

            return Internal.Lookup<TKey, TElement>.CreateAwaitAsync(configuredSource.GetAsyncEnumerator(),
                Internal.PromiseRefBase.DelegateWrapper.Create(keyCaptureValue, keySelector),
                Internal.PromiseRefBase.DelegateWrapper.Create(elementCaptureValue, elementSelector),
                comparer);
        }
        #endregion ConfiguredAsyncEnumerable
    }
#endif // CSHARP_7_3_OR_NEWER
}