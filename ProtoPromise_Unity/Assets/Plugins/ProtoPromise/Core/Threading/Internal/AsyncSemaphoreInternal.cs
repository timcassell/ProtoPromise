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

#pragma warning disable IDE0018 // Inline variable declaration
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
        internal sealed class AsyncSemaphorePromise : AsyncEventPromise<AsyncSemaphoreInternal>
        {
            [MethodImpl(InlineOption)]
            private static AsyncSemaphorePromise GetOrCreate()
            {
                var obj = ObjectPool.TryTakeOrInvalid<AsyncSemaphorePromise>();
                return obj == InvalidAwaitSentinel.s_instance
                    ? new AsyncSemaphorePromise()
                    : obj.UnsafeAs<AsyncSemaphorePromise>();
            }

            [MethodImpl(InlineOption)]
            internal static AsyncSemaphorePromise GetOrCreate(AsyncSemaphoreInternal owner, SynchronizationContext callerContext)
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

            public override void Cancel()
            {
                ThrowIfInPool(this);
#if PROMISE_DEBUG
                var _owner = base._owner;
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
        internal sealed class AsyncSemaphoreInternal : ITraceable
        {
            // This must not be readonly.
            private ValueLinkedQueue<AsyncEventPromiseBase> _waiters = new ValueLinkedQueue<AsyncEventPromiseBase>();
            volatile internal int _currentCount;
            private readonly int _maxCount;

            internal AsyncSemaphoreInternal(int initialCount, int maxCount)
            {
                _currentCount = initialCount;
                _maxCount = maxCount;
                SetCreatedStacktrace(this, 2);
            }

#if PROMISE_DEBUG
            CausalityTrace ITraceable.Trace { get; set; }

            ~AsyncSemaphoreInternal()
            {
                ValueLinkedStack<AsyncEventPromiseBase> waiters;
                lock (this)
                {
                    waiters = _waiters.MoveElementsToStack();
                }
                if (waiters.IsEmpty)
                {
                    return;
                }

                var rejectContainer = CreateRejectContainer(new Threading.AbandonedSemaphoreException("An AsyncSemaphore was collected with waiters still pending."), int.MinValue, null, this);
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
                AsyncSemaphorePromise promise;
                lock (this)
                {
                    // Read the _currentCount into a local variable to avoid extra unnecessary volatile accesses inside the lock.
                    int current = _currentCount;
                    if (current != 0)
                    {
                        _currentCount = current - 1;
                        return Promise.Resolved();
                    }
                    promise = AsyncSemaphorePromise.GetOrCreate(this, CaptureContext());
                    _waiters.Enqueue(promise);
                }
                return new Promise(promise, promise.Id, 0);
            }

            internal Promise<bool> TryWaitAsync(CancelationToken cancelationToken)
            {
                // We don't spinwait here because it's async; we want to return to caller as fast as possible.

                // Immediately query the cancelation state before entering the lock.
                bool isCanceled = cancelationToken.IsCancelationRequested;

                AsyncSemaphorePromise promise;
                lock (this)
                {
                    // Read the _currentCount into a local variable to avoid extra unnecessary volatile accesses inside the lock.
                    int current = _currentCount;
                    if (current != 0)
                    {
                        _currentCount = current - 1;
                        return Promise.Resolved(true);
                    }
                    if (isCanceled)
                    {
                        return Promise.Resolved(false);
                    }
                    promise = AsyncSemaphorePromise.GetOrCreate(this, CaptureContext());
                    _waiters.Enqueue(promise);
                    promise.MaybeHookupCancelation(cancelationToken);
                }
                return new Promise<bool>(promise, promise.Id, 0);
            }

            internal void WaitSync()
            {
                // Because this is a synchronous wait, we do a short spinwait before yielding the thread.
                var spinner = new SpinWait();
                while (_currentCount == 0 & !spinner.NextSpinWillYield)
                {
                    spinner.SpinOnce();
                }

                AsyncSemaphorePromise promise;
                lock (this)
                {
                    // Read the _currentCount into a local variable to avoid extra unnecessary volatile accesses inside the lock.
                    int current = _currentCount;
                    if (current != 0)
                    {
                        _currentCount = current - 1;
                        return;
                    }
                    promise = AsyncSemaphorePromise.GetOrCreate(this, null);
                    _waiters.Enqueue(promise);
                }
                new Promise(promise, promise.Id, 0).Wait();
            }

            internal bool TryWait(CancelationToken cancelationToken)
            {
                // Because this is a synchronous wait, we do a short spinwait before yielding the thread.
                var spinner = new SpinWait();
                bool isCanceled = cancelationToken.IsCancelationRequested;
                while (_currentCount == 0 & !isCanceled & !spinner.NextSpinWillYield)
                {
                    spinner.SpinOnce();
                    isCanceled = cancelationToken.IsCancelationRequested;
                }

                AsyncSemaphorePromise promise;
                lock (this)
                {
                    // Read the _currentCount into a local variable to avoid extra unnecessary volatile accesses inside the lock.
                    int current = _currentCount;
                    if (current != 0)
                    {
                        _currentCount = current - 1;
                        return true;
                    }
                    if (isCanceled)
                    {
                        return false;
                    }
                    promise = AsyncSemaphorePromise.GetOrCreate(this, null);
                    _waiters.Enqueue(promise);
                    promise.MaybeHookupCancelation(cancelationToken);
                }
                return new Promise<bool>(promise, promise.Id, 0).WaitForResult();
            }

            internal void Release()
            {
                // Special implementation for Release(1) to remove unnecessary branches.
                AsyncEventPromiseBase waiter;
                lock (this)
                {
                    // Read the _currentCount into a local variable to avoid extra unnecessary volatile accesses inside the lock.
                    int current = _currentCount;
                    if (_maxCount == current)
                    {
                        throw new Threading.SemaphoreFullException(GetFormattedStacktrace(2));
                    }

                    if (_waiters.IsEmpty)
                    {
                        _currentCount = current + 1;
                        return;
                    }
                    waiter = _waiters.Dequeue();
                }
                waiter.Resolve();
            }

            internal void Release(int releaseCount)
            {
                ValueLinkedStack<AsyncEventPromiseBase> waiters;
                lock (this)
                {
                    // Read the _currentCount into a local variable to avoid extra unnecessary volatile accesses inside the lock.
                    int current = _currentCount;
                    if (releaseCount > _maxCount - current)
                    {
                        throw new Threading.SemaphoreFullException(GetFormattedStacktrace(2));
                    }

                    int waiterCount;
                    waiters = _waiters.MoveElementsToStack(releaseCount, out waiterCount);
                    _currentCount = current + releaseCount - waiterCount;
                }
                while (waiters.IsNotEmpty)
                {
                    waiters.Pop().Resolve();
                }
            }

            internal bool TryRemoveWaiter(AsyncSemaphorePromise waiter)
            {
                lock (this)
                {
                    return _waiters.TryRemove(waiter);
                }
            }
        } // class AsyncSemaphoreInternal
    } // class Internal
} // namespace Proto.Promises