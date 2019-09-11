using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Proto.Promises;
using UnityEngine;

public class TestScript : MonoBehaviour
{
	Promise.Deferred[] protoDeferreds = new Promise.Deferred[0];
	uPromise.Deferred[] uDeferreds = new uPromise.Deferred[0];
	Task[] tasks = new Task[0];


	Action voidToVoid = () => { };
	Action<float> toVoid = x => { };
	Func<int> voidToInt = () => 0;
	Func<object, int> uToInt = x => 0;
	Func<int, float> toFloat = x => 0f;
	Func<int, int> intToInt = x => x;

	Func<Task, int> taskVoidToInt = x => 0;
	Func<Task<int>, float> taskIntToFloat = x => 0f;
	Action<Task<float>> taskFloatToVoid = x => { };

	public int thenCount = 100;
	public int trials = 100;

	System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();

	long protoCreation, protoThen, protoResolve,
		uCreation, uThen, uResolve,
		taskCreation, taskThen, taskResolve;

	public enum RunType
	{
		None,
		ProtoPromise,
		uPromise,
		Task
	}
	public RunType runType = RunType.None;

    private IEnumerator Start()
	{
        Promise.Config.ObjectPooling = Promise.PoolType.All;
        //Promise.Config.DebugStacktraceGenerator = Promise.GeneratedStacktrace.All;

		//Debug.LogWarning(System.Threading.Thread.CurrentThread.ManagedThreadId);
		//voidToVoid = () => { Debug.LogError(System.Threading.Thread.CurrentThread.ManagedThreadId); };
		//Task task = new Task(voidToVoid);
		//Task<int> task2 = new Task<int>(voidToInt);

		//task2
		//	.ContinueWith(i => { Debug.Log("deferred2.done: " + i); return i.Result; })
		//	//.End()
		//	//.Catch(e => {})
		//	//.Fail<int>(x => { Debug.LogError("Rejected: " + x); return x;})
		//	;

		//task
		//		.ContinueWith(x => Debug.Log("deferred1.done"))
		//		.ContinueWith(async x => await task2)
		// 			.ContinueWith(x => { Debug.Log("deferred.then " + x); /*throw new InvalidCastException();*/ return "deferred string."; })
		//		.ContinueWith(x => { Debug.Log("Promise.Done"); return x; })
		//		//.Catch<ArgumentException>( e => { Debug.LogError("caught argument"); return e.ToString(); })
		//		//.Catch((Exception e) => { Debug.LogError("caught exception"); return e.ToString(); })
		//		.ContinueWith(s => { Debug.Log("deferred.done " + s); return s; })
		//		.ContinueWith(s => { Debug.Log(s); return s; })
		//		//.End()
		//		;


		//task2 = task2
		//	.ContinueWith(_ => { Debug.Log("deferred2.then"); return "deferred2 string."; })
		//	.ContinueWith(s => Debug.Log(s))
		//	.ContinueWith(_ => Debug.Log("deferred2 complete"))
		//	.ContinueWith(_ => 0)
		//	//.Catch((Exception e) => { Debug.LogError("caught exception"); throw e; return e.ToString(); })
		//	//.End()
		//	//.Then(s => s)
		//	;
		//task.RunSynchronously();

		//task2.RunSynchronously();


		var deferred = Promise.NewDeferred();
		var deferred2 = Promise.NewDeferred<int>();


        var promise2 = deferred2.Promise
                  .Then(i => { Debug.Log("deferred2.then: " + i); return i; });
		//	//.End()
		//	//.Catch(e => {})
		//	//.Fail<int>(x => { Debug.LogError("Rejected: " + x); return x;})
		//	;

		var promise1 = deferred.Promise
                .Then(() => Debug.Log("deferred1.then"))
                .Complete(() => Debug.LogWarning("Promise 1 complete"))
				.Then(() => 1)
                .Then(x => { Debug.Log("deferred.then " + x); /*throw new InvalidCastException();*/ return "deferred string."; })
				.Then(x => { Debug.Log("Promise.Done " + x); return x; })
				//.Catch<ArgumentException>( e => { Debug.LogError("caught argument"); return e.ToString(); })
				//.Catch((Exception e) => { Debug.LogError("caught exception"); return e.ToString(); })
				.Then(s => { Debug.Log("deferred.done " + s); return s; })
				.Then(s => { Debug.Log(s); return s; })
				.Finally(() => { Debug.LogError("promise 1 final"); })
				;


		promise2
			.Then(() => { Debug.Log("deferred2.then"); return "deferred2 string."; })
			.Then(s => Debug.Log(s))
			.Complete(() => Debug.Log("deferred2 complete"))
			//.Catch((Exception e) => { Debug.LogError("caught exception"); throw e; return e.ToString(); })
			.Finally(() => { Debug.LogError("promise 2 final"); })
			//.Then(s => s)
			;

        Promise.All(promise1, promise2)
            .Then(() => Debug.LogError("All then."))
            ;


        deferred.Resolve();
        deferred2.Resolve(199);

        //try
        //{
        //	throw new Exception();
        //}
        //catch (Exception e)
        //{
        //	Debug.LogException(new UnhandledExceptionException().SetValue(e));
        //}


        //var temp = voidToVoid;
        //yield return null;
        //var temp2 = voidToVoid;
        //yield return null;
        //var temp3 = voidToVoid;
        //yield return null;
        //var temp4 = voidToVoid;
        //yield return null;
        //Debug.Log(null);
        //yield return null;

        //ProtoPromise.GlobalMonoBehaviour.Yield(voidToVoid);

        //yield return null;
        //yield return null;
        //yield return null;
        //yield return null;
        //yield return null;

        //yield return new WaitForSeconds(1);

        //cancelation.Invoke(false);

        ////deferred2.Reject(default(Exception));
        ////deferred2.Reject(new Exception());
        //yield return null;

        ////deferred.Reject(1.5f);

        //yield return null;
        //Debug.LogError("----------");

        //deferred2.Throw(new InvalidCastException());
        //try
        //{
        //	throw new InvalidCastException();
        //}
        //catch (Exception e)
        //{
        //	deferred2.Throw(e);
        //}

        StartCoroutine(Executor());
		StartCoroutine(Log());

		yield break;
	}

