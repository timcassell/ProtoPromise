#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using Proto.Promises.Linq.Sources;

namespace Proto.Promises.Linq
{
    partial class AsyncEnumerable
    {
        /// <summary>
        /// Returns a specified number of contiguous elements from the start of an async-enumerable sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the <paramref name="source"/> sequence.</typeparam>
        /// <param name="source">The sequence to take elements from.</param>
        /// <param name="count">The number of elements to take from the start of the <paramref name="source"/> sequence.</param>
        /// <returns>An <see cref="AsyncEnumerable{T}"/> that contains the specified number of elements from the start of the <paramref name="source"/> sequence.</returns>
        public static AsyncEnumerable<TSource> Take<TSource>(this AsyncEnumerable<TSource> source, int count)
        {
            if (count <= 0)
            {
                // No elements will be yielded, so we can simply return an empty AsyncEnumerable.
                // But we have to dispose the source, so we need to do it with a special empty, instead of AsyncEnumerable<TSource>.Empty().
                return AsyncEnumerableSourceHelpers.EmptyWithDispose(source);
            }

            if (source._target is Internal.AsyncEnumerablePartition<TSource> partition)
            {
                return partition.Take(source._id, count);
            }

            var enumerable = Internal.AsyncEnumerablePartition<TSource>.GetOrCreate(source.GetAsyncEnumerator(), 0, count - 1);
            return new AsyncEnumerable<TSource>(enumerable);
        }

        /// <summary>
        /// Returns a specified number of contiguous elements from the end of an async-enumerable sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the <paramref name="source"/> sequence.</typeparam>
        /// <param name="source">The sequence to take elements from.</param>
        /// <param name="count">The number of elements to take from the end of the <paramref name="source"/> sequence.</param>
        /// <returns>An <see cref="AsyncEnumerable{T}"/> that contains the specified number of elements from the end of the <paramref name="source"/> sequence.</returns>
        public static AsyncEnumerable<TSource> TakeLast<TSource>(this AsyncEnumerable<TSource> source, int count)
        {
            if (count <= 0)
            {
                // No elements will be yielded, so we can simply return an empty AsyncEnumerable.
                // But we have to dispose the source, so we need to do it with a special empty, instead of AsyncEnumerable<TSource>.Empty().
                return AsyncEnumerableSourceHelpers.EmptyWithDispose(source);
            }

            if (source._target is Internal.AsyncEnumerablePartitionFromLast<TSource> partition)
            {
                return partition.TakeLast(source._id, count);
            }

            var enumerable = Internal.AsyncEnumerablePartitionFromLast<TSource>.GetOrCreate(source.GetAsyncEnumerator(), 0, count - 1);
            return new AsyncEnumerable<TSource>(enumerable);
        }

#if NETSTANDARD2_1_OR_GREATER || UNITY_2021_2_OR_NEWER || NETCOREAPP3_0_OR_GREATER // System.Range is available in netcorapp3.0 and netstandard2.1.
        /// <summary>
        /// Returns a specified range of contiguous elements from an async-enumerable sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the <paramref name="source"/> sequence.</typeparam>
        /// <param name="source">The sequence to take elements from.</param>
        /// <param name="range">The range of elements to return, which has start and end indexes either from the beginning or the end of the <paramref name="source"/> sequence.</param>
        /// <returns>An <see cref="AsyncEnumerable{T}"/> that contains the specified range of elements from the <paramref name="source"/> sequence.</returns>
        public static AsyncEnumerable<TSource> Take<TSource>(this AsyncEnumerable<TSource> source, System.Range range)
        {
            var start = range.Start;
            var end = range.End;
            bool isStartIndexFromEnd = start.IsFromEnd;
            bool isEndIndexFromEnd = end.IsFromEnd;
            int startIndex = start.Value;
            int endIndex = end.Value;

            if (isStartIndexFromEnd)
            {
                if (startIndex == 0 | (isEndIndexFromEnd & endIndex >= startIndex))
                {
                    return AsyncEnumerableSourceHelpers.EmptyWithDispose(source);
                }

                if (isEndIndexFromEnd)
                {
                    // Start and end from end.
                    return source.TakeLast(startIndex).SkipLast(endIndex);
                }

                // Start from end and end from start.
                return AsyncEnumerable<TSource>.Create(new Internal.TakeRangeIterator<TSource>(source.GetAsyncEnumerator(), startIndex, endIndex));
            }

            if (!isEndIndexFromEnd & startIndex >= endIndex)
            {
                return AsyncEnumerableSourceHelpers.EmptyWithDispose(source);
            }

            source = source.Skip(startIndex);
            return isEndIndexFromEnd
                // Start from start and end from end.
                ? source.SkipLast(endIndex)
                // Start and end from start.
                : source.Take(endIndex - startIndex);
        }
#endif // NETSTANDARD2_1_OR_GREATER || UNITY_2021_2_OR_NEWER || NETCOREAPP3_0_OR_GREATER
    }
}