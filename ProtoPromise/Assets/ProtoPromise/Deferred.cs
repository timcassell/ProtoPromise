﻿using System;

namespace Proto.Promises
{
	partial class Promise
    {
        /// <summary>
        /// Deferred base. An instance of this can be used to handle the state of the attached <see cref="Promise"/>. except resolve. You must use <see cref="Deferred"/> or <see cref="Promise{T}.Deferred"/> to handle resolve.
        /// </summary>
        public abstract partial class DeferredBase : ICancelableAny, IRetainable
        {
            public State State { get; protected set; }

            /// <summary>
            /// The <see cref="Promise"/> that this controls.
            /// </summary>
            public Promise Promise { get; protected set; }

#if CSHARP_7_3_OR_NEWER // Really C# 7.2, but this symbol is the closest Unity offers.
            private
#endif
            protected DeferredBase() { }

            /// <summary>
            /// Retain this instance and the linked <see cref="Promise"/>.
            /// <para/>This should always be paired with a call to <see cref="Release"/>
            /// </summary>
            public void Retain()
            {
                Promise.Retain();
            }

            /// <summary>
            /// Release this instance and the linked <see cref="Promise"/>.
            /// <para/>This should always be paired with a call to <see cref="Retain"/>
            /// </summary>
            public void Release()
            {
                Promise.Release();
            }

            /// <summary>
            /// Reject the linked <see cref="Promise"/> without a reason.
            /// <para/>NOTE: It is recommended to always reject with a reason!
            /// </summary>
            public abstract void Reject();

            /// <summary>
            /// Reject the linked <see cref="Promise"/> with <paramref name="reason"/>.
            /// </summary>
            public abstract void Reject<TReject>(TReject reason);
        }

        /// <summary>
        /// An instance of this is used to handle the state of the <see cref="DeferredBase.Promise"/>.
        /// </summary>
        public abstract class Deferred : DeferredBase
        {
#if CSHARP_7_3_OR_NEWER // Really C# 7.2, but this symbol is the closest Unity offers.
            private
#endif
            protected Deferred() { }

            /// <summary>
            /// Resolve the linked <see cref="Promise"/>.
            /// </summary>
            public abstract void Resolve();
        }
    }

	public partial class Promise<T>
    {
        /// <summary>
        /// An instance of this is used to handle the state of the <see cref="Promise"/>.
        /// </summary>
		public abstract new class Deferred : DeferredBase
        {
            /// <summary>
            /// The <see cref="Promise{T}"/> that this controls.
            /// </summary>
            public new Promise<T> Promise { get { return (Promise<T>) base.Promise; } protected set { base.Promise = value; } }

#if CSHARP_7_3_OR_NEWER // Really C# 7.2, but this symbol is the closest Unity offers.
            private
#endif
            protected Deferred() { }

            /// <summary>
            /// Resolve the linked <see cref="Promise"/> with <paramref name="value"/>.
            /// </summary>
#if CSHARP_7_3_OR_NEWER // Really C# 7.2, but this symbol is the closest Unity offers.
            public abstract void Resolve(in T value);
#else
            public abstract void Resolve(T value);
#endif
        }
    }

    partial class Promise
    {
        partial class Internal
        {
            public sealed class DeferredInternal : Deferred
            {
                public DeferredInternal(Promise target)
                {
                    Promise = target;
                }

                public void Reset()
                {
                    State = State.Pending;
                }

                public override void ReportProgress(float progress)
                {
                    var promise = Promise;
                    ValidateProgress();
                    ValidateOperation(promise, 1);
                    ValidateProgress(progress, 1);

                    if (State != State.Pending)
                    {
                        Logger.LogWarning("Deferred.ReportProgress - Deferred is not in the pending state.");
                        return;
                    }

                    promise.ReportProgress(progress);
                }

                public override void Resolve()
                {
                    var promise = Promise;
                    ValidateOperation(promise, 1);

                    if (State == State.Pending)
                    {
                        State = State.Resolved;
                        promise.Release();
                        promise.Resolve();
                    }
                    else
                    {
                        Logger.LogWarning("Deferred.Resolve - Deferred is not in the pending state.");
                        return;
                    }
                }

