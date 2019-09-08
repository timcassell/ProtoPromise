using System;
using System.Collections.Generic;

namespace Proto.Promises
{
	public partial class Promise
	{
		// TODO
		//public static Promise<T[]> All<T>(params Promise<T>[] promises)
		//{
		//}

		//public static Promise<T[]> All<T>(IEnumerable<Promise<T>> promises)
		//{
		//}

		public static Promise All(params Promise[] promises)
		{
            return Internal.AllPromise.GetOrCreate(new ArrayEnumerator<Promise>(promises), 1);
		}

		public static Promise All(IEnumerable<Promise> promises)
		{
            return Internal.AllPromise.GetOrCreate(promises.GetEnumerator(), 1);
		}


		// Acts like a compiler-generated closure class, except this can be re-used.
//		private class PromiseClosureWithNonvalue<T1> : ILinked<PromiseClosureWithNonvalue<T1>>
//		{
//			PromiseClosureWithNonvalue<T1> ILinked<PromiseClosureWithNonvalue<T1>>.Next { get; set; }

//#pragma warning disable RECS0108 // Warns about static fields in generic types
//			private static ValueLinkedStack<PromiseClosureWithNonvalue<T1>> _pool;
//#pragma warning restore RECS0108 // Warns about static fields in generic types

//#pragma warning disable RECS0146 // Member hides static member from outer class
//			internal static PromiseClosureWithNonvalue<T1> New()
//#pragma warning restore RECS0146 // Member hides static member from outer class
		//	{
		//		return _pool.IsNotEmpty ? _pool.Pop() : new PromiseClosureWithNonvalue<T1>();
		//	}

		//	static PromiseClosureWithNonvalue()
		//	{
		//		Internal.OnClearPool += () => _pool.Clear();
		//	}

		//	protected PromiseClosureWithNonvalue() { }

		//	// non-value promise is the last element.
		//	public readonly Promise[] promises = new Promise[2];
		//	int waiting = 2;
		//	public Promise<T1>.Deferred deferred;
		//	T1 value;

		//	private void AddToPool()
		//	{
		//		waiting = 2;
		//		deferred = null;
		//		for (int i = 0; i < waiting; ++i)
		//		{
		//			promises[i] = null;
		//		}
  //              _pool.Push(this);
		//	}

		//	private void ResolveComplete()
		//	{
		//		if (--waiting == 0)
		//		{
		//			var temp = deferred;
		//			AddToPool();
		//			if (temp != null)
		//			{
		//				temp.Resolve(value);
		//			}
		//		}
		//	}

		//	void RejectComplete(int index)
		//	{
		//		var temp = deferred;
		//		var rejectValue = promises[index]._rejectedOrCanceledValue;
		//		if (--waiting == 0)
		//		{
		//			AddToPool();
		//		}
		//		if (temp != null)
		//		{
		//			temp.Promise.RejectDirect(rejectValue);
		//			temp.Dispose();
		//		}
		//	}

		//	public void ResolveClosure()
		//	{
		//		ResolveComplete();
		//	}

		//	public void ResolveClosure(T1 arg)
		//	{
		//		value = arg;
		//		ResolveComplete();
		//	}

		//	public void RejectClosure0()
		//	{
		//		RejectComplete(0);
		//	}

		//	public void RejectClosure1()
		//	{
		//		RejectComplete(1);
		//	}
		//}

		//public static Promise<T1> All<T1>(Promise<T1> promise1, Promise promise2)
		//{
		//	PromiseClosureWithNonvalue<T1> allClosure = PromiseClosureWithNonvalue<T1>.New();

		//	allClosure.deferred = GetDeferred<T1>();
		//	var promise = allClosure.deferred.Promise; // Cache the promise in case they all resolve synchronously.
		//	allClosure.promises[0] = promise1;
		//	allClosure.promises[1] = promise2;

		//	promise1.Then(allClosure.ResolveClosure, allClosure.RejectClosure0);
		//	promise2.Then(allClosure.ResolveClosure, allClosure.RejectClosure1);

		//	return promise;
		//}

        // TODO
		//public static Promise<T> Race<T>(params Promise<T>[] promises)
		//{
		//	if (promises.Length == 0)
		//	{
		//		Logger.LogWarning("Promise.Race - Race started with an empty collection. Returned promise will never resolve!");
		//	}

		//	var masterDeferred = GetDeferred<T>();

		//	for (int i = 0, max = promises.Length; i < max; ++i)
		//	{
		//		RaceClosure<T> raceClosure = RaceClosure<T>.New();

		//		raceClosure.deferred = masterDeferred;
		//		raceClosure.promise = promises[i];

		//		raceClosure.promise
		//			.Then(raceClosure.ResolveClosure, raceClosure.RejectClosure)
		//			.Done();
		//	}

		//	return masterDeferred.Promise;
		//}

		//public static Promise<T> Race<T>(IEnumerable<Promise<T>> promises)
		//{
		//	return Race(System.Linq.Enumerable.ToArray(promises));
		//}

		//public static Promise Race(params Promise[] promises)
		//{
		//	if (promises.Length == 0)
		//	{
		//		Logger.LogWarning("Promise.Race - Race started with an empty collection. Returned promise will never resolve!");
		//	}

		//	var masterDeferred = GetDeferred();

