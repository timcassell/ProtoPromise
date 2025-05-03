#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#pragma warning disable IDE0090 // Use 'new(...)'
#pragma warning disable IDE0270 // Use coalesce expression

using Proto.Promises.Collections;
using Proto.Promises.CompilerServices;
using System;
using System.Diagnostics;
using static Proto.Promises.Internal;

namespace Proto.Promises.Linq.Sources
{
    /// <summary>
    /// A static class containg helper methods used to facilitate <see cref="AsyncEnumerable{T}"/> extension implementations.
    /// </summary>
    public static class AsyncEnumerableSourceHelpers
    {
        /// <summary>
        /// Communicates to the async enumerator consumer that there are no more items, and asynchronously waits for the async enumerator to be disposed.
        /// <para/>
        /// <see langword="await"/> the returned <see cref="AsyncStreamYielder{T}"/> to pause the async iterator function until the reader has requested the async enumerator to move forward.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the async-enumerable sequence.</typeparam>
        /// <param name="streamWriter">The writer that was passed to the async iterator function.</param>
        /// <returns>An awaitable object that should be immediately awaited to pause the async iterator function.</returns>
        /// <exception cref="ArgumentException"><paramref name="streamWriter"/> is a default value.</exception>
        /// <exception cref="InvalidOperationException">The async iterator function for the associated <paramref name="streamWriter"/> is not valid for yielding.</exception>
        /// <remarks>
        /// This function is useful when wrapping another <see cref="AsyncEnumerable{T}"/> that may yield <see cref="TempCollection{T}"/>s to prevent disposing before the associated async enumerator has been disposed.
        /// <para/>
        /// This function should only be used as the last operation in the async iterator function before any <see langword="finally"/> blocks.
        /// </remarks>
        public static AsyncStreamYielder<T> WaitForDisposeAsync<T>(AsyncStreamWriter<T> streamWriter)
        {
            var target = streamWriter._target;
            if (target is null)
            {
                throw new ArgumentException("streamWriter is a default value.", nameof(streamWriter));
            }
            return target.YieldAsync(default, streamWriter._id, false);
        }

        /// <summary>
        /// Moves the <paramref name="source"/>, such that the previous object is no longer valid for enumeration.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the async-enumerable sequence.</typeparam>
        /// <param name="source">The <see cref="AsyncEnumerable{T}"/> to move.</param>
        /// <returns>The moved <paramref name="source"/>.</returns>
        /// <exception cref="ArgumentException"><paramref name="source"/> is a default value.</exception>
        /// <exception cref="InvalidOperationException">The <paramref name="source"/> is not valid for enumeration.</exception>
        public static AsyncEnumerable<T> Move<T>(AsyncEnumerable<T> source)
        {
            var target = source._target;
            if (target is null)
            {
                throw new ArgumentException("source is a default value.", nameof(source));
            }
            return target.GetSelfWithIncrementedId(source._id);
        }

        /// <summary>
        /// Moves the <paramref name="source"/>, such that the previous object is no longer valid for enumeration.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the async-enumerable sequence.</typeparam>
        /// <param name="source">The <see cref="ConfiguredAsyncEnumerable{T}"/> to move.</param>
        /// <returns>The moved <paramref name="source"/>.</returns>
        /// <exception cref="ArgumentException"><paramref name="source"/> is a default value.</exception>
        /// <exception cref="InvalidOperationException">The <paramref name="source"/> is not valid for enumeration.</exception>
        public static ConfiguredAsyncEnumerable<T> Move<T>(ConfiguredAsyncEnumerable<T> source)
            => source.WithMoved(Move(source._enumerable));

        /// <summary>
        /// Returns an empty async-enumerable sequence that wraps the <paramref name="source"/> for disposal.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the async-enumerable sequence.</typeparam>
        /// <param name="source">The async-enumerable sequence that will be disposed.</param>
        /// <returns>An async-enumerable sequence with no elements that wraps <paramref name="source"/> for disposal.</returns>
        /// <exception cref="ArgumentException"><paramref name="source"/> is a default value.</exception>
        /// <exception cref="InvalidOperationException">The <paramref name="source"/> is not valid for enumeration.</exception>
        internal static AsyncEnumerable<TSource> EmptyWithDispose<TSource>(AsyncEnumerable<TSource> source)
        {
            var target = source._target;
            if (target is null)
            {
                throw new ArgumentException("source is a default value.", nameof(source));
            }

            // TODO: optimize the empty enumerable with its own class.
            // Add IEmptyAsyncEnumerable interface to check against, implement it on AsyncEnumerableEmptySentinel<T> and AsyncEnumerableCanceledSentinel<T> and the new class.
            if (target is AsyncEnumerableCreate<TSource, EmptyIterator<TSource>>)
            {
                return Move(source);
            }

            var enumerable = AsyncEnumerableCreate<TSource, EmptyIterator<TSource>>.GetOrCreate(new EmptyIterator<TSource>(source.GetAsyncEnumerator()));
            return new AsyncEnumerable<TSource>(enumerable);
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        private readonly struct EmptyIterator<TSource> : IAsyncIterator<TSource>
        {
            private readonly AsyncEnumerator<TSource> _source;

            internal EmptyIterator(AsyncEnumerator<TSource> source)
                => _source = source;

            public Promise DisposeAsyncWithoutStart()
                => _source.DisposeAsync();

            // We're only using this to dispose the source. No elements will be yielded, so we don't need an async state machine.
            public AsyncIteratorMethod Start(AsyncStreamWriter<TSource> streamWriter, CancelationToken cancelationToken)
                => new AsyncIteratorMethod(_source.DisposeAsync());
        }
    }
}