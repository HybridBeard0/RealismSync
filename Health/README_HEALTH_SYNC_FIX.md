# RealismMod Health Sync - Fika Compatibility Fix

## Problem

When a friend in Fika coop uses medical items, a `NullReferenceException` occurs in the base game's health synchronization system:

```
NullReferenceException: Object reference not set to an instance of an object
at GClass2862+GClass2864`1[T].smethod_0 (System.String effectType)
at NetworkHealthControllerAbstractClass.HandleSyncPacket (NetworkHealthSyncPacketStruct packet)
```

## Root Cause

RealismMod adds custom health effects (tourniquets, surgery, custom healing) that don't have proper network identifiers. When these effects are synced through Fika's native health synchronization system, the `effectType` string is null, causing the NullReferenceException when Fika tries to deserialize and create the effect on the client side.

## Solution

### 1. FikaHealthSyncCompatibilityPatch.cs
This patch intercepts Fika's `HandleSyncPacket` method and filters out any health sync packets with null or empty effect types before they can cause exceptions.

**How it works:**
- Runs before `NetworkHealthControllerAbstractClass.HandleSyncPacket`
- Checks if the packet's `ExtraData.EffectType` is null or empty
- If null/empty (RealismMod custom effect), returns `false` to skip processing
- If valid (normal game effect), returns `true` to allow normal processing

**Result:** Prevents crashes when RealismMod custom effects try to sync through Fika's system.

### 2. RealismCustomEffectPatch.cs
This patch tracks when RealismMod adds custom effects to the health controller, providing visibility and a foundation for future custom effect synchronization.

**How it works:**
- Intercepts `RealismHealthController.AddCustomEffect`
- Logs when custom effects are added (for debugging)
- Provides hooks for future implementation of custom effect sync

**Result:** We can monitor and potentially implement proper sync for RealismMod's custom effects in the future.

### 3. Med Charges Sync (Existing)
The existing `HandleHealthEffectsSyncPatch` and `MedEffectSyncPatch` continue to sync medical item charges (HP resources) through our custom packet system, bypassing the problematic native health sync.

**How it works:**
- Syncs medical item HP resource changes through `RealismMedicalSyncPacket`
- Updates other players' inventories to match the healer's med charges
- Prevents inventory desync issues

## Why This Works

1. **Separation of Concerns**: Normal game health effects use Fika's native sync (now filtered for safety), while RealismMod effects are handled separately
2. **Fail-Safe**: If an invalid effect somehow gets through, it's caught and blocked rather than crashing
3. **Charge Sync**: Medical item charges are synced through our custom system, preventing the main desync issue
4. **Non-Breaking**: We don't prevent RealismMod from working, we just prevent its custom effects from breaking Fika's sync

## What Gets Synced

? **Successfully Synced:**
- Medical item HP resource (charges remaining)
- Med usage events
- Inventory state

?? **Blocked (Prevents Crashes):**
- RealismMod custom effects (tourniquets, surgery, custom healing modifiers)
- These effects only apply to the local player, not synced to teammates

## Future Improvements

To fully sync RealismMod's custom effects:
1. Implement custom network packets for tourniquet application
2. Add surgery effect synchronization
3. Sync custom healing modifiers
4. This would require deeper integration with RealismMod's effect system

## Testing

After these patches:
- ? Medical items can be used without crashes
- ? Med charges sync properly across clients
- ? RealismMod effects work correctly on the user
- ?? RealismMod custom effects (tourniquets, surgery) only apply to the user, not visible to teammates
- ? No more NullReferenceExceptions in health sync

## Configuration

Health sync can be disabled in the config if needed:
```
[Health Synchronization]
Enable Health Sync = true
```

When disabled, all health sync patches are not applied.
