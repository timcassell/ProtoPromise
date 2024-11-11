#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

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
        internal sealed class AsyncSemaphorePromise : AsyncEventPromise<Threading.AsyncSemaphore>
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
            internal static AsyncSemaphorePromise GetOrCreate(Threading.AsyncSemaphore owner, bool continueOnCapturedContext)
            {
                var promise = GetOrCreate();
                promise.Reset(continueOnCapturedContext);
                promise._owner = owner;
                return promise;
            }

            internal override void MaybeDispose()
            {
                Dispose();
                _owner = null;
                ObjectPool.MaybeRepool(this);
            }

            [MethodImpl(InlineOption)]
            internal void DisposeImmediate()
            {
                PrepareEarlyDispose();
                MaybeDispose();
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
                Continue();
            }
        }
    } // class Internal

    namespace Threading
    {
        partial class AsyncSemaphore : Internal.ITraceable
        {
            // These must not be readonly.
            private Internal.ValueLinkedQueue<Internal.AsyncEventPromiseBase> _waiters = new Internal.ValueLinkedQueue<Internal.AsyncEventPromiseBase>();
            private Internal.SpinLocker _locker = new Internal.SpinLocker();
            volatile private int _currentCount;
            private readonly int _maxCount;

#if PROMISE_DEBUG
            partial void SetCreatedStacktrace() => Internal.SetCreatedStacktraceImpl(this, 2);

            Internal.CausalityTrace Internal.ITraceable.Trace { get; set; }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
            ~AsyncSemaphore()
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
            {
                _locker.Enter();
                var waiters = _waiters.TakeElements();
                _locker.Exit();
                if (waiters.IsEmpty)
                {
                    return;
                }

                var rejectContainer = Internal.CreateRejectContainer(new AbandonedSemaphoreException("An AsyncSemaphore was collected with waiters still pending."), int.MinValue, null, this);

                var stack = waiters.MoveElementsToStackUnsafe();
                do
                {
                    stack.Pop().Reject(rejectContainer);
                } while (stack.IsNotEmpty);
                rejectContainer.ReportUnhandled();
            }
#endif // PROMISE_DEBUG

            private Promise WaitAsyncImpl(bool continueOnCapturedContext)
            {
                // We don't spinwait here because it's async; we want to return to caller as fast as possible.
                Internal.AsyncSemaphorePromise promise;
                _locker.Enter();
                {
                    // Read the _currentCount into a local variable to avoid extra unnecessary volatile accesses inside the lock.
                    int current = _currentCount;
                    if (current != 0)
                    {
                        _currentCount = current - 1;
                        _locker.Exit();
                        return Promise.Resolved();
                    }
                    promise = Internal.AsyncSemaphorePromise.GetOrCreate(this, continueOnCapturedContext);
                    _waiters.Enqueue(promise);
                }
                _locker.Exit();
                return new Promise(promise, promise.Id);
            }

            private Promise<bool> TryWaitAsyncImpl(CancelationToken cancelationToken, bool continueOnCapturedContext)
            {
                // We don't spinwait here because it's async; we want to return to caller as fast as possible.

                // Immediately query the cancelation state before entering the lock.
                bool isCanceled = cancelationToken.IsCancelationRequested;

                Internal.AsyncSemaphorePromise promise;
                _locker.Enter();
                {
                    // Read the _currentCount into a local variable to avoid extra unnecessary volatile accesses inside the lock.
                    int current = _currentCount;
                    if (current != 0)
                    {
                        _currentCount = current - 1;
                        _locker.Exit();
                        return Promise.Resolved(true);
                    }
                    if (isCanceled)
                    {
                        _locker.Exit();
                        return Promise.Resolved(false);
                    }
                    promise = Internal.AsyncSemaphorePromise.GetOrCreate(this, continueOnCapturedContext);
                    if (promise.HookupAndGetIsCanceled(cancelationToken))
                    {
                        _locker.Exit();
                        promise.DisposeImmediate();
                        return Promise.Resolved(false);
                    }
                    _waiters.Enqueue(promise);
                }
                _locker.Exit();
                return new Promise<bool>(promise, promise.Id);
            }

            private void WaitSyncImpl()
            {
                // Because this is a synchronous wait, we do a short spinwait before yielding the thread.
                var spinner = new SpinWait();
                while (_currentCount == 0 & !spinner.NextSpinWillYield)
                {
                    spinner.SpinOnce();
                }

                WaitAsyncImpl(false).Wait();
            }

            private bool TryWaitImpl(CancelationToken cancelationToken)
            {
                // Because this is a synchronous wait, we do a short spinwait before yielding the thread.
                var spinner = new SpinWait();
                while (_currentCount == 0 & !spinner.NextSpinWillYield & !cancelationToken.IsCancelationRequested)
                {
                    spinner.SpinOnce();
                }

                return TryWaitAsyncImpl(cancelationToken, false).WaitForResult();
            }

            private void ReleaseImpl()
            {
                // Special implementation for Release(1) to remove unnecessary branches.
                Internal.AsyncEventPromiseBase waiter;
                _locker.Enter();
                {
                    // Read the _currentCount into a local variable to avoid extra unnecessary volatile accesses inside the lock.
                    int current = _currentCount;
                    if (_maxCount == current)
                    {
                        _locker.Exit();
                        throw new SemaphoreFullException(Internal.GetFormattedStacktrace(2));
                    }

                    if (_waiters.IsEmpty)
                    {
                        _currentCount = current + 1;
                        _locker.Exit();
                        return;
                    }
                    waiter = _waiters.Dequeue();
                }
                _locker.Exit();
                waiter.Resolve();
            }

            private void ReleaseImpl(int releaseCount)
            {
                Internal.ValueLinkedStack<Internal.AsyncEventPromiseBase> waiters;
                _locker.Enter();
                {
                    // Read the _currentCount into a local variable to avoid extra unnecessary volatile accesses inside the lock.
                    int current = _currentCount;
                    if (releaseCount > _maxCount - current)
                    {
                        _locker.Exit();
                        throw new SemaphoreFullException(Internal.GetFormattedStacktrace(2));
                    }

                    waiters = _waiters.MoveElementsToStack(releaseCount, out int waiterCount);
                    _currentCount = current + releaseCount - waiterCount;
                }
                _locker.Exit();
                while (waiters.IsNotEmpty)
                {
                    waiters.Pop().Resolve();
                }
            }

            internal bool TryRemoveWaiter(Internal.AsyncSemaphorePromise waiter)
            {
                _locker.Enter();
                var removed = _waiters.TryRemove(waiter);
                _locker.Exit();
                return removed;
            }
        } // class AsyncSemaphore
    } // namespace Threading
} // namespace Proto.Promises