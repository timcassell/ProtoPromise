using System;
using System.Collections.Generic;

namespace ProtoPromise
{
	public partial class Promise
	{
		public static bool AutoDone = true;


		// Acts like a compiler-generated closure class, except this can be re-used.
		private class PromiseClosure : ILinked<PromiseClosure>
		{
			PromiseClosure ILinked<PromiseClosure>.Next { get; set; }

			public Promise promise;
			public int index;
			public AllClosure allClosure;

			private void AddToPool()
			{
				promise = null;
				objectPoolInternal.AddInternal(this);
			}

			private void AddAllToPool()
			{
				AddToPool();
				allClosure.masterDeferred = null;
				objectPoolInternal.AddInternal(allClosure);
			}

			public void ResolveClosure()
			{
				var deferred = allClosure.masterDeferred;
				if (deferred != null)
				{
					if (--allClosure.waiting == 0)
					{
						AddAllToPool();
						deferred.Resolve();
						return;
					}
				}
				AddToPool();
			}

			public void RejectClosure()
			{
				var deferred = allClosure.masterDeferred;
				if (deferred != null)
				{
					var p = promise;
					AddAllToPool();
					deferred.RejectInternal(p.rejectedOrCanceledValueInternal);
					return;
				}
				AddToPool();
			}
		}

		// Acts like a compiler-generated closure class, except this can be re-used.
		private class AllClosure : ILinked<AllClosure>
		{
			AllClosure ILinked<AllClosure>.Next { get; set; }

			public Deferred masterDeferred;
			public int waiting;
		}

		// Acts like a compiler-generated closure class, except this can be re-used.
		private class PromiseClosure<T> : ILinked<PromiseClosure<T>>
		{
			PromiseClosure<T> ILinked<PromiseClosure<T>>.Next { get; set; }

			public Promise<T> promise;
			public int index;
			public AllClosure<T> allClosure;

			private void AddToPool()
			{
				promise = null;
				objectPoolInternal.AddInternal(this);
			}

			private void AddAllToPool()
			{
				AddToPool();
				allClosure.args = null;
				allClosure.masterDeferred = null;
				objectPoolInternal.AddInternal(allClosure);
			}

			public void ResolveClosure(T arg)
			{
				var deferred = allClosure.masterDeferred;
				if (deferred != null)
				{
					var args = allClosure.args;
					args[index] = arg;
					if (--allClosure.waiting == 0)
					{
						AddAllToPool();
						deferred.Resolve(args);
						return;
					}
				}
				AddToPool();
			}

			public void RejectClosure()
			{
				var deferred = allClosure.masterDeferred;
				if (deferred != null)
				{
					var p = promise;
					AddAllToPool();
					deferred.RejectInternal(p.rejectedOrCanceledValueInternal);
					return;
				}
				AddToPool();
			}
		}

		// Acts like a compiler-generated closure class, except this can be re-used.
		private class AllClosure<T> : ILinked<AllClosure<T>>
		{
			AllClosure<T> ILinked<AllClosure<T>>.Next { get; set; }

			public Deferred<T[]> masterDeferred;
			public int waiting;
			public T[] args;
		}


		public static Promise<T[]> All<T>(params Promise<T>[] promises)
		{
			if (promises.Length == 0)
			{
				return Resolve(new T[0]);
			}

			AllClosure<T> allClosure;
			if (!objectPoolInternal.TryTakeInternal(out allClosure))
			{
				allClosure = new AllClosure<T>();
			}

			int waiting = promises.Length;

			allClosure.masterDeferred = GetDeferred<T[]>();
			var promise = allClosure.masterDeferred.Promise; // Cache the promise in case they all resolve instantly.
			allClosure.waiting = waiting;
			allClosure.args = new T[waiting];

			for (int i = 0; i < waiting; ++i)
			{
				PromiseClosure<T> promiseClosure;
				if (!objectPoolInternal.TryTakeInternal(out promiseClosure))
				{
					promiseClosure = new PromiseClosure<T>();
				}

				promiseClosure.allClosure = allClosure;
				promiseClosure.index = i;
				promiseClosure.promise = promises[i];

				promiseClosure.promise
					.Then(promiseClosure.ResolveClosure, promiseClosure.RejectClosure)
					.Done();
			}

			return promise;
		}

		public static Promise<T[]> All<T>(IEnumerable<Promise<T>> promises)
		{
			return All(System.Linq.Enumerable.ToArray(promises));
		}

