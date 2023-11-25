using System.Collections.Generic;

namespace Proto.Promises.Linq
{
#if CSHARP_7_3_OR_NEWER
    partial class AsyncEnumerable
    {
        /// <summary>
        /// Merges elements from all of the specified async-enumerable sequences into a single async-enumerable sequence.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the source sequence.</typeparam>
        /// <param name="sources">Async-enumerable sequences.</param>
        /// <returns>The async-enumerable sequence that merges the elements of the async-enumerable sequences.</returns>
        public static AsyncEnumerable<T> Merge<T>(this AsyncEnumerable<AsyncEnumerable<T>> sources)
        {
            var enumerable = Internal.AsyncEnumerableMergerAsync<T>.GetOrCreate(sources.GetAsyncEnumerator());
            return new AsyncEnumerable<T>(enumerable);
        }

        /// <summary>
        /// Merges elements from all of the specified async-enumerable sequences into a single async-enumerable sequence.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the source sequence.</typeparam>
        /// <param name="sources">Async-enumerable sequences.</param>
        /// <returns>The async-enumerable sequence that merges the elements of the async-enumerable sequences.</returns>
        public static AsyncEnumerable<T> Merge<T>(params AsyncEnumerable<T>[] sources)
            => Merge<T, Internal.ArrayEnumerator<AsyncEnumerable<T>>>(sources.GetGenericEnumerator());

        /// <summary>
        /// Merges elements from all of the specified async-enumerable sequences into a single async-enumerable sequence.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the source sequence.</typeparam>
        /// <param name="sources">Async-enumerable sequences.</param>
        /// <returns>The async-enumerable sequence that merges the elements of the async-enumerable sequences.</returns>
        public static AsyncEnumerable<T> Merge<T>(this IEnumerable<AsyncEnumerable<T>> sources)
            => Merge<T, IEnumerator<AsyncEnumerable<T>>>(sources.GetEnumerator());

        /// <summary>
        /// Merges elements from all of the specified async-enumerable sequences into a single async-enumerable sequence.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TEnumerator">The type of the enumerator containing the source sequences.</typeparam>
        /// <param name="sources">Async-enumerable sequences.</param>
        /// <returns>The async-enumerable sequence that merges the elements of the async-enumerable sequences.</returns>
        public static AsyncEnumerable<T> Merge<T, TEnumerator>(this TEnumerator sources)
            where TEnumerator : IEnumerator<AsyncEnumerable<T>>
        {
            ValidateArgument(sources, nameof(sources), 1);

            var enumerable = Internal.AsyncEnumerableMergerSync<T, TEnumerator>.GetOrCreate(sources);
            return new AsyncEnumerable<T>(enumerable);
        }

        /// <summary>
        /// Merges elements from all of the specified async-enumerable sequences into a single async-enumerable sequence.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the source sequence.</typeparam>
        /// <param name="source1">The first async-enumerable sequence.</param>
        /// <param name="source2">The second async-enumerable sequence.</param>
        /// <returns>The async-enumerable sequence that merges the elements of the async-enumerable sequences.</returns>
        public static AsyncEnumerable<T> Merge<T>(AsyncEnumerable<T> source1, AsyncEnumerable<T> source2)
            => Merge<T, Internal.Enumerator<AsyncEnumerable<T>, Internal.TwoItems<AsyncEnumerable<T>>>>(Internal.GetEnumerator(source1, source2));

        /// <summary>
        /// Merges elements from all of the specified async-enumerable sequences into a single async-enumerable sequence.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the source sequence.</typeparam>
        /// <param name="source1">The first async-enumerable sequence.</param>
        /// <param name="source2">The second async-enumerable sequence.</param>
        /// <param name="source3">The third async-enumerable sequence.</param>
        /// <returns>The async-enumerable sequence that merges the elements of the async-enumerable sequences.</returns>
        public static AsyncEnumerable<T> Merge<T>(AsyncEnumerable<T> source1, AsyncEnumerable<T> source2, AsyncEnumerable<T> source3)
            => Merge<T, Internal.Enumerator<AsyncEnumerable<T>, Internal.ThreeItems<AsyncEnumerable<T>>>>(Internal.GetEnumerator(source1, source2, source3));

        /// <summary>
        /// Merges elements from all of the specified async-enumerable sequences into a single async-enumerable sequence.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the source sequence.</typeparam>
        /// <param name="source1">The first async-enumerable sequence.</param>
        /// <param name="source2">The second async-enumerable sequence.</param>
        /// <param name="source3">The third async-enumerable sequence.</param>
        /// <param name="source4">The fourth async-enumerable sequence.</param>
        /// <returns>The async-enumerable sequence that merges the elements of the async-enumerable sequences.</returns>
        public static AsyncEnumerable<T> Merge<T>(AsyncEnumerable<T> source1, AsyncEnumerable<T> source2, AsyncEnumerable<T> source3, AsyncEnumerable<T> source4)
            => Merge<T, Internal.Enumerator<AsyncEnumerable<T>, Internal.FourItems<AsyncEnumerable<T>>>>(Internal.GetEnumerator(source1, source2, source3, source4));
    }

#if UNITY_2021_2_OR_NEWER || NETSTANDARD2_1_OR_GREATER || NETCOREAPP
    // Old IL2CPP runtime immediately crashes if these methods exist, even if they are not used. So we only include them in newer build targets that old Unity versions cannot consume.
    // See https://github.com/timcassell/ProtoPromise/pull/106 for details.

