using Comfort.Common;
using EFT;
using System.Collections.Concurrent;

namespace RealismModSync.QuestExtended
{
    public static class Core
    {
        private static bool _isInitialized = false;
        private static bool _questExtendedAvailable = false;
        private static System.Type _optionalConditionControllerType = null;

        // Track synced condition completions to prevent duplicate syncs
        public static ConcurrentDictionary<string, bool> SyncedConditions = new ConcurrentDictionary<string, bool>();

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
        }
    }
}
