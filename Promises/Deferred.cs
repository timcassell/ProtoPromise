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
                    AddRejectionToUnhandledStack(UnhandledDeferredException.instance, Promise);
                }
            }

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
                    promise.ReleaseWithoutDisposeCheck();
                    promise.CancelDirectIfPending();
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
                    promise.ReleaseWithoutDisposeCheck();
                    promise.CancelDirectIfPending(reason);
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
                        promise.ReleaseWithoutDisposeCheck();
                        promise.ResolveDirectIfNotCanceled();
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

                    if (State == State.Pending)
                    {
                        State = State.Rejected;
                        promise.ReleaseWithoutDisposeCheck();
                        promise.RejectDirectIfNotCanceled(reason, true);
                    }
                    else
                    {
                        AddRejectionToUnhandledStack(reason, null);
                        Logger.LogWarning("Deferred.Reject - Deferred is not in the pending state.");
                    }
                }
            }

            public sealed class DeferredInternal<T> : Promise<T>.Deferred
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

                public override void Resolve(T value)
                {
                    var promise = Promise;
                    ValidateOperation(promise, 1);

                    if (State == State.Pending)
                    {
                        State = State.Resolved;
                        promise.ReleaseWithoutDisposeCheck();
                        promise.ResolveDirectIfNotCanceled(value);
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

                    if (State == State.Pending)
                    {
                        State = State.Rejected;
                        promise.ReleaseWithoutDisposeCheck();
                        promise.RejectDirectIfNotCanceled(reason, true);
                    }
                    else
                    {
                        AddRejectionToUnhandledStack(reason, null);
                        Logger.LogWarning("Deferred.Reject - Deferred is not in the pending state.");
                    }
                }
            }
        }
    }
}