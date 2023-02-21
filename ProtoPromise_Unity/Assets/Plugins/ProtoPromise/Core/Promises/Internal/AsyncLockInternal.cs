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

#pragma warning disable IDE0031 // Use null propagation
#pragma warning disable IDE0034 // Simplify 'default' expression
#pragma warning disable IDE0090 // Use 'new(...)'

using Proto.Promises.Threading;
using System.Collections.Generic;
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
            long KeyForDebug { get; }
#endif
        }

        partial class PromiseRefBase
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal sealed class AsyncLockPromise : PromiseSingleAwait<AsyncLock.Key>, IAsyncLockPromise, ICancelable
            {
                // We post continuations to the caller's context to prevent blocking the thread that released the lock (and to avoid StackOverflowException).
                private SynchronizationContext _callerContext;
                private CancelationRegistration _cancelationRegistration;
                // We have to store the previous state in a separate field until the next awaiter is ready to be invoked on the proper context.
                private Promise.State _tempState;
                internal AsyncLockInternal Owner { get { return _result._owner; } }
                IAsyncLockPromise ILinked<IAsyncLockPromise>.Next { get; set; }

#if PROMISE_DEBUG
                long IAsyncLockPromise.KeyForDebug { get { return 0; } }
#endif

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
                    promise._result = new AsyncLock.Key(owner, 0); // This will be overwritten when this is resolved, we just store the owner here for cancelation.
                    return promise;
                }

                internal override void MaybeDispose()
                {
                    Dispose();
                    _callerContext = null;
                    _cancelationRegistration = default(CancelationRegistration);
                    ObjectPool.MaybeRepool(this);
                }

                internal void MaybeHookupCancelation(CancelationToken cancelationToken)
                {
                    ThrowIfInPool(this);
                    if (cancelationToken.IsCancelationRequested)
                    {
                        if (Owner.TryUnregister(this))
                        {
                            // We know no continuations have been hooked up at this point, so we can just set the canceled state without worrying about handling the next waiter.
                            // This is equivalent to `HandleNextInternal(null, Promise.State.Canceled)`, but without the extra branches.
                            _next = PromiseCompletionSentinel.s_instance;
                            SetCompletionState(null, Promise.State.Canceled);
                        }
                        return;
                    }
                    cancelationToken.TryRegister(this, out _cancelationRegistration);
                }

                private void Continue(Promise.State state)
                {
                    if (_callerContext == null)
                    {
                        // It was a synchronous lock, handle next continuation synchronously so that the PromiseSynchronousWaiter will be pulsed to wake the waiting thread.
                        HandleNextInternal(null, state);
                        return;
                    }
                    // Post the continuation to the caller's context. This prevents blocking the current thread and avoids StackOverflowException.
                    _tempState = state;
                    ScheduleForHandle(this, _callerContext);
                }

                internal override void HandleFromContext()
                {
                    HandleNextInternal(null, _tempState);
                }

                public void Resolve(ref long currentKey)
                {
                    ThrowIfInPool(this);

                    // We don't need to check if the unregister was successful or not.
                    // The fact that this was called means the cancelation was unable to unregister this from the lock.
                    // We just dispose to wait for the callback to complete before we continue.
                    _cancelationRegistration.Dispose();

                    _result = new AsyncLock.Key(Owner, currentKey);
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

                internal override void Handle(PromiseRefBase handler, object rejectContainer, Promise.State state) { throw new System.InvalidOperationException(); }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal sealed class AsyncLockWaitPromise : PromiseSingleAwait<bool>, IAsyncLockPromise, ICancelable
            {
                private AsyncLockInternal _owner;
                private long _key;
                // We post continuations to the caller's context to prevent blocking the thread that released the lock (and to avoid StackOverflowException).
                private SynchronizationContext _callerContext;
                private CancelationRegistration _cancelationRegistration;
                IAsyncLockPromise ILinked<IAsyncLockPromise>.Next { get; set; }

#if PROMISE_DEBUG
                long IAsyncLockPromise.KeyForDebug { get { return _key; } }

                internal void RejectFromEarlyLockRelease(InvalidOperationException exception)
                {
                    // This can only happen with an AsyncMonitor.WaitAsync call. Synchronous waits cannot be misused.
                    ThrowIfInPool(this);
                    _cancelationRegistration.Dispose();
                    var rejectContainer = CreateRejectContainer(exception, int.MinValue, null, this);
                    Promise.Run(() => HandleNextInternal(rejectContainer, Promise.State.Rejected), _callerContext, forceAsync: true)
                        .Forget();
                }
#endif

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
                    _callerContext = null;
                    _cancelationRegistration = default(CancelationRegistration);
                    ObjectPool.MaybeRepool(this);
                }

                internal void MaybeHookupCancelation(CancelationToken cancelationToken)
                {
                    ThrowIfInPool(this);
                    // Just hook up the listener, don't check for already canceled since we have to make sure the lock is re-acquired before this is resolved.
                    cancelationToken.TryRegister(this, out _cancelationRegistration);
                }

                public void Resolve(ref long currentKey)
                {
                    ThrowIfInPool(this);

                    // Dispose to wait for the callback to complete before we continue.
                    _cancelationRegistration.Dispose();

                    currentKey = _key;
                    if (_callerContext == null)
                    {
                        // It was a synchronous wait, handle next continuation synchronously so that the PromiseSynchronousWaiter will be pulsed to wake the waiting thread.
                        HandleNextInternal(null, Promise.State.Resolved);
                        return;
                    }
                    // Post the continuation to the caller's context. This prevents blocking the current thread and avoids StackOverflowException.
                    ScheduleForHandle(this, _callerContext);
                }

                internal override void HandleFromContext()
                {
                    // We only continue with resolved state here. If this was rejected due to mis-use, the rejection is posted via Promise.Run.
                    HandleNextInternal(null, Promise.State.Resolved);
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

                internal override void Handle(PromiseRefBase handler, object rejectContainer, Promise.State state) { throw new System.InvalidOperationException(); }
            }
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal sealed partial class AsyncLockInternal : ITraceable
        {
            // We use a shared key generator to prevent false positives if a Key is torn.
            private static long s_keyGenerator;

            // These must not be readonly.
            private ValueLinkedQueue<IAsyncLockPromise> _waitPulseQueue = new ValueLinkedQueue<IAsyncLockPromise>();
            private ValueLinkedQueue<IAsyncLockPromise> _queue = new ValueLinkedQueue<IAsyncLockPromise>();
            private long _currentKey;

            partial void SetTrace(ITraceable traceable);
            partial void AddWaitPulseKey(long key, int skipFrames);
            partial void RemoveWaitPulseKey(IAsyncLockPromise promise);
            partial void ValidateKeyRelease(long key, int skipFrames);
#if PROMISE_DEBUG
            // This is to help debug where a Key was left undisposed.
            private CausalityTrace _trace;
            CausalityTrace ITraceable.Trace
            {
                get { return _trace; }
                set { _trace = value; }
            }

            partial void SetTrace(ITraceable traceable)
            {
                _trace = traceable == null ? null : traceable.Trace;
            }

            // This is used to tell if a key was disposed before all wait promises were resolved.
            private readonly HashSet<long> _waitPulseKeys = new HashSet<long>();

            partial void AddWaitPulseKey(long key, int skipFrames)
            {
                if (!_waitPulseKeys.Add(key))
                {
                    throw new InvalidOperationException("Cannot AsyncMonitor.Wait(Async) while the previous AsyncMonitor.WaitAsync has not completed.", GetFormattedStacktrace(skipFrames + 1));
                }
            }

            partial void RemoveWaitPulseKey(IAsyncLockPromise promise)
            {
                long key = promise.KeyForDebug;
                if (key != 0 && !_waitPulseKeys.Remove(key))
                {
                    // This should never happen.
                    throw new System.InvalidOperationException("Could not remove key: " + key);
                }
            }

            partial void ValidateKeyRelease(long key, int skipFrames)
            {
                if (!_waitPulseKeys.Contains(key))
                {
                    return;
                }

                // We make sure there can only be 1 wait promise per key at a time, validated in AddWaitPulseKey, so we only need to remove 1 when it's found.
                IAsyncLockPromise matchedPromise = null;
                foreach (var promise in _waitPulseQueue)
                {
                    if (promise.KeyForDebug == key)
                    {
                        matchedPromise = promise;
                        break;
                    }
                }
                if (matchedPromise != null)
                {
                    _waitPulseQueue.TryRemove(matchedPromise);
                }
                else
                {
                    foreach (var promise in _queue)
                    {
                        if (promise.KeyForDebug == key)
                        {
                            matchedPromise = promise;
                            break;
                        }
                    }
                    _queue.TryRemove(matchedPromise);
                }
                var exception = new InvalidOperationException("AsyncLock.Key was disposed before an AsyncMonitor.WaitAsync promise was complete.", GetFormattedStacktrace(skipFrames + 1));
                ((PromiseRefBase.AsyncLockWaitPromise) matchedPromise).RejectFromEarlyLockRelease(exception);
                throw exception;
            }
#endif

            ~AsyncLockInternal()
            {
                if (_currentKey != 0)
                {
                    ReportRejection(new UnreleasedObjectException("An AsyncLock.Key was not disposed. Any code waiting on the AsyncLock will never continue."), this);
                }
            }

            private static long GenerateKey()
            {
                // We don't check for overflow to let the key generator wrap around for infinite re-use.
                long newKey = Interlocked.Increment(ref s_keyGenerator);
                return newKey != 0L ? newKey : Interlocked.Increment(ref s_keyGenerator); // Don't allow 0 key.
            }

            private static SynchronizationContext CaptureContext()
            {
                // We capture the current context to post the continuation. If it's null, we use the background context.
                return ts_currentContext
                    ?? SynchronizationContext.Current
                    ?? Promise.Config.BackgroundContext
                    ?? BackgroundSynchronizationContextSentinel.s_instance;
            }

            internal Promise<AsyncLock.Key> LockAsync(bool isSynchronous, CancelationToken cancelationToken)
            {
                // Unfortunately, there is no way to detect async recursive lock enter. A deadlock will occur, instead of throw.
                PromiseRefBase.AsyncLockPromise promise;
                lock (this)
                {
                    if (_currentKey == 0)
                    {
                        _currentKey = GenerateKey();
                        SetCreatedStacktraceInternal(this, 2);
                        return Promise.Resolved(new AsyncLock.Key(this, _currentKey));
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
                    if (_currentKey == 0)
                    {
                        _currentKey = GenerateKey();
                        SetCreatedStacktraceInternal(this, 2);
                        key = new AsyncLock.Key(this, _currentKey);
                        return true;
                    }
                }
                key = default(AsyncLock.Key);
                return false;
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
                    ValidateKeyRelease(key, 2);

                    // We keep the lock until there are no more waiters.
                    if (_queue.IsEmpty)
                    {
                        _currentKey = 0;
                        SetTrace(null);
                        return;
                    }
                    _currentKey = GenerateKey();
                    next = _queue.Dequeue();

                    SetTrace(next);
                    RemoveWaitPulseKey(next);
                }
                next.Resolve(ref _currentKey);
            }

            internal Promise<bool> WaitAsync(long key, bool isSynchronous, CancelationToken cancelationToken)
            {
                PromiseRefBase.AsyncLockWaitPromise promise;
                lock (this)
                {
                    if (key != _currentKey)
                    {
                        ThrowInvalidKey(3);
                    }
                    AddWaitPulseKey(key, 3);

                    promise = PromiseRefBase.AsyncLockWaitPromise.GetOrCreate(this, key, isSynchronous ? null : CaptureContext());
                    _waitPulseQueue.Enqueue(promise);
                    promise.MaybeHookupCancelation(cancelationToken);
                    // We don't release the lock until the promise has been awaited.
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
                    _currentKey = GenerateKey();
                    next = _queue.Dequeue();

                    SetTrace(next);
                    RemoveWaitPulseKey(next);
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
                    _currentKey = GenerateKey();
                    next = _queue.Dequeue();

                    SetTrace(next);
                    RemoveWaitPulseKey(next);
                }
                next.Resolve(ref _currentKey);
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            internal static void ThrowInvalidKey(int skipFrames)
            {
                throw new InvalidOperationException("The AsyncLock.Key is invalid for this operation.", GetFormattedStacktrace(skipFrames + 1));
            }
        } // class AsyncLockInternal
    }
#endif // UNITY_2021_2_OR_NEWER || NETSTANDARD2_1_OR_GREATER || NETCOREAPP

    namespace Threading
    {
#if UNITY_2021_2_OR_NEWER || NETSTANDARD2_1_OR_GREATER || NETCOREAPP
        partial class AsyncLock
        {
            // We wrap the impl with another class so that we can lock on it safely.
            private readonly Internal.AsyncLockInternal _locker = new Internal.AsyncLockInternal();

            [MethodImpl(Internal.InlineOption)]
            internal bool TryEnter(out Key key)
            {
                return _locker.TryEnter(out key);
            }

            partial struct Key
            {
                internal readonly Internal.AsyncLockInternal _owner;
                private readonly long _key;

                [MethodImpl(Internal.InlineOption)]
                internal Key(Internal.AsyncLockInternal owner, long key)
                {
                    _owner = owner;
                    _key = key;
                }

                internal Promise<bool> WaitAsync(CancelationToken cancelationToken = default(CancelationToken))
                {
                    return ValidateAndGetOwner().WaitAsync(_key, false, cancelationToken);
                }

                internal bool Wait(CancelationToken cancelationToken = default(CancelationToken))
                {
                    return ValidateAndGetOwner().WaitAsync(_key, true, cancelationToken).WaitForResult();
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
        } // class AsyncLock
#endif // UNITY_2021_2_OR_NEWER || NETSTANDARD2_1_OR_GREATER || NETCOREAPP
    } // namespace Threading
} // namespace Proto.Promises