		public static Promise All(params Promise[] promises)
		{
			if (promises.Length == 0)
			{
				return Resolve();
			}

			AllClosure allClosure;
			if (!objectPoolInternal.TryTakeInternal(out allClosure))
			{
				allClosure = new AllClosure();
			}

			allClosure.masterDeferred = GetDeferred();
			var promise = allClosure.masterDeferred.Promise; // Cache the promise in case they all resolve instantly.
			allClosure.waiting = promises.Length;

			for (int i = 0, max = promises.Length; i < max; ++i)
			{
				PromiseClosure promiseClosure;
				if (!objectPoolInternal.TryTakeInternal(out promiseClosure))
				{
					promiseClosure = new PromiseClosure();
				}

				promiseClosure.allClosure = allClosure;
				promiseClosure.index = i;
				promiseClosure.promise = promises[i];

				promiseClosure.promise
		            .Then(promiseClosure.ResolveClosure, promiseClosure.RejectClosure)
					.Done();
			}

			return promise;
		}

		public static Promise All(IEnumerable<Promise> promises)
		{
			return All(System.Linq.Enumerable.ToArray(promises));
		}


		// Acts like a compiler-generated closure class, except this can be re-used.
		private class PromiseClosureWithNonvalue<T1> : ILinked<PromiseClosureWithNonvalue<T1>>
		{
			PromiseClosureWithNonvalue<T1> ILinked<PromiseClosureWithNonvalue<T1>>.Next { get; set; }

			// non-value promise is the last element.
			public readonly Promise[] promises = new Promise[2];
			int waiting = 2;
			public Deferred<T1> deferred;
			T1 value;

			private void AddToPool()
			{
				waiting = 2;
				deferred = null;
				for (int i = 0; i < waiting; ++i)
				{
					promises[i] = null;
				}
				objectPoolInternal.AddInternal(this);
			}

			private void ResolveComplete()
			{
				if (deferred == null)
				{
					return;
				}
				var temp = deferred;
				AddToPool();
				temp.Resolve(value);
			}

			void RejectComplete(int index)
			{
				if (deferred == null)
				{
					return;
				}
				var temp = deferred;
				var rejectValue = promises[index].rejectedOrCanceledValueInternal;
				AddToPool();
				temp.RejectInternal(rejectValue);
			}

			public void ResolveClosure()
			{
				if (--waiting == 0)
				{
					ResolveComplete();
				}
			}

			public void ResolveClosure(T1 arg)
			{
				value = arg;
				ResolveClosure();
			}

			public void RejectClosure0()
			{
				RejectComplete(0);
			}

			public void RejectClosure1()
			{
				RejectComplete(1);
			}
		}

		public static Promise<T1> All<T1>(Promise<T1> promise1, Promise promise2)
		{
			PromiseClosureWithNonvalue<T1> allClosure;
			if (!objectPoolInternal.TryTakeInternal(out allClosure))
			{
				allClosure = new PromiseClosureWithNonvalue<T1>();
			}

			allClosure.deferred = GetDeferred<T1>();
			allClosure.promises[0] = promise1;
			allClosure.promises[1] = promise2;

			promise1.Then(allClosure.ResolveClosure, allClosure.RejectClosure0);
			promise2.Then(allClosure.ResolveClosure, allClosure.RejectClosure1);

			return allClosure.deferred.Promise;
		}



		// Acts like a compiler-generated closure class, except this can be re-used.
		private class RaceClosure : ILinked<RaceClosure>
		{
			RaceClosure ILinked<RaceClosure>.Next { get; set; }

			public Deferred deferred;
			public Promise promise;

			public void ResolveClosure()
			{
				var def = deferred;
				deferred = null;
				promise = null;
				objectPoolInternal.AddInternal(this);
				if (def.State == PromiseState.Pending)
				{
					def.Resolve();
				}
			}

			public void RejectClosure()
			{
				var def = deferred;
				deferred = null;
				var p = promise;
				promise = null;
				objectPoolInternal.AddInternal(this);
				if (def.State == PromiseState.Pending)
				{
					def.RejectInternal(p.rejectedOrCanceledValueInternal);
				}
			}
		}

		// Acts like a compiler-generated closure class, except this can be re-used.
		private class RaceClosure<T> : ILinked<RaceClosure<T>>
		{
			RaceClosure<T> ILinked<RaceClosure<T>>.Next { get; set; }

			public Deferred<T> deferred;
			public Promise<T> promise;

			public void ResolveClosure(T arg)
			{
				var def = deferred;
				deferred = null;
				promise = null;
				objectPoolInternal.AddInternal(this);
				if (def.State == PromiseState.Pending)
				{
					def.Resolve(arg);
				}
			}

			public void RejectClosure()
			{
				var def = deferred;
				deferred = null;
				var p = promise;
				promise = null;
				objectPoolInternal.AddInternal(this);
				if (def.State == PromiseState.Pending)
				{
					def.RejectInternal(p.rejectedOrCanceledValueInternal);
				}
			}
		}

