#if !PROTO_PROMISE_PROGRESS_DISABLE
#define PROMISE_PROGRESS
#else
#undef PROMISE_PROGRESS
#endif

#pragma warning disable IDE0034 // Simplify 'default' expression
#pragma warning disable CS0618 // Type or member is obsolete

using System;
using System.Runtime.CompilerServices;

namespace Proto.Promises
{
    partial struct Promise
    {
        /// <summary>
        /// Deferred base. An instance of this can be used to report progress and reject or cancel the attached <see cref="Promise"/>.
        /// <para/>You must use <see cref="Deferred"/> or <see cref="Promise{T}.Deferred"/> to resolve the attached <see cref="Promise"/>.
        /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
        [System.Diagnostics.DebuggerNonUserCode]
#endif
        public
#if CSHARP_7_3_OR_NEWER
            readonly
#endif
            struct DeferredBase : ICancelable, IProgress<float>, IEquatable<DeferredBase>
        {
            private readonly Internal.PromiseRef.DeferredPromiseBase _ref;
            private readonly short _promiseId;
            private readonly short _deferredId;

            void IProgress<float>.Report(float value)
            {
                ReportProgress(value);
            }

            /// <summary>
            /// The attached <see cref="Promises.Promise"/> that this controls.
            /// </summary>
            public Promise Promise
            {
                [MethodImpl(Internal.InlineOption)]
                get
                {
                    return new Promise(_ref, _promiseId, 0);
                }
            }

            [Obsolete("Use IsValidAndPending.", false)]
            public bool IsValid
            {
                [MethodImpl(Internal.InlineOption)]
                get
                {
                    return IsValidAndPending;
                }
            }

            /// <summary>
            /// Get whether or not this instance is valid and the attached <see cref="Promise"/> is still pending.
            /// </summary>
            public bool IsValidAndPending
            {
                [MethodImpl(Internal.InlineOption)]
                get
                {
                    return Internal.PromiseRef.DeferredPromiseBase.GetIsValidAndPending(_ref, _deferredId);
                }
            }

            /// <summary>
            /// Internal use for implicit cast operator.
            /// </summary>
            [MethodImpl(Internal.InlineOption)]
            internal DeferredBase(Internal.PromiseRef.DeferredPromiseBase promise, short promiseId, short deferredId)
            {
                _ref = promise;
                _promiseId = promiseId;
                _deferredId = deferredId;
            }

            /// <summary>
            /// Cast this to <see cref="Deferred"/>. Throws an <see cref="InvalidCastException"/> if it cannot be casted.
            /// </summary>
            /// <exception cref="InvalidCastException"/>
            [MethodImpl(Internal.InlineOption)]
            public Deferred ToDeferred()
            {
                return new Deferred(
                    (Internal.PromiseRef.DeferredPromise<Internal.VoidResult>) _ref,
                    _promiseId,
                    _deferredId);
            }

            /// <summary>
            /// Cast this to <see cref="Deferred"/>. Returns an invalid <see cref="Deferred"/> if it cannot be casted.
            /// </summary>
            [MethodImpl(Internal.InlineOption)]
            public Deferred AsDeferred()
            {
                // If the cast fails, the new _promiseId must not == Internal.ValidIdFromApi, or Deferred.Promise will return a valid, resolved promise.
                var deferred = _ref as Internal.PromiseRef.DeferredPromise<Internal.VoidResult>;
                return new Deferred(
                    deferred,
                    deferred != null ? _promiseId : (short) 0,
                    _deferredId);
            }

            /// <summary>
            /// Cast this to <see cref="Promise{T}.Deferred"/>. Throws an <see cref="InvalidCastException"/> if it cannot be casted.
            /// </summary>
            /// <exception cref="InvalidCastException"/>
            [MethodImpl(Internal.InlineOption)]
            public Promise<T>.Deferred ToDeferred<T>()
            {
                return new Promise<T>.Deferred(
                    (Internal.PromiseRef.DeferredPromise<T>) _ref,
                    _promiseId,
                    _deferredId);
            }

