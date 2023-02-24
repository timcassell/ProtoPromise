#if UNITY_5_5 || NET_2_0 || NET_2_0_SUBSET
#define NET_LEGACY
#endif

#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif
#if !PROTO_PROMISE_PROGRESS_DISABLE
#define PROMISE_PROGRESS
#else
#undef PROMISE_PROGRESS
#endif

#pragma warning disable IDE0034 // Simplify 'default' expression

using System;
using System.Diagnostics;

namespace Proto.Promises
{
    /// <summary>
    /// Provides support for asynchronous lazy initialization.
    /// </summary>
    /// <typeparam name="T">The type of object that is being lazily initialized.</typeparam>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public sealed class AsyncLazy<T>
    {
        // Must be volatile to prevent out-of-order memory read/write with the result.
        // This is set to null when we have successfully obtained the result, so we will have zero lock contention on future accesses.
        volatile private LazyFields _lazyFields;
        private T _result;

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncLazy{T}"/> class that uses the specified initialization function.
        /// </summary>
        /// <param name="asyncValueFactory">The delegate that is invoked to produce the lazily initialized value when it is needed.</param>
        public AsyncLazy(Func<Promise<T>> asyncValueFactory)
        {
            _lazyFields = new LazyFields(asyncValueFactory);
        }

        /// <summary>
        /// Whether the asynchronous factory method has started. This is initially <c>false</c> and becomes <c>true</c> when this instance is awaited or after <see cref="Promise"/> is accessed.
        /// </summary>
        /// <remarks>This reverts to <c>false</c> if the factory does not complete successfully.</remarks>
        public bool IsStarted
        {
            get
            {
                var fields = _lazyFields;
                return fields == null || fields._isStarted;
            }
        }

        /// <summary>
        /// Starts the asynchronous factory method, if it has not already started, and returns the resulting <see cref="Promise{T}"/>.
        /// </summary>
        public Promise<T> Promise
        {
            get
            {
                // This is a volatile read, so we don't need a full memory barrier to prevent the result read from moving before it.
                var fields = _lazyFields;
                return fields == null
                    ? Promise<T>.Resolved(_result)
                    : fields.GetOrStartPromise(this);
            }
        }

        /// <summary>
        /// Asynchronous infrastructure support. This method permits instances of <see cref="AsyncLazy{T}"/> to be awaited.
        /// </summary>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public Async.CompilerServices.PromiseAwaiter<T> GetAwaiter()
        {
            return Promise.GetAwaiter();
        }

        // This allows us to have zero lock contention after the result has been obtained,
        // and we release all resources that are no longer needed for lazy initialization.
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        private sealed class LazyFields
        {
            private Promise<T> _promise;
            private readonly Func<Promise<T>> _asyncValueFactory;
            private bool _isComplete;
            internal bool _isStarted;

            internal LazyFields(Func<Promise<T>> asyncValueFactory)
            {
                _asyncValueFactory = asyncValueFactory;
            }

            internal Promise<T> GetOrStartPromise(AsyncLazy<T> owner)
            {
                Promises.Promise.Deferred deferred;
                Promise<T> promise;
                lock (this)
                {
                    if (_isComplete)
                    {
                        // We create a new resolved promise from the cached result, as it may need a backing reference if this is in DEBUG mode.
                        return Promise<T>.Resolved(_promise._result);
                    }

                    if (_isStarted)
                    {
                        return _promise.Duplicate();
                    }

                    _isStarted = true;
                    // We create a deferred so that we aren't invoking the factory delegate inside the lock.
                    deferred = Promises.Promise.NewDeferred();
                    // Depth -1 to properly normalize the progress from the returned promise.
                    _promise = promise = new Promise(deferred._ref, deferred._promiseId, Internal.NegativeOneDepth)
                        .Then(_asyncValueFactory)
                        // We have to cast the delegate type explicitly to appease the old .Net 3.5 compiler in Unity.
                        .ContinueWith(owner, (Promise<T>.ContinueFunc<AsyncLazy<T>, T>) ((_owner, rc) =>
                        {
                            var _this = _owner._lazyFields;
                            Promise<T> preservedPromise = default(Promise<T>);
                            try
                            {
                                if (rc.State != Promises.Promise.State.Resolved)
                                {
                                    lock (_this)
                                    {
                                        // Reset the state so that the factory will be ran again the next time the Promise is accessed.
                                        preservedPromise = _this._promise;
                                        _this._promise = default(Promise<T>);
                                        _this._isStarted = false;
                                    }

                                    rc.RethrowIfCanceled();
                                    rc.RethrowIfRejected();
                                }

                                // Release resources only when we have obtained the result successfully.
                                T result = rc.Result;
                                _owner._result = result;
                                // This is a volatile write, so we don't need a full memory barrier to prevent the result write from moving after it.
                                _owner._lazyFields = null;

                                lock (_this)
                                {
                                    preservedPromise = _this._promise;
                                    // We overwrite the cached promise with the result in case another thread has accessed this before the field was nulled.
                                    // We only cache the result without creating a backing reference.
                                    _this._promise = new Promise<T>(result);
                                    _this._isComplete = true;
                                }

                                return result;
                            }
                            finally
                            {
                                preservedPromise.Forget();
                            }
                        }))
                        .Preserve();
                } // lock

                promise = promise.Duplicate();
                deferred.TryResolve();
                return promise;
            }
        } // class LazyFields
    } // class AsyncLazy<T>
}