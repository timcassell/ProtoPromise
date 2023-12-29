using Proto.Promises.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

#pragma warning disable IDE0074 // Use compound assignment
#pragma warning disable IDE0090 // Use 'new(...)'

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
        /// <param name="synchronizationContext">The context on which the continuations will be executed. If null, <see cref="ThreadPool.QueueUserWorkItem(WaitCallback, object)"/> will be used.</param>
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
            internal readonly AsyncEnumerator<T> _enumerator;
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
            {
                // Implementation is similar to promise.WaitAsync, but we aren't using the cancelation token and we don't need to get a Duplicate if the option is synchronous.
                var moveNextPromise = _enumerator.MoveNextAsync();
                var synchronizationContext = _synchronizationContext;
                switch (_synchronizationOption)
                {
                    case Internal.SynchronizationOption.Synchronous:
                    {
                        return moveNextPromise;
                    }
                    case Internal.SynchronizationOption.Foreground:
                    {
                        synchronizationContext = Promise.Config.ForegroundContext;
                        if (synchronizationContext == null)
                        {
                            throw new InvalidOperationException(
                                "SynchronizationOption.Foreground was provided, but Promise.Config.ForegroundContext was null. " +
                                "You should set Promise.Config.ForegroundContext at the start of your application (which may be as simple as 'Promise.Config.ForegroundContext = SynchronizationContext.Current;').",
                                Internal.GetFormattedStacktrace(2));
                        }
                        break;
                    }
                    case Internal.SynchronizationOption.Background:
                    {
                        synchronizationContext = Promise.Config.BackgroundContext;
                        goto default;
                    }
                    default: // SynchronizationOption.Explicit
                    {
                        if (synchronizationContext == null)
                        {
                            synchronizationContext = Internal.BackgroundSynchronizationContextSentinel.s_instance;
                        }
                        break;
                    }
                }

                Internal.PromiseRefBase.PromiseConfigured<bool> promise;
                if (moveNextPromise._ref == null)
                {
                    promise = Internal.PromiseRefBase.PromiseConfigured<bool>.GetOrCreateFromResolved(synchronizationContext, moveNextPromise._result, moveNextPromise.Depth, _forceAsync);
                }
                else
                {
                    promise = Internal.PromiseRefBase.PromiseConfigured<bool>.GetOrCreate(synchronizationContext, moveNextPromise.Depth, _forceAsync);
                    moveNextPromise._ref.HookupNewPromise(moveNextPromise._id, promise);
                }
                return new Promise<bool>(promise, promise.Id, moveNextPromise.Depth, moveNextPromise._result);
            }

            /// <summary>
            /// Asynchronously releases resources used by the <see cref="AsyncEnumerator{T}"/>.
            /// </summary>
            [MethodImpl(Internal.InlineOption)]
            public Promise DisposeAsync()
            {
                // Implementation is similar to promise.WaitAsync, but we aren't using the cancelation token and we don't need to get a Duplicate if the option is synchronous.
                var moveNextPromise = _enumerator.DisposeAsync();
                var synchronizationContext = _synchronizationContext;
                switch (_synchronizationOption)
                {
                    case Internal.SynchronizationOption.Synchronous:
                    {
                        return moveNextPromise;
                    }
                    case Internal.SynchronizationOption.Foreground:
                    {
                        synchronizationContext = Promise.Config.ForegroundContext;
                        if (synchronizationContext == null)
                        {
                            throw new InvalidOperationException(
                                "SynchronizationOption.Foreground was provided, but Promise.Config.ForegroundContext was null. " +
                                "You should set Promise.Config.ForegroundContext at the start of your application (which may be as simple as 'Promise.Config.ForegroundContext = SynchronizationContext.Current;').",
                                Internal.GetFormattedStacktrace(2));
                        }
                        break;
                    }
                    case Internal.SynchronizationOption.Background:
                    {
                        synchronizationContext = Promise.Config.BackgroundContext;
                        goto default;
                    }
                    default: // SynchronizationOption.Explicit
                    {
                        if (synchronizationContext == null)
                        {
                            synchronizationContext = Internal.BackgroundSynchronizationContextSentinel.s_instance;
                        }
                        break;
                    }
                }

                Internal.PromiseRefBase.PromiseConfigured<Internal.VoidResult> promise;
                if (moveNextPromise._ref == null)
                {
                    promise = Internal.PromiseRefBase.PromiseConfigured<Internal.VoidResult>.GetOrCreateFromResolved(synchronizationContext, default, moveNextPromise.Depth, _forceAsync);
                }
                else
                {
                    promise = Internal.PromiseRefBase.PromiseConfigured<Internal.VoidResult>.GetOrCreate(synchronizationContext, moveNextPromise.Depth, _forceAsync);
                    moveNextPromise._ref.HookupNewPromise(moveNextPromise._id, promise);
                }
                return new Promise(promise, promise.Id, moveNextPromise.Depth);
            }

            internal SwitchToConfiguredContextAwaiter SwitchToContext()
            {
                SynchronizationContext synchronizationContext;
                switch (_synchronizationOption)
                {
                    case Internal.SynchronizationOption.Synchronous:
                    {
                        return default;
                    }
                    case Internal.SynchronizationOption.Foreground:
                    {
                        synchronizationContext = Promise.Config.ForegroundContext;
                        if (synchronizationContext == null)
                        {
                            throw new InvalidOperationException(
                                "SynchronizationOption.Foreground was provided, but Promise.Config.ForegroundContext was null. " +
                                "You should set Promise.Config.ForegroundContext at the start of your application (which may be as simple as 'Promise.Config.ForegroundContext = SynchronizationContext.Current;').",
                                Internal.GetFormattedStacktrace(2));
                        }
                        // We ignore the _forceAsync flag here.
                        return synchronizationContext == Internal.ts_currentContext
                            ? default
                            : new SwitchToConfiguredContextAwaiter(synchronizationContext);
                    }
                    case Internal.SynchronizationOption.Background:
                    {
                        synchronizationContext = Promise.Config.BackgroundContext;
                        break;
                    }
                    default: // SynchronizationOption.Explicit
                    {
                        synchronizationContext = _synchronizationContext;
                        break;
                    }
                }

                if (synchronizationContext == null)
                {
                    synchronizationContext = Internal.BackgroundSynchronizationContextSentinel.s_instance;
                }
                // We ignore the _forceAsync flag here.
                return synchronizationContext == Internal.ts_currentContext
                    ? default
                    : new SwitchToConfiguredContextAwaiter(synchronizationContext);
            }
        }
    }
    
    // Internal so we can guarantee it will be awaited the moment it's created.
    internal readonly struct SwitchToConfiguredContextAwaiter : ICriticalNotifyCompletion
    {
        private readonly SynchronizationContext _context;

        [MethodImpl(Internal.InlineOption)]
        internal SwitchToConfiguredContextAwaiter(SynchronizationContext context)
        {
            _context = context;
        }

        public bool IsCompleted
        {
            [MethodImpl(Internal.InlineOption)]
            get => _context == null;
        }

        [MethodImpl(Internal.InlineOption)]
        public SwitchToConfiguredContextAwaiter GetAwaiter() => this;

        [MethodImpl(Internal.InlineOption)]
        public void GetResult() { }

        [MethodImpl(Internal.InlineOption)]
        public void OnCompleted(System.Action continuation)
            => UnsafeOnCompleted(continuation);

        [MethodImpl(Internal.InlineOption)]
        public void UnsafeOnCompleted(System.Action continuation)
        {
            if (_context == Internal.BackgroundSynchronizationContextSentinel.s_instance)
            {
                ThreadPool.QueueUserWorkItem(state => state.UnsafeAs<System.Action>().Invoke(), continuation);
            }
            else
            {
                _context.Post(state => state.UnsafeAs<System.Action>().Invoke(), continuation);
            }
        }
    }
#endif
}