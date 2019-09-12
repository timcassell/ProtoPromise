using System;
using UnityEngine;

namespace Proto
{
	/// <summary>
	/// Assign your own delegate to use your own logger instead of Unity's default.
	/// </summary>
	public static class Logger
	{
		public static Action<string> LogWarning = Debug.LogWarning;
	}
}