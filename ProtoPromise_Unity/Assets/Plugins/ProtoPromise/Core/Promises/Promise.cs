// define PROTO_PROMISE_DEBUG_ENABLE to enable debugging options in RELEASE mode. define PROTO_PROMISE_DEBUG_DISABLE to disable debugging options in DEBUG mode.
#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif
// define PROTO_PROMISE_PROGRESS_DISABLE to disable progress reports on promises.
// If Progress is enabled, promises use more memory, and it creates an upper bound to the depth of a promise chain (see Config for details).
#if !PROTO_PROMISE_PROGRESS_DISABLE
#define PROMISE_PROGRESS
#else
#undef PROMISE_PROGRESS
#endif

#pragma warning disable IDE0018 // Inline variable declaration
#pragma warning disable IDE0034 // Simplify 'default' expression

using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Proto.Promises
{
    /// <summary>
    /// A <see cref="Promise"/> represents the eventual result of an asynchronous operation.
    /// The primary way of interacting with a <see cref="Promise"/> is through its then method,
    /// which registers callbacks to be invoked when the <see cref="Promise"/> is resolved,
    /// or the reason why the <see cref="Promise"/> cannot be resolved.
    /// </summary>
    public
#if CSHARP_7_3_OR_NEWER
        readonly
#endif
        partial struct Promise : IEquatable<Promise>
    {
        public bool IsValid
        {
            [MethodImpl(Internal.InlineOption)]
            get
            {
                return _target.IsValid;
            }
        }

        public override string ToString()
        {
            var target = _target;
            string state =
                !target.IsValid ? "Invalid"
                : target._ref != null ? target._ref.State.ToString()
                : State.Resolved.ToString();
            return string.Format("Type: Promise, State: {0}", state);
        }

        /// <summary>
        /// Mark <see cref="this"/> as awaited and get a new <see cref="Promise"/> that inherits the state of <see cref="this"/> and can be awaited multiple times until <see cref="Forget"/> is called on it.
        /// <para/><see cref="Forget"/> must be called when you are finished with it.
        /// <para/>NOTE: You should not return a preserved <see cref="Promise"/> from a public API. Use <see cref="Duplicate"/> to get a <see cref="Promise"/> that is publicly safe.
        /// </summary>
        [MethodImpl(Internal.InlineOption)]
        public Promise Preserve()
        {
            return _target.Preserve();
        }

        /// <summary>
        /// Mark <see cref="this"/> as awaited and prevent any further awaits or callbacks on <see cref="this"/>.
        /// <para/>NOTE: It is imperative to terminate your promise chains with Forget so that any uncaught rejections will be reported and objects repooled (if pooling is enabled).
        /// </summary>
        [MethodImpl(Internal.InlineOption)]
        public void Forget()
        {
            _target.Forget();
        }


        /// <summary>
        /// Mark <see cref="this"/> as awaited and get a new <see cref="Promise"/> that inherits the state of <see cref="this"/> and can be awaited once.
        /// <para/>Preserved promises are unsafe to return from public APIs. Use <see cref="Duplicate"/> to get a <see cref="Promise"/> that is publicly safe.
        /// <para/><see cref="Duplicate"/> is safe to call even if you are unsure if <see cref="this"/> is preserved.
        /// </summary>
        [MethodImpl(Internal.InlineOption)]
        public Promise Duplicate()
        {
            return _target.Duplicate();
        }

        /// <summary>
        /// Mark <see cref="this"/> as awaited and schedule the next continuation to execute on the context of the provided option. Returns a new <see cref="Promise"/>.
        /// </summary>
        [MethodImpl(Internal.InlineOption)]
        public Promise WaitAsync(SynchronizationOption continuationOption)
        {
            return _target.WaitAsync(continuationOption);
        }

        /// <summary>
        /// Mark <see cref="this"/> as awaited and schedule the next continuation to execute on <paramref name="continuationContext"/>. Returns a new <see cref="Promise"/>.
        /// <para/>If <paramref name="continuationContext"/> is null, <see cref="ThreadPool.QueueUserWorkItem(WaitCallback, object)"/> will be used.
        /// </summary>
        [MethodImpl(Internal.InlineOption)]
        public Promise WaitAsync(SynchronizationContext continuationContext)
        {
            return _target.WaitAsync(continuationContext);
        }

        /// <summary>
        /// Add a progress listener. Returns a new <see cref="Promise"/>.
        /// <para/><paramref name="progressListener"/> will be reported with progress that is normalized between 0 and 1 on the context of the provided option.
        /// 
        /// <para/>If/when this is resolved, <paramref name="onProgress"/> will be invoked with <paramref name="progressCaptureValue"/> and 1.0, then the new <see cref="Promise"/> will be resolved when it returns.
        /// <para/>If/when this is rejected with any reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, progress will stop being reported.
        /// </summary>
#if !PROMISE_PROGRESS
        [Obsolete("Progress is disabled, progress will not be reported. Remove PROTO_PROMISE_PROGRESS_DISABLE from your compiler symbols to enable progress reports.", false)]
#endif
        [MethodImpl(Internal.InlineOption)]
        public Promise Progress<TProgress>(TProgress progressListener, SynchronizationOption invokeOption = SynchronizationOption.Foreground, CancelationToken cancelationToken = default(CancelationToken))
            where TProgress : IProgress<float>
        {
            return _target.Progress(progressListener, invokeOption, cancelationToken);
        }

        /// <summary>
        /// Add a progress listener. Returns a new <see cref="Promise"/>.
        /// <para/><paramref name="onProgress"/> will be invoked with <paramref name="progressCaptureValue"/> and progress that is normalized between 0 and 1 on the context of the provided option.
        /// 
        /// <para/>If/when this is resolved, <paramref name="onProgress"/> will be invoked with <paramref name="progressCaptureValue"/> and 1.0, then the new <see cref="Promise"/> will be resolved when it returns.
        /// <para/>If/when this is rejected with any reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, <paramref name="onProgress"/> will stop being invoked.
        /// </summary>
#if !PROMISE_PROGRESS
        [Obsolete("Progress is disabled, onProgress will not be invoked. Remove PROTO_PROMISE_PROGRESS_DISABLE from your compiler symbols to enable progress reports.", false)]
#endif
        [MethodImpl(Internal.InlineOption)]
        public Promise Progress(Action<float> onProgress, SynchronizationOption invokeOption = SynchronizationOption.Foreground, CancelationToken cancelationToken = default(CancelationToken))
        {
            return _target.Progress(onProgress, invokeOption, cancelationToken);
        }

        /// <summary>
        /// Add a progress listener. Returns a new <see cref="Promise"/>.
        /// <para/><paramref name="progressListener"/> will be reported with progress that is normalized between 0 and 1 on <paramref name="invokeContext"/>.
        /// <para/>If <paramref name="invokeContext"/> is null, <see cref="ThreadPool.QueueUserWorkItem(WaitCallback, object)"/> will be used.
        /// 
        /// <para/>If/when this is resolved, <paramref name="onProgress"/> will be invoked with <paramref name="progressCaptureValue"/> and 1.0, then the new <see cref="Promise"/> will be resolved when it returns.
        /// <para/>If/when this is rejected with any reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, progress will stop being reported.
        /// </summary>
#if !PROMISE_PROGRESS
        [Obsolete("Progress is disabled, progress will not be reported. Remove PROTO_PROMISE_PROGRESS_DISABLE from your compiler symbols to enable progress reports.", false)]
#endif
        [MethodImpl(Internal.InlineOption)]
        public Promise Progress<TProgress>(TProgress progressListener, SynchronizationContext invokeContext, CancelationToken cancelationToken = default(CancelationToken))
            where TProgress : IProgress<float>
        {
            return _target.Progress(progressListener, invokeContext, cancelationToken);
        }

        /// <summary>
        /// Add a progress listener. Returns a new <see cref="Promise"/>.
        /// <para/><paramref name="onProgress"/> will be invoked with <paramref name="progressCaptureValue"/> and progress that is normalized between 0 and 1 on <paramref name="invokeContext"/>.
        /// <para/>If <paramref name="invokeContext"/> is null, <see cref="ThreadPool.QueueUserWorkItem(WaitCallback, object)"/> will be used.
        /// 
        /// <para/>If/when this is resolved, <paramref name="onProgress"/> will be invoked with <paramref name="progressCaptureValue"/> and 1.0, then the new <see cref="Promise"/> will be resolved when it returns.
        /// <para/>If/when this is rejected with any reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, <paramref name="onProgress"/> will stop being invoked.
        /// </summary>
#if !PROMISE_PROGRESS
        [Obsolete("Progress is disabled, onProgress will not be invoked. Remove PROTO_PROMISE_PROGRESS_DISABLE from your compiler symbols to enable progress reports.", false)]
#endif
        [MethodImpl(Internal.InlineOption)]
        public Promise Progress(Action<float> onProgress, SynchronizationContext invokeContext, CancelationToken cancelationToken = default(CancelationToken))
        {
            return _target.Progress(onProgress, invokeContext, cancelationToken);
        }

        /// <summary>
        /// Add a cancel callback. Returns a new <see cref="Promise"/> that inherits the state of <see cref="this"/> and can be awaited once.
        /// <para/>If/when this instance is canceled, <paramref name="onCanceled"/> will be invoked with the cancelation reason.
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, <paramref name="onCanceled"/> will not be invoked.
        /// </summary>
        public Promise CatchCancelation(CanceledAction onCanceled, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(1);
            ValidateArgument(onCanceled, "onCanceled", 1);

            return Internal.PromiseRef.CallbackHelper.AddCancel(_target, new Internal.PromiseRef.DelegateCancel(onCanceled), cancelationToken);
        }

        /// <summary>
        /// Add a finally callback. Returns a new <see cref="Promise"/>.
        /// <para/>When this is resolved, rejected, or canceled, <paramref name="onFinally"/> will be invoked.
        /// <para/>If <paramref name="onFinally"/> throws an exception, the new <see cref="Promise"/> will be rejected with that exception,
        /// otherwise it will be resolved, rejected, or canceled with the same value or reason as this.
        /// </summary>
        [MethodImpl(Internal.InlineOption)]
        public Promise Finally(Action onFinally)
        {
            return _target.Finally(onFinally);
        }

        #region Resolve Callbacks
        /// <summary>
        /// Add a resolve callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is rejected with any reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onResolved"/> will not be invoked.
        /// </summary>
        public Promise Then(Action onResolved, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, "onResolved", 1);

            return Internal.PromiseRef.CallbackHelper.AddResolve(_target, Internal.PromiseRef.DelegateWrapper.Create(onResolved), cancelationToken);
        }

        /// <summary>
        /// Add a resolve callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="TResult"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is rejected with any reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onResolved"/> will not be invoked.
        /// </summary>
        public Promise<TResult> Then<TResult>(Func<TResult> onResolved, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, "onResolved", 1);

            return Internal.PromiseRef.CallbackHelper.AddResolve(_target, Internal.PromiseRef.DelegateWrapper.Create(onResolved), cancelationToken);
        }

        /// <summary>
        /// Add a resolve callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is rejected with any reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onResolved"/> will not be invoked.
        /// </summary>
        public Promise Then(Func<Promise> onResolved, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, "onResolved", 1);

            return Internal.PromiseRef.CallbackHelper.AddResolveWait(_target, Internal.PromiseRef.DelegateWrapper.Create(onResolved), cancelationToken);
        }

        /// <summary>
        /// Add a resolve callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="TResult"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is rejected with any reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onResolved"/> will not be invoked.
        /// </summary>
        public Promise<TResult> Then<TResult>(Func<Promise<TResult>> onResolved, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, "onResolved", 1);

            return Internal.PromiseRef.CallbackHelper.AddResolveWait(_target, Internal.PromiseRef.DelegateWrapper.Create(onResolved), cancelationToken);
        }
        #endregion

        #region Reject Callbacks
        /// <summary>
        /// Add a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, the new <see cref="Promise"/> will be resolved.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise Catch(Action onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(1);
            ValidateArgument(onRejected, "onRejected", 1);

            return Internal.PromiseRef.CallbackHelper
                .AddResolveReject(_target, Internal.PromiseRef.DelegateWrapper.CreatePassthrough(), Internal.PromiseRef.DelegateWrapper.Create(onRejected), cancelationToken);
        }

        /// <summary>
        /// Add a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, the new <see cref="Promise"/> will be resolved.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise Catch<TReject>(Action<TReject> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(1);
            ValidateArgument(onRejected, "onRejected", 1);

            return Internal.PromiseRef.CallbackHelper
                .AddResolveReject(_target, Internal.PromiseRef.DelegateWrapper.CreatePassthrough(), Internal.PromiseRef.DelegateWrapper.Create(onRejected), cancelationToken);
        }

        /// <summary>
        /// Add a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, the new <see cref="Promise"/> will be resolved.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise Catch(Func<Promise> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(1);
            ValidateArgument(onRejected, "onRejected", 1);

            return Internal.PromiseRef.CallbackHelper
                .AddResolveRejectWait(_target, Internal.PromiseRef.DelegateWrapper.CreatePassthrough(), Internal.PromiseRef.DelegateWrapper.Create(onRejected), cancelationToken);
        }

        /// <summary>
        /// Add a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, the new <see cref="Promise"/> will be resolved.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise Catch<TReject>(Func<TReject, Promise> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(1);
            ValidateArgument(onRejected, "onRejected", 1);

            return Internal.PromiseRef.CallbackHelper
                .AddResolveRejectWait(_target, Internal.PromiseRef.DelegateWrapper.CreatePassthrough(), Internal.PromiseRef.DelegateWrapper.Create(onRejected), cancelationToken);
        }
        #endregion

        #region Resolve or Reject Callbacks
        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise Then(Action onResolved, Action onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            return Internal.PromiseRef.CallbackHelper
                .AddResolveReject(_target, Internal.PromiseRef.DelegateWrapper.Create(onResolved), Internal.PromiseRef.DelegateWrapper.Create(onRejected), cancelationToken);
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise Then<TReject>(Action onResolved, Action<TReject> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            return Internal.PromiseRef.CallbackHelper
                .AddResolveReject(_target, Internal.PromiseRef.DelegateWrapper.Create(onResolved), Internal.PromiseRef.DelegateWrapper.Create(onRejected), cancelationToken);
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="TResult"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise<TResult> Then<TResult>(Func<TResult> onResolved, Func<TResult> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            return Internal.PromiseRef.CallbackHelper
                .AddResolveReject(_target, Internal.PromiseRef.DelegateWrapper.Create(onResolved), Internal.PromiseRef.DelegateWrapper.Create(onRejected), cancelationToken);
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="TResult"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise<TResult> Then<TResult, TReject>(Func<TResult> onResolved, Func<TReject, TResult> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            return Internal.PromiseRef.CallbackHelper
                .AddResolveReject(_target, Internal.PromiseRef.DelegateWrapper.Create(onResolved), Internal.PromiseRef.DelegateWrapper.Create(onRejected), cancelationToken);
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise Then(Func<Promise> onResolved, Func<Promise> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            return Internal.PromiseRef.CallbackHelper
                .AddResolveRejectWait(_target, Internal.PromiseRef.DelegateWrapper.Create(onResolved), Internal.PromiseRef.DelegateWrapper.Create(onRejected), cancelationToken);
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise Then<TReject>(Func<Promise> onResolved, Func<TReject, Promise> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            return Internal.PromiseRef.CallbackHelper
                .AddResolveRejectWait(_target, Internal.PromiseRef.DelegateWrapper.Create(onResolved), Internal.PromiseRef.DelegateWrapper.Create(onRejected), cancelationToken);
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="TResult"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise<TResult> Then<TResult>(Func<Promise<TResult>> onResolved, Func<Promise<TResult>> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            return Internal.PromiseRef.CallbackHelper
                .AddResolveRejectWait(_target, Internal.PromiseRef.DelegateWrapper.Create(onResolved), Internal.PromiseRef.DelegateWrapper.Create(onRejected), cancelationToken);
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="TResult"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise<TResult> Then<TResult, TReject>(Func<Promise<TResult>> onResolved, Func<TReject, Promise<TResult>> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            return Internal.PromiseRef.CallbackHelper
                .AddResolveRejectWait(_target, Internal.PromiseRef.DelegateWrapper.Create(onResolved), Internal.PromiseRef.DelegateWrapper.Create(onRejected), cancelationToken);
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise Then(Action onResolved, Func<Promise> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            return Internal.PromiseRef.CallbackHelper
                .AddResolveRejectWait(_target, Internal.PromiseRef.DelegateWrapper.Create(onResolved), Internal.PromiseRef.DelegateWrapper.Create(onRejected), cancelationToken);
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise Then<TReject>(Action onResolved, Func<TReject, Promise> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            return Internal.PromiseRef.CallbackHelper
                .AddResolveRejectWait(_target, Internal.PromiseRef.DelegateWrapper.Create(onResolved), Internal.PromiseRef.DelegateWrapper.Create(onRejected), cancelationToken);
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="TResult"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise<TResult> Then<TResult>(Func<TResult> onResolved, Func<Promise<TResult>> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            return Internal.PromiseRef.CallbackHelper
                .AddResolveRejectWait(_target, Internal.PromiseRef.DelegateWrapper.Create(onResolved), Internal.PromiseRef.DelegateWrapper.Create(onRejected), cancelationToken);
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="TResult"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise<TResult> Then<TResult, TReject>(Func<TResult> onResolved, Func<TReject, Promise<TResult>> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            return Internal.PromiseRef.CallbackHelper
                .AddResolveRejectWait(_target, Internal.PromiseRef.DelegateWrapper.Create(onResolved), Internal.PromiseRef.DelegateWrapper.Create(onRejected), cancelationToken);
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise Then(Func<Promise> onResolved, Action onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            return Internal.PromiseRef.CallbackHelper
                .AddResolveRejectWait(_target, Internal.PromiseRef.DelegateWrapper.Create(onResolved), Internal.PromiseRef.DelegateWrapper.Create(onRejected), cancelationToken);
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise Then<TReject>(Func<Promise> onResolved, Action<TReject> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            return Internal.PromiseRef.CallbackHelper
                .AddResolveRejectWait(_target, Internal.PromiseRef.DelegateWrapper.Create(onResolved), Internal.PromiseRef.DelegateWrapper.Create(onRejected), cancelationToken);
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="TResult"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise<TResult> Then<TResult>(Func<Promise<TResult>> onResolved, Func<TResult> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            return Internal.PromiseRef.CallbackHelper
                .AddResolveRejectWait(_target, Internal.PromiseRef.DelegateWrapper.Create(onResolved), Internal.PromiseRef.DelegateWrapper.Create(onRejected), cancelationToken);
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="TResult"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise<TResult> Then<TResult, TReject>(Func<Promise<TResult>> onResolved, Func<TReject, TResult> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            return Internal.PromiseRef.CallbackHelper
                .AddResolveRejectWait(_target, Internal.PromiseRef.DelegateWrapper.Create(onResolved), Internal.PromiseRef.DelegateWrapper.Create(onRejected), cancelationToken);
        }
        #endregion

        #region Continue Callbacks
        /// <summary>
        /// Add a continuation callback. Returns a new <see cref="Promise"/>.
        /// <para/>When this is resolved, rejected, or canceled, <paramref name="onContinue"/> will be invoked with the <see cref="ResultContainer"/>, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onContinue"/> will not be invoked.
        /// </summary>
        public Promise ContinueWith(ContinueAction onContinue, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(1);
            ValidateArgument(onContinue, "onContinue", 1);

            return Internal.PromiseRef.CallbackHelper.AddContinue(_target, Internal.PromiseRef.DelegateWrapper.Create(onContinue), cancelationToken);
        }

        /// <summary>
        /// Add a continuation callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="TResult"/>.
        /// <para/>When this is resolved, rejected, or canceled, <paramref name="onContinue"/> will be invoked with the <see cref="ResultContainer"/>, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onContinue"/> will not be invoked.
        /// </summary>
        public Promise<TResult> ContinueWith<TResult>(ContinueFunc<TResult> onContinue, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(1);
            ValidateArgument(onContinue, "onContinue", 1);

            return Internal.PromiseRef.CallbackHelper.AddContinue(_target, Internal.PromiseRef.DelegateWrapper.Create(onContinue), cancelationToken);
        }

        /// <summary>
        /// Add a continuation callback. Returns a new <see cref="Promise"/>.
        /// <para/>When this is resolved, rejected, or canceled, <paramref name="onContinue"/> will be invoked with the <see cref="ResultContainer"/>, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onContinue"/> will not be invoked.
        /// </summary>
        public Promise ContinueWith(ContinueFunc<Promise> onContinue, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(1);
            ValidateArgument(onContinue, "onContinue", 1);

            return Internal.PromiseRef.CallbackHelper.AddContinueWait(_target, Internal.PromiseRef.DelegateWrapper.Create(onContinue), cancelationToken);
        }

        /// <summary>
        /// Add a continuation callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="TResult"/>.
        /// <para/>When this is resolved, rejected, or canceled, <paramref name="onContinue"/> will be invoked with the <see cref="ResultContainer"/>, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onContinue"/> will not be invoked.
        /// </summary>
        public Promise<TResult> ContinueWith<TResult>(ContinueFunc<Promise<TResult>> onContinue, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(1);
            ValidateArgument(onContinue, "onContinue", 1);

            return Internal.PromiseRef.CallbackHelper.AddContinueWait(_target, Internal.PromiseRef.DelegateWrapper.Create(onContinue), cancelationToken);
        }
        #endregion

        // Capture values below.

        /// <summary>
        /// Capture a value and add a progress listener. Returns a new <see cref="Promise"/>.
        /// <para/><paramref name="onProgress"/> will be invoked with <paramref name="progressCaptureValue"/> and progress that is normalized between 0 and 1 on the context of the provided option.
        /// 
        /// <para/>If/when this is resolved, <paramref name="onProgress"/> will be invoked with <paramref name="progressCaptureValue"/> and 1.0, then the new <see cref="Promise"/> will be resolved when it returns.
        /// <para/>If/when this is rejected with any reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, <paramref name="onProgress"/> will stop being invoked.
        /// </summary>
#if !PROMISE_PROGRESS
        [Obsolete("Progress is disabled, onProgress will not be invoked. Remove PROTO_PROMISE_PROGRESS_DISABLE from your compiler symbols to enable progress reports.", false)]
#endif
        [MethodImpl(Internal.InlineOption)]
        public Promise Progress<TCaptureProgress>(TCaptureProgress progressCaptureValue, Action<TCaptureProgress, float> onProgress, SynchronizationOption invokeOption = SynchronizationOption.Foreground, CancelationToken cancelationToken = default(CancelationToken))
        {
            return _target.Progress(progressCaptureValue, onProgress, invokeOption, cancelationToken);
        }

        /// <summary>
        /// Capture a value and add a progress listener. Returns a new <see cref="Promise"/>.
        /// <para/><paramref name="onProgress"/> will be invoked with <paramref name="progressCaptureValue"/> and progress that is normalized between 0 and 1 on <paramref name="invokeContext"/>.
        /// <para/>If <paramref name="invokeContext"/> is null, <see cref="ThreadPool.QueueUserWorkItem(WaitCallback, object)"/> will be used.
        /// 
        /// <para/>If/when this is resolved, <paramref name="onProgress"/> will be invoked with <paramref name="progressCaptureValue"/> and 1.0, then the new <see cref="Promise"/> will be resolved when it returns.
        /// <para/>If/when this is rejected with any reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, <paramref name="onProgress"/> will stop being invoked.
        /// </summary>
#if !PROMISE_PROGRESS
        [Obsolete("Progress is disabled, onProgress will not be invoked. Remove PROTO_PROMISE_PROGRESS_DISABLE from your compiler symbols to enable progress reports.", false)]
#endif
        [MethodImpl(Internal.InlineOption)]
        public Promise Progress<TCaptureProgress>(TCaptureProgress progressCaptureValue, Action<TCaptureProgress, float> onProgress, SynchronizationContext invokeContext, CancelationToken cancelationToken = default(CancelationToken))
        {
            return _target.Progress(progressCaptureValue, onProgress, invokeContext, cancelationToken);
        }

        /// <summary>
        /// Capture a value and add a cancel callback. Returns a new <see cref="Promise"/> that inherits the state of <see cref="this"/> and can be awaited once.
        /// <para/>If/when this instance is canceled, <paramref name="onCanceled"/> will be invoked with <paramref name="cancelCaptureValue"/> and the cancelation reason.
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, <paramref name="onCanceled"/> will not be invoked.
        /// </summary>
        public Promise CatchCancelation<TCaptureCancel>(TCaptureCancel cancelCaptureValue, CanceledAction<TCaptureCancel> onCanceled, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(1);
            ValidateArgument(onCanceled, "onCanceled", 1);

            return Internal.PromiseRef.CallbackHelper.AddCancel(_target, new Internal.PromiseRef.DelegateCaptureCancel<TCaptureCancel>(cancelCaptureValue, onCanceled), cancelationToken);
        }

        /// <summary>
        /// Capture a value and add a finally callback. Returns a new <see cref="Promise"/>.
        /// <para/>When this is resolved, rejected, or canceled, <paramref name="onFinally"/> will be invoked with <paramref name="finallyCaptureValue"/>.
        /// <para/>If <paramref name="onFinally"/> throws an exception, the new <see cref="Promise"/> will be rejected with that exception,
        /// otherwise it will be resolved, rejected, or canceled with the same value or reason as this.
        /// </summary>
        [MethodImpl(Internal.InlineOption)]
        public Promise Finally<TCaptureFinally>(TCaptureFinally finallyCaptureValue, Action<TCaptureFinally> onFinally)
        {
            return _target.Finally(finallyCaptureValue, onFinally);
        }

        #region Resolve Callbacks
        /// <summary>
        /// Capture a value and add a resolve callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onResolved"/> will not be invoked.
        /// </summary>
        public Promise Then<TCaptureResolve>(TCaptureResolve resolveCaptureValue, Action<TCaptureResolve> onResolved, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, "onResolved", 1);

            return Internal.PromiseRef.CallbackHelper.AddResolve(_target, Internal.PromiseRef.DelegateWrapper.Create(resolveCaptureValue, onResolved), cancelationToken);
        }

        /// <summary>
        /// Capture a value and add a resolve callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="TResult"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onResolved"/> will not be invoked.
        /// </summary>
        public Promise<TResult> Then<TCaptureResolve, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, TResult> onResolved, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, "onResolved", 1);

            return Internal.PromiseRef.CallbackHelper.AddResolve(_target, Internal.PromiseRef.DelegateWrapper.Create(resolveCaptureValue, onResolved), cancelationToken);
        }

        /// <summary>
        /// Capture a value and add a resolve callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onResolved"/> will not be invoked.
        /// </summary>
        public Promise Then<TCaptureResolve>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise> onResolved, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, "onResolved", 1);

            return Internal.PromiseRef.CallbackHelper.AddResolveWait(_target, Internal.PromiseRef.DelegateWrapper.Create(resolveCaptureValue, onResolved), cancelationToken);
        }

        /// <summary>
        /// Capture a value and add a resolve callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="TResult"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onResolved"/> will not be invoked.
        /// </summary>
        public Promise<TResult> Then<TCaptureResolve, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise<TResult>> onResolved, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, "onResolved", 1);

            return Internal.PromiseRef.CallbackHelper.AddResolveWait(_target, Internal.PromiseRef.DelegateWrapper.Create(resolveCaptureValue, onResolved), cancelationToken);
        }
        #endregion

        #region Reject Callbacks
        /// <summary>
        /// Capture a value and add a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, the new <see cref="Promise"/> will be resolved.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise Catch<TCaptureReject>(TCaptureReject rejectCaptureValue, Action<TCaptureReject> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(1);
            ValidateArgument(onRejected, "onRejected", 1);

            return Internal.PromiseRef.CallbackHelper.AddResolveReject(_target,
                Internal.PromiseRef.DelegateWrapper.CreatePassthrough(),
                Internal.PromiseRef.DelegateWrapper.Create(rejectCaptureValue, onRejected),
                cancelationToken);
        }

        /// <summary>
        /// Capture a value and add a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, the new <see cref="Promise"/> will be resolved.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise Catch<TCaptureReject, TReject>(TCaptureReject rejectCaptureValue, Action<TCaptureReject, TReject> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(1);
            ValidateArgument(onRejected, "onRejected", 1);

            return Internal.PromiseRef.CallbackHelper.AddResolveReject(_target,
                Internal.PromiseRef.DelegateWrapper.CreatePassthrough(),
                Internal.PromiseRef.DelegateWrapper.Create(rejectCaptureValue, onRejected),
                cancelationToken);
        }

        /// <summary>
        /// Capture a value and add a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, the new <see cref="Promise"/> will be resolved.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise Catch<TCaptureReject>(TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(1);
            ValidateArgument(onRejected, "onRejected", 1);

            return Internal.PromiseRef.CallbackHelper.AddResolveRejectWait(_target,
                Internal.PromiseRef.DelegateWrapper.CreatePassthrough(),
                Internal.PromiseRef.DelegateWrapper.Create(rejectCaptureValue, onRejected),
                cancelationToken);
        }

        /// <summary>
        /// Capture a value and add a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, the new <see cref="Promise"/> will be resolved.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise Catch<TCaptureReject, TReject>(TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(1);
            ValidateArgument(onRejected, "onRejected", 1);

            return Internal.PromiseRef.CallbackHelper.AddResolveRejectWait(_target,
                Internal.PromiseRef.DelegateWrapper.CreatePassthrough(),
                Internal.PromiseRef.DelegateWrapper.Create(rejectCaptureValue, onRejected),
                cancelationToken);
        }
        #endregion

        #region Resolve or Reject Callbacks
        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise Then<TCaptureResolve>(TCaptureResolve resolveCaptureValue, Action<TCaptureResolve> onResolved, Action onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            return Internal.PromiseRef.CallbackHelper.AddResolveReject(_target,
                Internal.PromiseRef.DelegateWrapper.Create(resolveCaptureValue, onResolved),
                Internal.PromiseRef.DelegateWrapper.Create(onRejected),
                cancelationToken);
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise Then<TCaptureReject>(Action onResolved, TCaptureReject rejectCaptureValue, Action<TCaptureReject> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            return Internal.PromiseRef.CallbackHelper.AddResolveReject(_target,
                Internal.PromiseRef.DelegateWrapper.Create(onResolved),
                Internal.PromiseRef.DelegateWrapper.Create(rejectCaptureValue, onRejected),
                cancelationToken);
        }

        /// <summary>
        /// Capture 2 values and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise Then<TCaptureResolve, TCaptureReject>(TCaptureResolve resolveCaptureValue, Action<TCaptureResolve> onResolved, TCaptureReject rejectCaptureValue, Action<TCaptureReject> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            return Internal.PromiseRef.CallbackHelper.AddResolveReject(_target,
                Internal.PromiseRef.DelegateWrapper.Create(resolveCaptureValue, onResolved),
                Internal.PromiseRef.DelegateWrapper.Create(rejectCaptureValue, onRejected),
                cancelationToken);
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise Then<TCaptureResolve, TReject>(TCaptureResolve resolveCaptureValue, Action<TCaptureResolve> onResolved, Action<TReject> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            return Internal.PromiseRef.CallbackHelper.AddResolveReject(_target,
                Internal.PromiseRef.DelegateWrapper.Create(resolveCaptureValue, onResolved),
                Internal.PromiseRef.DelegateWrapper.Create(onRejected),
                cancelationToken);
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise Then<TCaptureReject, TReject>(Action onResolved, TCaptureReject rejectCaptureValue, Action<TCaptureReject, TReject> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            return Internal.PromiseRef.CallbackHelper.AddResolveReject(_target,
                Internal.PromiseRef.DelegateWrapper.Create(onResolved),
                Internal.PromiseRef.DelegateWrapper.Create(rejectCaptureValue, onRejected),
                cancelationToken);
        }

        /// <summary>
        /// Capture 2 values and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise Then<TCaptureResolve, TCaptureReject, TReject>(TCaptureResolve resolveCaptureValue, Action<TCaptureResolve> onResolved, TCaptureReject rejectCaptureValue, Action<TCaptureReject, TReject> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            return Internal.PromiseRef.CallbackHelper.AddResolveReject(_target,
                Internal.PromiseRef.DelegateWrapper.Create(resolveCaptureValue, onResolved),
                Internal.PromiseRef.DelegateWrapper.Create(rejectCaptureValue, onRejected),
                cancelationToken);
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="TResult"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise<TResult> Then<TCaptureResolve, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, TResult> onResolved, Func<TResult> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            return Internal.PromiseRef.CallbackHelper.AddResolveReject(_target,
                Internal.PromiseRef.DelegateWrapper.Create(resolveCaptureValue, onResolved),
                Internal.PromiseRef.DelegateWrapper.Create(onRejected),
                cancelationToken);
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="TResult"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise<TResult> Then<TCaptureReject, TResult>(Func<TResult> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TResult> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            return Internal.PromiseRef.CallbackHelper.AddResolveReject(_target,
                Internal.PromiseRef.DelegateWrapper.Create(onResolved),
                Internal.PromiseRef.DelegateWrapper.Create(rejectCaptureValue, onRejected),
                cancelationToken);
        }

        /// <summary>
        /// Capture 2 values and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="TResult"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise<TResult> Then<TCaptureResolve, TCaptureReject, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, TResult> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TResult> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            return Internal.PromiseRef.CallbackHelper.AddResolveReject(_target,
                Internal.PromiseRef.DelegateWrapper.Create(resolveCaptureValue, onResolved),
                Internal.PromiseRef.DelegateWrapper.Create(rejectCaptureValue, onRejected),
                cancelationToken);
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="TResult"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise<TResult> Then<TCaptureResolve, TResult, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, TResult> onResolved, Func<TReject, TResult> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            return Internal.PromiseRef.CallbackHelper.AddResolveReject(_target,
                Internal.PromiseRef.DelegateWrapper.Create(resolveCaptureValue, onResolved),
                Internal.PromiseRef.DelegateWrapper.Create(onRejected),
                cancelationToken);
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="TResult"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise<TResult> Then<TCaptureReject, TResult, TReject>(Func<TResult> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, TResult> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            return Internal.PromiseRef.CallbackHelper.AddResolveReject(_target,
                Internal.PromiseRef.DelegateWrapper.Create(onResolved),
                Internal.PromiseRef.DelegateWrapper.Create(rejectCaptureValue, onRejected),
                cancelationToken);
        }

        /// <summary>
        /// Capture 2 values and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="TResult"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise<TResult> Then<TCaptureResolve, TCaptureReject, TResult, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, TResult> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, TResult> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            return Internal.PromiseRef.CallbackHelper.AddResolveReject(_target,
                Internal.PromiseRef.DelegateWrapper.Create(resolveCaptureValue, onResolved),
                Internal.PromiseRef.DelegateWrapper.Create(rejectCaptureValue, onRejected),
                cancelationToken);
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise Then<TCaptureResolve>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise> onResolved, Func<Promise> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            return Internal.PromiseRef.CallbackHelper.AddResolveRejectWait(_target,
                Internal.PromiseRef.DelegateWrapper.Create(resolveCaptureValue, onResolved),
                Internal.PromiseRef.DelegateWrapper.Create(onRejected),
                cancelationToken);
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise Then<TCaptureReject>(Func<Promise> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            return Internal.PromiseRef.CallbackHelper.AddResolveRejectWait(_target,
                Internal.PromiseRef.DelegateWrapper.Create(onResolved),
                Internal.PromiseRef.DelegateWrapper.Create(rejectCaptureValue, onRejected),
                cancelationToken);
        }

        /// <summary>
        /// Capture 2 values and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise Then<TCaptureResolve, TCaptureReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            return Internal.PromiseRef.CallbackHelper.AddResolveRejectWait(_target,
                Internal.PromiseRef.DelegateWrapper.Create(resolveCaptureValue, onResolved),
                Internal.PromiseRef.DelegateWrapper.Create(rejectCaptureValue, onRejected),
                cancelationToken);
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise Then<TCaptureResolve, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise> onResolved, Func<TReject, Promise> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            return Internal.PromiseRef.CallbackHelper.AddResolveRejectWait(_target,
                Internal.PromiseRef.DelegateWrapper.Create(resolveCaptureValue, onResolved),
                Internal.PromiseRef.DelegateWrapper.Create(onRejected),
                cancelationToken);
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise Then<TCaptureReject, TReject>(Func<Promise> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            return Internal.PromiseRef.CallbackHelper.AddResolveRejectWait(_target,
                Internal.PromiseRef.DelegateWrapper.Create(onResolved),
                Internal.PromiseRef.DelegateWrapper.Create(rejectCaptureValue, onRejected),
                cancelationToken);
        }

        /// <summary>
        /// Capture 2 values and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise Then<TCaptureResolve, TCaptureReject, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            return Internal.PromiseRef.CallbackHelper.AddResolveRejectWait(_target,
                Internal.PromiseRef.DelegateWrapper.Create(resolveCaptureValue, onResolved),
                Internal.PromiseRef.DelegateWrapper.Create(rejectCaptureValue, onRejected),
                cancelationToken);
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="TResult"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise<TResult> Then<TCaptureResolve, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise<TResult>> onResolved, Func<Promise<TResult>> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            return Internal.PromiseRef.CallbackHelper.AddResolveRejectWait(_target,
                Internal.PromiseRef.DelegateWrapper.Create(resolveCaptureValue, onResolved),
                Internal.PromiseRef.DelegateWrapper.Create(onRejected),
                cancelationToken);
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="TResult"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise<TResult> Then<TCaptureReject, TResult>(Func<Promise<TResult>> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise<TResult>> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            return Internal.PromiseRef.CallbackHelper.AddResolveRejectWait(_target,
                Internal.PromiseRef.DelegateWrapper.Create(onResolved),
                Internal.PromiseRef.DelegateWrapper.Create(rejectCaptureValue, onRejected),
                cancelationToken);
        }

        /// <summary>
        /// Capture 2 values and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="TResult"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise<TResult> Then<TCaptureResolve, TCaptureReject, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise<TResult>> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise<TResult>> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            return Internal.PromiseRef.CallbackHelper.AddResolveRejectWait(_target,
                Internal.PromiseRef.DelegateWrapper.Create(resolveCaptureValue, onResolved),
                Internal.PromiseRef.DelegateWrapper.Create(rejectCaptureValue, onRejected),
                cancelationToken);
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="TResult"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise<TResult> Then<TCaptureResolve, TResult, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise<TResult>> onResolved, Func<TReject, Promise<TResult>> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            return Internal.PromiseRef.CallbackHelper.AddResolveRejectWait(_target,
                Internal.PromiseRef.DelegateWrapper.Create(resolveCaptureValue, onResolved),
                Internal.PromiseRef.DelegateWrapper.Create(onRejected),
                cancelationToken);
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="TResult"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise<TResult> Then<TCaptureReject, TResult, TReject>(Func<Promise<TResult>> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise<TResult>> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            return Internal.PromiseRef.CallbackHelper.AddResolveRejectWait(_target,
                Internal.PromiseRef.DelegateWrapper.Create(onResolved),
                Internal.PromiseRef.DelegateWrapper.Create(rejectCaptureValue, onRejected),
                cancelationToken);
        }

        /// <summary>
        /// Capture 2 values and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="TResult"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise<TResult> Then<TCaptureResolve, TCaptureReject, TResult, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise<TResult>> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise<TResult>> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            return Internal.PromiseRef.CallbackHelper.AddResolveRejectWait(_target,
                Internal.PromiseRef.DelegateWrapper.Create(resolveCaptureValue, onResolved),
                Internal.PromiseRef.DelegateWrapper.Create(rejectCaptureValue, onRejected),
                cancelationToken);
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise Then<TCaptureResolve>(TCaptureResolve resolveCaptureValue, Action<TCaptureResolve> onResolved, Func<Promise> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            return Internal.PromiseRef.CallbackHelper.AddResolveRejectWait(_target,
                Internal.PromiseRef.DelegateWrapper.Create(resolveCaptureValue, onResolved),
                Internal.PromiseRef.DelegateWrapper.Create(onRejected),
                cancelationToken);
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise Then<TCaptureReject>(Action onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            return Internal.PromiseRef.CallbackHelper.AddResolveRejectWait(_target,
                Internal.PromiseRef.DelegateWrapper.Create(onResolved),
                Internal.PromiseRef.DelegateWrapper.Create(rejectCaptureValue, onRejected),
                cancelationToken);
        }

        /// <summary>
        /// Capture 2 values and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise Then<TCaptureResolve, TCaptureReject>(TCaptureResolve resolveCaptureValue, Action<TCaptureResolve> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            return Internal.PromiseRef.CallbackHelper.AddResolveRejectWait(_target,
                Internal.PromiseRef.DelegateWrapper.Create(resolveCaptureValue, onResolved),
                Internal.PromiseRef.DelegateWrapper.Create(rejectCaptureValue, onRejected),
                cancelationToken);
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise Then<TCaptureResolve, TReject>(TCaptureResolve resolveCaptureValue, Action<TCaptureResolve> onResolved, Func<TReject, Promise> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            return Internal.PromiseRef.CallbackHelper.AddResolveRejectWait(_target,
                Internal.PromiseRef.DelegateWrapper.Create(resolveCaptureValue, onResolved),
                Internal.PromiseRef.DelegateWrapper.Create(onRejected),
                cancelationToken);
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise Then<TCaptureReject, TReject>(Action onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            return Internal.PromiseRef.CallbackHelper.AddResolveRejectWait(_target,
                Internal.PromiseRef.DelegateWrapper.Create(onResolved),
                Internal.PromiseRef.DelegateWrapper.Create(rejectCaptureValue, onRejected),
                cancelationToken);
        }

        /// <summary>
        /// Capture 2 values and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise Then<TCaptureResolve, TCaptureReject, TReject>(TCaptureResolve resolveCaptureValue, Action<TCaptureResolve> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            return Internal.PromiseRef.CallbackHelper.AddResolveRejectWait(_target,
                Internal.PromiseRef.DelegateWrapper.Create(resolveCaptureValue, onResolved),
                Internal.PromiseRef.DelegateWrapper.Create(rejectCaptureValue, onRejected),
                cancelationToken);
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="TResult"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise<TResult> Then<TCaptureResolve, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, TResult> onResolved, Func<Promise<TResult>> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            return Internal.PromiseRef.CallbackHelper.AddResolveRejectWait(_target,
                Internal.PromiseRef.DelegateWrapper.Create(resolveCaptureValue, onResolved),
                Internal.PromiseRef.DelegateWrapper.Create(onRejected),
                cancelationToken);
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="TResult"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise<TResult> Then<TCaptureReject, TResult>(Func<TResult> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise<TResult>> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            return Internal.PromiseRef.CallbackHelper.AddResolveRejectWait(_target,
                Internal.PromiseRef.DelegateWrapper.Create(onResolved),
                Internal.PromiseRef.DelegateWrapper.Create(rejectCaptureValue, onRejected),
                cancelationToken);
        }

        /// <summary>
        /// Capture 2 values and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="TResult"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise<TResult> Then<TCaptureResolve, TCaptureReject, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, TResult> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise<TResult>> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            return Internal.PromiseRef.CallbackHelper.AddResolveRejectWait(_target,
                Internal.PromiseRef.DelegateWrapper.Create(resolveCaptureValue, onResolved),
                Internal.PromiseRef.DelegateWrapper.Create(rejectCaptureValue, onRejected),
                cancelationToken);
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="TResult"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise<TResult> Then<TCaptureResolve, TResult, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, TResult> onResolved, Func<TReject, Promise<TResult>> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            return Internal.PromiseRef.CallbackHelper.AddResolveRejectWait(_target,
                Internal.PromiseRef.DelegateWrapper.Create(resolveCaptureValue, onResolved),
                Internal.PromiseRef.DelegateWrapper.Create(onRejected),
                cancelationToken);
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="TResult"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise<TResult> Then<TCaptureReject, TResult, TReject>(Func<TResult> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise<TResult>> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            return Internal.PromiseRef.CallbackHelper.AddResolveRejectWait(_target,
                Internal.PromiseRef.DelegateWrapper.Create(onResolved),
                Internal.PromiseRef.DelegateWrapper.Create(rejectCaptureValue, onRejected),
                cancelationToken);
        }

        /// <summary>
        /// Capture 2 values and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="TResult"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise<TResult> Then<TCaptureResolve, TCaptureReject, TResult, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, TResult> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise<TResult>> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            return Internal.PromiseRef.CallbackHelper.AddResolveRejectWait(_target,
                Internal.PromiseRef.DelegateWrapper.Create(resolveCaptureValue, onResolved),
                Internal.PromiseRef.DelegateWrapper.Create(rejectCaptureValue, onRejected),
                cancelationToken);
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise Then<TCaptureResolve>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise> onResolved, Action onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            return Internal.PromiseRef.CallbackHelper.AddResolveRejectWait(_target,
                Internal.PromiseRef.DelegateWrapper.Create(resolveCaptureValue, onResolved),
                Internal.PromiseRef.DelegateWrapper.Create(onRejected),
                cancelationToken);
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise Then<TCaptureReject>(Func<Promise> onResolved, TCaptureReject rejectCaptureValue, Action<TCaptureReject> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            return Internal.PromiseRef.CallbackHelper.AddResolveRejectWait(_target,
                Internal.PromiseRef.DelegateWrapper.Create(onResolved),
                Internal.PromiseRef.DelegateWrapper.Create(rejectCaptureValue, onRejected),
                cancelationToken);
        }

        /// <summary>
        /// Capture 2 values and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise Then<TCaptureResolve, TCaptureReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise> onResolved, TCaptureReject rejectCaptureValue, Action<TCaptureReject> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            return Internal.PromiseRef.CallbackHelper.AddResolveRejectWait(_target,
                Internal.PromiseRef.DelegateWrapper.Create(resolveCaptureValue, onResolved),
                Internal.PromiseRef.DelegateWrapper.Create(rejectCaptureValue, onRejected),
                cancelationToken);
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise Then<TCaptureResolve, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise> onResolved, Action<TReject> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            return Internal.PromiseRef.CallbackHelper.AddResolveRejectWait(_target,
                Internal.PromiseRef.DelegateWrapper.Create(resolveCaptureValue, onResolved),
                Internal.PromiseRef.DelegateWrapper.Create(onRejected),
                cancelationToken);
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise Then<TCaptureReject, TReject>(Func<Promise> onResolved, TCaptureReject rejectCaptureValue, Action<TCaptureReject, TReject> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            return Internal.PromiseRef.CallbackHelper.AddResolveRejectWait(_target,
                Internal.PromiseRef.DelegateWrapper.Create(onResolved),
                Internal.PromiseRef.DelegateWrapper.Create(rejectCaptureValue, onRejected),
                cancelationToken);
        }

        /// <summary>
        /// Capture 2 values and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise Then<TCaptureResolve, TCaptureReject, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise> onResolved, TCaptureReject rejectCaptureValue, Action<TCaptureReject, TReject> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            return Internal.PromiseRef.CallbackHelper.AddResolveRejectWait(_target,
                Internal.PromiseRef.DelegateWrapper.Create(resolveCaptureValue, onResolved),
                Internal.PromiseRef.DelegateWrapper.Create(rejectCaptureValue, onRejected),
                cancelationToken);
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="TResult"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise<TResult> Then<TCaptureResolve, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise<TResult>> onResolved, Func<TResult> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            return Internal.PromiseRef.CallbackHelper.AddResolveRejectWait(_target,
                Internal.PromiseRef.DelegateWrapper.Create(resolveCaptureValue, onResolved),
                Internal.PromiseRef.DelegateWrapper.Create(onRejected),
                cancelationToken);
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="TResult"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise<TResult> Then<TCaptureReject, TResult>(Func<Promise<TResult>> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TResult> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            return Internal.PromiseRef.CallbackHelper.AddResolveRejectWait(_target,
                Internal.PromiseRef.DelegateWrapper.Create(onResolved),
                Internal.PromiseRef.DelegateWrapper.Create(rejectCaptureValue, onRejected),
                cancelationToken);
        }

        /// <summary>
        /// Capture 2 values and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="TResult"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise<TResult> Then<TCaptureResolve, TCaptureReject, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise<TResult>> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TResult> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            return Internal.PromiseRef.CallbackHelper.AddResolveRejectWait(_target,
                Internal.PromiseRef.DelegateWrapper.Create(resolveCaptureValue, onResolved),
                Internal.PromiseRef.DelegateWrapper.Create(rejectCaptureValue, onRejected),
                cancelationToken);
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="TResult"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise<TResult> Then<TCaptureResolve, TResult, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise<TResult>> onResolved, Func<TReject, TResult> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            return Internal.PromiseRef.CallbackHelper.AddResolveRejectWait(_target,
                Internal.PromiseRef.DelegateWrapper.Create(resolveCaptureValue, onResolved),
                Internal.PromiseRef.DelegateWrapper.Create(onRejected),
                cancelationToken);
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="TResult"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise<TResult> Then<TCaptureReject, TResult, TReject>(Func<Promise<TResult>> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, TResult> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            return Internal.PromiseRef.CallbackHelper.AddResolveRejectWait(_target,
                Internal.PromiseRef.DelegateWrapper.Create(onResolved),
                Internal.PromiseRef.DelegateWrapper.Create(rejectCaptureValue, onRejected),
                cancelationToken);
        }

        /// <summary>
        /// Capture 2 values and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="TResult"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise<TResult> Then<TCaptureResolve, TCaptureReject, TResult, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise<TResult>> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, TResult> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            return Internal.PromiseRef.CallbackHelper.AddResolveRejectWait(_target,
                Internal.PromiseRef.DelegateWrapper.Create(resolveCaptureValue, onResolved),
                Internal.PromiseRef.DelegateWrapper.Create(rejectCaptureValue, onRejected),
                cancelationToken);
        }
        #endregion

        #region Continue Callbacks
        /// <summary>
        /// Capture a value and add a continuation callback. Returns a new <see cref="Promise"/>.
        /// <para/>When this is resolved, rejected, or canceled, <paramref name="onContinue"/> will be invoked with <paramref name="continueCaptureValue"/> and the <see cref="ResultContainer"/>, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onContinue"/> will not be invoked.
        /// </summary>
        public Promise ContinueWith<TCapture>(TCapture continueCaptureValue, ContinueAction<TCapture> onContinue, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(1);
            ValidateArgument(onContinue, "onContinue", 1);

            return Internal.PromiseRef.CallbackHelper.AddContinue(_target, Internal.PromiseRef.DelegateWrapper.Create(continueCaptureValue, onContinue), cancelationToken);
        }

        /// <summary>
        /// Capture a value and add a continuation callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="TResult"/>.
        /// <para/>When this is resolved, rejected, or canceled, <paramref name="onContinue"/> will be invoked with <paramref name="continueCaptureValue"/> and the <see cref="ResultContainer"/>, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onContinue"/> will not be invoked.
        /// </summary>
        public Promise<TResult> ContinueWith<TCapture, TResult>(TCapture continueCaptureValue, ContinueFunc<TCapture, TResult> onContinue, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(1);
            ValidateArgument(onContinue, "onContinue", 1);

            return Internal.PromiseRef.CallbackHelper.AddContinue(_target, Internal.PromiseRef.DelegateWrapper.Create(continueCaptureValue, onContinue), cancelationToken);
        }

        /// <summary>
        /// Capture a value and add a continuation callback. Returns a new <see cref="Promise"/>.
        /// <para/>When this is resolved, rejected, or canceled, <paramref name="onContinue"/> will be invoked with <paramref name="continueCaptureValue"/> and the <see cref="ResultContainer"/>, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onContinue"/> will not be invoked.
        /// </summary>
        public Promise ContinueWith<TCapture>(TCapture continueCaptureValue, ContinueFunc<TCapture, Promise> onContinue, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(1);
            ValidateArgument(onContinue, "onContinue", 1);

            return Internal.PromiseRef.CallbackHelper.AddContinueWait(_target, Internal.PromiseRef.DelegateWrapper.Create(continueCaptureValue, onContinue), cancelationToken);
        }

        /// <summary>
        /// Add a continuation callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="TResult"/>.
        /// <para/>When this is resolved, rejected, or canceled, <paramref name="onContinue"/> will be invoked with <paramref name="continueCaptureValue"/> and the <see cref="ResultContainer"/>, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onContinue"/> will not be invoked.
        /// </summary>
        public Promise<TResult> ContinueWith<TCapture, TResult>(TCapture continueCaptureValue, ContinueFunc<TCapture, Promise<TResult>> onContinue, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(1);
            ValidateArgument(onContinue, "onContinue", 1);

            return Internal.PromiseRef.CallbackHelper.AddContinueWait(_target, Internal.PromiseRef.DelegateWrapper.Create(continueCaptureValue, onContinue), cancelationToken);
        }
        #endregion

        [MethodImpl(Internal.InlineOption)]
        public bool Equals(Promise other)
        {
            return this == other;
        }

        public override bool Equals(object obj)
        {
#if CSHARP_7_3_OR_NEWER
            return obj is Promise promise && Equals(promise);
#else
            return obj is Promise && Equals((Promise) obj);
#endif
        }

        // Promises really shouldn't be used for lookups, but GetHashCode is overridden to complement ==.
        [MethodImpl(Internal.InlineOption)]
        public override int GetHashCode()
        {
            return _target.GetHashCode();
        }

        [MethodImpl(Internal.InlineOption)]
        public static bool operator ==(Promise lhs, Promise rhs)
        {
            return lhs._target == rhs._target;
        }

        [MethodImpl(Internal.InlineOption)]
        public static bool operator !=(Promise lhs, Promise rhs)
        {
            return !(lhs == rhs);
        }

        /// <summary>
        /// Returns null.
        /// </summary>
        [Obsolete]
        public Type ResultType { get { return null; } }

        [Obsolete("Retain is no longer valid, use Preserve instead.", true)]
        public void Retain()
        {
            throw new InvalidOperationException("Retain is no longer valid, use Preserve instead.", Internal.GetFormattedStacktrace(1));
        }

        [Obsolete("Release is no longer valid, use Forget instead.", true)]
        public void Release()
        {
            throw new InvalidOperationException("Release is no longer valid, use Forget instead.", Internal.GetFormattedStacktrace(1));
        }
    }
}