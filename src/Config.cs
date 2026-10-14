using MelonLoader;

namespace SteamFrameMod
{
    /// <summary>
    /// All settings, in ModSettings format: ints and floats need [Min,Max,Step,Default] in the
    /// description to show up in game, and "[Header]" string prefs become dividers.
    /// Defaults are the lightest settings (everything on, MSAA off), except debug logs.
    /// Descriptions of int and float prefs must not contain commas outside the brackets:
    /// ModSettings splits the whole description on '[', ',' and ']' to read the range.
    /// </summary>
    internal static class Config
    {
        public const string Category = "SteamFrame";

        public static string renderingHeader = "[Header]Rendering";
        public static int MsaaLevel;           // 0 = off, 2, 4 (game default 4)
        public static bool DisableHDR;

        public static string postProcessingHeader = "[Header]Post-processing";
        public static bool DisablePostProcessing;
        public static bool DisableBloom;
        public static bool DisableColorGrading;
        public static bool UsePS4Profile;

        public static string environmentHeader = "[Header]Environment";
        public static bool HideAmbientParticles;

        public static string debugHeader = "[Header]Debug";
        public static bool DebugLogs;

        public static void RegisterConfig()
        {
            MelonPrefs.RegisterString(Category, nameof(renderingHeader), "", "[Header]Rendering");
            MelonPrefs.RegisterInt(Category, nameof(MsaaLevel), 0, "Anti-aliasing (MSAA) samples for the headset view. 0 is off and 4 is the game default [0,4,2,0] {G}");
            MelonPrefs.RegisterBool(Category, nameof(DisableHDR), true, "Renders the headset view without HDR. Cheaper, but bloom looks flatter");

            MelonPrefs.RegisterString(Category, nameof(postProcessingHeader), "", "[Header]Post-processing");
            MelonPrefs.RegisterBool(Category, nameof(DisablePostProcessing), true, "Turns off all post-processing on the headset view (bloom and color grading)");
            MelonPrefs.RegisterBool(Category, nameof(DisableBloom), true, "Turns off bloom only");
            MelonPrefs.RegisterBool(Category, nameof(DisableColorGrading), true, "Turns off color grading only");
            MelonPrefs.RegisterBool(Category, nameof(UsePS4Profile), true, "Uses the game's leftover PS4 post-processing profile instead of the PC one");

            MelonPrefs.RegisterString(Category, nameof(environmentHeader), "", "[Header]Environment");
            MelonPrefs.RegisterBool(Category, nameof(HideAmbientParticles), true, "Hides large ambient particle fields (dust, star fields) in arenas");

            MelonPrefs.RegisterString(Category, nameof(debugHeader), "", "[Header]Debug");
            MelonPrefs.RegisterBool(Category, nameof(DebugLogs), false, "Writes the mod's logs, including per-song frame time stats, to the MelonLoader console/log file");

            OnModSettingsApplied();
        }

        public static void OnModSettingsApplied()
        {
            MsaaLevel = MelonPrefs.GetInt(Category, nameof(MsaaLevel));
            DisableHDR = MelonPrefs.GetBool(Category, nameof(DisableHDR));

            DisablePostProcessing = MelonPrefs.GetBool(Category, nameof(DisablePostProcessing));
            DisableBloom = MelonPrefs.GetBool(Category, nameof(DisableBloom));
            DisableColorGrading = MelonPrefs.GetBool(Category, nameof(DisableColorGrading));
            UsePS4Profile = MelonPrefs.GetBool(Category, nameof(UsePS4Profile));

            HideAmbientParticles = MelonPrefs.GetBool(Category, nameof(HideAmbientParticles));

            DebugLogs = MelonPrefs.GetBool(Category, nameof(DebugLogs));
        }

        /// <summary>
        /// MSAA only accepts 0, 2, 4 or 8 samples (1 is treated as off).
        /// Snaps whatever is in the config file to the nearest valid value we offer.
        /// </summary>
        public static int SanitizedMsaa()
        {
            if (MsaaLevel <= 1) return 0;
            if (MsaaLevel <= 3) return 2;
            return 4;
        }

        /// <summary>One-line summary of the active settings, attached to every frame time log.</summary>
        public static string Summary()
        {
            return "MSAA=" + SanitizedMsaa()
                + " HDR=" + (DisableHDR ? "off" : "on")
                + " PostProc=" + (DisablePostProcessing ? "off" : "on")
                + " Bloom=" + (DisableBloom ? "off" : "on")
                + " ColorGrading=" + (DisableColorGrading ? "off" : "on")
                + " PS4Profile=" + (UsePS4Profile ? "on" : "off")
                + " AmbientParticles=" + (HideAmbientParticles ? "hidden" : "shown");
        }
    }
}