using EFT.Quests;
using HarmonyLib;
using System.Reflection;
using SPT.Reflection.Patching;
using Comfort.Common;
using EFT;
using EFT.UI;

namespace RealismModSync.QuestExtended.Patches
{
    /// <summary>
    /// Patches Quest Extended to synchronize optional condition completions
    /// Only syncs Quest Extended-specific quests to avoid interfering with Fika's native sharedQuestProgression
    /// </summary>
    public static class QuestExtendedSyncPatches
    {
        public static void ApplyPatches()
        {
            if (!Core.IsQuestExtendedAvailable())
            {
                Plugin.REAL_Logger.LogInfo("Quest Extended not available - sync patches not applied");
                return;
            }

            try
            {
                new HandleVanillaConditionChangedPatch().Enable();
                Plugin.REAL_Logger.LogInfo("Applied HandleVanillaConditionChangedPatch (Quest Extended conditions only)");
            }
            catch (System.Exception ex)
            {
                Plugin.REAL_Logger.LogError($"Failed to apply HandleVanillaConditionChangedPatch: {ex.Message}");
            }

            try
            {
                new HandleQuestStartingConditionCompletionPatch().Enable();
                Plugin.REAL_Logger.LogInfo("Applied HandleQuestStartingConditionCompletionPatch (Quest Extended conditions only)");
            }
            catch (System.Exception ex)
            {
                Plugin.REAL_Logger.LogError($"Failed to apply HandleQuestStartingConditionCompletionPatch: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Patches Quest Extended's HandleVanillaConditionChanged to sync condition progress
    /// Only syncs Quest Extended-specific quests to let Fika handle vanilla quest progression
    /// </summary>
    public class HandleVanillaConditionChangedPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            var optionalConditionControllerType = AccessTools.TypeByName("QuestsExtended.Quests.OptionalConditionController");
            if (optionalConditionControllerType == null)
            {
                Plugin.REAL_Logger.LogWarning("Could not find OptionalConditionController type");
                return null;
            }

            return AccessTools.Method(optionalConditionControllerType, "HandleVanillaConditionChanged");
        }

        [PatchPostfix]
        private static void Postfix(string conditionId, int currentValue)
        {
            try
            {
                if (!Core.CanSendQuestSync())
                    return;

                // Find which quest this condition belongs to
                var questId = FindQuestIdForCondition(conditionId);
                if (string.IsNullOrEmpty(questId))
                    return;

                // IMPORTANT: Only sync Quest Extended-specific quests
                // Let Fika's sharedQuestProgression handle vanilla quests (like PMC kills)
                if (!Core.IsQuestExtendedQuest(questId))
                {
                    if (Config.OnlySyncQuestExtendedConditions.Value)
                    {
                        // This is a vanilla quest - let Fika handle it
                        return;
                    }
                }

                // Don't sync if already synced
                if (Core.IsConditionAlreadySynced(questId, conditionId))
                    return;

                // Send sync packet (only for Quest Extended quests)
                var packet = new Packets.QuestExtendedSyncPacket
                {
                    QuestId = questId,
                    ConditionId = conditionId,
                    SyncType = Packets.EQuestSyncType.ConditionProgress,
                    CurrentValue = currentValue,
                    IsCompleted = false
                };

                Fika.SendQuestSyncPacket(packet);

                if (Config.EnableQuestSync.Value)
                {
                    Plugin.REAL_Logger.LogInfo($"Synced Quest Extended condition progress: {questId}/{conditionId} = {currentValue}");
                }
            }
            catch (System.Exception ex)
            {
                Plugin.REAL_Logger.LogError($"Error in HandleVanillaConditionChangedPatch: {ex.Message}");
            }
        }

        private static string FindQuestIdForCondition(string conditionId)
        {
            try
            {
                var player = Utils.GetYourPlayer();
                if (player == null)
                    return null;

                var profile = player.Profile;
                if (profile == null)
                    return null;

                var activeQuests = profile.QuestsData;
                if (activeQuests == null)
                    return null;

                foreach (var quest in activeQuests)
                {
                    if (quest.Template?.conditionsDict_0 == null)
                        continue;

                    foreach (var condGroup in quest.Template.conditionsDict_0)
                    {
                        if (condGroup.Value?.list_0 == null)
                            continue;

                        foreach (var condition in condGroup.Value.list_0)
                        {
                            if (condition.id == conditionId)
                                return quest.Id;
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                Plugin.REAL_Logger.LogError($"Error finding quest ID for condition {conditionId}: {ex.Message}");
            }

            return null;
        }
    }

    /// <summary>
    /// Patches Quest Extended's HandleQuestStartingConditionCompletion to sync optional condition completions
    /// Only syncs Quest Extended-specific quests to let Fika handle vanilla quest progression
    /// </summary>
    public class HandleQuestStartingConditionCompletionPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            var optionalConditionControllerType = AccessTools.TypeByName("QuestsExtended.Quests.OptionalConditionController");
            if (optionalConditionControllerType == null)
            {
                Plugin.REAL_Logger.LogWarning("Could not find OptionalConditionController type");
                return null;
            }

            return AccessTools.Method(optionalConditionControllerType, "HandleQuestStartingConditionCompletion");
        }

        [PatchPostfix]
        private static void Postfix(Condition condition)
        {
            try
            {
                if (condition == null)
                    return;

                if (!Core.CanSendQuestSync())
                    return;

                // Find which quest this condition belongs to
                var questId = FindQuestIdForCondition(condition.id);
                if (string.IsNullOrEmpty(questId))
                    return;

                // IMPORTANT: Only sync Quest Extended-specific quests
                // Let Fika's sharedQuestProgression handle vanilla quests
                if (!Core.IsQuestExtendedQuest(questId))
                {
                    if (Config.OnlySyncQuestExtendedConditions.Value)
                    {
                        // This is a vanilla quest - let Fika handle it
                        return;
                    }
                }

                // Mark as synced
                Core.MarkConditionSynced(questId, condition.id);

                // Send completion sync packet (only for Quest Extended quests)
                var packet = new Packets.QuestExtendedSyncPacket
                {
                    QuestId = questId,
                    ConditionId = condition.id,
                    SyncType = Packets.EQuestSyncType.ConditionCompleted,
                    CurrentValue = (int)condition.value, // Cast float to int
                    IsCompleted = true
                };

                Fika.SendQuestSyncPacket(packet);

                if (Config.EnableQuestSync.Value)
                {
                    Plugin.REAL_Logger.LogInfo($"Synced Quest Extended optional condition completion: {questId}/{condition.id}");
                }
            }
            catch (System.Exception ex)
            {
                Plugin.REAL_Logger.LogError($"Error in HandleQuestStartingConditionCompletionPatch: {ex.Message}");
            }
        }

        private static string FindQuestIdForCondition(string conditionId)
        {
            try
            {
                var player = Utils.GetYourPlayer();
                if (player == null)
                    return null;

                var profile = player.Profile;
                if (profile == null)
                    return null;

                var activeQuests = profile.QuestsData;
                if (activeQuests == null)
                    return null;

                foreach (var quest in activeQuests)
                {
                    if (quest.Template?.conditionsDict_0 == null)
                        continue;

                    foreach (var condGroup in quest.Template.conditionsDict_0)
                    {
                        if (condGroup.Value?.list_0 == null)
                            continue;

                        foreach (var condition in condGroup.Value.list_0)
                        {
                            if (condition.id == conditionId)
                                return quest.Id;
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                Plugin.REAL_Logger.LogError($"Error finding quest ID for condition {conditionId}: {ex.Message}");
            }

            return null;
        }
    }
}
