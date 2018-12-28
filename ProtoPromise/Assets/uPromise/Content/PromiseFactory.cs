using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using uPromise;

public class PromiseFactory
{
	private static MonoBehaviour GlobalBehaviour { get { return GlobalMonoBehaviour.Instance; } }

	#region Promise<T>
	/// <summary>
	/// Initiate a Promise from a non-async function and resolve it immediately with the return value of the specified function.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="func"></param>
	/// <param name="delay"></param>
	/// <param name="cancellation"></param>
	/// <returns></returns>
	/// <remarks>
	/// <c>MonoBehaviour monoBehaviour</c>, ideally it wont be as a parameter but instead there will be 1 global 'Task' manager.
	/// </remarks>
	public static Promise<T> StartNew<T>(Func<T> func, float delay = 0, Func<bool> cancellation = null)
	{
		return StartNewDeferred<T>(x => x.Resolve(func()), delay, cancellation);
	}

	public static Promise<T> StartNewDeferred<T>(Action<Deferred<T>> action, float delay = 0, Func<bool> cancellation = null)
	{
		var deferred = new Deferred<T>();
		return StartNewDeferred(action, deferred, delay, cancellation);
	}

	public static Promise<T> StartNewDeferred<T>(Action<Deferred<T>> action, Deferred<T> deferred, float delay = 0, Func<bool> cancellation = null)
	{
		GlobalBehaviour.StartCoroutine(StartNewDeferred_Coroutine(x => action(deferred), deferred, delay, cancellation));
		return deferred.Promise;
	}
	#endregion


	#region Promise
	public static Promise WaitForCoroutine(IEnumerator routine)
	{
		return WaitForCoroutine(GlobalBehaviour.StartCoroutine(routine));
	}

	public static Promise WaitForCoroutine(Coroutine routine)
	{
		Deferred deferred = new Deferred();
		GlobalBehaviour.StartCoroutine(_CoroutineWait(deferred, routine));
		return deferred.Promise;
	}

	static IEnumerator _CoroutineWait(Deferred deferred, Coroutine routine)
	{
		yield return routine;
        yield return null;
		deferred.Resolve();
	}

	/// <summary>
	/// Initiate a Promise from a non-async function and resolve it immediately.
	/// </summary>
	/// <param name="action"></param>
	/// <param name="delay"></param>
	/// <param name="cancellation"></param>
	/// <returns></returns>
	public static Promise StartNew(Action action, float delay = 0, Func<bool> cancellation = null)
	{
		return StartNewDeferred(x =>
		{
			if (action != null)
				action();
			x.Resolve();
		}, delay, cancellation);
	}


	public static Promise StartNewDeferred(Action<Deferred> action, float delay = 0, Func<bool> cancellation = null)
	{
		var deferred = new Deferred();
		return StartNewDeferred(action, deferred, delay, cancellation);
	}

	public static Promise StartNewDeferred(Action<Deferred> action, Deferred deferred, float delay = 0, Func<bool> cancellation = null)
	{
		GlobalBehaviour.StartCoroutine(StartNewDeferred_Coroutine(action, deferred, delay, cancellation));
		return deferred.Promise;
	}

	private static IEnumerator StartNewDeferred_Coroutine(Action<Deferred> action, Deferred deferred, float delay = 0, Func<bool> cancellation = null)
	{
		if (delay > 0)
		{
			// Delay it instantly so that first the promise will return and the subscribe with promise will work accordingly.
			yield return new WaitForSeconds(delay);
		}
		else
		{
			// Delay it instantly so that first the promise will return and the subscribe with promise will work accordingly.
			yield return null;
		}

		if (cancellation != null)
		{
			var isCancelled = cancellation();
			if (isCancelled)
			{
				deferred.Reject();
				yield break;
			}
		}

		if (action != null)
			action(deferred);
	}


	/// <summary>
	/// Initiate a Promise from a non-async function and resolve it after the specified delay time if the cancellation remains false, otherwise it will be rejected.
	/// </summary>
	/// <param name="delay"></param>
	/// <param name="cancellation"></param>
	/// <returns></returns>
	public static Promise StartNewDelayed(float delay, Func<bool> cancellation = null)
	{
		return StartNew(null, delay, cancellation);
	}
	#endregion
}