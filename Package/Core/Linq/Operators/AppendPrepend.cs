#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

namespace Proto.Promises.Linq
{
    partial class AsyncEnumerable
    {
        /// <summary>
        /// Append a value to an async-enumerable sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <param name="source">Source sequence to check for emptiness.</param>
        /// <param name="element">Element to append to the specified sequence.</param>
        /// <returns>The source sequence appended with the specified value.</returns>
        public static AsyncEnumerable<TSource> Append<TSource>(this AsyncEnumerable<TSource> source, TSource element)
            => Internal.AppendPrependHelper<TSource>.Append(source, element);

        /// <summary>
        /// Prepend a value to an async-enumerable sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <param name="source">Source sequence to check for emptiness.</param>
        /// <param name="element">Element to prepend to the specified sequence.</param>
        /// <returns>The source sequence prepended with the specified value.</returns>
        public static AsyncEnumerable<TSource> Prepend<TSource>(this AsyncEnumerable<TSource> source, TSource element)
            => Internal.AppendPrependHelper<TSource>.Prepend(source, element);
    }
}