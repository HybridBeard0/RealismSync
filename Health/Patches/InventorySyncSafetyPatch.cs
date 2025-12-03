using EFT;
using EFT.InventoryLogic;
using HarmonyLib;
using System.Reflection;
using SPT.Reflection.Patching;
using Comfort.Common;
using System.Linq;

namespace RealismModSync.Health.Patches
{
    /// <summary>
    /// Patches to ensure RealismMod's inventory changes don't break Fika's inventory sync
    /// Particularly important for bot inventory operations with LootingBots
    /// Also handles UIFixes and DynamicItemWeights compatibility
    /// </summary>
    public static class InventorySyncSafetyPatches
    {
        private static Harmony _harmonyInstance;

        public static void ApplyPatches()
        {
            // Create a dedicated Harmony instance for inventory safety
            _harmonyInstance = new Harmony("RealismModSync.InventorySafety");

            try
            {
                // Apply CoopBotInventoryController patch using Harmony directly instead of ModulePatch
                // This ensures it patches DMD methods correctly
                var coopBotInventoryControllerType = AccessTools.TypeByName("Fika.Core.Coop.BotClasses.CoopBotInventoryController");
                if (coopBotInventoryControllerType != null)
                {
                    var vmethod1 = AccessTools.Method(coopBotInventoryControllerType, "vmethod_1");
                    if (vmethod1 != null)
                    {
                        var prefix = new HarmonyMethod(typeof(CoopBotInventorySafetyPatch).GetMethod(nameof(CoopBotInventorySafetyPatch.Prefix), BindingFlags.Static | BindingFlags.Public));
                        _harmonyInstance.Patch(vmethod1, prefix: prefix);
                        Plugin.REAL_Logger.LogInfo("Applied CoopBotInventorySafetyPatch using Harmony");
                    }
                    else
                    {
                        Plugin.REAL_Logger.LogWarning("Could not find CoopBotInventoryController.vmethod_1");
                    }
                }
                else
                {
                    Plugin.REAL_Logger.LogWarning("Could not find CoopBotInventoryController type");
                }
            }
            catch (System.Exception ex)
            {
                Plugin.REAL_Logger.LogError($"Failed to apply CoopBotInventorySafetyPatch: {ex.Message}");
            }

            try
            {
                new ItemValidationPatch().Enable();
                Plugin.REAL_Logger.LogInfo("Applied ItemValidationPatch");
            }
            catch (System.Exception ex)
            {
                Plugin.REAL_Logger.LogError($"Failed to apply ItemValidationPatch: {ex.Message}");
            }

            try
            {
                new ItemWeightUpdateSafetyPatch().Enable();
                Plugin.REAL_Logger.LogInfo("Applied ItemWeightUpdateSafetyPatch (DynamicItemWeights compatibility)");
            }
            catch (System.Exception ex)
            {
                Plugin.REAL_Logger.LogError($"Failed to apply ItemWeightUpdateSafetyPatch: {ex.Message}");
            }
        }