	WaitForSeconds waitFrame = new WaitForSeconds(0f);

	IEnumerator Executor()
	{
		Reset:
		yield return null;

		switch (runType)
		{
			case RunType.ProtoPromise:
				{
					protoThen = 0;
					break;
				}
			case RunType.uPromise:
				{
					uThen = 0;
					break;
				}
			case RunType.Task:
				{
					taskThen = 0;
					break;
				}
		}
		for (int j = 0; j < trials; ++j)
		{
			AddThens(j);
		}

		goto Reset;
	}

	WaitForEndOfFrame waitForEndOfFrame = new WaitForEndOfFrame();

	IEnumerator Log()
	{
		Reset:
		yield return waitForEndOfFrame;

		switch(runType)
		{
			case RunType.ProtoPromise:
				{
					protoCreation /= trials;
					protoThen /= trials;
					protoResolve /= trials;
					break;
				}
			case RunType.uPromise:
				{
					uCreation /= trials;
					uThen /= trials;
					uResolve /= trials;
					break;
				}
			case RunType.Task:
				{
					taskCreation /= trials;
					taskThen /= trials;
					taskResolve /= trials;
					break;
				}
		}
		if (runType != RunType.None)
		{
			Debug.LogFormat("ProtoPromise average Creation: {0}, Then: {1}, Resolve: {2}", protoCreation, protoThen, protoResolve);
			Debug.LogFormat("uPromise average Creation: {0}, Then: {1}, Resolve: {2}", uCreation, uThen, uResolve);
			Debug.LogFormat("Task average Creation: {0}, Then: {1}, Resolve: {2}", taskCreation, taskThen, taskResolve);
		}
		goto Reset;
	}

