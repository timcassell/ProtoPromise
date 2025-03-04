﻿#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
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
        /// Asynchronously waits for space to be available to write an item.
        /// </summary>
        /// <returns>
        /// A <see cref="Promise{T}"/> that will resolve with <see langword="true"/> when space is available to write an item,
        /// or <see langword="false"/> when the channel is closed.
        /// </returns>
        /// <exception cref="ObjectDisposedException">The channel was disposed.</exception>
        /// <exception cref="NullReferenceException">This is a default value.</exception>
        public Promise<bool> WaitToWriteAsync()
            => WaitToWriteAsync(CancelationToken.None, true);

        /// <summary>
        /// Asynchronously waits for space to be available to write an item.
        /// </summary>
        /// <param name="continueOnCapturedContext">If <see langword="true"/> and space is not immediately available, the async continuation will be executed on the captured context.</param>
        /// <returns>
        /// A <see cref="Promise{T}"/> that will resolve with <see langword="true"/> when space is available to write an item,
        /// or <see langword="false"/> when the channel is closed.
        /// </returns>
        /// <exception cref="ObjectDisposedException">The channel was disposed.</exception>
        /// <exception cref="NullReferenceException">This is a default value.</exception>
        public Promise<bool> WaitToWriteAsync(bool continueOnCapturedContext)
            => WaitToWriteAsync(CancelationToken.None, continueOnCapturedContext);

        /// <summary>
        /// Asynchronously waits for space to be available to write an item.
        /// </summary>
        /// <param name="cancelationToken">A <see cref="CancelationToken"/> used to cancel the wait operation.</param>
        /// <returns>
        /// A <see cref="Promise{T}"/> that will resolve with <see langword="true"/> when space is available to write an item,
        /// or <see langword="false"/> when the channel is closed.
        /// </returns>
        /// <exception cref="ObjectDisposedException">The channel was disposed.</exception>
        /// <exception cref="NullReferenceException">This is a default value.</exception>
        public Promise<bool> WaitToWriteAsync(CancelationToken cancelationToken)
            => WaitToWriteAsync(cancelationToken, true);

        /// <summary>
        /// Asynchronously waits for space to be available to write an item.
        /// </summary>
        /// <param name="cancelationToken">A <see cref="CancelationToken"/> used to cancel the wait operation.</param>
        /// <param name="continueOnCapturedContext">If <see langword="true"/> and space is not immediately available, the async continuation will be executed on the captured context.</param>
        /// <returns>
        /// A <see cref="Promise{T}"/> that will resolve with <see langword="true"/> when space is available to write an item,
        /// or <see langword="false"/> when the channel is closed.
        /// </returns>
        /// <exception cref="ObjectDisposedException">The channel was disposed.</exception>
        /// <exception cref="NullReferenceException">This is a default value.</exception>
        public Promise<bool> WaitToWriteAsync(CancelationToken cancelationToken, bool continueOnCapturedContext)
            => _channel._ref.WaitToWriteAsync(_channel._id, cancelationToken, continueOnCapturedContext);

        /// <summary>
        /// Attempts to write an item to the channel in a non-blocking manner.
        /// </summary>
        /// <param name="item">The value to write to the channel.</param>
        /// <returns>The result of the write operation.</returns>
        /// <exception cref="ObjectDisposedException">The channel was disposed.</exception>
        /// <exception cref="NullReferenceException">This is a default value.</exception>
        public ChannelWriteResult<T> TryWrite(T item)
            => _channel._ref.TryWrite(item, _channel._id);

        /// <summary>
        /// Asynchronously writes an item to the channel.
        /// </summary>
        /// <param name="item">The value to write to the channel.</param>
        /// <returns>A <see cref="Promise{T}"/> that yields the result of the write operation.</returns>
        /// <exception cref="ObjectDisposedException">The channel was disposed.</exception>
        /// <exception cref="NullReferenceException">This is a default value.</exception>
        public Promise<ChannelWriteResult<T>> WriteAsync(T item)
            => WriteAsync(item, CancelationToken.None, true);

        /// <summary>
        /// Asynchronously writes an item to the channel.
        /// </summary>
        /// <param name="item">The value to write to the channel.</param>
        /// <param name="continueOnCapturedContext">If <see langword="true"/> and space is not immediately available, the async continuation will be executed on the captured context.</param>
        /// <returns>A <see cref="Promise{T}"/> that yields the result of the write operation.</returns>
        /// <exception cref="ObjectDisposedException">The channel was disposed.</exception>
        /// <exception cref="NullReferenceException">This is a default value.</exception>
        public Promise<ChannelWriteResult<T>> WriteAsync(T item, bool continueOnCapturedContext)
            => WriteAsync(item, CancelationToken.None, continueOnCapturedContext);

        /// <summary>
        /// Asynchronously writes an item to the channel.
        /// </summary>
        /// <param name="item">The value to write to the channel.</param>
        /// <param name="cancelationToken">A <see cref="CancelationToken"/> used to cancel the write operation.</param>
        /// <returns>A <see cref="Promise{T}"/> that yields the result of the write operation.</returns>
        /// <exception cref="ObjectDisposedException">The channel was disposed.</exception>
        /// <exception cref="NullReferenceException">This is a default value.</exception>
        public Promise<ChannelWriteResult<T>> WriteAsync(T item, CancelationToken cancelationToken)
            => WriteAsync(item, cancelationToken, true);

        /// <summary>
        /// Asynchronously writes an item to the channel.
        /// </summary>
        /// <param name="item">The value to write to the channel.</param>
        /// <param name="cancelationToken">A <see cref="CancelationToken"/> used to cancel the write operation.</param>
        /// <param name="continueOnCapturedContext">If <see langword="true"/> and space is not immediately available, the async continuation will be executed on the captured context.</param>
        /// <returns>A <see cref="Promise{T}"/> that yields the result of the write operation.</returns>
        /// <exception cref="ObjectDisposedException">The channel was disposed.</exception>
        /// <exception cref="NullReferenceException">This is a default value.</exception>
        public Promise<ChannelWriteResult<T>> WriteAsync(T item, CancelationToken cancelationToken, bool continueOnCapturedContext)
            => _channel._ref.WriteAsync(item, _channel._id, cancelationToken, continueOnCapturedContext);

        /// <summary>
        /// Attempts to close the channel in a rejected state.
        /// </summary>
        /// <typeparam name="TReject">The type of the <paramref name="reason"/>.</typeparam>
        /// <param name="reason">The reason for the rejection.</param>
        /// <returns><see langword="true"/> if the channel was not already closed, <see langword="false"/> otherwise.</returns>
        /// <exception cref="ObjectDisposedException">The channel was disposed.</exception>
        /// <exception cref="NullReferenceException">This is a default value.</exception>
        public bool TryReject<TReject>(TReject reason)
        {
            var channel = _channel._ref;
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
        /// Attempts to close the channel in a canceled state.
        /// </summary>
        /// <returns><see langword="true"/> if the channel was not already closed, <see langword="false"/> otherwise.</returns>
        /// <exception cref="ObjectDisposedException">The channel was disposed.</exception>
        /// <exception cref="NullReferenceException">This is a default value.</exception>
        public bool TryCancel()
            => _channel._ref.TryCancel(_channel._id);

        /// <summary>
        /// Attempts to close the channel in a resolved state.
        /// </summary>
        /// <returns><see langword="true"/> if the channel was not already closed, <see langword="false"/> otherwise.</returns>
        /// <exception cref="ObjectDisposedException">The channel was disposed.</exception>
        /// <exception cref="NullReferenceException">This is a default value.</exception>
        public bool TryClose()
            => _channel._ref.TryClose(_channel._id);

        /// <summary>Returns a value indicating whether this value is equal to a specified <see cref="ChannelWriter{T}"/>.</summary>
        [MethodImpl(Internal.InlineOption)]
        public bool Equals(ChannelWriter<T> other)
            => this == other;

        /// <summary>Returns a value indicating whether this value is equal to a specified <see cref="object"/>.</summary>
        public override bool Equals(object obj)
            => obj is ChannelWriter<T> writer && Equals(writer);

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