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
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal sealed class AsyncReaderLockPromise : PromiseRefBase.AsyncSynchronizationPromiseBase<AsyncReaderWriterLock.ReaderKey>, ICancelable, ILinked<AsyncReaderLockPromise>
        {
            AsyncReaderLockPromise ILinked<AsyncReaderLockPromise>.Next { get; set; }
            private AsyncReaderWriterLock Owner => _result._impl._owner;

            [MethodImpl(InlineOption)]
            private static AsyncReaderLockPromise GetOrCreate()
            {
                var obj = ObjectPool.TryTakeOrInvalid<AsyncReaderLockPromise>();
                return obj == InvalidAwaitSentinel.s_instance
                    ? new AsyncReaderLockPromise()
                    : obj.UnsafeAs<AsyncReaderLockPromise>();
            }

            internal static AsyncReaderLockPromise GetOrCreate(AsyncReaderWriterLock owner, bool continueOnCapturedContext)
            {
                var promise = GetOrCreate();
                promise.Reset(continueOnCapturedContext);
                promise._result = new AsyncReaderWriterLock.ReaderKey(owner); // This will be overwritten when this is resolved, we just store the key with the owner here for cancelation.
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
            private AsyncReaderWriterLock Owner => _result._impl._owner;

            [MethodImpl(InlineOption)]
            private static AsyncWriterLockPromise GetOrCreate()
            {
                var obj = ObjectPool.TryTakeOrInvalid<AsyncWriterLockPromise>();
                return obj == InvalidAwaitSentinel.s_instance
                    ? new AsyncWriterLockPromise()
                    : obj.UnsafeAs<AsyncWriterLockPromise>();
            }

            internal static AsyncWriterLockPromise GetOrCreate(AsyncReaderWriterLock owner, bool continueOnCapturedContext)
            {
                var promise = GetOrCreate();
                promise.Reset(continueOnCapturedContext);
                promise._result = new AsyncReaderWriterLock.WriterKey(owner); // This will be overwritten when this is resolved, we just store the key with the owner here for cancelation.
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
            private AsyncReaderWriterLock Owner => _result._impl._owner;

            [MethodImpl(InlineOption)]
            private static AsyncUpgradeableReaderLockPromise GetOrCreate()
            {
                var obj = ObjectPool.TryTakeOrInvalid<AsyncUpgradeableReaderLockPromise>();
                return obj == InvalidAwaitSentinel.s_instance
                    ? new AsyncUpgradeableReaderLockPromise()
                    : obj.UnsafeAs<AsyncUpgradeableReaderLockPromise>();
            }

            internal static AsyncUpgradeableReaderLockPromise GetOrCreate(AsyncReaderWriterLock owner, bool continueOnCapturedContext)
            {
                var promise = GetOrCreate();
                promise.Reset(continueOnCapturedContext);
                promise._result = new AsyncReaderWriterLock.UpgradeableReaderKey(owner); // This will be overwritten when this is resolved, we just store the key with the owner here for cancelation.
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

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal sealed class AsyncUpgradedWriterLockPromise : PromiseRefBase.AsyncSynchronizationPromiseBase<AsyncReaderWriterLock.UpgradedWriterKey>, ICancelable, ILinked<AsyncUpgradedWriterLockPromise>
        {
            AsyncUpgradedWriterLockPromise ILinked<AsyncUpgradedWriterLockPromise>.Next { get; set; }
            private AsyncReaderWriterLock Owner => _result._impl._owner;

            [MethodImpl(InlineOption)]
            private static AsyncUpgradedWriterLockPromise GetOrCreate()
            {
                var obj = ObjectPool.TryTakeOrInvalid<AsyncUpgradedWriterLockPromise>();
                return obj == InvalidAwaitSentinel.s_instance
                    ? new AsyncUpgradedWriterLockPromise()
                    : obj.UnsafeAs<AsyncUpgradedWriterLockPromise>();
            }

            internal static AsyncUpgradedWriterLockPromise GetOrCreate(AsyncReaderWriterLock owner, bool continueOnCapturedContext)
            {
                var promise = GetOrCreate();
                promise.Reset(continueOnCapturedContext);
                promise._result = new AsyncReaderWriterLock.UpgradedWriterKey(owner); // This will be overwritten when this is resolved, we just store the key with the owner here for cancelation.
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

            internal void Resolve(long writerKey)
            {
                ThrowIfInPool(this);

                // We don't need to check if the unregister was successful or not.
                // The fact that this was called means the cancelation was unable to unregister this from the lock.
                // We just dispose to wait for the callback to complete before we continue.
                _cancelationRegistration.Dispose();

                _result = new AsyncReaderWriterLock.UpgradedWriterKey(Owner, writerKey, this);
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

                internal readonly AsyncReaderWriterLock _owner;
                private int _isDisposedFlag;
                private readonly AsyncReaderWriterLockType _keyType;

                internal DisposedChecker(AsyncReaderWriterLock owner, AsyncReaderWriterLockType keyType, ITraceable traceable)
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
                    _owner.NotifyAbandoned("An " + AsyncReaderWriterLock.GetKeyTypeString(_keyType) + " was never disposed.", this);
                }

                internal void ValidateNotDisposed()
                {
                    if (_isDisposedFlag != 0)
                    {
                        throw new ObjectDisposedException(AsyncReaderWriterLock.GetKeyTypeString(_keyType));
                    }
                }

                public void Dispose()
                {
                    _isDisposedFlag = 1;
                    GC.SuppressFinalize(this);
                }
            }

            internal readonly AsyncReaderWriterLock _owner;
            internal readonly long _key;
            private readonly DisposedChecker _disposedChecker;

            internal AsyncReaderWriterLockKey(AsyncReaderWriterLock owner, long key, AsyncReaderWriterLockType lockType, ITraceable traceable)
            {
                _owner = owner;
                _key = key;
                _disposedChecker = new DisposedChecker(owner, lockType, traceable);
            }

            internal AsyncReaderWriterLockKey(AsyncReaderWriterLock owner)
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
                    AsyncReaderWriterLock.ThrowInvalidKey(AsyncReaderWriterLockType.Reader, 2);
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
                    AsyncReaderWriterLock.ThrowInvalidKey(AsyncReaderWriterLockType.Writer, 2);
                }
                lock (disposedChecker)
                {
                    disposedChecker.ValidateNotDisposed();
                    owner.ReleaseNormalWriterLock(_key);
                    disposedChecker.Dispose();
                }
            }

            internal void ReleaseUpgradeableReaderLock()
            {
                var owner = _owner;
                var disposedChecker = _disposedChecker;
                if ((owner == null | disposedChecker == null) || owner != disposedChecker._owner)
                {
                    AsyncReaderWriterLock.ThrowInvalidKey(AsyncReaderWriterLockType.Upgradeable, 2);
                }
                lock (disposedChecker)
                {
                    disposedChecker.ValidateNotDisposed();
                    owner.ReleaseUpgradeableReaderLock(_key);
                    disposedChecker.Dispose();
                }
            }

            internal void ReleaseUpgradedWriterLock()
            {
                var owner = _owner;
                var disposedChecker = _disposedChecker;
                if ((owner == null | disposedChecker == null) || owner != disposedChecker._owner)
                {
                    AsyncReaderWriterLock.ThrowInvalidKey(AsyncReaderWriterLockType.Writer, 2);
                }
                lock (disposedChecker)
                {
                    disposedChecker.ValidateNotDisposed();
                    owner.ReleaseUpgradedWriterLock(_key);
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
            internal readonly AsyncReaderWriterLock _owner;
            internal readonly long _key;

            [MethodImpl(InlineOption)]
#pragma warning disable IDE0060 // Remove unused parameter
            internal AsyncReaderWriterLockKey(AsyncReaderWriterLock owner, long key, AsyncReaderWriterLockType lockType, ITraceable traceable)
#pragma warning restore IDE0060 // Remove unused parameter
            {
                _owner = owner;
                _key = key;
            }

            [MethodImpl(InlineOption)]
            internal AsyncReaderWriterLockKey(AsyncReaderWriterLock owner)
            {
                _owner = owner;
                _key = 0;
            }

            [MethodImpl(InlineOption)]
            internal void ReleaseReaderLock()
                => ValidateAndGetOwner(AsyncReaderWriterLockType.Reader).ReleaseReaderLock(_key);

            [MethodImpl(InlineOption)]
            internal void ReleaseWriterLock()
                => ValidateAndGetOwner(AsyncReaderWriterLockType.Writer).ReleaseNormalWriterLock(_key);

            [MethodImpl(InlineOption)]
            internal void ReleaseUpgradeableReaderLock()
                => ValidateAndGetOwner(AsyncReaderWriterLockType.Upgradeable).ReleaseUpgradeableReaderLock(_key);

            [MethodImpl(InlineOption)]
            internal void ReleaseUpgradedWriterLock()
                => ValidateAndGetOwner(AsyncReaderWriterLockType.Writer).ReleaseUpgradedWriterLock(_key);

            private AsyncReaderWriterLock ValidateAndGetOwner(AsyncReaderWriterLockType lockType)
            {
                var owner = _owner;
                if (owner == null)
                {
                    AsyncReaderWriterLock.ThrowInvalidKey(lockType, 3);
                }
                return owner;
            }

            [MethodImpl(InlineOption)]
            public static bool operator ==(AsyncReaderWriterLockKey lhs, AsyncReaderWriterLockKey rhs)
                => lhs._owner == rhs._owner
                & lhs._key == rhs._key;
        }
#endif // PROMISE_DEBUG
    } // class Internal
#endif // UNITY_2021_2_OR_NEWER || NETSTANDARD2_1_OR_GREATER || NETCOREAPP

    namespace Threading
    {
#if UNITY_2021_2_OR_NEWER || NETSTANDARD2_1_OR_GREATER || NETCOREAPP
        [Flags]
        internal enum AsyncReaderWriterLockType : byte
        {
            None = 0,
            Reader = 1 << 0,
            Writer = 1 << 1,
            Upgradeable = 1 << 2
        }

        partial class AsyncReaderWriterLock
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            // Wrapping struct fields smaller than 64-bits in another struct fixes issue with extra padding
            // (see https://stackoverflow.com/questions/67068942/c-sharp-why-do-class-fields-of-struct-types-take-up-more-space-than-the-size-of).
            private struct SmallFields
            {
                internal Internal.SpinLocker _locker;
                volatile internal AsyncReaderWriterLockType _lockType;
                internal AsyncReaderWriterLockType _writerType;
                internal readonly ContentionStrategy _contentionStrategy;

                internal SmallFields(ContentionStrategy contentionStrategy)
                {
                    _locker = new Internal.SpinLocker();
                    _lockType = AsyncReaderWriterLockType.None;
                    _writerType = AsyncReaderWriterLockType.None;
                    _contentionStrategy = contentionStrategy;
                }
            }

            // If there is contention on the lock, we alternate between writers and readers, so one type of lock won't starve out the other.
            // We also alternate between upgraded writers and regular writers to prevent writer starvation.

            // These must not be readonly.
            private Internal.ValueLinkedQueue<Internal.AsyncReaderLockPromise> _readerQueue = new Internal.ValueLinkedQueue<Internal.AsyncReaderLockPromise>();
            private Internal.ValueLinkedQueue<Internal.AsyncWriterLockPromise> _writerQueue = new Internal.ValueLinkedQueue<Internal.AsyncWriterLockPromise>();
            private Internal.ValueLinkedQueue<Internal.AsyncUpgradeableReaderLockPromise> _upgradeQueue = new Internal.ValueLinkedQueue<Internal.AsyncUpgradeableReaderLockPromise>();
            private Internal.AsyncUpgradedWriterLockPromise _upgradeWaiter;
            private long _currentKey;
            // This stores the previous reader key from an upgraded writer.
            // We store it here instead of in the WriterKey struct to prevent bloating other types that store the struct (like async state machines).
            private long _previousReaderKey;
            private uint _readerLockCount;
            private uint _readerWaitCount;
            // This must not be readonly.
            private SmallFields _smallFields;

            private bool PrioritizeWriters
            {
                [MethodImpl(Internal.InlineOption)]
                get => _smallFields._contentionStrategy == ContentionStrategy.PrioritizeWriters;
            }

            private bool PrioritizeReaders
            {
                [MethodImpl(Internal.InlineOption)]
                get => _smallFields._contentionStrategy == ContentionStrategy.PrioritizeReaders;
            }

            private bool PrioritizeUpgradeableReaders
            {
                [MethodImpl(Internal.InlineOption)]
                get => _smallFields._contentionStrategy == ContentionStrategy.PrioritizeUpgradeableReaders;
            }

            private bool PrioritizeReadersOrUpgradeableReaders
            {
                [MethodImpl(Internal.InlineOption)]
                get => _smallFields._contentionStrategy >= ContentionStrategy.PrioritizeReaders;
            }

            [MethodImpl(Internal.InlineOption)]
            private void SetNextKey()
                => _currentKey = Internal.KeyGenerator<AsyncReaderWriterLock>.Next();

            [MethodImpl(Internal.InlineOption)]
            private void SetNextAndPreviousKeys()
            {
                _previousReaderKey = _currentKey;
                SetNextKey();
            }

            [MethodImpl(Internal.InlineOption)]
            private void RestorePreviousKey()
                => _currentKey = _previousReaderKey;

            [MethodImpl(Internal.InlineOption)]
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

            [MethodImpl(Internal.InlineOption)]
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
                    _smallFields._locker.Exit();
                    throw new OverflowException("AsyncReaderWriterLock does not support more than " + maxReaders + " simultaneous reader locks.");
                }
            }

            [MethodImpl(Internal.InlineOption)]
            private bool CanEnterReaderLock(AsyncReaderWriterLockType currentLockType)
            {
                // Normal reader locks can be taken simultaneously along with an upgradeable reader lock.
                // If a writer lock is taken or waiting to be taken, and the contention strategy is not reader prioritized,
                // we wait for it to be released, to prevent starvation.
                bool noWriters = (currentLockType & AsyncReaderWriterLockType.Writer) == 0;
                bool noUpgradeWriter = (currentLockType & (AsyncReaderWriterLockType.Writer | AsyncReaderWriterLockType.Upgradeable)) == 0;
                bool writerDoesNotOwnLock = _smallFields._writerType == AsyncReaderWriterLockType.None;
                bool prioritizedEntrance = writerDoesNotOwnLock & (PrioritizeReaders | (PrioritizeUpgradeableReaders & noUpgradeWriter));
                return noWriters | prioritizedEntrance;
            }

            private Promise<ReaderKey> ReaderLockAsyncImpl(bool continueOnCapturedContext)
            {
                // We don't spinwait here because it's async; we want to return to caller as fast as possible.

                Internal.AsyncReaderLockPromise promise;
                _smallFields._locker.Enter();
                {
                    ValidateNotAbandoned();
                    ValidateReaderCounts();

                    // Read the _smallFields._lockType into a local variable to avoid extra unnecessary volatile accesses inside the lock.
                    var lockType = _smallFields._lockType;

                    if (CanEnterReaderLock(lockType))
                    {
                        IncrementReaderLockCount();
                        if (lockType == AsyncReaderWriterLockType.None)
                        {
                            SetNextKey();
                        }
                        _smallFields._lockType = lockType | AsyncReaderWriterLockType.Reader;
                        var key = _currentKey;
                        _smallFields._locker.Exit();
                        return Promise.Resolved(new ReaderKey(this, key, null));
                    }

                    // Unchecked context since we're manually checking overflow.
                    unchecked
                    {
                        ++_readerWaitCount;
                    }
                    promise = Internal.AsyncReaderLockPromise.GetOrCreate(this, continueOnCapturedContext);
                    _readerQueue.Enqueue(promise);
                }
                _smallFields._locker.Exit();
                return new Promise<ReaderKey>(promise, promise.Id);
            }

            private Promise<ReaderKey> ReaderLockAsyncImpl(CancelationToken cancelationToken, bool continueOnCapturedContext)
            {
                // We don't spinwait here because it's async; we want to return to caller as fast as possible.

                // Quick check to see if the token is already canceled before entering.
                if (cancelationToken.IsCancelationRequested)
                {
                    ValidateNotAbandoned();

                    return Promise<ReaderKey>.Canceled();
                }

                Internal.AsyncReaderLockPromise promise;
                _smallFields._locker.Enter();
                {
                    ValidateNotAbandoned();
                    ValidateReaderCounts();

                    // Read the _smallFields._lockType into a local variable to avoid extra unnecessary volatile accesses inside the lock.
                    var lockType = _smallFields._lockType;

                    if (CanEnterReaderLock(lockType))
                    {
                        IncrementReaderLockCount();
                        if (lockType == AsyncReaderWriterLockType.None)
                        {
                            SetNextKey();
                        }
                        _smallFields._lockType = lockType | AsyncReaderWriterLockType.Reader;
                        var key = _currentKey;
                        _smallFields._locker.Exit();
                        return Promise.Resolved(new ReaderKey(this, key, null));
                    }

                    // Unchecked context since we're manually checking overflow.
                    unchecked
                    {
                        ++_readerWaitCount;
                    }
                    promise = Internal.AsyncReaderLockPromise.GetOrCreate(this, continueOnCapturedContext);
                    if (promise.HookupAndGetIsCanceled(cancelationToken))
                    {
                        _smallFields._locker.Exit();
                        promise.DisposeImmediate();
                        return Promise<ReaderKey>.Canceled();
                    }
                    _readerQueue.Enqueue(promise);
                }
                _smallFields._locker.Exit();
                return new Promise<ReaderKey>(promise, promise.Id);
            }

            private ReaderKey ReaderLockImpl()
            {
                // Since this is a synchronous lock, we do a short spinwait before yielding the thread.
                var spinner = new SpinWait();
                while (!CanEnterReaderLock(_smallFields._lockType) & !spinner.NextSpinWillYield)
                {
                    spinner.SpinOnce();
                }

                return ReaderLockAsyncImpl(false).WaitForResult();
            }

            private ReaderKey ReaderLockImpl(CancelationToken cancelationToken)
            {
                // Since this is a synchronous lock, we do a short spinwait before yielding the thread.
                var spinner = new SpinWait();
                while (!CanEnterReaderLock(_smallFields._lockType) & !spinner.NextSpinWillYield & !cancelationToken.IsCancelationRequested)
                {
                    spinner.SpinOnce();
                }

                return ReaderLockAsyncImpl(cancelationToken, false).WaitForResult();
            }

            private bool TryEnterReaderLockImpl(out ReaderKey readerKey)
            {
                // We don't spinwait here because we want to return immediately without waiting.
                _smallFields._locker.Enter();
                {
                    ValidateNotAbandoned();
                    ValidateReaderCounts();

                    // Read the _smallFields._lockType into a local variable to avoid extra unnecessary volatile accesses inside the lock.
                    var lockType = _smallFields._lockType;

                    if (CanEnterReaderLock(lockType))
                    {
                        IncrementReaderLockCount();
                        if (lockType == AsyncReaderWriterLockType.None)
                        {
                            SetNextKey();
                        }
                        _smallFields._lockType = lockType | AsyncReaderWriterLockType.Reader;
                        var key = _currentKey;
                        _smallFields._locker.Exit();
                        readerKey = new ReaderKey(this, key, null);
                        return true;
                    }
                }
                _smallFields._locker.Exit();
                readerKey = default;
                return false;
            }

            private Promise<(bool didEnter, ReaderKey readerKey)> TryEnterReaderLockAsyncImpl(CancelationToken cancelationToken, bool continueOnCapturedContext)
            {
                // We don't spinwait here because it's async; we want to return to caller as fast as possible.

                Internal.AsyncReaderLockPromise promise;
                _smallFields._locker.Enter();
                {
                    ValidateNotAbandoned();
                    ValidateReaderCounts();

                    // Read the _smallFields._lockType into a local variable to avoid extra unnecessary volatile accesses inside the lock.
                    var lockType = _smallFields._lockType;

                    if (CanEnterReaderLock(lockType))
                    {
                        IncrementReaderLockCount();
                        if (lockType == AsyncReaderWriterLockType.None)
                        {
                            SetNextKey();
                        }
                        _smallFields._lockType = lockType | AsyncReaderWriterLockType.Reader;
                        var key = _currentKey;
                        _smallFields._locker.Exit();
                        return Promise.Resolved((true, new ReaderKey(this, key, null)));
                    }
                    // Quick check to see if the token is already canceled before waiting.
                    if (cancelationToken.IsCancelationRequested)
                    {
                        _smallFields._locker.Exit();
                        return Promise.Resolved((false, default(ReaderKey)));
                    }

                    // Unchecked context since we're manually checking overflow.
                    unchecked
                    {
                        ++_readerWaitCount;
                    }
                    promise = Internal.AsyncReaderLockPromise.GetOrCreate(this, continueOnCapturedContext);
                    if (promise.HookupAndGetIsCanceled(cancelationToken))
                    {
                        _smallFields._locker.Exit();
                        promise.DisposeImmediate();
                        return Promise.Resolved((false, default(ReaderKey)));
                    }
                    _readerQueue.Enqueue(promise);
                }
                _smallFields._locker.Exit();
                return new Promise<ReaderKey>(promise, promise.Id)
                    .ContinueWith(resultContainer =>
                    {
                        resultContainer.RethrowIfRejected();
                        return (resultContainer.State == Promise.State.Resolved, resultContainer.Value);
                    });
            }

            private bool TryEnterReaderLockImpl(out ReaderKey readerKey, CancelationToken cancelationToken)
            {
                // Since this is a synchronous lock, we do a short spinwait before yielding the thread.
                var spinner = new SpinWait();
                while (!CanEnterReaderLock(_smallFields._lockType) & !spinner.NextSpinWillYield & !cancelationToken.IsCancelationRequested)
                {
                    spinner.SpinOnce();
                }

                var (success, key) = TryEnterReaderLockAsyncImpl(cancelationToken, false).WaitForResult();
                readerKey = key;
                return success;
            }

            private Promise<WriterKey> WriterLockAsyncImpl(bool continueOnCapturedContext)
            {
                // We don't spinwait here because it's async; we want to return to caller as fast as possible.
                Internal.AsyncWriterLockPromise promise;
                _smallFields._locker.Enter();
                {
                    ValidateNotAbandoned();

                    // Writer locks are mutually exclusive.
                    var lockType = _smallFields._lockType;
                    _smallFields._lockType = lockType | AsyncReaderWriterLockType.Writer;
                    if (lockType == AsyncReaderWriterLockType.None)
                    {
                        _smallFields._writerType = AsyncReaderWriterLockType.Writer;
                        SetNextKey();
                        var key = _currentKey;
                        _smallFields._locker.Exit();
                        return Promise.Resolved(new WriterKey(this, key, null));
                    }

                    promise = Internal.AsyncWriterLockPromise.GetOrCreate(this, continueOnCapturedContext);
                    _writerQueue.Enqueue(promise);
                }
                _smallFields._locker.Exit();
                return new Promise<WriterKey>(promise, promise.Id);
            }

            private Promise<WriterKey> WriterLockAsyncImpl(CancelationToken cancelationToken, bool continueOnCapturedContext)
            {
                // We don't spinwait here because it's async; we want to return to caller as fast as possible.

                // Quick check to see if the token is already canceled before entering.
                if (cancelationToken.IsCancelationRequested)
                {
                    ValidateNotAbandoned();

                    return Promise<WriterKey>.Canceled();
                }

                Internal.AsyncWriterLockPromise promise;
                _smallFields._locker.Enter();
                {
                    ValidateNotAbandoned();

                    // Writer locks are mutually exclusive.
                    var lockType = _smallFields._lockType;
                    _smallFields._lockType = lockType | AsyncReaderWriterLockType.Writer;
                    if (lockType == AsyncReaderWriterLockType.None)
                    {
                        _smallFields._writerType = AsyncReaderWriterLockType.Writer;
                        SetNextKey();
                        var key = _currentKey;
                        _smallFields._locker.Exit();
                        return Promise.Resolved(new WriterKey(this, key, null));
                    }

                    promise = Internal.AsyncWriterLockPromise.GetOrCreate(this, continueOnCapturedContext);
                    if (promise.HookupAndGetIsCanceled(cancelationToken))
                    {
                        _smallFields._locker.Exit();
                        promise.DisposeImmediate();
                        return Promise<WriterKey>.Canceled();
                    }
                    _writerQueue.Enqueue(promise);
                }
                _smallFields._locker.Exit();
                return new Promise<WriterKey>(promise, promise.Id);
            }

            private WriterKey WriterLockImpl()
            {
                // Since this is a synchronous lock, we do a short spinwait before yielding the thread.
                var spinner = new SpinWait();
                while (_smallFields._lockType != AsyncReaderWriterLockType.None & !spinner.NextSpinWillYield)
                {
                    spinner.SpinOnce();
                }

                return WriterLockAsyncImpl(false).WaitForResult();
            }

            private WriterKey WriterLockImpl(CancelationToken cancelationToken)
            {
                // Since this is a synchronous lock, we do a short spinwait before yielding the thread.
                var spinner = new SpinWait();
                while (_smallFields._lockType != AsyncReaderWriterLockType.None & !spinner.NextSpinWillYield & !cancelationToken.IsCancelationRequested)
                {
                    spinner.SpinOnce();
                }

                return WriterLockAsyncImpl(cancelationToken, false).WaitForResult();
            }

            private bool TryEnterWriterLockImpl(out WriterKey writerKey)
            {
                // We don't spinwait here because we want to return immediately without waiting.
                _smallFields._locker.Enter();
                {
                    ValidateNotAbandoned();

                    // Writer locks are mutually exclusive.
                    var lockType = _smallFields._lockType;
                    if (lockType == AsyncReaderWriterLockType.None)
                    {
                        _smallFields._lockType = lockType | AsyncReaderWriterLockType.Writer;
                        _smallFields._writerType = AsyncReaderWriterLockType.Writer;
                        SetNextKey();
                        var key = _currentKey;
                        _smallFields._locker.Exit();
                        writerKey = new WriterKey(this, key, null);
                        return true;
                    }
                }
                _smallFields._locker.Exit();
                writerKey = default;
                return false;
            }

            private Promise<(bool didEnter, WriterKey writerKey)> TryEnterWriterLockAsyncImpl(CancelationToken cancelationToken, bool continueOnCapturedContext)
            {
                // We don't spinwait here because it's async; we want to return to caller as fast as possible.
                Internal.AsyncWriterLockPromise promise;
                _smallFields._locker.Enter();
                {
                    ValidateNotAbandoned();

                    // Writer locks are mutually exclusive.
                    var lockType = _smallFields._lockType;
                    if (lockType == AsyncReaderWriterLockType.None)
                    {
                        _smallFields._lockType = lockType | AsyncReaderWriterLockType.Writer;
                        _smallFields._writerType = AsyncReaderWriterLockType.Writer;
                        SetNextKey();
                        var key = _currentKey;
                        _smallFields._locker.Exit();
                        return Promise.Resolved((true, new WriterKey(this, key, null)));
                    }
                    // Quick check to see if the token is already canceled before waiting.
                    if (cancelationToken.IsCancelationRequested)
                    {
                        _smallFields._locker.Exit();
                        return Promise.Resolved((false, default(WriterKey)));
                    }

                    _smallFields._lockType = lockType | AsyncReaderWriterLockType.Writer;
                    promise = Internal.AsyncWriterLockPromise.GetOrCreate(this, continueOnCapturedContext);
                    if (promise.HookupAndGetIsCanceled(cancelationToken))
                    {
                        _smallFields._locker.Exit();
                        promise.DisposeImmediate();
                        return Promise.Resolved((false, default(WriterKey)));
                    }
                    _writerQueue.Enqueue(promise);
                }
                _smallFields._locker.Exit();
                return new Promise<WriterKey>(promise, promise.Id)
                    .ContinueWith(resultContainer =>
                    {
                        resultContainer.RethrowIfRejected();
                        return (resultContainer.State == Promise.State.Resolved, resultContainer.Value);
                    });
            }

            private bool TryEnterWriterLockImpl(out WriterKey writerKey, CancelationToken cancelationToken)
            {
                // Since this is a synchronous lock, we do a short spinwait before yielding the thread.
                var spinner = new SpinWait();
                while (_smallFields._lockType != AsyncReaderWriterLockType.None & !spinner.NextSpinWillYield & !cancelationToken.IsCancelationRequested)
                {
                    spinner.SpinOnce();
                }

                var (success, key) = TryEnterWriterLockAsyncImpl(cancelationToken, false).WaitForResult();
                writerKey = key;
                return success;
            }

            [MethodImpl(Internal.InlineOption)]
            private bool CanEnterUpgradeableReaderLock(AsyncReaderWriterLockType currentLockType)
            {
                // Normal reader locks can be taken simultaneously along with an upgradeable reader lock.
                // Only 1 upgradeable reader lock is allowed at a time.
                // If a writer lock is taken or waiting to be taken, and the contention strategy is not reader prioritized,
                // we wait for it to be released, to prevent starvation.
                bool noWritersOrUpgradeableReaders = currentLockType <= AsyncReaderWriterLockType.Reader;
                bool writerDoesNotOwnLock = _smallFields._writerType == AsyncReaderWriterLockType.None;
                bool noUpgradeableReaders = _upgradeQueue.IsEmpty & (currentLockType & AsyncReaderWriterLockType.Upgradeable) == 0;
                bool prioritizedEntrance = writerDoesNotOwnLock & noUpgradeableReaders & PrioritizeReadersOrUpgradeableReaders;
                return noWritersOrUpgradeableReaders | prioritizedEntrance;
            }

            private Promise<UpgradeableReaderKey> UpgradeableReaderLockAsyncImpl(bool continueOnCapturedContext)
            {
                // We don't spinwait here because it's async; we want to return to caller as fast as possible.
                Internal.AsyncUpgradeableReaderLockPromise promise;
                _smallFields._locker.Enter();
                {
                    ValidateNotAbandoned();

                    // Read the _smallFields._lockType into a local variable to avoid extra unnecessary volatile accesses inside the lock.
                    var lockType = _smallFields._lockType;

                    if (CanEnterUpgradeableReaderLock(lockType))
                    {
                        IncrementReaderLockCount();
                        if (lockType == AsyncReaderWriterLockType.None)
                        {
                            SetNextKey();
                        }
                        _smallFields._lockType = lockType | AsyncReaderWriterLockType.Upgradeable;
                        var key = _currentKey;
                        _smallFields._locker.Exit();
                        return Promise.Resolved(new UpgradeableReaderKey(this, key, null));
                    }

                    promise = Internal.AsyncUpgradeableReaderLockPromise.GetOrCreate(this, continueOnCapturedContext);
                    _upgradeQueue.Enqueue(promise);
                }
                _smallFields._locker.Exit();
                return new Promise<UpgradeableReaderKey>(promise, promise.Id);
            }

            private Promise<UpgradeableReaderKey> UpgradeableReaderLockAsyncImpl(CancelationToken cancelationToken, bool continueOnCapturedContext)
            {
                // We don't spinwait here because it's async; we want to return to caller as fast as possible.

                // Quick check to see if the token is already canceled before entering.
                if (cancelationToken.IsCancelationRequested)
                {
                    ValidateNotAbandoned();

                    return Promise<UpgradeableReaderKey>.Canceled();
                }

                Internal.AsyncUpgradeableReaderLockPromise promise;
                _smallFields._locker.Enter();
                {
                    ValidateNotAbandoned();

                    // Read the _smallFields._lockType into a local variable to avoid extra unnecessary volatile accesses inside the lock.
                    var lockType = _smallFields._lockType;

                    if (CanEnterUpgradeableReaderLock(lockType))
                    {
                        IncrementReaderLockCount();
                        if (lockType == AsyncReaderWriterLockType.None)
                        {
                            SetNextKey();
                        }
                        _smallFields._lockType = lockType | AsyncReaderWriterLockType.Upgradeable;
                        var key = _currentKey;
                        _smallFields._locker.Exit();
                        return Promise.Resolved(new UpgradeableReaderKey(this, key, null));
                    }

                    promise = Internal.AsyncUpgradeableReaderLockPromise.GetOrCreate(this, continueOnCapturedContext);
                    if (promise.HookupAndGetIsCanceled(cancelationToken))
                    {
                        _smallFields._locker.Exit();
                        promise.DisposeImmediate();
                        return Promise<UpgradeableReaderKey>.Canceled();
                    }
                    _upgradeQueue.Enqueue(promise);
                }
                _smallFields._locker.Exit();
                return new Promise<UpgradeableReaderKey>(promise, promise.Id);
            }

            private UpgradeableReaderKey UpgradeableReaderLockImpl()
            {
                // Since this is a synchronous lock, we do a short spinwait before yielding the thread.
                var spinner = new SpinWait();
                while (!CanEnterUpgradeableReaderLock(_smallFields._lockType) & !spinner.NextSpinWillYield)
                {
                    spinner.SpinOnce();
                }

                return UpgradeableReaderLockAsyncImpl(false).WaitForResult();
            }

            private UpgradeableReaderKey UpgradeableReaderLockImpl(CancelationToken cancelationToken)
            {
                // Since this is a synchronous lock, we do a short spinwait before yielding the thread.
                var spinner = new SpinWait();
                while (!CanEnterUpgradeableReaderLock(_smallFields._lockType) & !spinner.NextSpinWillYield & !cancelationToken.IsCancelationRequested)
                {
                    spinner.SpinOnce();
                }

                return UpgradeableReaderLockAsyncImpl(cancelationToken, false).WaitForResult();
            }

            private bool TryEnterUpgradeableReaderLockImpl(out UpgradeableReaderKey readerKey)
            {
                // We don't spinwait here because we want to return immediately without waiting.
                _smallFields._locker.Enter();
                {
                    ValidateNotAbandoned();

                    // Read the _smallFields._lockType into a local variable to avoid extra unnecessary volatile accesses inside the lock.
                    var lockType = _smallFields._lockType;

                    if (CanEnterUpgradeableReaderLock(lockType))
                    {
                        IncrementReaderLockCount();
                        if (lockType == AsyncReaderWriterLockType.None)
                        {
                            SetNextKey();
                        }
                        _smallFields._lockType = lockType | AsyncReaderWriterLockType.Upgradeable;
                        var key = _currentKey;
                        _smallFields._locker.Exit();
                        readerKey = new UpgradeableReaderKey(this, key, null);
                        return true;
                    }
                }
                _smallFields._locker.Exit();
                readerKey = default;
                return false;
            }

            private Promise<(bool didEnter, UpgradeableReaderKey readerKey)> TryEnterUpgradeableReaderLockAsyncImpl(CancelationToken cancelationToken, bool continueOnCapturedContext)
            {
                // We don't spinwait here because it's async; we want to return to caller as fast as possible.
                Internal.AsyncUpgradeableReaderLockPromise promise;
                _smallFields._locker.Enter();
                {
                    ValidateNotAbandoned();

                    // Read the _smallFields._lockType into a local variable to avoid extra unnecessary volatile accesses inside the lock.
                    var lockType = _smallFields._lockType;

                    if (CanEnterUpgradeableReaderLock(lockType))
                    {
                        IncrementReaderLockCount();
                        if (lockType == AsyncReaderWriterLockType.None)
                        {
                            SetNextKey();
                        }
                        _smallFields._lockType = lockType | AsyncReaderWriterLockType.Upgradeable;
                        var key = _currentKey;
                        _smallFields._locker.Exit();
                        return Promise.Resolved((true, new UpgradeableReaderKey(this, key, null)));
                    }
                    // Quick check to see if the token is already canceled before waiting.
                    if (cancelationToken.IsCancelationRequested)
                    {
                        _smallFields._locker.Exit();
                        return Promise.Resolved((false, default(UpgradeableReaderKey)));
                    }

                    promise = Internal.AsyncUpgradeableReaderLockPromise.GetOrCreate(this, continueOnCapturedContext);
                    if (promise.HookupAndGetIsCanceled(cancelationToken))
                    {
                        _smallFields._locker.Exit();
                        promise.DisposeImmediate();
                        return Promise.Resolved((false, default(UpgradeableReaderKey)));
                    }
                    _upgradeQueue.Enqueue(promise);
                }
                _smallFields._locker.Exit();
                return new Promise<UpgradeableReaderKey>(promise, promise.Id)
                    .ContinueWith(resultContainer =>
                    {
                        resultContainer.RethrowIfRejected();
                        return (resultContainer.State == Promise.State.Resolved, resultContainer.Value);
                    });
            }

            private bool TryEnterUpgradeableReaderLockImpl(out UpgradeableReaderKey readerKey, CancelationToken cancelationToken)
            {
                // Since this is a synchronous lock, we do a short spinwait before yielding the thread.
                var spinner = new SpinWait();
                while (!CanEnterUpgradeableReaderLock(_smallFields._lockType) & !spinner.NextSpinWillYield & !cancelationToken.IsCancelationRequested)
                {
                    spinner.SpinOnce();
                }

                var (success, key) = TryEnterUpgradeableReaderLockAsyncImpl(cancelationToken, false).WaitForResult();
                readerKey = key;
                return success;
            }

            private Promise<UpgradedWriterKey> UpgradeToWriterLockAsyncImpl(UpgradeableReaderKey readerKey, bool continueOnCapturedContext)
            {
                // We don't spinwait here because it's async; we want to return to caller as fast as possible.
                Internal.AsyncUpgradedWriterLockPromise promise;
                _smallFields._locker.Enter();
                {
                    ValidateNotAbandoned();

                    if (_currentKey != readerKey._impl._key | this != readerKey._impl._owner)
                    {
                        _smallFields._locker.Exit();
                        ThrowInvalidKey(AsyncReaderWriterLockType.Upgradeable, 2);
                    }

                    DecrementReaderLockCount();
                    _smallFields._lockType |= AsyncReaderWriterLockType.Writer;
                    if (_readerLockCount == 0)
                    {
                        _smallFields._writerType = AsyncReaderWriterLockType.Upgradeable;
                        // We cache the previous key for when the lock gets downgraded.
                        SetNextAndPreviousKeys();
                        var key = _currentKey;
                        _smallFields._locker.Exit();
                        return Promise.Resolved(new UpgradedWriterKey(this, key, null));
                    }

                    promise = Internal.AsyncUpgradedWriterLockPromise.GetOrCreate(this, continueOnCapturedContext);
                    _upgradeWaiter = promise;
                }
                _smallFields._locker.Exit();
                return new Promise<UpgradedWriterKey>(promise, promise.Id);
            }

            private Promise<UpgradedWriterKey> UpgradeToWriterLockAsyncImpl(UpgradeableReaderKey readerKey, CancelationToken cancelationToken, bool continueOnCapturedContext)
            {
                // We don't spinwait here because it's async; we want to return to caller as fast as possible.

                // Quick check to see if the token is already canceled before entering.
                if (cancelationToken.IsCancelationRequested)
                {
                    ValidateNotAbandoned();

                    return Promise<UpgradedWriterKey>.Canceled();
                }

                Internal.AsyncUpgradedWriterLockPromise promise;
                _smallFields._locker.Enter();
                {
                    ValidateNotAbandoned();

                    if (_currentKey != readerKey._impl._key | this != readerKey._impl._owner)
                    {
                        _smallFields._locker.Exit();
                        ThrowInvalidKey(AsyncReaderWriterLockType.Upgradeable, 2);
                    }

                    DecrementReaderLockCount();
                    _smallFields._lockType |= AsyncReaderWriterLockType.Writer;
                    if (_readerLockCount == 0)
                    {
                        _smallFields._writerType = AsyncReaderWriterLockType.Upgradeable;
                        // We cache the previous key for when the lock gets downgraded.
                        SetNextAndPreviousKeys();
                        var key = _currentKey;
                        _smallFields._locker.Exit();
                        return Promise.Resolved(new UpgradedWriterKey(this, key, null));
                    }

                    promise = Internal.AsyncUpgradedWriterLockPromise.GetOrCreate(this, continueOnCapturedContext);
                    if (promise.HookupAndGetIsCanceled(cancelationToken))
                    {
                        _smallFields._locker.Exit();
                        promise.DisposeImmediate();
                        return Promise<UpgradedWriterKey>.Canceled();
                    }
                    _upgradeWaiter = promise;
                }
                _smallFields._locker.Exit();
                return new Promise<UpgradedWriterKey>(promise, promise.Id);
            }

            private UpgradedWriterKey UpgradeToWriterLockImpl(UpgradeableReaderKey readerKey)
            {
                // Since this is a synchronous lock, we do a short spinwait before yielding the thread.
                var spinner = new SpinWait();
                while (_readerLockCount != 1 & !spinner.NextSpinWillYield)
                {
                    spinner.SpinOnce();
                }

                return UpgradeToWriterLockAsyncImpl(readerKey, false).WaitForResult();
            }

            private UpgradedWriterKey UpgradeToWriterLockImpl(UpgradeableReaderKey readerKey, CancelationToken cancelationToken)
            {
                // Since this is a synchronous lock, we do a short spinwait before yielding the thread.
                var spinner = new SpinWait();
                while (_readerLockCount != 1 & !spinner.NextSpinWillYield & !cancelationToken.IsCancelationRequested)
                {
                    spinner.SpinOnce();
                }

                return UpgradeToWriterLockAsyncImpl(readerKey, cancelationToken, false).WaitForResult();
            }

            private bool TryUpgradeToWriterLockImpl(UpgradeableReaderKey readerKey, out UpgradedWriterKey writerKey)
            {
                // We don't spinwait here because we want to return immediately without waiting.
                _smallFields._locker.Enter();
                {
                    ValidateNotAbandoned();

                    if (_currentKey != readerKey._impl._key | this != readerKey._impl._owner)
                    {
                        _smallFields._locker.Exit();
                        ThrowInvalidKey(AsyncReaderWriterLockType.Upgradeable, 2);
                    }

                    if (_readerLockCount == 1)
                    {
                        DecrementReaderLockCount();
                        _smallFields._lockType |= AsyncReaderWriterLockType.Writer;
                        _smallFields._writerType = AsyncReaderWriterLockType.Upgradeable;
                        // We cache the previous key for when the lock gets downgraded.
                        SetNextAndPreviousKeys();
                        var key = _currentKey;
                        _smallFields._locker.Exit();
                        writerKey = new UpgradedWriterKey(this, key, null);
                        return true;
                    }
                }
                _smallFields._locker.Exit();
                writerKey = default;
                return false;
            }

            private Promise<(bool didEnter, UpgradedWriterKey writerKey)> TryUpgradeToWriterLockAsyncImpl(
                UpgradeableReaderKey readerKey, CancelationToken cancelationToken, bool continueOnCapturedContext)
            {
                // We don't spinwait here because it's async; we want to return to caller as fast as possible.
                Internal.AsyncUpgradedWriterLockPromise promise;
                _smallFields._locker.Enter();
                {
                    ValidateNotAbandoned();

                    if (_currentKey != readerKey._impl._key | this != readerKey._impl._owner)
                    {
                        _smallFields._locker.Exit();
                        ThrowInvalidKey(AsyncReaderWriterLockType.Upgradeable, 2);
                    }

                    if (_readerLockCount == 1)
                    {
                        DecrementReaderLockCount();
                        _smallFields._lockType |= AsyncReaderWriterLockType.Writer;
                        _smallFields._writerType = AsyncReaderWriterLockType.Upgradeable;
                        // We cache the previous key for when the lock gets downgraded.
                        SetNextAndPreviousKeys();
                        var key = _currentKey;
                        _smallFields._locker.Exit();
                        return Promise.Resolved((true, new UpgradedWriterKey(this, key, null)));
                    }
                    // Quick check to see if the token is already canceled before waiting.
                    if (cancelationToken.IsCancelationRequested)
                    {
                        _smallFields._locker.Exit();
                        return Promise.Resolved((false, default(UpgradedWriterKey)));
                    }

                    DecrementReaderLockCount();
                    _smallFields._lockType |= AsyncReaderWriterLockType.Writer;
                    promise = Internal.AsyncUpgradedWriterLockPromise.GetOrCreate(this, continueOnCapturedContext);
                    if (promise.HookupAndGetIsCanceled(cancelationToken))
                    {
                        _smallFields._locker.Exit();
                        promise.DisposeImmediate();
                        return Promise.Resolved((true, new UpgradedWriterKey(this, _currentKey, null)));
                    }
                    _upgradeWaiter = promise;
                }
                _smallFields._locker.Exit();
                return new Promise<UpgradedWriterKey>(promise, promise.Id)
                    .ContinueWith(resultContainer =>
                    {
                        resultContainer.RethrowIfRejected();
                        return (resultContainer.State == Promise.State.Resolved, resultContainer.Value);
                    });
            }

            private bool TryUpgradeToWriterLockImpl(UpgradeableReaderKey readerKey, out UpgradedWriterKey writerKey, CancelationToken cancelationToken)
            {
                // Since this is a synchronous lock, we do a short spinwait before yielding the thread.
                var spinner = new SpinWait();
                while (_readerLockCount != 1 & !spinner.NextSpinWillYield & !cancelationToken.IsCancelationRequested)
                {
                    spinner.SpinOnce();
                }

                var (success, key) = TryUpgradeToWriterLockAsyncImpl(readerKey, cancelationToken, false).WaitForResult();
                writerKey = key;
                return success;
            }

            internal void ReleaseReaderLock(long key)
            {
                Internal.AsyncWriterLockPromise writerPromise;
                Internal.AsyncUpgradedWriterLockPromise upgradedWriterPromise;
                _smallFields._locker.Enter();
                if (_currentKey != key)
                {
                    _smallFields._locker.Exit();
                    ThrowInvalidKey(AsyncReaderWriterLockType.Reader, 2);
                }

                // We check for underflow, because multiple readers share the same key.
                checked
                {
                    if (--_readerLockCount != 0)
                    {
                        _smallFields._locker.Exit();
                        return;
                    }
                }
                // Check upgrade waiter before regular writer queue.
                if (_upgradeWaiter != null)
                {
                    _smallFields._lockType = AsyncReaderWriterLockType.Writer | AsyncReaderWriterLockType.Upgradeable;
                    _smallFields._writerType = AsyncReaderWriterLockType.Upgradeable;
                    // We cache the previous key for when the lock gets downgraded.
                    SetNextAndPreviousKeys();
                    upgradedWriterPromise = _upgradeWaiter;
                    _upgradeWaiter = null;
                    _smallFields._locker.Exit();
                    upgradedWriterPromise.Resolve(_currentKey);
                    return;
                }
                if (_writerQueue.IsNotEmpty)
                {
                    _smallFields._lockType = AsyncReaderWriterLockType.Writer | (_smallFields._lockType & AsyncReaderWriterLockType.Upgradeable);
                    _smallFields._writerType = AsyncReaderWriterLockType.Writer;
                    // We cache the previous key for when the lock gets downgraded.
                    SetNextAndPreviousKeys();
                    writerPromise = _writerQueue.Dequeue();
                    _smallFields._locker.Exit();
                    writerPromise.Resolve(_currentKey);
                    return;
                }
                // The lock is exited completely.
                _smallFields._lockType = AsyncReaderWriterLockType.None;
                _smallFields._writerType = AsyncReaderWriterLockType.None;
                _currentKey = 0L;
                _smallFields._locker.Exit();
            }

            internal void ReleaseNormalWriterLock(long key)
            {
                _smallFields._locker.Enter();
                if (_currentKey != key)
                {
                    _smallFields._locker.Exit();
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
                    _smallFields._writerType = AsyncReaderWriterLockType.None;
                    _readerLockCount = _readerWaitCount;
                    _readerWaitCount = 0;
                    var readers = _readerQueue.MoveElementsToStack();
                    if (_upgradeQueue.IsEmpty)
                    {
                        _smallFields._lockType = lockType;
                        _smallFields._locker.Exit();
                    }
                    else
                    {
                        IncrementReaderLockCount();
                        _smallFields._lockType = lockType | AsyncReaderWriterLockType.Upgradeable;
                        var upgradeablePromise = _upgradeQueue.Dequeue();
                        _smallFields._locker.Exit();
                        upgradeablePromise.Resolve(_currentKey);
                    }
                    while (readers.IsNotEmpty)
                    {
                        readers.Pop().Resolve(_currentKey);
                    }
                    return;
                }

                // Check upgrade queue before writer queue, to prevent writers from starving upgradeable readers.
                // Unless there is a waiting writer, and the contention strategy is writer prioritized.
                if (_upgradeQueue.IsNotEmpty & !prioritizedWriter)
                {
                    _smallFields._lockType = hasWaitingWriter
                        ? _smallFields._lockType | AsyncReaderWriterLockType.Upgradeable
                        : AsyncReaderWriterLockType.Upgradeable;
                    _smallFields._writerType = AsyncReaderWriterLockType.None;
                    IncrementReaderLockCount();
                    var upgradeablePromise = _upgradeQueue.Dequeue();
                    _smallFields._locker.Exit();
                    upgradeablePromise.Resolve(_currentKey);
                    return;
                }

                if (hasWaitingWriter)
                {
                    SetNextKey();
                    var writerPromise = _writerQueue.Dequeue();
                    _smallFields._locker.Exit();
                    writerPromise.Resolve(_currentKey);
                    return;
                }

                // The lock is exited completely.
                _smallFields._lockType = AsyncReaderWriterLockType.None;
                _smallFields._writerType = AsyncReaderWriterLockType.None;
                _currentKey = 0;
                _smallFields._locker.Exit();
            }

            internal void ReleaseUpgradedWriterLock(long key)
            {
                Internal.ValueLinkedStack<Internal.AsyncReaderLockPromise> readers;
                _smallFields._locker.Enter();
                {
                    if (_currentKey != key)
                    {
                        _smallFields._locker.Exit();
                        ThrowInvalidKey(AsyncReaderWriterLockType.Writer | AsyncReaderWriterLockType.Upgradeable, 2);
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
                        _smallFields._lockType = hasWaitingWriter
                            ? AsyncReaderWriterLockType.Upgradeable | AsyncReaderWriterLockType.Writer
                            : AsyncReaderWriterLockType.Upgradeable;
                        _smallFields._writerType = AsyncReaderWriterLockType.None;
                        _smallFields._locker.Exit();
                        return;
                    }

                    // We add 1 more reader count since it was removed when the lock was upgraded.
                    // Unchecked context since we already checked for overflow when the reader lock was acquired.
                    unchecked
                    {
                        _readerLockCount = _readerWaitCount + 1;
                    }
                    _readerWaitCount = 0;
                    _smallFields._lockType = hasWaitingWriter
                        ? AsyncReaderWriterLockType.Reader | AsyncReaderWriterLockType.Upgradeable | AsyncReaderWriterLockType.Writer
                        : AsyncReaderWriterLockType.Reader | AsyncReaderWriterLockType.Upgradeable;
                    _smallFields._writerType = AsyncReaderWriterLockType.None;
                    readers = _readerQueue.MoveElementsToStack();
                }
                _smallFields._locker.Exit();

                do
                {
                    readers.Pop().Resolve(_currentKey);
                } while (readers.IsNotEmpty);
            }

            internal void ReleaseUpgradeableReaderLock(long key)
            {
                _smallFields._locker.Enter();
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
                        _smallFields._lockType &= ~AsyncReaderWriterLockType.Upgradeable;
                        _smallFields._locker.Exit();
                        return;
                    }
                    IncrementReaderLockCount();
                    var upgradeablePromise = _upgradeQueue.Dequeue();
                    _smallFields._locker.Exit();
                    upgradeablePromise.Resolve(_currentKey);
                    return;
                }

                // Check writer queue before checking upgrade queue, to prevent starvation.
                // Unless there is a waiting upgradeable reader, and the contention strategy is (upgradeable)reader prioritized.
                bool hasUpgradeable = _upgradeQueue.IsNotEmpty;
                bool prioritizedUpgradeable = hasUpgradeable & PrioritizeReadersOrUpgradeableReaders;
                if (_writerQueue.IsNotEmpty & !prioritizedUpgradeable)
                {
                    var writerPromise = _writerQueue.Dequeue();
                    _smallFields._lockType &= ~AsyncReaderWriterLockType.Upgradeable;
                    _smallFields._writerType = AsyncReaderWriterLockType.Writer;
                    SetNextKey();
                    _smallFields._locker.Exit();
                    writerPromise.Resolve(_currentKey);
                    return;
                }

                if (hasUpgradeable)
                {
                    IncrementReaderLockCount();
                    var upgradeablePromise = _upgradeQueue.Dequeue();
                    _smallFields._locker.Exit();
                    upgradeablePromise.Resolve(_currentKey);
                    return;
                }

                // The lock is exited completely.
                _smallFields._lockType = AsyncReaderWriterLockType.None;
                _smallFields._writerType = AsyncReaderWriterLockType.None;
                _currentKey = 0;
                _smallFields._locker.Exit();
            }

            internal bool TryUnregister(Internal.AsyncReaderLockPromise promise)
            {
                _smallFields._locker.Enter();
                if (_readerQueue.TryRemove(promise))
                {
                    --_readerWaitCount;
                    _smallFields._locker.Exit();
                    return true;
                }
                _smallFields._locker.Exit();
                return false;
            }

            internal bool TryUnregister(Internal.AsyncWriterLockPromise promise)
            {
                _smallFields._locker.Enter();
                if (!_writerQueue.TryRemove(promise))
                {
                    _smallFields._locker.Exit();
                    return false;
                }

                bool hasNoWriter = _writerQueue.IsEmpty & _upgradeWaiter == null & _smallFields._writerType == AsyncReaderWriterLockType.None;
                if (!hasNoWriter)
                {
                    _smallFields._locker.Exit();
                    return true;
                }

                _smallFields._lockType &= ~AsyncReaderWriterLockType.Writer;
                _readerLockCount += _readerWaitCount;
                _readerWaitCount = 0;
                var readers = _readerQueue.MoveElementsToStack();

                if ((_smallFields._lockType & AsyncReaderWriterLockType.Upgradeable) != 0 | _upgradeQueue.IsEmpty)
                {
                    _smallFields._locker.Exit();
                }
                else
                {
                    IncrementReaderLockCount();
                    var upgradeablePromise = _upgradeQueue.Dequeue();
                    _smallFields._locker.Exit();
                    upgradeablePromise.Resolve(_currentKey);
                }
                while (readers.IsNotEmpty)
                {
                    readers.Pop().Resolve(_currentKey);
                }
                return true;
            }

            internal bool TryUnregister(Internal.AsyncUpgradeableReaderLockPromise promise)
            {
                _smallFields._locker.Enter();
                var removed = _upgradeQueue.TryRemove(promise);
                _smallFields._locker.Exit();
                return removed;
            }

            internal bool TryUnregister(Internal.AsyncUpgradedWriterLockPromise promise)
            {
                Internal.ValueLinkedStack<Internal.AsyncReaderLockPromise> readers;
                _smallFields._locker.Enter();
                {
                    if (_upgradeWaiter != promise)
                    {
                        _smallFields._locker.Exit();
                        return false;
                    }

                    // Re-add the count that was subtracted when it was upgraded.
                    IncrementReaderLockCount();
                    _upgradeWaiter = null;

                    bool hasNoWriter = _writerQueue.IsEmpty & _smallFields._writerType == AsyncReaderWriterLockType.None;
                    if (!hasNoWriter)
                    {
                        _smallFields._locker.Exit();
                        return true;
                    }

                    _smallFields._lockType = AsyncReaderWriterLockType.Reader | AsyncReaderWriterLockType.Upgradeable;
                    _readerLockCount += _readerWaitCount;
                    _readerWaitCount = 0;
                    readers = _readerQueue.MoveElementsToStack();
                }
                _smallFields._locker.Exit();

                while (readers.IsNotEmpty)
                {
                    readers.Pop().Resolve(_currentKey);
                }
                return true;
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            internal static void ThrowInvalidKey(AsyncReaderWriterLockType keyType, int skipFrames)
                => throw new InvalidOperationException($"The {GetKeyTypeString(keyType)} is invalid for this operation.", Internal.GetFormattedStacktrace(skipFrames + 1));

            [MethodImpl(MethodImplOptions.NoInlining)]
            private void ThrowInvalidUpgradeableKeyReleased(long key, int skipFrames)
            {
                string message = _currentKey != key
                    ? "The AsyncReaderWriterLock.UpgradeableReaderKey is invalid for this operation. Did you dispose it before the upgraded writer key?"
                    : "The AsyncReaderWriterLock.UpgradeableReaderKey cannot be released before the UpgradeToWriterLockAsync promise is complete.";
                _smallFields._locker.Exit();
                throw new InvalidOperationException(message, Internal.GetFormattedStacktrace(skipFrames + 1));
            }

            internal static string GetKeyTypeString(AsyncReaderWriterLockType keyType)
            {
                return $"{nameof(AsyncReaderWriterLock)}." +
                    (keyType == AsyncReaderWriterLockType.Reader ? nameof(ReaderKey)
                    : keyType == AsyncReaderWriterLockType.Writer ? nameof(WriterKey)
                    : keyType == AsyncReaderWriterLockType.Upgradeable ? nameof(UpgradeableReaderKey)
                    : nameof(UpgradedWriterKey));
            }

            partial void ValidateNotAbandoned();
#if PROMISE_DEBUG
            volatile private string _abandonedMessage;

            partial void ValidateNotAbandoned()
            {
                if (_abandonedMessage != null)
                {
                    _smallFields._locker.Exit();
                    throw new AbandonedLockException(_abandonedMessage);
                }
            }

            internal void NotifyAbandoned(string abandonedMessage, Internal.ITraceable traceable)
            {
                Internal.ValueLinkedStack<Internal.AsyncReaderLockPromise> readers;
                Internal.ValueLinkedStack<Internal.AsyncWriterLockPromise> writers;
                Internal.ValueLinkedStack<Internal.AsyncUpgradeableReaderLockPromise> upgradeables;
                Internal.AsyncUpgradedWriterLockPromise upgradeWaiter;
                _smallFields._locker.Enter();
                {
                    if (_abandonedMessage != null)
                    {
                        // Additional abandoned keys could be caused by the first abandoned key, so do nothing.
                        _smallFields._locker.Exit();
                        return;
                    }
                    _abandonedMessage = abandonedMessage;

                    readers = _readerQueue.MoveElementsToStack();
                    writers = _writerQueue.MoveElementsToStack();
                    upgradeables = _upgradeQueue.MoveElementsToStack();
                    upgradeWaiter = _upgradeWaiter;
                    _upgradeWaiter = null;
                }
                _smallFields._locker.Exit();

                var rejectContainer = Internal.CreateRejectContainer(new AbandonedLockException(abandonedMessage), int.MinValue, null, traceable);

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

            partial struct ReaderKey
            {
                internal readonly Internal.AsyncReaderWriterLockKey _impl;

                [MethodImpl(Internal.InlineOption)]
                internal ReaderKey(AsyncReaderWriterLock owner, long key, Internal.ITraceable traceable)
                {
                    _impl = new Internal.AsyncReaderWriterLockKey(owner, key, AsyncReaderWriterLockType.Reader, traceable);
                }

                [MethodImpl(Internal.InlineOption)]
                internal ReaderKey(AsyncReaderWriterLock owner)
                {
                    _impl = new Internal.AsyncReaderWriterLockKey(owner);
                }
            }

            partial struct WriterKey
            {
                internal readonly Internal.AsyncReaderWriterLockKey _impl;

                [MethodImpl(Internal.InlineOption)]
                internal WriterKey(AsyncReaderWriterLock owner, long key, Internal.ITraceable traceable)
                {
                    _impl = new Internal.AsyncReaderWriterLockKey(owner, key, AsyncReaderWriterLockType.Writer, traceable);
                }

                [MethodImpl(Internal.InlineOption)]
                internal WriterKey(AsyncReaderWriterLock owner)
                {
                    _impl = new Internal.AsyncReaderWriterLockKey(owner);
                }
            }

            partial struct UpgradeableReaderKey
            {
                internal readonly Internal.AsyncReaderWriterLockKey _impl;

                [MethodImpl(Internal.InlineOption)]
                internal UpgradeableReaderKey(AsyncReaderWriterLock owner, long key, Internal.ITraceable traceable)
                {
                    _impl = new Internal.AsyncReaderWriterLockKey(owner, key, AsyncReaderWriterLockType.Upgradeable, traceable);
                }

                [MethodImpl(Internal.InlineOption)]
                internal UpgradeableReaderKey(AsyncReaderWriterLock owner)
                {
                    _impl = new Internal.AsyncReaderWriterLockKey(owner);
                }
            }

            partial struct UpgradedWriterKey
            {
                internal readonly Internal.AsyncReaderWriterLockKey _impl;

                [MethodImpl(Internal.InlineOption)]
                internal UpgradedWriterKey(AsyncReaderWriterLock owner, long key, Internal.ITraceable traceable)
                {
                    _impl = new Internal.AsyncReaderWriterLockKey(owner, key, AsyncReaderWriterLockType.Writer, traceable);
                }

                [MethodImpl(Internal.InlineOption)]
                internal UpgradedWriterKey(AsyncReaderWriterLock owner)
                {
                    _impl = new Internal.AsyncReaderWriterLockKey(owner);
                }
            }
        } // class AsyncReaderWriterLock
#endif // UNITY_2021_2_OR_NEWER || NETSTANDARD2_1_OR_GREATER || NETCOREAPP
    } // namespace Threading
} // namespace Proto.Promises.Threading