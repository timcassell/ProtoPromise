using System;

namespace Proto
{
    /// <summary>
    /// Assign your own delegate to use your own logger instead of Unity's default.
    /// </summary>
    [System.Diagnostics.DebuggerNonUserCode]
    public static class Logger
    {
        public static Action<string> logWarning = UnityEngine.Debug.LogWarning;

        [System.Diagnostics.DebuggerHidden]
        public static void LogWarning(string message)
        {
            var temp = logWarning;
            if (temp != null)
            {
                temp.Invoke(message);
            }
        }
    }
}