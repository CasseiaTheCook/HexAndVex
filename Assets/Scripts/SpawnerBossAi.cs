using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class SpawnerBossAI : MonoBehaviour
{
    public static SpawnerBossAI instance;

    [Header("Boss Ayarları")]
    public int activeTotems = 4;
    public bool isShielded = true;
    public GameObject shieldVisual; 

    [Header("AoE Saldırı Ayarları (4 Adımlı Döngü)")]
    public int aoeCycleStep = 0; 
    private bool isAoEWarningActive = false;
    private List<Vector3Int> aoeWarningCells = new List<Vector3Int>();

    [Header("Summon Ayarları")]
    public List<EnemyAI> summonedMinions = new List<EnemyAI>();
    
    private bool isTransitioning = false; 
    private bool isSummoning = false; 
    private int previousHP; 

    private Tilemap groundMap;
    private Tilemap warningMap;
    public TileBase warningTile; 
    
    private EnemyAI myEnemyAI;
    private Coroutine telegraphCoroutine;

    void Awake() { instance = this; }

    void Start()
    {
        myEnemyAI = GetComponent<EnemyAI>();
        groundMap = LevelGenerator.instance.groundMap;

        GameObject warnObj = GameObject.Find("WarningMap");
        if (warnObj != null) warningMap = warnObj.GetComponent<Tilemap>();

        if (warningTile == null)
        {
            warningTile = LevelGenerator.instance.groundTile; 
        }

        activeTotems = 4;
        isShielded = true;
        if (shieldVisual != null) shieldVisual.SetActive(true);

        previousHP = myEnemyAI.health.maxHP;

        StartCoroutine(InitialSpawnDelay());
    }

    private IEnumerator InitialSpawnDelay()
    {
        isTransitioning = true; 
        yield return new WaitForSeconds(0.5f); 
        int desiredMinionCount = 2 + (RunManager.instance.currentLevel / 4);
        yield return StartCoroutine(SummonMinions(desiredMinionCount));
        isTransitioning = false; 
    }

    void Update()
    {
        if (isShielded)
        {
            if (myEnemyAI.health.currentHP < myEnemyAI.health.maxHP)
            {
                myEnemyAI.health.currentHP = myEnemyAI.health.maxHP;
                myEnemyAI.health.updateHealth();
            }
        }
        else
        {
            // KALKAN İNDİKTEN SONRA VURULMA KONTROLÜ
            if (myEnemyAI.health.currentHP > 0 && myEnemyAI.health.currentHP < previousHP && !isTransitioning)
            {
                previousHP = myEnemyAI.health.currentHP;
                StartCoroutine(HitAndTeleportSequence());
            }
        }
    }

    private IEnumerator HitAndTeleportSequence()
    {
        isTransitioning = true; 

        TriggerHitSpawn();

        yield return StartCoroutine(myEnemyAI.FadeOutWithoutDestroy());

        List<Vector3Int> farCells = new List<Vector3Int>();
        Vector3Int playerCell = TurnManager.instance.player.GetCurrentCellPosition();
        
        int radius = LevelGenerator.instance.baseMapRadius + 2;
        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                Vector3Int c = new Vector3Int(x, y, 0);
                if (groundMap.HasTile(c) && !TurnManager.instance.IsEnemyAtCell(c) && myEnemyAI.Distance(c, playerCell) >= 4f)
                {
                    farCells.Add(c);
                }
            }
        }

        if (farCells.Count == 0)
        {
            for (int x = -radius; x <= radius; x++) {
                for (int y = -radius; y <= radius; y++) {
                    Vector3Int c = new Vector3Int(x, y, 0);
                    if (groundMap.HasTile(c) && !TurnManager.instance.IsEnemyAtCell(c) && myEnemyAI.Distance(c, playerCell) >= 3f) {
                        farCells.Add(c);
                    }
                }
            }
        }

        if (farCells.Count > 0)
        {
            Vector3Int randomCell = farCells[Random.Range(0, farCells.Count)];
            myEnemyAI.TeleportTo(randomCell);
        }

        yield return StartCoroutine(myEnemyAI.FadeSpawnCoroutine());

        isTransitioning = false; 
    }

    public IEnumerator ExecuteBossTurn()
    {
        if (myEnemyAI.skipTurns > 0 || isTransitioning) yield break;

        if (aoeCycleStep == 0 || aoeCycleStep == 1)
        {
            aoeCycleStep++;
        }
        else if (aoeCycleStep == 2)
        {
            ShowCheckerboardWarning();
            aoeCycleStep++;
        }
        else if (aoeCycleStep == 3)
        {
            yield return StartCoroutine(ExecuteCheckerboardAoE());
            aoeCycleStep = 0;
        }

        // DÜZELTME: "isShielded" içindeki otomatik çağırma kodunu SİLDİM! 
        // Artık kalkanlıyken asla kendi turunda düşman çağırmaz, sadece Totem kırılınca çağırır.
    }

    private void TriggerHitSpawn()
    {
        summonedMinions.RemoveAll(m => m == null || m.health.currentHP <= 0);
        int currentMinions = summonedMinions.Count;
        int toSpawn = 0;

        if (currentMinions < 3) 
            toSpawn = 3 - currentMinions; 
        else 
            toSpawn = 1; 

        StartCoroutine(SummonMinions(toSpawn));
    }

    private void ShowCheckerboardWarning()
    {
        isAoEWarningActive = true;
        aoeWarningCells.Clear();

        int radius = LevelGenerator.instance.baseMapRadius + 2;
        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                Vector3Int cell = new Vector3Int(x, y, 0);
                if (Mathf.Abs(x + y) % 2 == 0 && groundMap.HasTile(cell))
                {
                    aoeWarningCells.Add(cell);
                }
            }
        }

        if (telegraphCoroutine != null) StopCoroutine(telegraphCoroutine);
        telegraphCoroutine = StartCoroutine(AnimateTelegraphCoroutine(true, new List<Vector3Int>(aoeWarningCells)));
    }

    private IEnumerator AnimateTelegraphCoroutine(bool show, List<Vector3Int> cellsToAnimate)
    {
        float duration = show ? 0.35f : 0.2f; 
        float elapsed = 0f;

        Color invisibleColor = new Color(1f, 0.2f, 0.2f, 0f);
        Color visibleColor = new Color(1f, 0.2f, 0.2f, 0.65f); 

        Color startColor = show ? invisibleColor : visibleColor;
        Color endColor = show ? visibleColor : invisibleColor;

        if (show)
        {
            foreach (var c in cellsToAnimate)
            {
                warningMap.SetTile(c, warningTile);
                warningMap.SetTileFlags(c, TileFlags.None);
                warningMap.SetColor(c, startColor);
            }
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            Color current = Color.Lerp(startColor, endColor, elapsed / duration);
            foreach (var c in cellsToAnimate)
            {
                if (warningMap.HasTile(c)) warningMap.SetColor(c, current);
            }
            yield return null;
        }

        foreach (var c in cellsToAnimate) if (warningMap.HasTile(c)) warningMap.SetColor(c, endColor);

        if (!show)
        {
            foreach (var c in cellsToAnimate)
            {
                if (!IsCellTargetedByNormalEnemy(c)) warningMap.SetTile(c, null);
                else warningMap.SetColor(c, visibleColor);
            }
        }
    }

    private IEnumerator ExecuteCheckerboardAoE()
    {
        List<Vector3Int> cellsToExplode = new List<Vector3Int>(aoeWarningCells);
        aoeWarningCells.Clear();
        isAoEWarningActive = false;

        foreach (var c in cellsToExplode) warningMap.SetColor(c, Color.white);
        yield return new WaitForSeconds(0.1f);
        foreach (var c in cellsToExplode) warningMap.SetColor(c, new Color(1f, 0.8f, 0f, 0.9f));
        yield return new WaitForSeconds(0.2f);

        Vector3Int playerCell = TurnManager.instance.player.GetCurrentCellPosition();
        if (cellsToExplode.Contains(playerCell))
        {
            TurnManager.instance.player.health.TakeDamage(2);
        }

        if (telegraphCoroutine != null) StopCoroutine(telegraphCoroutine);
        telegraphCoroutine = StartCoroutine(AnimateTelegraphCoroutine(false, cellsToExplode));

        yield return new WaitForSeconds(0.2f);
    }

    private IEnumerator SummonMinions(int countToSummon)
    {
        if (isSummoning) yield break; 
        isSummoning = true;

        List<Vector3Int> availableCells = new List<Vector3Int>();
        Vector3Int playerCell = TurnManager.instance.player.GetCurrentCellPosition();

        int radius = LevelGenerator.instance.baseMapRadius + 2;
        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                Vector3Int cell = new Vector3Int(x, y, 0);
                if (groundMap.HasTile(cell) && !TurnManager.instance.IsEnemyAtCell(cell) && myEnemyAI.Distance(cell, playerCell) >= 2f)
                {
                    availableCells.Add(cell);
                }
            }
        }

        for (int i = 0; i < availableCells.Count; i++)
        {
            Vector3Int temp = availableCells[i];
            int randomIndex = Random.Range(i, availableCells.Count);
            availableCells[i] = availableCells[randomIndex];
            availableCells[randomIndex] = temp;
        }

        float baseHealth = 10f * Mathf.Pow(1.15f, RunManager.instance.currentLevel);

        for (int i = 0; i < countToSummon && i < availableCells.Count; i++)
        {
            Vector3Int spawnCell = availableCells[i];
            GameObject prefab = Random.value > 0.5f ? LevelGenerator.instance.meleeEnemyPrefab : LevelGenerator.instance.aoeEnemyPrefab;

            Vector3 spawnPos = groundMap.GetCellCenterWorld(spawnCell);
            GameObject minionObj = Instantiate(prefab, spawnPos, Quaternion.identity);

            EnemyAI minionAI = minionObj.GetComponent<EnemyAI>();
            minionAI.groundMap = this.groundMap;

            int minionHP = Mathf.RoundToInt(baseHealth * 0.7f); 
            minionAI.health.maxHP = Mathf.Max(1, minionHP);
            minionAI.health.currentHP = minionAI.health.maxHP;
            minionAI.health.updateHealth();

            summonedMinions.Add(minionAI);
            StartCoroutine(minionAI.FadeSpawnCoroutine());
        }

        yield return new WaitForSeconds(0.5f);
        isSummoning = false; 
    }

    public void OnTotemDestroyed()
    {
        activeTotems--;
        Debug.Log($"💥 Totem Kırıldı! Kalan Totem: {activeTotems}");
        
        StartCoroutine(TotemDestroySequence());
    }

    private IEnumerator TotemDestroySequence()
    {
        isTransitioning = true; 

        foreach (var minion in summonedMinions)
        {
            if (minion != null && minion.health.currentHP > 0)
            {
                StartCoroutine(minion.FadeDieCoroutine());
            }
        }
        summonedMinions.Clear();

        yield return new WaitForSeconds(0.45f);

        if (activeTotems <= 0)
        {
            isShielded = false;
            if (shieldVisual != null) shieldVisual.SetActive(false);
            Debug.Log("🛡️ BOSS KALKANI DÜŞTÜ! SALDIR!");
            
            previousHP = myEnemyAI.health.currentHP;
        }
        else
        {
            yield return StartCoroutine(SummonMinions(2 + (RunManager.instance.currentLevel / 4)));
        }

        isTransitioning = false; 
    }

    public void OnBossDied()
    {
        Debug.Log("💀 BOSS ÖLDÜ! ARENA TEMİZLENİYOR!");
        
        foreach (var minion in summonedMinions)
        {
            if (minion != null && minion.health.currentHP > 0)
                StartCoroutine(minion.FadeDieCoroutine());
        }
        
        foreach (var e in TurnManager.instance.enemies.ToList())
        {
            if (e != null && e.enemyBehavior == EnemyAI.EnemyBehavior.Totem && e.health.currentHP > 0)
                StartCoroutine(e.FadeDieCoroutine());
        }
    }

    private bool IsCellTargetedByNormalEnemy(Vector3Int targetCell)
    {
        if (TurnManager.instance == null) return false;
        foreach (var e in TurnManager.instance.enemies)
        {
            if (e != null && e.isChargingAttack && e.warningCells.Contains(targetCell)) return true;
        }
        return false;
    }

    public bool IsCellTargetedByBoss(Vector3Int cell)
    {
        return isAoEWarningActive && aoeWarningCells.Contains(cell);
    }
}