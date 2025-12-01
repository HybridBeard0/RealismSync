using EFT;
using Comfort.Common;
using UnityEngine;

namespace RealismModSync.Health
{
    public static class Core
    {
        private static bool _isInitialized = false;
        private static bool _bringMeToLifeChecked = false;
        private static System.Reflection.MethodInfo _isPlayerInCriticalMethod = null;
        private static System.Reflection.MethodInfo _isPlayerInvulnerableMethod = null;
        private static bool _bringMeToLifeAvailable = false;

        public static void Initialize()
        {
            if (_isInitialized) return;

            Plugin.REAL_Logger.LogInfo("Health Core module initialized");
            _isInitialized = true;
        }

        public static bool ShouldHealthControllerTick(Player player)
        {
            // Check if player exists
            if (player == null)
                return false;

            // Check if game world is instantiated
            if (!Singleton<GameWorld>.Instantiated)
                return false;

            var gameWorld = Singleton<GameWorld>.Instance;
            if (gameWorld == null)
                return false;

            // Check if player health controller is valid
            if (player.ActiveHealthController == null)
                return false;

            // Don't tick health effects after raid ends or player dies
            var mainPlayer = gameWorld.MainPlayer;
            if (mainPlayer == null)
                return false;

            // If this is the main player and they're dead, don't tick
            if (player.IsYourPlayer && !mainPlayer.HealthController.IsAlive)
                return false;

            return true;
        }

        public static bool IsPlayerUnconsciousOrReviving(Player player)
        {
            if (player == null) return false;

            // Initialize BringMeToLifeMod integration on first call
            if (!_bringMeToLifeChecked)
            {
                _bringMeToLifeChecked = true;
                try
                {
                    var revivalFeaturesType = System.Type.GetType("RevivalMod.Features.RevivalFeatures, RevivalMod");
                    if (revivalFeaturesType != null)
                    {
                        _isPlayerInCriticalMethod = revivalFeaturesType.GetMethod("IsPlayerInCriticalState", 
                            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                        
                        _isPlayerInvulnerableMethod = revivalFeaturesType.GetMethod("IsPlayerInvulnerable",
                            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

                        if (_isPlayerInCriticalMethod != null && _isPlayerInvulnerableMethod != null)
                        {
                            _bringMeToLifeAvailable = true;
                            Plugin.REAL_Logger.LogInfo("BringMeToLifeMod integration initialized successfully");
                        }
                        else
                        {
                            Plugin.REAL_Logger.LogInfo("BringMeToLifeMod detected but required methods not found");
                        }
                    }
                    else
                    {
                        Plugin.REAL_Logger.LogInfo("BringMeToLifeMod not detected");
                    }
                }
                catch (System.Exception ex)
                {
                    Plugin.REAL_Logger.LogWarning($"Failed to initialize BringMeToLifeMod integration: {ex.Message}");
                    _bringMeToLifeAvailable = false;
                }
            }

            // If BringMeToLifeMod is not available, return false
            if (!_bringMeToLifeAvailable)
                return false;

            // Check player state using cached methods
            try
            {
                bool isCritical = (bool)_isPlayerInCriticalMethod.Invoke(null, new object[] { player.ProfileId });
                bool isInvulnerable = (bool)_isPlayerInvulnerableMethod.Invoke(null, new object[] { player.ProfileId });
                
                return isCritical || isInvulnerable;
            }
            catch (System.Exception ex)
            {
                // Only log this once per player state check failure to avoid spam
                // This likely means the player is not in any special state
                return false;
            }
        }
    }
}
