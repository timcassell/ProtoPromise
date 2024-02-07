#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using Proto.Promises.Collections;
using System;

#pragma warning disable IDE0062 // Make local function 'static'
#pragma warning disable IDE0063 // Use simple 'using' statement

namespace Proto.Promises.Linq
{
#if CSHARP_7_3_OR_NEWER
    partial class AsyncEnumerable
    {
        /// <summary>
        /// Creates an array from an async-enumerable sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <param name="source">An async-enumerable sequence to create a lookup for.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>A <see cref="Promise{T}"/> whose result will be an array that contains the elements from the source sequence.</returns>
        public static Promise<TSource[]> ToArrayAsync<TSource>(this AsyncEnumerable<TSource> source, CancelationToken cancelationToken = default)
        {
            return Core(source.GetAsyncEnumerator(cancelationToken));

            async Promise<TSource[]> Core(AsyncEnumerator<TSource> asyncEnumerator)
            {
                try
                {
                    // Make sure at least 1 element exists before creating the builder.
                    if (!await asyncEnumerator.MoveNextAsync())
                    {
                        return Array.Empty<TSource>();
                    }

                    using (var builder = new TempCollectionBuilder<TSource>(1))
                    {
                        do
                        {
                            builder.Add(asyncEnumerator.Current);
                        } while (await asyncEnumerator.MoveNextAsync());

                        return builder.View.ToArray();
                    }
                }
                finally
                {
                    await asyncEnumerator.DisposeAsync();
                }
            }
        }
    }
#endif // CSHARP_7_3_OR_NEWER
}