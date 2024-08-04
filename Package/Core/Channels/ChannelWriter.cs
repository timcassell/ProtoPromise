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
    public readonly struct ChannelWriter<T> : IDisposable, IEquatable<ChannelWriter<T>>
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
        /// <param name="synchronousContinuation">
        /// If the write operation did not complete immediately, the async continuation will be executed synchronously if <see langword="true"/>, or asynchronously if <see langword="false"/>.
        /// </param>
        /// <returns>A <see cref="Promise{T}"/> that yields the result of the write operation.</returns>
        public Promise<ChannelWriteResult<T>> WriteAsync(T item, CancelationToken cancelationToken = default, bool synchronousContinuation = false)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Completes the channel in a rejected state.
        /// </summary>
        /// <typeparam name="TReject">The type of the <paramref name="reason"/>.</typeparam>
        /// <param name="reason">The reason for the rejection.</param>
        /// <returns><see langword="true"/> if the channel was not already rejected, <see langword="false"/> otherwise.</returns>
        public bool TryReject<TReject>(TReject reason)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Adds a writer to the channel. Call <see cref="Dispose"/> to remove the writer.
        /// </summary>
        /// <returns><see langword="this"/></returns>
        public ChannelWriter<T> AddWriter()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Removes the writer from the channel.
        /// </summary>
        /// <remarks>
        /// When all writers have been disposed, the channel will be completed in a resolved state if it was not rejected.
        /// Every writer should be disposed, even if it was rejected, in order to ensure proper cleanup of the channel.
        /// </remarks>
        public void Dispose()
        {
            throw new NotImplementedException();
        }

        /// <summary>Returns a value indicating whether this value is equal to a specified <see cref="ChannelWriter{T}"/>.</summary>
        [MethodImpl(Internal.InlineOption)]
        public bool Equals(ChannelWriter<T> other)
            => _channel == other._channel;

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
            => lhs._channel == rhs._channel;
    }
}