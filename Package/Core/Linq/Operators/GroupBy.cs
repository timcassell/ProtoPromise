#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using Proto.Promises.Async.CompilerServices;
using Proto.Promises.Collections;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Proto.Promises.Linq
{
#if CSHARP_7_3_OR_NEWER
    /// <summary>
    /// A temporary collection of objects that have a common key.
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <typeparam name="TElement">The type of the elements.</typeparam>
    /// <remarks>
    /// The temporary collection will no longer be valid when the <see cref="AsyncEnumerator{T}"/>
    /// that produced the <see cref="Grouping{TKey, TElement}"/> is completed.
    /// Copy the elements to a new collection or call <see cref="TempCollection{T}.ToArray"/>
    /// or <see cref="TempCollection{T}.ToList"/> if you need the elements to persist.
    /// </remarks>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    // We use this struct with a TempCollection instead of IGrouping to avoid extra allocations.
    public readonly struct Grouping<TKey, TElement> : IGrouping<TKey, TElement>
    {
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
        private readonly TKey _key;
        private readonly TempCollection<TElement> _elements;
#else
        // We use the reference in RELEASE mode to reduce the size of the struct.
        // TempCollection is not validated in release mode for performance reasons,
        // so it's fine for this to also not be validated.
        private readonly Internal.Grouping<TKey, TElement> _grouping;
#endif

        /// <summary>
        /// Gets the key of the <see cref="Grouping{TKey, TElement}"/>.
        /// </summary>
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
        public TKey Key => _key;
#else
        public TKey Key => _grouping.Key;
#endif

        /// <summary>
        /// Gets the temporary collection of elements of the <see cref="Grouping{TKey, TElement}"/>.
        /// </summary>
        /// <remarks>
        /// The temporary collection will no longer be valid when the <see cref="AsyncEnumerator{T}"/>
        /// that produced this <see cref="Grouping{TKey, TElement}"/> is completed.
        /// Copy the elements to a new collection or call <see cref="TempCollection{T}.ToArray"/>
        /// or <see cref="TempCollection{T}.ToList"/> if you need the elements to persist.
        /// </remarks>
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
        public TempCollection<TElement> Elements => _elements;
#else
        public TempCollection<TElement> Elements => _grouping._elements.View;
#endif

        internal Grouping(Internal.Grouping<TKey, TElement> grouping)
        {
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
            _key = grouping.Key;
            _elements = grouping._elements.View;
#else
            _grouping = grouping;
#endif
        }

        TKey IGrouping<TKey, TElement>.Key => Key;
        IEnumerator<TElement> IEnumerable<TElement>.GetEnumerator() => Elements.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => Elements.GetEnumerator();
    }

    partial class AsyncEnumerable
    {
        #region AsyncEnumerable
        /// <summary>
        /// Groups the elements of an async-enumerable sequence according to a specified key selector function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the lookup key computed for each element in the source sequence.</typeparam>
        /// <param name="source">An async-enumerable sequence to create a lookup for.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <returns>An async-enumerable sequence of groups, each of which corresponds to a unique key value, containing all elements that share that same key value.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        public static AsyncEnumerable<Grouping<TKey, TSource>> GroupBy<TSource, TKey>(
            this AsyncEnumerable<TSource> source,
            Func<TSource, TKey> keySelector)
            => GroupBy(source, keySelector, comparer: null);

        /// <summary>
        /// Groups the elements of an async-enumerable sequence according to a specified key selector function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to the key selector.</typeparam>
        /// <typeparam name="TKey">The type of the lookup key computed for each element in the source sequence.</typeparam>
        /// <param name="source">An async-enumerable sequence to create a lookup for.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <returns>An async-enumerable sequence of groups, each of which corresponds to a unique key value, containing all elements that share that same key value.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        public static AsyncEnumerable<Grouping<TKey, TSource>> GroupBy<TSource, TCaptureKey, TKey>(
            this AsyncEnumerable<TSource> source,
            TCaptureKey keyCaptureValue,
            Func<TCaptureKey, TSource, TKey> keySelector)
            => GroupBy(source, keyCaptureValue, keySelector, comparer: null);

        /// <summary>
        /// Groups the elements of an async-enumerable sequence according to a specified key selector function, and a comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the lookup key computed for each element in the source sequence.</typeparam>
        /// <param name="source">An async-enumerable sequence to create a lookup for.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="comparer">An equality comparer to compare keys. If null, the default equality comparer will be used.</param>
        /// <returns>An async-enumerable sequence of groups, each of which corresponds to a unique key value, containing all elements that share that same key value.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        public static AsyncEnumerable<Grouping<TKey, TSource>> GroupBy<TSource, TKey>(
            this AsyncEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            IEqualityComparer<TKey> comparer)
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);

            return Internal.Lookup<TKey, TSource>.GroupBy(source.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(keySelector), comparer);
        }

        /// <summary>
        /// Groups the elements of an async-enumerable sequence according to a specified key selector function, and a comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to the key selector.</typeparam>
        /// <typeparam name="TKey">The type of the lookup key computed for each element in the source sequence.</typeparam>
        /// <param name="source">An async-enumerable sequence to create a lookup for.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="comparer">An equality comparer to compare keys. If null, the default equality comparer will be used.</param>
        /// <returns>An async-enumerable sequence of groups, each of which corresponds to a unique key value, containing all elements that share that same key value.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        public static AsyncEnumerable<Grouping<TKey, TSource>> GroupBy<TSource, TCaptureKey, TKey>(
            this AsyncEnumerable<TSource> source,
            TCaptureKey keyCaptureValue,
            Func<TCaptureKey, TSource, TKey> keySelector,
            IEqualityComparer<TKey> comparer)
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);

            return Internal.Lookup<TKey, TSource>.GroupBy(source.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(keyCaptureValue, keySelector), comparer);
        }

        /// <summary>
        /// Groups the elements of an async-enumerable sequence by invoking a key-selector function on each element and awaiting the result.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the lookup key computed for each element in the source sequence.</typeparam>
        /// <param name="source">An async-enumerable sequence to create a lookup for.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from each element.</param>
        /// <returns>An async-enumerable sequence of groups, each of which corresponds to a unique key value, containing all elements that share that same key value.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        public static AsyncEnumerable<Grouping<TKey, TSource>> GroupBy<TSource, TKey>(
            this AsyncEnumerable<TSource> source,
            Func<TSource, Promise<TKey>> keySelector)
            => GroupBy<TSource, TKey>(source, keySelector, comparer: null);

        /// <summary>
        /// Groups the elements of an async-enumerable sequence by invoking a key-selector function on each element and awaiting the result.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to the key selector.</typeparam>
        /// <typeparam name="TKey">The type of the lookup key computed for each element in the source sequence.</typeparam>
        /// <param name="source">An async-enumerable sequence to create a lookup for.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from each element.</param>
        /// <returns>An async-enumerable sequence of groups, each of which corresponds to a unique key value, containing all elements that share that same key value.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        public static AsyncEnumerable<Grouping<TKey, TSource>> GroupBy<TSource, TCaptureKey, TKey>(
            this AsyncEnumerable<TSource> source,
            TCaptureKey keyCaptureValue,
            Func<TCaptureKey, TSource, Promise<TKey>> keySelector)
            => GroupBy<TSource, TCaptureKey, TKey>(source, keyCaptureValue, keySelector, comparer: null);

        /// <summary>
        /// Groups the elements of an async-enumerable sequence by invoking a key-selector function on each element and awaiting the result.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the lookup key computed for each element in the source sequence.</typeparam>
        /// <param name="source">An async-enumerable sequence to create a lookup for.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from each element.</param>
        /// <param name="comparer">An equality comparer to compare keys. If null, the default equality comparer will be used.</param>
        /// <returns>An async-enumerable sequence of groups, each of which corresponds to a unique key value, containing all elements that share that same key value.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        public static AsyncEnumerable<Grouping<TKey, TSource>> GroupBy<TSource, TKey>(
            this AsyncEnumerable<TSource> source,
            Func<TSource, Promise<TKey>> keySelector,
            IEqualityComparer<TKey> comparer)
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);

            return Internal.Lookup<TKey, TSource>.GroupByAwait(source.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(keySelector), comparer);
        }

        /// <summary>
        /// Groups the elements of an async-enumerable sequence by invoking a key-selector function on each element and awaiting the result.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to the key selector.</typeparam>
        /// <typeparam name="TKey">The type of the lookup key computed for each element in the source sequence.</typeparam>
        /// <param name="source">An async-enumerable sequence to create a lookup for.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from each element.</param>
        /// <param name="comparer">An equality comparer to compare keys. If null, the default equality comparer will be used.</param>
        /// <returns>An async-enumerable sequence of groups, each of which corresponds to a unique key value, containing all elements that share that same key value.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        public static AsyncEnumerable<Grouping<TKey, TSource>> GroupBy<TSource, TCaptureKey, TKey>(
            this AsyncEnumerable<TSource> source,
            TCaptureKey keyCaptureValue,
            Func<TCaptureKey, TSource, Promise<TKey>> keySelector,
            IEqualityComparer<TKey> comparer)
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);

            return Internal.Lookup<TKey, TSource>.GroupByAwait(source.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(keyCaptureValue, keySelector), comparer);
        }

        /// <summary>
        /// Groups the elements of an async-enumerable sequence according to a specified key selector function, and an element selector function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the lookup key computed for each element in the source sequence.</typeparam>
        /// <typeparam name="TElement">The type of the lookup value computed for each element in the source sequence.</typeparam>
        /// <param name="source">An async-enumerable sequence to create a lookup for.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="elementSelector">A transform function to produce a result element value from each element.</param>
        /// <returns>An async-enumerable sequence of groups, each of which corresponds to a unique key value, containing all elements that share that same key value.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="elementSelector"/> is null.</exception>
        public static AsyncEnumerable<Grouping<TKey, TElement>> GroupBy<TSource, TKey, TElement>(
            this AsyncEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            Func<TSource, TElement> elementSelector)
            => GroupBy(source, keySelector, elementSelector, comparer: null);

        /// <summary>
        /// Groups the elements of an async-enumerable sequence according to a specified key selector function, and an element selector function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to the key selector.</typeparam>
        /// <typeparam name="TKey">The type of the lookup key computed for each element in the source sequence.</typeparam>
        /// <typeparam name="TElement">The type of the lookup value computed for each element in the source sequence.</typeparam>
        /// <param name="source">An async-enumerable sequence to create a lookup for.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="elementSelector">A transform function to produce a result element value from each element.</param>
        /// <returns>An async-enumerable sequence of groups, each of which corresponds to a unique key value, containing all elements that share that same key value.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="elementSelector"/> is null.</exception>
        public static AsyncEnumerable<Grouping<TKey, TElement>> GroupBy<TSource, TCaptureKey, TKey, TElement>(
            this AsyncEnumerable<TSource> source,
            TCaptureKey keyCaptureValue,
            Func<TCaptureKey, TSource, TKey> keySelector,
            Func<TSource, TElement> elementSelector)
            => GroupBy(source, keyCaptureValue, keySelector, elementSelector, comparer: null);

        /// <summary>
        /// Groups the elements of an async-enumerable sequence according to a specified key selector function, and an element selector function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the lookup key computed for each element in the source sequence.</typeparam>
        /// <typeparam name="TCaptureElement">The type of the captured value that will be passed to the element selector.</typeparam>
        /// <typeparam name="TElement">The type of the lookup value computed for each element in the source sequence.</typeparam>
        /// <param name="source">An async-enumerable sequence to create a lookup for.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="elementCaptureValue">The extra value that will be passed to <paramref name="elementSelector"/>.</param>
        /// <param name="elementSelector">A transform function to produce a result element value from each element.</param>
        /// <returns>An async-enumerable sequence of groups, each of which corresponds to a unique key value, containing all elements that share that same key value.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="elementSelector"/> is null.</exception>
        public static AsyncEnumerable<Grouping<TKey, TElement>> GroupBy<TSource, TKey, TCaptureElement, TElement>(
            this AsyncEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            TCaptureElement elementCaptureValue,
            Func<TCaptureElement, TSource, TElement> elementSelector)
            => GroupBy(source, keySelector, elementCaptureValue, elementSelector, comparer: null);

        /// <summary>
        /// Groups the elements of an async-enumerable sequence according to a specified key selector function, and an element selector function.
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
        /// <returns>An async-enumerable sequence of groups, each of which corresponds to a unique key value, containing all elements that share that same key value.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="elementSelector"/> is null.</exception>
        public static AsyncEnumerable<Grouping<TKey, TElement>> GroupBy<TSource, TCaptureKey, TKey, TCaptureElement, TElement>(
            this AsyncEnumerable<TSource> source,
            TCaptureKey keyCaptureValue,
            Func<TCaptureKey, TSource, TKey> keySelector,
            TCaptureElement elementCaptureValue,
            Func<TCaptureElement, TSource, TElement> elementSelector)
            => GroupBy(source, keyCaptureValue, keySelector, elementCaptureValue, elementSelector, comparer: null);

        /// <summary>
        /// Groups the elements of an async-enumerable sequence according to a specified key selector function, a comparer, and an element selector function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the lookup key computed for each element in the source sequence.</typeparam>
        /// <typeparam name="TElement">The type of the lookup value computed for each element in the source sequence.</typeparam>
        /// <param name="source">An async-enumerable sequence to create a lookup for.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="elementSelector">A transform function to produce a result element value from each element.</param>
        /// <param name="comparer">An equality comparer to compare keys. If null, the default equality comparer will be used.</param>
        /// <returns>An async-enumerable sequence of groups, each of which corresponds to a unique key value, containing all elements that share that same key value.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="elementSelector"/> is null.</exception>
        public static AsyncEnumerable<Grouping<TKey, TElement>> GroupBy<TSource, TKey, TElement>(
            this AsyncEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            Func<TSource, TElement> elementSelector,
            IEqualityComparer<TKey> comparer)
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(elementSelector, nameof(elementSelector), 1);

            return Internal.Lookup<TKey, TElement>.GroupBy(source.GetAsyncEnumerator(),
                Internal.PromiseRefBase.DelegateWrapper.Create(keySelector),
                Internal.PromiseRefBase.DelegateWrapper.Create(elementSelector),
                comparer);
        }

        /// <summary>
        /// Groups the elements of an async-enumerable sequence according to a specified key selector function, a comparer, and an element selector function.
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
        /// <returns>An async-enumerable sequence of groups, each of which corresponds to a unique key value, containing all elements that share that same key value.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="elementSelector"/> is null.</exception>
        public static AsyncEnumerable<Grouping<TKey, TElement>> GroupBy<TSource, TCaptureKey, TKey, TElement>(
            this AsyncEnumerable<TSource> source,
            TCaptureKey keyCaptureValue,
            Func<TCaptureKey, TSource, TKey> keySelector,
            Func<TSource, TElement> elementSelector,
            IEqualityComparer<TKey> comparer)
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(elementSelector, nameof(elementSelector), 1);

            return Internal.Lookup<TKey, TElement>.GroupBy(source.GetAsyncEnumerator(),
                Internal.PromiseRefBase.DelegateWrapper.Create(keyCaptureValue, keySelector),
                Internal.PromiseRefBase.DelegateWrapper.Create(elementSelector),
                comparer);
        }

        /// <summary>
        /// Groups the elements of an async-enumerable sequence according to a specified key selector function, a comparer, and an element selector function.
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
        /// <returns>An async-enumerable sequence of groups, each of which corresponds to a unique key value, containing all elements that share that same key value.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="elementSelector"/> is null.</exception>
        public static AsyncEnumerable<Grouping<TKey, TElement>> GroupBy<TSource, TKey, TCaptureElement, TElement>(
            this AsyncEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            TCaptureElement elementCaptureValue,
            Func<TCaptureElement, TSource, TElement> elementSelector,
            IEqualityComparer<TKey> comparer)
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(elementSelector, nameof(elementSelector), 1);

            return Internal.Lookup<TKey, TElement>.GroupBy(source.GetAsyncEnumerator(),
                Internal.PromiseRefBase.DelegateWrapper.Create(keySelector),
                Internal.PromiseRefBase.DelegateWrapper.Create(elementCaptureValue, elementSelector),
                comparer);
        }

        /// <summary>
        /// Groups the elements of an async-enumerable sequence according to a specified key selector function, a comparer, and an element selector function.
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
        /// <returns>An async-enumerable sequence of groups, each of which corresponds to a unique key value, containing all elements that share that same key value.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="elementSelector"/> is null.</exception>
        public static AsyncEnumerable<Grouping<TKey, TElement>> GroupBy<TSource, TCaptureKey, TKey, TCaptureElement, TElement>(
            this AsyncEnumerable<TSource> source,
            TCaptureKey keyCaptureValue,
            Func<TCaptureKey, TSource, TKey> keySelector,
            TCaptureElement elementCaptureValue,
            Func<TCaptureElement, TSource, TElement> elementSelector,
            IEqualityComparer<TKey> comparer)
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(elementSelector, nameof(elementSelector), 1);

            return Internal.Lookup<TKey, TElement>.GroupBy(source.GetAsyncEnumerator(),
                Internal.PromiseRefBase.DelegateWrapper.Create(keyCaptureValue, keySelector),
                Internal.PromiseRefBase.DelegateWrapper.Create(elementCaptureValue, elementSelector),
                comparer);
        }

        /// <summary>
        /// Groups the elements of an async-enumerable sequence by invoking key and element selector functions on each source element and awaiting the results.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the lookup key computed for each element in the source sequence.</typeparam>
        /// <typeparam name="TElement">The type of the lookup value computed for each element in the source sequence.</typeparam>
        /// <param name="source">An async-enumerable sequence to create a lookup for.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from each element.</param>
        /// <param name="elementSelector">An asynchronous transform function to produce a result element value from each element.</param>
        /// <returns>An async-enumerable sequence of groups, each of which corresponds to a unique key value, containing all elements that share that same key value.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="elementSelector"/> is null.</exception>
        public static AsyncEnumerable<Grouping<TKey, TElement>> GroupBy<TSource, TKey, TElement>(
            this AsyncEnumerable<TSource> source,
            Func<TSource, Promise<TKey>> keySelector,
            Func<TSource, Promise<TElement>> elementSelector)
            => GroupBy<TSource, TKey, TElement>(source, keySelector, elementSelector, comparer: null);

        /// <summary>
        /// Groups the elements of an async-enumerable sequence by invoking key and element selector functions on each source element and awaiting the results.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to the key selector.</typeparam>
        /// <typeparam name="TKey">The type of the lookup key computed for each element in the source sequence.</typeparam>
        /// <typeparam name="TElement">The type of the lookup value computed for each element in the source sequence.</typeparam>
        /// <param name="source">An async-enumerable sequence to create a lookup for.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from each element.</param>
        /// <param name="elementSelector">An asynchronous transform function to produce a result element value from each element.</param>
        /// <returns>An async-enumerable sequence of groups, each of which corresponds to a unique key value, containing all elements that share that same key value.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="elementSelector"/> is null.</exception>
        public static AsyncEnumerable<Grouping<TKey, TElement>> GroupBy<TSource, TCaptureKey, TKey, TElement>(
            this AsyncEnumerable<TSource> source,
            TCaptureKey keyCaptureValue,
            Func<TCaptureKey, TSource, Promise<TKey>> keySelector,
            Func<TSource, Promise<TElement>> elementSelector)
            => GroupBy<TSource, TCaptureKey, TKey, TElement>(source, keyCaptureValue, keySelector, elementSelector, comparer: null);

        /// <summary>
        /// Groups the elements of an async-enumerable sequence by invoking key and element selector functions on each source element and awaiting the results.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the lookup key computed for each element in the source sequence.</typeparam>
        /// <typeparam name="TCaptureElement">The type of the captured value that will be passed to the element selector.</typeparam>
        /// <typeparam name="TElement">The type of the lookup value computed for each element in the source sequence.</typeparam>
        /// <param name="source">An async-enumerable sequence to create a lookup for.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from each element.</param>
        /// <param name="elementCaptureValue">The extra value that will be passed to <paramref name="elementSelector"/>.</param>
        /// <param name="elementSelector">An asynchronous transform function to produce a result element value from each element.</param>
        /// <returns>An async-enumerable sequence of groups, each of which corresponds to a unique key value, containing all elements that share that same key value.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="elementSelector"/> is null.</exception>
        public static AsyncEnumerable<Grouping<TKey, TElement>> GroupBy<TSource, TKey, TCaptureElement, TElement>(
            this AsyncEnumerable<TSource> source,
            Func<TSource, Promise<TKey>> keySelector,
            TCaptureElement elementCaptureValue,
            Func<TCaptureElement, TSource, Promise<TElement>> elementSelector)
            => GroupBy<TSource, TKey, TCaptureElement, TElement>(source, keySelector, elementCaptureValue, elementSelector, comparer: null);

        /// <summary>
        /// Groups the elements of an async-enumerable sequence by invoking key and element selector functions on each source element and awaiting the results.
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
        /// <returns>An async-enumerable sequence of groups, each of which corresponds to a unique key value, containing all elements that share that same key value.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="elementSelector"/> is null.</exception>
        public static AsyncEnumerable<Grouping<TKey, TElement>> GroupBy<TSource, TCaptureKey, TKey, TCaptureElement, TElement>(
            this AsyncEnumerable<TSource> source,
            TCaptureKey keyCaptureValue,
            Func<TCaptureKey, TSource, Promise<TKey>> keySelector,
            TCaptureElement elementCaptureValue,
            Func<TCaptureElement, TSource, Promise<TElement>> elementSelector)
            => GroupBy<TSource, TCaptureKey, TKey, TCaptureElement, TElement>(source, keyCaptureValue, keySelector, elementCaptureValue, elementSelector, comparer: null);

        /// <summary>
        /// Groups the elements of an async-enumerable sequence by invoking key and element selector functions on each source element and awaiting the results.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the lookup key computed for each element in the source sequence.</typeparam>
        /// <typeparam name="TElement">The type of the lookup value computed for each element in the source sequence.</typeparam>
        /// <param name="source">An async-enumerable sequence to create a lookup for.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from each element.</param>
        /// <param name="elementSelector">An asynchronous transform function to produce a result element value from each source element.</param>
        /// <param name="comparer">An equality comparer to compare keys. If null, the default equality comparer will be used.</param>
        /// <returns>An async-enumerable sequence of groups, each of which corresponds to a unique key value, containing all elements that share that same key value.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="elementSelector"/> is null.</exception>
        public static AsyncEnumerable<Grouping<TKey, TElement>> GroupBy<TSource, TKey, TElement>(
            this AsyncEnumerable<TSource> source,
            Func<TSource, Promise<TKey>> keySelector,
            Func<TSource, Promise<TElement>> elementSelector,
            IEqualityComparer<TKey> comparer)
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(elementSelector, nameof(elementSelector), 1);

            return Internal.Lookup<TKey, TElement>.GroupByAwait(source.GetAsyncEnumerator(),
                Internal.PromiseRefBase.DelegateWrapper.Create(keySelector),
                Internal.PromiseRefBase.DelegateWrapper.Create(elementSelector),
                comparer);
        }

        /// <summary>
        /// Groups the elements of an async-enumerable sequence by invoking key and element selector functions on each source element and awaiting the results.
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
        /// <returns>An async-enumerable sequence of groups, each of which corresponds to a unique key value, containing all elements that share that same key value.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="elementSelector"/> is null.</exception>
        public static AsyncEnumerable<Grouping<TKey, TElement>> GroupBy<TSource, TCaptureKey, TKey, TElement>(
            this AsyncEnumerable<TSource> source,
            TCaptureKey keyCaptureValue,
            Func<TCaptureKey, TSource, Promise<TKey>> keySelector,
            Func<TSource, Promise<TElement>> elementSelector,
            IEqualityComparer<TKey> comparer)
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(elementSelector, nameof(elementSelector), 1);

            return Internal.Lookup<TKey, TElement>.GroupByAwait(source.GetAsyncEnumerator(),
                Internal.PromiseRefBase.DelegateWrapper.Create(keyCaptureValue, keySelector),
                Internal.PromiseRefBase.DelegateWrapper.Create(elementSelector),
                comparer);
        }

        /// <summary>
        /// Groups the elements of an async-enumerable sequence by invoking key and element selector functions on each source element and awaiting the results.
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
        /// <returns>An async-enumerable sequence of groups, each of which corresponds to a unique key value, containing all elements that share that same key value.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="elementSelector"/> is null.</exception>
        public static AsyncEnumerable<Grouping<TKey, TElement>> GroupBy<TSource, TKey, TCaptureElement, TElement>(
            this AsyncEnumerable<TSource> source,
            Func<TSource, Promise<TKey>> keySelector,
            TCaptureElement elementCaptureValue,
            Func<TCaptureElement, TSource, Promise<TElement>> elementSelector,
            IEqualityComparer<TKey> comparer)
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(elementSelector, nameof(elementSelector), 1);

            return Internal.Lookup<TKey, TElement>.GroupByAwait(source.GetAsyncEnumerator(),
                Internal.PromiseRefBase.DelegateWrapper.Create(keySelector),
                Internal.PromiseRefBase.DelegateWrapper.Create(elementCaptureValue, elementSelector),
                comparer);
        }

        /// <summary>
        /// Groups the elements of an async-enumerable sequence by invoking key and element selector functions on each source element and awaiting the results.
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
        /// <returns>An async-enumerable sequence of groups, each of which corresponds to a unique key value, containing all elements that share that same key value.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="elementSelector"/> is null.</exception>
        public static AsyncEnumerable<Grouping<TKey, TElement>> GroupBy<TSource, TCaptureKey, TKey, TCaptureElement, TElement>(
            this AsyncEnumerable<TSource> source,
            TCaptureKey keyCaptureValue,
            Func<TCaptureKey, TSource, Promise<TKey>> keySelector,
            TCaptureElement elementCaptureValue,
            Func<TCaptureElement, TSource, Promise<TElement>> elementSelector,
            IEqualityComparer<TKey> comparer)
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(elementSelector, nameof(elementSelector), 1);

            return Internal.Lookup<TKey, TElement>.GroupByAwait(source.GetAsyncEnumerator(),
                Internal.PromiseRefBase.DelegateWrapper.Create(keyCaptureValue, keySelector),
                Internal.PromiseRefBase.DelegateWrapper.Create(elementCaptureValue, elementSelector),
                comparer);
        }
        #endregion AsyncEnumerable

        #region ConfiguredAsyncEnumerable
        /// <summary>
        /// Groups the elements of a configured async-enumerable sequence according to a specified key selector function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the lookup key computed for each element in the source sequence.</typeparam>
        /// <param name="configuredSource">A configured async-enumerable sequence to create a lookup for.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <returns>An async-enumerable sequence of groups, each of which corresponds to a unique key value, containing all elements that share that same key value.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        public static AsyncEnumerable<Grouping<TKey, TSource>> GroupBy<TSource, TKey>(
            this ConfiguredAsyncEnumerable<TSource> configuredSource,
            Func<TSource, TKey> keySelector)
            => GroupBy(configuredSource, keySelector, comparer: null);

        /// <summary>
        /// Groups the elements of a configured async-enumerable sequence according to a specified key selector function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to the key selector.</typeparam>
        /// <typeparam name="TKey">The type of the lookup key computed for each element in the source sequence.</typeparam>
        /// <param name="configuredSource">A configured async-enumerable sequence to create a lookup for.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <returns>An async-enumerable sequence of groups, each of which corresponds to a unique key value, containing all elements that share that same key value.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        public static AsyncEnumerable<Grouping<TKey, TSource>> GroupBy<TSource, TCaptureKey, TKey>(
            this ConfiguredAsyncEnumerable<TSource> configuredSource,
            TCaptureKey keyCaptureValue,
            Func<TCaptureKey, TSource, TKey> keySelector)
            => GroupBy(configuredSource, keyCaptureValue, keySelector, comparer: null);

        /// <summary>
        /// Groups the elements of a configured async-enumerable sequence according to a specified key selector function, and a comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the lookup key computed for each element in the source sequence.</typeparam>
        /// <param name="configuredSource">A configured async-enumerable sequence to create a lookup for.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="comparer">An equality comparer to compare keys. If null, the default equality comparer will be used.</param>
        /// <returns>An async-enumerable sequence of groups, each of which corresponds to a unique key value, containing all elements that share that same key value.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        public static AsyncEnumerable<Grouping<TKey, TSource>> GroupBy<TSource, TKey>(
            this ConfiguredAsyncEnumerable<TSource> configuredSource,
            Func<TSource, TKey> keySelector,
            IEqualityComparer<TKey> comparer)
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);

            return Internal.Lookup<TKey, TSource>.GroupBy(configuredSource.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(keySelector), comparer);
        }

        /// <summary>
        /// Groups the elements of a configured async-enumerable sequence according to a specified key selector function, and a comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to the key selector.</typeparam>
        /// <typeparam name="TKey">The type of the lookup key computed for each element in the source sequence.</typeparam>
        /// <param name="configuredSource">A configured async-enumerable sequence to create a lookup for.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="comparer">An equality comparer to compare keys. If null, the default equality comparer will be used.</param>
        /// <returns>An async-enumerable sequence of groups, each of which corresponds to a unique key value, containing all elements that share that same key value.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        public static AsyncEnumerable<Grouping<TKey, TSource>> GroupBy<TSource, TCaptureKey, TKey>(
            this ConfiguredAsyncEnumerable<TSource> configuredSource,
            TCaptureKey keyCaptureValue,
            Func<TCaptureKey, TSource, TKey> keySelector,
            IEqualityComparer<TKey> comparer)
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);

            return Internal.Lookup<TKey, TSource>.GroupBy(configuredSource.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(keyCaptureValue, keySelector), comparer);
        }

        /// <summary>
        /// Groups the elements of a configured async-enumerable sequence by invoking a key-selector function on each element and awaiting the result.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the lookup key computed for each element in the source sequence.</typeparam>
        /// <param name="configuredSource">A configured async-enumerable sequence to create a lookup for.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from each element.</param>
        /// <returns>An async-enumerable sequence of groups, each of which corresponds to a unique key value, containing all elements that share that same key value.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        public static AsyncEnumerable<Grouping<TKey, TSource>> GroupBy<TSource, TKey>(
            this ConfiguredAsyncEnumerable<TSource> configuredSource,
            Func<TSource, Promise<TKey>> keySelector)
            => GroupBy<TSource, TKey>(configuredSource, keySelector, comparer: null);

        /// <summary>
        /// Groups the elements of a configured async-enumerable sequence by invoking a key-selector function on each element and awaiting the result.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to the key selector.</typeparam>
        /// <typeparam name="TKey">The type of the lookup key computed for each element in the source sequence.</typeparam>
        /// <param name="configuredSource">A configured async-enumerable sequence to create a lookup for.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from each element.</param>
        /// <returns>An async-enumerable sequence of groups, each of which corresponds to a unique key value, containing all elements that share that same key value.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        public static AsyncEnumerable<Grouping<TKey, TSource>> GroupBy<TSource, TCaptureKey, TKey>(
            this ConfiguredAsyncEnumerable<TSource> configuredSource,
            TCaptureKey keyCaptureValue,
            Func<TCaptureKey, TSource, Promise<TKey>> keySelector)
            => GroupBy<TSource, TCaptureKey, TKey>(configuredSource, keyCaptureValue, keySelector, comparer: null);

        /// <summary>
        /// Groups the elements of a configured async-enumerable sequence by invoking a key-selector function on each element and awaiting the result.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the lookup key computed for each element in the source sequence.</typeparam>
        /// <param name="configuredSource">A configured async-enumerable sequence to create a lookup for.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from each element.</param>
        /// <param name="comparer">An equality comparer to compare keys. If null, the default equality comparer will be used.</param>
        /// <returns>An async-enumerable sequence of groups, each of which corresponds to a unique key value, containing all elements that share that same key value.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        public static AsyncEnumerable<Grouping<TKey, TSource>> GroupBy<TSource, TKey>(
            this ConfiguredAsyncEnumerable<TSource> configuredSource,
            Func<TSource, Promise<TKey>> keySelector,
            IEqualityComparer<TKey> comparer)
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);

            return Internal.Lookup<TKey, TSource>.GroupByAwait(configuredSource.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(keySelector), comparer);
        }

        /// <summary>
        /// Groups the elements of a configured async-enumerable sequence by invoking a key-selector function on each element and awaiting the result.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to the key selector.</typeparam>
        /// <typeparam name="TKey">The type of the lookup key computed for each element in the source sequence.</typeparam>
        /// <param name="configuredSource">A configured async-enumerable sequence to create a lookup for.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from each element.</param>
        /// <param name="comparer">An equality comparer to compare keys. If null, the default equality comparer will be used.</param>
        /// <returns>An async-enumerable sequence of groups, each of which corresponds to a unique key value, containing all elements that share that same key value.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
        public static AsyncEnumerable<Grouping<TKey, TSource>> GroupBy<TSource, TCaptureKey, TKey>(
            this ConfiguredAsyncEnumerable<TSource> configuredSource,
            TCaptureKey keyCaptureValue,
            Func<TCaptureKey, TSource, Promise<TKey>> keySelector,
            IEqualityComparer<TKey> comparer)
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);

            return Internal.Lookup<TKey, TSource>.GroupByAwait(configuredSource.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(keyCaptureValue, keySelector), comparer);
        }

        /// <summary>
        /// Groups the elements of a configured async-enumerable sequence according to a specified key selector function, and an element selector function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the lookup key computed for each element in the source sequence.</typeparam>
        /// <typeparam name="TElement">The type of the lookup value computed for each element in the source sequence.</typeparam>
        /// <param name="configuredSource">A configured async-enumerable sequence to create a lookup for.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="elementSelector">A transform function to produce a result element value from each element.</param>
        /// <returns>An async-enumerable sequence of groups, each of which corresponds to a unique key value, containing all elements that share that same key value.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="elementSelector"/> is null.</exception>
        public static AsyncEnumerable<Grouping<TKey, TElement>> GroupBy<TSource, TKey, TElement>(
            this ConfiguredAsyncEnumerable<TSource> configuredSource,
            Func<TSource, TKey> keySelector,
            Func<TSource, TElement> elementSelector)
            => GroupBy(configuredSource, keySelector, elementSelector, comparer: null);

        /// <summary>
        /// Groups the elements of a configured async-enumerable sequence according to a specified key selector function, and an element selector function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to the key selector.</typeparam>
        /// <typeparam name="TKey">The type of the lookup key computed for each element in the source sequence.</typeparam>
        /// <typeparam name="TElement">The type of the lookup value computed for each element in the source sequence.</typeparam>
        /// <param name="configuredSource">A configured async-enumerable sequence to create a lookup for.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="elementSelector">A transform function to produce a result element value from each element.</param>
        /// <returns>An async-enumerable sequence of groups, each of which corresponds to a unique key value, containing all elements that share that same key value.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="elementSelector"/> is null.</exception>
        public static AsyncEnumerable<Grouping<TKey, TElement>> GroupBy<TSource, TCaptureKey, TKey, TElement>(
            this ConfiguredAsyncEnumerable<TSource> configuredSource,
            TCaptureKey keyCaptureValue,
            Func<TCaptureKey, TSource, TKey> keySelector,
            Func<TSource, TElement> elementSelector)
            => GroupBy(configuredSource, keyCaptureValue, keySelector, elementSelector, comparer: null);

        /// <summary>
        /// Groups the elements of a configured async-enumerable sequence according to a specified key selector function, and an element selector function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the lookup key computed for each element in the source sequence.</typeparam>
        /// <typeparam name="TCaptureElement">The type of the captured value that will be passed to the element selector.</typeparam>
        /// <typeparam name="TElement">The type of the lookup value computed for each element in the source sequence.</typeparam>
        /// <param name="configuredSource">A configured async-enumerable sequence to create a lookup for.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="elementCaptureValue">The extra value that will be passed to <paramref name="elementSelector"/>.</param>
        /// <param name="elementSelector">A transform function to produce a result element value from each element.</param>
        /// <returns>An async-enumerable sequence of groups, each of which corresponds to a unique key value, containing all elements that share that same key value.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="elementSelector"/> is null.</exception>
        public static AsyncEnumerable<Grouping<TKey, TElement>> GroupBy<TSource, TKey, TCaptureElement, TElement>(
            this ConfiguredAsyncEnumerable<TSource> configuredSource,
            Func<TSource, TKey> keySelector,
            TCaptureElement elementCaptureValue,
            Func<TCaptureElement, TSource, TElement> elementSelector)
            => GroupBy(configuredSource, keySelector, elementCaptureValue, elementSelector, comparer: null);

        /// <summary>
        /// Groups the elements of a configured async-enumerable sequence according to a specified key selector function, and an element selector function.
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
        /// <returns>An async-enumerable sequence of groups, each of which corresponds to a unique key value, containing all elements that share that same key value.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="elementSelector"/> is null.</exception>
        public static AsyncEnumerable<Grouping<TKey, TElement>> GroupBy<TSource, TCaptureKey, TKey, TCaptureElement, TElement>(
            this ConfiguredAsyncEnumerable<TSource> configuredSource,
            TCaptureKey keyCaptureValue,
            Func<TCaptureKey, TSource, TKey> keySelector,
            TCaptureElement elementCaptureValue,
            Func<TCaptureElement, TSource, TElement> elementSelector)
            => GroupBy(configuredSource, keyCaptureValue, keySelector, elementCaptureValue, elementSelector, comparer: null);

        /// <summary>
        /// Groups the elements of a configured async-enumerable sequence according to a specified key selector function, a comparer, and an element selector function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the lookup key computed for each element in the source sequence.</typeparam>
        /// <typeparam name="TElement">The type of the lookup value computed for each element in the source sequence.</typeparam>
        /// <param name="configuredSource">A configured async-enumerable sequence to create a lookup for.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="elementSelector">A transform function to produce a result element value from each element.</param>
        /// <param name="comparer">An equality comparer to compare keys. If null, the default equality comparer will be used.</param>
        /// <returns>An async-enumerable sequence of groups, each of which corresponds to a unique key value, containing all elements that share that same key value.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="elementSelector"/> is null.</exception>
        public static AsyncEnumerable<Grouping<TKey, TElement>> GroupBy<TSource, TKey, TElement>(
            this ConfiguredAsyncEnumerable<TSource> configuredSource,
            Func<TSource, TKey> keySelector,
            Func<TSource, TElement> elementSelector,
            IEqualityComparer<TKey> comparer)
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(elementSelector, nameof(elementSelector), 1);

            return Internal.Lookup<TKey, TElement>.GroupBy(configuredSource.GetAsyncEnumerator(),
                Internal.PromiseRefBase.DelegateWrapper.Create(keySelector),
                Internal.PromiseRefBase.DelegateWrapper.Create(elementSelector),
                comparer);
        }

        /// <summary>
        /// Groups the elements of a configured async-enumerable sequence according to a specified key selector function, a comparer, and an element selector function.
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
        /// <returns>An async-enumerable sequence of groups, each of which corresponds to a unique key value, containing all elements that share that same key value.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="elementSelector"/> is null.</exception>
        public static AsyncEnumerable<Grouping<TKey, TElement>> GroupBy<TSource, TCaptureKey, TKey, TElement>(
            this ConfiguredAsyncEnumerable<TSource> configuredSource,
            TCaptureKey keyCaptureValue,
            Func<TCaptureKey, TSource, TKey> keySelector,
            Func<TSource, TElement> elementSelector,
            IEqualityComparer<TKey> comparer)
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(elementSelector, nameof(elementSelector), 1);

            return Internal.Lookup<TKey, TElement>.GroupBy(configuredSource.GetAsyncEnumerator(),
                Internal.PromiseRefBase.DelegateWrapper.Create(keyCaptureValue, keySelector),
                Internal.PromiseRefBase.DelegateWrapper.Create(elementSelector),
                comparer);
        }

        /// <summary>
        /// Groups the elements of a configured async-enumerable sequence according to a specified key selector function, a comparer, and an element selector function.
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
        /// <returns>An async-enumerable sequence of groups, each of which corresponds to a unique key value, containing all elements that share that same key value.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="elementSelector"/> is null.</exception>
        public static AsyncEnumerable<Grouping<TKey, TElement>> GroupBy<TSource, TKey, TCaptureElement, TElement>(
            this ConfiguredAsyncEnumerable<TSource> configuredSource,
            Func<TSource, TKey> keySelector,
            TCaptureElement elementCaptureValue,
            Func<TCaptureElement, TSource, TElement> elementSelector,
            IEqualityComparer<TKey> comparer)
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(elementSelector, nameof(elementSelector), 1);

            return Internal.Lookup<TKey, TElement>.GroupBy(configuredSource.GetAsyncEnumerator(),
                Internal.PromiseRefBase.DelegateWrapper.Create(keySelector),
                Internal.PromiseRefBase.DelegateWrapper.Create(elementCaptureValue, elementSelector),
                comparer);
        }

        /// <summary>
        /// Groups the elements of a configured async-enumerable sequence according to a specified key selector function, a comparer, and an element selector function.
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
        /// <returns>An async-enumerable sequence of groups, each of which corresponds to a unique key value, containing all elements that share that same key value.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="elementSelector"/> is null.</exception>
        public static AsyncEnumerable<Grouping<TKey, TElement>> GroupBy<TSource, TCaptureKey, TKey, TCaptureElement, TElement>(
            this ConfiguredAsyncEnumerable<TSource> configuredSource,
            TCaptureKey keyCaptureValue,
            Func<TCaptureKey, TSource, TKey> keySelector,
            TCaptureElement elementCaptureValue,
            Func<TCaptureElement, TSource, TElement> elementSelector,
            IEqualityComparer<TKey> comparer)
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(elementSelector, nameof(elementSelector), 1);

            return Internal.Lookup<TKey, TElement>.GroupBy(configuredSource.GetAsyncEnumerator(),
                Internal.PromiseRefBase.DelegateWrapper.Create(keyCaptureValue, keySelector),
                Internal.PromiseRefBase.DelegateWrapper.Create(elementCaptureValue, elementSelector),
                comparer);
        }

        /// <summary>
        /// Groups the elements of a configured async-enumerable sequence by invoking key and element selector functions on each source element and awaiting the results.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the lookup key computed for each element in the source sequence.</typeparam>
        /// <typeparam name="TElement">The type of the lookup value computed for each element in the source sequence.</typeparam>
        /// <param name="configuredSource">A configured async-enumerable sequence to create a lookup for.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from each element.</param>
        /// <param name="elementSelector">An asynchronous transform function to produce a result element value from each element.</param>
        /// <returns>An async-enumerable sequence of groups, each of which corresponds to a unique key value, containing all elements that share that same key value.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="elementSelector"/> is null.</exception>
        public static AsyncEnumerable<Grouping<TKey, TElement>> GroupBy<TSource, TKey, TElement>(
            this ConfiguredAsyncEnumerable<TSource> configuredSource,
            Func<TSource, Promise<TKey>> keySelector,
            Func<TSource, Promise<TElement>> elementSelector)
            => GroupBy<TSource, TKey, TElement>(configuredSource, keySelector, elementSelector, comparer: null);

        /// <summary>
        /// Groups the elements of a configured async-enumerable sequence by invoking key and element selector functions on each source element and awaiting the results.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TCaptureKey">The type of the captured value that will be passed to the key selector.</typeparam>
        /// <typeparam name="TKey">The type of the lookup key computed for each element in the source sequence.</typeparam>
        /// <typeparam name="TElement">The type of the lookup value computed for each element in the source sequence.</typeparam>
        /// <param name="configuredSource">A configured async-enumerable sequence to create a lookup for.</param>
        /// <param name="keyCaptureValue">The extra value that will be passed to <paramref name="keySelector"/>.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from each element.</param>
        /// <param name="elementSelector">An asynchronous transform function to produce a result element value from each element.</param>
        /// <returns>An async-enumerable sequence of groups, each of which corresponds to a unique key value, containing all elements that share that same key value.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="elementSelector"/> is null.</exception>
        public static AsyncEnumerable<Grouping<TKey, TElement>> GroupBy<TSource, TCaptureKey, TKey, TElement>(
            this ConfiguredAsyncEnumerable<TSource> configuredSource,
            TCaptureKey keyCaptureValue,
            Func<TCaptureKey, TSource, Promise<TKey>> keySelector,
            Func<TSource, Promise<TElement>> elementSelector)
            => GroupBy<TSource, TCaptureKey, TKey, TElement>(configuredSource, keyCaptureValue, keySelector, elementSelector, comparer: null);

        /// <summary>
        /// Groups the elements of a configured async-enumerable sequence by invoking key and element selector functions on each source element and awaiting the results.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the lookup key computed for each element in the source sequence.</typeparam>
        /// <typeparam name="TCaptureElement">The type of the captured value that will be passed to the element selector.</typeparam>
        /// <typeparam name="TElement">The type of the lookup value computed for each element in the source sequence.</typeparam>
        /// <param name="configuredSource">A configured async-enumerable sequence to create a lookup for.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from each element.</param>
        /// <param name="elementCaptureValue">The extra value that will be passed to <paramref name="elementSelector"/>.</param>
        /// <param name="elementSelector">An asynchronous transform function to produce a result element value from each element.</param>
        /// <returns>An async-enumerable sequence of groups, each of which corresponds to a unique key value, containing all elements that share that same key value.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="elementSelector"/> is null.</exception>
        public static AsyncEnumerable<Grouping<TKey, TElement>> GroupBy<TSource, TKey, TCaptureElement, TElement>(
            this ConfiguredAsyncEnumerable<TSource> configuredSource,
            Func<TSource, Promise<TKey>> keySelector,
            TCaptureElement elementCaptureValue,
            Func<TCaptureElement, TSource, Promise<TElement>> elementSelector)
            => GroupBy<TSource, TKey, TCaptureElement, TElement>(configuredSource, keySelector, elementCaptureValue, elementSelector, comparer: null);

        /// <summary>
        /// Groups the elements of a configured async-enumerable sequence by invoking key and element selector functions on each source element and awaiting the results.
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
        /// <returns>An async-enumerable sequence of groups, each of which corresponds to a unique key value, containing all elements that share that same key value.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="elementSelector"/> is null.</exception>
        public static AsyncEnumerable<Grouping<TKey, TElement>> GroupBy<TSource, TCaptureKey, TKey, TCaptureElement, TElement>(
            this ConfiguredAsyncEnumerable<TSource> configuredSource,
            TCaptureKey keyCaptureValue,
            Func<TCaptureKey, TSource, Promise<TKey>> keySelector,
            TCaptureElement elementCaptureValue,
            Func<TCaptureElement, TSource, Promise<TElement>> elementSelector)
            => GroupBy<TSource, TCaptureKey, TKey, TCaptureElement, TElement>(configuredSource, keyCaptureValue, keySelector, elementCaptureValue, elementSelector, comparer: null);

        /// <summary>
        /// Groups the elements of a configured async-enumerable sequence by invoking key and element selector functions on each source element and awaiting the results.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the lookup key computed for each element in the source sequence.</typeparam>
        /// <typeparam name="TElement">The type of the lookup value computed for each element in the source sequence.</typeparam>
        /// <param name="configuredSource">A configured async-enumerable sequence to create a lookup for.</param>
        /// <param name="keySelector">An asynchronous function to extract a key from each element.</param>
        /// <param name="elementSelector">An asynchronous transform function to produce a result element value from each source element.</param>
        /// <param name="comparer">An equality comparer to compare keys. If null, the default equality comparer will be used.</param>
        /// <returns>An async-enumerable sequence of groups, each of which corresponds to a unique key value, containing all elements that share that same key value.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="elementSelector"/> is null.</exception>
        public static AsyncEnumerable<Grouping<TKey, TElement>> GroupBy<TSource, TKey, TElement>(
            this ConfiguredAsyncEnumerable<TSource> configuredSource,
            Func<TSource, Promise<TKey>> keySelector,
            Func<TSource, Promise<TElement>> elementSelector,
            IEqualityComparer<TKey> comparer)
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(elementSelector, nameof(elementSelector), 1);

            return Internal.Lookup<TKey, TElement>.GroupByAwait(configuredSource.GetAsyncEnumerator(),
                Internal.PromiseRefBase.DelegateWrapper.Create(keySelector),
                Internal.PromiseRefBase.DelegateWrapper.Create(elementSelector),
                comparer);
        }

        /// <summary>
        /// Groups the elements of a configured async-enumerable sequence by invoking key and element selector functions on each source element and awaiting the results.
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
        /// <returns>An async-enumerable sequence of groups, each of which corresponds to a unique key value, containing all elements that share that same key value.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="elementSelector"/> is null.</exception>
        public static AsyncEnumerable<Grouping<TKey, TElement>> GroupBy<TSource, TCaptureKey, TKey, TElement>(
            this ConfiguredAsyncEnumerable<TSource> configuredSource,
            TCaptureKey keyCaptureValue,
            Func<TCaptureKey, TSource, Promise<TKey>> keySelector,
            Func<TSource, Promise<TElement>> elementSelector,
            IEqualityComparer<TKey> comparer)
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(elementSelector, nameof(elementSelector), 1);

            return Internal.Lookup<TKey, TElement>.GroupByAwait(configuredSource.GetAsyncEnumerator(),
                Internal.PromiseRefBase.DelegateWrapper.Create(keyCaptureValue, keySelector),
                Internal.PromiseRefBase.DelegateWrapper.Create(elementSelector),
                comparer);
        }

        /// <summary>
        /// Groups the elements of a configured async-enumerable sequence by invoking key and element selector functions on each source element and awaiting the results.
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
        /// <returns>An async-enumerable sequence of groups, each of which corresponds to a unique key value, containing all elements that share that same key value.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="elementSelector"/> is null.</exception>
        public static AsyncEnumerable<Grouping<TKey, TElement>> GroupBy<TSource, TKey, TCaptureElement, TElement>(
            this ConfiguredAsyncEnumerable<TSource> configuredSource,
            Func<TSource, Promise<TKey>> keySelector,
            TCaptureElement elementCaptureValue,
            Func<TCaptureElement, TSource, Promise<TElement>> elementSelector,
            IEqualityComparer<TKey> comparer)
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(elementSelector, nameof(elementSelector), 1);

            return Internal.Lookup<TKey, TElement>.GroupByAwait(configuredSource.GetAsyncEnumerator(),
                Internal.PromiseRefBase.DelegateWrapper.Create(keySelector),
                Internal.PromiseRefBase.DelegateWrapper.Create(elementCaptureValue, elementSelector),
                comparer);
        }

        /// <summary>
        /// Groups the elements of a configured async-enumerable sequence by invoking key and element selector functions on each source element and awaiting the results.
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
        /// <returns>An async-enumerable sequence of groups, each of which corresponds to a unique key value, containing all elements that share that same key value.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or <paramref name="elementSelector"/> is null.</exception>
        public static AsyncEnumerable<Grouping<TKey, TElement>> GroupBy<TSource, TCaptureKey, TKey, TCaptureElement, TElement>(
            this ConfiguredAsyncEnumerable<TSource> configuredSource,
            TCaptureKey keyCaptureValue,
            Func<TCaptureKey, TSource, Promise<TKey>> keySelector,
            TCaptureElement elementCaptureValue,
            Func<TCaptureElement, TSource, Promise<TElement>> elementSelector,
            IEqualityComparer<TKey> comparer)
        {
            ValidateArgument(keySelector, nameof(keySelector), 1);
            ValidateArgument(elementSelector, nameof(elementSelector), 1);

            return Internal.Lookup<TKey, TElement>.GroupByAwait(configuredSource.GetAsyncEnumerator(),
                Internal.PromiseRefBase.DelegateWrapper.Create(keyCaptureValue, keySelector),
                Internal.PromiseRefBase.DelegateWrapper.Create(elementCaptureValue, elementSelector),
                comparer);
        }
        #endregion ConfiguredAsyncEnumerable
    }
#endif // CSHARP_7_3_OR_NEWER
    }