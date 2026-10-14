using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace SteamFrameMod
{
    /// <summary>
    /// Records frame times during a song and logs a summary when it ends, so settings can be
    /// compared on the Frame without UnityExplorer. Only records while debug logs are on.
    /// Skips the first few seconds (loading hitches) and paused frames.
    /// </summary>
    internal static class FrameStats
    {
        private const float WarmupSeconds = 3f;

        // Missed frames are judged against the 72fps target. A frame counts as missed when it
        // takes more than 20% over budget, so normal timing jitter isn't counted.
        private const float TargetFps = 72f;
        private const float MissedFrameTolerance = 1.2f;

        private static readonly List<float> frameMs = new List<float>(20000);
        private static bool recording;
        private static float startTime;

        public static void Begin()
        {
            frameMs.Clear();
            recording = true;
            startTime = Time.realtimeSinceStartup;
        }

        public static void Tick()
        {
            if (!recording || !Config.DebugLogs) return;
            if (Time.timeScale == 0f) return;
            if (Time.realtimeSinceStartup - startTime < WarmupSeconds) return;

            frameMs.Add(Time.unscaledDeltaTime * 1000f);
        }

        public static void End(string outcome)
        {
            if (!recording) return;
            recording = false;

            if (!Config.DebugLogs) return;

            if (frameMs.Count < 30)
            {
                DebugLog.Log("[SteamFrame] Frame times (" + outcome + "): too few frames recorded (" + frameMs.Count + ")");
                return;
            }

            var sorted = new List<float>(frameMs);
            sorted.Sort();

            float total = 0f;
            foreach (float ms in sorted) total += ms;
            float avg = total / sorted.Count;
            float median = sorted[sorted.Count / 2];
            float p99 = sorted[Math.Min(sorted.Count - 1, (int)(sorted.Count * 0.99f))];
            float max = sorted[sorted.Count - 1];

            float budgetMs = 1000f / TargetFps;
            int missed = 0;
            foreach (float ms in sorted)
                if (ms > budgetMs * MissedFrameTolerance) missed++;
            float missedPct = 100f * missed / sorted.Count;

            DebugLog.Log("[SteamFrame] Frame times (" + outcome + ", " + sorted.Count + " frames): "
                + "avg=" + avg.ToString("0.00") + "ms (" + (1000f / avg).ToString("0.0") + "fps)"
                + " median=" + median.ToString("0.00") + "ms"
                + " 1%worst=" + p99.ToString("0.00") + "ms"
                + " max=" + max.ToString("0.00") + "ms"
                + " missed72=" + missedPct.ToString("0.0") + "%");
            DebugLog.Log("[SteamFrame] Settings for that run: " + Config.Summary()
                + " | eyeTexScale=" + GetEyeTextureScale());
        }

        /// <summary>
        /// The game's supersampling value, so each log line records it too. Read by reflection
        /// because XRSettings lives in a different Unity module depending on the engine version,
        /// and this keeps the project free of that reference.
        /// </summary>
        private static string GetEyeTextureScale()
        {
            try
            {
                foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    Type type = asm.GetType("UnityEngine.XR.XRSettings");
                    if (type == null) continue;

                    PropertyInfo prop = type.GetProperty("eyeTextureResolutionScale", BindingFlags.Public | BindingFlags.Static);
                    if (prop == null) continue;

                    return ((float)prop.GetValue(null, null)).ToString("0.00");
                }
            }
            catch (Exception)
            {
            }
            return "?";
        }
    }
}
