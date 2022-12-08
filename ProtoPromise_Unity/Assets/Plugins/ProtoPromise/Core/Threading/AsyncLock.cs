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

#pragma warning disable IDE0034 // Simplify 'default' expression
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
        internal interface IAsyncLockPromise : ILinked<IAsyncLockPromise>, ITraceable
        {
            void Resolve(ref long currentKey);
        }

        partial class PromiseRefBase
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal sealed class AsyncLockPromise : PromiseSingleAwait<Threading.AsyncLock.Key>, IAsyncLockPromise, ICancelable
            {
                // We post continuations to the caller's context to prevent blocking the thread that released the lock (and to avoid StackOverflowException).
                private SynchronizationContext _callerContext;
                private CancelationRegistration _cancelationRegistration;
                internal Threading.AsyncLock Owner { get { return _result._owner; } }
                IAsyncLockPromise ILinked<IAsyncLockPromise>.Next { get; set; }

                [MethodImpl(InlineOption)]
                private static AsyncLockPromise GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<AsyncLockPromise>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new AsyncLockPromise()
                        : obj.UnsafeAs<AsyncLockPromise>();
                }

                [MethodImpl(InlineOption)]
                internal static AsyncLockPromise GetOrCreate(Threading.AsyncLock owner, SynchronizationContext callerContext)
                {
                    var promise = GetOrCreate();
                    promise.Reset();
                    promise._callerContext = callerContext;
                    promise._result = new Threading.AsyncLock.Key(owner, 0); // This will be overwritten when this is resolved, we just store the owner here for cancelation.
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
                        if (Owner.TryCancelLock(this))
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

                public void Resolve(ref long currentKey)
                {
                    lock (this)
                    {
                        ThrowIfInPool(this);

                        // We don't need to check if the unregister was successful or not.
                        // The fact that this was called means the cancelation was unable to unregister this from the lock.
                        // We just dispose to wait for the callback to complete before we continue.
                        _cancelationRegistration.Dispose();

                        _result = new Threading.AsyncLock.Key(Owner, currentKey);
                        if (_callerContext == null)
                        {
                            // It was a synchronous lock, handle next continuation synchronously so that the PromiseSynchronousWaiter will be pulsed to wake the waiting thread.
                            HandleNextInternal(null, Promise.State.Resolved);
                            return;
                        }
                    }

                    // Post the continuation to the caller's context. This prevents blocking the current thread and avoids StackOverflowException.
                    Promise.Run(this, _this => _this.HandleNextInternal(null, Promise.State.Resolved), _callerContext, forceAsync: true)
                        .Forget();
                }

                void ICancelable.Cancel()
                {
                    ThrowIfInPool(this);
                    if (!Owner.TryCancelLock(this))
                    {
                        return;
                    }

                    if (_callerContext == null)
                    {
                        // It was a synchronous lock, handle next continuation synchronously so that the PromiseSynchronousWaiter will be pulsed to wake the waiting thread.
                        HandleNextInternal(null, Promise.State.Canceled);
                        return;
                    }

                    // Post the continuation to the caller's context. This prevents blocking the current thread and avoids StackOverflowException.
                    Promise.Run(this, _this => _this.HandleNextInternal(null, Promise.State.Canceled), _callerContext, forceAsync: true)
                        .Forget();
                }

                internal override void Handle(PromiseRefBase handler, object rejectContainer, Promise.State state) { throw new System.InvalidOperationException(); }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal sealed class AsyncLockWaitPromise : PromiseSingleAwait<bool>, IAsyncLockPromise, ICancelable
            {
                private Threading.AsyncLock _owner;
                private long _key;
                // We post continuations to the caller's context to prevent blocking the thread that released the lock (and to avoid StackOverflowException).
                private SynchronizationContext _callerContext;
                private CancelationRegistration _cancelationRegistration;
                IAsyncLockPromise ILinked<IAsyncLockPromise>.Next { get; set; }

                [MethodImpl(InlineOption)]
                private static AsyncLockWaitPromise GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<AsyncLockWaitPromise>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new AsyncLockWaitPromise()
                        : obj.UnsafeAs<AsyncLockWaitPromise>();
                }

                [MethodImpl(InlineOption)]
                internal static AsyncLockWaitPromise GetOrCreate(Threading.AsyncLock owner, long key, SynchronizationContext callerContext)
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
                    lock (this)
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
                    }

                    // Post the continuation to the caller's context. This prevents blocking the current thread and avoids StackOverflowException.
                    Promise.Run(this, _this => _this.HandleNextInternal(null, Promise.State.Resolved), _callerContext, forceAsync: true)
                        .Forget();
                }

                void ICancelable.Cancel()
                {
                    ThrowIfInPool(this);
                    // We just move this to the ready queue, we don't post the continuation yet because we have to make sure the lock is re-acquired before this is resolved.
                    _result = false;
                    _owner.MaybeMakeReady(this);
                }

                internal override void Handle(PromiseRefBase handler, object rejectContainer, Promise.State state) { throw new System.InvalidOperationException(); }
            }
        }
    }

    namespace Threading
    {
        /// <summary>
        /// A mutual exclusive lock that is compatible with asynchronous operations.
        /// </summary>
        /// <remarks>
        /// Lock re-entrancy is not supported. Attempting to re-enter the lock while it is already acquired will cause a deadlock.
        /// </remarks>
        /// <example>
        /// <para>The vast majority of use cases are to just replace a <c>lock</c> statement. That is, with the original code looking like this:</para>
        /// <code>
        /// private readonly object _mutex = new object();
        /// public void DoStuff()
        /// {
        ///     lock (_mutex)
        ///     {
        ///         Thread.Sleep(TimeSpan.FromSeconds(1));
        ///     }
        /// }
        /// </code>
        /// <para>If we want to replace the blocking operation <c>Thread.Sleep</c> with an asynchronous equivalent, it's not directly possible because of the <c>lock</c> block. We cannot <c>await</c> inside of a <c>lock</c>.</para>
        /// <para>So, we use the <c>async</c>-compatible <see cref="AsyncLock"/> instead:</para>
        /// <code>
        /// private readonly AsyncLock _mutex = new AsyncLock();
        /// public async Promise DoStuffAsync()
        /// {
        ///     using (await _mutex.LockAsync())
        ///     {
        ///         await Task.Delay(TimeSpan.FromSeconds(1));
        ///     }
        /// }
        /// </code>
        /// </example>
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        public sealed class AsyncLock
        {
            /// <summary>
            /// A disposable object used to release the associated <see cref="AsyncLock"/>.
            /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            public
#if CSHARP_7_3_OR_NEWER
                readonly
#endif
                partial struct Key : IDisposable
            {
                internal readonly AsyncLock _owner;
                private readonly long _key;

                internal Key(AsyncLock owner, long key)
                {
                    _owner = owner;
                    _key = key;
                }

                internal Promise<bool> WaitAsync(CancelationToken cancelationToken = default(CancelationToken))
                {
                    return ValidateAndGetOwner()._locker.WaitAsync(_owner, _key, false, cancelationToken);
                }

                internal bool Wait(CancelationToken cancelationToken = default(CancelationToken))
                {
                    return ValidateAndGetOwner()._locker.WaitAsync(_owner, _key, true, cancelationToken).WaitForResult();
                }

                internal void Pulse()
                {
                    ValidateAndGetOwner()._locker.Pulse(_key);
                }

                internal void PulseAll()
                {
                    ValidateAndGetOwner()._locker.PulseAll(_key);
                }

                /// <summary>
                /// Release the lock on the associated <see cref="AsyncLock"/>.
                /// </summary>
                public void Dispose()
                {
                    ValidateAndGetOwner()._locker.ReleaseLock(_key);
                }

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

            private readonly Locker _locker = new Locker();

            /// <summary>
            /// Creates a new async-compatible mutual exclusion lock that does not support re-entrancy.
            /// </summary>
            public AsyncLock() { }

            /// <summary>
            /// Asynchronously acquire the lock. Returns a <see cref="Promise{T}"/> that will be resolved when the lock has been acquired.
            /// The result of the promise is the key that will release the lock when it is disposed.
            /// </summary>
            /// <param name="cancelationToken">The <see cref="CancelationToken"/> used to cancel the lock. If the token is canceled before the lock has been acquired, the returned <see cref="Promise{T}"/> will be canceled.</param>
            public Promise<Key> LockAsync(CancelationToken cancelationToken = default(CancelationToken))
            {
                return _locker.LockAsync(this, false, cancelationToken);
            }

            /// <summary>
            /// Synchronously acquire the lock. Returns the key that will release the lock when it is disposed.
            /// </summary>
            /// <param name="cancelationToken">The <see cref="CancelationToken"/> used to cancel the lock. If the token is canceled before the lock has been acquired, a <see cref="CanceledException"/> will be thrown.</param>
            public Key Lock(CancelationToken cancelationToken = default(CancelationToken))
            {
                return _locker.LockAsync(this, true, cancelationToken).WaitForResult();
            }

            internal bool TryEnter(out Key key)
            {
                return _locker.TryEnter(this, out key);
            }

            internal void MaybeMakeReady(Internal.PromiseRefBase.AsyncLockWaitPromise promise)
            {
                _locker.MaybeMakeReady(promise);
            }

            internal bool TryCancelLock(Internal.PromiseRefBase.AsyncLockPromise promise)
            {
                return _locker.TryUnregister(promise);
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            private static void ThrowInvalidKey(int skipFrames)
            {
                throw new InvalidOperationException("The AsyncLock.Key is invalid for this operation.", Internal.GetFormattedStacktrace(skipFrames + 1));
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private sealed partial class Locker : Internal.ITraceable
            {
                // We use a shared key generator to prevent false positives if a Key is torn.
                private static long s_keyGenerator;

                // These must not be readonly.
                private Internal.ValueLinkedQueue<Internal.IAsyncLockPromise> _waitPulseQueue = new Internal.ValueLinkedQueue<Internal.IAsyncLockPromise>();
                private Internal.ValueLinkedQueue<Internal.IAsyncLockPromise> _queue = new Internal.ValueLinkedQueue<Internal.IAsyncLockPromise>();
                private long _currentKey;

                partial void SetTrace(Internal.ITraceable traceable);
#if PROMISE_DEBUG
                // This is to help debug where a Key was left undisposed.
                private Internal.CausalityTrace _trace;
                Internal.CausalityTrace Internal.ITraceable.Trace
                {
                    get { return _trace; }
                    set { _trace = value; }
                }

                partial void SetTrace(Internal.ITraceable traceable)
                {
                    _trace = traceable == null ? null : traceable.Trace;
                }
#endif

                ~Locker()
                {
                    if (_currentKey != 0)
                    {
                        Internal.ReportRejection(new UnreleasedObjectException("An AsyncLock.Key was not disposed. Any code waiting on the AsyncLock will never continue."), this);
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
                    return Internal.ts_currentContext
                        ?? SynchronizationContext.Current
                        ?? Promise.Config.BackgroundContext
                        ?? Internal.BackgroundSynchronizationContextSentinel.s_instance;
                }

                internal Promise<Key> LockAsync(AsyncLock owner, bool isSynchronous, CancelationToken cancelationToken)
                {
                    // Unfortunately, there is no way to detect async recursive lock enter. A deadlock will occur, instead of throw.
                    Internal.PromiseRefBase.AsyncLockPromise promise;
                    lock (this)
                    {
                        if (_currentKey == 0)
                        {
                            _currentKey = GenerateKey();
                            Internal.SetCreatedStacktraceInternal(this, 2);
                            return Promise.Resolved(new Key(owner, _currentKey));
                        }

                        promise = Internal.PromiseRefBase.AsyncLockPromise.GetOrCreate(owner, isSynchronous ? null : CaptureContext());
                        _queue.Enqueue(promise);
                        promise.MaybeHookupCancelation(cancelationToken);
                    }
                    return new Promise<Key>(promise, promise.Id, 0);
                }

                internal bool TryEnter(AsyncLock owner, out Key key)
                {
                    lock (this)
                    {
                        if (_currentKey == 0)
                        {
                            _currentKey = GenerateKey();
                            Internal.SetCreatedStacktraceInternal(this, 2);
                            key = new Key(owner, _currentKey);
                            return true;
                        }
                    }
                    key = default(Key);
                    return false;
                }

                internal void ReleaseLock(long key)
                {
                    Internal.IAsyncLockPromise next;
                    lock (this)
                    {
                        if (_currentKey != key)
                        {
                            ThrowInvalidKey(3);
                        }
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
                    }
                    next.Resolve(ref _currentKey);
                }

                internal Promise<bool> WaitAsync(AsyncLock owner, long key, bool isSynchronous, CancelationToken cancelationToken)
                {
                    Internal.IAsyncLockPromise next;
                    Internal.PromiseRefBase.AsyncLockWaitPromise promise;
                    lock (this)
                    {
                        if (key != _currentKey)
                        {
                            ThrowInvalidKey(3);
                        }
                        promise = Internal.PromiseRefBase.AsyncLockWaitPromise.GetOrCreate(owner, key, isSynchronous ? null : CaptureContext());
                        _waitPulseQueue.Enqueue(promise);
                        promise.MaybeHookupCancelation(cancelationToken);
                        // We keep the lock until there are no more waiters.
                        if (_queue.IsEmpty)
                        {
                            _currentKey = 0;
                            return new Promise<bool>(promise, promise.Id, 0);
                        }
                        _currentKey = GenerateKey();
                        next = _queue.Dequeue();
                        SetTrace(next);
                    }
                    next.Resolve(ref _currentKey);
                    return new Promise<bool>(promise, promise.Id, 0);
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
                        if (!_waitPulseQueue.IsEmpty)
                        {
                            _queue.TakeAndEnqueueElements(ref _waitPulseQueue);
                        }
                    }
                }

                internal bool TryUnregister(Internal.PromiseRefBase.AsyncLockPromise promise)
                {
                    lock (this)
                    {
                        return _queue.TryRemove(promise);
                    }
                }

                internal void MaybeMakeReady(Internal.PromiseRefBase.AsyncLockWaitPromise promise)
                {
                    lock (this)
                    {
                        if (_waitPulseQueue.TryRemove(promise))
                        {
                            _queue.Enqueue(promise);
                        }
                    }
                }
            } // class Locker
        } // class AsyncLock
    } // namespace Threading
} // namespace Proto.Promises