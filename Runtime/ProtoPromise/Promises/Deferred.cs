#if !PROTO_PROMISE_PROGRESS_DISABLE
#define PROMISE_PROGRESS
#else
#undef PROMISE_PROGRESS
#endif

#pragma warning disable IDE0034 // Simplify 'default' expression

using System;

namespace Proto.Promises
{
    partial struct Promise
    {
        /// <summary>
        /// Deferred base. An instance of this can be used to report progress and reject the attached <see cref="Promises.Promise"/>.
        /// <para/>You must use <see cref="Deferred"/> or <see cref="Promise{T}.Deferred"/> to resolve the attached <see cref="Promises.Promise"/>.
        /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
        [System.Diagnostics.DebuggerNonUserCode]
#endif
        public
#if CSHARP_7_3_OR_NEWER
            readonly
#endif
            struct DeferredBase : IEquatable<DeferredBase>
        {
            private readonly Internal.PromiseRef.DeferredPromiseBase _ref;
            private readonly short _promiseId;
            private readonly short _deferredId;

            /// <summary>
            /// The attached <see cref="Promises.Promise"/> that this controls.
            /// </summary>
            public Promise Promise
            {
                get
                {
                    return new Promise(_ref, _promiseId);
                }
            }

            [Obsolete("Use IsValidAndPending.", false)]
            public bool IsValid
            {
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
                get
                {
                    return _ref != null && _deferredId == _ref.DeferredId;
                }
            }

            /// <summary>
            /// Internal use for implicit cast operator.
            /// </summary>
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
            public Deferred ToDeferred()
            {
                return new Deferred((Internal.PromiseRef.DeferredPromiseVoid) _ref, _promiseId, _deferredId);
            }

            /// <summary>
            /// Cast this to <see cref="Deferred"/>. Returns an invalid <see cref="Deferred"/> if it cannot be casted.
            /// </summary>
            public Deferred AsDeferred()
            {
                return new Deferred(_ref as Internal.PromiseRef.DeferredPromiseVoid, _promiseId, _deferredId);
            }

            /// <summary>
            /// Cast this to <see cref="Promise{T}.Deferred"/>. Throws an <see cref="InvalidCastException"/> if it cannot be casted.
            /// </summary>
            /// <exception cref="InvalidCastException"/>
            public Promise<T>.Deferred ToDeferred<T>()
            {
                return new Promise<T>.Deferred((Internal.PromiseRef.DeferredPromise<T>) _ref, _promiseId, _deferredId);
            }

            /// <summary>
            /// Cast this to <see cref="Promise{T}.Deferred"/>. Returns an invalid <see cref="Promise{T}.Deferred"/> if it cannot be casted.
            /// </summary>
            public Promise<T>.Deferred AsDeferred<T>()
            {
                return new Promise<T>.Deferred(_ref as Internal.PromiseRef.DeferredPromise<T>, _promiseId, _deferredId);
            }

            /// <summary>
            /// Reject the linked <see cref="Promise"/> with <paramref name="reason"/>.
            /// </summary>
            /// <exception cref="InvalidOperationException"/>
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
            public bool TryReject<TReject>(TReject reason)
            {
                return _ref != null && _ref.TryReject(ref reason, _deferredId, 1);
            }

            /// <summary>
            /// Cancel the linked <see cref="Promise"/> with <paramref name="reason"/>.
            /// <para/>Note: This is not recommended. Instead, you should pass a <see cref="CancelationToken"/> into <see cref="Deferred.New(CancelationToken)"/>.
            /// </summary>
            /// <exception cref="InvalidOperationException"/>
            public void Cancel<TCancel>(TCancel reason)
            {
                if (!TryCancel(reason))
                {
                    throw new InvalidOperationException("Deferred.Cancel: instance is not valid.", Internal.GetFormattedStacktrace(1));
                }
            }

            /// <summary>
            /// Try to cancel the linked <see cref="Promise"/> with <paramref name="reason"/>.
            /// <para/> Returns true if successful, false otherwise.
            /// <para/>Note: This is not recommended. Instead, you should pass a <see cref="CancelationToken"/> into <see cref="Deferred.New(CancelationToken)"/>.
            /// </summary>
            public bool TryCancel<TCancel>(TCancel reason)
            {
                return _ref != null && _ref.TryCancel(ref reason, _deferredId);
            }

