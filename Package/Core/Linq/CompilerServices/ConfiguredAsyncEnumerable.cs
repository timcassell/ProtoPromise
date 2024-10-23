using Proto.Promises.Linq;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;

#pragma warning disable IDE0090 // Use 'new(...)'

namespace Proto.Promises.CompilerServices
{
    /// <summary>
    /// Provides an awaitable async enumerable that enables cancelable iteration and configured awaits.
    /// </summary>
    /// <typeparam name="T">The type of values to enumerate.</typeparam>
    public readonly struct ConfiguredAsyncEnumerable<T>
    {
        private readonly AsyncEnumerable<T> _enumerable;
        private readonly CancelationToken _cancelationToken;
        private readonly ContinuationOptions _continuationOptions;

        /// <summary>
        /// Gets the <see cref="ContinuationOptions"/> that this was configured with.
        /// </summary>
        public ContinuationOptions ContinuationOptions
        {
            [MethodImpl(Internal.InlineOption)]
            get => _continuationOptions;
        }

        /// <summary>
        /// Gets the <see cref="CancelationToken"/> that this was configured with.
        /// </summary>
        public CancelationToken CancelationToken
        {
            [MethodImpl(Internal.InlineOption)]
            get => _cancelationToken;
        }

        internal ConfiguredAsyncEnumerable(AsyncEnumerable<T> enumerable,
            CancelationToken cancelationToken,
            ContinuationOptions continuationOptions)
        {
            _enumerable = enumerable;
            _cancelationToken = cancelationToken;
            _continuationOptions = continuationOptions;
        }

        /// <summary>
        /// Sets the <see cref="CancelationToken"/> to be passed to <see cref="AsyncEnumerable{T}.GetAsyncEnumerator(CancelationToken)"/> when iterating.
        /// </summary>
        /// <param name="cancelationToken">The cancelation token to use.</param>
        /// <returns>The configured enumerable.</returns>
        public ConfiguredAsyncEnumerable<T> WithCancelation(CancelationToken cancelationToken)
            => new ConfiguredAsyncEnumerable<T>(_enumerable, cancelationToken, _continuationOptions);

        /// <summary>
        /// Configures how awaits on the promises returned from an async iteration will be performed.
        /// </summary>
        /// <param name="synchronizationOption">On which context the continuations will be executed.</param>
        /// <param name="forceAsync">If true, forces the continuations to be invoked asynchronously. If <paramref name="synchronizationOption"/> is <see cref="SynchronizationOption.Synchronous"/>, this value will be ignored.</param>
        /// <returns>The configured enumerable.</returns>
        [EditorBrowsable(EditorBrowsableState.Never)] // This is not deprecated, but hidden to encourage users to use the newer overload accepting ContinuationOptions.
        public ConfiguredAsyncEnumerable<T> ConfigureAwait(SynchronizationOption synchronizationOption, bool forceAsync = false)
            => ConfigureAwait(new ContinuationOptions(synchronizationOption, forceAsync));

        /// <summary>
        /// Configures how awaits on the promises returned from an async iteration will be performed.
        /// </summary>
        /// <param name="synchronizationContext">The context on which the continuations will be executed. If null, <see cref="ThreadPool.QueueUserWorkItem(WaitCallback, object)"/> will be used.</param>
        /// <param name="forceAsync">If true, forces the continuations to be invoked asynchronously.</param>
        /// <returns>The configured enumerable.</returns>
        [EditorBrowsable(EditorBrowsableState.Never)] // This is not deprecated, but hidden to encourage users to use the newer overload accepting ContinuationOptions.
        public ConfiguredAsyncEnumerable<T> ConfigureAwait(SynchronizationContext synchronizationContext, bool forceAsync = false)
            => ConfigureAwait(new ContinuationOptions(synchronizationContext, forceAsync));

        /// <summary>
        /// Configures how awaits on the promises returned from an async iteration will be performed.
        /// </summary>
        /// <param name="continuationOptions">The options used to configure the execution behavior of async continuations.</param>
        /// <returns>The configured enumerable.</returns>
        public ConfiguredAsyncEnumerable<T> ConfigureAwait(ContinuationOptions continuationOptions)
            => new ConfiguredAsyncEnumerable<T>(_enumerable, _cancelationToken, continuationOptions);

        /// <summary>
        /// Returns an enumerator that iterates asynchronously through collections that enables cancelable iteration and configured awaits.
        /// </summary>
        /// <returns>An enumerator for the <see cref="ConfiguredAsyncEnumerable{T}"/>.</returns>
        public Enumerator GetAsyncEnumerator()
            => new Enumerator(_enumerable.GetAsyncEnumerator(_cancelationToken), _continuationOptions);

        /// <summary>
        /// Provides an awaitable async enumerator that enables cancelable iteration and configured awaits.
        /// </summary>
        public readonly struct Enumerator
        {
            internal readonly AsyncEnumerator<T> _enumerator;
            private readonly ContinuationOptions _continuationOptions;

            /// <summary>
            /// Gets the <see cref="ContinuationOptions"/> that this was configured with.
            /// </summary>
            public ContinuationOptions ContinuationOptions
            {
                [MethodImpl(Internal.InlineOption)]
                get => _continuationOptions;
            }

            internal Enumerator(AsyncEnumerator<T> enumerator, ContinuationOptions continuationOptions)
            {
                _enumerator = enumerator;
                _continuationOptions = continuationOptions;
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
                => _enumerator.MoveNextAsync().ConfigureContinuation(_continuationOptions);

            /// <summary>
            /// Asynchronously releases resources used by the <see cref="AsyncEnumerator{T}"/>.
            /// </summary>
            [MethodImpl(Internal.InlineOption)]
            public Promise DisposeAsync()
                => _enumerator.DisposeAsync().ConfigureContinuation(_continuationOptions);
        }
    }
}