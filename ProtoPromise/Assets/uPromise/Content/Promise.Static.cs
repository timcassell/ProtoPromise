using System;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using Object = System.Object;

namespace uPromise
{
	// Promise Static methods.
	public partial class Promise
	{
		private static MonoBehaviour GlobalBehaviour
		{
			get { return GlobalMonoBehaviour.Instance; }
		}

		#region All

		/// <summary>
		/// Wait for all promises specified to be resolved (parallel) in order for the All Promise to be resolved, if any of them fails, it will immediately reject the promise.
		/// </summary>
		/// <param name="promises">Promises waiting for being resolved.</param>
		/// <returns></returns>
		public static Promise<object[]> All(params Promise[] promises)
		{
			var masterDeferred = new Deferred<object[]>();

			GlobalBehaviour.StartCoroutine(All_Coroutine(masterDeferred, promises));

			return masterDeferred.Promise;
		}

		private static IEnumerator All_Coroutine(Deferred<object[]> masterDeferred, Promise[] promises)
		{
			int count = 0;
			var args = new object[promises.Length];
			int i = 0;
			foreach (var promise in promises)
			{
				HandleAllPromise(promise, i, args)
						.Then(x =>
						{
							count++;
							masterDeferred.Notify(count);
						})
						.Fail(x => masterDeferred.Reject(x));

				i++; // increment iterator.
			}

			while (count != promises.Length && masterDeferred.State == DeferredState.Pending)
			{
				//Debug.Log(string.Format("UntilCoroutine Iterate! MasterDeferState={0}", masterDeferred.State));
				yield return new WaitForSeconds(0.1f);
			}

			//Debug.Log(string.Format("All_Coroutine Complete ; Count={0} ; ArgsLength={1} ; MasterDeferredState={2} ; count != args.Length={3} ; masterDeferred.State == PromiseState.Pending={4}", count, promises.Length, masterDeferred.State, count != promises.Length, masterDeferred.State == DeferredState.Pending));
			if (masterDeferred.State == DeferredState.Pending)
				masterDeferred.Resolve(args);
		}

		// This is a helper, in order to pass the arguments in sequence according to the promises.
		private static Promise HandleAllPromise(Promise promise, int index, object[] args)
		{
			return promise.Then(x =>
			{
				args[index] = x;
				return x;
			});
		}

		public static Promise<object[]> AllSequentially(params Func<Promise>[] promises)
		{
			var deferred = new Deferred<object[]>();

			var args = new object[promises.Length];

			var queue = new Queue<Func<Promise>>(promises);
			HandleNext_AllSequentially(queue, deferred, args, 0);

			return deferred.Promise;
		}
		
		private static void HandleNext_AllSequentially(Queue<Func<Promise>> queue, Deferred<object[]> masterDeferred, object[] args, int indexer)
		{
			if (queue.Count == 0)
			{
				masterDeferred.Resolve(args);
				return;
			}
			var promiseFunc = queue.Dequeue();
			promiseFunc()
					.Then(x =>
					{
						args[indexer] = x;
						HandleNext_AllSequentially(queue, masterDeferred, args, indexer++);
					})
					.Fail(x => masterDeferred.Reject(x));
		}

		#endregion

		/// <summary>
		/// Converts WWW into a promise.
		/// </summary>
		/// <param name="www"></param>
		/// <returns></returns>
		public static Promise<WWW> AsPromise(WWW www)
		{
			var deferred = new Deferred<WWW>();
			GlobalBehaviour.StartCoroutine(WwwAsPromise_CoroutineHandler(www, deferred));

			return deferred.Promise;
		}

		private static IEnumerator WwwAsPromise_CoroutineHandler(WWW www, Deferred<WWW> deferred)
		{
			yield return www;
			deferred.Resolve(www);
		}

		public static Promise Reject(object reason)
		{
			return PromiseFactory.StartNewDeferred(dfd =>
			{
				dfd.Reject(reason);
			});
		}

		public static Promise<T> Reject<T>(object reason)
		{
			return PromiseFactory.StartNewDeferred<T>(dfd =>
			{
				dfd.Reject(reason);
			});
		}
	}
}