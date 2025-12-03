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

            // Apply health serialization patch to filter out custom effects FIRST
            // This prevents NullReferenceException when creating ObservedCoopPlayers
            try
            {
                // Try primary patch first
                try
                {
                    new Patches.RealismHealthSerializationPatch().Enable();
                    Plugin.REAL_Logger.LogInfo("RealismMod health serialization patch applied (primary)");
                }
                catch (System.Exception primaryEx)
                {
                    Plugin.REAL_Logger.LogWarning($"Primary health serialization patch failed: {primaryEx.Message}");
                    
                    // Try alternative patch
                    try
                    {
                        new Patches.RealismHealthSerializationAlternativePatch().Enable();
                        Plugin.REAL_Logger.LogInfo("RealismMod health serialization patch applied (alternative)");
                    }
                    catch (System.Exception altEx)
                    {
                        Plugin.REAL_Logger.LogWarning($"Alternative health serialization patch failed: {altEx.Message}");
                        Plugin.REAL_Logger.LogWarning("Could not apply health serialization patch - custom effects may cause issues in multiplayer");
                    }
                }
            }
            catch (System.Exception ex)
            {
                Plugin.REAL_Logger.LogError($"Failed to apply RealismMod health serialization patches: {ex.Message}");
            }

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
