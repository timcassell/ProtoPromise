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
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal sealed class AsyncReaderLockPromise : PromiseRefBase.AsyncSynchronizationPromiseBase<AsyncReaderWriterLock.ReaderKey>, ICancelable, ILinked<AsyncReaderLockPromise>
        {
            AsyncReaderLockPromise ILinked<AsyncReaderLockPromise>.Next { get; set; }
            private AsyncReaderWriterLockInternal Owner => _result._impl._owner;

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
                promise.Reset(callerContext);
                promise._result = new AsyncReaderWriterLock.ReaderKey(owner); ; // This will be overwritten when this is resolved, we just store the key with the owner here for cancelation.
                return promise;
            }

            internal override void MaybeDispose()
            {
                Dispose();
                ObjectPool.MaybeRepool(this);
            }

            internal void Resolve(long currentKey)
            {
                ThrowIfInPool(this);

                // We don't need to check if the unregister was successful or not.
                // The fact that this was called means the cancelation was unable to unregister this from the lock.
                // We just dispose to wait for the callback to complete before we continue.
                _cancelationRegistration.Dispose();

                _result = new AsyncReaderWriterLock.ReaderKey(Owner, currentKey, this);
                Continue();
            }

            public override void Cancel()
            {
                ThrowIfInPool(this);
                if (!Owner.TryUnregister(this))
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
        internal sealed class AsyncWriterLockPromise : PromiseRefBase.AsyncSynchronizationPromiseBase<AsyncReaderWriterLock.WriterKey>, ICancelable, ILinked<AsyncWriterLockPromise>
        {
            AsyncWriterLockPromise ILinked<AsyncWriterLockPromise>.Next { get; set; }
            private AsyncReaderWriterLockInternal Owner => _result._impl._owner;

            [MethodImpl(InlineOption)]
            private static AsyncWriterLockPromise GetOrCreate()
            {
                var obj = ObjectPool.TryTakeOrInvalid<AsyncWriterLockPromise>();
                return obj == InvalidAwaitSentinel.s_instance
                    ? new AsyncWriterLockPromise()
                    : obj.UnsafeAs<AsyncWriterLockPromise>();
            }

            internal static AsyncWriterLockPromise GetOrCreate(AsyncReaderWriterLockInternal owner, SynchronizationContext callerContext)
            {
                var promise = GetOrCreate();
                promise.Reset(callerContext);
                promise._result = new AsyncReaderWriterLock.WriterKey(owner); // This will be overwritten when this is resolved, we just store the key with the owner here for cancelation.
                return promise;
            }

            internal override void MaybeDispose()
            {
                Dispose();
                ObjectPool.MaybeRepool(this);
            }

            internal void Resolve(long writerKey)
            {
                ThrowIfInPool(this);

                // We don't need to check if the unregister was successful or not.
                // The fact that this was called means the cancelation was unable to unregister this from the lock.
                // We just dispose to wait for the callback to complete before we continue.
                _cancelationRegistration.Dispose();

                _result = new AsyncReaderWriterLock.WriterKey(Owner, writerKey, this);
                Continue();
            }

            public override void Cancel()
            {
                ThrowIfInPool(this);
                if (!Owner.TryUnregister(this))
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
        internal sealed class AsyncUpgradeableReaderLockPromise : PromiseRefBase.AsyncSynchronizationPromiseBase<AsyncReaderWriterLock.UpgradeableReaderKey>, ICancelable, ILinked<AsyncUpgradeableReaderLockPromise>
        {
            AsyncUpgradeableReaderLockPromise ILinked<AsyncUpgradeableReaderLockPromise>.Next { get; set; }
            private AsyncReaderWriterLockInternal Owner => _result._impl._owner;

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
                promise.Reset(callerContext);
                promise._result = new AsyncReaderWriterLock.UpgradeableReaderKey(owner); // This will be overwritten when this is resolved, we just store the key with the owner here for cancelation.
                return promise;
            }

            internal override void MaybeDispose()
            {
                Dispose();
                ObjectPool.MaybeRepool(this);
            }

            internal void Resolve(long currentKey)
            {
                ThrowIfInPool(this);

                // We don't need to check if the unregister was successful or not.
                // The fact that this was called means the cancelation was unable to unregister this from the lock.
                // We just dispose to wait for the callback to complete before we continue.
                _cancelationRegistration.Dispose();

                _result = new AsyncReaderWriterLock.UpgradeableReaderKey(Owner, currentKey, this);
                Continue();
            }

            public override void Cancel()
            {
                ThrowIfInPool(this);
                if (!Owner.TryUnregister(this))
                {
                    return;
                }
                _tempState = Promise.State.Canceled;
                Continue();
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
        internal sealed partial class AsyncReaderWriterLockInternal
        {
            // If there is contention on the lock, we alternate between writers and readers, so one type of lock won't starve out the other.
            // We also alternate between upgraded writers and regular writers to prevent writer starvation.

            // These must not be readonly.
            private ValueLinkedQueue<AsyncReaderLockPromise> _readerQueue = new ValueLinkedQueue<AsyncReaderLockPromise>();
            private ValueLinkedQueue<AsyncWriterLockPromise> _writerQueue = new ValueLinkedQueue<AsyncWriterLockPromise>();
            private ValueLinkedQueue<AsyncUpgradeableReaderLockPromise> _upgradeQueue = new ValueLinkedQueue<AsyncUpgradeableReaderLockPromise>();
            private AsyncWriterLockPromise _upgradeWaiter;
            private long _currentKey;
            // This stores the previous reader key from an upgraded writer.
            // We store it here instead of in the WriterKey struct to prevent bloating other types that store the struct (like async state machines).
            private long _previousReaderKey;
            private uint _readerLockCount;
            private uint _readerWaitCount;
            volatile private AsyncReaderWriterLockType _lockType;
            private AsyncReaderWriterLockType _writerType;
            private readonly AsyncReaderWriterLock.ContentionStrategy _contentionStrategy;

            private bool PrioritizeWriters
            {
                [MethodImpl(InlineOption)]
                get { return _contentionStrategy == AsyncReaderWriterLock.ContentionStrategy.PrioritizeWriters; }
            }

            private bool PrioritizeReaders
            {
                [MethodImpl(InlineOption)]
                get { return _contentionStrategy == AsyncReaderWriterLock.ContentionStrategy.PrioritizeReaders; }
            }

            private bool PrioritizeUpgradeableReaders
            {
                [MethodImpl(InlineOption)]
                get { return _contentionStrategy == AsyncReaderWriterLock.ContentionStrategy.PrioritizeUpgradeableReaders; }
            }

            private bool PrioritizeReadersOrUpgradeableReaders
            {
                [MethodImpl(InlineOption)]
                get { return _contentionStrategy >= AsyncReaderWriterLock.ContentionStrategy.PrioritizeReaders; }
            }

            internal AsyncReaderWriterLockInternal(AsyncReaderWriterLock.ContentionStrategy contentionStrategy)
            {
                _contentionStrategy = contentionStrategy;
            }

            [MethodImpl(InlineOption)]
            private void SetNextKey()
            {
                _currentKey = KeyGenerator<AsyncReaderWriterLockInternal>.Next();
            }

            [MethodImpl(InlineOption)]
            private void SetNextAndPreviousKeys()
            {
                _previousReaderKey = _currentKey;
                SetNextKey();
            }

            [MethodImpl(InlineOption)]
            private void RestorePreviousKey()
            {
                _currentKey = _previousReaderKey;
            }

            [MethodImpl(InlineOption)]
            private void IncrementReaderLockCount()
            {
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                checked
#else
                unchecked
#endif
                {
                    ++_readerLockCount;
                }
            }

            [MethodImpl(InlineOption)]
            private void DecrementReaderLockCount()
            {
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                checked
#else
                unchecked
#endif
                {
                    --_readerLockCount;
                }
            }

            private void ValidateReaderCounts()
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

            [MethodImpl(InlineOption)]
            private bool CanEnterReaderLock(AsyncReaderWriterLockType currentLockType)
            {
                // Normal reader locks can be taken simultaneously along with an upgradeable reader lock.
                // If a writer lock is taken or waiting to be taken, and the contention strategy is not reader prioritized,
                // we wait for it to be released, to prevent starvation.
                bool noWriters = (currentLockType & AsyncReaderWriterLockType.Writer) == 0;
                bool noUpgradeWriter = (currentLockType & (AsyncReaderWriterLockType.Writer | AsyncReaderWriterLockType.Upgradeable)) == 0;
                bool writerDoesNotOwnLock = _writerType == AsyncReaderWriterLockType.None;
                bool prioritizedEntrance = writerDoesNotOwnLock & (PrioritizeReaders | (PrioritizeUpgradeableReaders & noUpgradeWriter));
                return noWriters | prioritizedEntrance;
            }

            internal Promise<AsyncReaderWriterLock.ReaderKey> ReaderLockAsync()
            {
                // We don't spinwait here because it's async; we want to return to caller as fast as possible.

                AsyncReaderLockPromise promise;
                lock (this)
                {
                    ValidateNotAbandoned();
                    ValidateReaderCounts();

                    // Read the _lockType into a local variable to avoid extra unnecessary volatile accesses inside the lock.
                    var lockType = _lockType;

                    if (CanEnterReaderLock(lockType))
                    {
                        IncrementReaderLockCount();
                        if (lockType == AsyncReaderWriterLockType.None)
                        {
                            SetNextKey();
                        }
                        _lockType = lockType | AsyncReaderWriterLockType.Reader;
                        return Promise.Resolved(new AsyncReaderWriterLock.ReaderKey(this, _currentKey, null));
                    }

                    // Unchecked context since we're manually checking overflow.
                    unchecked
                    {
                        ++_readerWaitCount;
                    }
                    promise = AsyncReaderLockPromise.GetOrCreate(this, CaptureContext());
                    _readerQueue.Enqueue(promise);
                }
                return new Promise<AsyncReaderWriterLock.ReaderKey>(promise, promise.Id, 0);
            }

            internal Promise<AsyncReaderWriterLock.ReaderKey> ReaderLockAsync(CancelationToken cancelationToken)
            {
                // We don't spinwait here because it's async; we want to return to caller as fast as possible.

                // Quick check to see if the token is already canceled before entering.
                if (cancelationToken.IsCancelationRequested)
                {
                    ValidateNotAbandoned();

                    return Promise<AsyncReaderWriterLock.ReaderKey>.Canceled();
                }

                AsyncReaderLockPromise promise;
                lock (this)
                {
                    ValidateNotAbandoned();
                    ValidateReaderCounts();

                    // Read the _lockType into a local variable to avoid extra unnecessary volatile accesses inside the lock.
                    var lockType = _lockType;

                    if (CanEnterReaderLock(lockType))
                    {
                        IncrementReaderLockCount();
                        if (lockType == AsyncReaderWriterLockType.None)
                        {
                            SetNextKey();
                        }
                        _lockType = lockType | AsyncReaderWriterLockType.Reader;
                        return Promise.Resolved(new AsyncReaderWriterLock.ReaderKey(this, _currentKey, null));
                    }

                    // Unchecked context since we're manually checking overflow.
                    unchecked
                    {
                        ++_readerWaitCount;
                    }
                    promise = AsyncReaderLockPromise.GetOrCreate(this, CaptureContext());
                    _readerQueue.Enqueue(promise);
                    promise.MaybeHookupCancelation(cancelationToken);
                }
                return new Promise<AsyncReaderWriterLock.ReaderKey>(promise, promise.Id, 0);
            }

            internal AsyncReaderWriterLock.ReaderKey ReaderLock()
            {
                // Since this is a synchronous lock, we do a short spinwait before yielding the thread.
                var spinner = new SpinWaitWithTimeout(Promise.Config.SpinTimeout);
                while (!CanEnterReaderLock(_lockType) & !spinner.NextSpinWillYield)
                {
                    spinner.SpinOnce();
                }

                AsyncReaderLockPromise promise;
                lock (this)
                {
                    ValidateNotAbandoned();
                    ValidateReaderCounts();

                    // Read the _lockType into a local variable to avoid extra unnecessary volatile accesses inside the lock.
                    var lockType = _lockType;

                    if (CanEnterReaderLock(lockType))
                    {
                        IncrementReaderLockCount();
                        if (lockType == AsyncReaderWriterLockType.None)
                        {
                            SetNextKey();
                        }
                        _lockType = lockType | AsyncReaderWriterLockType.Reader;
                        return new AsyncReaderWriterLock.ReaderKey(this, _currentKey, null);
                    }

                    // Unchecked context since we're manually checking overflow.
                    unchecked
                    {
                        ++_readerWaitCount;
                    }
                    promise = AsyncReaderLockPromise.GetOrCreate(this, null);
                    _readerQueue.Enqueue(promise);
                }
                PromiseSynchronousWaiter.TryWaitForResult(promise, promise.Id, TimeSpan.FromMilliseconds(Timeout.Infinite), out var resultContainer);
                resultContainer.RethrowIfRejected();
                return resultContainer.Value;
            }

            internal AsyncReaderWriterLock.ReaderKey ReaderLock(CancelationToken cancelationToken)
            {
                // Since this is a synchronous lock, we do a short spinwait before yielding the thread.
                var spinner = new SpinWaitWithTimeout(Promise.Config.SpinTimeout);
                bool isCanceled = cancelationToken.IsCancelationRequested;
                while (!CanEnterReaderLock(_lockType) & !isCanceled & !spinner.NextSpinWillYield)
                {
                    spinner.SpinOnce();
                    isCanceled = cancelationToken.IsCancelationRequested;
                }

                // Quick check to see if the token is already canceled before entering.
                if (isCanceled)
                {
                    ValidateNotAbandoned();

                    throw Promise.CancelException();
                }

                AsyncReaderLockPromise promise;
                lock (this)
                {
                    ValidateNotAbandoned();
                    ValidateReaderCounts();

                    // Read the _lockType into a local variable to avoid extra unnecessary volatile accesses inside the lock.
                    var lockType = _lockType;

                    if (CanEnterReaderLock(lockType))
                    {
                        IncrementReaderLockCount();
                        if (lockType == AsyncReaderWriterLockType.None)
                        {
                            SetNextKey();
                        }
                        _lockType = lockType | AsyncReaderWriterLockType.Reader;
                        return new AsyncReaderWriterLock.ReaderKey(this, _currentKey, null);
                    }

                    // Unchecked context since we're manually checking overflow.
                    unchecked
                    {
                        ++_readerWaitCount;
                    }
                    promise = AsyncReaderLockPromise.GetOrCreate(this, null);
                    _readerQueue.Enqueue(promise);
                    promise.MaybeHookupCancelation(cancelationToken);
                }
                PromiseSynchronousWaiter.TryWaitForResult(promise, promise.Id, TimeSpan.FromMilliseconds(Timeout.Infinite), out var resultContainer);
                resultContainer.RethrowIfRejectedOrCanceled();
                return resultContainer.Value;
            }

            internal bool TryEnterReaderLock(out AsyncReaderWriterLock.ReaderKey readerKey)
            {
                // We don't spinwait here because we want to return immediately without waiting.
                lock (this)
                {
                    ValidateNotAbandoned();
                    ValidateReaderCounts();

                    // Read the _lockType into a local variable to avoid extra unnecessary volatile accesses inside the lock.
                    var lockType = _lockType;

                    if (CanEnterReaderLock(lockType))
                    {
                        IncrementReaderLockCount();
                        if (lockType == AsyncReaderWriterLockType.None)
                        {
                            SetNextKey();
                        }
                        _lockType = lockType | AsyncReaderWriterLockType.Reader;
                        readerKey = new AsyncReaderWriterLock.ReaderKey(this, _currentKey, null);
                        return true;
                    }
                }
                readerKey = default;
                return false;
            }

            internal Promise<(bool didEnter, AsyncReaderWriterLock.ReaderKey readerKey)> TryEnterReaderLockAsync(CancelationToken cancelationToken)
            {
                // We don't spinwait here because it's async; we want to return to caller as fast as possible.

                AsyncReaderLockPromise promise;
                lock (this)
                {
                    ValidateNotAbandoned();
                    ValidateReaderCounts();

                    // Read the _lockType into a local variable to avoid extra unnecessary volatile accesses inside the lock.
                    var lockType = _lockType;

                    if (CanEnterReaderLock(lockType))
                    {
                        IncrementReaderLockCount();
                        if (lockType == AsyncReaderWriterLockType.None)
                        {
                            SetNextKey();
                        }
                        _lockType = lockType | AsyncReaderWriterLockType.Reader;
                        return Promise.Resolved((true, new AsyncReaderWriterLock.ReaderKey(this, _currentKey, null)));
                    }
                    // Quick check to see if the token is already canceled before waiting.
                    if (cancelationToken.IsCancelationRequested)
                    {
                        return Promise.Resolved((false, default(AsyncReaderWriterLock.ReaderKey)));
                    }

                    // Unchecked context since we're manually checking overflow.
                    unchecked
                    {
                        ++_readerWaitCount;
                    }
                    promise = AsyncReaderLockPromise.GetOrCreate(this, null);
                    _readerQueue.Enqueue(promise);
                    promise.MaybeHookupCancelation(cancelationToken);
                }
                return new Promise<AsyncReaderWriterLock.ReaderKey>(promise, promise.Id, 0)
                    .ContinueWith(resultContainer =>
                    {
                        resultContainer.RethrowIfRejected();
                        return (resultContainer.State == Promise.State.Resolved, resultContainer.Value);
                    });
            }

            internal bool TryEnterReaderLock(out AsyncReaderWriterLock.ReaderKey readerKey, CancelationToken cancelationToken)
            {
                // Since this is a synchronous lock, we do a short spinwait before yielding the thread.
                var spinner = new SpinWaitWithTimeout(Promise.Config.SpinTimeout);
                bool isCanceled = cancelationToken.IsCancelationRequested;
                while (!CanEnterReaderLock(_lockType) & !isCanceled & !spinner.NextSpinWillYield)
                {
                    spinner.SpinOnce();
                    isCanceled = cancelationToken.IsCancelationRequested;
                }

                AsyncReaderLockPromise promise;
                lock (this)
                {
                    ValidateNotAbandoned();
                    ValidateReaderCounts();

                    // Read the _lockType into a local variable to avoid extra unnecessary volatile accesses inside the lock.
                    var lockType = _lockType;

                    if (CanEnterReaderLock(lockType))
                    {
                        IncrementReaderLockCount();
                        if (lockType == AsyncReaderWriterLockType.None)
                        {
                            SetNextKey();
                        }
                        _lockType = lockType | AsyncReaderWriterLockType.Reader;
                        readerKey = new AsyncReaderWriterLock.ReaderKey(this, _currentKey, null);
                        return true;
                    }
                    // Quick check to see if the token is already canceled before waiting.
                    if (isCanceled)
                    {
                        readerKey = default;
                        return false;
                    }

                    // Unchecked context since we're manually checking overflow.
                    unchecked
                    {
                        ++_readerWaitCount;
                    }
                    promise = AsyncReaderLockPromise.GetOrCreate(this, null);
                    _readerQueue.Enqueue(promise);
                    promise.MaybeHookupCancelation(cancelationToken);
                }
                PromiseSynchronousWaiter.TryWaitForResult(promise, promise.Id, Timeout.InfiniteTimeSpan, out var resultContainer);
                resultContainer.RethrowIfRejected();
                readerKey = resultContainer.Value;
                return resultContainer.State == Promise.State.Resolved;
            }

            internal Promise<AsyncReaderWriterLock.WriterKey> WriterLockAsync()
            {
                // We don't spinwait here because it's async; we want to return to caller as fast as possible.
                AsyncWriterLockPromise promise;
                lock (this)
                {
                    ValidateNotAbandoned();

                    // Writer locks are mutually exclusive.
                    var lockType = _lockType;
                    _lockType = lockType | AsyncReaderWriterLockType.Writer;
                    if (lockType == AsyncReaderWriterLockType.None)
                    {
                        _writerType = AsyncReaderWriterLockType.Writer;
                        SetNextKey();
                        return Promise.Resolved(new AsyncReaderWriterLock.WriterKey(this, _currentKey, null));
                    }

                    promise = AsyncWriterLockPromise.GetOrCreate(this, CaptureContext());
                    _writerQueue.Enqueue(promise);
                }
                return new Promise<AsyncReaderWriterLock.WriterKey>(promise, promise.Id, 0);
            }

            internal Promise<AsyncReaderWriterLock.WriterKey> WriterLockAsync(CancelationToken cancelationToken)
            {
                // We don't spinwait here because it's async; we want to return to caller as fast as possible.
                
                // Quick check to see if the token is already canceled before entering.
                if (cancelationToken.IsCancelationRequested)
                {
                    ValidateNotAbandoned();

                    return Promise<AsyncReaderWriterLock.WriterKey>.Canceled();
                }

                AsyncWriterLockPromise promise;
                lock (this)
                {
                    ValidateNotAbandoned();

                    // Writer locks are mutually exclusive.
                    var lockType = _lockType;
                    _lockType = lockType | AsyncReaderWriterLockType.Writer;
                    if (lockType == AsyncReaderWriterLockType.None)
                    {
                        _writerType = AsyncReaderWriterLockType.Writer;
                        SetNextKey();
                        return Promise.Resolved(new AsyncReaderWriterLock.WriterKey(this, _currentKey, null));
                    }

                    promise = AsyncWriterLockPromise.GetOrCreate(this, CaptureContext());
                    _writerQueue.Enqueue(promise);
                    promise.MaybeHookupCancelation(cancelationToken);
                }
                return new Promise<AsyncReaderWriterLock.WriterKey>(promise, promise.Id, 0);
            }

            internal AsyncReaderWriterLock.WriterKey WriterLock()
            {
                // Since this is a synchronous lock, we do a short spinwait before yielding the thread.
                var spinner = new SpinWaitWithTimeout(Promise.Config.SpinTimeout);
                while (_lockType != AsyncReaderWriterLockType.None & !spinner.NextSpinWillYield)
                {
                    spinner.SpinOnce();
                }

                AsyncWriterLockPromise promise;
                lock (this)
                {
                    ValidateNotAbandoned();

                    // Writer locks are mutually exclusive.
                    var lockType = _lockType;
                    _lockType = lockType | AsyncReaderWriterLockType.Writer;
                    if (lockType == AsyncReaderWriterLockType.None)
                    {
                        _writerType = AsyncReaderWriterLockType.Writer;
                        SetNextKey();
                        return new AsyncReaderWriterLock.WriterKey(this, _currentKey, null);
                    }

                    promise = AsyncWriterLockPromise.GetOrCreate(this, null);
                    _writerQueue.Enqueue(promise);
                }
                PromiseSynchronousWaiter.TryWaitForResult(promise, promise.Id, TimeSpan.FromMilliseconds(Timeout.Infinite), out var resultContainer);
                resultContainer.RethrowIfRejected();
                return resultContainer.Value;
            }

            internal AsyncReaderWriterLock.WriterKey WriterLock(CancelationToken cancelationToken)
            {
                // Since this is a synchronous lock, we do a short spinwait before yielding the thread.
                var spinner = new SpinWaitWithTimeout(Promise.Config.SpinTimeout);
                bool isCanceled = cancelationToken.IsCancelationRequested;
                while (_lockType != AsyncReaderWriterLockType.None & !isCanceled & !spinner.NextSpinWillYield)
                {
                    spinner.SpinOnce();
                    isCanceled = cancelationToken.IsCancelationRequested;
                }

                // Quick check to see if the token is already canceled before entering.
                if (isCanceled)
                {
                    ValidateNotAbandoned();

                    throw Promise.CancelException();
                }

                AsyncWriterLockPromise promise;
                lock (this)
                {
                    ValidateNotAbandoned();

                    // Writer locks are mutually exclusive.
                    var lockType = _lockType;
                    _lockType = lockType | AsyncReaderWriterLockType.Writer;
                    if (lockType == AsyncReaderWriterLockType.None)
                    {
                        _writerType = AsyncReaderWriterLockType.Writer;
                        SetNextKey();
                        return new AsyncReaderWriterLock.WriterKey(this, _currentKey, null);
                    }

                    promise = AsyncWriterLockPromise.GetOrCreate(this, null);
                    _writerQueue.Enqueue(promise);
                    promise.MaybeHookupCancelation(cancelationToken);
                }
                PromiseSynchronousWaiter.TryWaitForResult(promise, promise.Id, TimeSpan.FromMilliseconds(Timeout.Infinite), out var resultContainer);
                resultContainer.RethrowIfRejectedOrCanceled();
                return resultContainer.Value;
            }

            internal bool TryEnterWriterLock(out AsyncReaderWriterLock.WriterKey writerKey)
            {
                // We don't spinwait here because we want to return immediately without waiting.
                lock (this)
                {
                    ValidateNotAbandoned();

                    // Writer locks are mutually exclusive.
                    var lockType = _lockType;
                    if (lockType == AsyncReaderWriterLockType.None)
                    {
                        _lockType = lockType | AsyncReaderWriterLockType.Writer;
                        _writerType = AsyncReaderWriterLockType.Writer;
                        SetNextKey();
                        writerKey = new AsyncReaderWriterLock.WriterKey(this, _currentKey, null);
                        return true;
                    }
                }
                writerKey = default;
                return false;
            }

            internal Promise<(bool didEnter, AsyncReaderWriterLock.WriterKey writerKey)> TryEnterWriterLockAsync(CancelationToken cancelationToken)
            {
                // We don't spinwait here because it's async; we want to return to caller as fast as possible.
                AsyncWriterLockPromise promise;
                lock (this)
                {
                    ValidateNotAbandoned();

                    // Writer locks are mutually exclusive.
                    var lockType = _lockType;
                    if (lockType == AsyncReaderWriterLockType.None)
                    {
                        _lockType = lockType | AsyncReaderWriterLockType.Writer;
                        _writerType = AsyncReaderWriterLockType.Writer;
                        SetNextKey();
                        return Promise.Resolved((true, new AsyncReaderWriterLock.WriterKey(this, _currentKey, null)));
                    }
                    // Quick check to see if the token is already canceled before waiting.
                    if (cancelationToken.IsCancelationRequested)
                    {
                        return Promise.Resolved((false, default(AsyncReaderWriterLock.WriterKey)));
                    }

                    _lockType = lockType | AsyncReaderWriterLockType.Writer;
                    promise = AsyncWriterLockPromise.GetOrCreate(this, CaptureContext());
                    _writerQueue.Enqueue(promise);
                    promise.MaybeHookupCancelation(cancelationToken);
                }
                return new Promise<AsyncReaderWriterLock.WriterKey>(promise, promise.Id, 0)
                    .ContinueWith(resultContainer =>
                    {
                        resultContainer.RethrowIfRejected();
                        return (resultContainer.State == Promise.State.Resolved, resultContainer.Value);
                    });
            }

            internal bool TryEnterWriterLock(out AsyncReaderWriterLock.WriterKey writerKey, CancelationToken cancelationToken)
            {
                // Since this is a synchronous lock, we do a short spinwait before yielding the thread.
                var spinner = new SpinWaitWithTimeout(Promise.Config.SpinTimeout);
                bool isCanceled = cancelationToken.IsCancelationRequested;
                while (_lockType != AsyncReaderWriterLockType.None & !isCanceled & !spinner.NextSpinWillYield)
                {
                    spinner.SpinOnce();
                    isCanceled = cancelationToken.IsCancelationRequested;
                }

                AsyncWriterLockPromise promise;
                lock (this)
                {
                    ValidateNotAbandoned();

                    // Writer locks are mutually exclusive.
                    var lockType = _lockType;
                    if (lockType == AsyncReaderWriterLockType.None)
                    {
                        _lockType = lockType | AsyncReaderWriterLockType.Writer;
                        _writerType = AsyncReaderWriterLockType.Writer;
                        SetNextKey();
                        writerKey = new AsyncReaderWriterLock.WriterKey(this, _currentKey, null);
                        return true;
                    }
                    // Quick check to see if the token is already canceled before waiting.
                    if (isCanceled)
                    {
                        writerKey = default;
                        return false;
                    }

                    _lockType = lockType | AsyncReaderWriterLockType.Writer;
                    promise = AsyncWriterLockPromise.GetOrCreate(this, CaptureContext());
                    _writerQueue.Enqueue(promise);
                    promise.MaybeHookupCancelation(cancelationToken);
                }
                PromiseSynchronousWaiter.TryWaitForResult(promise, promise.Id, Timeout.InfiniteTimeSpan, out var resultContainer);
                resultContainer.RethrowIfRejected();
                writerKey = resultContainer.Value;
                return resultContainer.State == Promise.State.Resolved;
            }

            [MethodImpl(InlineOption)]
            private bool CanEnterUpgradeableReaderLock(AsyncReaderWriterLockType currentLockType)
            {
                // Normal reader locks can be taken simultaneously along with an upgradeable reader lock.
                // Only 1 upgradeable reader lock is allowed at a time.
                // If a writer lock is taken or waiting to be taken, and the contention strategy is not reader prioritized,
                // we wait for it to be released, to prevent starvation.
                bool noWritersOrUpgradeableReaders = currentLockType <= AsyncReaderWriterLockType.Reader;
                bool writerDoesNotOwnLock = _writerType == AsyncReaderWriterLockType.None;
                bool noUpgradeableReaders = _upgradeQueue.IsEmpty & (currentLockType & AsyncReaderWriterLockType.Upgradeable) == 0;
                bool prioritizedEntrance = writerDoesNotOwnLock & noUpgradeableReaders & PrioritizeReadersOrUpgradeableReaders;
                return noWritersOrUpgradeableReaders | prioritizedEntrance;
            }

            internal Promise<AsyncReaderWriterLock.UpgradeableReaderKey> UpgradeableReaderLockAsync()
            {
                // We don't spinwait here because it's async; we want to return to caller as fast as possible.
                AsyncUpgradeableReaderLockPromise promise;
                lock (this)
                {
                    ValidateNotAbandoned();

                    // Read the _lockType into a local variable to avoid extra unnecessary volatile accesses inside the lock.
                    var lockType = _lockType;

                    if (CanEnterUpgradeableReaderLock(lockType))
                    {
                        IncrementReaderLockCount();
                        if (lockType == AsyncReaderWriterLockType.None)
                        {
                            SetNextKey();
                        }
                        _lockType = lockType | AsyncReaderWriterLockType.Upgradeable;
                        return Promise.Resolved(new AsyncReaderWriterLock.UpgradeableReaderKey(this, _currentKey, null));
                    }

                    promise = AsyncUpgradeableReaderLockPromise.GetOrCreate(this, CaptureContext());
                    _upgradeQueue.Enqueue(promise);
                }
                return new Promise<AsyncReaderWriterLock.UpgradeableReaderKey>(promise, promise.Id, 0);
            }

            internal Promise<AsyncReaderWriterLock.UpgradeableReaderKey> UpgradeableReaderLockAsync(CancelationToken cancelationToken)
            {
                // We don't spinwait here because it's async; we want to return to caller as fast as possible.
                
                // Quick check to see if the token is already canceled before entering.
                if (cancelationToken.IsCancelationRequested)
                {
                    ValidateNotAbandoned();

                    return Promise<AsyncReaderWriterLock.UpgradeableReaderKey>.Canceled();
                }

                AsyncUpgradeableReaderLockPromise promise;
                lock (this)
                {
                    ValidateNotAbandoned();

                    // Read the _lockType into a local variable to avoid extra unnecessary volatile accesses inside the lock.
                    var lockType = _lockType;

                    if (CanEnterUpgradeableReaderLock(lockType))
                    {
                        IncrementReaderLockCount();
                        if (lockType == AsyncReaderWriterLockType.None)
                        {
                            SetNextKey();
                        }
                        _lockType = lockType | AsyncReaderWriterLockType.Upgradeable;
                        return Promise.Resolved(new AsyncReaderWriterLock.UpgradeableReaderKey(this, _currentKey, null));
                    }

                    promise = AsyncUpgradeableReaderLockPromise.GetOrCreate(this, CaptureContext());
                    _upgradeQueue.Enqueue(promise);
                    promise.MaybeHookupCancelation(cancelationToken);
                }
                return new Promise<AsyncReaderWriterLock.UpgradeableReaderKey>(promise, promise.Id, 0);
            }

            internal AsyncReaderWriterLock.UpgradeableReaderKey UpgradeableReaderLock()
            {
                // Since this is a synchronous lock, we do a short spinwait before yielding the thread.
                var spinner = new SpinWaitWithTimeout(Promise.Config.SpinTimeout);
                while (!CanEnterUpgradeableReaderLock(_lockType) & !spinner.NextSpinWillYield)
                {
                    spinner.SpinOnce();
                }

                AsyncUpgradeableReaderLockPromise promise;
                lock (this)
                {
                    ValidateNotAbandoned();

                    // Read the _lockType into a local variable to avoid extra unnecessary volatile accesses inside the lock.
                    var lockType = _lockType;

                    if (CanEnterUpgradeableReaderLock(lockType))
                    {
                        IncrementReaderLockCount();
                        if (lockType == AsyncReaderWriterLockType.None)
                        {
                            SetNextKey();
                        }
                        _lockType = lockType | AsyncReaderWriterLockType.Upgradeable;
                        return new AsyncReaderWriterLock.UpgradeableReaderKey(this, _currentKey, null);
                    }

                    promise = AsyncUpgradeableReaderLockPromise.GetOrCreate(this, null);
                    _upgradeQueue.Enqueue(promise);
                }
                PromiseSynchronousWaiter.TryWaitForResult(promise, promise.Id, TimeSpan.FromMilliseconds(Timeout.Infinite), out var resultContainer);
                resultContainer.RethrowIfRejected();
                return resultContainer.Value;
            }

            internal AsyncReaderWriterLock.UpgradeableReaderKey UpgradeableReaderLock(CancelationToken cancelationToken)
            {
                // Since this is a synchronous lock, we do a short spinwait before yielding the thread.
                var spinner = new SpinWaitWithTimeout(Promise.Config.SpinTimeout);
                bool isCanceled = cancelationToken.IsCancelationRequested;
                while (!CanEnterUpgradeableReaderLock(_lockType) & !isCanceled & !spinner.NextSpinWillYield)
                {
                    spinner.SpinOnce();
                    isCanceled = cancelationToken.IsCancelationRequested;
                }

                // Quick check to see if the token is already canceled before entering.
                if (isCanceled)
                {
                    ValidateNotAbandoned();

                    throw Promise.CancelException();
                }

                AsyncUpgradeableReaderLockPromise promise;
                lock (this)
                {
                    ValidateNotAbandoned();

                    // Read the _lockType into a local variable to avoid extra unnecessary volatile accesses inside the lock.
                    var lockType = _lockType;

                    if (CanEnterUpgradeableReaderLock(lockType))
                    {
                        IncrementReaderLockCount();
                        if (lockType == AsyncReaderWriterLockType.None)
                        {
                            SetNextKey();
                        }
                        _lockType = lockType | AsyncReaderWriterLockType.Upgradeable;
                        return new AsyncReaderWriterLock.UpgradeableReaderKey(this, _currentKey, null);
                    }

                    promise = AsyncUpgradeableReaderLockPromise.GetOrCreate(this, null);
                    _upgradeQueue.Enqueue(promise);
                    promise.MaybeHookupCancelation(cancelationToken);
                }
                PromiseSynchronousWaiter.TryWaitForResult(promise, promise.Id, TimeSpan.FromMilliseconds(Timeout.Infinite), out var resultContainer);
                resultContainer.RethrowIfRejectedOrCanceled();
                return resultContainer.Value;
            }

            internal bool TryEnterUpgradeableReaderLock(out AsyncReaderWriterLock.UpgradeableReaderKey readerKey)
            {
                // We don't spinwait here because we want to return immediately without waiting.
                lock (this)
                {
                    ValidateNotAbandoned();

                    // Read the _lockType into a local variable to avoid extra unnecessary volatile accesses inside the lock.
                    var lockType = _lockType;

                    if (CanEnterUpgradeableReaderLock(lockType))
                    {
                        IncrementReaderLockCount();
                        if (lockType == AsyncReaderWriterLockType.None)
                        {
                            SetNextKey();
                        }
                        _lockType = lockType | AsyncReaderWriterLockType.Upgradeable;
                        readerKey = new AsyncReaderWriterLock.UpgradeableReaderKey(this, _currentKey, null);
                        return true;
                    }
                }
                readerKey = default;
                return false;
            }

            internal Promise<(bool didEnter, AsyncReaderWriterLock.UpgradeableReaderKey readerKey)> TryEnterUpgradeableReaderLockAsync(CancelationToken cancelationToken)
            {
                // We don't spinwait here because it's async; we want to return to caller as fast as possible.
                AsyncUpgradeableReaderLockPromise promise;
                lock (this)
                {
                    ValidateNotAbandoned();

                    // Read the _lockType into a local variable to avoid extra unnecessary volatile accesses inside the lock.
                    var lockType = _lockType;

                    if (CanEnterUpgradeableReaderLock(lockType))
                    {
                        IncrementReaderLockCount();
                        if (lockType == AsyncReaderWriterLockType.None)
                        {
                            SetNextKey();
                        }
                        _lockType = lockType | AsyncReaderWriterLockType.Upgradeable;
                        return Promise.Resolved((true, new AsyncReaderWriterLock.UpgradeableReaderKey(this, _currentKey, null)));
                    }
                    // Quick check to see if the token is already canceled before waiting.
                    if (cancelationToken.IsCancelationRequested)
                    {
                        return Promise.Resolved((false, default(AsyncReaderWriterLock.UpgradeableReaderKey)));
                    }

                    promise = AsyncUpgradeableReaderLockPromise.GetOrCreate(this, CaptureContext());
                    _upgradeQueue.Enqueue(promise);
                    promise.MaybeHookupCancelation(cancelationToken);
                }
                return new Promise<AsyncReaderWriterLock.UpgradeableReaderKey>(promise, promise.Id, 0)
                    .ContinueWith(resultContainer =>
                    {
                        resultContainer.RethrowIfRejected();
                        return (resultContainer.State == Promise.State.Resolved, resultContainer.Value);
                    });
            }

            internal bool TryEnterUpgradeableReaderLock(out AsyncReaderWriterLock.UpgradeableReaderKey readerKey, CancelationToken cancelationToken)
            {
                // Since this is a synchronous lock, we do a short spinwait before yielding the thread.
                var spinner = new SpinWaitWithTimeout(Promise.Config.SpinTimeout);
                bool isCanceled = cancelationToken.IsCancelationRequested;
                while (!CanEnterUpgradeableReaderLock(_lockType) & !isCanceled & !spinner.NextSpinWillYield)
                {
                    spinner.SpinOnce();
                    isCanceled = cancelationToken.IsCancelationRequested;
                }

                AsyncUpgradeableReaderLockPromise promise;
                lock (this)
                {
                    ValidateNotAbandoned();

                    // Read the _lockType into a local variable to avoid extra unnecessary volatile accesses inside the lock.
                    var lockType = _lockType;

                    if (CanEnterUpgradeableReaderLock(lockType))
                    {
                        IncrementReaderLockCount();
                        if (lockType == AsyncReaderWriterLockType.None)
                        {
                            SetNextKey();
                        }
                        _lockType = lockType | AsyncReaderWriterLockType.Upgradeable;
                        readerKey = new AsyncReaderWriterLock.UpgradeableReaderKey(this, _currentKey, null);
                        return true;
                    }
                    // Quick check to see if the token is already canceled before waiting.
                    if (isCanceled)
                    {
                        readerKey = default;
                        return false;
                    }

                    promise = AsyncUpgradeableReaderLockPromise.GetOrCreate(this, CaptureContext());
                    _upgradeQueue.Enqueue(promise);
                    promise.MaybeHookupCancelation(cancelationToken);
                }
                PromiseSynchronousWaiter.TryWaitForResult(promise, promise.Id, Timeout.InfiniteTimeSpan, out var resultContainer);
                resultContainer.RethrowIfRejected();
                readerKey = resultContainer.Value;
                return resultContainer.State == Promise.State.Resolved;
            }

            internal Promise<AsyncReaderWriterLock.WriterKey> UpgradeToWriterLockAsync(AsyncReaderWriterLock.UpgradeableReaderKey readerKey)
            {
                // We don't spinwait here because it's async; we want to return to caller as fast as possible.
                AsyncWriterLockPromise promise;
                lock (this)
                {
                    ValidateNotAbandoned();

                    if (_currentKey != readerKey._impl._key | this != readerKey._impl._owner)
                    {
                        ThrowInvalidKey(AsyncReaderWriterLockType.Upgradeable, 2);
                    }

                    DecrementReaderLockCount();
                    _lockType |= AsyncReaderWriterLockType.Writer;
                    if (_readerLockCount == 0)
                    {
                        _writerType = AsyncReaderWriterLockType.Upgradeable;
                        // We cache the previous key for when the lock gets downgraded.
                        SetNextAndPreviousKeys();
                        return Promise.Resolved(new AsyncReaderWriterLock.WriterKey(this, _currentKey, null));
                    }

                    promise = AsyncWriterLockPromise.GetOrCreate(this, CaptureContext());
                    _upgradeWaiter = promise;
                }
                return new Promise<AsyncReaderWriterLock.WriterKey>(promise, promise.Id, 0);
            }

            internal Promise<AsyncReaderWriterLock.WriterKey> UpgradeToWriterLockAsync(AsyncReaderWriterLock.UpgradeableReaderKey readerKey, CancelationToken cancelationToken)
            {
                // We don't spinwait here because it's async; we want to return to caller as fast as possible.
                
                // Quick check to see if the token is already canceled before entering.
                if (cancelationToken.IsCancelationRequested)
                {
                    ValidateNotAbandoned();

                    return Promise<AsyncReaderWriterLock.WriterKey>.Canceled();
                }

                AsyncWriterLockPromise promise;
                lock (this)
                {
                    ValidateNotAbandoned();

                    if (_currentKey != readerKey._impl._key | this != readerKey._impl._owner)
                    {
                        ThrowInvalidKey(AsyncReaderWriterLockType.Upgradeable, 2);
                    }

                    DecrementReaderLockCount();
                    _lockType |= AsyncReaderWriterLockType.Writer;
                    if (_readerLockCount == 0)
                    {
                        _writerType = AsyncReaderWriterLockType.Upgradeable;
                        // We cache the previous key for when the lock gets downgraded.
                        SetNextAndPreviousKeys();
                        return Promise.Resolved(new AsyncReaderWriterLock.WriterKey(this, _currentKey, null));
                    }

                    promise = AsyncWriterLockPromise.GetOrCreate(this, CaptureContext());
                    _upgradeWaiter = promise;
                    promise.MaybeHookupCancelation(cancelationToken);
                }
                return new Promise<AsyncReaderWriterLock.WriterKey>(promise, promise.Id, 0);
            }

            internal AsyncReaderWriterLock.WriterKey UpgradeToWriterLock(AsyncReaderWriterLock.UpgradeableReaderKey readerKey)
            {
                // Since this is a synchronous lock, we do a short spinwait before yielding the thread.
                var spinner = new SpinWaitWithTimeout(Promise.Config.SpinTimeout);
                while (_readerLockCount != 1 & !spinner.NextSpinWillYield)
                {
                    spinner.SpinOnce();
                }

                AsyncWriterLockPromise promise;
                lock (this)
                {
                    ValidateNotAbandoned();

                    if (_currentKey != readerKey._impl._key | this != readerKey._impl._owner)
                    {
                        ThrowInvalidKey(AsyncReaderWriterLockType.Upgradeable, 2);
                    }

                    DecrementReaderLockCount();
                    _lockType |= AsyncReaderWriterLockType.Writer;
                    if (_readerLockCount == 0)
                    {
                        _writerType = AsyncReaderWriterLockType.Upgradeable;
                        // We cache the previous key for when the lock gets downgraded.
                        SetNextAndPreviousKeys();
                        return new AsyncReaderWriterLock.WriterKey(this, _currentKey, null);
                    }

                    promise = AsyncWriterLockPromise.GetOrCreate(this, null);
                    _upgradeWaiter = promise;
                }
                PromiseSynchronousWaiter.TryWaitForResult(promise, promise.Id, TimeSpan.FromMilliseconds(Timeout.Infinite), out var resultContainer);
                resultContainer.RethrowIfRejected();
                return resultContainer.Value;
            }

            internal AsyncReaderWriterLock.WriterKey UpgradeToWriterLock(AsyncReaderWriterLock.UpgradeableReaderKey readerKey, CancelationToken cancelationToken)
            {
                // Since this is a synchronous lock, we do a short spinwait before yielding the thread.
                var spinner = new SpinWaitWithTimeout(Promise.Config.SpinTimeout);
                bool isCanceled = cancelationToken.IsCancelationRequested;
                while (_readerLockCount != 1 & !isCanceled & !spinner.NextSpinWillYield)
                {
                    spinner.SpinOnce();
                    isCanceled = cancelationToken.IsCancelationRequested;
                }

                // Quick check to see if the token is already canceled before entering.
                if (isCanceled)
                {
                    ValidateNotAbandoned();

                    throw Promise.CancelException();
                }

                AsyncWriterLockPromise promise;
                lock (this)
                {
                    ValidateNotAbandoned();

                    if (_currentKey != readerKey._impl._key | this != readerKey._impl._owner)
                    {
                        ThrowInvalidKey(AsyncReaderWriterLockType.Upgradeable, 2);
                    }

                    DecrementReaderLockCount();
                    _lockType |= AsyncReaderWriterLockType.Writer;
                    if (_readerLockCount == 0)
                    {
                        _writerType = AsyncReaderWriterLockType.Upgradeable;
                        // We cache the previous key for when the lock gets downgraded.
                        SetNextAndPreviousKeys();
                        return new AsyncReaderWriterLock.WriterKey(this, _currentKey, null);
                    }

                    promise = AsyncWriterLockPromise.GetOrCreate(this, null);
                    _upgradeWaiter = promise;
                    promise.MaybeHookupCancelation(cancelationToken);
                }
                PromiseSynchronousWaiter.TryWaitForResult(promise, promise.Id, TimeSpan.FromMilliseconds(Timeout.Infinite), out var resultContainer);
                resultContainer.RethrowIfRejectedOrCanceled();
                return resultContainer.Value;
            }

            internal bool TryUpgradeToWriterLock(AsyncReaderWriterLock.UpgradeableReaderKey readerKey, out AsyncReaderWriterLock.WriterKey writerKey)
            {
                // We don't spinwait here because we want to return immediately without waiting.
                lock (this)
                {
                    ValidateNotAbandoned();

                    if (_currentKey != readerKey._impl._key | this != readerKey._impl._owner)
                    {
                        ThrowInvalidKey(AsyncReaderWriterLockType.Upgradeable, 2);
                    }

                    if (_readerLockCount == 1)
                    {
                        DecrementReaderLockCount();
                        _lockType |= AsyncReaderWriterLockType.Writer;
                        _writerType = AsyncReaderWriterLockType.Upgradeable;
                        // We cache the previous key for when the lock gets downgraded.
                        SetNextAndPreviousKeys();
                        writerKey = new AsyncReaderWriterLock.WriterKey(this, _currentKey, null);
                        return true;
                    }
                }
                writerKey = default;
                return false;
            }

            internal Promise<(bool didEnter, AsyncReaderWriterLock.WriterKey writerKey)> TryUpgradeToWriterLockAsync(AsyncReaderWriterLock.UpgradeableReaderKey readerKey, CancelationToken cancelationToken)
            {
                // We don't spinwait here because it's async; we want to return to caller as fast as possible.
                AsyncWriterLockPromise promise;
                lock (this)
                {
                    ValidateNotAbandoned();

                    if (_currentKey != readerKey._impl._key | this != readerKey._impl._owner)
                    {
                        ThrowInvalidKey(AsyncReaderWriterLockType.Upgradeable, 2);
                    }

                    if (_readerLockCount == 1)
                    {
                        DecrementReaderLockCount();
                        _lockType |= AsyncReaderWriterLockType.Writer;
                        _writerType = AsyncReaderWriterLockType.Upgradeable;
                        // We cache the previous key for when the lock gets downgraded.
                        SetNextAndPreviousKeys();
                        return Promise.Resolved((true, new AsyncReaderWriterLock.WriterKey(this, _currentKey, null)));
                    }
                    // Quick check to see if the token is already canceled before waiting.
                    if (cancelationToken.IsCancelationRequested)
                    {
                        return Promise.Resolved((false, default(AsyncReaderWriterLock.WriterKey)));
                    }

                    DecrementReaderLockCount();
                    _lockType |= AsyncReaderWriterLockType.Writer;
                    promise = AsyncWriterLockPromise.GetOrCreate(this, CaptureContext());
                    _upgradeWaiter = promise;
                    promise.MaybeHookupCancelation(cancelationToken);
                }
                return new Promise<AsyncReaderWriterLock.WriterKey>(promise, promise.Id, 0)
                    .ContinueWith(resultContainer =>
                    {
                        resultContainer.RethrowIfRejected();
                        return (resultContainer.State == Promise.State.Resolved, resultContainer.Value);
                    });
            }

            internal bool TryUpgradeToWriterLock(AsyncReaderWriterLock.UpgradeableReaderKey readerKey, out AsyncReaderWriterLock.WriterKey writerKey, CancelationToken cancelationToken)
            {
                // Since this is a synchronous lock, we do a short spinwait before yielding the thread.
                var spinner = new SpinWaitWithTimeout(Promise.Config.SpinTimeout);
                bool isCanceled = cancelationToken.IsCancelationRequested;
                while (_readerLockCount != 1 & !isCanceled & !spinner.NextSpinWillYield)
                {
                    spinner.SpinOnce();
                    isCanceled = cancelationToken.IsCancelationRequested;
                }

                AsyncWriterLockPromise promise;
                lock (this)
                {
                    ValidateNotAbandoned();

                    if (_currentKey != readerKey._impl._key | this != readerKey._impl._owner)
                    {
                        ThrowInvalidKey(AsyncReaderWriterLockType.Upgradeable, 2);
                    }

                    if (_readerLockCount == 1)
                    {
                        DecrementReaderLockCount();
                        _lockType |= AsyncReaderWriterLockType.Writer;
                        _writerType = AsyncReaderWriterLockType.Upgradeable;
                        // We cache the previous key for when the lock gets downgraded.
                        SetNextAndPreviousKeys();
                        writerKey = new AsyncReaderWriterLock.WriterKey(this, _currentKey, null);
                        return true;
                    }
                    // Quick check to see if the token is already canceled before waiting.
                    if (isCanceled)
                    {
                        writerKey = default;
                        return false;
                    }

                    DecrementReaderLockCount();
                    _lockType |= AsyncReaderWriterLockType.Writer;
                    promise = AsyncWriterLockPromise.GetOrCreate(this, CaptureContext());
                    _upgradeWaiter = promise;
                    promise.MaybeHookupCancelation(cancelationToken);
                }
                PromiseSynchronousWaiter.TryWaitForResult(promise, promise.Id, Timeout.InfiniteTimeSpan, out var resultContainer);
                resultContainer.RethrowIfRejected();
                writerKey = resultContainer.Value;
                return resultContainer.State == Promise.State.Resolved;
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
                        _writerType = AsyncReaderWriterLockType.None;
                        _currentKey = 0L;
                        return;
                    }
                } // lock

                promise.Resolve(_currentKey);
            }

            internal void ReleaseWriterLock(long key)
            {
                // TODO: create UpgradedWriterKey to use instead of WriterKey to remove branches.
                // Will be a breaking change, so probably has to wait until a major version update (v3).

                if (_writerType == AsyncReaderWriterLockType.Writer)
                {
                    ReleaseNormalWriterLock(key);
                }
                else
                {
                    ReleaseUpgradedWriterLock(key);
                }
            }

            internal void ReleaseNormalWriterLock(long key)
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

                    // If there are any normal readers waiting, we resolve them, even if there are more writer waiters.
                    // This prevents writers from starving readers.
                    // Unless there is a waiting writer, and the contention strategy is writer prioritized.
                    bool hasWaitingWriter = _writerQueue.IsNotEmpty;
                    bool prioritizedWriter = hasWaitingWriter & PrioritizeWriters;
                    if (_readerWaitCount != 0 & !prioritizedWriter)
                    {
                        SetNextKey();
                        var lockType = hasWaitingWriter
                            ? AsyncReaderWriterLockType.Reader | AsyncReaderWriterLockType.Writer
                            : AsyncReaderWriterLockType.Reader;
                        _writerType = AsyncReaderWriterLockType.None;
                        _readerLockCount = _readerWaitCount;
                        _readerWaitCount = 0;
                        readers = _readerQueue.MoveElementsToStack();
                        if (_upgradeQueue.IsEmpty)
                        {
                            _lockType = lockType;
                            goto ResolveReaders;
                        }
                        IncrementReaderLockCount();
                        _lockType = lockType | AsyncReaderWriterLockType.Upgradeable;
                        upgradeablePromise = _upgradeQueue.Dequeue();
                        goto ResolveReadersAndUpgradeableReader;
                    }

                    // Check upgrade queue before writer queue, to prevent writers from starving upgradeable readers.
                    // Unless there is a waiting writer, and the contention strategy is writer prioritized.
                    if (_upgradeQueue.IsNotEmpty & !prioritizedWriter)
                    {
                        _lockType = hasWaitingWriter
                            ? _lockType | AsyncReaderWriterLockType.Upgradeable
                            : AsyncReaderWriterLockType.Upgradeable;
                        _writerType = AsyncReaderWriterLockType.None;
                        IncrementReaderLockCount();
                        upgradeablePromise = _upgradeQueue.Dequeue();
                        goto ResolveUpgradeableReader;
                    }
                    else if (hasWaitingWriter)
                    {
                        SetNextKey();
                        writerPromise = _writerQueue.Dequeue();
                    }
                    else
                    {
                        // The lock is exited completely.
                        _lockType = AsyncReaderWriterLockType.None;
                        _writerType = AsyncReaderWriterLockType.None;
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

            internal void ReleaseUpgradedWriterLock(long key)
            {
                ValueLinkedStack<AsyncReaderLockPromise> readers;
                lock (this)
                {
                    if (_currentKey != key)
                    {
                        ThrowInvalidKey(AsyncReaderWriterLockType.Writer, 2);
                    }

                    // This reverts the key to the previous reader key so that releasing the UpgradeableReaderKey will work.
                    RestorePreviousKey();

                    // If there are any normal readers waiting, we resolve them, even if there are more writer waiters.
                    // This prevents writers from starving readers.
                    // Unless there is a waiting writer, and the contention strategy is writer prioritized.
                    bool hasWaitingWriter = _writerQueue.IsNotEmpty;
                    bool prioritizedWriter = hasWaitingWriter & PrioritizeWriters;
                    if (_readerQueue.IsEmpty | prioritizedWriter)
                    {
                        // We set reader count to 1 since it was removed when the lock was upgraded.
                        _readerLockCount = 1;
                        _lockType = hasWaitingWriter
                            ? AsyncReaderWriterLockType.Upgradeable | AsyncReaderWriterLockType.Writer
                            : AsyncReaderWriterLockType.Upgradeable;
                        _writerType = AsyncReaderWriterLockType.None;
                        return;
                    }

                    // We add 1 more reader count since it was removed when the lock was upgraded.
                    // Unchecked context since we already checked for overflow when the reader lock was acquired.
                    unchecked
                    {
                        _readerLockCount = _readerWaitCount + 1;
                    }
                    _readerWaitCount = 0;
                    _lockType = hasWaitingWriter
                        ? AsyncReaderWriterLockType.Reader | AsyncReaderWriterLockType.Upgradeable | AsyncReaderWriterLockType.Writer
                        : AsyncReaderWriterLockType.Reader | AsyncReaderWriterLockType.Upgradeable;
                    _writerType = AsyncReaderWriterLockType.None;
                    readers = _readerQueue.MoveElementsToStack();
                } // lock

                do
                {
                    readers.Pop().Resolve(_currentKey);
                } while (readers.IsNotEmpty);
            }

            internal void ReleaseUpgradeableReaderLock(long key)
            {
                AsyncUpgradeableReaderLockPromise upgradeablePromise;
                AsyncWriterLockPromise writerPromise;
                lock (this)
                {
                    if (_currentKey != key | _upgradeWaiter != null)
                    {
                        ThrowInvalidUpgradeableKeyReleased(key, 2);
                    }

                    // We check for underflow, because multiple readers share the same key.
                    checked
                    {
                        --_readerLockCount;
                    }
                    if (_readerLockCount != 0)
                    {
                        // If there's a regular writer waiting, do nothing, to prevent starvation.
                        // Unless the contention strategy is (upgradeable)reader prioritized.
                        // Otherwise, if there's an upgradeable reader waiting, resolve it.
                        if (_upgradeQueue.IsEmpty | (_writerQueue.IsNotEmpty & !PrioritizeReadersOrUpgradeableReaders))
                        {
                            _lockType &= ~AsyncReaderWriterLockType.Upgradeable;
                            return;
                        }
                        IncrementReaderLockCount();
                        upgradeablePromise = _upgradeQueue.Dequeue();
                        goto ResolveUpgradeableReader;
                    }

                    // Check writer queue before checking upgrade queue, to prevent starvation.
                    // Unless there is a waiting upgradeable reader, and the contention strategy is (upgradeable)reader prioritized.
                    bool hasUpgradeable = _upgradeQueue.IsNotEmpty;
                    bool prioritizedUpgradeable = hasUpgradeable & PrioritizeReadersOrUpgradeableReaders;
                    if (_writerQueue.IsNotEmpty & !prioritizedUpgradeable)
                    {
                        writerPromise = _writerQueue.Dequeue();
                        _lockType &= ~AsyncReaderWriterLockType.Upgradeable;
                        _writerType = AsyncReaderWriterLockType.Writer;
                        SetNextKey();
                    }
                    else if (hasUpgradeable)
                    {
                        IncrementReaderLockCount();
                        upgradeablePromise = _upgradeQueue.Dequeue();
                        goto ResolveUpgradeableReader;
                    }
                    else
                    {
                        // The lock is exited completely.
                        _lockType = AsyncReaderWriterLockType.None;
                        _writerType = AsyncReaderWriterLockType.None;
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
                    if (_readerQueue.TryRemove(promise))
                    {
                        --_readerWaitCount;
                        return true;
                    }
                    return false;
                }
            }

            internal bool TryUnregister(AsyncWriterLockPromise promise)
            {
                ValueLinkedStack<AsyncReaderLockPromise> readers;
                AsyncUpgradeableReaderLockPromise upgradeablePromise;
                lock (this)
                {
                    if (_upgradeWaiter == promise)
                    {
                        // Re-add the count that was subtracted when it was upgraded.
                        IncrementReaderLockCount();
                        _upgradeWaiter = null;

                        if (_writerQueue.IsEmpty & _writerType == AsyncReaderWriterLockType.None)
                        {
                            _lockType = AsyncReaderWriterLockType.Reader | (_lockType & ~AsyncReaderWriterLockType.Writer);
                            _readerLockCount += _readerWaitCount;
                            _readerWaitCount = 0;
                            readers = _readerQueue.MoveElementsToStack();
                            goto ResolveReaders;
                        }

                        _lockType |= AsyncReaderWriterLockType.Reader;
                        return true;
                    }

                    if (!_writerQueue.TryRemove(promise))
                    {
                        return false;
                    }

                    bool hasNoWriter = _writerQueue.IsEmpty & _upgradeWaiter == null & _writerType == AsyncReaderWriterLockType.None;
                    if (!hasNoWriter)
                    {
                        return true;
                    }

                    _lockType &= ~AsyncReaderWriterLockType.Writer;
                    _readerLockCount += _readerWaitCount;
                    _readerWaitCount = 0;
                    readers = _readerQueue.MoveElementsToStack();

                    if ((_lockType & AsyncReaderWriterLockType.Upgradeable) != 0 | _upgradeQueue.IsEmpty)
                    {
                        goto ResolveReaders;
                    }

                    IncrementReaderLockCount();
                    upgradeablePromise = _upgradeQueue.Dequeue();
                }

                upgradeablePromise.Resolve(_currentKey);

            ResolveReaders:
                while (readers.IsNotEmpty)
                {
                    readers.Pop().Resolve(_currentKey);
                }
                return true;
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
                throw new InvalidOperationException("The " + GetKeyTypeString(keyType) + " is invalid for this operation.", GetFormattedStacktrace(skipFrames + 1));
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            internal void ThrowInvalidUpgradeableKeyReleased(long key, int skipFrames)
            {
                string message = _currentKey != key
                    ? "The AsyncReaderWriterLock.UpgradeableReaderKey is invalid for this operation. Did you dispose it before the upgraded writer key?"
                    : "The AsyncReaderWriterLock.UpgradeableReaderKey cannot be released before the UpgradeToWriterLockAsync promise is complete.";
                throw new InvalidOperationException(message, GetFormattedStacktrace(skipFrames + 1));
            }

            internal static string GetKeyTypeString(AsyncReaderWriterLockType keyType)
            {
                return keyType == AsyncReaderWriterLockType.Reader ? "AsyncReaderWriterLock.ReaderKey"
                    : keyType == AsyncReaderWriterLockType.Writer ? "AsyncReaderWriterLock.WriterKey"
                    : "AsyncReaderWriterLock.UpgradeableReaderKey";
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
                ValueLinkedStack<AsyncReaderLockPromise> readers;
                ValueLinkedStack<AsyncWriterLockPromise> writers;
                ValueLinkedStack<AsyncUpgradeableReaderLockPromise> upgradeables;
                AsyncWriterLockPromise upgradeWaiter;
                lock (this)
                {
                    if (_abandonedMessage != null)
                    {
                        // Additional abandoned keys could be caused by the first abandoned key, so do nothing.
                        return;
                    }
                    _abandonedMessage = abandonedMessage;

                    readers = _readerQueue.MoveElementsToStack();
                    writers = _writerQueue.MoveElementsToStack();
                    upgradeables = _upgradeQueue.MoveElementsToStack();
                    upgradeWaiter = _upgradeWaiter;
                    _upgradeWaiter = null;
                }

                var rejectContainer = CreateRejectContainer(new AbandonedLockException(abandonedMessage), int.MinValue, null, traceable);

                while (readers.IsNotEmpty)
                {
                    readers.Pop().Reject(rejectContainer);
                }
                while (writers.IsNotEmpty)
                {
                    writers.Pop().Reject(rejectContainer);
                }
                while (upgradeables.IsNotEmpty)
                {
                    upgradeables.Pop().Reject(rejectContainer);
                }
                upgradeWaiter?.Reject(rejectContainer);

                rejectContainer.ReportUnhandled();
            }
#endif // PROMISE_DEBUG
        } // class AsyncReaderWriterLockInternal

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal readonly partial struct AsyncReaderWriterLockKey
        {
            // Never called, only added to make the compiler shut up.
            public override bool Equals(object obj) => throw new System.InvalidOperationException();

            [MethodImpl(InlineOption)]
            public override int GetHashCode()
            {
                return BuildHashCode(_owner, _key.GetHashCode(), 0);
            }

            [MethodImpl(InlineOption)]
            public static bool operator !=(AsyncReaderWriterLockKey lhs, AsyncReaderWriterLockKey rhs)
            {
                return !(lhs == rhs);
            }
        }

        // We check to make sure every key is disposed exactly once in DEBUG mode.
        // We don't verify this in RELEASE mode to avoid allocating on every lock enter.
#if PROMISE_DEBUG
        partial struct AsyncReaderWriterLockKey
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private sealed class DisposedChecker : IDisposable, ITraceable
            {
                public CausalityTrace Trace { get; set; }

                internal readonly AsyncReaderWriterLockInternal _owner;
                private int _isDisposedFlag;
                private readonly AsyncReaderWriterLockType _keyType;

                internal DisposedChecker(AsyncReaderWriterLockInternal owner, AsyncReaderWriterLockType keyType, ITraceable traceable)
                {
                    _owner = owner;
                    _keyType = keyType;
                    if (traceable == null)
                    {
                        SetCreatedStacktrace(this, 3);
                    }
                    else
                    {
                        Trace = traceable.Trace;
                    }
                }

                ~DisposedChecker()
                {
                    _owner.NotifyAbandoned("An " + AsyncReaderWriterLockInternal.GetKeyTypeString(_keyType) + " was never disposed.", this);
                }

                internal void ValidateNotDisposed()
                {
                    if (_isDisposedFlag != 0)
                    {
                        throw new ObjectDisposedException(AsyncReaderWriterLockInternal.GetKeyTypeString(_keyType));
                    }
                }

                public void Dispose()
                {
                    _isDisposedFlag = 1;
                    GC.SuppressFinalize(this);
                }
            }

            internal readonly AsyncReaderWriterLockInternal _owner;
            internal readonly long _key;
            private readonly DisposedChecker _disposedChecker;

            internal AsyncReaderWriterLockKey(AsyncReaderWriterLockInternal owner, long key, AsyncReaderWriterLockType lockType, ITraceable traceable)
            {
                _owner = owner;
                _key = key;
                _disposedChecker = new DisposedChecker(owner, lockType, traceable);
            }

            internal AsyncReaderWriterLockKey(AsyncReaderWriterLockInternal owner)
            {
                _owner = owner;
                _key = 0;
                _disposedChecker = null;
            }

            internal void ReleaseReaderLock()
            {
                var owner = _owner;
                var disposedChecker = _disposedChecker;
                if ((owner == null | disposedChecker == null) || owner != disposedChecker._owner)
                {
                    AsyncReaderWriterLockInternal.ThrowInvalidKey(AsyncReaderWriterLockType.Reader, 2);
                }
                lock (disposedChecker)
                {
                    disposedChecker.ValidateNotDisposed();
                    owner.ReleaseReaderLock(_key);
                    disposedChecker.Dispose();
                }
            }

            internal void ReleaseWriterLock()
            {
                var owner = _owner;
                var disposedChecker = _disposedChecker;
                if ((owner == null | disposedChecker == null) || owner != disposedChecker._owner)
                {
                    AsyncReaderWriterLockInternal.ThrowInvalidKey(AsyncReaderWriterLockType.Writer, 2);
                }
                lock (disposedChecker)
                {
                    disposedChecker.ValidateNotDisposed();
                    owner.ReleaseWriterLock(_key);
                    disposedChecker.Dispose();
                }
            }

            internal void ReleaseUpgradeableReaderLock()
            {
                var owner = _owner;
                var disposedChecker = _disposedChecker;
                if ((owner == null | disposedChecker == null) || owner != disposedChecker._owner)
                {
                    AsyncReaderWriterLockInternal.ThrowInvalidKey(AsyncReaderWriterLockType.Upgradeable, 2);
                }
                lock (disposedChecker)
                {
                    disposedChecker.ValidateNotDisposed();
                    owner.ReleaseUpgradeableReaderLock(_key);
                    disposedChecker.Dispose();
                }
            }

            public static bool operator ==(AsyncReaderWriterLockKey lhs, AsyncReaderWriterLockKey rhs)
            {
                return lhs._owner == rhs._owner & lhs._key == rhs._key & lhs._disposedChecker == rhs._disposedChecker;
            }
        }
#else // PROMISE_DEBUG
        partial struct AsyncReaderWriterLockKey
        {
            internal readonly AsyncReaderWriterLockInternal _owner;
            internal readonly long _key;

            [MethodImpl(InlineOption)]
#pragma warning disable IDE0060 // Remove unused parameter
            internal AsyncReaderWriterLockKey(AsyncReaderWriterLockInternal owner, long key, AsyncReaderWriterLockType lockType, ITraceable traceable)
#pragma warning restore IDE0060 // Remove unused parameter
            {
                _owner = owner;
                _key = key;
            }

            [MethodImpl(InlineOption)]
            internal AsyncReaderWriterLockKey(AsyncReaderWriterLockInternal owner)
            {
                _owner = owner;
                _key = 0;
            }

            [MethodImpl(InlineOption)]
            internal void ReleaseReaderLock()
            {
                ValidateAndGetOwner(Internal.AsyncReaderWriterLockType.Reader).ReleaseReaderLock(_key);

            }

            [MethodImpl(InlineOption)]
            internal void ReleaseWriterLock()
            {
                ValidateAndGetOwner(Internal.AsyncReaderWriterLockType.Writer).ReleaseWriterLock(_key);

            }

            [MethodImpl(InlineOption)]
            internal void ReleaseUpgradeableReaderLock()
            {
                ValidateAndGetOwner(Internal.AsyncReaderWriterLockType.Upgradeable).ReleaseUpgradeableReaderLock(_key);

            }

            private Internal.AsyncReaderWriterLockInternal ValidateAndGetOwner(AsyncReaderWriterLockType lockType)
            {
                var owner = _owner;
                if (owner == null)
                {
                    AsyncReaderWriterLockInternal.ThrowInvalidKey(lockType, 3);
                }
                return owner;
            }

            [MethodImpl(InlineOption)]
            public static bool operator ==(AsyncReaderWriterLockKey lhs, AsyncReaderWriterLockKey rhs)
            {
                return lhs._owner == rhs._owner & lhs._key == rhs._key;
            }
        }
#endif // PROMISE_DEBUG
    } // class Internal
#endif // UNITY_2021_2_OR_NEWER || NETSTANDARD2_1_OR_GREATER || NETCOREAPP

    namespace Threading
    {
#if UNITY_2021_2_OR_NEWER || NETSTANDARD2_1_OR_GREATER || NETCOREAPP
        partial class AsyncReaderWriterLock
        {
            partial struct ReaderKey
            {
                internal readonly Internal.AsyncReaderWriterLockKey _impl;

                [MethodImpl(Internal.InlineOption)]
                internal ReaderKey(Internal.AsyncReaderWriterLockInternal owner, long key, Internal.ITraceable traceable)
                {
                    _impl = new Internal.AsyncReaderWriterLockKey(owner, key, Internal.AsyncReaderWriterLockType.Reader, traceable);
                }

                [MethodImpl(Internal.InlineOption)]
                internal ReaderKey(Internal.AsyncReaderWriterLockInternal owner)
                {
                    _impl = new Internal.AsyncReaderWriterLockKey(owner);
                }
            }

            partial struct WriterKey
            {
                internal readonly Internal.AsyncReaderWriterLockKey _impl;

                [MethodImpl(Internal.InlineOption)]
                internal WriterKey(Internal.AsyncReaderWriterLockInternal owner, long key, Internal.ITraceable traceable)
                {
                    _impl = new Internal.AsyncReaderWriterLockKey(owner, key, Internal.AsyncReaderWriterLockType.Writer, traceable);
                }

                [MethodImpl(Internal.InlineOption)]
                internal WriterKey(Internal.AsyncReaderWriterLockInternal owner)
                {
                    _impl = new Internal.AsyncReaderWriterLockKey(owner);
                }
            }

            partial struct UpgradeableReaderKey
            {
                internal readonly Internal.AsyncReaderWriterLockKey _impl;

                [MethodImpl(Internal.InlineOption)]
                internal UpgradeableReaderKey(Internal.AsyncReaderWriterLockInternal owner, long key, Internal.ITraceable traceable)
                {
                    _impl = new Internal.AsyncReaderWriterLockKey(owner, key, Internal.AsyncReaderWriterLockType.Upgradeable, traceable);
                }

                [MethodImpl(Internal.InlineOption)]
                internal UpgradeableReaderKey(Internal.AsyncReaderWriterLockInternal owner)
                {
                    _impl = new Internal.AsyncReaderWriterLockKey(owner);
                }
            }
        } // class AsyncReaderWriterLock
#endif // UNITY_2021_2_OR_NEWER || NETSTANDARD2_1_OR_GREATER || NETCOREAPP
    } // namespace Threading
} // namespace Proto.Promises.Threading