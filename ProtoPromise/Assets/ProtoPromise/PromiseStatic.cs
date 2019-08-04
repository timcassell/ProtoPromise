using System;
using System.Collections.Generic;

namespace ProtoPromise
{
	public partial class Promise
	{
		// Acts like a compiler-generated closure class, except this can be re-used.
		private class PromiseClosure : ILinked<PromiseClosure>
		{
			PromiseClosure ILinked<PromiseClosure>.Next { get; set; }

			private static ValueLinkedStack<PromiseClosure> pool;

#pragma warning disable RECS0146 // Member hides static member from outer class
			internal static PromiseClosure New()
#pragma warning restore RECS0146 // Member hides static member from outer class
			{
				return pool.IsNotEmpty ? pool.Pop() : new PromiseClosure();
			}

			static PromiseClosure()
			{
				Internal.OnClearPool += () => pool.Clear();
			}

			protected PromiseClosure() { }

			public Promise promise;
			public AllClosure allClosure;

			private void AddToPool()
			{
				promise = null;
				pool.Push(this);
			}

			public void ResolveClosure()
			{
				var deferred = allClosure.masterDeferred;
				if (--allClosure.waiting == 0)
				{
					allClosure.AddToPool();
					if (deferred != null)
					{
						deferred.Resolve();
					}
				}
				AddToPool();
			}

			public void RejectClosure()
			{
				var deferred = allClosure.masterDeferred;
				if (--allClosure.waiting == 0)
				{
					allClosure.AddToPool();
				}
				if (deferred != null)
				{
					deferred.Promise.RejectDirect(promise._rejectedOrCanceledValue);
					deferred.Dispose();
				}
				AddToPool();
			}
		}

		// Acts like a compiler-generated closure class, except this can be re-used.
		private class AllClosure : ILinked<AllClosure>
		{
			AllClosure ILinked<AllClosure>.Next { get; set; }

#pragma warning disable RECS0108 // Warns about static fields in generic types
			private static ValueLinkedStack<AllClosure> pool;
#pragma warning restore RECS0108 // Warns about static fields in generic types

#pragma warning disable RECS0146 // Member hides static member from outer class
			internal static AllClosure New()
#pragma warning restore RECS0146 // Member hides static member from outer class
			{
				return pool.IsNotEmpty ? pool.Pop() : new AllClosure();
			}

			static AllClosure()
			{
				Internal.OnClearPool += () => pool.Clear();
			}

			protected AllClosure() { }

			public Deferred masterDeferred;
			public int waiting;

			public void AddToPool()
			{
				masterDeferred = null;
				pool.Push(this);
			}
		}

		// Acts like a compiler-generated closure class, except this can be re-used.
		private class PromiseClosure<T> : ILinked<PromiseClosure<T>>
		{
			PromiseClosure<T> ILinked<PromiseClosure<T>>.Next { get; set; }

			public Promise<T> promise;
			public int index;
			public AllClosure<T> allClosure;

#pragma warning disable RECS0108 // Warns about static fields in generic types
			private static ValueLinkedStack<PromiseClosure<T>> pool;
#pragma warning restore RECS0108 // Warns about static fields in generic types

#pragma warning disable RECS0146 // Member hides static member from outer class
			internal static PromiseClosure<T> New()
#pragma warning restore RECS0146 // Member hides static member from outer class
			{
				return pool.IsNotEmpty ? pool.Pop() : new PromiseClosure<T>();
			}

			static PromiseClosure()
			{
				Internal.OnClearPool += () => pool.Clear();
			}

			protected PromiseClosure() { }

			private void AddToPool()
			{
				promise = null;
				pool.Push(this);
			}

			public void ResolveClosure(T arg)
			{
				var deferred = allClosure.masterDeferred;
				var args = allClosure.args;
				args[index] = arg;
				if (--allClosure.waiting == 0)
				{
					allClosure.AddToPool();
					if (deferred != null)
					{
						deferred.Resolve(args);
					}
				}
				AddToPool();
			}

			public void RejectClosure()
			{
				var deferred = allClosure.masterDeferred;
				if (--allClosure.waiting == 0)
				{
					allClosure.AddToPool();
				}
				if (deferred != null)
				{
					deferred.Promise.RejectDirect(promise._rejectedOrCanceledValue);
					deferred.Dispose();
				}
				AddToPool();
			}
		}

		// Acts like a compiler-generated closure class, except this can be re-used.
		private class AllClosure<T> : ILinked<AllClosure<T>>
		{
			AllClosure<T> ILinked<AllClosure<T>>.Next { get; set; }

#pragma warning disable RECS0108 // Warns about static fields in generic types
			private static ValueLinkedStack<AllClosure<T>> pool;
#pragma warning restore RECS0108 // Warns about static fields in generic types

#pragma warning disable RECS0146 // Member hides static member from outer class
			internal static AllClosure<T> New()
#pragma warning restore RECS0146 // Member hides static member from outer class
			{
				return pool.IsNotEmpty ? pool.Pop() : new AllClosure<T>();
			}

