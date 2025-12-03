# Quest Extended Sync Module

## Overview

This module adds **multiplayer synchronization** for the Quest Extended mod in Fika co-op sessions. Without this sync, optional quest conditions and multi-choice quests completed by one player don't sync to other players, causing desynchronization issues.

## The Problem

Quest Extended adds advanced quest features to SPT:
- **Optional quest conditions** - Complete different objectives to finish the same quest
- **Multi-choice quests** - Choose between different quest paths
- **Custom quest conditions** - Extended condition types beyond vanilla

### Without Sync:
```
Player 1: Completes Optional Condition A
Player 2: Still sees condition as incomplete
Result: Quest states don't match, potential bugs
```

### Error Without Sync:
```
[Error:Quests Extended] NullReferenceException in HandleQuestStartingConditionCompletion
at OptionalConditionController.HandleVanillaConditionChanged
```

This happens during extraction when Quest Extended tries to process conditions but encounters null references due to desynchronization.

## The Solution

### What Gets Synced

? **Optional Condition Progress**
- When a player makes progress on an optional condition
- Current value is synced to all clients
- Example: "Kill 5 PMCs" - each kill syncs progress

? **Optional Condition Completions**
- When a player completes an optional condition
- All clients receive the completion event
- Quest Extended's completion handler runs for everyone

? **Multi-Choice Quest Starts** (Future)
- When a player starts a multi-choice quest variant
- All clients see the same quest path chosen

? **Custom Condition Updates**
- Any Quest Extended custom condition changes
- Synced to keep all clients in same state

## How It Works

### Architecture

```
Quest Extended
    ?
RealismModSync patches
    ?
Condition completion detected
    ?
Create sync packet
    ?
Send via Fika network
    ?
All clients receive
    ?
Update local quest state
```

### Sync Flow Example

**Player 1 completes optional condition**:
```
1. Quest Extended: HandleVanillaConditionChanged
   ?
2. Our Patch (Postfix): Detect completion
   ?
3. Create QuestExtendedSyncPacket
   ?
4. Send to Fika Server/Clients
   ?
5. All clients receive packet
   ?
6. Update their local Quest Extended state
   ?
7. All players now have synced quest progress
```

## Implementation Details

### Patches Applied

#### 1. HandleVanillaConditionChangedPatch
**Patches**: `OptionalConditionController.HandleVanillaConditionChanged`

**Purpose**: Syncs vanilla condition progress changes

**What it does**:
- Detects when a condition value changes
- Finds which quest the condition belongs to
- Sends sync packet with new progress value
- Prevents duplicate syncs with tracking

#### 2. HandleQuestStartingConditionCompletionPatch
**Patches**: `OptionalConditionController.HandleQuestStartingConditionCompletion`

**Purpose**: Syncs optional condition completions

**What it does**:
- Detects when an optional condition completes
- Marks condition as synced (prevent echo)
- Sends completion packet to all clients
- Triggers Quest Extended's completion handler on all clients

### Packet Structure

```csharp
public struct QuestExtendedSyncPacket
{
    public string QuestId;        // Which quest
    public string ConditionId;    // Which condition
    public EQuestSyncType SyncType;   // What changed
    public int CurrentValue;      // New progress value
    public bool IsCompleted;      // Is it completed?
}
```

### Sync Types

```csharp
public enum EQuestSyncType
{
    ConditionProgress = 0,          // Progress update (e.g., 3/5 kills)
    ConditionCompleted = 1,         // Condition fully completed
    OptionalChoiceMade = 2,         // Player made optional choice (future)
    MultiChoiceQuestStarted = 3     // Multi-choice variant started (future)
}
```

## Configuration

```ini
[Quest Extended Synchronization]
Enable Quest Sync = true
```

**Enable Quest Sync**: Master toggle for all Quest Extended synchronization

## Compatibility

### Required Mods
- ? **Fika.Core** - Multiplayer framework
- ? **Quest Extended** - The mod we're syncing

### Optional
Quest Extended is detected automatically. If not installed, sync module is disabled gracefully.

