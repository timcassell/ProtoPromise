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
        internal sealed class AsyncAutoResetEventPromise : AsyncEventPromise<Threading.AsyncAutoResetEvent>
        {
            [MethodImpl(InlineOption)]
            private static AsyncAutoResetEventPromise GetOrCreate()
            {
                var obj = ObjectPool.TryTakeOrInvalid<AsyncAutoResetEventPromise>();
                return obj == InvalidAwaitSentinel.s_instance
                    ? new AsyncAutoResetEventPromise()
                    : obj.UnsafeAs<AsyncAutoResetEventPromise>();
            }

            [MethodImpl(InlineOption)]
            internal static AsyncAutoResetEventPromise GetOrCreate(Threading.AsyncAutoResetEvent owner, SynchronizationContext callerContext)
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
                SetCompletionState(Promise.State.Resolved);
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
        partial class AsyncAutoResetEvent : Internal.ITraceable
        {
            // These must not be readonly.
            private Internal.ValueLinkedQueue<Internal.AsyncEventPromiseBase> _waiterQueue = new Internal.ValueLinkedQueue<Internal.AsyncEventPromiseBase>();
            private Internal.SpinLocker _locker = new Internal.SpinLocker();
            volatile private bool _isSet;

#if PROMISE_DEBUG
            partial void SetCreatedStacktrace() => Internal.SetCreatedStacktraceImpl(this, 2);

            Internal.CausalityTrace Internal.ITraceable.Trace { get; set; }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
            ~AsyncAutoResetEvent()
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
            {
                _locker.Enter();
                var waiters = _waiterQueue.MoveElementsToStack();
                _locker.Exit();
                if (waiters.IsEmpty)
                {
                    return;
                }

                var rejectContainer = Internal.CreateRejectContainer(new AbandonedResetEventException("An AsyncAutoResetEvent was collected with waiters still pending."), int.MinValue, null, this);
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
                Internal.AsyncAutoResetEventPromise promise;
                _locker.Enter();
                {
                    if (_isSet)
                    {
                        _isSet = false;
                        _locker.Exit();
                        return Promise.Resolved();
                    }

                    promise = Internal.AsyncAutoResetEventPromise.GetOrCreate(this, Internal.CaptureContext());
                    _waiterQueue.Enqueue(promise);
                }
                _locker.Exit();
                return new Promise(promise, promise.Id);
            }

            private Promise<bool> TryWaitAsyncImpl(CancelationToken cancelationToken)
            {
                // We don't spinwait here because it's async; we want to return to caller as fast as possible.

                // Immediately query the cancelation state before entering the lock.
                bool isCanceled = cancelationToken.IsCancelationRequested;

                Internal.AsyncAutoResetEventPromise promise;
                _locker.Enter();
                {
                    bool isSet = _isSet;
                    if (isSet | isCanceled)
                    {
                        _isSet = false;
                        _locker.Exit();
                        return Promise.Resolved(isSet);
                    }

                    promise = Internal.AsyncAutoResetEventPromise.GetOrCreate(this, Internal.CaptureContext());
                    if (promise.HookupAndGetIsCanceled(cancelationToken))
                    {
                        _isSet = false;
                        _locker.Exit();
                        promise.DisposeImmediate();
                        return Promise.Resolved(isSet);
                    }
                    _waiterQueue.Enqueue(promise);
                }
                _locker.Exit();
                return new Promise<bool>(promise, promise.Id);
            }

            private void WaitImpl()
            {
                // Because this is a synchronous wait, we do a short spinwait before yielding the thread.
                var spinner = new SpinWait();
                while (!_isSet & !spinner.NextSpinWillYield)
                {
                    spinner.SpinOnce();
                }

                Internal.AsyncAutoResetEventPromise promise;
                _locker.Enter();
                {
                    if (_isSet)
                    {
                        _isSet = false;
                        _locker.Exit();
                        return;
                    }
                    promise = Internal.AsyncAutoResetEventPromise.GetOrCreate(this, null);
                    _waiterQueue.Enqueue(promise);
                }
                _locker.Exit();
                Internal.PromiseSynchronousWaiter.TryWaitForResult(promise, promise.Id, TimeSpan.FromMilliseconds(Timeout.Infinite), out var resultContainer);
                resultContainer.RethrowIfRejected();
            }

            private bool TryWaitImpl(CancelationToken cancelationToken)
            {
                // Because this is a synchronous wait, we do a short spinwait before yielding the thread.
                var spinner = new SpinWait();
                bool isCanceled = cancelationToken.IsCancelationRequested;
                while (!_isSet & !isCanceled & !spinner.NextSpinWillYield)
                {
                    spinner.SpinOnce();
                    isCanceled = cancelationToken.IsCancelationRequested;
                }

                Internal.AsyncAutoResetEventPromise promise;
                _locker.Enter();
                {
                    bool isSet = _isSet;
                    if (isSet | isCanceled)
                    {
                        _isSet = false;
                        _locker.Exit();
                        return isSet;
                    }

                    promise = Internal.AsyncAutoResetEventPromise.GetOrCreate(this, null);
                    if (promise.HookupAndGetIsCanceled(cancelationToken))
                    {
                        _isSet = false;
                        _locker.Exit();
                        promise.DisposeImmediate();
                        return isSet;
                    }
                    _waiterQueue.Enqueue(promise);
                }
                _locker.Exit();
                Internal.PromiseSynchronousWaiter.TryWaitForResult(promise, promise.Id, TimeSpan.FromMilliseconds(Timeout.Infinite), out var resultContainer);
                resultContainer.RethrowIfRejected();
                return resultContainer.Value;
            }

            private void SetImpl()
            {
                Internal.AsyncEventPromiseBase waiter;
                _locker.Enter();
                {
                    if (_waiterQueue.IsEmpty)
                    {
                        _isSet = true;
                        _locker.Exit();
                        return;
                    }
                    waiter = _waiterQueue.Dequeue();
                }
                _locker.Exit();
                waiter.Resolve();
            }

            [MethodImpl(Internal.InlineOption)]
            private void ResetImpl()
                => _isSet = false;

            internal bool TryRemoveWaiter(Internal.AsyncEventPromiseBase waiter)
            {
                _locker.Enter();
                var removed = _waiterQueue.TryRemove(waiter);
                _locker.Exit();
                return removed;
            }
        } // class AsyncAutoResetEvent
    } // namespace Threading
} // namespace Proto.Promises