#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#pragma warning disable IDE0018 // Inline variable declaration
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
        internal sealed class AsyncManualResetEventPromise : AsyncEventPromise<AsyncManualResetEventInternal>
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
            internal static AsyncManualResetEventPromise GetOrCreate(AsyncManualResetEventInternal owner, SynchronizationContext callerContext)
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

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal sealed class AsyncManualResetEventInternal : ITraceable
        {
            // This must not be readonly.
            private ValueLinkedQueue<AsyncEventPromiseBase> _waiters = new ValueLinkedQueue<AsyncEventPromiseBase>();
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
                ValueLinkedStack<AsyncEventPromiseBase> waiters;
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
                return new Promise(promise, promise.Id);
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
                    if (promise.HookupAndGetIsCanceled(cancelationToken))
                    {
                        promise.DisposeImmediate();
                        return Promise.Resolved(false);
                    }
                    _waiters.Enqueue(promise);
                }
                return new Promise<bool>(promise, promise.Id);
            }

            internal void Wait()
            {
                // Because this is a synchronous wait, we do a short spinwait before yielding the thread.
                var spinner = new SpinWait();
                bool isSet = _isSet;
                while (!isSet & !spinner.NextSpinWillYield)
                {
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
                Promise.ResultContainer resultContainer;
                PromiseSynchronousWaiter.TryWaitForResult(promise, promise.Id, TimeSpan.FromMilliseconds(Timeout.Infinite), out resultContainer);
                resultContainer.RethrowIfRejected();
            }

            internal bool TryWait(CancelationToken cancelationToken)
            {
                // Because this is a synchronous wait, we do a short spinwait before yielding the thread.
                var spinner = new SpinWait();
                bool isSet = _isSet;
                bool isCanceled = cancelationToken.IsCancelationRequested;
                while (!isSet & !isCanceled & !spinner.NextSpinWillYield)
                {
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
                    if (promise.HookupAndGetIsCanceled(cancelationToken))
                    {
                        promise.DisposeImmediate();
                        return false;
                    }
                    _waiters.Enqueue(promise);
                }
                Promise<bool>.ResultContainer resultContainer;
                PromiseSynchronousWaiter.TryWaitForResult(promise, promise.Id, TimeSpan.FromMilliseconds(Timeout.Infinite), out resultContainer);
                resultContainer.RethrowIfRejected();
                return resultContainer.Value;
            }

            internal void Set()
            {
                // Set the field before lock.
                _isSet = true;

                ValueLinkedStack<AsyncEventPromiseBase> waiters;
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