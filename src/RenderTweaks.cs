using System;
using System.Collections.Generic;
using MelonLoader;
using UnhollowerRuntimeLib;
using UnityEngine;
using UnityEngine.PostProcessing;

namespace SteamFrameMod
{
    /// <summary>
    /// Applies the rendering settings to the headset camera(s), the post-processing profile and
    /// the arena's particle fields. Safe to call any number of times: the game's original values
    /// are captured the first time something is touched, so turning a setting back off restores
    /// exactly what the game had.
    /// </summary>
    internal static class RenderTweaks
    {
        // Particle systems hidden by HideAmbientParticles, matched by exact GameObject name.
        // From the Env5 arena dump; LogHeavyParticleSystems finds candidates on other arenas.
        private static readonly string[] AmbientParticleNames =
        {
            "FxAmbientDust",
            "star_particles",
        };

        // Systems at or above this many live particles get logged as candidates.
        private const int HeavyParticleThreshold = 300;

        // ── Originals, keyed by Il2Cpp object pointer ──
        private static readonly Dictionary<IntPtr, bool> originalCameraMsaa = new Dictionary<IntPtr, bool>();
        private static readonly Dictionary<IntPtr, bool> originalCameraHdr = new Dictionary<IntPtr, bool>();
        private static readonly Dictionary<IntPtr, bool> originalBehaviourEnabled = new Dictionary<IntPtr, bool>();
        private static readonly Dictionary<IntPtr, PostProcessingProfile> originalBehaviourProfile = new Dictionary<IntPtr, PostProcessingProfile>();
        private static readonly Dictionary<IntPtr, bool> originalBloom = new Dictionary<IntPtr, bool>();
        private static readonly Dictionary<IntPtr, bool> originalColorGrading = new Dictionary<IntPtr, bool>();
        private static readonly List<GameObject> hiddenParticleObjects = new List<GameObject>();

        public static void Apply(string reason)
        {
            try { ApplyQualityMsaa(); }
            catch (Exception e) { MelonLogger.LogError("[SteamFrame] MSAA: " + e); }

            List<Camera> cameras = null;
            try { cameras = GetHeadsetCameras(); }
            catch (Exception e) { MelonLogger.LogError("[SteamFrame] Finding headset cameras: " + e); }

            if (cameras != null)
            {
                foreach (Camera cam in cameras)
                {
                    try { ApplyCamera(cam); }
                    catch (Exception e) { MelonLogger.LogError("[SteamFrame] Camera " + cam.name + ": " + e); }

                    try { ApplyPostProcessing(cam); }
                    catch (Exception e) { MelonLogger.LogError("[SteamFrame] Post-processing on " + cam.name + ": " + e); }
                }
            }

            try { ApplyAmbientParticles(); }
            catch (Exception e) { MelonLogger.LogError("[SteamFrame] Ambient particles: " + e); }

            DebugLog.Log("[SteamFrame] Applied (" + reason + "): " + Config.Summary()
                + " | headset cameras=" + (cameras == null ? 0 : cameras.Count));
        }

        // ───────────────────────── Cameras ─────────────────────────

        /// <summary>
        /// The cameras rendering to the headset: enabled, active and with a stereo target.
        /// The spectator camera targets no eye, so it's left alone (the game's own spectator
        /// setting already deactivates it).
        /// </summary>
        private static List<Camera> GetHeadsetCameras()
        {
            var result = new List<Camera>();
            var objs = Resources.FindObjectsOfTypeAll(Il2CppType.Of<Camera>());
            for (int i = 0; i < objs.Length; i++)
            {
                Camera cam = objs[i].TryCast<Camera>();
                if (cam == null || !cam.gameObject.scene.IsValid()) continue;
                if (!cam.enabled || !cam.gameObject.activeInHierarchy) continue;
                if (cam.stereoTargetEye == StereoTargetEyeMask.None) continue;
                result.Add(cam);
            }
            return result;
        }

        private static void ApplyQualityMsaa()
        {
            int wanted = Config.SanitizedMsaa();
            if (QualitySettings.antiAliasing != wanted)
            {
                QualitySettings.antiAliasing = wanted;
                DebugLog.Log("[SteamFrame] QualitySettings.antiAliasing = " + wanted);
            }
        }

        private static void ApplyCamera(Camera cam)
        {
            IntPtr ptr = cam.Pointer;

            if (!originalCameraMsaa.ContainsKey(ptr)) originalCameraMsaa[ptr] = cam.allowMSAA;
            if (!originalCameraHdr.ContainsKey(ptr)) originalCameraHdr[ptr] = cam.allowHDR;

            bool wantMsaa = Config.SanitizedMsaa() > 0 && originalCameraMsaa[ptr];
            bool wantHdr = !Config.DisableHDR && originalCameraHdr[ptr];

            if (cam.allowMSAA != wantMsaa) cam.allowMSAA = wantMsaa;
            if (cam.allowHDR != wantHdr) cam.allowHDR = wantHdr;
        }

        // ───────────────────────── Post-processing ─────────────────────────

