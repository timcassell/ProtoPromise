using System;

namespace ProtoPromise
{
	partial class Promise
	{
		public abstract class DeferredBase : ICancelableAny, IRetainable
		{
			public void Retain()
			{
				_promise.Retain();
			}

			public void Release()
			{
				_promise.Release();
			}

			public PromiseState State { get { return _promise.State; } }

			protected Promise _promise;

			protected DeferredBase() { }

			/// <summary>
			/// Report progress between 0 and 1.
			/// </summary>
			public void ReportProgress(float progress)
			{
#if DEBUG
				if (_promise == null)
				{
					throw new ObjectDisposedException("Deferred");
				}
				if (progress < 0f || progress > 1f)
				{
#pragma warning disable RECS0163 // Suggest the usage of the nameof operator
					throw new ArgumentOutOfRangeException("progress", "Must be between 0 and 1.");
#pragma warning restore RECS0163 // Suggest the usage of the nameof operator
				}
#endif
				if (State != PromiseState.Pending)
				{
					Logger.LogWarning("Deferred.ReportProgress - Deferred is not in the pending state.");
					return;
				}

				if (progress >= 1f)
				{
					// Don't report progress 1.0, that will be reported automatically when the promise is resolved.
					return;
				}

				_promise.ReportProgress(progress);
			}

			public void Cancel()
			{
#if DEBUG
				if (_promise == null)
				{
					throw new ObjectDisposedException("Deferred");
				}
#endif
				if (State != PromiseState.Pending)
				{
					Logger.LogWarning("Deferred.Cancel - Deferred is not in the pending state.");
					return;
				}

				_promise.Cancel();
			}

			public void Cancel<TCancel>(TCancel reason)
			{
#if DEBUG
				if (_promise == null)
				{
					throw new ObjectDisposedException("Deferred");
				}
#endif
				if (State != PromiseState.Pending)
				{
					Logger.LogWarning("Deferred.Cancel - Deferred is not in the pending state.");
					return;
				}

				_promise.Cancel(reason);
			}

			public void Reject<TReject>(TReject reason)
			{
#if DEBUG
				if (_promise == null)
				{
					throw new ObjectDisposedException("Deferred");
				}
#endif
				if (State != PromiseState.Pending)
				{
					Logger.LogWarning("Deferred.Reject - Deferred is not in the pending state. Attempted reject reason:\n" + reason);
					return;
				}

				_promise.Reject(reason);
			}

			public void Reject()
			{
#if DEBUG
				if (_promise == null)
				{
					throw new ObjectDisposedException("Deferred");
				}
#endif
				if (State != PromiseState.Pending)
				{
					Logger.LogError("Deferred.Reject - Deferred is not in the pending state.");
					return;
				}

				_promise.Reject();
			}

			protected void TryDispose()
			{
				if (retains.ContainsKey(_promise))
				{
					return;
				}

				Dispose();
			}

			protected virtual void Dispose()
			{
				_promise = null;
			}
		}


		// TODO: Make these abstract and put the concrete implementation in Internal.
		public sealed class Deferred : DeferredBase, ILinked<Deferred>
		{
			public Promise Promise { get { return _promise; } }

			Deferred ILinked<Deferred>.Next { get; set; }

			private Deferred() : base() { }

			private static ValueLinkedStack<Deferred> pool;

			internal static Deferred GetOrCreate(Promise promise)
			{
				var deferred = pool.IsNotEmpty ? pool.Pop() : new Deferred();
				deferred._promise = promise;
				return deferred;
			}

			public void Resolve()
			{
#if DEBUG
				if (_promise == null)
				{
					throw new ObjectDisposedException("Deferred");
				}
#endif
				if (State != PromiseState.Pending)
				{
					Logger.LogWarning("Deferred.Resolve - Deferred is not in the pending state.");
					return;
				}

				_promise.Resolve();
			}

			protected override void Dispose()
			{
				pool.Push(this);
			}
		}
	}

	public partial class Promise<T>
	{
		public sealed new class Deferred : DeferredBase, ILinked<Deferred>
		{
			public Promise<T> Promise { get { return (Promise<T>) _promise; } }

			Deferred ILinked<Deferred>.Next { get; set; }

			internal Deferred() : base() { }

#pragma warning disable RECS0108 // Warns about static fields in generic types
			private static ValueLinkedStack<Deferred> pool;
#pragma warning restore RECS0108 // Warns about static fields in generic types

			internal static Deferred GetOrCreate(Promise<T> promise)
			{
				var deferred = pool.IsNotEmpty ? pool.Pop() : new Deferred();
				deferred._promise = promise;
				return deferred;
			}

			public void Resolve(T arg)
			{
#if DEBUG
				if (_promise == null)
				{
					throw new ObjectDisposedException("Deferred");
				}
#endif
				if (State != PromiseState.Pending)
				{
					Logger.LogWarning("Deferred.Resolve - Deferred is not in the pending state.");
					return;
				}

				Promise.Resolve(arg);
			}

			protected override void Dispose()
			{
				pool.Push(this);
			}
		}
	}
}