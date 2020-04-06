#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
# endif

using System;

namespace Proto.Promises
{
    partial class Promise
    {
        /// <summary>
        /// Used to get the value of a <see cref="Promise"/> cancelation.
        /// An instance of <see cref="CancelReason"/> is only valid during the invocation of the onCanceled delegate it is passed into.
        /// </summary>
        [System.Diagnostics.DebuggerStepThrough]
        public partial struct CancelReason
        {
            private readonly Internal.IValueContainer _valueContainer;
#if PROMISE_DEBUG
            private readonly ulong _id;
#endif

            internal CancelReason(object valueContainer)
            {
                _valueContainer = (Internal.IValueContainer) valueContainer;
#if PROMISE_DEBUG
                _id = _invokeId;
#endif
            }

            /// <summary>
            /// Get the type of the cancel value, or null if there is no value.
            /// </summary>
            /// <value>The type of the value.</value>
            public Type ValueType
            {
                get
                {
                    Validate();
                    return _valueContainer.ValueType;
                }
            }

            /// <summary>
            /// Get the cancel value.
            /// <para/>NOTE: Use <see cref="TryGetValueAs{T}(out T)"/> if you want to prevent value type boxing.
            /// </summary>
            public object Value
            {
                get
                {
                    Validate();
                    return _valueContainer.Value;
                }
            }

            /// <summary>
            /// Try to get the cancel value casted to <typeparamref name="T"/>.
            /// Returns true if successful, false otherwise.
            /// </summary>
            public bool TryGetValueAs<T>(out T value)
            {
                Validate();
                return TryConvert(_valueContainer, out value);
            }


            partial void Validate();
#if PROMISE_DEBUG
            partial void Validate()
            {
                if (_id != _invokeId | ReferenceEquals(_valueContainer, null))
                {
                    throw new InvalidOperationException("An instance of Promise.CancelContainer is only valid during the invocation of the onCanceled delegate it is passed into.", GetFormattedStacktrace(2));
                }
            }
#endif
        }
    }
}