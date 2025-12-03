# Quick Test Guide: ObservedCoopPlayer Spawn Fix

## Before You Start

**Update RealismModSync to v1.0.5** which includes the health serialization fix.

## Test Scenario 1: Basic Multiplayer Spawn

### Steps:
1. Host creates a Fika raid (any map)
2. Second player joins the session
3. Both players wait for spawn

### Expected Result: ?
- Both players spawn successfully
- No crashes
- No NullReferenceException errors

### If It Fails: ?
Check logs for:
```
[Error] NullReferenceException in NetworkHealthController constructor
```

? Report this with full log file

## Test Scenario 2: Verify Patch Loaded

### Steps:
1. Start the game
2. Wait for all mods to load
3. Open the log file: `BepInEx/LogOutput.log`

### Expected Result: ?
Look for this message during startup:
```
[Info:RealismModSync] RealismMod health serialization patch applied (primary)
```

OR

```
[Info:RealismModSync] RealismMod health serialization patch applied (alternative)
```

### If Neither Message Appears: ?
You should see:
```
[Warning:RealismModSync] Could not apply health serialization patch
```

? This means the patch failed to find the target method. Report this!

## Test Scenario 3: Verify Filtering (Optional)

### Steps:
1. Enable debug logging in RealismModSync config
2. Host a raid
3. Client joins
4. Check logs during spawn

### Expected Result: ?
You should see (in debug logs):
```
[Debug:RealismModSync] Filtered out custom effect for serialization: PassiveHealthRegenEffect
[Debug:RealismModSync] Filtered out custom effect for serialization: ResourceRateEffect
[Info:RealismModSync] Removed 2 custom RealismMod effects from health serialization
```

**Note**: This only appears if RealismMod has added custom effects to the player.

## Test Scenario 4: Medical Sync Still Works

### Steps:
1. Both players spawn successfully (Test 1 passed ?)
2. Player 1 uses a medical item (bandage, medkit, etc.)
3. Player 2 checks Player 1's inventory

### Expected Result: ?
- Medical item charges decrease correctly
- Both players see the same item state
- No errors in logs

### If Medical Sync Broken: ?
```
[Error:RealismModSync] Failed to sync medical item
```

? This is a different issue, report it separately

## Common Issues & Solutions

### Issue 1: Patch Not Loading

**Symptoms**:
```
[Warning:RealismModSync] Could not apply health serialization patch
```

**Possible Causes**:
1. RealismMod not installed
2. RealismMod version incompatibility
3. Method signature changed in EFT update

**Solution**:
1. Verify RealismMod is installed and enabled
2. Check RealismModSync and RealismMod versions match SPT version
3. Report the issue with mod versions

### Issue 2: Still Crashing on Spawn

**Symptoms**:
```
[Error] NullReferenceException in NetworkHealthController constructor
```

**Possible Causes**:
1. Patch loaded but didn't filter effects
2. Another mod adding custom effects
3. Different serialization path used

**Debug Steps**:
1. Check if patch loaded (Test 2)
2. Enable debug logging and check if filtering occurs (Test 3)
3. Disable other health-affecting mods one by one
4. Report with full mod list + logs

### Issue 3: No Debug Messages About Filtering

**Symptoms**:
- Patch loads successfully
- Spawn works
- But no "Filtered out custom effect" messages

**This is NORMAL if**:
- RealismMod hasn't added custom effects yet
- Player doesn't have any active custom effects at spawn time

**Only report if**:
- Patch loads ?
- Spawn crashes ?
- No filtering messages ?

## What To Include When Reporting Issues

### 1. Full Log File
`BepInEx/LogOutput.log` or `BepInEx/FullLogOutput.log`

### 2. Mod List
List of ALL installed mods (especially health-affecting mods):
- RealismMod (version)
- RealismModSync (version)
- SPT version
- Fika version
- Any other health/medical mods

### 3. Exact Error Message
Copy the full error stack trace from logs:
```
[Error] NullReferenceException: ...
Stack trace:
  at ...
  at ...
```

### 4. Reproduction Steps
1. What you were doing
2. When it crashed
3. Host or client?
4. How many players?

## Success Criteria

All of these should be ?:

- [ ] Patch loads successfully (Test 2)
- [ ] Both players spawn without crash (Test 1)
- [ ] Medical sync still works (Test 4)
- [ ] No NullReferenceException errors in logs

If all ? ? **Fix is working perfectly!**

## Performance Check

After fix is working:

### Expected:
- No noticeable performance difference
- Same FPS as before
- No lag during spawn
- No lag during medical item use

### If Performance Issues:
- This fix adds < 0.01ms overhead
- Performance issues are from another cause
- Check other mods

## Additional Notes

### Custom Effects Are Local

After this fix:
- ? RealismMod custom effects work locally
- ? Custom effects don't sync to other players
- ? Medical item usage still syncs
- ? Vanilla effects (bleeding, fractures) still sync

**This is intentional and acceptable** because:
- Passive effects are personal modifiers
- Medical actions are synced through dedicated packet system
- Each player can have different passive regen/drain rates

### Update Frequency

Re-test after:
- **SPT updates** - May change method signatures
- **Fika updates** - May change serialization code
- **RealismMod updates** - May add new custom effects
- **RealismModSync updates** - May improve fix

## Quick Checklist

Before reporting issues:

- [ ] Updated to RealismModSync v1.0.5+
- [ ] Verified patch loads (Test 2)
- [ ] Tested basic spawn (Test 1)
- [ ] Checked medical sync (Test 4)
- [ ] Collected full log file
- [ ] Listed all installed mods
- [ ] Tried disabling other health mods

If all done and still issues ? Report with all info!

---

## TL;DR

1. Update RealismModSync
2. Start game, check logs for: `"RealismMod health serialization patch applied"`  
3. Test multiplayer spawn
4. If crashes persist ? report with logs + mod list

**Expected**: No crashes, smooth multiplayer, medical sync works
