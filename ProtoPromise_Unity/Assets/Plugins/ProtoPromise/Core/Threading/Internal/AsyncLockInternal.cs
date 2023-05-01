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

using Proto.Promises.Threading;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Proto.Promises
{
#if UNITY_2021_2_OR_NEWER || NETSTANDARD2_1_OR_GREATER || NETCOREAPP
    // Removed the code without removing the namespace to prevent Unity complaining.
    partial class Internal
    {
        internal interface IAsyncLockPromise : ILinked<IAsyncLockPromise>, ITraceable
        {
            void Resolve(ref long currentKey);
#if PROMISE_DEBUG
            void Reject(IRejectContainer rejectContainer);
#endif
        }

        partial class PromiseRefBase
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal abstract class AsyncLockPromiseBase<TResult> : AsyncSynchronizationPromiseBase<TResult>, IAsyncLockPromise
            {
                IAsyncLockPromise ILinked<IAsyncLockPromise>.Next { get; set; }

                public abstract void Resolve(ref long currentKey);

#if PROMISE_DEBUG
                void IAsyncLockPromise.Reject(IRejectContainer rejectContainer) { Reject(rejectContainer); }
#endif
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal sealed class AsyncLockPromise : AsyncLockPromiseBase<AsyncLock.Key>, ICancelable
            {
                internal AsyncLockInternal Owner => _result._owner;

                [MethodImpl(InlineOption)]
                private static AsyncLockPromise GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<AsyncLockPromise>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new AsyncLockPromise()
                        : obj.UnsafeAs<AsyncLockPromise>();
                }

                [MethodImpl(InlineOption)]
                internal static AsyncLockPromise GetOrCreate(AsyncLockInternal owner, SynchronizationContext callerContext)
                {
                    var promise = GetOrCreate();
                    promise.Reset();
                    promise._callerContext = callerContext;
                    promise._result = new AsyncLock.Key(owner); // This will be overwritten when this is resolved, we just store the owner here for cancelation.
                    return promise;
                }

                internal override void MaybeDispose()
                {
                    Dispose();
                    ObjectPool.MaybeRepool(this);
                }

                internal void MaybeHookupCancelation(CancelationToken cancelationToken)
                {
                    ThrowIfInPool(this);
                    cancelationToken.TryRegister(this, out _cancelationRegistration);
                }

                public override void Resolve(ref long currentKey)
                {
                    ThrowIfInPool(this);

                    // We don't need to check if the unregister was successful or not.
                    // The fact that this was called means the cancelation was unable to unregister this from the lock.
                    // We just dispose to wait for the callback to complete before we continue.
                    _cancelationRegistration.Dispose();

                    _result = new AsyncLock.Key(Owner, currentKey, this);
                    Continue(Promise.State.Resolved);
                }

                void ICancelable.Cancel()
                {
                    ThrowIfInPool(this);
                    if (!Owner.TryUnregister(this))
                    {
                        return;
                    }
                    Continue(Promise.State.Canceled);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal sealed class AsyncLockWaitPromise : AsyncLockPromiseBase<bool>, ICancelable
            {
                private AsyncLockInternal _owner;
                private long _key;

                [MethodImpl(InlineOption)]
                private static AsyncLockWaitPromise GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<AsyncLockWaitPromise>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new AsyncLockWaitPromise()
                        : obj.UnsafeAs<AsyncLockWaitPromise>();
                }

                [MethodImpl(InlineOption)]
                internal static AsyncLockWaitPromise GetOrCreate(AsyncLockInternal owner, long key, SynchronizationContext callerContext)
                {
                    var promise = GetOrCreate();
                    promise.Reset();
                    promise._owner = owner;
                    promise._key = key;
                    promise._callerContext = callerContext;
                    promise._result = true;
                    return promise;
                }

                internal override void MaybeDispose()
                {
                    Dispose();
                    _owner = null;
                    ObjectPool.MaybeRepool(this);
                }

                internal void MaybeHookupCancelation(CancelationToken cancelationToken)
                {
                    ThrowIfInPool(this);
                    // Just hook up the listener, don't check for already canceled since we have to make sure the lock is re-acquired before this is resolved.
                    cancelationToken.TryRegister(this, out _cancelationRegistration);
                }

                public override void Resolve(ref long currentKey)
                {
                    ThrowIfInPool(this);

                    // Dispose to wait for the callback to complete before we continue.
                    _cancelationRegistration.Dispose();

                    currentKey = _key;
                    Continue(Promise.State.Resolved);
                }

                void ICancelable.Cancel()
                {
                    ThrowIfInPool(this);
                    // We just move this to the ready queue, we don't post the continuation yet because we have to make sure the lock is re-acquired before this is resolved.
                    _result = false;
                    _owner.MaybeMakeReady(this);
                }

                internal override PromiseRefBase AddWaiter(short promiseId, HandleablePromiseBase waiter, out HandleablePromiseBase previousWaiter)
                {
                    if (promiseId != Id)
                    {
                        previousWaiter = InvalidAwaitSentinel.s_instance;
                        return InvalidAwaitSentinel.s_instance;
                    }
                    ThrowIfInPool(this);
                    WasAwaitedOrForgotten = true;
                    previousWaiter = CompareExchangeWaiter(waiter, PendingAwaitSentinel.s_instance);
                    if (previousWaiter != PendingAwaitSentinel.s_instance
                        && CompareExchangeWaiter(waiter, PromiseCompletionSentinel.s_instance) != PromiseCompletionSentinel.s_instance)
                    {
                        previousWaiter = InvalidAwaitSentinel.s_instance;
                        return InvalidAwaitSentinel.s_instance;
                    }
                    _owner.ReleaseLockFromWaitPromise(_key);
                    previousWaiter = PendingAwaitSentinel.s_instance;
                    return null; // It doesn't matter what we return since previousWaiter is set to PendingAwaitSentinel.s_instance.
                }
            }
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal sealed partial class AsyncLockInternal
        {
            // These must not be readonly.
            private ValueLinkedQueue<IAsyncLockPromise> _waitPulseQueue = new ValueLinkedQueue<IAsyncLockPromise>();
            private ValueLinkedQueue<IAsyncLockPromise> _queue = new ValueLinkedQueue<IAsyncLockPromise>();
            private long _currentKey; // 0 if there is no lock held, otherwise the key of the lock holder.

            [MethodImpl(InlineOption)]
            private void SetNextKey()
            {
                _currentKey = KeyGenerator<AsyncLockInternal>.Next();
            }

            internal Promise<AsyncLock.Key> LockAsync(bool isSynchronous, CancelationToken cancelationToken)
            {
                if (cancelationToken.IsCancelationRequested)
                {
                    return Promise<AsyncLock.Key>.Canceled();
                }

                // Unfortunately, there is no way to detect async recursive lock enter. A deadlock will occur, instead of throw.
                PromiseRefBase.AsyncLockPromise promise;
                lock (this)
                {
                    ValidateNotAbandoned();

                    if (_currentKey == 0)
                    {
                        SetNextKey();
                        return Promise.Resolved(new AsyncLock.Key(this, _currentKey, null));
                    }

                    promise = PromiseRefBase.AsyncLockPromise.GetOrCreate(this, isSynchronous ? null : CaptureContext());
                    _queue.Enqueue(promise);
                    promise.MaybeHookupCancelation(cancelationToken);
                }
                return new Promise<AsyncLock.Key>(promise, promise.Id, 0);
            }

            internal bool TryEnter(out AsyncLock.Key key)
            {
                lock (this)
                {
                    ValidateNotAbandoned();

                    if (_currentKey == 0)
                    {
                        SetNextKey();
                        key = new AsyncLock.Key(this, _currentKey, null);
                        return true;
                    }
                }
                key = default;
                return false;
            }

            internal Promise<(bool didEnter, AsyncLock.Key key)> TryEnterAsync(CancelationToken cancelationToken)
            {
                PromiseRefBase.AsyncLockPromise promise;
                lock (this)
                {
                    ValidateNotAbandoned();

                    if (_currentKey == 0)
                    {
                        SetNextKey();
                        return Promise.Resolved((true, new AsyncLock.Key(this, _currentKey, null)));
                    }
                    // Quick check to see if the token is already canceled before waiting.
                    if (cancelationToken.IsCancelationRequested)
                    {
                        return Promise.Resolved((false, default(AsyncLock.Key)));
                    }

                    promise = PromiseRefBase.AsyncLockPromise.GetOrCreate(this, CaptureContext());
                    _queue.Enqueue(promise);
                    promise.MaybeHookupCancelation(cancelationToken);
                }
                return new Promise<AsyncLock.Key>(promise, promise.Id, 0)
                    .ContinueWith(resultContainer =>
                    {
                        resultContainer.RethrowIfRejected();
                        return (resultContainer.State == Promise.State.Resolved, resultContainer.Result);
                    });
            }

            internal bool TryEnter(out AsyncLock.Key key, CancelationToken cancelationToken)
            {
                PromiseRefBase.AsyncLockPromise promise;
                lock (this)
                {
                    ValidateNotAbandoned();

                    if (_currentKey == 0)
                    {
                        SetNextKey();
                        key = new AsyncLock.Key(this, _currentKey, null);
                        return true;
                    }
                    // Quick check to see if the token is already canceled before waiting.
                    if (cancelationToken.IsCancelationRequested)
                    {
                        key = default;
                        return false;
                    }

                    promise = PromiseRefBase.AsyncLockPromise.GetOrCreate(this, null);
                    _queue.Enqueue(promise);
                    promise.MaybeHookupCancelation(cancelationToken);
                }
                PromiseSynchronousWaiter.TryWaitForResult(promise, promise.Id, TimeSpan.FromMilliseconds(Timeout.Infinite), out var resultContainer);
                resultContainer.RethrowIfRejected();
                key = resultContainer.Result;
                return resultContainer.State == Promise.State.Resolved;
            }

            internal void ReleaseLock(long key)
            {
                IAsyncLockPromise next;
                lock (this)
                {
                    if (_currentKey != key)
                    {
                        ThrowInvalidKey(2);
                    }

                    // We keep the lock until there are no more waiters.
                    if (_queue.IsEmpty)
                    {
                        _currentKey = 0;
                        return;
                    }
                    SetNextKey();
                    next = _queue.Dequeue();
                }
                next.Resolve(ref _currentKey);
            }

            internal Promise WaitAsync(long key, SynchronizationContext callerContext)
            {
                PromiseRefBase.AsyncLockWaitPromise promise;
                lock (this)
                {
                    if (key != _currentKey)
                    {
                        ThrowInvalidKey(3);
                    }

                    promise = PromiseRefBase.AsyncLockWaitPromise.GetOrCreate(this, key, callerContext);
                    _waitPulseQueue.Enqueue(promise);
                }
                return new Promise(promise, promise.Id, 0);
            }

            internal Promise<bool> TryWaitAsync(long key, CancelationToken cancelationToken, SynchronizationContext callerContext)
            {
                PromiseRefBase.AsyncLockWaitPromise promise;
                lock (this)
                {
                    if (key != _currentKey)
                    {
                        ThrowInvalidKey(3);
                    }

                    promise = PromiseRefBase.AsyncLockWaitPromise.GetOrCreate(this, key, callerContext);
                    _waitPulseQueue.Enqueue(promise);
                    promise.MaybeHookupCancelation(cancelationToken);
                }
                return new Promise<bool>(promise, promise.Id, 0);
            }

            internal void ReleaseLockFromWaitPromise(long key)
            {
                // This is called when the WaitAsync promise has been awaited.
                IAsyncLockPromise next;
                lock (this)
                {
                    if (key != _currentKey)
                    {
                        throw new InvalidOperationException("AsyncMonitor.WaitAsync promise must be awaited while the lock is held.", GetFormattedStacktrace(5));
                    }

                    // We keep the lock until there are no more waiters.
                    if (_queue.IsEmpty)
                    {
                        _currentKey = 0;
                        return;
                    }
                    SetNextKey();
                    next = _queue.Dequeue();
                }
                next.Resolve(ref _currentKey);
            }

            internal void Pulse(long key)
            {
                lock (this)
                {
                    if (key != _currentKey)
                    {
                        ThrowInvalidKey(3);
                    }
                    if (!_waitPulseQueue.IsEmpty)
                    {
                        _queue.Enqueue(_waitPulseQueue.Dequeue());
                    }
                }
            }

            internal void PulseAll(long key)
            {
                lock (this)
                {
                    if (key != _currentKey)
                    {
                        ThrowInvalidKey(3);
                    }
                    _queue.TakeAndEnqueueElements(ref _waitPulseQueue);
                }
            }

            internal bool TryUnregister(PromiseRefBase.AsyncLockPromise promise)
            {
                lock (this)
                {
                    return _queue.TryRemove(promise);
                }
            }

            internal void MaybeMakeReady(PromiseRefBase.AsyncLockWaitPromise promise)
            {
                IAsyncLockPromise next;
                lock (this)
                {
                    if (_waitPulseQueue.TryRemove(promise))
                    {
                        _queue.Enqueue(promise);
                    }
                    if (_currentKey != 0 | _queue.IsEmpty)
                    {
                        return;
                    }
                    // The lock is not currently held, and there is at least 1 waiter attempting to take the lock, we need to resolve the next.
                    SetNextKey();
                    next = _queue.Dequeue();
                }
                next.Resolve(ref _currentKey);
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            internal static void ThrowInvalidKey(int skipFrames)
            {
                throw new InvalidOperationException("The AsyncLock.Key is invalid for this operation.", GetFormattedStacktrace(skipFrames + 1));
            }

            partial void ValidateNotAbandoned();
#if PROMISE_DEBUG
            volatile private string _abandonedMessage;

            partial void ValidateNotAbandoned()
            {
                if (_abandonedMessage != null)
                {
                    throw new AbandonedLockException(_abandonedMessage);
                }
            }

            internal void NotifyAbandoned(string abandonedMessage, ITraceable traceable)
            {
                ValueLinkedStack<IAsyncLockPromise> waitPulses;
                ValueLinkedStack<IAsyncLockPromise> queue;
                lock (this)
                {
                    if (_abandonedMessage != null)
                    {
                        // Additional abandoned keys could be caused by the first abandoned key, so do nothing.
                        return;
                    }
                    _abandonedMessage = abandonedMessage;

                    waitPulses = _waitPulseQueue.MoveElementsToStack();
                    queue = _queue.MoveElementsToStack();
                }

                var rejectContainer = CreateRejectContainer(new AbandonedLockException(abandonedMessage), int.MinValue, null, traceable);

                while (waitPulses.IsNotEmpty)
                {
                    waitPulses.Pop().Reject(rejectContainer);
                }
                while (queue.IsNotEmpty)
                {
                    queue.Pop().Reject(rejectContainer);
                }

                rejectContainer.ReportUnhandled();
            }
#endif // PROMISE_DEBUG
        } // class AsyncLockInternal
    }
#endif // UNITY_2021_2_OR_NEWER || NETSTANDARD2_1_OR_GREATER || NETCOREAPP

    namespace Threading
    {
#if UNITY_2021_2_OR_NEWER || NETSTANDARD2_1_OR_GREATER || NETCOREAPP
        partial class AsyncLock
        {
            // We wrap the impl with another class so that we can lock on it safely.
            private readonly Internal.AsyncLockInternal _impl = new Internal.AsyncLockInternal();

            [MethodImpl(Internal.InlineOption)]
            internal bool TryEnter(out Key key)
            {
                return _impl.TryEnter(out key);
            }

            [MethodImpl(Internal.InlineOption)]
            internal Promise<(bool didEnter, AsyncLock.Key key)> TryEnterAsync(CancelationToken cancelationToken)
            {
                return _impl.TryEnterAsync(cancelationToken);
            }

            [MethodImpl(Internal.InlineOption)]
            internal bool TryEnter(out Key key, CancelationToken cancelationToken)
            {
                return _impl.TryEnter(out key, cancelationToken);
            }

            // We check to make sure every key is disposed exactly once in DEBUG mode.
            // We don't verify this in RELEASE mode to avoid allocating on every lock enter.
#if PROMISE_DEBUG
            partial struct Key
            {
#if !PROTO_PROMISE_DEVELOPER_MODE
                [DebuggerNonUserCode, StackTraceHidden]
#endif
                private sealed class DisposedChecker : IDisposable, Internal.ITraceable
                {
                    public Internal.CausalityTrace Trace { get; set; }

                    internal readonly Internal.AsyncLockInternal _owner;
                    private int _isDisposedFlag;
                    private Internal.PromiseRefBase _waitPromise;

                    internal DisposedChecker(Internal.AsyncLockInternal owner, Internal.ITraceable traceable)
                    {
                        _owner = owner;
                        if (traceable == null)
                        {
                            Internal.SetCreatedStacktraceInternal(this, 3);
                        }
                        else
                        {
                            Trace = traceable.Trace;
                        }
                    }

                    ~DisposedChecker()
                    {
                        _owner.NotifyAbandoned("An AsyncLock.Key was never disposed.", this);
                    }

                    internal Promise<bool> TryWaitAsync(long key, CancelationToken cancelationToken, SynchronizationContext callerContext)
                    {
                        ValidateCall();

                        var promise = _owner.TryWaitAsync(key, cancelationToken, callerContext);
                        _waitPromise = promise._ref;
                        return promise
                            .Finally(this, _this => _this._waitPromise = null);
                    }

                    internal void ValidateCall()
                    {
                        if (_isDisposedFlag != 0 | _waitPromise != null)
                        {
                            throw _isDisposedFlag != 0
                                ? new ObjectDisposedException("AsyncLock.Key")
                                : new InvalidOperationException(
                                    "This AsyncLock.Key instance has a pending AsyncMonitor.Wait(Async) associated with it. No other operation is permitted on this instance until it is complete.",
                                    Internal.GetFormattedStacktrace(2));
                        }
                    }

                    public void Dispose()
                    {
                        _isDisposedFlag = 1;
                        GC.SuppressFinalize(this);
                    }
                }

                internal readonly Internal.AsyncLockInternal _owner;
                private readonly long _key;
                private readonly DisposedChecker _disposedChecker;

                internal Key(Internal.AsyncLockInternal owner, long key, Internal.ITraceable traceable)
                {
                    _owner = owner;
                    _key = key;
                    _disposedChecker = new DisposedChecker(owner, traceable);
                }

                internal Key(Internal.AsyncLockInternal owner)
                {
                    _owner = owner;
                    _key = 0;
                    _disposedChecker = null;
                }

                private void Release()
                {
                    var copy = this;
                    copy.ValidateOwnerAndDisposedChecker();
                    lock (copy._disposedChecker)
                    {
                        copy._disposedChecker.ValidateCall();
                        copy._owner.ReleaseLock(copy._key);
                        copy._disposedChecker.Dispose();
                    }
                }

                internal Promise WaitAsync()
                {
                    var copy = this;
                    copy.ValidateOwnerAndDisposedChecker();
                    lock (copy._disposedChecker)
                    {
                        return copy._disposedChecker.TryWaitAsync(copy._key, default, Internal.CaptureContext());
                    }
                }

                internal Promise<bool> TryWaitAsync(CancelationToken cancelationToken)
                {
                    var copy = this;
                    copy.ValidateOwnerAndDisposedChecker();
                    lock (copy._disposedChecker)
                    {
                        return copy._disposedChecker.TryWaitAsync(copy._key, cancelationToken, Internal.CaptureContext());
                    }
                }

                internal void Wait()
                {
                    var copy = this;
                    copy.ValidateOwnerAndDisposedChecker();
                    lock (copy._disposedChecker)
                    {
                        copy._disposedChecker.TryWaitAsync(copy._key, default, null).WaitForResult();
                    }
                }

                internal bool TryWait(CancelationToken cancelationToken)
                {
                    var copy = this;
                    copy.ValidateOwnerAndDisposedChecker();
                    lock (copy._disposedChecker)
                    {
                        return copy._disposedChecker.TryWaitAsync(copy._key, cancelationToken, null).WaitForResult();
                    }
                }

                internal void Pulse()
                {
                    var copy = this;
                    copy.ValidateOwnerAndDisposedChecker();
                    lock (copy._disposedChecker)
                    {
                        copy._disposedChecker.ValidateCall();
                        copy._owner.Pulse(copy._key);
                    }
                }

                internal void PulseAll()
                {
                    var copy = this;
                    copy.ValidateOwnerAndDisposedChecker();
                    lock (copy._disposedChecker)
                    {
                        copy._disposedChecker.ValidateCall();
                        copy._owner.PulseAll(copy._key);
                    }
                }

                private void ValidateOwnerAndDisposedChecker()
                {
                    var owner = _owner;
                    var disposedChecker = _disposedChecker;
                    if ((owner == null | disposedChecker == null) || owner != disposedChecker._owner)
                    {
                        Internal.AsyncLockInternal.ThrowInvalidKey(2);
                    }
                }
            }
#else // PROMISE_DEBUG
            partial struct Key
            {
                internal readonly Internal.AsyncLockInternal _owner;
                private readonly long _key;

                [MethodImpl(Internal.InlineOption)]
#pragma warning disable IDE0060 // Remove unused parameter
                internal Key(Internal.AsyncLockInternal owner, long key, Internal.ITraceable traceable)
#pragma warning restore IDE0060 // Remove unused parameter
                {
                    _owner = owner;
                    _key = key;
                }

                [MethodImpl(Internal.InlineOption)]
                internal Key(Internal.AsyncLockInternal owner)
                {
                    _owner = owner;
                    _key = 0;
                }

                [MethodImpl(Internal.InlineOption)]
                private void Release()
                {
                    ValidateAndGetOwner().ReleaseLock(_key);
                }

                internal Promise WaitAsync()
                {
                    return ValidateAndGetOwner().WaitAsync(_key, Internal.CaptureContext());
                }

                internal Promise<bool> TryWaitAsync(CancelationToken cancelationToken)
                {
                    return ValidateAndGetOwner().TryWaitAsync(_key, cancelationToken, Internal.CaptureContext());
                }

                internal void Wait()
                {
                    ValidateAndGetOwner().WaitAsync(_key, null).Wait();
                }

                internal bool TryWait(CancelationToken cancelationToken)
                {
                    return ValidateAndGetOwner().TryWaitAsync(_key, cancelationToken, null).WaitForResult();
                }

                internal void Pulse()
                {
                    ValidateAndGetOwner().Pulse(_key);
                }

                internal void PulseAll()
                {
                    ValidateAndGetOwner().PulseAll(_key);
                }

                private Internal.AsyncLockInternal ValidateAndGetOwner()
                {
                    var owner = _owner;
                    if (owner == null)
                    {
                        Internal.AsyncLockInternal.ThrowInvalidKey(2);
                    }
                    return owner;
                }
            }
#endif // PROMISE_DEBUG
        } // class AsyncLock
#endif // UNITY_2021_2_OR_NEWER || NETSTANDARD2_1_OR_GREATER || NETCOREAPP
    } // namespace Threading
} // namespace Proto.Promises