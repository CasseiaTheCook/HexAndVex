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
    private Tilemap bossWarningMap; 
    
    [Header("BOSS'A ÖZEL UYARI KAROSU (BUNU ATAMALISIN!)")]
    public TileBase warningTile; 
    
    private EnemyAI myEnemyAI;

    void Awake() { instance = this; }

    void Start()
    {
        myEnemyAI = GetComponent<EnemyAI>();
        groundMap = LevelGenerator.instance.groundMap;

        GameObject warnObj = GameObject.Find("BossWarningMap");
        if (warnObj != null) 
        {
            bossWarningMap = warnObj.GetComponent<Tilemap>();
        }
        else
        {
            Debug.LogError("🚨 KANKA DİKKAT: 'BossWarningMap' bulunamadı! Yedek olarak normal WarningMap kullanılıyor.");
            if (TurnManager.instance != null) bossWarningMap = TurnManager.instance.warningMap;
        }

        // ==========================================
        // DÜZELTME: BOSS KENDİ ÖZEL TILE'INI KULLANIR!
        // ZORLA BAŞKA BİR ŞEYE ÇEVİRME KODUNU SİLDİK.
        // ==========================================
        if (warningTile == null && myEnemyAI != null)
            warningTile = myEnemyAI.warningTile;
        if (warningTile == null && TurnManager.instance != null)
            warningTile = TurnManager.instance.warningTile;
        if (warningTile == null)
            Debug.LogError("🚨 KANKA DİKKAT: Boss'un 'Warning Tile' boş kalmış! Lütfen Inspector'dan boss'un özel uyarı karosunu ata.");

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

        yield return StartCoroutine(BossTeleportFade(1f, 0f, 0.25f));

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

        yield return StartCoroutine(BossTeleportFade(0f, 1f, 0.25f));

        isTransitioning = false; 
    }

    private IEnumerator BossTeleportFade(float startAlpha, float endAlpha, float duration)
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr == null) sr = GetComponentInChildren<SpriteRenderer>();
        if (sr == null) yield break;

        Color c = sr.color;
        float elapsed = 0f;

        Vector3 normalScale = Vector3.one;
        Vector3 stretchedScale = new Vector3(0.5f, 1.5f, 1f); 
        Vector3 startScale = startAlpha > endAlpha ? normalScale : stretchedScale;
        Vector3 endScale = startAlpha > endAlpha ? stretchedScale : normalScale;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            c.a = Mathf.Lerp(startAlpha, endAlpha, t);
            sr.color = c;
            
            transform.localScale = Vector3.Lerp(startScale, endScale, t);
            
            yield return null;
        }

        c.a = endAlpha;
        sr.color = c;
        transform.localScale = normalScale;
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

        if (bossWarningMap != null)
        {
            foreach (var wCell in aoeWarningCells)
            {
                StartCoroutine(SmoothBossWarningFadeIn(wCell));
            }
        }
    }

    private IEnumerator SmoothBossWarningFadeIn(Vector3Int cell)
    {
        bossWarningMap.SetTile(cell, warningTile);
        bossWarningMap.SetTileFlags(cell, TileFlags.None); 

        // ==========================================
        // DÜZELTME: Rengini bozmasın diye BEYAZ YAPTIK! (Sadece saydamlığı değişir)
        // Senin kendi uyarı karon ne renkse o renk çıkar.
        // ==========================================
        Color startColor = new Color(1f, 1f, 1f, 0f);   
        Color endColor = new Color(1f, 1f, 1f, 0.8f); // %80 Görünür   

        bossWarningMap.SetColor(cell, startColor);

        float duration = 0.3f; 
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            t = t * t * (3f - 2f * t);

            bossWarningMap.SetColor(cell, Color.Lerp(startColor, endColor, t));
            yield return null;
        }
        bossWarningMap.SetColor(cell, endColor);
    }

    private IEnumerator ExecuteCheckerboardAoE()
    {
        List<Vector3Int> cellsToExplode = new List<Vector3Int>(aoeWarningCells);
        aoeWarningCells.Clear();
        isAoEWarningActive = false;

        if (bossWarningMap != null && cellsToExplode.Count > 0)
        {
            // Patlama anında Kendi orijinal rengini %100 parlak yapar!
            Color intenseBright = new Color(1f, 1f, 1f, 1f); 
            
            foreach (var c in cellsToExplode)
            {
                if (bossWarningMap.HasTile(c)) bossWarningMap.SetColor(c, intenseBright);
            }

            yield return new WaitForSeconds(0.1f); 

            float fadeDur = 0.5f; 
            float elapsed = 0f;
            Color startFadeColor = intenseBright;
            Color endFadeColor = new Color(1f, 1f, 1f, 0f); 

            while (elapsed < fadeDur)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeDur;
                t = t * t * (3f - 2f * t); 
                
                Color current = Color.Lerp(startFadeColor, endFadeColor, t);
                
                foreach (var c in cellsToExplode)
                {
                    if (bossWarningMap.HasTile(c)) bossWarningMap.SetColor(c, current);
                }
                yield return null;
            }
        }

        Vector3Int playerCell = TurnManager.instance.player.GetCurrentCellPosition();
        if (cellsToExplode.Contains(playerCell))
        {
            TurnManager.instance.player.health.TakeDamage(2);
        }

        foreach (var c in cellsToExplode)
        {
            if (bossWarningMap != null && bossWarningMap.HasTile(c))
            {
                bossWarningMap.SetTile(c, null);
            }
        }

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
            StartCoroutine(ShatterShieldVisual());
            previousHP = myEnemyAI.health.currentHP;
            
            yield return StartCoroutine(SummonMinions(2 + (RunManager.instance.currentLevel / 4)));
        }
        else
        {
            yield return StartCoroutine(SummonMinions(2 + (RunManager.instance.currentLevel / 4)));
        }

        isTransitioning = false; 
    }

    private IEnumerator ShatterShieldVisual()
    {
        if (shieldVisual == null) yield break;

        if (TurnManager.instance != null && TurnManager.instance.explosionPrefab != null)
        {
            GameObject fx = Instantiate(TurnManager.instance.explosionPrefab, transform.position, Quaternion.identity);
            StartCoroutine(FadeAndDestroyExplosion(fx)); 
        }

        SpriteRenderer shieldSr = shieldVisual.GetComponent<SpriteRenderer>();
        Vector3 startScale = shieldVisual.transform.localScale;
        Vector3 targetScale = startScale * 2.5f; 

        float duration = 0.3f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            shieldVisual.transform.localScale = Vector3.Lerp(startScale, targetScale, t);
            
            if (shieldSr != null)
            {
                Color c = shieldSr.color;
                c.a = Mathf.Lerp(1f, 0f, t);
                shieldSr.color = c;
            }
            yield return null;
        }

        shieldVisual.SetActive(false);
        shieldVisual.transform.localScale = startScale; 
        if (shieldSr != null)
        {
            Color c = shieldSr.color;
            c.a = 1f;
            shieldSr.color = c;
        }
    }

    private IEnumerator FadeAndDestroyExplosion(GameObject fx)
    {
        SpriteRenderer[] renderers = fx.GetComponentsInChildren<SpriteRenderer>();
        Vector3 startScale = Vector3.one * 0.5f; 
        Vector3 endScale = Vector3.one * 3f;    
        
        float duration = 0.3f; 
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            fx.transform.localScale = Vector3.Lerp(startScale, endScale, t);

            foreach (var sr in renderers)
            {
                Color c = sr.color;
                c.a = Mathf.Lerp(0.8f, 0f, t); 
                sr.color = c;
            }
            yield return null;
        }
        
        Destroy(fx);
    }

    public void OnBossDied()
    {
        if (bossWarningMap != null) bossWarningMap.ClearAllTiles(); 

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

    public bool IsCellTargetedByBoss(Vector3Int cell)
    {
        return isAoEWarningActive && aoeWarningCells.Contains(cell);
    }
}