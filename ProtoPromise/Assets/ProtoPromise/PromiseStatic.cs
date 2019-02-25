using System;
using System.Collections.Generic;

namespace ProtoPromise
{
	public partial class Promise
	{
		public static bool AutoDone = false;


		// Acts like a compiler-generated closure class, except this can be re-used.
		private class PromiseClosure : ILinked<PromiseClosure>
		{
			PromiseClosure ILinked<PromiseClosure>.Next { get; set; }

			public Promise promise;
			public int index;
			public AllClosure allClosure;

			private Deferred AddToPoolAndGetDeferred()
			{
				var deferred = allClosure.masterDeferred;
				allClosure.masterDeferred = null;
				objectPool.AddInternal(allClosure);
				promise = null;
				objectPool.AddInternal(this);
				return deferred;
			}

			public void ResolveClosure()
			{
				if (allClosure.masterDeferred.State == PromiseState.Pending && --allClosure.waiting == 0)
				{
					AddToPoolAndGetDeferred().Resolve();
					return;
				}
				AddToPoolAndGetDeferred();
			}

			public void RejectClosure()
			{
				if (allClosure.masterDeferred.State == PromiseState.Pending)
				{
					var p = promise;
					AddToPoolAndGetDeferred().RejectInternal(p.rejectedValueInternal);
					return;
				}
				AddToPoolAndGetDeferred();
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

			private Deferred<T[]> AddToPoolAndGetDeferred()
			{
				var deferred = allClosure.masterDeferred;
				allClosure.args = null;
				allClosure.masterDeferred = null;
				objectPool.AddInternal(allClosure);
				promise = null;
				objectPool.AddInternal(this);
				return deferred;
			}

			public void ResolveClosure(T arg)
			{
				if (allClosure.masterDeferred.State == PromiseState.Pending)
				{
					var args = allClosure.args;
					args[index] = arg;
					if (--allClosure.waiting == 0)
					{
						AddToPoolAndGetDeferred().Resolve(args);
						return;
					}
				}
				AddToPoolAndGetDeferred();
			}

			public void RejectClosure()
			{
				if (allClosure.masterDeferred.State == PromiseState.Pending)
				{
					var p = promise;
					AddToPoolAndGetDeferred().RejectInternal(p.rejectedValueInternal);
					return;
				}
				AddToPoolAndGetDeferred();
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
				throw new ArgumentException("promises.Length must be greater than zero");
				//return Reject<T[], ArgumentException>(new ArgumentException("promises.Length must be greater than zero"));
			}

			AllClosure<T> allClosure;
			if (!objectPool.TryTakeInternal(out allClosure))
			{
				allClosure = new AllClosure<T>();
			}

			int waiting = promises.Length;

			allClosure.masterDeferred = Deferred<T[]>();
			allClosure.waiting = waiting;
			allClosure.args = new T[waiting];

			for (int i = 0; i < waiting; ++i)
			{
				PromiseClosure<T> promiseClosure;
				if (!objectPool.TryTakeInternal(out promiseClosure))
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

			return allClosure.masterDeferred.Promise;
		}

		public static Promise<T[]> All<T>(IEnumerable<Promise<T>> promises)
		{
			return All(System.Linq.Enumerable.ToArray(promises));
		}

		public static Promise All(params Promise[] promises)
		{
			if (promises.Length == 0)
			{
				throw new ArgumentException("promises.Length must be greater than zero");
				//return Reject(new ArgumentException("promises.Length must be greater than zero"));
			}

			AllClosure allClosure;
			if (!objectPool.TryTakeInternal(out allClosure))
			{
				allClosure = new AllClosure();
			}

			allClosure.masterDeferred = Deferred();
			allClosure.waiting = promises.Length;

			for (int i = 0, max = promises.Length; i < max; ++i)
			{
				PromiseClosure promiseClosure;
				if (!objectPool.TryTakeInternal(out promiseClosure))
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

			return allClosure.masterDeferred.Promise;
		}

		public static Promise All(IEnumerable<Promise> promises)
		{
			return All(System.Linq.Enumerable.ToArray(promises));
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
				objectPool.AddInternal(this);
				deferred.Resolve();
			}

			public void RejectClosure()
			{
				var def = deferred;
				deferred = null;
				var p = promise;
				promise = null;
				objectPool.AddInternal(this);
				deferred.RejectInternal(p.rejectedValueInternal);
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
				objectPool.AddInternal(this);
				if (deferred.State == PromiseState.Pending)
				{
					deferred.Resolve(arg);
				}
			}

			public void RejectClosure()
			{
				var def = deferred;
				deferred = null;
				var p = promise;
				promise = null;
				objectPool.AddInternal(this);
				if (deferred.State == PromiseState.Pending)
				{
					deferred.RejectInternal(p.rejectedValueInternal);
				}
			}
		}

		public static Promise<T> Race<T>(params Promise<T>[] promises)
		{
			if (promises.Length == 0)
			{
				throw new ArgumentException("promises.Length must be greater than zero");
				//return Reject<T, ArgumentException>(new ArgumentException("promises.Length must be greater than zero"));
			}

			var masterDeferred = Deferred<T>();

			for (int i = 0, max = promises.Length; i < max; ++i)
			{
				RaceClosure<T> raceClosure;
				if (!objectPool.TryTakeInternal(out raceClosure))
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
				throw new ArgumentException("promises.Length must be greater than zero");
				//return Reject(new ArgumentException("promises.Length must be greater than zero"));
			}

			var masterDeferred = Deferred();

			for (int i = 0, max = promises.Length; i < max; ++i)
			{
				RaceClosure raceClosure;
				if (!objectPool.TryTakeInternal(out raceClosure))
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
				throw new ArgumentException("funcs.Length must be greater than zero");
				//return Reject(new ArgumentException("funcs.Length must be greater than zero"));
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
				cancel.Invoke(false);
				AddToPool();
			}

			public void AddToPool()
			{
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
			Deferred<TYieldInstruction> deferred = Deferred<TYieldInstruction>();

			CancelClosure cancelClosure = cancelClosures.IsEmpty ? new CancelClosure() : cancelClosures.Pop();
			cancelClosure.cancel = GlobalMonoBehaviour.Yield(yieldInstruction, deferred.Resolve);
			deferred.Promise.OnCanceled(cancelClosure.Invoke);
			deferred.Promise.Finally(cancelClosure.AddToPool);

			return deferred.Promise;
		}

		/// <summary>
		/// Returns a promise that resolves after 1 frame.
		/// </summary>
		public static Promise Yield()
		{
			Deferred deferred = Deferred();
			GlobalMonoBehaviour.Yield(deferred.Resolve);
			return deferred.Promise;
		}

		public static Promise New(Action<Deferred> resolver)
		{
			Deferred deferred = Deferred();
			resolver.Invoke(deferred);
			return deferred.Promise;
		}

		public static Promise<T> New<T>(Action<Deferred<T>> resolver)
		{
			Deferred<T> deferred = Deferred<T>();
			resolver.Invoke(deferred);
			return deferred.Promise;
		}

		public static Promise Resolve()
		{
			Deferred deferred = Deferred();
			deferred.Resolve();
			return deferred.Promise;
		}

		public static Promise<T> Resolve<T>(T value)
		{
			Deferred<T> deferred = Deferred<T>();
			deferred.Resolve(value);
			return deferred.Promise;
		}

		public static Promise<T> Reject<T, TException>(TException exception)
		{
			Deferred<T> deferred = Deferred<T>();
			deferred.RejectInternal(exception, 1);
			return deferred.Promise;
		}

		public static Promise Reject<TException>(TException exception)
		{
			Deferred deferred = Deferred();
			deferred.RejectInternal(exception, 1);
			return deferred.Promise;
		}

		public static Deferred Deferred()
		{
			Promise promise;
			if (!objectPool.TryTakeInternal(out promise))
			{
				promise = new Promise();
				new Deferred(promise);
			}
			promise.ResetInternal();
			return (Deferred) promise.DeferredInternal;
		}

		public static Deferred<T> Deferred<T>()
		{
			Promise<T> promise;
			if (!objectPool.TryTakeInternal(out promise))
			{
				promise = new Promise<T>();
				new Deferred<T>(promise);
			}
			promise.ResetInternal();
			return (Deferred<T>) promise.DeferredInternal;
		}
	}
}