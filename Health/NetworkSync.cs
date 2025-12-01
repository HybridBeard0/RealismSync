using EFT;
using EFT.InventoryLogic;
using Comfort.Common;
using Fika.Core.Coop.Players;
using System.Linq;

namespace RealismModSync.Health
{
    /// <summary>
    /// Handles network synchronization of Realism medical system changes
    /// </summary>
    public static class NetworkSync
    {
        public static void ProcessMedicalSyncPacket(Packets.RealismMedicalSyncPacket packet)
        {
            if (!Singleton<GameWorld>.Instantiated)
                return;

            var gameWorld = Singleton<GameWorld>.Instance;
            var player = gameWorld?.RegisteredPlayers?.FirstOrDefault(p => p is CoopPlayer cp && cp.NetId == packet.NetId) as Player;

            if (player == null)
            {
                Plugin.REAL_Logger.LogWarning($"Could not find player with NetId {packet.NetId} for medical sync");
                return;
            }

            switch (packet.SyncType)
            {
                case Packets.RealismMedicalSyncPacket.EMedicalSyncType.UseMedItem:
                    ProcessUseMedItem(player, packet.Data.UseMedItem);
                    break;

                case Packets.RealismMedicalSyncPacket.EMedicalSyncType.ApplyCustomEffect:
                    ProcessApplyCustomEffect(player, packet.Data.ApplyCustomEffect);
                    break;

                case Packets.RealismMedicalSyncPacket.EMedicalSyncType.RemoveCustomEffect:
                    ProcessRemoveCustomEffect(player, packet.Data.RemoveCustomEffect);
                    break;

                case Packets.RealismMedicalSyncPacket.EMedicalSyncType.UpdateMedCharges:
                    ProcessUpdateMedCharges(player, packet.Data.UpdateMedCharges);
                    break;

                case Packets.RealismMedicalSyncPacket.EMedicalSyncType.TourniquetApplied:
                    ProcessTourniquetApplied(player, packet.Data.TourniquetApplied);
                    break;

                case Packets.RealismMedicalSyncPacket.EMedicalSyncType.SurgeryEffect:
                    ProcessSurgeryEffect(player, packet.Data.SurgeryEffect);
                    break;
            }
        }

        private static void ProcessUseMedItem(Player player, Packets.RealismMedicalSyncPacket.UseMedItemData data)
        {
            try
            {
                var item = FindItemById(player, data.ItemId);
                if (item == null)
                {
                    Plugin.REAL_Logger.LogWarning($"Could not find item {data.ItemId} for med sync");
                    return;
                }

                var medKitItem = item as MedsItemClass;
                if (medKitItem != null)
                {
                    // Use reflection to access HpResource field
                    var medKitType = medKitItem.GetType();
                    var hpResourceField = HarmonyLib.AccessTools.Field(medKitType, "HpResource");
                    if (hpResourceField != null)
                    {
                        hpResourceField.SetValue(medKitItem, data.HpResource);
                    }
                }
            }
            catch (System.Exception ex)
            {
                Plugin.REAL_Logger.LogError($"Error processing UseMedItem sync: {ex.Message}");
            }
        }

        private static void ProcessApplyCustomEffect(Player player, Packets.RealismMedicalSyncPacket.ApplyCustomEffectData data)
        {
            // This would integrate with RealismMod's custom effect system
            // For now, just log it
            Plugin.REAL_Logger.LogInfo($"Received custom effect: {data.EffectType} on {(EBodyPart)data.BodyPart}");
        }

        private static void ProcessRemoveCustomEffect(Player player, Packets.RealismMedicalSyncPacket.RemoveCustomEffectData data)
        {
            Plugin.REAL_Logger.LogInfo($"Received remove custom effect: {data.EffectType} from {(EBodyPart)data.BodyPart}");
        }

        private static void ProcessUpdateMedCharges(Player player, Packets.RealismMedicalSyncPacket.UpdateMedChargesData data)
        {
            try
            {
                var item = FindItemById(player, data.ItemId);
                if (item == null)
                    return;

                var medKitItem = item as MedsItemClass;
                if (medKitItem != null)
                {
                    var medKitType = medKitItem.GetType();
                    var hpResourceField = HarmonyLib.AccessTools.Field(medKitType, "HpResource");
                    if (hpResourceField != null)
                    {
                        hpResourceField.SetValue(medKitItem, data.NewCharges);
                        Plugin.REAL_Logger.LogInfo($"Synced med charges for {data.ItemId}: {data.NewCharges}");
                    }
                }
            }
            catch (System.Exception ex)
            {
                Plugin.REAL_Logger.LogError($"Error processing UpdateMedCharges: {ex.Message}");
            }
        }

        private static void ProcessTourniquetApplied(Player player, Packets.RealismMedicalSyncPacket.TourniquetAppliedData data)
        {
            Plugin.REAL_Logger.LogInfo($"Received tourniquet application on {(EBodyPart)data.BodyPart}");
        }

        private static void ProcessSurgeryEffect(Player player, Packets.RealismMedicalSyncPacket.SurgeryEffectData data)
        {
            Plugin.REAL_Logger.LogInfo($"Received surgery effect on {(EBodyPart)data.BodyPart}");
        }

        private static Item FindItemById(Player player, string itemId)
        {
            // Find item in player's inventory by ID
            return player?.Profile?.Inventory?.AllRealPlayerItems?.FirstOrDefault(item => item.Id == itemId);
        }
    }
}
