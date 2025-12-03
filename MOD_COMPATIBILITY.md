# RealismModSync - Mod Compatibility Matrix

## Fully Compatible Mods

### ? Quest Extended
**What it does**: Adds advanced quest features (optional conditions, multi-choice quests, custom conditions)  
**Potential conflicts**: Quest state desync in multiplayer without sync  
**Protection**: Quest Extended sync module synchronizes all quest progress  
**Status**: **Fully Synced** - Optional conditions and completions synchronized  
**How it works**:
1. Player completes optional condition
2. RealismModSync syncs to all clients
3. Quest Extended's completion handler runs for everyone
4. All players have matching quest states

### ? LootingBots
**What it does**: Allows bots to loot items from corpses and containers  
**Potential conflicts**: Bot inventory sync with modified medical items  
**Protection**: CoopBotInventorySafetyPatch validates items before bots pick them up  
**Status**: **Fully Protected** - No crashes

### ? UIFixes  
**What it does**: Quick weapon/item swapping without opening inventory UI  
**Potential conflicts**: Swap operations while RealismMod is modifying items  
**Protection**: ItemValidationPatch validates items before UIFixes swaps  
**Status**: **Fully Protected** - Quick-swaps work perfectly  
**Note**: Avoid swapping equipped weapons (known UIFixes bug), but medical items are safe

### ? DynamicItemWeights
**What it does**: Changes item weight based on remaining uses/charges  
**Potential conflicts**: Weight recalculation after RealismMod changes charges  
**Protection**: ItemWeightUpdateSafetyPatch validates and fixes weight values  
**Status**: **Fully Protected** - Weight updates safely  
**How it works**:
1. You use med, RealismMod reduces charges
2. DynamicItemWeights recalculates weight based on new charges
3. Our patch validates the new weight
4. If invalid (NaN, Infinity, negative) ? fixed automatically
5. Fika syncs valid weight

### ? BringMeToLifeMod (Revival Mod)
**What it does**: Adds unconscious/revival mechanics  
**Potential conflicts**: Health controller updates during unconscious state  
**Protection**: Core.IsPlayerUnconsciousOrReviving() checks revival state  
**Status**: **Fully Compatible** - Health updates properly during revival

## Incompatible Mods

### ? com.lacyway.rsr (Old RSR - Realism Stance Replication)
**Reason**: Conflicts with RealismModSync's stance replication  
**Action**: Use RealismModSync instead  
**Status**: **Hard Incompatibility** declared in Plugin.cs

## Required Mods

### ?? Fika.Core
**Version**: Latest (tested with current Fika release)  
**Dependency**: Hard dependency  
**Purpose**: Multiplayer framework that RealismModSync extends

### ?? RealismMod
**Version**: Latest (tested with current version)  
**Dependency**: Hard dependency  
**Purpose**: The mod we're synchronizing across Fika clients

## Optional Mods

### ? Quest Extended
**Version**: Latest (tested with current version)  
**Dependency**: Soft dependency (auto-detected)  
**Purpose**: Advanced quest features with RealismModSync synchronization  
**If installed**: Quest sync enabled automatically  
**If not installed**: Quest sync module disabled gracefully

## Recommended Load Order

```
1. SPT Core & Fika.Core (framework)
2. RealismMod (base mod)
3. BringMeToLifeMod (optional)
4. Quest Extended (optional)
5. DynamicItemWeights (optional)
6. UIFixes (optional)
7. LootingBots (optional)
8. RealismModSync (LOAD LAST)
```

**Why RealismModSync should load last**:
- Our patches need to wrap other mods' operations
- Safety validations need to catch all modifications
- Network sync needs to see final item states
- Quest Extended sync patches need QE loaded first

## What Gets Synced

### ? Synchronized Across Clients
- **Medical item charges (HpResource)**
- **Med usage events**
- **Item state after modifications**
- **Weight changes** (if DynamicItemWeights is installed)
- **Quest Extended optional condition progress** (NEW in 1.0.4!)
- **Quest Extended condition completions** (NEW in 1.0.4!)

### ?? Local Only (Not Synced)
- RealismMod custom effects (tourniquets, surgery)
  - These only apply to the user
  - Teammates won't see these effects
  - **Why**: Native Fika health sync can't handle custom effect types
- Visual/animation effects (always local)
- Stance replication (handled by StanceReplication module)

