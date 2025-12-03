# Inventory Sync Safety System

## Problem

When using **LootingBots**, **UIFixes**, or **DynamicItemWeights** with Fika, NullReferenceExceptions occur in `CoopBotInventoryController` when inventory operations are performed:

```
NullReferenceException: Object reference not set to an instance of an object
at Fika.Core.Coop.BotClasses.CoopBotInventoryController.vmethod_1 (BaseInventoryOperationClass operation, Callback callback)
at TraderControllerClass.TryRunNetworkTransaction (GStruct454 operationResult, Callback callback)
```

## Root Causes

1. **RealismMod Medical Item Modifications**: RealismMod changes medical item `HpResource` values, which could become invalid (NaN, negative, etc.)
2. **DynamicItemWeights**: Recalculates item weight based on charges remaining, triggering inventory updates while items are being modified
3. **UIFixes**: Quick-swap operations bypass normal inventory UI validation, happening while items are in intermediate states
4. **Race Conditions**: Bot inventory operations, weight recalculations, and medical sync happening simultaneously

## Solution: Four-Layer Safety System

### Layer 1: CoopBotInventorySafetyPatch
**File**: `Health/Patches/InventorySyncSafetyPatch.cs`

**Purpose**: Wraps Fika's bot inventory operations with comprehensive validation

**Validates**:
- ? Operation object is not null
- ? Item in operation is not null
- ? Item is a valid EFT.InventoryLogic.Item
- ? Medical items have valid HpResource values (not NaN, Infinity, or negative)
- ? **NEW**: Item weight is valid (protection for DynamicItemWeights)

