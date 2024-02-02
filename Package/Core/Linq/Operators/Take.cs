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
        /// Returns a specified number of contiguous elements from the start of an async-enumerable sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">The sequence to take elements from.</param>
        /// <param name="count">The number of elements to return.</param>
        /// <returns>An <see cref="AsyncEnumerable{T}"/> that contains the specified number of elements from the start of the input sequence.</returns>
        public static AsyncEnumerable<TSource> Take<TSource>(this AsyncEnumerable<TSource> source, int count)
        {
            if (count <= 0)
            {
                // No elements will be yielded, so we can simply return an empty AsyncEnumerable.
                // But we have to dispose the source, so we need to do it with a special empty, instead of AsyncEnumerable<TSource>.Empty().
                return Internal.EmptyHelper.EmptyWithDispose(source.GetAsyncEnumerator());
            }

            if (source._target is Internal.AsyncEnumerablePartition<TSource> partition)
            {
                return partition.Take(source._id, count);
            }

            var enumerable = Internal.AsyncEnumerablePartition<TSource>.GetOrCreate(source.GetAsyncEnumerator(), 0, count - 1);
            return new AsyncEnumerable<TSource>(enumerable);
        }
    }
#endif // CSHARP_7_3_OR_NEWER
}