## Known Issues & Workarounds

### Issue: Weapon Disappears After Equipped Weapon Swap (UIFixes)
**Cause**: Known UIFixes bug, not related to RealismModSync  
**Workaround**: 
1. Switch to different weapon first
2. Then replace the weapon slot
3. Then switch back
**Our Protection**: Prevents crashes, but can't fix UIFixes bug itself

### Issue: Bot Inventory Errors in Logs
**Symptoms**: 
```
[Error:LootingBots] NullReferenceException in CoopBotInventoryController
```
**Cause**: Various mods modifying items simultaneously  
**Our Fix**: All patched and protected automatically  
**If you see this**: The patches are working! Check logs for:
- "Fixed invalid HpResource value" ? Working
- "Fixed invalid weight" ? Working  
- "Blocked null inventory operation" ? Working

These are **good** - they mean crashes were prevented!

### Issue: Quest Extended NullReferenceException During Extraction (FIXED!)
**Symptoms**:
```
[Error:Quests Extended] NullReferenceException in HandleQuestStartingConditionCompletion
```
**Cause**: Quest state desync between clients  
**Our Fix**: Quest Extended sync module (v1.0.4+)  
**Status**: **RESOLVED** - Quest progress syncs automatically

## Performance Impact

| Patch | When Active | Overhead |
|-------|-------------|----------|
| CoopBotInventorySafetyPatch | Bot picks up item | < 0.05ms |
| ItemValidationPatch | Any inventory operation | < 0.05ms |
| ItemWeightUpdateSafetyPatch | Weight accessed | < 0.01ms (cached) |
| Medical Sync | Med item used | < 0.1ms |
| **Quest Extended Sync** | **Condition completed** | **< 0.1ms** |

**Total**: Negligible impact on gameplay performance

## Configuration

All compatibility patches are enabled automatically when modules are enabled:

```ini
[Health Synchronization]
Enable Health Sync = true  # Enables medical sync + inventory safety

[Quest Extended Synchronization]
Enable Quest Sync = true  # Enables quest condition sync
```

No additional configuration needed for mod compatibility!

## Testing Your Setup

1. **Test LootingBots**:
   - Use a medical item
   - Kill a bot
   - Let another bot loot the body
   - ? Should work without errors

2. **Test UIFixes**:
   - Use a medical item partially
   - Use UIFixes hotkey to swap it
   - ? Should swap without crashes

3. **Test DynamicItemWeights**:
   - Pick up a medical item
   - Note its weight
   - Use it partially
   - Check weight again
   - ? Weight should update smoothly

4. **Test Quest Extended** (NEW!):
   - Start co-op raid with Quest Extended quest
   - Player 1: Complete optional condition
   - Player 2: Check quest progress
   - ? Condition should auto-complete for all players

5. **Test Multiplayer**:
   - Join Fika coop
   - You use a medical item
   - Friend checks your inventory
   - ? Charges should match

## Getting Help

If you have issues:

1. **Check your logs** for:
   - "Fixed invalid..." messages (patches working!)
   - "Quest Extended detected" (QE sync active)
   - Actual errors (report these!)

2. **Provide this info**:
   - Which mods you have installed
   - What action caused the error
   - Full error stack trace
   - Log file excerpt

3. **Common solutions**:
   - Make sure RealismModSync loads LAST
   - Disable UIFixes temporarily if weapon swap bug occurs
   - Check that all mods are up to date
   - For Quest Extended: Ensure all players have same QE version

## Version History

### 1.0.4 (Current)
- ? Added Quest Extended synchronization
- ? Syncs optional condition progress
- ? Syncs condition completions
- ? Prevents Quest Extended extraction errors
- ? Graceful fallback if QE not installed

### 1.0.3
- Fixed extraction NullReferenceException spam
- Added IsNetworkActive() checks
- Improved PacketSender disposal detection

### 1.0.2
- Added DMD (dynamic method) patching support
- Fixed CoopBotInventorySafetyPatch for Fika wrappers
- Eliminated AccessTools.Field warnings

### 1.0.1
- Added inventory sync safety patches
- UIFixes compatibility
- DynamicItemWeights compatibility
- LootingBots protection

### 1.0.0
- Initial release
- Health sync
- Stance replication
- Hazard zones sync
- Audio sync