            /// <summary>
            /// Cancel the linked <see cref="Promise"/> without a reason.
            /// <para/>Note: This is not recommended. Instead, you should pass a <see cref="CancelationToken"/> into <see cref="Deferred.New(CancelationToken)"/>.
            /// </summary>
            /// <exception cref="InvalidOperationException"/>
            public void Cancel()
            {
                if (!TryCancel())
                {
                    throw new InvalidOperationException("Deferred.Cancel: instance is not valid.", Internal.GetFormattedStacktrace(1));
                }
            }

            /// <summary>
            /// Try to cancel the linked <see cref="Promise"/> with <paramref name="reason"/>.
            /// <para/> Returns true if successful, false otherwise.
            /// <para/>Note: This is not recommended. Instead, you should pass a <see cref="CancelationToken"/> into <see cref="Deferred.New(CancelationToken)"/>.
            /// </summary>
            public bool TryCancel()
            {
                return _ref != null && _ref.TryCancel(_deferredId);
            }

            /// <summary>
            /// Report progress between 0 and 1.
            /// </summary>
            /// <exception cref="InvalidOperationException"/>
            /// <exception cref="ArgumentOutOfRangeException"/>
#if !PROMISE_PROGRESS
            [System.Obsolete("Progress is disabled. Remove PROTO_PROMISE_PROGRESS_DISABLE from your compiler symbols to enable progress reports.", true)]
#endif
            public void ReportProgress(float progress)
            {
#if !PROMISE_PROGRESS
                Internal.ThrowProgressException(1);
#else
                if (!TryReportProgress(progress))
                {
                    throw new InvalidOperationException("Deferred.ReportProgress: instance is not valid.", Internal.GetFormattedStacktrace(1));
                }
#endif
            }

            /// <summary>
            /// Try to report progress between 0 and 1.
            /// <para/> Returns true if successful, false otherwise.
            /// </summary>
            /// <exception cref="ArgumentOutOfRangeException"/>
#if !PROMISE_PROGRESS
            [System.Obsolete("Progress is disabled, this will always return false. Remove PROTO_PROMISE_PROGRESS_DISABLE from your compiler symbols to enable progress reports.", false)]
#endif
            public bool TryReportProgress(float progress)
            {
#if !PROMISE_PROGRESS
                return false;
#else
                ValidateProgress(progress, 1);

                return _ref != null && _ref.TryReportProgress(progress, _deferredId);
#endif
            }

            public bool Equals(DeferredBase other)
            {
                return this == other;
            }

            public override bool Equals(object obj)
            {
#if CSHARP_7_OR_LATER
                return obj is DeferredBase deferredBase && Equals(deferredBase);
#else
                return obj is DeferredBase && Equals((DeferredBase) obj);
#endif
            }

            public override int GetHashCode()
            {
                var temp = _ref;
                if (temp == null)
                {
                    return 0;
                }
                unchecked
                {
                    int hash = 17;
                    hash = hash * 31 + _deferredId.GetHashCode();
                    hash = hash * 31 + _promiseId.GetHashCode();
                    hash = hash * 31 + temp.GetHashCode();
                    return hash;
                }
            }

            public static bool operator ==(DeferredBase lhs, DeferredBase rhs)
            {
                return lhs._ref == rhs._ref & lhs._deferredId == rhs._deferredId & lhs._promiseId == rhs._promiseId;
            }

            public static bool operator !=(DeferredBase lhs, DeferredBase rhs)
            {
                return !(lhs == rhs);
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
        }

        /// <summary>
        /// An instance of this is used to report progress and resolve or reject the attached <see cref="Promises.Promise"/>.
        /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
        [System.Diagnostics.DebuggerNonUserCode]
#endif
        public
#if CSHARP_7_3_OR_NEWER
            readonly
#endif
            struct Deferred : IEquatable<Deferred>
        {
            private readonly Internal.PromiseRef.DeferredPromiseVoid _ref;
            private readonly short _promiseId;
            private readonly short _deferredId;

            /// <summary>
            /// The attached <see cref="Promises.Promise"/> that this controls.
            /// </summary>
            public Promise Promise
            {
                get
                {
                    return new Promise(_ref, _promiseId);
                }
            }

