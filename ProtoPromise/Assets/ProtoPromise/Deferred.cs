using System;

namespace ProtoPromise
{
	partial class Promise
    {
        public enum PromiseState : byte
        {
            Pending,
            Resolved,
            Rejected,
            Canceled // This violates Promises/A+ API when CANCEL is enabled.
        }

        public abstract class DeferredBase : ICancelableAny, IRetainable
        {
            public PromiseState State { get; protected set; }

            public virtual Promise Promise { get; protected set; }

#if CSHARP_7_3_OR_NEWER // Really C# 7.2, but this symbol is the closest Unity offers.
            private
#endif
            protected DeferredBase() { }

            public void Retain()
            {
                Promise.Retain();
            }
            
            public void Release()
            {
                Promise.Release();
            }

            public bool IsRetained { get { return Promise.IsRetained; } }

            /// <summary>
            /// Report progress between 0 and 1.
            /// </summary>
            public abstract void ReportProgress(float progress);

            public abstract void Cancel();

            public abstract void Cancel<TCancel>(TCancel reason);
            
            public abstract void Reject();

            public abstract void Reject<TReject>(TReject reason);
        }

        public abstract class Deferred : DeferredBase
        {
#if CSHARP_7_3_OR_NEWER // Really C# 7.2, but this symbol is the closest Unity offers.
            private
#endif
            protected Deferred() { }

            public abstract void Resolve();
        }
    }

	public partial class Promise<T>
	{
		public abstract new class Deferred : DeferredBase
        {
            public new Promise<T> Promise { get { return (Promise<T>) base.Promise; } protected set { base.Promise = value; } }

#if CSHARP_7_3_OR_NEWER // Really C# 7.2, but this symbol is the closest Unity offers.
            private
#endif
            protected Deferred() { }

            public abstract void Resolve(T arg);
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
                    State = PromiseState.Pending;
                }

                public override void ReportProgress(float progress)
                {
                    var promise = Promise;
                    ValidateProgress();
                    ValidateOperation(promise);
                    ValidateProgress(progress);

                    if (State != PromiseState.Pending)
                    {
                        Logger.LogWarning("Deferred.ReportProgress - Deferred is not in the pending state.");
                        return;
                    }

                    promise.ReportProgress(progress);
                }

                public override void Resolve()
                {
                    var promise = Promise;
                    ValidateOperation(promise);

                    if (State == PromiseState.Pending)
                    {
                        promise.Release();
                    }
                    else
                    {
                        Logger.LogWarning("Deferred.Resolve - Deferred is not in the pending state.");
                        return;
                    }

                    State = PromiseState.Resolved;
                    promise.Resolve();
                }

                public override void Cancel()
                {
                    var promise = Promise;
                    ValidateCancel();
                    ValidateOperation(promise);

                    if (State == PromiseState.Pending)
                    {
                        promise.Release();
                    }
                    else
                    {
                        Logger.LogWarning("Deferred.Cancel - Deferred is not in the pending state.");
                        return;
                    }

                    State = PromiseState.Canceled;
                    promise.Cancel();
                }

                public override void Cancel<TCancel>(TCancel reason)
                {
                    var promise = Promise;
                    ValidateCancel();
                    ValidateOperation(promise);

                    if (State == PromiseState.Pending)
                    {
                        promise.Release();
                    }
                    else
                    {
                        Logger.LogWarning("Deferred.Cancel - Deferred is not in the pending state.");
                        return;
                    }

                    State = PromiseState.Canceled;
                    promise.Cancel(reason);
                }

                public override void Reject()
                {
                    var promise = Promise;
                    ValidateOperation(promise);

                    if (State == PromiseState.Pending)
                    {
                        promise.Release();
                        State = PromiseState.Rejected;
                    }
                    else
                    {
                        Logger.LogWarning("Deferred.Reject - Deferred is not in the pending state.");
                    }

                    promise.Reject(1);
                }

