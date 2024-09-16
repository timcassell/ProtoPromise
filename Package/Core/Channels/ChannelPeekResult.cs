#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Proto.Promises.Channels
{
    /// <summary>
    /// The result of the channel peek operation.
    /// </summary>
    public enum ChannelPeekResult : byte
    {
        /// <summary>
        /// An item was not peeked at from the channel because it was empty, and all writers were disposed, ensuring no more items will ever be written to the channel.
        /// </summary>
        Closed,
        /// <summary>
        /// An item was successfully peeked at from the channel.
        /// </summary>
        Success

        // TODO: If we add a ChannelReader<T>.TryPeek synchronous method that does not wait, we need to add the Empty result.
    }

    /// <summary>
    /// The result of peeking at an item from the channel.
    /// </summary>
    /// <typeparam name="T">Specifies the type of data that is channeled.</typeparam>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public readonly struct ChannelPeekResult<T>
    {
        private readonly T _item;
        private readonly ChannelPeekResult _result;

        /// <summary>
        /// The result of the peek operation.
        /// </summary>
        public ChannelPeekResult Result
        {
            [MethodImpl(Internal.InlineOption)]
            get => _result;
        }

        [MethodImpl(Internal.InlineOption)]
        internal ChannelPeekResult(T item, ChannelPeekResult result)
        {
            _result = result;
            _item = item;
        }

        /// <summary>
        /// Get the item that was peeked at if the peek operation was successful.
        /// </summary>
        /// <param name="item">When this method returns, contains the item that was peeked at if the operation was successful, <see langword="default"/> otherwise.</param>
        /// <returns><see langword="true"/> if the peek operation was successful, <see langword="false"/> otherwise.</returns>
        [MethodImpl(Internal.InlineOption)]
        public bool TryGetItem(out T item)
        {
            item = _item;
            return _result == ChannelPeekResult.Success;
        }
    }
}