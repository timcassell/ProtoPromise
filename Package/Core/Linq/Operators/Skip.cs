#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

namespace Proto.Promises.Linq
{
#if CSHARP_7_3_OR_NEWER
    partial class AsyncEnumerable
    {
        /// <summary>
        /// Bypasses a specified number of elements in an async-enumerable sequence and then returns the remaining elements.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the <paramref name="source"/> sequence.</typeparam>
        /// <param name="source">The sequence to take elements from.</param>
        /// <param name="count">The number of elements to skip before returning the remaining elements.</param>
        /// <returns>An <see cref="AsyncEnumerable{T}"/> that contains the elements that occur after the specified index in the <paramref name="source"/> sequence.</returns>
        public static AsyncEnumerable<TSource> Skip<TSource>(this AsyncEnumerable<TSource> source, int count)
        {
            if (count <= 0)
            {
                // No elements will be skipped, so we can simply return the source. But we need to invalidate it, so increment the id.
                var target = source._target;
                if (target == null)
                {
                    Internal.ThrowInvalidAsyncEnumerable(1);
                }
                return target.GetSelfWithIncrementedId(source._id);
            }

            if (source._target is Internal.AsyncEnumerablePartition<TSource> partition)
            {
                return partition.Skip(source._id, count);
            }

            var enumerable = Internal.AsyncEnumerablePartition<TSource>.GetOrCreate(source.GetAsyncEnumerator(), count, -1);
            return new AsyncEnumerable<TSource>(enumerable);
        }

        /// <summary>
        /// Bypasses a specified number of elements at the end of an async-enumerable sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the <paramref name="source"/> sequence.</typeparam>
        /// <param name="source">Source sequence.</param>
        /// <param name="count">The number of elements to bypass at the end of the <paramref name="source"/> sequence.</param>
        /// <returns>An <see cref="AsyncEnumerable{T}"/> containing the <paramref name="source"/> sequence's elements except for the bypassed ones at the end.</returns>
        public static AsyncEnumerable<TSource> SkipLast<TSource>(this AsyncEnumerable<TSource> source, int count)
        {
            if (count <= 0)
            {
                // No elements will be skipped, so we can simply return the source. But we need to invalidate it, so increment the id.
                var target = source._target;
                if (target == null)
                {
                    Internal.ThrowInvalidAsyncEnumerable(1);
                }
                return target.GetSelfWithIncrementedId(source._id);
            }

            if (source._target is Internal.AsyncEnumerablePartitionFromLast<TSource> partition)
            {
                return partition.SkipLast(source._id, count);
            }

            var enumerable = Internal.AsyncEnumerablePartitionFromLast<TSource>.GetOrCreate(source.GetAsyncEnumerator(), count, -1);
            return new AsyncEnumerable<TSource>(enumerable);
        }
    }
#endif // CSHARP_7_3_OR_NEWER
}