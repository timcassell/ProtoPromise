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
    public readonly struct Channel<T> : IEquatable<Channel<T>>
    {
        private readonly Internal.ChannelBase<T> _ref;
        internal readonly int _id;

        private Channel(Internal.ChannelBase<T> channel, int id)
        {
            _ref = channel;
            _id = id;
        }

        /// <summary>
        /// Creates a bounded channel subject to the provided options, usable by any number of readers and writers concurrently, initialized with 1 reader and 1 writer.
        /// </summary>
        /// <returns>The created channel.</returns>
        public static Channel<T> NewBounded(BoundedChannelOptions<T> options)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Creates an unbounded channel usable by any number of readers and writers concurrently, initialized with 1 reader and 1 writer.
        /// </summary>
        /// <returns>The created channel.</returns>
        public static Channel<T> NewUnbounded()
        {
            throw new NotImplementedException();
        }

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
        /// Gets the current number of items available from this channel.
        /// </summary>
        public int Count
        {
            get => ValidateAndGetRef().GetCount(_id);
        }

        /// <summary>Returns a value indicating whether this value is equal to a specified <see cref="Channel{T}"/>.</summary>
        [MethodImpl(Internal.InlineOption)]
        public bool Equals(Channel<T> other)
            => this == other;

        /// <summary>Returns a value indicating whether this value is equal to a specified <see cref="object"/>.</summary>
        public override bool Equals(object obj)
            => obj is Channel<T> other && Equals(other);

        /// <summary>Returns the hash code for this instance.</summary>
        [MethodImpl(Internal.InlineOption)]
        public override int GetHashCode()
            => Internal.BuildHashCode(_ref, _id, 0);

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

        [MethodImpl(Internal.InlineOption)]
        internal Internal.ChannelBase<T> ValidateAndGetRef()
        {
            var r = _ref;
            if (r == null)
            {
                throw new InvalidOperationException("The channel is not valid.", Internal.GetFormattedStacktrace(2));
            }
            return r;
        }
    }
}