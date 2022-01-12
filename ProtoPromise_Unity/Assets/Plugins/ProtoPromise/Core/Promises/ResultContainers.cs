#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
# endif

#pragma warning disable IDE0034 // Simplify 'default' expression

using System;
using System.Runtime.CompilerServices;

namespace Proto.Promises
{
    /// <summary>
    /// Used to get the value of a rejection or cancelation.
    /// An instance of <see cref="ReasonContainer"/> is only valid during the invocation of the delegate it is passed into.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [System.Diagnostics.DebuggerNonUserCode]
#endif
    public
#if CSHARP_7_3_OR_NEWER
        readonly ref
#endif
        partial struct ReasonContainer
    {
        private readonly Internal.ValueContainer _valueContainer;
#if PROMISE_DEBUG
        private readonly long _id;
#endif

        /// <summary>
        /// FOR INTERNAL USE ONLY!
        /// </summary>
        internal ReasonContainer(Internal.ValueContainer valueContainer, long id)
        {
            _valueContainer = valueContainer;
#if PROMISE_DEBUG
            _id = id;
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
            return Internal.TryGetValue(_valueContainer, out value);
        }


        partial void Validate();
#if PROMISE_DEBUG
        partial void Validate()
        {
            bool isValid = _valueContainer != null && _id == Internal.InvokeId;
            if (!isValid)
            {
                throw new InvalidOperationException("An instance of ReasonContainer is only valid during the invocation of the delegate it is passed into.", Internal.GetFormattedStacktrace(2));
            }
        }
#endif
        }

    partial struct Promise
    {
        /// <summary>
        /// Used to get the value of a settled <see cref="Promise"/>.
        /// An instance of <see cref="ResultContainer"/> is only valid during the invocation of the onContinue delegate it is passed into.
        /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
        [System.Diagnostics.DebuggerNonUserCode]
#endif
        public
#if CSHARP_7_3_OR_NEWER
            readonly ref
#endif
            partial struct ResultContainer
        {
            private readonly Promise<Internal.VoidResult>.ResultContainer _target;

            /// <summary>
            /// FOR INTERNAL USE ONLY!
            /// </summary>
            [MethodImpl(Internal.InlineOption)]
            internal ResultContainer(Internal.ValueContainer valueContainer)
            {
                _target = new Promise<Internal.VoidResult>.ResultContainer(valueContainer);
            }

            /// <summary>
            /// FOR INTERNAL USE ONLY!
            /// </summary>
            [MethodImpl(Internal.InlineOption)]
            internal ResultContainer(
#if CSHARP_7_3_OR_NEWER
                in
#endif
                Promise<Internal.VoidResult>.ResultContainer target)
            {
                _target = target;
            }

            /// <summary>
            /// If the <see cref="Promise"/> is rejected, rethrow the rejection.
            /// </summary>
            [MethodImpl(Internal.InlineOption)]
            public void RethrowIfRejected()
            {
                _target.RethrowIfRejected();
            }

            /// <summary>
            /// If the <see cref="Promise"/> is canceled, rethrow the cancelation.
            /// </summary>
            [MethodImpl(Internal.InlineOption)]
            public void RethrowIfCanceled()
            {
                _target.RethrowIfCanceled();
            }

            /// <summary>
            /// Get the state of the <see cref="Promise"/>.
            /// </summary>
            public State State
            {
                [MethodImpl(Internal.InlineOption)]
                get { return _target.State; }
            }

            /// <summary>
            /// If the <see cref="Promise"/> is rejected, get a container of the reason.
            /// </summary>
            public ReasonContainer RejectContainer
            {
                [MethodImpl(Internal.InlineOption)]
                get { return _target.RejectContainer; }
            }

            [Obsolete("Cancelation reasons are no longer supported.", true)]
            public ReasonContainer CancelContainer
            {
                get { return _target.CancelContainer; }
            }
        }
    }

    partial struct Promise<T>
    {
        /// <summary>
        /// Used to get the value of a settled <see cref="Promise{T}"/>.
        /// An instance of <see cref="ResultContainer"/> is only valid during the invocation of the onContinue delegate it is passed into.
        /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
        [System.Diagnostics.DebuggerNonUserCode]
#endif
        public
#if CSHARP_7_3_OR_NEWER
            readonly ref
#endif
            partial struct ResultContainer
        {
            /// <summary>
            /// FOR INTERNAL USE ONLY!
            /// </summary>
            internal readonly Internal.ValueContainer _valueContainer;
            private readonly T _result;
#if PROMISE_DEBUG
            private readonly long _id;
            private long Id
            {
                [MethodImpl(Internal.InlineOption)]
                get { return _id; }
            }
#else
            private long Id
            {
                [MethodImpl(Internal.InlineOption)]
                get { return Internal.ValidIdFromApi; }
            }
#endif

