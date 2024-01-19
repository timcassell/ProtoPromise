#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using Proto.Promises.Collections;

namespace Proto.Promises.Linq
{
#if CSHARP_7_3_OR_NEWER
    partial class AsyncEnumerable
    {
        /// <summary>
        /// Splits the elements of an async-enumerable sequence into chunks of size at most <paramref name="size"/>.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <param name="source">An async-enumerable sequence whose elements to chunk.</param>
        /// <param name="size">The maximum size of each chunk.</param>
        /// <returns>An <see cref="AsyncEnumerable{T}"/> that contains the elements of the input sequence split into chunks of size <paramref name="size"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="size"/> is below 1.</exception>
        /// <remarks>
        /// Each chunk except the last one will be of size <paramref name="size"/>. The last chunk will contain the remaining elements and may be of a smaller size.
        /// <para/>
        /// Each <see cref="TempCollection{T}"/> yielded by the returned <see cref="AsyncEnumerable{T}"/> will no longer be valid when its
        /// <see cref="AsyncEnumerator{T}"/> is disposed or completed unsuccessfully. If you need the elements to persist, copy them to a new collection,
        /// or call <see cref="TempCollection{T}.ToArray"/> or <see cref="TempCollection{T}.ToList"/>.
        /// </remarks>
        public static AsyncEnumerable<TempCollection<TSource>> Chunk<TSource>(this AsyncEnumerable<TSource> source, int size)
        {
            if (size < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(size), "size must be greater than 0.", Internal.GetFormattedStacktrace(1));
            }

            return ChunkFull(source.GetAsyncEnumerator(), size);
        }
        /// <summary>
        /// Splits the elements of an async-enumerable sequence into chunks of size at most <paramref name="size"/>.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <param name="source">An async-enumerable sequence whose elements to chunk.</param>
        /// <param name="size">The maximum size of each chunk.</param>
        /// <param name="allowSameStorage">If <see langword="true"/>, each yielded <see cref="TempCollection{T}"/> may use the same backing storage.</param>
        /// <returns>An <see cref="AsyncEnumerable{T}"/> that contains the elements of the input sequence split into chunks of size <paramref name="size"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="size"/> is below 1.</exception>
        /// <remarks>
        /// Each chunk except the last one will be of size <paramref name="size"/>. The last chunk will contain the remaining elements and may be of a smaller size.
        /// <para/>
        /// Each <see cref="TempCollection{T}"/> yielded by the returned <see cref="AsyncEnumerable{T}"/> will no longer be valid when its
        /// <see cref="AsyncEnumerator{T}"/> is disposed or completed unsuccessfully, or when the <see cref="AsyncEnumerator{T}"/> is moved forward if
        /// <paramref name="allowSameStorage"/> is set to <see langword="true"/>. If you need the elements to persist, copy them to a new collection,
        /// or call <see cref="TempCollection{T}.ToArray"/> or <see cref="TempCollection{T}.ToList"/>.
        /// </remarks>
        public static AsyncEnumerable<TempCollection<TSource>> Chunk<TSource>(this AsyncEnumerable<TSource> source, int size, bool allowSameStorage)
        {
            if (size < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(size), "size must be greater than 0.", Internal.GetFormattedStacktrace(1));
            }

