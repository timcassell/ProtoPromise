using System;
using System.Net.NetworkInformation;
using UnityEngine;
using System.Collections;
using uPromise;
using uPromise.Example.Http;
using uPromise.Example.Http.Promise;

public class PromiseHttpController : MonoBehaviour
{
	private UserDataService _userDataService;
	private uPromise.Example.Http.Traditional.UserDataService _userTraditionalDataService;

	void Awake()
	{
		_userDataService = new UserDataService();
		_userTraditionalDataService = new uPromise.Example.Http.Traditional.UserDataService();
	}

	void OnGUI()
	{

		if (GUILayout.Button("GetUser Promise"))
		{
			_userDataService.Get(300)
				.Then(x => Debug.Log(string.Format("GetUser - x.id={0}", x.Id)));

		}
		if (GUILayout.Button("Get User Traditional"))
		{
			_userTraditionalDataService.Get(id: 2,
				onDone: x => Debug.Log(string.Format("User={0} Score={1}", x.Name, x.Score)),
				onFail: x => Debug.LogError("An error has occoured."));
			
		}

		if (GUILayout.Button("Auth Promise Chaining"))
		{
			AuthPromiseChaining();
		}
		if (GUILayout.Button("Auth Traditional"))
		{
			AuthTraditional(null, null);
		}
	}

	private Promise<string> AuthPromiseChaining()
	{
		var authModel = new Auth { Username = "xyz", Password = "123" };
		return _userDataService.Auth(authModel)
					.Then(authResponse => _userDataService.Get(authResponse.UserId))
					.Then(user => user.Level)
					.Then(x => Debug.Log(string.Format("PromiseHttpController.OnGUI - [Thenx3] User Complete - LoadLevel={0}", x)))
					.Fail(x => Debug.LogError("An error has occoured."));

		//NOTE: Instead of the above you can write it as below as well.

		//return _userDataService.Auth(authModel)
		//			.Then(delegate(AuthResponse authResponse)
		//				{
		//					Debug.Log(string.Format("PromiseHttpController.OnGUI - [Then] authResponse={0}", authResponse));
		//					return _userDataService.Get(authResponse.UserId);
		//				})
		//			.Then(delegate(User user)
		//				{
		//					Debug.Log(string.Format("PromiseHttpController.OnGUI - [Thenx2] User Result. user={0}", user));
		//					return user.Level;
		//				})
		//			.Then(delegate(string level)
		//				  {
		//					  Debug.Log(string.Format("PromiseHttpController.OnGUI - [Thenx3] User Complete - LoadLevel={0}", level));
		//					  //TODO. LoadLevel...
		//				  })
		//			.Fail(x => Debug.LogError("An error has occoured."));
	}

	private void AuthTraditional(Action<string> onComplete, Action<object> onFail)
	{
		var authModel = new Auth { Username = "xyz", Password = "123" };
		_userTraditionalDataService.Auth(authModel,
			onDone: delegate(AuthResponse authResponse)
					{
						_userTraditionalDataService.Get(authResponse.UserId,
							onDone: delegate(User user)
									{
										if (onComplete != null)
											onComplete(user.Level);
									},
							onFail: error =>
									{
										// NOTE: If you don't handle this manually it won't bubble up the error!
										if (onFail != null)
											onFail(error);
									});

					},
			onFail: error =>
			{
				// NOTE: If you don't handle this manually it won't bubble up the error!
				if (onFail != null)
					onFail(error);
			});
	}
}

namespace uPromise.Example.Http.Promise
{
	public class UserDataService
	{
		public Promise<AuthResponse> Auth(Auth model)
		{
			string url = string.Format("http://my.api.com/api/auth");
			return FakeHttpClient.Post<Auth, AuthResponse>(url, model, fakeResponse: () => new AuthResponse { UserId = 150, DisplayName = "Chiko", Token = "Chiko::150" })
				.Then(delegate(AuthResponse x)
				{
					AuthResponse auth = x;
					return auth; // By doing this you will be returning - Promise<User>
				});//NOTE: USE THE ONE BELOW INSTEAD! This is just for showing purposes only.
			//.Then(x => x.Content); 
		}

		public Promise<User> Get(int id)
		{
			string url = string.Format("http://my.api.com/api/user/{0}", id);

			return FakeHttpClient.Get<User>(url, fakeResponse: () => new User { Id = 150, Name = "Stephen", Score = 1337, Level = "LevelAwesome" })
				.Then(delegate(User x)
				{
					User user = x;
					//throw new PingException("TROLOL");
					return user; // By doing this you will be returning - Promise<User>
				});//NOTE: USE THE ONE BELOW INSTEAD! This is just for showing purposes only.
			//.Then(x => x.Content); 
		}
	}

	public static class FakeHttpClient
	{
		public static Promise<T> Get<T>(string url, Func<T> fakeResponse)
		{
			return PromiseFactory.StartNew(fakeResponse, 2.5f);
		}

		public static Promise<TResult> Post<T, TResult>(string url, T input, Func<TResult> fakeResponse)
		{
			return PromiseFactory.StartNew(fakeResponse, 2.5f);
		}
	}

}

namespace uPromise.Example.Http.Traditional
{
	public class UserDataService
	{
		public void Auth(Auth auth, Action<AuthResponse> onDone, Action<object> onFail)
		{
			string url = string.Format("http://my.api.com/api/auth");

			FakeHttpClient.Post<Auth, AuthResponse>(url, auth, onDone, onFail);
		}

		// NOTE: If you don't always include OnDone, OnFail in the method signitures the invoker (e.g. UI) can't handle them.
		public void Get(int id, Action<User> onDone, Action<object> onFail)
		{
			string url = string.Format("http://my.api.com/api/user/{0}", id);

			FakeHttpClient.Get<User>(url, onDone, onFail);
		}
	}

	public class FakeHttpClient
	{
		public static void Get<T>(string url, Action<T> onDone, Action<object> onFail)
		{
			// Not important...

			//THE BELOW IS JUST FOR COMPILATION. IT ISNT A REAL IMPLEMENTATION!
			bool isSuccess = true;
			if (isSuccess)
				onDone(default(T));
			else
				onFail(default(T));
		}

		public static void Post<T, TResult>(string url, T input, Action<TResult> onDone, Action<object> onFail)
		{
			// Not important...

			//THE BELOW IS JUST FOR COMPILATION. IT ISNT A REAL IMPLEMENTATION!
			bool isSuccess = true;
			if (isSuccess)
				onDone(default(TResult));
			else
				onFail(default(T));
		}
	}

}

namespace uPromise.Example.Http
{
	public class User
	{
		public int Id { get; set; }
		public string Name { get; set; }
		public int Score { get; set; }
		public string Level { get; set; }
	}

	public class Auth
	{
		public string Username { get; set; }
		public string Password { get; set; }
	}

	public class AuthResponse
	{
		public string Token { get; set; }
		public int UserId { get; set; }
		public string DisplayName { get; set; }
	}
}