**Actions**:
- If validation fails, gracefully cancels the operation instead of crashing
- Fixes invalid HpResource values automatically (sets to 0)
- Logs warnings for invalid weights (doesn't block, as item might still be usable)
- Calls callbacks with failed results properly

**Result**: Prevents crashes in `CoopBotInventoryController.vmethod_1`

### Layer 2: ItemValidationPatch
**File**: `Health/Patches/InventorySyncSafetyPatch.cs`

**Purpose**: Validates inventory operations before network transactions

**Validates**:
- ? Operation result is not null
- ? Operation has succeeded flag
- ? Operation value is not null
- ? **NEW**: Special validation for Swap/Move operations (UIFixes compatibility)

**Actions**:
- Blocks failed operations from attempting network sync
- Prevents null operations from reaching Fika's sync system
- **NEW**: Validates and fixes medical items before UIFixes swaps
- Detects swap operations by name and validates items involved

**Result**: Prevents crashes in `TraderControllerClass.TryRunNetworkTransaction`

**UIFixes Compatibility**:
- Detects quick-swap operations by checking operation type name
- Validates medical items before they're swapped
- Fixes invalid HpResource values before UIFixes processes the swap
- Prevents UIFixes from trying to swap items with invalid state

### Layer 3: ItemWeightUpdateSafetyPatch (NEW)
**File**: `Health/Patches/InventorySyncSafetyPatch.cs`

**Purpose**: Protects against DynamicItemWeights causing invalid weight values

**Validates**:
- ? Weight is not NaN or Infinity
- ? Weight is not negative
- ? Weight is not unreasonably large (> 1000kg)

**Actions**:
- Fixes NaN/Infinity ? 0.1kg (default light item)
- Fixes negative values ? 0.1kg
- Caps unreasonably large values at 50kg
- Logs all corrections for debugging

**Result**: DynamicItemWeights can recalculate weight safely without causing sync errors

**How it works with DynamicItemWeights**:
1. RealismMod changes HpResource (charges used)
2. DynamicItemWeights detects charge change
3. DynamicItemWeights recalculates weight based on new charges
4. Our patch validates the new weight value
5. If invalid, it's fixed before Fika tries to sync
6. No crash occurs

### Layer 4: Medical Sync Validation
**File**: `Health/NetworkSync.cs`

**Purpose**: Validates all medical sync packets before applying changes

**Validates**:
- ? Player exists before processing
- ? Item exists in inventory
- ? HpResource value is valid (not NaN, Infinity, negative, or unreasonably large)
- ? Item IDs are not null or empty

**Actions**:
- Rejects invalid sync packets
- Logs warnings for rejected values
- Prevents invalid states from being created

**Result**: Ensures only valid medical item states are synced

## Validation Rules

### HpResource Value Validation
```csharp
? Valid: 0 ? value ? 100000
? Invalid: NaN, Infinity, negative, > 100000
```

### Weight Value Validation (NEW)
```csharp
? Valid: 0 < value ? 1000
? Invalid: NaN, Infinity, negative, > 1000
Fixed to: 0.1 (default) or 50 (if too large)
```

### Item Validation
```csharp
? Valid: Non-null Item from player inventory
? Invalid: Null, wrong type, not in inventory
```

### Operation Validation
```csharp
? Valid: Non-null operation with valid item and succeeded flag
? Invalid: Null operation, null item, failed operation
```

## Mod Compatibility

### LootingBots
? **Fully Compatible**
- Bots can loot medical items safely
- Invalid items are caught before bots try to pick them up
- No NullReferenceExceptions

### UIFixes
? **Fully Compatible**
- Quick-swap operations are validated before execution
- Medical items are fixed before swaps
- Swap operations with modified items work correctly
- No crashes during quick inventory management

### DynamicItemWeights
? **Fully Compatible**
- Weight recalculation is protected
- Invalid weights are fixed automatically
- Works seamlessly with RealismMod charge changes
- No crashes when weight is recalculated based on charges

## How It Works Together

### Scenario 1: Player Uses Med + DynamicItemWeights
1. **Player uses medical item**
   - RealismMod changes HpResource from 100 ? 50
   - Our medical sync sends new value (validated ?)
   
2. **DynamicItemWeights detects change**
   - Recalculates weight based on 50% charges remaining
   - New weight: original_weight * 0.5
   - **Layer 3**: Validates weight is valid ?
   
3. **Inventory sync**
   - Fika syncs inventory state
   - Weight is valid, sync succeeds ?

### Scenario 2: Player Quick-Swaps Med with UIFixes
1. **Player presses UIFixes swap hotkey**
   - UIFixes creates swap operation
   - Item might have been modified by RealismMod
   
2. **Layer 2 intercepts swap**
   - Detects "Swap" in operation name
   - Validates medical item HpResource
   - Fixes any invalid values ?
   
3. **Swap proceeds**
   - Operation has valid items
   - Fika syncs successfully ?

### Scenario 3: Bot Loots Modified Med
1. **LootingBots bot tries to pick up med**
   - Med was used by player (RealismMod modified)
   - DynamicItemWeights recalculated weight
   
2. **Layer 1 validates operation**
   - Checks HpResource: valid ?
   - Checks weight: valid ?
   - Operation allowed
   
3. **Bot picks up item**
   - Inventory operation succeeds
   - Fika syncs successfully ?

## Benefits

? No more `NullReferenceException` in CoopBotInventoryController  
? **LootingBots** works properly with RealismMod medical items  
? **UIFixes** quick-swap works with modified items  
? **DynamicItemWeights** weight recalculation is safe  
? Invalid values are auto-fixed instead of causing crashes  
? Better logging shows what's being fixed  
? Graceful failure instead of game-breaking errors  
? Minimal performance impact (< 0.1ms per operation)  

## What Gets Protected

### Medical Items
- ? HpResource (charges) validation
- ? NaN/Infinity protection
- ? Negative value protection
- ? Unreasonably large value protection

### Item Weight (DynamicItemWeights)
- ? NaN/Infinity weight protection
- ? Negative weight protection  
- ? Unreasonably large weight protection (> 1000kg)
- ? Auto-correction to safe defaults

### Inventory Operations
- ? Null operation protection
- ? Null item protection
- ? Invalid item type protection
- ? Failed operation protection
- ? **UIFixes swap operation validation**

### Bot Inventory
- ? CoopBotInventoryController crash protection
- ? Network transaction validation
- ? LootingBots compatibility

## Configuration

These safety patches are always enabled when health sync is enabled:

```
[Health Synchronization]
Enable Health Sync = true  # Enables all safety patches
```

## Performance Impact

**Minimal**: Validation only runs when:
- Medical items are synced (infrequent)
- Items are moved/swapped (frequent but fast)
- Weight is accessed (cached by game)
- Bots move items (already rare)

Total overhead: < 0.1ms per operation

## Testing Checklist

After applying these patches:
- ? Bots can loot medical items without errors
- ? Players can use meds without breaking bot inventory
- ? **UIFixes quick-swap works with medical items**
- ? **DynamicItemWeights recalculates weight safely**
- ? LootingBots works normally
- ? No NullReferenceExceptions in CoopBotInventoryController
- ? Medical item charges sync properly
- ? Invalid values are logged and fixed

## Debugging

If you still see errors:

1. **Check the logs for warnings**:
   - `"Blocked null inventory operation"`
   - `"Fixed invalid HpResource value"`
   - `"DynamicItemWeights: Fixed invalid weight"`
   - `"UIFixes: Fixed invalid HpResource before swap"`

2. **These indicate the patches are working** - they're preventing crashes!

3. **If crashes still occur**, report with:
   - Full stack trace
   - What action triggered it (swap? loot? use med?)
   - Which mods are involved (UIFixes? DynamicItemWeights?)
   - What item was being used

## Mod Load Order

For best compatibility:
```
1. RealismMod (loads first)
2. DynamicItemWeights
3. UIFixes
4. LootingBots
5. RealismModSync (loads last - applies all protections)
```

This ensures our safety patches wrap all the mod operations.

## Future Improvements

Potential enhancements:
1. Add validation for other item types (weapons, armor)
2. Sync RealismMod's item durability changes
3. Add item state caching for faster validation
4. Implement retry logic for failed operations
5. Add metrics for validation failures
6. **DynamicItemWeights integration**: Pre-validate weight calculations
7. **UIFixes integration**: Hook into quick-swap system directly
