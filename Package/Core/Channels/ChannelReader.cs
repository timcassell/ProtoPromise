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
    public readonly struct ChannelReader<T> : IDisposable, IEquatable<ChannelReader<T>>
    {
        private readonly Channel<T> _channel;

        [MethodImpl(Internal.InlineOption)]
        internal ChannelReader(Channel<T> channel)
        {
            _channel = channel;
        }

        /// <summary>
        /// Asynchronously attempts to peek at an item from the channel.
        /// </summary>
        /// <param name="cancelationToken">A <see cref="CancelationToken"/> used to cancel the peek operation.</param>
        /// <returns>A <see cref="Promise{T}"/> that yields the result of the peek operation.</returns>
        public Promise<ChannelReadOrPeekResult<T>> TryPeekAsync(CancelationToken cancelationToken = default)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Asynchronously attempts to read an item from the channel.
        /// </summary>
        /// <param name="cancelationToken">A <see cref="CancelationToken"/> used to cancel the read operation.</param>
        /// <returns>A <see cref="Promise{T}"/> that yields the result of the read operation.</returns>
        public Promise<ChannelReadOrPeekResult<T>> TryReadAsync(CancelationToken cancelationToken = default)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Adds a reader to the channel. Call <see cref="Dispose"/> to remove the reader.
        /// </summary>
        /// <returns><see langword="this"/></returns>
        public ChannelReader<T> AddReader()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Creates an <see cref="AsyncEnumerable{T}"/> that enables reading all of the data from the channel.
        /// </summary>
        /// <returns>The created <see cref="AsyncEnumerable{T}"/>.</returns>
        public AsyncEnumerable<T> ReadAllAsync()
            // We add a reader in case this is disposed before the channel is completed.
            => AsyncEnumerable<T>.Create(new AsyncIterator(AddReader()));

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

            public Promise DisposeAsyncWithoutStart()
            {
                _channelReader.Dispose();
                return Promise.Resolved();
            }

            public async AsyncIteratorMethod Start(AsyncStreamWriter<T> streamWriter, CancelationToken cancelationToken)
            {
                using (_channelReader)
                {
                    while ((await _channelReader.TryReadAsync(cancelationToken)).TryGetItem(out T item))
                    {
                        await streamWriter.YieldAsync(item);
                    }
                }
            }
        }

        /// <summary>
        /// Removes the reader from the channel.
        /// </summary>
        /// <remarks>
        /// When all readers have been disposed, no more items will be written to the channel, and the channel may not be read from again.
        /// Every reader should be disposed in order to ensure proper cleanup of the channel.
        /// </remarks>
        public void Dispose()
        {
            throw new NotImplementedException();
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