                public override void Reject()
                {
                    var promise = Promise;
                    ValidateOperation(promise, 1);

                    var rejection = CreateRejection(1);

                    if (State == State.Pending)
                    {
                        State = State.Rejected;
                        promise.Release();
                        promise.RejectDirect(rejection);
                    }
                    else
                    {
                        AddRejectionToUnhandledStack(rejection);
                        Logger.LogWarning("Deferred.Reject - Deferred is not in the pending state.");
                    }
                }

                public override void Reject<TReject>(TReject reason)
                {
                    var promise = Promise;
                    ValidateOperation(promise, 1);

                    var rejection = CreateRejection(reason, 1);

                    if (State == State.Pending)
                    {
                        State = State.Rejected;
                        promise.Release();
                        promise.RejectDirect(rejection);
                    }
                    else
                    {
                        AddRejectionToUnhandledStack(rejection);
                        Logger.LogWarning("Deferred.Reject - Deferred is not in the pending state.");
                    }
                }

                public void RejectWithPromiseStacktrace(Exception exception)
                {
                    var promise = Promise;
                    var rejection = UnhandledExceptionException.GetOrCreate(exception);
                    _SetStackTraceFromCreated(promise, rejection);

                    if (State == State.Pending)
                    {
                        State = State.Rejected;
                        promise.Release();
                        promise.RejectDirect(rejection);
                    }
                    else
                    {
                        AddRejectionToUnhandledStack(rejection);
                    }
                }
            }
        }
    }

    partial class Promise<T>
    {
        protected static new class Internal
        {
            public sealed class DeferredInternal : Deferred
            {
                public DeferredInternal(Promise<T> target)
                {
                    Promise = target;
                }

                public void Reset()
                {
                    State = State.Pending;
                }

                public override void ReportProgress(float progress)
                {
                    var promise = Promise;
                    ValidateProgress();
                    ValidateOperation(promise, 1);
                    ValidateProgress(progress, 1);

                    if (State != State.Pending)
                    {
                        Logger.LogWarning("Deferred.ReportProgress - Deferred is not in the pending state.");
                        return;
                    }

                    promise.ReportProgress(progress);
                }

#if CSHARP_7_3_OR_NEWER // Really C# 7.2, but this symbol is the closest Unity offers.
                public override void Resolve(in T value)
#else
                public override void Resolve(T value)
#endif
                {
                    var promise = Promise;
                    ValidateOperation(promise, 1);

                    if (State == State.Pending)
                    {
                        State = State.Resolved;
                        promise.Release();
                        promise.Resolve(value);
                    }
                    else
                    {
                        Logger.LogWarning("Deferred.Resolve - Deferred is not in the pending state.");
                        return;
                    }
                }

                public override void Reject()
                {
                    var promise = Promise;
                    ValidateOperation(promise, 1);

                    var rejection = CreateRejection(1);

                    if (State == State.Pending)
                    {
                        State = State.Rejected;
                        promise.Release();
                        promise.RejectDirect(rejection);
                    }
                    else
                    {
                        AddRejectionToUnhandledStack(rejection);
                        Logger.LogWarning("Deferred.Reject - Deferred is not in the pending state.");
                    }
                }

                public override void Reject<TReject>(TReject reason)
                {
                    var promise = Promise;
                    ValidateOperation(promise, 1);

                    var rejection = CreateRejection(reason, 1);

                    if (State == State.Pending)
                    {
                        State = State.Rejected;
                        promise.Release();
                        promise.RejectDirect(rejection);
                    }
                    else
                    {
                        AddRejectionToUnhandledStack(rejection);
                        Logger.LogWarning("Deferred.Reject - Deferred is not in the pending state.");
                    }
                }

                public void RejectWithPromiseStacktrace(Exception exception)
                {
                    var promise = Promise;
                    var rejection = Promises.Promise.Internal.UnhandledExceptionException.GetOrCreate(exception);
                    _SetStackTraceFromCreated(promise, rejection);

                    if (State == State.Pending)
                    {
                        promise.Release();
                        State = State.Rejected;
                        promise.RejectDirect(rejection);
                    }
                    else
                    {
                        AddRejectionToUnhandledStack(rejection);
                    }
                }
            }
        }
    }
}