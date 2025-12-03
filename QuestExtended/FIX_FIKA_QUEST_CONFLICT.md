# Fix: Quest Extended Sync Interfering with Fika's sharedQuestProgression

## The Problem

**Symptoms**:
- PMC kills don't count for teammates in Fika co-op
- Vanilla quest progress doesn't sync
- Quest progress worked in SPT 3.10.5 but broken in SPT 3.11.4 with RealismModSync

**Root Cause**:
The Quest Extended sync module was patching `HandleVanillaConditionChanged`, which handles **ALL quest conditions** - both Quest Extended-specific and vanilla quests. This was interfering with Fika's native `sharedQuestProgression` feature.

## How Fika's Quest Sharing Works

**Fika's Built-in Feature** (`sharedQuestProgression`):
- Automatically syncs vanilla quest progress (PMC kills, looting, etc.)
- Works natively without mods
- Enabled in Fika's config

**The Conflict**:
```
Player 1 kills PMC
    ?
Fika's sharedQuestProgression tries to sync
    ?
Quest Extended Sync intercepts HandleVanillaConditionChanged
    ?
RealismModSync processes it (but blocks vanilla quests)
    ?
Fika's sync gets interfered with
    ?
PMC kill doesn't count for Player 2 ?
```

## The Solution

### Two-Tier Quest Detection

**Quest Extended Quests** (sync via RealismModSync):
- Quests with `CompleteOptionals` condition
- Custom Quest Extended features
- Multi-choice quests
- Synced by RealismModSync

**Vanilla Quests** (sync via Fika):
- Standard SPT quests (PMC kills, looting, etc.)
- Handled by Fika's sharedQuestProgression
- RealismModSync doesn't interfere

### Implementation

#### 1. New Config Option

```ini
[Quest Extended Synchronization]
Enable Quest Sync = true

# NEW: Only sync Quest Extended-specific quests
Only Sync Quest Extended Conditions = true
```