            /// <summary>
            /// Cast this to <see cref="Promise{T}.Deferred"/>. Returns an invalid <see cref="Promise{T}.Deferred"/> if it cannot be casted.
            /// </summary>
            [MethodImpl(Internal.InlineOption)]
            public Promise<T>.Deferred AsDeferred<T>()
            {
                // If the cast fails, the new _promiseId must not == Internal.ValidIdFromApi, or Deferred.Promise will return a valid, resolved promise.
                var deferred = _ref as Internal.PromiseRef.DeferredPromise<T>;
                return new Promise<T>.Deferred(
                    deferred,
                    deferred != null ? _promiseId : (short) 0,
                    _deferredId);
            }

            /// <summary>
            /// Reject the linked <see cref="Promise"/> with <paramref name="reason"/>.
            /// </summary>
            /// <exception cref="InvalidOperationException"/>
            [MethodImpl(Internal.InlineOption)]
            public void Reject<TReject>(TReject reason)
            {
                if (!TryReject(reason))
                {
                    throw new InvalidOperationException("Deferred.Reject: instance is not valid.", Internal.GetFormattedStacktrace(1));
                }
            }

            /// <summary>
            /// Try to reject the linked <see cref="Promise"/> with <paramref name="reason"/>.
            /// <para/> Returns true if successful, false otherwise.
            /// </summary>
            [MethodImpl(Internal.InlineOption)]
            public bool TryReject<TReject>(TReject reason)
            {
                return Internal.PromiseRef.DeferredPromiseBase.TryReject(_ref, _deferredId, reason, 1);
            }

            /// <summary>
            /// Cancel the linked <see cref="Promise"/> without a reason.
            /// <para/>Note: This is not recommended. Instead, you should pass a <see cref="CancelationToken"/> into <see cref="New(CancelationToken)"/>.
            /// </summary>
            /// <exception cref="InvalidOperationException"/>
            [MethodImpl(Internal.InlineOption)]
            public void Cancel()
            {
                if (!TryCancel())
                {
                    throw new InvalidOperationException("Deferred.Reject: instance is not valid.", Internal.GetFormattedStacktrace(1));
                }
            }

            /// <summary>
            /// Try to cancel the linked <see cref="Promise"/> with <paramref name="reason"/>.
            /// <para/> Returns true if successful, false otherwise.
            /// <para/>Note: This is not recommended. Instead, you should pass a <see cref="CancelationToken"/> into <see cref="New(CancelationToken)"/>.
            /// </summary>
            [MethodImpl(Internal.InlineOption)]
            public bool TryCancel()
            {
                return Internal.PromiseRef.DeferredPromiseBase.TryCancel(_ref, _deferredId);
            }

            /// <summary>
            /// Report progress between 0 and 1.
            /// </summary>
            /// <exception cref="InvalidOperationException"/>
            /// <exception cref="ArgumentOutOfRangeException"/>
#if !PROMISE_PROGRESS
            [Obsolete("Progress is disabled. Remove PROTO_PROMISE_PROGRESS_DISABLE from your compiler symbols to enable progress reports.", false)]
#endif
            [MethodImpl(Internal.InlineOption)]
            public void ReportProgress(float progress)
            {
                if (!TryReportProgress(progress))
                {
                    throw new InvalidOperationException("Deferred.ReportProgress: instance is not valid.", Internal.GetFormattedStacktrace(1));
                }
            }

            /// <summary>
            /// Try to report progress between 0 and 1.
            /// <para/> Returns true if successful, false otherwise.
            /// </summary>
            /// <exception cref="ArgumentOutOfRangeException"/>
#if !PROMISE_PROGRESS
            [Obsolete("Progress is disabled. Remove PROTO_PROMISE_PROGRESS_DISABLE from your compiler symbols to enable progress reports.", false)]
#endif
            [MethodImpl(Internal.InlineOption)]
            public bool TryReportProgress(float progress)
            {
                return Internal.PromiseRef.DeferredPromiseBase.TryReportProgress(_ref, _deferredId, progress);
            }

            [MethodImpl(Internal.InlineOption)]
            public bool Equals(DeferredBase other)
            {
                return this == other;
            }

            public override bool Equals(object obj)
            {
#if CSHARP_7_3_OR_NEWER
                return obj is DeferredBase deferred && Equals(deferred);
#else
                return obj is DeferredBase && Equals((DeferredBase) obj);
#endif
            }