## Features

### ? Implemented

**Condition Progress Sync**:
- Syncs incremental progress (e.g., 3/5 kills)
- Updates all clients in real-time
- Prevents progress loss

**Condition Completion Sync**:
- Syncs when optional conditions complete
- Triggers Quest Extended handlers on all clients
- Keeps quest states synchronized

**Duplicate Prevention**:
- Tracks synced conditions to prevent echo
- Avoids infinite sync loops
- Prevents packet spam

**Error Prevention**:
- Prevents NullReferenceExceptions during extraction
- Validates quest/condition existence before sync
- Graceful fallback if Quest Extended not found

### ?? Future Enhancements

**Multi-Choice Quest Sync**:
- Sync quest variant selections
- All players see same quest path chosen

**Optional Choice Sync**:
- Sync which optional objectives were chosen
- Coordinate team quest strategies

**Quest State Reconciliation**:
- Periodic full state sync
- Recover from desync situations

## Testing

### Test Scenarios

1. **Optional Condition Progress**:
   - Player 1: Make progress on optional condition
   - Player 2: Should see same progress
   - ? Both players' progress matches

2. **Optional Condition Completion**:
   - Player 1: Complete optional condition
   - Player 2: Condition auto-completes
   - ? Quest completes for both players

3. **Multiple Players**:
   - 3+ players in raid
   - One completes condition
   - ? All players receive sync

4. **Extraction**:
   - Complete condition then extract
   - ? No NullReferenceExceptions
   - ? Quest state persists

### What to Check

**In logs**:
```
[Info:RealismModSync] Quest Extended detected - sync enabled
[Info:RealismModSync] Applied HandleVanillaConditionChangedPatch
[Info:RealismModSync] Applied HandleQuestStartingConditionCompletionPatch
[Info:RealismModSync] Quest Extended Fika events registered
```

**During gameplay**:
```
[Info:RealismModSync] Synced quest condition progress: quest123/cond456 = 3
[Info:RealismModSync] Synced optional condition completion: quest123/cond456
```

**On clients**:
```
[Info:RealismModSync] Client received quest sync packet: quest123/cond456
[Info:RealismModSync] Updated quest condition progress: quest123/cond456 = 3
```

## Known Limitations

1. **Server-side quest validation**: 
   - SPT server still validates quest states
   - Sync keeps clients in agreement
   - Server is source of truth

2. **Quest Extended version compatibility**:
   - Patches Quest Extended's current methods
   - May need updates if Quest Extended changes significantly

3. **Network delay**:
   - Small delay (< 1 second) for sync to propagate
   - Not instant but fast enough for gameplay

## Troubleshooting

### Quest conditions not syncing

**Check**:
1. Quest Extended is installed on all clients
2. `Enable Quest Sync = true` in config
3. All players running same Quest Extended version
4. Check logs for "Quest Extended detected"

### NullReferenceException during extraction

**This is what we fix!** If you still see them:
1. Update to latest RealismModSync
2. Check Quest Extended sync patches applied
3. Report to mod author with logs

### Desync after rejoining

**Current limitation**: 
- Quest state syncs during raid only
- Rejoining mid-raid may have desync
- Future enhancement: state reconciliation

## Performance Impact

**Negligible**:
- Packets sent only when conditions change (infrequent)
- Small packet size (~100 bytes)
- Reliable delivery (no spam)
- No continuous polling

**Total overhead**: < 0.1ms per condition change

## Credits

- **DrakiaXYZ** - Original Quest Extended mod
- **Fika Team** - Multiplayer framework
- **RealismModSync** - This integration

## Version History

### 1.0.4
- Initial Quest Extended sync implementation
- Condition progress and completion sync
- Extraction error prevention
- Duplicate sync prevention

## Future Roadmap

- [ ] Multi-choice quest sync
- [ ] Optional quest choice sync
- [ ] Full quest state reconciliation
- [ ] Quest progress UI notifications
- [ ] Admin commands for quest sync debugging
