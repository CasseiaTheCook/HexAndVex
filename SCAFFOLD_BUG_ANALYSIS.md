# Scaffold Movement Bug Analysis Report

## Issue Summary
**Turkish:** "Scaffold yanımızda doğarsa üstüne yürüyemiyoruz"  
**English:** Players cannot walk on scaffolds that spawn adjacent/near to their starting position.

---

## Root Cause Analysis

### Primary Issue: Missing UpdateHighlights() Call After Spawn Movement

**Location:** [HexMovement.cs](HexMovement.cs#L257-L263) - `StartKnockbackMovement()` method

**Problem:**
```csharp
public void StartKnockbackMovement(Vector3Int targetCell, bool preserveFacing = false)
{
    bool isScaffold = ScaffoldManager.instance != null && 
                      ScaffoldManager.instance.IsScaffoldCell(targetCell);
    if (groundMap.HasTile(targetCell) || isScaffold)
    {
        previousCellForScaffold = currentCellPosition;
        _preserveFacingNextMove = preserveFacing;
        isKnockbackMove = true;
        currentCellPosition = targetCell;    // Position updated here
        MoveCharacter(targetCell);           // Animation starts
        // ❌ MISSING: UpdateHighlights() is never called!
    }
}
```

When the player spawns at the level start, this method is called via:
- [LevelGenerator.cs](LevelGenerator.cs#L221): `TurnManager.instance.player.StartKnockbackMovement(playerStartCell);`

The movement DOES trigger later via `OnEntityEnter/Leave` calls, but **the highlights map is not refreshed when the player first moves to the spawn position**.

### Secondary Issue: Timing of UpdateHighlights() During Level Load

**Location:** [HexMovement.cs](HexMovement.cs#L69) - Called during `Start()`

**Timeline Issue:**
1. **Scene loads** → `HexMovement.Start()` runs → `UpdateHighlights()` called (L69)
   - At this point, `LevelGenerator` may not have generated scaffolds yet
   - `scaffoldCells` is empty or contains scaffolds from previous level
   
2. **Scaffolds generated** → Level generation populates `scaffoldCells`

3. **Player moved to spawn** → `StartKnockbackMovement()` called, but UpdateHighlights() still not called

4. **Finally:** `UpdateHighlights()` called at [LevelGenerator.cs L352](LevelGenerator.cs#L352)

While the final UpdateHighlights() call should correct this, there's a window where the highlights cache might be stale.

---

## Code Validation: UpdateHighlights() Logic

### ✅ Correct Implementation
[HexMovement.cs L273-312](HexMovement.cs#L273-312) properly handles scaffolds:

```csharp
foreach (var off in offsets)
{
    Vector3Int neighbor = currentCellPosition + off;
    bool isScaffold = ScaffoldManager.instance != null && 
                      ScaffoldManager.instance.IsScaffoldCell(neighbor);
    
    if (groundMap.HasTile(neighbor) || isScaffold)  // ✅ OR condition catches scaffolds
    {
        bool isHazard = LevelGenerator.instance != null && 
                       LevelGenerator.instance.hazardCells != null && 
                       LevelGenerator.instance.hazardCells.Contains(neighbor);
        bool isCollapsing = ScaffoldManager.instance != null && 
                           ScaffoldManager.instance.IsCollapsing(neighbor);
        
        // Only add if: has ground/scaffold, NOT hazard, NOT collapsing, NO enemy
        if (!isHazard && !isCollapsing && TurnManager.instance != null && 
            !TurnManager.instance.IsEnemyAtCell(neighbor))
        {
            validCells.Add(neighbor);
        }
    }
}
```

### ✅ Correct: ScaffoldManager.IsScaffoldCell()
[ScaffoldManager.cs L24-31](ScaffoldManager.cs#L24-L31):

```csharp
public bool IsScaffoldCell(Vector3Int cell)
{
    return LevelGenerator.instance != null
        && LevelGenerator.instance.scaffoldCells != null
        && LevelGenerator.instance.scaffoldCells.Contains(cell);
}
```

Properly null-checks before accessing `scaffoldCells` HashSet.

### ✅ Correct: Scaffold Registration During Generation
[LevelGenerator.cs L163-172](LevelGenerator.cs#L163-L172):

```csharp
if (roll < scaffoldSpawnChance)
{
    // Set visual tile on scaffold layer
    if (scaffoldMap != null && scaffoldTile != null)
        scaffoldMap.SetTile(cell, scaffoldTile);
    
    // CRITICAL: Remove ground tile (scaffolds don't walk on ground)
    groundMap.SetTile(cell, null);  
    
    // Register in the list
    scaffoldCells.Add(cell);
}
```

Explicitly clears ground tiles and properly registers scaffolds.

### ✓ Verified: Player Spawn Safety
[LevelGenerator.cs L201-217](LevelGenerator.cs#L201-L217):

```csharp
List<Vector3Int> safePlayerSpawns = validCells
    .Where(c => !hazardCells.Contains(c) && !scaffoldCells.Contains(c))
    .ToList();
```

Player spawns on safe ground, but **scaffolds CAN spawn as neighbors** (only the spawn cell itself is protected).

---

## Identified Issues

### 🔴 CRITICAL: StartKnockbackMovement() Doesn't Call UpdateHighlights()

**Files:**
- [HexMovement.cs L257-263](HexMovement.cs#L257-L263) - `StartKnockbackMovement()`

**Impact:**
- When player is placed at spawn position, highlights need refreshing but aren't
- Adjacent scaffolds won't show as highlighted/walkable
- Player cannot click to move onto them (because they're not in highlights dict)

**Fix Required:**
Add `UpdateHighlights()` call after movement begins OR ensure it's called immediately after spawn setup.

---

### 🔴 SECONDARY: Highlights Dictionary Cache Management

**Files:**
- [HexMovement.cs L107-127](HexMovement.cs#L107-L127) - ProcessHighlights()
- [HexMovement.cs L310-316](HexMovement.cs#L310-L316) - UpdateHighlights() fade-out

**Potential Issue:**
```csharp
foreach (var cell in highlights.Keys.ToList())
{
    if (!validCells.Contains(cell)) 
    {
        highlights[cell].targetAlpha = 0f;  // Marked for fade-out
        highlights[cell].fadeSpeed = 4f;
    }
}
```

If UpdateHighlights() is called inconsistently or scaffolds are added/removed between calls, cells might be marked for fade-out prematurely.

---

## Verification Checklist

### Need to Verify:
- [ ] Confirm scaffolds are actually being added to `scaffoldCells` during level generation
- [ ] Check if `UpdateHighlights()` is called after `StartKnockbackMovement()` completes or immediately after spawn
- [ ] Verify `ScaffoldManager.instance` exists when `UpdateHighlights()` is called
- [ ] Confirm `ProcessHighlights()` isn't prematurely fading out adjacent scaffolds
- [ ] Check if there are multiple calls to `StartKnockbackMovement()` that might not have UpdateHighlights()

---

## Code Sections Requiring Investigation

### 1. StartKnockbackMovement() Aftermath
**File:** [HexMovement.cs L257-263](HexMovement.cs#L257-L263)  
**Issue:** No UpdateHighlights() call after movement  
**Current Code:**
```csharp
currentCellPosition = targetCell;
MoveCharacter(targetCell);
// Missing UpdateHighlights() here!
```

### 2. Player Spawn Sequence  
**File:** [LevelGenerator.cs L217-222](LevelGenerator.cs#L217-L222)  
**Context:**
```csharp
TurnManager.instance.player.transform.position = groundMap.GetCellCenterWorld(playerStartCell);
TurnManager.instance.player.StartKnockbackMovement(playerStartCell);
validCells.Remove(playerStartCell);
// ... enemies spawn (lines 228-320) ...
TurnManager.instance.player.UpdateHighlights();  // Finally called at line 352
```

### 3. Similar Issues in Other Movement Methods
**File:** [HexMovement.cs](HexMovement.cs)  
**Check:** Other methods that call `MoveCharacter()` - do they also need UpdateHighlights()?
- `MoveCharacter()` itself (L236-243)
- Any knockback from enemies

---

## Recommendations for Fix

### Fix #1: Add UpdateHighlights() After Spawn (CRITICAL)
```csharp
public void StartKnockbackMovement(Vector3Int targetCell, bool preserveFacing = false)
{
    bool isScaffold = ScaffoldManager.instance != null && 
                      ScaffoldManager.instance.IsScaffoldCell(targetCell);
    if (groundMap.HasTile(targetCell) || isScaffold)
    {
        previousCellForScaffold = currentCellPosition;
        _preserveFacingNextMove = preserveFacing;
        isKnockbackMove = true;
        currentCellPosition = targetCell;
        MoveCharacter(targetCell);
        UpdateHighlights();  // ADD THIS LINE - immediate refresh for spawn
    }
}
```

### Fix #2: Ensure Level Generation Always Refreshes Highlights
Verify that after `StartKnockbackMovement()` is called in level generation, UpdateHighlights() is definitely called before giving control to the player.

### Fix #3: Add Null Checks & Debugging
Add debug logging to verify:
- `scaffoldCells` is being populated
- `UpdateHighlights()` is called when expected
- Scaffolds are correctly identified via `IsScaffoldCell()`

---

## Related Code Flow

### Player Spawn Flow
1. Scene loads → [HexMovement.cs Start()](HexMovement.cs#L60-L69)
2. Level generates → [LevelGenerator.GenerateNextLevel()](LevelGenerator.cs#L117-L130)
3. Player positioned → [LevelGenerator.cs L221-222](LevelGenerator.cs#L217-L222)
4. Enemies spawn → [LevelGenerator.cs L228-320](LevelGenerator.cs#L228-L320)
5. Highlights refreshed → [LevelGenerator.cs L352](LevelGenerator.cs#L352)

### Scaffold Walking Flow
1. UpdateHighlights() identifies valid cells
2. Player clicks on highlighted scaffold
3. MoveCharacter(scaffoldCell) starts movement
4. On arrive: ScaffoldManager.OnEntityEnter() called
5. Scaffold shakes
6. On leave: ScaffoldManager.OnEntityLeave() called
7. Scaffold collapses (unless knockback)

---

## Summary

**Root Cause:** `StartKnockbackMovement()` doesn't call `UpdateHighlights()`, so when the player spawns to their starting position, adjacent scaffolds are never added to the highlights dictionary.

**Manifestation:** Players see scaffolds adjacent to their spawn visually, but they don't appear highlighted and can't click to move there.

**Solution:** Add `UpdateHighlights()` call in `StartKnockbackMovement()` after player position is updated, OR ensure UpdateHighlights() is called synchronously before the player gets their first turn.