		public static Promise<T> Race<T>(params Promise<T>[] promises)
		{
			if (promises.Length == 0)
			{
				Logger.LogWarning("Promise.Race - Race started with an empty collection. Returned promise will never resolve!");
			}

			var masterDeferred = GetDeferred<T>();

			for (int i = 0, max = promises.Length; i < max; ++i)
			{
				RaceClosure<T> raceClosure;
				if (!objectPoolInternal.TryTakeInternal(out raceClosure))
				{
					raceClosure = new RaceClosure<T>();
				}

				raceClosure.deferred = masterDeferred;
				raceClosure.promise = promises[i];

				raceClosure.promise
					.Then(raceClosure.ResolveClosure, raceClosure.RejectClosure)
					.Done();
			}

			return masterDeferred.Promise;
		}

		public static Promise<T> Race<T>(IEnumerable<Promise<T>> promises)
		{
			return Race(System.Linq.Enumerable.ToArray(promises));
		}

		public static Promise Race(params Promise[] promises)
		{
			if (promises.Length == 0)
			{
				Logger.LogWarning("Promise.Race - Race started with an empty collection. Returned promise will never resolve!");
			}

			var masterDeferred = GetDeferred();

			for (int i = 0, max = promises.Length; i < max; ++i)
			{
				RaceClosure raceClosure;
				if (!objectPoolInternal.TryTakeInternal(out raceClosure))
				{
					raceClosure = new RaceClosure();
				}

				raceClosure.deferred = masterDeferred;
				raceClosure.promise = promises[i];

				raceClosure.promise
			        .Then(raceClosure.ResolveClosure, raceClosure.RejectClosure)
					.Done();
			}

			return masterDeferred.Promise;
		}

		public static Promise Race(IEnumerable<Promise> promises)
		{
			return Race(System.Linq.Enumerable.ToArray(promises));
		}

		public static Promise Sequence(params Func<Promise>[] funcs)
		{
			if (funcs.Length == 0)
			{
				return Resolve();
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
			Deferred<TYieldInstruction> deferred = GetDeferred<TYieldInstruction>();

			CancelClosure cancelClosure = cancelClosures.IsEmpty ? new CancelClosure() : cancelClosures.Pop();
			cancelClosure.cancel = GlobalMonoBehaviour.Yield(yieldInstruction, deferred.Resolve);
			deferred.Promise.Canceled(cancelClosure.Invoke);
			deferred.Promise.Complete(cancelClosure.AddToPool);

			return deferred.Promise;
		}

		/// <summary>
		/// Returns a promise that resolves after 1 frame.
		/// </summary>
		public static Promise Yield()
		{
			Deferred deferred = GetDeferred();
			GlobalMonoBehaviour.Yield(deferred.Resolve);
			return deferred.Promise;
		}

		public static Promise New(Action<Deferred> resolver)
		{
			Deferred deferred = GetDeferred();
			resolver.Invoke(deferred);
			return deferred.Promise;
		}

		public static Promise<T> New<T>(Action<Deferred<T>> resolver)
		{
			Deferred<T> deferred = GetDeferred<T>();
			resolver.Invoke(deferred);
			return deferred.Promise;
		}

		public static Promise Resolve()
		{
			Deferred deferred = GetDeferred();
			deferred.Resolve();
			return deferred.Promise;
		}

		public static Promise<T> Resolve<T>(T value)
		{
			Deferred<T> deferred = GetDeferred<T>();
			deferred.Resolve(value);
			return deferred.Promise;
		}

		public static Promise<T> Reject<T, TReject>(TReject exception)
		{
			Deferred<T> deferred = GetDeferred<T>();
			deferred.RejectInternal(exception);
			return deferred.Promise;
		}

		public static Promise Reject<TReject>(TReject exception)
		{
			Deferred deferred = GetDeferred();
			deferred.RejectInternal(exception);
			return deferred.Promise;
		}

		public static Promise<T> Reject<T>()
		{
			Deferred<T> deferred = GetDeferred<T>();
			deferred.RejectInternal();
			return deferred.Promise;
		}

		public static Promise Reject()
		{
			Deferred deferred = GetDeferred();
			deferred.RejectInternal();
			return deferred.Promise;
		}

		public static Deferred Deferred()
		{
			return GetDeferred();
		}

		public static Deferred<T> Deferred<T>()
		{
			return GetDeferred<T>();
		}

		private static Deferred GetDeferred()
		{
			Promise promise;
			if (!objectPoolInternal.TryTakeInternal(out promise))
			{
				promise = new Promise();
				new Deferred(promise);
			}
			promise.ResetInternal(4);
			return (Deferred) promise.deferredInternal;
		}

		public static Deferred<T> GetDeferred<T>()
		{
			Promise<T> promise;
			if (!objectPoolInternal.TryTakeInternal(out promise))
			{
				promise = new Promise<T>();
				new Deferred<T>(promise);
			}
			promise.ResetInternal(4);
			return (Deferred<T>) promise.deferredInternal;
		}
	}
}