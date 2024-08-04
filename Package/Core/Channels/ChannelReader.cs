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
        /// Asynchronously tries to read an item from the channel.
        /// </summary>
        /// <param name="cancelationToken">A <see cref="CancelationToken"/> used to cancel the read operation.</param>
        /// <param name="synchronousContinuation">
        /// If the read operation did not complete immediately, the async continuation will be executed synchronously if <see langword="true"/>, or asynchronously if <see langword="false"/>.
        /// </param>
        /// <returns>A <see cref="Promise{T}"/> that yields whether the read was a success, and the item that was read.</returns>
        /// <remarks>
        /// If the read was unsuccessful, it means the channel was completed, and no more items will be written to it.
        /// </remarks>
        public Promise<(bool success, T item)> TryReadAsync(CancelationToken cancelationToken = default, bool synchronousContinuation = false)
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
        /// <param name="synchronousContinuations">
        /// If the read operations do not complete immediately, the async continuations will be executed synchronously if <see langword="true"/>, or asynchronously if <see langword="false"/>.
        /// </param>
        /// <returns>The created <see cref="AsyncEnumerable{T}"/>.</returns>
        public AsyncEnumerable<T> ReadAllAsync(bool synchronousContinuations = false)
            // We add a reader in case this is disposed before the channel is completed.
            => AsyncEnumerable<T>.Create(new AsyncIterator(AddReader(), synchronousContinuations));

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        private readonly struct AsyncIterator : IAsyncIterator<T>
        {
            private readonly ChannelReader<T> _channelReader;
            private readonly bool _synchronousContinuations;

            [MethodImpl(Internal.InlineOption)]
            public AsyncIterator(ChannelReader<T> channelReader, bool synchronousContinuations)
            {
                _channelReader = channelReader;
                _synchronousContinuations = synchronousContinuations;
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
                    while (true)
                    {
                        var (success, item) = await _channelReader.TryReadAsync(cancelationToken, _synchronousContinuations);
                        if (!success)
                        {
                            break;
                        }
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
            => _channel == other._channel;

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
            => lhs._channel == rhs._channel;
    }
}