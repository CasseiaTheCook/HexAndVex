using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using System.Linq;

public class LevelGenerator : MonoBehaviour
{
    public static LevelGenerator instance;

    [Header("Tilemaps")]
    public Tilemap groundMap;
    public Tilemap backgroundMap;
    // ==========================================
    // YENİ: DİKENLER İÇİN AYRI TİLEMAP
    // ==========================================
    public Tilemap hazardMap;

    [Header("Tiles (Üst Zemin)")]
    public TileBase groundTile;
    public TileBase hazardTile;

    [Header("Tiles (Arka Plan Sütun)")]
    public TileBase columnTile;

    [Header("Prefabs & Settings")]
    public GameObject playerPrefab;
    public GameObject meleeEnemyPrefab;
    public GameObject aoeEnemyPrefab;

    [Header("Boss Arenası Prefableri")]
    public GameObject bossPrefab;
    public GameObject totemPrefab;

    [Header("Warlock Düşman")]
    public GameObject warlockEnemyPrefab;
    public int warlockStartLevel = 6; // İlk bosstan sonra (level 6+)
    [Range(0f, 1f)] public float warlockSpawnChance = 0.10f;

    public float CurrentEnemyHealth
    {
        get { return 10f * Mathf.Pow(1.15f, RunManager.instance.currentLevel); }
    }

    public int baseMapRadius = 3;
    public int aoeStartLevel = 3;

    private List<Vector3Int> validCells = new List<Vector3Int>();
    public HashSet<Vector3Int> hazardCells = new HashSet<Vector3Int>();

    private static readonly Vector3Int[] oddOffsets = { new Vector3Int(+1, 0, 0), new Vector3Int(0, +1, 0), new Vector3Int(-1, +1, 0), new Vector3Int(-1, 0, 0), new Vector3Int(-1, -1, 0), new Vector3Int(0, -1, 0) };
    private static readonly Vector3Int[] evenOffsets = { new Vector3Int(+1, 0, 0), new Vector3Int(+1, +1, 0), new Vector3Int(0, +1, 0), new Vector3Int(-1, 0, 0), new Vector3Int(0, -1, 0), new Vector3Int(+1, -1, 0) };

    void Awake()
    {
        if (instance == null) instance = this;

        if (backgroundMap == null)
        {
            GameObject bgObj = GameObject.Find("BackgroundMap");
            if (bgObj != null) backgroundMap = bgObj.GetComponent<Tilemap>();
        }
        
        // ==========================================
        // YENİ: Hazard Map'i otomatik bul (Eğer Unity'den atanmamışsa)
        // ==========================================
        if (hazardMap == null)
        {
            GameObject hzObj = GameObject.Find("HazardMap");
            if (hzObj != null) hazardMap = hzObj.GetComponent<Tilemap>();
        }
    }

    void Start()
    {
        StartCoroutine(LevelBaslatmaSırası());
    }

    System.Collections.IEnumerator LevelBaslatmaSırası()
    {
        yield return null; 

        if (ScreenFader.instance != null)
        {
            Debug.Log("Fader bulundu, karartma başlıyor...");
            ScreenFader.instance.FadeAndLoad(() =>
            {
                GenerateNextLevel();
            });
        }
        else
        {
            Debug.LogWarning("Fader bulunamadı, direkt yükleniyor!");
            GenerateNextLevel();
        }
    }

