using BepInEx.Configuration;

namespace RealismModSync.Health
{
    public static class Config
    {
        public static ConfigEntry<bool> EnableHealthSync;

        public static void Bind(ConfigFile config)
        {
            EnableHealthSync = config.Bind(
                "Health Synchronization",
                "Enable Health Sync",
                true,
                "Enable synchronization of Realism health effects with Fika and other mods"
            );
        }
    }
}
