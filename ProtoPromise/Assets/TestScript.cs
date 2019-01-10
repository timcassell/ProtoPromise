using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using ProtoPromise;
using UnityEngine;

public class TestScript : MonoBehaviour
{
	//private void Awake()
	//{
	//	string throwTrace = null;
	//	string newTrace = null;
	//	try
	//	{
	//		newTrace = new System.Diagnostics.StackTrace(0, true).ToString();
	//		throw new Exception();
	//	}
	//	catch (Exception e)
	//	{
	//		throwTrace = e.StackTrace;
	//	}
	//	Debug.Log("throwTrace: {" + throwTrace + "}");
	//	Debug.Log("newTrace: {" + newTrace + "}");

	//	//throw new UnhandledException(null, new System.Diagnostics.StackTrace(0, true).ToString().Replace("line ", string.Empty));
	//}

	class MyYield : IEnumerator
	{
		int frame = Time.frameCount;
		public bool keepWaiting { get { /*Debug.LogError("custom wait initial frame: " + frame + ", real frame: " + Time.frameCount);*/ return frame + 0 > Time.frameCount; } }

		public object Current { get { return null; } }

		public bool MoveNext()
		{
			return keepWaiting;
		}

		public void Reset()
		{
			frame = Time.frameCount;
		}
	}

	Deferred[] lolDeferreds;
	uPromise.Deferred[] uDeferreds;

	IEnumerator WaitForEnd(int index, object yield)
	{
		Debug.LogError(index + " WaitForEndFirst: " + Time.frameCount);
		yield return yield;
		Debug.LogError(index + " WaitForEndSecond: " + Time.frameCount);
	}

	private IEnumerator Start()
	{
		//var wait = WaitForEnd(0, null);
		//StartCoroutine(wait);
		//StopCoroutine(wait);

		//StartCoroutine(wait);
		//yield return null;
		//yield return null;
		//yield return null;

		//yield break;

		//MyYield myYield = new MyYield();
		//yield return new WaitForSeconds(1);
		//while (true)
		//{
		//	var coroutine = StartCoroutine(myYield);
		//	for (int i = 0; i < 100000; ++i)
		//	{
		//		coroutine = StartCoroutine(myYield);
		//	}
		//	yield return coroutine;
		//}
		//yield return new WaitForEndOfFrame();
		////StartCoroutine(WaitForEnd(0, myYield));
		////StartCoroutine(WaitForEnd(0, myYield));
		//yield return myYield;
		//Debug.LogError(Time.frameCount);
		//yield return null;
		//StartCoroutine(WaitForEnd(1, new WaitForEndOfFrame()));
		//yield return new WaitForEndOfFrame();
		//Debug.LogError(Time.frameCount);
		//StartCoroutine(WaitForEnd(2, new WaitForEndOfFrame()));
		//ProtoPromise.GlobalMonoBehaviour.Yield(new WaitForSeconds(2), () => Debug.LogError("Waited 2 seconds"));

		Deferred deferred = Promise.Deferred();
		Deferred<int> deferred2 = Promise.Deferred<int>();


		var promise2 = deferred2.Promise
			.Done(i => Debug.Log("deferred2.done: " + i))
            .Catch(e => {})
            //.Fail<int>(x => { Debug.LogError("Rejected: " + x); return x;})
            ;

		deferred.Promise
				.Done(() => Debug.Log("deferred1.done"))
		        .Then(() => promise2)
	   			.Then(() => { Debug.Log("deferred.then"); /*throw new InvalidCastException();*/ return "deferred string."; })
				.Done(() => Debug.Log("Promise.Done"))
				//.Catch<ArgumentException>( e => { Debug.LogError("caught argument"); return e.ToString(); })
		        .Fail<Exception>( e => { Debug.LogError("caught exception"); return e.ToString(); })
				.Done(s => Debug.Log("deferred.done " + s))
				.Then(s => { Debug.Log(s); return s; })
				.Fail<int>(f => Debug.Log("Failed: " + f));
		;
		promise2
			.Then(() => { Debug.Log("deferred2.then"); return "deferred2 string."; })
			.Done(s => Debug.Log(s))
			.Fail<float>(f => Debug.Log("Failed: " + f))
			//.Then(s => s)
			;


		deferred.Resolve();
		deferred2.Reject(0);
		yield return null;

		//deferred.Reject(1.5f);

		yield return null;
		Debug.LogError("----------");

		//deferred2.Throw(new InvalidCastException());
		//try
		//{
		//	throw new InvalidCastException();
		//}
		//catch (Exception e)
		//{
		//	deferred2.Throw(e);
		//}

		//StartCoroutine(Executor());
		//StartCoroutine(Log());

		//StartCoroutine(wait());

		//Debug.Log(Time.frameCount);

		//new Deferred().Promise.Then<Coroutine>(() => StartCoroutine(tester()));

		//routine = tester();
		//StartCoroutine(routine);
		//StartCoroutine(routine);
		//yield break;
		////yield return routine;
		//Debug.Log(Time.frameCount);

		//yield return routine;
		//Debug.Log(Time.frameCount);

		//routine = null;
		//yield return routine;
		//Debug.Log(Time.frameCount);
	}
	//IEnumerator routine;

	//IEnumerator tester()
	//{
	//	Debug.Log(Time.frameCount);
	//	yield return new WaitForSeconds(1);
	//	Debug.Log(Time.frameCount);
	//	StopCoroutine(routine);
	//	Debug.Log(Time.frameCount);
	//	StartCoroutine(routine);
	//	Debug.Log(Time.frameCount);
	//	yield return null;
	//	Debug.Log(Time.frameCount);

	//	yield break;
	//}

	//IEnumerator wait()
	//{
	//	yield return new WaitForSeconds(1);

