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
    /// The result of the channel write operation.
    /// </summary>
    public enum ChannelWriteResult : byte
    {
        /// <summary>
        /// The item was not written to the channel because it was closed.
        /// </summary>
        Closed,
        /// <summary>
        /// The item was written to the channel without dropping another item.
        /// </summary>
        Success,
        /// <summary>
        /// An item was dropped from the channel because it was full. The item that was dropped depends on the options that were used to create the channel.
        /// </summary>
        DroppedItem,
    }

    /// <summary>
    /// The result of writing an item to the channel.
    /// </summary>
    /// <typeparam name="T">Specifies the type of data that is channeled.</typeparam>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public readonly struct ChannelWriteResult<T>
    {
        private readonly T _droppedItem;
        private readonly ChannelWriteResult _result;

        /// <summary>
        /// The result of the write operation.
        /// </summary>
        public ChannelWriteResult Result
        {
            [MethodImpl(Internal.InlineOption)]
            get => _result;
        }

        [MethodImpl(Internal.InlineOption)]
        internal ChannelWriteResult(T droppedItem, ChannelWriteResult result)
        {
            _result = result;
            _droppedItem = droppedItem;
        }

        /// <summary>
        /// Get the item that was dropped if an item was dropped.
        /// </summary>
        /// <param name="droppedItem">When this method returns, contains the dropped item if an item was dropped, <see langword="default"/> otherwise.</param>
        /// <returns><see langword="true"/> if an item was dropped, <see langword="false"/> otherwise.</returns>
        [MethodImpl(Internal.InlineOption)]
        public bool TryGetDroppedItem(out T droppedItem)
        {
            droppedItem = _droppedItem;
            return _result != ChannelWriteResult.Success;
        }
    }
}