            [MethodImpl(Internal.InlineOption)]
            public override int GetHashCode()
            {
                return Internal.BuildHashCode(_ref, _deferredId.GetHashCode(), _promiseId.GetHashCode());
            }

            [MethodImpl(Internal.InlineOption)]
            public static bool operator ==(DeferredBase lhs, DeferredBase rhs)
            {
                return lhs._ref == rhs._ref
                    & lhs._deferredId == rhs._deferredId
                    & lhs._promiseId == rhs._promiseId;
            }

            [MethodImpl(Internal.InlineOption)]
            public static bool operator !=(DeferredBase lhs, DeferredBase rhs)
            {
                return !(lhs == rhs);
            }

            [Obsolete("Cancelation reasons are no longer supported. Use Cancel() instead.", true)]
            public void Cancel<TCancel>(TCancel reason)
            {
                throw new InvalidOperationException("Cancelation reasons are no longer supported. Use Cancel() instead.", Internal.GetFormattedStacktrace(1));
            }

            [Obsolete("Cancelation reasons are no longer supported. Use TryCancel() instead.", true)]
            public bool TryCancel<TCancel>(TCancel reason)
            {
                throw new InvalidOperationException("Cancelation reasons are no longer supported. Use TryCancel() instead.", Internal.GetFormattedStacktrace(1));
            }

            [Obsolete("DeferredBase.State is no longer valid. Use IsValidAndPending.", true)]
            public State State
            {
                get
                {
                    throw new InvalidOperationException("DeferredBase.State is no longer valid. Use IsValidAndPending.", Internal.GetFormattedStacktrace(1));
                }
            }

            [Obsolete("DeferredBase.Retain is no longer valid.", true)]
            public void Retain()
            {
                throw new InvalidOperationException("DeferredBase.Retain is no longer valid.", Internal.GetFormattedStacktrace(1));
            }

            [Obsolete("DeferredBase.Release is no longer valid.", true)]
            public void Release()
            {
                throw new InvalidOperationException("DeferredBase.Release is no longer valid.", Internal.GetFormattedStacktrace(1));
            }
        } // struct DeferredBase

        /// <summary>
        /// An instance of this is used to report progress and resolve, reject, or cancel the attached <see cref="Promise"/>.
        /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
        [System.Diagnostics.DebuggerNonUserCode]
#endif
        public
#if CSHARP_7_3_OR_NEWER
            readonly
#endif
            struct Deferred : ICancelable, IProgress<float>, IEquatable<Deferred>
        {
            private readonly Promise<Internal.VoidResult>.Deferred _target;

            void IProgress<float>.Report(float value)
            {
                _target.ReportProgress(value);
            }

            /// <summary>
            /// The attached <see cref="Promises.Promise"/> that this controls.
            /// </summary>
            public Promise Promise
            {
                [MethodImpl(Internal.InlineOption)]
                get
                {
                    return _target.Promise;
                }
            }

            [Obsolete("Use IsValidAndPending.", false)]
            public bool IsValid
            {
                [MethodImpl(Internal.InlineOption)]
                get
                {
                    return IsValidAndPending;
                }
            }

            /// <summary>
            /// Get whether or not this instance is valid and the attached <see cref="Promise"/> is still pending.
            /// </summary>
            public bool IsValidAndPending
            {
                [MethodImpl(Internal.InlineOption)]
                get
                {
                    return _target.IsValidAndPending;
                }
            }

            [MethodImpl(Internal.InlineOption)]
            private Deferred(Promise<Internal.VoidResult>.Deferred target)
            {
                _target = target;
            }

            /// <summary>
            /// Internal use.
            /// </summary>
            [MethodImpl(Internal.InlineOption)]
            internal Deferred(Internal.PromiseRef.DeferredPromise<Internal.VoidResult> promise, short promiseId, short deferredId)
            {
                _target = new Promise<Internal.VoidResult>.Deferred(promise, promiseId, deferredId);
            }

