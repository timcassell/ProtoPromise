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
    /// The result of the channel read operation.
    /// </summary>
    public enum ChannelReadResult : byte
    {
        /// <summary>
        /// The item was not read from the channel because it was empty, and all writers were disposed, ensuring no more items will ever be written to the channel.
        /// </summary>
        Closed,
        /// <summary>
        /// The item was successfully read from the channel.
        /// </summary>
        Success
    }

    /// <summary>
    /// The result of reading an item from the channel.
    /// </summary>
    /// <typeparam name="T">Specifies the type of data that is channeled.</typeparam>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public readonly struct ChannelReadResult<T>
    {
        private readonly T _item;
        private readonly ChannelReadResult _result;

        /// <summary>
        /// The result of the channel read operation.
        /// </summary>
        public ChannelReadResult Result
        {
            [MethodImpl(Internal.InlineOption)]
            get => _result;
        }

        [MethodImpl(Internal.InlineOption)]
        internal ChannelReadResult(T item, ChannelReadResult result)
        {
            _result = result;
            _item = item;
        }

        /// <summary>
        /// Get the item that was read if the channel read operation was successful.
        /// </summary>
        /// <param name="item">When this method returns, contains the item that was read if the operation was successful, <see langword="default"/> otherwise.</param>
        /// <returns><see langword="true"/> if the read operation was successful, <see langword="false"/> otherwise.</returns>
        [MethodImpl(Internal.InlineOption)]
        public bool TryGetItem(out T item)
        {
            item = _item;
            return _result == ChannelReadResult.Success;
        }
    }
}