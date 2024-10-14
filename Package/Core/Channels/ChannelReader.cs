#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using Proto.Promises.Linq;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

#pragma warning disable IDE0290 // Use primary constructor

namespace Proto.Promises.Channels
{
    /// <summary>
    /// Type used to read data from a <see cref="Channel{T}"/>.
    /// </summary>
    /// <typeparam name="T">Specifies the type of data that is channeled.</typeparam>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public readonly struct ChannelReader<T> : IEquatable<ChannelReader<T>>
    {
        private readonly Channel<T> _channel;

        [MethodImpl(Internal.InlineOption)]
        internal ChannelReader(Channel<T> channel)
        {
            _channel = channel;
        }

        /// <summary>
        /// Gets the current number of items available from the channel.
        /// </summary>
        public int Count => _channel.ValidateAndGetRef().GetCount(_channel._id);

        /// <summary>
        /// Asynchronously waits for data to be available to be read.
        /// </summary>
        /// <param name="cancelationToken">A <see cref="CancelationToken"/> used to cancel the wait operation.</param>
        /// <returns>
        /// A <see cref="Promise{T}"/> that will resolve with <see langword="true"/> when data is available to be read,
        /// or <see langword="false"/> when the channel is closed.
        /// </returns>
        public Promise<bool> WaitToReadAsync(CancelationToken cancelationToken = default)
            => _channel.ValidateAndGetRef().WaitToReadAsync(_channel._id, cancelationToken);

        /// <summary>
        /// Attempts to peek at an item from the channel in a non-blocking manner.
        /// </summary>
        /// <returns>The result of the peek operation.</returns>
        public ChannelPeekResult<T> TryPeek()
            => _channel.ValidateAndGetRef().TryPeek(_channel._id);

        /// <summary>
        /// Attempts to read an item from the channel in a non-blocking manner.
        /// </summary>
        /// <returns>The result of the read operation.</returns>
        public ChannelReadResult<T> TryRead()
            => _channel.ValidateAndGetRef().TryRead(_channel._id);

        /// <summary>
        /// Asynchronously reads an item from the channel.
        /// </summary>
        /// <param name="cancelationToken">A <see cref="CancelationToken"/> used to cancel the read operation.</param>
        /// <returns>A <see cref="Promise{T}"/> that yields the result of the read operation.</returns>
        public Promise<ChannelReadResult<T>> ReadAsync(CancelationToken cancelationToken = default)
            => _channel.ValidateAndGetRef().ReadAsync(_channel._id, cancelationToken);

        /// <summary>
        /// Creates an <see cref="AsyncEnumerable{T}"/> that enables reading all of the data from the channel.
        /// </summary>
        /// <returns>The created <see cref="AsyncEnumerable{T}"/>.</returns>
        public AsyncEnumerable<T> ReadAllAsync()
            // We add a reader in case this is disposed before the channel is completed.
            => AsyncEnumerable<T>.Create(new AsyncIterator(this));

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        private readonly struct AsyncIterator : IAsyncIterator<T>
        {
            private readonly ChannelReader<T> _channelReader;

            [MethodImpl(Internal.InlineOption)]
            public AsyncIterator(ChannelReader<T> channelReader)
            {
                _channelReader = channelReader;
            }

            [MethodImpl(Internal.InlineOption)]
            public Promise DisposeAsyncWithoutStart()
                => Promise.Resolved();

            public async AsyncIteratorMethod Start(AsyncStreamWriter<T> streamWriter, CancelationToken cancelationToken)
            {
                while ((await _channelReader.ReadAsync(cancelationToken)).TryGetItem(out T item))
                {
                    await streamWriter.YieldAsync(item);
                }
            }
        }

        /// <summary>Returns a value indicating whether this value is equal to a specified <see cref="ChannelReader{T}"/>.</summary>
        [MethodImpl(Internal.InlineOption)]
        public bool Equals(ChannelReader<T> other)
            => this == other;

        /// <summary>Returns a value indicating whether this value is equal to a specified <see cref="object"/>.</summary>
        public override bool Equals(object obj)
            => obj is ChannelReader<T> other && Equals(other);

        /// <summary>Returns the hash code for this instance.</summary>
        [MethodImpl(Internal.InlineOption)]
        public override int GetHashCode()
            => _channel.GetHashCode();

        /// <summary>Returns a value indicating whether two <see cref="ChannelReader{T}"/> values are equal.</summary>
        [MethodImpl(Internal.InlineOption)]
        public static bool operator ==(ChannelReader<T> lhs, ChannelReader<T> rhs)
            => lhs._channel == rhs._channel;

        /// <summary>Returns a value indicating whether two <see cref="ChannelReader{T}"/> values are not equal.</summary>
        [MethodImpl(Internal.InlineOption)]
        public static bool operator !=(ChannelReader<T> lhs, ChannelReader<T> rhs)
            => !(lhs == rhs);
    }
}