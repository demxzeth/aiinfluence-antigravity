using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using AIInfluenceAntigravity.Infra;

namespace AIInfluenceAntigravity.Hook
{
    public static class Engine
    {
        private const float PrimingIntervalSeconds = 1f;
        private const int MaxPrimingAttempts = 60;

        private static Harmony _harmony;
        private static float _nextAttemptIn;
        private static int _attempts;
        private static bool _installed;

        public static bool Installed => _installed;
        public static int Attempts => _attempts;

        public static void Create()
        {
            if (_harmony == null)
            {
                _harmony = new Harmony(ModInfo.HarmonyId);
            }
        }

        public static void PrimeStep(float deltaTime)
        {
            if (_harmony == null || _installed) return;
            if (_attempts >= MaxPrimingAttempts) return;

            _nextAttemptIn -= deltaTime;
            if (_nextAttemptIn > 0f) return;

            _nextAttemptIn = PrimingIntervalSeconds;
            _attempts++;

            try
            {
                Installer.Install(_harmony);
                if (Installer.PatchedCount > 0)
                {
                    _installed = true;
                    LogSink.Always("hooks installed: " + Installer.PatchedCount + " target method(s).");
                }
                else
                {
                    LogSink.Info("priming attempt " + _attempts + ": target not ready yet.");
                }
            }
            catch (Exception ex)
            {
                LogSink.Error("hook installation failed.", ex);
                _installed = true;
            }
        }

        public static void TearDown()
        {
            if (_harmony != null)
            {
                _harmony.UnpatchAll(ModInfo.HarmonyId);
                _harmony = null;
            }

            Installer.Reset();
            _installed = false;
            _attempts = 0;
            _nextAttemptIn = 0f;
        }
    }

    internal static class Installer
    {
        public static int PatchedCount { get; private set; }

        public static void Install(Harmony harmony)
        {
            PatchedCount = 0;

            IReadOnlyList<HookTarget> targets = TargetScanner.CaptureTargets();
            MethodInfo dispatch = typeof(Dispatch).GetMethod(
                nameof(Dispatch.Prefix), BindingFlags.Public | BindingFlags.Static);

            foreach (HookTarget target in targets)
            {
                harmony.Patch(target.Method, prefix: new HarmonyMethod(dispatch));
                PatchedCount++;
            }
        }

        public static void Reset()
        {
            PatchedCount = 0;
            TargetScanner.Reset();
        }
    }
}
