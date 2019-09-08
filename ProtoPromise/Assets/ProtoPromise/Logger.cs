using System;
using UnityEngine;

namespace Proto
{
	/// <summary>
	/// Assign your own delegates to use your own logger instead of Unity's default.
	/// </summary>
	public static class Logger
	{
		public static Action<string> LogWarning = Debug.LogWarning;

		public static Action<string> LogError = Debug.LogError;

		public static Action<Exception> LogException = Debug.LogException;
	}
}