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
        /// Creates a new async-compatible reader/writer lock that does not support re-entrancy.
        /// </summary>
        public AsyncReaderWriterLock() { }

        /// <summary>
        /// Asynchronously acquire the lock as a reader. Returns a <see cref="Promise{T}"/> that will be resolved when the lock has been acquired.
        /// The result of the promise is the key that will release the lock when it is disposed.
        /// </summary>
        /// <param name="cancelationToken">The <see cref="CancelationToken"/> used to cancel the lock. If the token is canceled before the lock has been acquired, the returned <see cref="Promise{T}"/> will be canceled.</param>
        public Promise<ReaderKey> ReaderLockAsync(CancelationToken cancelationToken = default)
        {
            return _impl.ReaderLock(false, cancelationToken);
        }

        /// <summary>
        /// Synchronously acquire the lock as a reader. Returns the key that will release the lock when it is disposed.
        /// </summary>
        /// <param name="cancelationToken">The <see cref="CancelationToken"/> used to cancel the lock. If the token is canceled before the lock has been acquired, a <see cref="CanceledException"/> will be thrown.</param>
        public ReaderKey ReaderLock(CancelationToken cancelationToken = default)
        {
            return _impl.ReaderLock(true, cancelationToken).WaitForResult();
        }

        /// <summary>
        /// Synchronously try to acquire the lock as a reader. If successful, <paramref name="readerKey"/> is the key that will release the lock when it is disposed.
        /// </summary>
        /// <param name="readerKey">If successful, the key that will release the lock when it is disposed.</param>
        public bool TryEnterReaderLock(out ReaderKey readerKey)
        {
            return _impl.TryEnterReaderLock(out readerKey);
        }

        /// <summary>
        /// Asynchronously acquire the lock as a writer. Returns a <see cref="Promise{T}"/> that will be resolved when the lock has been acquired.
        /// The result of the promise is the key that will release the lock when it is disposed.
        /// </summary>
        /// <param name="cancelationToken">The <see cref="CancelationToken"/> used to cancel the lock. If the token is canceled before the lock has been acquired, the returned <see cref="Promise{T}"/> will be canceled.</param>
        public Promise<WriterKey> WriterLockAsync(CancelationToken cancelationToken = default)
        {
            return _impl.WriterLock(false, cancelationToken);
        }

        /// <summary>
        /// Synchronously acquire the lock as a writer. Returns the key that will release the lock when it is disposed.
        /// </summary>
        /// <param name="cancelationToken">The <see cref="CancelationToken"/> used to cancel the lock. If the token is canceled before the lock has been acquired, a <see cref="CanceledException"/> will be thrown.</param>
        public WriterKey WriterLock(CancelationToken cancelationToken = default)
        {
            return _impl.WriterLock(true, cancelationToken).WaitForResult();
        }

        /// <summary>
        /// Synchronously try to acquire the lock as a writer. If successful, <paramref name="writerKey"/> is the key that will release the lock when it is disposed.
        /// </summary>
        /// <param name="writerKey">If successful, the key that will release the lock when it is disposed.</param>
        public bool TryEnterWriterLock(out WriterKey writerKey)
        {
            return _impl.TryEnterWriterLock(out writerKey);
        }

        /// <summary>
        /// Asynchronously acquire the lock as an upgradeable reader. Returns a <see cref="Promise{T}"/> that will be resolved when the lock has been acquired.
        /// The result of the promise is the key that will release the lock when it is disposed.
        /// </summary>
        /// <param name="cancelationToken">The <see cref="CancelationToken"/> used to cancel the lock. If the token is canceled before the lock has been acquired, the returned <see cref="Promise{T}"/> will be canceled.</param>
        public Promise<UpgradeableReaderKey> UpgradeableReaderLockAsync(CancelationToken cancelationToken = default)
        {
            return _impl.UpgradeableReaderLock(false, cancelationToken);
        }

        /// <summary>
        /// Synchronously acquire the lock as an upgradeable reader. Returns the key that will release the lock when it is disposed.
        /// </summary>
        /// <param name="cancelationToken">The <see cref="CancelationToken"/> used to cancel the lock. If the token is canceled before the lock has been acquired, a <see cref="CanceledException"/> will be thrown.</param>
        public UpgradeableReaderKey UpgradeableReaderLock(CancelationToken cancelationToken = default)
        {
            return _impl.UpgradeableReaderLock(true, cancelationToken).WaitForResult();
        }

        /// <summary>
        /// Synchronously try to acquire the lock as an upgradeable reader. If successful, <paramref name="readerKey"/> is the key that will release the lock when it is disposed.
        /// </summary>
        /// <param name="readerKey">If successful, the key that will release the lock when it is disposed.</param>
        public bool TryEnterUpgradeableReaderLock(out UpgradeableReaderKey readerKey)
        {
            return _impl.TryEnterUpgradeableReaderLock(out readerKey);
        }

        /// <summary>
        /// Asynchronously upgrade the lock from an upgradeable reader lock to a writer lock. Returns a <see cref="Promise{T}"/> that will be resolved when the lock has been upgraded.
        /// The result of the promise is the key that will downgrade the lock to an upgradeable reader lock when it is disposed.
        /// </summary>
        /// <param name="readerKey">The key required to upgrade the lock.</param>
        /// <param name="cancelationToken">The <see cref="CancelationToken"/> used to cancel the upgrade. If the token is canceled before the lock has been upgraded, the returned <see cref="Promise{T}"/> will be canceled.</param>
        public Promise<WriterKey> UpgradeToWriterLockAsync(UpgradeableReaderKey readerKey, CancelationToken cancelationToken = default)
        {
            return _impl.UpgradeToWriterLock(readerKey, false, cancelationToken);
        }

        /// <summary>
        /// Synchronously upgrade the lock from an upgradeable reader lock to a writer lock. Returns the key that will downgrade the lock to an upgradeable reader lock when it is disposed.
        /// </summary>
        /// <param name="readerKey">The key required to upgrade the lock.</param>
        /// <param name="cancelationToken">The <see cref="CancelationToken"/> used to cancel the upgrade. If the token is canceled before the lock has been upgraded, a <see cref="CanceledException"/> will be thrown.</param>
        public WriterKey UpgradeToWriterLock(UpgradeableReaderKey readerKey, CancelationToken cancelationToken = default)
        {
            return _impl.UpgradeToWriterLock(readerKey, true, cancelationToken).WaitForResult();
        }

        /// <summary>
        /// Synchronously try to upgrade the lock from an upgradeable reader lock to a writer lock. If successful, <paramref name="writerKey"/> is the key that will downgrade the lock to an upgradeable reader lock when it is disposed.
        /// </summary>
        /// <param name="readerKey">The key required to upgrade the lock.</param>
        /// <param name="writerKey">If successful, the key that will downgrade the lock to an upgradeable reader lock when it is disposed.</param>
        public bool TryUpgradeToWriterLock(UpgradeableReaderKey readerKey, out WriterKey writerKey)
        {
            return _impl.TryUpgradeToWriterLock(readerKey, out writerKey);
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
            /// Release the lock on the associated <see cref="AsyncReaderWriterLock"/>.
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
            /// Release the lock on the associated <see cref="AsyncReaderWriterLock"/>.
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
            /// Release the lock on the associated <see cref="AsyncReaderWriterLock"/>.
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