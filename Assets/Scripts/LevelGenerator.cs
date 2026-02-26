using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using System.Linq; // HashSet ve List işlemleri için gerekli

public class LevelGenerator : MonoBehaviour
{
    public static LevelGenerator instance;

    public Tilemap groundMap;
    public TileBase groundTile;
    public GameObject playerPrefab;
    public GameObject enemyPrefab;

    public float enemyHealth = 10;
    public int baseMapRadius = 3;

    private List<Vector3Int> validCells = new List<Vector3Int>();

    // --- HEX OFFSETLERİ (Bağlantıları kontrol etmek için) ---
    private static readonly Vector3Int[] oddOffsets = { new Vector3Int(+1, 0, 0), new Vector3Int(0, +1, 0), new Vector3Int(-1, +1, 0), new Vector3Int(-1, 0, 0), new Vector3Int(-1, -1, 0), new Vector3Int(0, -1, 0) };
    private static readonly Vector3Int[] evenOffsets = { new Vector3Int(+1, 0, 0), new Vector3Int(+1, +1, 0), new Vector3Int(0, +1, 0), new Vector3Int(-1, 0, 0), new Vector3Int(0, -1, 0), new Vector3Int(+1, -1, 0) };

    void Awake()
    {
        if (instance == null) instance = this;
    }

    void Start()
    {
        GenerateNextLevel();
    }

