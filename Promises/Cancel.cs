using System;

namespace Proto.Promises
{
    partial class Promise
    {
        /// <summary>
        /// Cancelation source used to cancel promises.
        /// </summary>
        public struct CancelationSource : IDisposable, IEquatable<CancelationSource>
        {
            private readonly Internal.CancelationRef _ref;
            private readonly int _id;

            /// <summary>
            /// Get a new <see cref="CancelationSource"/>.
            /// </summary>
            public static CancelationSource New()
            {
                return new CancelationSource(Internal.CancelationRef.GetOrCreate());
            }

            private CancelationSource(Internal.CancelationRef cancelationRef)
            {
                _ref = cancelationRef;
                _id = _ref.SourceId;
            }

            /// <summary>
            /// Get the <see cref="CancelationToken"/> associated with this <see cref="CancelationSource"/>.
            /// </summary>
            public CancelationToken Token
            {
                get
                {
                    if (!IsValid)
                    {
                        throw new InvalidOperationException("CancelationSource is not valid.", GetFormattedStacktrace(1));
                    }
                    return new CancelationToken(_ref);
                }
            }

            /// <summary>
            /// Get whether or not this <see cref="CancelationSource"/> is valid.
            /// <para/>A <see cref="CancelationSource"/> is valid if it was created from <see cref="New"/> and was not disposed.
            /// </summary>
            public bool IsValid
            {
                get
                {
                    return _ref != null && _ref.SourceId == _id;
                }
            }

            /// <summary>
            /// Release all resources used by this <see cref="CancelationSource"/>. This instance will no longer be valid.
            /// </summary>
            public void Dispose()
            {
                if (!IsValid)
                {
                    throw new InvalidOperationException("CancelationSource is not valid.", GetFormattedStacktrace(1));
                }
                _ref.Dispose();
            }

            public bool Equals(CancelationSource other)
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

            public static bool operator ==(CancelationSource c1, CancelationSource c2)
            {
                return c1._ref == c2._ref & c1._id == c2._id;
            }

            public static bool operator !=(CancelationSource c1, CancelationSource c2)
            {
                return !(c1 == c2);
            }
        }

        /// <summary>
        /// Propagates notification that operations should be canceled.
        /// </summary>
        public struct CancelationToken : IRetainable, IEquatable<CancelationToken>
        {
            private readonly Internal.CancelationRef _ref;
            private readonly int _id;

            internal CancelationToken(object cancelationRef)
            {
                _ref = (Internal.CancelationRef) cancelationRef;
                _id = _ref.TokenId;
            }

            /// <summary>
            /// Returns an empty <see cref="CancelationToken"/>.
            /// </summary>
            public static CancelationToken None { get { return default(CancelationToken); } }

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
                        throw new InvalidOperationException("CancelationToken has not been canceled.", GetFormattedStacktrace(1));
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
                        throw new InvalidOperationException("CancelationToken has not been canceled.", GetFormattedStacktrace(1));
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
                    throw new InvalidOperationException("CancelationToken has not been canceled.", GetFormattedStacktrace(1));
                }
                return TryConvert(_ref.ValueContainer, out value);
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
                    throw new InvalidOperationException("CancelationToken.Register is invalid because it cannot be canceled.", GetFormattedStacktrace(1));
                }
                ValidateArgument(callback, "callback", 1);
                if (_ref.IsCanceled)
                {
                    callback.Invoke(new ReasonContainer(_ref.ValueContainer));
                    return default(CancelationRegistration);
                }
                return new CancelationRegistration(_ref, Internal.CancelDelegate.GetOrCreate(callback, 1));
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
                    throw new InvalidOperationException("CancelationToken.Register is invalid because it cannot be canceled.", GetFormattedStacktrace(1));
                }
                ValidateArgument(callback, "callback", 1);
                if (_ref.IsCanceled)
                {
                    callback.Invoke(captureValue, new ReasonContainer(_ref.ValueContainer));
                    return default(CancelationRegistration);
                }
                return new CancelationRegistration(_ref, Internal.CancelDelegateCapture<TCapture>.GetOrCreate(captureValue, callback, 1));
            }

            /// <summary>
            /// Retain this instance. Allows continued use of this instance, even after the associated <see cref="CancelationSource"/> has been disposed, until this is released.
            /// <para/>This should always be paired with a call to <see cref="Release"/>
            /// </summary>
            public void Retain()
            {
                if (!CanBeCanceled)
                {
                    throw new InvalidOperationException("CancelationToken.Retain is invalid because it cannot be canceled.", GetFormattedStacktrace(1));
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
                    throw new InvalidOperationException("CancelationToken.Release is invalid because it cannot be canceled.", GetFormattedStacktrace(1));
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
        }

        /// <summary>
        /// Represents a callback delegate that has been registered with a <see cref="CancelationToken"/>.
        /// </summary>
        public struct CancelationRegistration : IEquatable<CancelationRegistration>
        {
            private readonly Internal.CancelationRef _ref;
            private readonly int _id;
            private readonly int _order;

            internal CancelationRegistration(object cancelationRef, object cancelDelegate)
            {
                _ref = (Internal.CancelationRef) cancelationRef;
                _id = _ref.SourceId;
                _order = _ref.Register((Internal.ICancelDelegate) cancelDelegate);
            }

            /// <summary>
            /// Get whether the callback is registered and the associated <see cref="CancelationToken"/> has not been canceled.
            /// </summary>
            public bool IsRegistered
            {
                get
                {
                    return _ref != null && _ref.IsRegistered(_order);
                }
            }

            /// <summary>
            /// Unregister the callback from the associated <see cref="CancelationToken"/>.
            /// </summary>
            public void Unregister()
            {
                if (!IsRegistered)
                {
                    throw new InvalidOperationException("CancelationRegistration is not registered.", GetFormattedStacktrace(1));
                }
                _ref.Unregister(_order);
            }

            public bool Equals(CancelationRegistration other)
            {
                return this == other;
            }

            public override bool Equals(object obj)
            {
                if (obj is CancelationRegistration)
                {
                    return Equals((CancelationRegistration) obj);
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
                    hash = hash * 31 + _order.GetHashCode();
                    hash = hash * 31 + _ref.GetHashCode();
                    return hash;
                }
            }

            public static bool operator ==(CancelationRegistration c1, CancelationRegistration c2)
            {
                return c1._ref == c2._ref & c1._id == c2._id & c1._order == c2._order;
            }

            public static bool operator !=(CancelationRegistration c1, CancelationRegistration c2)
            {
                return !(c1 == c2);
            }
        }
    }
}