using System;
using System.Collections.Generic;
using System.Linq;

namespace ProtoPromise
{
	public partial class Promise
	{
		public static Promise<T[]> All<T>(params Promise<T>[] promises)
		{
			var masterDeferred = Deferred<T[]>();

			int waiting = promises.Length;
			T[] args = new T[waiting];

			for (int i = 0, max = promises.Length; i < max; ++i)
			{
				int index = i;
				Promise<T> promise = promises[index];

				promise
					.Done(x =>
					{
						if (masterDeferred.StateInternal != PromiseState.Pending)
						{
							return;
						}

						args[index] = x;
						if (--waiting == 0)
						{
							masterDeferred.Resolve(args);
						}
					})
					.Catch<Exception>(e =>
					{
						masterDeferred.Reject(e);
					});
			}

			return masterDeferred.Promise;
		}

		public static Promise<T[]> All<T>(IEnumerable<Promise<T>> promises)
		{
			return All(promises.ToArray());
		}

		public static Promise All(params Promise[] promises)
		{
			var masterDeferred = Deferred();

			int waiting = promises.Length;

			for (int i = 0, max = promises.Length; i < max; ++i)
			{
				Promise promise = promises[i];

				promise
					.Done(() =>
					{
						if (masterDeferred.StateInternal != PromiseState.Pending)
						{
							return;
						}

						if (--waiting == 0)
						{
							masterDeferred.Resolve();
						}
					})
					.Catch<Exception>(e =>
					{
						masterDeferred.Reject(e);
					});
			}

			return masterDeferred.Promise;
		}

		public static Promise All(IEnumerable<Promise> promises)
		{
			return All(promises.ToArray());
		}

		public static Promise<T> Race<T>(params Promise<T>[] promises)
		{
			var masterDeferred = Deferred<T>();

			for (int i = 0, max = promises.Length; i < max; ++i)
			{
				Promise<T> promise = promises[i];

				promise
					.Done(x =>
					{
						if (masterDeferred.StateInternal != PromiseState.Pending)
						{
							return;
						}

						masterDeferred.Resolve(x);
					})
					.Catch<Exception>(e =>
					{
						masterDeferred.Reject(e);
					});
			}

			return masterDeferred.Promise;
		}

		public static Promise<T> Race<T>(IEnumerable<Promise<T>> promises)
		{
			return Race(promises.ToArray());
		}

		public static Promise Race(params Promise[] promises)
		{
			var masterDeferred = Deferred();

			for (int i = 0, max = promises.Length; i < max; ++i)
			{
				Promise promise = promises[i];

				promise
					.Done(() =>
					{
						if (masterDeferred.StateInternal != PromiseState.Pending)
						{
							return;
						}

						masterDeferred.Resolve();
					})
					.Catch<Exception>(e =>
					{
						masterDeferred.Reject(e);
					});
			}

			return masterDeferred.Promise;
		}

		public static Promise Race(IEnumerable<Promise> promises)
		{
			return Race(promises.ToArray());
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
			return Sequence(funcs.ToArray());
		}

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
			GlobalMonoBehaviour.Yield(yieldInstruction, () => deferred.Resolve(yieldInstruction));
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

		public static Promise<T> Reject<T, TException>(TException exception) where TException : Exception
		{
			Deferred<T> deferred = Deferred<T>();
			deferred.Reject(exception);
			return deferred.Promise;
		}

		public static Promise Reject<TException>(TException exception) where TException : Exception
		{
			Deferred deferred = Deferred();
			deferred.Reject(exception);
			return deferred.Promise;
		}

		public static Deferred Deferred()
		{
			return new Deferred();
		}

		public static Deferred<T> Deferred<T>()
		{
			return new Deferred<T>();
		}
	}
}