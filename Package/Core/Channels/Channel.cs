﻿#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

#pragma warning disable IDE0090 // Use 'new(...)'
#pragma warning disable IDE0270 // Use coalesce expression

namespace Proto.Promises.Channels
{
    /// <summary>
    /// Provides a channel that supports reading and writing elements of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">Specifies the type of data that is channeled.</typeparam>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public readonly struct Channel<T> : IEquatable<Channel<T>>, IDisposable
    {
        internal readonly Internal.ChannelBase<T> _ref;
        internal readonly int _id;

        [MethodImpl(Internal.InlineOption)]
        private Channel(Internal.ChannelBase<T> channel)
        {
            _ref = channel;
            _id = channel.Id;
        }

        /// <summary>
        /// Creates a bounded channel subject to the provided options.
        /// </summary>
        /// <returns>The created channel.</returns>
        public static Channel<T> NewBounded(BoundedChannelOptions<T> options)
        {
            if (options.Capacity < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(options), "Capacity must be greater than zero.", Internal.GetFormattedStacktrace(1));
            }
            return new Channel<T>(Internal.BoundedChannel<T>.GetOrCreate(options.Capacity, options.FullMode));
        }

        /// <summary>
        /// Creates an unbounded channel.
        /// </summary>
        /// <returns>The created channel.</returns>
        public static Channel<T> NewUnbounded()
            => new Channel<T>(Internal.UnboundedChannel<T>.GetOrCreate());

        /// <summary>
        /// Gets the readable half of this channel.
        /// </summary>
        public ChannelReader<T> Reader
        {
            [MethodImpl(Internal.InlineOption)]
            get => new ChannelReader<T>(this);
        }

        /// <summary>
        /// Gets the writable half of this channel.
        /// </summary>
        public ChannelWriter<T> Writer
        {
            [MethodImpl(Internal.InlineOption)]
            get => new ChannelWriter<T>(this);
        }

        /// <summary>
        /// Releases all resources from this channel.
        /// If any items are remaining in the channel, they will be discarded.
        /// If any read or write operations are pending, they will be rejected with <see cref="ObjectDisposedException"/>.
        /// </summary>
        /// <exception cref="ObjectDisposedException">This was already disposed.</exception>
        /// <exception cref="NullReferenceException">This is a default value.</exception>
        public void Dispose()
            => _ref.Dispose(_id);

        /// <summary>Returns a value indicating whether this value is equal to a specified <see cref="Channel{T}"/>.</summary>
        [MethodImpl(Internal.InlineOption)]
        public bool Equals(Channel<T> other)
            => this == other;

        /// <summary>Returns a value indicating whether this value is equal to a specified <see cref="object"/>.</summary>
        public override bool Equals(object obj)
            => obj is Channel<T> channel && Equals(channel);

        /// <summary>Returns the hash code for this instance.</summary>
        [MethodImpl(Internal.InlineOption)]
        public override int GetHashCode()
            => HashCode.Combine(_ref, _id);

        /// <summary>Returns a value indicating whether two <see cref="Channel{T}"/> values are equal.</summary>
        [MethodImpl(Internal.InlineOption)]
        public static bool operator ==(Channel<T> lhs, Channel<T> rhs)
            => lhs._ref == rhs._ref
            & lhs._id == rhs._id;

        /// <summary>Returns a value indicating whether two <see cref="Channel{T}"/> values are not equal.</summary>
        [MethodImpl(Internal.InlineOption)]
        public static bool operator !=(Channel<T> lhs, Channel<T> rhs)
            => !(lhs == rhs);

        /// <summary>
        /// Implicit cast from a <see cref="Channel{T}"/> to its readable half.
        /// </summary>
        /// <param name="channel">The <see cref="Channel{T}"/> being cast.</param>
        /// <returns>The readable half.</returns>
        [MethodImpl(Internal.InlineOption)]
        public static implicit operator ChannelReader<T>(Channel<T> channel)
            => channel.Reader;

        /// <summary>
        /// Implicit cast from a <see cref="Channel{T}"/> to its writable half.
        /// </summary>
        /// <param name="channel">The <see cref="Channel{T}"/> being cast.</param>
        /// <returns>The writable half.</returns>
        [MethodImpl(Internal.InlineOption)]
        public static implicit operator ChannelWriter<T>(Channel<T> channel)
            => channel.Writer;
    }
}