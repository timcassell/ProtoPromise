#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
# endif

#pragma warning disable IDE0034 // Simplify 'default' expression
#pragma warning disable 1591 // Missing XML comment for publicly visible type or member

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Proto.Promises
{
    /// <summary>
    /// Used to get the value of a rejection or cancelation.
    /// An instance of <see cref="ReasonContainer"/> is only valid during the invocation of the delegate it is passed into.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    [Obsolete("Promise.ResultContainer.RejectContainer is deprected, use RejectReason instead (returns object)", false), EditorBrowsable(EditorBrowsableState.Never)]
    public
#if CSHARP_7_3_OR_NEWER
        readonly ref
#endif
        partial struct ReasonContainer
    {
        private readonly Internal.IRejectContainer _rejectContainer;

        /// <summary>
        /// FOR INTERNAL USE ONLY!
        /// </summary>
        internal ReasonContainer(Internal.IRejectContainer valueContainer)
        {
            _rejectContainer = valueContainer;
        }

        /// <summary>
        /// Get the type of the value.
        /// </summary>
        public Type ValueType
        {
            get
            {
                return _rejectContainer.Value.GetType();
            }
        }

        /// <summary>
        /// Get the value.
        /// </summary>
        public object Value
        {
            get
            {
                return _rejectContainer.Value;
            }
        }

        /// <summary>
        /// Try to get the value casted to <typeparamref name="T"/>.
        /// Returns true if successful, false otherwise.
        /// </summary>
        public bool TryGetValueAs<T>(out T value)
        {
            return _rejectContainer.TryGetValue(out value);
        }
    }

    partial struct Promise
    {
        /// <summary>
        /// Used to get the state and/or reason of a settled <see cref="Promise"/>.
        /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        public
#if CSHARP_7_3_OR_NEWER
            readonly
#endif
            struct ResultContainer
        {
            /// <summary>
            /// FOR INTERNAL USE ONLY!
            /// </summary>
            internal readonly Promise<Internal.VoidResult>.ResultContainer _target;

            /// <summary>
            /// FOR INTERNAL USE ONLY!
            /// </summary>
            [MethodImpl(Internal.InlineOption)]
            internal ResultContainer(object rejectContainer, State state)
            {
                _target = new Promise<Internal.VoidResult>.ResultContainer(default(Internal.VoidResult), rejectContainer, state);
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
            /// If the <see cref="Promise"/> is rejected or canceled, rethrow the reason.
            /// </summary>
            [MethodImpl(Internal.InlineOption)]
            public void RethrowIfRejectedOrCanceled()
            {
                _target.RethrowIfRejectedOrCanceled();
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
            /// Gets the reason of the rejected <see cref="Promise{T}"/>.
            /// </summary>
            public object RejectReason
            {
                [MethodImpl(Internal.InlineOption)]
                get { return _target.RejectReason; }
            }

            [Obsolete("Prefer RejectReason", false), EditorBrowsable(EditorBrowsableState.Never)]
            public ReasonContainer RejectContainer
            {
                get { return _target.RejectContainer; }
            }

            [Obsolete("Cancelation reasons are no longer supported.", true), EditorBrowsable(EditorBrowsableState.Never)]
            public ReasonContainer CancelContainer
            {
                get { return _target.CancelContainer; }
            }
        }
    }

    partial struct Promise<T>
    {
        /// <summary>
        /// Used to get the state and result or reason of a settled <see cref="Promise{T}"/>.
        /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        public
#if CSHARP_7_3_OR_NEWER
            readonly
#endif
            partial struct ResultContainer
        {
            /// <summary>
            /// FOR INTERNAL USE ONLY!
            /// </summary>
            internal readonly object _rejectContainer;
            private readonly Promise.State _state;
            private readonly T _result;

            /// <summary>
            /// FOR INTERNAL USE ONLY!
            /// </summary>
            [MethodImpl(Internal.InlineOption)]
            internal ResultContainer(
#if CSHARP_7_3_OR_NEWER
                in
#endif
                T result, object rejectContainer, Promise.State state)
            {
                _rejectContainer = rejectContainer;
                _state = state;
                _result = result;
            }

            [MethodImpl(Internal.InlineOption)]
            private ResultContainer(object rejectContainer, Promise.State state)
                : this(default(T), rejectContainer, state)
            {
            }

            /// <summary>
            /// If the <see cref="Promise{T}"/> is rejected or canceled, rethrow the reason.
            /// </summary>
            public void RethrowIfRejectedOrCanceled()
            {
                if (State >= Promise.State.Rejected)
                {
                    if (_state == Promise.State.Canceled)
                    {
                        throw Promise.CancelException();
                    }
                    _rejectContainer.UnsafeAs<Internal.IRejectContainer>().GetExceptionDispatchInfo().Throw();
                }
            }

            /// <summary>
            /// If the <see cref="Promise{T}"/> is rejected, rethrow the rejection.
            /// </summary>
            public void RethrowIfRejected()
            {
                if (State == Promise.State.Rejected)
                {
                    _rejectContainer.UnsafeAs<Internal.IRejectContainer>().GetExceptionDispatchInfo().Throw();
                }
            }

            /// <summary>
            /// If the <see cref="Promise{T}"/> is canceled, rethrow the cancelation.
            /// </summary>
            public void RethrowIfCanceled()
            {
                if (State == Promise.State.Canceled)
                {
                    throw Promise.CancelException();
                }
            }

            /// <summary>
            /// Get the state of the <see cref="Promise{T}"/>.
            /// </summary>
            public Promise.State State
            {
                [MethodImpl(Internal.InlineOption)]
                get { return _state; }
            }

            /// <summary>
            /// Gets the result of the resolved <see cref="Promise{T}"/>.
            /// </summary>
            public T Result
            {
                [MethodImpl(Internal.InlineOption)]
                get { return _result; }
            }

            /// <summary>
            /// Gets the reason of the rejected <see cref="Promise{T}"/>.
            /// </summary>
            public object RejectReason
            {
                get
                {
                    return _state == Promise.State.Rejected
                        ? _rejectContainer.UnsafeAs<Internal.IRejectContainer>().Value
                        : null;
                }
            }

            [Obsolete("Prefer RejectReason", false), EditorBrowsable(EditorBrowsableState.Never)]
            public ReasonContainer RejectContainer
            {
                get
                {
                    ValidateRejected();
                    return new ReasonContainer(_rejectContainer.UnsafeAs<Internal.IRejectContainer>());
                }
            }

            /// <summary>
            /// Cast to <see cref="Promise.ResultContainer"/>.
            /// </summary>
            [MethodImpl(Internal.InlineOption)]
            public static implicit operator Promise.ResultContainer(ResultContainer rhs)
            {
                var newContainer = new Promise<Internal.VoidResult>.ResultContainer(rhs._rejectContainer, rhs._state);
                return new Promise.ResultContainer(newContainer);
            }

            partial void ValidateRejected();
#if PROMISE_DEBUG
            partial void ValidateRejected()
            {
                if (State != Promise.State.Rejected)
                {
                    throw new InvalidOperationException("Promise must be rejected in order to access RejectContainer.", Internal.GetFormattedStacktrace(2));
                }
            }
#endif

            [Obsolete("Cancelation reasons are no longer supported.", true), EditorBrowsable(EditorBrowsableState.Never)]
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