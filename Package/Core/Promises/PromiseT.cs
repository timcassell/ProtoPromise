#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#pragma warning disable IDE0090 // Use 'new(...)'

using Proto.Promises.CompilerServices;
using Proto.Timers;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Proto.Promises
{
    /// <summary>
    /// A <see cref="Promise{T}"/> represents the eventual result of an asynchronous operation.
    /// The primary ways of interacting with a <see cref="Promise{T}"/> are via the `await` keyword in an async function,
    /// or its then method, which registers callbacks to be invoked with its resolve value when the <see cref="Promise{T}"/> is resolved,
    /// or the reason why the <see cref="Promise{T}"/> cannot be resolved.
    /// </summary>
    public readonly partial struct Promise<T> : IEquatable<Promise<T>>
    {
#if UNITY_2021_2_OR_NEWER || !UNITY_2018_3_OR_NEWER
        /// <summary>
        /// Convert this to a <see cref="System.Threading.Tasks.ValueTask{T}"/>.
        /// </summary>
        [MethodImpl(Internal.InlineOption)]
        public System.Threading.Tasks.ValueTask<T> AsValueTask()
        {
            ValidateOperation(1);
            var r = _ref;
            return r == null
                ? new System.Threading.Tasks.ValueTask<T>(_result)
                : r.ToValueTask(_id);
        }

        /// <summary>
        /// Cast to <see cref="System.Threading.Tasks.ValueTask{T}"/>.
        /// </summary>
        [MethodImpl(Internal.InlineOption)]
        public static implicit operator System.Threading.Tasks.ValueTask<T>(in Promise<T> rhs)
            => rhs.AsValueTask();

        /// <summary>
        /// Convert this to a <see cref="System.Threading.Tasks.ValueTask"/>.
        /// </summary>
        [MethodImpl(Internal.InlineOption)]
        public System.Threading.Tasks.ValueTask AsValueTaskVoid()
            => AsPromise().AsValueTask();

        /// <summary>
        /// Cast to <see cref="System.Threading.Tasks.ValueTask"/>.
        /// </summary>
        [MethodImpl(Internal.InlineOption)]
        public static implicit operator System.Threading.Tasks.ValueTask(in Promise<T> rhs)
            => rhs.AsValueTaskVoid();
#endif // UNITY_2021_2_OR_NEWER || !UNITY_2018_3_OR_NEWER

        /// <summary>
        /// Cast to <see cref="Promise"/>.
        /// </summary>
        [MethodImpl(Internal.InlineOption)]
        public Promise AsPromise()
            => new Promise(_ref, _id);

        /// <summary>
        /// Cast to <see cref="Promise"/>.
        /// </summary>
        [MethodImpl(Internal.InlineOption)]
        public static implicit operator Promise(in Promise<T> rhs)
            => rhs.AsPromise();

        /// <summary>
        /// Mark this as awaited and prevent any further awaits or callbacks on this.
        /// <para/>NOTE: It is imperative to terminate your promise chains with Forget so that any uncaught rejections will be reported and objects repooled (if pooling is enabled).
        /// </summary>
        public void Forget()
        {
            ValidateOperation(1);
            _ref?.Forget(_id);
        }

        /// <summary>
        /// Mark this as awaited and wait for the operation to complete. Returns the result of the operation.
        /// If the operation was rejected or canceled, the appropriate exception will be thrown.
        /// </summary>
        /// <remarks>Warning: this may cause a deadlock if you are not careful. Make sure you know what you are doing!</remarks>
        public T WaitForResult()
        {
            var resultContainer = WaitForResultNoThrow();
            resultContainer.RethrowIfRejectedOrCanceled();
            return resultContainer.Value;
        }

        /// <summary>
        /// Mark this as awaited and wait for the operation to complete, without throwing. Returns a <see cref="ResultContainer"/> that wraps the completion state and result or reason of the operation.
        /// </summary>
        /// <remarks>Warning: this may cause a deadlock if you are not careful. Make sure you know what you are doing!</remarks>
        public ResultContainer WaitForResultNoThrow()
        {
            ValidateOperation(1);
            var r = _ref;
            if (r == null)
            {
                return new ResultContainer(_result, null, Promise.State.Resolved);
            }
            Internal.PromiseSynchronousWaiter.TryWaitForResult(r, _id, TimeSpan.FromMilliseconds(Timeout.Infinite), out var resultContainer);
            return resultContainer;
        }

        /// <summary>
        /// Mark this as awaited and wait for the operation to complete with a specified timeout.
        /// <para/>If the operation completed successfully before the timeout expired, this will return <see langword="true"/> and <paramref name="result"/> will be assigned from the result of the operation. Otherwise, this will return <see langword="false"/>.
        /// If the operation was rejected or canceled, the appropriate exception will be thrown.
        /// </summary>
        /// <remarks>
        /// If a <see cref="TimeSpan"/> representing -1 millisecond is specified for the timeout parameter, this method blocks indefinitely until the operation is complete.
        /// <para/>Warning: this may cause a deadlock if you are not careful. Make sure you know what you are doing!
        /// </remarks>
        public bool TryWaitForResult(TimeSpan timeout, out T result)
        {
            if (!TryWaitForResultNoThrow(timeout, out var resultContainer))
            {
                result = default;
                return false;
            }
            resultContainer.RethrowIfRejectedOrCanceled();
            result = resultContainer.Value;
            return true;
        }

        /// <summary>
        /// Mark this as awaited and wait for the operation to complete with a specified timeout, without throwing.
        /// <para/>If the operation completed before the timeout expired, this will return <see langword="true"/> and <paramref name="resultContainer"/> will be assigned from the result of the operation. Otherwise, this will return <see langword="false"/>.
        /// </summary>
        /// <remarks>
        /// If a <see cref="TimeSpan"/> representing -1 millisecond is specified for the timeout parameter, this method blocks indefinitely until the operation is complete.
        /// <para/>Warning: this may cause a deadlock if you are not careful. Make sure you know what you are doing!
        /// </remarks>
        public bool TryWaitForResultNoThrow(TimeSpan timeout, out ResultContainer resultContainer)
        {
            ValidateOperation(1);
            var r = _ref;
            if (r == null)
            {
                resultContainer = new ResultContainer(_result, null, Promise.State.Resolved);
                return true;
            }
            return Internal.PromiseSynchronousWaiter.TryWaitForResult(r, _id, timeout, out resultContainer);
        }


        /// <summary>
        /// Returns a new <see cref="Promise{T}"/> that inherits the state of this, or will be canceled if/when the <paramref name="cancelationToken"/> is canceled before this is complete.
        /// </summary>
        /// <param name="cancelationToken">The <see cref="CancelationToken"/> to monitor for a cancelation request.</param>
        [MethodImpl(Internal.InlineOption)]
        public Promise<T> WaitAsync(CancelationToken cancelationToken)
        {
            ValidateOperation(1);
            return Internal.PromiseRefBase.CallbackHelperResult<T>.WaitAsync(this, cancelationToken);
        }

        /// <summary>
        /// Returns a new <see cref="Promise{T}"/> that inherits the state of this, or will be rejected with a <see cref="TimeoutException"/>
        /// if/when the <paramref name="timeout"/> has elapsed before this is complete.
        /// </summary>
        /// <param name="timeout">The timeout after which the returned <see cref="Promise{T}"/> should be rejected with a <see cref="TimeoutException"/> if it hasn't otherwise completed.</param>
        [MethodImpl(Internal.InlineOption)]
        public Promise<T> WaitAsync(TimeSpan timeout)
            => WaitAsync(timeout, Promise.Config.DefaultTimerFactory);

        /// <summary>
        /// Returns a new <see cref="Promise{T}"/> that inherits the state of this, or will be rejected with a <see cref="TimeoutException"/>
        /// if/when the <paramref name="timeout"/> has elapsed, or will be canceled if/when the <paramref name="cancelationToken"/> is canceled, before this is complete.
        /// </summary>
        /// <param name="timeout">The timeout after which the returned <see cref="Promise{T}"/> should be rejected with a <see cref="TimeoutException"/> if it hasn't otherwise completed.</param>
        /// <param name="cancelationToken">The <see cref="CancelationToken"/> to monitor for a cancelation request.</param>
        [MethodImpl(Internal.InlineOption)]
        public Promise<T> WaitAsync(TimeSpan timeout, CancelationToken cancelationToken)
            => WaitAsync(timeout, Promise.Config.DefaultTimerFactory, cancelationToken);

        /// <summary>
        /// Returns a new <see cref="Promise{T}"/> that inherits the state of this, or will be rejected with a <see cref="TimeoutException"/>
        /// if/when the <paramref name="timeout"/> has elapsed before this is complete.
        /// </summary>
        /// <param name="timeout">The timeout after which the returned <see cref="Promise{T}"/> should be rejected with a <see cref="TimeoutException"/> if it hasn't otherwise completed.</param>
        /// <param name="timerFactory">The <see cref="TimerFactory"/> with which to interpet <paramref name="timeout"/>.</param>
        [MethodImpl(Internal.InlineOption)]
        public Promise<T> WaitAsync(TimeSpan timeout, TimerFactory timerFactory)
        {
            ValidateOperation(1);
            ValidateArgument(timerFactory, nameof(timerFactory), 1);
            return Internal.PromiseRefBase.CallbackHelperResult<T>.WaitAsync(this, timeout, timerFactory);
        }

        /// <summary>
        /// Returns a new <see cref="Promise{T}"/> that inherits the state of this, or will be rejected with a <see cref="TimeoutException"/>
        /// if/when the <paramref name="timeout"/> has elapsed, or will be canceled if/when the <paramref name="cancelationToken"/> is canceled, before this is complete.
        /// </summary>
        /// <param name="timeout">The timeout after which the returned <see cref="Promise{T}"/> should be rejected with a <see cref="TimeoutException"/> if it hasn't otherwise completed.</param>
        /// <param name="timerFactory">The <see cref="TimerFactory"/> with which to interpet <paramref name="timeout"/>.</param>
        /// <param name="cancelationToken">The <see cref="CancelationToken"/> to monitor for a cancelation request.</param>
        [MethodImpl(Internal.InlineOption)]
        public Promise<T> WaitAsync(TimeSpan timeout, TimerFactory timerFactory, CancelationToken cancelationToken)
        {
            ValidateOperation(1);
            ValidateArgument(timerFactory, nameof(timerFactory), 1);
            return Internal.PromiseRefBase.CallbackHelperResult<T>.WaitAsync(this, timeout, timerFactory, cancelationToken);
        }

        /// <summary>
        /// Configure the next continuation.
        /// Returns a new <see cref="Promise"/> that will adopt the state of this and be completed according to the provided <paramref name="continuationOptions"/>.
        /// </summary>
        /// <param name="continuationOptions">The options used to configure the execution behavior of the next continuation.</param>
        [MethodImpl(Internal.InlineOption)]
        public Promise<T> ConfigureContinuation(ContinuationOptions continuationOptions)
        {
            ValidateOperation(1);
            return Internal.PromiseRefBase.CallbackHelperResult<T>.ConfigureContinuation(this, continuationOptions);
        }

        /// <summary>
        /// Configure the await.
        /// Returns a <see cref="ConfiguredPromiseAwaiter{T}"/> that configures the continuation behavior of the await according to the provided <paramref name="continuationOptions"/>.
        /// </summary>
        /// <param name="continuationOptions">The options used to configure the execution behavior of the async continuation.</param>
        [MethodImpl(Internal.InlineOption)]
        public ConfiguredPromiseAwaiter<T> ConfigureAwait(ContinuationOptions continuationOptions)
        {
            ValidateOperation(1);
            return new ConfiguredPromiseAwaiter<T>(this, continuationOptions);
        }

        /// <summary>
        /// Add a finally callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="T"/>.
        /// <para/>When this is resolved, rejected, or canceled, <paramref name="onFinally"/> will be invoked.
        /// <para/>If <paramref name="onFinally"/> throws an exception, the new <see cref="Promise{T}"/> will be rejected with that exception,
        /// otherwise it will be resolved, rejected, or canceled with the same value or reason as this.
        /// </summary>
        public Promise<T> Finally(Action onFinally)
        {
            ValidateOperation(1);
            ValidateArgument(onFinally, nameof(onFinally), 1);

            return Internal.PromiseRefBase.CallbackHelperVoid.AddFinally(this, Internal.PromiseRefBase.DelegateWrapper.Create(onFinally));
        }

        /// <summary>
        /// Add a finally callback. Returns a new <see cref="Promise{T}"/>.
        /// </summary>
        /// <remarks>
        /// When this is resolved, rejected, or canceled, <paramref name="onFinally"/> will be invoked, and the new <see cref="Promise{T}"/> will wait for the returned <see cref="Promise"/> to settle.
        /// <para/>
        /// If <paramref name="onFinally"/> throws an exception, the new <see cref="Promise{T}"/> will be rejected with that exception.
        /// <para/>
        /// If the <see cref="Promise"/> returned from <paramref name="onFinally"/> is rejected or canceled, the new <see cref="Promise{T}"/> will adopt its state.
        /// <para/>
        /// Otherwise, the new <see cref="Promise{T}"/> will adopt the state of this when the <see cref="Promise"/> returned from <paramref name="onFinally"/> is resolved.
        /// </remarks>
        [MethodImpl(Internal.InlineOption)]
        public Promise<T> Finally(Func<Promise> onFinally)
        {
            ValidateOperation(1);
            ValidateArgument(onFinally, nameof(onFinally), 1);

            return Internal.PromiseRefBase.CallbackHelperVoid.AddFinallyWait(this, Internal.PromiseRefBase.DelegateWrapper.Create(onFinally));
        }

        /// <summary>
        /// Add a cancel callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is canceled, <paramref name="onCanceled"/> will be invoked, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is resolved, the new <see cref="Promise{T}"/> will be resolved with the same value.
        /// <para/>If/when this is rejected with any reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// </summary>
        public Promise<T> CatchCancelation(Func<T> onCanceled)
        {
            ValidateOperation(1);
            ValidateArgument(onCanceled, nameof(onCanceled), 1);

            return Internal.PromiseRefBase.CallbackHelperResult<T>.AddCancel(this, Internal.PromiseRefBase.DelegateWrapper.Create(onCanceled));
        }

        /// <summary>
        /// Add a cancel callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is canceled, <paramref name="onCanceled"/> will be invoked, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is resolved, the new <see cref="Promise{T}"/> will be resolved with the same value.
        /// <para/>If/when this is rejected with any reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// </summary>
        public Promise<T> CatchCancelation(Func<Promise<T>> onCanceled)
        {
            ValidateOperation(1);
            ValidateArgument(onCanceled, nameof(onCanceled), 1);

            return Internal.PromiseRefBase.CallbackHelperResult<T>.AddCancelWait(this, Internal.PromiseRefBase.DelegateWrapper.Create(onCanceled));
        }

        #region Resolve Callbacks
        /// <summary>
        /// Add a resolve callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is rejected with any reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled, the new <see cref="Promise"/> will be canceled.
        /// </summary>
        public Promise Then(Action<T> onResolved)
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, nameof(onResolved), 1);

            return Internal.PromiseRefBase.CallbackHelperArg<T>.AddResolve(this, Internal.PromiseRefBase.DelegateWrapper.Create(onResolved));
        }

        /// <summary>
        /// Add a resolve callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="TResult"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is rejected with any reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled, the new <see cref="Promise{T}"/> will be canceled.
        /// </summary>
        public Promise<TResult> Then<TResult>(Func<T, TResult> onResolved)
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, nameof(onResolved), 1);

            return Internal.PromiseRefBase.CallbackHelper<T, TResult>.AddResolve(this, Internal.PromiseRefBase.DelegateWrapper.Create(onResolved));
        }

        /// <summary>
        /// Add a resolve callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is rejected with any reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled, the new <see cref="Promise"/> will be canceled.
        /// </summary>
        public Promise Then(Func<T, Promise> onResolved)
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, nameof(onResolved), 1);

            return Internal.PromiseRefBase.CallbackHelperArg<T>.AddResolveWait(this, Internal.PromiseRefBase.DelegateWrapper.Create(onResolved));
        }

        /// <summary>
        /// Add a resolve callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="TResult"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is rejected with any reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled, the new <see cref="Promise{T}"/> will be canceled.
        /// </summary>
        public Promise<TResult> Then<TResult>(Func<T, Promise<TResult>> onResolved)
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, nameof(onResolved), 1);

            return Internal.PromiseRefBase.CallbackHelper<T, TResult>.AddResolveWait(this, Internal.PromiseRefBase.DelegateWrapper.Create(onResolved));
        }
        #endregion

        #region Reject Callbacks
        /// <summary>
        /// Add a reject callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="T"/>.
        /// <para/>If/when this is resolved, the new <see cref="Promise{T}"/> will be resolved with the resolve value.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise{T}"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is canceled, the new <see cref="Promise{T}"/> will be canceled.
        /// </summary>
        public Promise<T> Catch(Func<T> onRejected)
        {
            ValidateOperation(1);
            ValidateArgument(onRejected, nameof(onRejected), 1);

            return Internal.PromiseRefBase.CallbackHelper<T, T>
                .AddResolveReject(this, Internal.PromiseRefBase.DelegateWrapper.CreatePassthrough<T>(), Internal.PromiseRefBase.DelegateWrapper.Create(onRejected));
        }

        /// <summary>
        /// Add a reject callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="T"/>.
        /// <para/>If/when this is resolved, the new <see cref="Promise{T}"/> will be resolved with the resolve value.
        /// <para/>If/when this is rejected with any reason that is assignable to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise{T}"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled, the new <see cref="Promise{T}"/> will be canceled.
        /// </summary>
        public Promise<T> Catch<TReject>(Func<TReject, T> onRejected)
        {
            ValidateOperation(1);
            ValidateArgument(onRejected, nameof(onRejected), 1);

            return Internal.PromiseRefBase.CallbackHelper<T, T>
                .AddResolveReject(this, Internal.PromiseRefBase.DelegateWrapper.CreatePassthrough<T>(), Internal.PromiseRefBase.DelegateWrapper.Create(onRejected));
        }

        /// <summary>
        /// Add a reject callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="T"/>.
        /// <para/>If/when this is resolved, the new <see cref="Promise{T}"/> will be resolved with the resolve value.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is canceled, the new <see cref="Promise{T}"/> will be canceled.
        /// </summary>
        public Promise<T> Catch(Func<Promise<T>> onRejected)
        {
            ValidateOperation(1);
            ValidateArgument(onRejected, nameof(onRejected), 1);

            return Internal.PromiseRefBase.CallbackHelper<T, T>
                .AddResolveRejectWait(this, Internal.PromiseRefBase.DelegateWrapper.CreatePassthrough<T>(), Internal.PromiseRefBase.DelegateWrapper.Create(onRejected));
        }

        /// <summary>
        /// Add a reject callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="T"/>.
        /// <para/>If/when this is rejected with any reason that is assignable to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is resolved, the new <see cref="Promise{T}"/> will be resolved with the resolve value.
        /// <para/>If/when this is canceled or rejected with any other reason or no reason, the new <see cref="Promise{T}"/> will be canceled or rejected with the same reason.
        /// </summary>
        public Promise<T> Catch<TReject>(Func<TReject, Promise<T>> onRejected)
        {
            ValidateOperation(1);
            ValidateArgument(onRejected, nameof(onRejected), 1);

            return Internal.PromiseRefBase.CallbackHelper<T, T>
                .AddResolveRejectWait(this, Internal.PromiseRefBase.DelegateWrapper.CreatePassthrough<T>(), Internal.PromiseRefBase.DelegateWrapper.Create(onRejected));
        }
        #endregion

        #region Resolve or Reject Callbacks
        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is canceled, the new <see cref="Promise"/> will be canceled.
        /// </summary>
        public Promise Then(Action<T> onResolved, Action onRejected)
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, nameof(onResolved), 1);
            ValidateArgument(onRejected, nameof(onRejected), 1);

            return Internal.PromiseRefBase.CallbackHelperArg<T>
                .AddResolveReject(this, Internal.PromiseRefBase.DelegateWrapper.Create(onResolved), Internal.PromiseRefBase.DelegateWrapper.Create(onRejected));
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is rejected with any reason that is assignable to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled, the new <see cref="Promise"/> will be canceled.
        /// </summary>
        public Promise Then<TReject>(Action<T> onResolved, Action<TReject> onRejected)
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, nameof(onResolved), 1);
            ValidateArgument(onRejected, nameof(onRejected), 1);

            return Internal.PromiseRefBase.CallbackHelperArg<T>
                .AddResolveReject(this, Internal.PromiseRefBase.DelegateWrapper.Create(onResolved), Internal.PromiseRefBase.DelegateWrapper.Create(onRejected));
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="TResult"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is canceled, the new <see cref="Promise{T}"/> will be canceled.
        /// </summary>
        public Promise<TResult> Then<TResult>(Func<T, TResult> onResolved, Func<TResult> onRejected)
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, nameof(onResolved), 1);
            ValidateArgument(onRejected, nameof(onRejected), 1);

            return Internal.PromiseRefBase.CallbackHelper<T, TResult>
                .AddResolveReject(this, Internal.PromiseRefBase.DelegateWrapper.Create(onResolved), Internal.PromiseRefBase.DelegateWrapper.Create(onRejected));
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="TResult"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is rejected with any reason that is assignable to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled, the new <see cref="Promise{T}"/> will be canceled.
        /// </summary>
        public Promise<TResult> Then<TResult, TReject>(Func<T, TResult> onResolved, Func<TReject, TResult> onRejected)
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, nameof(onResolved), 1);
            ValidateArgument(onRejected, nameof(onRejected), 1);

            return Internal.PromiseRefBase.CallbackHelper<T, TResult>
                .AddResolveReject(this, Internal.PromiseRefBase.DelegateWrapper.Create(onResolved), Internal.PromiseRefBase.DelegateWrapper.Create(onRejected));
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is canceled, the new <see cref="Promise"/> will be canceled.
        /// </summary>
        public Promise Then(Func<T, Promise> onResolved, Func<Promise> onRejected)
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, nameof(onResolved), 1);
            ValidateArgument(onRejected, nameof(onRejected), 1);

            return Internal.PromiseRefBase.CallbackHelperArg<T>
                .AddResolveRejectWait(this, Internal.PromiseRefBase.DelegateWrapper.Create(onResolved), Internal.PromiseRefBase.DelegateWrapper.Create(onRejected));
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is rejected with any reason that is assignable to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled, the new <see cref="Promise"/> will be canceled.
        /// </summary>
        public Promise Then<TReject>(Func<T, Promise> onResolved, Func<TReject, Promise> onRejected)
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, nameof(onResolved), 1);
            ValidateArgument(onRejected, nameof(onRejected), 1);

            return Internal.PromiseRefBase.CallbackHelperArg<T>
                .AddResolveRejectWait(this, Internal.PromiseRefBase.DelegateWrapper.Create(onResolved), Internal.PromiseRefBase.DelegateWrapper.Create(onRejected));
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="TResult"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is canceled, the new <see cref="Promise{T}"/> will be canceled.
        /// </summary>
        public Promise<TResult> Then<TResult>(Func<T, Promise<TResult>> onResolved, Func<Promise<TResult>> onRejected)
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, nameof(onResolved), 1);
            ValidateArgument(onRejected, nameof(onRejected), 1);

            return Internal.PromiseRefBase.CallbackHelper<T, TResult>
                .AddResolveRejectWait(this, Internal.PromiseRefBase.DelegateWrapper.Create(onResolved), Internal.PromiseRefBase.DelegateWrapper.Create(onRejected));
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="TResult"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is rejected with any reason that is assignable to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled, the new <see cref="Promise{T}"/> will be canceled.
        /// </summary>
        public Promise<TResult> Then<TResult, TReject>(Func<T, Promise<TResult>> onResolved, Func<TReject, Promise<TResult>> onRejected)
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, nameof(onResolved), 1);
            ValidateArgument(onRejected, nameof(onRejected), 1);

            return Internal.PromiseRefBase.CallbackHelper<T, TResult>
                .AddResolveRejectWait(this, Internal.PromiseRefBase.DelegateWrapper.Create(onResolved), Internal.PromiseRefBase.DelegateWrapper.Create(onRejected));
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is canceled, the new <see cref="Promise"/> will be canceled.
        /// </summary>
        public Promise Then(Action<T> onResolved, Func<Promise> onRejected)
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, nameof(onResolved), 1);
            ValidateArgument(onRejected, nameof(onRejected), 1);

            return Internal.PromiseRefBase.CallbackHelperArg<T>
                .AddResolveRejectWait(this, Internal.PromiseRefBase.DelegateWrapper.Create(onResolved), Internal.PromiseRefBase.DelegateWrapper.Create(onRejected));
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is rejected with any reason that is assignable to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled, the new <see cref="Promise"/> will be canceled.
        /// </summary>
        public Promise Then<TReject>(Action<T> onResolved, Func<TReject, Promise> onRejected)
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, nameof(onResolved), 1);
            ValidateArgument(onRejected, nameof(onRejected), 1);

            return Internal.PromiseRefBase.CallbackHelperArg<T>
                .AddResolveRejectWait(this, Internal.PromiseRefBase.DelegateWrapper.Create(onResolved), Internal.PromiseRefBase.DelegateWrapper.Create(onRejected));
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="TResult"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is canceled, the new <see cref="Promise{T}"/> will be canceled.
        /// </summary>
        public Promise<TResult> Then<TResult>(Func<T, TResult> onResolved, Func<Promise<TResult>> onRejected)
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, nameof(onResolved), 1);
            ValidateArgument(onRejected, nameof(onRejected), 1);

            return Internal.PromiseRefBase.CallbackHelper<T, TResult>
                .AddResolveRejectWait(this, Internal.PromiseRefBase.DelegateWrapper.Create(onResolved), Internal.PromiseRefBase.DelegateWrapper.Create(onRejected));
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="TResult"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is rejected with any reason that is assignable to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled, the new <see cref="Promise{T}"/> will be canceled.
        /// </summary>
        public Promise<TResult> Then<TResult, TReject>(Func<T, TResult> onResolved, Func<TReject, Promise<TResult>> onRejected)
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, nameof(onResolved), 1);
            ValidateArgument(onRejected, nameof(onRejected), 1);

            return Internal.PromiseRefBase.CallbackHelper<T, TResult>
                .AddResolveRejectWait(this, Internal.PromiseRefBase.DelegateWrapper.Create(onResolved), Internal.PromiseRefBase.DelegateWrapper.Create(onRejected));
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is canceled, the new <see cref="Promise"/> will be canceled.
        /// </summary>
        public Promise Then(Func<T, Promise> onResolved, Action onRejected)
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, nameof(onResolved), 1);
            ValidateArgument(onRejected, nameof(onRejected), 1);

            return Internal.PromiseRefBase.CallbackHelperArg<T>
                .AddResolveRejectWait(this, Internal.PromiseRefBase.DelegateWrapper.Create(onResolved), Internal.PromiseRefBase.DelegateWrapper.Create(onRejected));
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is rejected with any reason that is assignable to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled, the new <see cref="Promise"/> will be canceled.
        /// </summary>
        public Promise Then<TReject>(Func<T, Promise> onResolved, Action<TReject> onRejected)
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, nameof(onResolved), 1);
            ValidateArgument(onRejected, nameof(onRejected), 1);

            return Internal.PromiseRefBase.CallbackHelperArg<T>
                .AddResolveRejectWait(this, Internal.PromiseRefBase.DelegateWrapper.Create(onResolved), Internal.PromiseRefBase.DelegateWrapper.Create(onRejected));
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="TResult"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is canceled, the new <see cref="Promise{T}"/> will be canceled.
        /// </summary>
        public Promise<TResult> Then<TResult>(Func<T, Promise<TResult>> onResolved, Func<TResult> onRejected)
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, nameof(onResolved), 1);
            ValidateArgument(onRejected, nameof(onRejected), 1);

            return Internal.PromiseRefBase.CallbackHelper<T, TResult>
                .AddResolveRejectWait(this, Internal.PromiseRefBase.DelegateWrapper.Create(onResolved), Internal.PromiseRefBase.DelegateWrapper.Create(onRejected));
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="TResult"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is rejected with any reason that is assignable to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled, the new <see cref="Promise{T}"/> will be canceled.
        /// </summary>
        public Promise<TResult> Then<TResult, TReject>(Func<T, Promise<TResult>> onResolved, Func<TReject, TResult> onRejected)
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, nameof(onResolved), 1);
            ValidateArgument(onRejected, nameof(onRejected), 1);

            return Internal.PromiseRefBase.CallbackHelper<T, TResult>
                .AddResolveRejectWait(this, Internal.PromiseRefBase.DelegateWrapper.Create(onResolved), Internal.PromiseRefBase.DelegateWrapper.Create(onRejected));
        }
        #endregion

        #region Continue Callbacks
        /// <summary>
        /// Add a continuation callback. Returns a new <see cref="Promise"/>.
        /// <para/>When this is resolved, rejected, or canceled, <paramref name="onContinue"/> will be invoked with the <see cref="ResultContainer"/>, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// </summary>
        public Promise ContinueWith(Action<ResultContainer> onContinue)
        {
            ValidateOperation(1);
            ValidateArgument(onContinue, nameof(onContinue), 1);

            return Internal.PromiseRefBase.CallbackHelperArg<T>.ContinueWith(this,
                ContinuerArg<T>.ContinueWith(DelegateWrapper.Create(onContinue))
            );
        }

        /// <summary>
        /// Add a continuation callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="TResult"/>.
        /// <para/>When this is resolved, rejected, or canceled, <paramref name="onContinue"/> will be invoked with the <see cref="ResultContainer"/>, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// </summary>
        public Promise<TResult> ContinueWith<TResult>(Func<ResultContainer, TResult> onContinue)
        {
            ValidateOperation(1);
            ValidateArgument(onContinue, nameof(onContinue), 1);

            return Internal.PromiseRefBase.CallbackHelper<T, TResult>.ContinueWith(this,
                Continuer<T, TResult>.ContinueWith(DelegateWrapper.Create(onContinue))
            );
        }


        /// <summary>
        /// Add a continuation callback. Returns a new <see cref="Promise"/>.
        /// <para/>When this is resolved, rejected, or canceled, <paramref name="onContinue"/> will be invoked with the <see cref="ResultContainer"/>, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// </summary>
        public Promise ContinueWith(Func<ResultContainer, Promise> onContinue)
        {
            ValidateOperation(1);
            ValidateArgument(onContinue, nameof(onContinue), 1);

            return Internal.PromiseRefBase.CallbackHelperArg<T>.ContinueWithWait(this,
                ContinuerArg<T>.ContinueWithWait(DelegateWrapper.Create(onContinue))
            );
        }

        /// <summary>
        /// Add a continuation callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="TResult"/>.
        /// <para/>When this is resolved, rejected, or canceled, <paramref name="onContinue"/> will be invoked with the <see cref="ResultContainer"/>, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// </summary>
        public Promise<TResult> ContinueWith<TResult>(Func<ResultContainer, Promise<TResult>> onContinue)
        {
            ValidateOperation(1);
            ValidateArgument(onContinue, nameof(onContinue), 1);

            return Internal.PromiseRefBase.CallbackHelper<T, TResult>.ContinueWithWait(this,
                Continuer<T, TResult>.ContinueWithWait(DelegateWrapper.Create(onContinue))
            );
        }

        /// <summary>
        /// Add a continuation callback. Returns a new <see cref="Promise"/>.
        /// <para/>When this is resolved, rejected, or canceled, <paramref name="onContinue"/> will be invoked with the <see cref="ResultContainer"/>, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled, and <paramref name="onContinue"/> will not be invoked.
        /// </summary>
        public Promise ContinueWith(Action<ResultContainer> onContinue, CancelationToken cancelationToken = default)
        {
            ValidateOperation(1);
            ValidateArgument(onContinue, nameof(onContinue), 1);

            return Internal.PromiseRefBase.CallbackHelperArg<T>.ContinueWith(this,
                ContinuerArg<T>.ContinueWith(DelegateWrapper.Create(onContinue)),
                cancelationToken
            );
        }

        /// <summary>
        /// Add a continuation callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="TResult"/>.
        /// <para/>When this is resolved, rejected, or canceled, <paramref name="onContinue"/> will be invoked with the <see cref="ResultContainer"/>, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled, and <paramref name="onContinue"/> will not be invoked.
        /// </summary>
        public Promise<TResult> ContinueWith<TResult>(Func<ResultContainer, TResult> onContinue, CancelationToken cancelationToken = default)
        {
            ValidateOperation(1);
            ValidateArgument(onContinue, nameof(onContinue), 1);

            return Internal.PromiseRefBase.CallbackHelper<T, TResult>.ContinueWith(this,
                Continuer<T, TResult>.ContinueWith(DelegateWrapper.Create(onContinue)),
                cancelationToken
            );
        }


        /// <summary>
        /// Add a continuation callback. Returns a new <see cref="Promise"/>.
        /// <para/>When this is resolved, rejected, or canceled, <paramref name="onContinue"/> will be invoked with the <see cref="ResultContainer"/>, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled, and <paramref name="onContinue"/> will not be invoked.
        /// </summary>
        public Promise ContinueWith(Func<ResultContainer, Promise> onContinue, CancelationToken cancelationToken = default)
        {
            ValidateOperation(1);
            ValidateArgument(onContinue, nameof(onContinue), 1);

            return Internal.PromiseRefBase.CallbackHelperArg<T>.ContinueWithWait(this,
                ContinuerArg<T>.ContinueWithWait(DelegateWrapper.Create(onContinue)),
                cancelationToken
            );
        }

        /// <summary>
        /// Add a continuation callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="TResult"/>.
        /// <para/>When this is resolved, rejected, or canceled, <paramref name="onContinue"/> will be invoked with the <see cref="ResultContainer"/>, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled, and <paramref name="onContinue"/> will not be invoked.
        /// </summary>
        public Promise<TResult> ContinueWith<TResult>(Func<ResultContainer, Promise<TResult>> onContinue, CancelationToken cancelationToken = default)
        {
            ValidateOperation(1);
            ValidateArgument(onContinue, nameof(onContinue), 1);

            return Internal.PromiseRefBase.CallbackHelper<T, TResult>.ContinueWithWait(this,
                Continuer<T, TResult>.ContinueWithWait(DelegateWrapper.Create(onContinue)),
                cancelationToken
            );
        }
        #endregion

        // Capture values below.

        /// <summary>
        /// Capture a value and add a finally callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="T"/>.
        /// <para/>When this is resolved, rejected, or canceled, <paramref name="onFinally"/> will be invoked with <paramref name="finallyCaptureValue"/>.
        /// <para/>If <paramref name="onFinally"/> throws an exception, the new <see cref="Promise{T}"/> will be rejected with that exception,
        /// otherwise it will be resolved, rejected, or canceled with the same value or reason as this.
        /// </summary>
        public Promise<T> Finally<TCaptureFinally>(TCaptureFinally finallyCaptureValue, Action<TCaptureFinally> onFinally)
        {
            ValidateOperation(1);
            ValidateArgument(onFinally, nameof(onFinally), 1);

            return Internal.PromiseRefBase.CallbackHelperVoid.AddFinally(this, Internal.PromiseRefBase.DelegateWrapper.Create(finallyCaptureValue, onFinally));
        }

        /// <summary>
        /// Capture a value and add a finally callback. Returns a new <see cref="Promise{T}"/>.
        /// </summary>
        /// <remarks>
        /// When this is resolved, rejected, or canceled, <paramref name="onFinally"/> will be invoked with <paramref name="finallyCaptureValue"/>,
        /// and the new <see cref="Promise{T}"/> will wait for the returned <see cref="Promise"/> to settle.
        /// <para/>
        /// If <paramref name="onFinally"/> throws an exception, the new <see cref="Promise{T}"/> will be rejected with that exception.
        /// <para/>
        /// If the <see cref="Promise"/> returned from <paramref name="onFinally"/> is rejected or canceled, the new <see cref="Promise{T}"/> will adopt its state.
        /// <para/>
        /// Otherwise, the new <see cref="Promise{T}"/> will adopt the state of this when the <see cref="Promise"/> returned from <paramref name="onFinally"/> is resolved.
        /// </remarks>
        [MethodImpl(Internal.InlineOption)]
        public Promise<T> Finally<TCaptureFinally>(TCaptureFinally finallyCaptureValue, Func<TCaptureFinally, Promise> onFinally)
        {
            ValidateOperation(1);
            ValidateArgument(onFinally, nameof(onFinally), 1);

            return Internal.PromiseRefBase.CallbackHelperVoid.AddFinallyWait(this, Internal.PromiseRefBase.DelegateWrapper.Create(finallyCaptureValue, onFinally));
        }

        /// <summary>
        /// Capture a value and add a cancel callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is canceled, <paramref name="onCanceled"/> will be invoked, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is resolved, the new <see cref="Promise{T}"/> will be resolved with the same value.
        /// <para/>If/when this is rejected with any reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// </summary>
        public Promise<T> CatchCancelation<TCaptureCancel>(TCaptureCancel cancelCaptureValue, Func<TCaptureCancel, T> onCanceled)
        {
            ValidateOperation(1);
            ValidateArgument(onCanceled, nameof(onCanceled), 1);

            return Internal.PromiseRefBase.CallbackHelperResult<T>.AddCancel(this, Internal.PromiseRefBase.DelegateWrapper.Create(cancelCaptureValue, onCanceled));
        }

        /// <summary>
        /// Capture a value and add a cancel callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is canceled, <paramref name="onCanceled"/> will be invoked, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is resolved, the new <see cref="Promise{T}"/> will be resolved with the same value.
        /// <para/>If/when this is rejected with any reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// </summary>
        public Promise<T> CatchCancelation<TCaptureCancel>(TCaptureCancel cancelCaptureValue, Func<TCaptureCancel, Promise<T>> onCanceled)
        {
            ValidateOperation(1);
            ValidateArgument(onCanceled, nameof(onCanceled), 1);

            return Internal.PromiseRefBase.CallbackHelperResult<T>.AddCancelWait(this, Internal.PromiseRefBase.DelegateWrapper.Create(cancelCaptureValue, onCanceled));
        }

        #region Resolve Callbacks
        /// <summary>
        /// Capture a value and add a resolve callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled, the new <see cref="Promise"/> will be canceled.
        /// </summary>
        public Promise Then<TCaptureResolve>(TCaptureResolve resolveCaptureValue, Action<TCaptureResolve, T> onResolved)
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, nameof(onResolved), 1);

            return Internal.PromiseRefBase.CallbackHelperArg<T>.AddResolve(this, Internal.PromiseRefBase.DelegateWrapper.Create(resolveCaptureValue, onResolved));
        }

        /// <summary>
        /// Capture a value and add a resolve callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="TResult"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled, the new <see cref="Promise{T}"/> will be canceled.
        /// </summary>
        public Promise<TResult> Then<TCaptureResolve, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, TResult> onResolved)
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, nameof(onResolved), 1);

            return Internal.PromiseRefBase.CallbackHelper<T, TResult>.AddResolve(this, Internal.PromiseRefBase.DelegateWrapper.Create(resolveCaptureValue, onResolved));
        }

        /// <summary>
        /// Capture a value and add a resolve callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled, the new <see cref="Promise"/> will be canceled.
        /// </summary>
        public Promise Then<TCaptureResolve>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise> onResolved)
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, nameof(onResolved), 1);

            return Internal.PromiseRefBase.CallbackHelperArg<T>.AddResolveWait(this, Internal.PromiseRefBase.DelegateWrapper.Create(resolveCaptureValue, onResolved));
        }

        /// <summary>
        /// Capture a value and add a resolve callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="TResult"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled, the new <see cref="Promise{T}"/> will be canceled.
        /// </summary>
        public Promise<TResult> Then<TCaptureResolve, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise<TResult>> onResolved)
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, nameof(onResolved), 1);

            return Internal.PromiseRefBase.CallbackHelper<T, TResult>.AddResolveWait(this, Internal.PromiseRefBase.DelegateWrapper.Create(resolveCaptureValue, onResolved));
        }
        #endregion

        #region Reject Callbacks
        /// <summary>
        /// Capture a value and add a reject callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="T"/>.
        /// <para/>If/when this is resolved, the new <see cref="Promise{T}"/> will be resolved with the resolve value.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise{T}"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled, the new <see cref="Promise{T}"/> will be canceled.
        /// </summary>
        public Promise<T> Catch<TCaptureReject>(TCaptureReject rejectCaptureValue, Func<TCaptureReject, T> onRejected)
        {
            ValidateOperation(1);
            ValidateArgument(onRejected, nameof(onRejected), 1);

            return Internal.PromiseRefBase.CallbackHelper<T, T>.AddResolveReject(this,
                Internal.PromiseRefBase.DelegateWrapper.CreatePassthrough<T>(),
                Internal.PromiseRefBase.DelegateWrapper.Create(rejectCaptureValue, onRejected));
        }

        /// <summary>
        /// Capture a value and add a reject callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="T"/>.
        /// <para/>If/when this is resolved, the new <see cref="Promise{T}"/> will be resolved with the resolve value.
        /// <para/>If/when this is rejected with any reason that is assignable to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise{T}"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled, the new <see cref="Promise{T}"/> will be canceled.
        /// </summary>
        public Promise<T> Catch<TCaptureReject, TReject>(TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, T> onRejected)
        {
            ValidateOperation(1);
            ValidateArgument(onRejected, nameof(onRejected), 1);

            return Internal.PromiseRefBase.CallbackHelper<T, T>.AddResolveReject(this,
                Internal.PromiseRefBase.DelegateWrapper.CreatePassthrough<T>(),
                Internal.PromiseRefBase.DelegateWrapper.Create(rejectCaptureValue, onRejected));
        }

        /// <summary>
        /// Capture a value and add a reject callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="T"/>.
        /// <para/>If/when this is resolved, the new <see cref="Promise{T}"/> will be resolved with the resolve value.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled, the new <see cref="Promise{T}"/> will be canceled.
        /// </summary>
        public Promise<T> Catch<TCaptureReject>(TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise<T>> onRejected)
        {
            ValidateOperation(1);
            ValidateArgument(onRejected, nameof(onRejected), 1);

            return Internal.PromiseRefBase.CallbackHelper<T, T>.AddResolveRejectWait(this,
                Internal.PromiseRefBase.DelegateWrapper.CreatePassthrough<T>(),
                Internal.PromiseRefBase.DelegateWrapper.Create(rejectCaptureValue, onRejected));
        }

        /// <summary>
        /// Capture a value and add a reject callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="T"/>.
        /// <para/>If/when this is resolved, the new <see cref="Promise{T}"/> will be resolved with the resolve value.
        /// <para/>If/when this is rejected with any reason that is assignable to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled, the new <see cref="Promise{T}"/> will be canceled.
        /// </summary>
        public Promise<T> Catch<TCaptureReject, TReject>(TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise<T>> onRejected)
        {
            ValidateOperation(1);
            ValidateArgument(onRejected, nameof(onRejected), 1);

            return Internal.PromiseRefBase.CallbackHelper<T, T>.AddResolveRejectWait(this,
                Internal.PromiseRefBase.DelegateWrapper.CreatePassthrough<T>(),
                Internal.PromiseRefBase.DelegateWrapper.Create(rejectCaptureValue, onRejected));
        }
        #endregion

        #region Resolve or Reject Callbacks
        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled, the new <see cref="Promise"/> will be canceled.
        /// </summary>
        public Promise Then<TCaptureResolve>(TCaptureResolve resolveCaptureValue, Action<TCaptureResolve, T> onResolved, Action onRejected)
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, nameof(onResolved), 1);
            ValidateArgument(onRejected, nameof(onRejected), 1);

            return Internal.PromiseRefBase.CallbackHelperArg<T>.AddResolveReject(this,
                Internal.PromiseRefBase.DelegateWrapper.Create(resolveCaptureValue, onResolved),
                Internal.PromiseRefBase.DelegateWrapper.Create(onRejected));
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with and the resolve value, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled, the new <see cref="Promise"/> will be canceled.
        /// </summary>
        public Promise Then<TCaptureReject>(Action<T> onResolved, TCaptureReject rejectCaptureValue, Action<TCaptureReject> onRejected)
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, nameof(onResolved), 1);
            ValidateArgument(onRejected, nameof(onRejected), 1);

            return Internal.PromiseRefBase.CallbackHelperArg<T>.AddResolveReject(this,
                Internal.PromiseRefBase.DelegateWrapper.Create(onResolved),
                Internal.PromiseRefBase.DelegateWrapper.Create(rejectCaptureValue, onRejected));
        }

        /// <summary>
        /// Capture 2 values and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled, the new <see cref="Promise"/> will be canceled.
        /// </summary>
        public Promise Then<TCaptureResolve, TCaptureReject>(TCaptureResolve resolveCaptureValue, Action<TCaptureResolve, T> onResolved, TCaptureReject rejectCaptureValue, Action<TCaptureReject> onRejected)
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, nameof(onResolved), 1);
            ValidateArgument(onRejected, nameof(onRejected), 1);

            return Internal.PromiseRefBase.CallbackHelperArg<T>.AddResolveReject(this,
                Internal.PromiseRefBase.DelegateWrapper.Create(resolveCaptureValue, onResolved),
                Internal.PromiseRefBase.DelegateWrapper.Create(rejectCaptureValue, onRejected));
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is assignable to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled, the new <see cref="Promise"/> will be canceled.
        /// </summary>
        public Promise Then<TCaptureResolve, TReject>(TCaptureResolve resolveCaptureValue, Action<TCaptureResolve, T> onResolved, Action<TReject> onRejected)
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, nameof(onResolved), 1);
            ValidateArgument(onRejected, nameof(onRejected), 1);

            return Internal.PromiseRefBase.CallbackHelperArg<T>.AddResolveReject(this,
                Internal.PromiseRefBase.DelegateWrapper.Create(resolveCaptureValue, onResolved),
                Internal.PromiseRefBase.DelegateWrapper.Create(onRejected));
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with and the resolve value, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is assignable to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled, the new <see cref="Promise"/> will be canceled.
        /// </summary>
        public Promise Then<TCaptureReject, TReject>(Action<T> onResolved, TCaptureReject rejectCaptureValue, Action<TCaptureReject, TReject> onRejected)
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, nameof(onResolved), 1);
            ValidateArgument(onRejected, nameof(onRejected), 1);

            return Internal.PromiseRefBase.CallbackHelperArg<T>.AddResolveReject(this,
                Internal.PromiseRefBase.DelegateWrapper.Create(onResolved),
                Internal.PromiseRefBase.DelegateWrapper.Create(rejectCaptureValue, onRejected));
        }

        /// <summary>
        /// Capture 2 values and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is assignable to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled, the new <see cref="Promise"/> will be canceled.
        /// </summary>
        public Promise Then<TCaptureResolve, TCaptureReject, TReject>(TCaptureResolve resolveCaptureValue, Action<TCaptureResolve, T> onResolved, TCaptureReject rejectCaptureValue, Action<TCaptureReject, TReject> onRejected)
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, nameof(onResolved), 1);
            ValidateArgument(onRejected, nameof(onRejected), 1);

            return Internal.PromiseRefBase.CallbackHelperArg<T>.AddResolveReject(this,
                Internal.PromiseRefBase.DelegateWrapper.Create(resolveCaptureValue, onResolved),
                Internal.PromiseRefBase.DelegateWrapper.Create(rejectCaptureValue, onRejected));
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="TResult"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled, the new <see cref="Promise{T}"/> will be canceled.
        /// </summary>
        public Promise<TResult> Then<TCaptureResolve, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, TResult> onResolved, Func<TResult> onRejected)
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, nameof(onResolved), 1);
            ValidateArgument(onRejected, nameof(onRejected), 1);

            return Internal.PromiseRefBase.CallbackHelper<T, TResult>.AddResolveReject(this,
                Internal.PromiseRefBase.DelegateWrapper.Create(resolveCaptureValue, onResolved),
                Internal.PromiseRefBase.DelegateWrapper.Create(onRejected));
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="TResult"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled, the new <see cref="Promise{T}"/> will be canceled.
        /// </summary>
        public Promise<TResult> Then<TCaptureReject, TResult>(Func<T, TResult> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TResult> onRejected)
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, nameof(onResolved), 1);
            ValidateArgument(onRejected, nameof(onRejected), 1);

            return Internal.PromiseRefBase.CallbackHelper<T, TResult>.AddResolveReject(this,
                Internal.PromiseRefBase.DelegateWrapper.Create(onResolved),
                Internal.PromiseRefBase.DelegateWrapper.Create(rejectCaptureValue, onRejected));
        }

        /// <summary>
        /// Capture 2 values and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="TResult"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled, the new <see cref="Promise{T}"/> will be canceled.
        /// </summary>
        public Promise<TResult> Then<TCaptureResolve, TCaptureReject, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, TResult> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TResult> onRejected)
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, nameof(onResolved), 1);
            ValidateArgument(onRejected, nameof(onRejected), 1);

            return Internal.PromiseRefBase.CallbackHelper<T, TResult>.AddResolveReject(this,
                Internal.PromiseRefBase.DelegateWrapper.Create(resolveCaptureValue, onResolved),
                Internal.PromiseRefBase.DelegateWrapper.Create(rejectCaptureValue, onRejected));
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="TResult"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is assignable to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled, the new <see cref="Promise{T}"/> will be canceled.
        /// </summary>
        public Promise<TResult> Then<TCaptureResolve, TResult, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, TResult> onResolved, Func<TReject, TResult> onRejected)
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, nameof(onResolved), 1);
            ValidateArgument(onRejected, nameof(onRejected), 1);

            return Internal.PromiseRefBase.CallbackHelper<T, TResult>.AddResolveReject(this,
                Internal.PromiseRefBase.DelegateWrapper.Create(resolveCaptureValue, onResolved),
                Internal.PromiseRefBase.DelegateWrapper.Create(onRejected));
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="TResult"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is assignable to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled, the new <see cref="Promise{T}"/> will be canceled.
        /// </summary>
        public Promise<TResult> Then<TCaptureReject, TResult, TReject>(Func<T, TResult> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, TResult> onRejected)
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, nameof(onResolved), 1);
            ValidateArgument(onRejected, nameof(onRejected), 1);

            return Internal.PromiseRefBase.CallbackHelper<T, TResult>.AddResolveReject(this,
                Internal.PromiseRefBase.DelegateWrapper.Create(onResolved),
                Internal.PromiseRefBase.DelegateWrapper.Create(rejectCaptureValue, onRejected));
        }

        /// <summary>
        /// Capture 2 values and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="TResult"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is assignable to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled, the new <see cref="Promise{T}"/> will be canceled.
        /// </summary>
        public Promise<TResult> Then<TCaptureResolve, TCaptureReject, TResult, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, TResult> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, TResult> onRejected)
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, nameof(onResolved), 1);
            ValidateArgument(onRejected, nameof(onRejected), 1);

            return Internal.PromiseRefBase.CallbackHelper<T, TResult>.AddResolveReject(this,
                Internal.PromiseRefBase.DelegateWrapper.Create(resolveCaptureValue, onResolved),
                Internal.PromiseRefBase.DelegateWrapper.Create(rejectCaptureValue, onRejected));
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled, the new <see cref="Promise"/> will be canceled.
        /// </summary>
        public Promise Then<TCaptureResolve>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise> onResolved, Func<Promise> onRejected)
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, nameof(onResolved), 1);
            ValidateArgument(onRejected, nameof(onRejected), 1);

            return Internal.PromiseRefBase.CallbackHelperArg<T>.AddResolveRejectWait(this,
                Internal.PromiseRefBase.DelegateWrapper.Create(resolveCaptureValue, onResolved),
                Internal.PromiseRefBase.DelegateWrapper.Create(onRejected));
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled, the new <see cref="Promise"/> will be canceled.
        /// </summary>
        public Promise Then<TCaptureReject>(Func<T, Promise> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise> onRejected)
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, nameof(onResolved), 1);
            ValidateArgument(onRejected, nameof(onRejected), 1);

            return Internal.PromiseRefBase.CallbackHelperArg<T>.AddResolveRejectWait(this,
                Internal.PromiseRefBase.DelegateWrapper.Create(onResolved),
                Internal.PromiseRefBase.DelegateWrapper.Create(rejectCaptureValue, onRejected));
        }

        /// <summary>
        /// Capture 2 values and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled, the new <see cref="Promise"/> will be canceled.
        /// </summary>
        public Promise Then<TCaptureResolve, TCaptureReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise> onRejected)
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, nameof(onResolved), 1);
            ValidateArgument(onRejected, nameof(onRejected), 1);

            return Internal.PromiseRefBase.CallbackHelperArg<T>.AddResolveRejectWait(this,
                Internal.PromiseRefBase.DelegateWrapper.Create(resolveCaptureValue, onResolved),
                Internal.PromiseRefBase.DelegateWrapper.Create(rejectCaptureValue, onRejected));
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is assignable to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled, the new <see cref="Promise"/> will be canceled.
        /// </summary>
        public Promise Then<TCaptureResolve, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise> onResolved, Func<TReject, Promise> onRejected)
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, nameof(onResolved), 1);
            ValidateArgument(onRejected, nameof(onRejected), 1);

            return Internal.PromiseRefBase.CallbackHelperArg<T>.AddResolveRejectWait(this,
                Internal.PromiseRefBase.DelegateWrapper.Create(resolveCaptureValue, onResolved),
                Internal.PromiseRefBase.DelegateWrapper.Create(onRejected));
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is assignable to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled, the new <see cref="Promise"/> will be canceled.
        /// </summary>
        public Promise Then<TCaptureReject, TReject>(Func<T, Promise> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise> onRejected)
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, nameof(onResolved), 1);
            ValidateArgument(onRejected, nameof(onRejected), 1);

            return Internal.PromiseRefBase.CallbackHelperArg<T>.AddResolveRejectWait(this,
                Internal.PromiseRefBase.DelegateWrapper.Create(onResolved),
                Internal.PromiseRefBase.DelegateWrapper.Create(rejectCaptureValue, onRejected));
        }

        /// <summary>
        /// Capture 2 values and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is assignable to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled, the new <see cref="Promise"/> will be canceled.
        /// </summary>
        public Promise Then<TCaptureResolve, TCaptureReject, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise> onRejected)
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, nameof(onResolved), 1);
            ValidateArgument(onRejected, nameof(onRejected), 1);

            return Internal.PromiseRefBase.CallbackHelperArg<T>.AddResolveRejectWait(this,
                Internal.PromiseRefBase.DelegateWrapper.Create(resolveCaptureValue, onResolved),
                Internal.PromiseRefBase.DelegateWrapper.Create(rejectCaptureValue, onRejected));
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="TResult"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled, the new <see cref="Promise{T}"/> will be canceled.
        /// </summary>
        public Promise<TResult> Then<TCaptureResolve, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise<TResult>> onResolved, Func<Promise<TResult>> onRejected)
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, nameof(onResolved), 1);
            ValidateArgument(onRejected, nameof(onRejected), 1);

            return Internal.PromiseRefBase.CallbackHelper<T, TResult>.AddResolveRejectWait(this,
                Internal.PromiseRefBase.DelegateWrapper.Create(resolveCaptureValue, onResolved),
                Internal.PromiseRefBase.DelegateWrapper.Create(onRejected));
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="TResult"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled, the new <see cref="Promise{T}"/> will be canceled.
        /// </summary>
        public Promise<TResult> Then<TCaptureReject, TResult>(Func<T, Promise<TResult>> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise<TResult>> onRejected)
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, nameof(onResolved), 1);
            ValidateArgument(onRejected, nameof(onRejected), 1);

            return Internal.PromiseRefBase.CallbackHelper<T, TResult>.AddResolveRejectWait(this,
                Internal.PromiseRefBase.DelegateWrapper.Create(onResolved),
                Internal.PromiseRefBase.DelegateWrapper.Create(rejectCaptureValue, onRejected));
        }

        /// <summary>
        /// Capture 2 values and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="TResult"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled, the new <see cref="Promise{T}"/> will be canceled.
        /// </summary>
        public Promise<TResult> Then<TCaptureResolve, TCaptureReject, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise<TResult>> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise<TResult>> onRejected)
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, nameof(onResolved), 1);
            ValidateArgument(onRejected, nameof(onRejected), 1);

            return Internal.PromiseRefBase.CallbackHelper<T, TResult>.AddResolveRejectWait(this,
                Internal.PromiseRefBase.DelegateWrapper.Create(resolveCaptureValue, onResolved),
                Internal.PromiseRefBase.DelegateWrapper.Create(rejectCaptureValue, onRejected));
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="TResult"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is assignable to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled, the new <see cref="Promise{T}"/> will be canceled.
        /// </summary>
        public Promise<TResult> Then<TCaptureResolve, TResult, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise<TResult>> onResolved, Func<TReject, Promise<TResult>> onRejected)
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, nameof(onResolved), 1);
            ValidateArgument(onRejected, nameof(onRejected), 1);

            return Internal.PromiseRefBase.CallbackHelper<T, TResult>.AddResolveRejectWait(this,
                Internal.PromiseRefBase.DelegateWrapper.Create(resolveCaptureValue, onResolved),
                Internal.PromiseRefBase.DelegateWrapper.Create(onRejected));
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="TResult"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is assignable to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled, the new <see cref="Promise{T}"/> will be canceled.
        /// </summary>
        public Promise<TResult> Then<TCaptureReject, TResult, TReject>(Func<T, Promise<TResult>> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise<TResult>> onRejected)
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, nameof(onResolved), 1);
            ValidateArgument(onRejected, nameof(onRejected), 1);

            return Internal.PromiseRefBase.CallbackHelper<T, TResult>.AddResolveRejectWait(this,
                Internal.PromiseRefBase.DelegateWrapper.Create(onResolved),
                Internal.PromiseRefBase.DelegateWrapper.Create(rejectCaptureValue, onRejected));
        }

        /// <summary>
        /// Capture 2 values and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="TResult"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is assignable to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled, the new <see cref="Promise{T}"/> will be canceled.
        /// </summary>
        public Promise<TResult> Then<TCaptureResolve, TCaptureReject, TResult, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise<TResult>> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise<TResult>> onRejected)
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, nameof(onResolved), 1);
            ValidateArgument(onRejected, nameof(onRejected), 1);

            return Internal.PromiseRefBase.CallbackHelper<T, TResult>.AddResolveRejectWait(this,
                Internal.PromiseRefBase.DelegateWrapper.Create(resolveCaptureValue, onResolved),
                Internal.PromiseRefBase.DelegateWrapper.Create(rejectCaptureValue, onRejected));
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled, the new <see cref="Promise"/> will be canceled.
        /// </summary>
        public Promise Then<TCaptureResolve>(TCaptureResolve resolveCaptureValue, Action<TCaptureResolve, T> onResolved, Func<Promise> onRejected)
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, nameof(onResolved), 1);
            ValidateArgument(onRejected, nameof(onRejected), 1);

            return Internal.PromiseRefBase.CallbackHelperArg<T>.AddResolveRejectWait(this,
                Internal.PromiseRefBase.DelegateWrapper.Create(resolveCaptureValue, onResolved),
                Internal.PromiseRefBase.DelegateWrapper.Create(onRejected));
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with and the resolve value, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled, the new <see cref="Promise"/> will be canceled.
        /// </summary>
        public Promise Then<TCaptureReject>(Action<T> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise> onRejected)
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, nameof(onResolved), 1);
            ValidateArgument(onRejected, nameof(onRejected), 1);

            return Internal.PromiseRefBase.CallbackHelperArg<T>.AddResolveRejectWait(this,
                Internal.PromiseRefBase.DelegateWrapper.Create(onResolved),
                Internal.PromiseRefBase.DelegateWrapper.Create(rejectCaptureValue, onRejected));
        }

        /// <summary>
        /// Capture 2 values and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled, the new <see cref="Promise"/> will be canceled.
        /// </summary>
        public Promise Then<TCaptureResolve, TCaptureReject>(TCaptureResolve resolveCaptureValue, Action<TCaptureResolve, T> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise> onRejected)
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, nameof(onResolved), 1);
            ValidateArgument(onRejected, nameof(onRejected), 1);

            return Internal.PromiseRefBase.CallbackHelperArg<T>.AddResolveRejectWait(this,
                Internal.PromiseRefBase.DelegateWrapper.Create(resolveCaptureValue, onResolved),
                Internal.PromiseRefBase.DelegateWrapper.Create(rejectCaptureValue, onRejected));
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is assignable to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled, the new <see cref="Promise"/> will be canceled.
        /// </summary>
        public Promise Then<TCaptureResolve, TReject>(TCaptureResolve resolveCaptureValue, Action<TCaptureResolve, T> onResolved, Func<TReject, Promise> onRejected)
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, nameof(onResolved), 1);
            ValidateArgument(onRejected, nameof(onRejected), 1);

            return Internal.PromiseRefBase.CallbackHelperArg<T>.AddResolveRejectWait(this,
                Internal.PromiseRefBase.DelegateWrapper.Create(resolveCaptureValue, onResolved),
                Internal.PromiseRefBase.DelegateWrapper.Create(onRejected));
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with and the resolve value, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is assignable to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled, the new <see cref="Promise"/> will be canceled.
        /// </summary>
        public Promise Then<TCaptureReject, TReject>(Action<T> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise> onRejected)
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, nameof(onResolved), 1);
            ValidateArgument(onRejected, nameof(onRejected), 1);

            return Internal.PromiseRefBase.CallbackHelperArg<T>.AddResolveRejectWait(this,
                Internal.PromiseRefBase.DelegateWrapper.Create(onResolved),
                Internal.PromiseRefBase.DelegateWrapper.Create(rejectCaptureValue, onRejected));
        }

        /// <summary>
        /// Capture 2 values and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is assignable to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled, the new <see cref="Promise"/> will be canceled.
        /// </summary>
        public Promise Then<TCaptureResolve, TCaptureReject, TReject>(TCaptureResolve resolveCaptureValue, Action<TCaptureResolve, T> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise> onRejected)
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, nameof(onResolved), 1);
            ValidateArgument(onRejected, nameof(onRejected), 1);

            return Internal.PromiseRefBase.CallbackHelperArg<T>.AddResolveRejectWait(this,
                Internal.PromiseRefBase.DelegateWrapper.Create(resolveCaptureValue, onResolved),
                Internal.PromiseRefBase.DelegateWrapper.Create(rejectCaptureValue, onRejected));
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="TResult"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled, the new <see cref="Promise{T}"/> will be canceled.
        /// </summary>
        public Promise<TResult> Then<TCaptureResolve, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, TResult> onResolved, Func<Promise<TResult>> onRejected)
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, nameof(onResolved), 1);
            ValidateArgument(onRejected, nameof(onRejected), 1);

            return Internal.PromiseRefBase.CallbackHelper<T, TResult>.AddResolveRejectWait(this,
                Internal.PromiseRefBase.DelegateWrapper.Create(resolveCaptureValue, onResolved),
                Internal.PromiseRefBase.DelegateWrapper.Create(onRejected));
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="TResult"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled, the new <see cref="Promise{T}"/> will be canceled.
        /// </summary>
        public Promise<TResult> Then<TCaptureReject, TResult>(Func<T, TResult> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise<TResult>> onRejected)
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, nameof(onResolved), 1);
            ValidateArgument(onRejected, nameof(onRejected), 1);

            return Internal.PromiseRefBase.CallbackHelper<T, TResult>.AddResolveRejectWait(this,
                Internal.PromiseRefBase.DelegateWrapper.Create(onResolved),
                Internal.PromiseRefBase.DelegateWrapper.Create(rejectCaptureValue, onRejected));
        }

        /// <summary>
        /// Capture 2 values and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="TResult"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled, the new <see cref="Promise{T}"/> will be canceled.
        /// </summary>
        public Promise<TResult> Then<TCaptureResolve, TCaptureReject, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, TResult> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise<TResult>> onRejected)
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, nameof(onResolved), 1);
            ValidateArgument(onRejected, nameof(onRejected), 1);

            return Internal.PromiseRefBase.CallbackHelper<T, TResult>.AddResolveRejectWait(this,
                Internal.PromiseRefBase.DelegateWrapper.Create(resolveCaptureValue, onResolved),
                Internal.PromiseRefBase.DelegateWrapper.Create(rejectCaptureValue, onRejected));
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="TResult"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is assignable to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled, the new <see cref="Promise{T}"/> will be canceled.
        /// </summary>
        public Promise<TResult> Then<TCaptureResolve, TResult, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, TResult> onResolved, Func<TReject, Promise<TResult>> onRejected)
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, nameof(onResolved), 1);
            ValidateArgument(onRejected, nameof(onRejected), 1);

            return Internal.PromiseRefBase.CallbackHelper<T, TResult>.AddResolveRejectWait(this,
                Internal.PromiseRefBase.DelegateWrapper.Create(resolveCaptureValue, onResolved),
                Internal.PromiseRefBase.DelegateWrapper.Create(onRejected));
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="TResult"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is assignable to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled, the new <see cref="Promise{T}"/> will be canceled.
        /// </summary>
        public Promise<TResult> Then<TCaptureReject, TResult, TReject>(Func<T, TResult> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise<TResult>> onRejected)
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, nameof(onResolved), 1);
            ValidateArgument(onRejected, nameof(onRejected), 1);

            return Internal.PromiseRefBase.CallbackHelper<T, TResult>.AddResolveRejectWait(this,
                Internal.PromiseRefBase.DelegateWrapper.Create(onResolved),
                Internal.PromiseRefBase.DelegateWrapper.Create(rejectCaptureValue, onRejected));
        }

        /// <summary>
        /// Capture 2 values and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="TResult"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is assignable to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled, the new <see cref="Promise{T}"/> will be canceled.
        /// </summary>
        public Promise<TResult> Then<TCaptureResolve, TCaptureReject, TResult, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, TResult> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise<TResult>> onRejected)
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, nameof(onResolved), 1);
            ValidateArgument(onRejected, nameof(onRejected), 1);

            return Internal.PromiseRefBase.CallbackHelper<T, TResult>.AddResolveRejectWait(this,
                Internal.PromiseRefBase.DelegateWrapper.Create(resolveCaptureValue, onResolved),
                Internal.PromiseRefBase.DelegateWrapper.Create(rejectCaptureValue, onRejected));
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled, the new <see cref="Promise"/> will be canceled.
        /// </summary>
        public Promise Then<TCaptureResolve>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise> onResolved, Action onRejected)
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, nameof(onResolved), 1);
            ValidateArgument(onRejected, nameof(onRejected), 1);

            return Internal.PromiseRefBase.CallbackHelperArg<T>.AddResolveRejectWait(this,
                Internal.PromiseRefBase.DelegateWrapper.Create(resolveCaptureValue, onResolved),
                Internal.PromiseRefBase.DelegateWrapper.Create(onRejected));
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled, the new <see cref="Promise"/> will be canceled.
        /// </summary>
        public Promise Then<TCaptureReject>(Func<T, Promise> onResolved, TCaptureReject rejectCaptureValue, Action<TCaptureReject> onRejected)
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, nameof(onResolved), 1);
            ValidateArgument(onRejected, nameof(onRejected), 1);

            return Internal.PromiseRefBase.CallbackHelperArg<T>.AddResolveRejectWait(this,
                Internal.PromiseRefBase.DelegateWrapper.Create(onResolved),
                Internal.PromiseRefBase.DelegateWrapper.Create(rejectCaptureValue, onRejected));
        }

        /// <summary>
        /// Capture 2 values and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled, the new <see cref="Promise"/> will be canceled.
        /// </summary>
        public Promise Then<TCaptureResolve, TCaptureReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise> onResolved, TCaptureReject rejectCaptureValue, Action<TCaptureReject> onRejected)
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, nameof(onResolved), 1);
            ValidateArgument(onRejected, nameof(onRejected), 1);

            return Internal.PromiseRefBase.CallbackHelperArg<T>.AddResolveRejectWait(this,
                Internal.PromiseRefBase.DelegateWrapper.Create(resolveCaptureValue, onResolved),
                Internal.PromiseRefBase.DelegateWrapper.Create(rejectCaptureValue, onRejected));
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is assignable to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled, the new <see cref="Promise"/> will be canceled.
        /// </summary>
        public Promise Then<TCaptureResolve, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise> onResolved, Action<TReject> onRejected)
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, nameof(onResolved), 1);
            ValidateArgument(onRejected, nameof(onRejected), 1);

            return Internal.PromiseRefBase.CallbackHelperArg<T>.AddResolveRejectWait(this,
                Internal.PromiseRefBase.DelegateWrapper.Create(resolveCaptureValue, onResolved),
                Internal.PromiseRefBase.DelegateWrapper.Create(onRejected));
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is assignable to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled, the new <see cref="Promise"/> will be canceled.
        /// </summary>
        public Promise Then<TCaptureReject, TReject>(Func<T, Promise> onResolved, TCaptureReject rejectCaptureValue, Action<TCaptureReject, TReject> onRejected)
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, nameof(onResolved), 1);
            ValidateArgument(onRejected, nameof(onRejected), 1);

            return Internal.PromiseRefBase.CallbackHelperArg<T>.AddResolveRejectWait(this,
                Internal.PromiseRefBase.DelegateWrapper.Create(onResolved),
                Internal.PromiseRefBase.DelegateWrapper.Create(rejectCaptureValue, onRejected));
        }

        /// <summary>
        /// Capture 2 values and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is assignable to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled, the new <see cref="Promise"/> will be canceled.
        /// </summary>
        public Promise Then<TCaptureResolve, TCaptureReject, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise> onResolved, TCaptureReject rejectCaptureValue, Action<TCaptureReject, TReject> onRejected)
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, nameof(onResolved), 1);
            ValidateArgument(onRejected, nameof(onRejected), 1);

            return Internal.PromiseRefBase.CallbackHelperArg<T>.AddResolveRejectWait(this,
                Internal.PromiseRefBase.DelegateWrapper.Create(resolveCaptureValue, onResolved),
                Internal.PromiseRefBase.DelegateWrapper.Create(rejectCaptureValue, onRejected));
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="TResult"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled, the new <see cref="Promise{T}"/> will be canceled.
        /// </summary>
        public Promise<TResult> Then<TCaptureResolve, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise<TResult>> onResolved, Func<TResult> onRejected)
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, nameof(onResolved), 1);
            ValidateArgument(onRejected, nameof(onRejected), 1);

            return Internal.PromiseRefBase.CallbackHelper<T, TResult>.AddResolveRejectWait(this,
                Internal.PromiseRefBase.DelegateWrapper.Create(resolveCaptureValue, onResolved),
                Internal.PromiseRefBase.DelegateWrapper.Create(onRejected));
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="TResult"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled, the new <see cref="Promise{T}"/> will be canceled.
        /// </summary>
        public Promise<TResult> Then<TCaptureReject, TResult>(Func<T, Promise<TResult>> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TResult> onRejected)
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, nameof(onResolved), 1);
            ValidateArgument(onRejected, nameof(onRejected), 1);

            return Internal.PromiseRefBase.CallbackHelper<T, TResult>.AddResolveRejectWait(this,
                Internal.PromiseRefBase.DelegateWrapper.Create(onResolved),
                Internal.PromiseRefBase.DelegateWrapper.Create(rejectCaptureValue, onRejected));
        }

        /// <summary>
        /// Capture 2 values and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="TResult"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled, the new <see cref="Promise{T}"/> will be canceled.
        /// </summary>
        public Promise<TResult> Then<TCaptureResolve, TCaptureReject, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise<TResult>> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TResult> onRejected)
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, nameof(onResolved), 1);
            ValidateArgument(onRejected, nameof(onRejected), 1);

            return Internal.PromiseRefBase.CallbackHelper<T, TResult>.AddResolveRejectWait(this,
                Internal.PromiseRefBase.DelegateWrapper.Create(resolveCaptureValue, onResolved),
                Internal.PromiseRefBase.DelegateWrapper.Create(rejectCaptureValue, onRejected));
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="TResult"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is assignable to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled, the new <see cref="Promise{T}"/> will be canceled.
        /// </summary>
        public Promise<TResult> Then<TCaptureResolve, TResult, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise<TResult>> onResolved, Func<TReject, TResult> onRejected)
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, nameof(onResolved), 1);
            ValidateArgument(onRejected, nameof(onRejected), 1);

            return Internal.PromiseRefBase.CallbackHelper<T, TResult>.AddResolveRejectWait(this,
                Internal.PromiseRefBase.DelegateWrapper.Create(resolveCaptureValue, onResolved),
                Internal.PromiseRefBase.DelegateWrapper.Create(onRejected));
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="TResult"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is assignable to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled, the new <see cref="Promise{T}"/> will be canceled.
        /// </summary>
        public Promise<TResult> Then<TCaptureReject, TResult, TReject>(Func<T, Promise<TResult>> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, TResult> onRejected)
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, nameof(onResolved), 1);
            ValidateArgument(onRejected, nameof(onRejected), 1);

            return Internal.PromiseRefBase.CallbackHelper<T, TResult>.AddResolveRejectWait(this,
                Internal.PromiseRefBase.DelegateWrapper.Create(onResolved),
                Internal.PromiseRefBase.DelegateWrapper.Create(rejectCaptureValue, onRejected));
        }

        /// <summary>
        /// Capture 2 values and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="TResult"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is assignable to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled, the new <see cref="Promise{T}"/> will be canceled.
        /// </summary>
        public Promise<TResult> Then<TCaptureResolve, TCaptureReject, TResult, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise<TResult>> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, TResult> onRejected)
        {
            ValidateOperation(1);
            ValidateArgument(onResolved, nameof(onResolved), 1);
            ValidateArgument(onRejected, nameof(onRejected), 1);

            return Internal.PromiseRefBase.CallbackHelper<T, TResult>.AddResolveRejectWait(this,
                Internal.PromiseRefBase.DelegateWrapper.Create(resolveCaptureValue, onResolved),
                Internal.PromiseRefBase.DelegateWrapper.Create(rejectCaptureValue, onRejected));
        }
        #endregion

        #region Continue Callbacks
        /// <summary>
        /// Capture a value and add a continuation callback. Returns a new <see cref="Promise"/>.
        /// <para/>When this is resolved, rejected, or canceled, <paramref name="onContinue"/> will be invoked with <paramref name="continueCaptureValue"/> and the <see cref="ResultContainer"/>, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// </summary>
        public Promise ContinueWith<TCapture>(TCapture continueCaptureValue, Action<TCapture, ResultContainer> onContinue)
        {
            ValidateOperation(1);
            ValidateArgument(onContinue, nameof(onContinue), 1);

            return Internal.PromiseRefBase.CallbackHelperArg<T>.ContinueWith(this,
                ContinuerArg<T>.ContinueWith(DelegateWrapper.Create(continueCaptureValue, onContinue))
            );
        }

        /// <summary>
        /// Capture a value and add a continuation callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="TResult"/>.
        /// <para/>When this is resolved, rejected, or canceled, <paramref name="onContinue"/> will be invoked with <paramref name="continueCaptureValue"/> and the <see cref="ResultContainer"/>, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// </summary>
        public Promise<TResult> ContinueWith<TCapture, TResult>(TCapture continueCaptureValue, Func<TCapture, ResultContainer, TResult> onContinue)
        {
            ValidateOperation(1);
            ValidateArgument(onContinue, nameof(onContinue), 1);

            return Internal.PromiseRefBase.CallbackHelper<T, TResult>.ContinueWith(this,
                Continuer<T, TResult>.ContinueWith(DelegateWrapper.Create(continueCaptureValue, onContinue))
            );
        }

        /// <summary>
        /// Capture a value and add a continuation callback. Returns a new <see cref="Promise"/>.
        /// <para/>When this is resolved, rejected, or canceled, <paramref name="onContinue"/> will be invoked with <paramref name="continueCaptureValue"/> and the <see cref="ResultContainer"/>, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// </summary>
        public Promise ContinueWith<TCapture>(TCapture continueCaptureValue, Func<TCapture, ResultContainer, Promise> onContinue)
        {
            ValidateOperation(1);
            ValidateArgument(onContinue, nameof(onContinue), 1);

            return Internal.PromiseRefBase.CallbackHelperArg<T>.ContinueWithWait(this,
                ContinuerArg<T>.ContinueWithWait(DelegateWrapper.Create(continueCaptureValue, onContinue))
            );
        }

        /// <summary>
        /// Add a continuation callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="TResult"/>.
        /// <para/>When this is resolved, rejected, or canceled, <paramref name="onContinue"/> will be invoked with <paramref name="continueCaptureValue"/> and the <see cref="ResultContainer"/>, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// </summary>
        public Promise<TResult> ContinueWith<TCapture, TResult>(TCapture continueCaptureValue, Func<TCapture, ResultContainer, Promise<TResult>> onContinue)
        {
            ValidateOperation(1);
            ValidateArgument(onContinue, nameof(onContinue), 1);

            return Internal.PromiseRefBase.CallbackHelper<T, TResult>.ContinueWithWait(this,
                Continuer<T, TResult>.ContinueWithWait(DelegateWrapper.Create(continueCaptureValue, onContinue))
            );
        }

        /// <summary>
        /// Capture a value and add a continuation callback. Returns a new <see cref="Promise"/>.
        /// <para/>When this is resolved, rejected, or canceled, <paramref name="onContinue"/> will be invoked with <paramref name="continueCaptureValue"/> and the <see cref="ResultContainer"/>, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled, and <paramref name="onContinue"/> will not be invoked.
        /// </summary>
        public Promise ContinueWith<TCapture>(TCapture continueCaptureValue, Action<TCapture, ResultContainer> onContinue, CancelationToken cancelationToken = default)
        {
            ValidateOperation(1);
            ValidateArgument(onContinue, nameof(onContinue), 1);

            return Internal.PromiseRefBase.CallbackHelperArg<T>.ContinueWith(this,
                ContinuerArg<T>.ContinueWith(DelegateWrapper.Create(continueCaptureValue, onContinue)),
                cancelationToken
            );
        }

        /// <summary>
        /// Capture a value and add a continuation callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="TResult"/>.
        /// <para/>When this is resolved, rejected, or canceled, <paramref name="onContinue"/> will be invoked with <paramref name="continueCaptureValue"/> and the <see cref="ResultContainer"/>, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled, and <paramref name="onContinue"/> will not be invoked.
        /// </summary>
        public Promise<TResult> ContinueWith<TCapture, TResult>(TCapture continueCaptureValue, Func<TCapture, ResultContainer, TResult> onContinue, CancelationToken cancelationToken = default)
        {
            ValidateOperation(1);
            ValidateArgument(onContinue, nameof(onContinue), 1);

            return Internal.PromiseRefBase.CallbackHelper<T, TResult>.ContinueWith(this,
                Continuer<T, TResult>.ContinueWith(DelegateWrapper.Create(continueCaptureValue, onContinue)),
                cancelationToken
            );
        }

        /// <summary>
        /// Capture a value and add a continuation callback. Returns a new <see cref="Promise"/>.
        /// <para/>When this is resolved, rejected, or canceled, <paramref name="onContinue"/> will be invoked with <paramref name="continueCaptureValue"/> and the <see cref="ResultContainer"/>, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled, and <paramref name="onContinue"/> will not be invoked.
        /// </summary>
        public Promise ContinueWith<TCapture>(TCapture continueCaptureValue, Func<TCapture, ResultContainer, Promise> onContinue, CancelationToken cancelationToken = default)
        {
            ValidateOperation(1);
            ValidateArgument(onContinue, nameof(onContinue), 1);

            return Internal.PromiseRefBase.CallbackHelperArg<T>.ContinueWithWait(this,
                ContinuerArg<T>.ContinueWithWait(DelegateWrapper.Create(continueCaptureValue, onContinue)),
                cancelationToken
            );
        }

        /// <summary>
        /// Add a continuation callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="TResult"/>.
        /// <para/>When this is resolved, rejected, or canceled, <paramref name="onContinue"/> will be invoked with <paramref name="continueCaptureValue"/> and the <see cref="ResultContainer"/>, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled, and <paramref name="onContinue"/> will not be invoked.
        /// </summary>
        public Promise<TResult> ContinueWith<TCapture, TResult>(TCapture continueCaptureValue, Func<TCapture, ResultContainer, Promise<TResult>> onContinue, CancelationToken cancelationToken = default)
        {
            ValidateOperation(1);
            ValidateArgument(onContinue, nameof(onContinue), 1);

            return Internal.PromiseRefBase.CallbackHelper<T, TResult>.ContinueWithWait(this,
                Continuer<T, TResult>.ContinueWithWait(DelegateWrapper.Create(continueCaptureValue, onContinue)),
                cancelationToken
            );
        }
        #endregion
    }

    // Inherited from Promise (must copy since structs cannot inherit).
    // Did not copy Finally or ContinueWith.
    partial struct Promise<T>
    {
        /// <inheritdoc cref="Promise.CatchCancelation(Action)"/>
        [MethodImpl(Internal.InlineOption)]
        public Promise CatchCancelation(Action onCanceled)
            => AsPromise().CatchCancelation(onCanceled);

        /// <inheritdoc cref="Promise.CatchCancelation(Func{Promise})"/>
        [MethodImpl(Internal.InlineOption)]
        public Promise CatchCancelation(Func<Promise> onCanceled)
            => AsPromise().CatchCancelation(onCanceled);

        #region Resolve Callbacks
        /// <inheritdoc cref="Promise.Then(Action)"/>
        [MethodImpl(Internal.InlineOption)]
        public Promise Then(Action onResolved)
            => AsPromise().Then(onResolved);

        /// <inheritdoc cref="Promise.Then{TResult}(Func{TResult})"/>
        [MethodImpl(Internal.InlineOption)]
        public Promise<TResult> Then<TResult>(Func<TResult> onResolved)
            => AsPromise().Then(onResolved);

        /// <inheritdoc cref="Promise.Then(Func{Promise})"/>
        [MethodImpl(Internal.InlineOption)]
        public Promise Then(Func<Promise> onResolved)
            => AsPromise().Then(onResolved);

        /// <inheritdoc cref="Promise.Then{TResult}(Func{Promise{TResult}})"/>
        [MethodImpl(Internal.InlineOption)]
        public Promise<TResult> Then<TResult>(Func<Promise<TResult>> onResolved)
            => AsPromise().Then(onResolved);
        #endregion

        #region Reject Callbacks
        /// <inheritdoc cref="Promise.Catch(Action)"/>
        [MethodImpl(Internal.InlineOption)]
        public Promise Catch(Action onRejected)
            => AsPromise().Catch(onRejected);

        /// <inheritdoc cref="Promise.Catch{TReject}(Action{TReject})"/>
        [MethodImpl(Internal.InlineOption)]
        public Promise Catch<TReject>(Action<TReject> onRejected)
            => AsPromise().Catch(onRejected);

        /// <inheritdoc cref="Promise.Catch(Func{Promise})"/>
        [MethodImpl(Internal.InlineOption)]
        public Promise Catch(Func<Promise> onRejected)
            => AsPromise().Catch(onRejected);

        /// <inheritdoc cref="Promise.Catch{TReject}(Func{TReject, Promise})"/>
        [MethodImpl(Internal.InlineOption)]
        public Promise Catch<TReject>(Func<TReject, Promise> onRejected)
            => AsPromise().Catch(onRejected);
        #endregion

        #region Resolve or Reject Callbacks
        /// <inheritdoc cref="Promise.Then(Action, Action)"/>
        [MethodImpl(Internal.InlineOption)]
        public Promise Then(Action onResolved, Action onRejected)
            => AsPromise().Then(onResolved, onRejected);

        /// <inheritdoc cref="Promise.Then{TReject}(Action, Action{TReject})"/>
        [MethodImpl(Internal.InlineOption)]
        public Promise Then<TReject>(Action onResolved, Action<TReject> onRejected)
            => AsPromise().Then(onResolved, onRejected);

        /// <inheritdoc cref="Promise.Then{TResult}(Func{TResult}, Func{TResult})"/>
        [MethodImpl(Internal.InlineOption)]
        public Promise<TResult> Then<TResult>(Func<TResult> onResolved, Func<TResult> onRejected)
            => AsPromise().Then(onResolved, onRejected);

        /// <inheritdoc cref="Promise.Then{TResult, TReject}(Func{TResult}, Func{TReject, TResult})"/>
        [MethodImpl(Internal.InlineOption)]
        public Promise<TResult> Then<TResult, TReject>(Func<TResult> onResolved, Func<TReject, TResult> onRejected)
            => AsPromise().Then(onResolved, onRejected);

        /// <inheritdoc cref="Promise.Then(Func{Promise}, Func{Promise})"/>
        [MethodImpl(Internal.InlineOption)]
        public Promise Then(Func<Promise> onResolved, Func<Promise> onRejected)
            => AsPromise().Then(onResolved, onRejected);

        /// <inheritdoc cref="Promise.Then{TReject}(Func{Promise}, Func{TReject, Promise})"/>
        [MethodImpl(Internal.InlineOption)]
        public Promise Then<TReject>(Func<Promise> onResolved, Func<TReject, Promise> onRejected)
            => AsPromise().Then(onResolved, onRejected);

        /// <inheritdoc cref="Promise.Then{TResult}(Func{Promise{TResult}}, Func{Promise{TResult}})"/>
        [MethodImpl(Internal.InlineOption)]
        public Promise<TResult> Then<TResult>(Func<Promise<TResult>> onResolved, Func<Promise<TResult>> onRejected)
            => AsPromise().Then(onResolved, onRejected);

        /// <inheritdoc cref="Promise.Then{TResult, TReject}(Func{Promise{TResult}}, Func{TReject, Promise{TResult}})"/>
        [MethodImpl(Internal.InlineOption)]
        public Promise<TResult> Then<TResult, TReject>(Func<Promise<TResult>> onResolved, Func<TReject, Promise<TResult>> onRejected)
            => AsPromise().Then(onResolved, onRejected);

        /// <inheritdoc cref="Promise.Then(Action, Func{Promise})"/>
        [MethodImpl(Internal.InlineOption)]
        public Promise Then(Action onResolved, Func<Promise> onRejected)
            => AsPromise().Then(onResolved, onRejected);

        /// <inheritdoc cref="Promise.Then{TReject}(Action, Func{TReject, Promise})"/>
        [MethodImpl(Internal.InlineOption)]
        public Promise Then<TReject>(Action onResolved, Func<TReject, Promise> onRejected)
            => AsPromise().Then(onResolved, onRejected);

        /// <inheritdoc cref="Promise.Then{TResult}(Func{TResult}, Func{Promise{TResult}})"/>
        [MethodImpl(Internal.InlineOption)]
        public Promise<TResult> Then<TResult>(Func<TResult> onResolved, Func<Promise<TResult>> onRejected)
            => AsPromise().Then(onResolved, onRejected);

        /// <inheritdoc cref="Promise.Then{TResult, TReject}(Func{TResult}, Func{TReject, Promise{TResult}})"/>
        [MethodImpl(Internal.InlineOption)]
        public Promise<TResult> Then<TResult, TReject>(Func<TResult> onResolved, Func<TReject, Promise<TResult>> onRejected)
            => AsPromise().Then(onResolved, onRejected);

        /// <inheritdoc cref="Promise.Then(Func{Promise}, Action)"/>
        [MethodImpl(Internal.InlineOption)]
        public Promise Then(Func<Promise> onResolved, Action onRejected)
            => AsPromise().Then(onResolved, onRejected);

        /// <inheritdoc cref="Promise.Then{TReject}(Func{Promise}, Action{TReject})"/>
        [MethodImpl(Internal.InlineOption)]
        public Promise Then<TReject>(Func<Promise> onResolved, Action<TReject> onRejected)
            => AsPromise().Then(onResolved, onRejected);

        /// <inheritdoc cref="Promise.Then{TResult}(Func{Promise{TResult}}, Func{TResult})"/>
        [MethodImpl(Internal.InlineOption)]
        public Promise<TResult> Then<TResult>(Func<Promise<TResult>> onResolved, Func<TResult> onRejected)
            => AsPromise().Then(onResolved, onRejected);

        /// <inheritdoc cref="Promise.Then{TResult, TReject}(Func{Promise{TResult}}, Func{TReject, TResult})"/>
        [MethodImpl(Internal.InlineOption)]
        public Promise<TResult> Then<TResult, TReject>(Func<Promise<TResult>> onResolved, Func<TReject, TResult> onRejected)
            => AsPromise().Then(onResolved, onRejected);
        #endregion

        // Capture values below.

        /// <inheritdoc cref="Promise.CatchCancelation{TCaptureCancel}(TCaptureCancel, Action{TCaptureCancel})"/>
        [MethodImpl(Internal.InlineOption)]
        public Promise CatchCancelation<TCaptureCancel>(TCaptureCancel cancelCaptureValue, Action<TCaptureCancel> onCanceled)
            => AsPromise().CatchCancelation(cancelCaptureValue, onCanceled);

        /// <inheritdoc cref="Promise.CatchCancelation{TCaptureCancel}(TCaptureCancel, Func{TCaptureCancel, Promise})"/>
        [MethodImpl(Internal.InlineOption)]
        public Promise CatchCancelation<TCaptureCancel>(TCaptureCancel cancelCaptureValue, Func<TCaptureCancel, Promise> onCanceled)
            => AsPromise().CatchCancelation(cancelCaptureValue, onCanceled);

        #region Resolve Callbacks
        /// <inheritdoc cref="Promise.Then{TCaptureResolve}(TCaptureResolve, Action{TCaptureResolve})"/>
        [MethodImpl(Internal.InlineOption)]
        public Promise Then<TCaptureResolve>(TCaptureResolve resolveCaptureValue, Action<TCaptureResolve> onResolved)
            => AsPromise().Then(resolveCaptureValue, onResolved);

        /// <inheritdoc cref="Promise.Then{TCaptureResolve, TResult}(TCaptureResolve, Func{TCaptureResolve, TResult})"/>
        [MethodImpl(Internal.InlineOption)]
        public Promise<TResult> Then<TCaptureResolve, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, TResult> onResolved)
            => AsPromise().Then(resolveCaptureValue, onResolved);

        /// <inheritdoc cref="Promise.Then{TCaptureResolve}(TCaptureResolve, Func{TCaptureResolve, Promise})"/>
        [MethodImpl(Internal.InlineOption)]
        public Promise Then<TCaptureResolve>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise> onResolved)
            => AsPromise().Then(resolveCaptureValue, onResolved);

        /// <inheritdoc cref="Promise.Then{TCaptureResolve, TResult}(TCaptureResolve, Func{TCaptureResolve, Promise{TResult}})"/>
        [MethodImpl(Internal.InlineOption)]
        public Promise<TResult> Then<TCaptureResolve, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise<TResult>> onResolved)
            => AsPromise().Then(resolveCaptureValue, onResolved);
        #endregion

        #region Reject Callbacks
        /// <inheritdoc cref="Promise.Catch{TCaptureReject}(TCaptureReject, Action{TCaptureReject})"/>
        [MethodImpl(Internal.InlineOption)]
        public Promise Catch<TCaptureReject>(TCaptureReject rejectCaptureValue, Action<TCaptureReject> onRejected)
            => AsPromise().Catch(rejectCaptureValue, onRejected);

        /// <inheritdoc cref="Promise.Catch{TCaptureReject, TReject}(TCaptureReject, Action{TCaptureReject, TReject})"/>
        [MethodImpl(Internal.InlineOption)]
        public Promise Catch<TCaptureReject, TReject>(TCaptureReject rejectCaptureValue, Action<TCaptureReject, TReject> onRejected)
            => AsPromise().Catch(rejectCaptureValue, onRejected);

        /// <inheritdoc cref="Promise.Catch{TCaptureReject}(TCaptureReject, Func{TCaptureReject, Promise})"/>
        [MethodImpl(Internal.InlineOption)]
        public Promise Catch<TCaptureReject>(TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise> onRejected)
            => AsPromise().Catch(rejectCaptureValue, onRejected);

        /// <inheritdoc cref="Promise.Catch{TCaptureReject, TReject}(TCaptureReject, Func{TCaptureReject, TReject, Promise})"/>
        [MethodImpl(Internal.InlineOption)]
        public Promise Catch<TCaptureReject, TReject>(TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise> onRejected)
            => AsPromise().Catch(rejectCaptureValue, onRejected);
        #endregion

        #region Resolve or Reject Callbacks
        /// <inheritdoc cref="Promise.Then{TCaptureResolve}(TCaptureResolve, Action{TCaptureResolve}, Action)"/>
        [MethodImpl(Internal.InlineOption)]
        public Promise Then<TCaptureResolve>(TCaptureResolve resolveCaptureValue, Action<TCaptureResolve> onResolved, Action onRejected)
            => AsPromise().Then(resolveCaptureValue, onResolved, onRejected);

        /// <inheritdoc cref="Promise.Then{TCaptureReject}(Action, TCaptureReject, Action{TCaptureReject})"/>
        [MethodImpl(Internal.InlineOption)]
        public Promise Then<TCaptureReject>(Action onResolved, TCaptureReject rejectCaptureValue, Action<TCaptureReject> onRejected)
            => AsPromise().Then(onResolved, rejectCaptureValue, onRejected);

        /// <inheritdoc cref="Promise.Then{TCaptureResolve, TCaptureReject}(TCaptureResolve, Action{TCaptureResolve}, TCaptureReject, Action{TCaptureReject})"/>
        [MethodImpl(Internal.InlineOption)]
        public Promise Then<TCaptureResolve, TCaptureReject>(TCaptureResolve resolveCaptureValue, Action<TCaptureResolve> onResolved, TCaptureReject rejectCaptureValue, Action<TCaptureReject> onRejected)
            => AsPromise().Then(resolveCaptureValue, onResolved, rejectCaptureValue, onRejected);

        /// <inheritdoc cref="Promise.Then{TCaptureResolve, TReject}(TCaptureResolve, Action{TCaptureResolve}, Action{TReject})"/>
        [MethodImpl(Internal.InlineOption)]
        public Promise Then<TCaptureResolve, TReject>(TCaptureResolve resolveCaptureValue, Action<TCaptureResolve> onResolved, Action<TReject> onRejected)
            => AsPromise().Then(resolveCaptureValue, onResolved, onRejected);

        /// <inheritdoc cref="Promise.Then{TCaptureReject, TReject}(Action, TCaptureReject, Action{TCaptureReject, TReject})"/>
        [MethodImpl(Internal.InlineOption)]
        public Promise Then<TCaptureReject, TReject>(Action onResolved, TCaptureReject rejectCaptureValue, Action<TCaptureReject, TReject> onRejected)
            => AsPromise().Then(onResolved, rejectCaptureValue, onRejected);

        /// <inheritdoc cref="Promise.Then{TCaptureResolve, TCaptureReject, TReject}(TCaptureResolve, Action{TCaptureResolve}, TCaptureReject, Action{TCaptureReject, TReject})"/>
        [MethodImpl(Internal.InlineOption)]
        public Promise Then<TCaptureResolve, TCaptureReject, TReject>(TCaptureResolve resolveCaptureValue, Action<TCaptureResolve> onResolved, TCaptureReject rejectCaptureValue, Action<TCaptureReject, TReject> onRejected)
            => AsPromise().Then(resolveCaptureValue, onResolved, rejectCaptureValue, onRejected);

        /// <inheritdoc cref="Promise.Then{TCaptureResolve, TResult}(TCaptureResolve, Func{TCaptureResolve, TResult}, Func{TResult})"/>
        [MethodImpl(Internal.InlineOption)]
        public Promise<TResult> Then<TCaptureResolve, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, TResult> onResolved, Func<TResult> onRejected)
            => AsPromise().Then(resolveCaptureValue, onResolved, onRejected);

        /// <inheritdoc cref="Promise.Then{TCaptureReject, TResult}(Func{TResult}, TCaptureReject, Func{TCaptureReject, TResult})"/>
        [MethodImpl(Internal.InlineOption)]
        public Promise<TResult> Then<TCaptureReject, TResult>(Func<TResult> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TResult> onRejected)
            => AsPromise().Then(onResolved, rejectCaptureValue, onRejected);

        /// <inheritdoc cref="Promise.Then{TCaptureResolve, TCaptureReject, TResult}(TCaptureResolve, Func{TCaptureResolve, TResult}, TCaptureReject, Func{TCaptureReject, TResult})"/>
        [MethodImpl(Internal.InlineOption)]
        public Promise<TResult> Then<TCaptureResolve, TCaptureReject, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, TResult> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TResult> onRejected)
            => AsPromise().Then(resolveCaptureValue, onResolved, rejectCaptureValue, onRejected);

        /// <inheritdoc cref="Promise.Then{TCaptureResolve, TResult, TReject}(TCaptureResolve, Func{TCaptureResolve, TResult}, Func{TReject, TResult})"/>
        [MethodImpl(Internal.InlineOption)]
        public Promise<TResult> Then<TCaptureResolve, TResult, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, TResult> onResolved, Func<TReject, TResult> onRejected)
            => AsPromise().Then(resolveCaptureValue, onResolved, onRejected);

        /// <inheritdoc cref="Promise.Then{TCaptureReject, TResult, TReject}(Func{TResult}, TCaptureReject, Func{TCaptureReject, TReject, TResult})"/>
        [MethodImpl(Internal.InlineOption)]
        public Promise<TResult> Then<TCaptureReject, TResult, TReject>(Func<TResult> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, TResult> onRejected)
            => AsPromise().Then(onResolved, rejectCaptureValue, onRejected);

        /// <inheritdoc cref="Promise.Then{TCaptureResolve, TCaptureReject, TResult, TReject}(TCaptureResolve, Func{TCaptureResolve, TResult}, TCaptureReject, Func{TCaptureReject, TReject, TResult})"/>
        [MethodImpl(Internal.InlineOption)]
        public Promise<TResult> Then<TCaptureResolve, TCaptureReject, TResult, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, TResult> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, TResult> onRejected)
            => AsPromise().Then(resolveCaptureValue, onResolved, rejectCaptureValue, onRejected);

        /// <inheritdoc cref="Promise.Then{TCaptureResolve}(TCaptureResolve, Func{TCaptureResolve, Promise}, Func{Promise})"/>
        [MethodImpl(Internal.InlineOption)]
        public Promise Then<TCaptureResolve>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise> onResolved, Func<Promise> onRejected)
            => AsPromise().Then(resolveCaptureValue, onResolved, onRejected);

        /// <inheritdoc cref="Promise.Then{TCaptureReject}(Func{Promise}, TCaptureReject, Func{TCaptureReject, Promise})"/>
        [MethodImpl(Internal.InlineOption)]
        public Promise Then<TCaptureReject>(Func<Promise> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise> onRejected)
            => AsPromise().Then(onResolved, rejectCaptureValue, onRejected);

        /// <inheritdoc cref="Promise.Then{TCaptureResolve, TCaptureReject}(TCaptureResolve, Func{TCaptureResolve, Promise}, TCaptureReject, Func{TCaptureReject, Promise})"/>
        [MethodImpl(Internal.InlineOption)]
        public Promise Then<TCaptureResolve, TCaptureReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise> onRejected)
            => AsPromise().Then(resolveCaptureValue, onResolved, rejectCaptureValue, onRejected);

        /// <inheritdoc cref="Promise.Then{TCaptureResolve, TReject}(TCaptureResolve, Func{TCaptureResolve, Promise}, Func{TReject, Promise})"/>
        [MethodImpl(Internal.InlineOption)]
        public Promise Then<TCaptureResolve, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise> onResolved, Func<TReject, Promise> onRejected)
            => AsPromise().Then(resolveCaptureValue, onResolved, onRejected);

        /// <inheritdoc cref="Promise.Then{TCaptureReject, TReject}(Func{Promise}, TCaptureReject, Func{TCaptureReject, TReject, Promise})"/>
        [MethodImpl(Internal.InlineOption)]
        public Promise Then<TCaptureReject, TReject>(Func<Promise> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise> onRejected)
            => AsPromise().Then(onResolved, rejectCaptureValue, onRejected);

        /// <inheritdoc cref="Promise.Then{TCaptureResolve, TCaptureReject, TReject}(TCaptureResolve, Func{TCaptureResolve, Promise}, TCaptureReject, Func{TCaptureReject, TReject, Promise})"/>
        [MethodImpl(Internal.InlineOption)]
        public Promise Then<TCaptureResolve, TCaptureReject, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise> onRejected)
            => AsPromise().Then(resolveCaptureValue, onResolved, rejectCaptureValue, onRejected);

        /// <inheritdoc cref="Promise.Then{TCaptureResolve, TResult}(TCaptureResolve, Func{TCaptureResolve, Promise{TResult}}, Func{Promise{TResult}})"/>
        [MethodImpl(Internal.InlineOption)]
        public Promise<TResult> Then<TCaptureResolve, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise<TResult>> onResolved, Func<Promise<TResult>> onRejected)
            => AsPromise().Then(resolveCaptureValue, onResolved, onRejected);

        /// <inheritdoc cref="Promise.Then{TCaptureReject, TResult}(Func{Promise{TResult}}, TCaptureReject, Func{TCaptureReject, Promise{TResult}})"/>
        [MethodImpl(Internal.InlineOption)]
        public Promise<TResult> Then<TCaptureReject, TResult>(Func<Promise<TResult>> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise<TResult>> onRejected)
            => AsPromise().Then(onResolved, rejectCaptureValue, onRejected);

        /// <inheritdoc cref="Promise.Then{TCaptureResolve, TCaptureReject, TResult}(TCaptureResolve, Func{TCaptureResolve, Promise{TResult}}, TCaptureReject, Func{TCaptureReject, Promise{TResult}})"/>
        [MethodImpl(Internal.InlineOption)]
        public Promise<TResult> Then<TCaptureResolve, TCaptureReject, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise<TResult>> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise<TResult>> onRejected)
            => AsPromise().Then(resolveCaptureValue, onResolved, rejectCaptureValue, onRejected);

        /// <inheritdoc cref="Promise.Then{TCaptureResolve, TResult, TReject}(TCaptureResolve, Func{TCaptureResolve, Promise{TResult}}, Func{TReject, Promise{TResult}})"/>
        [MethodImpl(Internal.InlineOption)]
        public Promise<TResult> Then<TCaptureResolve, TResult, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise<TResult>> onResolved, Func<TReject, Promise<TResult>> onRejected)
            => AsPromise().Then(resolveCaptureValue, onResolved, onRejected);

        /// <inheritdoc cref="Promise.Then{TCaptureReject, TResult, TReject}(Func{Promise{TResult}}, TCaptureReject, Func{TCaptureReject, TReject, Promise{TResult}})"/>
        [MethodImpl(Internal.InlineOption)]
        public Promise<TResult> Then<TCaptureReject, TResult, TReject>(Func<Promise<TResult>> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise<TResult>> onRejected)
            => AsPromise().Then(onResolved, rejectCaptureValue, onRejected);

        /// <inheritdoc cref="Promise.Then{TCaptureResolve, TCaptureReject, TResult, TReject}(TCaptureResolve, Func{TCaptureResolve, Promise{TResult}}, TCaptureReject, Func{TCaptureReject, TReject, Promise{TResult}})"/>
        [MethodImpl(Internal.InlineOption)]
        public Promise<TResult> Then<TCaptureResolve, TCaptureReject, TResult, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise<TResult>> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise<TResult>> onRejected)
            => AsPromise().Then(resolveCaptureValue, onResolved, rejectCaptureValue, onRejected);

        /// <inheritdoc cref="Promise.Then{TCaptureResolve}(TCaptureResolve, Action{TCaptureResolve}, Func{Promise})"/>
        [MethodImpl(Internal.InlineOption)]
        public Promise Then<TCaptureResolve>(TCaptureResolve resolveCaptureValue, Action<TCaptureResolve> onResolved, Func<Promise> onRejected)
            => AsPromise().Then(resolveCaptureValue, onResolved, onRejected);

        /// <inheritdoc cref="Promise.Then{TCaptureReject}(Action, TCaptureReject, Func{TCaptureReject, Promise})"/>
        [MethodImpl(Internal.InlineOption)]
        public Promise Then<TCaptureReject>(Action onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise> onRejected)
            => AsPromise().Then(onResolved, rejectCaptureValue, onRejected);

        /// <inheritdoc cref="Promise.Then{TCaptureResolve, TCaptureReject}(TCaptureResolve, Action{TCaptureResolve}, TCaptureReject, Func{TCaptureReject, Promise})"/>
        [MethodImpl(Internal.InlineOption)]
        public Promise Then<TCaptureResolve, TCaptureReject>(TCaptureResolve resolveCaptureValue, Action<TCaptureResolve> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise> onRejected)
            => AsPromise().Then(resolveCaptureValue, onResolved, rejectCaptureValue, onRejected);

        /// <inheritdoc cref="Promise.Then{TCaptureResolve, TReject}(TCaptureResolve, Action{TCaptureResolve}, Func{TReject, Promise})"/>
        [MethodImpl(Internal.InlineOption)]
        public Promise Then<TCaptureResolve, TReject>(TCaptureResolve resolveCaptureValue, Action<TCaptureResolve> onResolved, Func<TReject, Promise> onRejected)
            => AsPromise().Then(resolveCaptureValue, onResolved, onRejected);

        /// <inheritdoc cref="Promise.Then{TCaptureReject, TReject}(Action, TCaptureReject, Func{TCaptureReject, TReject, Promise})"/>
        [MethodImpl(Internal.InlineOption)]
        public Promise Then<TCaptureReject, TReject>(Action onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise> onRejected)
            => AsPromise().Then(onResolved, rejectCaptureValue, onRejected);

        /// <inheritdoc cref="Promise.Then{TCaptureResolve, TCaptureReject, TReject}(TCaptureResolve, Action{TCaptureResolve}, TCaptureReject, Func{TCaptureReject, TReject, Promise})"/>
        [MethodImpl(Internal.InlineOption)]
        public Promise Then<TCaptureResolve, TCaptureReject, TReject>(TCaptureResolve resolveCaptureValue, Action<TCaptureResolve> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise> onRejected)
            => AsPromise().Then(resolveCaptureValue, onResolved, rejectCaptureValue, onRejected);

        /// <inheritdoc cref="Promise.Then{TCaptureResolve, TResult}(TCaptureResolve, Func{TCaptureResolve, TResult}, Func{Promise{TResult}})"/>
        [MethodImpl(Internal.InlineOption)]
        public Promise<TResult> Then<TCaptureResolve, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, TResult> onResolved, Func<Promise<TResult>> onRejected)
            => AsPromise().Then(resolveCaptureValue, onResolved, onRejected);

        /// <inheritdoc cref="Promise.Then{TCaptureReject, TResult}(Func{TResult}, TCaptureReject, Func{TCaptureReject, Promise{TResult}})"/>
        [MethodImpl(Internal.InlineOption)]
        public Promise<TResult> Then<TCaptureReject, TResult>(Func<TResult> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise<TResult>> onRejected)
            => AsPromise().Then(onResolved, rejectCaptureValue, onRejected);

        /// <inheritdoc cref="Promise.Then{TCaptureResolve, TCaptureReject, TResult}(TCaptureResolve, Func{TCaptureResolve, TResult}, TCaptureReject, Func{TCaptureReject, Promise{TResult}})"/>
        [MethodImpl(Internal.InlineOption)]
        public Promise<TResult> Then<TCaptureResolve, TCaptureReject, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, TResult> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise<TResult>> onRejected)
            => AsPromise().Then(resolveCaptureValue, onResolved, rejectCaptureValue, onRejected);

        /// <inheritdoc cref="Promise.Then{TCaptureResolve, TResult, TReject}(TCaptureResolve, Func{TCaptureResolve, TResult}, Func{TReject, Promise{TResult}})"/>
        [MethodImpl(Internal.InlineOption)]
        public Promise<TResult> Then<TCaptureResolve, TResult, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, TResult> onResolved, Func<TReject, Promise<TResult>> onRejected)
            => AsPromise().Then(resolveCaptureValue, onResolved, onRejected);

        /// <inheritdoc cref="Promise.Then{TCaptureReject, TResult, TReject}(Func{TResult}, TCaptureReject, Func{TCaptureReject, TReject, Promise{TResult}})"/>
        [MethodImpl(Internal.InlineOption)]
        public Promise<TResult> Then<TCaptureReject, TResult, TReject>(Func<TResult> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise<TResult>> onRejected)
            => AsPromise().Then(onResolved, rejectCaptureValue, onRejected);

        /// <inheritdoc cref="Promise.Then{TCaptureResolve, TCaptureReject, TResult, TReject}(TCaptureResolve, Func{TCaptureResolve, TResult}, TCaptureReject, Func{TCaptureReject, TReject, Promise{TResult}})"/>
        [MethodImpl(Internal.InlineOption)]
        public Promise<TResult> Then<TCaptureResolve, TCaptureReject, TResult, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, TResult> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise<TResult>> onRejected)
            => AsPromise().Then(resolveCaptureValue, onResolved, rejectCaptureValue, onRejected);

        /// <inheritdoc cref="Promise.Then{TCaptureResolve}(TCaptureResolve, Func{TCaptureResolve, Promise}, Action)"/>
        [MethodImpl(Internal.InlineOption)]
        public Promise Then<TCaptureResolve>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise> onResolved, Action onRejected)
            => AsPromise().Then(resolveCaptureValue, onResolved, onRejected);

        /// <inheritdoc cref="Promise.Then{TCaptureReject}(Func{Promise}, TCaptureReject, Action{TCaptureReject})"/>
        [MethodImpl(Internal.InlineOption)]
        public Promise Then<TCaptureReject>(Func<Promise> onResolved, TCaptureReject rejectCaptureValue, Action<TCaptureReject> onRejected)
            => AsPromise().Then(onResolved, rejectCaptureValue, onRejected);

        /// <inheritdoc cref="Promise.Then{TCaptureResolve, TCaptureReject}(TCaptureResolve, Func{TCaptureResolve, Promise}, TCaptureReject, Action{TCaptureReject})"/>
        [MethodImpl(Internal.InlineOption)]
        public Promise Then<TCaptureResolve, TCaptureReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise> onResolved, TCaptureReject rejectCaptureValue, Action<TCaptureReject> onRejected)
            => AsPromise().Then(resolveCaptureValue, onResolved, rejectCaptureValue, onRejected);

        /// <inheritdoc cref="Promise.Then{TCaptureResolve, TReject}(TCaptureResolve, Func{TCaptureResolve, Promise}, Action{TReject})"/>
        [MethodImpl(Internal.InlineOption)]
        public Promise Then<TCaptureResolve, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise> onResolved, Action<TReject> onRejected)
            => AsPromise().Then(resolveCaptureValue, onResolved, onRejected);

        /// <inheritdoc cref="Promise.Then{TCaptureReject, TReject}(Func{Promise}, TCaptureReject, Action{TCaptureReject, TReject})"/>
        [MethodImpl(Internal.InlineOption)]
        public Promise Then<TCaptureReject, TReject>(Func<Promise> onResolved, TCaptureReject rejectCaptureValue, Action<TCaptureReject, TReject> onRejected)
            => AsPromise().Then(onResolved, rejectCaptureValue, onRejected);

        /// <inheritdoc cref="Promise.Then{TCaptureResolve, TCaptureReject, TReject}(TCaptureResolve, Func{TCaptureResolve, Promise}, TCaptureReject, Action{TCaptureReject, TReject})"/>
        [MethodImpl(Internal.InlineOption)]
        public Promise Then<TCaptureResolve, TCaptureReject, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise> onResolved, TCaptureReject rejectCaptureValue, Action<TCaptureReject, TReject> onRejected)
            => AsPromise().Then(resolveCaptureValue, onResolved, rejectCaptureValue, onRejected);

        /// <inheritdoc cref="Promise.Then{TCaptureResolve, TResult}(TCaptureResolve, Func{TCaptureResolve, Promise{TResult}}, Func{TResult})"/>
        [MethodImpl(Internal.InlineOption)]
        public Promise<TResult> Then<TCaptureResolve, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise<TResult>> onResolved, Func<TResult> onRejected)
            => AsPromise().Then(resolveCaptureValue, onResolved, onRejected);

        /// <inheritdoc cref="Promise.Then{TCaptureReject, TResult}(Func{Promise{TResult}}, TCaptureReject, Func{TCaptureReject, TResult})"/>
        [MethodImpl(Internal.InlineOption)]
        public Promise<TResult> Then<TCaptureReject, TResult>(Func<Promise<TResult>> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TResult> onRejected)
            => AsPromise().Then(onResolved, rejectCaptureValue, onRejected);

        /// <inheritdoc cref="Promise.Then{TCaptureResolve, TCaptureReject, TResult}(TCaptureResolve, Func{TCaptureResolve, Promise{TResult}}, TCaptureReject, Func{TCaptureReject, TResult})"/>
        [MethodImpl(Internal.InlineOption)]
        public Promise<TResult> Then<TCaptureResolve, TCaptureReject, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise<TResult>> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TResult> onRejected)
            => AsPromise().Then(resolveCaptureValue, onResolved, rejectCaptureValue, onRejected);

        /// <inheritdoc cref="Promise.Then{TCaptureResolve, TResult, TReject}(TCaptureResolve, Func{TCaptureResolve, Promise{TResult}}, Func{TReject, TResult})"/>
        [MethodImpl(Internal.InlineOption)]
        public Promise<TResult> Then<TCaptureResolve, TResult, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise<TResult>> onResolved, Func<TReject, TResult> onRejected)
            => AsPromise().Then(resolveCaptureValue, onResolved, onRejected);

        /// <inheritdoc cref="Promise.Then{TCaptureReject, TResult, TReject}(Func{Promise{TResult}}, TCaptureReject, Func{TCaptureReject, TReject, TResult})"/>
        [MethodImpl(Internal.InlineOption)]
        public Promise<TResult> Then<TCaptureReject, TResult, TReject>(Func<Promise<TResult>> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, TResult> onRejected)
            => AsPromise().Then(onResolved, rejectCaptureValue, onRejected);

        /// <inheritdoc cref="Promise.Then{TCaptureResolve, TCaptureReject, TResult, TReject}(TCaptureResolve, Func{TCaptureResolve, Promise{TResult}}, TCaptureReject, Func{TCaptureReject, TReject, TResult})"/>
        [MethodImpl(Internal.InlineOption)]
        public Promise<TResult> Then<TCaptureResolve, TCaptureReject, TResult, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise<TResult>> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, TResult> onRejected)
            => AsPromise().Then(resolveCaptureValue, onResolved, rejectCaptureValue, onRejected);
        #endregion

        /// <summary>Returns a value indicating whether this value is equal to a specified <see cref="Promise{T}"/>.</summary>
        [MethodImpl(Internal.InlineOption)]
        public bool Equals(Promise<T> other)
            => this == other;

        /// <summary>Returns a value indicating whether this value is equal to a specified <see cref="object"/>.</summary>
        public override bool Equals(object obj)
            => obj is Promise<T> promise && Equals(promise);

        // Promises really shouldn't be used for lookups, but GetHashCode is overridden to complement ==.
        /// <summary>Returns the hash code for this instance.</summary>
        public override int GetHashCode()
            => HashCode.Combine(_ref, _id, _result);

        /// <summary>Returns a value indicating whether two <see cref="Promise{T}"/> values are equal.</summary>
        public static bool operator ==(Promise<T> lhs, Promise<T> rhs)
            => lhs._ref == rhs._ref
                & lhs._id == rhs._id
                & EqualityComparer<T>.Default.Equals(lhs._result, rhs._result);

        /// <summary>Returns a value indicating whether two <see cref="Promise{T}"/> values are not equal.</summary>
        [MethodImpl(Internal.InlineOption)]
        public static bool operator !=(Promise<T> lhs, Promise<T> rhs)
            => !(lhs == rhs);

        /// <summary>
        /// Gets the string representation of this instance.
        /// </summary>
        /// <returns>The string representation of this instance.</returns>
        public override string ToString()
        {
            var _this = this;
            string state = _this._ref == null
                ? Promise.State.Resolved.ToString()
                : _this._ref.GetIsValid(_this._id)
                ? _this._ref.State.ToString()
                : "Invalid";
            return string.Format("Type: Promise<{0}>, State: {1}", typeof(T), state);
        }
    }
}