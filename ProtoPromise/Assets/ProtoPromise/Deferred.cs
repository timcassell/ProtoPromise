using System;

namespace ProtoPromise
{
	partial class Promise
    {
        public enum DeferredState : byte
        {
            Pending,
            Resolved,
            Rejected,
            Canceled // This violates Promises/A+ API, but I felt its usefulness outweighs API adherence.
        }

        public abstract class DeferredBase : ICancelableAny, IRetainable
        {
            public DeferredState State { get { return Promise._state; } }

            public virtual Promise Promise { get; protected set; }

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
            protected Deferred() { }

            public abstract void Resolve();
        }
    }

	public partial class Promise<T>
	{
		public abstract new class Deferred : DeferredBase
        {
            public new Promise<T> Promise { get { return (Promise<T>) base.Promise; } protected set { base.Promise = value; } }

            protected Deferred() { }

            public abstract void Resolve(T arg);
        }
    }
}