#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
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
            internal sealed class AsyncLockPromise : AsyncLockPromiseBase<AsyncLock.Key>
            {
                [MethodImpl(InlineOption)]
                private static AsyncLockPromise GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<AsyncLockPromise>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new AsyncLockPromise()
                        : obj.UnsafeAs<AsyncLockPromise>();
                }

                [MethodImpl(InlineOption)]
                internal static AsyncLockPromise GetOrCreate(AsyncLock owner, bool continueOnCapturedContext)
                {
                    var promise = GetOrCreate();
                    promise.Reset(continueOnCapturedContext);
                    promise._result = new AsyncLock.Key(owner); // This will be overwritten when this is resolved, we just store the owner here for cancelation.
                    return promise;
                }

                internal override void MaybeDispose()
                {
                    Dispose();
                    ObjectPool.MaybeRepool(this);
                }

                [MethodImpl(InlineOption)]
                internal void DisposeImmediate()
                {
                    PrepareEarlyDispose();
                    MaybeDispose();
                }

                public override void Resolve(ref long currentKey)
                {
                    ThrowIfInPool(this);

                    // We don't need to check if the unregister was successful or not.
                    // The fact that this was called means the cancelation was unable to unregister this from the lock.
                    // We just dispose to wait for the callback to complete before we continue.
                    _cancelationRegistration.Dispose();

                    _result = new AsyncLock.Key(_result._owner, currentKey, this);
                    Continue();
                }

                public override void Cancel()
                {
                    ThrowIfInPool(this);
                    if (!_result._owner.TryUnregister(this))
                    {
                        return;
                    }
                    _tempState = Promise.State.Canceled;
                    Continue();
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal sealed class AsyncLockWaitPromise : AsyncLockPromiseBase<bool>, IAsyncLockPromise
            {
#if PROMISE_DEBUG
                // We use a weak reference in DEBUG mode so the owner's finalizer can still run if it's dropped.
                private readonly WeakReference _ownerReference = new WeakReference(null, false);
#pragma warning disable IDE1006 // Naming Styles
                private AsyncConditionVariable _owner
#pragma warning restore IDE1006 // Naming Styles
                {
                    get => _ownerReference.Target as AsyncConditionVariable;
                    set => _ownerReference.Target = value;
                }
#else
                private AsyncConditionVariable _owner;
#endif
                private AsyncLock _lock;
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
                internal static AsyncLockWaitPromise GetOrCreate(AsyncConditionVariable owner, long key, bool continueOnCapturedContext)
                {
                    var promise = GetOrCreate();
                    promise.Reset(continueOnCapturedContext);
                    promise._owner = owner;
                    promise._lock = owner._lock;
                    promise._key = key;
                    promise._result = true;
                    return promise;
                }

                internal override void MaybeDispose()
                {
                    Dispose();
                    _owner = null;
                    _lock = null;
                    ObjectPool.MaybeRepool(this);
                }

                [MethodImpl(InlineOption)]
                internal void DisposeImmediate()
                {
                    PrepareEarlyDispose();
                    MaybeDispose();
                }

                public override void Resolve(ref long currentKey)
                {
                    ThrowIfInPool(this);

                    // Dispose to wait for the callback to complete before we continue.
                    _cancelationRegistration.Dispose();

                    currentKey = _key;
                    Continue();
                }

                public override void Cancel()
                {
                    ThrowIfInPool(this);
                    // We just move this to the ready queue, we don't post the continuation yet because we have to make sure the lock is re-acquired before this is resolved.
                    _result = false;
#if PROMISE_DEBUG
                    var _owner = this._owner;
                    if (_owner == null)
                    {
                        return;
                    }
#endif
                    _lock.MaybeMakeReady(_owner, this);
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
                    _lock.ReleaseLockFromWaitPromise(_key);
                    previousWaiter = PendingAwaitSentinel.s_instance;
                    return null; // It doesn't matter what we return since previousWaiter is set to PendingAwaitSentinel.s_instance.
                }

#if PROMISE_DEBUG
                void IAsyncLockPromise.Reject(IRejectContainer rejectContainer)
                {
                    _cancelationRegistration.Dispose();
                    RejectContainer = rejectContainer;
                    _tempState = Promise.State.Rejected;
                    // Notify the lock so this will be placed on the ready queue to re-acquire the lock before continuing.
                    _lock.NotifyAbandonedConditionVariable(this);
                }
#endif
            }
        }
    }
