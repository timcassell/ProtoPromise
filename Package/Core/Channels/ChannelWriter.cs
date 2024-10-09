#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Proto.Promises.Channels
{
    /// <summary>
    /// Type used to write data to a <see cref="Channel{T}"/>.
    /// </summary>
    /// <typeparam name="T">Specifies the type of data that is channeled.</typeparam>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public readonly struct ChannelWriter<T> : IEquatable<ChannelWriter<T>>
    {
        private readonly Channel<T> _channel;

        [MethodImpl(Internal.InlineOption)]
        internal ChannelWriter(Channel<T> channel)
        {
            _channel = channel;
        }

        /// <summary>
        /// Asynchronously writes an item to the channel.
        /// </summary>
        /// <param name="item">The value to write to the channel.</param>
        /// <param name="cancelationToken">A <see cref="CancelationToken"/> used to cancel the write operation.</param>
        /// <returns>A <see cref="Promise{T}"/> that yields the result of the write operation.</returns>
        public Promise<ChannelWriteResult<T>> WriteAsync(T item, CancelationToken cancelationToken = default)
            => _channel.ValidateAndGetRef().WriteAsync(item, _channel._id, cancelationToken);

        /// <summary>
        /// Attempts to close the channel in a rejected state.
        /// </summary>
        /// <typeparam name="TReject">The type of the <paramref name="reason"/>.</typeparam>
        /// <param name="reason">The reason for the rejection.</param>
        /// <returns><see langword="true"/> if the channel was not already closed, <see langword="false"/> otherwise.</returns>
        public bool TryReject<TReject>(TReject reason)
        {
            var channel = _channel.ValidateAndGetRef();
            // Check before potentially boxing reason.
            var channelId = _channel._id;
            bool isValid = channelId == channel.Id;
            if (!isValid | channel._closedReason != null)
            {
                if (isValid)
                {
                    return false;
                }
                throw new ObjectDisposedException(nameof(Channel<T>));
            }
            return channel.TryReject(reason, channelId);
        }

        /// <summary>
        /// Attempts to close the channel in a resolved state.
        /// </summary>
        /// <returns><see langword="true"/> if the channel was not already closed, <see langword="false"/> otherwise.</returns>
        public bool TryClose()
            => _channel.ValidateAndGetRef().TryClose(_channel._id);

        /// <summary>Returns a value indicating whether this value is equal to a specified <see cref="ChannelWriter{T}"/>.</summary>
        [MethodImpl(Internal.InlineOption)]
        public bool Equals(ChannelWriter<T> other)
            => this == other;

        /// <summary>Returns a value indicating whether this value is equal to a specified <see cref="object"/>.</summary>
        public override bool Equals(object obj)
            => obj is ChannelWriter<T> other && Equals(other);

        /// <summary>Returns the hash code for this instance.</summary>
        [MethodImpl(Internal.InlineOption)]
        public override int GetHashCode()
            => _channel.GetHashCode();

        /// <summary>Returns a value indicating whether two <see cref="ChannelWriter{T}"/> values are equal.</summary>
        [MethodImpl(Internal.InlineOption)]
        public static bool operator ==(ChannelWriter<T> lhs, ChannelWriter<T> rhs)
            => lhs._channel == rhs._channel;

        /// <summary>Returns a value indicating whether two <see cref="ChannelWriter{T}"/> values are not equal.</summary>
        [MethodImpl(Internal.InlineOption)]
        public static bool operator !=(ChannelWriter<T> lhs, ChannelWriter<T> rhs)
            => !(lhs == rhs);
    }
}