**`Only Sync Quest Extended Conditions`**:
- `true` (default): Only syncs Quest Extended features, lets Fika handle vanilla quests
- `false`: Syncs all quests (not recommended with Fika's sharedQuestProgression)

#### 2. Quest Type Detection

**Method**: `Core.IsQuestExtendedQuest(questId)`

Checks if a quest is Quest Extended-specific by looking for:
- Condition type name contains "CompleteOptionals"
- Condition type name contains "Optional"
- Quest has Quest Extended custom conditions

**Cache**: Results cached to avoid repeated checks

#### 3. Patch Filtering

**Before** (synced everything):
```csharp
[PatchPostfix]
private static void Postfix(string conditionId, int currentValue)
{
    // Synced ALL conditions (vanilla + QE)
    SendSyncPacket(conditionId, currentValue);
}
```

**After** (filters by quest type):
```csharp
[PatchPostfix]
private static void Postfix(string conditionId, int currentValue)
{
    var questId = FindQuestIdForCondition(conditionId);
    
    // Only sync if it's a Quest Extended quest
    if (!Core.IsQuestExtendedQuest(questId))
    {
        // This is a vanilla quest - let Fika handle it
        return;
    }
    
    // Only Quest Extended quests get synced
    SendSyncPacket(conditionId, currentValue);
}
```

## How It Works Now

### Vanilla Quest (PMC Kill)

```
Player 1 kills PMC
    ?
HandleVanillaConditionChanged called
    ?
RealismModSync Patch: Is this a QE quest?
    ?
NO - it's vanilla
    ?
RealismModSync: Skip sync (return early)
    ?
Fika's sharedQuestProgression: Syncs the kill
    ?
Player 2 gets PMC kill credit ?
```

### Quest Extended Quest (Optional Condition)

```
Player 1 completes optional condition
    ?
HandleVanillaConditionChanged called
    ?
RealismModSync Patch: Is this a QE quest?
    ?
YES - has CompleteOptionals condition
    ?
RealismModSync: Create sync packet
    ?
Send to all clients
    ?
All players: Condition completes ?
```

## Configuration Guide

### Recommended Setup (Default)

```ini
[Quest Extended Synchronization]
Enable Quest Sync = true
Only Sync Quest Extended Conditions = true
```

**Use when**:
- Using Fika's sharedQuestProgression
- Want vanilla quests to sync via Fika
- Only need Quest Extended features synced

**Result**:
- ? PMC kills sync (via Fika)
- ? Vanilla quest progress syncs (via Fika)
- ? Quest Extended optional conditions sync (via RealismModSync)

### Alternative Setup (Not Recommended)

```ini
[Quest Extended Synchronization]
Enable Quest Sync = true
Only Sync Quest Extended Conditions = false
```

**Use when**:
- Fika's sharedQuestProgression is disabled
- You want RealismModSync to handle ALL quests
- Testing or debugging

**Result**:
- ? May conflict with Fika's quest sync
- ? All quests synced via RealismModSync
- ?? Not recommended for normal use

### Disable Quest Extended Sync

```ini
[Quest Extended Synchronization]
Enable Quest Sync = false
```

**Use when**:
- Quest Extended not installed
- Only want Fika's vanilla quest sync
- Troubleshooting

**Result**:
- ? Vanilla quests sync (via Fika)
- ? Quest Extended features not synced
- ?? QE quests may desync

## Testing

### Test Vanilla Quest Sync (PMC Kills)

1. **Setup**:
   - Enable Fika's sharedQuestProgression
   - Set `Only Sync Quest Extended Conditions = true`
   - Start co-op raid with PMC kill quest

2. **Test**:
   - Player 1: Kill a PMC
   - Player 2: Check quest progress
   - ? Both should have +1 PMC kill

3. **Expected Logs**:
   ```
   [Info:Fika] Quest progress synced (via Fika's native sync)
   ```

   **Should NOT see**:
   ```
   [Info:RealismModSync] Synced quest condition progress
   ```
   (RealismModSync shouldn't interfere with vanilla quests)

### Test Quest Extended Sync (Optional Conditions)

1. **Setup**:
   - Install Quest Extended
   - Set `Enable Quest Sync = true`
   - Start co-op with QE quest

2. **Test**:
   - Player 1: Complete optional condition
   - Player 2: Check quest
   - ? Condition should auto-complete

3. **Expected Logs**:
   ```
   [Info:RealismModSync] Synced Quest Extended condition progress: ...
   ```

## Troubleshooting

### PMC kills still not syncing

**Check**:
1. Fika's `sharedQuestProgression` enabled in Fika config?
2. `Only Sync Quest Extended Conditions = true` in RealismModSync config?
3. Both players on same Fika version?
4. Check Fika logs for quest sync messages

**If still not working**:
- Temporarily set `Enable Quest Sync = false` to test if RealismModSync is interfering
- Check Fika's config.json for sharedQuestProgression setting
- Verify both players connected to same Fika server

### Quest Extended quests not syncing

**Check**:
1. `Enable Quest Sync = true`?
2. Quest Extended installed on all clients?
3. Quest is actually a Quest Extended quest (has CompleteOptionals)?
4. Check logs for "Synced Quest Extended condition"

**If quest isn't detected as QE quest**:
- Verify quest has CompleteOptionals or Optional condition
- Check Quest Extended version compatibility
- Temporarily set `Only Sync Quest Extended Conditions = false` to force sync

### Both vanilla and QE quests not syncing

**Likely issue**: Configuration conflict

**Try**:
1. Default config: `Enable Quest Sync = true, Only Sync QE = true`
2. Restart game/server
3. Test vanilla quest first (via Fika)
4. Test QE quest second (via RealismModSync)

## Performance Impact

**Negligible**:
- Quest type detection cached (only checked once per quest)
- Early return for vanilla quests (no packet creation)
- Fika handles vanilla quests natively
- Only Quest Extended quests create sync packets

## Credits

- **Fika Team** - Native sharedQuestProgression feature
- **DrakiaXYZ** - Quest Extended mod
- **User** - Reported PMC kill sync issue

## Version History

### 1.0.4.1 (Fix)
- ? Added `Only Sync Quest Extended Conditions` config
- ? Quest type detection (QE vs vanilla)
- ? Filtered patches to not interfere with Fika
- ? Fixed PMC kill sync issues
- ? Maintained Quest Extended sync for optional conditions

### 1.0.4 (Original)
- ? Synced all quests (interfered with Fika)
- ? PMC kills didn't count for teammates

---

**Recommended Config**:
```ini
[Quest Extended Synchronization]
Enable Quest Sync = true
Only Sync Quest Extended Conditions = true  # Let Fika handle vanilla quests!
```

This gives you the best of both worlds:
- ? Fika syncs vanilla quests (PMC kills, etc.)
- ? RealismModSync syncs Quest Extended features
- ? No conflicts or interference
