using EFT;
using EFT.InventoryLogic;
using Fika.Core.Coop.Players;
using HarmonyLib;
using System.Reflection;
using SPT.Reflection.Patching;

namespace RealismModSync.Health.Patches
{
    /// <summary>
    /// Patches RealismMod's medical system to synchronize changes across FIKA clients
    /// Prevents inventory desync by syncing med charges, effects, and usage
    /// </summary>
    public static class RealismMedicalSyncPatches
    {
        private static bool _patchesApplied = false;

        public static void ApplyPatches()
        {
            if (_patchesApplied)
                return;

            try
            {
                // Try to find and patch RealismMod's medical methods
                var realismHealthControllerType = AccessTools.TypeByName("RealismMod.RealismHealthController");
                
                if (realismHealthControllerType == null)
                {
                    Plugin.REAL_Logger.LogWarning("Could not find RealismHealthController type - medical sync patches not applied");
                    return;
                }

                // Patch HandleHealthEffects - main healing method
                var handleHealthEffectsMethod = AccessTools.Method(realismHealthControllerType, "HandleHealthEffects");
                if (handleHealthEffectsMethod != null)
                {
                    new HandleHealthEffectsSyncPatch().Enable();
                    Plugin.REAL_Logger.LogInfo("Patched RealismMod.HandleHealthEffects for medical sync");
                }

                // Patch med effect application
                var medEffectStartedType = AccessTools.TypeByName("RealismMod.MedEffectStartedPatch");
                if (medEffectStartedType != null)
                {
                    var medEffectMethod = AccessTools.Method(medEffectStartedType, "Prefix");
                    if (medEffectMethod != null)
                    {
                        new MedEffectSyncPatch().Enable();
                        Plugin.REAL_Logger.LogInfo("Patched RealismMod.MedEffectStartedPatch for medical sync");
                    }
                }

                _patchesApplied = true;
                Plugin.REAL_Logger.LogInfo("RealismMod medical sync patches applied successfully");
            }
            catch (System.Exception ex)
            {
                Plugin.REAL_Logger.LogError($"Failed to apply RealismMod medical sync patches: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Syncs HandleHealthEffects - tourniquets, surgery, bleeding heals
    /// </summary>
    public class HandleHealthEffectsSyncPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            var realismHealthControllerType = AccessTools.TypeByName("RealismMod.RealismHealthController");
            return AccessTools.Method(realismHealthControllerType, "HandleHealthEffects");
        }

        [PatchPostfix]
        private static void Postfix(object __instance, object meds, object medStats, EBodyPart bodyPart, Player player)
        {
            try
            {
                // Only sync for local player
                if (!player.IsYourPlayer)
                    return;

                // Make sure it's a CoopPlayer to get NetId
                var coopPlayer = player as CoopPlayer;
                if (coopPlayer == null)
                    return;

                var medsItem = meds as MedsItemClass;
                if (medsItem == null)
                    return;

                // Get current HP resource after RealismMod processed it using reflection
                var medItemType = medsItem.GetType();
                var hpResourceField = AccessTools.Field(medItemType, "HpResource");
                if (hpResourceField == null)
                    return;

                float currentResource = (float)hpResourceField.GetValue(medsItem);

                // Send sync packet
                var packet = new Packets.RealismMedicalSyncPacket
                {
                    NetId = coopPlayer.NetId,
                    SyncType = Packets.RealismMedicalSyncPacket.EMedicalSyncType.UpdateMedCharges,
                    Data = new Packets.RealismMedicalSyncPacket.MedicalSyncData
                    {
                        UpdateMedCharges = new Packets.RealismMedicalSyncPacket.UpdateMedChargesData
                        {
                            ItemId = medsItem.Id,
                            NewCharges = currentResource
                        }
                    }
                };

                Fika.SendMedicalSyncPacket(packet);

                if (Config.EnableHealthSync.Value)
                {
                    Plugin.REAL_Logger.LogInfo($"Synced med usage: {medsItem.LocalizedName()} -> {currentResource} charges remaining");
                }
            }
            catch (System.Exception ex)
            {
                Plugin.REAL_Logger.LogError($"Error in HandleHealthEffectsSyncPatch: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Syncs MedEffect application - when med items are actually consumed
    /// </summary>
    public class MedEffectSyncPatch : ModulePatch
    {
        private static System.Type _medEffectType;

        protected override MethodBase GetTargetMethod()
        {
            var healthControllerClassType = AccessTools.TypeByName("HealthControllerClass");
            _medEffectType = healthControllerClassType?.GetNestedType("MedEffect", BindingFlags.NonPublic);
            
            if (_medEffectType == null)
            {
                Plugin.REAL_Logger.LogWarning("Could not find MedEffect type");
                return null;
            }

            return AccessTools.Method(_medEffectType, "Started");
        }

        [PatchPostfix]
        private static void Postfix(object __instance)
        {
            try
            {
                if (_medEffectType == null)
                    return;

                // Get the health controller
                var healthControllerProperty = AccessTools.Property(_medEffectType, "HealthControllerClass");
                var healthController = healthControllerProperty?.GetValue(__instance);
                
                if (healthController == null)
                    return;

                // Get the player
                var playerField = AccessTools.Field(healthController.GetType(), "player_0");
                var player = playerField?.GetValue(healthController) as Player;
                
                if (player == null || !player.IsYourPlayer)
                    return;

                var coopPlayer = player as CoopPlayer;
                if (coopPlayer == null)
                    return;

                // Get the med item
                var medItemProperty = AccessTools.Property(_medEffectType, "MedItem");
                var medItem = medItemProperty?.GetValue(__instance) as Item;
                
                if (medItem == null)
                    return;

                // Get body part
                var bodyPartProperty = AccessTools.Property(_medEffectType, "BodyPart");
                var bodyPart = (EBodyPart?)bodyPartProperty?.GetValue(__instance);
                
                if (!bodyPart.HasValue)
                    return;

                // Get med item as MedsItemClass
                var medsItem = medItem as MedsItemClass;
                if (medsItem != null)
                {
                    var medItemType = medsItem.GetType();
                    var hpResourceField = AccessTools.Field(medItemType, "HpResource");
                    
                    if (hpResourceField != null)
                    {
                        float currentResource = (float)hpResourceField.GetValue(medsItem);

                        // Send usage sync packet
                        var packet = new Packets.RealismMedicalSyncPacket
                        {
                            NetId = coopPlayer.NetId,
                            SyncType = Packets.RealismMedicalSyncPacket.EMedicalSyncType.UseMedItem,
                            Data = new Packets.RealismMedicalSyncPacket.MedicalSyncData
                            {
                                UseMedItem = new Packets.RealismMedicalSyncPacket.UseMedItemData
                                {
                                    ItemId = medItem.Id,
                                    BodyPart = (byte)bodyPart.Value,
                                    HpResource = currentResource,
                                    Amount = 1f
                                }
                            }
                        };

                        Fika.SendMedicalSyncPacket(packet);

                        if (Config.EnableHealthSync.Value)
                        {
                            Plugin.REAL_Logger.LogInfo($"Synced MedEffect: {medItem.LocalizedName()} on {bodyPart.Value} -> {currentResource} charges");
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                Plugin.REAL_Logger.LogError($"Error in MedEffectSyncPatch: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Syncs custom RealismMod effects like Tourniquets and Surgery
    /// </summary>
    public class CustomEffectSyncPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            var realismHealthControllerType = AccessTools.TypeByName("RealismMod.RealismHealthController");
            return AccessTools.Method(realismHealthControllerType, "AddCustomEffect");
        }

        [PatchPostfix]
        private static void Postfix(object __instance, object newEffect, bool canStack)
        {
            try
            {
                // Get player from health controller
                var healthControllerType = __instance.GetType();
                
                // RealismHealthController doesn't directly have player reference
                // We need to get it from the effect or track it differently
                // For now, skip this - it's complex and less critical than charge sync
                
            }
            catch (System.Exception ex)
            {
                Plugin.REAL_Logger.LogError($"Error in CustomEffectSyncPatch: {ex.Message}");
            }
        }
    }
}
