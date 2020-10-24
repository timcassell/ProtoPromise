#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

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
    public partial struct CancelationToken : IRetainable, IEquatable<CancelationToken>
    {
        private readonly Internal.CancelationRef _ref;
        private readonly ushort _id;

        /// <summary>
        /// Returns an empty <see cref="CancelationToken"/>.
        /// </summary>
        public static CancelationToken None { get { return default(CancelationToken); } }

        /// <summary>
        /// FOR INTERNAL USE ONLY!
        /// </summary>
        internal CancelationToken(Internal.CancelationRef cancelationRef)
        {
            _ref = cancelationRef;
            _id = _ref.TokenId;
        }

        /// <summary>
        /// FOR INTERNAL USE ONLY!
        /// </summary>
        internal void MaybeLinkSourceInternal(Internal.CancelationRef cancelationRef)
        {
            if (CanBeCanceled)
            {
                _ref.AddLinkedCancelation(cancelationRef);
            }
        }

        /// <summary>
        /// FOR INTERNAL USE ONLY!
        /// </summary>
        internal CancelationRegistration RegisterInternal(Internal.ICancelDelegate listener)
        {
            if (CanBeCanceled)
            {
                if (!_ref.IsCanceled)
                {
                    return new CancelationRegistration(_ref, listener);
                }
                listener.Invoke(_ref.ValueContainer);
            }
            return default(CancelationRegistration);
        }

        /// <summary>
        /// Gets whether this token is capable of being in the canceled state.
        /// </summary>
        public bool CanBeCanceled
        {
            get
            {
                return _ref != null && _ref.TokenId == _id;
            }
        }

        /// <summary>
        /// Gets whether cancelation has been requested for this token.
        /// </summary>
        public bool IsCancelationRequested
        {
            get
            {
                return CanBeCanceled && _ref.IsCanceled;
            }
        }

        /// <summary>
        /// If cancelation was requested on this token, throws a <see cref="CancelException"/>.
        /// </summary>
        public void ThrowIfCancelationRequested()
        {
            if (IsCancelationRequested)
            {
                throw Internal.CancelExceptionInternal<object>.GetOrCreate(_ref.ValueContainer);
            }
        }

        /// <summary>
        /// Get the type of the cancelation value, or null if there is no value.
        /// </summary>
        /// <value>The type of the value.</value>
        public Type CancelationValueType
        {
            get
            {
                if (!IsCancelationRequested)
                {
                    throw new InvalidOperationException("CancelationToken.CancelationValueType: token has not been canceled.", Internal.GetFormattedStacktrace(1));
                }
                return _ref.ValueContainer.ValueType;
            }
        }

        /// <summary>
        /// Get the cancelation value.
        /// <para/>NOTE: Use <see cref="TryGetCancelationValueAs{T}(out T)"/> if you want to prevent value type boxing.
        /// </summary>
        public object CancelationValue
        {
            get
            {
                if (!IsCancelationRequested)
                {
                    throw new InvalidOperationException("CancelationToken.CancelationValue: token has not been canceled.", Internal.GetFormattedStacktrace(1));
                }
                return _ref.ValueContainer.Value;
            }
        }

        /// <summary>
        /// Try to get the cancelation value casted to <typeparamref name="T"/>.
        /// Returns true if successful, false otherwise.
        /// </summary>
        public bool TryGetCancelationValueAs<T>(out T value)
        {
            if (!IsCancelationRequested)
            {
                throw new InvalidOperationException("CancelationToken.CancelationValue: token has not been canceled.", Internal.GetFormattedStacktrace(1));
            }
            return Internal.TryConvert(_ref.ValueContainer, out value);
        }

        /// <summary>
        /// Register a delegate that will be invoked with the cancelation reason when this <see cref="CancelationToken"/> is canceled.
        /// If this is already canceled, the callback will be invoked immediately.
        /// </summary>
        /// <param name="callback">The delegate to be executed when the <see cref="CancelationToken"/> is canceled.</param>
        /// <returns>The <see cref="CancelationRegistration"/> instance that can be used to unregister the callback.</returns>
        public CancelationRegistration Register(Action<ReasonContainer> callback)
        {
            if (!CanBeCanceled)
            {
                throw new InvalidOperationException("CancelationToken.Register: token cannot be canceled.", Internal.GetFormattedStacktrace(1));
            }
            ValidateArgument(callback, "callback", 1);
            if (_ref.IsCanceled)
            {
                callback.Invoke(new ReasonContainer(_ref.ValueContainer));
                return default(CancelationRegistration);
            }
            var cancelDelegate = Internal.CancelDelegate<Internal.CancelDelegateToken>.GetOrCreate();
            cancelDelegate.canceler = new Internal.CancelDelegateToken(callback);
            return new CancelationRegistration(_ref, cancelDelegate);
        }

        /// <summary>
        /// Capture a value and register a delegate that will be invoked with the captured value and the cancelation reason when this <see cref="CancelationToken"/> is canceled.
        /// If this is already canceled, the callback will be invoked immediately.
        /// </summary>
        /// <param name="callback">The delegate to be executed when the <see cref="CancelationToken"/> is canceled.</param>
        /// <returns>The <see cref="CancelationRegistration"/> instance that can be used to unregister the callback.</returns>
        public CancelationRegistration Register<TCapture>(TCapture captureValue, Action<TCapture, ReasonContainer> callback)
        {
            if (!CanBeCanceled)
            {
                throw new InvalidOperationException("CancelationToken.Register: token cannot be canceled.", Internal.GetFormattedStacktrace(1));
            }
            ValidateArgument(callback, "callback", 1);
            if (_ref.IsCanceled)
            {
                callback.Invoke(captureValue, new ReasonContainer(_ref.ValueContainer));
                return default(CancelationRegistration);
            }
            var cancelDelegate = Internal.CancelDelegate<Internal.CancelDelegateToken<TCapture>>.GetOrCreate();
            cancelDelegate.canceler = new Internal.CancelDelegateToken<TCapture>(ref captureValue, callback);
            return new CancelationRegistration(_ref, cancelDelegate);
        }

        /// <summary>
        /// Retain this instance. Allows continued use of this instance, even after the associated <see cref="CancelationSource"/> has been disposed, until this is released.
        /// <para/>This should always be paired with a call to <see cref="Release"/>
        /// </summary>
        public void Retain()
        {
            if (!CanBeCanceled)
            {
                throw new InvalidOperationException("CancelationToken.Retain: token cannot be canceled.", Internal.GetFormattedStacktrace(1));
            }
            _ref.Retain();
        }

        /// <summary>
        /// Release this instance. Allows resources to be released when the associated <see cref="CancelationSource"/> is disposed (if <see cref="Release"/> has been called for all <see cref="Retain"/> calls).
        /// <para/>This should always be paired with a call to <see cref="Retain"/>
        /// </summary>
        public void Release()
        {
            if (!CanBeCanceled)
            {
                throw new InvalidOperationException("CancelationToken.Release: token cannot be canceled.", Internal.GetFormattedStacktrace(1));
            }
            _ref.Release();
        }

        public bool Equals(CancelationToken other)
        {
            return this == other;
        }

        public override bool Equals(object obj)
        {
            if (obj is CancelationSource)
            {
                return Equals((CancelationSource) obj);
            }
            return false;
        }

        public override int GetHashCode()
        {
            if (_ref == null)
            {
                return 0;
            }
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + _id.GetHashCode();
                hash = hash * 31 + _ref.GetHashCode();
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

        static partial void ValidateArgument(object arg, string argName, int skipFrames);
#if PROMISE_DEBUG
        static partial void ValidateArgument(object arg, string argName, int skipFrames)
        {
            Internal.ValidateArgument(arg, argName, skipFrames + 1);
        }
#endif
    }
}