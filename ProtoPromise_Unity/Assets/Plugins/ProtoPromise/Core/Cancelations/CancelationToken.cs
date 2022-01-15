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
        private readonly bool _isCanceled;

        /// <summary>
        /// Returns an empty <see cref="CancelationToken"/>.
        /// </summary>
        public static CancelationToken None { get { return default(CancelationToken); } }

        /// <summary>
        /// FOR INTERNAL USE ONLY!
        /// </summary>
        internal CancelationToken(Internal.CancelationRef cancelationRef, short tokenId, bool isCanceled)
        {
            _ref = cancelationRef;
            _id = tokenId;
            _isCanceled = isCanceled;
        }

        /// <summary>
        /// FOR INTERNAL USE ONLY!
        /// </summary>
        internal void MaybeLinkSourceInternal(Internal.CancelationRef cancelationRef)
        {
            Internal.CancelationRef.MaybeAddLinkedCancelation(cancelationRef, _ref, _id, _isCanceled);
        }

        /// <summary>
        /// Get a token that is already in the canceled state.
        /// </summary>
        public static CancelationToken Canceled()
        {
            return new CancelationToken(null, Internal.ValidIdFromApi, true);
        }

        /// <summary>
        /// Gets whether this token is capable of being in the canceled state.
        /// </summary>
        /// <remarks>A <see cref="CancelationToken"/> is capable of being in the canceled state when the <see cref="CancelationSource"/> it is attached to is valid, or if the token has been retained and not yet released.</remarks>
        public bool CanBeCanceled
        {
            get
            {
                return _isCanceled | Internal.CancelationRef.CanTokenBeCanceled(_ref, _id);
            }
        }

        /// <summary>
        /// Gets whether cancelation has been requested for this token.
        /// </summary>
        public bool IsCancelationRequested
        {
            get
            {
                return _isCanceled | Internal.CancelationRef.IsTokenCanceled(_ref, _id);
            }
        }

        /// <summary>
        /// If cancelation was requested on this token, throws a <see cref="CanceledException"/>.
        /// </summary>
        /// <exception cref="CanceledException"/>
        public void ThrowIfCancelationRequested()
        {
            Internal.CancelationRef.ThrowIfCanceled(_ref, _id, _isCanceled);
        }

        /// <summary>
        /// Try to register a delegate that will be invoked when this <see cref="CancelationToken"/> is canceled.
        /// If this is already canceled, the callback will be invoked immediately. If the associated <see cref="CancelationSource"/> was disposed (and this was not retained and canceled), the delegate will not be registered.
        /// </summary>
        /// <param name="callback">The delegate to be executed when the <see cref="CancelationToken"/> is canceled.</param>
        /// <param name="cancelationRegistration">The <see cref="CancelationRegistration"/> instance that can be used to unregister the callback.</param>
        /// <returns>true if <paramref name="callback"/> was registered successfully, false otherwise.</returns>
        public bool TryRegister(Action callback, out CancelationRegistration cancelationRegistration)
        {
            ValidateArgument(callback, "callback", 1);
            return Internal.CancelationRef.TryRegister(_ref, _id, _isCanceled, new Internal.CancelDelegateTokenVoid(callback), out cancelationRegistration);
        }

        /// <summary>
        /// Try to capture a value and register a delegate that will be invoked with the captured value when this <see cref="CancelationToken"/> is canceled.
        /// If this is already canceled, the callback will be invoked immediately. If the associated <see cref="CancelationSource"/> was disposed (and this was not retained and canceled), the delegate will not be registered.
        /// </summary>
        /// <param name="captureValue">The value to pass into <paramref name="callback"/>.</param>
        /// <param name="callback">The delegate to be executed when the <see cref="CancelationToken"/> is canceled.</param>
        /// <param name="cancelationRegistration">The <see cref="CancelationRegistration"/> instance that can be used to unregister the callback.</param>
        /// <returns>true if <paramref name="callback"/> was registered successfully, false otherwise.</returns>
        public bool TryRegister<TCapture>(TCapture captureValue, Action<TCapture> callback, out CancelationRegistration cancelationRegistration)
        {
            ValidateArgument(callback, "callback", 1);
            return Internal.CancelationRef.TryRegister(_ref, _id, _isCanceled, new Internal.CancelDelegateToken<TCapture>(captureValue, callback), out cancelationRegistration);
        }

        /// <summary>
        /// Try to register a cancelable that will be canceled when this <see cref="CancelationToken"/> is canceled.
        /// If this is already canceled, it will be canceled immediately. If the associated <see cref="CancelationSource"/> was disposed (and this was not retained and canceled), it will not be registered.
        /// </summary>
        /// <param name="captureValue">The value to pass into <paramref name="callback"/>.</param>
        /// <param name="cancelable">The cancelable to be canceled when the <see cref="CancelationToken"/> is canceled.</param>
        /// <param name="cancelationRegistration">The <see cref="CancelationRegistration"/> instance that can be used to unregister the callback.</param>
        /// <returns>true if <paramref name="cancelable"/> was registered successfully, false otherwise.</returns>
        public bool TryRegister<TCancelable>(TCancelable cancelable, out CancelationRegistration cancelationRegistration) where TCancelable : ICancelable
        {
            ValidateArgument(cancelable, "cancelable", 1);
            return Internal.CancelationRef.TryRegister(_ref, _id, _isCanceled, cancelable, out cancelationRegistration);
        }

        /// <summary>
        /// Register a delegate that will be invoked when this <see cref="CancelationToken"/> is canceled.
        /// If this is already canceled, the callback will be invoked immediately.
        /// </summary>
        /// <param name="callback">The delegate to be executed when the <see cref="CancelationToken"/> is canceled.</param>
        /// <returns>The <see cref="CancelationRegistration"/> instance that can be used to unregister the callback.</returns>
        /// <exception cref="InvalidOperationException"/>
        public CancelationRegistration Register(Action callback)
        {
            CancelationRegistration registration;
            if (TryRegister(callback, out registration))
            {
                return registration;
            }
            throw new InvalidOperationException("CancelationToken.Register: token cannot be canceled.", Internal.GetFormattedStacktrace(1));
        }

        /// <summary>
        /// Capture a value and register a delegate that will be invoked with the captured value when this <see cref="CancelationToken"/> is canceled.
        /// If this is already canceled, the callback will be invoked immediately.
        /// </summary>
        /// <param name="callback">The delegate to be executed when the <see cref="CancelationToken"/> is canceled.</param>
        /// <returns>The <see cref="CancelationRegistration"/> instance that can be used to unregister the callback.</returns>
        /// <exception cref="InvalidOperationException"/>
        public CancelationRegistration Register<TCapture>(TCapture captureValue, Action<TCapture> callback)
        {
            CancelationRegistration registration;
            if (TryRegister(captureValue, callback, out registration))
            {
                return registration;
            }
            throw new InvalidOperationException("CancelationToken.Register: token cannot be canceled.", Internal.GetFormattedStacktrace(1));
        }

        /// <summary>
        /// Register a cancelable that will be canceled when this <see cref="CancelationToken"/> is canceled.
        /// If this is already canceled, it will be canceled immediately.
        /// </summary>
        /// <param name="cancelable">The cancelable to be canceled when the <see cref="CancelationToken"/> is canceled.</param>
        /// <returns>The <see cref="CancelationRegistration"/> instance that can be used to unregister the callback.</returns>
        /// <exception cref="InvalidOperationException"/>
        public CancelationRegistration Register<TCancelable>(TCancelable cancelable) where TCancelable : ICancelable
        {
            CancelationRegistration registration;
            if (TryRegister(cancelable, out registration))
            {
                return registration;
            }
            throw new InvalidOperationException("CancelationToken.Register: token cannot be canceled.", Internal.GetFormattedStacktrace(1));
        }

        /// <summary>
        /// Try to retain this instance. Returns true if successful, false otherwise.
        /// <para/>If successful, allows continued use of this instance, even after the associated <see cref="CancelationSource"/> has been disposed, until this is released.
        /// If successful, this should be paired with a call to <see cref="Release"/>.
        /// </summary>
        public bool TryRetain()
        {
            return Internal.CancelationRef.TryRetainUser(_ref, _id, _isCanceled);
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
            if (!Internal.CancelationRef.TryReleaseUser(_ref, _id, _isCanceled))
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
            return Internal.BuildHashCode(_ref, _id.GetHashCode(), 0, _isCanceled.GetHashCode());
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
        static partial void ValidateArgument<TArg>(TArg arg, string argName, int skipFrames);
#if PROMISE_DEBUG
        static partial void ValidateArgument<TArg>(TArg arg, string argName, int skipFrames)
        {
            Internal.ValidateArgument(arg, argName, skipFrames + 1);
        }
#endif

        [Obsolete("Cancelation reasons are no longer supported.", true)]
        public Type CancelationValueType
        {
            get
            {
                throw new InvalidOperationException("Cancelation reasons are no longer supported.", Internal.GetFormattedStacktrace(1));
            }
        }

        [Obsolete("Cancelation reasons are no longer supported.", true)]
        public object CancelationValue
        {
            get
            {
                throw new InvalidOperationException("Cancelation reasons are no longer supported.", Internal.GetFormattedStacktrace(1));
            }
        }

        [Obsolete("Cancelation reasons are no longer supported.", true)]
        public bool TryGetCancelationValueAs<T>(out T value)
        {
            throw new InvalidOperationException("Cancelation reasons are no longer supported.", Internal.GetFormattedStacktrace(1));
        }
    }
}