            [Obsolete("Use IsValidAndPending.", false)]
            public bool IsValid
            {
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
                get
                {
                    return _ref != null && _deferredId == _ref.DeferredId;
                }
            }

            /// <summary>
            /// Internal use.
            /// </summary>
            internal Deferred(Internal.PromiseRef.DeferredPromiseVoid promise, short promiseId, short deferredId)
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
                var promise = cancelationToken.CanBeCanceled
                    ? Internal.PromiseRef.DeferredPromiseVoidCancel.GetOrCreate(cancelationToken)
                    : Internal.PromiseRef.DeferredPromiseVoid.GetOrCreate();
                return new Deferred(promise, promise.Id, promise.DeferredId);
            }

            /// <summary>
            /// Reject the linked <see cref="Promise"/> with <paramref name="reason"/>.
            /// </summary>
            /// <exception cref="InvalidOperationException"/>
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
            public bool TryReject<TReject>(TReject reason)
            {
                return _ref != null && _ref.TryReject(ref reason, _deferredId, 1);
            }

            /// <summary>
            /// Cancel the linked <see cref="Promise"/> with <paramref name="reason"/>.
            /// <para/>Note: This is not recommended. Instead, you should pass a <see cref="CancelationToken"/> into <see cref="Deferred.New(CancelationToken)"/>.
            /// </summary>
            /// <exception cref="InvalidOperationException"/>
            public void Cancel<TCancel>(TCancel reason)
            {
                if (!TryCancel(reason))
                {
                    throw new InvalidOperationException("Deferred.Reject: instance is not valid.", Internal.GetFormattedStacktrace(1));
                }
            }

            /// <summary>
            /// Try to cancel the linked <see cref="Promise"/> with <paramref name="reason"/>.
            /// <para/> Returns true if successful, false otherwise.
            /// <para/>Note: This is not recommended. Instead, you should pass a <see cref="CancelationToken"/> into <see cref="Deferred.New(CancelationToken)"/>.
            /// </summary>
            public bool TryCancel<TCancel>(TCancel reason)
            {
                return _ref != null && _ref.TryCancel(ref reason, _deferredId);
            }

            /// <summary>
            /// Cancel the linked <see cref="Promise"/> without a reason.
            /// <para/>Note: This is not recommended. Instead, you should pass a <see cref="CancelationToken"/> into <see cref="Deferred.New(CancelationToken)"/>.
            /// </summary>
            /// <exception cref="InvalidOperationException"/>
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
            /// <para/>Note: This is not recommended. Instead, you should pass a <see cref="CancelationToken"/> into <see cref="Deferred.New(CancelationToken)"/>.
            /// </summary>
            public bool TryCancel()
            {
                return _ref != null && _ref.TryCancel(_deferredId);
            }

            /// <summary>
            /// Report progress between 0 and 1.
            /// </summary>
            /// <exception cref="InvalidOperationException"/>
            /// <exception cref="ArgumentOutOfRangeException"/>
#if !PROMISE_PROGRESS
            [System.Obsolete("Progress is disabled. Remove PROTO_PROMISE_PROGRESS_DISABLE from your compiler symbols to enable progress reports.", true)]
#endif
            public void ReportProgress(float progress)
            {
#if !PROMISE_PROGRESS
                Internal.ThrowProgressException(1);
#else
                if (!TryReportProgress(progress))
                {
                    throw new InvalidOperationException("Deferred.ReportProgress: instance is not valid.", Internal.GetFormattedStacktrace(1));
                }
#endif
            }

            /// <summary>
            /// Try to report progress between 0 and 1.
            /// <para/> Returns true if successful, false otherwise.
            /// </summary>
            /// <exception cref="ArgumentOutOfRangeException"/>
#if !PROMISE_PROGRESS
            [System.Obsolete("Progress is disabled, this will always return false. Remove PROTO_PROMISE_PROGRESS_DISABLE from your compiler symbols to enable progress reports.", false)]
#endif
            public bool TryReportProgress(float progress)
            {
#if !PROMISE_PROGRESS
                return false;
#else
                ValidateProgress(progress, 1);

                return _ref != null && _ref.TryReportProgress(progress, _deferredId);
#endif
            }

