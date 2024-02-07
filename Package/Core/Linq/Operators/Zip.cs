#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using System.Diagnostics;

namespace Proto.Promises.Linq
{
#if CSHARP_7_3_OR_NEWER
    partial class AsyncEnumerable
    {
        /// <summary>
        /// Produces an async-enumerable sequence of tuples with elements from the specified sequences.
        /// </summary>
        /// <typeparam name="TFirst">The type of the elements of the first input sequences.</typeparam>
        /// <typeparam name="TSecond">The type of the elements of the second input sequences.</typeparam>
        /// <param name="first">The first async-enumerable sequence to merge.</param>
        /// <param name="second">The first async-enumerable sequence to merge.</param>
        /// <returns>An async-enumerable sequence that contains the elements from both input sequences, excluding duplicates.</returns>
        public static AsyncEnumerable<(TFirst First, TSecond Second)> Zip<TFirst, TSecond>(this AsyncEnumerable<TFirst> first, AsyncEnumerable<TSecond> second)
            => AsyncEnumerable<(TFirst, TSecond)>.Create(new ZipIterator<TFirst, TSecond>(first.GetAsyncEnumerator(), second.GetAsyncEnumerator()));

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        private readonly struct ZipIterator<TFirst, TSecond> : IAsyncIterator<(TFirst First, TSecond Second)>
        {
            private readonly AsyncEnumerator<TFirst> _first;
            private readonly AsyncEnumerator<TSecond> _second;

            internal ZipIterator(AsyncEnumerator<TFirst> first, AsyncEnumerator<TSecond> second)
            {
                _first = first;
                _second = second;
            }

            public Promise DisposeAsyncWithoutStart()
            {
                // We consume less memory by using .Finally instead of async/await.
                return _first.DisposeAsync()
                  .Finally(_second, e => e.DisposeAsync());
            }

            public async AsyncIteratorMethod Start(AsyncStreamWriter<(TFirst First, TSecond Second)> writer, CancelationToken cancelationToken)
            {
                // The enumerators were retrieved without a cancelation token when the original function was called.
                // We need to propagate the token that was passed in, so we assign it before starting iteration.
                _first._target._cancelationToken = cancelationToken;
                _second._target._cancelationToken = cancelationToken;

                try
                {
                    while (await _first.MoveNextAsync() && await _second.MoveNextAsync())
                    {
                        await writer.YieldAsync((_first.Current, _second.Current));
                    }

                    // We yield and wait for the enumerator to be disposed, but only if there were no exceptions.
                    await writer.YieldAsync(default).ForLinqExtension();
                }
                finally
                {
                    try
                    {
                        await _first.DisposeAsync();
                    }
                    finally
                    {
                        await _second.DisposeAsync();
                    }
                }
            }
        }

        /// <summary>
        /// Produces an async-enumerable sequence of tuples with elements from the specified sequences.
        /// </summary>
        /// <typeparam name="TFirst">The type of the elements of the first input sequences.</typeparam>
        /// <typeparam name="TSecond">The type of the elements of the second input sequences.</typeparam>
        /// <typeparam name="TThird">The type of the elements of the third input sequences.</typeparam>
        /// <param name="first">The first async-enumerable sequence to merge.</param>
        /// <param name="second">The first async-enumerable sequence to merge.</param>
        /// <param name="third">The first async-enumerable sequence to merge.</param>
        /// <returns>An async-enumerable sequence that contains the elements from both input sequences, excluding duplicates.</returns>
        public static AsyncEnumerable<(TFirst First, TSecond Second, TThird Third)> Zip<TFirst, TSecond, TThird>(this AsyncEnumerable<TFirst> first, AsyncEnumerable<TSecond> second, AsyncEnumerable<TThird> third)
            => AsyncEnumerable<(TFirst, TSecond, TThird)>.Create(new ZipIterator<TFirst, TSecond, TThird>(first.GetAsyncEnumerator(), second.GetAsyncEnumerator(), third.GetAsyncEnumerator()));

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        private readonly struct ZipIterator<TFirst, TSecond, TThird> : IAsyncIterator<(TFirst First, TSecond Second, TThird Third)>
        {
            private readonly AsyncEnumerator<TFirst> _first;
            private readonly AsyncEnumerator<TSecond> _second;
            private readonly AsyncEnumerator<TThird> _third;

            internal ZipIterator(AsyncEnumerator<TFirst> first, AsyncEnumerator<TSecond> second, AsyncEnumerator<TThird> third)
            {
                _first = first;
                _second = second;
                _third = third;
            }

            public Promise DisposeAsyncWithoutStart()
            {
                // We consume less memory by using .Finally instead of async/await.
                return _first.DisposeAsync()
                  .Finally(_second, e => e.DisposeAsync())
                  .Finally(_third, e => e.DisposeAsync());
            }

            public async AsyncIteratorMethod Start(AsyncStreamWriter<(TFirst First, TSecond Second, TThird Third)> writer, CancelationToken cancelationToken)
            {
                // The enumerators were retrieved without a cancelation token when the original function was called.
                // We need to propagate the token that was passed in, so we assign it before starting iteration.
                _first._target._cancelationToken = cancelationToken;
                _second._target._cancelationToken = cancelationToken;
                _third._target._cancelationToken = cancelationToken;

                try
                {
                    while (await _first.MoveNextAsync() && await _second.MoveNextAsync() && await _third.MoveNextAsync())
                    {
                        await writer.YieldAsync((_first.Current, _second.Current, _third.Current));
                    }

                    // We yield and wait for the enumerator to be disposed, but only if there were no exceptions.
                    await writer.YieldAsync(default).ForLinqExtension();
                }
                finally
                {
                    try
                    {
                        await _first.DisposeAsync();
                    }
                    finally
                    {
                        try
                        {
                            await _second.DisposeAsync();
                        }
                        finally
                        {
                            await _third.DisposeAsync();
                        }
                    }
                }
            }
        }
    }
#endif // CSHARP_7_3_OR_NEWER
}