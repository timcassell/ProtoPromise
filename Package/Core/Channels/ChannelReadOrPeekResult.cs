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
    /// The result of the channel read or peek operation.
    /// </summary>
    public enum ChannelReadOrPeekResult : byte
    {
        /// <summary>
        /// The item was not read or peeked from the channel because it was empty, and all writers were disposed, ensuring no more items will ever be written to the channel.
        /// </summary>
        Closed,
        /// <summary>
        /// The item was successfully read or peeked from the channel.
        /// </summary>
        Success

        // TODO: If we add a ChannelReader<T>.TryPeek synchronous method that does not wait, we need to add the Empty result.
    }

    /// <summary>
    /// The result of reading or peeking an item from the channel.
    /// </summary>
    /// <typeparam name="T">Specifies the type of data that is channeled.</typeparam>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public readonly struct ChannelReadOrPeekResult<T>
    {
        private readonly T _item;
        private readonly ChannelReadOrPeekResult _result;

        /// <summary>
        /// The result of the read or peek operation.
        /// </summary>
        public ChannelReadOrPeekResult Result
        {
            [MethodImpl(Internal.InlineOption)]
            get => _result;
        }

        [MethodImpl(Internal.InlineOption)]
        internal ChannelReadOrPeekResult(T item, ChannelReadOrPeekResult result)
        {
            _result = result;
            _item = item;
        }

        /// <summary>
        /// Get the item that was read or peeked if the operation was successful.
        /// </summary>
        /// <param name="item">When this method returns, contains the item that was read or peeked if the operation was successful, <see langword="default"/> otherwise.</param>
        /// <returns><see langword="true"/> if the read or peek operation was successful, <see langword="false"/> otherwise.</returns>
        [MethodImpl(Internal.InlineOption)]
        public bool TryGetItem(out T item)
        {
            item = _item;
            return _result != ChannelReadOrPeekResult.Success;
        }
    }
}