#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
# endif
#if !PROTO_PROMISE_CANCEL_DISABLE
#define PROMISE_CANCEL
#else
#undef PROMISE_CANCEL
#endif

using System;

namespace Proto.Promises
{
    partial class Promise
    {
        /// <summary>
        /// Used to get the value of a settled <see cref="Promise"/>.
        /// An instance of <see cref="ResultContainer"/> is only valid during the invocation of the onContinue delegate it is passed into.
        /// </summary>
        [System.Diagnostics.DebuggerStepThrough]
        public partial struct ResultContainer
        {
            private readonly Internal.IValueContainer _valueContainer;
#if PROMISE_DEBUG
            private readonly ulong _id;
#endif

            internal ResultContainer(object valueContainer)
            {
                _valueContainer = (Internal.IValueContainer) valueContainer;
#if PROMISE_DEBUG
                _id = _invokeId;
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
                    throw RethrowException.instance;
                }
            }

            /// <summary>
            /// If the <see cref="Promise"/> is canceled, rethrow the cancelation.
            /// </summary>
#if !PROMISE_CANCEL
            [Obsolete("Cancelations are disabled. Remove PROTO_PROMISE_CANCEL_DISABLE from your compiler symbols to enable cancelations.", true)]
#endif
            public void RethrowIfCanceled()
            {
                ValidateCancel(1);
                ValidateCall();
                if (_valueContainer.GetState() == State.Canceled)
                {
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
#if !PROMISE_CANCEL
            [Obsolete("Cancelations are disabled. Remove PROTO_PROMISE_CANCEL_DISABLE from your compiler symbols to enable cancelations.", true)]
#endif
            public ReasonContainer CancelContainer
            {
                get
                {
                    ValidateCancel(1);
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
                if (_id != _invokeId | ReferenceEquals(_valueContainer, null))
                {
                    throw new InvalidOperationException("An instance of Promise.CancelContainer is only valid during the invocation of the delegate it is passed into.", GetFormattedStacktrace(2));
                }
            }

            partial void ValidateRejected()
            {
                if (_valueContainer.GetState() != State.Rejected)
                {
                    throw new InvalidOperationException("Promise must be rejected in order to access RejectContainer.", GetFormattedStacktrace(2));
                }
            }

#if PROMISE_CANCEL
            partial void ValidateCanceled()
            {
                if (_valueContainer.GetState() != State.Canceled)
                {
                    throw new InvalidOperationException("Promise must be canceled in order to access CancelContainer.", GetFormattedStacktrace(2));
                }
            }
#endif
#endif
        }

        /// <summary>
        /// Used to get the value of a <see cref="Promise"/> rejection or cancelation.
        /// An instance of <see cref="ReasonContainer"/> is only valid during the invocation of the delegate it is passed into.
        /// </summary>
        [System.Diagnostics.DebuggerStepThrough]
        public partial struct ReasonContainer
        {
            private readonly Internal.IValueContainer _valueContainer;
#if PROMISE_DEBUG
            private readonly ulong _id;
#endif

            internal ReasonContainer(object valueContainer)
            {
                _valueContainer = (Internal.IValueContainer) valueContainer;
#if PROMISE_DEBUG
                _id = _invokeId;
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
                return TryConvert(_valueContainer, out value);
            }


            partial void Validate();
#if PROMISE_DEBUG
            partial void Validate()
            {
                if (_id != _invokeId | ReferenceEquals(_valueContainer, null))
                {
                    throw new InvalidOperationException("An instance of Promise.ReasonContainer is only valid during the invocation of the delegate it is passed into.", GetFormattedStacktrace(2));
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
        [System.Diagnostics.DebuggerStepThrough]
        public new partial struct ResultContainer
        {
            private readonly Internal.IValueContainer _valueContainer;
#if PROMISE_DEBUG
            private readonly ulong _id;
#endif

            internal ResultContainer(object valueContainer)
            {
                _valueContainer = (Internal.IValueContainer) valueContainer;
#if PROMISE_DEBUG
                _id = _invokeId;
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
                    throw RethrowException.instance;
                }
            }

            /// <summary>
            /// If the <see cref="Promise{T}"/> is canceled, rethrow the cancelation.
            /// </summary>
#if !PROMISE_CANCEL
            [Obsolete("Cancelations are disabled. Remove PROTO_PROMISE_CANCEL_DISABLE from your compiler symbols to enable cancelations.", true)]
#endif
            public void RethrowIfCanceled()
            {
                ValidateCancel(1);
                ValidateCall();
                if (_valueContainer.GetState() == State.Canceled)
                {
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
#if !PROMISE_CANCEL
            [Obsolete("Cancelations are disabled. Remove PROTO_PROMISE_CANCEL_DISABLE from your compiler symbols to enable cancelations.", true)]
#endif
            public ReasonContainer CancelContainer
            {
                get
                {
                    ValidateCancel(1);
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
                if (_id != _invokeId | ReferenceEquals(_valueContainer, null))
                {
                    throw new InvalidOperationException("An instance of Promise.CancelContainer is only valid during the invocation of the delegate it is passed into.", GetFormattedStacktrace(2));
                }
            }

            partial void ValidateResolved()
            {
                if (_valueContainer.GetState() != State.Resolved)
                {
                    throw new InvalidOperationException("Promise must be resolved in order to access Result.", GetFormattedStacktrace(2));
                }
            }

            partial void ValidateRejected()
            {
                if (_valueContainer.GetState() != State.Rejected)
                {
                    throw new InvalidOperationException("Promise must be rejected in order to access RejectContainer.", GetFormattedStacktrace(2));
                }
            }

#if PROMISE_CANCEL
            partial void ValidateCanceled()
            {
                if (_valueContainer.GetState() != State.Canceled)
                {
                    throw new InvalidOperationException("Promise must be canceled in order to access CancelContainer.", GetFormattedStacktrace(2));
                }
            }
#endif
#endif
        }
    }
}