            /// <summary>
            /// Resolve the linked <see cref="Promise"/>.
            /// </summary>
            /// <exception cref="InvalidOperationException"/>
            public void Resolve()
            {
                if (!TryResolve())
                {
                    throw new InvalidOperationException("Deferred.Resolve: instance is not valid.", Internal.GetFormattedStacktrace(1));
                }
            }

            /// <summary>
            /// Try to resolve the linked <see cref="Promise"/>.
            /// <para/> Returns true if successful, false otherwise.
            /// </summary>
            public bool TryResolve()
            {
                return _ref != null && _ref.TryResolve(_deferredId);
            }

            /// <summary>
            /// Cast to <see cref="DeferredBase"/>.
            /// </summary>
            public static implicit operator DeferredBase(Deferred rhs)
            {
                return new DeferredBase(rhs._ref, rhs._promiseId, rhs._deferredId);
            }

            public bool Equals(Deferred other)
            {
                return this == other;
            }

            public override bool Equals(object obj)
            {
#if CSHARP_7_OR_LATER
                return obj is Deferred deferred && Equals(deferred);
#else
                return obj is Deferred && Equals((Deferred) obj);
#endif
            }

            public override int GetHashCode()
            {
                var temp = _ref;
                if (temp == null)
                {
                    return 0;
                }
                unchecked
                {
                    int hash = 17;
                    hash = hash * 31 + _deferredId.GetHashCode();
                    hash = hash * 31 + _promiseId.GetHashCode();
                    hash = hash * 31 + temp.GetHashCode();
                    return hash;
                }
            }

            public static bool operator ==(Deferred lhs, Deferred rhs)
            {
                return lhs._ref == rhs._ref & lhs._deferredId == rhs._deferredId & lhs._promiseId == rhs._promiseId;
            }

            public static bool operator !=(Deferred lhs, Deferred rhs)
            {
                return !(lhs == rhs);
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
        }
    }

    public partial struct Promise<T>
    {
        /// <summary>
        /// An instance of this is used to handle the state of the <see cref="Promise"/>.
        /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
        [System.Diagnostics.DebuggerNonUserCode]
#endif
        public
#if CSHARP_7_3_OR_NEWER
            readonly
#endif
            struct Deferred : IEquatable<Deferred>
        {
            private readonly Internal.PromiseRef.DeferredPromise<T> _ref;
            private readonly short _promiseId;
            private readonly short _deferredId;

            /// <summary>
            /// The attached <see cref="Promise{T}"/> that this controls.
            /// </summary>
            public Promise<T> Promise
            {
                get
                {
                    return new Promise<T>(_ref, _promiseId);
                }
            }

            [Obsolete("Use IsValidAndPending.", false)]
            public bool IsValid
            {
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
                get
                {
                    return _ref != null && _deferredId == _ref.DeferredId;
                }
            }

            /// <summary>
            /// Internal use.
            /// </summary>
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
                var promise = cancelationToken.CanBeCanceled
                    ? Internal.PromiseRef.DeferredPromiseCancel<T>.GetOrCreate(cancelationToken)
                    : Internal.PromiseRef.DeferredPromise<T>.GetOrCreate();
                return new Deferred(promise, promise.Id, promise.DeferredId);
            }

            /// <summary>
            /// Reject the linked <see cref="Promise"/> with <paramref name="reason"/>.
            /// </summary>
            /// <exception cref="InvalidOperationException"/>
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
            public bool TryReject<TReject>(TReject reason)
            {
                return _ref != null && _ref.TryReject(ref reason, _deferredId, 1);
            }

            /// <summary>
            /// Cancel the linked <see cref="Promise"/> with <paramref name="reason"/>.
            /// <para/>Note: This is not recommended. Instead, you should pass a <see cref="CancelationToken"/> into <see cref="Deferred.New(CancelationToken)"/>.
            /// </summary>
            /// <exception cref="InvalidOperationException"/>
            public void Cancel<TCancel>(TCancel reason)
            {
                if (!TryCancel(reason))
                {
                    throw new InvalidOperationException("Deferred.Reject: instance is not valid.", Internal.GetFormattedStacktrace(1));
                }
            }

