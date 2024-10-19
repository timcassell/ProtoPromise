#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using Proto.Promises.CompilerServices;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

#pragma warning disable IDE0090 // Use 'new(...)'

namespace Proto.Promises.Linq
{
    /// <summary>
    /// Exposes an enumerator that provides asynchronous iteration over values of a specified type.
    /// An instance of this type may only be consumed once.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the async-enumerable sequence.</typeparam>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public readonly partial struct AsyncEnumerable<T>
#if UNITY_2021_2_OR_NEWER || !UNITY_2018_3_OR_NEWER
        : IAsyncEnumerable<T>
#endif
    {
        internal readonly Internal.IAsyncEnumerable<T> _target;
        internal readonly int _id;

        /// <summary>
        /// Gets whether this instance is valid for enumeration. Once enumeration has begun, this will return false.
        /// </summary>
        public bool CanBeEnumerated
            => _target?.GetCanBeEnumerated(_id) == true;

        [MethodImpl(Internal.InlineOption)]
        internal AsyncEnumerable(Internal.PromiseRefBase.AsyncEnumerableBase<T> target) : this(target, target.EnumerableId) { }

        [MethodImpl(Internal.InlineOption)]
        internal AsyncEnumerable(Internal.IAsyncEnumerable<T> target, int id)
        {
            _target = target;
            _id = id;
        }

        /// <summary>
        /// Returns an enumerator that iterates asynchronously through the async-enumerable sequence.
        /// </summary>
        [MethodImpl(Internal.InlineOption)]
        public AsyncEnumerator<T> GetAsyncEnumerator(CancelationToken cancelationToken)
        {
            var target = _target;
            if (target == null)
            {
                Internal.ThrowInvalidAsyncEnumerable(1);
            }
            return target.GetAsyncEnumerator(_id, cancelationToken);
        }

        /// <summary>
        /// Returns an enumerator that iterates asynchronously through the async-enumerable sequence.
        /// </summary>
        [MethodImpl(Internal.InlineOption)]
        public AsyncEnumerator<T> GetAsyncEnumerator() => GetAsyncEnumerator(CancelationToken.None);

#if UNITY_2021_2_OR_NEWER || !UNITY_2018_3_OR_NEWER
        IAsyncEnumerator<T> IAsyncEnumerable<T>.GetAsyncEnumerator(CancellationToken cancellationToken) => GetAsyncEnumerator(cancellationToken.ToCancelationToken());
#endif

        /// <summary>
        /// Sets the <see cref="CancelationToken"/> to be passed to <see cref="AsyncEnumerable{T}.GetAsyncEnumerator(CancelationToken)"/> when iterating.
        /// </summary>
        /// <param name="cancelationToken">The cancelation token to use.</param>
        /// <returns>The configured enumerable.</returns>
        public ConfiguredAsyncEnumerable<T> WithCancelation(CancelationToken cancelationToken)
            => new ConfiguredAsyncEnumerable<T>(this, cancelationToken, ContinuationOptions.Synchronous);

        /// <summary>
        /// Configures how awaits on the promises returned from an async iteration will be performed.
        /// </summary>
        /// <param name="synchronizationOption">On which context the continuations will be executed.</param>
        /// <param name="forceAsync">If true, forces the continuations to be invoked asynchronously. If <paramref name="synchronizationOption"/> is <see cref="SynchronizationOption.Synchronous"/>, this value will be ignored.</param>
        /// <returns>The configured enumerable.</returns>
        public ConfiguredAsyncEnumerable<T> ConfigureAwait(SynchronizationOption synchronizationOption, bool forceAsync = false)
            => ConfigureAwait(new ContinuationOptions(synchronizationOption, forceAsync));

        /// <summary>
        /// Configures how awaits on the promises returned from an async iteration will be performed.
        /// </summary>
        /// <param name="synchronizationContext">The context on which the continuations will be executed. If null, <see cref="ThreadPool.QueueUserWorkItem(WaitCallback, object)"/> will be used.</param>
        /// <param name="forceAsync">If true, forces the continuations to be invoked asynchronously.</param>
        /// <returns>The configured enumerable.</returns>
        public ConfiguredAsyncEnumerable<T> ConfigureAwait(SynchronizationContext synchronizationContext, bool forceAsync = false)
            => ConfigureAwait(new ContinuationOptions(synchronizationContext, forceAsync));

        /// <summary>
        /// Configures how awaits on the promises returned from an async iteration will be performed.
        /// </summary>
        /// <param name="continuationOptions">The options used to configure the execution behavior of async continuations.</param>
        /// <returns>The configured enumerable.</returns>
        public ConfiguredAsyncEnumerable<T> ConfigureAwait(ContinuationOptions continuationOptions)
            => new ConfiguredAsyncEnumerable<T>(this, CancelationToken.None, continuationOptions);

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
    /// Supports a simple asynchronous iteration over an async-enumerable sequence.
    /// An instance of this type may only be consumed once.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the async-enumerable sequence.</typeparam>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public readonly struct AsyncEnumerator<T>
#if UNITY_2021_2_OR_NEWER || !UNITY_2018_3_OR_NEWER
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
        /// Gets the element in the async-enumerable sequence at the current position of the enumerator.
        /// </summary>
        public T Current
        {
            [MethodImpl(Internal.InlineOption)]
            get { return _target.GetCurrent(_id); }
        }

        /// <summary>
        /// Advances the enumerator asynchronously to the next element of the async-enumerable sequence.
        /// </summary>
        public Promise<bool> MoveNextAsync()
            => _target.MoveNextAsync(_id);

        /// <summary>
        /// Asynchronously releases resources used by this enumerator.
        /// </summary>
        public Promise DisposeAsync()
            => _target.DisposeAsync(_id);

#if UNITY_2021_2_OR_NEWER || !UNITY_2018_3_OR_NEWER
        System.Threading.Tasks.ValueTask<bool> IAsyncEnumerator<T>.MoveNextAsync() => MoveNextAsync();
        System.Threading.Tasks.ValueTask IAsyncDisposable.DisposeAsync() => DisposeAsync();
#endif
    }
}