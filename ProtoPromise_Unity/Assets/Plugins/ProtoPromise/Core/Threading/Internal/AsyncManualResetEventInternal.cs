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

#pragma warning disable IDE0019 // Use pattern matching
#pragma warning disable IDE0090 // Use 'new(...)'

using System;
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
        internal sealed class AsyncManualResetEventPromise : AsyncResetEventPromise
        {
#if PROMISE_DEBUG
            // We use a weak reference in DEBUG mode so the MRE's finalizer can still run if it's dropped.
            private readonly WeakReference _ownerReference = new WeakReference(null, false);
#else
            private AsyncManualResetEventInternal _owner;
#endif

            [MethodImpl(InlineOption)]
            private static AsyncManualResetEventPromise GetOrCreate()
            {
                var obj = ObjectPool.TryTakeOrInvalid<AsyncManualResetEventPromise>();
                return obj == InvalidAwaitSentinel.s_instance
                    ? new AsyncManualResetEventPromise()
                    : obj.UnsafeAs<AsyncManualResetEventPromise>();
            }

            [MethodImpl(InlineOption)]
            internal static AsyncManualResetEventPromise GetOrCreate(AsyncManualResetEventInternal owner, SynchronizationContext callerContext)
            {
                var promise = GetOrCreate();
                promise.Reset();
                promise._callerContext = callerContext;
#if PROMISE_DEBUG
                promise._ownerReference.Target = owner;
#else
                promise._owner = owner;
#endif
                return promise;
            }

            internal override void MaybeDispose()
            {
                Dispose();
#if PROMISE_DEBUG
                _ownerReference.Target = null;
#else
                _owner = null;
#endif
                ObjectPool.MaybeRepool(this);
            }

            public override void Cancel()
            {
                ThrowIfInPool(this);
#if PROMISE_DEBUG
                var _owner = _ownerReference.Target as AsyncManualResetEventInternal;
                if (_owner == null)
                {
                    return;
                }
#endif
                if (!_owner.TryRemoveWaiter(this))
                {
                    return;
                }
                _result = false;
                Continue(Promise.State.Resolved);
            }
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal sealed class AsyncManualResetEventInternal : ITraceable
        {
            // This must not be readonly.
            private ValueLinkedQueue<AsyncResetEventPromise> _waiters = new ValueLinkedQueue<AsyncResetEventPromise>();
            volatile internal bool _isSet;

            internal AsyncManualResetEventInternal(bool initialState)
            {
                _isSet = initialState;
                SetCreatedStacktrace(this, 2);
            }

#if PROMISE_DEBUG
            CausalityTrace ITraceable.Trace { get; set; }

            ~AsyncManualResetEventInternal()
            {
                ValueLinkedStack<AsyncResetEventPromise> waiters;
                lock (this)
                {
                    waiters = _waiters.MoveElementsToStack();
                }
                if (waiters.IsEmpty)
                {
                    return;
                }

                var rejectContainer = CreateRejectContainer(new Threading.AbandonedResetEventException("An AsyncManualResetEvent was collected with waiters still pending."), int.MinValue, null, this);
                do
                {
                    waiters.Pop().Reject(rejectContainer);
                } while (waiters.IsNotEmpty);
                rejectContainer.ReportUnhandled();
            }
#endif // PROMISE_DEBUG

            internal Promise WaitAsync()
            {
                // We don't spinwait here because it's async; we want to return to caller as fast as possible.
                if (_isSet)
                {
                    return Promise.Resolved();
                }

                AsyncManualResetEventPromise promise;
                lock (this)
                {
                    // Check the flag again inside the lock to resolve race condition with Set().
                    if (_isSet)
                    {
                        return Promise.Resolved();
                    }
                    promise = AsyncManualResetEventPromise.GetOrCreate(this, CaptureContext());
                    _waiters.Enqueue(promise);
                }
                return new Promise(promise, promise.Id, 0);
            }

            internal Promise<bool> TryWaitAsync(CancelationToken cancelationToken)
            {
                // We don't spinwait here because it's async; we want to return to caller as fast as possible.
                bool isSet = _isSet;
                if (isSet | cancelationToken.IsCancelationRequested)
                {
                    return Promise.Resolved(isSet);
                }

                AsyncManualResetEventPromise promise;
                lock (this)
                {
                    // Check the flag again inside the lock to resolve race condition with Set().
                    if (_isSet)
                    {
                        return Promise.Resolved(true);
                    }
                    promise = AsyncManualResetEventPromise.GetOrCreate(this, CaptureContext());
                    _waiters.Enqueue(promise);
                    promise.MaybeHookupCancelation(cancelationToken);
                }
                return new Promise<bool>(promise, promise.Id, 0);
            }

            internal void Wait()
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

                AsyncManualResetEventPromise promise;
                lock (this)
                {
                    // Check the flag again inside the lock to resolve race condition with Set().
                    if (_isSet)
                    {
                        return;
                    }
                    promise = AsyncManualResetEventPromise.GetOrCreate(this, null);
                    _waiters.Enqueue(promise);
                }
                new Promise(promise, promise.Id, 0).Wait();
            }

            internal bool TryWait(CancelationToken cancelationToken)
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

                AsyncManualResetEventPromise promise;
                lock (this)
                {
                    // Check the flag again inside the lock to resolve race condition with Set().
                    if (_isSet)
                    {
                        return true;
                    }
                    promise = AsyncManualResetEventPromise.GetOrCreate(this, null);
                    _waiters.Enqueue(promise);
                    promise.MaybeHookupCancelation(cancelationToken);
                }
                return new Promise<bool>(promise, promise.Id, 0).WaitForResult();
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

            internal bool TryRemoveWaiter(AsyncManualResetEventPromise waiter)
            {
                lock (this)
                {
                    return _waiters.TryRemove(waiter);
                }
            }
        } // class AsyncManualResetEventInternal
    } // class Internal
} // namespace Proto.Promises