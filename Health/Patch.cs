using HarmonyLib;

namespace RealismModSync.Health
{
    public static class Patch
    {
        private static Harmony _harmony;

        public static void Awake()
        {
            _harmony = new Harmony("RealismModSync.Health");

            _harmony.PatchAll(typeof(Patches.RealismHealthControllerUpdatePatch));

            Plugin.REAL_Logger.LogInfo("Health patches applied");

            // Apply medical sync patches
            Patches.RealismMedicalSyncPatches.ApplyPatches();
        }
    }
}
