using UnityEngine;
using System.Collections;
using uPromise;

public class BulbBehaviour : MonoBehaviour
{
	public Color onColor = Color.yellow;
	public Color offColor = Color.gray;
	public Color alertColor = Color.red;
	public float lightDuration = 2f;

	public Renderer target;

	// Use this for initialization
	void Start()
	{
		//Light(lightDuration)
		//	.Done(() => ChangeColour(offColor))
		//	.Done((x) => Debug.Log("CHIKO LIGHTS OFF!"));
	}

	public Promise Light(float duration)
	{
		ChangeColour(onColor);

		var deferred = new Deferred();

		StartCoroutine(Light_Coroutine(deferred, duration));
		 return deferred.Promise;
	}

	private IEnumerator Light_Coroutine(Deferred deferred, float duration)
	{
		yield return new WaitForSeconds(duration);

		if(duration > 3)
			deferred.Reject();
		else
			deferred.Resolve();
	}

	public void SwitchToOn()
	{
		ChangeColour(onColor);
	}
	public void SwitchToOff()
	{
		ChangeColour(offColor);
	}
	public void SwitchToAlert()
	{
		ChangeColour(alertColor);
	}

	private void ChangeColour(Color color)
	{
		target.material.color = color;
	}
}