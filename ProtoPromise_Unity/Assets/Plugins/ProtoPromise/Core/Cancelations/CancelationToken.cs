#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#pragma warning disable IDE0018 // Inline variable declaration
#pragma warning disable IDE0034 // Simplify 'default' expression

using System;

namespace Proto.Promises
{
    /// <summary>
    /// Propagates notification that operations should be canceled.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [System.Diagnostics.DebuggerNonUserCode]
#endif
    public
#if CSHARP_7_3_OR_NEWER
        readonly
#endif
        partial struct CancelationToken : IRetainable, IEquatable<CancelationToken>
    {
        private readonly Internal.CancelationRef _ref;
        private readonly short _id;

        /// <summary>
        /// Returns an empty <see cref="CancelationToken"/>.
        /// </summary>
        public static CancelationToken None { get { return default(CancelationToken); } }

        /// <summary>
        /// FOR INTERNAL USE ONLY!
        /// </summary>
        internal CancelationToken(Internal.CancelationRef cancelationRef, short tokenId)
        {
            _ref = cancelationRef;
            _id = tokenId;
        }

        /// <summary>
        /// FOR INTERNAL USE ONLY!
        /// </summary>
        internal void MaybeLinkSourceInternal(Internal.CancelationRef cancelationRef)
        {
            CancelationToken _this = this;
            if (_this._ref != null)
            {
                _this._ref.MaybeAddLinkedCancelation(cancelationRef, _this._id);
            }
        }

        /// <summary>
        /// FOR INTERNAL USE ONLY!
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(Internal.InlineOption)]
        internal bool TryRegisterInternal(Internal.ICancelDelegate listener, out CancelationRegistration cancelationRegistration)
        {
            // Retain for thread safety.
            CancelationToken _this = this;
            if (_this._ref == null || !_this._ref.TryRetainInternal(_this._id))
            {
                cancelationRegistration = default(CancelationRegistration);
                return false;
            }
            bool success = _this._ref.TryRegister(listener, out cancelationRegistration);
            _this._ref.ReleaseAfterRetainInternal();
            return success;
        }

        /// <summary>
        /// Gets whether this token is capable of being in the canceled state.
        /// </summary>
        /// <remarks>A <see cref="CancelationToken"/> is capable of being in the canceled state when the <see cref="CancelationSource"/> it is attached to is valid, or if the token has been retained and not yet released.</remarks>
        public bool CanBeCanceled
        {
            get
            {
                CancelationToken _this = this;
                return _this._ref != null && _this._ref.TokenId == _this._id;
            }
        }

        /// <summary>
        /// Gets whether cancelation has been requested for this token.
        /// </summary>
        public bool IsCancelationRequested
        {
            get
            {
                CancelationToken _this = this;
                return _this._ref != null && _this._ref.IsTokenCanceled(_this._id);
            }
        }

        /// <summary>
        /// If cancelation was requested on this token, throws a <see cref="CancelException"/>.
        /// </summary>
        /// <exception cref="CancelException"/>
        public void ThrowIfCancelationRequested()
        {
            CancelationToken _this = this;
            if (_this._ref != null)
            {
                _this._ref.ThrowIfCanceled(_this._id);
            }
        }

        /// <summary>
        /// Get the type of the cancelation value, or null if there is no value.
        /// </summary>
        /// <value>The type of the value.</value>
        /// <exception cref="InvalidOperationException"/>
        public Type CancelationValueType
        {
            get
            {
                CancelationToken _this = this;
                Type type;
                if (_this._ref != null && _this._ref.TryGetCanceledType(_this._id, out type))
                {
                    return type;
                }
                throw new InvalidOperationException("CancelationToken.CancelationValueType: token has not been canceled.", Internal.GetFormattedStacktrace(1));
            }
        }

