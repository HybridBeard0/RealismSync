using BepInEx.Configuration;

namespace RealismModSync.QuestExtended
{
    public static class Config
    {
        public static ConfigEntry<bool> EnableQuestSync;
        public static ConfigEntry<bool> OnlySyncQuestExtendedConditions;

        public static void Bind(ConfigFile config)
        {
            EnableQuestSync = config.Bind(
                "Quest Extended Synchronization",
                "Enable Quest Sync",
                true,
                "Enable synchronization of Quest Extended optional conditions and multi-choice quests across Fika clients. " +
                "NOTE: Disable this if you're using Fika's native sharedQuestProgression for vanilla quests!"
            );

            OnlySyncQuestExtendedConditions = config.Bind(
                "Quest Extended Synchronization",
                "Only Sync Quest Extended Conditions",
                true,
                "If true, only syncs Quest Extended-specific optional conditions and lets Fika handle vanilla quest progression. " +
                "Set to false only if you need to sync ALL quest conditions (not recommended with Fika's sharedQuestProgression enabled)."
            );
        }
    }
}
