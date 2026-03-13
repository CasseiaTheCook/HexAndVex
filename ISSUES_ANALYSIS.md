# Codebase Issues Analysis - Gameplay-Breaking Bugs

## Overview
Three critical gameplay-breaking issues identified with specific file locations and code snippets.

---

## ISSUE #1: Scaffolding Breaks Level Generation 🔴 CRITICAL

**Problem:** When scaffolds collapse, they can split the map into two islands or block all paths with hazards/spikes, making levels unplayable.

**Root Cause:** Scaffolds are placed randomly throughout the entire map (including edge cells). When an edge/boundary scaffold collapses, it can disconnect the map or leave only hazard cells walkable.

### Files & Locations

#### [LevelGenerator.cs](LevelGenerator.cs) - Lines 150-192
**What's happening:**
```csharp
for (int x = -currentRadius; x <= currentRadius; x++)
{
    for (int y = -currentRadius; y <= currentRadius; y++)
    {
        if (Mathf.Abs(x + y) <= currentRadius)
        {
            Vector3Int cell = new Vector3Int(x, y, 0);

            if (Random.value > 0.15f)
            {
                float roll = Random.value;

                if (roll < scaffoldSpawnChance)  // ← PROBLEM: No edge detection!
                {
                    // SCAFFOLD OLUŞTUR
                    if (scaffoldMap != null && scaffoldTile != null)
                    {
                        scaffoldMap.SetTile(cell, scaffoldTile);
                    }

                    groundMap.SetTile(cell, null);  // ← Removes ground tile
                    scaffoldCells.Add(cell);        // ← Registers in list
                }
                else 
                {
                    // DİKENLİ VEYA DÜZ ZEMİN OLUŞTUR
                    groundMap.SetTile(cell, groundTile);
                    // ...
                }

                validCells.Add(cell);
            }
        }
    }
}
```

**The Issue:**
- ✗ No check to prevent scaffolds from being placed on **edge cells** or **outer ring cells**
- ✗ Scaffolds are placed uniformly in `validCells` which includes the entire hex ring
- ✗ Removal of scaffolds on outer edges disconnects the valid walkable area
- ✗ Edge scaffolds with adjacent hazards can trap or island the player

#### [LevelGenerator.cs](LevelGenerator.cs) - Lines 390-404 (Boss Arena)
Same issue for boss arena scaffold placement with half the spawn chance:
```csharp
float effectiveScaffoldChance = scaffoldSpawnChance * 0.5f;

if (roll < effectiveScaffoldChance && Vector3Int.zero != cell)
{
    // SCAFFOLD OLUŞTUR
    if (scaffoldMap != null && scaffoldTile != null)
    {
        scaffoldMap.SetTile(cell, scaffoldTile);  // ← No edge protection
    }
    groundMap.SetTile(cell, null);
    scaffoldCells.Add(cell);
}
```

### Solution Required
After level generation is complete (before player spawns), place scaffolds **ONLY in the middle/inner cells** of the map:
1. Calculate which cells are "edge cells" (cells on the outermost ring)
2. During generation, filter out scaffolds from being placed on edge cells
3. Or after generation, remove scaffolds from edge cells before the game starts
4. Verify path connectivity remains intact after scaffold removal

---

## ISSUE #2: Player Stuck When Clicking Scaffold Tile 🔴 CRITICAL

**Problem:** When trying to move to a scaffold tile by clicking, the player doesn't move, the game freezes, and highlights disappear.

**Root Cause:** `StartKnockbackMovement()` updates player position but doesn't call `UpdateHighlights()`. Scaffold tiles adjacent to the player's current position aren't registered in the highlights dictionary, so they can't be clicked.

### Files & Locations

#### [HexMovement.cs](HexMovement.cs) - Lines 257-270
**What's happening (THE BUG):**
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
        // ✗ MISSING: UpdateHighlights() call here!
    }
}
```

**Impact:**
- When player is placed at spawn, they're moved to `playerStartCell` via `StartKnockbackMovement()`
- `currentCellPosition` is updated, but `UpdateHighlights()` isn't called
- Adjacent scaffolds exist but aren't in the highlights dictionary
- Player can't click on them (click validation requires the cell to be in `highlights`)

#### [HexMovement.cs](HexMovement.cs) - Lines 114-160
**Click handling that requires highlights:**
```csharp
private void HandleMovementInput()
{
    if (Input.GetMouseButtonDown(0))
    {
        Vector3 worldPoint = GetMousePositionOnZPlane();
        Vector3Int clickedCell = groundMap.WorldToCell(worldPoint);

        if (highlights.ContainsKey(clickedCell) && highlights[clickedCell].targetAlpha > 0f)
        {
            // ✗ If scaffold isn't in highlights dict, this check fails!
            isKnockbackMove = false;
            // ... rest of movement code
        }
    }
}
```

#### [HexMovement.cs](HexMovement.cs) - Lines 273-340
**UpdateHighlights() correctly includes scaffolds (this part is OK):**
```csharp
public void UpdateHighlights()
{
    // ...
    foreach (var off in offsets)
    {
        Vector3Int neighbor = currentCellPosition + off;
        bool isScaffold = ScaffoldManager.instance != null && 
                          ScaffoldManager.instance.IsScaffoldCell(neighbor);
        
        if (groundMap.HasTile(neighbor) || isScaffold)  // ✅ Correctly includes scaffolds
        {
            bool isHazard = /* ... */;
            bool isCollapsing = /* ... */;
            if (!isHazard && !isCollapsing && /* ... */)
            {
                validCells.Add(neighbor);
            }
        }
    }
    // ...
}
```

### Verification Points
Scaffolds **ARE** being registered correctly in `LevelGenerator.scaffoldCells` (verified at lines 163-175)
- `ScaffoldManager.IsScaffoldCell()` correctly checks the list
- `UpdateHighlights()` correctly includes scaffolds in the OR condition
- **The problem is timing:** `UpdateHighlights()` must be called after `StartKnockbackMovement()`

### Solution Required
**Simple fix:** Add `UpdateHighlights()` call at the end of `StartKnockbackMovement()`:
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
        UpdateHighlights();  // ← ADD THIS LINE
    }
}
```