        /// <summary>
        /// Get the cancelation value.
        /// <para/>NOTE: Use <see cref="TryGetCancelationValueAs{T}(out T)"/> if you want to prevent value type boxing.
        /// </summary>
        /// <exception cref="InvalidOperationException"/>
        public object CancelationValue
        {
            get
            {
                CancelationToken _this = this;
                object value;
                if (_this._ref != null && _this._ref.TryGetCanceledValue(_this._id, out value))
                {
                    return value;
                }
                throw new InvalidOperationException("CancelationToken.CancelationValue: token has not been canceled.", Internal.GetFormattedStacktrace(1));
            }
        }

        /// <summary>
        /// Try to get the cancelation value casted to <typeparamref name="T"/>.
        /// Returns true if successful, false otherwise.
        /// </summary>
        /// <exception cref="InvalidOperationException"/>
        public bool TryGetCancelationValueAs<T>(out T value)
        {
            CancelationToken _this = this;
            bool didConvert;
            if (_this._ref != null && _this._ref.TryGetCanceledValueAs(_this._id, out didConvert, out value))
            {
                return didConvert;
            }
            throw new InvalidOperationException("CancelationToken.TryGetCancelationValueAs: token has not been canceled.", Internal.GetFormattedStacktrace(1));
        }

        /// <summary>
        /// Try to register a delegate that will be invoked with the cancelation reason when this <see cref="CancelationToken"/> is canceled.
        /// If this is already canceled, the callback will be invoked immediately. If the associated <see cref="CancelationSource"/> was disposed (and this was not retained and canceled), the delegate will not be registered.
        /// </summary>
        /// <param name="callback">The delegate to be executed when the <see cref="CancelationToken"/> is canceled.</param>
        /// <param name="cancelationRegistration">The <see cref="CancelationRegistration"/> instance that can be used to unregister the callback.</param>
        /// <returns>true if <paramref name="callback"/> was registered successfully, false otherwise.</returns>
        public bool TryRegister(Promise.CanceledAction callback, out CancelationRegistration cancelationRegistration)
        {
            ValidateArgument(callback, "callback", 1);
            CancelationToken _this = this;
            if (_this._ref == null)
            {
                cancelationRegistration = default(CancelationRegistration);
                return false;
            }
            return _this._ref.TryRegister(callback, _this._id, out cancelationRegistration);
        }

        /// <summary>
        /// Try to capture a value and register a delegate that will be invoked with the captured value and the cancelation reason when this <see cref="CancelationToken"/> is canceled.
        /// If this is already canceled, the callback will be invoked immediately. If the associated <see cref="CancelationSource"/> was disposed (and this was not retained and canceled), the delegate will not be registered.
        /// </summary>
        /// <param name="captureValue">The value to pass into <paramref name="callback"/>.</param>
        /// <param name="callback">The delegate to be executed when the <see cref="CancelationToken"/> is canceled.</param>
        /// <param name="cancelationRegistration">The <see cref="CancelationRegistration"/> instance that can be used to unregister the callback.</param>
        /// <returns>true if <paramref name="callback"/> was registered successfully, false otherwise.</returns>
        public bool TryRegister<TCapture>(TCapture captureValue, Promise.CanceledAction<TCapture> callback, out CancelationRegistration cancelationRegistration)
        {
            ValidateArgument(callback, "callback", 1);
            CancelationToken _this = this;
            if (_this._ref == null)
            {
                cancelationRegistration = default(CancelationRegistration);
                return false;
            }
            return _this._ref.TryRegister(ref captureValue, callback, _this._id, out cancelationRegistration);
        }

        /// <summary>
        /// Register a delegate that will be invoked with the cancelation reason when this <see cref="CancelationToken"/> is canceled.
        /// If this is already canceled, the callback will be invoked immediately.
        /// </summary>
        /// <param name="callback">The delegate to be executed when the <see cref="CancelationToken"/> is canceled.</param>
        /// <returns>The <see cref="CancelationRegistration"/> instance that can be used to unregister the callback.</returns>
        /// <exception cref="InvalidOperationException"/>
        public CancelationRegistration Register(Promise.CanceledAction callback)
        {
            CancelationRegistration registration;
            if (TryRegister(callback, out registration))
            {
                return registration;
            }
            throw new InvalidOperationException("CancelationToken.Register: token cannot be canceled.", Internal.GetFormattedStacktrace(1));
        }

