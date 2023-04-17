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

#pragma warning disable IDE0090 // Use 'new(...)'

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Proto.Promises
{
    partial class Internal
    {
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal sealed class AsyncResetEventPromise : PromiseRefBase.AsyncSynchronizationPromiseBase<VoidResult>, ICancelable, ILinked<AsyncResetEventPromise>
        {
            AsyncResetEventPromise ILinked<AsyncResetEventPromise>.Next { get; set; }
            private AsyncManualResetEventInternal _owner;

            [MethodImpl(InlineOption)]
            private static AsyncResetEventPromise GetOrCreate()
            {
                var obj = ObjectPool.TryTakeOrInvalid<AsyncResetEventPromise>();
                return obj == InvalidAwaitSentinel.s_instance
                    ? new AsyncResetEventPromise()
                    : obj.UnsafeAs<AsyncResetEventPromise>();
            }

            [MethodImpl(InlineOption)]
            internal static AsyncResetEventPromise GetOrCreate(AsyncManualResetEventInternal owner, SynchronizationContext callerContext)
            {
                var promise = GetOrCreate();
                promise.Reset();
                promise._callerContext = callerContext;
                promise._owner = owner;
                return promise;
            }

            internal override void MaybeDispose()
            {
                Dispose();
                _owner = null;
                ObjectPool.MaybeRepool(this);
            }

            internal void Resolve()
            {
                ThrowIfInPool(this);

                // We don't need to check if the unregister was successful or not.
                // The fact that this was called means the cancelation was unable to unregister this from the lock.
                // We just dispose to wait for the callback to complete before we continue.
                _cancelationRegistration.Dispose();

                Continue(Promise.State.Resolved);
            }

            internal void MaybeHookupCancelation(CancelationToken cancelationToken)
            {
                ThrowIfInPool(this);
                cancelationToken.TryRegister(this, out _cancelationRegistration);
            }

            void ICancelable.Cancel()
            {
                ThrowIfInPool(this);
                if (!_owner.TryRemoveWaiter(this))
                {
                    return;
                }
                Continue(Promise.State.Canceled);
            }
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal sealed class AsyncManualResetEventInternal
        {
            // This must not be readonly.
            private ValueLinkedQueue<AsyncResetEventPromise> _waiters = new ValueLinkedQueue<AsyncResetEventPromise>();
            volatile internal bool _isSet;

            internal AsyncManualResetEventInternal(bool set)
            {
                _isSet = set;
            }

            // I'm unsure if there is a legitimate case for dropping a MRE without setting it, causing the waiters to never continue,
            // so not adding a finalizer for validation.

            internal Promise WaitAsync()
            {
                // We don't spinwait here because it's async; we want to return to caller as fast as possible.
                if (_isSet)
                {
                    return Promise.Resolved();
                }

                AsyncResetEventPromise promise;
                lock (this)
                {
                    promise = AsyncResetEventPromise.GetOrCreate(this, CaptureContext());
                    _waiters.Enqueue(promise);
                }
                return new Promise(promise, promise.Id, 0);
            }

            internal Promise<bool> WaitAsync(CancelationToken cancelationToken)
            {
                // We don't spinwait here because it's async; we want to return to caller as fast as possible.
                bool isSet = _isSet;
                if (isSet | cancelationToken.IsCancelationRequested)
                {
                    return Promise.Resolved(isSet);
                }

                AsyncResetEventPromise promise;
                lock (this)
                {
                    promise = AsyncResetEventPromise.GetOrCreate(this, CaptureContext());
                    _waiters.Enqueue(promise);
                    promise.MaybeHookupCancelation(cancelationToken);
                }
                return new Promise(promise, promise.Id, 0)
                    .ContinueWith(r =>
                    {
                        r.RethrowIfRejected();
                        return r.State == Promise.State.Resolved;
                    });
            }

            internal void WaitSync()
            {
                // Because this is a synchronous wait, we do a short spinwait before yielding the thread.
                var spinner = new SpinWait();
                bool isSet = _isSet;
                while (true)
                {
                    if (isSet | spinner.NextSpinWillYield)
                    {
                        break;
                    }
                    spinner.SpinOnce();
                    isSet = _isSet;
                }

                if (isSet)
                {
                    return;
                }

                AsyncResetEventPromise promise;
                lock (this)
                {
                    promise = AsyncResetEventPromise.GetOrCreate(this, null);
                    _waiters.Enqueue(promise);
                }
                new Promise(promise, promise.Id, 0).Wait();
            }

            internal bool WaitSync(CancelationToken cancelationToken)
            {
                // Because this is a synchronous wait, we do a short spinwait before yielding the thread.
                var spinner = new SpinWait();
                bool isSet = _isSet;
                bool isCanceled = cancelationToken.IsCancelationRequested;
                while (true)
                {
                    if (isSet | isCanceled | spinner.NextSpinWillYield)
                    {
                        break;
                    }
                    spinner.SpinOnce();
                    isSet = _isSet;
                    isCanceled = cancelationToken.IsCancelationRequested;
                }

                if (isSet | isCanceled)
                {
                    return isSet;
                }

                AsyncResetEventPromise promise;
                lock (this)
                {
                    promise = AsyncResetEventPromise.GetOrCreate(this, null);
                    _waiters.Enqueue(promise);
                    promise.MaybeHookupCancelation(cancelationToken);
                }
                return new Promise(promise, promise.Id, 0)
                    .ContinueWith(r =>
                    {
                        r.RethrowIfRejected();
                        return r.State == Promise.State.Resolved;
                    })
                    .WaitForResult();
            }

            internal void Set()
            {
                // Set the field before lock.
                _isSet = true;

                ValueLinkedStack<AsyncResetEventPromise> waiters;
                lock (this)
                {
                    waiters = _waiters.MoveElementsToStack();
                }
                while (waiters.IsNotEmpty)
                {
                    waiters.Pop().Resolve();
                }
            }

            [MethodImpl(InlineOption)]
            internal void Reset()
            {
                _isSet = false;
            }

            internal bool TryRemoveWaiter(AsyncResetEventPromise waiter)
            {
                lock (this)
                {
                    return _waiters.TryRemove(waiter);
                }
            }
        } // class AsyncManualResetEventInternal
    } // class Internal
} // namespace Proto.Promises