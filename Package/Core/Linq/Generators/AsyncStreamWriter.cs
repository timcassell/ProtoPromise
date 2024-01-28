using Proto.Promises.CompilerServices;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Proto.Promises.Linq
{
#if CSHARP_7_3_OR_NEWER
    /// <summary>
    /// Type that enables writing to the async stream created from <see cref="AsyncEnumerable{T}.Create(Func{AsyncStreamWriter{T}, CancelationToken, AsyncIteratorMethod})"/>.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public readonly struct AsyncStreamWriter<T>
    {
        private readonly Internal.PromiseRefBase.AsyncEnumerableWithIterator<T> _target;
        private readonly int _id;

        [MethodImpl(Internal.InlineOption)]
        internal AsyncStreamWriter(Internal.PromiseRefBase.AsyncEnumerableWithIterator<T> target, int id)
        {
            _target = target;
            _id = id;
        }

        /// <summary>
        /// Asynchronously writes the <paramref name="value"/> to the stream.
        /// <see langword="await"/> the returned <see cref="AsyncStreamYielder{T}"/> to pause execution until the reader has requested the async iterator to move forward.
        /// </summary>
        [MethodImpl(Internal.InlineOption)]
        public AsyncStreamYielder<T> YieldAsync(T value)
            => _target.YieldAsync(value, _id);
    }
#endif // CSHARP_7_3_OR_NEWER
}