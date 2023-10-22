#if UNITY_5_5 || NET_2_0 || NET_2_0_SUBSET
#define NET_LEGACY
#endif

using System;
using System.Diagnostics;

namespace Proto.Promises.Threading
{

#if UNITY_2021_2_OR_NEWER || NETSTANDARD2_1_OR_GREATER || NETCOREAPP
    // Old IL2CPP runtime crashes if this code exists, even if it is not used. So we only include them in newer build targets that old Unity versions cannot consume.
    // See https://github.com/timcassell/ProtoPromise/issues/169 for details.

    /// <summary>
    /// A reader/writer lock that is compatible with asynchronous operations. This type supports multiple readers or exclusive writer access.
    /// </summary>
    /// <remarks>
    /// Lock re-entrancy is not supported. Attempting to re-acquire the lock while it is already acquired (without upgrading) will cause a deadlock.
    /// </remarks>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public sealed partial class AsyncReaderWriterLock
    {
        /// <summary>
        /// The strategy used to resolve reader and writer contention on the <see cref="AsyncReaderWriterLock"/>.
        /// </summary>
        public enum ContentionStrategy
        {
            /// <summary>
            /// The default, recommended contention strategy that prevents reader or writer starvation.
            /// If there is contention on the lock between readers and writers, they take turns.
            /// </summary>
            Balanced,
            /// <summary>
            /// A contention strategy that prefers writers over readers.
            /// </summary>
            /// <remarks>
            /// This strategy is not recommended! It can cause reader starvation.
            /// Only use this strategy if you have profiled your use case and found this strategy to be necessary!
            /// </remarks>
            PrioritizeWriters,
            /// <summary>
            /// A contention strategy that prefers readers over writers.
            /// </summary>
            /// <remarks>
            /// This strategy is not recommended! It can cause writer starvation.
            /// Only use this strategy if you have profiled your use case and found this strategy to be necessary!
            /// </remarks>
            PrioritizeReaders,
            /// <summary>
            /// A contention strategy that prefers readers over writers, except when an upgradeable reader upgrades to a writer.
            /// </summary>
            /// <remarks>
            /// This strategy is not recommended! It can cause normal writer starvation.
            /// Only use this strategy if you have profiled your use case and found this strategy to be necessary!
            /// </remarks>
            PrioritizeUpgradeableReaders
        }

        // We wrap the impl with another class so that we can lock on it safely.
        private readonly Internal.AsyncReaderWriterLockInternal _impl;

        /// <summary>
        /// Creates a new async-compatible reader/writer lock that does not support re-entrancy, with a balanced contention strategy.
        /// </summary>
        public AsyncReaderWriterLock() : this(ContentionStrategy.Balanced) { }

        /// <summary>
        /// Creates a new async-compatible reader/writer lock that does not support re-entrancy, with the provided contention strategy.
        /// </summary>
        /// <param name="contentionStrategy">The strategy used to resolve reader and writer contention on the lock.</param>
        /// <remarks>
        /// Using a strategy other than <see cref="ContentionStrategy.Balanced"/> is not recommended! It can cause reader or writer starvation.
        /// Only use another strategy if you have profiled your use case and found it to be necessary!
        /// </remarks>
        public AsyncReaderWriterLock(ContentionStrategy contentionStrategy)
        {
            _impl = new Internal.AsyncReaderWriterLockInternal(contentionStrategy);
        }

        /// <summary>
        /// Asynchronously acquire the lock as a reader.
        /// Returns a <see cref="Promise{T}"/> that will be resolved when the lock has been acquired.
        /// The result of the promise is the key that will release the lock when it is disposed.
        /// </summary>
        public Promise<ReaderKey> ReaderLockAsync()
        {
            return _impl.ReaderLockAsync();
        }

        /// <summary>
        /// Asynchronously acquire the lock as a reader, while observing a <see cref="CancelationToken"/>.
        /// Returns a <see cref="Promise{T}"/> that will be resolved when the lock has been acquired.
        /// The result of the promise is the key that will release the lock when it is disposed.
        /// </summary>
        /// <param name="cancelationToken">The <see cref="CancelationToken"/> used to cancel the lock. If the token is canceled before the lock has been acquired, the returned <see cref="Promise{T}"/> will be canceled.</param>
        public Promise<ReaderKey> ReaderLockAsync(CancelationToken cancelationToken)
        {
            return _impl.ReaderLockAsync(cancelationToken);
        }

        /// <summary>
        /// Synchronously acquire the lock as a reader.
        /// Returns the key that will release the lock when it is disposed.
        /// </summary>
        public ReaderKey ReaderLock()
        {
            return _impl.ReaderLock();
        }

        /// <summary>
        /// Synchronously acquire the lock as a reader, while observing a <see cref="CancelationToken"/>.
        /// Returns the key that will release the lock when it is disposed.
        /// </summary>
        /// <param name="cancelationToken">The <see cref="CancelationToken"/> used to cancel the lock. If the token is canceled before the lock has been acquired, a <see cref="CanceledException"/> will be thrown.</param>
        public ReaderKey ReaderLock(CancelationToken cancelationToken)
        {
            return _impl.ReaderLock(cancelationToken);
        }

        /// <summary>
        /// Synchronously try to acquire the lock as a reader.
        /// If successful, <paramref name="readerKey"/> is the key that will release the lock when it is disposed.
        /// This method does not wait, and returns immediately.
        /// </summary>
        /// <param name="readerKey">If successful, the key that will release the lock when it is disposed.</param>
        public bool TryEnterReaderLock(out ReaderKey readerKey)
        {
            return _impl.TryEnterReaderLock(out readerKey);
        }

        /// <summary>
        /// Asynchronously try to acquire the lock as a reader, while observing a <see cref="CancelationToken"/>.
        /// Returns a <see cref="Promise{T}"/> that will be resolved when the lock has been acquired, or the <paramref name="cancelationToken"/> has been canceled, with the success state and key.
        /// If successful, the key will release the lock when it is disposed.
        /// </summary>
        /// <param name="cancelationToken">
        /// The <see cref="CancelationToken"/> used to cancel the lock. If the token is canceled before the lock has been acquired, the success state of the returned <see cref="Promise{T}"/> will be <see langword="false"/>.
        /// </param>
        /// <remarks>
        /// This first tries to take the lock before checking the <paramref name="cancelationToken"/>>.
        /// If the lock is available, the result will be (<see langword="true"/>, key), even if the <paramref name="cancelationToken"/> is already canceled.
        /// </remarks>
        public Promise<(bool didEnter, ReaderKey readerKey)> TryEnterReaderLockAsync(CancelationToken cancelationToken)
        {
            return _impl.TryEnterReaderLockAsync(cancelationToken);
        }

        /// <summary>
        /// Synchronously try to acquire the lock as a reader, while observing a <see cref="CancelationToken"/>.
        /// If successful, <paramref name="readerKey"/> is the key that will release the lock when it is disposed.
        /// </summary>
        /// <param name="readerKey">If successful, the key that will release the lock when it is disposed.</param>
        /// <param name="cancelationToken">The <see cref="CancelationToken"/> used to cancel the lock. If the token is canceled before the lock has been acquired, this will return <see langword="false"/>.</param>
        /// <returns><see langword="true"/> if the reader lock was acquired before the <paramref name="cancelationToken"/> was canceled, <see langword="false"/> otherwise.</returns>
        /// <remarks>
        /// This first tries to take the lock before checking the <paramref name="cancelationToken"/>>.
        /// If the reader lock is available, this will return <see langword="true"/>, even if the <paramref name="cancelationToken"/> is already canceled.
        /// </remarks>
        public bool TryEnterReaderLock(out ReaderKey readerKey, CancelationToken cancelationToken)
        {
            return _impl.TryEnterReaderLock(out readerKey, cancelationToken);
        }

        /// <summary>
        /// Asynchronously acquire the lock as a writer.
        /// Returns a <see cref="Promise{T}"/> that will be resolved when the lock has been acquired.
        /// The result of the promise is the key that will release the lock when it is disposed.
        /// </summary>
        public Promise<WriterKey> WriterLockAsync()
        {
            return _impl.WriterLockAsync();
        }

        /// <summary>
        /// Asynchronously acquire the lock as a writer, while observing a <see cref="CancelationToken"/>.
        /// Returns a <see cref="Promise{T}"/> that will be resolved when the lock has been acquired.
        /// The result of the promise is the key that will release the lock when it is disposed.
        /// </summary>
        /// <param name="cancelationToken">The <see cref="CancelationToken"/> used to cancel the lock. If the token is canceled before the lock has been acquired, the returned <see cref="Promise{T}"/> will be canceled.</param>
        public Promise<WriterKey> WriterLockAsync(CancelationToken cancelationToken)
        {
            return _impl.WriterLockAsync(cancelationToken);
        }

        /// <summary>
        /// Synchronously acquire the lock as a writer.
        /// Returns the key that will release the lock when it is disposed.
        /// </summary>
        public WriterKey WriterLock()
        {
            return _impl.WriterLock();
        }

        /// <summary>
        /// Synchronously acquire the lock as a writer, while observing a <see cref="CancelationToken"/>.
        /// Returns the key that will release the lock when it is disposed.
        /// </summary>
        /// <param name="cancelationToken">The <see cref="CancelationToken"/> used to cancel the lock. If the token is canceled before the lock has been acquired, a <see cref="CanceledException"/> will be thrown.</param>
        public WriterKey WriterLock(CancelationToken cancelationToken)
        {
            return _impl.WriterLock(cancelationToken);
        }

        /// <summary>
        /// Synchronously try to acquire the lock as a writer.
        /// If successful, <paramref name="writerKey"/> is the key that will release the lock when it is disposed.
        /// This method does not wait, and returns immediately.
        /// </summary>
        /// <param name="writerKey">If successful, the key that will release the lock when it is disposed.</param>
        public bool TryEnterWriterLock(out WriterKey writerKey)
        {
            return _impl.TryEnterWriterLock(out writerKey);
        }

        /// <summary>
        /// Asynchronously try to acquire the lock as a writer, while observing a <see cref="CancelationToken"/>.
        /// Returns a <see cref="Promise{T}"/> that will be resolved when the lock has been acquired, or the <paramref name="cancelationToken"/> has been canceled, with the success state and key.
        /// If successful, the key will release the lock when it is disposed.
        /// </summary>
        /// <param name="cancelationToken">
        /// The <see cref="CancelationToken"/> used to cancel the lock. If the token is canceled before the lock has been acquired, the success state of the returned <see cref="Promise{T}"/> will be <see langword="false"/>.
        /// </param>
        /// <remarks>
        /// This first tries to take the lock before checking the <paramref name="cancelationToken"/>>.
        /// If the lock is available, the result will be (<see langword="true"/>, key), even if the <paramref name="cancelationToken"/> is already canceled.
        /// </remarks>
        public Promise<(bool didEnter, WriterKey writerKey)> TryEnterWriterLockAsync(CancelationToken cancelationToken)
        {
            return _impl.TryEnterWriterLockAsync(cancelationToken);
        }

        /// <summary>
        /// Synchronously try to acquire the lock as a writer, while observing a <see cref="CancelationToken"/>.
        /// If successful, <paramref name="writerKey"/> is the key that will release the lock when it is disposed.
        /// </summary>
        /// <param name="writerKey">If successful, the key that will release the lock when it is disposed.</param>
        /// <param name="cancelationToken">The <see cref="CancelationToken"/> used to cancel the lock. If the token is canceled before the lock has been acquired, this will return <see langword="false"/>.</param>
        /// <returns><see langword="true"/> if the reader lock was acquired before the <paramref name="cancelationToken"/> was canceled, <see langword="false"/> otherwise.</returns>
        /// <remarks>
        /// This first tries to take the lock before checking the <paramref name="cancelationToken"/>>.
        /// If the reader lock is available, this will return <see langword="true"/>, even if the <paramref name="cancelationToken"/> is already canceled.
        /// </remarks>
        public bool TryEnterWriterLock(out WriterKey writerKey, CancelationToken cancelationToken)
        {
            return _impl.TryEnterWriterLock(out writerKey, cancelationToken);
        }

        /// <summary>
        /// Asynchronously acquire the lock as an upgradeable reader.
        /// Returns a <see cref="Promise{T}"/> that will be resolved when the lock has been acquired.
        /// The result of the promise is the key that will release the lock when it is disposed.
        /// </summary>
        /// <remarks>
        /// Upgradeable reader locks may be entered concurrently with normal reader locks, but they are mutually exclusive with respect to other upgradeable reader locks and writer locks.
        /// Only 1 upgradeable reader lock may be entered at a time.
        /// </remarks>
        public Promise<UpgradeableReaderKey> UpgradeableReaderLockAsync()
        {
            return _impl.UpgradeableReaderLockAsync();
        }

        /// <summary>
        /// Asynchronously acquire the lock as an upgradeable reader, while observing a <see cref="CancelationToken"/>.
        /// Returns a <see cref="Promise{T}"/> that will be resolved when the lock has been acquired.
        /// The result of the promise is the key that will release the lock when it is disposed.
        /// </summary>
        /// <param name="cancelationToken">The <see cref="CancelationToken"/> used to cancel the lock. If the token is canceled before the lock has been acquired, the returned <see cref="Promise{T}"/> will be canceled.</param>
        /// <remarks>
        /// Upgradeable reader locks may be entered concurrently with normal reader locks, but they are mutually exclusive with respect to other upgradeable reader locks and writer locks.
        /// Only 1 upgradeable reader lock may be entered at a time.
        /// </remarks>
        public Promise<UpgradeableReaderKey> UpgradeableReaderLockAsync(CancelationToken cancelationToken)
        {
            return _impl.UpgradeableReaderLockAsync(cancelationToken);
        }

        /// <summary>
        /// Synchronously acquire the lock as an upgradeable reader.
        /// Returns the key that will release the lock when it is disposed.
        /// </summary>
        /// <remarks>
        /// Upgradeable reader locks may be entered concurrently with normal reader locks, but they are mutually exclusive with respect to other upgradeable reader locks and writer locks.
        /// Only 1 upgradeable reader lock may be entered at a time.
        /// </remarks>
        public UpgradeableReaderKey UpgradeableReaderLock()
        {
            return _impl.UpgradeableReaderLock();
        }

        /// <summary>
        /// Synchronously acquire the lock as an upgradeable reader, while observing a <see cref="CancelationToken"/>.
        /// Returns the key that will release the lock when it is disposed.
        /// </summary>
        /// <param name="cancelationToken">The <see cref="CancelationToken"/> used to cancel the lock. If the token is canceled before the lock has been acquired, a <see cref="CanceledException"/> will be thrown.</param>
        /// <remarks>
        /// Upgradeable reader locks may be entered concurrently with normal reader locks, but they are mutually exclusive with respect to other upgradeable reader locks and writer locks.
        /// Only 1 upgradeable reader lock may be entered at a time.
        /// </remarks>
        public UpgradeableReaderKey UpgradeableReaderLock(CancelationToken cancelationToken)
        {
            return _impl.UpgradeableReaderLock(cancelationToken);
        }

        /// <summary>
        /// Synchronously try to acquire the lock as an upgradeable reader.
        /// If successful, <paramref name="readerKey"/> is the key that will release the lock when it is disposed.
        /// This method does not wait, and returns immediately.
        /// </summary>
        /// <param name="readerKey">If successful, the key that will release the lock when it is disposed.</param>
        /// <remarks>
        /// Upgradeable reader locks may be entered concurrently with normal reader locks, but they are mutually exclusive with respect to other upgradeable reader locks and writer locks.
        /// Only 1 upgradeable reader lock may be entered at a time.
        /// </remarks>
        public bool TryEnterUpgradeableReaderLock(out UpgradeableReaderKey readerKey)
        {
            return _impl.TryEnterUpgradeableReaderLock(out readerKey);
        }

        /// <summary>
        /// Asynchronously try to acquire the lock as an upgradeable reader, while observing a <see cref="CancelationToken"/>.
        /// Returns a <see cref="Promise{T}"/> that will be resolved when the lock has been acquired, or the <paramref name="cancelationToken"/> has been canceled, with the success state and key.
        /// If successful, the key will release the lock when it is disposed.
        /// </summary>
        /// <param name="cancelationToken">
        /// The <see cref="CancelationToken"/> used to cancel the lock. If the token is canceled before the lock has been acquired, the success state of the returned <see cref="Promise{T}"/> will be <see langword="false"/>.
        /// </param>
        /// <remarks>
        /// This first tries to take the lock before checking the <paramref name="cancelationToken"/>>.
        /// If the lock is available, the result will be (<see langword="true"/>, key), even if the <paramref name="cancelationToken"/> is already canceled.
        /// </remarks>
        public Promise<(bool didEnter, UpgradeableReaderKey readerKey)> TryEnterUpgradeableReaderLockAsync(CancelationToken cancelationToken)
        {
            return _impl.TryEnterUpgradeableReaderLockAsync(cancelationToken);
        }

        /// <summary>
        /// Synchronously try to acquire the lock as an upgradeable reader, while observing a <see cref="CancelationToken"/>.
        /// If successful, <paramref name="readerKey"/> is the key that will release the lock when it is disposed.
        /// </summary>
        /// <param name="readerKey">If successful, the key that will release the lock when it is disposed.</param>
        /// <param name="cancelationToken">The <see cref="CancelationToken"/> used to cancel the lock. If the token is canceled before the lock has been acquired, this will return <see langword="false"/>.</param>
        /// <returns><see langword="true"/> if the reader lock was acquired before the <paramref name="cancelationToken"/> was canceled, <see langword="false"/> otherwise.</returns>
        /// <remarks>
        /// This first tries to take the lock before checking the <paramref name="cancelationToken"/>>.
        /// If the reader lock is available, this will return <see langword="true"/>, even if the <paramref name="cancelationToken"/> is already canceled.
        /// </remarks>
        public bool TryEnterUpgradeableReaderLock(out UpgradeableReaderKey readerKey, CancelationToken cancelationToken)
        {
            return _impl.TryEnterUpgradeableReaderLock(out readerKey, cancelationToken);
        }

        /// <summary>
        /// Asynchronously upgrade the lock from an upgradeable reader lock to a writer lock.
        /// Returns a <see cref="Promise{T}"/> that will be resolved when the lock has been upgraded.
        /// The result of the promise is the key that will downgrade the lock to an upgradeable reader lock when it is disposed.
        /// </summary>
        /// <param name="readerKey">The key required to upgrade the lock.</param>
        public Promise<WriterKey> UpgradeToWriterLockAsync(UpgradeableReaderKey readerKey)
        {
            return _impl.UpgradeToWriterLockAsync(readerKey);
        }

        /// <summary>
        /// Asynchronously upgrade the lock from an upgradeable reader lock to a writer lock, while observing a <see cref="CancelationToken"/>.
        /// Returns a <see cref="Promise{T}"/> that will be resolved when the lock has been upgraded.
        /// The result of the promise is the key that will downgrade the lock to an upgradeable reader lock when it is disposed.
        /// </summary>
        /// <param name="readerKey">The key required to upgrade the lock.</param>
        /// <param name="cancelationToken">The <see cref="CancelationToken"/> used to cancel the upgrade. If the token is canceled before the lock has been upgraded, the returned <see cref="Promise{T}"/> will be canceled.</param>
        public Promise<WriterKey> UpgradeToWriterLockAsync(UpgradeableReaderKey readerKey, CancelationToken cancelationToken)
        {
            return _impl.UpgradeToWriterLockAsync(readerKey, cancelationToken);
        }

        /// <summary>
        /// Synchronously upgrade the lock from an upgradeable reader lock to a writer lock.
        /// Returns the key that will downgrade the lock to an upgradeable reader lock when it is disposed.
        /// </summary>
        /// <param name="readerKey">The key required to upgrade the lock.</param>
        public WriterKey UpgradeToWriterLock(UpgradeableReaderKey readerKey)
        {
            return _impl.UpgradeToWriterLock(readerKey);
        }

        /// <summary>
        /// Synchronously upgrade the lock from an upgradeable reader lock to a writer lock, while observing a <see cref="CancelationToken"/>.
        /// Returns the key that will downgrade the lock to an upgradeable reader lock when it is disposed.
        /// </summary>
        /// <param name="readerKey">The key required to upgrade the lock.</param>
        /// <param name="cancelationToken">The <see cref="CancelationToken"/> used to cancel the upgrade. If the token is canceled before the lock has been upgraded, a <see cref="CanceledException"/> will be thrown.</param>
        public WriterKey UpgradeToWriterLock(UpgradeableReaderKey readerKey, CancelationToken cancelationToken)
        {
            return _impl.UpgradeToWriterLock(readerKey, cancelationToken);
        }

        /// <summary>
        /// Synchronously try to upgrade the lock from an upgradeable reader lock to a writer lock.
        /// If successful, <paramref name="writerKey"/> is the key that will downgrade the lock to an upgradeable reader lock when it is disposed.
        /// This method does not wait, and returns immediately.
        /// </summary>
        /// <param name="readerKey">The key required to upgrade the lock.</param>
        /// <param name="writerKey">If successful, the key that will downgrade the lock to an upgradeable reader lock when it is disposed.</param>
        public bool TryUpgradeToWriterLock(UpgradeableReaderKey readerKey, out WriterKey writerKey)
        {
            return _impl.TryUpgradeToWriterLock(readerKey, out writerKey);
        }

        /// <summary>
        /// Asynchronously try to upgrade the lock from an upgradeable reader lock to a writer lock, while observing a <see cref="CancelationToken"/>.
        /// Returns a <see cref="Promise{T}"/> that will be resolved when the lock has been acquired, or the <paramref name="cancelationToken"/> has been canceled, with the success state and key.
        /// If successful, the key will release the lock when it is disposed.
        /// </summary>
        /// <param name="readerKey">The key required to upgrade the lock.</param>
        /// <param name="cancelationToken">
        /// The <see cref="CancelationToken"/> used to cancel the lock. If the token is canceled before the lock has been acquired, the success state of the returned <see cref="Promise{T}"/> will be <see langword="false"/>.
        /// </param>
        /// <remarks>
        /// This first tries to take the lock before checking the <paramref name="cancelationToken"/>>.
        /// If the lock is available, the result will be (<see langword="true"/>, key), even if the <paramref name="cancelationToken"/> is already canceled.
        /// </remarks>
        public Promise<(bool didEnter, WriterKey writerKey)> TryUpgradeToWriterLockAsync(UpgradeableReaderKey readerKey, CancelationToken cancelationToken)
        {
            return _impl.TryUpgradeToWriterLockAsync(readerKey, cancelationToken);
        }

        /// <summary>
        /// Synchronously try to upgrade the lock from an upgradeable reader lock to a writer lock, while observing a <see cref="CancelationToken"/>.
        /// If successful, <paramref name="writerKey"/> is the key that will release the lock when it is disposed.
        /// </summary>
        /// <param name="readerKey">The key required to upgrade the lock.</param>
        /// <param name="writerKey">If successful, the key that will release the lock when it is disposed.</param>
        /// <param name="cancelationToken">The <see cref="CancelationToken"/> used to cancel the lock. If the token is canceled before the lock has been acquired, this will return <see langword="false"/>.</param>
        /// <returns><see langword="true"/> if the reader lock was acquired before the <paramref name="cancelationToken"/> was canceled, <see langword="false"/> otherwise.</returns>
        /// <remarks>
        /// This first tries to take the lock before checking the <paramref name="cancelationToken"/>>.
        /// If the reader lock is available, this will return <see langword="true"/>, even if the <paramref name="cancelationToken"/> is already canceled.
        /// </remarks>
        public bool TryUpgradeToWriterLock(UpgradeableReaderKey readerKey, out WriterKey writerKey, CancelationToken cancelationToken)
        {
            return _impl.TryUpgradeToWriterLock(readerKey, out writerKey, cancelationToken);
        }

        /// <summary>
        /// A disposable object used to release the reader lock on the associated <see cref="AsyncReaderWriterLock"/>.
        /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        public readonly partial struct ReaderKey : IDisposable, IEquatable<ReaderKey>
        {
            /// <summary>
            /// Release the reader lock on the associated <see cref="AsyncReaderWriterLock"/>.
            /// </summary>
            public void Dispose() => _impl.ReleaseReaderLock();

            /// <summary>Returns a value indicating whether this value is equal to a specified <see cref="ReaderKey"/>.</summary>
            public bool Equals(ReaderKey other) => this == other;

            /// <summary>Returns a value indicating whether this value is equal to a specified <see cref="object"/>.</summary>
            public override bool Equals(object obj) => obj is ReaderKey token && Equals(token);

            /// <summary>Returns the hash code for this instance.</summary>
            public override int GetHashCode() => _impl.GetHashCode();

            /// <summary>Returns a value indicating whether two <see cref="ReaderKey"/> values are equal.</summary>
            public static bool operator ==(ReaderKey lhs, ReaderKey rhs) => lhs._impl == rhs._impl;

            /// <summary>Returns a value indicating whether two <see cref="ReaderKey"/> values are not equal.</summary>
            public static bool operator !=(ReaderKey lhs, ReaderKey rhs) => lhs._impl != rhs._impl;
        }

        /// <summary>
        /// A disposable object used to release the writer lock on the associated <see cref="AsyncReaderWriterLock"/>.
        /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        public readonly partial struct WriterKey : IDisposable, IEquatable<WriterKey>
        {
            /// <summary>
            /// Release the writer lock on the associated <see cref="AsyncReaderWriterLock"/>. If the lock was upgraded from an upgradeable reader lock, it will be downgraded back to an upgradeable reader lock.
            /// </summary>
            public void Dispose() => _impl.ReleaseWriterLock();

            /// <summary>Returns a value indicating whether this value is equal to a specified <see cref="WriterKey"/>.</summary>
            public bool Equals(WriterKey other) => this == other;

            /// <summary>Returns a value indicating whether this value is equal to a specified <see cref="object"/>.</summary>
            public override bool Equals(object obj) => obj is WriterKey token && Equals(token);

            /// <summary>Returns the hash code for this instance.</summary>
            public override int GetHashCode() => _impl.GetHashCode();

            /// <summary>Returns a value indicating whether two <see cref="WriterKey"/> values are equal.</summary>
            public static bool operator ==(WriterKey lhs, WriterKey rhs) => lhs._impl == rhs._impl;

            /// <summary>Returns a value indicating whether two <see cref="WriterKey"/> values are not equal.</summary>
            public static bool operator !=(WriterKey lhs, WriterKey rhs) => lhs._impl != rhs._impl;
        }

        /// <summary>
        /// A disposable object used to release the upgradeable reader lock on the associated <see cref="AsyncReaderWriterLock"/>.
        /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        public readonly partial struct UpgradeableReaderKey : IDisposable, IEquatable<UpgradeableReaderKey>
        {
            /// <summary>
            /// Release the upgradeable reader lock on the associated <see cref="AsyncReaderWriterLock"/>.
            /// </summary>
            public void Dispose() => _impl.ReleaseUpgradeableReaderLock();

            /// <summary>Returns a value indicating whether this value is equal to a specified <see cref="UpgradeableReaderKey"/>.</summary>
            public bool Equals(UpgradeableReaderKey other) => this == other;

            /// <summary>Returns a value indicating whether this value is equal to a specified <see cref="object"/>.</summary>
            public override bool Equals(object obj) => obj is UpgradeableReaderKey token && Equals(token);

            /// <summary>Returns the hash code for this instance.</summary>
            public override int GetHashCode() => _impl.GetHashCode();

            /// <summary>Returns a value indicating whether two <see cref="UpgradeableReaderKey"/> values are equal.</summary>
            public static bool operator ==(UpgradeableReaderKey lhs, UpgradeableReaderKey rhs) => lhs._impl == rhs._impl;

            /// <summary>Returns a value indicating whether two <see cref="UpgradeableReaderKey"/> values are not equal.</summary>
            public static bool operator !=(UpgradeableReaderKey lhs, UpgradeableReaderKey rhs) => lhs._impl != rhs._impl;
        }
    } // class AsyncReaderWriterLock

#endif // UNITY_2021_2_OR_NEWER || NETSTANDARD2_1_OR_GREATER || NETCOREAPP
} // namespace Proto.Promises.Threading