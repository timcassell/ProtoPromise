using System;
using UnityEngine;
using System.Collections;
using uPromise;

/// <summary>
/// Example of converting MoveTowards with Promises VS without.
/// This example is a real scenario how to convert a simple coroutine function from just coroutine+callbacks to use Promises.
/// </summary>
public class PromiseMoveController : MonoBehaviour
{
	public Transform pointA;
	public Transform pointB;

	public Transform target;
	public float speed;

	public Color stoppedColor = Color.red;
	public Color movingColor = Color.magenta;

	private bool _isMoving;

	void OnGUI()
	{
		GUI.enabled = !_isMoving;
		if (GUILayout.Button("GO TO A"))
		{
			Move(pointA.position);
		}
		if (GUILayout.Button("GO TO B"))
		{
			Move(pointB.position);
		}
		if (GUILayout.Button("GO TO A THEN B"))
		{
			Move(pointA.position)
				.Then(x => Move(pointB.position))
				.Then(x => Debug.Log(string.Format("Moving to both locations complete! x={0}", x)));
		}
	}

	private Promise Move(Vector3 point)
	{
		ChangeColor(movingColor);

		// Use one at a time to try them out.

		// Using callbacks
		//MoveToPoint(point, () => ChangeColor(stoppedColor));

		// OR

		// Using Promises
		return MoveToPointPromise(point)
			.Then(() => ChangeColor(stoppedColor));
	}

	private void MoveToPoint(Vector3 point, Action onDone)
	{
		_isMoving = true;

		MoveTowards(target, point, () =>
		{
			_isMoving = false;
			onDone();
		});
	}

	private Promise MoveToPointPromise(Vector3 point)
	{
		_isMoving = true;
		// By simply returning it others can optionally hook into it.
		return MoveTowardsPromise(target, point)
			.Then(() =>
			{
				_isMoving = false;
			});
	}

	#region MoveTowards Normal

	public void MoveTowards(Transform moveTransform, Vector3 point, Action onDone)
	{
		StartCoroutine(MoveTowards_Coroutine(target, point, onDone));
	}

	private IEnumerator MoveTowards_Coroutine(Transform moveTransform, Vector3 point, Action onDone)
	{
		while (true)
		{
			if (Vector3.Distance(target.position, point) < 0.1)
			{
				Debug.Log("MoveTowards - In distance! Exit!");
				break;
			}

			float step = speed * Time.deltaTime;
			moveTransform.position = Vector3.MoveTowards(moveTransform.position, point, step);
			Debug.Log(string.Format("MoveTowards - [Loop] MoveTo={0}", moveTransform.position));

			yield return null;
		}
		Debug.Log("MoveTowards - Exit!");
		onDone();
	}
	#endregion

	#region Movetowards Promises

	public Promise MoveTowardsPromise(Transform moveTransform, Vector3 point)
	{
		var deferred = new Deferred();

		StartCoroutine(MoveTowardsPromise_Coroutine(target, point, deferred));

		return deferred.Promise;
	}

	private IEnumerator MoveTowardsPromise_Coroutine(Transform moveTransform, Vector3 point, Deferred deferred)
	{
		while (true)
		{
			if (Vector3.Distance(target.position, point) < 0.1)
			{
				Debug.Log("MoveTowards - In distance! Exit!");
				break;
			}

			float step = speed * Time.deltaTime;
			moveTransform.position = Vector3.MoveTowards(moveTransform.position, point, step);
			Debug.Log(string.Format("MoveTowards - [Loop] MoveTo={0}", moveTransform.position));

			yield return null;
		}
		yield return null;
		Debug.Log("MoveTowards - Exit!");
		deferred.Resolve();
	}
	#endregion
	private void ChangeColor(Color color)
	{
		target.GetComponent<Renderer>().material.color = color;
	}
}