            /// <summary>
            /// Returns a new <see cref="Deferred"/> instance that is linked to and controls the state of a new <see cref="Promises.Promise"/>.
            /// <para/>If the <paramref name="cancelationToken"/> is canceled while the <see cref="Deferred"/> is pending, it and the <see cref="Promises.Promise"/> will be canceled with its reason.
            /// </summary>
            [MethodImpl(Internal.InlineOption)]
            public static Deferred New(CancelationToken cancelationToken = default(CancelationToken))
            {
                return new Deferred(Promise<Internal.VoidResult>.Deferred.New(cancelationToken));
            }

            /// <summary>
            /// Resolve the linked <see cref="Promise"/>.
            /// </summary>
            /// <exception cref="InvalidOperationException"/>
            [MethodImpl(Internal.InlineOption)]
            public void Resolve()
            {
                _target.Resolve(new Internal.VoidResult());
            }

            /// <summary>
            /// Try to resolve the linked <see cref="Promise"/>.
            /// <para/> Returns true if successful, false otherwise.
            /// </summary>
            [MethodImpl(Internal.InlineOption)]
            public bool TryResolve()
            {
                return _target.TryResolve(new Internal.VoidResult());
            }

            /// <summary>
            /// Reject the linked <see cref="Promise"/> with <paramref name="reason"/>.
            /// </summary>
            /// <exception cref="InvalidOperationException"/>
            [MethodImpl(Internal.InlineOption)]
            public void Reject<TReject>(TReject reason)
            {
                _target.Reject(reason);
            }

            /// <summary>
            /// Try to reject the linked <see cref="Promise"/> with <paramref name="reason"/>.
            /// <para/> Returns true if successful, false otherwise.
            /// </summary>
            [MethodImpl(Internal.InlineOption)]
            public bool TryReject<TReject>(TReject reason)
            {
                return _target.TryReject(reason);
            }

            /// <summary>
            /// Cancel the linked <see cref="Promise"/> without a reason.
            /// <para/>Note: This is not recommended. Instead, you should pass a <see cref="CancelationToken"/> into <see cref="New(CancelationToken)"/>.
            /// </summary>
            /// <exception cref="InvalidOperationException"/>
            [MethodImpl(Internal.InlineOption)]
            public void Cancel()
            {
                _target.Cancel();
            }

            /// <summary>
            /// Try to cancel the linked <see cref="Promise"/> with <paramref name="reason"/>.
            /// <para/> Returns true if successful, false otherwise.
            /// <para/>Note: This is not recommended. Instead, you should pass a <see cref="CancelationToken"/> into <see cref="New(CancelationToken)"/>.
            /// </summary>
            [MethodImpl(Internal.InlineOption)]
            public bool TryCancel()
            {
                return _target.TryCancel();
            }

            /// <summary>
            /// Report progress between 0 and 1.
            /// </summary>
            /// <exception cref="InvalidOperationException"/>
            /// <exception cref="ArgumentOutOfRangeException"/>
#if !PROMISE_PROGRESS
            [Obsolete("Progress is disabled. Remove PROTO_PROMISE_PROGRESS_DISABLE from your compiler symbols to enable progress reports.", false)]
#endif
            [MethodImpl(Internal.InlineOption)]
            public void ReportProgress(float progress)
            {
                _target.ReportProgress(progress);
            }

            /// <summary>
            /// Try to report progress between 0 and 1.
            /// <para/> Returns true if successful, false otherwise.
            /// </summary>
            /// <exception cref="ArgumentOutOfRangeException"/>
#if !PROMISE_PROGRESS
            [Obsolete("Progress is disabled. Remove PROTO_PROMISE_PROGRESS_DISABLE from your compiler symbols to enable progress reports.", false)]
#endif
            [MethodImpl(Internal.InlineOption)]
            public bool TryReportProgress(float progress)
            {
                return _target.TryReportProgress(progress);
            }

            /// <summary>
            /// Cast to <see cref="DeferredBase"/>.
            /// </summary>
            [MethodImpl(Internal.InlineOption)]
            public static implicit operator DeferredBase(Deferred rhs)
            {
                return new DeferredBase(rhs._target._ref, rhs._target._promiseId, rhs._target._deferredId);
            }

