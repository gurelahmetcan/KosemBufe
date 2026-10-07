using System.Diagnostics;

namespace Bufe
{
    /// <summary>
    /// Debug.Log that is stripped from release builds, arguments included, so
    /// interpolated messages cost nothing there. Warnings and errors stay on
    /// UnityEngine.Debug since those should reach a player log.
    /// </summary>
    public static class Log
    {
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Info(object message, UnityEngine.Object context = null)
            => UnityEngine.Debug.Log(message, context);
    }
}
