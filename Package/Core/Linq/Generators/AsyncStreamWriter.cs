using Proto.Promises.CompilerServices;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Proto.Promises.Linq
{
    /// <summary>
    /// Type that enables writing to the async stream created from <see cref="AsyncEnumerable{T}.Create(Func{AsyncStreamWriter{T}, CancelationToken, AsyncIteratorMethod})"/>.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public readonly struct AsyncStreamWriter<T>
    {
        internal readonly Internal.PromiseRefBase.AsyncEnumerableWithIterator<T> _target;
        internal readonly int _id;

        [MethodImpl(Internal.InlineOption)]
        internal AsyncStreamWriter(Internal.PromiseRefBase.AsyncEnumerableWithIterator<T> target, int id)
        {
            _target = target;
            _id = id;
        }

        /// <summary>
        /// Asynchronously writes the <paramref name="value"/> to the stream.
        /// <para/>
        /// <see langword="await"/> the returned <see cref="AsyncStreamYielder{T}"/> to pause the async iterator function until the reader has requested the async enumerator to move forward.
        /// </summary>
        /// <returns>An awaitable object that should be immediately awaited to pause the async iterator function.</returns>
        /// <exception cref="NullReferenceException">This is a default value.</exception>
        [MethodImpl(Internal.InlineOption)]
        public AsyncStreamYielder<T> YieldAsync(T value)
            => _target.YieldAsync(value, _id);
    }
}