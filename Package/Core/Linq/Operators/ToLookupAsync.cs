using Proto.Promises.CompilerServices;
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
            => ToLookupAsync(source, keySelector, EqualityComparer<TKey>.Default, cancelationToken);

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
            => ToLookupAsync(source, keyCaptureValue, keySelector, EqualityComparer<TKey>.Default, cancelationToken);

        /// <summary>
        /// Creates a lookup from an async-enumerable sequence according to a specified key selector function, and a comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the lookup key computed for each element in the source sequence.</typeparam>
        /// <typeparam name="TEqualityComparer">The type of the comparer.</typeparam>
        /// <param name="source">An async-enumerable sequence to create a lookup for.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="comparer">An equality comparer to compare keys.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>A <see cref="Promise{T}"/> whose result will be a lookup mapping unique key values onto the corresponding source sequence's elements.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="comparer"/> is null.</exception>
        public static Promise<ILookup<TKey, TSource>> ToLookupAsync<TSource, TKey, TEqualityComparer>(
            this AsyncEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            TEqualityComparer comparer,
            CancelationToken cancelationToken = default)
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.LookupHelper<TKey, TSource>.GetOrCreateAsync(source.GetAsyncEnumerator(cancelationToken), Internal.PromiseRefBase.DelegateWrapper.Create(keySelector), comparer);
        }

        /// <summary>
        /// Creates a lookup from an async-enumerable sequence according to a specified key selector function, and a comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to the key selector.</typeparam>
        /// <typeparam name="TKey">The type of the lookup key computed for each element in the source sequence.</typeparam>
        /// <typeparam name="TEqualityComparer">The type of the comparer.</typeparam>
        /// <param name="source">An async-enumerable sequence to create a lookup for.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="comparer">An equality comparer to compare keys.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>A <see cref="Promise{T}"/> whose result will be a lookup mapping unique key values onto the corresponding source sequence's elements.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="comparer"/> is null.</exception>
        public static Promise<ILookup<TKey, TSource>> ToLookupAsync<TSource, TCaptureKey, TKey, TEqualityComparer>(
            this AsyncEnumerable<TSource> source,
            TCaptureKey keyCaptureValue,
            Func<TCaptureKey, TSource, TKey> keySelector,
            TEqualityComparer comparer,
            CancelationToken cancelationToken = default)
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.LookupHelper<TKey, TSource>.GetOrCreateAsync(source.GetAsyncEnumerator(cancelationToken), Internal.PromiseRefBase.DelegateWrapper.Create(keyCaptureValue, keySelector), comparer);
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
            => ToLookupAsync<TSource, TKey, IEqualityComparer<TKey>>(source, keySelector, EqualityComparer<TKey>.Default, cancelationToken);

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
            => ToLookupAsync<TSource, TCaptureKey, TKey, IEqualityComparer<TKey>>(source, keyCaptureValue, keySelector, EqualityComparer<TKey>.Default, cancelationToken);

        /// <summary>
        /// Creates a lookup from an async-enumerable sequence by invoking a key-selector function on each element and awaiting the result.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the lookup key computed for each element in the source sequence.</typeparam>
        /// <typeparam name="TEqualityComparer">The type of the comparer.</typeparam>
        /// <param name="source">An async-enumerable sequence to create a lookup for.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from each element.</param>
        /// <param name="comparer">An equality comparer to compare keys.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>A <see cref="Promise{T}"/> whose result will be a lookup mapping unique key values onto the corresponding source sequence's elements.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="comparer"/> is null.</exception>
        public static Promise<ILookup<TKey, TSource>> ToLookupAsync<TSource, TKey, TEqualityComparer>(
            this AsyncEnumerable<TSource> source,
            Func<TSource, Promise<TKey>> keySelector,
            TEqualityComparer comparer,
            CancelationToken cancelationToken = default)
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.LookupHelper<TKey, TSource>.GetOrCreateAwaitAsync(source.GetAsyncEnumerator(cancelationToken), Internal.PromiseRefBase.DelegateWrapper.Create(keySelector), comparer);
        }

        /// <summary>
        /// Creates a lookup from an async-enumerable sequence by invoking a key-selector function on each element and awaiting the result.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to the key selector.</typeparam>
        /// <typeparam name="TKey">The type of the lookup key computed for each element in the source sequence.</typeparam>
        /// <typeparam name="TEqualityComparer">The type of the comparer.</typeparam>
        /// <param name="source">An async-enumerable sequence to create a lookup for.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from each element.</param>
        /// <param name="comparer">An equality comparer to compare keys.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>A <see cref="Promise{T}"/> whose result will be a lookup mapping unique key values onto the corresponding source sequence's elements.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="comparer"/> is null.</exception>
        public static Promise<ILookup<TKey, TSource>> ToLookupAsync<TSource, TCaptureKey, TKey, TEqualityComparer>(
            this AsyncEnumerable<TSource> source,
            TCaptureKey keyCaptureValue,
            Func<TCaptureKey, TSource, Promise<TKey>> keySelector,
            TEqualityComparer comparer,
            CancelationToken cancelationToken = default)
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.LookupHelper<TKey, TSource>.GetOrCreateAwaitAsync(source.GetAsyncEnumerator(cancelationToken), Internal.PromiseRefBase.DelegateWrapper.Create(keyCaptureValue, keySelector), comparer);
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
            => ToLookupAsync(source, keySelector, elementSelector, EqualityComparer<TKey>.Default, cancelationToken);

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
            => ToLookupAsync(source, keyCaptureValue, keySelector, elementSelector, EqualityComparer<TKey>.Default, cancelationToken);

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
            => ToLookupAsync(source, keySelector, elementCaptureValue, elementSelector, EqualityComparer<TKey>.Default, cancelationToken);

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
            => ToLookupAsync(source, keyCaptureValue, keySelector, elementCaptureValue, elementSelector, EqualityComparer<TKey>.Default, cancelationToken);

        /// <summary>
        /// Creates a lookup from an async-enumerable sequence according to a specified key selector function, a comparer, and an element selector function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the lookup key computed for each element in the source sequence.</typeparam>
        /// <typeparam name="TElement">The type of the lookup value computed for each element in the source sequence.</typeparam>
        /// <typeparam name="TEqualityComparer">The type of the comparer.</typeparam>
        /// <param name="source">An async-enumerable sequence to create a lookup for.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="elementSelector">A transform function to produce a result element value from each element.</param>
        /// <param name="comparer">An equality comparer to compare keys.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>A <see cref="Promise{T}"/> whose result will be a lookup mapping unique key values onto the corresponding source sequence's elements.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="elementSelector"/> or <paramref name="comparer"/> is null.</exception>
        public static Promise<ILookup<TKey, TElement>> ToLookupAsync<TSource, TKey, TElement, TEqualityComparer>(
            this AsyncEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            Func<TSource, TElement> elementSelector,
            TEqualityComparer comparer,
            CancelationToken cancelationToken = default)
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(elementSelector, nameof(elementSelector), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.LookupHelper<TKey, TElement>.GetOrCreateAsync(source.GetAsyncEnumerator(cancelationToken),
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
        /// <typeparam name="TEqualityComparer">The type of the comparer.</typeparam>
        /// <param name="source">An async-enumerable sequence to create a lookup for.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="elementSelector">A transform function to produce a result element value from each element.</param>
        /// <param name="comparer">An equality comparer to compare keys.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>A <see cref="Promise{T}"/> whose result will be a lookup mapping unique key values onto the corresponding source sequence's elements.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="elementSelector"/> or <paramref name="comparer"/> is null.</exception>
        public static Promise<ILookup<TKey, TElement>> ToLookupAsync<TSource, TCaptureKey, TKey, TElement, TEqualityComparer>(
            this AsyncEnumerable<TSource> source,
            TCaptureKey keyCaptureValue,
            Func<TCaptureKey, TSource, TKey> keySelector,
            Func<TSource, TElement> elementSelector,
            TEqualityComparer comparer,
            CancelationToken cancelationToken = default)
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(elementSelector, nameof(elementSelector), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.LookupHelper<TKey, TElement>.GetOrCreateAsync(source.GetAsyncEnumerator(cancelationToken),
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
        /// <typeparam name="TEqualityComparer">The type of the comparer.</typeparam>
        /// <param name="source">An async-enumerable sequence to create a lookup for.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="elementCaptureValue">The extra value that will be passed to <paramref name="elementSelector"/>.</param>
        /// <param name="elementSelector">A transform function to produce a result element value from each element.</param>
        /// <param name="comparer">An equality comparer to compare keys.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>A <see cref="Promise{T}"/> whose result will be a lookup mapping unique key values onto the corresponding source sequence's elements.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="elementSelector"/> or <paramref name="comparer"/> is null.</exception>
        public static Promise<ILookup<TKey, TElement>> ToLookupAsync<TSource, TKey, TCaptureElement, TElement, TEqualityComparer>(
            this AsyncEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            TCaptureElement elementCaptureValue,
            Func<TCaptureElement, TSource, TElement> elementSelector,
            TEqualityComparer comparer,
            CancelationToken cancelationToken = default)
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(elementSelector, nameof(elementSelector), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.LookupHelper<TKey, TElement>.GetOrCreateAsync(source.GetAsyncEnumerator(cancelationToken),
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
        /// <typeparam name="TEqualityComparer">The type of the comparer.</typeparam>
        /// <param name="source">An async-enumerable sequence to create a lookup for.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="elementCaptureValue">The extra value that will be passed to <paramref name="elementSelector"/>.</param>
        /// <param name="elementSelector">A transform function to produce a result element value from each element.</param>
        /// <param name="comparer">An equality comparer to compare keys.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>A <see cref="Promise{T}"/> whose result will be a lookup mapping unique key values onto the corresponding source sequence's elements.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="elementSelector"/> or <paramref name="comparer"/> is null.</exception>
        public static Promise<ILookup<TKey, TElement>> ToLookupAsync<TSource, TCaptureKey, TKey, TCaptureElement, TElement, TEqualityComparer>(
            this AsyncEnumerable<TSource> source,
            TCaptureKey keyCaptureValue,
            Func<TCaptureKey, TSource, TKey> keySelector,
            TCaptureElement elementCaptureValue,
            Func<TCaptureElement, TSource, TElement> elementSelector,
            TEqualityComparer comparer,
            CancelationToken cancelationToken = default)
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(elementSelector, nameof(elementSelector), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.LookupHelper<TKey, TElement>.GetOrCreateAsync(source.GetAsyncEnumerator(cancelationToken),
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
            => ToLookupAsync<TSource, TKey, TElement, IEqualityComparer<TKey>>(source, keySelector, elementSelector, EqualityComparer<TKey>.Default, cancelationToken);

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
            => ToLookupAsync<TSource, TCaptureKey, TKey, TElement, IEqualityComparer<TKey>>(source, keyCaptureValue, keySelector, elementSelector, EqualityComparer<TKey>.Default, cancelationToken);

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
            => ToLookupAsync<TSource, TKey, TCaptureElement, TElement, IEqualityComparer<TKey>>(source, keySelector, elementCaptureValue, elementSelector, EqualityComparer<TKey>.Default, cancelationToken);

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
            => ToLookupAsync<TSource, TCaptureKey, TKey, TCaptureElement, TElement, IEqualityComparer<TKey>>(source, keyCaptureValue, keySelector, elementCaptureValue, elementSelector, EqualityComparer<TKey>.Default, cancelationToken);

        /// <summary>
        /// Creates a lookup from an async-enumerable sequence by invoking key and element selector functions on each source element and awaiting the results.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the lookup key computed for each element in the source sequence.</typeparam>
        /// <typeparam name="TElement">The type of the lookup value computed for each element in the source sequence.</typeparam>
        /// <typeparam name="TEqualityComparer">The type of the comparer.</typeparam>
        /// <param name="source">An async-enumerable sequence to create a lookup for.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from each element.</param>
        /// <param name="elementSelector">An asynchronous transform function to produce a result element value from each source element.</param>
        /// <param name="comparer">An equality comparer to compare keys.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>A <see cref="Promise{T}"/> whose result will be a lookup mapping unique key values onto the corresponding source sequence's elements.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="elementSelector"/> or <paramref name="comparer"/> is null.</exception>
        public static Promise<ILookup<TKey, TElement>> ToLookupAsync<TSource, TKey, TElement, TEqualityComparer>(
            this AsyncEnumerable<TSource> source,
            Func<TSource, Promise<TKey>> keySelector,
            Func<TSource, Promise<TElement>> elementSelector,
            TEqualityComparer comparer,
            CancelationToken cancelationToken = default)
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(elementSelector, nameof(elementSelector), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.LookupHelper<TKey, TElement>.GetOrCreateAwaitAsync(source.GetAsyncEnumerator(cancelationToken),
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
        /// <typeparam name="TEqualityComparer">The type of the comparer.</typeparam>
        /// <param name="source">An async-enumerable sequence to create a lookup for.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from each element.</param>
        /// <param name="elementSelector">An asynchronous transform function to produce a result element value from each source element.</param>
        /// <param name="comparer">An equality comparer to compare keys.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>A <see cref="Promise{T}"/> whose result will be a lookup mapping unique key values onto the corresponding source sequence's elements.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="elementSelector"/> or <paramref name="comparer"/> is null.</exception>
        public static Promise<ILookup<TKey, TElement>> ToLookupAsync<TSource, TCaptureKey, TKey, TElement, TEqualityComparer>(
            this AsyncEnumerable<TSource> source,
            TCaptureKey keyCaptureValue,
            Func<TCaptureKey, TSource, Promise<TKey>> keySelector,
            Func<TSource, Promise<TElement>> elementSelector,
            TEqualityComparer comparer,
            CancelationToken cancelationToken = default)
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(elementSelector, nameof(elementSelector), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.LookupHelper<TKey, TElement>.GetOrCreateAwaitAsync(source.GetAsyncEnumerator(cancelationToken),
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
        /// <typeparam name="TEqualityComparer">The type of the comparer.</typeparam>
        /// <param name="source">An async-enumerable sequence to create a lookup for.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from each element.</param>
        /// <param name="elementCaptureValue">The extra value that will be passed to <paramref name="elementSelector"/>.</param>
        /// <param name="elementSelector">An asynchronous transform function to produce a result element value from each source element.</param>
        /// <param name="comparer">An equality comparer to compare keys.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>A <see cref="Promise{T}"/> whose result will be a lookup mapping unique key values onto the corresponding source sequence's elements.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="elementSelector"/> or <paramref name="comparer"/> is null.</exception>
        public static Promise<ILookup<TKey, TElement>> ToLookupAsync<TSource, TKey, TCaptureElement, TElement, TEqualityComparer>(
            this AsyncEnumerable<TSource> source,
            Func<TSource, Promise<TKey>> keySelector,
            TCaptureElement elementCaptureValue,
            Func<TCaptureElement, TSource, Promise<TElement>> elementSelector,
            TEqualityComparer comparer,
            CancelationToken cancelationToken = default)
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(elementSelector, nameof(elementSelector), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.LookupHelper<TKey, TElement>.GetOrCreateAwaitAsync(source.GetAsyncEnumerator(cancelationToken),
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
        /// <typeparam name="TEqualityComparer">The type of the comparer.</typeparam>
        /// <param name="source">An async-enumerable sequence to create a lookup for.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from each element.</param>
        /// <param name="elementCaptureValue">The extra value that will be passed to <paramref name="elementSelector"/>.</param>
        /// <param name="elementSelector">An asynchronous transform function to produce a result element value from each source element.</param>
        /// <param name="comparer">An equality comparer to compare keys.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>A <see cref="Promise{T}"/> whose result will be a lookup mapping unique key values onto the corresponding source sequence's elements.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="elementSelector"/> or <paramref name="comparer"/> is null.</exception>
        public static Promise<ILookup<TKey, TElement>> ToLookupAsync<TSource, TCaptureKey, TKey, TCaptureElement, TElement, TEqualityComparer>(
            this AsyncEnumerable<TSource> source,
            TCaptureKey keyCaptureValue,
            Func<TCaptureKey, TSource, Promise<TKey>> keySelector,
            TCaptureElement elementCaptureValue,
            Func<TCaptureElement, TSource, Promise<TElement>> elementSelector,
            TEqualityComparer comparer,
            CancelationToken cancelationToken = default)
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(elementSelector, nameof(elementSelector), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.LookupHelper<TKey, TElement>.GetOrCreateAwaitAsync(source.GetAsyncEnumerator(cancelationToken),
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
            this in ConfiguredAsyncEnumerable<TSource> configuredSource,
            Func<TSource, TKey> keySelector)
            => ToLookupAsync(configuredSource, keySelector, EqualityComparer<TKey>.Default);

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
            this in ConfiguredAsyncEnumerable<TSource> configuredSource,
            TCaptureKey keyCaptureValue,
            Func<TCaptureKey, TSource, TKey> keySelector)
            => ToLookupAsync(configuredSource, keyCaptureValue, keySelector, EqualityComparer<TKey>.Default);

        /// <summary>
        /// Creates a lookup from a configured async-enumerable sequence according to a specified key selector function, and a comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the lookup key computed for each element in the source sequence.</typeparam>
        /// <typeparam name="TEqualityComparer">The type of the comparer.</typeparam>
        /// <param name="configuredSource">A configured async-enumerable sequence to create a lookup for.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="comparer">An equality comparer to compare keys.</param>
        /// <returns>A <see cref="Promise{T}"/> whose result will be a lookup mapping unique key values onto the corresponding source sequence's elements.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="comparer"/> is null.</exception>
        public static Promise<ILookup<TKey, TSource>> ToLookupAsync<TSource, TKey, TEqualityComparer>(
            this in ConfiguredAsyncEnumerable<TSource> configuredSource,
            Func<TSource, TKey> keySelector,
            TEqualityComparer comparer)
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.LookupHelper<TKey, TSource>.GetOrCreateAsync(configuredSource.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(keySelector), comparer);
        }

        /// <summary>
        /// Creates a lookup from a configured async-enumerable sequence according to a specified key selector function, and a comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to the key selector.</typeparam>
        /// <typeparam name="TKey">The type of the lookup key computed for each element in the source sequence.</typeparam>
        /// <typeparam name="TEqualityComparer">The type of the comparer.</typeparam>
        /// <param name="configuredSource">A configured async-enumerable sequence to create a lookup for.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="comparer">An equality comparer to compare keys.</param>
        /// <returns>A <see cref="Promise{T}"/> whose result will be a lookup mapping unique key values onto the corresponding source sequence's elements.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="comparer"/> is null.</exception>
        public static Promise<ILookup<TKey, TSource>> ToLookupAsync<TSource, TCaptureKey, TKey, TEqualityComparer>(
            this in ConfiguredAsyncEnumerable<TSource> configuredSource,
            TCaptureKey keyCaptureValue,
            Func<TCaptureKey, TSource, TKey> keySelector,
            TEqualityComparer comparer)
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.LookupHelper<TKey, TSource>.GetOrCreateAsync(configuredSource.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(keyCaptureValue, keySelector), comparer);
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
            this in ConfiguredAsyncEnumerable<TSource> configuredSource,
            Func<TSource, Promise<TKey>> keySelector)
            => ToLookupAsync<TSource, TKey, IEqualityComparer<TKey>>(configuredSource, keySelector, EqualityComparer<TKey>.Default);

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
            this in ConfiguredAsyncEnumerable<TSource> configuredSource,
            TCaptureKey keyCaptureValue,
            Func<TCaptureKey, TSource, Promise<TKey>> keySelector)
            => ToLookupAsync<TSource, TCaptureKey, TKey, IEqualityComparer<TKey>>(configuredSource, keyCaptureValue, keySelector, EqualityComparer<TKey>.Default);

        /// <summary>
        /// Creates a lookup from a configured async-enumerable sequence by invoking a key-selector function on each element and awaiting the result.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the lookup key computed for each element in the source sequence.</typeparam>
        /// <typeparam name="TEqualityComparer">The type of the comparer.</typeparam>
        /// <param name="configuredSource">A configured async-enumerable sequence to create a lookup for.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from each element.</param>
        /// <param name="comparer">An equality comparer to compare keys.</param>
        /// <returns>A <see cref="Promise{T}"/> whose result will be a lookup mapping unique key values onto the corresponding source sequence's elements.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="comparer"/> is null.</exception>
        public static Promise<ILookup<TKey, TSource>> ToLookupAsync<TSource, TKey, TEqualityComparer>(
            this in ConfiguredAsyncEnumerable<TSource> configuredSource,
            Func<TSource, Promise<TKey>> keySelector,
            TEqualityComparer comparer)
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.LookupHelper<TKey, TSource>.GetOrCreateAwaitAsync(configuredSource.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(keySelector), comparer);
        }

        /// <summary>
        /// Creates a lookup from a configured async-enumerable sequence by invoking a key-selector function on each element and awaiting the result.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to the key selector.</typeparam>
        /// <typeparam name="TKey">The type of the lookup key computed for each element in the source sequence.</typeparam>
        /// <typeparam name="TEqualityComparer">The type of the comparer.</typeparam>
        /// <param name="configuredSource">A configured async-enumerable sequence to create a lookup for.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from each element.</param>
        /// <param name="comparer">An equality comparer to compare keys.</param>
        /// <returns>A <see cref="Promise{T}"/> whose result will be a lookup mapping unique key values onto the corresponding source sequence's elements.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="comparer"/> is null.</exception>
        public static Promise<ILookup<TKey, TSource>> ToLookupAsync<TSource, TCaptureKey, TKey, TEqualityComparer>(
            this in ConfiguredAsyncEnumerable<TSource> configuredSource,
            TCaptureKey keyCaptureValue,
            Func<TCaptureKey, TSource, Promise<TKey>> keySelector,
            TEqualityComparer comparer)
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.LookupHelper<TKey, TSource>.GetOrCreateAwaitAsync(configuredSource.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(keyCaptureValue, keySelector), comparer);
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
            this in ConfiguredAsyncEnumerable<TSource> configuredSource,
            Func<TSource, TKey> keySelector,
            Func<TSource, TElement> elementSelector)
            => ToLookupAsync(configuredSource, keySelector, elementSelector, EqualityComparer<TKey>.Default);

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
            this in ConfiguredAsyncEnumerable<TSource> configuredSource,
            TCaptureKey keyCaptureValue,
            Func<TCaptureKey, TSource, TKey> keySelector,
            Func<TSource, TElement> elementSelector)
            => ToLookupAsync(configuredSource, keyCaptureValue, keySelector, elementSelector, EqualityComparer<TKey>.Default);

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
            this in ConfiguredAsyncEnumerable<TSource> configuredSource,
            Func<TSource, TKey> keySelector,
            TCaptureElement elementCaptureValue,
            Func<TCaptureElement, TSource, TElement> elementSelector)
            => ToLookupAsync(configuredSource, keySelector, elementCaptureValue, elementSelector, EqualityComparer<TKey>.Default);

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
            this in ConfiguredAsyncEnumerable<TSource> configuredSource,
            TCaptureKey keyCaptureValue,
            Func<TCaptureKey, TSource, TKey> keySelector,
            TCaptureElement elementCaptureValue,
            Func<TCaptureElement, TSource, TElement> elementSelector)
            => ToLookupAsync(configuredSource, keyCaptureValue, keySelector, elementCaptureValue, elementSelector, EqualityComparer<TKey>.Default);

        /// <summary>
        /// Creates a lookup from a configured async-enumerable sequence according to a specified key selector function, a comparer, and an element selector function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the lookup key computed for each element in the source sequence.</typeparam>
        /// <typeparam name="TElement">The type of the lookup value computed for each element in the source sequence.</typeparam>
        /// <typeparam name="TEqualityComparer">The type of the comparer.</typeparam>
        /// <param name="configuredSource">A configured async-enumerable sequence to create a lookup for.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="elementSelector">A transform function to produce a result element value from each element.</param>
        /// <param name="comparer">An equality comparer to compare keys.</param>
        /// <returns>A <see cref="Promise{T}"/> whose result will be a lookup mapping unique key values onto the corresponding source sequence's elements.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="elementSelector"/> or <paramref name="comparer"/> is null.</exception>
        public static Promise<ILookup<TKey, TElement>> ToLookupAsync<TSource, TKey, TElement, TEqualityComparer>(
            this in ConfiguredAsyncEnumerable<TSource> configuredSource,
            Func<TSource, TKey> keySelector,
            Func<TSource, TElement> elementSelector,
            TEqualityComparer comparer)
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(elementSelector, nameof(elementSelector), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.LookupHelper<TKey, TElement>.GetOrCreateAsync(configuredSource.GetAsyncEnumerator(),
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
        /// <typeparam name="TEqualityComparer">The type of the comparer.</typeparam>
        /// <param name="configuredSource">A configured async-enumerable sequence to create a lookup for.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="elementSelector">A transform function to produce a result element value from each element.</param>
        /// <param name="comparer">An equality comparer to compare keys.</param>
        /// <returns>A <see cref="Promise{T}"/> whose result will be a lookup mapping unique key values onto the corresponding source sequence's elements.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="elementSelector"/> or <paramref name="comparer"/> is null.</exception>
        public static Promise<ILookup<TKey, TElement>> ToLookupAsync<TSource, TCaptureKey, TKey, TElement, TEqualityComparer>(
            this in ConfiguredAsyncEnumerable<TSource> configuredSource,
            TCaptureKey keyCaptureValue,
            Func<TCaptureKey, TSource, TKey> keySelector,
            Func<TSource, TElement> elementSelector,
            TEqualityComparer comparer)
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(elementSelector, nameof(elementSelector), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.LookupHelper<TKey, TElement>.GetOrCreateAsync(configuredSource.GetAsyncEnumerator(),
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
        /// <typeparam name="TEqualityComparer">The type of the comparer.</typeparam>
        /// <param name="configuredSource">A configured async-enumerable sequence to create a lookup for.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="elementCaptureValue">The extra value that will be passed to <paramref name="elementSelector"/>.</param>
        /// <param name="elementSelector">A transform function to produce a result element value from each element.</param>
        /// <param name="comparer">An equality comparer to compare keys.</param>
        /// <returns>A <see cref="Promise{T}"/> whose result will be a lookup mapping unique key values onto the corresponding source sequence's elements.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="elementSelector"/> or <paramref name="comparer"/> is null.</exception>
        public static Promise<ILookup<TKey, TElement>> ToLookupAsync<TSource, TKey, TCaptureElement, TElement, TEqualityComparer>(
            this in ConfiguredAsyncEnumerable<TSource> configuredSource,
            Func<TSource, TKey> keySelector,
            TCaptureElement elementCaptureValue,
            Func<TCaptureElement, TSource, TElement> elementSelector,
            TEqualityComparer comparer)
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(elementSelector, nameof(elementSelector), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.LookupHelper<TKey, TElement>.GetOrCreateAsync(configuredSource.GetAsyncEnumerator(),
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
        /// <typeparam name="TEqualityComparer">The type of the comparer.</typeparam>
        /// <param name="configuredSource">A configured async-enumerable sequence to create a lookup for.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="elementCaptureValue">The extra value that will be passed to <paramref name="elementSelector"/>.</param>
        /// <param name="elementSelector">A transform function to produce a result element value from each element.</param>
        /// <param name="comparer">An equality comparer to compare keys.</param>
        /// <returns>A <see cref="Promise{T}"/> whose result will be a lookup mapping unique key values onto the corresponding source sequence's elements.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="elementSelector"/> or <paramref name="comparer"/> is null.</exception>
        public static Promise<ILookup<TKey, TElement>> ToLookupAsync<TSource, TCaptureKey, TKey, TCaptureElement, TElement, TEqualityComparer>(
            this in ConfiguredAsyncEnumerable<TSource> configuredSource,
            TCaptureKey keyCaptureValue,
            Func<TCaptureKey, TSource, TKey> keySelector,
            TCaptureElement elementCaptureValue,
            Func<TCaptureElement, TSource, TElement> elementSelector,
            TEqualityComparer comparer)
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(elementSelector, nameof(elementSelector), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.LookupHelper<TKey, TElement>.GetOrCreateAsync(configuredSource.GetAsyncEnumerator(),
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
            this in ConfiguredAsyncEnumerable<TSource> configuredSource,
            Func<TSource, Promise<TKey>> keySelector,
            Func<TSource, Promise<TElement>> elementSelector)
            => ToLookupAsync<TSource, TKey, TElement, IEqualityComparer<TKey>>(configuredSource, keySelector, elementSelector, EqualityComparer<TKey>.Default);

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
            this in ConfiguredAsyncEnumerable<TSource> configuredSource,
            TCaptureKey keyCaptureValue,
            Func<TCaptureKey, TSource, Promise<TKey>> keySelector,
            Func<TSource, Promise<TElement>> elementSelector)
            => ToLookupAsync<TSource, TCaptureKey, TKey, TElement, IEqualityComparer<TKey>>(configuredSource, keyCaptureValue, keySelector, elementSelector, EqualityComparer<TKey>.Default);

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
            this in ConfiguredAsyncEnumerable<TSource> configuredSource,
            Func<TSource, Promise<TKey>> keySelector,
            TCaptureElement elementCaptureValue,
            Func<TCaptureElement, TSource, Promise<TElement>> elementSelector)
            => ToLookupAsync<TSource, TKey, TCaptureElement, TElement, IEqualityComparer<TKey>>(configuredSource, keySelector, elementCaptureValue, elementSelector, EqualityComparer<TKey>.Default);

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
            this in ConfiguredAsyncEnumerable<TSource> configuredSource,
            TCaptureKey keyCaptureValue,
            Func<TCaptureKey, TSource, Promise<TKey>> keySelector,
            TCaptureElement elementCaptureValue,
            Func<TCaptureElement, TSource, Promise<TElement>> elementSelector)
            => ToLookupAsync<TSource, TCaptureKey, TKey, TCaptureElement, TElement, IEqualityComparer<TKey>>(configuredSource, keyCaptureValue, keySelector, elementCaptureValue, elementSelector, EqualityComparer<TKey>.Default);

        /// <summary>
        /// Creates a lookup from a configured async-enumerable sequence by invoking key and element selector functions on each source element and awaiting the results.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the lookup key computed for each element in the source sequence.</typeparam>
        /// <typeparam name="TElement">The type of the lookup value computed for each element in the source sequence.</typeparam>
        /// <typeparam name="TEqualityComparer">The type of the comparer.</typeparam>
        /// <param name="configuredSource">A configured async-enumerable sequence to create a lookup for.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from each element.</param>
        /// <param name="elementSelector">An asynchronous transform function to produce a result element value from each source element.</param>
        /// <param name="comparer">An equality comparer to compare keys.</param>
        /// <returns>A <see cref="Promise{T}"/> whose result will be a lookup mapping unique key values onto the corresponding source sequence's elements.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="elementSelector"/> or <paramref name="comparer"/> is null.</exception>
        public static Promise<ILookup<TKey, TElement>> ToLookupAsync<TSource, TKey, TElement, TEqualityComparer>(
            this in ConfiguredAsyncEnumerable<TSource> configuredSource,
            Func<TSource, Promise<TKey>> keySelector,
            Func<TSource, Promise<TElement>> elementSelector,
            TEqualityComparer comparer)
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(elementSelector, nameof(elementSelector), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.LookupHelper<TKey, TElement>.GetOrCreateAwaitAsync(configuredSource.GetAsyncEnumerator(),
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
        /// <typeparam name="TEqualityComparer">The type of the comparer.</typeparam>
        /// <param name="configuredSource">A configured async-enumerable sequence to create a lookup for.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from each element.</param>
        /// <param name="elementSelector">An asynchronous transform function to produce a result element value from each source element.</param>
        /// <param name="comparer">An equality comparer to compare keys.</param>
        /// <returns>A <see cref="Promise{T}"/> whose result will be a lookup mapping unique key values onto the corresponding source sequence's elements.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="elementSelector"/> or <paramref name="comparer"/> is null.</exception>
        public static Promise<ILookup<TKey, TElement>> ToLookupAsync<TSource, TCaptureKey, TKey, TElement, TEqualityComparer>(
            this in ConfiguredAsyncEnumerable<TSource> configuredSource,
            TCaptureKey keyCaptureValue,
            Func<TCaptureKey, TSource, Promise<TKey>> keySelector,
            Func<TSource, Promise<TElement>> elementSelector,
            TEqualityComparer comparer)
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(elementSelector, nameof(elementSelector), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.LookupHelper<TKey, TElement>.GetOrCreateAwaitAsync(configuredSource.GetAsyncEnumerator(),
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
        /// <typeparam name="TEqualityComparer">The type of the comparer.</typeparam>
        /// <param name="configuredSource">A configured async-enumerable sequence to create a lookup for.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from each element.</param>
        /// <param name="elementCaptureValue">The extra value that will be passed to <paramref name="elementSelector"/>.</param>
        /// <param name="elementSelector">An asynchronous transform function to produce a result element value from each source element.</param>
        /// <param name="comparer">An equality comparer to compare keys.</param>
        /// <returns>A <see cref="Promise{T}"/> whose result will be a lookup mapping unique key values onto the corresponding source sequence's elements.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="elementSelector"/> or <paramref name="comparer"/> is null.</exception>
        public static Promise<ILookup<TKey, TElement>> ToLookupAsync<TSource, TKey, TCaptureElement, TElement, TEqualityComparer>(
            this in ConfiguredAsyncEnumerable<TSource> configuredSource,
            Func<TSource, Promise<TKey>> keySelector,
            TCaptureElement elementCaptureValue,
            Func<TCaptureElement, TSource, Promise<TElement>> elementSelector,
            TEqualityComparer comparer)
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(elementSelector, nameof(elementSelector), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.LookupHelper<TKey, TElement>.GetOrCreateAwaitAsync(configuredSource.GetAsyncEnumerator(),
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
        /// <typeparam name="TEqualityComparer">The type of the comparer.</typeparam>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from each element.</param>
        /// <param name="elementCaptureValue">The extra value that will be passed to <paramref name="elementSelector"/>.</param>
        /// <param name="elementSelector">An asynchronous transform function to produce a result element value from each source element.</param>
        /// <param name="comparer">An equality comparer to compare keys.</param>
        /// <returns>A <see cref="Promise{T}"/> whose result will be a lookup mapping unique key values onto the corresponding source sequence's elements.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="elementSelector"/> or <paramref name="comparer"/> is null.</exception>
        public static Promise<ILookup<TKey, TElement>> ToLookupAsync<TSource, TCaptureKey, TKey, TCaptureElement, TElement, TEqualityComparer>(
            this in ConfiguredAsyncEnumerable<TSource> configuredSource,
            TCaptureKey keyCaptureValue,
            Func<TCaptureKey, TSource, Promise<TKey>> keySelector,
            TCaptureElement elementCaptureValue,
            Func<TCaptureElement, TSource, Promise<TElement>> elementSelector,
            TEqualityComparer comparer)
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(elementSelector, nameof(elementSelector), 1);
            ValidateArgument(comparer, nameof(comparer), 1);

            return Internal.LookupHelper<TKey, TElement>.GetOrCreateAwaitAsync(configuredSource.GetAsyncEnumerator(),
                Internal.PromiseRefBase.DelegateWrapper.Create(keyCaptureValue, keySelector),
                Internal.PromiseRefBase.DelegateWrapper.Create(elementCaptureValue, elementSelector),
                comparer);
        }
        #endregion ConfiguredAsyncEnumerable
    }
#endif // CSHARP_7_3_OR_NEWER
}