#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#pragma warning disable IDE0180 // Use tuple to swap values

namespace Proto.Promises.Linq
{
    partial class AsyncEnumerable
    {
        /// <summary>
        /// Concatenates two async-enumerable sequences.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of the input sequences.</typeparam>
        /// <param name="first">The first sequence to concatenate.</param>
        /// <param name="second">The sequence to concatenate to the first sequence.</param>
        /// <returns>An <see cref="AsyncEnumerable{T}"/> that contains the concatenated elements of the two input sequences.</returns>
        public static AsyncEnumerable<TSource> Concat<TSource>(this AsyncEnumerable<TSource> first, AsyncEnumerable<TSource> second)
        {
            // If the first or second were already concatenated, we flatten the concatenations instead of calling into them recursively.
            if (first._target is Internal.ConcatAsyncEnumerableBase<TSource> concatEnumerable1)
            {
                concatEnumerable1.IncrementId(first._id);
                if (second._target is Internal.ConcatAsyncEnumerableBase<TSource> concatEnumerable2)
                {
                    concatEnumerable2.IncrementId(second._id);
                    // Both were previously concatenated, just link them together and return.
                    var secondHead = concatEnumerable2._next;
                    concatEnumerable2._next = concatEnumerable1._next;
                    concatEnumerable1._next = secondHead;
                    return new AsyncEnumerable<TSource>(concatEnumerable2, concatEnumerable2._id);
                }

                var enumerable = Internal.ConcatNAsyncEnumerable<TSource>.GetOrCreate(concatEnumerable1._next, second.GetAsyncEnumerator());
                concatEnumerable1._next = enumerable;
                return new AsyncEnumerable<TSource>(enumerable, enumerable._id);
            }
            else if (second._target is Internal.ConcatAsyncEnumerableBase<TSource> concatEnumerable2)
            {
                concatEnumerable2.IncrementId(second._id);
                var newHead = Internal.ConcatNAsyncEnumerable<TSource>.GetOrCreate(concatEnumerable2._next, first.GetAsyncEnumerator());
                concatEnumerable2._next = newHead;
                return new AsyncEnumerable<TSource>(concatEnumerable2, concatEnumerable2._id);
            }
            else
            {
                var enumerable = Internal.Concat2AsyncEnumerable<TSource>.GetOrCreate(first.GetAsyncEnumerator(), second.GetAsyncEnumerator());
                return new AsyncEnumerable<TSource>(enumerable, enumerable._id);
            }
        }
    }
}