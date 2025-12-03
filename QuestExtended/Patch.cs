using HarmonyLib;

namespace RealismModSync.QuestExtended
{
    public static class Patch
    {
        private static Harmony _harmony;

        public static void Awake()
        {
            if (!Config.EnableQuestSync.Value)
            {
                Plugin.REAL_Logger.LogInfo("Quest Extended sync is disabled in config");
                return;
            }

            _harmony = new Harmony("RealismModSync.QuestExtended");

            Plugin.REAL_Logger.LogInfo("Quest Extended patches applied");

            // Apply Quest Extended sync patches
            Patches.QuestExtendedSyncPatches.ApplyPatches();
        }
    }
}
