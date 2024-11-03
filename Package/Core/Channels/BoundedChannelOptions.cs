#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using System.Diagnostics;

namespace Proto.Promises.Channels
{
    /// <summary>
    /// Specifies the behavior to use when writing to a bounded channel that is already full.
    /// </summary>
    public enum BoundedChannelFullMode : byte
    {
        /// <summary>
        /// Wait for space to be available in order to complete the write operation.
        /// </summary>
        Wait,
        /// <summary>
        /// Remove the newest item in the channel in order to make room for the item being written.
        /// </summary>
        DropNewest,
        /// <summary>
        /// Remove the oldest item in the channel in order to make room for the item being written.
        /// </summary>
        DropOldest,
        /// <summary>
        /// Drop the item being written.
        /// </summary>
        DropWrite
    }

    /// <summary>
    /// Provides options that control the behavior of bounded <see cref="Channel{T}"/> instances.
    /// </summary>
    /// <typeparam name="T">Specifies the type of data that is channeled.</typeparam>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public struct BoundedChannelOptions<T>
    {
        /// <summary>
        /// Gets or sets the maximum number of items the bounded channel may store.
        /// </summary>
        public int Capacity { get; set; }

        /// <summary>
        /// Gets or sets the behavior incurred by write operations when the channel is full.
        /// </summary>
        public BoundedChannelFullMode FullMode { get; set; }
    }
}