            return allowSameStorage
                ? ChunkSameStorage(source.GetAsyncEnumerator(), size)
                : ChunkFull(source.GetAsyncEnumerator(), size);
        }

        private static AsyncEnumerable<TempCollection<TSource>> ChunkFull<TSource>(AsyncEnumerator<TSource> asyncEnumerator, int size)
            => AsyncEnumerable<TempCollection<TSource>>.Create((asyncEnumerator, size), async (cv, writer, cancelationToken) =>
            {
                // We store each builder to be disposed after the entire operation.
                // In most cases one might think we could use a single builder for all yields, but users could combine this with further Linq queries,
                // so we need them to persist until the enumerator is disposed.
                var chunkBuilders = new TempCollectionBuilder<TempCollectionBuilder<TSource>>(0);
                try
                {
                    while (await cv.asyncEnumerator.MoveNextAsync())
                    {
                        int currentChunk = chunkBuilders._count;
                        // We start with capacity 1 instead of size in case the chunk size is very large but the source only has few elements.
                        chunkBuilders.Add(new TempCollectionBuilder<TSource>(1, 1));
                        chunkBuilders._items[currentChunk]._items[0] = cv.asyncEnumerator.Current;
                        for (int i = 1; i < cv.size; ++i)
                        {
                            if (!await cv.asyncEnumerator.MoveNextAsync())
                            {
                                await writer.YieldAsync(chunkBuilders._items[currentChunk].View);
                                goto End;
                            }
                            chunkBuilders._items[currentChunk].Add(cv.asyncEnumerator.Current);
                        }

                        await writer.YieldAsync(chunkBuilders._items[currentChunk].View);
                    }
                End:

                    // We yield and wait for the enumerator to be disposed, but only if there were no exceptions.
                    await writer.YieldAsync(default).ForLinqExtension();
                }
                finally
                {
                    for (int i = 0; i < chunkBuilders._count; ++i)
                    {
                        chunkBuilders._items[i].Dispose();
                    }
                    chunkBuilders.Dispose();
                    await cv.asyncEnumerator.DisposeAsync();
                }
            });

        private static AsyncEnumerable<TempCollection<TSource>> ChunkSameStorage<TSource>(AsyncEnumerator<TSource> asyncEnumerator, int size)
            => AsyncEnumerable<TempCollection<TSource>>.Create((asyncEnumerator, size), async (cv, writer, cancelationToken) =>
            {
                // In this case, the user explicitly declared that they don't need each yielded TempCollection to persist, so we can optimize it to use a single builder.
                try
                {
                    if (!await cv.asyncEnumerator.MoveNextAsync())
                    {
                        // Empty source.
                        return;
                    }

#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                    // Even though the user declared that the TempCollection storage can be re-used, we use different builders in DEBUG mode for validation purposes.
                    do
                    {
                        // We start with capacity 1 instead of size in case the chunk size is very large but the source only has few elements.
                        using (var chunkBuilder = new TempCollectionBuilder<TSource>(1))
                        {
                            chunkBuilder.Add(cv.asyncEnumerator.Current);
                            for (int i = 1; i < cv.size; ++i)
                            {
                                if (!await cv.asyncEnumerator.MoveNextAsync())
                                {
                                    await writer.YieldAsync(chunkBuilder.View);
                                    goto End;
                                }
                                chunkBuilder.Add(cv.asyncEnumerator.Current);
                            }

                            await writer.YieldAsync(chunkBuilder.View);
                        }
                    } while (await cv.asyncEnumerator.MoveNextAsync());
#else // PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                    // We start with capacity 1 instead of size in case the chunk size is very large but the source only has few elements.
                    using (var chunkBuilder = new TempCollectionBuilder<TSource>(1))
                    {
                        do
                        {
                            chunkBuilder.Clear();
                            chunkBuilder.Add(cv.asyncEnumerator.Current);
                            for (int i = 1; i < cv.size; ++i)
                            {
                                if (!await cv.asyncEnumerator.MoveNextAsync())
                                {
                                    await writer.YieldAsync(chunkBuilder.View);
                                    goto End;
                                }
                                chunkBuilder.Add(cv.asyncEnumerator.Current);
                            }

                            await writer.YieldAsync(chunkBuilder.View);
                        } while (await cv.asyncEnumerator.MoveNextAsync());
                    }
#endif // PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                End:

                    // We yield and wait for the enumerator to be disposed, but only if there were no exceptions.
                    await writer.YieldAsync(default).ForLinqExtension();
                }
                finally
                {
                    await cv.asyncEnumerator.DisposeAsync();
                }
            });
    }
#endif // CSHARP_7_3_OR_NEWER
}