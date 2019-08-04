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

        // TODO: Handle resolve/reject called after Promise is canceled.
        public abstract class DeferredBase : ICancelableAny, IRetainable
        {
            public DeferredState State { get; protected set; }

            public virtual Promise Promise { get; protected set; }

            protected DeferredBase() { }

            public void Retain()
            {
                Promise.Retain();
            }
            
            public void Release()
            {
                Promise.Release();
                if (!Promise.IsRetained && State != DeferredState.Pending)
                {
                    Dispose();
                }
            }

            public bool IsRetained { get { return Promise.IsRetained; } }

            /// <summary>
            /// Report progress between 0 and 1.
            /// </summary>
            public void ReportProgress(float progress)
			{
#if DEBUG
                // TODO
				//if (_promise == null)
				//{
				//	throw new ObjectDisposedException("Deferred");
				//}
				if (progress < 0f || progress > 1f)
				{
#pragma warning disable RECS0163 // Suggest the usage of the nameof operator
					throw new ArgumentOutOfRangeException("progress", "Must be between 0 and 1.");
#pragma warning restore RECS0163 // Suggest the usage of the nameof operator
				}
#endif
				if (State != DeferredState.Pending)
				{
					Logger.LogWarning("Deferred.ReportProgress - Deferred is not in the pending state.");
					return;
				}

                HandleProgress(progress);
			}

			public void Cancel()
			{
                ValidateCancel();
#if DEBUG
                // TODO
                //if (_promise == null)
                //{
                //  throw new ObjectDisposedException("Deferred");
                //}
#endif
                if (State != DeferredState.Pending)
				{
					Logger.LogWarning("Deferred.Cancel - Deferred is not in the pending state.");
					return;
				}

                State = DeferredState.Canceled;

                HandleCancel();
			}

			public void Cancel<TCancel>(TCancel reason)
			{
                ValidateCancel();
#if DEBUG
                // TODO
                //if (_promise == null)
                //{
                //  throw new ObjectDisposedException("Deferred");
                //}
#endif
                if (State != DeferredState.Pending)
				{
					Logger.LogWarning("Deferred.Cancel - Deferred is not in the pending state.");
					return;
                }

                State = DeferredState.Canceled;

                HandleCancel(reason);
			}

			public void Reject<TReject>(TReject reason)
			{
#if DEBUG
                // TODO
                //if (_promise == null)
                //{
                //  throw new ObjectDisposedException("Deferred");
                //}
#endif
                if (State != DeferredState.Pending)
				{
					Logger.LogWarning("Deferred.Reject - Deferred is not in the pending state. Attempted reject reason:\n" + reason);
					return;
                }

                State = DeferredState.Rejected;

                HandleReject(reason);
			}

			public void Reject()
			{
#if DEBUG
                // TODO
                //if (_promise == null)
				//{
				//	throw new ObjectDisposedException("Deferred");
				//}
#endif
				if (State != DeferredState.Pending)
				{
					Logger.LogError("Deferred.Reject - Deferred is not in the pending state.");
					return;
                }

                State = DeferredState.Rejected;

                HandleReject();
			}

            protected virtual void Dispose()
            {
                Promise = null;
            }

            // These functions protect the inner workings of the promises in case someone decides to inherit from DeferredBase.
            protected abstract void HandleProgress(float progress);
            protected abstract void HandleCancel();
            protected abstract void HandleCancel<TCancel>(TCancel reason);
            protected abstract void HandleReject();
            protected abstract void HandleReject<TReject>(TReject reason);
        }

        static partial void ValidateDisposed();

        public abstract class Deferred : DeferredBase
        {
            protected Deferred() : base() { }

            public void Resolve()
            {
#if DEBUG
                // TODO
                //if (_promise == null)
                //{
                //  throw new ObjectDisposedException("Deferred");
                //}
#endif
                if (State != DeferredState.Pending)
                {
                    Logger.LogWarning("Deferred.Resolve - Deferred is not in the pending state.");
                    return;
                }

                HandleResolve();
            }

            protected abstract void HandleResolve();
        }
    }

	public partial class Promise<T>
	{
		public abstract new class Deferred : DeferredBase
        {
            public new Promise<T> Promise { get { return (Promise<T>) base.Promise; } protected set { base.Promise = value; } }

            protected Deferred() : base() { }

            public void Resolve(T arg)
            {
#if DEBUG
                // TODO
                //if (_promise == null)
                //{
                //  throw new ObjectDisposedException("Deferred");
                //}
#endif
                if (State != DeferredState.Pending)
                {
                    Logger.LogWarning("Deferred.Resolve - Deferred is not in the pending state.");
                    return;
                }

                HandleResolve(arg);
            }

            protected abstract void HandleResolve(T arg);
        }
    }

    partial class Promise
    {
        partial class Internal
        {
            public sealed class DeferredInternal : Deferred, ILinked<DeferredInternal>
            {
                DeferredInternal ILinked<DeferredInternal>.Next { get; set; }

                private static ValueLinkedStack<DeferredInternal> _pool;

                public static DeferredInternal GetOrCreate(Promise target)
                {
                    var deferred = _pool.IsNotEmpty ? _pool.Pop() : new DeferredInternal();
                    deferred.Promise = target;
                    return deferred;
                }

                protected override void Dispose()
                {
                    base.Dispose();
                    _pool.Push(this);
                }

                protected override void HandleProgress(float progress)
                {
                    if (progress >= 1f)
                    {
                        // Don't report progress 1.0, that will be reported automatically when the promise is resolved.
                        return;
                    }

                    Promise.ReportProgress(progress);
                }

                protected override void HandleResolve()
                {
                    Promise.Resolve();
                }

                protected override void HandleCancel()
                {
                    Promise.Cancel();
                }

                protected override void HandleCancel<TCancel>(TCancel reason)
                {
                    Promise.Cancel(reason);
                }

                protected override void HandleReject()
                {
                    Promise.Reject();
                }

                protected override void HandleReject<TReject>(TReject reason)
                {
                    Promise.Reject(reason);
                }
            }
        }
    }

    partial class Promise<T>
    {
        protected static new class Internal
        {
            public sealed class DeferredInternal : Deferred, ILinked<DeferredInternal>
            {
                DeferredInternal ILinked<DeferredInternal>.Next { get; set; }

#pragma warning disable RECS0108 // Warns about static fields in generic types
                private static ValueLinkedStack<DeferredInternal> _pool;
#pragma warning restore RECS0108 // Warns about static fields in generic types

                public static DeferredInternal GetOrCreate(Promise<T> target)
                {
                    var deferred = _pool.IsNotEmpty ? _pool.Pop() : new DeferredInternal();
                    deferred.Promise = target;
                    return deferred;
                }

                protected override void Dispose()
                {
                    base.Dispose();
                    _pool.Push(this);
                }

                protected override void HandleProgress(float progress)
                {
                    if (progress >= 1f)
                    {
                        // Don't report progress 1.0, that will be reported automatically when the promise is resolved.
                        return;
                    }

                    Promise.ReportProgress(progress);
                }

                protected override void HandleResolve(T arg)
                {
                    Promise.Resolve(arg);
                }

                protected override void HandleCancel()
                {
                    Promise.Cancel();
                }

                protected override void HandleCancel<TCancel>(TCancel reason)
                {
                    Promise.Cancel(reason);
                }

                protected override void HandleReject()
                {
                    Promise.Reject();
                }

                protected override void HandleReject<TReject>(TReject reason)
                {
                    Promise.Reject(reason);
                }
            }
        }
    }
}