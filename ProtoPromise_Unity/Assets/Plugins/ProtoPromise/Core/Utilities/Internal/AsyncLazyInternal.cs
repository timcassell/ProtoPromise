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
    partial class AsyncLazy<T>
    {
        // Must be volatile to prevent out-of-order memory read/write with the result.
        // This is set to null when we have successfully obtained the result, so we will have zero lock contention on future accesses.
        volatile private LazyFields _lazyFields;
        private T _result;

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
                Internal.PromiseRefBase.LazyPromiseWrapper<T> lazyWrapper;
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

                    // We create a lazy wrapper so that we aren't invoking the factory delegate inside the lock.
                    lazyWrapper = new Internal.PromiseRefBase.LazyPromiseWrapper<T>(_asyncValueFactory);
                    _promise = promise = lazyWrapper.Promise
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
                lazyWrapper.Start();
                return promise;
            }
        } // class LazyFields
    } // class AsyncLazy<T>

    partial class Internal
    {
        partial class PromiseRefBase
        {
            // This wrapper has to be nested under Internal.PromiseRefBase, because the PromiseResolvePromise type is private.
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal
#if CSHARP_7_3_OR_NEWER
                readonly ref
#endif
                struct LazyPromiseWrapper<T>
            {
                private readonly PromiseResolvePromise<T, DelegatePromiseVoidResult<T>> _promise;
                private readonly Func<Promise<T>> _factory;

                internal LazyPromiseWrapper(Func<Promise<T>> factory)
                {
                    _factory = factory;
                    _promise = PromiseResolvePromise<T, DelegatePromiseVoidResult<T>>.GetOrCreate(default(DelegatePromiseVoidResult<T>), 0);
                }

                internal Promise<T> Promise
                {
                    get { return new Promise<T>(_promise, _promise.Id, 0); }
                }

                internal void Start()
                {
                    SetCurrentInvoker(_promise);
                    try
                    {
                        var result = _factory.Invoke();
                        _promise.WaitFor(result, null);
                    }
                    catch (OperationCanceledException)
                    {
                        _promise.HandleNextInternal(null, Promises.Promise.State.Canceled);
                    }
                    catch (Exception e)
                    {
                        var rejectContainer = CreateRejectContainer(e, int.MinValue, null, _promise);
                        _promise.HandleNextInternal(rejectContainer, Promises.Promise.State.Rejected);
                    }
                    ClearCurrentInvoker();
                }
            } // struct LazyPromiseWrapper<T>
        } // class PromiseRefBase
    } // class Internal
}