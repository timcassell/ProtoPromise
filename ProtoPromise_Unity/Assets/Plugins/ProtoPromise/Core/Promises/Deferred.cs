#if !PROTO_PROMISE_PROGRESS_DISABLE
#define PROMISE_PROGRESS
#else
#undef PROMISE_PROGRESS
#endif

#pragma warning disable IDE0034 // Simplify 'default' expression

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
            struct DeferredBase : IProgress<float>, IEquatable<DeferredBase>
        {
            private readonly Internal.DeferredInternal<Internal.PromiseRef.DeferredPromiseBase> _target;

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
                    return _target.GetPromiseVoid();
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

            /// <summary>
            /// Internal use for implicit cast operator.
            /// </summary>
            [MethodImpl(Internal.InlineOption)]
            internal DeferredBase(Internal.PromiseRef.DeferredPromiseBase promise, short promiseId, short deferredId)
            {
                _target = new Internal.DeferredInternal<Internal.PromiseRef.DeferredPromiseBase>(promise, promiseId, deferredId);
            }

            /// <summary>
            /// Cast this to <see cref="Deferred"/>. Throws an <see cref="InvalidCastException"/> if it cannot be casted.
            /// </summary>
            /// <exception cref="InvalidCastException"/>
            [MethodImpl(Internal.InlineOption)]
            public Deferred ToDeferred()
            {
                return new Deferred((Internal.PromiseRef.DeferredPromiseVoid) _target._ref, _target._promiseId, _target._deferredId);
            }

            /// <summary>
            /// Cast this to <see cref="Deferred"/>. Returns an invalid <see cref="Deferred"/> if it cannot be casted.
            /// </summary>
            [MethodImpl(Internal.InlineOption)]
            public Deferred AsDeferred()
            {
                return new Deferred(_target._ref as Internal.PromiseRef.DeferredPromiseVoid, _target._promiseId, _target._deferredId);
            }

            /// <summary>
            /// Cast this to <see cref="Promise{T}.Deferred"/>. Throws an <see cref="InvalidCastException"/> if it cannot be casted.
            /// </summary>
            /// <exception cref="InvalidCastException"/>
            [MethodImpl(Internal.InlineOption)]
            public Promise<T>.Deferred ToDeferred<T>()
            {
                return new Promise<T>.Deferred((Internal.PromiseRef.DeferredPromise<T>) _target._ref, _target._promiseId, _target._deferredId);
            }

            /// <summary>
            /// Cast this to <see cref="Promise{T}.Deferred"/>. Returns an invalid <see cref="Promise{T}.Deferred"/> if it cannot be casted.
            /// </summary>
            [MethodImpl(Internal.InlineOption)]
            public Promise<T>.Deferred AsDeferred<T>()
            {
                return new Promise<T>.Deferred(_target._ref as Internal.PromiseRef.DeferredPromise<T>, _target._promiseId, _target._deferredId);
            }

            /// <summary>
            /// Reject the linked <see cref="Promise"/> with <paramref name="reason"/>.
            /// </summary>
            /// <exception cref="InvalidOperationException"/>
            [MethodImpl(Internal.InlineOption)]
            public void Reject<TReject>(TReject reason)
            {
                _target.Reject(ref reason);
            }

            /// <summary>
            /// Try to reject the linked <see cref="Promise"/> with <paramref name="reason"/>.
            /// <para/> Returns true if successful, false otherwise.
            /// </summary>
            [MethodImpl(Internal.InlineOption)]
            public bool TryReject<TReject>(TReject reason)
            {
                return _target.TryReject(ref reason);
            }

            /// <summary>
            /// Cancel the linked <see cref="Promise"/> with <paramref name="reason"/>.
            /// <para/>Note: This is not recommended. Instead, you should pass a <see cref="CancelationToken"/> into <see cref="New(CancelationToken)"/>.
            /// </summary>
            /// <exception cref="InvalidOperationException"/>
            [MethodImpl(Internal.InlineOption)]
            public void Cancel<TCancel>(TCancel reason)
            {
                _target.Cancel(ref reason);
            }

            /// <summary>
            /// Try to cancel the linked <see cref="Promise"/> with <paramref name="reason"/>.
            /// <para/> Returns true if successful, false otherwise.
            /// <para/>Note: This is not recommended. Instead, you should pass a <see cref="CancelationToken"/> into <see cref="New(CancelationToken)"/>.
            /// </summary>
            [MethodImpl(Internal.InlineOption)]
            public bool TryCancel<TCancel>(TCancel reason)
            {
                return _target.TryCancel(ref reason);
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
            // TODO: don't error if progress is disabled, just do nothing. Set Obsolete attribute to warning.
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

            [MethodImpl(Internal.InlineOption)]
            public bool Equals(DeferredBase other)
            {
                return _target == other._target;
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
                return _target.GetHashCode();
            }

            [MethodImpl(Internal.InlineOption)]
            public static bool operator ==(DeferredBase lhs, DeferredBase rhs)
            {
                return lhs._target == rhs._target;
            }

            [MethodImpl(Internal.InlineOption)]
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
            struct Deferred : IProgress<float>, IEquatable<Deferred>
        {
            private readonly Internal.DeferredInternal<Internal.PromiseRef.DeferredPromiseVoid> _target;

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
                    return _target.GetPromiseVoid();
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

            /// <summary>
            /// Internal use.
            /// </summary>
            [MethodImpl(Internal.InlineOption)]
            internal Deferred(Internal.PromiseRef.DeferredPromiseVoid promise, short promiseId, short deferredId)
            {
                _target = new Internal.DeferredInternal<Internal.PromiseRef.DeferredPromiseVoid>(promise, promiseId, deferredId);
            }

            /// <summary>
            /// Returns a new <see cref="Deferred"/> instance that is linked to and controls the state of a new <see cref="Promises.Promise"/>.
            /// <para/>If the <paramref name="cancelationToken"/> is canceled while the <see cref="Deferred"/> is pending, it and the <see cref="Promises.Promise"/> will be canceled with its reason.
            /// </summary>
            public static Deferred New(CancelationToken cancelationToken = default(CancelationToken))
            {
                Internal.PromiseRef.DeferredPromiseVoid promise = cancelationToken.CanBeCanceled
                    ? Internal.PromiseRef.DeferredPromiseVoidCancel.GetOrCreate(cancelationToken)
                    : Internal.PromiseRef.DeferredPromiseVoid.GetOrCreate();
                return new Deferred(promise, promise.Id, promise.DeferredId);
            }

            /// <summary>
            /// Resolve the linked <see cref="Promise"/>.
            /// </summary>
            /// <exception cref="InvalidOperationException"/>
            [MethodImpl(Internal.InlineOption)]
            public void Resolve()
            {
                _target.ResolveVoid();
            }

            /// <summary>
            /// Try to resolve the linked <see cref="Promise"/>.
            /// <para/> Returns true if successful, false otherwise.
            /// </summary>
            [MethodImpl(Internal.InlineOption)]
            public bool TryResolve()
            {
                return _target.TryResolveVoid();
            }

            /// <summary>
            /// Reject the linked <see cref="Promise"/> with <paramref name="reason"/>.
            /// </summary>
            /// <exception cref="InvalidOperationException"/>
            [MethodImpl(Internal.InlineOption)]
            public void Reject<TReject>(TReject reason)
            {
                _target.Reject(ref reason);
            }

            /// <summary>
            /// Try to reject the linked <see cref="Promise"/> with <paramref name="reason"/>.
            /// <para/> Returns true if successful, false otherwise.
            /// </summary>
            [MethodImpl(Internal.InlineOption)]
            public bool TryReject<TReject>(TReject reason)
            {
                return _target.TryReject(ref reason);
            }

            /// <summary>
            /// Cancel the linked <see cref="Promise"/> with <paramref name="reason"/>.
            /// <para/>Note: This is not recommended. Instead, you should pass a <see cref="CancelationToken"/> into <see cref="New(CancelationToken)"/>.
            /// </summary>
            /// <exception cref="InvalidOperationException"/>
            [MethodImpl(Internal.InlineOption)]
            public void Cancel<TCancel>(TCancel reason)
            {
                _target.Cancel(ref reason);
            }

            /// <summary>
            /// Try to cancel the linked <see cref="Promise"/> with <paramref name="reason"/>.
            /// <para/> Returns true if successful, false otherwise.
            /// <para/>Note: This is not recommended. Instead, you should pass a <see cref="CancelationToken"/> into <see cref="New(CancelationToken)"/>.
            /// </summary>
            [MethodImpl(Internal.InlineOption)]
            public bool TryCancel<TCancel>(TCancel reason)
            {
                return _target.TryCancel(ref reason);
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
            struct Deferred : IProgress<float>, IEquatable<Deferred>
        {
            private readonly Internal.DeferredInternal<Internal.PromiseRef.DeferredPromise<T>> _target;

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
                    return _target.GetPromise<T>();
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

            /// <summary>
            /// Internal use.
            /// </summary>
            [MethodImpl(Internal.InlineOption)]
            internal Deferred(Internal.PromiseRef.DeferredPromise<T> promise, short promiseId, short deferredId)
            {
                _target = new Internal.DeferredInternal<Internal.PromiseRef.DeferredPromise<T>>(promise, promiseId, deferredId);
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
                _target.Resolve(ref value);
            }

            /// <summary>
            /// Try to resolve the linked <see cref="Promise{T}"/> with <paramref name="value"/>.
            /// <para/> Returns true if successful, false otherwise.
            /// </summary>
            [MethodImpl(Internal.InlineOption)]
            public bool TryResolve(T value)
            {
                return _target.TryResolve(ref value);
            }

            /// <summary>
            /// Reject the linked <see cref="Promise"/> with <paramref name="reason"/>.
            /// </summary>
            /// <exception cref="InvalidOperationException"/>
            [MethodImpl(Internal.InlineOption)]
            public void Reject<TReject>(TReject reason)
            {
                _target.Reject(ref reason);
            }

            /// <summary>
            /// Try to reject the linked <see cref="Promise"/> with <paramref name="reason"/>.
            /// <para/> Returns true if successful, false otherwise.
            /// </summary>
            [MethodImpl(Internal.InlineOption)]
            public bool TryReject<TReject>(TReject reason)
            {
                return _target.TryReject(ref reason);
            }

            /// <summary>
            /// Cancel the linked <see cref="Promise"/> with <paramref name="reason"/>.
            /// <para/>Note: This is not recommended. Instead, you should pass a <see cref="CancelationToken"/> into <see cref="New(CancelationToken)"/>.
            /// </summary>
            /// <exception cref="InvalidOperationException"/>
            [MethodImpl(Internal.InlineOption)]
            public void Cancel<TCancel>(TCancel reason)
            {
                _target.Cancel(ref reason);
            }

            /// <summary>
            /// Try to cancel the linked <see cref="Promise"/> with <paramref name="reason"/>.
            /// <para/> Returns true if successful, false otherwise.
            /// <para/>Note: This is not recommended. Instead, you should pass a <see cref="CancelationToken"/> into <see cref="New(CancelationToken)"/>.
            /// </summary>
            [MethodImpl(Internal.InlineOption)]
            public bool TryCancel<TCancel>(TCancel reason)
            {
                return _target.TryCancel(ref reason);
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
            /// Cast to <see cref="Promise.DeferredBase"/>.
            /// </summary>
            [MethodImpl(Internal.InlineOption)]
            public static implicit operator Promise.DeferredBase(Deferred rhs)
            {
                return new Promise.DeferredBase(rhs._target._ref, rhs._target._promiseId, rhs._target._deferredId);
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