            /// <summary>
            /// Cast <see cref="DeferredBase"/> to <see cref="Deferred"/>.
            /// </summary>
            [MethodImpl(Internal.InlineOption)]
            public static explicit operator Deferred(DeferredBase rhs)
            {
                return rhs.ToDeferred();
            }

            [MethodImpl(Internal.InlineOption)]
            public bool Equals(Deferred other)
            {
                return _target == other._target;
            }

            public override bool Equals(object obj)
            {
#if CSHARP_7_3_OR_NEWER
                return obj is Deferred deferred && Equals(deferred);
#else
                return obj is Deferred && Equals((Deferred) obj);
#endif
            }

            [MethodImpl(Internal.InlineOption)]
            public override int GetHashCode()
            {
                return _target.GetHashCode();
            }

            [MethodImpl(Internal.InlineOption)]
            public static bool operator ==(Deferred lhs, Deferred rhs)
            {
                return lhs._target == rhs._target;
            }

            [MethodImpl(Internal.InlineOption)]
            public static bool operator !=(Deferred lhs, Deferred rhs)
            {
                return !(lhs == rhs);
            }

            [Obsolete("Cancelation reasons are no longer supported. Use Cancel() instead.", true)]
            public void Cancel<TCancel>(TCancel reason)
            {
                throw new InvalidOperationException("Cancelation reasons are no longer supported. Use Cancel() instead.", Internal.GetFormattedStacktrace(1));
            }

            [Obsolete("Cancelation reasons are no longer supported. Use TryCancel() instead.", true)]
            public bool TryCancel<TCancel>(TCancel reason)
            {
                throw new InvalidOperationException("Cancelation reasons are no longer supported. Use TryCancel() instead.", Internal.GetFormattedStacktrace(1));
            }

            [Obsolete("Deferred.State is no longer valid. Use IsValidAndPending.", true)]
            public State State
            {
                get
                {
                    throw new InvalidOperationException("Deferred.State is no longer valid. Use IsValidAndPending.", Internal.GetFormattedStacktrace(1));
                }
            }

            [Obsolete("Deferred.Retain is no longer valid.", true)]
            public void Retain()
            {
                throw new InvalidOperationException("Deferred.Retain is no longer valid.", Internal.GetFormattedStacktrace(1));
            }

            [Obsolete("Deferred.Release is no longer valid.", true)]
            public void Release()
            {
                throw new InvalidOperationException("Deferred.Release is no longer valid.", Internal.GetFormattedStacktrace(1));
            }
        } // struct Deferred
    } // struct Promise

    public partial struct Promise<T>
    {
        /// <summary>
        /// An instance of this is used to report progress and resolve, reject, or cancel the attached <see cref="Promise"/>.
        /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
        [System.Diagnostics.DebuggerNonUserCode]
#endif
        public
#if CSHARP_7_3_OR_NEWER
            readonly
#endif
            struct Deferred : ICancelable, IProgress<float>, IEquatable<Deferred>
        {
            internal readonly Internal.PromiseRef.DeferredPromise<T> _ref;
            internal readonly short _promiseId;
            internal readonly short _deferredId;

            void IProgress<float>.Report(float value)
            {
                ReportProgress(value);
            }

            /// <summary>
            /// The attached <see cref="Promise{T}"/> that this controls.
            /// </summary>
            public Promise<T> Promise
            {
                [MethodImpl(Internal.InlineOption)]
                get
                {
                    return new Promise<T>(_ref, _promiseId, 0);
                }
            }

            [Obsolete("Use IsValidAndPending.", false)]
            public bool IsValid
            {
                [MethodImpl(Internal.InlineOption)]
                get
                {
                    return IsValidAndPending;
                }
            }

            /// <summary>
            /// Get whether or not this instance is valid and the attached <see cref="Promise"/> is still pending.
            /// </summary>
            public bool IsValidAndPending
            {
                [MethodImpl(Internal.InlineOption)]
                get
                {
                    return Internal.PromiseRef.DeferredPromiseBase.GetIsValidAndPending(_ref, _deferredId);
                }
            }

            /// <summary>
            /// Internal use.
            /// </summary>
            [MethodImpl(Internal.InlineOption)]
            internal Deferred(Internal.PromiseRef.DeferredPromise<T> promise, short promiseId, short deferredId)
            {
                _ref = promise;
                _promiseId = promiseId;
                _deferredId = deferredId;
            }

