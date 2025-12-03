using BepInEx.Configuration;

namespace RealismModSync.QuestExtended
{
    public static class Config
    {
        public static ConfigEntry<bool> EnableQuestSync;

        public static void Bind(ConfigFile config)
        {
            EnableQuestSync = config.Bind(
                "Quest Extended Synchronization",
                "Enable Quest Sync",
                true,
                "Enable synchronization of Quest Extended optional conditions and multi-choice quests across Fika clients"
            );
        }
    }
}
