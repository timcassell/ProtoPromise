﻿#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
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
        /// The item was not written to the channel because all readers were disposed, ensuring no more items will ever be read from the channel.
        /// </summary>
        Closed,
        /// <summary>
        /// The item was written to the channel without removing another item.
        /// </summary>
        Success,
        /// <summary>
        /// The channel was full and the newest item was removed to make room for the item that was written.
        /// </summary>
        DroppedNewest,
        /// <summary>
        /// The channel was full and the oldest item was removed to make room for the item that was written.
        /// </summary>
        DroppedOldest,
        /// <summary>
        /// The item was not written to the channel because the channel was full.
        /// </summary>
        DroppedWrite
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
        /// Get the item that was dropped if an item was dropped. An item was dropped if <see cref="Result"/> != <see cref="ChannelWriteResult.Success"/>.
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