        // Helper method to get field by name using reflection (no AccessTools warnings)
        private static FieldInfo GetFieldSafe(System.Type type, params string[] fieldNames)
        {
            var allFields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var name in fieldNames)
            {
                var field = allFields.FirstOrDefault(f => f.Name.Equals(name, System.StringComparison.OrdinalIgnoreCase));
                if (field != null)
                    return field;
            }
            return null;
        }
    }

    /// <summary>
    /// Wraps Fika's CoopBotInventoryController operations with null checks
    /// Now applies using Harmony directly to handle DMD methods
    /// </summary>
    public static class CoopBotInventorySafetyPatch
    {
        public static bool Prefix(object __instance, object operation, object callback)
        {
            try
            {
                // Validate the operation object
                if (operation == null)
                {
                    Plugin.REAL_Logger.LogWarning("Blocked null inventory operation in CoopBotInventoryController");
                    InvokeFailedCallback(callback);
                    return false;
                }

                // Use reflection to get Item field (avoid AccessTools warnings)
                var operationType = operation.GetType();
                var itemField = GetFieldSafe(operationType, "Item", "item", "_item");
                
                if (itemField != null)
                {
                    var item = itemField.GetValue(operation);
                    if (item == null)
                    {
                        Plugin.REAL_Logger.LogWarning($"Blocked inventory operation with null item: {operationType.Name}");
                        InvokeFailedCallback(callback);
                        return false;
                    }

                    // Validate the item is a valid EFT item
                    if (!(item is Item))
                    {
                        Plugin.REAL_Logger.LogWarning($"Blocked inventory operation with invalid item type: {item.GetType().Name}");
                        InvokeFailedCallback(callback);
                        return false;
                    }

                    var eftItem = item as Item;
                    
                    // Additional validation for medical items
                    if (eftItem is MedsItemClass medsItem)
                    {
                        if (!ValidateMedicalItem(medsItem))
                        {
                            InvokeFailedCallback(callback);
                            return false;
                        }
                    }

                    // Validate item weight
                    if (!ValidateItemWeight(eftItem))
                    {
                        // Log but don't block - item might still be usable
                    }
                }

                return true;
            }
            catch (System.Exception ex)
            {
                Plugin.REAL_Logger.LogError($"Error in CoopBotInventorySafetyPatch.Prefix: {ex.Message}");
                return true;
            }
        }

        private static bool ValidateMedicalItem(MedsItemClass medsItem)
        {
            try
            {
                var medItemType = medsItem.GetType();
                var hpResourceField = GetFieldSafe(medItemType, "HpResource", "hpResource", "_hpResource");
                
                if (hpResourceField != null)
                {
                    var hpResource = hpResourceField.GetValue(medsItem);
                    if (hpResource is float hpValue)
                    {
                        if (float.IsNaN(hpValue) || float.IsInfinity(hpValue))
                        {
                            Plugin.REAL_Logger.LogWarning($"Fixed invalid HpResource value for {medsItem.LocalizedName()}: {hpValue} -> 0");
                            hpResourceField.SetValue(medsItem, 0f);
                        }
                        else if (hpValue < 0f)
                        {
                            Plugin.REAL_Logger.LogWarning($"Fixed negative HpResource value for {medsItem.LocalizedName()}: {hpValue} -> 0");
                            hpResourceField.SetValue(medsItem, 0f);
                        }
                    }
                }
                return true;
            }
            catch (System.Exception ex)
            {
                Plugin.REAL_Logger.LogError($"Error validating medical item: {ex.Message}");
                return false;
            }
        }

        private static bool ValidateItemWeight(Item item)
        {
            try
            {
                var weight = item.Weight;
                if (float.IsNaN(weight) || float.IsInfinity(weight) || weight < 0f)
                {
                    Plugin.REAL_Logger.LogWarning($"Item {item.LocalizedName()} has invalid weight: {weight} (DynamicItemWeights issue?)");
                    return false;
                }
                return true;
            }
            catch (System.Exception ex)
            {
                Plugin.REAL_Logger.LogWarning($"Could not validate weight: {ex.Message}");
                return true;
            }
        }

        private static void InvokeFailedCallback(object callback)
        {
            try
            {
                if (callback != null)
                {
                    var callbackType = callback.GetType();
                    var invokeMethod = callbackType.GetMethod("Invoke");
                    if (invokeMethod != null)
                    {
                        var parameters = invokeMethod.GetParameters();
                        if (parameters.Length > 0)
                        {
                            var resultType = parameters[0].ParameterType;
                            var failedResult = System.Activator.CreateInstance(resultType);
                            
                            // Try to find Succeeded field
                            var succeededField = GetFieldSafe(resultType, "Succeeded", "succeeded", "_succeeded");
                            if (succeededField != null)
                            {
                                succeededField.SetValue(failedResult, false);
                            }
                            
                            invokeMethod.Invoke(callback, new[] { failedResult });
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                Plugin.REAL_Logger.LogError($"Error invoking failed callback: {ex.Message}");
            }
        }

        private static FieldInfo GetFieldSafe(System.Type type, params string[] fieldNames)
        {
            var allFields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var name in fieldNames)
            {
                var field = allFields.FirstOrDefault(f => f.Name.Equals(name, System.StringComparison.OrdinalIgnoreCase));
                if (field != null)
                    return field;
            }
            return null;
        }
    }

    /// <summary>
    /// Validates items before they're used in inventory operations
    /// </summary>
    public class ItemValidationPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            var traderControllerType = AccessTools.TypeByName("TraderControllerClass");
            if (traderControllerType == null)
            {
                Plugin.REAL_Logger.LogWarning("Could not find TraderControllerClass");
                return null;
            }

            return AccessTools.Method(traderControllerType, "TryRunNetworkTransaction");
        }

        [PatchPrefix]
        private static bool Prefix(object operationResult)
        {
            try
            {
                if (operationResult == null)
                {
                    Plugin.REAL_Logger.LogWarning("Blocked null operationResult in TryRunNetworkTransaction");
                    return false;
                }

                var resultType = operationResult.GetType();
                
                // Use reflection to get Succeeded field
                var succeededField = GetFieldSafe(resultType, "Succeeded", "succeeded", "_succeeded");
                if (succeededField != null)
                {
                    var succeeded = succeededField.GetValue(operationResult);
                    if (succeeded is bool successValue && !successValue)
                    {
                        return false;
                    }
                }

                // Get Value field
                var valueField = GetFieldSafe(resultType, "Value", "value", "_value");
                if (valueField != null)
                {
                    var value = valueField.GetValue(operationResult);
                    if (value == null)
                    {
                        Plugin.REAL_Logger.LogWarning("Blocked transaction with null operation value");
                        return false;
                    }

                    // Validate swap operations
                    var operationType = value.GetType();
                    var operationName = operationType.Name;

                    if (operationName.Contains("Swap") || operationName.Contains("Move"))
                    {
                        ValidateSwapOperation(value);
                    }
                }

                return true;
            }
            catch (System.Exception ex)
            {
                Plugin.REAL_Logger.LogError($"Error in ItemValidationPatch: {ex.Message}");
                return true;
            }
        }

        private static void ValidateSwapOperation(object operation)
        {
            try
            {
                var operationType = operation.GetType();
                
                // Use reflection to get Item field
                var itemField = GetFieldSafe(operationType, "Item", "item", "_item");
                if (itemField != null)
                {
                    var item = itemField.GetValue(operation) as Item;
                    if (item != null && item is MedsItemClass medsItem)
                    {
                        var medItemType = medsItem.GetType();
                        var hpResourceField = GetFieldSafe(medItemType, "HpResource", "hpResource", "_hpResource");
                        
                        if (hpResourceField != null)
                        {
                            var hpResource = hpResourceField.GetValue(medsItem);
                            if (hpResource is float hpValue)
                            {
                                if (float.IsNaN(hpValue) || float.IsInfinity(hpValue) || hpValue < 0f)
                                {
                                    Plugin.REAL_Logger.LogWarning($"UIFixes: Fixed invalid HpResource before swap for {medsItem.LocalizedName()}: {hpValue} -> 0");
                                    hpResourceField.SetValue(medsItem, 0f);
                                }
                            }
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                Plugin.REAL_Logger.LogError($"Error validating swap operation: {ex.Message}");
            }
        }

        private static FieldInfo GetFieldSafe(System.Type type, params string[] fieldNames)
        {
            var allFields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var name in fieldNames)
            {
                var field = allFields.FirstOrDefault(f => f.Name.Equals(name, System.StringComparison.OrdinalIgnoreCase));
                if (field != null)
                    return field;
            }
            return null;
        }
    }

    /// <summary>
    /// Protects against DynamicItemWeights causing issues
    /// </summary>
    public class ItemWeightUpdateSafetyPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            var itemType = typeof(Item);
            var weightProperty = itemType.GetProperty("Weight");
            
            if (weightProperty == null)
            {
                Plugin.REAL_Logger.LogWarning("Could not find Item.Weight property");
                return null;
            }

            return weightProperty.GetGetMethod();
        }

        [PatchPostfix]
        private static void Postfix(Item __instance, ref float __result)
        {
            try
            {
                if (float.IsNaN(__result) || float.IsInfinity(__result))
                {
                    Plugin.REAL_Logger.LogWarning($"DynamicItemWeights: Fixed invalid weight for {__instance.LocalizedName()}: {__result} -> 0.1");
                    __result = 0.1f;
                }
                else if (__result < 0f)
                {
                    Plugin.REAL_Logger.LogWarning($"DynamicItemWeights: Fixed negative weight for {__instance.LocalizedName()}: {__result} -> 0.1");
                    __result = 0.1f;
                }
                else if (__result > 1000f)
                {
                    Plugin.REAL_Logger.LogWarning($"DynamicItemWeights: Fixed unreasonably large weight for {__instance.LocalizedName()}: {__result} -> 50");
                    __result = 50f;
                }
            }
            catch (System.Exception ex)
            {
                Plugin.REAL_Logger.LogError($"Error in ItemWeightUpdateSafetyPatch: {ex.Message}");
            }
        }
    }
}
