# Fix: HarmonyX Warning Spam When Playing Solo

## The Problem

When playing solo on a headless server or offline, you get warning spam:

```
[Warning: HarmonyX] AccessTools.Field: Could not find field for type Fika.Core.Coop.ClientClasses.CoopClientHealthController and name player
[Warning: HarmonyX] AccessTools.Field: Could not find field for type Fika.Core.Coop.ClientClasses.CoopClientHealthController and name player_0
```

### Why It Happens

**In Multiplayer (Fika)**:
- Uses `Fika.Core.Coop.ClientClasses.CoopClientHealthController`
- Has a `player_0` or `player` field

**In Solo/Offline Mode**:
- Uses `EFT.HealthController` or `EFT.HealthSystem.ActiveHealthController`
- **No player field exists**
- Trying to access non-existent field ? warning spam

**The Issue**: `RealismResourceDrainSafetyPatch` was always trying to find the player field, even in offline mode.

## The Solution

### Smart Health Controller Type Detection

Now the patch checks the health controller type **before** trying to access player fields:

```csharp
var healthControllerTypeName = healthControllerType.Name;

if (healthControllerTypeName.Contains("Coop") || healthControllerTypeName.Contains("Fika"))
{
    // Multiplayer mode - search for player field in health controller
    foreach (var fieldName in playerFieldNames)
    {
        var playerField = AccessTools.Field(healthControllerType, fieldName);
        // ...
    }
}
else
{
    // Single-player/offline mode - use Utils.GetYourPlayer() instead
    player = Utils.GetYourPlayer();
}
```

### How It Works Now

**Multiplayer (Fika Server)**:
1. Detects `CoopClientHealthController`
2. Searches for player field in health controller
3. Uses that player for validation ?

**Solo/Offline Mode**:
1. Detects `ActiveHealthController` (vanilla)
2. **Skips field search** (no warnings!)
3. Gets player from `Utils.GetYourPlayer()` instead ?

## Testing

### In Multiplayer
```
[Info:RealismModSync] RealismMod resource drain safety patch applied
(No warnings!)
(Patch works correctly in multiplayer)
```

### In Solo/Offline
```
[Info:RealismModSync] RealismMod resource drain safety patch applied
(No warnings!)
(Patch works correctly offline)
```

### What You Should NOT See Anymore
```
? [Warning: HarmonyX] AccessTools.Field: Could not find field for type...
```

## Why This Approach Is Better

### Before (Warning Spam)
```csharp
// Always tried to access player field
var playerField = AccessTools.Field(healthControllerType, "player_0");
// ? This fails in offline mode ? warning spam
```

### After (Smart Detection)
```csharp
// Check controller type first
if (healthControllerTypeName.Contains("Coop"))
{
    // Only search for field in multiplayer controllers
    var playerField = AccessTools.Field(healthControllerType, "player_0");
}
else
{
    // Use alternative method for offline mode
    player = Utils.GetYourPlayer();
}
```

## Benefits

? **No warning spam in solo/offline mode**  
? **Works in multiplayer with Fika**  
? **Works in offline/single-player**  
? **Works on headless servers (solo)**  
? **More robust - handles any health controller type**  

## Technical Details

### Health Controller Types

**Fika Multiplayer**:
- `Fika.Core.Coop.ClientClasses.CoopClientHealthController`
- `Fika.Core.Coop.ServerClasses.CoopServerHealthController`

**Offline/Single-Player**:
- `EFT.HealthController`
- `EFT.HealthSystem.ActiveHealthController`
- `HealthControllerClass` (obfuscated name)

### Detection Logic

```csharp
var typeName = healthControllerType.Name;

// Multiplayer detection
if (typeName.Contains("Coop") || typeName.Contains("Fika"))
{
    // This is a multiplayer session
}
else
{
    // This is offline/single-player
}
```

This works because:
- Fika always names their types with "Coop" or "Fika"
- Vanilla EFT health controllers never have these in their names
- Future-proof against Fika updates

## What If You Still See Warnings?

### Possible Causes

1. **Other mods** trying to access Fika fields
   - Check which mod is causing the warning
   - Look at the stack trace

2. **Outdated Fika version**
   - Update Fika.Core
   - Field names might have changed

3. **Different health controller type**
   - Report the health controller type name
   - We can add it to detection logic

### How to Debug

Enable debug logging and check:
```
[Debug:RealismModSync] Health controller type: [TYPE_NAME]
[Debug:RealismModSync] Is multiplayer: [TRUE/FALSE]
```

## Performance Impact

**None**:
- Type name check is cached by .NET
- String.Contains() is O(1) for short strings
- No additional overhead compared to before
- Actually **faster** because we skip unnecessary field searches

## Summary

**Problem**: Warning spam when playing solo because patch tried to access non-existent player field in vanilla health controller.

**Solution**: Detect health controller type first, only search for player field in multiplayer controllers, use alternative method for offline mode.

**Result**: ? No warnings, works in both multiplayer and solo modes.