            /// <summary>
            /// FOR INTERNAL USE ONLY!
            /// </summary>
            [MethodImpl(Internal.InlineOption)]
            internal ResultContainer(
#if CSHARP_7_3_OR_NEWER
                in
#endif
                T result)
            {
                _valueContainer = null;
                _result = result;
#if PROMISE_DEBUG
                _id = Internal.InvokeId;
#endif
            }

            /// <summary>
            /// FOR INTERNAL USE ONLY!
            /// </summary>
            [MethodImpl(Internal.InlineOption)]
            internal ResultContainer(Internal.ValueContainer valueContainer)
            {
                _valueContainer = valueContainer;
                _result = default(T);
#if PROMISE_DEBUG
                _id = Internal.InvokeId;
#endif
            }

            [MethodImpl(Internal.InlineOption)]
            private ResultContainer(Internal.ValueContainer valueContainer, long id, T result = default(T))
            {
                _valueContainer = valueContainer;
                _result = result;
#if PROMISE_DEBUG
                _id = id;
#endif
            }

            /// <summary>
            /// If the <see cref="Promise{T}"/> is rejected, rethrow the rejection.
            /// </summary>
            public void RethrowIfRejected()
            {
                ValidateCall();
                if (_valueContainer != null && _valueContainer.GetState() == Promise.State.Rejected)
                {
                    throw Internal.ForcedRethrowException.GetOrCreate();
                }
            }

            /// <summary>
            /// If the <see cref="Promise{T}"/> is canceled, rethrow the cancelation.
            /// </summary>
            public void RethrowIfCanceled()
            {
                ValidateCall();
                if (_valueContainer != null && _valueContainer.GetState() == Promise.State.Canceled)
                {
                    throw Internal.ForcedRethrowException.GetOrCreate();
                }
            }

            /// <summary>
            /// Get the state of the <see cref="Promise{T}"/>.
            /// </summary>
            public Promise.State State
            {
                get
                {
                    ValidateCall();
                    return _valueContainer != null
                        ? _valueContainer.GetState()
                        : Promise.State.Resolved;
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
                    return _valueContainer != null
                        ? _valueContainer.GetValue<T>()
                        : _result;
                }
            }

            /// <summary>
            /// If the <see cref="Promise{T}"/> is rejected, get a container of the reason.
            /// </summary>
            public ReasonContainer RejectContainer
            {
                [MethodImpl(Internal.InlineOption)]
                get
                {
                    ValidateCall();
                    ValidateRejected();
                    return new ReasonContainer(_valueContainer, Id);
                }
            }

            [MethodImpl(Internal.InlineOption)]
            public static implicit operator Promise.ResultContainer(ResultContainer rhs)
            {
                var newContainer = new Promise<Internal.VoidResult>.ResultContainer(rhs._valueContainer, rhs.Id);
                return new Promise.ResultContainer(newContainer);
            }

            partial void ValidateCall();
            partial void ValidateResolved();
            partial void ValidateRejected();
            partial void ValidateCanceled();
#if PROMISE_DEBUG
            partial void ValidateCall()
            {
                if (Id != Internal.InvokeId)
                {
                    throw new InvalidOperationException("An instance of ResultContainer is only valid during the invocation of the delegate it is passed into.", Internal.GetFormattedStacktrace(2));
                }
            }

            partial void ValidateResolved()
            {
                if (State != Promise.State.Resolved)
                {
                    throw new InvalidOperationException("Promise must be resolved in order to access Result.", Internal.GetFormattedStacktrace(2));
                }
            }

            partial void ValidateRejected()
            {
                if (State != Promise.State.Rejected)
                {
                    throw new InvalidOperationException("Promise must be rejected in order to access RejectContainer.", Internal.GetFormattedStacktrace(2));
                }
            }

            partial void ValidateCanceled()
            {
                if (State != Promise.State.Canceled)
                {
                    throw new InvalidOperationException("Promise must be canceled in order to access CancelContainer.", Internal.GetFormattedStacktrace(2));
                }
            }
#endif

            [Obsolete("Cancelation reasons are no longer supported.", true)]
            public ReasonContainer CancelContainer
            {
                [MethodImpl(Internal.InlineOption)]
                get
                {
                    throw new InvalidOperationException("Cancelation reasons are no longer supported.", Internal.GetFormattedStacktrace(1));
                }
            }
        }
    }
}