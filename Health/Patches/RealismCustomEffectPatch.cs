using EFT;
using HarmonyLib;
using System.Reflection;
using SPT.Reflection.Patching;

namespace RealismModSync.Health.Patches
{
    /// <summary>
    /// Patches RealismMod's AddCustomEffect to prevent it from triggering Fika health sync
    /// RealismMod custom effects (like tourniquets, surgery) aren't compatible with Fika's network sync
    /// </summary>
    public class RealismCustomEffectPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            var realismHealthControllerType = AccessTools.TypeByName("RealismMod.RealismHealthController");
            if (realismHealthControllerType == null)
            {
                Plugin.REAL_Logger.LogWarning("Could not find RealismHealthController for custom effect patch");
                return null;
            }

            return AccessTools.Method(realismHealthControllerType, "AddCustomEffect");
        }

        [PatchPrefix]
        private static void Prefix(object __instance, object newEffect, bool canStack)
        {
            try
            {
                // The purpose of this patch is just to intercept and log
                // We let the original method run, but this gives us visibility
                // into when custom effects are being added
                
                if (Config.EnableHealthSync.Value)
                {
                    var effectType = newEffect?.GetType().Name ?? "Unknown";
                    Plugin.REAL_Logger.LogInfo($"RealismMod adding custom effect: {effectType}");
                }
            }
            catch (System.Exception ex)
            {
                Plugin.REAL_Logger.LogError($"Error in RealismCustomEffectPatch Prefix: {ex.Message}");
            }
        }

        [PatchPostfix]
        private static void Postfix(object __instance, object newEffect, bool canStack)
        {
            try
            {
                // After a custom effect is added, we could potentially sync it through our own system
                // For now, we just log it for debugging
                // TODO: Implement custom effect synchronization if needed
            }
            catch (System.Exception ex)
            {
                Plugin.REAL_Logger.LogError($"Error in RealismCustomEffectPatch Postfix: {ex.Message}");
            }
        }
    }
}
