#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#pragma warning disable IDE0090 // Use 'new(...)'
#pragma warning disable IDE0270 // Use coalesce expression

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Proto.Promises
{
    partial struct Promise
    {
        /// <summary>
        /// Deferred base. An instance of this can be used to reject or cancel the attached <see cref="Promise"/>.
        /// <para/>You must cast to <see cref="Deferred"/> or <see cref="Promise{T}.Deferred"/> to resolve the attached <see cref="Promise"/>.
        /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        // Deferreds behave more like write-only, but this prevents the compiler from emitting defensive copies when passing to a function with the `in` keyword.
        public readonly struct DeferredBase : ICancelable, IEquatable<DeferredBase>
        {
            private readonly Internal.IDeferredPromise _ref;
            private readonly short _promiseId;
            private readonly int _deferredId;

            /// <summary>
            /// The attached <see cref="Promises.Promise"/> that this controls.
            /// </summary>
            public Promise Promise
            {
                [MethodImpl(Internal.InlineOption)]
                get
                {
                    var _this = _ref;
#if PROMISE_DEBUG // If the reference is null, this is invalid. We only check in DEBUG mode for performance.
                    if (_this == null)
                    {
                        throw new InvalidOperationException("DeferredBase.Promise: instance is not valid.", Internal.GetFormattedStacktrace(1));
                    }
#endif
                    return new Promise((Internal.PromiseRefBase) _this, _promiseId);
                }
            }

            /// <summary>
            /// Get whether or not this instance and the attached <see cref="Promise"/> are valid.
            /// </summary>
            public bool IsValid
            {
                [MethodImpl(Internal.InlineOption)]
                get => _ref is Internal.PromiseRefBase _this && _this.GetIsValid(_promiseId);
            }

            /// <summary>
            /// Get whether or not this instance is valid and the attached <see cref="Promise"/> is still pending.
            /// </summary>
            public bool IsValidAndPending
            {
                [MethodImpl(Internal.InlineOption)]
                get => Internal.DeferredPromiseHelper.GetIsValidAndPending(_ref, _deferredId);
            }

            /// <summary>
            /// Used for implicit cast operator.
            /// </summary>
            [MethodImpl(Internal.InlineOption)]
            internal DeferredBase(Internal.IDeferredPromise promise, short promiseId, int deferredId)
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
                => new Deferred(
                    (Internal.PromiseRefBase.DeferredPromise<Internal.VoidResult>) _ref,
                    _promiseId,
                    _deferredId);

            /// <summary>
            /// Cast this to <see cref="Deferred"/>. Returns an invalid <see cref="Deferred"/> if it cannot be casted.
            /// </summary>
            [MethodImpl(Internal.InlineOption)]
            public Deferred AsDeferred()
                => new Deferred(_ref as Internal.PromiseRefBase.DeferredPromise<Internal.VoidResult>, _promiseId, _deferredId);

            /// <summary>
            /// Cast this to <see cref="Promise{T}.Deferred"/>. Throws an <see cref="InvalidCastException"/> if it cannot be casted.
            /// </summary>
            /// <exception cref="InvalidCastException"/>
            [MethodImpl(Internal.InlineOption)]
            public Promise<T>.Deferred ToDeferred<T>()
                => new Promise<T>.Deferred(
                    (Internal.PromiseRefBase.DeferredPromise<T>) _ref,
                    _promiseId,
                    _deferredId);

            /// <summary>
            /// Cast this to <see cref="Promise{T}.Deferred"/>. Returns an invalid <see cref="Promise{T}.Deferred"/> if it cannot be casted.
            /// </summary>
            [MethodImpl(Internal.InlineOption)]
            public Promise<T>.Deferred AsDeferred<T>()
                => new Promise<T>.Deferred(_ref as Internal.PromiseRefBase.DeferredPromise<T>, _promiseId, _deferredId);

            /// <summary>
            /// Reject the linked <see cref="Promise"/> with <paramref name="reason"/>.
            /// </summary>
            /// <exception cref="InvalidOperationException"/>
            public void Reject<TReject>(TReject reason)
            {
                if (!TryReject(reason))
                {
                    throw new InvalidOperationException("DeferredBase.Reject: instance is not valid or already complete.", Internal.GetFormattedStacktrace(1));
                }
            }

            /// <summary>
            /// Try to reject the linked <see cref="Promise"/> with <paramref name="reason"/>.
            /// <para/> Returns true if successful, false otherwise.
            /// </summary>
            public bool TryReject<TReject>(TReject reason)
            {
                var _this = _ref;
                if (Internal.DeferredPromiseHelper.TryIncrementDeferredId(_this, _deferredId))
                {
                    _this.RejectDirect(Internal.CreateRejectContainer(reason, 1, null, _this));
                    return true;
                }
                return false;
            }

            /// <summary>
            /// Cancel the linked <see cref="Promise"/>.
            /// </summary>
            /// <exception cref="InvalidOperationException"/>
            [MethodImpl(Internal.InlineOption)]
            public void Cancel()
            {
                var _this = _ref;
                if (
#if PROMISE_DEBUG // We can skip the null check in RELEASE, the runtime will just throw NullReferenceException if it's null.
                    _this == null ||
#endif
                    !_this.TryIncrementDeferredId(_deferredId))
                {
                    throw new InvalidOperationException("DeferredBase.Cancel: instance is not valid or already complete.", Internal.GetFormattedStacktrace(1));
                }
                _this.CancelDirect();
            }

            /// <summary>
            /// Try to cancel the linked <see cref="Promise"/>.
            /// <para/> Returns true if successful, false otherwise.
            /// </summary>
            [MethodImpl(Internal.InlineOption)]
            public bool TryCancel()
            {
                var _this = _ref;
                if (Internal.DeferredPromiseHelper.TryIncrementDeferredId(_this, _deferredId))
                {
                    _this.CancelDirect();
                    return true;
                }
                return false;
            }

            /// <summary>Returns a value indicating whether this value is equal to a specified <see cref="DeferredBase"/>.</summary>
            [MethodImpl(Internal.InlineOption)]
            public bool Equals(DeferredBase other)
                => this == other;

            /// <summary>Returns a value indicating whether this value is equal to a specified <see cref="object"/>.</summary>
            public override bool Equals(object obj)
                => obj is DeferredBase deferred && Equals(deferred);

            /// <summary>Returns the hash code for this instance.</summary>
            [MethodImpl(Internal.InlineOption)]
            public override int GetHashCode()
                => Internal.BuildHashCode(_ref, _deferredId.GetHashCode(), _promiseId.GetHashCode());

            /// <summary>Returns a value indicating whether two <see cref="DeferredBase"/> values are equal.</summary>
            [MethodImpl(Internal.InlineOption)]
            public static bool operator ==(DeferredBase lhs, DeferredBase rhs)
                => lhs._ref == rhs._ref
                    & lhs._deferredId == rhs._deferredId
                    & lhs._promiseId == rhs._promiseId;

            /// <summary>Returns a value indicating whether two <see cref="DeferredBase"/> values are not equal.</summary>
            [MethodImpl(Internal.InlineOption)]
            public static bool operator !=(DeferredBase lhs, DeferredBase rhs)
                => !(lhs == rhs);
        } // struct DeferredBase

        /// <summary>
        /// An instance of this is used to resolve, reject, or cancel the attached <see cref="Promise"/>.
        /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        public readonly struct Deferred : ICancelable, IEquatable<Deferred>
        {
            internal readonly Internal.PromiseRefBase.DeferredPromise<Internal.VoidResult> _ref;
            internal readonly short _promiseId;
            internal readonly int _deferredId;

            /// <summary>
            /// The attached <see cref="Promises.Promise"/> that this controls.
            /// </summary>
            public Promise Promise
            {
                [MethodImpl(Internal.InlineOption)]
                get
                {
                    var _this = _ref;
#if PROMISE_DEBUG // If the reference is null, this is invalid. We only check in DEBUG mode for performance.
                    if (_this == null)
                    {
                        throw new InvalidOperationException("Deferred.Promise: instance is not valid.", Internal.GetFormattedStacktrace(1));
                    }
#endif
                    return new Promise(_this, _promiseId);
                }
            }

            /// <summary>
            /// Get whether or not this instance and the attached <see cref="Promise"/> are valid.
            /// </summary>
            public bool IsValid
            {
                [MethodImpl(Internal.InlineOption)]
                get => _ref?.GetIsValid(_promiseId) == true;
            }

            /// <summary>
            /// Get whether or not this instance is valid and the attached <see cref="Promise"/> is still pending.
            /// </summary>
            public bool IsValidAndPending
            {
                [MethodImpl(Internal.InlineOption)]
                get => _ref?.DeferredId == _deferredId;
            }

            [MethodImpl(Internal.InlineOption)]
            internal Deferred(Internal.PromiseRefBase.DeferredPromise<Internal.VoidResult> promise, short promiseId, int deferredId)
            {
                _ref = promise;
                _promiseId = promiseId;
                _deferredId = deferredId;
            }

            /// <summary>
            /// Returns a new <see cref="Deferred"/> instance that is linked to and controls the state of a new <see cref="Promises.Promise"/>.
            /// </summary>
            [MethodImpl(Internal.InlineOption)]
            public static Deferred New()
            {
                var promise = Internal.PromiseRefBase.DeferredPromise<Internal.VoidResult>.GetOrCreate();
                return new Deferred(promise, promise.Id, promise.DeferredId);
            }

            /// <summary>
            /// Resolve the linked <see cref="Promise"/>.
            /// </summary>
            /// <exception cref="InvalidOperationException"/>
            [MethodImpl(Internal.InlineOption)]
            public void Resolve()
            {
                var _this = _ref;
                if (
#if PROMISE_DEBUG // We can skip the null check in RELEASE, the runtime will just throw NullReferenceException if it's null.
                    _this == null ||
#endif
                    !_this.TryIncrementDeferredId(_deferredId))
                {
                    throw new InvalidOperationException("Deferred.Resolve: instance is not valid or already complete.", Internal.GetFormattedStacktrace(1));
                }
                _this.ResolveDirectVoid();
            }

            /// <summary>
            /// Try to resolve the linked <see cref="Promise"/>.
            /// <para/> Returns true if successful, false otherwise.
            /// </summary>
            [MethodImpl(Internal.InlineOption)]
            public bool TryResolve()
                => Internal.PromiseRefBase.DeferredPromise<Internal.VoidResult>.TryResolveVoid(_ref, _deferredId);

            /// <summary>
            /// Reject the linked <see cref="Promise"/> with <paramref name="reason"/>.
            /// </summary>
            /// <exception cref="InvalidOperationException"/>
            public void Reject<TReject>(TReject reason)
            {
                if (!TryReject(reason))
                {
                    throw new InvalidOperationException("Deferred.Reject: instance is not valid or already complete.", Internal.GetFormattedStacktrace(1));
                }
            }

            /// <summary>
            /// Try to reject the linked <see cref="Promise"/> with <paramref name="reason"/>.
            /// <para/> Returns true if successful, false otherwise.
            /// </summary>
            public bool TryReject<TReject>(TReject reason)
            {
                var _this = _ref;
                if (_this != null && _this.TryIncrementDeferredId(_deferredId))
                {
                    _this.RejectDirect(Internal.CreateRejectContainer(reason, 1, null, _this));
                    return true;
                }
                return false;
            }

            /// <summary>
            /// Cancel the linked <see cref="Promise"/>.
            /// </summary>
            /// <exception cref="InvalidOperationException"/>
            [MethodImpl(Internal.InlineOption)]
            public void Cancel()
            {
                var _this = _ref;
                if (
#if PROMISE_DEBUG // We can skip the null check in RELEASE, the runtime will just throw NullReferenceException if it's null.
                    _this == null ||
#endif
                    !_this.TryIncrementDeferredId(_deferredId))
                {
                    throw new InvalidOperationException("Deferred.Cancel: instance is not valid or already complete.", Internal.GetFormattedStacktrace(1));
                }
                _this.CancelDirect();
            }

            /// <summary>
            /// Try to cancel the linked <see cref="Promise"/>.
            /// <para/> Returns true if successful, false otherwise.
            /// </summary>
            [MethodImpl(Internal.InlineOption)]
            public bool TryCancel()
            {
                var _this = _ref;
                if (_this != null && _this.TryIncrementDeferredId(_deferredId))
                {
                    _this.CancelDirect();
                    return true;
                }
                return false;
            }

            /// <summary>
            /// Cast to <see cref="DeferredBase"/>.
            /// </summary>
            [MethodImpl(Internal.InlineOption)]
            public static implicit operator DeferredBase(Deferred rhs)
                => new DeferredBase(rhs._ref, rhs._promiseId, rhs._deferredId);

            /// <summary>
            /// Cast <see cref="DeferredBase"/> to <see cref="Deferred"/>.
            /// </summary>
            [MethodImpl(Internal.InlineOption)]
            public static explicit operator Deferred(DeferredBase rhs)
                => rhs.ToDeferred();

            /// <summary>Returns a value indicating whether this value is equal to a specified <see cref="Deferred"/>.</summary>
            [MethodImpl(Internal.InlineOption)]
            public bool Equals(Deferred other)
                => this == other;

            /// <summary>Returns a value indicating whether this value is equal to a specified <see cref="object"/>.</summary>
            public override bool Equals(object obj)
                => obj is Deferred deferred && Equals(deferred);

            /// <summary>Returns the hash code for this instance.</summary>
            [MethodImpl(Internal.InlineOption)]
            public override int GetHashCode()
                => Internal.BuildHashCode(_ref, _deferredId.GetHashCode(), _promiseId.GetHashCode());

            /// <summary>Returns a value indicating whether two <see cref="Deferred"/> values are equal.</summary>
            [MethodImpl(Internal.InlineOption)]
            public static bool operator ==(Deferred lhs, Deferred rhs)
                => lhs._ref == rhs._ref
                    & lhs._deferredId == rhs._deferredId
                    & lhs._promiseId == rhs._promiseId;

            /// <summary>Returns a value indicating whether two <see cref="Deferred"/> values are not equal.</summary>
            [MethodImpl(Internal.InlineOption)]
            public static bool operator !=(Deferred lhs, Deferred rhs)
                => !(lhs == rhs);
        } // struct Deferred
    } // struct Promise

    public partial struct Promise<T>
    {
        /// <summary>
        /// An instance of this is used to resolve, reject, or cancel the attached <see cref="Promise"/>.
        /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        public readonly struct Deferred : ICancelable, IEquatable<Deferred>
        {
            internal readonly Internal.PromiseRefBase.DeferredPromise<T> _ref;
            internal readonly short _promiseId;
            internal readonly int _deferredId;

            /// <summary>
            /// The attached <see cref="Promise{T}"/> that this controls.
            /// </summary>
            public Promise<T> Promise
            {
                [MethodImpl(Internal.InlineOption)]
                get
                {
                    var _this = _ref;
#if PROMISE_DEBUG // If the reference is null, this is invalid. We only check in DEBUG mode for performance.
                    if (_this == null)
                    {
                        throw new InvalidOperationException("Deferred.Promise: instance is not valid.", Internal.GetFormattedStacktrace(1));
                    }
#endif
                    return new Promise<T>(_this, _promiseId);
                }
            }

            /// <summary>
            /// Get whether or not this instance and the attached <see cref="Promise"/> are valid.
            /// </summary>
            public bool IsValid
            {
                [MethodImpl(Internal.InlineOption)]
                get => _ref?.GetIsValid(_promiseId) == true;
            }

            /// <summary>
            /// Get whether or not this instance is valid and the attached <see cref="Promise"/> is still pending.
            /// </summary>
            public bool IsValidAndPending
            {
                [MethodImpl(Internal.InlineOption)]
                get => _ref?.DeferredId == _deferredId;
            }

            [MethodImpl(Internal.InlineOption)]
            internal Deferred(Internal.PromiseRefBase.DeferredPromise<T> promise, short promiseId, int deferredId)
            {
                _ref = promise;
                _promiseId = promiseId;
                _deferredId = deferredId;
            }

            /// <summary>
            /// Returns a new <see cref="Deferred"/> instance that is linked to and controls the state of a new <see cref="Promises.Promise"/>.
            /// </summary>
            [MethodImpl(Internal.InlineOption)]
            public static Deferred New()
            {
                var promise = Internal.PromiseRefBase.DeferredPromise<T>.GetOrCreate();
                return new Deferred(promise, promise.Id, promise.DeferredId);
            }

            /// <summary>
            /// Resolve the linked <see cref="Promise"/> with <paramref name="value"/>.
            /// </summary>
            /// <exception cref="InvalidOperationException"/>
            [MethodImpl(Internal.InlineOption)]
            public void Resolve(T value)
            {
                var _this = _ref;
                if (
#if PROMISE_DEBUG // We can skip the null check in RELEASE, the runtime will just throw NullReferenceException if it's null.
                    _this == null ||
#endif
                    !_this.TryIncrementDeferredId(_deferredId))
                {
                    throw new InvalidOperationException("Deferred.Resolve: instance is not valid or already complete.", Internal.GetFormattedStacktrace(1));
                }
                _this.ResolveDirect(value);
            }

            /// <summary>
            /// Try to resolve the linked <see cref="Promise"/> with <paramref name="value"/>.
            /// <para/> Returns true if successful, false otherwise.
            /// </summary>
            [MethodImpl(Internal.InlineOption)]
            public bool TryResolve(T value)
                => Internal.PromiseRefBase.DeferredPromise<T>.TryResolve(_ref, _deferredId, value);

            /// <summary>
            /// Reject the linked <see cref="Promise"/> with <paramref name="reason"/>.
            /// </summary>
            /// <exception cref="InvalidOperationException"/>
            public void Reject<TReject>(TReject reason)
            {
                if (!TryReject(reason))
                {
                    throw new InvalidOperationException("Deferred.Reject: instance is not valid or already complete.", Internal.GetFormattedStacktrace(1));
                }
            }

            /// <summary>
            /// Try to reject the linked <see cref="Promise"/> with <paramref name="reason"/>.
            /// <para/> Returns true if successful, false otherwise.
            /// </summary>
            public bool TryReject<TReject>(TReject reason)
            {
                var _this = _ref;
                if (_this != null && _this.TryIncrementDeferredId(_deferredId))
                {
                    _this.RejectDirect(Internal.CreateRejectContainer(reason, 1, null, _this));
                    return true;
                }
                return false;
            }

            /// <summary>
            /// Cancel the linked <see cref="Promise"/>.
            /// </summary>
            /// <exception cref="InvalidOperationException"/>
            [MethodImpl(Internal.InlineOption)]
            public void Cancel()
            {
                var _this = _ref;
                if (
#if PROMISE_DEBUG // We can skip the null check in RELEASE, the runtime will just throw NullReferenceException if it's null.
                    _this == null ||
#endif
                    !_this.TryIncrementDeferredId(_deferredId))
                {
                    throw new InvalidOperationException("Deferred.Cancel: instance is not valid or already complete.", Internal.GetFormattedStacktrace(1));
                }
                _this.CancelDirect();
            }

            /// <summary>
            /// Try to cancel the linked <see cref="Promise"/>.
            /// <para/> Returns true if successful, false otherwise.
            /// </summary>
            [MethodImpl(Internal.InlineOption)]
            public bool TryCancel()
            {
                var _this = _ref;
                if (_this != null && _this.TryIncrementDeferredId(_deferredId))
                {
                    _this.CancelDirect();
                    return true;
                }
                return false;
            }

            /// <summary>
            /// Cast to <see cref="Promise.DeferredBase"/>.
            /// </summary>
            [MethodImpl(Internal.InlineOption)]
            public static implicit operator Promise.DeferredBase(Deferred rhs)
                => new Promise.DeferredBase(rhs._ref, rhs._promiseId, rhs._deferredId);

            /// <summary>
            /// Cast <see cref="Promise.DeferredBase"/> to <see cref="Deferred"/>.
            /// </summary>
            [MethodImpl(Internal.InlineOption)]
            public static explicit operator Deferred(Promise.DeferredBase rhs)
                => rhs.ToDeferred<T>();

            /// <summary>Returns a value indicating whether this value is equal to a specified <see cref="Deferred"/>.</summary>
            [MethodImpl(Internal.InlineOption)]
            public bool Equals(Deferred other)
                => this == other;

            /// <summary>Returns a value indicating whether this value is equal to a specified <see cref="object"/>.</summary>
            public override bool Equals(object obj)
                => obj is Deferred deferred && Equals(deferred);

            /// <summary>Returns the hash code for this instance.</summary>
            [MethodImpl(Internal.InlineOption)]
            public override int GetHashCode()
                => Internal.BuildHashCode(_ref, _deferredId.GetHashCode(), _promiseId.GetHashCode());

            /// <summary>Returns a value indicating whether two <see cref="Deferred"/> values are equal.</summary>
            [MethodImpl(Internal.InlineOption)]
            public static bool operator ==(Deferred lhs, Deferred rhs)
                => lhs._ref == rhs._ref
                    & lhs._deferredId == rhs._deferredId
                    & lhs._promiseId == rhs._promiseId;

            /// <summary>Returns a value indicating whether two <see cref="Deferred"/> values are not equal.</summary>
            [MethodImpl(Internal.InlineOption)]
            public static bool operator !=(Deferred lhs, Deferred rhs)
                => !(lhs == rhs);
        } // struct Deferred
    } // struct Promise<T>
} // namespace Proto.Promises