#endif // UNITY_2021_2_OR_NEWER || NETSTANDARD2_1_OR_GREATER || NETCOREAPP

    namespace Threading
    {
#if UNITY_2021_2_OR_NEWER || NETSTANDARD2_1_OR_GREATER || NETCOREAPP
        partial class AsyncLock
        {
            // These must not be readonly.
            private Internal.ValueLinkedQueue<Internal.IAsyncLockPromise> _queue = new Internal.ValueLinkedQueue<Internal.IAsyncLockPromise>();
            private long _currentKey; // 0 if there is no lock held, otherwise the key of the lock holder.
            private Internal.SpinLocker _locker = new Internal.SpinLocker();

            [MethodImpl(Internal.InlineOption)]
            private void SetNextKey()
                => _currentKey = Internal.KeyGenerator<AsyncLock>.Next();

            private Promise<Key> LockAsyncImpl(bool continueOnCapturedContext)
            {
                // We don't spinwait here because it's async; we want to return to caller as fast as possible.

                // Unfortunately, there is no way to detect async recursive lock enter. A deadlock will occur, instead of throw.
                Internal.PromiseRefBase.AsyncLockPromise promise;
                _locker.Enter();
                {
                    ValidateNotAbandoned();

                    if (_currentKey == 0)
                    {
                        SetNextKey();
                        _locker.Exit();
                        return Promise.Resolved(new Key(this, _currentKey, null));
                    }

                    promise = Internal.PromiseRefBase.AsyncLockPromise.GetOrCreate(this, continueOnCapturedContext);
                    _queue.Enqueue(promise);
                }
                _locker.Exit();
                return new Promise<Key>(promise, promise.Id);
            }

            private Promise<Key> LockAsyncImpl(CancelationToken cancelationToken, bool continueOnCapturedContext)
            {
                // We don't spinwait here because it's async; we want to return to caller as fast as possible.

                // Quick check to see if the token is already canceled before entering.
                if (cancelationToken.IsCancelationRequested)
                {
                    ValidateNotAbandoned();

                    return Promise<Key>.Canceled();
                }

                // Unfortunately, there is no way to detect async recursive lock enter. A deadlock will occur, instead of throw.
                Internal.PromiseRefBase.AsyncLockPromise promise;
                _locker.Enter();
                {
                    ValidateNotAbandoned();

                    if (_currentKey == 0)
                    {
                        SetNextKey();
                        _locker.Exit();
                        return Promise.Resolved(new Key(this, _currentKey, null));
                    }

                    promise = Internal.PromiseRefBase.AsyncLockPromise.GetOrCreate(this, continueOnCapturedContext);
                    if (promise.HookupAndGetIsCanceled(cancelationToken))
                    {
                        _locker.Exit();
                        promise.DisposeImmediate();
                        return Promise<Key>.Canceled();
                    }
                    _queue.Enqueue(promise);
                }
                _locker.Exit();
                return new Promise<Key>(promise, promise.Id);
            }

            private Key LockImpl()
            {
                // Since this is a synchronous lock, we do a short spinwait before entering the full lock.
                var spinner = new SpinWait();
                while (Volatile.Read(ref _currentKey) != 0 & !spinner.NextSpinWillYield)
                {
                    spinner.SpinOnce();
                }

                return LockAsyncImpl(false).WaitForResult();
            }

            private Key LockImpl(CancelationToken cancelationToken)
            {
                // Because this is a synchronous wait, we do a short spinwait before yielding the thread.
                var spinner = new SpinWait();
                while (Volatile.Read(ref _currentKey) != 0 & !spinner.NextSpinWillYield & !cancelationToken.IsCancelationRequested)
                {
                    spinner.SpinOnce();
                }

                return LockAsyncImpl(cancelationToken, false).WaitForResult();
            }

            internal bool TryEnterImpl(out Key key)
            {
                _locker.Enter();
                {
                    ValidateNotAbandoned();

                    if (_currentKey == 0)
                    {
                        SetNextKey();
                        _locker.Exit();
                        key = new Key(this, _currentKey, null);
                        return true;
                    }
                }
                _locker.Exit();
                key = default;
                return false;
            }

            internal Promise<(bool didEnter, Key key)> TryEnterAsyncImpl(CancelationToken cancelationToken, bool continueOnCapturedContext)
            {
                Internal.PromiseRefBase.AsyncLockPromise promise;
                _locker.Enter();
                {
                    ValidateNotAbandoned();

                    if (_currentKey == 0)
                    {
                        SetNextKey();
                        _locker.Exit();
                        return Promise.Resolved((true, new Key(this, _currentKey, null)));
                    }
                    // Quick check to see if the token is already canceled before waiting.
                    if (cancelationToken.IsCancelationRequested)
                    {
                        _locker.Exit();
                        return Promise.Resolved((false, default(Key)));
                    }

                    promise = Internal.PromiseRefBase.AsyncLockPromise.GetOrCreate(this, continueOnCapturedContext);
                    if (promise.HookupAndGetIsCanceled(cancelationToken))
                    {
                        _locker.Exit();
                        promise.DisposeImmediate();
                        return Promise.Resolved((false, default(Key)));
                    }
                    _queue.Enqueue(promise);
                }
                _locker.Exit();
                return new Promise<Key>(promise, promise.Id)
                    .ContinueWith(resultContainer =>
                    {
                        resultContainer.RethrowIfRejected();
                        return (resultContainer.State == Promise.State.Resolved, resultContainer.Value);
                    });
            }

            internal bool TryEnterImpl(out Key key, CancelationToken cancelationToken)
            {
                // Because this is a synchronous wait, we do a short spinwait before yielding the thread.
                var spinner = new SpinWait();
                while (Volatile.Read(ref _currentKey) != 0 & !spinner.NextSpinWillYield & !cancelationToken.IsCancelationRequested)
                {
                    spinner.SpinOnce();
                }

                var (success, k) = TryEnterAsyncImpl(cancelationToken, false).WaitForResult();
                key = k;
                return success;
            }

            private void ReleaseLock(long key)
            {
                Internal.IAsyncLockPromise next;
                _locker.Enter();
                {
                    if (_currentKey != key)
                    {
                        _locker.Exit();
                        ThrowInvalidKey(2);
                    }

                    // We keep the lock until there are no more waiters.
                    if (_queue.IsEmpty)
                    {
                        _currentKey = 0;
                        _locker.Exit();
                        return;
                    }
                    SetNextKey();
                    next = _queue.Dequeue();
                }
                _locker.Exit();
                next.Resolve(ref _currentKey);
            }

            private Promise WaitAsyncImpl(AsyncConditionVariable condVar, long key, bool continueOnCapturedContext)
            {
                Internal.PromiseRefBase.AsyncLockWaitPromise promise;
                _locker.Enter();
                {
                    if (key != _currentKey)
                    {
                        ThrowInvalidKey(3);
                    }
                    // We set this as the current lock using the condition variable, and validate that it's not currently being used by another lock.
                    // This allows a condition variable to be used by multiple locks, as long as only 1 is using it at a time.
                    var previousCondVarOwner = Interlocked.CompareExchange(ref condVar._lock, this, null);
                    if (previousCondVarOwner != null & previousCondVarOwner != this)
                    {
                        _locker.Exit();
                        ThrowConditionVariableAlreadyInUse(3);
                    }

                    promise = Internal.PromiseRefBase.AsyncLockWaitPromise.GetOrCreate(condVar, key, continueOnCapturedContext);
                    condVar._queue.Enqueue(promise);
                }
                _locker.Exit();
                return new Promise(promise, promise.Id);
            }

            private Promise<bool> TryWaitAsyncImpl(AsyncConditionVariable condVar, long key, CancelationToken cancelationToken, bool continueOnCapturedContext)
            {
                Internal.PromiseRefBase.AsyncLockWaitPromise promise;
                _locker.Enter();
                {
                    if (key != _currentKey)
                    {
                        _locker.Exit();
                        ThrowInvalidKey(3);
                    }
                    // We set this as the current lock using the condition variable, and validate that it's not currently being used by another lock.
                    // This allows a condition variable to be used by multiple locks, as long as only 1 is using it at a time.
                    var previousCondVarOwner = Interlocked.CompareExchange(ref condVar._lock, this, null);
                    if (previousCondVarOwner != null & previousCondVarOwner != this)
                    {
                        _locker.Exit();
                        ThrowConditionVariableAlreadyInUse(3);
                    }

                    // Quick check to see if the token is already canceled before waiting.
                    if (cancelationToken.IsCancelationRequested)
                    {
                        if (condVar._queue.IsEmpty)
                        {
                            // Remove this association from the condition variable so that another AsyncLock can use it.
                            condVar._lock = null;
                        }
                        _locker.Exit();
                        return Promise.Resolved(false);
                    }

                    promise = Internal.PromiseRefBase.AsyncLockWaitPromise.GetOrCreate(condVar, key, continueOnCapturedContext);
                    if (promise.HookupAndGetIsCanceled(cancelationToken))
                    {
                        if (condVar._queue.IsEmpty)
                        {
                            // Remove this association from the condition variable so that another AsyncLock can use it.
                            condVar._lock = null;
                        }
                        _locker.Exit();
                        promise.DisposeImmediate();
                        return Promise.Resolved(false);
                    }
                    condVar._queue.Enqueue(promise);
                }
                _locker.Exit();
                return new Promise<bool>(promise, promise.Id);
            }

            internal void ReleaseLockFromWaitPromise(long key)
            {
                // This is called when the WaitAsync promise has been awaited.
                Internal.IAsyncLockPromise next;
                _locker.Enter();
                {
                    if (key != _currentKey)
                    {
                        _locker.Exit();
                        throw new InvalidOperationException("WaitAsync promise must be awaited while the lock is held.", Internal.GetFormattedStacktrace(5));
                    }

                    // We keep the lock until there are no more waiters.
                    if (_queue.IsEmpty)
                    {
                        _currentKey = 0;
                        _locker.Exit();
                        return;
                    }
                    SetNextKey();
                    next = _queue.Dequeue();
                }
                _locker.Exit();
                next.Resolve(ref _currentKey);
            }

            internal void Pulse(AsyncConditionVariable condVar, long key)
            {
                _locker.Enter();
                {
                    if (key != _currentKey)
                    {
                        _locker.Exit();
                        ThrowInvalidKey(3);
                    }
                    // We set this as the current lock using the condition variable, and validate that it's not currently being used by another lock.
                    // This allows a condition variable to be used by multiple locks, as long as only 1 is using it at a time.
                    var previousCondVarOwner = Interlocked.CompareExchange(ref condVar._lock, this, null);
                    if (previousCondVarOwner != null & previousCondVarOwner != this)
                    {
                        _locker.Exit();
                        ThrowConditionVariableAlreadyInUse(3);
                    }

                    if (condVar._queue.IsEmpty)
                    {
                        // Remove this association from the condition variable so that another AsyncLock can use it.
                        condVar._lock = null;
                        _locker.Exit();
                        return;
                    }

                    _queue.Enqueue(condVar._queue.Dequeue());
                    if (condVar._queue.IsEmpty)
                    {
                        // Remove this association from the condition variable so that another AsyncLock can use it.
                        condVar._lock = null;
                    }
                }
                _locker.Exit();
            }

            internal void PulseAll(AsyncConditionVariable condVar, long key)
            {
                _locker.Enter();
                {
                    if (key != _currentKey)
                    {
                        _locker.Exit();
                        ThrowInvalidKey(3);
                    }
                    // We set this as the current lock using the condition variable, and validate that it's not currently being used by another lock.
                    // This allows a condition variable to be used by multiple locks, as long as only 1 is using it at a time.
                    var previousCondVarOwner = Interlocked.CompareExchange(ref condVar._lock, this, null);
                    if (previousCondVarOwner != null & previousCondVarOwner != this)
                    {
                        _locker.Exit();
                        ThrowConditionVariableAlreadyInUse(3);
                    }
                    _queue.TakeAndEnqueueElements(ref condVar._queue);
                    // Remove this association from the condition variable so that another AsyncLock can use it.
                    condVar._lock = null;
                }
                _locker.Exit();
            }

            internal bool TryUnregister(Internal.PromiseRefBase.AsyncLockPromise promise)
            {
                _locker.Enter();
                var removed = _queue.TryRemove(promise);
                _locker.Exit();
                return removed;
            }

            internal void MaybeMakeReady(AsyncConditionVariable condVar, Internal.PromiseRefBase.AsyncLockWaitPromise promise)
            {
                // This is called when a WaitAsync promise is canceled.
                // We have to remove it from the condition variable queue and add it to the lock queue,
                // so that the lock will be re-acquired before it continues.
                Internal.IAsyncLockPromise next;
                _locker.Enter();
                {
                    if (!condVar._queue.TryRemove(promise))
                    {
                        _locker.Exit();
                        return;
                    }
                    _queue.Enqueue(promise);
                    if (condVar._queue.IsEmpty)
                    {
                        // Remove this association from the condition variable so that another AsyncLock can use it.
                        condVar._lock = null;
                        _locker.Exit();
                    }
                    if (_currentKey != 0)
                    {
                        _locker.Exit();
                        return;
                    }
                    // The lock is not currently held, and there is at least 1 waiter attempting to take the lock, we need to resolve the next.
                    SetNextKey();
                    next = _queue.Dequeue();
                }
                _locker.Exit();
                next.Resolve(ref _currentKey);
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            internal static void ThrowInvalidKey(int skipFrames)
                => throw new InvalidOperationException("The AsyncLock.Key is invalid for this operation.", Internal.GetFormattedStacktrace(skipFrames + 1));

            [MethodImpl(MethodImplOptions.NoInlining)]
            internal static void ThrowConditionVariableAlreadyInUse(int skipFrames)
                => throw new InvalidOperationException("The AsyncConditionVariable is currently being used by another AsyncLock.", Internal.GetFormattedStacktrace(skipFrames + 1));

            partial void ValidateNotAbandoned();
#if PROMISE_DEBUG
            volatile private string _abandonedMessage;
            volatile internal Internal.IRejectContainer _abandonedRejection;

            partial void ValidateNotAbandoned()
            {
                if (_abandonedMessage != null)
                {
                    _locker.Exit();
                    throw new AbandonedLockException(_abandonedMessage);
                }
            }

            internal void NotifyAbandoned(string abandonedMessage, Internal.ITraceable traceable)
            {
                Internal.ValueLinkedStack<Internal.IAsyncLockPromise> queue;
                _locker.Enter();
                {
                    if (_abandonedMessage != null)
                    {
                        // Additional abandoned keys could be caused by the first abandoned key, so do nothing.
                        _locker.Exit();
                        return;
                    }
                    _abandonedMessage = abandonedMessage;
                    _abandonedRejection = Internal.CreateRejectContainer(new AbandonedLockException(abandonedMessage), int.MinValue, null, traceable);
                    queue = _queue.MoveElementsToStack();
                }
                _locker.Exit();
                while (queue.IsNotEmpty)
                {
                    queue.Pop().Reject(_abandonedRejection);
                }
                _abandonedRejection.ReportUnhandled();
            }

            internal void NotifyAbandonedConditionVariable(Internal.PromiseRefBase.AsyncLockWaitPromise promise)
            {
                // This is called when a WaitAsync promise is rejected due to the AsyncConditionVariable being collected without being notified.
                // We have to add it to the lock queue, so that the lock will be re-acquired before it continues.
                Internal.IAsyncLockPromise next;
                _locker.Enter();
                {
                    _queue.Enqueue(promise);
                    if (_currentKey != 0)
                    {
                        _locker.Exit();
                        return;
                    }
                    // The lock is not currently held, and there is at least 1 waiter attempting to take the lock, we need to resolve the next.
                    SetNextKey();
                    next = _queue.Dequeue();
                }
                _locker.Exit();
                next.Resolve(ref _currentKey);
            }
#endif // PROMISE_DEBUG

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

                    internal readonly AsyncLock _owner;
                    private int _isDisposedFlag;
                    private Internal.PromiseRefBase _waitPromise;

                    internal DisposedChecker(AsyncLock owner, Internal.ITraceable traceable)
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

                    internal Promise<bool> TryWaitAsync(AsyncConditionVariable condVar, long key, CancelationToken cancelationToken, bool continueOnCapturedContext)
                    {
                        ValidateCall();

                        var promise = _owner.TryWaitAsyncImpl(condVar, key, cancelationToken, continueOnCapturedContext);
                        _waitPromise = promise._ref;
                        return promise
                            .Finally(this, _this => _this._waitPromise = null);
                    }

                    internal void ValidateCall()
                    {
                        if (_isDisposedFlag != 0 | _waitPromise != null)
                        {
                            if (_isDisposedFlag != 0)
                            {
                                throw new ObjectDisposedException("AsyncLock.Key");
                            }
                            throw new InvalidOperationException(
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

                internal readonly AsyncLock _owner;
                private readonly long _key;
                private readonly DisposedChecker _disposedChecker;

                internal Key(AsyncLock owner, long key, Internal.ITraceable traceable)
                {
                    _owner = owner;
                    _key = key;
                    _disposedChecker = new DisposedChecker(owner, traceable);
                }

                internal Key(AsyncLock owner)
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

                internal Promise WaitAsync(AsyncConditionVariable condVar, bool continueOnCapturedContext)
                {
                    var copy = this;
                    copy.ValidateOwnerAndDisposedChecker();
                    lock (copy._disposedChecker)
                    {
                        return copy._disposedChecker.TryWaitAsync(condVar, copy._key, default, continueOnCapturedContext);
                    }
                }

                internal Promise<bool> TryWaitAsync(AsyncConditionVariable condVar, CancelationToken cancelationToken, bool continueOnCapturedContext)
                {
                    var copy = this;
                    copy.ValidateOwnerAndDisposedChecker();
                    lock (copy._disposedChecker)
                    {
                        return copy._disposedChecker.TryWaitAsync(condVar, copy._key, cancelationToken, continueOnCapturedContext);
                    }
                }

                internal void Wait(AsyncConditionVariable condVar)
                {
                    var copy = this;
                    copy.ValidateOwnerAndDisposedChecker();
                    lock (copy._disposedChecker)
                    {
                        copy._disposedChecker.TryWaitAsync(condVar, copy._key, default, false).WaitForResult();
                    }
                }

                internal bool TryWait(AsyncConditionVariable condVar, CancelationToken cancelationToken)
                {
                    var copy = this;
                    copy.ValidateOwnerAndDisposedChecker();
                    lock (copy._disposedChecker)
                    {
                        return copy._disposedChecker.TryWaitAsync(condVar, copy._key, cancelationToken, false).WaitForResult();
                    }
                }

                internal void Pulse(AsyncConditionVariable condVar)
                {
                    var copy = this;
                    copy.ValidateOwnerAndDisposedChecker();
                    lock (copy._disposedChecker)
                    {
                        copy._disposedChecker.ValidateCall();
                        copy._owner.Pulse(condVar, copy._key);
                    }
                }

                internal void PulseAll(AsyncConditionVariable condVar)
                {
                    var copy = this;
                    copy.ValidateOwnerAndDisposedChecker();
                    lock (copy._disposedChecker)
                    {
                        copy._disposedChecker.ValidateCall();
                        copy._owner.PulseAll(condVar, copy._key);
                    }
                }

                private void ValidateOwnerAndDisposedChecker()
                {
                    var owner = _owner;
                    var disposedChecker = _disposedChecker;
                    if ((owner == null | disposedChecker == null) || owner != disposedChecker._owner)
                    {
                        ThrowInvalidKey(2);
                    }
                }
            }
#else // PROMISE_DEBUG
            partial struct Key
            {
                internal readonly AsyncLock _owner;
                private readonly long _key;

                [MethodImpl(Internal.InlineOption)]
#pragma warning disable IDE0060 // Remove unused parameter
                internal Key(AsyncLock owner, long key, Internal.ITraceable traceable)
#pragma warning restore IDE0060 // Remove unused parameter
                {
                    _owner = owner;
                    _key = key;
                }

                [MethodImpl(Internal.InlineOption)]
                internal Key(AsyncLock owner)
                {
                    _owner = owner;
                    _key = 0;
                }

                [MethodImpl(Internal.InlineOption)]
                private void Release()
                {
                    ValidateAndGetOwner().ReleaseLock(_key);
                }

                internal Promise WaitAsync(AsyncConditionVariable condVar, bool continueOnCapturedContext)
                    => ValidateAndGetOwner().WaitAsyncImpl(condVar, _key, continueOnCapturedContext);

                internal Promise<bool> TryWaitAsync(AsyncConditionVariable condVar, CancelationToken cancelationToken, bool continueOnCapturedContext)
                    => ValidateAndGetOwner().TryWaitAsyncImpl(condVar, _key, cancelationToken, continueOnCapturedContext);

                internal void Wait(AsyncConditionVariable condVar)
                    => ValidateAndGetOwner().WaitAsyncImpl(condVar, _key, false).Wait();

                internal bool TryWait(AsyncConditionVariable condVar, CancelationToken cancelationToken)
                    => ValidateAndGetOwner().TryWaitAsyncImpl(condVar, _key, cancelationToken, false).WaitForResult();

                internal void Pulse(AsyncConditionVariable condVar)
                    => ValidateAndGetOwner().Pulse(condVar, _key);

                internal void PulseAll(AsyncConditionVariable condVar)
                    => ValidateAndGetOwner().PulseAll(condVar, _key);

                private AsyncLock ValidateAndGetOwner()
                {
                    var owner = _owner;
                    if (owner == null)
                    {
                        ThrowInvalidKey(2);
                    }
                    return owner;
                }
            }
#endif // PROMISE_DEBUG
        } // class AsyncLock
#endif // UNITY_2021_2_OR_NEWER || NETSTANDARD2_1_OR_GREATER || NETCOREAPP
    } // namespace Threading
} // namespace Proto.Promises