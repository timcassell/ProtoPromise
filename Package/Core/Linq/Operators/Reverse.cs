#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using Proto.Promises.Collections;
using System.Diagnostics;

namespace Proto.Promises.Linq
{
#if CSHARP_7_3_OR_NEWER
    partial class AsyncEnumerable
    {
        /// <summary>
        /// Inverts the order of the elements in an async-enumerable sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the <paramref name="source"/> sequence.</typeparam>
        /// <param name="source">An async-enumerable sequence of values to reverse.</param>
        /// <returns>An <see cref="AsyncEnumerable{T}"/> whose elements correspond to those of the <paramref name="source"/> in reverse order.</returns>
        public static AsyncEnumerable<TSource> Reverse<TSource>(this AsyncEnumerable<TSource> source)
            => AsyncEnumerable<TSource>.Create(new ReverseIterator<TSource>(source.GetAsyncEnumerator()));

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        private readonly struct ReverseIterator<TSource> : IAsyncIterator<TSource>
        {
            private readonly AsyncEnumerator<TSource> _source;

            internal ReverseIterator(AsyncEnumerator<TSource> source)
                => _source = source;

            public Promise DisposeAsyncWithoutStart()
                => _source.DisposeAsync();

            public async AsyncIteratorMethod Start(AsyncStreamWriter<TSource> writer, CancelationToken cancelationToken)
            {
                // The enumerator was retrieved without a cancelation token when the original function was called.
                // We need to propagate the token that was passed in, so we assign it before starting iteration.
                _source._target._cancelationToken = cancelationToken;

                try
                {
                    // Make sure at least 1 element exists before creating the builder.
                    if (!await _source.MoveNextAsync())
                    {
                        return;
                    }

                    using (var builder = new TempCollectionBuilder<TSource>(1))
                    {
                        do
                        {
                            builder.Add(_source.Current);
                        } while (await _source.MoveNextAsync());

                        for (int i = builder._count - 1; i >= 0; --i)
                        {
                            await writer.YieldAsync(builder[i]);
                        }
                    }

                    // We wait for this enumerator to be disposed in case the source contains temp collections.
                    await writer.YieldAsync(default).ForLinqExtension();
                }
                finally
                {
                    await _source.DisposeAsync();
                }
            }
        }
    }
#endif // CSHARP_7_3_OR_NEWER
}