	//	StartCoroutine(routine);
	//}

	//IEnumerator Executor()
	//{
	//	Reset:
	//	yield return null;

	//	if (runLoL)
	//	{
	//		lolThen = 0;
	//	}
	//	else
	//	{
	//		uThen = 0;
	//	}
	//	for (int j = 0; j < trials; ++j)
	//	{
	//		AddThens(j);
	//	}

	//	goto Reset;
	//}

	//IEnumerator Log()
	//{
	//	Reset:
	//	yield return new WaitForEndOfFrame();

	//	if (runLoL)
	//	{
	//		lolTotal = (lolCreation + lolThen + lolResolve) / trials;
	//		lolCreation /= trials;
	//		lolThen /= trials;
	//		lolResolve /= trials;
	//	}
	//	else
	//	{
	//		uTotal = (uCreation + uThen + uResolve) / trials;
	//		uCreation /= trials;
	//		uThen /= trials;
	//		uResolve /= trials;
	//	}
	//	Debug.LogFormat("lolPromise average Creation: {0}, Then: {1}, Resolve: {2}, Total: {3}", lolCreation, lolThen, lolResolve, lolTotal);
	//	Debug.LogFormat("uuuPromise average Creation: {0}, Then: {1}, Resolve: {2}, Total: {3}", uCreation, uThen, uResolve, uTotal);
	//	Debug.LogWarningFormat("Differences: Creation: {0}, Then: {1}, Resolve: {2}, Total: {3}",
	//	                       ((double)lolCreation / uCreation).ToString("n3"),
	//	                       ((double)lolThen / uThen).ToString("n3"),
	//	                       ((double)lolResolve / uResolve).ToString("n5"),
	//	                       ((double)lolTotal / uTotal).ToString("n3"));

	//	goto Reset;
	//}


	//Action voidToVoid = () => { };
	//Action<float> toVoid = x => { };
	//Func<int> toInt = () => 0;
	//Func<object, int> uToInt = x => 0;
	//Func<int, float> toFloat = x => 0f;
	//Func<int, int> intToInt = x => x;

	//public int thenCount = 100;
	//public int trials = 100;

	//System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();

	//long lolCreation, lolThen, lolResolve, lolTotal,
	//	uCreation, uThen, uResolve, uTotal;

	//public bool runLoL = true;


	//private void Update()
	//{
	//	if (runLoL)
	//	{
	//		lolDeferreds = new Deferred[trials];
	//		lolCreation = 0;

	//		watch.Reset();
	//		watch.Start();

	//		for (int j = 0; j < trials; ++j)
	//		{
	//			lolDeferreds[j] = new Deferred();
	//		}

	//		watch.Stop();
	//		lolCreation = watch.ElapsedTicks;
	//	}
	//	else
	//	{
	//		uDeferreds = new uPromise.Deferred[trials];
	//		uCreation = 0;

	//		watch.Reset();
	//		watch.Start();
	//		for (int j = 0; j < trials; ++j)
	//		{
	//			uDeferreds[j] = new uPromise.Deferred();
	//		}
	//		watch.Stop();
	//		uCreation = watch.ElapsedTicks;
	//	}
	//}

	//public void LateUpdate ()
	//{
	//	if (runLoL)
	//	{
	//		lolResolve = 0;
	//	}
	//	else
	//	{
	//		uResolve = 0;
	//	}
	//	for (int j = 0; j < trials; ++j)
	//	{
	//		Resolve(j);
	//	}
	//}


	//public void AddThens(int index)
	//{
	//	if (runLoL)
	//	{
	//		var lolPromise = lolDeferreds[index].Promise;
	//		var lolPromiseInt = lolPromise.Then(toInt);
	//		var lolPromiseFloat = lolPromiseInt.Then(toFloat);

	//		watch.Reset();
	//		watch.Start();

	//		for (int i = 0; i < thenCount; ++i)
	//		{
	//			lolPromise = lolPromise.Then(voidToVoid);
	//			//lolPromiseInt.Then(intToInt);
	//			lolPromise = lolPromiseFloat.Then(toVoid);
	//			lolPromiseInt = lolPromise.Then(toInt);
	//			lolPromiseFloat = lolPromiseInt.Then(toFloat);
	//		}

	//		watch.Stop();
	//		lolThen += watch.ElapsedTicks;
	//	}
	//	else
	//	{
	//		var uPromise = uDeferreds[index].Promise;
	//		var uPromiseInt = uPromise.Then(uToInt);
	//		var uPromiseFloat = uPromiseInt.Then(toFloat);

	//		watch.Reset();
	//		watch.Start();

	//		for (int i = 0; i < thenCount; ++i)
	//		{
	//			//uPromise = uPromise.Then(voidToVoid);
	//			//uPromiseInt.Then(intToInt);
	//			uPromise = uPromiseFloat.Then(toVoid);
	//			uPromiseInt = uPromise.Then(uToInt);
	//			uPromiseFloat = uPromiseInt.Then(toFloat);
	//		}

	//		watch.Stop();
	//		uThen += watch.ElapsedTicks;
	//	}
	//}

	//public void Resolve(int index)
	//{
	//	if (runLoL)
	//	{
	//		var lolDeferred = lolDeferreds[index];

	//		watch.Reset();
	//		watch.Start();

	//		lolDeferred.Resolve();

	//		watch.Stop();
	//		lolResolve += watch.ElapsedTicks;
	//	}
	//	else
	//	{
	//		var uDeferred = uDeferreds[index];

	//		watch.Reset();
	//		watch.Start();

	//		uDeferred.Resolve();

	//		watch.Stop();
	//		uResolve += watch.ElapsedTicks;
	//	}
	//}
}
