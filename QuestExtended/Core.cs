using Comfort.Common;
using EFT;
using System.Collections.Concurrent;
using System.Linq;

namespace RealismModSync.QuestExtended
{
    public static class Core
    {
        private static bool _isInitialized = false;
        private static bool _questExtendedAvailable = false;
        private static System.Type _optionalConditionControllerType = null;

        // Track synced condition completions to prevent duplicate syncs
        public static ConcurrentDictionary<string, bool> SyncedConditions = new ConcurrentDictionary<string, bool>();

        // Cache of Quest Extended-specific quest IDs (quests with CompleteOptionals condition)
        private static ConcurrentDictionary<string, bool> _questExtendedQuestIds = new ConcurrentDictionary<string, bool>();

        public static void Initialize()
        {
            if (_isInitialized) return;

            // Check if Quest Extended is installed
            try
            {
                _optionalConditionControllerType = System.Type.GetType("QuestsExtended.Quests.OptionalConditionController, QuestsExtended");
                
                if (_optionalConditionControllerType != null)
                {
                    _questExtendedAvailable = true;
                    Plugin.REAL_Logger.LogInfo("Quest Extended detected - sync enabled");
                }
                else
                {
                    Plugin.REAL_Logger.LogInfo("Quest Extended not detected - sync disabled");
                }
            }
            catch (System.Exception ex)
            {
                Plugin.REAL_Logger.LogWarning($"Failed to detect Quest Extended: {ex.Message}");
                _questExtendedAvailable = false;
            }

            _isInitialized = true;
        }

        public static bool IsQuestExtendedAvailable()
        {
            return _questExtendedAvailable;
        }

        public static bool CanSendQuestSync()
        {
            if (!_questExtendedAvailable)
                return false;

            if (!Config.EnableQuestSync.Value)
                return false;

            // Check if game world is active
            if (!Singleton<GameWorld>.Instantiated)
                return false;

            var gameWorld = Singleton<GameWorld>.Instance;
            if (gameWorld == null)
                return false;

            // Check if player exists
            var player = gameWorld.MainPlayer;
            if (player == null)
                return false;

            return true;
        }

        /// <summary>
        /// Checks if a quest is a Quest Extended quest (has CompleteOptionals condition)
        /// This prevents syncing vanilla quests when Fika's sharedQuestProgression is enabled
        /// </summary>
        public static bool IsQuestExtendedQuest(string questId)
        {
            // If we're not in "only sync QE conditions" mode, sync everything
            if (!Config.OnlySyncQuestExtendedConditions.Value)
                return true;

            // Check cache first
            if (_questExtendedQuestIds.TryGetValue(questId, out bool isQE))
                return isQE;

            try
            {
                var player = Utils.GetYourPlayer();
                if (player == null)
                    return false;

                var profile = player.Profile;
                if (profile == null)
                    return false;

                var activeQuests = profile.QuestsData;
                if (activeQuests == null)
                    return false;

                // Find the quest
                var quest = activeQuests.FirstOrDefault(q => q.Id == questId);
                if (quest?.Template?.conditionsDict_0 == null)
                    return false;

                // Check if quest has CompleteOptionals condition (Quest Extended feature)
                // We check by looking at the condition's type name
                foreach (var condGroup in quest.Template.conditionsDict_0)
                {
                    if (condGroup.Value?.list_0 == null)
                        continue;

                    foreach (var condition in condGroup.Value.list_0)
                    {
                        // Check if the condition type contains "CompleteOptionals" or "Optional"
                        // This is Quest Extended's custom condition type
                        var conditionTypeName = condition.GetType().Name;
                        if (conditionTypeName.Contains("CompleteOptionals") || conditionTypeName.Contains("Optional"))
                        {
                            _questExtendedQuestIds.TryAdd(questId, true);
                            return true;
                        }

                        // Also check the string representation
                        var conditionTypeStr = condition.ToString();
                        if (conditionTypeStr != null && (conditionTypeStr.Contains("CompleteOptionals") || conditionTypeStr.Contains("Optional")))
                        {
                            _questExtendedQuestIds.TryAdd(questId, true);
                            return true;
                        }
                    }
                }

                // Not a Quest Extended quest - it's a vanilla quest
                _questExtendedQuestIds.TryAdd(questId, false);
                return false;
            }
            catch (System.Exception ex)
            {
                Plugin.REAL_Logger.LogError($"Error checking if quest {questId} is Quest Extended quest: {ex.Message}");
                // If we can't determine, don't sync it (let Fika handle it)
                return false;
            }
        }

        public static void MarkConditionSynced(string questId, string conditionId)
        {
            var key = $"{questId}_{conditionId}";
            SyncedConditions.TryAdd(key, true);
        }

        public static bool IsConditionAlreadySynced(string questId, string conditionId)
        {
            var key = $"{questId}_{conditionId}";
            return SyncedConditions.ContainsKey(key);
        }

        public static void ClearSyncedConditions()
        {
            SyncedConditions.Clear();
            _questExtendedQuestIds.Clear();
        }
    }
}
