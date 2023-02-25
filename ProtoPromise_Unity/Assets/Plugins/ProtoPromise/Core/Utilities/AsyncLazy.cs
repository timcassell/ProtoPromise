#if UNITY_5_5 || NET_2_0 || NET_2_0_SUBSET
#define NET_LEGACY
#endif

using System;
using System.Diagnostics;

namespace Proto.Promises
{
    /// <summary>
    /// Provides support for asynchronous lazy initialization.
    /// </summary>
    /// <typeparam name="T">The type of object that is being lazily initialized.</typeparam>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public sealed partial class AsyncLazy<T>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncLazy{T}"/> class that uses the specified initialization function.
        /// </summary>
        /// <param name="asyncValueFactory">The delegate that is invoked to produce the lazily initialized value when it is needed.</param>
        public AsyncLazy(Func<Promise<T>> asyncValueFactory)
        {
            _lazyPromise = new LazyPromise(this, asyncValueFactory);
        }

        /// <summary>
        /// Whether the asynchronous factory method has started. This is initially <c>false</c> and becomes <c>true</c> when this instance is awaited or after <see cref="Promise"/> is accessed.
        /// </summary>
        /// <remarks>This reverts to <c>false</c> if the factory does not complete successfully.</remarks>
        public bool IsStarted
        {
            get
            {
                var promise = _lazyPromise;
                return promise == null || promise._isStarted;
            }
        }

        /// <summary>
        /// Starts the asynchronous factory method, if it has not already started, and returns the resulting <see cref="Promise{T}"/>.
        /// </summary>
        public Promise<T> Promise
        {
            get
            {
                // This is a volatile read, so we don't need a full memory barrier to prevent the result read from moving before it.
                var promise = _lazyPromise;
                return promise == null
                    ? Promise<T>.Resolved(_result)
                    : promise.GetOrStartPromise();
            }
        }

        /// <summary>
        /// Asynchronous infrastructure support. This method permits instances of <see cref="AsyncLazy{T}"/> to be awaited.
        /// </summary>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public Async.CompilerServices.PromiseAwaiter<T> GetAwaiter()
        {
            return Promise.GetAwaiter();
        }
    } // class AsyncLazy<T>
}