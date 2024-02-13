#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
# endif

#pragma warning disable IDE0034 // Simplify 'default' expression

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Proto.Promises
{
    partial struct Promise
    {
        /// <summary>
        /// Used to get the state and/or reason of a settled <see cref="Promise"/>.
        /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        public readonly struct ResultContainer
        {
            internal static ResultContainer Resolved
            {
                [MethodImpl(Internal.InlineOption)]
                get { return new ResultContainer(null, State.Resolved); }
            }

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
            internal ResultContainer(in Promise<Internal.VoidResult>.ResultContainer target)
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
            public object Reason
            {
                [MethodImpl(Internal.InlineOption)]
                get { return _target.Reason; }
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
        public readonly partial struct ResultContainer
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
            internal ResultContainer(in T result, object rejectContainer, Promise.State state)
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
            public T Value
            {
                [MethodImpl(Internal.InlineOption)]
                get { return _result; }
            }

            /// <summary>
            /// Gets the reason of the rejected <see cref="Promise{T}"/>.
            /// </summary>
            public object Reason
            {
                get
                {
                    return _state == Promise.State.Rejected
                        ? _rejectContainer.UnsafeAs<Internal.IRejectContainer>().Value
                        : null;
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

            /// <summary>
            /// Wrap the value in <see cref="ResultContainer"/>.
            /// </summary>
            [MethodImpl(Internal.InlineOption)]
            public static implicit operator ResultContainer(in T value)
            {
                return new ResultContainer(value, null, Promise.State.Resolved);
            }
        }
    }
}