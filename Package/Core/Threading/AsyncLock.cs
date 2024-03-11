#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Proto.Promises.Threading
{

#if UNITY_2021_2_OR_NEWER || NETSTANDARD2_1_OR_GREATER || NETCOREAPP
    // Old IL2CPP runtime crashes if this code exists, even if it is not used. So we only include them in newer build targets that old Unity versions cannot consume.
    // See https://github.com/timcassell/ProtoPromise/issues/169 for details.

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
    public sealed partial class AsyncLock
    {
        /// <summary>
        /// Creates a new async-compatible mutual exclusion lock that does not support re-entrancy.
        /// </summary>
        public AsyncLock() { }

        /// <summary>
        /// Asynchronously acquire the lock.
        /// Returns a <see cref="Promise{T}"/> that will be resolved when the lock has been acquired.
        /// The result of the promise is the key that will release the lock when it is disposed.
        /// </summary>
        [MethodImpl(Internal.InlineOption)]
        public Promise<Key> LockAsync() => LockAsyncImpl();

        /// <summary>
        /// Asynchronously acquire the lock, while observing a <see cref="CancelationToken"/>.
        /// Returns a <see cref="Promise{T}"/> that will be resolved when the lock has been acquired.
        /// The result of the promise is the key that will release the lock when it is disposed.
        /// </summary>
        /// <param name="cancelationToken">The <see cref="CancelationToken"/> used to cancel the lock. If the token is canceled before the lock has been acquired, the returned <see cref="Promise{T}"/> will be canceled.</param>
        [MethodImpl(Internal.InlineOption)]
        public Promise<Key> LockAsync(CancelationToken cancelationToken) => LockAsyncImpl(cancelationToken);

        /// <summary>
        /// Synchronously acquire the lock. Returns the key that will release the lock when it is disposed.
        /// </summary>
        [MethodImpl(Internal.InlineOption)]
        public Key Lock() => LockImpl();

        /// <summary>
        /// Synchronously acquire the lock, while observing a <see cref="CancelationToken"/>.
        /// Returns the key that will release the lock when it is disposed.
        /// </summary>
        /// <param name="cancelationToken">The <see cref="CancelationToken"/> used to cancel the lock. If the token is canceled before the lock has been acquired, a <see cref="CanceledException"/> will be thrown.</param>
        [MethodImpl(Internal.InlineOption)]
        public Key Lock(CancelationToken cancelationToken) => LockImpl(cancelationToken);

        /// <summary>
        /// A disposable object used to release the associated <see cref="AsyncLock"/>.
        /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        public readonly partial struct Key : IDisposable, IEquatable<Key>
        {
            /// <summary>
            /// Release the lock on the associated <see cref="AsyncLock"/>.
            /// </summary>
            public void Dispose() => Release();

            /// <summary>Returns a value indicating whether this value is equal to a specified <see cref="Key"/>.</summary>
            public bool Equals(Key other) => this == other;

            /// <summary>Returns a value indicating whether this value is equal to a specified <see cref="object"/>.</summary>
            public override bool Equals(object obj) => obj is Key token && Equals(token);

            /// <summary>Returns the hash code for this instance.</summary>
            public override int GetHashCode() => Internal.BuildHashCode(_owner, _key.GetHashCode(), 0);

            /// <summary>Returns a value indicating whether two <see cref="Key"/> values are equal.</summary>
            public static bool operator ==(Key lhs, Key rhs) => lhs._owner == rhs._owner & lhs._key == rhs._key;

            /// <summary>Returns a value indicating whether two <see cref="Key"/> values are not equal.</summary>
            public static bool operator !=(Key lhs, Key rhs) => !(lhs == rhs);
        }
    } // class AsyncLock

#endif // UNITY_2021_2_OR_NEWER || NETSTANDARD2_1_OR_GREATER || NETCOREAPP
} // namespace Proto.Promises.Threading