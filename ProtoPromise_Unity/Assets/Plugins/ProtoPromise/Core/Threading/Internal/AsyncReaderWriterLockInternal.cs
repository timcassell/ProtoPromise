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
#pragma warning disable IDE0090 // Use 'new(...)'
#pragma warning disable IDE0180 // Use tuple to swap values

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
        partial class PromiseRefBase
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal abstract class AsyncReaderWriterLockPromise<TKey> : PromiseSingleAwait<TKey>
            {
                // We post continuations to the caller's context to prevent blocking the thread that released the lock (and to avoid StackOverflowException).
                protected SynchronizationContext _callerContext;
                protected CancelationRegistration _cancelationRegistration;
                // We have to store the state in a separate field until the next awaiter is ready to be invoked on the proper context.
                private Promise.State _tempState;

                new protected void Dispose()
                {
                    base.Dispose();
                    _callerContext = null;
                    _cancelationRegistration = default(CancelationRegistration);
                }

                protected void Continue(Promise.State state)
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

                internal override sealed void HandleFromContext()
                {
                    HandleNextInternal(null, _tempState);
                }

                internal override sealed void Handle(PromiseRefBase handler, object rejectContainer, Promise.State state) { throw new System.InvalidOperationException(); }
            }
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal sealed class AsyncReaderLockPromise : PromiseRefBase.AsyncReaderWriterLockPromise<AsyncReaderWriterLock.ReaderKey>, ICancelable, ILinked<AsyncReaderLockPromise>
        {
            AsyncReaderLockPromise ILinked<AsyncReaderLockPromise>.Next { get; set; }
            private AsyncReaderWriterLockInternal Owner { get { return _result._owner; } }

            [MethodImpl(InlineOption)]
            private static AsyncReaderLockPromise GetOrCreate()
            {
                var obj = ObjectPool.TryTakeOrInvalid<AsyncReaderLockPromise>();
                return obj == InvalidAwaitSentinel.s_instance
                    ? new AsyncReaderLockPromise()
                    : obj.UnsafeAs<AsyncReaderLockPromise>();
            }

            internal static AsyncReaderLockPromise GetOrCreate(AsyncReaderWriterLockInternal owner, SynchronizationContext callerContext)
            {
                var promise = GetOrCreate();
                promise.Reset();
                promise._result = new AsyncReaderWriterLock.ReaderKey(owner);
                promise._callerContext = callerContext;
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

            internal void Resolve(long currentKey)
            {
                ThrowIfInPool(this);

                // We don't need to check if the unregister was successful or not.
                // The fact that this was called means the cancelation was unable to unregister this from the lock.
                // We just dispose to wait for the callback to complete before we continue.
                _cancelationRegistration.Dispose();

                _result = new AsyncReaderWriterLock.ReaderKey(Owner, currentKey);
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
        internal sealed class AsyncWriterLockPromise : PromiseRefBase.AsyncReaderWriterLockPromise<AsyncReaderWriterLock.WriterKey>, ICancelable, ILinked<AsyncWriterLockPromise>
        {
            AsyncWriterLockPromise ILinked<AsyncWriterLockPromise>.Next { get; set; }
            private AsyncReaderWriterLockInternal Owner { get { return _result._owner; } }

            [MethodImpl(InlineOption)]
            private static AsyncWriterLockPromise GetOrCreate()
            {
                var obj = ObjectPool.TryTakeOrInvalid<AsyncWriterLockPromise>();
                return obj == InvalidAwaitSentinel.s_instance
                    ? new AsyncWriterLockPromise()
                    : obj.UnsafeAs<AsyncWriterLockPromise>();
            }

            internal static AsyncWriterLockPromise GetOrCreate(AsyncReaderWriterLock.WriterKey key, SynchronizationContext callerContext)
            {
                var promise = GetOrCreate();
                promise.Reset();
                promise._callerContext = callerContext;
                promise._result = key; // This will be overwritten when this is resolved, we just store the key with the owner here for cancelation.
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

            internal void Resolve(long writerKey)
            {
                ThrowIfInPool(this);

                // We don't need to check if the unregister was successful or not.
                // The fact that this was called means the cancelation was unable to unregister this from the lock.
                // We just dispose to wait for the callback to complete before we continue.
                _cancelationRegistration.Dispose();

                _result = new AsyncReaderWriterLock.WriterKey(Owner, writerKey);
                Continue(Promise.State.Resolved);
            }

            void ICancelable.Cancel()
            {
                ThrowIfInPool(this);
                if (Owner.TryUnregister(this))
                {
                    Continue(Promise.State.Canceled);
                }
            }
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal sealed class AsyncUpgradeableReaderLockPromise : PromiseRefBase.AsyncReaderWriterLockPromise<AsyncReaderWriterLock.UpgradeableReaderKey>, ICancelable, ILinked<AsyncUpgradeableReaderLockPromise>
        {
            AsyncUpgradeableReaderLockPromise ILinked<AsyncUpgradeableReaderLockPromise>.Next { get; set; }
            private AsyncReaderWriterLockInternal Owner { get { return _result._owner; } }

            [MethodImpl(InlineOption)]
            private static AsyncUpgradeableReaderLockPromise GetOrCreate()
            {
                var obj = ObjectPool.TryTakeOrInvalid<AsyncUpgradeableReaderLockPromise>();
                return obj == InvalidAwaitSentinel.s_instance
                    ? new AsyncUpgradeableReaderLockPromise()
                    : obj.UnsafeAs<AsyncUpgradeableReaderLockPromise>();
            }

            internal static AsyncUpgradeableReaderLockPromise GetOrCreate(AsyncReaderWriterLockInternal owner, SynchronizationContext callerContext)
            {
                var promise = GetOrCreate();
                promise.Reset();
                promise._result = new AsyncReaderWriterLock.UpgradeableReaderKey(owner, 0);
                promise._callerContext = callerContext;
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

            internal void Resolve(long currentKey)
            {
                ThrowIfInPool(this);

                // We don't need to check if the unregister was successful or not.
                // The fact that this was called means the cancelation was unable to unregister this from the lock.
                // We just dispose to wait for the callback to complete before we continue.
                _cancelationRegistration.Dispose();

                _result = new AsyncReaderWriterLock.UpgradeableReaderKey(Owner, currentKey);
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

        [Flags]
        internal enum AsyncReaderWriterLockType : byte
        {
            None = 0,
            Reader = 1 << 0,
            Writer = 1 << 1,
            Upgradeable = 1 << 2
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal sealed class AsyncReaderWriterLockInternal
        {
            // If there is contention on the lock, we alternate between writers and readers, so one type of lock won't starve out the other.
            // We also alternate between upgraded writers and regular writers to prevent writer starvation.

            // These must not be readonly.
            private ValueLinkedQueue<AsyncReaderLockPromise> _readerQueue = new ValueLinkedQueue<AsyncReaderLockPromise>();
            private ValueLinkedQueue<AsyncWriterLockPromise> _writerQueue = new ValueLinkedQueue<AsyncWriterLockPromise>();
            private ValueLinkedQueue<AsyncUpgradeableReaderLockPromise> _upgradeQueue = new ValueLinkedQueue<AsyncUpgradeableReaderLockPromise>();
            private AsyncWriterLockPromise _upgradeWaiter;
            private long _currentKey;
            // This either stores the next key normally, or the previous key if the lock is upgraded.
            // This is done to avoid a branch instruction when an upgraded writer lock is released.
            private long _nextKey = KeyGenerator<AsyncReaderWriterLockInternal>.Next();
            private uint _readerLockCount;
            private uint _readerWaitCount;
            private AsyncReaderWriterLockType _lockType;
            private AsyncReaderWriterLockType _writerType;

            [MethodImpl(InlineOption)]
            private void SetNextKey()
            {
                _currentKey = _nextKey;
                _nextKey = KeyGenerator<AsyncReaderWriterLockInternal>.Next();
            }

            [MethodImpl(InlineOption)]
            private void SetNextAndPreviousKeys()
            {
                var temp = _nextKey;
                _nextKey = _currentKey;
                _currentKey = temp;
            }

            private void VerifyReaderCounts()
            {
                // Check to make sure the reader count doesn't overflow. Subtract 1 to leave room for an upgradeable reader lock.
                // We could queue up reader locks that extend beyond the maximum value, but that would cost extra overhead that I don't think is worth it.
                // If the count overflows, the user is probably doing something wrong, anyway.
                const uint maxReaders = uint.MaxValue - 1u;
                if ((_readerLockCount + _readerWaitCount) == maxReaders)
                {
                    throw new OverflowException("AsyncReaderWriterLock does not support more than " + maxReaders + " simultaneous reader locks.");
                }
            }

            internal Promise<AsyncReaderWriterLock.ReaderKey> ReaderLock(bool isSynchronous, CancelationToken cancelationToken)
            {
                if (cancelationToken.IsCancelationRequested)
                {
                    return Promise<AsyncReaderWriterLock.ReaderKey>.Canceled();
                }

                // Unchecked context since we're manually checking overflow.
                unchecked
                {
                    AsyncReaderLockPromise promise;
                    lock (this)
                    {
                        VerifyReaderCounts();

                        // Normal reader locks can be taken simultaneously along with an upgradeable reader lock.
                        // If a writer lock is taken or waiting to be taken, we wait for it to be released, to prevent starvation.
                        if ((_lockType & AsyncReaderWriterLockType.Writer) == 0)
                        {
                            ++_readerLockCount;
                            if (_lockType == AsyncReaderWriterLockType.None)
                            {
                                SetNextKey();
                            }
                            _lockType |= AsyncReaderWriterLockType.Reader;
                            return Promise.Resolved(new AsyncReaderWriterLock.ReaderKey(this, _currentKey));
                        }

                        ++_readerWaitCount;
                        promise = AsyncReaderLockPromise.GetOrCreate(this, isSynchronous ? null : CaptureContext());
                        _readerQueue.Enqueue(promise);
                        promise.MaybeHookupCancelation(cancelationToken);
                    }
                    return new Promise<AsyncReaderWriterLock.ReaderKey>(promise, promise.Id, 0);
                }
            }

            internal bool TryEnterReaderLock(out AsyncReaderWriterLock.ReaderKey readerKey)
            {
                lock (this)
                {
                    VerifyReaderCounts();

                    // Normal reader locks can be taken simultaneously along with an upgradeable reader lock.
                    // If a writer lock is taken or waiting to be taken, we do not take the reader lock, to prevent starvation.
                    if ((_lockType & AsyncReaderWriterLockType.Writer) == 0)
                    {
                        // Unchecked context since we're manually checking overflow.
                        unchecked
                        {
                            ++_readerLockCount;
                        }
                        if (_lockType == AsyncReaderWriterLockType.None)
                        {
                            SetNextKey();
                        }
                        _lockType |= AsyncReaderWriterLockType.Reader;
                        readerKey = new AsyncReaderWriterLock.ReaderKey(this, _currentKey);
                        return true;
                    }
                }
                readerKey = default(AsyncReaderWriterLock.ReaderKey);
                return false;
            }

            internal Promise<AsyncReaderWriterLock.WriterKey> WriterLock(bool isSynchronous, CancelationToken cancelationToken)
            {
                if (cancelationToken.IsCancelationRequested)
                {
                    return Promise<AsyncReaderWriterLock.WriterKey>.Canceled();
                }

                AsyncWriterLockPromise promise;
                lock (this)
                {
                    // Writer locks are mutually exclusive.
                    var lockType = _lockType;
                    _lockType = lockType | AsyncReaderWriterLockType.Writer;
                    if (lockType == AsyncReaderWriterLockType.None)
                    {
                        _writerType = AsyncReaderWriterLockType.Writer;
                        SetNextKey();
                        return Promise.Resolved(new AsyncReaderWriterLock.WriterKey(this, _currentKey));
                    }

                    promise = AsyncWriterLockPromise.GetOrCreate(new AsyncReaderWriterLock.WriterKey(this), isSynchronous ? null : CaptureContext());
                    _writerQueue.Enqueue(promise);
                    promise.MaybeHookupCancelation(cancelationToken);
                }
                return new Promise<AsyncReaderWriterLock.WriterKey>(promise, promise.Id, 0);
            }

            internal bool TryEnterWriterLock(out AsyncReaderWriterLock.WriterKey writerKey)
            {
                lock (this)
                {
                    // Writer locks are mutually exclusive.
                    var lockType = _lockType;
                    _lockType = lockType | AsyncReaderWriterLockType.Writer;
                    if (lockType == AsyncReaderWriterLockType.None)
                    {
                        _writerType = AsyncReaderWriterLockType.Writer;
                        SetNextKey();
                        writerKey = new AsyncReaderWriterLock.WriterKey(this, _currentKey);
                        return true;
                    }
                }
                writerKey = default(AsyncReaderWriterLock.WriterKey);
                return false;
            }

            internal Promise<AsyncReaderWriterLock.UpgradeableReaderKey> UpgradeableReaderLock(bool isSynchronous, CancelationToken cancelationToken)
            {
                if (cancelationToken.IsCancelationRequested)
                {
                    return Promise<AsyncReaderWriterLock.UpgradeableReaderKey>.Canceled();
                }

                AsyncUpgradeableReaderLockPromise promise;
                lock (this)
                {
                    // Normal reader locks can be taken simultaneously along with the upgradeable reader lock.
                    // Only 1 upgradeable reader lock is allowed at a time.
                    // If a writer lock is taken or waiting to be taken, we wait for it to be released, to prevent starvation.
                    if (_lockType <= AsyncReaderWriterLockType.Reader)
                    {
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                        checked
#else
                        unchecked
#endif
                        {
                            ++_readerLockCount;
                        }
                        if (_lockType == AsyncReaderWriterLockType.None)
                        {
                            SetNextKey();
                        }
                        _lockType |= AsyncReaderWriterLockType.Upgradeable;
                        return Promise.Resolved(new AsyncReaderWriterLock.UpgradeableReaderKey(this, _currentKey));
                    }

                    promise = AsyncUpgradeableReaderLockPromise.GetOrCreate(this, isSynchronous ? null : CaptureContext());
                    _upgradeQueue.Enqueue(promise);
                    promise.MaybeHookupCancelation(cancelationToken);
                }
                return new Promise<AsyncReaderWriterLock.UpgradeableReaderKey>(promise, promise.Id, 0);
            }

            internal bool TryEnterUpgradeableReaderLock(out AsyncReaderWriterLock.UpgradeableReaderKey readerKey)
            {
                lock (this)
                {
                    // Normal reader locks can be taken simultaneously along with the upgradeable reader lock.
                    // Only 1 upgradeable reader lock is allowed at a time.
                    // If a writer lock is taken or waiting to be taken, we do not take the upgradeable reader lock, to prevent starvation.
                    if (_lockType <= AsyncReaderWriterLockType.Reader)
                    {
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                        checked
#else
                        unchecked
#endif
                        {
                            ++_readerLockCount;
                        }
                        if (_lockType == AsyncReaderWriterLockType.None)
                        {
                            SetNextKey();
                        }
                        _lockType |= AsyncReaderWriterLockType.Upgradeable;
                        readerKey = new AsyncReaderWriterLock.UpgradeableReaderKey(this, _currentKey);
                        return true;
                    }
                }
                readerKey = default(AsyncReaderWriterLock.UpgradeableReaderKey);
                return false;
            }

            internal Promise<AsyncReaderWriterLock.WriterKey> UpgradeToWriterLock(AsyncReaderWriterLock.UpgradeableReaderKey readerKey, bool isSynchronous, CancelationToken cancelationToken)
            {
                if (cancelationToken.IsCancelationRequested)
                {
                    return Promise<AsyncReaderWriterLock.WriterKey>.Canceled();
                }

                AsyncWriterLockPromise promise;
                lock (this)
                {
                    if (_currentKey != readerKey._key | this != readerKey._owner)
                    {
                        ThrowInvalidKey(AsyncReaderWriterLockType.Upgradeable, 2);
                    }

#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                    checked
#else
                    unchecked
#endif
                    {
                        // Count will be re-added when the lock is downgraded.
                        --_readerLockCount;
                    }
                    _lockType |= AsyncReaderWriterLockType.Writer;
                    if (_readerLockCount == 0)
                    {
                        _writerType = AsyncReaderWriterLockType.Upgradeable;
                        // We cache the previous key for when the lock gets downgraded.
                        SetNextAndPreviousKeys();
                        return Promise.Resolved(new AsyncReaderWriterLock.WriterKey(this, _currentKey));
                    }

                    promise = AsyncWriterLockPromise.GetOrCreate(new AsyncReaderWriterLock.WriterKey(this), isSynchronous ? null : CaptureContext());
                    _upgradeWaiter = promise;
                    promise.MaybeHookupCancelation(cancelationToken);
                }
                return new Promise<AsyncReaderWriterLock.WriterKey>(promise, promise.Id, 0);
            }

            internal bool TryUpgradeToWriterLock(AsyncReaderWriterLock.UpgradeableReaderKey readerKey, out AsyncReaderWriterLock.WriterKey writerKey)
            {
                lock (this)
                {
                    if (_currentKey != readerKey._key | this != readerKey._owner)
                    {
                        ThrowInvalidKey(AsyncReaderWriterLockType.Upgradeable, 2);
                    }

                    if (_readerLockCount == 1)
                    {
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                        checked
#else
                        unchecked
#endif
                        {
                            // Count will be re-added when the lock is downgraded.
                            --_readerLockCount;
                        }
                        _lockType |= AsyncReaderWriterLockType.Writer;
                        _writerType = AsyncReaderWriterLockType.Upgradeable;
                        // We cache the previous key for when the lock gets downgraded.
                        SetNextAndPreviousKeys();
                        writerKey = new AsyncReaderWriterLock.WriterKey(this, _currentKey);
                        return true;
                    }
                }
                writerKey = default(AsyncReaderWriterLock.WriterKey);
                return false;
            }

            internal void ReleaseReaderLock(long key)
            {
                AsyncWriterLockPromise promise;
                lock (this)
                {
                    if (_currentKey != key)
                    {
                        ThrowInvalidKey(AsyncReaderWriterLockType.Reader, 2);
                    }

                    // We check for underflow, because multiple readers share the same key.
                    checked
                    {
                        if (--_readerLockCount != 0)
                        {
                            return;
                        }
                    }
                    // Check upgrade waiter before regular writer queue.
                    if (_upgradeWaiter != null)
                    {
                        _lockType = AsyncReaderWriterLockType.Writer | AsyncReaderWriterLockType.Upgradeable;
                        _writerType = AsyncReaderWriterLockType.Upgradeable;
                        // We cache the previous key for when the lock gets downgraded.
                        SetNextAndPreviousKeys();
                        promise = _upgradeWaiter;
                        _upgradeWaiter = null;
                    }
                    else if (_writerQueue.IsNotEmpty)
                    {
                        _lockType = AsyncReaderWriterLockType.Writer | (_lockType & AsyncReaderWriterLockType.Upgradeable);
                        _writerType = AsyncReaderWriterLockType.Writer;
                        // We cache the previous key for when the lock gets downgraded.
                        SetNextAndPreviousKeys();
                        promise = _writerQueue.Dequeue();
                    }
                    else
                    {
                        // The lock is exited completely.
                        _lockType = AsyncReaderWriterLockType.None;
                        _currentKey = 0L;
                        return;
                    }
                } // lock

                promise.Resolve(_currentKey);
            }

            internal void ReleaseWriterLock(long key)
            {
                ValueLinkedStack<AsyncReaderLockPromise> readers;
                AsyncUpgradeableReaderLockPromise upgradeablePromise;
                AsyncWriterLockPromise writerPromise;
                lock (this)
                {
                    if (_currentKey != key)
                    {
                        ThrowInvalidKey(AsyncReaderWriterLockType.Writer, 2);
                    }

                    // If the writer type is upgraded, we add 1 more reader count since it was removed when the lock was upgraded.
                    // This does the calculation without a branch. Unchecked context since we already checked for overflow when the reader lock was acquired.
                    unchecked
                    {
                        _readerWaitCount += (uint) ((byte) _writerType >> 2);
                    }
                    // If there are any readers waiting (including the upgradeable reader that is releasing its writer lock),
                    // we resolve them, even if there are more writer waiters. This prevents writers from starving readers.
                    if (_readerWaitCount != 0)
                    {
                        if (_writerQueue.IsEmpty)
                        {
                            _lockType = AsyncReaderWriterLockType.Reader | (_lockType & ~AsyncReaderWriterLockType.Writer);
                        }
                        else
                        {
                            _lockType |= AsyncReaderWriterLockType.Reader;
                        }
                        var writerType = _writerType;
                        _writerType = AsyncReaderWriterLockType.None;
                        _readerLockCount = _readerWaitCount;
                        _readerWaitCount = 0;
                        // This either sets the next key, or reverts the key, depending if it was an upgraded lock or a regular lock, without a branch.
                        SetNextKey();
                        readers = _readerQueue.MoveElementsToStack();
                        if (writerType == AsyncReaderWriterLockType.Upgradeable | _upgradeQueue.IsEmpty)
                        {
                            goto ResolveReaders;
                        }
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                        checked
#else
                        unchecked
#endif
                        {
                            ++_readerLockCount;
                        }
                        upgradeablePromise = _upgradeQueue.Dequeue();
                        goto ResolveReadersAndUpgradeableReader;
                    }

                    // At this point, we know it was a regular writer, and there are no waiting normal readers.
                    // Check upgrade queue before writer queue, to prevent writers from starving upgradeable readers.
                    if (_upgradeQueue.IsNotEmpty)
                    {
                        _lockType = _writerQueue.IsEmpty
                            ? AsyncReaderWriterLockType.Upgradeable
                            : _lockType | AsyncReaderWriterLockType.Upgradeable;
                        _writerType = AsyncReaderWriterLockType.None;
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                        checked
#else
                        unchecked
#endif
                        {
                            ++_readerLockCount;
                        }
                        upgradeablePromise = _upgradeQueue.Dequeue();
                        goto ResolveUpgradeableReader;
                    }
                    else if (_writerQueue.IsNotEmpty)
                    {
                        SetNextKey();
                        writerPromise = _writerQueue.Dequeue();
                    }
                    else
                    {
                        // The lock is exited completely.
                        _lockType = AsyncReaderWriterLockType.None;
                        _currentKey = 0;
                        return;
                    }
                } // lock

                writerPromise.Resolve(_currentKey);
                return;

            ResolveReadersAndUpgradeableReader:
                upgradeablePromise.Resolve(_currentKey);

            ResolveReaders:
                while (readers.IsNotEmpty)
                {
                    readers.Pop().Resolve(_currentKey);
                }
                return;

            ResolveUpgradeableReader:
                upgradeablePromise.Resolve(_currentKey);
            }

            internal void ReleaseUpgradeableReaderLock(long key)
            {
                AsyncUpgradeableReaderLockPromise upgradeablePromise;
                AsyncWriterLockPromise writerPromise;
                lock (this)
                {
                    if (_currentKey != key | _writerType == AsyncReaderWriterLockType.Upgradeable)
                    {
                        if (_currentKey != key)
                        {
                            ThrowInvalidKey(AsyncReaderWriterLockType.Upgradeable, 2);
                        }
                        else
                        {
                            ThrowUpgradeableKeyReleasedTooSoon(2);
                        }
                    }

                    // We check for underflow, because multiple readers share the same key.
                    checked
                    {
                        --_readerLockCount;
                    }
                    if (_readerLockCount != 0)
                    {
                        // If there's a regular writer waiting, do nothing, to prevent starvation.
                        // Otherwise, if there's an upgradeable reader waiting, resolve it.
                        if (_writerQueue.IsNotEmpty | _upgradeQueue.IsEmpty)
                        {
                            _lockType &= ~AsyncReaderWriterLockType.Upgradeable;
                            return;
                        }
                        unchecked
                        {
                            ++_readerLockCount;
                        }
                        upgradeablePromise = _upgradeQueue.Dequeue();
                        goto ResolveUpgradeableReader;
                    }

                    // Check writer queue before checking upgrade queue, to prevent starvation.
                    if (_writerQueue.IsNotEmpty)
                    {
                        writerPromise = _writerQueue.Dequeue();
                        _lockType &= ~AsyncReaderWriterLockType.Upgradeable;
                        _writerType = AsyncReaderWriterLockType.Writer;
                        SetNextKey();
                    }
                    else if (_upgradeQueue.IsNotEmpty)
                    {
                        unchecked
                        {
                            ++_readerLockCount;
                        }
                        upgradeablePromise = _upgradeQueue.Dequeue();
                        goto ResolveUpgradeableReader;
                    }
                    else
                    {
                        // The lock is exited completely.
                        _lockType = AsyncReaderWriterLockType.None;
                        _currentKey = 0;
                        return;
                    }
                } // lock

                writerPromise.Resolve(_currentKey);
                return;

            ResolveUpgradeableReader:
                upgradeablePromise.Resolve(_currentKey);
            }

            internal bool TryUnregister(AsyncReaderLockPromise promise)
            {
                lock (this)
                {
                    return _readerQueue.TryRemove(promise);
                }
            }

            internal bool TryUnregister(AsyncWriterLockPromise promise)
            {
                lock (this)
                {
                    if (_upgradeWaiter == promise)
                    {
                        // Re-add the count that was subtracted when it was upgraded.
                        ++_readerLockCount;
                        _upgradeWaiter = null;
                        return true;
                    }
                    return _writerQueue.TryRemove(promise);
                }
            }

            internal bool TryUnregister(AsyncUpgradeableReaderLockPromise promise)
            {
                lock (this)
                {
                    return _upgradeQueue.TryRemove(promise);
                }
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            internal static void ThrowInvalidKey(AsyncReaderWriterLockType keyType, int skipFrames)
            {
                string keyString = keyType == AsyncReaderWriterLockType.Reader ? "ReaderKey"
                    : keyType == AsyncReaderWriterLockType.Writer ? "WriterKey"
                    : "UpgradeableReaderKey";
                throw new InvalidOperationException("The AsyncReaderWriterLock." + keyString + " is invalid for this operation.", GetFormattedStacktrace(skipFrames + 1));
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            internal static void ThrowUpgradeableKeyReleasedTooSoon(int skipFrames)
            {
                throw new InvalidOperationException("The AsyncReaderWriterLock.UpgradeableReaderKey cannot be released before the upgraded writer is released.", GetFormattedStacktrace(skipFrames + 1));
            }
        } // class AsyncReaderWriterLockInternal
    } // class Internal
#endif // UNITY_2021_2_OR_NEWER || NETSTANDARD2_1_OR_GREATER || NETCOREAPP

    namespace Threading
    {
#if UNITY_2021_2_OR_NEWER || NETSTANDARD2_1_OR_GREATER || NETCOREAPP
        partial class AsyncReaderWriterLock
        {
            // We wrap the impl with another class so that we can lock on it safely.
            private readonly Internal.AsyncReaderWriterLockInternal _impl = new Internal.AsyncReaderWriterLockInternal();

            partial struct ReaderKey
            {
                internal readonly Internal.AsyncReaderWriterLockInternal _owner;
                internal readonly long _key;

                internal ReaderKey(Internal.AsyncReaderWriterLockInternal owner, long key)
                {
                    _owner = owner;
                    _key = key;
                }

                internal ReaderKey(Internal.AsyncReaderWriterLockInternal owner)
                {
                    _owner = owner;
                    _key = 0;
                }

                private Internal.AsyncReaderWriterLockInternal ValidateAndGetOwner()
                {
                    var owner = _owner;
                    if (owner == null)
                    {
                        Internal.AsyncReaderWriterLockInternal.ThrowInvalidKey(Internal.AsyncReaderWriterLockType.Reader, 2);
                    }
                    return owner;
                }
            }

            partial struct WriterKey
            {
                internal readonly Internal.AsyncReaderWriterLockInternal _owner;
                internal readonly long _key;

                internal WriterKey(Internal.AsyncReaderWriterLockInternal owner, long key)
                {
                    _owner = owner;
                    _key = key;
                }

                internal WriterKey(Internal.AsyncReaderWriterLockInternal owner)
                {
                    _owner = owner;
                    _key = 0;
                }

                private Internal.AsyncReaderWriterLockInternal ValidateAndGetOwner()
                {
                    var owner = _owner;
                    if (owner == null)
                    {
                        Internal.AsyncReaderWriterLockInternal.ThrowInvalidKey(Internal.AsyncReaderWriterLockType.Writer, 2);
                    }
                    return owner;
                }
            }

            partial struct UpgradeableReaderKey
            {
                internal readonly Internal.AsyncReaderWriterLockInternal _owner;
                internal readonly long _key;

                internal UpgradeableReaderKey(Internal.AsyncReaderWriterLockInternal owner, long key)
                {
                    _owner = owner;
                    _key = key;
                }

                internal UpgradeableReaderKey(Internal.AsyncReaderWriterLockInternal owner)
                {
                    _owner = owner;
                    _key = 0;
                }

                private Internal.AsyncReaderWriterLockInternal ValidateAndGetOwner()
                {
                    var owner = _owner;
                    if (owner == null)
                    {
                        Internal.AsyncReaderWriterLockInternal.ThrowInvalidKey(Internal.AsyncReaderWriterLockType.Upgradeable, 2);
                    }
                    return owner;
                }
            }
        } // class AsyncReaderWriterLock
#endif // UNITY_2021_2_OR_NEWER || NETSTANDARD2_1_OR_GREATER || NETCOREAPP
    } // namespace Threading
} // namespace Proto.Promises.Threading