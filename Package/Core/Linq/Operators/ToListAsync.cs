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
        /// Creates a list from an async-enumerable sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <param name="source">An async-enumerable sequence to create a list for.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>A <see cref="Promise{T}"/> whose result will be a list that contains the elements from the source sequence.</returns>
        public static Promise<List<TSource>> ToListAsync<TSource>(this AsyncEnumerable<TSource> source, CancelationToken cancelationToken = default)
        {
            return Core(source.GetAsyncEnumerator(cancelationToken));

            async Promise<List<TSource>> Core(AsyncEnumerator<TSource> asyncEnumerator)
            {
                try
                {
                    var list = new List<TSource>();
                    while (await asyncEnumerator.MoveNextAsync())
                    {
                        list.Add(asyncEnumerator.Current);
                    }
                    return list;
                }
                finally
                {
                    await asyncEnumerator.DisposeAsync();
                }
            }
        }
    }
}