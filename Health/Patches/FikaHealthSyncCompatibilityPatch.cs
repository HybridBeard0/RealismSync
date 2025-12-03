using EFT;
using HarmonyLib;
using System.Reflection;
using SPT.Reflection.Patching;

namespace RealismModSync.Health.Patches
{
    /// <summary>
    /// Patches Fika's health sync to prevent RealismMod custom effects from causing NullReferenceExceptions
    /// </summary>
    public class FikaHealthSyncCompatibilityPatch : ModulePatch
    {
        private static bool _fieldStructureLogged = false;
        private static bool _patchActive = false;

        protected override MethodBase GetTargetMethod()
        {
            // Find NetworkHealthControllerAbstractClass
            var networkHealthControllerType = AccessTools.TypeByName("NetworkHealthControllerAbstractClass");
            if (networkHealthControllerType == null)
            {
                Plugin.REAL_Logger.LogWarning("Could not find NetworkHealthControllerAbstractClass - Fika health sync compatibility patch disabled");
                return null;
            }

            // Find the HandleSyncPacket method
            var handleSyncPacketMethod = AccessTools.Method(networkHealthControllerType, "HandleSyncPacket");
            if (handleSyncPacketMethod == null)
            {
                Plugin.REAL_Logger.LogWarning("Could not find HandleSyncPacket method - Fika health sync compatibility patch disabled");
                return null;
            }

            _patchActive = true;
            return handleSyncPacketMethod;
        }

        [PatchPrefix]
        private static bool Prefix(object __instance, object packet)
        {
            if (!_patchActive || packet == null)
                return true;

            try
            {
                // Only log packet structure once for debugging
                if (!_fieldStructureLogged && Config.EnableHealthSync.Value)
                {
                    LogPacketStructure(packet);
                    _fieldStructureLogged = true;
                }

                // Try to validate the packet without spamming AccessTools warnings
                if (!ValidateHealthPacket(packet))
                {
                    if (Config.EnableHealthSync.Value)
                    {
                        Plugin.REAL_Logger.LogWarning("Blocked invalid health sync packet (possibly RealismMod custom effect)");
                    }
                    return false;
                }

                return true;
            }
            catch (System.Exception ex)
            {
                // Don't log every error, just the first one
                if (!_fieldStructureLogged)
                {
                    Plugin.REAL_Logger.LogError($"Error in FikaHealthSyncCompatibilityPatch: {ex.Message}");
                    _fieldStructureLogged = true; // Prevent spam
                }
                return true;
            }
        }

        private static bool ValidateHealthPacket(object packet)
        {
            try
            {
                var packetType = packet.GetType();
                
                // Get all fields to avoid repeated AccessTools.Field calls
                var allFields = packetType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                
                // Look for ExtraData field (case-insensitive to handle potential name changes)
                var extraDataField = System.Array.Find(allFields, f => 
                    f.Name.Equals("ExtraData", System.StringComparison.OrdinalIgnoreCase) ||
                    f.Name.Equals("Data", System.StringComparison.OrdinalIgnoreCase) ||
                    f.Name.Contains("Extra"));

                if (extraDataField == null)
                    return true; // No extra data field, packet is probably fine

                var extraData = extraDataField.GetValue(packet);
                if (extraData == null)
                    return true; // No extra data, packet is fine

                // Check if the extra data has an effect type field
                var extraDataType = extraData.GetType();
                var extraDataFields = extraDataType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                
                var effectTypeField = System.Array.Find(extraDataFields, f => 
                    f.Name.Equals("EffectType", System.StringComparison.OrdinalIgnoreCase) ||
                    f.Name.Contains("Effect"));

                if (effectTypeField == null)
                    return true; // No effect type field, not an effect packet

                var effectType = effectTypeField.GetValue(extraData);
                
                // Check if effect type is null or empty string
                if (effectType == null || (effectType is string str && string.IsNullOrEmpty(str)))
                {
                    // This is likely a RealismMod custom effect without proper serialization
                    return false;
                }

                return true;
            }
            catch
            {
                // If we can't validate, assume it's valid and let it through
                // This prevents breaking normal health sync
                return true;
            }
        }

        private static void LogPacketStructure(object packet)
        {
            try
            {
                Plugin.REAL_Logger.LogInfo("=== Health Sync Packet Structure ===");
                var packetType = packet.GetType();
                Plugin.REAL_Logger.LogInfo($"Packet Type: {packetType.Name}");
                
                var fields = packetType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                Plugin.REAL_Logger.LogInfo($"Fields found: {fields.Length}");
                
                foreach (var field in fields)
                {
                    Plugin.REAL_Logger.LogInfo($"  - {field.Name} ({field.FieldType.Name})");
                }
                
                Plugin.REAL_Logger.LogInfo("=== End Packet Structure ===");
            }
            catch (System.Exception ex)
            {
                Plugin.REAL_Logger.LogError($"Error logging packet structure: {ex.Message}");
            }
        }
    }
}