    public void GenerateNextLevel()
    {
        if (TurnManager.instance != null) TurnManager.instance.isLevelClearTriggered = false;

        foreach (var perk in RunManager.instance.activePerks)
        {
            if (perk != null) perk.OnLevelStart();
        }

        if (RunManager.instance.currentLevel > 0 && RunManager.instance.currentLevel % 5 == 0)
        {
            GenerateBossArena();
            return;
        }

        groundMap.ClearAllTiles();
        if (backgroundMap != null) backgroundMap.ClearAllTiles();
        if (hazardMap != null) hazardMap.ClearAllTiles(); // YENİ: Eski dikenleri temizle

        validCells.Clear();
        hazardCells.Clear();

        foreach (var enemy in TurnManager.instance.enemies)
        {
            if (enemy != null) Destroy(enemy.gameObject);
        }
        TurnManager.instance.enemies.Clear();

        bool isPostBossLevel = RunManager.instance.currentLevel > 1 && RunManager.instance.currentLevel % 5 == 1;
        int currentRadius = baseMapRadius + (RunManager.instance.currentLevel / 6);

        for (int x = -currentRadius; x <= currentRadius; x++)
        {
            for (int y = -currentRadius; y <= currentRadius; y++)
            {
                if (Mathf.Abs(x + y) <= currentRadius)
                {
                    Vector3Int cell = new Vector3Int(x, y, 0);

                    if (Random.value > 0.15f)
                    {
                        // Her halükarda normal bir zemin döşe
                        groundMap.SetTile(cell, groundTile);
                        
                        if (Random.value < 0.10f)
                        {
                            // ==========================================
                            // YENİ: Eğer burası dikenliyse, dikeni hazardMap'e çiz
                            // ==========================================
                            if (hazardMap != null) hazardMap.SetTile(cell, hazardTile);
                            hazardCells.Add(cell);
                        }
                        
                        validCells.Add(cell);
                    }
                }
            }
        }

        CleanUpDisconnectedIslands();
        EnsureSafeConnectivity();
        GenerateColumns();

        Vector3 worldCenter = groundMap.GetCellCenterWorld(Vector3Int.zero);
        List<Vector3Int> safePlayerSpawns = validCells.Where(c => !hazardCells.Contains(c)).ToList();

        Vector3Int playerStartCell = safePlayerSpawns.OrderBy(c => Vector3.Distance(groundMap.GetCellCenterWorld(c), worldCenter)).First();

        TurnManager.instance.player.transform.position = groundMap.GetCellCenterWorld(playerStartCell);
        TurnManager.instance.player.StartKnockbackMovement(playerStartCell);
        validCells.Remove(playerStartCell);

        int enemyCountToSpawn = 2 + (RunManager.instance.currentLevel / 3);

        List<Vector3Int> spawnedEnemyCells = new List<Vector3Int>();
        int spawnedWarlockCount = 0;
        Vector3 playerWorldPos = groundMap.GetCellCenterWorld(playerStartCell);

        for (int i = 0; i < enemyCountToSpawn; i++)
        {
            if (validCells.Count == 0) break;

            List<Vector3Int> candidates = new List<Vector3Int>();
            int minHexDist = 3;

            while (candidates.Count == 0 && minHexDist >= 2)
            {
                int dist = minHexDist;
                candidates = validCells.FindAll(cell =>
                    !hazardCells.Contains(cell) &&
                    HexDistance(cell, playerStartCell) >= dist
                );

                candidates.RemoveAll(cell =>
                    spawnedEnemyCells.Any(spawned => HexDistance(cell, spawned) < 2)
                );

                if (candidates.Count == 0)
                    minHexDist--;
            }

            Vector3Int bestSpawnCell;

            if (candidates.Count == 0)
            {
                // Fallback: en az oyuncudan 2 hex uzakta güvenli hücre
                var safeCells = validCells.Where(c =>
                    !hazardCells.Contains(c) &&
                    HexDistance(c, playerStartCell) >= 2
                ).ToList();
                if (safeCells.Count == 0)
                    safeCells = validCells.Where(c => !hazardCells.Contains(c)).ToList();
                bestSpawnCell = safeCells.Count > 0 ? safeCells[Random.Range(0, safeCells.Count)] : validCells[0];
            }
            else if (spawnedEnemyCells.Count == 0)
            {
                bestSpawnCell = candidates.OrderByDescending(c => Vector3.Distance(groundMap.GetCellCenterWorld(c), playerWorldPos)).First();
            }
            else
            {
                float bestScore = -float.MaxValue;
                bestSpawnCell = candidates[0];

                foreach (var candidate in candidates)
                {
                    Vector3 candidateDir = (groundMap.GetCellCenterWorld(candidate) - playerWorldPos).normalized;
                    float maxDotProduct = -1f;

                    foreach (var spawned in spawnedEnemyCells)
                    {
                        Vector3 spawnedDir = (groundMap.GetCellCenterWorld(spawned) - playerWorldPos).normalized;
                        float dot = Vector3.Dot(candidateDir, spawnedDir);
                        if (dot > maxDotProduct) maxDotProduct = dot;
                    }

                    float score = (-maxDotProduct * 10f) + Vector3.Distance(groundMap.GetCellCenterWorld(candidate), playerWorldPos);

                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestSpawnCell = candidate;
                    }
                }
            }

            validCells.Remove(bestSpawnCell);
            spawnedEnemyCells.Add(bestSpawnCell);

            Vector3 spawnPos = groundMap.GetCellCenterWorld(bestSpawnCell);

            GameObject prefabToSpawn = meleeEnemyPrefab;

            // Warlock: max 3, ilk bosstan sonra (level >= warlockStartLevel) veya test için level 0'da da çıksın
            if (warlockEnemyPrefab != null && spawnedWarlockCount < 3 &&
                (RunManager.instance.currentLevel >= warlockStartLevel || RunManager.instance.currentLevel == 0))
            {
                float effectiveChance = (RunManager.instance.currentLevel == 0) ? 0.50f : warlockSpawnChance;
                if (Random.value < effectiveChance)
                {
                    prefabToSpawn = warlockEnemyPrefab;
                }
            }

            // Warlock seçilmediyse AoE şansını dene
            if (prefabToSpawn == meleeEnemyPrefab && RunManager.instance.currentLevel >= aoeStartLevel)
            {
                if (Random.value < 0.30f && aoeEnemyPrefab != null)
                {
                    prefabToSpawn = aoeEnemyPrefab;
                }
            }

            GameObject newEnemyObj = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);
            if (prefabToSpawn == warlockEnemyPrefab) spawnedWarlockCount++;
            EnemyAI enemyAI = newEnemyObj.GetComponent<EnemyAI>();
            enemyAI.groundMap = this.groundMap;

