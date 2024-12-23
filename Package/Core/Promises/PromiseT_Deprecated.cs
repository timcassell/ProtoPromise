#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#pragma warning disable IDE0090 // Use 'new(...)'

using System;
using System.ComponentModel;
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
        /// <summary>
        /// Mark this as awaited and get a new <see cref="Promise{T}"/> of <typeparamref name="T"/> that inherits the state of this and can be awaited multiple times until <see cref="Forget"/> is called on it.
        /// <para/><see cref="Forget"/> must be called when you are finished with it.
        /// <para/>NOTE: You should not return a preserved <see cref="Promise{T}"/> from a public API. Use <see cref="Duplicate"/> to get a <see cref="Promise{T}"/> that is publicly safe.
        /// </summary>
        /// <remarks>This method is obsolete. You should instead use <see cref="GetRetainer"/>.</remarks>
        [Obsolete("Prefer Promise<T>.GetRetainer()", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise<T> Preserve()
        {
            ValidateOperation(1);
            var r = _ref;
            if (r == null)
            {
                return this;
            }
            var newPromise = Internal.PromiseRefBase.PromiseMultiAwait<T>.GetOrCreateAndHookup(r, _id);
            return new Promise<T>(newPromise, newPromise.Id);
        }

        /// <summary>
        /// Mark this as awaited and get a new <see cref="Promise{T}"/> of <typeparamref name="T"/> that inherits the state of this and can be awaited once.
        /// <para/>Preserved promises are unsafe to return from public APIs. Use <see cref="Duplicate"/> to get a <see cref="Promise{T}"/> that is publicly safe.
        /// <para/><see cref="Duplicate"/> is safe to call even if you are unsure if this is preserved.
        /// </summary>
        /// <remarks>This method is obsolete. You should instead use <see cref="GetRetainer"/> with <see cref="Retainer.WaitAsync"/>.</remarks>
        [Obsolete("Prefer Promise<T>.Retainer.WaitAsync()", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise<T> Duplicate()
        {
            ValidateOperation(1);
            return Internal.PromiseRefBase.CallbackHelperResult<T>.Duplicate(this);
        }

        /// <summary>
        /// Mark this as awaited and schedule the next continuation to execute on the context of the provided <paramref name="continuationOption"/>.
        /// Returns a new <see cref="Promise{T}"/> that inherits the state of this, or will be canceled if/when the <paramref name="cancelationToken"/> is canceled before this is complete.
        /// </summary>
        /// <param name="continuationOption">Indicates on which context the next continuation will be executed.</param>
        /// <param name="forceAsync">If true, forces the next continuation to be invoked asynchronously. If <paramref name="continuationOption"/> is <see cref="SynchronizationOption.Synchronous"/>, this value will be ignored.</param>
        /// <param name="cancelationToken">If canceled before this is complete, the returned <see cref="Promise{T}"/> will be canceled, and the cancelation will propagate on the context of the provided <paramref name="continuationOption"/>.</param>
        [Obsolete("Prefer ConfigureAwait", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise<T> WaitAsync(SynchronizationOption continuationOption, bool forceAsync = false, CancelationToken cancelationToken = default)
            => WaitAsync(cancelationToken).ConfigureContinuation(new ContinuationOptions(continuationOption, forceAsync));

        /// <summary>
        /// Mark this as awaited and schedule the next continuation to execute on <paramref name="continuationContext"/>.
        /// Returns a new <see cref="Promise{T}"/> that inherits the state of this, or will be canceled if/when the <paramref name="cancelationToken"/> is canceled before this is complete.
        /// </summary>
        /// <param name="continuationContext">The context on which context the next continuation will be executed. If null, <see cref="ThreadPool.QueueUserWorkItem(WaitCallback, object)"/> will be used.</param>
        /// <param name="forceAsync">If true, forces the next continuation to be invoked asynchronously.</param>
        /// <param name="cancelationToken">If canceled before this is complete, the returned <see cref="Promise{T}"/> will be canceled, and the cancelation will propagate on the provided <paramref name="continuationContext"/>.</param>
        [Obsolete("Prefer ConfigureAwait", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise<T> WaitAsync(SynchronizationContext continuationContext, bool forceAsync = false, CancelationToken cancelationToken = default)
            => WaitAsync(cancelationToken).ConfigureContinuation(new ContinuationOptions(continuationContext, forceAsync));

        /// <summary>
        /// Add a cancel callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is canceled, <paramref name="onCanceled"/> will be invoked, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is resolved, the new <see cref="Promise{T}"/> will be resolved with the same value.
        /// <para/>If/when this is rejected with any reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled, and <paramref name="onCanceled"/> will not be invoked.
        /// </summary>
        [Obsolete("Prefer ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise<T> CatchCancelation(Func<T> onCanceled, CancelationToken cancelationToken = default)
        {
            ValidateOperation(1);
            ValidateArgument(onCanceled, nameof(onCanceled), 1);

            return ContinueWith(onCanceled,
                (callback, resultContainer) => resultContainer.State == Promise.State.Canceled ? callback.Invoke() : resultContainer.Value,
                cancelationToken);
        }

        /// <summary>
        /// Add a cancel callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is canceled, <paramref name="onCanceled"/> will be invoked, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is resolved, the new <see cref="Promise{T}"/> will be resolved with the same value.
        /// <para/>If/when this is rejected with any reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled, and <paramref name="onCanceled"/> will not be invoked.
        /// </summary>
        [Obsolete("Prefer ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise<T> CatchCancelation(Func<Promise<T>> onCanceled, CancelationToken cancelationToken = default)
        {
            ValidateOperation(1);
            ValidateArgument(onCanceled, nameof(onCanceled), 1);

            return ContinueWith(onCanceled,
                (callback, resultContainer) => resultContainer.State == Promise.State.Canceled ? callback.Invoke() : Promise.Resolved(resultContainer.Value),
                cancelationToken);
        }

#region Resolve Callbacks
        /// <summary>
        /// Add a resolve callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is rejected with any reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled, the new <see cref="Promise"/> will be canceled.
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled, and <paramref name="onResolved"/> will not be invoked.
        /// </summary>
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise Then(Action<T> onResolved, CancelationToken cancelationToken = default)
            => WaitAsync(cancelationToken).Then(onResolved);

        /// <summary>
        /// Add a resolve callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="TResult"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is rejected with any reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled, the new <see cref="Promise{T}"/> will be canceled.
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled, and <paramref name="onResolved"/> will not be invoked.
        /// </summary>
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise<TResult> Then<TResult>(Func<T, TResult> onResolved, CancelationToken cancelationToken = default)
            => WaitAsync(cancelationToken).Then(onResolved);

        /// <summary>
        /// Add a resolve callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is rejected with any reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled, the new <see cref="Promise"/> will be canceled.
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled, and <paramref name="onResolved"/> will not be invoked.
        /// </summary>
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise Then(Func<T, Promise> onResolved, CancelationToken cancelationToken = default)
            => WaitAsync(cancelationToken).Then(onResolved);

        /// <summary>
        /// Add a resolve callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="TResult"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is rejected with any reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled, the new <see cref="Promise{T}"/> will be canceled.
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled, and <paramref name="onResolved"/> will not be invoked.
        /// </summary>
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise<TResult> Then<TResult>(Func<T, Promise<TResult>> onResolved, CancelationToken cancelationToken = default)
            => WaitAsync(cancelationToken).Then(onResolved);
#endregion

#region Reject Callbacks
        /// <summary>
        /// Add a reject callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="T"/>.
        /// <para/>If/when this is resolved, the new <see cref="Promise{T}"/> will be resolved with the resolve value.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise{T}"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is canceled, the new <see cref="Promise{T}"/> will be canceled.
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled, and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        [Obsolete("Prefer WaitAsync(cancelationToken).Catch(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise<T> Catch(Func<T> onRejected, CancelationToken cancelationToken = default)
            => WaitAsync(cancelationToken).Catch(onRejected);

        /// <summary>
        /// Add a reject callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="T"/>.
        /// <para/>If/when this is resolved, the new <see cref="Promise{T}"/> will be resolved with the resolve value.
        /// <para/>If/when this is rejected with any reason that is assignable to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise{T}"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled, the new <see cref="Promise{T}"/> will be canceled.
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled, and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        [Obsolete("Prefer WaitAsync(cancelationToken).Catch(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise<T> Catch<TReject>(Func<TReject, T> onRejected, CancelationToken cancelationToken = default)
            => WaitAsync(cancelationToken).Catch(onRejected);

        /// <summary>
        /// Add a reject callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="T"/>.
        /// <para/>If/when this is resolved, the new <see cref="Promise{T}"/> will be resolved with the resolve value.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is canceled, the new <see cref="Promise{T}"/> will be canceled.
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled, and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        [Obsolete("Prefer WaitAsync(cancelationToken).Catch(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise<T> Catch(Func<Promise<T>> onRejected, CancelationToken cancelationToken = default)
            => WaitAsync(cancelationToken).Catch(onRejected);

        /// <summary>
        /// Add a reject callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="T"/>.
        /// <para/>If/when this is rejected with any reason that is assignable to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is resolved, the new <see cref="Promise{T}"/> will be resolved with the resolve value.
        /// <para/>If/when this is canceled or rejected with any other reason or no reason, the new <see cref="Promise{T}"/> will be canceled or rejected with the same reason.
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled, and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        [Obsolete("Prefer WaitAsync(cancelationToken).Catch(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise<T> Catch<TReject>(Func<TReject, Promise<T>> onRejected, CancelationToken cancelationToken = default)
            => WaitAsync(cancelationToken).Catch(onRejected);
#endregion

#region Resolve or Reject Callbacks
        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is canceled, the new <see cref="Promise"/> will be canceled.
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise Then(Action<T> onResolved, Action onRejected, CancelationToken cancelationToken = default)
            => WaitAsync(cancelationToken).Then(onResolved, onRejected);

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is rejected with any reason that is assignable to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled, the new <see cref="Promise"/> will be canceled.
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise Then<TReject>(Action<T> onResolved, Action<TReject> onRejected, CancelationToken cancelationToken = default)
            => WaitAsync(cancelationToken).Then(onResolved, onRejected);

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="TResult"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is canceled, the new <see cref="Promise{T}"/> will be canceled.
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise<TResult> Then<TResult>(Func<T, TResult> onResolved, Func<TResult> onRejected, CancelationToken cancelationToken = default)
            => WaitAsync(cancelationToken).Then(onResolved, onRejected);

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="TResult"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is rejected with any reason that is assignable to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled, the new <see cref="Promise{T}"/> will be canceled.
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise<TResult> Then<TResult, TReject>(Func<T, TResult> onResolved, Func<TReject, TResult> onRejected, CancelationToken cancelationToken = default)
            => WaitAsync(cancelationToken).Then(onResolved, onRejected);

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is canceled, the new <see cref="Promise"/> will be canceled.
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise Then(Func<T, Promise> onResolved, Func<Promise> onRejected, CancelationToken cancelationToken = default)
            => WaitAsync(cancelationToken).Then(onResolved, onRejected);

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is rejected with any reason that is assignable to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled, the new <see cref="Promise"/> will be canceled.
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise Then<TReject>(Func<T, Promise> onResolved, Func<TReject, Promise> onRejected, CancelationToken cancelationToken = default)
            => WaitAsync(cancelationToken).Then(onResolved, onRejected);

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="TResult"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is canceled, the new <see cref="Promise{T}"/> will be canceled.
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise<TResult> Then<TResult>(Func<T, Promise<TResult>> onResolved, Func<Promise<TResult>> onRejected, CancelationToken cancelationToken = default)
            => WaitAsync(cancelationToken).Then(onResolved, onRejected);

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="TResult"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is rejected with any reason that is assignable to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled, the new <see cref="Promise{T}"/> will be canceled.
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise<TResult> Then<TResult, TReject>(Func<T, Promise<TResult>> onResolved, Func<TReject, Promise<TResult>> onRejected, CancelationToken cancelationToken = default)
            => WaitAsync(cancelationToken).Then(onResolved, onRejected);

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is canceled, the new <see cref="Promise"/> will be canceled.
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise Then(Action<T> onResolved, Func<Promise> onRejected, CancelationToken cancelationToken = default)
            => WaitAsync(cancelationToken).Then(onResolved, onRejected);

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is rejected with any reason that is assignable to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled, the new <see cref="Promise"/> will be canceled.
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise Then<TReject>(Action<T> onResolved, Func<TReject, Promise> onRejected, CancelationToken cancelationToken = default)
            => WaitAsync(cancelationToken).Then(onResolved, onRejected);

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="TResult"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is canceled, the new <see cref="Promise{T}"/> will be canceled.
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise<TResult> Then<TResult>(Func<T, TResult> onResolved, Func<Promise<TResult>> onRejected, CancelationToken cancelationToken = default)
            => WaitAsync(cancelationToken).Then(onResolved, onRejected);

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="TResult"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is rejected with any reason that is assignable to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled, the new <see cref="Promise{T}"/> will be canceled.
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise<TResult> Then<TResult, TReject>(Func<T, TResult> onResolved, Func<TReject, Promise<TResult>> onRejected, CancelationToken cancelationToken = default)
            => WaitAsync(cancelationToken).Then(onResolved, onRejected);

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is canceled, the new <see cref="Promise"/> will be canceled.
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise Then(Func<T, Promise> onResolved, Action onRejected, CancelationToken cancelationToken = default)
            => WaitAsync(cancelationToken).Then(onResolved, onRejected);

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is rejected with any reason that is assignable to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled, the new <see cref="Promise"/> will be canceled.
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise Then<TReject>(Func<T, Promise> onResolved, Action<TReject> onRejected, CancelationToken cancelationToken = default)
            => WaitAsync(cancelationToken).Then(onResolved, onRejected);

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="TResult"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is canceled, the new <see cref="Promise{T}"/> will be canceled.
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise<TResult> Then<TResult>(Func<T, Promise<TResult>> onResolved, Func<TResult> onRejected, CancelationToken cancelationToken = default)
            => WaitAsync(cancelationToken).Then(onResolved, onRejected);

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="TResult"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is rejected with any reason that is assignable to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled, the new <see cref="Promise{T}"/> will be canceled.
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise<TResult> Then<TResult, TReject>(Func<T, Promise<TResult>> onResolved, Func<TReject, TResult> onRejected, CancelationToken cancelationToken = default)
            => WaitAsync(cancelationToken).Then(onResolved, onRejected);
#endregion
        
        // Capture values below.

        /// <summary>
        /// Capture a value and add a cancel callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is canceled, <paramref name="onCanceled"/> will be invoked, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is resolved, the new <see cref="Promise{T}"/> will be resolved with the same value.
        /// <para/>If/when this is rejected with any reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled, and <paramref name="onCanceled"/> will not be invoked.
        /// </summary>
        [Obsolete("Prefer ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise<T> CatchCancelation<TCaptureCancel>(TCaptureCancel cancelCaptureValue, Func<TCaptureCancel, T> onCanceled, CancelationToken cancelationToken = default)
        {
            ValidateOperation(1);
            ValidateArgument(onCanceled, nameof(onCanceled), 1);

            return ContinueWith((cancelCaptureValue, onCanceled),
                (cv, resultContainer) => resultContainer.State == Promise.State.Canceled ? cv.onCanceled.Invoke(cv.cancelCaptureValue) : resultContainer.Value,
                cancelationToken);
        }

        /// <summary>
        /// Capture a value and add a cancel callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is canceled, <paramref name="onCanceled"/> will be invoked, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is resolved, the new <see cref="Promise{T}"/> will be resolved with the same value.
        /// <para/>If/when this is rejected with any reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled, and <paramref name="onCanceled"/> will not be invoked.
        /// </summary>
        [Obsolete("Prefer ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise<T> CatchCancelation<TCaptureCancel>(TCaptureCancel cancelCaptureValue, Func<TCaptureCancel, Promise<T>> onCanceled, CancelationToken cancelationToken = default)
        {
            ValidateOperation(1);
            ValidateArgument(onCanceled, nameof(onCanceled), 1);

            return ContinueWith((cancelCaptureValue, onCanceled),
                (cv, resultContainer) => resultContainer.State == Promise.State.Canceled ? cv.onCanceled.Invoke(cv.cancelCaptureValue) : Promise.Resolved(resultContainer.Value),
                cancelationToken);
        }

#region Resolve Callbacks
        /// <summary>
        /// Capture a value and add a resolve callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled, the new <see cref="Promise"/> will be canceled.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled, and <paramref name="onResolved"/> will not be invoked.
        /// </summary>
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise Then<TCaptureResolve>(TCaptureResolve resolveCaptureValue, Action<TCaptureResolve, T> onResolved, CancelationToken cancelationToken = default)
            => WaitAsync(cancelationToken).Then(resolveCaptureValue, onResolved);

        /// <summary>
        /// Capture a value and add a resolve callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="TResult"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled, the new <see cref="Promise{T}"/> will be canceled.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled, and <paramref name="onResolved"/> will not be invoked.
        /// </summary>
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise<TResult> Then<TCaptureResolve, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, TResult> onResolved, CancelationToken cancelationToken = default)
            => WaitAsync(cancelationToken).Then(resolveCaptureValue, onResolved);

        /// <summary>
        /// Capture a value and add a resolve callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled, the new <see cref="Promise"/> will be canceled.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled, and <paramref name="onResolved"/> will not be invoked.
        /// </summary>
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise Then<TCaptureResolve>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise> onResolved, CancelationToken cancelationToken = default)
            => WaitAsync(cancelationToken).Then(resolveCaptureValue, onResolved);

        /// <summary>
        /// Capture a value and add a resolve callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="TResult"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled, the new <see cref="Promise{T}"/> will be canceled.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled, and <paramref name="onResolved"/> will not be invoked.
        /// </summary>
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise<TResult> Then<TCaptureResolve, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise<TResult>> onResolved, CancelationToken cancelationToken = default)
            => WaitAsync(cancelationToken).Then(resolveCaptureValue, onResolved);
#endregion

#region Reject Callbacks
        /// <summary>
        /// Capture a value and add a reject callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="T"/>.
        /// <para/>If/when this is resolved, the new <see cref="Promise{T}"/> will be resolved with the resolve value.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise{T}"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled, the new <see cref="Promise{T}"/> will be canceled.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled, and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise<T> Catch<TCaptureReject>(TCaptureReject rejectCaptureValue, Func<TCaptureReject, T> onRejected, CancelationToken cancelationToken = default)
            => WaitAsync(cancelationToken).Catch(rejectCaptureValue, onRejected);

        /// <summary>
        /// Capture a value and add a reject callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="T"/>.
        /// <para/>If/when this is resolved, the new <see cref="Promise{T}"/> will be resolved with the resolve value.
        /// <para/>If/when this is rejected with any reason that is assignable to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise{T}"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled, the new <see cref="Promise{T}"/> will be canceled.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled, and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise<T> Catch<TCaptureReject, TReject>(TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, T> onRejected, CancelationToken cancelationToken = default)
            => WaitAsync(cancelationToken).Catch(rejectCaptureValue, onRejected);

        /// <summary>
        /// Capture a value and add a reject callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="T"/>.
        /// <para/>If/when this is resolved, the new <see cref="Promise{T}"/> will be resolved with the resolve value.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled, the new <see cref="Promise{T}"/> will be canceled.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled, and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise<T> Catch<TCaptureReject>(TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise<T>> onRejected, CancelationToken cancelationToken = default)
            => WaitAsync(cancelationToken).Catch(rejectCaptureValue, onRejected);

        /// <summary>
        /// Capture a value and add a reject callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="T"/>.
        /// <para/>If/when this is resolved, the new <see cref="Promise{T}"/> will be resolved with the resolve value.
        /// <para/>If/when this is rejected with any reason that is assignable to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled, the new <see cref="Promise{T}"/> will be canceled.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled, and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise<T> Catch<TCaptureReject, TReject>(TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise<T>> onRejected, CancelationToken cancelationToken = default)
            => WaitAsync(cancelationToken).Catch(rejectCaptureValue, onRejected);
#endregion

#region Resolve or Reject Callbacks
        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled, the new <see cref="Promise"/> will be canceled.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise Then<TCaptureResolve>(TCaptureResolve resolveCaptureValue, Action<TCaptureResolve, T> onResolved, Action onRejected, CancelationToken cancelationToken = default)
            => WaitAsync(cancelationToken).Then(resolveCaptureValue, onResolved, onRejected);

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with and the resolve value, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled, the new <see cref="Promise"/> will be canceled.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise Then<TCaptureReject>(Action<T> onResolved, TCaptureReject rejectCaptureValue, Action<TCaptureReject> onRejected, CancelationToken cancelationToken = default)
            => WaitAsync(cancelationToken).Then(onResolved, rejectCaptureValue, onRejected);

        /// <summary>
        /// Capture 2 values and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled, the new <see cref="Promise"/> will be canceled.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise Then<TCaptureResolve, TCaptureReject>(TCaptureResolve resolveCaptureValue, Action<TCaptureResolve, T> onResolved, TCaptureReject rejectCaptureValue, Action<TCaptureReject> onRejected, CancelationToken cancelationToken = default)
            => WaitAsync(cancelationToken).Then(resolveCaptureValue, onResolved, rejectCaptureValue, onRejected);

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is assignable to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled, the new <see cref="Promise"/> will be canceled.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise Then<TCaptureResolve, TReject>(TCaptureResolve resolveCaptureValue, Action<TCaptureResolve, T> onResolved, Action<TReject> onRejected, CancelationToken cancelationToken = default)
            => WaitAsync(cancelationToken).Then(resolveCaptureValue, onResolved, onRejected);

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with and the resolve value, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is assignable to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled, the new <see cref="Promise"/> will be canceled.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise Then<TCaptureReject, TReject>(Action<T> onResolved, TCaptureReject rejectCaptureValue, Action<TCaptureReject, TReject> onRejected, CancelationToken cancelationToken = default)
            => WaitAsync(cancelationToken).Then(onResolved, rejectCaptureValue, onRejected);

        /// <summary>
        /// Capture 2 values and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is assignable to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled, the new <see cref="Promise"/> will be canceled.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise Then<TCaptureResolve, TCaptureReject, TReject>(TCaptureResolve resolveCaptureValue, Action<TCaptureResolve, T> onResolved, TCaptureReject rejectCaptureValue, Action<TCaptureReject, TReject> onRejected, CancelationToken cancelationToken = default)
            => WaitAsync(cancelationToken).Then(resolveCaptureValue, onResolved, rejectCaptureValue, onRejected);

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="TResult"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled, the new <see cref="Promise{T}"/> will be canceled.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise<TResult> Then<TCaptureResolve, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, TResult> onResolved, Func<TResult> onRejected, CancelationToken cancelationToken = default)
            => WaitAsync(cancelationToken).Then(resolveCaptureValue, onResolved, onRejected);

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="TResult"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled, the new <see cref="Promise{T}"/> will be canceled.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise<TResult> Then<TCaptureReject, TResult>(Func<T, TResult> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TResult> onRejected, CancelationToken cancelationToken = default)
            => WaitAsync(cancelationToken).Then(onResolved, rejectCaptureValue, onRejected);

        /// <summary>
        /// Capture 2 values and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="TResult"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled, the new <see cref="Promise{T}"/> will be canceled.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise<TResult> Then<TCaptureResolve, TCaptureReject, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, TResult> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TResult> onRejected, CancelationToken cancelationToken = default)
            => WaitAsync(cancelationToken).Then(resolveCaptureValue, onResolved, rejectCaptureValue, onRejected);

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="TResult"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is assignable to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled, the new <see cref="Promise{T}"/> will be canceled.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise<TResult> Then<TCaptureResolve, TResult, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, TResult> onResolved, Func<TReject, TResult> onRejected, CancelationToken cancelationToken = default)
            => WaitAsync(cancelationToken).Then(resolveCaptureValue, onResolved, onRejected);

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="TResult"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is assignable to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled, the new <see cref="Promise{T}"/> will be canceled.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise<TResult> Then<TCaptureReject, TResult, TReject>(Func<T, TResult> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, TResult> onRejected, CancelationToken cancelationToken = default)
            => WaitAsync(cancelationToken).Then(onResolved, rejectCaptureValue, onRejected);

        /// <summary>
        /// Capture 2 values and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="TResult"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is assignable to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled, the new <see cref="Promise{T}"/> will be canceled.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise<TResult> Then<TCaptureResolve, TCaptureReject, TResult, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, TResult> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, TResult> onRejected, CancelationToken cancelationToken = default)
            => WaitAsync(cancelationToken).Then(resolveCaptureValue, onResolved, rejectCaptureValue, onRejected);

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled, the new <see cref="Promise"/> will be canceled.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise Then<TCaptureResolve>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise> onResolved, Func<Promise> onRejected, CancelationToken cancelationToken = default)
            => WaitAsync(cancelationToken).Then(resolveCaptureValue, onResolved, onRejected);

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled, the new <see cref="Promise"/> will be canceled.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise Then<TCaptureReject>(Func<T, Promise> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise> onRejected, CancelationToken cancelationToken = default)
            => WaitAsync(cancelationToken).Then(onResolved, rejectCaptureValue, onRejected);

        /// <summary>
        /// Capture 2 values and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled, the new <see cref="Promise"/> will be canceled.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise Then<TCaptureResolve, TCaptureReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise> onRejected, CancelationToken cancelationToken = default)
            => WaitAsync(cancelationToken).Then(resolveCaptureValue, onResolved, rejectCaptureValue, onRejected);

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is assignable to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled, the new <see cref="Promise"/> will be canceled.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise Then<TCaptureResolve, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise> onResolved, Func<TReject, Promise> onRejected, CancelationToken cancelationToken = default)
            => WaitAsync(cancelationToken).Then(resolveCaptureValue, onResolved, onRejected);

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is assignable to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled, the new <see cref="Promise"/> will be canceled.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise Then<TCaptureReject, TReject>(Func<T, Promise> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise> onRejected, CancelationToken cancelationToken = default)
            => WaitAsync(cancelationToken).Then(onResolved, rejectCaptureValue, onRejected);

        /// <summary>
        /// Capture 2 values and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is assignable to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled, the new <see cref="Promise"/> will be canceled.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise Then<TCaptureResolve, TCaptureReject, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise> onRejected, CancelationToken cancelationToken = default)
            => WaitAsync(cancelationToken).Then(resolveCaptureValue, onResolved, rejectCaptureValue, onRejected);

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="TResult"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled, the new <see cref="Promise{T}"/> will be canceled.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise<TResult> Then<TCaptureResolve, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise<TResult>> onResolved, Func<Promise<TResult>> onRejected, CancelationToken cancelationToken = default)
            => WaitAsync(cancelationToken).Then(resolveCaptureValue, onResolved, onRejected);

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="TResult"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled, the new <see cref="Promise{T}"/> will be canceled.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise<TResult> Then<TCaptureReject, TResult>(Func<T, Promise<TResult>> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise<TResult>> onRejected, CancelationToken cancelationToken = default)
            => WaitAsync(cancelationToken).Then(onResolved, rejectCaptureValue, onRejected);

        /// <summary>
        /// Capture 2 values and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="TResult"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled, the new <see cref="Promise{T}"/> will be canceled.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise<TResult> Then<TCaptureResolve, TCaptureReject, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise<TResult>> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise<TResult>> onRejected, CancelationToken cancelationToken = default)
            => WaitAsync(cancelationToken).Then(resolveCaptureValue, onResolved, rejectCaptureValue, onRejected);

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="TResult"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is assignable to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled, the new <see cref="Promise{T}"/> will be canceled.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise<TResult> Then<TCaptureResolve, TResult, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise<TResult>> onResolved, Func<TReject, Promise<TResult>> onRejected, CancelationToken cancelationToken = default)
            => WaitAsync(cancelationToken).Then(resolveCaptureValue, onResolved, onRejected);

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="TResult"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is assignable to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled, the new <see cref="Promise{T}"/> will be canceled.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise<TResult> Then<TCaptureReject, TResult, TReject>(Func<T, Promise<TResult>> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise<TResult>> onRejected, CancelationToken cancelationToken = default)
            => WaitAsync(cancelationToken).Then(onResolved, rejectCaptureValue, onRejected);

        /// <summary>
        /// Capture 2 values and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="TResult"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is assignable to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled, the new <see cref="Promise{T}"/> will be canceled.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise<TResult> Then<TCaptureResolve, TCaptureReject, TResult, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise<TResult>> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise<TResult>> onRejected, CancelationToken cancelationToken = default)
            => WaitAsync(cancelationToken).Then(resolveCaptureValue, onResolved, rejectCaptureValue, onRejected);

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled, the new <see cref="Promise"/> will be canceled.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise Then<TCaptureResolve>(TCaptureResolve resolveCaptureValue, Action<TCaptureResolve, T> onResolved, Func<Promise> onRejected, CancelationToken cancelationToken = default)
            => WaitAsync(cancelationToken).Then(resolveCaptureValue, onResolved, onRejected);

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with and the resolve value, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled, the new <see cref="Promise"/> will be canceled.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise Then<TCaptureReject>(Action<T> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise> onRejected, CancelationToken cancelationToken = default)
            => WaitAsync(cancelationToken).Then(onResolved, rejectCaptureValue, onRejected);

        /// <summary>
        /// Capture 2 values and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled, the new <see cref="Promise"/> will be canceled.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise Then<TCaptureResolve, TCaptureReject>(TCaptureResolve resolveCaptureValue, Action<TCaptureResolve, T> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise> onRejected, CancelationToken cancelationToken = default)
            => WaitAsync(cancelationToken).Then(resolveCaptureValue, onResolved, rejectCaptureValue, onRejected);

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is assignable to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled, the new <see cref="Promise"/> will be canceled.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise Then<TCaptureResolve, TReject>(TCaptureResolve resolveCaptureValue, Action<TCaptureResolve, T> onResolved, Func<TReject, Promise> onRejected, CancelationToken cancelationToken = default)
            => WaitAsync(cancelationToken).Then(resolveCaptureValue, onResolved, onRejected);

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with and the resolve value, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is assignable to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled, the new <see cref="Promise"/> will be canceled.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise Then<TCaptureReject, TReject>(Action<T> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise> onRejected, CancelationToken cancelationToken = default)
            => WaitAsync(cancelationToken).Then(onResolved, rejectCaptureValue, onRejected);

        /// <summary>
        /// Capture 2 values and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is assignable to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled, the new <see cref="Promise"/> will be canceled.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise Then<TCaptureResolve, TCaptureReject, TReject>(TCaptureResolve resolveCaptureValue, Action<TCaptureResolve, T> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise> onRejected, CancelationToken cancelationToken = default)
            => WaitAsync(cancelationToken).Then(resolveCaptureValue, onResolved, rejectCaptureValue, onRejected);

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="TResult"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled, the new <see cref="Promise{T}"/> will be canceled.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise<TResult> Then<TCaptureResolve, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, TResult> onResolved, Func<Promise<TResult>> onRejected, CancelationToken cancelationToken = default)
            => WaitAsync(cancelationToken).Then(resolveCaptureValue, onResolved, onRejected);

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="TResult"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled, the new <see cref="Promise{T}"/> will be canceled.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise<TResult> Then<TCaptureReject, TResult>(Func<T, TResult> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise<TResult>> onRejected, CancelationToken cancelationToken = default)
            => WaitAsync(cancelationToken).Then(onResolved, rejectCaptureValue, onRejected);

        /// <summary>
        /// Capture 2 values and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="TResult"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled, the new <see cref="Promise{T}"/> will be canceled.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise<TResult> Then<TCaptureResolve, TCaptureReject, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, TResult> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise<TResult>> onRejected, CancelationToken cancelationToken = default)
            => WaitAsync(cancelationToken).Then(resolveCaptureValue, onResolved, rejectCaptureValue, onRejected);

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="TResult"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is assignable to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled, the new <see cref="Promise{T}"/> will be canceled.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise<TResult> Then<TCaptureResolve, TResult, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, TResult> onResolved, Func<TReject, Promise<TResult>> onRejected, CancelationToken cancelationToken = default)
            => WaitAsync(cancelationToken).Then(resolveCaptureValue, onResolved, onRejected);

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="TResult"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is assignable to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled, the new <see cref="Promise{T}"/> will be canceled.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise<TResult> Then<TCaptureReject, TResult, TReject>(Func<T, TResult> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise<TResult>> onRejected, CancelationToken cancelationToken = default)
            => WaitAsync(cancelationToken).Then(onResolved, rejectCaptureValue, onRejected);

        /// <summary>
        /// Capture 2 values and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="TResult"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is assignable to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled, the new <see cref="Promise{T}"/> will be canceled.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise<TResult> Then<TCaptureResolve, TCaptureReject, TResult, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, TResult> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise<TResult>> onRejected, CancelationToken cancelationToken = default)
            => WaitAsync(cancelationToken).Then(resolveCaptureValue, onResolved, rejectCaptureValue, onRejected);

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled, the new <see cref="Promise"/> will be canceled.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise Then<TCaptureResolve>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise> onResolved, Action onRejected, CancelationToken cancelationToken = default)
            => WaitAsync(cancelationToken).Then(resolveCaptureValue, onResolved, onRejected);

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled, the new <see cref="Promise"/> will be canceled.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise Then<TCaptureReject>(Func<T, Promise> onResolved, TCaptureReject rejectCaptureValue, Action<TCaptureReject> onRejected, CancelationToken cancelationToken = default)
            => WaitAsync(cancelationToken).Then(onResolved, rejectCaptureValue, onRejected);

        /// <summary>
        /// Capture 2 values and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled, the new <see cref="Promise"/> will be canceled.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise Then<TCaptureResolve, TCaptureReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise> onResolved, TCaptureReject rejectCaptureValue, Action<TCaptureReject> onRejected, CancelationToken cancelationToken = default)
            => WaitAsync(cancelationToken).Then(resolveCaptureValue, onResolved, rejectCaptureValue, onRejected);

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is assignable to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled, the new <see cref="Promise"/> will be canceled.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise Then<TCaptureResolve, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise> onResolved, Action<TReject> onRejected, CancelationToken cancelationToken = default)
            => WaitAsync(cancelationToken).Then(resolveCaptureValue, onResolved, onRejected);

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is assignable to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled, the new <see cref="Promise"/> will be canceled.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise Then<TCaptureReject, TReject>(Func<T, Promise> onResolved, TCaptureReject rejectCaptureValue, Action<TCaptureReject, TReject> onRejected, CancelationToken cancelationToken = default)
            => WaitAsync(cancelationToken).Then(onResolved, rejectCaptureValue, onRejected);

        /// <summary>
        /// Capture 2 values and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is assignable to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled, the new <see cref="Promise"/> will be canceled.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise Then<TCaptureResolve, TCaptureReject, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise> onResolved, TCaptureReject rejectCaptureValue, Action<TCaptureReject, TReject> onRejected, CancelationToken cancelationToken = default)
            => WaitAsync(cancelationToken).Then(resolveCaptureValue, onResolved, rejectCaptureValue, onRejected);

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="TResult"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled, the new <see cref="Promise{T}"/> will be canceled.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise<TResult> Then<TCaptureResolve, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise<TResult>> onResolved, Func<TResult> onRejected, CancelationToken cancelationToken = default)
            => WaitAsync(cancelationToken).Then(resolveCaptureValue, onResolved, onRejected);

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="TResult"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled, the new <see cref="Promise{T}"/> will be canceled.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise<TResult> Then<TCaptureReject, TResult>(Func<T, Promise<TResult>> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TResult> onRejected, CancelationToken cancelationToken = default)
            => WaitAsync(cancelationToken).Then(onResolved, rejectCaptureValue, onRejected);

        /// <summary>
        /// Capture 2 values and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="TResult"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled, the new <see cref="Promise{T}"/> will be canceled.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise<TResult> Then<TCaptureResolve, TCaptureReject, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise<TResult>> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TResult> onRejected, CancelationToken cancelationToken = default)
            => WaitAsync(cancelationToken).Then(resolveCaptureValue, onResolved, rejectCaptureValue, onRejected);

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="TResult"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is assignable to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled, the new <see cref="Promise{T}"/> will be canceled.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise<TResult> Then<TCaptureResolve, TResult, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise<TResult>> onResolved, Func<TReject, TResult> onRejected, CancelationToken cancelationToken = default)
            => WaitAsync(cancelationToken).Then(resolveCaptureValue, onResolved, onRejected);

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="TResult"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is assignable to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled, the new <see cref="Promise{T}"/> will be canceled.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise<TResult> Then<TCaptureReject, TResult, TReject>(Func<T, Promise<TResult>> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, TResult> onRejected, CancelationToken cancelationToken = default)
            => WaitAsync(cancelationToken).Then(onResolved, rejectCaptureValue, onRejected);

        /// <summary>
        /// Capture 2 values and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/> of <typeparamref name="TResult"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is assignable to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled, the new <see cref="Promise{T}"/> will be canceled.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise<TResult> Then<TCaptureResolve, TCaptureReject, TResult, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise<TResult>> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, TResult> onRejected, CancelationToken cancelationToken = default)
            => WaitAsync(cancelationToken).Then(resolveCaptureValue, onResolved, rejectCaptureValue, onRejected);
        #endregion
    }

    // Inherited from Promise (must copy since structs cannot inherit).
    // Did not copy Finally or ContinueWith.
    partial struct Promise<T>
    {
        /// <inheritdoc cref="Promise.CatchCancelation(Action, CancelationToken)"/>
        [MethodImpl(Internal.InlineOption)]
        [Obsolete("Prefer ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise CatchCancelation(Action onCanceled, CancelationToken cancelationToken = default)
            => AsPromise().CatchCancelation(onCanceled, cancelationToken);

        /// <inheritdoc cref="Promise.CatchCancelation(Func{Promise}, CancelationToken)"/>
        [MethodImpl(Internal.InlineOption)]
        [Obsolete("Prefer ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise CatchCancelation(Func<Promise> onCanceled, CancelationToken cancelationToken = default)
            => AsPromise().CatchCancelation(onCanceled, cancelationToken);

        #region Resolve Callbacks
        /// <inheritdoc cref="Promise.Then(Action, CancelationToken)"/>
        [MethodImpl(Internal.InlineOption)]
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise Then(Action onResolved, CancelationToken cancelationToken = default)
            => AsPromise().Then(onResolved, cancelationToken);

        /// <inheritdoc cref="Promise.Then{TResult}(Func{TResult}, CancelationToken)"/>
        [MethodImpl(Internal.InlineOption)]
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise<TResult> Then<TResult>(Func<TResult> onResolved, CancelationToken cancelationToken = default)
            => AsPromise().Then(onResolved, cancelationToken);

        /// <inheritdoc cref="Promise.Then(Func{Promise}, CancelationToken)"/>
        [MethodImpl(Internal.InlineOption)]
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise Then(Func<Promise> onResolved, CancelationToken cancelationToken = default)
            => AsPromise().Then(onResolved, cancelationToken);

        /// <inheritdoc cref="Promise.Then{TResult}(Func{Promise{TResult}}, CancelationToken)"/>
        [MethodImpl(Internal.InlineOption)]
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise<TResult> Then<TResult>(Func<Promise<TResult>> onResolved, CancelationToken cancelationToken = default)
            => AsPromise().Then(onResolved, cancelationToken);
        #endregion

        #region Reject Callbacks
        /// <inheritdoc cref="Promise.Catch(Action, CancelationToken)"/>
        [MethodImpl(Internal.InlineOption)]
        [Obsolete("Prefer WaitAsync(cancelationToken).Catch(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise Catch(Action onRejected, CancelationToken cancelationToken = default)
            => AsPromise().Catch(onRejected, cancelationToken);

        /// <inheritdoc cref="Promise.Catch{TReject}(Action{TReject}, CancelationToken)"/>
        [MethodImpl(Internal.InlineOption)]
        [Obsolete("Prefer WaitAsync(cancelationToken).Catch(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise Catch<TReject>(Action<TReject> onRejected, CancelationToken cancelationToken = default)
            => AsPromise().Catch(onRejected, cancelationToken);

        /// <inheritdoc cref="Promise.Catch(Func{Promise}, CancelationToken)"/>
        [MethodImpl(Internal.InlineOption)]
        [Obsolete("Prefer WaitAsync(cancelationToken).Catch(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise Catch(Func<Promise> onRejected, CancelationToken cancelationToken = default)
            => AsPromise().Catch(onRejected, cancelationToken);

        /// <inheritdoc cref="Promise.Catch{TReject}(Func{TReject, Promise}, CancelationToken)"/>
        [MethodImpl(Internal.InlineOption)]
        [Obsolete("Prefer WaitAsync(cancelationToken).Catch(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise Catch<TReject>(Func<TReject, Promise> onRejected, CancelationToken cancelationToken = default)
            => AsPromise().Catch(onRejected, cancelationToken);
        #endregion

        #region Resolve or Reject Callbacks
        /// <inheritdoc cref="Promise.Then(Action, Action, CancelationToken)"/>
        [MethodImpl(Internal.InlineOption)]
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise Then(Action onResolved, Action onRejected, CancelationToken cancelationToken = default)
            => AsPromise().Then(onResolved, onRejected, cancelationToken);

        /// <inheritdoc cref="Promise.Then{TReject}(Action, Action{TReject}, CancelationToken)"/>
        [MethodImpl(Internal.InlineOption)]
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise Then<TReject>(Action onResolved, Action<TReject> onRejected, CancelationToken cancelationToken = default)
            => AsPromise().Then(onResolved, onRejected, cancelationToken);

        /// <inheritdoc cref="Promise.Then{TResult}(Func{TResult}, Func{TResult}, CancelationToken)"/>
        [MethodImpl(Internal.InlineOption)]
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise<TResult> Then<TResult>(Func<TResult> onResolved, Func<TResult> onRejected, CancelationToken cancelationToken = default)
            => AsPromise().Then(onResolved, onRejected, cancelationToken);

        /// <inheritdoc cref="Promise.Then{TResult, TReject}(Func{TResult}, Func{TReject, TResult}, CancelationToken)"/>
        [MethodImpl(Internal.InlineOption)]
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise<TResult> Then<TResult, TReject>(Func<TResult> onResolved, Func<TReject, TResult> onRejected, CancelationToken cancelationToken = default)
            => AsPromise().Then(onResolved, onRejected, cancelationToken);

        /// <inheritdoc cref="Promise.Then(Func{Promise}, Func{Promise}, CancelationToken)"/>
        [MethodImpl(Internal.InlineOption)]
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise Then(Func<Promise> onResolved, Func<Promise> onRejected, CancelationToken cancelationToken = default)
            => AsPromise().Then(onResolved, onRejected, cancelationToken);

        /// <inheritdoc cref="Promise.Then{TReject}(Func{Promise}, Func{TReject, Promise}, CancelationToken)"/>
        [MethodImpl(Internal.InlineOption)]
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise Then<TReject>(Func<Promise> onResolved, Func<TReject, Promise> onRejected, CancelationToken cancelationToken = default)
            => AsPromise().Then(onResolved, onRejected, cancelationToken);

        /// <inheritdoc cref="Promise.Then{TResult}(Func{Promise{TResult}}, Func{Promise{TResult}}, CancelationToken)"/>
        [MethodImpl(Internal.InlineOption)]
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise<TResult> Then<TResult>(Func<Promise<TResult>> onResolved, Func<Promise<TResult>> onRejected, CancelationToken cancelationToken = default)
            => AsPromise().Then(onResolved, onRejected, cancelationToken);

        /// <inheritdoc cref="Promise.Then{TResult, TReject}(Func{Promise{TResult}}, Func{TReject, Promise{TResult}}, CancelationToken)"/>
        [MethodImpl(Internal.InlineOption)]
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise<TResult> Then<TResult, TReject>(Func<Promise<TResult>> onResolved, Func<TReject, Promise<TResult>> onRejected, CancelationToken cancelationToken = default)
            => AsPromise().Then(onResolved, onRejected, cancelationToken);

        /// <inheritdoc cref="Promise.Then(Action, Func{Promise}, CancelationToken)"/>
        [MethodImpl(Internal.InlineOption)]
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise Then(Action onResolved, Func<Promise> onRejected, CancelationToken cancelationToken = default)
            => AsPromise().Then(onResolved, onRejected, cancelationToken);

        /// <inheritdoc cref="Promise.Then{TReject}(Action, Func{TReject, Promise}, CancelationToken)"/>
        [MethodImpl(Internal.InlineOption)]
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise Then<TReject>(Action onResolved, Func<TReject, Promise> onRejected, CancelationToken cancelationToken = default)
            => AsPromise().Then(onResolved, onRejected, cancelationToken);

        /// <inheritdoc cref="Promise.Then{TResult}(Func{TResult}, Func{Promise{TResult}}, CancelationToken)"/>
        [MethodImpl(Internal.InlineOption)]
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise<TResult> Then<TResult>(Func<TResult> onResolved, Func<Promise<TResult>> onRejected, CancelationToken cancelationToken = default)
            => AsPromise().Then(onResolved, onRejected, cancelationToken);

        /// <inheritdoc cref="Promise.Then{TResult, TReject}(Func{TResult}, Func{TReject, Promise{TResult}}, CancelationToken)"/>
        [MethodImpl(Internal.InlineOption)]
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise<TResult> Then<TResult, TReject>(Func<TResult> onResolved, Func<TReject, Promise<TResult>> onRejected, CancelationToken cancelationToken = default)
            => AsPromise().Then(onResolved, onRejected, cancelationToken);

        /// <inheritdoc cref="Promise.Then(Func{Promise}, Action, CancelationToken)"/>
        [MethodImpl(Internal.InlineOption)]
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise Then(Func<Promise> onResolved, Action onRejected, CancelationToken cancelationToken = default)
            => AsPromise().Then(onResolved, onRejected, cancelationToken);

        /// <inheritdoc cref="Promise.Then{TReject}(Func{Promise}, Action{TReject}, CancelationToken)"/>
        [MethodImpl(Internal.InlineOption)]
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise Then<TReject>(Func<Promise> onResolved, Action<TReject> onRejected, CancelationToken cancelationToken = default)
            => AsPromise().Then(onResolved, onRejected, cancelationToken);

        /// <inheritdoc cref="Promise.Then{TResult}(Func{Promise{TResult}}, Func{TResult}, CancelationToken)"/>
        [MethodImpl(Internal.InlineOption)]
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise<TResult> Then<TResult>(Func<Promise<TResult>> onResolved, Func<TResult> onRejected, CancelationToken cancelationToken = default)
            => AsPromise().Then(onResolved, onRejected, cancelationToken);

        /// <inheritdoc cref="Promise.Then{TResult, TReject}(Func{Promise{TResult}}, Func{TReject, TResult}, CancelationToken)"/>
        [MethodImpl(Internal.InlineOption)]
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise<TResult> Then<TResult, TReject>(Func<Promise<TResult>> onResolved, Func<TReject, TResult> onRejected, CancelationToken cancelationToken = default)
            => AsPromise().Then(onResolved, onRejected, cancelationToken);
        #endregion

        // Capture values below.

        /// <inheritdoc cref="Promise.CatchCancelation{TCaptureCancel}(TCaptureCancel, Action{TCaptureCancel}, CancelationToken)"/>
        [MethodImpl(Internal.InlineOption)]
        [Obsolete("Prefer ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise CatchCancelation<TCaptureCancel>(TCaptureCancel cancelCaptureValue, Action<TCaptureCancel> onCanceled, CancelationToken cancelationToken = default)
            => AsPromise().CatchCancelation(cancelCaptureValue, onCanceled, cancelationToken);

        /// <inheritdoc cref="Promise.CatchCancelation{TCaptureCancel}(TCaptureCancel, Func{TCaptureCancel, Promise}, CancelationToken)"/>
        [MethodImpl(Internal.InlineOption)]
        [Obsolete("Prefer ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise CatchCancelation<TCaptureCancel>(TCaptureCancel cancelCaptureValue, Func<TCaptureCancel, Promise> onCanceled, CancelationToken cancelationToken = default)
            => AsPromise().CatchCancelation(cancelCaptureValue, onCanceled, cancelationToken);

        #region Resolve Callbacks
        /// <inheritdoc cref="Promise.Then{TCaptureResolve}(TCaptureResolve, Action{TCaptureResolve}, CancelationToken)"/>
        [MethodImpl(Internal.InlineOption)]
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise Then<TCaptureResolve>(TCaptureResolve resolveCaptureValue, Action<TCaptureResolve> onResolved, CancelationToken cancelationToken = default)
            => AsPromise().Then(resolveCaptureValue, onResolved, cancelationToken);

        /// <inheritdoc cref="Promise.Then{TCaptureResolve, TResult}(TCaptureResolve, Func{TCaptureResolve, TResult}, CancelationToken)"/>
        [MethodImpl(Internal.InlineOption)]
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise<TResult> Then<TCaptureResolve, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, TResult> onResolved, CancelationToken cancelationToken = default)
            => AsPromise().Then(resolveCaptureValue, onResolved, cancelationToken);

        /// <inheritdoc cref="Promise.Then{TCaptureResolve}(TCaptureResolve, Func{TCaptureResolve, Promise}, CancelationToken)"/>
        [MethodImpl(Internal.InlineOption)]
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise Then<TCaptureResolve>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise> onResolved, CancelationToken cancelationToken = default)
            => AsPromise().Then(resolveCaptureValue, onResolved, cancelationToken);

        /// <inheritdoc cref="Promise.Then{TCaptureResolve, TResult}(TCaptureResolve, Func{TCaptureResolve, Promise{TResult}}, CancelationToken)"/>
        [MethodImpl(Internal.InlineOption)]
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise<TResult> Then<TCaptureResolve, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise<TResult>> onResolved, CancelationToken cancelationToken = default)
            => AsPromise().Then(resolveCaptureValue, onResolved, cancelationToken);
        #endregion

        #region Reject Callbacks
        /// <inheritdoc cref="Promise.Catch{TCaptureReject}(TCaptureReject, Action{TCaptureReject}, CancelationToken)"/>
        [MethodImpl(Internal.InlineOption)]
        [Obsolete("Prefer WaitAsync(cancelationToken).Catch(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise Catch<TCaptureReject>(TCaptureReject rejectCaptureValue, Action<TCaptureReject> onRejected, CancelationToken cancelationToken = default)
            => AsPromise().Catch(rejectCaptureValue, onRejected, cancelationToken);

        /// <inheritdoc cref="Promise.Catch{TCaptureReject, TReject}(TCaptureReject, Action{TCaptureReject, TReject}, CancelationToken)"/>
        [MethodImpl(Internal.InlineOption)]
        [Obsolete("Prefer WaitAsync(cancelationToken).Catch(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise Catch<TCaptureReject, TReject>(TCaptureReject rejectCaptureValue, Action<TCaptureReject, TReject> onRejected, CancelationToken cancelationToken = default)
            => AsPromise().Catch(rejectCaptureValue, onRejected, cancelationToken);

        /// <inheritdoc cref="Promise.Catch{TCaptureReject}(TCaptureReject, Func{TCaptureReject, Promise}, CancelationToken)"/>
        [MethodImpl(Internal.InlineOption)]
        [Obsolete("Prefer WaitAsync(cancelationToken).Catch(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise Catch<TCaptureReject>(TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise> onRejected, CancelationToken cancelationToken = default)
            => AsPromise().Catch(rejectCaptureValue, onRejected, cancelationToken);

        /// <inheritdoc cref="Promise.Catch{TCaptureReject, TReject}(TCaptureReject, Func{TCaptureReject, TReject, Promise}, CancelationToken)"/>
        [MethodImpl(Internal.InlineOption)]
        [Obsolete("Prefer WaitAsync(cancelationToken).Catch(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise Catch<TCaptureReject, TReject>(TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise> onRejected, CancelationToken cancelationToken = default)
            => AsPromise().Catch(rejectCaptureValue, onRejected, cancelationToken);
        #endregion

        #region Resolve or Reject Callbacks
        /// <inheritdoc cref="Promise.Then{TCaptureResolve}(TCaptureResolve, Action{TCaptureResolve}, Action, CancelationToken)"/>
        [MethodImpl(Internal.InlineOption)]
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise Then<TCaptureResolve>(TCaptureResolve resolveCaptureValue, Action<TCaptureResolve> onResolved, Action onRejected, CancelationToken cancelationToken = default)
            => AsPromise().Then(resolveCaptureValue, onResolved, onRejected, cancelationToken);

        /// <inheritdoc cref="Promise.Then{TCaptureReject}(Action, TCaptureReject, Action{TCaptureReject}, CancelationToken)"/>
        [MethodImpl(Internal.InlineOption)]
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise Then<TCaptureReject>(Action onResolved, TCaptureReject rejectCaptureValue, Action<TCaptureReject> onRejected, CancelationToken cancelationToken = default)
            => AsPromise().Then(onResolved, rejectCaptureValue, onRejected, cancelationToken);

        /// <inheritdoc cref="Promise.Then{TCaptureResolve, TCaptureReject}(TCaptureResolve, Action{TCaptureResolve}, TCaptureReject, Action{TCaptureReject}, CancelationToken)"/>
        [MethodImpl(Internal.InlineOption)]
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise Then<TCaptureResolve, TCaptureReject>(TCaptureResolve resolveCaptureValue, Action<TCaptureResolve> onResolved, TCaptureReject rejectCaptureValue, Action<TCaptureReject> onRejected, CancelationToken cancelationToken = default)
            => AsPromise().Then(resolveCaptureValue, onResolved, rejectCaptureValue, onRejected, cancelationToken);

        /// <inheritdoc cref="Promise.Then{TCaptureResolve, TReject}(TCaptureResolve, Action{TCaptureResolve}, Action{TReject}, CancelationToken)"/>
        [MethodImpl(Internal.InlineOption)]
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise Then<TCaptureResolve, TReject>(TCaptureResolve resolveCaptureValue, Action<TCaptureResolve> onResolved, Action<TReject> onRejected, CancelationToken cancelationToken = default)
            => AsPromise().Then(resolveCaptureValue, onResolved, onRejected, cancelationToken);

        /// <inheritdoc cref="Promise.Then{TCaptureReject, TReject}(Action, TCaptureReject, Action{TCaptureReject, TReject}, CancelationToken)"/>
        [MethodImpl(Internal.InlineOption)]
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise Then<TCaptureReject, TReject>(Action onResolved, TCaptureReject rejectCaptureValue, Action<TCaptureReject, TReject> onRejected, CancelationToken cancelationToken = default)
            => AsPromise().Then(onResolved, rejectCaptureValue, onRejected, cancelationToken);

        /// <inheritdoc cref="Promise.Then{TCaptureResolve, TCaptureReject, TReject}(TCaptureResolve, Action{TCaptureResolve}, TCaptureReject, Action{TCaptureReject, TReject}, CancelationToken)"/>
        [MethodImpl(Internal.InlineOption)]
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise Then<TCaptureResolve, TCaptureReject, TReject>(TCaptureResolve resolveCaptureValue, Action<TCaptureResolve> onResolved, TCaptureReject rejectCaptureValue, Action<TCaptureReject, TReject> onRejected, CancelationToken cancelationToken = default)
            => AsPromise().Then(resolveCaptureValue, onResolved, rejectCaptureValue, onRejected, cancelationToken);

        /// <inheritdoc cref="Promise.Then{TCaptureResolve, TResult}(TCaptureResolve, Func{TCaptureResolve, TResult}, Func{TResult}, CancelationToken)"/>
        [MethodImpl(Internal.InlineOption)]
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise<TResult> Then<TCaptureResolve, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, TResult> onResolved, Func<TResult> onRejected, CancelationToken cancelationToken = default)
            => AsPromise().Then(resolveCaptureValue, onResolved, onRejected, cancelationToken);

        /// <inheritdoc cref="Promise.Then{TCaptureReject, TResult}(Func{TResult}, TCaptureReject, Func{TCaptureReject, TResult}, CancelationToken)"/>
        [MethodImpl(Internal.InlineOption)]
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise<TResult> Then<TCaptureReject, TResult>(Func<TResult> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TResult> onRejected, CancelationToken cancelationToken = default)
            => AsPromise().Then(onResolved, rejectCaptureValue, onRejected, cancelationToken);

        /// <inheritdoc cref="Promise.Then{TCaptureResolve, TCaptureReject, TResult}(TCaptureResolve, Func{TCaptureResolve, TResult}, TCaptureReject, Func{TCaptureReject, TResult}, CancelationToken)"/>
        [MethodImpl(Internal.InlineOption)]
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise<TResult> Then<TCaptureResolve, TCaptureReject, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, TResult> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TResult> onRejected, CancelationToken cancelationToken = default)
            => AsPromise().Then(resolveCaptureValue, onResolved, rejectCaptureValue, onRejected, cancelationToken);

        /// <inheritdoc cref="Promise.Then{TCaptureResolve, TResult, TReject}(TCaptureResolve, Func{TCaptureResolve, TResult}, Func{TReject, TResult}, CancelationToken)"/>
        [MethodImpl(Internal.InlineOption)]
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise<TResult> Then<TCaptureResolve, TResult, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, TResult> onResolved, Func<TReject, TResult> onRejected, CancelationToken cancelationToken = default)
            => AsPromise().Then(resolveCaptureValue, onResolved, onRejected, cancelationToken);

        /// <inheritdoc cref="Promise.Then{TCaptureReject, TResult, TReject}(Func{TResult}, TCaptureReject, Func{TCaptureReject, TReject, TResult}, CancelationToken)"/>
        [MethodImpl(Internal.InlineOption)]
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise<TResult> Then<TCaptureReject, TResult, TReject>(Func<TResult> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, TResult> onRejected, CancelationToken cancelationToken = default)
            => AsPromise().Then(onResolved, rejectCaptureValue, onRejected, cancelationToken);

        /// <inheritdoc cref="Promise.Then{TCaptureResolve, TCaptureReject, TResult, TReject}(TCaptureResolve, Func{TCaptureResolve, TResult}, TCaptureReject, Func{TCaptureReject, TReject, TResult}, CancelationToken)"/>
        [MethodImpl(Internal.InlineOption)]
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise<TResult> Then<TCaptureResolve, TCaptureReject, TResult, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, TResult> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, TResult> onRejected, CancelationToken cancelationToken = default)
            => AsPromise().Then(resolveCaptureValue, onResolved, rejectCaptureValue, onRejected, cancelationToken);

        /// <inheritdoc cref="Promise.Then{TCaptureResolve}(TCaptureResolve, Func{TCaptureResolve, Promise}, Func{Promise}, CancelationToken)"/>
        [MethodImpl(Internal.InlineOption)]
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise Then<TCaptureResolve>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise> onResolved, Func<Promise> onRejected, CancelationToken cancelationToken = default)
            => AsPromise().Then(resolveCaptureValue, onResolved, onRejected, cancelationToken);

        /// <inheritdoc cref="Promise.Then{TCaptureReject}(Func{Promise}, TCaptureReject, Func{TCaptureReject, Promise}, CancelationToken)"/>
        [MethodImpl(Internal.InlineOption)]
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise Then<TCaptureReject>(Func<Promise> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise> onRejected, CancelationToken cancelationToken = default)
            => AsPromise().Then(onResolved, rejectCaptureValue, onRejected, cancelationToken);

        /// <inheritdoc cref="Promise.Then{TCaptureResolve, TCaptureReject}(TCaptureResolve, Func{TCaptureResolve, Promise}, TCaptureReject, Func{TCaptureReject, Promise}, CancelationToken)"/>
        [MethodImpl(Internal.InlineOption)]
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise Then<TCaptureResolve, TCaptureReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise> onRejected, CancelationToken cancelationToken = default)
            => AsPromise().Then(resolveCaptureValue, onResolved, rejectCaptureValue, onRejected, cancelationToken);

        /// <inheritdoc cref="Promise.Then{TCaptureResolve, TReject}(TCaptureResolve, Func{TCaptureResolve, Promise}, Func{TReject, Promise}, CancelationToken)"/>
        [MethodImpl(Internal.InlineOption)]
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise Then<TCaptureResolve, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise> onResolved, Func<TReject, Promise> onRejected, CancelationToken cancelationToken = default)
            => AsPromise().Then(resolveCaptureValue, onResolved, onRejected, cancelationToken);

        /// <inheritdoc cref="Promise.Then{TCaptureReject, TReject}(Func{Promise}, TCaptureReject, Func{TCaptureReject, TReject, Promise}, CancelationToken)"/>
        [MethodImpl(Internal.InlineOption)]
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise Then<TCaptureReject, TReject>(Func<Promise> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise> onRejected, CancelationToken cancelationToken = default)
            => AsPromise().Then(onResolved, rejectCaptureValue, onRejected, cancelationToken);

        /// <inheritdoc cref="Promise.Then{TCaptureResolve, TCaptureReject, TReject}(TCaptureResolve, Func{TCaptureResolve, Promise}, TCaptureReject, Func{TCaptureReject, TReject, Promise}, CancelationToken)"/>
        [MethodImpl(Internal.InlineOption)]
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise Then<TCaptureResolve, TCaptureReject, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise> onRejected, CancelationToken cancelationToken = default)
            => AsPromise().Then(resolveCaptureValue, onResolved, rejectCaptureValue, onRejected, cancelationToken);

        /// <inheritdoc cref="Promise.Then{TCaptureResolve, TResult}(TCaptureResolve, Func{TCaptureResolve, Promise{TResult}}, Func{Promise{TResult}}, CancelationToken)"/>
        [MethodImpl(Internal.InlineOption)]
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise<TResult> Then<TCaptureResolve, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise<TResult>> onResolved, Func<Promise<TResult>> onRejected, CancelationToken cancelationToken = default)
            => AsPromise().Then(resolveCaptureValue, onResolved, onRejected, cancelationToken);

        /// <inheritdoc cref="Promise.Then{TCaptureReject, TResult}(Func{Promise{TResult}}, TCaptureReject, Func{TCaptureReject, Promise{TResult}}, CancelationToken)"/>
        [MethodImpl(Internal.InlineOption)]
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise<TResult> Then<TCaptureReject, TResult>(Func<Promise<TResult>> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise<TResult>> onRejected, CancelationToken cancelationToken = default)
            => AsPromise().Then(onResolved, rejectCaptureValue, onRejected, cancelationToken);

        /// <inheritdoc cref="Promise.Then{TCaptureResolve, TCaptureReject, TResult}(TCaptureResolve, Func{TCaptureResolve, Promise{TResult}}, TCaptureReject, Func{TCaptureReject, Promise{TResult}}, CancelationToken)"/>
        [MethodImpl(Internal.InlineOption)]
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise<TResult> Then<TCaptureResolve, TCaptureReject, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise<TResult>> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise<TResult>> onRejected, CancelationToken cancelationToken = default)
            => AsPromise().Then(resolveCaptureValue, onResolved, rejectCaptureValue, onRejected, cancelationToken);

        /// <inheritdoc cref="Promise.Then{TCaptureResolve, TResult, TReject}(TCaptureResolve, Func{TCaptureResolve, Promise{TResult}}, Func{TReject, Promise{TResult}}, CancelationToken)"/>
        [MethodImpl(Internal.InlineOption)]
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise<TResult> Then<TCaptureResolve, TResult, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise<TResult>> onResolved, Func<TReject, Promise<TResult>> onRejected, CancelationToken cancelationToken = default)
            => AsPromise().Then(resolveCaptureValue, onResolved, onRejected, cancelationToken);

        /// <inheritdoc cref="Promise.Then{TCaptureReject, TResult, TReject}(Func{Promise{TResult}}, TCaptureReject, Func{TCaptureReject, TReject, Promise{TResult}}, CancelationToken)"/>
        [MethodImpl(Internal.InlineOption)]
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise<TResult> Then<TCaptureReject, TResult, TReject>(Func<Promise<TResult>> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise<TResult>> onRejected, CancelationToken cancelationToken = default)
            => AsPromise().Then(onResolved, rejectCaptureValue, onRejected, cancelationToken);

        /// <inheritdoc cref="Promise.Then{TCaptureResolve, TCaptureReject, TResult, TReject}(TCaptureResolve, Func{TCaptureResolve, Promise{TResult}}, TCaptureReject, Func{TCaptureReject, TReject, Promise{TResult}}, CancelationToken)"/>
        [MethodImpl(Internal.InlineOption)]
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise<TResult> Then<TCaptureResolve, TCaptureReject, TResult, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise<TResult>> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise<TResult>> onRejected, CancelationToken cancelationToken = default)
            => AsPromise().Then(resolveCaptureValue, onResolved, rejectCaptureValue, onRejected, cancelationToken);

        /// <inheritdoc cref="Promise.Then{TCaptureResolve}(TCaptureResolve, Action{TCaptureResolve}, Func{Promise}, CancelationToken)"/>
        [MethodImpl(Internal.InlineOption)]
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise Then<TCaptureResolve>(TCaptureResolve resolveCaptureValue, Action<TCaptureResolve> onResolved, Func<Promise> onRejected, CancelationToken cancelationToken = default)
            => AsPromise().Then(resolveCaptureValue, onResolved, onRejected, cancelationToken);

        /// <inheritdoc cref="Promise.Then{TCaptureReject}(Action, TCaptureReject, Func{TCaptureReject, Promise}, CancelationToken)"/>
        [MethodImpl(Internal.InlineOption)]
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise Then<TCaptureReject>(Action onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise> onRejected, CancelationToken cancelationToken = default)
            => AsPromise().Then(onResolved, rejectCaptureValue, onRejected, cancelationToken);

        /// <inheritdoc cref="Promise.Then{TCaptureResolve, TCaptureReject}(TCaptureResolve, Action{TCaptureResolve}, TCaptureReject, Func{TCaptureReject, Promise}, CancelationToken)"/>
        [MethodImpl(Internal.InlineOption)]
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise Then<TCaptureResolve, TCaptureReject>(TCaptureResolve resolveCaptureValue, Action<TCaptureResolve> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise> onRejected, CancelationToken cancelationToken = default)
            => AsPromise().Then(resolveCaptureValue, onResolved, rejectCaptureValue, onRejected, cancelationToken);

        /// <inheritdoc cref="Promise.Then{TCaptureResolve, TReject}(TCaptureResolve, Action{TCaptureResolve}, Func{TReject, Promise}, CancelationToken)"/>
        [MethodImpl(Internal.InlineOption)]
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise Then<TCaptureResolve, TReject>(TCaptureResolve resolveCaptureValue, Action<TCaptureResolve> onResolved, Func<TReject, Promise> onRejected, CancelationToken cancelationToken = default)
            => AsPromise().Then(resolveCaptureValue, onResolved, onRejected, cancelationToken);

        /// <inheritdoc cref="Promise.Then{TCaptureReject, TReject}(Action, TCaptureReject, Func{TCaptureReject, TReject, Promise}, CancelationToken)"/>
        [MethodImpl(Internal.InlineOption)]
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise Then<TCaptureReject, TReject>(Action onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise> onRejected, CancelationToken cancelationToken = default)
            => AsPromise().Then(onResolved, rejectCaptureValue, onRejected, cancelationToken);

        /// <inheritdoc cref="Promise.Then{TCaptureResolve, TCaptureReject, TReject}(TCaptureResolve, Action{TCaptureResolve}, TCaptureReject, Func{TCaptureReject, TReject, Promise}, CancelationToken)"/>
        [MethodImpl(Internal.InlineOption)]
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise Then<TCaptureResolve, TCaptureReject, TReject>(TCaptureResolve resolveCaptureValue, Action<TCaptureResolve> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise> onRejected, CancelationToken cancelationToken = default)
            => AsPromise().Then(resolveCaptureValue, onResolved, rejectCaptureValue, onRejected, cancelationToken);

        /// <inheritdoc cref="Promise.Then{TCaptureResolve, TResult}(TCaptureResolve, Func{TCaptureResolve, TResult}, Func{Promise{TResult}}, CancelationToken)"/>
        [MethodImpl(Internal.InlineOption)]
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise<TResult> Then<TCaptureResolve, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, TResult> onResolved, Func<Promise<TResult>> onRejected, CancelationToken cancelationToken = default)
            => AsPromise().Then(resolveCaptureValue, onResolved, onRejected, cancelationToken);

        /// <inheritdoc cref="Promise.Then{TCaptureReject, TResult}(Func{TResult}, TCaptureReject, Func{TCaptureReject, Promise{TResult}}, CancelationToken)"/>
        [MethodImpl(Internal.InlineOption)]
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise<TResult> Then<TCaptureReject, TResult>(Func<TResult> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise<TResult>> onRejected, CancelationToken cancelationToken = default)
            => AsPromise().Then(onResolved, rejectCaptureValue, onRejected, cancelationToken);

        /// <inheritdoc cref="Promise.Then{TCaptureResolve, TCaptureReject, TResult}(TCaptureResolve, Func{TCaptureResolve, TResult}, TCaptureReject, Func{TCaptureReject, Promise{TResult}}, CancelationToken)"/>
        [MethodImpl(Internal.InlineOption)]
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise<TResult> Then<TCaptureResolve, TCaptureReject, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, TResult> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise<TResult>> onRejected, CancelationToken cancelationToken = default)
            => AsPromise().Then(resolveCaptureValue, onResolved, rejectCaptureValue, onRejected, cancelationToken);

        /// <inheritdoc cref="Promise.Then{TCaptureResolve, TResult, TReject}(TCaptureResolve, Func{TCaptureResolve, TResult}, Func{TReject, Promise{TResult}}, CancelationToken)"/>
        [MethodImpl(Internal.InlineOption)]
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise<TResult> Then<TCaptureResolve, TResult, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, TResult> onResolved, Func<TReject, Promise<TResult>> onRejected, CancelationToken cancelationToken = default)
            => AsPromise().Then(resolveCaptureValue, onResolved, onRejected, cancelationToken);

        /// <inheritdoc cref="Promise.Then{TCaptureReject, TResult, TReject}(Func{TResult}, TCaptureReject, Func{TCaptureReject, TReject, Promise{TResult}}, CancelationToken)"/>
        [MethodImpl(Internal.InlineOption)]
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise<TResult> Then<TCaptureReject, TResult, TReject>(Func<TResult> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise<TResult>> onRejected, CancelationToken cancelationToken = default)
            => AsPromise().Then(onResolved, rejectCaptureValue, onRejected, cancelationToken);

        /// <inheritdoc cref="Promise.Then{TCaptureResolve, TCaptureReject, TResult, TReject}(TCaptureResolve, Func{TCaptureResolve, TResult}, TCaptureReject, Func{TCaptureReject, TReject, Promise{TResult}}, CancelationToken)"/>
        [MethodImpl(Internal.InlineOption)]
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise<TResult> Then<TCaptureResolve, TCaptureReject, TResult, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, TResult> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise<TResult>> onRejected, CancelationToken cancelationToken = default)
            => AsPromise().Then(resolveCaptureValue, onResolved, rejectCaptureValue, onRejected, cancelationToken);

        /// <inheritdoc cref="Promise.Then{TCaptureResolve}(TCaptureResolve, Func{TCaptureResolve, Promise}, Action, CancelationToken)"/>
        [MethodImpl(Internal.InlineOption)]
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise Then<TCaptureResolve>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise> onResolved, Action onRejected, CancelationToken cancelationToken = default)
            => AsPromise().Then(resolveCaptureValue, onResolved, onRejected, cancelationToken);

        /// <inheritdoc cref="Promise.Then{TCaptureReject}(Func{Promise}, TCaptureReject, Action{TCaptureReject}, CancelationToken)"/>
        [MethodImpl(Internal.InlineOption)]
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise Then<TCaptureReject>(Func<Promise> onResolved, TCaptureReject rejectCaptureValue, Action<TCaptureReject> onRejected, CancelationToken cancelationToken = default)
            => AsPromise().Then(onResolved, rejectCaptureValue, onRejected, cancelationToken);

        /// <inheritdoc cref="Promise.Then{TCaptureResolve, TCaptureReject}(TCaptureResolve, Func{TCaptureResolve, Promise}, TCaptureReject, Action{TCaptureReject}, CancelationToken)"/>
        [MethodImpl(Internal.InlineOption)]
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise Then<TCaptureResolve, TCaptureReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise> onResolved, TCaptureReject rejectCaptureValue, Action<TCaptureReject> onRejected, CancelationToken cancelationToken = default)
            => AsPromise().Then(resolveCaptureValue, onResolved, rejectCaptureValue, onRejected, cancelationToken);

        /// <inheritdoc cref="Promise.Then{TCaptureResolve, TReject}(TCaptureResolve, Func{TCaptureResolve, Promise}, Action{TReject}, CancelationToken)"/>
        [MethodImpl(Internal.InlineOption)]
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise Then<TCaptureResolve, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise> onResolved, Action<TReject> onRejected, CancelationToken cancelationToken = default)
            => AsPromise().Then(resolveCaptureValue, onResolved, onRejected, cancelationToken);

        /// <inheritdoc cref="Promise.Then{TCaptureReject, TReject}(Func{Promise}, TCaptureReject, Action{TCaptureReject, TReject}, CancelationToken)"/>
        [MethodImpl(Internal.InlineOption)]
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise Then<TCaptureReject, TReject>(Func<Promise> onResolved, TCaptureReject rejectCaptureValue, Action<TCaptureReject, TReject> onRejected, CancelationToken cancelationToken = default)
            => AsPromise().Then(onResolved, rejectCaptureValue, onRejected, cancelationToken);

        /// <inheritdoc cref="Promise.Then{TCaptureResolve, TCaptureReject, TReject}(TCaptureResolve, Func{TCaptureResolve, Promise}, TCaptureReject, Action{TCaptureReject, TReject}, CancelationToken)"/>
        [MethodImpl(Internal.InlineOption)]
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise Then<TCaptureResolve, TCaptureReject, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise> onResolved, TCaptureReject rejectCaptureValue, Action<TCaptureReject, TReject> onRejected, CancelationToken cancelationToken = default)
            => AsPromise().Then(resolveCaptureValue, onResolved, rejectCaptureValue, onRejected, cancelationToken);

        /// <inheritdoc cref="Promise.Then{TCaptureResolve, TResult}(TCaptureResolve, Func{TCaptureResolve, Promise{TResult}}, Func{TResult}, CancelationToken)"/>
        [MethodImpl(Internal.InlineOption)]
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise<TResult> Then<TCaptureResolve, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise<TResult>> onResolved, Func<TResult> onRejected, CancelationToken cancelationToken = default)
            => AsPromise().Then(resolveCaptureValue, onResolved, onRejected, cancelationToken);

        /// <inheritdoc cref="Promise.Then{TCaptureReject, TResult}(Func{Promise{TResult}}, TCaptureReject, Func{TCaptureReject, TResult}, CancelationToken)"/>
        [MethodImpl(Internal.InlineOption)]
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise<TResult> Then<TCaptureReject, TResult>(Func<Promise<TResult>> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TResult> onRejected, CancelationToken cancelationToken = default)
            => AsPromise().Then(onResolved, rejectCaptureValue, onRejected, cancelationToken);

        /// <inheritdoc cref="Promise.Then{TCaptureResolve, TCaptureReject, TResult}(TCaptureResolve, Func{TCaptureResolve, Promise{TResult}}, TCaptureReject, Func{TCaptureReject, TResult}, CancelationToken)"/>
        [MethodImpl(Internal.InlineOption)]
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise<TResult> Then<TCaptureResolve, TCaptureReject, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise<TResult>> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TResult> onRejected, CancelationToken cancelationToken = default)
            => AsPromise().Then(resolveCaptureValue, onResolved, rejectCaptureValue, onRejected, cancelationToken);

        /// <inheritdoc cref="Promise.Then{TCaptureResolve, TResult, TReject}(TCaptureResolve, Func{TCaptureResolve, Promise{TResult}}, Func{TReject, TResult}, CancelationToken)"/>
        [MethodImpl(Internal.InlineOption)]
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise<TResult> Then<TCaptureResolve, TResult, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise<TResult>> onResolved, Func<TReject, TResult> onRejected, CancelationToken cancelationToken = default)
            => AsPromise().Then(resolveCaptureValue, onResolved, onRejected, cancelationToken);

        /// <inheritdoc cref="Promise.Then{TCaptureReject, TResult, TReject}(Func{Promise{TResult}}, TCaptureReject, Func{TCaptureReject, TReject, TResult}, CancelationToken)"/>
        [MethodImpl(Internal.InlineOption)]
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise<TResult> Then<TCaptureReject, TResult, TReject>(Func<Promise<TResult>> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, TResult> onRejected, CancelationToken cancelationToken = default)
            => AsPromise().Then(onResolved, rejectCaptureValue, onRejected, cancelationToken);

        /// <inheritdoc cref="Promise.Then{TCaptureResolve, TCaptureReject, TResult, TReject}(TCaptureResolve, Func{TCaptureResolve, Promise{TResult}}, TCaptureReject, Func{TCaptureReject, TReject, TResult}, CancelationToken)"/>
        [MethodImpl(Internal.InlineOption)]
        [Obsolete("Prefer WaitAsync(cancelationToken).Then(...) or ContinueWith", false), EditorBrowsable(EditorBrowsableState.Never)]
        public Promise<TResult> Then<TCaptureResolve, TCaptureReject, TResult, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise<TResult>> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, TResult> onRejected, CancelationToken cancelationToken = default)
            => AsPromise().Then(resolveCaptureValue, onResolved, rejectCaptureValue, onRejected, cancelationToken);
        #endregion
    }
}