using System;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using uPromise.Test;

public class PromiseTestRunner : MonoBehaviour
{
	private NavController _navController;
	public Color navSuccessColor = Color.green;
	public Color navErrorColor = Color.red;
	public Color navProcessingColor = Color.yellow;

	public Renderer targetRenderer;

	void Awake()
	{
		_navController = new NavController();
		Navigate("SceneA");
	}

	void OnGUI()
	{

		if (Button("Navigate Scene A"))
		{
			Navigate("SceneA");
		}

		if (Button("Navigate Scene B"))
		{
			Navigate("SceneB");
		}
	}

	private void Navigate(string sceneName)
	{
		_navController.Navigate(sceneName)
						.Then(x =>
								{
									Debug.Log(string.Format("PromiseTestRunner.Navigate - Then={0}", x));
									Debug.Log(string.Format("PromiseTestRunner.Navigate - scene.SceneName={0}", x.SceneName));

									ChangeColor(navSuccessColor);

									return x;
								})
						.Fail<ArgumentException>(x => Debug.Log(string.Format("PromiseTestRunner.Navigate - [Fail<ArgumentException>] ERRORR! x={0}", x)))
						.Fail<System.Net.HttpStatusCode>(x => Debug.Log(string.Format("PromiseTestRunner.Navigate - [Fail<HttpStatusCode>] ERRORR! x={0}", x)))
						.Fail(x =>
								{
									Debug.Log(string.Format("PromiseTestRunner.Navigate - [Fail] GENERAL ERRORR! x={0}", x));
									ChangeColor(navErrorColor);
								})
						.Finally(() => Debug.Log(string.Format("PromiseTestRunner.Navigate - [Finally] ends")))
						.Done(x => Debug.Log(string.Format("PromiseTestRunner.Navigate - [Done] x={0}", x)));
		ChangeColor(navProcessingColor);

	}

	private void ChangeColor(Color color)
	{
		targetRenderer.material.color = color;
	}


	#region helpers
	private const float _guiSpace = 15f;
#if UNITY_IOS || UNITY_ANDROID
	int buttonHeight = 80;
	int mainWindowWidth = Screen.width - 30;
#else
	int buttonHeight = 24;
	int mainWindowWidth = 500;
#endif


	private bool Button(string label, float space = _guiSpace)
	{
		GUILayout.Space(space);
		return GUILayout.Button(
		  label,
		  GUILayout.MinHeight(buttonHeight),
		  GUILayout.MaxWidth(mainWindowWidth)
		);
	}
	#endregion
}

namespace uPromise.Test
{

	public class NavController
	{
		private readonly SceneLoader _loader = new SceneLoader();
		private readonly ControllerLocator _locator = new ControllerLocator();
		private ISceneController _currentSceneController;

		public Promise<MockScene> Navigate(string sceneName)
		{
			Debug.Log(string.Format("NavController.Navigate - init navigation ; sceneName={0}", sceneName));

			var controller = _locator.Locate(sceneName);
			Debug.Log(string.Format("NavController.Navigate - controller located ; controller.FriendlyName={0} ; controllerType={1}", controller.FriendlyName, controller.GetType()));
			var switchingControllerPromise = ChangeCurrentController(controller)
													.Then(x =>
														  {
															  Debug.Log(string.Format("NavController.Navigate - [Then] ChangeCurrentController complete. result.x={0}", x));
															  return "lawl";
														  })
													.Then(x =>
															{
																Debug.Log(string.Format("NavController.Navigate - [Then] ChangeCurrentController complete. result.x={0}", x));
															});

			var sceneLoad = _loader.Load(sceneName)
				.Then(x =>
				{
					Debug.Log(string.Format("NavController.Navigate - [Then] load complete. result.x={0}", x));
					//return "daIssue";//NOTE: This will cause an error, but for now its for testing purposes
				})
				.Then(x =>
				{
					Debug.Log(string.Format("NavController.Navigate - [Thenx2] load complete. result.x={0}", x));
					//throw new ArgumentException("Oqqqowww! :/");
				})
				.Fail<ArgumentException>(x => Debug.Log(string.Format("NavController.Navigate - [Fail<ArgumentException>] _loader.Load ERRORR! x={0}", x)));
			//.Fail<System.Net.HttpStatusCode>(x => Debug.Log(string.Format("NavController.Navigate - [Fail<HttpStatusCode>] _loader.Load ERRORR! x={0}", x)))
			//.Done(x => Debug.Log(string.Format("NavController.Navigate - [Done] x={0}", x)));

			return Promise.All(switchingControllerPromise, sceneLoad)
				.Then(x =>
					  {
						  Debug.Log(string.Format("NavController.Navigate - All [Then] all complete. result. x[0]={0} x[1]={1}", x));
						  return (MockScene)x[1]; // return second value which is sceneLoad result.
					  })
				.Progress<int>(x => Debug.Log(string.Format("NavController.Navigate - All [Progress]. result. x={0}", x)));
		}