        private static void ApplyPostProcessing(Camera cam)
        {
            PostProcessingBehaviour behaviour = cam.GetComponent<PostProcessingBehaviour>();
            if (behaviour == null) return;
            IntPtr ptr = behaviour.Pointer;

            // Whole stack on/off.
            if (!originalBehaviourEnabled.ContainsKey(ptr)) originalBehaviourEnabled[ptr] = behaviour.enabled;
            bool wantEnabled = !Config.DisablePostProcessing && originalBehaviourEnabled[ptr];
            if (behaviour.enabled != wantEnabled) behaviour.enabled = wantEnabled;

            // PC profile or the leftover PS4 one. The PC profile is remembered whenever the
            // camera holds something other than the PS4 profile, so arena changes are followed.
            PostProcessingProfile ps4Profile = null;
            MobilePostprocDisabler disabler = cam.GetComponent<MobilePostprocDisabler>();
            if (disabler != null) ps4Profile = disabler.PS4profile;

            PostProcessingProfile current = behaviour.profile;
            if (current != null && (ps4Profile == null || current.Pointer != ps4Profile.Pointer))
                originalBehaviourProfile[ptr] = current;

            PostProcessingProfile pcProfile;
            originalBehaviourProfile.TryGetValue(ptr, out pcProfile);

            PostProcessingProfile wanted = (Config.UsePS4Profile && ps4Profile != null) ? ps4Profile : pcProfile;
            if (wanted != null && (current == null || current.Pointer != wanted.Pointer))
            {
                behaviour.profile = wanted;
                DebugLog.Log("[SteamFrame] " + cam.name + " post-processing profile = " + wanted.name);
            }
            if (Config.UsePS4Profile && ps4Profile == null)
                DebugLog.Log("[SteamFrame] UsePS4Profile is on, but " + cam.name + " has no PS4 profile");

            // Individual effects on whichever profile is now active. Profiles are shared assets,
            // so this also affects the spectator camera, which uses the same one.
            PostProcessingProfile active = behaviour.profile;
            if (active == null) return;
            IntPtr profilePtr = active.Pointer;

            if (!originalBloom.ContainsKey(profilePtr)) originalBloom[profilePtr] = active.bloom.enabled;
            if (!originalColorGrading.ContainsKey(profilePtr)) originalColorGrading[profilePtr] = active.colorGrading.enabled;

            bool wantBloom = !Config.DisableBloom && originalBloom[profilePtr];
            bool wantColorGrading = !Config.DisableColorGrading && originalColorGrading[profilePtr];

            if (active.bloom.enabled != wantBloom) active.bloom.enabled = wantBloom;
            if (active.colorGrading.enabled != wantColorGrading) active.colorGrading.enabled = wantColorGrading;
        }

        // ───────────────────────── Particles ─────────────────────────

        private static void ApplyAmbientParticles()
        {
            if (!Config.HideAmbientParticles)
            {
                RestoreHiddenParticles();
                return;
            }

            var objs = Resources.FindObjectsOfTypeAll(Il2CppType.Of<ParticleSystem>());
            for (int i = 0; i < objs.Length; i++)
            {
                ParticleSystem ps = objs[i].TryCast<ParticleSystem>();
                if (ps == null || !ps.gameObject.scene.IsValid()) continue;

                GameObject go = ps.gameObject;
                if (!go.activeSelf || !IsAmbientParticleName(go.name)) continue;

                go.SetActive(false);
                hiddenParticleObjects.Add(go);
                DebugLog.Log("[SteamFrame] Hid particle system " + GetPath(go.transform));
            }
        }

        private static void RestoreHiddenParticles()
        {
            if (hiddenParticleObjects.Count == 0) return;

            foreach (GameObject go in hiddenParticleObjects)
            {
                try
                {
                    if (go != null) go.SetActive(true);
                }
                catch (Exception)
                {
                    // Object was unloaded with its arena; nothing to restore.
                }
            }
            DebugLog.Log("[SteamFrame] Restored " + hiddenParticleObjects.Count + " hidden particle systems");
            hiddenParticleObjects.Clear();
        }

        private static bool IsAmbientParticleName(string name)
        {
            foreach (string n in AmbientParticleNames)
                if (name == n) return true;
            return false;
        }

        /// <summary>
        /// Logs every active particle system with a lot of live particles, so the hide list can
        /// be extended for other arenas. Debug logs only.
        /// </summary>
        public static void LogHeavyParticleSystems()
        {
            if (!Config.DebugLogs) return;

            try
            {
                var objs = Resources.FindObjectsOfTypeAll(Il2CppType.Of<ParticleSystem>());
                int found = 0;
                for (int i = 0; i < objs.Length; i++)
                {
                    ParticleSystem ps = objs[i].TryCast<ParticleSystem>();
                    if (ps == null || !ps.gameObject.scene.IsValid() || !ps.gameObject.activeInHierarchy) continue;

                    int count = ps.particleCount;
                    if (count < HeavyParticleThreshold) continue;

                    found++;
                    DebugLog.Log("[SteamFrame] Heavy particle system: " + GetPath(ps.transform) + " | particles=" + count);
                }
                DebugLog.Log("[SteamFrame] Heavy particle systems (>= " + HeavyParticleThreshold + " particles): " + found);
            }
            catch (Exception e)
            {
                MelonLogger.LogError("[SteamFrame] Logging heavy particle systems: " + e);
            }
        }

        private static string GetPath(Transform t)
        {
            string path = t.name;
            while (t.parent != null)
            {
                t = t.parent;
                path = t.name + "/" + path;
            }
            return path;
        }
    }
}
