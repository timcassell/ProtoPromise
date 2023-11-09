using Proto.Promises.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Proto.Promises.Async.CompilerServices
{
#if CSHARP_7_3_OR_NEWER
    /// <summary>
    /// Provides an awaitable async enumerable that enables cancelable iteration and configured awaits.
    /// </summary>
    /// <typeparam name="T">The type of values to enumerate.</typeparam>
    public readonly struct ConfiguredAsyncEnumerable<T>
    {
        private readonly AsyncEnumerable<T> _enumerable;
        private readonly CancelationToken _cancelationToken;
        private readonly SynchronizationContext _synchronizationContext;
        private readonly Internal.SynchronizationOption _synchronizationOption;
        private readonly bool _forceAsync;

        internal ConfiguredAsyncEnumerable(AsyncEnumerable<T> enumerable,
            CancelationToken cancelationToken,
            Internal.SynchronizationOption synchronizationOption,
            SynchronizationContext synchronizationContext,
            bool forceAsync)
        {
            _enumerable = enumerable;
            _cancelationToken = cancelationToken;
            _synchronizationOption = synchronizationOption;
            _synchronizationContext = synchronizationContext;
            _forceAsync = forceAsync;
        }

        /// <summary>
        /// Sets the <see cref="CancelationToken"/> to be passed to <see cref="AsyncEnumerable{T}.GetAsyncEnumerator(CancelationToken)"/> when iterating.
        /// </summary>
        /// <param name="cancelationToken">The cancelation token to use.</param>
        /// <returns>The configured enumerable.</returns>
        public ConfiguredAsyncEnumerable<T> WithCancelation(CancelationToken cancelationToken)
            => new ConfiguredAsyncEnumerable<T>(_enumerable, cancelationToken, _synchronizationOption, _synchronizationContext, _forceAsync);

        /// <summary>
        /// Configures how awaits on the promises returned from an async iteration will be performed.
        /// </summary>
        /// <param name="synchronizationOption">On which context the continuations will be executed.</param>
        /// <param name="forceAsync">If true, forces the continuations to be invoked asynchronously. If <paramref name="synchronizationOption"/> is <see cref="SynchronizationOption.Synchronous"/>, this value will be ignored.</param>
        /// <returns>The configured enumerable.</returns>
        public ConfiguredAsyncEnumerable<T> ConfigureAwait(SynchronizationOption synchronizationOption, bool forceAsync = false)
            => new ConfiguredAsyncEnumerable<T>(_enumerable, _cancelationToken, (Internal.SynchronizationOption) synchronizationOption, null, forceAsync);

        /// <summary>
        /// Configures how awaits on the promises returned from an async iteration will be performed.
        /// </summary>
        /// <param name="synchronizationContext">The context on which the continuations will be executed.</param>
        /// <param name="forceAsync">If true, forces the continuations to be invoked asynchronously.</param>
        /// <returns>The configured enumerable.</returns>
        public ConfiguredAsyncEnumerable<T> ConfigureAwait(SynchronizationContext synchronizationContext, bool forceAsync = false)
            => new ConfiguredAsyncEnumerable<T>(_enumerable, _cancelationToken, Internal.SynchronizationOption.Explicit, synchronizationContext, forceAsync);

        /// <summary>
        /// Returns an enumerator that iterates asynchronously through collections that enables cancelable iteration and configured awaits.
        /// </summary>
        /// <returns>An enumerator for the <see cref="ConfiguredAsyncEnumerable{T}"/>.</returns>
        public Enumerator GetAsyncEnumerator()
            => new Enumerator(_enumerable.GetAsyncEnumerator(_cancelationToken), _synchronizationContext, _synchronizationOption, _forceAsync);

        /// <summary>
        /// Provides an awaitable async enumerator that enables cancelable iteration and configured awaits.
        /// </summary>
        public readonly struct Enumerator
        {
            private readonly AsyncEnumerator<T> _enumerator;
            private readonly SynchronizationContext _synchronizationContext;
            private readonly Internal.SynchronizationOption _synchronizationOption;
            private readonly bool _forceAsync;

            internal Enumerator(AsyncEnumerator<T> enumerator, SynchronizationContext synchronizationContext, Internal.SynchronizationOption synchronizationOption, bool forceAsync)
            {
                _enumerator = enumerator;
                _synchronizationContext = synchronizationContext;
                _synchronizationOption = synchronizationOption;
                _forceAsync = forceAsync;
            }

            /// <summary>
            /// Gets the element in the collection at the current position of the enumerator.
            /// </summary>
            public T Current
            {
                [MethodImpl(Internal.InlineOption)]
                get { return _enumerator.Current; }
            }

            /// <summary>
            /// Advances the enumerator asynchronously to the next element of the collection.
            /// </summary>
            [MethodImpl(Internal.InlineOption)]
            public Promise<bool> MoveNextAsync()
                => Internal.PromiseRefBase.CallbackHelperVoid.WaitAsync(_enumerator.MoveNextAsync(), _synchronizationOption, _synchronizationContext, _forceAsync, CancelationToken.None);

            /// <summary>
            /// Asynchronously releases resources used by the <see cref="AsyncEnumerator{T}"/>.
            /// </summary>
            [MethodImpl(Internal.InlineOption)]
            public Promise DisposeAsync()
                => Internal.PromiseRefBase.CallbackHelperVoid.WaitAsync(_enumerator.DisposeAsync(), _synchronizationOption, _synchronizationContext, _forceAsync, CancelationToken.None);
        }
    }
#endif
}