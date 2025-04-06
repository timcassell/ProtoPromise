#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using System.Collections.Generic;

#pragma warning disable IDE0062 // Make local function 'static'

namespace Proto.Promises.Linq
{
    partial class AsyncEnumerable
    {
        /// <summary>
        /// Creates a <see cref="HashSet{T}"/> from an async-enumerable sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <param name="source">An async-enumerable sequence to create a <see cref="HashSet{T}"/> for.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>A <see cref="Promise{T}"/> whose result will be a <see cref="HashSet{T}"/> that contains the elements from the source sequence.</returns>
        public static Promise<HashSet<TSource>> ToHashSetAsync<TSource>(this AsyncEnumerable<TSource> source, CancelationToken cancelationToken = default)
            => ToHashSetAsync(source, null, cancelationToken);

        /// <summary>
        /// Creates a <see cref="HashSet{T}"/> from an async-enumerable sequence using the specified <paramref name="comparer"/> to compare keys.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <param name="source">An async-enumerable sequence to create a <see cref="HashSet{T}"/> for.</param>
        /// <param name="comparer">An <see cref="IEqualityComparer{T}"/> to compare keys.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>A <see cref="Promise{T}"/> whose result will be a <see cref="HashSet{T}"/> that contains values of type <typeparamref name="TSource"/> selected from the source sequence.</returns>
        public static Promise<HashSet<TSource>> ToHashSetAsync<TSource>(this AsyncEnumerable<TSource> source, IEqualityComparer<TSource> comparer, CancelationToken cancelationToken = default)
        {
            return Core(source.GetAsyncEnumerator(cancelationToken), comparer);

            async Promise<HashSet<TSource>> Core(AsyncEnumerator<TSource> asyncEnumerator, IEqualityComparer<TSource> c)
            {
                try
                {
                    var set = new HashSet<TSource>(c);
                    while (await asyncEnumerator.MoveNextAsync())
                    {
                        set.Add(asyncEnumerator.Current);
                    }
                    return set;
                }
                finally
                {
                    await asyncEnumerator.DisposeAsync();
                }
            }
        }
    }
}