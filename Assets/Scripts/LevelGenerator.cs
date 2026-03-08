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

    // enemyHealth'i kilitli bir float yapmak yerine, get ile levela göre otomatik hesaplatan bir property yaptık!
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
    }

    void Start()
    {
        GenerateNextLevel();
    }

    public void GenerateNextLevel()
    {
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

        validCells.Clear();
        hazardCells.Clear();

        foreach (var enemy in TurnManager.instance.enemies)
        {
            if (enemy != null) Destroy(enemy.gameObject);
        }
        TurnManager.instance.enemies.Clear();

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
                        if (Random.value < 0.10f)
                        {
                            groundMap.SetTile(cell, hazardTile);
                            hazardCells.Add(cell);
                        }
                        else
                        {
                            groundMap.SetTile(cell, groundTile);
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
        Vector3 playerWorldPos = groundMap.GetCellCenterWorld(playerStartCell);

        for (int i = 0; i < enemyCountToSpawn; i++)
        {
            if (validCells.Count == 0) break;

            List<Vector3Int> candidates = new List<Vector3Int>();
            float currentSafeDist = 2.5f;
            float currentEnemyDist = 2.5f;

            while (candidates.Count == 0 && currentSafeDist >= 0f)
            {
                candidates = validCells.FindAll(cell =>
                    !hazardCells.Contains(cell) &&
                    Vector3.Distance(groundMap.GetCellCenterWorld(cell), playerWorldPos) >= currentSafeDist
                );

                candidates.RemoveAll(cell =>
                    spawnedEnemyCells.Any(spawned => Vector3.Distance(groundMap.GetCellCenterWorld(cell), groundMap.GetCellCenterWorld(spawned)) < currentEnemyDist)
                );

                if (candidates.Count == 0)
                {
                    currentSafeDist -= 0.5f;
                    currentEnemyDist -= 0.5f;
                }
            }

            Vector3Int bestSpawnCell;

            if (candidates.Count == 0)
            {
                var safeCells = validCells.Where(c => !hazardCells.Contains(c)).ToList();
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

            if (RunManager.instance.currentLevel >= aoeStartLevel)
            {
                if (Random.value < 0.30f && aoeEnemyPrefab != null)
                {
                    prefabToSpawn = aoeEnemyPrefab;
                }
            }

            GameObject newEnemyObj = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);
            EnemyAI enemyAI = newEnemyObj.GetComponent<EnemyAI>();
            enemyAI.groundMap = this.groundMap;

            float randomMultiplier = Random.Range(0.8f, 1.25f);
            if (Random.value < 0.10f)
            {
                randomMultiplier *= 2.0f;
                newEnemyObj.transform.localScale *= 1.2f;
                newEnemyObj.name = "ELITE " + newEnemyObj.name;
            }

            // DÜZELTME: Canlar artık Property'den çekiliyor, tam scale oluyor
            int finalHP = Mathf.RoundToInt(CurrentEnemyHealth * randomMultiplier);
            enemyAI.health.maxHP = Mathf.Max(1, finalHP);
            enemyAI.health.currentHP = enemyAI.health.maxHP;

            enemyAI.health.updateHealth();
            TurnManager.instance.RegisterEnemy(enemyAI);
            // Bunu RegisterEnemy(enemyAI); yazan yerin hemen altına ekle:
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
        validCells.Clear();
        hazardCells.Clear();

        foreach (var enemy in TurnManager.instance.enemies)
        {
            if (enemy != null) Destroy(enemy.gameObject);
        }
        TurnManager.instance.enemies.Clear();

        int arenaRadius = baseMapRadius + 2;
        for (int x = -arenaRadius; x <= arenaRadius; x++)
        {
            for (int y = -arenaRadius; y <= arenaRadius; y++)
            {
                if (Mathf.Abs(x + y) <= arenaRadius)
                {
                    Vector3Int cell = new Vector3Int(x, y, 0);
                    groundMap.SetTile(cell, groundTile);
                    validCells.Add(cell);
                }
            }
        }
        GenerateColumns();

        TurnManager.instance.player.transform.position = groundMap.GetCellCenterWorld(Vector3Int.zero);
        TurnManager.instance.player.StartKnockbackMovement(Vector3Int.zero);
        validCells.Remove(Vector3Int.zero);

        List<Vector3Int> edgeCells = validCells.OrderByDescending(c => Vector3.Distance(groundMap.GetCellCenterWorld(c), Vector3.zero)).ToList();

        if (bossPrefab != null)
        {
            Vector3Int bossCell = edgeCells[0];
            Vector3 bossPos = groundMap.GetCellCenterWorld(bossCell);

            GameObject bossObj = Instantiate(bossPrefab, bossPos, Quaternion.identity);
            EnemyAI bossAI = bossObj.GetComponent<EnemyAI>();

            // DÜZELTME: Boss'un canı normal düşmanın 5 katı olacak şekilde scale oluyor!
            bossAI.health.maxHP = Mathf.RoundToInt(CurrentEnemyHealth * 3f);
            bossAI.health.currentHP = bossAI.health.maxHP;
            bossAI.health.updateHealth();

            StartCoroutine(bossAI.FadeSpawnCoroutine());

            validCells.Remove(bossCell);
            edgeCells.RemoveAt(0);
        }

        if (totemPrefab != null)
        {
            for (int i = 0; i < 4; i++)
            {
                int index = i * (edgeCells.Count / 4);
                Vector3Int totemCell = edgeCells[index];
                Vector3 totemPos = groundMap.GetCellCenterWorld(totemCell);

                GameObject totemObj = Instantiate(totemPrefab, totemPos, Quaternion.identity);
                EnemyAI totemAI = totemObj.GetComponent<EnemyAI>();

                // DÜZELTME: Totemin canı level ile çok az artar
                totemAI.health.maxHP = 5 + (RunManager.instance.currentLevel / 2);
                totemAI.health.currentHP = totemAI.health.maxHP;
                totemAI.health.updateHealth();

                StartCoroutine(totemAI.FadeSpawnCoroutine());

                validCells.Remove(totemCell);
            }
        }

        TurnManager.instance.isPlayerTurn = true;
        TurnManager.instance.player.UpdateHighlights();
    }

    private void GenerateColumns()
    {
        if (backgroundMap == null || columnTile == null) return;

        foreach (var cell in validCells)
        {
            Vector3Int[] offsets = (cell.y % 2 != 0) ? evenOffsets : oddOffsets;

            int[] exposedIndices = { 0, 3, 4, 5 };
            bool isExposedEdge = false;

            foreach (int i in exposedIndices)
            {
                Vector3Int neighbor = cell + offsets[i];
                if (!validCells.Contains(neighbor))
                {
                    isExposedEdge = true;
                    break;
                }
            }

            if (isExposedEdge)
            {
                backgroundMap.SetTile(cell, columnTile);
            }
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
                toRemove.Add(cell);
            }
        }

        foreach (var c in toRemove)
        {
            validCells.Remove(c);
            hazardCells.Remove(c);
        }
    }
}