            /// <summary>
            /// Returns a new <see cref="Deferred"/> instance that is linked to and controls the state of a new <see cref="Promises.Promise"/>.
            /// <para/>If the <paramref name="cancelationToken"/> is canceled while the <see cref="Deferred"/> is pending, it and the <see cref="Promises.Promise"/> will be canceled with its reason.
            /// </summary>
            public static Deferred New(CancelationToken cancelationToken = default(CancelationToken))
            {
                Internal.PromiseRef.DeferredPromise<T> promise = cancelationToken.CanBeCanceled
                    ? Internal.PromiseRef.DeferredPromiseCancel<T>.GetOrCreate(cancelationToken)
                    : Internal.PromiseRef.DeferredPromise<T>.GetOrCreate();
                return new Deferred(promise, promise.Id, promise.DeferredId);
            }

            /// <summary>
            /// Resolve the linked <see cref="Promise{T}"/> with <paramref name="value"/>.
            /// </summary>
            /// <exception cref="InvalidOperationException"/>
            [MethodImpl(Internal.InlineOption)]
            public void Resolve(T value)
            {
                if (!TryResolve(value))
                {
                    throw new InvalidOperationException("Deferred.Resolve: instance is not valid.", Internal.GetFormattedStacktrace(1));
                }
            }

            /// <summary>
            /// Try to resolve the linked <see cref="Promise{T}"/> with <paramref name="value"/>.
            /// <para/> Returns true if successful, false otherwise.
            /// </summary>
            [MethodImpl(Internal.InlineOption)]
            public bool TryResolve(T value)
            {
                return Internal.PromiseRef.DeferredPromise<T>.TryResolve(_ref, _deferredId, value);
            }

            /// <summary>
            /// Reject the linked <see cref="Promise"/> with <paramref name="reason"/>.
            /// </summary>
            /// <exception cref="InvalidOperationException"/>
            [MethodImpl(Internal.InlineOption)]
            public void Reject<TReject>(TReject reason)
            {
                if (!TryReject(reason))
                {
                    throw new InvalidOperationException("Deferred.Reject: instance is not valid.", Internal.GetFormattedStacktrace(1));
                }
            }

            /// <summary>
            /// Try to reject the linked <see cref="Promise"/> with <paramref name="reason"/>.
            /// <para/> Returns true if successful, false otherwise.
            /// </summary>
            [MethodImpl(Internal.InlineOption)]
            public bool TryReject<TReject>(TReject reason)
            {
                return Internal.PromiseRef.DeferredPromiseBase.TryReject(_ref, _deferredId, reason, 1);
            }

            /// <summary>
            /// Cancel the linked <see cref="Promise"/> without a reason.
            /// <para/>Note: This is not recommended. Instead, you should pass a <see cref="CancelationToken"/> into <see cref="New(CancelationToken)"/>.
            /// </summary>
            /// <exception cref="InvalidOperationException"/>
            [MethodImpl(Internal.InlineOption)]
            public void Cancel()
            {
                if (!TryCancel())
                {
                    throw new InvalidOperationException("Deferred.Reject: instance is not valid.", Internal.GetFormattedStacktrace(1));
                }
            }

            /// <summary>
            /// Try to cancel the linked <see cref="Promise"/> with <paramref name="reason"/>.
            /// <para/> Returns true if successful, false otherwise.
            /// <para/>Note: This is not recommended. Instead, you should pass a <see cref="CancelationToken"/> into <see cref="New(CancelationToken)"/>.
            /// </summary>
            [MethodImpl(Internal.InlineOption)]
            public bool TryCancel()
            {
                return Internal.PromiseRef.DeferredPromiseBase.TryCancel(_ref, _deferredId);
            }

