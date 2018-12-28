using System;

namespace uPromise
{
    public partial class Promise : UnityEngine.CustomYieldInstruction
	{
        public override bool keepWaiting
        {
            get
            {
                return State == DeferredState.Pending;
            }
        }

		protected readonly Deferred Deferred;
		public DeferredState State{get { return Deferred.State; }}

		public Promise(Deferred deferred)
		{
			Deferred = deferred;
		}

		#region Public Events API
		
		public void Done(Action<object> callback)
		{
			Deferred.Done(callback);
		}

		public void Done(Action callback)
		{
			Deferred.Done(callback);
		}

		public Promise Fail(Action callback)
		{
			Deferred.Fail(callback);
			return this;
		}
		public Promise Fail(Action<object> callback)
		{
			Deferred.Fail(callback);
			return this;
		}

		public Promise Fail<TFail>(Action<TFail> callback)
		{
			Deferred.Fail(callback);
			return this;
		}

		public Promise Finally(Action<object> callback)
		{
			Deferred.Finally(callback);
			return this;
		}

		public Promise Finally(Action callback)
		{
			Deferred.Finally(callback);
			return this;
		}

		public Promise Progress(Action<object> callback)
		{
			Deferred.Progress(callback);
			return this;
		}

		public Promise Then(Func<object, object> callback)
		{
			Deferred.Then(callback);
			return this;
		}

		public Promise Then(Action<object> callback)
		{
			Deferred.Then(x =>
			              {
				              callback(x);
				              return x;
			              });
			return this;
		}

		public Promise<TResult> Then<TResult>(Func<object, TResult> callback)
		{
			var promise = new Promise<TResult>(Deferred);

			Func<object, object> internalCallback = o => callback(o);
			Then(internalCallback);

			return promise;
		}

		public Promise<TResult> Then<TResult>(Func<object, Promise<TResult>> callback)
		{
			var promise = new Promise<TResult>(Deferred);

			Func<object, object> internalCallback = o => callback(o);
			Then(internalCallback);
			return promise;
		}

		public Promise Then(Func<object, Promise> callback)
		{
			Func<object, object> internalCallback = o => callback(o);
			return Then(internalCallback);
		}

		#endregion

		public Promise Then(Action callback)
		{
			Deferred.Then(callback);
			return this;
		}
	}

	public class Promise<T> : Promise
	{
		public Promise(Deferred deferred) : base(deferred)
		{
		}

		public void Done(Action<T> callback)
		{
			base.Done(o => callback((T)o));
		}

		public new Promise<T> Fail(Action callback)
		{
			Deferred.Fail(callback);
			return this;
		}

		public new Promise<T> Fail(Action<object> callback)
		{
			Deferred.Fail(callback);
			return this;
		}

		public new Promise<T> Fail<TFail>(Action<TFail> callback)
		{
			base.Fail(callback);
			return this;
		}

		public new Promise<T> Finally(Action callback)
		{
			base.Finally(callback);
			return this;
		}

		public new Promise<T> Finally(Action<object> callback)
		{
			base.Finally(callback);
			return this;
		}

		public Promise<T> Progress(Action<T> callback)
		{
			base.Progress(o => callback((T)o));
			return this;
		}

		public Promise<T> Progress<TInput>(Action<TInput> callback)
		{
			base.Progress(o => callback((TInput)o));
			return this;
		}
		
		public Promise<T> Then(Func<T, T> callback)
		{
			Func<object, object> internalCallback = o => callback((T)o);
			base.Then(internalCallback);
			return this;
		}

		public Promise<T> Then(Action<T> callback)
		{
			Action<object> internalCallback = x => callback((T)x);
			base.Then(internalCallback);

			return this;
		}

		public Promise<TResult> Then<TResult>(Func<T, TResult> callback)
		{
			var promise = new Promise<TResult>(Deferred);

			Func<object, object> internalCallback = o => callback((T)o);
			base.Then(internalCallback);
			return promise;
		}

		public Promise<TResult> Then<TResult>(Func<T, Promise<TResult>> callback)
		{
			var promise = new Promise<TResult>(Deferred);

			Func<object, object> internalCallback = o => callback((T) o);
			base.Then(internalCallback);
			return promise;
		}

		public Promise Then(Func<T, Promise> callback)
		{
			var promise = new Promise(Deferred);

			Func<object, object> internalCallback = o => callback((T) o);
			base.Then(internalCallback);
			return promise;
		}

	}
}