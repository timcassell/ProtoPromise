#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
# endif

#pragma warning disable IDE0090 // Use 'new(...)'

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Proto.Promises
{
    partial class Internal
    {
        internal interface IResultContainer
        {
            IRejectContainer RejectContainer { get; }
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
        public readonly struct ResultContainer : Internal.IResultContainer
        {
            internal static ResultContainer Resolved
            {
                [MethodImpl(Internal.InlineOption)]
                get => new ResultContainer(null, State.Resolved);
            }

            internal readonly Promise<Internal.VoidResult>.ResultContainer _target;

            Internal.IRejectContainer Internal.IResultContainer.RejectContainer
            {
                [MethodImpl(Internal.InlineOption)]
                get => _target._rejectContainer;
            }

            [MethodImpl(Internal.InlineOption)]
            internal ResultContainer(Internal.IRejectContainer rejectContainer, State state)
            {
                _target = new Promise<Internal.VoidResult>.ResultContainer(default, rejectContainer, state);
            }

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
                => _target.RethrowIfRejectedOrCanceled();

            /// <summary>
            /// If the <see cref="Promise"/> is rejected, rethrow the rejection.
            /// </summary>
            [MethodImpl(Internal.InlineOption)]
            public void RethrowIfRejected()
                => _target.RethrowIfRejected();

            /// <summary>
            /// If the <see cref="Promise"/> is canceled, rethrow the cancelation.
            /// </summary>
            [MethodImpl(Internal.InlineOption)]
            public void RethrowIfCanceled()
                => _target.RethrowIfCanceled();

            /// <summary>
            /// Get the state of the <see cref="Promise"/>.
            /// </summary>
            public State State
            {
                [MethodImpl(Internal.InlineOption)]
                get => _target.State;
            }

            /// <summary>
            /// Gets the reason of the rejected <see cref="Promise{T}"/>.
            /// </summary>
            public object Reason
            {
                [MethodImpl(Internal.InlineOption)]
                get => _target.Reason;
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
        public readonly struct ResultContainer : Internal.IResultContainer
        {
            internal readonly Internal.IRejectContainer _rejectContainer;
            private readonly Promise.State _state;
            private readonly T _result;

            Internal.IRejectContainer Internal.IResultContainer.RejectContainer
            {
                [MethodImpl(Internal.InlineOption)]
                get => _rejectContainer;
            }

            [MethodImpl(Internal.InlineOption)]
            internal ResultContainer(in T result, Internal.IRejectContainer rejectContainer, Promise.State state)
            {
                _rejectContainer = rejectContainer;
                _state = state;
                _result = result;
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
                    _rejectContainer.GetExceptionDispatchInfo().Throw();
                }
            }

            /// <summary>
            /// If the <see cref="Promise{T}"/> is rejected, rethrow the rejection.
            /// </summary>
            public void RethrowIfRejected()
            {
                if (State == Promise.State.Rejected)
                {
                    _rejectContainer.GetExceptionDispatchInfo().Throw();
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
                get => _state;
            }

            /// <summary>
            /// Gets the result of the resolved <see cref="Promise{T}"/>.
            /// </summary>
            public T Value
            {
                [MethodImpl(Internal.InlineOption)]
                get => _result;
            }

            /// <summary>
            /// Gets the reason of the rejected <see cref="Promise{T}"/>.
            /// </summary>
            public object Reason => _state == Promise.State.Rejected ? _rejectContainer.Value : null;

            /// <summary>
            /// Cast to <see cref="Promise.ResultContainer"/>.
            /// </summary>
            [MethodImpl(Internal.InlineOption)]
            public static implicit operator Promise.ResultContainer(ResultContainer rhs)
                => new Promise.ResultContainer(rhs._rejectContainer, rhs._state);

            /// <summary>
            /// Wrap the value in <see cref="ResultContainer"/>.
            /// </summary>
            [MethodImpl(Internal.InlineOption)]
            public static implicit operator ResultContainer(in T value)
                => new ResultContainer(value, null, Promise.State.Resolved);
        }
    }
}