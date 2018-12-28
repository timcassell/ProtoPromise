using UnityEngine;
using System.Collections;
using uPromise;

public class BulbsController : MonoBehaviour
{
	public BulbBehaviour bulb1;
	public BulbBehaviour bulb2;

	public Renderer target;
	public Color successColor = Color.green;
	public Color failColor = Color.red;

	// Use this for initialization
	void Start()
	{
		AllTest();
	}

	public void AllTest()
	{
		Promise bulb1Promise = bulb1.Light(2)
			.Then(x => bulb1.SwitchToOff())
			.Fail(() => bulb1.SwitchToAlert())
			.Finally(() => Debug.Log("[Finally] BulbsController.WhenTest - Bulb1 has completed."));
		Promise bulb2Promise = bulb2.Light(3)
									.Then(x => bulb2.SwitchToOff())
									.Fail(() => bulb2.SwitchToAlert());

		Promise<string> otherPromise = OtherPromise(2)
								.Then(x => Debug.Log(string.Format("Other promise complete! x={0}", x)))
								.Fail<object>(x => Debug.Log(string.Format("Other promise fails! x={0}", x)))
								.Finally(x => Debug.Log(string.Format("[Finally] Other promise - completed. x={0}", x)));

		//const float bothPromiseFailDelay = 4;
		const float bothPromiseSuccessDelay = 3;
		//TODO: Fix api when Fail it will crash.
		Promise<string> bothPromise = BothPromise(bothPromiseSuccessDelay)
								.Then(x => Debug.Log(string.Format("Both promise complete! x={0}", x)))
								.Fail<string>(x => Debug.Log(string.Format("Both promise fails! x={0}", x)))
								.Finally(x => Debug.Log(string.Format("[Finally] Both promise - completed. x={0}", x)));
		if (bothPromise != null)
		{ }


		Promise.All(bulb1Promise, bulb2Promise, otherPromise)
			.Then(x => SwitchToSuccess())
			.Fail(SwitchToFail);
	}

	public Promise<string> OtherPromise(float duration)
	{
		var deferred = new Deferred<string>();

		StartCoroutine(OtherPromise_Coroutine(deferred, duration));

		return deferred.Promise;
	}

	private IEnumerator OtherPromise_Coroutine(Deferred<string> deferred, float duration)
	{
		yield return new WaitForSeconds(duration);

		deferred.Resolve("TROLOL");
	}

	public Promise<string> BothPromise(float duration)
	{
		var deferred = new Deferred<string>();

		StartCoroutine(BothPromise_Coroutine(deferred, duration));

		return deferred.Promise;
	}

	private IEnumerator BothPromise_Coroutine(Deferred<string> deferred, float duration)
	{
		yield return new WaitForSeconds(duration);

		if (duration > 3)
			deferred.Reject(2);
		else
			deferred.Resolve("BOTHOLOL");

	}

	private void ChangeColour(Color color)
	{
		target.material.color = color;
	}
	public void SwitchToSuccess()
	{
		ChangeColour(successColor);
	}
	public void SwitchToFail()
	{
		ChangeColour(failColor);
	}
}