    partial struct AsyncEnumerable<T>
    {
        /// <summary>
        /// Merges elements from all of the specified async-enumerable sequences into a single async-enumerable sequence.
        /// </summary>
        /// <param name="sources">Async-enumerable sequences.</param>
        /// <returns>The async-enumerable sequence that merges the elements of the async-enumerable sequences.</returns>
        public static AsyncEnumerable<T> Merge(AsyncEnumerable<AsyncEnumerable<T>> sources)
            => AsyncEnumerable.Merge(sources);

        /// <summary>
        /// Merges elements from all of the specified async-enumerable sequences into a single async-enumerable sequence.
        /// </summary>
        /// <param name="sources">Async-enumerable sequences.</param>
        /// <returns>The async-enumerable sequence that merges the elements of the async-enumerable sequences.</returns>
        public static AsyncEnumerable<T> Merge(params AsyncEnumerable<T>[] sources)
            => AsyncEnumerable.Merge(sources);

        /// <summary>
        /// Merges elements from all of the specified async-enumerable sequences into a single async-enumerable sequence.
        /// </summary>
        /// <param name="sources">Async-enumerable sequences.</param>
        /// <returns>The async-enumerable sequence that merges the elements of the async-enumerable sequences.</returns>
        public static AsyncEnumerable<T> Merge(IEnumerable<AsyncEnumerable<T>> sources)
            => AsyncEnumerable.Merge(sources);

        /// <summary>
        /// Merges elements from all of the specified async-enumerable sequences into a single async-enumerable sequence.
        /// </summary>
        /// <typeparam name="TEnumerator">The type of the enumerator containing the source sequences.</typeparam>
        /// <param name="sources">Async-enumerable sequences.</param>
        /// <returns>The async-enumerable sequence that merges the elements of the async-enumerable sequences.</returns>
        public static AsyncEnumerable<T> Merge<TEnumerator>(TEnumerator sources)
            where TEnumerator : IEnumerator<AsyncEnumerable<T>>
            => AsyncEnumerable.Merge<T, TEnumerator>(sources);

        /// <summary>
        /// Merges elements from all of the specified async-enumerable sequences into a single async-enumerable sequence.
        /// </summary>
        /// <param name="source1">The first async-enumerable sequence.</param>
        /// <param name="source2">The second async-enumerable sequence.</param>
        /// <returns>The async-enumerable sequence that merges the elements of the async-enumerable sequences.</returns>
        public static AsyncEnumerable<T> Merge(AsyncEnumerable<T> source1, AsyncEnumerable<T> source2)
            => AsyncEnumerable.Merge(source1, source2);

        /// <summary>
        /// Merges elements from all of the specified async-enumerable sequences into a single async-enumerable sequence.
        /// </summary>
        /// <param name="source1">The first async-enumerable sequence.</param>
        /// <param name="source2">The second async-enumerable sequence.</param>
        /// <param name="source3">The third async-enumerable sequence.</param>
        /// <returns>The async-enumerable sequence that merges the elements of the async-enumerable sequences.</returns>
        public static AsyncEnumerable<T> Merge(AsyncEnumerable<T> source1, AsyncEnumerable<T> source2, AsyncEnumerable<T> source3)
            => AsyncEnumerable.Merge(source1, source2, source3);

        /// <summary>
        /// Merges elements from all of the specified async-enumerable sequences into a single async-enumerable sequence.
        /// </summary>
        /// <param name="source1">The first async-enumerable sequence.</param>
        /// <param name="source2">The second async-enumerable sequence.</param>
        /// <param name="source3">The third async-enumerable sequence.</param>
        /// <param name="source4">The fourth async-enumerable sequence.</param>
        /// <returns>The async-enumerable sequence that merges the elements of the async-enumerable sequences.</returns>
        public static AsyncEnumerable<T> Merge(AsyncEnumerable<T> source1, AsyncEnumerable<T> source2, AsyncEnumerable<T> source3, AsyncEnumerable<T> source4)
            => AsyncEnumerable.Merge(source1, source2, source3, source4);
    }
#endif // UNITY_2021_2_OR_NEWER || NETSTANDARD2_1_OR_GREATER || NETCOREAPP
#endif // CSHARP_7_3_OR_NEWER
}