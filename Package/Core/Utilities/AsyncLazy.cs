#if UNITY_5_5 || NET_2_0 || NET_2_0_SUBSET
#define NET_LEGACY
#endif

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Proto.Promises
{
    /// <summary>
    /// Provides support for asynchronous lazy initialization.
    /// </summary>
    /// <typeparam name="T">The type of object that is being lazily initialized.</typeparam>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public sealed partial class AsyncLazy<T> : IAsyncLazy<T>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncLazy{T}"/> class that uses the specified initialization function.
        /// </summary>
        /// <param name="asyncValueFactory">The delegate that is invoked to produce the lazily initialized value when it is needed.</param>
        public AsyncLazy(Func<Promise<T>> asyncValueFactory)
        {
            _lazyFields = new LazyFields(asyncValueFactory);
        }

        /// <summary>
        /// Whether the asynchronous factory method has started. This is initially <c>false</c> and becomes <c>true</c> when this instance is awaited or after <see cref="Promise"/> is accessed.
        /// </summary>
        /// <remarks>This reverts to <c>false</c> if the factory does not complete successfully.</remarks>
        public bool IsStarted
        {
            get
            {
                var lazyFields = _lazyFields;
                return lazyFields == null || lazyFields.IsStarted;
            }
        }

        /// <summary>
        /// Starts the asynchronous factory method, if it has not already started, and returns the resulting <see cref="Promise{T}"/>.
        /// </summary>
        public Promise<T> Promise
        {
            [MethodImpl(Internal.InlineOption)]
            get { return GetResultAsync(); }
        }

        /// <summary>
        /// Starts the asynchronous factory method, if it has not already started, and returns the resulting <see cref="Promise{T}"/>.
        /// </summary>
        public Promise<T> GetResultAsync()
        {
            // This is a volatile read, so we don't need a full memory barrier to prevent the result read from moving before it.
            var lazyFields = _lazyFields;
            return lazyFields == null
                ? Promise<T>.Resolved(_result)
                : lazyFields.GetOrStartPromise(this);
        }

        Promise<T> IAsyncLazy<T>.GetResultAsync(ProgressToken progressToken)
        {
            // This is a volatile read, so we don't need a full memory barrier to prevent the result read from moving before it.
            var lazyFields = _lazyFields;
            if (lazyFields == null)
            {
                progressToken.Report(1d);
                return Promise<T>.Resolved(_result);
            }
            return lazyFields.GetOrStartPromise(this);
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