                public override void Reject<TReject>(TReject reason)
                {
                    var promise = Promise;
                    ValidateOperation(promise);

                    if (State == PromiseState.Pending)
                    {
                        promise.Release();
                        State = PromiseState.Rejected;
                    }
                    else
                    {
                        Logger.LogWarning("Deferred.Reject - Deferred is not in the pending state.");
                    }

                    promise.Reject(reason, 1);
                }

                public void RejectWithPromiseStacktrace(Exception exception)
                {
                    var promise = Promise;
                    var rejectValue = UnhandledExceptionException.GetOrCreate(exception);
                    _SetStackTraceFromCreated(promise, rejectValue);

                    if (State != PromiseState.Pending)
                    {
                        AddRejectionToUnhandledStack(rejectValue);
                        return;
                    }

                    State = PromiseState.Rejected;
                    promise.Release();
                    promise.RejectWithStateCheck(rejectValue);
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
                    State = PromiseState.Pending;
                }

                public override void ReportProgress(float progress)
                {
                    var promise = Promise;
                    ValidateProgress();
                    ValidateOperation(promise);
                    ValidateProgress(progress);

                    if (State != PromiseState.Pending)
                    {
                        Logger.LogWarning("Deferred.ReportProgress - Deferred is not in the pending state.");
                        return;
                    }

                    promise.ReportProgress(progress);
                }

                public override void Resolve(T arg)
                {
                    var promise = Promise;
                    ValidateOperation(promise);

                    if (State == PromiseState.Pending)
                    {
                        promise.Release();
                    }
                    else
                    {
                        Logger.LogWarning("Deferred.Resolve - Deferred is not in the pending state.");
                        return;
                    }

                    State = PromiseState.Resolved;
                    promise.Resolve(arg);
                }

                public override void Cancel()
                {
                    var promise = Promise;
                    ValidateCancel();
                    ValidateOperation(promise);

                    if (State == PromiseState.Pending)
                    {
                        promise.Release();
                    }
                    else
                    {
                        Logger.LogWarning("Deferred.Cancel - Deferred is not in the pending state.");
                        return;
                    }

                    State = PromiseState.Canceled;
                    promise.Cancel();
                }

                public override void Cancel<TCancel>(TCancel reason)
                {
                    var promise = Promise;
                    ValidateCancel();
                    ValidateOperation(promise);

                    if (State == PromiseState.Pending)
                    {
                        promise.Release();
                    }
                    else
                    {
                        Logger.LogWarning("Deferred.Cancel - Deferred is not in the pending state.");
                        return;
                    }

                    State = PromiseState.Canceled;
                    promise.Cancel(reason);
                }

                public override void Reject()
                {
                    var promise = Promise;
                    ValidateOperation(promise);

                    if (State == PromiseState.Pending)
                    {
                        promise.Release();
                        State = PromiseState.Rejected;
                    }
                    else
                    {
                        Logger.LogWarning("Deferred.Reject - Deferred is not in the pending state.");
                    }

                    promise.Reject(1);
                }

                public override void Reject<TReject>(TReject reason)
                {
                    var promise = Promise;
                    ValidateOperation(promise);

                    if (State == PromiseState.Pending)
                    {
                        promise.Release();
                        State = PromiseState.Rejected;
                    }
                    else
                    {
                        Logger.LogWarning("Deferred.Reject - Deferred is not in the pending state.");
                    }

                    promise.Reject(reason, 1);
                }

                public void RejectWithPromiseStacktrace(Exception exception)
                {
                    var promise = Promise;
                    var rejectValue = ProtoPromise.Promise.Internal.UnhandledExceptionException.GetOrCreate(exception);
                    _SetStackTraceFromCreated(promise, rejectValue);

                    if (State != PromiseState.Pending)
                    {
                        AddRejectionToUnhandledStack(rejectValue);
                        return;
                    }

                    State = PromiseState.Rejected;
                    promise.Release();
                    promise.RejectWithStateCheck(rejectValue);
                }
            }
        }
    }
}