			static AllClosure()
			{
				Internal.OnClearPool += () => pool.Clear();
			}

			protected AllClosure() { }

			public Promise<T[]>.Deferred masterDeferred;
			public int waiting;
			public T[] args;

			public void AddToPool()
			{
				args = null;
				masterDeferred = null;
				pool.Push(this);
			}
		}

		// TODO: handle canceled promises.
		public static Promise<T[]> All<T>(params Promise<T>[] promises)
		{
			if (promises.Length == 0)
			{
				return Resolved(new T[0]);
			}

			AllClosure<T> allClosure = AllClosure<T>.New();

			int waiting = promises.Length;

			allClosure.masterDeferred = GetDeferred<T[]>();
			var promise = allClosure.masterDeferred.Promise; // Cache the promise in case they all resolve synchronously.
			allClosure.waiting = waiting;
			allClosure.args = new T[waiting];

			for (int i = 0; i < waiting; ++i)
			{
				PromiseClosure<T> promiseClosure = PromiseClosure<T>.New();

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
				return Resolved();
			}

			AllClosure allClosure = AllClosure.New();

			allClosure.masterDeferred = GetDeferred();
			var promise = allClosure.masterDeferred.Promise; // Cache the promise in case they all resolve synchronously.
			allClosure.waiting = promises.Length;

			for (int i = 0, max = promises.Length; i < max; ++i)
			{
				PromiseClosure promiseClosure = PromiseClosure.New();

				promiseClosure.allClosure = allClosure;
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

#pragma warning disable RECS0108 // Warns about static fields in generic types
			private static ValueLinkedStack<PromiseClosureWithNonvalue<T1>> pool;
#pragma warning restore RECS0108 // Warns about static fields in generic types

#pragma warning disable RECS0146 // Member hides static member from outer class
			internal static PromiseClosureWithNonvalue<T1> New()
#pragma warning restore RECS0146 // Member hides static member from outer class
			{
				return pool.IsNotEmpty ? pool.Pop() : new PromiseClosureWithNonvalue<T1>();
			}

			static PromiseClosureWithNonvalue()
			{
				Internal.OnClearPool += () => pool.Clear();
			}

			protected PromiseClosureWithNonvalue() { }

			// non-value promise is the last element.
			public readonly Promise[] promises = new Promise[2];
			int waiting = 2;
			public Promise<T1>.Deferred deferred;
			T1 value;

			private void AddToPool()
			{
				waiting = 2;
				deferred = null;
				for (int i = 0; i < waiting; ++i)
				{
					promises[i] = null;
				}
				pool.Push(this);
			}

			private void ResolveComplete()
			{
				if (--waiting == 0)
				{
					var temp = deferred;
					AddToPool();
					if (temp != null)
					{
						temp.Resolve(value);
					}
				}
			}

			void RejectComplete(int index)
			{
				var temp = deferred;
				var rejectValue = promises[index]._rejectedOrCanceledValue;
				if (--waiting == 0)
				{
					AddToPool();
				}
				if (temp != null)
				{
					temp.Promise.RejectDirect(rejectValue);
					temp.Dispose();
				}
			}

			public void ResolveClosure()
			{
				ResolveComplete();
			}

			public void ResolveClosure(T1 arg)
			{
				value = arg;
				ResolveComplete();
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
			PromiseClosureWithNonvalue<T1> allClosure = PromiseClosureWithNonvalue<T1>.New();

			allClosure.deferred = GetDeferred<T1>();
			var promise = allClosure.deferred.Promise; // Cache the promise in case they all resolve synchronously.
			allClosure.promises[0] = promise1;
			allClosure.promises[1] = promise2;

			promise1.Then(allClosure.ResolveClosure, allClosure.RejectClosure0);
			promise2.Then(allClosure.ResolveClosure, allClosure.RejectClosure1);

			return promise;
		}



		// Acts like a compiler-generated closure class, except this can be re-used.
		private class RaceClosure : ILinked<RaceClosure>
		{
			RaceClosure ILinked<RaceClosure>.Next { get; set; }

#pragma warning disable RECS0108 // Warns about static fields in generic types
			private static ValueLinkedStack<RaceClosure> pool;
#pragma warning restore RECS0108 // Warns about static fields in generic types

#pragma warning disable RECS0146 // Member hides static member from outer class
			internal static RaceClosure New()
#pragma warning restore RECS0146 // Member hides static member from outer class
			{
				return pool.IsNotEmpty ? pool.Pop() : new RaceClosure();
			}

			static RaceClosure()
			{
				Internal.OnClearPool += () => pool.Clear();
			}

			protected RaceClosure() { }

			public Deferred deferred;
			public Promise promise;

			public void ResolveClosure()
			{
				var def = deferred;
				deferred = null;
				promise = null;
				pool.Push(this);
				if (def.State == DeferredState.Pending)
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
				pool.Push(this);
				if (def.State == DeferredState.Pending)
				{
					def.Promise.RejectDirect(p._rejectedOrCanceledValue);
					def.Dispose();
				}
			}
		}

		// Acts like a compiler-generated closure class, except this can be re-used.
		private class RaceClosure<T> : ILinked<RaceClosure<T>>
		{
			RaceClosure<T> ILinked<RaceClosure<T>>.Next { get; set; }

#pragma warning disable RECS0108 // Warns about static fields in generic types
			private static ValueLinkedStack<RaceClosure<T>> pool;
#pragma warning restore RECS0108 // Warns about static fields in generic types

#pragma warning disable RECS0146 // Member hides static member from outer class
			internal static RaceClosure<T> New()
#pragma warning restore RECS0146 // Member hides static member from outer class
			{
				return pool.IsNotEmpty ? pool.Pop() : new RaceClosure<T>();
			}

			static RaceClosure()
			{
				Internal.OnClearPool += () => pool.Clear();
			}

			protected RaceClosure() { }

			public Promise<T>.Deferred deferred;
			public Promise<T> promise;

			public void ResolveClosure(T arg)
			{
				var def = deferred;
				deferred = null;
				promise = null;
				pool.Push(this);
				if (def.State == DeferredState.Pending)
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
				pool.Push(this);
				if (def.State == DeferredState.Pending)
				{
					def.Promise.RejectDirect(p._rejectedOrCanceledValue);
					def.Dispose();
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
				RaceClosure<T> raceClosure = RaceClosure<T>.New();

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
				RaceClosure raceClosure = RaceClosure.New();

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
			var promise = Internal.LitePromise<TYieldInstruction>.GetOrCreate();

			CancelClosure cancelClosure = cancelClosures.IsEmpty ? new CancelClosure() : cancelClosures.Pop();
			cancelClosure.cancel = GlobalMonoBehaviour.Yield(yieldInstruction, (Action<TYieldInstruction>) promise.Resolve);
			promise.Canceled(cancelClosure.Invoke);
			promise.Complete(cancelClosure.AddToPool);

			return promise;
		}

		/// <summary>
		/// Returns a promise that resolves after 1 frame.
		/// </summary>
		public static Promise Yield()
		{
			var promise = Internal.LitePromise.GetOrCreate();
			GlobalMonoBehaviour.Yield(promise.Resolve);
			return promise;
		}

		public static Promise New(Action<Deferred> resolver)
		{
			var deferred = GetDeferred();
			var promise = deferred.Promise;
			resolver.Invoke(deferred);
			return promise;
		}

		public static Promise<T> New<T>(Action<Promise<T>.Deferred> resolver)
		{
			var deferred = GetDeferred<T>();
			var promise = deferred.Promise;
			resolver.Invoke(deferred);
			return promise;
		}

		public static Promise Resolved()
		{
			var promise = Internal.LitePromise.GetOrCreate();
			promise.Resolve();
			return promise;
		}

		public static Promise<T> Resolved<T>(T value)
		{
			var promise = Internal.LitePromise<T>.GetOrCreate();
			promise.Resolve(value);
			return promise;
		}

		public static Promise<T> Rejected<T, TReject>(TReject reason)
		{
			var promise = Internal.LitePromise<T>.GetOrCreate();
			promise.Reject(reason);
			return promise;
		}

		public static Promise Rejected<TReject>(TReject reason)
		{
			var promise = Internal.LitePromise.GetOrCreate();
			promise.Reject(reason);
			return promise;
		}

		public static Promise<T> Rejected<T>()
		{
			var promise = Internal.LitePromise<T>.GetOrCreate();
			promise.Reject();
			return promise;
		}

		public static Promise Rejected()
		{
			var promise = Internal.LitePromise.GetOrCreate();
			promise.Reject();
			return promise;
		}

		public static Promise<T> Canceled<T, TCancel>(TCancel reason)
		{
			var promise = Internal.LitePromise<T>.GetOrCreate();
			promise.Cancel(reason);
			return promise;
		}

		public static Promise Canceled<TCancel>(TCancel reason)
		{
			var promise = Internal.LitePromise.GetOrCreate();
			promise.Cancel(reason);
			return promise;
		}

		public static Promise<T> Canceled<T>()
		{
			var promise = Internal.LitePromise<T>.GetOrCreate();
			promise.Cancel();
			return promise;
		}

		public static Promise Canceled()
		{
			var promise = Internal.LitePromise.GetOrCreate();
			promise.Cancel();
			return promise;
		}

		public static Deferred NewDeferred()
		{
			return GetDeferred();
		}

		public static Promise<T>.Deferred NewDeferred<T>()
		{
			return GetDeferred<T>();
		}

		private static Deferred GetDeferred()
		{
			return Deferred.GetOrCreate(Internal.DeferredPromise.GetOrCreate());
		}

		private static Promise<T>.Deferred GetDeferred<T>()
		{
			return Promise<T>.Deferred.GetOrCreate(Internal.DeferredPromise<T>.GetOrCreate());
		}
	}
}