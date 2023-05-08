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
#pragma warning disable CA1507 // Use nameof to express symbol names

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
        internal sealed class AsyncCountdownEventPromise : AsyncEventPromise<AsyncCountdownEventInternal>
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
            internal static AsyncCountdownEventPromise GetOrCreate(AsyncCountdownEventInternal owner, SynchronizationContext callerContext)
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
        internal sealed class AsyncCountdownEventInternal : ITraceable
        {
            // This must not be readonly.
            private ValueLinkedQueue<AsyncEventPromiseBase> _waiters = new ValueLinkedQueue<AsyncEventPromiseBase>();
            volatile internal int _initialCount;
            volatile internal int _currentCount;

            internal AsyncCountdownEventInternal(int initialCount)
            {
                _initialCount = initialCount;
                _currentCount = initialCount;
                SetCreatedStacktrace(this, 2);
            }

#if PROMISE_DEBUG
            CausalityTrace ITraceable.Trace { get; set; }

            ~AsyncCountdownEventInternal()
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

                var rejectContainer = CreateRejectContainer(new Threading.AbandonedResetEventException("An AsyncCountdownEvent was collected with waiters still pending."), int.MinValue, null, this);
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
                if (_currentCount == 0)
                {
                    return Promise.Resolved();
                }

                AsyncCountdownEventPromise promise;
                lock (this)
                {
                    // Check the count again inside the lock to resolve race condition with Signal().
                    if (_currentCount == 0)
                    {
                        return Promise.Resolved();
                    }
                    promise = AsyncCountdownEventPromise.GetOrCreate(this, CaptureContext());
                    _waiters.Enqueue(promise);
                }
                return new Promise(promise, promise.Id, 0);
            }

            internal Promise<bool> TryWaitAsync(CancelationToken cancelationToken)
            {
                // We don't spinwait here because it's async; we want to return to caller as fast as possible.
                bool isSet = _currentCount == 0;
                if (isSet | cancelationToken.IsCancelationRequested)
                {
                    return Promise.Resolved(isSet);
                }

                AsyncCountdownEventPromise promise;
                lock (this)
                {
                    // Check the count again inside the lock to resolve race condition with Signal().
                    if (_currentCount == 0)
                    {
                        return Promise.Resolved(true);
                    }
                    promise = AsyncCountdownEventPromise.GetOrCreate(this, CaptureContext());
                    _waiters.Enqueue(promise);
                    promise.MaybeHookupCancelation(cancelationToken);
                }
                return new Promise<bool>(promise, promise.Id, 0);
            }

            internal void Wait()
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

                AsyncCountdownEventPromise promise;
                lock (this)
                {
                    // Check the count again inside the lock to resolve race condition with Signal().
                    if (_currentCount == 0)
                    {
                        return;
                    }
                    promise = AsyncCountdownEventPromise.GetOrCreate(this, null);
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

                AsyncCountdownEventPromise promise;
                lock (this)
                {
                    // Check the count again inside the lock to resolve race condition with Signal().
                    if (_currentCount == 0)
                    {
                        return true;
                    }
                    promise = AsyncCountdownEventPromise.GetOrCreate(this, null);
                    _waiters.Enqueue(promise);
                    promise.MaybeHookupCancelation(cancelationToken);
                }
                Promise<bool>.ResultContainer resultContainer;
                PromiseSynchronousWaiter.TryWaitForResult(promise, promise.Id, TimeSpan.FromMilliseconds(Timeout.Infinite), out resultContainer);
                resultContainer.RethrowIfRejected();
                return resultContainer.Result;
            }

            internal bool Signal(int signalCount)
            {
                ValueLinkedStack<AsyncEventPromiseBase> waiters;
                lock (this)
                {
                    // Read the _currentCount into a local variable to avoid extra unnecessary volatile accesses inside the lock.
                    int current = _currentCount;
                    if (signalCount < 1 | current == 0 | signalCount > current)
                    {
                        if (signalCount < 1)
                        {
                            throw new ArgumentOutOfRangeException("signalCount", "AsyncCountdownEvent.Signal: signalCount must be greater than or equal to 1.", GetFormattedStacktrace(2));
                        }
                        throw new InvalidOperationException(current == 0
                            ? "AsyncCountdownEvent.Signal: The AsyncCountdownEvent is already set."
                            : "AsyncCountdownEvent.Signal: signalCount cannot be greater than CurrentCount.", GetFormattedStacktrace(2));
                    }

                    current -= signalCount;
                    _currentCount = current;
                    if (current != 0)
                    {
                        return false;
                    }

                    waiters = _waiters.MoveElementsToStack();
                }
                while (waiters.IsNotEmpty)
                {
                    waiters.Pop().Resolve();
                }
                return true;
            }

            internal void Reset()
            {
                lock (this)
                {
                    _currentCount = _initialCount;
                }
            }

            internal void Reset(int count)
            {
                if (count < 0)
                {
                    throw new ArgumentOutOfRangeException("count", "AsyncCountdownEvent.Reset: count must be greater than or equal to 0.", GetFormattedStacktrace(2));
                }

                ValueLinkedStack<AsyncEventPromiseBase> waiters;
                lock (this)
                {
                    _initialCount = count;
                    _currentCount = count;
                    
                    if (count != 0)
                    {
                        return;
                    }

                    waiters = _waiters.MoveElementsToStack();
                }
                while (waiters.IsNotEmpty)
                {
                    waiters.Pop().Resolve();
                }
            }

            internal bool TryAddCount(int signalCount)
            {
                lock (this)
                {
                    // Read the _currentCount into a local variable to avoid extra unnecessary volatile accesses inside the lock.
                    int current = _currentCount;
                    if (signalCount < 1 | signalCount > int.MaxValue - current)
                    {
                        if (signalCount < 1)
                        {
                            throw new ArgumentOutOfRangeException("signalCount", "AsyncCountdownEvent.TryAddCount: signalCount must be greater than or equal to 1.", GetFormattedStacktrace(2));
                        }
                        throw new InvalidOperationException("AsyncCountdownEvent.TryAddCount: signalCount + CurrentCount exceeded int.MaxValue.", GetFormattedStacktrace(2));
                    }

                    if (current == 0)
                    {
                        return false;
                    }
                    _currentCount = current + signalCount;
                    return true;
                }
            }

            internal bool TryRemoveWaiter(AsyncCountdownEventPromise waiter)
            {
                lock (this)
                {
                    return _waiters.TryRemove(waiter);
                }
            }
        } // class AsyncCountdownEventInternal
    } // class Internal
} // namespace Proto.Promises