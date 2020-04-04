using System;

namespace Proto.Promises
{
    partial class Promise
    {
        /// <summary>
        /// Used to get the value of a <see cref="Promise"/> cancelation.
        /// An instance of <see cref="CancelReason"/> is only valid during the invocation of an onCanceled delegate.
        /// </summary>
        public struct CancelReason
        {
            private readonly Internal.IValueContainer _valueContainer;

            internal CancelReason(object valueContainer)
            {
                _valueContainer = (Internal.IValueContainer) valueContainer;
            }

            /// <summary>
            /// Gets the type of the cancel value, or null if there is no value.
            /// </summary>
            /// <value>The type of the value.</value>
            public Type ValueType
            {
                get
                {
                    ValidateCancelContainer(_valueContainer, 1);
                    return _valueContainer.ValueType;
                }
            }

            /// <summary>
            /// Gets the value.
            /// </summary>
            public object Value
            {
                get
                {
                    ValidateCancelContainer(_valueContainer, 1);
                    return _valueContainer.Value;
                }
            }

            /// <summary>
            /// Try to get the cancel value casted to <typeparamref name="T"/>.
            /// Returns true if successful, false otherwise.
            /// </summary>
            public bool TryGetValueAs<T>(out T value)
            {
                ValidateCancelContainer(_valueContainer, 1);
                return TryConvert(_valueContainer, out value);
            }
        }
    }
}