using System;

namespace ProtoPromiseLite
{
	public partial class Promise : UnityEngine.CustomYieldInstruction
	{
		internal Promise Next { get; set; }

		internal readonly ADeferred Deferred;
		public DeferredState State { get { return Deferred.State; } }

		internal Promise(ADeferred deferred)
		{
			Deferred = deferred;
		}

        public override bool keepWaiting
        {
            get
            {
				return State == DeferredState.Pending || State == DeferredState.Resolving;
            }
        }

		public Promise Done(Action callback)
		{
			Deferred.Done(callback);
			return this;
		}

		public Promise Fail(Action callback)
		{
			Deferred.Fail(callback);
			return this;
		}

		public Promise Fail<TFail>(Action<TFail> callback)
		{
			Deferred.Fail(callback);
			return this;
		}

		public Promise Fail(Action<Exception> callback)
		{
			Deferred.Fail(callback);
			return this;
		}

		public void Finally(Action callback)
		{
			Deferred.Finally(callback);
		}

		public Promise Progress(Action<int, int> callback)
		{
			Deferred.Progress(callback);
			return this;
		}

		public Promise<T> Then<T>(Func<Promise<T>> callback)
		{
			Deferred.Then(() => (Promise) callback());
			return Deferred.GetPromise<T>();
		}

		public Promise Then(Func<Promise> callback)
		{
			Deferred.Then(callback);
			return this;
		}

		public Promise<T> Then<T>(Func<T> callback)
		{


			Deferred.Then(callback);
			return Deferred.GetPromise<T>();
		}

		public Promise Then(Action callback)
		{
			Deferred.Then(callback);
			return this;
		}

		internal virtual IDelegate GetDoneDelegate()
		{
			return default(IDelegate);
		}
	}

	public class Promise<T> : Promise
	{
		internal Promise(ADeferred deferred) : base(deferred) {}

		public new Promise<T> Done(Action callback)
		{
			base.Done(callback);
			return this;
		}

		public Promise<T> Done(Action<T> callback)
		{
			Deferred.Done(callback);
			return this;
		}

		public new Promise<T> Fail(Action callback)
		{
			base.Fail(callback);
			return this;
		}

		public new Promise<T> Fail<TFail>(Action<TFail> callback)
		{
			base.Fail(callback);
			return this;
		}

		public new Promise<T> Fail(Action<Exception> callback)
		{
			base.Fail(callback);
			return this;
		}

		public new Promise<T> Progress(Action<int, int> callback)
		{
			base.Progress(callback);
			return this;
		}

		public Promise<T> Then(Func<Promise<T>> callback)
		{
			Deferred.Then(() => (Promise) callback());
			return this;
		}

		public Promise<T> Then(Func<T> callback)
		{
			base.Then(callback);
			return this;
		}

		public Promise Then(Action<T> callback)
		{
			Deferred.Then(callback);
			return this;
		}

		public Promise<T> Then(Func<T, T> callback)
		{
			Deferred.Then(callback);
			return this;
		}

		public Promise<TResult> Then<TResult>(Func<T, TResult> callback)
		{
			Deferred.Then(callback);
			return Deferred.GetPromise<TResult>();
		}

		public Promise Then(Func<T, Promise> callback)
		{
			Deferred.Then(callback);
			return this;
		}

		public Promise<T> Then(Func<T, Promise<T>> callback)
		{
			Func<T, Promise> cb = t => callback(t);
			Deferred.Then(cb);
			return this;
		}

		internal override IDelegate GetDoneDelegate()
		{
			return Deferred.GetDoneDelegate(typeof(T));
		}
	}
}