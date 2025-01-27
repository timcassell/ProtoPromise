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
        internal sealed class AsyncManualResetEventPromise : AsyncEventPromise<Threading.AsyncManualResetEvent>
        {
            [MethodImpl(InlineOption)]
            private static AsyncManualResetEventPromise GetOrCreate()
            {
                var obj = ObjectPool.TryTakeOrInvalid<AsyncManualResetEventPromise>();
                return obj == InvalidAwaitSentinel.s_instance
                    ? new AsyncManualResetEventPromise()
                    : obj.UnsafeAs<AsyncManualResetEventPromise>();
            }

            [MethodImpl(InlineOption)]
            internal static AsyncManualResetEventPromise GetOrCreate(Threading.AsyncManualResetEvent owner, bool continueOnCapturedContext)
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
        partial class AsyncManualResetEvent : Internal.ITraceable
        {
            // These must not be readonly.
            private Internal.ValueLinkedQueue<Internal.AsyncEventPromiseBase> _waiters = new Internal.ValueLinkedQueue<Internal.AsyncEventPromiseBase>();
            // TODO: wrap these in a struct to remove extra padding.
            private Internal.SpinLocker _locker = new Internal.SpinLocker();
            volatile internal bool _isSet;

#if PROMISE_DEBUG
            partial void SetCreatedStacktrace() => Internal.SetCreatedStacktraceImpl(this, 2);

            Internal.CausalityTrace Internal.ITraceable.Trace { get; set; }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
            ~AsyncManualResetEvent()
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
            {
                _locker.Enter();
                var waiters = _waiters.TakeElements();
                _locker.Exit();
                if (waiters.IsEmpty)
                {
                    return;
                }

                var rejectContainer = Internal.CreateRejectContainer(new AbandonedResetEventException("An AsyncManualResetEvent was collected with waiters still pending."), int.MinValue, null, this);

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
                if (_isSet)
                {
                    return Promise.Resolved();
                }

                Internal.AsyncManualResetEventPromise promise;
                _locker.Enter();
                {
                    // Check the flag again inside the lock to resolve race condition with Set().
                    if (_isSet)
                    {
                        _locker.Exit();
                        return Promise.Resolved();
                    }
                    promise = Internal.AsyncManualResetEventPromise.GetOrCreate(this, continueOnCapturedContext);
                    _waiters.Enqueue(promise);
                }
                _locker.Exit();
                return new Promise(promise, promise.Id);
            }

            private Promise<bool> TryWaitAsyncImpl(CancelationToken cancelationToken, bool continueOnCapturedContext)
            {
                // We don't spinwait here because it's async; we want to return to caller as fast as possible.
                bool isSet = _isSet;
                if (isSet | cancelationToken.IsCancelationRequested)
                {
                    return Promise.Resolved(isSet);
                }

                Internal.AsyncManualResetEventPromise promise;
                _locker.Enter();
                {
                    // Check the flag again inside the lock to resolve race condition with Set().
                    if (_isSet)
                    {
                        _locker.Exit();
                        return Promise.Resolved(true);
                    }
                    promise = Internal.AsyncManualResetEventPromise.GetOrCreate(this, continueOnCapturedContext);
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
                while (!_isSet & !spinner.NextSpinWillYield)
                {
                    spinner.SpinOnce();
                }

                WaitAsyncImpl(false).Wait();
            }

            private bool TryWaitImpl(CancelationToken cancelationToken)
            {
                // Because this is a synchronous wait, we do a short spinwait before yielding the thread.
                var spinner = new SpinWait();
                while (!_isSet & !spinner.NextSpinWillYield & !cancelationToken.IsCancelationRequested)
                {
                    spinner.SpinOnce();
                }

                return TryWaitAsyncImpl(cancelationToken, false).WaitForResult();
            }

            private void SetImpl()
            {
                // Set the field before lock.
                _isSet = true;

                _locker.Enter();
                var waiters = _waiters.TakeElements();
                _locker.Exit();

                if (waiters.IsNotEmpty)
                {
                    var stack = waiters.MoveElementsToStackUnsafe();
                    do
                    {
                        stack.Pop().Resolve();
                    } while (stack.IsNotEmpty);
                }
            }

            internal bool TryRemoveWaiter(Internal.AsyncManualResetEventPromise waiter)
            {
                _locker.Enter();
                var removed = _waiters.TryRemove(waiter);
                _locker.Exit();
                return removed;
            }
        } // class AsyncManualResetEvent
    } // namespace Threading
} // namespace Proto.Promises