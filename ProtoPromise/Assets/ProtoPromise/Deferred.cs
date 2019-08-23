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
}