        /// <summary>
        /// Capture a value and register a delegate that will be invoked with the captured value and the cancelation reason when this <see cref="CancelationToken"/> is canceled.
        /// If this is already canceled, the callback will be invoked immediately.
        /// </summary>
        /// <param name="callback">The delegate to be executed when the <see cref="CancelationToken"/> is canceled.</param>
        /// <returns>The <see cref="CancelationRegistration"/> instance that can be used to unregister the callback.</returns>
        /// <exception cref="InvalidOperationException"/>
        public CancelationRegistration Register<TCapture>(TCapture captureValue, Promise.CanceledAction<TCapture> callback)
        {
            CancelationRegistration registration;
            if (TryRegister(captureValue, callback, out registration))
            {
                return registration;
            }
            throw new InvalidOperationException("CancelationToken.Register: token cannot be canceled.", Internal.GetFormattedStacktrace(1));
        }

        /// <summary>
        /// Try to retain this instance. Return true if successful, false otherwise.
        /// <para/>If successful, allows continued use of this instance, even after the associated <see cref="CancelationSource"/> has been disposed, until this is released.
        /// If successful, this should be paired with a call to <see cref="Release"/>.
        /// </summary>
        public bool TryRetain()
        {
            CancelationToken _this = this;
            return _this._ref != null && _this._ref.TryRetainUser(_this._id);
        }

        /// <summary>
        /// Retain this instance. Allows continued use of this instance, even after the associated <see cref="CancelationSource"/> has been disposed, until this is released.
        /// <para/>This should always be paired with a call to <see cref="Release"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException"/>
        public void Retain()
        {
            if (!TryRetain())
            {
                throw new InvalidOperationException("CancelationToken.Retain: token cannot be canceled.", Internal.GetFormattedStacktrace(1));
            }
        }

        /// <summary>
        /// Release this instance. Allows resources to be released when the associated <see cref="CancelationSource"/> is disposed (if <see cref="Release"/> has been called for all <see cref="Retain"/> calls).
        /// <para/>This should always be paired with a call to <see cref="Retain"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException"/>
        public void Release()
        {
            CancelationToken _this = this;
            if (_this._ref == null || !_this._ref.TryReleaseUser(_this._id))
            {
                throw new InvalidOperationException("CancelationToken.Release: you must call Retain before you call Release.", Internal.GetFormattedStacktrace(1));
            }
        }

        public bool Equals(CancelationToken other)
        {
            return this == other;
        }

        public override bool Equals(object obj)
        {
#if CSHARP_7_3_OR_NEWER
            return obj is CancelationToken token && Equals(token);
#else
            return obj is CancelationToken && Equals((CancelationToken) obj);
#endif
        }

        public override int GetHashCode()
        {
            CancelationToken _this = this;
            if (_this._ref == null)
            {
                return 0;
            }
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + _this._id.GetHashCode();
                hash = hash * 31 + _this._ref.GetHashCode();
                return hash;
            }
        }

        public static bool operator ==(CancelationToken c1, CancelationToken c2)
        {
            return c1._ref == c2._ref & c1._id == c2._id;
        }

        public static bool operator !=(CancelationToken c1, CancelationToken c2)
        {
            return !(c1 == c2);
        }

        // Calls to this get compiled away in RELEASE mode
        static partial void ValidateArgument(object arg, string argName, int skipFrames);
#if PROMISE_DEBUG
        static partial void ValidateArgument(object arg, string argName, int skipFrames)
        {
            Internal.ValidateArgument(arg, argName, skipFrames + 1);
        }
#endif
    }
}