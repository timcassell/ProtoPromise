#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif
#if !PROTO_PROMISE_CANCEL_DISABLE
#define PROMISE_CANCEL
#else
#undef PROMISE_CANCEL
#endif
#if !PROTO_PROMISE_PROGRESS_DISABLE
#define PROMISE_PROGRESS
#else
#undef PROMISE_PROGRESS
#endif

#pragma warning disable CS0672 // Member overrides obsolete member
#pragma warning disable CS0618 // Type or member is obsolete

using System;

namespace Proto.Promises
{
    partial class Promise
    {
        /// <summary>
        /// Deferred base. An instance of this can be used to handle the state of the attached <see cref="Promise"/>, except resolve. You must use <see cref="Deferred"/> or <see cref="Promise{T}.Deferred"/> to handle resolve.
        /// </summary>
        public abstract class DeferredBase : ICancelableAny, IRetainable
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

            ~DeferredBase()
            {
                if (State == State.Pending)
                {
                    // Deferred wasn't handled.
                    var exception = Internal.UnhandledExceptionException.GetOrCreate(UnhandledDeferredException.instance);
                    SetStacktraceFromCreated(Promise, exception);
                    AddRejectionToUnhandledStack(exception);
                }
            }

            /// <summary>
            /// Retain this instance and the linked <see cref="Promise"/>.
            /// <para/>This should always be paired with a call to <see cref="Release"/>
            /// </summary>
            public void Retain()
            {
                Promise.RetainInternal();
            }

            /// <summary>
            /// Release this instance and the linked <see cref="Promise"/>.
            /// <para/>This should always be paired with a call to <see cref="Retain"/>
            /// </summary>
            public void Release()
            {
                Promise.ReleaseInternal();
            }

            /// <summary>
            /// Reject the linked <see cref="Promise"/> with <paramref name="reason"/>.
            /// </summary>
            public abstract void Reject<TReject>(TReject reason);


            /// <summary>
            /// Report progress between 0 and 1.
            /// </summary>
#if !PROMISE_PROGRESS
            [Obsolete("Progress is disabled. Remove PROTO_PROMISE_PROGRESS_DISABLE from your compiler symbols to enable progress reports.", true)]
#endif
            public abstract void ReportProgress(float progress);

            /// <summary>
            /// Cancel the linked <see cref="Promise"/> and all promises that have been chained from it without a reason.
            /// </summary>
#if !PROMISE_CANCEL
            [Obsolete("Cancelations are disabled. Remove PROTO_PROMISE_CANCEL_DISABLE from your compiler symbols to enable cancelations.", true)]
#endif
            public void Cancel()
            {
                ValidateCancel(1);
                var promise = Promise;
                ValidateOperation(promise, 1);

                if (State == State.Pending)
                {
                    State = State.Canceled;
                    promise.Cancel();
                    promise.ReleaseInternal();
                }
                else
                {
                    Logger.LogWarning("Deferred.Cancel - Deferred is not in the pending state.");
                }
            }

            /// <summary>
            /// Cancel the linked <see cref="Promise"/> and all promises that have been chained from it with <paramref name="reason"/>.
            /// </summary>
#if !PROMISE_CANCEL
            [Obsolete("Cancelations are disabled. Remove PROTO_PROMISE_CANCEL_DISABLE from your compiler symbols to enable cancelations.", true)]
#endif
            public void Cancel<TCancel>(TCancel reason)
            {
                ValidateCancel(1);
                var promise = Promise;
                ValidateOperation(promise, 1);

                if (State == State.Pending)
                {
                    State = State.Canceled;
                    promise.Cancel(reason);
                    promise.ReleaseInternal();
                }
                else
                {
                    Logger.LogWarning("Deferred.Cancel - Deferred is not in the pending state.");
                }
            }
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
            /// Resolve the linked <see cref="Promise{T}"/> with <paramref name="value"/>.
            /// </summary>
            public abstract void Resolve(T value);

#if CSHARP_7_3_OR_NEWER // Really C# 7.2, but this symbol is the closest Unity offers.
            /// <summary>
            /// Resolve the linked <see cref="Promise{T}"/> with <paramref name="value"/>.
            /// Use this method to resolve a large struct.
            /// </summary>
            public abstract void Resolve(in T value);
#endif
        }
    }

    partial class Promise
    {
        partial class Internal
        {
            public sealed class DeferredInternal0 : Deferred
            {
                public DeferredInternal0(Promise target)
                {
                    Promise = target;
                }

                public void Reset()
                {
                    State = State.Pending;
                }

                public void ReleaseDirect()
                {
                    State = State.Canceled; // Stop the finalizer from thinking this was not handled.
                    Promise.ReleaseInternal();
                }

                public override void ReportProgress(float progress)
                {
                    var promise = Promise;
                    ValidateProgress(1);
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
                        promise.ResolveDirectIfNotCanceled();
                        promise.ReleaseInternal();
                    }
                    else
                    {
                        Logger.LogWarning("Deferred.Resolve - Deferred is not in the pending state.");
                        return;
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
                        promise.RejectDirectIfNotCanceled(rejection);
                        promise.ReleaseInternal();
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
                        promise.RejectDirectIfNotCanceled(rejection);
                        promise.ReleaseInternal();
                    }
                    else
                    {
                        AddRejectionToUnhandledStack(rejection);
                    }
                }
            }

            public sealed class DeferredInternal<T> : Promise<T>.Deferred
            {
                public void Reset()
                {
                    State = State.Pending;
                }

                public void ReleaseDirect()
                {
                    State = State.Canceled; // Stop the finalizer from thinking this was not handled.
                    Promise.ReleaseInternal();
                }

                public override void ReportProgress(float progress)
                {
                    var promise = Promise;
                    ValidateProgress(1);
                    ValidateOperation(promise, 1);
                    ValidateProgress(progress, 1);

                    if (State != State.Pending)
                    {
                        Logger.LogWarning("Deferred.ReportProgress - Deferred is not in the pending state.");
                        return;
                    }

                    promise.ReportProgress(progress);
                }
                public DeferredInternal(Promise<T> target)
                {
                    Promise = target;
                }

                public override void Resolve(T value)
                {
                    var promise = (PromiseInternal<T>) Promise;
                    ValidateOperation(promise, 1);

                    if (State == State.Pending)
                    {
                        State = State.Resolved;
                        promise.ResolveDirectIfNotCanceled(value);
                        promise.ReleaseInternal();
                    }
                    else
                    {
                        Logger.LogWarning("Deferred.Resolve - Deferred is not in the pending state.");
                        return;
                    }
                }

#if CSHARP_7_3_OR_NEWER // Really C# 7.2, but this symbol is the closest Unity offers.
                public override void Resolve(in T value)
                {
                    var promise = (PromiseInternal<T>) Promise;
                    ValidateOperation(promise, 1);

                    if (State == State.Pending)
                    {
                        State = State.Resolved;
                        promise.ResolveDirectIfNotCanceled(value);
                        promise.ReleaseInternal();
                    }
                    else
                    {
                        Logger.LogWarning("Deferred.Resolve - Deferred is not in the pending state.");
                        return;
                    }
                }
#endif

                public override void Reject<TReject>(TReject reason)
                {
                    var promise = Promise;
                    ValidateOperation(promise, 1);

                    var rejection = CreateRejection(reason, 1);

                    if (State == State.Pending)
                    {
                        State = State.Rejected;
                        promise.RejectDirectIfNotCanceled(rejection);
                        promise.ReleaseInternal();
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
                        promise.RejectDirectIfNotCanceled(rejection);
                        promise.ReleaseInternal();
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