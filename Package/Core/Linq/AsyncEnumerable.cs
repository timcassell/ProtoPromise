#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using Proto.Promises.Async.CompilerServices;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

#pragma warning disable IDE0090 // Use 'new(...)'

namespace Proto.Promises.Linq
{
#if CSHARP_7_3_OR_NEWER // We only expose AsyncEnumerable where custom async method builders are supported.
    /// <summary>
    /// Exposes an enumerator that provides asynchronous iteration over values of a specified type.
    /// An instance of this type may only be consumed once.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public readonly partial struct AsyncEnumerable<T>
#if NET47_OR_GREATER || NETSTANDARD2_0_OR_GREATER || NETCOREAPP || UNITY_2021_2_OR_NEWER
        : IAsyncEnumerable<T>
#endif
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

#if NET47_OR_GREATER || NETSTANDARD2_0_OR_GREATER || NETCOREAPP || UNITY_2021_2_OR_NEWER
        IAsyncEnumerator<T> IAsyncEnumerable<T>.GetAsyncEnumerator(CancellationToken cancellationToken) => GetAsyncEnumerator(cancellationToken.ToCancelationToken());
#endif

        /// <summary>
        /// Sets the <see cref="CancelationToken"/> to be passed to <see cref="AsyncEnumerable{T}.GetAsyncEnumerator(CancelationToken)"/> when iterating.
        /// </summary>
        /// <param name="cancelationToken">The cancelation token to use.</param>
        /// <returns>The configured enumerable.</returns>
        public ConfiguredAsyncEnumerable<T> WithCancelation(CancelationToken cancelationToken)
            => new ConfiguredAsyncEnumerable<T>(this, cancelationToken, Internal.SynchronizationOption.Synchronous, null, false);

        /// <summary>
        /// Configures how awaits on the promises returned from an async iteration will be performed.
        /// </summary>
        /// <param name="synchronizationOption">On which context the continuations will be executed.</param>
        /// <param name="forceAsync">If true, forces the continuations to be invoked asynchronously. If <paramref name="synchronizationOption"/> is <see cref="SynchronizationOption.Synchronous"/>, this value will be ignored.</param>
        /// <returns>The configured enumerable.</returns>
        public ConfiguredAsyncEnumerable<T> ConfigureAwait(SynchronizationOption synchronizationOption, bool forceAsync = false)
            => new ConfiguredAsyncEnumerable<T>(this, CancelationToken.None, (Internal.SynchronizationOption) synchronizationOption, null, forceAsync);

        /// <summary>
        /// Configures how awaits on the promises returned from an async iteration will be performed.
        /// </summary>
        /// <param name="synchronizationContext">The context on which the continuations will be executed. If null, <see cref="ThreadPool.QueueUserWorkItem(WaitCallback, object)"/> will be used.</param>
        /// <param name="forceAsync">If true, forces the continuations to be invoked asynchronously.</param>
        /// <returns>The configured enumerable.</returns>
        public ConfiguredAsyncEnumerable<T> ConfigureAwait(SynchronizationContext synchronizationContext, bool forceAsync = false)
            => new ConfiguredAsyncEnumerable<T>(this, CancelationToken.None, Internal.SynchronizationOption.Explicit, synchronizationContext, forceAsync);

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        // These methods exist to hide the built-in IAsyncEnumerable extension methods, and use promise-optimized implementations instead if they are used.
        [EditorBrowsable(EditorBrowsableState.Never)]
        public ConfiguredAsyncEnumerable<T> WithCancellation(CancellationToken cancellationToken)
            => WithCancelation(cancellationToken.ToCancelationToken());

        [EditorBrowsable(EditorBrowsableState.Never)]
        public ConfiguredAsyncEnumerable<T> ConfigureAwait(bool continueOnCapturedContext)
            => continueOnCapturedContext ? ConfigureAwait(SynchronizationContext.Current) : ConfigureAwait(SynchronizationOption.Synchronous);
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    }

    /// <summary>
    /// Supports a simple asynchronous iteration over a generic collection.
    /// An instance of this type may only be consumed once.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public readonly struct AsyncEnumerator<T>
#if NET47_OR_GREATER || NETSTANDARD2_0_OR_GREATER || NETCOREAPP || UNITY_2021_2_OR_NEWER
        : IAsyncEnumerator<T>
#endif
    {
        internal readonly Internal.PromiseRefBase.AsyncEnumerableBase<T> _target;
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

#if NET47_OR_GREATER || NETSTANDARD2_0_OR_GREATER || NETCOREAPP || UNITY_2021_2_OR_NEWER
        System.Threading.Tasks.ValueTask<bool> IAsyncEnumerator<T>.MoveNextAsync() => MoveNextAsync();
        System.Threading.Tasks.ValueTask IAsyncDisposable.DisposeAsync() => DisposeAsync();
#endif
    }
#endif // CSHARP_7_3_OR_NEWER
}