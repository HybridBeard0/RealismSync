using EFT.Quests;
using EFT.UI;
using Comfort.Common;
using EFT;

namespace RealismModSync.QuestExtended
{
    public static class NetworkSync
    {
        public static void ProcessQuestSyncPacket(Packets.QuestExtendedSyncPacket packet)
        {
            try
            {
                if (!Core.IsQuestExtendedAvailable())
                    return;

                if (!Core.CanSendQuestSync())
                    return;

                switch (packet.SyncType)
                {
                    case Packets.EQuestSyncType.ConditionProgress:
                        ProcessConditionProgress(packet);
                        break;

                    case Packets.EQuestSyncType.ConditionCompleted:
                        ProcessConditionCompleted(packet);
                        break;

                    case Packets.EQuestSyncType.OptionalChoiceMade:
                        ProcessOptionalChoiceMade(packet);
                        break;

                    case Packets.EQuestSyncType.MultiChoiceQuestStarted:
                        ProcessMultiChoiceQuestStarted(packet);
                        break;
                }
            }
            catch (System.Exception ex)
            {
                Plugin.REAL_Logger.LogError($"Error processing quest sync packet: {ex.Message}");
            }
        }

        private static void ProcessConditionProgress(Packets.QuestExtendedSyncPacket packet)
        {
            try
            {
                // Mark as synced to prevent echo
                Core.MarkConditionSynced(packet.QuestId, packet.ConditionId);

                // Find the condition and update its progress
                var condition = FindCondition(packet.QuestId, packet.ConditionId);
                if (condition == null)
                {
                    Plugin.REAL_Logger.LogWarning($"Could not find condition {packet.ConditionId} in quest {packet.QuestId}");
                    return;
                }

                // Update the condition's current value using reflection
                var conditionType = condition.GetType();
                var currentCounterField = HarmonyLib.AccessTools.Field(conditionType, "int_0"); // currentCounter field
                
                if (currentCounterField != null)
                {
                    currentCounterField.SetValue(condition, packet.CurrentValue);
                    
                    if (Config.EnableQuestSync.Value)
                    {
                        Plugin.REAL_Logger.LogInfo($"Updated quest condition progress: {packet.QuestId}/{packet.ConditionId} = {packet.CurrentValue}");
                    }
                }
            }
            catch (System.Exception ex)
            {
                Plugin.REAL_Logger.LogError($"Error processing condition progress: {ex.Message}");
            }
        }

        private static void ProcessConditionCompleted(Packets.QuestExtendedSyncPacket packet)
        {
            try
            {
                // Mark as synced
                Core.MarkConditionSynced(packet.QuestId, packet.ConditionId);

                // Trigger Quest Extended's completion handler
                var optionalConditionControllerType = System.Type.GetType("QuestsExtended.Quests.OptionalConditionController, QuestsExtended");
                if (optionalConditionControllerType == null)
                    return;

                var condition = FindCondition(packet.QuestId, packet.ConditionId);
                if (condition == null)
                    return;

                // Call HandleQuestStartingConditionCompletion directly
                var handleMethod = optionalConditionControllerType.GetMethod("HandleQuestStartingConditionCompletion",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

                if (handleMethod != null)
                {
                    handleMethod.Invoke(null, new object[] { condition });

                    if (Config.EnableQuestSync.Value)
                    {
                        Plugin.REAL_Logger.LogInfo($"Processed optional condition completion: {packet.QuestId}/{packet.ConditionId}");
                    }
                }
            }
            catch (System.Exception ex)
            {
                Plugin.REAL_Logger.LogError($"Error processing condition completion: {ex.Message}");
            }
        }

        private static void ProcessOptionalChoiceMade(Packets.QuestExtendedSyncPacket packet)
        {
            // Future implementation for syncing optional quest choices
            Plugin.REAL_Logger.LogInfo($"Received optional choice: {packet.QuestId}");
        }

        private static void ProcessMultiChoiceQuestStarted(Packets.QuestExtendedSyncPacket packet)
        {
            // Future implementation for syncing multi-choice quest starts
            Plugin.REAL_Logger.LogInfo($"Received multi-choice quest start: {packet.QuestId}");
        }

        private static Condition FindCondition(string questId, string conditionId)
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
                    if (quest.Id != questId)
                        continue;

                    if (quest.Template?.conditionsDict_0 == null)
                        continue;

                    foreach (var condGroup in quest.Template.conditionsDict_0)
                    {
                        if (condGroup.Value?.list_0 == null)
                            continue;

                        foreach (var condition in condGroup.Value.list_0)
                        {
                            if (condition.id == conditionId)
                                return condition;
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                Plugin.REAL_Logger.LogError($"Error finding condition {conditionId}: {ex.Message}");
            }

            return null;
        }
    }
}
