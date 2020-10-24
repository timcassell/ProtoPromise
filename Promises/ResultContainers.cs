#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
# endif

using System;

namespace Proto.Promises
{
    /// <summary>
    /// Used to get the value of a rejection or cancelation.
    /// An instance of <see cref="ReasonContainer"/> is only valid during the invocation of the delegate it is passed into.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [System.Diagnostics.DebuggerNonUserCode]
#endif
    public partial struct ReasonContainer
    {
        private readonly Internal.IValueContainer _valueContainer;
#if PROMISE_DEBUG
        private readonly ulong _id;
#endif

        /// <summary>
        /// FOR INTERNAL USE ONLY!
        /// </summary>
        internal ReasonContainer(Internal.IValueContainer valueContainer)
        {
            _valueContainer = valueContainer;
#if PROMISE_DEBUG
            _id = Internal.InvokeId;
#endif
        }

        /// <summary>
        /// Get the type of the value, or null if there is no value.
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
        /// Get the value.
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
        /// Try to get the value casted to <typeparamref name="T"/>.
        /// Returns true if successful, false otherwise.
        /// </summary>
        public bool TryGetValueAs<T>(out T value)
        {
            Validate();
            return Internal.TryConvert(_valueContainer, out value);
        }


        partial void Validate();
#if PROMISE_DEBUG
        partial void Validate()
        {
            if (_id != Internal.InvokeId | ReferenceEquals(_valueContainer, null))
            {
                throw new InvalidOperationException("An instance of Promise.ReasonContainer is only valid during the invocation of the delegate it is passed into.", Internal.GetFormattedStacktrace(2));
            }
        }
#endif
    }

    partial class Promise
    {
        /// <summary>
        /// Used to get the value of a settled <see cref="Promise"/>.
        /// An instance of <see cref="ResultContainer"/> is only valid during the invocation of the onContinue delegate it is passed into.
        /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
        [System.Diagnostics.DebuggerNonUserCode]
#endif
        public partial struct ResultContainer
        {
            private readonly Internal.IValueContainer _valueContainer;
#if PROMISE_DEBUG
            private readonly ulong _id;
#endif

            /// <summary>
            /// FOR INTERNAL USE ONLY!
            /// </summary>
            internal ResultContainer(Internal.IValueContainer valueContainer)
            {
                _valueContainer = valueContainer;
#if PROMISE_DEBUG
                _id = Internal.InvokeId;
#endif
            }

            /// <summary>
            /// If the <see cref="Promise"/> is rejected, rethrow the rejection.
            /// </summary>
            public void RethrowIfRejected()
            {
                ValidateCall();
                if (_valueContainer.GetState() == State.Rejected)
                {
                    Internal.invokingRejected = true;
                    throw RethrowException.instance;
                }
            }

            /// <summary>
            /// If the <see cref="Promise"/> is canceled, rethrow the cancelation.
            /// </summary>
            public void RethrowIfCanceled()
            {
                ValidateCall();
                if (_valueContainer.GetState() == State.Canceled)
                {
                    Internal.invokingRejected = true;
                    throw RethrowException.instance;
                }
            }

            /// <summary>
            /// Get the state of the <see cref="Promise"/>.
            /// </summary>
            public State State
            {
                get
                {
                    ValidateCall();
                    return _valueContainer.GetState();
                }
            }

            /// <summary>
            /// If the <see cref="Promise"/> is rejected, get a container of the reason.
            /// </summary>
            public ReasonContainer RejectContainer
            {
                get
                {
                    ValidateCall();
                    ValidateRejected();
                    return new ReasonContainer(_valueContainer);
                }
            }

            /// <summary>
            /// If the <see cref="Promise"/> is canceled, get a container of the reason.
            /// </summary>
            public ReasonContainer CancelContainer
            {
                get
                {
                    ValidateCall();
                    ValidateCanceled();
                    return new ReasonContainer(_valueContainer);
                }
            }

            partial void ValidateCall();
            partial void ValidateRejected();
            partial void ValidateCanceled();
#if PROMISE_DEBUG
            partial void ValidateCall()
            {
                if (_id != Internal.InvokeId | ReferenceEquals(_valueContainer, null))
                {
                    throw new InvalidOperationException("An instance of Promise.CancelContainer is only valid during the invocation of the delegate it is passed into.", Internal.GetFormattedStacktrace(2));
                }
            }

