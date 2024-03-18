#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using Proto.Promises.CompilerServices;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

#pragma warning disable IDE0090 // Use 'new(...)'

namespace Proto.Promises.Linq
{
    /// <summary>
    /// Exposes an enumerator that provides asynchronous iteration over ordered values of a specified type.
    /// An instance of this type may only be consumed once.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the ordered async-enumerable sequence.</typeparam>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public readonly partial struct OrderedAsyncEnumerable<T>
#if UNITY_2021_2_OR_NEWER || !UNITY_2018_3_OR_NEWER
        : IAsyncEnumerable<T>
#endif
    {
        internal readonly Internal.OrderedAsyncEnumerableBase<T> _target;
        internal readonly int _id;

        /// <summary>
        /// Gets whether this instance is valid for enumeration. Once enumeration has begun, this will return false.
        /// </summary>
        public bool CanBeEnumerated
            => _target?.GetCanBeEnumerated(_id) == true;

        [MethodImpl(Internal.InlineOption)]
        internal OrderedAsyncEnumerable(Internal.OrderedAsyncEnumerableBase<T> target, int id)
        {
            _target = target;
            _id = id;
        }

        /// <summary>
        /// Returns an enumerator that iterates asynchronously through the async-enumerable sequence.
        /// </summary>
        [MethodImpl(Internal.InlineOption)]
        public AsyncEnumerator<T> GetAsyncEnumerator(CancelationToken cancelationToken)
            => _target.GetAsyncEnumerator(_id, cancelationToken);

        /// <summary>
        /// Returns an enumerator that iterates asynchronously through the async-enumerable sequence.
        /// </summary>
        [MethodImpl(Internal.InlineOption)]
        public AsyncEnumerator<T> GetAsyncEnumerator() => GetAsyncEnumerator(CancelationToken.None);

#if UNITY_2021_2_OR_NEWER || !UNITY_2018_3_OR_NEWER
        IAsyncEnumerator<T> IAsyncEnumerable<T>.GetAsyncEnumerator(CancellationToken cancellationToken) => GetAsyncEnumerator(cancellationToken.ToCancelationToken());
#endif

        /// <summary>
        /// Sets the <see cref="CancelationToken"/> to be passed to <see cref="OrderedAsyncEnumerable{T}.GetAsyncEnumerator(CancelationToken)"/> when iterating.
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

        /// <summary>
        /// Returns this ordered async-enumerable as a normal <see cref="AsyncEnumerable{T}"/>.
        /// </summary>
        [MethodImpl(Internal.InlineOption)]
        public AsyncEnumerable<T> AsAsyncEnumerable()
            => new AsyncEnumerable<T>(_target, _id);

        /// <summary>
        /// Casts the ordered async-enumerable to a normal <see cref="AsyncEnumerable{T}"/>.
        /// </summary>
        /// <param name="oae">The ordered async-enumerable to be casted.</param>
        [MethodImpl(Internal.InlineOption)]
        public static implicit operator AsyncEnumerable<T>(OrderedAsyncEnumerable<T> oae)
            => oae.AsAsyncEnumerable();
    }
}