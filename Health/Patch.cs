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

            // Apply Fika health sync compatibility patch to prevent RealismMod effects from causing errors
            try
            {
                new Patches.FikaHealthSyncCompatibilityPatch().Enable();
                Plugin.REAL_Logger.LogInfo("Fika health sync compatibility patch applied");
            }
            catch (System.Exception ex)
            {
                Plugin.REAL_Logger.LogError($"Failed to apply Fika health sync compatibility patch: {ex.Message}");
            }

            // Apply RealismMod custom effect tracking patch
            try
            {
                new Patches.RealismCustomEffectPatch().Enable();
                Plugin.REAL_Logger.LogInfo("RealismMod custom effect tracking patch applied");
            }
            catch (System.Exception ex)
            {
                Plugin.REAL_Logger.LogError($"Failed to apply RealismMod custom effect patch: {ex.Message}");
            }

            // Apply resource drain safety patch to prevent extraction errors
            try
            {
                new Patches.RealismResourceDrainSafetyPatch().Enable();
                Plugin.REAL_Logger.LogInfo("RealismMod resource drain safety patch applied");
            }
            catch (System.Exception ex)
            {
                Plugin.REAL_Logger.LogError($"Failed to apply RealismMod resource drain safety patch: {ex.Message}");
            }

            // Apply inventory sync safety patches to prevent bot inventory errors
            Plugin.REAL_Logger.LogInfo("Applying inventory sync safety patches...");
            Patches.InventorySyncSafetyPatches.ApplyPatches();
        }
    }
}
