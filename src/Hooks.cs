using System;
using System.Collections;
using Harmony;
using MelonLoader;
using UnityEngine;

namespace SteamFrameMod
{
    public partial class SteamFrame : MelonMod
    {
        public static MenuState.State menuState;

        [HarmonyPatch(typeof(MenuState), "SetState", new Type[]
        {
            typeof(MenuState.State)
        })]
        public static class SetStatePatch
        {
            public static void Postfix(
                MenuState __instance,
                MenuState.State state
                )
            {
                bool songStarting = state == MenuState.State.Launched && menuState != MenuState.State.Launched;
                bool leavingSong = menuState == MenuState.State.Launched && state != MenuState.State.Launched;

                // Leaving gameplay without SongEnd firing (quit from the pause menu). No-op if
                // the song already ended normally, since SongEnd closes the measurement first.
                if (leavingSong)
                    FrameStats.End("quit");

                menuState = state;

                // Arenas can load their own cameras and post-processing, so re-apply on every
                // state change. Cheap: a handful of camera and particle lookups.
                RenderTweaks.Apply("menu state " + state);

                if (songStarting)
                {
                    FrameStats.Begin();
                    MelonCoroutines.Start(AfterSongStart());
                }
            }
        }

        /// <summary>Fires once, exactly when the song ends (completed or failed).</summary>
        [HarmonyPatch(typeof(SongEnd), "OnSongOver", new Type[] { typeof(bool) })]
        public static class SongEndOnSongOverPatch
        {
            public static void Postfix(SongEnd __instance, bool failed)
            {
                FrameStats.End(failed ? "failed" : "completed");
            }
        }

        /// <summary>Restart from the pause or results screen: close the old run, start a new one.</summary>
        [HarmonyPatch(typeof(InGameUI), "Restart")]
        public static class InGameUIRestartPatch
        {
            public static void Postfix(InGameUI __instance)
            {
                FrameStats.End("restarted");
                FrameStats.Begin();
            }
        }

        /// <summary>
        /// Re-applies once the arena has had a moment to finish setting itself up, then logs the
        /// heaviest particle systems once they have filled up, to find candidates on other arenas.
        /// </summary>
        private static IEnumerator AfterSongStart()
        {
            yield return new WaitForSeconds(1f);
            RenderTweaks.Apply("song start (delayed)");

            yield return new WaitForSeconds(9f);
            if (menuState == MenuState.State.Launched)
                RenderTweaks.LogHeavyParticleSystems();
        }
    }
}
