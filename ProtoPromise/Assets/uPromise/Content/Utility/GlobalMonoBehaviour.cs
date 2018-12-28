using UnityEngine;
using System.Collections;

public class GlobalMonoBehaviour : Singleton<GlobalMonoBehaviour>
{
	//#region
	//private static GlobalMonoBehaviour _instance;
	//public static GlobalMonoBehaviour Instance
	//{
	//	get
	//	{
	//		if (_instance == null)
	//		{
	//			Debug.Log("GlobalMonoBehaviour.Instance_get - Create New Instance.");
	//			_instance = new GameObject("__Global", typeof(GlobalMonoBehaviour)).GetComponent<GlobalMonoBehaviour>();
	//			_instance.gameObject.hideFlags = HideFlags.HideAndDontSave;
	//		}
	//		return _instance;
	//	}
	//}

	//void Awake()
	//{
	//	if (_instance != null)
	//	{
	//		Debug.Log("GlobalMonoBehaviour.Awake - Create New Instance.");
	//		Destroy(gameObject);
	//		return;
	//	}
	//}


	//// Use this for initialization
	//void Start()
	//{

	//}

	//void OnApplicationQuit()
	//{
	//	if (this != _instance)
	//	{
	//		DestroyImmediate(this.gameObject);
	//		return;
	//	}
	//	DestroyImmediate(this.gameObject);
	//	_instance = null;
	//}
	//#endregion

	protected override void OnInit()
	{
	}

	public void Initialize()
	{
		Debug.Log("Initializing...");
	}

	public void Shutdown()
	{
		Debug.Log("Shutting down...");
	}
	
}