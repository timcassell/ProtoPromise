#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

#pragma warning disable IDE0034 // Simplify 'default' expression

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
        /// Initializes a new instance of the <see cref="AsyncLazy{T}"/> class that uses the specified initialization function, without supporting progress reports.
        /// </summary>
        /// <param name="asyncValueFactory">The delegate that is invoked to produce the lazily initialized value when it is needed.</param>
        public AsyncLazy(Func<Promise<T>> asyncValueFactory)
        {
            _lazyFields = new LazyFieldsNoProgress(asyncValueFactory);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncLazy{T}"/> class that uses the specified initialization function, supporting progress reports.
        /// </summary>
        /// <param name="asyncValueFactory">The delegate that is invoked to produce the lazily initialized value when it is needed.</param>
        public AsyncLazy(Func<ProgressToken, Promise<T>> asyncValueFactory)
        {
            _lazyFields = new LazyFieldsWithProgress(asyncValueFactory);
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
        /// <param name="progressToken">The progress token that will be reported to if this instance was created with progress support.</param>
        public Promise<T> GetResultAsync(ProgressToken progressToken = default(ProgressToken))
        {
            // This is a volatile read, so we don't need a full memory barrier to prevent the result read from moving before it.
            var lazyFields = _lazyFields;
            if (lazyFields == null)
            {
                progressToken.Report(1d);
                return Promise<T>.Resolved(_result);
            }
            return lazyFields.GetOrStartPromise(this, progressToken);
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