		//	for (int i = 0, max = promises.Length; i < max; ++i)
		//	{
		//		RaceClosure raceClosure = RaceClosure.New();

		//		raceClosure.deferred = masterDeferred;
		//		raceClosure.promise = promises[i];

		//		raceClosure.promise
		//	        .Then(raceClosure.ResolveClosure, raceClosure.RejectClosure)
		//			.Done();
		//	}

		//	return masterDeferred.Promise;
		//}

		//public static Promise Race(IEnumerable<Promise> promises)
		//{
		//	return Race(System.Linq.Enumerable.ToArray(promises));
		//}

		public static Promise Sequence(params Func<Promise>[] funcs)
		{
			if (funcs.Length == 0)
			{
				return Resolved();
			}
			Promise promise = funcs[0].Invoke();
			for (int i = 1, max = funcs.Length; i < max; ++i)
			{
				promise = promise.Then(funcs[i]);
			}
			return promise;
		}

		public static Promise Sequence(IEnumerable<Func<Promise>> funcs)
		{
			return Sequence(System.Linq.Enumerable.ToArray(funcs));
		}


        // TODO: clean this up.
		// Acts like a compiler-generated closure class, except this can be re-used.
		private class CancelClosure : ILinked<CancelClosure>
		{
			CancelClosure ILinked<CancelClosure>.Next { get; set; }

			public Action<bool> cancel;

			public void Invoke()
			{
				var c = cancel;
				cancel = null;
				AddToPool();
				c.Invoke(false);
			}

			public void AddToPool()
			{
				cancel = null;
				cancelClosures.Push(this);
			}
		}

		private static ValueLinkedStack<CancelClosure> cancelClosures;

		/// <summary>
		/// Returns a promise that resolves with the <paramref name="yieldInstruction"/> after the <paramref name="yieldInstruction"/> has completed.
		/// If <paramref name="yieldInstruction"/> is not a Unity supported <see cref="UnityEngine.YieldInstruction"/> or <see cref="UnityEngine.CustomYieldInstruction"/>, then the returned promise will resolve after 1 frame.
		/// If you are using Unity 5.3 or later and <paramref name="yieldInstruction"/> is an <see cref="System.Collections.IEnumerator"/>, it will be started and yielded as a Coroutine by Unity. Earlier versions will simply wait 1 frame.
		/// </summary>
		/// <param name="yieldInstruction">Yield instruction.</param>
		/// <typeparam name="TYieldInstruction">The type of yieldInstruction.</typeparam>
		public static Promise<TYieldInstruction> Yield<TYieldInstruction>(TYieldInstruction yieldInstruction)
		{
			var promise = Internal.LitePromise<TYieldInstruction>.GetOrCreate(1);

			CancelClosure cancelClosure = cancelClosures.IsEmpty ? new CancelClosure() : cancelClosures.Pop();
			cancelClosure.cancel = GlobalMonoBehaviour.Yield(yieldInstruction, (Action<TYieldInstruction>) promise.Resolve);
			//promise.Canceled(cancelClosure.Invoke);
			promise.Complete(cancelClosure.AddToPool);

			return promise;
		}

		/// <summary>
		/// Returns a promise that resolves after 1 frame.
		/// </summary>
		public static Promise Yield()
		{
			var promise = Internal.LitePromise.GetOrCreate(1);
			GlobalMonoBehaviour.Yield(promise.ResolveInternal);
			return promise;
		}

		public static Promise New(Action<Deferred> resolver)
        {
            var promise = Internal.DeferredPromise.GetOrCreate(1);
            try
            {
                resolver.Invoke(promise.Deferred);
            }
            catch (Exception e)
            {
                promise.Deferred.Reject(e);
            }
            return promise;
		}

		public static Promise<T> New<T>(Action<Promise<T>.Deferred> resolver)
        {
            var promise = Internal.DeferredPromise<T>.GetOrCreate(1);
            try
            {
                resolver.Invoke(promise.Deferred);
            }
            catch (Exception e)
            {
                promise.Deferred.Reject(e);
            }
            return promise;
        }

		public static Promise Resolved()
		{
			var promise = Internal.LitePromise.GetOrCreate(1);
			promise.Resolve();
			return promise;
		}

		public static Promise<T> Resolved<T>(T value)
		{
			var promise = Internal.LitePromise<T>.GetOrCreate(1);
			promise.Resolve(value);
			return promise;
		}

		public static Promise<T> Rejected<T, TReject>(TReject reason)
		{
			var promise = Internal.LitePromise<T>.GetOrCreate(1);
			promise.Reject(reason, 1);
			return promise;
		}

		public static Promise Rejected<TReject>(TReject reason)
		{
			var promise = Internal.LitePromise.GetOrCreate(1);
			promise.Reject(reason, 1);
			return promise;
		}

		public static Promise<T> Rejected<T>()
		{
			var promise = Internal.LitePromise<T>.GetOrCreate(1);
			promise.Reject(1);
			return promise;
		}

		public static Promise Rejected()
		{
			var promise = Internal.LitePromise.GetOrCreate(1);
			promise.Reject(1);
			return promise;
		}

		public static Deferred NewDeferred()
		{
            return Internal.DeferredPromise.GetOrCreate(1).Deferred;
		}

		public static Promise<T>.Deferred NewDeferred<T>()
		{
            return Internal.DeferredPromise<T>.GetOrCreate(1).Deferred;
		}
	}
}