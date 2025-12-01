using EFT;
using HarmonyLib;
using System.Reflection;
using Comfort.Common;

namespace RealismModSync.Health.Patches
{
    [HarmonyPatch]
    public class RealismHealthControllerUpdatePatch
    {
        private static MethodInfo _targetMethod;

        static bool Prepare()
        {
            if (!Config.EnableHealthSync.Value)
            {
                Plugin.REAL_Logger.LogInfo("Health sync is disabled in config");
                return false;
            }

            try
            {
                // Find RealismHealthController class
                var realismHealthControllerType = AccessTools.TypeByName("RealismMod.RealismHealthController");
                if (realismHealthControllerType == null)
                {
                    Plugin.REAL_Logger.LogWarning("RealismMod.RealismHealthController type not found - health tick patch disabled");
                    return false;
                }

                // Try to find the HealthEffecTick method
                _targetMethod = AccessTools.Method(realismHealthControllerType, "HealthEffecTick");
                
                if (_targetMethod == null)
                {
                    Plugin.REAL_Logger.LogWarning("RealismMod.RealismHealthController.HealthEffecTick method not found - health tick patch disabled");
                    return false;
                }

                Plugin.REAL_Logger.LogInfo("RealismMod.RealismHealthController.HealthEffecTick method found successfully");
                return true;
            }
            catch (System.Exception ex)
            {
                Plugin.REAL_Logger.LogError($"Failed to prepare RealismHealthControllerUpdatePatch: {ex.Message}");
                return false;
            }
        }

        static MethodBase TargetMethod()
        {
            return _targetMethod;
        }

        [HarmonyPrefix]
        static bool Prefix()
        {
            try
            {
                // Check if game world is instantiated
                if (!Singleton<GameWorld>.Instantiated)
                    return false;

                var gameWorld = Singleton<GameWorld>.Instance;
                if (gameWorld == null)
                    return false;

                // Get the main player
                Player player = gameWorld.MainPlayer;
                
                if (player == null)
                    return false; // Don't run health tick if no player yet

                // Check if health controller should tick
                if (!Core.ShouldHealthControllerTick(player))
                {
                    // Skip the health controller update to prevent errors after raid ends
                    return false;
                }

                // Check if player is in unconscious/revival state from BringMeToLifeMod
                if (Core.IsPlayerUnconsciousOrReviving(player))
                {
                    // Allow health controller to run during revival
                    return true;
                }

                // Normal operation - let the health controller tick
                return true;
            }
            catch (System.Exception ex)
            {
                Plugin.REAL_Logger.LogError($"Error in RealismHealthControllerUpdatePatch: {ex.Message}");
                return true; // Let original method run on error
            }
        }
    }
}