    public void GenerateNextLevel()
    {
        // 1. ESKİ HARİTAYI VE DÜŞMANLARI TEMİZLE
        groundMap.ClearAllTiles();
        validCells.Clear();

        foreach (var enemy in TurnManager.instance.enemies)
        {
            if (enemy != null) Destroy(enemy.gameObject);
        }
        TurnManager.instance.enemies.Clear();

        // 2. YENİ HARİTA ÇİZ
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
                        groundMap.SetTile(cell, groundTile);
                        validCells.Add(cell);
                    }
                }
            }
        }

        // --- YENİ EKLENEN KISIM: KOPUK ADALARI TEMİZLE ---
        CleanUpDisconnectedIslands();

        // 3. OYUNCUYU YERLEŞTİR
        Vector3Int playerStartCell = validCells[Random.Range(0, validCells.Count)];
        TurnManager.instance.player.transform.position = groundMap.GetCellCenterWorld(playerStartCell);
        TurnManager.instance.player.StartKnockbackMovement(playerStartCell);
        validCells.Remove(playerStartCell);

        // 4. DÜŞMANLARI SPAWN ET VE GÜÇLENDİR (SCALING)
        int enemyCountToSpawn = 2 + (RunManager.instance.currentLevel / 3);
        enemyHealth *= 1.1f;

        // --- GÜVENLİ BÖLGE AYARI ---
        int safeDistance = 2;

        // Düşmanların birbirinin dibinde doğmaması için konumlarını tutacağımız liste
        List<Vector3Int> spawnedEnemyCells = new List<Vector3Int>();

        for (int i = 0; i < enemyCountToSpawn; i++)
        {
            if (validCells.Count == 0) break;

            // Oyuncudan uzak olan güvenli hücreleri bul
            List<Vector3Int> safeSpawnCells = validCells.FindAll(cell =>
                Vector3Int.Distance(cell, playerStartCell) > safeDistance
            );

            Vector3Int bestSpawnCell = validCells[0]; // Güvenlik amaçlı varsayılan

            if (safeSpawnCells.Count > 0)
            {
                if (spawnedEnemyCells.Count == 0)
                {
                    // İLK DÜŞMAN: Güvenli alandan tamamen rastgele bir yer seç
                    bestSpawnCell = safeSpawnCells[Random.Range(0, safeSpawnCells.Count)];
                }
                else
                {
                    // DİĞER DÜŞMANLAR: Diğer düşmanlara en uzak olan noktayı bul
                    float maxDistToAnyEnemy = -1f;

                    foreach (var candidateCell in safeSpawnCells)
                    {
                        // Bu adayın, haritadaki en yakın düşmana olan uzaklığını bul
                        float minDistToClosestEnemy = float.MaxValue;

                        foreach (var spawnedCell in spawnedEnemyCells)
                        {
                            // Dünya pozisyonu (World Position) üzerinden gerçek mesafeyi ölçüyoruz (Hex için en sağlıklısı)
                            float dist = Vector3.Distance(
                                groundMap.GetCellCenterWorld(candidateCell),
                                groundMap.GetCellCenterWorld(spawnedCell)
                            );

                            if (dist < minDistToClosestEnemy)
                            {
                                minDistToClosestEnemy = dist;
                            }
                        }

                        // Amacımız düşmanlar arası minimum mesafeyi "maksimize" etmek (En uzağa kaçmak)
                        if (minDistToClosestEnemy > maxDistToAnyEnemy)
                        {
                            maxDistToAnyEnemy = minDistToClosestEnemy;
                            bestSpawnCell = candidateCell;
                        }
                    }
                }
            }
            else
            {
                // Eğer haritada güvenli yer kalmadıysa rastgele koy
                bestSpawnCell = validCells[Random.Range(0, validCells.Count)];
                Debug.LogWarning("⚠️ Güvenli alan kalmadığı için yakın spawn yapıldı.");
            }

            // Seçilen hücreyi listelerden çıkar ve düşman listesine ekle
            validCells.Remove(bestSpawnCell);
            spawnedEnemyCells.Add(bestSpawnCell);

            // --- INSTANTIATE (DÜŞMANI OLUŞTURMA) ---
            Vector3 spawnPos = groundMap.GetCellCenterWorld(bestSpawnCell);
            GameObject newEnemyObj = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);

            EnemyAI enemyAI = newEnemyObj.GetComponent<EnemyAI>();
            enemyAI.groundMap = this.groundMap;

            // Scaling Mantığı
            float randomMultiplier = Random.Range(0.8f, 1.25f);
            if (Random.value < 0.10f)
            {
                randomMultiplier *= 2.0f;
                newEnemyObj.transform.localScale *= 1.2f;
                newEnemyObj.name = "Elite Enemy";
            }

            int finalHP = Mathf.RoundToInt(enemyHealth * randomMultiplier);
            enemyAI.health.maxHP = Mathf.Max(1, finalHP);
            enemyAI.health.currentHP = enemyAI.health.maxHP;
            enemyAI.health.updateHealth();
        }

        // 5. TURU BAŞLAT
        TurnManager.instance.isPlayerTurn = true;
        TurnManager.instance.player.UpdateHighlights();
        TurnManager.instance.LockAllEnemyIntents();

        Debug.Log($"🗺️ Level {RunManager.instance.currentLevel} oluşturuldu!");
    }

    // --- FLOOD FILL ALGORİTMASI (Bağlantısız adaları bulur ve yok eder) ---
    private void CleanUpDisconnectedIslands()
    {
        if (validCells.Count == 0) return;

        List<List<Vector3Int>> allIslands = new List<List<Vector3Int>>();
        HashSet<Vector3Int> unvisited = new HashSet<Vector3Int>(validCells);

        // Tüm hücreleri gezerek birbirine bağlı adaları grupla
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

        // En büyük adayı bul (Ana kara)
        List<Vector3Int> largestIsland = allIslands[0];
        foreach (var island in allIslands)
        {
            if (island.Count > largestIsland.Count)
            {
                largestIsland = island;
            }
        }

        // Ana karaya bağlı olmayan (kopuk) tüm karaları haritadan sil
        foreach (var cell in validCells)
        {
            if (!largestIsland.Contains(cell))
            {
                groundMap.SetTile(cell, null); // Görseli sil
            }
        }

        // Sadece ana karayı geçerli hücreler olarak kaydet
        validCells = new List<Vector3Int>(largestIsland);
    }
}