            float randomMultiplier = Random.Range(0.8f, 1.25f);
            
            if (Random.value < 0.10f)
            {
                randomMultiplier *= 2.0f; 
                newEnemyObj.name = "ELITE " + newEnemyObj.name;
            }

            float postBossMultiplier = isPostBossLevel ? 2f : 1f;
            int finalHP = Mathf.RoundToInt(CurrentEnemyHealth * randomMultiplier * postBossMultiplier);
            enemyAI.health.maxHP = Mathf.Max(1, finalHP);
            enemyAI.health.currentHP = enemyAI.health.maxHP;

            enemyAI.health.updateHealth();
            TurnManager.instance.RegisterEnemy(enemyAI);
            StartCoroutine(enemyAI.FadeSpawnCoroutine());
        }

        TurnManager.instance.isPlayerTurn = true;
        TurnManager.instance.player.UpdateHighlights();

        TurnManager.instance.Invoke("LockAllEnemyIntents", 0.1f);

        Debug.Log($"🗺️ Level {RunManager.instance.currentLevel} oluşturuldu!");
    }

    public void GenerateBossArena()
    {
        Debug.Log("🔥 BOSS BÖLÜMÜ YÜKLENİYOR! 🔥");
        groundMap.ClearAllTiles();
        if (backgroundMap != null) backgroundMap.ClearAllTiles();
        if (hazardMap != null) hazardMap.ClearAllTiles(); // YENİ
        validCells.Clear();
        hazardCells.Clear();

        foreach (var enemy in TurnManager.instance.enemies)
        {
            if (enemy != null) Destroy(enemy.gameObject);
        }
        TurnManager.instance.enemies.Clear();

        int arenaRadius = baseMapRadius + 1 + (RunManager.instance.currentLevel / 10);

        for (int x = -arenaRadius; x <= arenaRadius; x++)
        {
            for (int y = -arenaRadius; y <= arenaRadius; y++)
            {
                if (Mathf.Abs(x + y) <= arenaRadius)
                {
                    Vector3Int cell = new Vector3Int(x, y, 0);

                    if (Random.value > 0.05f)
                    {
                        groundMap.SetTile(cell, groundTile);
                        
                        if (Random.value < 0.05f && Vector3Int.zero != cell)
                        {
                            if (hazardMap != null) hazardMap.SetTile(cell, hazardTile); // YENİ
                            hazardCells.Add(cell);
                        }
                        
                        validCells.Add(cell);
                    }
                }
            }
        }

        CleanUpDisconnectedIslands();
        EnsureSafeConnectivity();
        GenerateColumns();

        Vector3 worldCenter = groundMap.GetCellCenterWorld(Vector3Int.zero);
        List<Vector3Int> safePlayerSpawns = validCells.Where(c => !hazardCells.Contains(c)).ToList();
        Vector3Int playerStartCell = safePlayerSpawns.OrderBy(c => Vector3.Distance(groundMap.GetCellCenterWorld(c), worldCenter)).First();

        TurnManager.instance.player.transform.position = groundMap.GetCellCenterWorld(playerStartCell);
        TurnManager.instance.player.StartKnockbackMovement(playerStartCell);
        validCells.Remove(playerStartCell);

        List<Vector3Int> availableSpawnCells = validCells.Where(c => !hazardCells.Contains(c)).ToList();

        for (int i = 0; i < availableSpawnCells.Count; i++)
        {
            Vector3Int temp = availableSpawnCells[i];
            int r = Random.Range(i, availableSpawnCells.Count);
            availableSpawnCells[i] = availableSpawnCells[r];
            availableSpawnCells[r] = temp;
        }

        availableSpawnCells = availableSpawnCells.OrderByDescending(c => Vector3.Distance(groundMap.GetCellCenterWorld(c), worldCenter)).ToList();

        if (bossPrefab != null && availableSpawnCells.Count > 0)
        {
            Vector3Int bossCell = availableSpawnCells[0]; 
            Vector3 bossPos = groundMap.GetCellCenterWorld(bossCell);

            GameObject bossObj = Instantiate(bossPrefab, bossPos, Quaternion.identity);
            EnemyAI bossAI = bossObj.GetComponent<EnemyAI>();

            bossAI.health.maxHP = Mathf.RoundToInt(CurrentEnemyHealth * 3f);
            bossAI.health.currentHP = bossAI.health.maxHP;
            bossAI.health.updateHealth();

            StartCoroutine(bossAI.FadeSpawnCoroutine());

            availableSpawnCells.RemoveAt(0);
        }

        if (totemPrefab != null)
        {
            for (int i = 0; i < 4; i++)
            {
                if (availableSpawnCells.Count == 0) break; 

                int index = (i * (availableSpawnCells.Count / 4));
                Vector3Int totemCell = availableSpawnCells[index];
                Vector3 totemPos = groundMap.GetCellCenterWorld(totemCell);

                GameObject totemObj = Instantiate(totemPrefab, totemPos, Quaternion.identity);
                EnemyAI totemAI = totemObj.GetComponent<EnemyAI>();

                totemAI.health.maxHP = 1;
                totemAI.health.currentHP = 1;
                totemAI.health.updateHealth();

                StartCoroutine(totemAI.FadeSpawnCoroutine());

                availableSpawnCells.RemoveAt(index);
            }
        }

        TurnManager.instance.isPlayerTurn = true;
        TurnManager.instance.player.UpdateHighlights();
    }

    private void GenerateColumns()
    {
        if (backgroundMap == null || columnTile == null) return;
        backgroundMap.ClearAllTiles();

        foreach (var cell in validCells)
        {
            backgroundMap.SetTile(cell, columnTile);
        }
    }

    private void EnsureSafeConnectivity()
    {
        List<Vector3Int> safeCells = validCells.Where(c => !hazardCells.Contains(c)).ToList();
        if (safeCells.Count == 0) return;

        List<List<Vector3Int>> safeIslands = new List<List<Vector3Int>>();
        HashSet<Vector3Int> unvisitedSafe = new HashSet<Vector3Int>(safeCells);

        while (unvisitedSafe.Count > 0)
        {
            Vector3Int startCell = unvisitedSafe.First();
            List<Vector3Int> currentIsland = new List<Vector3Int>();
            Queue<Vector3Int> queue = new Queue<Vector3Int>();

            queue.Enqueue(startCell);
            unvisitedSafe.Remove(startCell);
            currentIsland.Add(startCell);

            while (queue.Count > 0)
            {
                Vector3Int curr = queue.Dequeue();
                Vector3Int[] offsets = (curr.y % 2 != 0) ? evenOffsets : oddOffsets;

                foreach (var off in offsets)
                {
                    Vector3Int neighbor = curr + off;
                    if (unvisitedSafe.Contains(neighbor))
                    {
                        unvisitedSafe.Remove(neighbor);
                        queue.Enqueue(neighbor);
                        currentIsland.Add(neighbor);
                    }
                }
            }
            safeIslands.Add(currentIsland);
        }

        List<Vector3Int> largestSafeIsland = safeIslands[0];
        foreach (var island in safeIslands)
        {
            if (island.Count > largestSafeIsland.Count) largestSafeIsland = island;
        }

        HashSet<Vector3Int> mainSafeSet = new HashSet<Vector3Int>(largestSafeIsland);
        List<Vector3Int> cellsToRemove = new List<Vector3Int>();

        foreach (var cell in validCells)
        {
            if (hazardCells.Contains(cell))
            {
                bool touchesMain = false;
                Vector3Int[] offsets = (cell.y % 2 != 0) ? evenOffsets : oddOffsets;
                foreach (var off in offsets)
                {
                    if (mainSafeSet.Contains(cell + off)) { touchesMain = true; break; }
                }
                if (!touchesMain) cellsToRemove.Add(cell);
            }
            else
            {
                if (!mainSafeSet.Contains(cell)) cellsToRemove.Add(cell);
            }
        }

        foreach (var cell in cellsToRemove)
        {
            groundMap.SetTile(cell, null);
            if (hazardMap != null) hazardMap.SetTile(cell, null); // YENİ
            validCells.Remove(cell);
            hazardCells.Remove(cell);
        }
    }

    private void CleanUpDisconnectedIslands()
    {
        if (validCells.Count == 0) return;

        List<List<Vector3Int>> allIslands = new List<List<Vector3Int>>();
        HashSet<Vector3Int> unvisited = new HashSet<Vector3Int>(validCells);

        while (unvisited.Count > 0)
        {
            Vector3Int startCell = unvisited.First();
            List<Vector3Int> currentIsland = new List<Vector3Int>();
            Queue<Vector3Int> queue = new Queue<Vector3Int>();

            queue.Enqueue(startCell);
            unvisited.Remove(startCell);
            currentIsland.Add(startCell);

            while (queue.Count > 0)
            {
                Vector3Int curr = queue.Dequeue();
                Vector3Int[] offsets = (curr.y % 2 != 0) ? evenOffsets : oddOffsets;

                foreach (var off in offsets)
                {
                    Vector3Int neighbor = curr + off;
                    if (unvisited.Contains(neighbor))
                    {
                        unvisited.Remove(neighbor);
                        queue.Enqueue(neighbor);
                        currentIsland.Add(neighbor);
                    }
                }
            }
            allIslands.Add(currentIsland);
        }

        List<Vector3Int> largestIsland = allIslands[0];
        foreach (var island in allIslands)
        {
            if (island.Count > largestIsland.Count)
            {
                largestIsland = island;
            }
        }

        List<Vector3Int> toRemove = new List<Vector3Int>();
        foreach (var cell in validCells)
        {
            if (!largestIsland.Contains(cell))
            {
                groundMap.SetTile(cell, null);
                if (hazardMap != null) hazardMap.SetTile(cell, null); // YENİ
                toRemove.Add(cell);
            }
        }

        foreach (var c in toRemove)
        {
            validCells.Remove(c);
            hazardCells.Remove(c);
        }
    }

    private int HexDistance(Vector3Int a, Vector3Int b)
    {
        // Offset koordinatları cube koordinata çevir (odd-row offset)
        int ax = a.x - (a.y - (a.y & 1)) / 2;
        int az = a.y;
        int ay = -ax - az;
        int bx = b.x - (b.y - (b.y & 1)) / 2;
        int bz = b.y;
        int by = -bx - bz;
        return Mathf.Max(Mathf.Abs(ax - bx), Mathf.Abs(ay - by), Mathf.Abs(az - bz));
    }
}