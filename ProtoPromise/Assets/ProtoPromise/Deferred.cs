using System;

namespace Proto.Promises
{
	partial class Promise
    {
        public abstract partial class DeferredBase : ICancelableAny, IRetainable
        {
            public State State { get; protected set; }

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
                    State = State.Pending;
                }

                public override void ReportProgress(float progress)
                {
                    var promise = Promise;
                    ValidateProgress();
                    ValidateOperation(promise);
                    ValidateProgress(progress);

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
                    ValidateOperation(promise);

                    if (State == State.Pending)
                    {
                        promise.Release();
                    }
                    else
                    {
                        Logger.LogWarning("Deferred.Resolve - Deferred is not in the pending state.");
                        return;
                    }

                    State = State.Resolved;
                    promise.Resolve();
                }

                public override void Reject()
                {
                    var promise = Promise;
                    ValidateOperation(promise);

                    if (State == State.Pending)
                    {
                        promise.Release();
                        State = State.Rejected;
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

                    if (State == State.Pending)
                    {
                        promise.Release();
                        State = State.Rejected;
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

                    if (State != State.Pending)
                    {
                        AddRejectionToUnhandledStack(rejectValue);
                        return;
                    }

                    State = State.Rejected;
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
                    State = State.Pending;
                }

                public override void ReportProgress(float progress)
                {
                    var promise = Promise;
                    ValidateProgress();
                    ValidateOperation(promise);
                    ValidateProgress(progress);

                    if (State != State.Pending)
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

                    if (State == State.Pending)
                    {
                        promise.Release();
                    }
                    else
                    {
                        Logger.LogWarning("Deferred.Resolve - Deferred is not in the pending state.");
                        return;
                    }

                    State = State.Resolved;
                    promise.Resolve(arg);
                }

                public override void Reject()
                {
                    var promise = Promise;
                    ValidateOperation(promise);

                    if (State == State.Pending)
                    {
                        promise.Release();
                        State = State.Rejected;
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

                    if (State == State.Pending)
                    {
                        promise.Release();
                        State = State.Rejected;
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
                    var rejectValue = Promises.Promise.Internal.UnhandledExceptionException.GetOrCreate(exception);
                    _SetStackTraceFromCreated(promise, rejectValue);

                    if (State != State.Pending)
                    {
                        AddRejectionToUnhandledStack(rejectValue);
                        return;
                    }

                    State = State.Rejected;
                    promise.Release();
                    promise.RejectWithStateCheck(rejectValue);
                }
            }
        }
    }
}