            /// <summary>
            /// Try to cancel the linked <see cref="Promise"/> with <paramref name="reason"/>.
            /// <para/> Returns true if successful, false otherwise.
            /// <para/>Note: This is not recommended. Instead, you should pass a <see cref="CancelationToken"/> into <see cref="Deferred.New(CancelationToken)"/>.
            /// </summary>
            public bool TryCancel<TCancel>(TCancel reason)
            {
                return _ref != null && _ref.TryCancel(ref reason, _deferredId);
            }

            /// <summary>
            /// Cancel the linked <see cref="Promise"/> without a reason.
            /// <para/>Note: This is not recommended. Instead, you should pass a <see cref="CancelationToken"/> into <see cref="Deferred.New(CancelationToken)"/>.
            /// </summary>
            /// <exception cref="InvalidOperationException"/>
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
            /// <para/>Note: This is not recommended. Instead, you should pass a <see cref="CancelationToken"/> into <see cref="Deferred.New(CancelationToken)"/>.
            /// </summary>
            public bool TryCancel()
            {
                return _ref != null && _ref.TryCancel(_deferredId);
            }

            /// <summary>
            /// Report progress between 0 and 1.
            /// </summary>
            /// <exception cref="InvalidOperationException"/>
            /// <exception cref="ArgumentOutOfRangeException"/>
#if !PROMISE_PROGRESS
            [System.Obsolete("Progress is disabled. Remove PROTO_PROMISE_PROGRESS_DISABLE from your compiler symbols to enable progress reports.", true)]
#endif
            public void ReportProgress(float progress)
            {
#if !PROMISE_PROGRESS
                Internal.ThrowProgressException(1);
#else
                if (!TryReportProgress(progress))
                {
                    throw new InvalidOperationException("Deferred.ReportProgress: instance is not valid.", Internal.GetFormattedStacktrace(1));
                }
#endif
            }

            /// <summary>
            /// Try to report progress between 0 and 1.
            /// <para/> Returns true if successful, false otherwise.
            /// </summary>
            /// <exception cref="ArgumentOutOfRangeException"/>
#if !PROMISE_PROGRESS
            [System.Obsolete("Progress is disabled, this will always return false. Remove PROTO_PROMISE_PROGRESS_DISABLE from your compiler symbols to enable progress reports.", false)]
#endif
            public bool TryReportProgress(float progress)
            {
#if !PROMISE_PROGRESS
                return false;
#else
                ValidateProgress(progress, 1);

                return _ref != null && _ref.TryReportProgress(progress, _deferredId);
#endif
            }

            /// <summary>
            /// Resolve the linked <see cref="Promise{T}"/> with <paramref name="value"/>.
            /// </summary>
            /// <exception cref="InvalidOperationException"/>
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
            public bool TryResolve(T value)
            {
                return _ref != null && _ref.TryResolve(ref value, _deferredId);
            }

            /// <summary>
            /// Cast to <see cref="Promise.DeferredBase"/>.
            /// </summary>
            public static implicit operator Promise.DeferredBase(Deferred rhs)
            {
                return new Promise.DeferredBase(rhs._ref, rhs._promiseId, rhs._deferredId);
            }

            public bool Equals(Deferred other)
            {
                return this == other;
            }

            public override bool Equals(object obj)
            {
#if CSHARP_7_OR_LATER
                return obj is Deferred deferred && Equals(deferred);
#else
                return obj is Deferred && Equals((Deferred) obj);
#endif
            }

            public override int GetHashCode()
            {
                var temp = _ref;
                if (temp == null)
                {
                    return 0;
                }
                unchecked
                {
                    int hash = 17;
                    hash = hash * 31 + _deferredId.GetHashCode();
                    hash = hash * 31 + _promiseId.GetHashCode();
                    hash = hash * 31 + temp.GetHashCode();
                    return hash;
                }
            }

            public static bool operator ==(Deferred lhs, Deferred rhs)
            {
                return lhs._ref == rhs._ref & lhs._deferredId == rhs._deferredId & lhs._promiseId == rhs._promiseId;
            }

            public static bool operator !=(Deferred lhs, Deferred rhs)
            {
                return !(lhs == rhs);
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
        }
    }
}