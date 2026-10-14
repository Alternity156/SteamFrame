using MelonLoader;

namespace SteamFrameMod
{
    /// <summary>
    /// Gate for all of the mod's regular logging. Every call site goes through here instead of
    /// MelonLogger.Log directly, so nothing is written unless Config.DebugLogs is enabled
    /// (Mod Settings > SteamFrame > Debug, off by default). MelonLogger.LogError is not routed
    /// through this and always logs.
    /// </summary>
    internal static class DebugLog
    {
        public static void Log(string message)
        {
            if (!Config.DebugLogs) return;
            MelonLogger.Log(message);
        }
    }
}
