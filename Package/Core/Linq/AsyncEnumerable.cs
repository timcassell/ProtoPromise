using Proto.Promises.Async.CompilerServices;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Proto.Promises.Linq
{
#if NET47_OR_GREATER || NETSTANDARD2_1_OR_GREATER || NETCOREAPP || UNITY_2021_2_OR_NEWER
    /// <summary>
    /// Provides helper functions to create <see cref="AsyncEnumerable{T}"/> async streams.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public static class AsyncEnumerable
    {
        // We use AsyncEnumerableMethod instead of Promise so it can specially handle early-exits (`break` keyword).

        /// <summary>
        /// Create a new <see cref="AsyncEnumerable{T}"/> async stream from the specified <paramref name="asyncIterator"/> function.
        /// </summary>
        public static AsyncEnumerable<T> Create<T>(Func<AsyncStreamWriter<T>, CancelationToken, AsyncEnumerableMethod> asyncIterator)
        {
            var enumerable = Internal.AsyncEnumerableImpl<T, Internal.AsyncIterator<T>>.GetOrCreate(new Internal.AsyncIterator<T>(asyncIterator));
            return new AsyncEnumerable<T>(enumerable);
        }

        /// <summary>
        /// Create a new <see cref="AsyncEnumerable{T}"/> async stream from the specified <paramref name="captureValue"/> and <paramref name="asyncIterator"/> function.
        /// </summary>
        public static AsyncEnumerable<T> Create<T, TCapture>(TCapture captureValue, Func<TCapture, AsyncStreamWriter<T>, CancelationToken, AsyncEnumerableMethod> asyncIterator)
        {
            var enumerable = Internal.AsyncEnumerableImpl<T, Internal.AsyncIterator<T, TCapture>>.GetOrCreate(new Internal.AsyncIterator<T, TCapture>(captureValue, asyncIterator));
            return new AsyncEnumerable<T>(enumerable);
        }
    }

    /// <summary>
    /// Exposes an enumerator that provides asynchronous iteration over values of a specified type.
    /// This type may only be consumed once.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public readonly struct AsyncEnumerable<T> : IAsyncEnumerable<T>
    {
        private readonly Internal.PromiseRefBase.AsyncEnumerableBase<T> _target;
        private readonly int _id;

        /// <summary>
        /// Gets whether this instance is valid for enumeration. Once enumeration has begun, this will return false.
        /// </summary>
        public bool IsValid
        {
            get
            {
                var target = _target;
                return target != null && target.EnumerableId == _id;
            }
        }

        [MethodImpl(Internal.InlineOption)]
        internal AsyncEnumerable(Internal.PromiseRefBase.AsyncEnumerableBase<T> target)
        {
            _target = target;
            _id = _target.EnumerableId;
        }

        /// <summary>
        /// Returns an enumerator that iterates asynchronously through the collection.
        /// </summary>
        [MethodImpl(Internal.InlineOption)]
        public AsyncEnumerator<T> GetAsyncEnumerator(CancelationToken cancelationToken)
            => _target.GetAsyncEnumerator(_id, cancelationToken);

        /// <summary>
        /// Returns an enumerator that iterates asynchronously through the collection.
        /// </summary>
        [MethodImpl(Internal.InlineOption)]
        public AsyncEnumerator<T> GetAsyncEnumerator() => GetAsyncEnumerator(CancelationToken.None);

        // TODO: add ConfigureAwait and WithCancellation methods to avoid IAsyncEnumerable extensions.

        IAsyncEnumerator<T> IAsyncEnumerable<T>.GetAsyncEnumerator(CancellationToken cancellationToken) => GetAsyncEnumerator(cancellationToken.ToCancelationToken());
    }

    /// <summary>
    /// Supports a simple asynchronous iteration over a generic collection.
    /// This type may only be consumed once.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public readonly struct AsyncEnumerator<T> : IAsyncEnumerator<T>
    {
        private readonly Internal.PromiseRefBase.AsyncEnumerableBase<T> _target;
        private readonly int _id;

        [MethodImpl(Internal.InlineOption)]
        internal AsyncEnumerator(Internal.PromiseRefBase.AsyncEnumerableBase<T> target, int id)
        {
            _target = target;
            _id = id;
        }

        /// <summary>
        /// Gets the element in the collection at the current position of the enumerator.
        /// </summary>
        public T Current
        {
            [MethodImpl(Internal.InlineOption)]
            get { return _target.GetCurrent(_id); }
        }

        /// <summary>
        /// Advances the enumerator asynchronously to the next element of the collection.
        /// </summary>
        public Promise<bool> MoveNextAsync()
            => _target.MoveNextAsync(_id);

        /// <summary>
        /// Asynchronously releases resources used by this enumerator.
        /// </summary>
        public Promise DisposeAsync()
            => _target.DisposeAsync(_id);

        System.Threading.Tasks.ValueTask<bool> IAsyncEnumerator<T>.MoveNextAsync() => MoveNextAsync();
        System.Threading.Tasks.ValueTask IAsyncDisposable.DisposeAsync() => DisposeAsync();
    }
#endif
}