            /// <summary>
            /// Report progress between 0 and 1.
            /// </summary>
            /// <exception cref="InvalidOperationException"/>
            /// <exception cref="ArgumentOutOfRangeException"/>
#if !PROMISE_PROGRESS
            [Obsolete("Progress is disabled. Remove PROTO_PROMISE_PROGRESS_DISABLE from your compiler symbols to enable progress reports.", false)]
#endif
            [MethodImpl(Internal.InlineOption)]
            public void ReportProgress(float progress)
            {
                if (!TryReportProgress(progress))
                {
                    throw new InvalidOperationException("Deferred.ReportProgress: instance is not valid.", Internal.GetFormattedStacktrace(1));
                }
            }

            /// <summary>
            /// Try to report progress between 0 and 1.
            /// <para/> Returns true if successful, false otherwise.
            /// </summary>
            /// <exception cref="ArgumentOutOfRangeException"/>
#if !PROMISE_PROGRESS
            [Obsolete("Progress is disabled. Remove PROTO_PROMISE_PROGRESS_DISABLE from your compiler symbols to enable progress reports.", false)]
#endif
            [MethodImpl(Internal.InlineOption)]
            public bool TryReportProgress(float progress)
            {
                return Internal.PromiseRef.DeferredPromiseBase.TryReportProgress(_ref, _deferredId, progress);
            }

            /// <summary>
            /// Cast to <see cref="Promise.DeferredBase"/>.
            /// </summary>
            [MethodImpl(Internal.InlineOption)]
            public static implicit operator Promise.DeferredBase(Deferred rhs)
            {
                return new Promise.DeferredBase(rhs._ref, rhs._promiseId, rhs._deferredId);
            }

            /// <summary>
            /// Cast <see cref="Promise{T}.DeferredBase"/> to <see cref="Deferred"/>.
            /// </summary>
            [MethodImpl(Internal.InlineOption)]
            public static explicit operator Deferred(Promise.DeferredBase rhs)
            {
                return rhs.ToDeferred<T>();
            }

            [MethodImpl(Internal.InlineOption)]
            public bool Equals(Deferred other)
            {
                return this == other;
            }

            public override bool Equals(object obj)
            {
#if CSHARP_7_3_OR_NEWER
                return obj is Deferred deferred && Equals(deferred);
#else
                return obj is Deferred && Equals((Deferred) obj);
#endif
            }

            [MethodImpl(Internal.InlineOption)]
            public override int GetHashCode()
            {
                return Internal.BuildHashCode(_ref, _deferredId.GetHashCode(), _promiseId.GetHashCode());
            }

            [MethodImpl(Internal.InlineOption)]
            public static bool operator ==(Deferred lhs, Deferred rhs)
            {
                return lhs._ref == rhs._ref
                    & lhs._deferredId == rhs._deferredId
                    & lhs._promiseId == rhs._promiseId;
            }

            [MethodImpl(Internal.InlineOption)]
            public static bool operator !=(Deferred lhs, Deferred rhs)
            {
                return !(lhs == rhs);
            }

            [Obsolete("Cancelation reasons are no longer supported. Use Cancel() instead.", true)]
            public void Cancel<TCancel>(TCancel reason)
            {
                throw new InvalidOperationException("Cancelation reasons are no longer supported. Use Cancel() instead.", Internal.GetFormattedStacktrace(1));
            }

            [Obsolete("Cancelation reasons are no longer supported. Use TryCancel() instead.", true)]
            public bool TryCancel<TCancel>(TCancel reason)
            {
                throw new InvalidOperationException("Cancelation reasons are no longer supported. Use TryCancel() instead.", Internal.GetFormattedStacktrace(1));
            }

            [Obsolete("Deferred.State is no longer valid. Use IsValidAndPending.", true)]
            public Promise.State State
            {
                get
                {
                    throw new InvalidOperationException("Deferred.State is no longer valid. Use IsValidAndPending.", Internal.GetFormattedStacktrace(1));
                }
            }

            [Obsolete("Deferred.Retain is no longer valid.", true)]
            public void Retain()
            {
                throw new InvalidOperationException("Deferred.Retain is no longer valid.", Internal.GetFormattedStacktrace(1));
            }

            [Obsolete("Deferred.Release is no longer valid.", true)]
            public void Release()
            {
                throw new InvalidOperationException("Deferred.Release is no longer valid.", Internal.GetFormattedStacktrace(1));
            }
        } // struct Deferred
    } // struct Promise<T>
} // namespace Proto.Promises