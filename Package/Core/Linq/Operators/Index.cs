#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Proto.Promises.Linq
{
#if CSHARP_7_3_OR_NEWER
    partial class AsyncEnumerable
    {
        /// <summary>
        /// Incorporates each element of an async-enumerable sequence into a tuple containg the element and its index.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <param name="source">The source async-enumerable sequence.</param>
        /// <returns>An <see cref="AsyncEnumerable{T}"/> that contains tuples of each element and its index in <paramref name="source"/>.</returns>
        public static AsyncEnumerable<(int Index, TSource Item)> Index<TSource>(this AsyncEnumerable<TSource> source)
            => AsyncEnumerable<(int, TSource)>.Create(new IndexIterator<TSource>(source.GetAsyncEnumerator()));

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        private readonly struct IndexIterator<TSource> : IAsyncIterator<(int, TSource)>
        {
            private readonly AsyncEnumerator<TSource> _asyncEnumerator;

            internal IndexIterator(AsyncEnumerator<TSource> asyncEnumerator)
            {
                _asyncEnumerator = asyncEnumerator;
            }

            public async AsyncIteratorMethod Start(AsyncStreamWriter<(int, TSource)> writer, CancelationToken cancelationToken)
            {
                // The enumerator was retrieved without a cancelation token when the original function was called.
                // We need to propagate the token that was passed in, so we assign it before starting iteration.
                _asyncEnumerator._target._cancelationToken = cancelationToken;

                try
                {
                    int i = -1;
                    while (await _asyncEnumerator.MoveNextAsync())
                    {
                        await writer.YieldAsync((checked(++i), _asyncEnumerator.Current));
                    }

                    // We yield and wait for the enumerator to be disposed, but only if there were no exceptions.
                    await writer.YieldAsync(default).ForLinqExtension();
                }
                finally
                {
                    await _asyncEnumerator.DisposeAsync();
                }
            }

            [MethodImpl(Internal.InlineOption)]
            public Promise DisposeAsyncWithoutStart()
                => _asyncEnumerator.DisposeAsync();
        }
    }
#endif // CSHARP_7_3_OR_NEWER
}