		private Promise ChangeCurrentController(ISceneController sceneController)
		{
			if (_currentSceneController == null)
			{
				Debug.Log(string.Format("NavController.ChangeCurrentController - SceneController={0}", sceneController));
				return sceneController.Init()
										.Then(x =>
										{
											Debug.Log(string.Format("NavController.ChangeCurrentController - Init [Then] - Controller changed. sceneController={0}", sceneController));
											_currentSceneController = sceneController;
										});
			}

			return Promise.All(_currentSceneController.Shutdown(), sceneController.Init())
				.Then(x =>
				{
					Debug.Log(string.Format("NavController.ChangeCurrentController - All [Then] - Controller changed. sceneController={0}", sceneController));
					_currentSceneController = sceneController;
					return x;
				})
				.Progress<string>(x => Debug.Log(string.Format("The progress status={0}", x)));
		}
	}

	public class SceneLoader
	{
		public Promise<MockScene> Load(string sceneName)
		{
			const float delay = 3;
			Debug.Log(string.Format("SceneLoader.Load - init scene loading delay by {1}s. SceneName={0}", sceneName, delay));

			var scene = new MockScene
			{
				SceneName = sceneName,
				GameObjects = new List<string>
								{
									"_app",
									"floor",
									"main camera"
								}
			};
			//return TaskFactory.StartNewDelayed(() => scene, delay);

			return PromiseFactory.StartNewDeferred<MockScene>(x =>
										{
											//x.Reject(System.Net.HttpStatusCode.Conflict);
											//x.Reject();
											//x.Reject(1337);
											x.Resolve(scene);
											//return scene;
										}, delay);
		}

	}

	public class ControllerLocator
	{
		public ISceneController Locate(string sceneName)
		{
			switch (sceneName)
			{
				case "SceneA":
					return new SceneAController();
				case "SceneB":
					return new SceneBController();
				default:
					throw new NotImplementedException();
			}
		}
	}

	public interface ISceneController
	{
		string FriendlyName { get; }

		Promise Init();
		Promise Shutdown();
	}

	public class SceneAController : ISceneController
	{
		public string FriendlyName { get { return "Scene A"; } }

		public Promise Init()
		{
			const float delay = 8f;
			Debug.Log(string.Format("SceneAController.Init - FriendlyName={0}", FriendlyName));

			return PromiseFactory.StartNew(() =>
			{
				Debug.Log(string.Format("SceneAController.Init - [Then] complete. FriendlyName={0}", FriendlyName));
				return "Init";

			}, delay);
		}

		public Promise Shutdown()
		{
			const float delay = 8f;
			Debug.Log(string.Format("SceneAController.Shutdown - FriendlyName={0}", FriendlyName));

			return PromiseFactory.StartNew(() =>
			{
				Debug.Log(string.Format("SceneAController.Shutdown - [Then] complete. FriendlyName={0}", FriendlyName));
				return "Shutdown";

			}, delay);
		}
	}

	public class SceneBController : ISceneController
	{
		public string FriendlyName { get { return "Scene B"; } }

		public Promise Init()
		{
			const float delay = 3f;
			Debug.Log(string.Format("SceneBController.Init - FriendlyName={0}", FriendlyName));

			return PromiseFactory.StartNew(() =>
			{
				Debug.Log(string.Format("SceneBController.Init - [Then] complete. FriendlyName={0}", FriendlyName));
				return "Init";

			}, delay);
		}

		public Promise Shutdown()
		{
			const float delay = 2f;
			Debug.Log(string.Format("SceneBController.Shutdown - FriendlyName={0}", FriendlyName));

			return PromiseFactory.StartNew(() =>
			{
				Debug.Log(string.Format("SceneBController.Shutdown - [Then] complete. FriendlyName={0}", FriendlyName));
				return "Shutdown";

			}, delay);
		}
	}

	public class MockScene
	{
		public string SceneName { get; set; }
		public List<string> GameObjects { get; set; }
	}
}