	private void Update()
	{
		GC.Collect();

		switch (runType)
		{
			case RunType.ProtoPromise:
				{
					if (protoDeferreds.Length != trials)
					{
						protoDeferreds = new Promise.Deferred[trials];
					}
					protoCreation = 0;

					watch.Reset();
					watch.Start();

					for (int j = 0; j < trials; ++j)
					{
						protoDeferreds[j] = Promise.NewDeferred();
					}

					watch.Stop();
					protoCreation = watch.ElapsedTicks;
					break;
				}
			case RunType.uPromise:
				{
					if (uDeferreds.Length != trials)
					{
						uDeferreds = new uPromise.Deferred[trials];
					}
					uCreation = 0;

					watch.Reset();
					watch.Start();
					for (int j = 0; j < trials; ++j)
					{
						uDeferreds[j] = new uPromise.Deferred();
					}
					watch.Stop();
					uCreation = watch.ElapsedTicks;
					break;
				}
			case RunType.Task:
				{
					if (tasks.Length != trials)
					{
						tasks = new Task[trials];
					}
					taskCreation = 0;

					watch.Reset();
					watch.Start();
					for (int j = 0; j < trials; ++j)
					{
						tasks[j] = new Task(voidToVoid);
					}
					watch.Stop();
					taskCreation = watch.ElapsedTicks;
					break;
				}
		}
	}

	public void LateUpdate ()
	{
		switch (runType)
		{
			case RunType.ProtoPromise:
				{
					protoResolve = 0;
					break;
				}
			case RunType.uPromise:
				{
					uResolve = 0;
					break;
				}
			case RunType.Task:
				{
					taskResolve = 0;
					break;
				}
		}
		for (int j = 0; j < trials; ++j)
		{
			Resolve(j);
		}
	}


	public void AddThens(int index)
	{
		switch (runType)
		{
			case RunType.ProtoPromise:
				{
					var lolPromise = protoDeferreds[index].Promise;
					var lolPromiseInt = lolPromise.Then(voidToInt);
					var lolPromiseFloat = lolPromiseInt.Then(toFloat);

					watch.Reset();
					watch.Start();

					for (int i = 0; i < thenCount; ++i)
					{
						//lolPromise = lolPromise.Then(voidToVoid);
						//lolPromiseInt.Then(intToInt);
						lolPromise = lolPromiseFloat.Then(toVoid);
						lolPromiseInt = lolPromise.Then(voidToInt);
						lolPromiseFloat = lolPromiseInt.Then(toFloat);
					}

					watch.Stop();
					protoThen += watch.ElapsedTicks;
					break;
				}
			case RunType.uPromise:
				{
					var uPromise = uDeferreds[index].Promise;
					var uPromiseInt = uPromise.Then(uToInt);
					var uPromiseFloat = uPromiseInt.Then(toFloat);

					watch.Reset();
					watch.Start();

					for (int i = 0; i < thenCount; ++i)
					{
						//uPromise = uPromise.Then(voidToVoid);
						//uPromiseInt.Then(intToInt);
						uPromise = uPromiseFloat.Then(toVoid);
						uPromiseInt = uPromise.Then(uToInt);
						uPromiseFloat = uPromiseInt.Then(toFloat);
					}

					watch.Stop();
					uThen += watch.ElapsedTicks;
					break;
				}
			case RunType.Task:
				{
					var task = tasks[index];
					var taskInt = task.ContinueWith(taskVoidToInt);
					var taskFloat = taskInt.ContinueWith(taskIntToFloat);

					watch.Reset();
					watch.Start();

					for (int i = 0; i < thenCount; ++i)
					{
						task = taskFloat.ContinueWith(taskFloatToVoid);
						taskInt = task.ContinueWith(taskVoidToInt);
						taskFloat = taskInt.ContinueWith(taskIntToFloat);
					}

					watch.Stop();
					taskThen += watch.ElapsedTicks;
					break;
				}
		}
	}

	public void Resolve(int index)
	{
		switch (runType)
		{
			case RunType.ProtoPromise:
				{
					var lolDeferred = protoDeferreds[index];

					watch.Reset();
					watch.Start();

					lolDeferred.Resolve();
                    Promise.Manager.HandleCompletes();

                    watch.Stop();
					protoResolve += watch.ElapsedTicks;
					break;
				}
			case RunType.uPromise:
				{
					var uDeferred = uDeferreds[index];

					watch.Reset();
					watch.Start();

					uDeferred.Resolve();

					watch.Stop();
					uResolve += watch.ElapsedTicks;
					break;
				}
			case RunType.Task:
				{
					var task = tasks[index];

					watch.Reset();
					watch.Start();

					task.RunSynchronously();
					task.Wait();

					watch.Stop();
					taskResolve += watch.ElapsedTicks;
					break;
				}
		}
	}
}
