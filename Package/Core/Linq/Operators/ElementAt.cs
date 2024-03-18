#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#pragma warning disable IDE0062 // Make local function 'static'
#pragma warning disable IDE0063 // Use simple 'using' statement

namespace Proto.Promises.Linq
{
    partial class AsyncEnumerable
    {
        // Implementation note: unlike System.Linq which does a type-check for IList<T> before iterating, we don't do it.
        // This is mostly because async enumerable values are expected to be evaluated lazily rather than all at once.
        // There are a few cases where the optimization would help (like array.ToAsyncEnumerable()), but it's not worth the added complexity to support those.

        /// <summary>
        /// Asynchronously returns the element at a specified index in an async-enumerable sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">The sequence to retrieve an element from.</param>
        /// <param name="index">The zero-based index of the element to retrieve.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>A <see cref="Promise{T}"/> resulting in the element at the specified index.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is less than 0 or greater than or equal to the number of elements in <paramref name="source"/>.</exception>
        public static Promise<TSource> ElementAtAsync<TSource>(this AsyncEnumerable<TSource> source, int index, CancelationToken cancelationToken = default)
        {
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "index must be greater or equal to zero.", Internal.GetFormattedStacktrace(1));
            }

            return Core(source.GetAsyncEnumerator(cancelationToken), index);

            async Promise<TSource> Core(AsyncEnumerator<TSource> asyncEnumerator, int i)
            {
                try
                {
                    while (await asyncEnumerator.MoveNextAsync())
                    {
                        if (i == 0)
                        {
                            return asyncEnumerator.Current;
                        }
                        --i;
                    }
                }
                finally
                {
                    await asyncEnumerator.DisposeAsync();
                }

                throw new ArgumentOutOfRangeException(nameof(index), "index must be less than the number of elements in the sequence.", Internal.GetFormattedStacktrace(1));
            }
        }

        /// <summary>
        /// Asynchronously returns the element at a specified index in an async-enumerable sequence or a default value if the index is out of range.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">The sequence to retrieve an element from.</param>
        /// <param name="index">The zero-based index of the element to retrieve.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>A <see cref="Promise{T}"/> resulting in the element at the specified index.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is less than 0 or greater than or equal to the number of elements in <paramref name="source"/>.</exception>
        public static Promise<TSource> ElementAtOrDefaultAsync<TSource>(this AsyncEnumerable<TSource> source, int index, CancelationToken cancelationToken = default)
        {
            if (index < 0)
            {
                return Promise.Resolved(default(TSource));
            }

            return Core(source.GetAsyncEnumerator(cancelationToken), index);

            async Promise<TSource> Core(AsyncEnumerator<TSource> asyncEnumerator, int i)
            {
                try
                {
                    while (await asyncEnumerator.MoveNextAsync())
                    {
                        if (i == 0)
                        {
                            return asyncEnumerator.Current;
                        }
                        --i;
                    }
                    return default;
                }
                finally
                {
                    await asyncEnumerator.DisposeAsync();
                }
            }
        }

#if NETSTANDARD2_1_OR_GREATER || UNITY_2021_2_OR_NEWER || NETCOREAPP3_0_OR_GREATER // System.Index is available in netcorapp3.0 and netstandard2.1.
        /// <summary>
        /// Asynchronously returns the element at a specified index in an async-enumerable sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">The sequence to retrieve an element from.</param>
        /// <param name="index">The zero-based index of the element to retrieve.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>A <see cref="Promise{T}"/> resulting in the element at the specified index.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is less than 0 or greater than or equal to the number of elements in <paramref name="source"/>.</exception>
        public static Promise<TSource> ElementAtAsync<TSource>(this AsyncEnumerable<TSource> source, System.Index index, CancelationToken cancelationToken = default)
        {
            if (!index.IsFromEnd)
            {
                return source.ElementAtAsync(index.Value, cancelationToken);
            }

            if (index.Value < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(index), $"index ({index}) is out of range.", Internal.GetFormattedStacktrace(1));
            }

            return Core(source.GetAsyncEnumerator(cancelationToken), index.Value);

            async Promise<TSource> Core(AsyncEnumerator<TSource> asyncEnumerator, int indexFromEnd)
            {
                try
                {
                    if (await asyncEnumerator.MoveNextAsync())
                    {
                        using (var queue = new Internal.PoolBackedQueue<TSource>(1))
                        {
                            queue.Enqueue(asyncEnumerator.Current);
                            while (await asyncEnumerator.MoveNextAsync())
                            {
                                if (queue.Count == indexFromEnd)
                                {
                                    queue.Dequeue();
                                }

                                queue.Enqueue(asyncEnumerator.Current);
                            }

                            if (queue.Count == indexFromEnd)
                            {
                                return queue.Dequeue();
                            }
                        }
                    }
                }
                finally
                {
                    await asyncEnumerator.DisposeAsync();
                }

                throw new ArgumentOutOfRangeException(nameof(index), $"index ({new System.Index(indexFromEnd, true)}) is out of range.", Internal.GetFormattedStacktrace(1));
            }
        }

        /// <summary>
        /// Asynchronously returns the element at a specified index in an async-enumerable sequence or a default value if the index is out of range.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">The sequence to retrieve an element from.</param>
        /// <param name="index">The zero-based index of the element to retrieve.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>A <see cref="Promise{T}"/> resulting in the element at the specified index.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is less than 0 or greater than or equal to the number of elements in <paramref name="source"/>.</exception>
        public static Promise<TSource> ElementAtOrDefaultAsync<TSource>(this AsyncEnumerable<TSource> source, System.Index index, CancelationToken cancelationToken = default)
        {
            if (!index.IsFromEnd)
            {
                return source.ElementAtOrDefaultAsync(index.Value, cancelationToken);
            }

            return index.Value < 1
                ? Promise.Resolved(default(TSource))
                : Core(source.GetAsyncEnumerator(cancelationToken), index.Value);

            async Promise<TSource> Core(AsyncEnumerator<TSource> asyncEnumerator, int indexFromEnd)
            {
                try
                {
                    if (await asyncEnumerator.MoveNextAsync())
                    {
                        using (var queue = new Internal.PoolBackedQueue<TSource>(1))
                        {
                            queue.Enqueue(asyncEnumerator.Current);
                            while (await asyncEnumerator.MoveNextAsync())
                            {
                                if (queue.Count == indexFromEnd)
                                {
                                    queue.Dequeue();
                                }

                                queue.Enqueue(asyncEnumerator.Current);
                            }

                            if (queue.Count == indexFromEnd)
                            {
                                return queue.Dequeue();
                            }
                        }
                    }

                    return default;
                }
                finally
                {
                    await asyncEnumerator.DisposeAsync();
                }
            }
        }
#endif // NETSTANDARD2_1_OR_GREATER || UNITY_2021_2_OR_NEWER || NETCOREAPP3_0_OR_GREATER
    }
}