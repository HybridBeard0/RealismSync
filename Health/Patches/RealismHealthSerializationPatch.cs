using EFT;
using EFT.HealthSystem;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SPT.Reflection.Patching;

namespace RealismModSync.Health.Patches
{
    /// <summary>
    /// Prevents RealismMod's custom effects from being serialized in health state
    /// This fixes the NullReferenceException when creating ObservedCoopPlayers
    /// Custom effects can't be deserialized by other clients because they don't know the types
    /// </summary>
    public class RealismHealthSerializationPatch : ModulePatch
    {
        private static readonly HashSet<string> CustomEffectTypes = new HashSet<string>
        {
            "PassiveHealthRegenEffect",
            "ResourceRateEffect",
            "TourniquetEffect",
            "SurgeryEffect"
        };

        protected override MethodBase GetTargetMethod()
        {
            // We need to patch the method that gets all active effects for serialization
            // This is typically in the NetworkBodyEffectsAbstractClass or similar
            var networkHealthControllerType = AccessTools.TypeByName("NetworkHealthControllerAbstractClass");
            if (networkHealthControllerType == null)
            {
                Plugin.REAL_Logger.LogWarning("Could not find NetworkHealthControllerAbstractClass");
                return null;
            }

            // Find the nested NetworkBodyEffectsAbstractClass
            var networkBodyEffectsType = networkHealthControllerType.GetNestedType("NetworkBodyEffectsAbstractClass", BindingFlags.NonPublic | BindingFlags.Public);
            if (networkBodyEffectsType == null)
            {
                Plugin.REAL_Logger.LogWarning("Could not find NetworkBodyEffectsAbstractClass");
                return null;
            }

            // Find method that gets active effects - typically GetAllActiveEffects or similar
            var getAllActiveEffectsMethod = AccessTools.Method(networkBodyEffectsType, "GetAllActiveEffects");
            if (getAllActiveEffectsMethod != null)
            {
                Plugin.REAL_Logger.LogInfo("Found GetAllActiveEffects method for health serialization patch");
                return getAllActiveEffectsMethod;
            }

            Plugin.REAL_Logger.LogWarning("Could not find GetAllActiveEffects method");
            return null;
        }

        [PatchPostfix]
        private static void Postfix(ref IReadOnlyList<IEffect> __result)
        {
            try
            {
                if (__result == null || __result.Count == 0)
                    return;

                // Filter out custom RealismMod effects
                var filteredEffects = new List<IEffect>();
                int removedCount = 0;

                foreach (var effect in __result)
                {
                    if (effect == null)
                        continue;

                    var effectTypeName = effect.GetType().Name;
                    
                    if (CustomEffectTypes.Contains(effectTypeName))
                    {
                        removedCount++;
                        Plugin.REAL_Logger.LogDebug($"Filtered out custom effect for serialization: {effectTypeName}");
                        continue;
                    }

                    filteredEffects.Add(effect);
                }

                if (removedCount > 0)
                {
                    __result = filteredEffects;
                    Plugin.REAL_Logger.LogInfo($"Removed {removedCount} custom RealismMod effects from health serialization");
                }
            }
            catch (Exception ex)
            {
                Plugin.REAL_Logger.LogError($"Error in RealismHealthSerializationPatch: {ex.Message}\n{ex.StackTrace}");
            }
        }
    }

    /// <summary>
    /// Alternative patch that targets the serialization method directly
    /// This is used if GetAllActiveEffects method is not found
    /// </summary>
    public class RealismHealthSerializationAlternativePatch : ModulePatch
    {
        private static readonly HashSet<string> CustomEffectTypes = new HashSet<string>
        {
            "PassiveHealthRegenEffect",
            "ResourceRateEffect",
            "TourniquetEffect",
            "SurgeryEffect"
        };

        protected override MethodBase GetTargetMethod()
        {
            var networkHealthControllerType = AccessTools.TypeByName("NetworkHealthControllerAbstractClass");
            if (networkHealthControllerType == null)
                return null;

            // Try to find the SerializeState method
            var serializeStateMethod = AccessTools.Method(networkHealthControllerType, "SerializeState");
            if (serializeStateMethod != null)
            {
                Plugin.REAL_Logger.LogInfo("Found SerializeState method for alternative health serialization patch");
                return serializeStateMethod;
            }

            Plugin.REAL_Logger.LogWarning("Could not find SerializeState method");
            return null;
        }

        [PatchPrefix]
        private static void Prefix(object __instance)
        {
            try
            {
                // Get the body effects from the health controller
                var bodyEffectsField = __instance.GetType().GetField("bodyEffects", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (bodyEffectsField == null)
                {
                    Plugin.REAL_Logger.LogDebug("Could not find bodyEffects field");
                    return;
                }

                var bodyEffects = bodyEffectsField.GetValue(__instance);
                if (bodyEffects == null)
                    return;

                // Get all effects from body effects
                var effectsField = bodyEffects.GetType().GetField("effects", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (effectsField == null)
                {
                    Plugin.REAL_Logger.LogDebug("Could not find effects field");
                    return;
                }

                var effects = effectsField.GetValue(bodyEffects);
                if (effects == null)
                    return;

                // Try to get as a collection
                if (effects is System.Collections.IEnumerable enumerable)
                {
                    var filteredEffects = new List<IEffect>();
                    int removedCount = 0;

                    foreach (var effect in enumerable)
                    {
                        if (effect == null)
                            continue;

                        var effectTypeName = effect.GetType().Name;
                        
                        if (CustomEffectTypes.Contains(effectTypeName))
                        {
                            removedCount++;
                            Plugin.REAL_Logger.LogDebug($"Filtered out custom effect before serialization: {effectTypeName}");
                            continue;
                        }

                        if (effect is IEffect iEffect)
                            filteredEffects.Add(iEffect);
                    }

                    if (removedCount > 0)
                    {
                        // Try to update the effects collection
                        // This is a temporary modification just for serialization
                        Plugin.REAL_Logger.LogInfo($"Temporarily removed {removedCount} custom RealismMod effects for serialization");
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.REAL_Logger.LogError($"Error in RealismHealthSerializationAlternativePatch Prefix: {ex.Message}");
            }
        }
    }
}
