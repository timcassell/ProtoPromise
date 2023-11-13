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
#pragma warning disable IDE0044 // Add readonly modifier
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
        internal sealed class AsyncAutoResetEventPromise : AsyncEventPromise<AsyncAutoResetEventInternal>
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
            internal static AsyncAutoResetEventPromise GetOrCreate(AsyncAutoResetEventInternal owner, SynchronizationContext callerContext)
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
        internal sealed partial class AsyncAutoResetEventInternal : ITraceable
        {
            // This must not be readonly.
            private ValueLinkedQueue<AsyncEventPromiseBase> _waiterQueue = new ValueLinkedQueue<AsyncEventPromiseBase>();
            volatile internal bool _isSet;

            internal AsyncAutoResetEventInternal(bool initialState)
            {
                _isSet = initialState;
                Track();
            }

            partial void Track();

            internal Promise WaitAsync()
            {
                // We don't spinwait here because it's async; we want to return to caller as fast as possible.
                AsyncEventPromiseBase promise;
                lock (this)
                {
                    if (_isSet)
                    {
                        _isSet = false;
                        return Promise.Resolved();
                    }

                    promise = AsyncAutoResetEventPromise.GetOrCreate(this, CaptureContext());
                    _waiterQueue.Enqueue(promise);
                }
                return new Promise(promise, promise.Id, 0);
            }

            internal Promise<bool> TryWaitAsync(CancelationToken cancelationToken)
            {
                // We don't spinwait here because it's async; we want to return to caller as fast as possible.

                // Immediately query the cancelation state before entering the lock.
                bool isCanceled = cancelationToken.IsCancelationRequested;

                AsyncEventPromiseBase promise;
                lock (this)
                {
                    bool isSet = _isSet;
                    if (isSet | isCanceled)
                    {
                        _isSet = false;
                        return Promise.Resolved(isSet);
                    }

                    promise = AsyncAutoResetEventPromise.GetOrCreate(this, CaptureContext());
                    _waiterQueue.Enqueue(promise);
                    promise.MaybeHookupCancelation(cancelationToken);
                }
                return new Promise<bool>(promise, promise.Id, 0);
            }

            internal void Wait()
            {
                // Because this is a synchronous wait, we do a short spinwait before yielding the thread.
                var spinner = new SpinWait();
                while (!_isSet & !spinner.NextSpinWillYield)
                {
                    spinner.SpinOnce();
                }

                AsyncEventPromiseBase promise;
                lock (this)
                {
                    if (_isSet)
                    {
                        _isSet = false;
                        return;
                    }
                    promise = AsyncAutoResetEventPromise.GetOrCreate(this, null);
                    _waiterQueue.Enqueue(promise);
                }
                Promise.ResultContainer resultContainer;
                PromiseSynchronousWaiter.TryWaitForResult(promise, promise.Id, TimeSpan.FromMilliseconds(Timeout.Infinite), out resultContainer);
                resultContainer.RethrowIfRejected();
            }

            internal bool TryWait(CancelationToken cancelationToken)
            {
                // Because this is a synchronous wait, we do a short spinwait before yielding the thread.
                var spinner = new SpinWait();
                bool isCanceled = cancelationToken.IsCancelationRequested;
                while (!_isSet & !isCanceled & !spinner.NextSpinWillYield)
                {
                    spinner.SpinOnce();
                    isCanceled = cancelationToken.IsCancelationRequested;
                }

                AsyncEventPromiseBase promise;
                lock (this)
                {
                    bool isSet = _isSet;
                    if (isSet | isCanceled)
                    {
                        _isSet = false;
                        return isSet;
                    }
                    promise = AsyncAutoResetEventPromise.GetOrCreate(this, null);
                    _waiterQueue.Enqueue(promise);
                    promise.MaybeHookupCancelation(cancelationToken);
                }
                Promise<bool>.ResultContainer resultContainer;
                PromiseSynchronousWaiter.TryWaitForResult(promise, promise.Id, TimeSpan.FromMilliseconds(Timeout.Infinite), out resultContainer);
                resultContainer.RethrowIfRejected();
                return resultContainer.Value;
            }

            internal void Set()
            {
                AsyncEventPromiseBase waiter;
                lock (this)
                {
                    if (_waiterQueue.IsEmpty)
                    {
                        _isSet = true;
                        return;
                    }
                    waiter = _waiterQueue.Dequeue();
                }
                waiter.Resolve();
            }

            [MethodImpl(InlineOption)]
            internal void Reset()
            {
                _isSet = false;
            }

            internal bool TryRemoveWaiter(AsyncEventPromiseBase waiter)
            {
                lock (this)
                {
                    return _waiterQueue.TryRemove(waiter);
                }
            }
        } // class AsyncAutoResetEventInternal

#if PROMISE_DEBUG
        partial class AsyncAutoResetEventInternal : ITraceable, IFinalizable
        {
            CausalityTrace ITraceable.Trace { get; set; }
            WeakNode IFinalizable.Tracker { get; set; }

            partial void Track()
            {
                SetCreatedStacktrace(this, 3);
                TrackFinalizable(this);
            }

            ~AsyncAutoResetEventInternal()
            {
                try
                {
                    UntrackFinalizable(this);
                    ValueLinkedStack<AsyncEventPromiseBase> waiters;
                    lock (this)
                    {
                        waiters = _waiterQueue.MoveElementsToStack();
                    }
                    if (waiters.IsEmpty)
                    {
                        return;
                    }

                    var rejectContainer = CreateRejectContainer(new Threading.AbandonedResetEventException("An AsyncAutoResetEvent was collected with waiters still pending."), int.MinValue, null, this);
                    do
                    {
                        waiters.Pop().Reject(rejectContainer);
                    } while (waiters.IsNotEmpty);
                    rejectContainer.ReportUnhandled();
                }
                catch (Exception e)
                {
                    // This should never happen.
                    ReportRejection(e, this);
                }
            }
        } // class AsyncAutoResetEventInternal
#endif // PROMISE_DEBUG
    } // class Internal
} // namespace Proto.Promises