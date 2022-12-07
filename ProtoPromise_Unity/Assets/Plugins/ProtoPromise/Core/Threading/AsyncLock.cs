#if UNITY_5_5 || NET_2_0 || NET_2_0_SUBSET
#define NET_LEGACY
#endif

#pragma warning disable IDE0034 // Simplify 'default' expression
#pragma warning disable IDE0090 // Use 'new(...)'

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Proto.Promises
{
    partial class Internal
    {
        partial class PromiseRefBase
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal sealed class AsyncLockPromise : PromiseSingleAwait<Threading.AsyncLock.Key>, ILinked<AsyncLockPromise>, ICancelable
            {
                // We post continuations to the caller's context to prevent blocking the thread that released the lock (and to avoid StackOverflowException).
                private SynchronizationContext _callerContext;
                internal Threading.AsyncLock _owner;
                private CancelationRegistration _cancelationRegistration;
                private bool _disposed;
                AsyncLockPromise ILinked<AsyncLockPromise>.Next { get; set; }

                ~AsyncLockPromise()
                {
                    // If this finalizer is called and this wasn't disposed, it means the AsyncLock instance was garbage collected without the AsyncLock.Key ever having been disposed.
                    // If the state is resolved, it means the lock was taken by the creator of this promise. Otherwise, a different promise creator has the lock.
                    if (!_disposed & State == Promise.State.Resolved)
                    {
                        // We simply report the error.
                        // We cannot release the lock from the finalizer, as it may cause some issues due to resurrected objects.
                        // In that case, there will be a deadlock, and any code waiting on the lock will never continue.
                        // The same thing happens if a regular lock is never released, so I'm not too concerned about it.
                        ReportRejection(new UnreleasedObjectException("An AsyncLock.Key was not disposed. Any code waiting on the AsyncLock will never continue."), this);
                    }
                }

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
                    promise._disposed = false;
                    promise._owner = owner;
                    promise._result = new Threading.AsyncLock.Key(promise);
                    return promise;
                }

                void ICancelable.Cancel()
                {
                    ThrowIfInPool(this);
                    if (!_owner.TryCancelLock(this))
                    {
                        return;
                    }

                    if (_callerContext == null)
                    {
                        // It was a synchronous lock, set the state and pulse so the other thread will wake.
                        State = Promise.State.Canceled;
                        WasAwaitedOrForgotten = true;
                        lock (this)
                        {
                            Monitor.Pulse(this);
                        }
                        return;
                    }

                    // Post the continuation to the caller's context. This prevents blocking the current thread and avoids StackOverflowException.
                    Promise.Run(this, _this => _this.HandleNextInternal(null, Promise.State.Canceled), _callerContext, forceAsync: true)
                        .Forget();
                }

                internal void MaybeHookupCancelation(CancelationToken cancelationToken)
                {
                    ThrowIfInPool(this);
                    if (State == Promise.State.Resolved)
                    {
                        return;
                    }
                    if (cancelationToken.IsCancelationRequested)
                    {
                        if (_owner.TryCancelLock(this))
                        {
                            CompleteImmediate(Promise.State.Canceled);
                        }
                        return;
                    }
                    cancelationToken.TryRegister(this, out _cancelationRegistration);
                }

                internal override void MaybeDispose()
                {
                    // Do nothing, this will be disposed when the lock is released.
                }

                new internal void Dispose()
                {
                    base.Dispose();
                    _disposed = true;
                    _callerContext = null;
                    _owner = null;
                    _cancelationRegistration = default(CancelationRegistration);
                    ObjectPool.MaybeRepool(this);
                }

                internal void CompleteImmediate(Promise.State state)
                {
                    // This is called if the promise is completed immediately when the lock is attempted to be entered.
                    // Either the lock was not already taken, or the cancelation token was already canceled.
                    // In either case, we know no continuations have been added to the promise yet,
                    // so we don't have to worry about StackOverflowException, or handling the next waiter.
                    // We just set the completion state.
                    // This is equivalent to `HandleNextInternal(null, state)`, but without the extra branches.
                    ThrowIfInPool(this);
                    _next = PromiseCompletionSentinel.s_instance;
                    SetCompletionState(null, state);
                }

                internal void Resolve()
                {
                    lock (this)
                    {
                        ThrowIfInPool(this);

                        // We don't need to check if the unregister was successful or not.
                        // The fact that this was called means the cancelation was unable to unregister this from the lock.
                        // We just dispose to wait for the callback to complete before we continue.
                        _cancelationRegistration.Dispose();

                        if (_callerContext == null)
                        {
                            // It was a synchronous lock, set the state and pulse so the other thread will wake.
                            State = Promise.State.Resolved;
                            WasAwaitedOrForgotten = true;
                            Monitor.Pulse(this);
                            return;
                        }
                    }

                    // Post the continuation to the caller's context. This prevents blocking the current thread and avoids StackOverflowException.
                    Promise.Run(this, _this => _this.HandleNextInternal(null, Promise.State.Resolved), _callerContext, forceAsync: true)
                        .Forget();
                }
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
                private readonly Internal.PromiseRefBase.AsyncLockPromise _promise;
                private readonly short _id;

                internal Key(Internal.PromiseRefBase.AsyncLockPromise promise)
                {
                    _promise = promise;
                    _id = promise.Id;
                }

                /// <summary>
                /// Release the lock on the associated <see cref="AsyncLock"/>.
                /// </summary>
                public void Dispose()
                {
                    var promise = _promise;
                    if (promise == null)
                    {
                        ThrowInvalidRelease(1);
                    }
                    var owner = promise._owner;
                    if (owner == null)
                    {
                        ThrowInvalidRelease(1);
                    }
                    owner._locker.ReleaseLock(promise, _id);
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
                // We capture the current context to post the continuation. If it's null, we use the background context.
                var context = Internal.ts_currentContext
                    ?? SynchronizationContext.Current
                    ?? Promise.Config.BackgroundContext
                    ?? Internal.BackgroundSynchronizationContextSentinel.s_instance;
                var promise = Internal.PromiseRefBase.AsyncLockPromise.GetOrCreate(this, context);
                // Lock on the promise to fix a race condition with another thread resolving it while the token is being hooked up.
                lock (promise)
                {
                    _locker.MaybeLock(promise);
                    promise.MaybeHookupCancelation(cancelationToken);
                }
                return new Promise<Key>(promise, promise.Id, 0);
            }

            /// <summary>
            /// Synchronously acquire the lock. Returns the key that will release the lock when it is disposed.
            /// </summary>
            /// <param name="cancelationToken">The <see cref="CancelationToken"/> used to cancel the lock. If the token is canceled before the lock has been acquired, <see cref="CanceledException"/> will be thrown.</param>
            public Key Lock(CancelationToken cancelationToken = default(CancelationToken))
            {
                var promise = Internal.PromiseRefBase.AsyncLockPromise.GetOrCreate(this, null);
                lock (promise)
                {
                    _locker.MaybeLock(promise);
                    promise.MaybeHookupCancelation(cancelationToken);
                    if (promise.State == Promise.State.Pending)
                    {
                        Monitor.Wait(promise);
                    }
                }
                if (promise.State == Promise.State.Canceled)
                {
                    throw Promise.CancelException();
                }
                return promise._result;
            }

            internal bool TryCancelLock(Internal.PromiseRefBase.AsyncLockPromise promise)
            {
                return _locker.TryUnregister(promise);
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            private static void ThrowInvalidRelease(int skipFrames)
            {
                throw new InvalidOperationException("AsyncLock was attempted to be released by an invalid AsyncLock.Key.", Internal.GetFormattedStacktrace(skipFrames + 1));
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private sealed class Locker
            {
                // This must not be readonly.
                private Internal.ValueLinkedQueue<Internal.PromiseRefBase.AsyncLockPromise> _queue = new Internal.ValueLinkedQueue<Internal.PromiseRefBase.AsyncLockPromise>();
                private Internal.PromiseRefBase.AsyncLockPromise _currentLockHolder;
                private bool _isLockHeld;

                internal void MaybeLock(Internal.PromiseRefBase.AsyncLockPromise promise)
                {
                    lock (this)
                    {
                        // Unfortunately, there is no way to detect async recursive lock enter. A deadlock will occur, instead of throw.
                        if (!_isLockHeld)
                        {
                            _isLockHeld = true;
                            _currentLockHolder = promise;
                            promise.CompleteImmediate(Promise.State.Resolved);
                        }
                        else
                        {
                            _queue.Enqueue(promise);
                        }
                    }
                }

                internal void ReleaseLock(Internal.PromiseRefBase.AsyncLockPromise promise, short id)
                {
                    Internal.PromiseRefBase.AsyncLockPromise next;
                    lock (this)
                    {
                        if (promise != _currentLockHolder | promise.Id != id)
                        {
                            ThrowInvalidRelease(3);
                        }
                        // We keep the lock until there are no more waiters.
                        if (_queue.IsEmpty)
                        {
                            _currentLockHolder = null;
                            _isLockHeld = false;
                            goto Done;
                        }
                        next = _queue.Dequeue();
                        _currentLockHolder = next;
                    }
                    promise.Dispose();
                    next.Resolve();
                    return;

                Done:
                    promise.Dispose();
                }

                internal bool TryUnregister(Internal.PromiseRefBase.AsyncLockPromise promise)
                {
                    lock (this)
                    {
                        return _queue.TryRemove(promise);
                    }
                }
            } // class Locker
        } // class AsyncLock
    } // namespace Threading
} // namespace Proto.Promises