            partial void ValidateRejected()
            {
                if (_valueContainer.GetState() != State.Rejected)
                {
                    throw new InvalidOperationException("Promise must be rejected in order to access RejectContainer.", Internal.GetFormattedStacktrace(2));
                }
            }

            partial void ValidateCanceled()
            {
                if (_valueContainer.GetState() != State.Canceled)
                {
                    throw new InvalidOperationException("Promise must be canceled in order to access CancelContainer.", Internal.GetFormattedStacktrace(2));
                }
            }
#endif
        }
    }

    partial class Promise<T>
    {
        /// <summary>
        /// Used to get the value of a settled <see cref="Promise{T}"/>.
        /// An instance of <see cref="ResultContainer"/> is only valid during the invocation of the onContinue delegate it is passed into.
        /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
        [System.Diagnostics.DebuggerNonUserCode]
#endif
        public new partial struct ResultContainer
        {
            private readonly Internal.IValueContainer _valueContainer;
#if PROMISE_DEBUG
            private readonly ulong _id;
#endif

            /// <summary>
            /// FOR INTERNAL USE ONLY!
            /// </summary>
            internal ResultContainer(Internal.IValueContainer valueContainer)
            {
                _valueContainer = valueContainer;
#if PROMISE_DEBUG
                _id = Internal.InvokeId;
#endif
            }

            /// <summary>
            /// If the <see cref="Promise{T}"/> is rejected, rethrow the rejection.
            /// </summary>
            public void RethrowIfRejected()
            {
                ValidateCall();
                if (_valueContainer.GetState() == State.Rejected)
                {
                    Internal.invokingRejected = true;
                    throw RethrowException.instance;
                }
            }

            /// <summary>
            /// If the <see cref="Promise{T}"/> is canceled, rethrow the cancelation.
            /// </summary>
            public void RethrowIfCanceled()
            {
                ValidateCall();
                if (_valueContainer.GetState() == State.Canceled)
                {
                    Internal.invokingRejected = true;
                    throw RethrowException.instance;
                }
            }

            /// <summary>
            /// Get the state of the <see cref="Promise{T}"/>.
            /// </summary>
            public State State
            {
                get
                {
                    ValidateCall();
                    return _valueContainer.GetState();
                }
            }

            /// <summary>
            /// If the <see cref="Promise{T}"/> is resolved, get its result.
            /// </summary>
            public T Result
            {
                get
                {
                    ValidateCall();
                    ValidateResolved();
                    return ((Internal.ResolveContainer<T>) _valueContainer).value;
                }
            }

            /// <summary>
            /// If the <see cref="Promise{T}"/> is rejected, get a container of the reason.
            /// </summary>
            public ReasonContainer RejectContainer
            {
                get
                {
                    ValidateCall();
                    ValidateRejected();
                    return new ReasonContainer(_valueContainer);
                }
            }

            /// <summary>
            /// If the <see cref="Promise{T}"/> is canceled, get a container of the reason.
            /// </summary>
            public ReasonContainer CancelContainer
            {
                get
                {
                    ValidateCall();
                    ValidateCanceled();
                    return new ReasonContainer(_valueContainer);
                }
            }

            partial void ValidateCall();
            partial void ValidateResolved();
            partial void ValidateRejected();
            partial void ValidateCanceled();
#if PROMISE_DEBUG
            partial void ValidateCall()
            {
                if (_id != Internal.InvokeId | ReferenceEquals(_valueContainer, null))
                {
                    throw new InvalidOperationException("An instance of Promise.CancelContainer is only valid during the invocation of the delegate it is passed into.", Internal.GetFormattedStacktrace(2));
                }
            }

            partial void ValidateResolved()
            {
                if (_valueContainer.GetState() != State.Resolved)
                {
                    throw new InvalidOperationException("Promise must be resolved in order to access Result.", Internal.GetFormattedStacktrace(2));
                }
            }

            partial void ValidateRejected()
            {
                if (_valueContainer.GetState() != State.Rejected)
                {
                    throw new InvalidOperationException("Promise must be rejected in order to access RejectContainer.", Internal.GetFormattedStacktrace(2));
                }
            }

            partial void ValidateCanceled()
            {
                if (_valueContainer.GetState() != State.Canceled)
                {
                    throw new InvalidOperationException("Promise must be canceled in order to access CancelContainer.", Internal.GetFormattedStacktrace(2));
                }
            }
#endif
        }
    }
}