---

## ISSUE #3: Death Menu Stays Open with Main Menu 🟡 HIGH PRIORITY

**Problem:** When player dies and opens death menu, then goes to main menu, death panel stays visible alongside main menu.

**Root Cause:** When `PauseManager.LoadMainMenu()` loads the main menu scene, the death panel (`deathMenuUI`) is never explicitly deactivated. Since scenes aren't immediately disposed, both UIs persist briefly or the death panel remains in the UI hierarchy.

### Files & Locations

#### [HealthScript.cs](HealthScript.cs) - Lines 198-215
**Where death menu is opened (NOT closed):**
```csharp
private void Die()
{
    isDead = true;
    OnDeath?.Invoke();

    if (hptext != null) hptext.gameObject.SetActive(false);

    if (gameObject.CompareTag("Player"))
    {
        if (RunManager.instance != null) RunManager.instance.SaveBestRun();
        if (deathMenuUI != null)
        {
            deathMenuUI.SetActive(true);  // ← Opens death panel
            Time.timeScale = 0f;          // ← Pauses game
        }
        return;
    }
    // ...
}
```

#### [PauseScript.cs](PauseScript.cs) - Lines 75-95
**Main menu loading (doesn't deactivate death panel):**
```csharp
public void LoadMainMenu()
{
    Time.timeScale = 1f; // ← Unpauses time
    
    if (TurnManager.instance != null)
    {
        TurnManager.instance.ResetGame();
    }

    // ✗ MISSING: deathMenuUI.SetActive(false) here!

    if (ScreenFader.instance != null)
    {
        ScreenFader.instance.FadeAndLoad(() =>
        {
            SceneManager.LoadScene(1); // ← Loading main menu scene
        });
    }
    else
    {
        SceneManager.LoadScene(1);
    }
}
```

#### [PauseScript.cs](PauseScript.cs) - Lines 1-20
**References to death menu:**
```csharp
public class PauseManager : MonoBehaviour
{
    public GameObject pauseMenuUI;
    public GameObject deathMenuUI;  // ← Should be deactivated on main menu load
    // ...
}
```

#### [PauseScript.cs](PauseScript.cs) - Lines 72-74
**LoadSceneByIndex() also doesn't deactivate death panel:**
```csharp
public void LoadSceneByIndex(int sceneIndex)
{
    Time.timeScale = 1f; // ← Unpauses time
    // ✗ MISSING: deathMenuUI.SetActive(false) here too!
    SceneManager.LoadScene(sceneIndex);
}
```

### Solution Required
**Deactivate death panel before loading scenes:**

1. In `LoadMainMenu()` - Add deactivation before scene load
2. In `LoadSceneByIndex()` - Add deactivation before scene load
3. Optional: Also deactivate `pauseMenuUI` to ensure clean state

```csharp
public void LoadMainMenu()
{
    Time.timeScale = 1f;
    
    if (TurnManager.instance != null)
    {
        TurnManager.instance.ResetGame();
    }

    // Deactivate all UI panels
    if (pauseMenuUI != null) pauseMenuUI.SetActive(false);
    if (deathMenuUI != null) deathMenuUI.SetActive(false);

    if (ScreenFader.instance != null)
    {
        ScreenFader.instance.FadeAndLoad(() =>
        {
            SceneManager.LoadScene(1);
        });
    }
    else
    {
        SceneManager.LoadScene(1);
    }
}

public void LoadSceneByIndex(int sceneIndex)
{
    Time.timeScale = 1f;
    
    // Deactivate all UI panels
    if (pauseMenuUI != null) pauseMenuUI.SetActive(false);
    if (deathMenuUI != null) deathMenuUI.SetActive(false);
    
    SceneManager.LoadScene(sceneIndex);
}
```

---

## Summary Table

| Issue | File | Line(s) | Problem | Fix Location |
|-------|------|---------|---------|--------------|
| **Scaffold Edge Placement** | [LevelGenerator.cs](LevelGenerator.cs) | 150-192, 390-404 | No edge detection, scaffolds placed anywhere | Add edge cell check before placement |
| **Player Stuck on Scaffold** | [HexMovement.cs](HexMovement.cs) | 257-270 | Missing `UpdateHighlights()` call | Add call at end of `StartKnockbackMovement()` |
| **Death Menu Persist** | [PauseScript.cs](PauseScript.cs) | 75-95, ~66 | Death panel never deactivated | Add `deathMenuUI.SetActive(false)` in menu load methods |

---

## Testing Checklist After Fixes

- [ ] Place scaffold with intentional edge position and verify it doesn't appear
- [ ] Move player to scaffold tile by clicking and verify movement completes
- [ ] Die, see death menu, click main menu, verify no death panel visible in main menu
- [ ] Verify game resets properly when returning to gameplay
