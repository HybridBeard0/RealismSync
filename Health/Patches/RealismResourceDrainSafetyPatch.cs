using EFT;
using HarmonyLib;
using System.Reflection;
using SPT.Reflection.Patching;

namespace RealismModSync.Health.Patches
{
    /// <summary>
    /// Safety patch to prevent RealismMod's DoResourceDrain from running during extraction/cleanup
    /// This prevents NullReferenceExceptions when Fika's network layer is shutting down
    /// </summary>
    public class RealismResourceDrainSafetyPatch : ModulePatch
    {
        private static MethodInfo _targetMethod;

        protected override MethodBase GetTargetMethod()
        {
            if (!Config.EnableHealthSync.Value)
            {
                Plugin.REAL_Logger.LogInfo("Health sync is disabled - resource drain safety patch disabled");
                return null;
            }

            try
            {
                // Find RealismHealthController class
                var realismHealthControllerType = AccessTools.TypeByName("RealismMod.RealismHealthController");
                if (realismHealthControllerType == null)
                {
                    Plugin.REAL_Logger.LogWarning("RealismMod.RealismHealthController type not found - resource drain safety patch disabled");
                    return null;
                }

                // Find DoResourceDrain method
                _targetMethod = AccessTools.Method(realismHealthControllerType, "DoResourceDrain");
                
                if (_targetMethod == null)
                {
                    Plugin.REAL_Logger.LogWarning("RealismMod.RealismHealthController.DoResourceDrain method not found - resource drain safety patch disabled");
                    return null;
                }

                Plugin.REAL_Logger.LogInfo("RealismMod.RealismHealthController.DoResourceDrain safety patch initialized");
                return _targetMethod;
            }
            catch (System.Exception ex)
            {
                Plugin.REAL_Logger.LogError($"Failed to initialize RealismResourceDrainSafetyPatch: {ex.Message}");
                return null;
            }
        }

        [PatchPrefix]
        private static bool Prefix(object __instance, object hc)
        {
            try
            {
                // Check if network is active before allowing resource drain
                // During extraction, this prevents trying to send health sync packets
                if (!Core.IsNetworkActive())
                {
                    // Network is shutting down - skip resource drain
                    return false;
                }

                // Get the health controller and validate it
                var activeHealthController = hc as EFT.HealthSystem.ActiveHealthController;
                if (activeHealthController == null)
                    return false;

                // Try to get the player - use different methods based on health controller type
                Player player = null;
                var healthControllerType = activeHealthController.GetType();
                var healthControllerTypeName = healthControllerType.Name;

                // Only try to access player field if this is a Fika health controller
                // In offline/single-player mode, we don't need to check the player
                if (healthControllerTypeName.Contains("Coop") || healthControllerTypeName.Contains("Fika"))
                {
                    // This is a multiplayer health controller - try to get player
                    var playerFieldNames = new[] { "player_0", "_player", "player", "Player" };
                    
                    foreach (var fieldName in playerFieldNames)
                    {
                        try
                        {
                            var playerField = AccessTools.Field(healthControllerType, fieldName);
                            if (playerField != null)
                            {
                                player = playerField.GetValue(activeHealthController) as Player;
                                if (player != null)
                                    break;
                            }
                        }
                        catch
                        {
                            // Silently continue to next field name
                            continue;
                        }
                    }

                    // If we couldn't find the player in multiplayer mode, be conservative and block
                    if (player == null)
                        return false;

                    // Use our standard check for multiplayer
                    if (!Core.ShouldHealthControllerTick(player))
                    {
                        // Player is dead, extracting, or game is ending - skip resource drain
                        return false;
                    }
                }
                else
                {
                    // Single-player/offline mode - use main player from game world
                    player = Utils.GetYourPlayer();
                    
                    if (player == null)
                        return false;

                    // Use our standard check
                    if (!Core.ShouldHealthControllerTick(player))
                    {
                        // Player is dead, extracting, or game is ending - skip resource drain
                        return false;
                    }
                }

                // Everything is valid - allow resource drain
                return true;
            }
            catch (System.Exception ex)
            {
                // On error, block the resource drain to be safe
                Plugin.REAL_Logger.LogWarning($"Error in RealismResourceDrainSafetyPatch, blocking drain: {ex.Message}");
                return false;
            }
        }
    }
}
