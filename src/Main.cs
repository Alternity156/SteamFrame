using MelonLoader;

namespace SteamFrameMod
{
    public partial class SteamFrame : MelonMod
    {
        public static class BuildInfo
        {
            public const string Name = "SteamFrame";  // Name of the Mod.  (MUST BE SET)
            public const string Author = "Alternity"; // Author of the Mod.  (Set as null if none)
            public const string Company = null; // Company that made the Mod.  (Set as null if none)
            public const string Version = "0.1.0"; // Version of the Mod.  (MUST BE SET)
            public const string DownloadLink = null; // Download Link for the Mod.  (Set as null if none)
        }

        public override void OnApplicationStart()
        {
            Config.RegisterConfig();
            DebugLog.Log("[SteamFrame] Loaded. " + Config.Summary());
        }

        /// <summary>
        /// Called by ModSettings every time a value changes, so settings apply live,
        /// including mid-song.
        /// </summary>
        public override void OnModSettingsApplied()
        {
            Config.OnModSettingsApplied();
            RenderTweaks.Apply("settings changed");
        }

        public override void OnUpdate()
        {
            FrameStats.Tick();
        }
    }
}
