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
        internal sealed class AsyncCountdownEventPromise : AsyncEventPromise<Threading.AsyncCountdownEvent>
        {
            [MethodImpl(InlineOption)]
            private static AsyncCountdownEventPromise GetOrCreate()
            {
                var obj = ObjectPool.TryTakeOrInvalid<AsyncCountdownEventPromise>();
                return obj == InvalidAwaitSentinel.s_instance
                    ? new AsyncCountdownEventPromise()
                    : obj.UnsafeAs<AsyncCountdownEventPromise>();
            }

            [MethodImpl(InlineOption)]
            internal static AsyncCountdownEventPromise GetOrCreate(Threading.AsyncCountdownEvent owner, SynchronizationContext callerContext)
            {
                var promise = GetOrCreate();
                promise.Reset(callerContext);
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
                this.PrepareEarlyDispose();
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
        partial class AsyncCountdownEvent : Internal.ITraceable
        {
            // These must not be readonly.
            private Internal.ValueLinkedQueue<Internal.AsyncEventPromiseBase> _waiters = new Internal.ValueLinkedQueue<Internal.AsyncEventPromiseBase>();
            private Internal.SpinLocker _locker = new Internal.SpinLocker();
            private int _initialCount;
            volatile private int _currentCount;

#if PROMISE_DEBUG
            partial void SetCreatedStacktrace() => Internal.SetCreatedStacktraceImpl(this, 2);

            Internal.CausalityTrace Internal.ITraceable.Trace { get; set; }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
            ~AsyncCountdownEvent()
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
            {
                _locker.Enter();
                var waiters = _waiters.MoveElementsToStack();
                _locker.Exit();
                if (waiters.IsEmpty)
                {
                    return;
                }

                var rejectContainer = Internal.CreateRejectContainer(new AbandonedResetEventException("An AsyncCountdownEvent was collected with waiters still pending."), int.MinValue, null, this);
                do
                {
                    waiters.Pop().Reject(rejectContainer);
                } while (waiters.IsNotEmpty);
                rejectContainer.ReportUnhandled();
            }
#endif // PROMISE_DEBUG

            private Promise WaitAsyncImpl()
            {
                // We don't spinwait here because it's async; we want to return to caller as fast as possible.
                if (_currentCount == 0)
                {
                    return Promise.Resolved();
                }

                Internal.AsyncCountdownEventPromise promise;
                _locker.Enter();
                {
                    // Check the count again inside the lock to resolve race condition with Signal().
                    if (_currentCount == 0)
                    {
                        _locker.Exit();
                        return Promise.Resolved();
                    }
                    promise = Internal.AsyncCountdownEventPromise.GetOrCreate(this, Internal.CaptureContext());
                    _waiters.Enqueue(promise);
                }
                _locker.Exit();
                return new Promise(promise, promise.Id);
            }

            private Promise<bool> TryWaitAsyncImpl(CancelationToken cancelationToken)
            {
                // We don't spinwait here because it's async; we want to return to caller as fast as possible.
                bool isSet = _currentCount == 0;
                if (isSet | cancelationToken.IsCancelationRequested)
                {
                    return Promise.Resolved(isSet);
                }

                Internal.AsyncCountdownEventPromise promise;
                _locker.Enter();
                {
                    // Check the count again inside the lock to resolve race condition with Signal().
                    if (_currentCount == 0)
                    {
                        _locker.Exit();
                        return Promise.Resolved(true);
                    }
                    promise = Internal.AsyncCountdownEventPromise.GetOrCreate(this, Internal.CaptureContext());
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

            private void WaitImpl()
            {
                // Because this is a synchronous wait, we do a short spinwait before yielding the thread.
                var spinner = new SpinWait();
                bool isSet = _currentCount == 0;
                while (!isSet & !spinner.NextSpinWillYield)
                {
                    spinner.SpinOnce();
                    isSet = _currentCount == 0;
                }

                if (isSet)
                {
                    return;
                }

                Internal.AsyncCountdownEventPromise promise;
                _locker.Enter();
                {
                    // Check the count again inside the lock to resolve race condition with Signal().
                    if (_currentCount == 0)
                    {
                        _locker.Exit();
                        return;
                    }
                    promise = Internal.AsyncCountdownEventPromise.GetOrCreate(this, null);
                    _waiters.Enqueue(promise);
                }
                _locker.Exit();
                Internal.PromiseSynchronousWaiter.TryWaitForResult(promise, promise.Id, TimeSpan.FromMilliseconds(Timeout.Infinite), out var resultContainer);
                resultContainer.RethrowIfRejected();
            }

            private bool TryWaitImpl(CancelationToken cancelationToken)
            {
                // Because this is a synchronous wait, we do a short spinwait before yielding the thread.
                var spinner = new SpinWait();
                bool isSet = _currentCount == 0;
                bool isCanceled = cancelationToken.IsCancelationRequested;
                while (!isSet & !isCanceled & !spinner.NextSpinWillYield)
                {
                    spinner.SpinOnce();
                    isSet = _currentCount == 0;
                    isCanceled = cancelationToken.IsCancelationRequested;
                }

                if (isSet | isCanceled)
                {
                    return isSet;
                }

                Internal.AsyncCountdownEventPromise promise;
                _locker.Enter();
                {
                    // Check the count again inside the lock to resolve race condition with Signal().
                    if (_currentCount == 0)
                    {
                        _locker.Exit();
                        return true;
                    }
                    promise = Internal.AsyncCountdownEventPromise.GetOrCreate(this, null);
                    if (promise.HookupAndGetIsCanceled(cancelationToken))
                    {
                        _locker.Exit();
                        promise.DisposeImmediate();
                        return false;
                    }
                    _waiters.Enqueue(promise);
                }
                _locker.Exit();
                Internal.PromiseSynchronousWaiter.TryWaitForResult(promise, promise.Id, TimeSpan.FromMilliseconds(Timeout.Infinite), out var resultContainer);
                resultContainer.RethrowIfRejected();
                return resultContainer.Value;
            }

            private bool SignalImpl(int signalCount)
            {
                Internal.ValueLinkedStack<Internal.AsyncEventPromiseBase> waiters;
                _locker.Enter();
                {
                    // Read the _currentCount into a local variable to avoid extra unnecessary volatile accesses inside the lock.
                    int current = _currentCount;
                    if (signalCount < 1 | current == 0 | signalCount > current)
                    {
                        _locker.Exit();
                        if (signalCount < 1)
                        {
                            throw new ArgumentOutOfRangeException(nameof(signalCount), "AsyncCountdownEvent.Signal: signalCount must be greater than or equal to 1.", Internal.GetFormattedStacktrace(2));
                        }
                        throw new InvalidOperationException(current == 0
                            ? "AsyncCountdownEvent.Signal: The AsyncCountdownEvent is already set."
                            : "AsyncCountdownEvent.Signal: signalCount cannot be greater than CurrentCount.", Internal.GetFormattedStacktrace(2));
                    }

                    current -= signalCount;
                    _currentCount = current;
                    if (current != 0)
                    {
                        _locker.Exit();
                        return false;
                    }

                    waiters = _waiters.MoveElementsToStack();
                }
                _locker.Exit();
                while (waiters.IsNotEmpty)
                {
                    waiters.Pop().Resolve();
                }
                return true;
            }

            private void ResetImpl()
            {
                _locker.Enter();
                _currentCount = _initialCount;
                _locker.Exit();
            }

            private void ResetImpl(int count)
            {
                if (count < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(count), "AsyncCountdownEvent.Reset: count must be greater than or equal to 0.", Internal.GetFormattedStacktrace(2));
                }

                Internal.ValueLinkedStack<Internal.AsyncEventPromiseBase> waiters;
                _locker.Enter();
                {
                    _initialCount = count;
                    _currentCount = count;

                    if (count != 0)
                    {
                        _locker.Exit();
                        return;
                    }

                    waiters = _waiters.MoveElementsToStack();
                }
                _locker.Exit();
                while (waiters.IsNotEmpty)
                {
                    waiters.Pop().Resolve();
                }
            }

            private bool TryAddCountImpl(int signalCount)
            {
                _locker.Enter();
                {
                    // Read the _currentCount into a local variable to avoid extra unnecessary volatile accesses inside the lock.
                    int current = _currentCount;
                    if (signalCount < 1 | signalCount > int.MaxValue - current)
                    {
                        _locker.Exit();
                        if (signalCount < 1)
                        {
                            throw new ArgumentOutOfRangeException(nameof(signalCount), "AsyncCountdownEvent.TryAddCount: signalCount must be greater than or equal to 1.", Internal.GetFormattedStacktrace(2));
                        }
                        throw new InvalidOperationException("AsyncCountdownEvent.TryAddCount: signalCount + CurrentCount exceeded int.MaxValue.", Internal.GetFormattedStacktrace(2));
                    }

                    if (current == 0)
                    {
                        _locker.Exit();
                        return false;
                    }
                    _currentCount = current + signalCount;
                }
                _locker.Exit();
                return true;
            }

            internal bool TryRemoveWaiter(Internal.AsyncCountdownEventPromise waiter)
            {
                _locker.Enter();
                var removed = _waiters.TryRemove(waiter);
                _locker.Exit();
                return removed;
            }
        } // class AsyncCountdownEvent
    } // namespace Threading
} // namespace Proto.Promises