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
    public bool isAoEWarningActive = false; 
    public bool readyToExplodeThisTurn = false; // YENİ: Patlamayı bir tur bekleten kilit!
    private List<Vector3Int> aoeWarningCells = new List<Vector3Int>();

    [Header("Summon Ayarları")]
    public List<EnemyAI> summonedMinions = new List<EnemyAI>();
    
    private bool isTransitioning = false; 
    private bool isSummoning = false; 
    private int previousHP; 

    private Tilemap groundMap;
    private Tilemap bossWarningMap;
    private int arenaRadius;
    
    [Header("BOSS'A ÖZEL UYARI KAROSU (BUNU ATAMALISIN!)")]
    public TileBase warningTile; 
    
    private EnemyAI myEnemyAI;

    void Awake() { instance = this; }

    void Start()
    {
        myEnemyAI = GetComponent<EnemyAI>();
        groundMap = LevelGenerator.instance.groundMap;
        arenaRadius = LevelGenerator.instance.baseMapRadius + 1 + (RunManager.instance.currentLevel / 10);

        GameObject warnObj = GameObject.Find("BossWarningMap");
        if (warnObj != null) 
        {
            bossWarningMap = warnObj.GetComponent<Tilemap>();
        }
        else
        {
            if (TurnManager.instance != null) bossWarningMap = TurnManager.instance.warningMap;
        }

        if (warningTile == null && myEnemyAI != null)
            warningTile = myEnemyAI.warningTile;
        if (warningTile == null && TurnManager.instance != null)
            warningTile = TurnManager.instance.warningTile;

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
        // Normal bölümle aynı formul
        int desiredMinionCount = 2 + (RunManager.instance.currentLevel / 3);
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

        if (isAoEWarningActive)
        {
            isAoEWarningActive = false;
            readyToExplodeThisTurn = false; // YENİ
            aoeCycleStep = 0; 
            
            if (bossWarningMap != null)
            {
                foreach (var c in aoeWarningCells) bossWarningMap.SetTile(c, null);
            }
            aoeWarningCells.Clear();
        }

        TriggerHitSpawn();

        yield return StartCoroutine(BossTeleportFade(1f, 0f, 0.25f));

        List<Vector3Int> farCells = new List<Vector3Int>();
        Vector3Int playerCell = TurnManager.instance.player.GetCurrentCellPosition();
        
        int radius = arenaRadius;
        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                Vector3Int c = new Vector3Int(x, y, 0);
                if (groundMap.HasTile(c) && !TurnManager.instance.IsEnemyAtCell(c) && myEnemyAI.Distance(c, playerCell) >= 4f && !LevelGenerator.instance.hazardCells.Contains(c))
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
                    if (groundMap.HasTile(c) && !TurnManager.instance.IsEnemyAtCell(c) && myEnemyAI.Distance(c, playerCell) >= 3f && !LevelGenerator.instance.hazardCells.Contains(c)) {
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
        if (myEnemyAI.skipTurns > 0) 
        {
            // Oyuncu bunun yerine attack yazmışsa durumu sıfırla ki next turda hazırlanabilsin
            readyToExplodeThisTurn = false;
            yield break;
        }
        // isTransitioning sırasında bile cycle ilerlesin, sadece aksiyon yapmasın
        if (isTransitioning)
        {
            if (aoeCycleStep < 2) aoeCycleStep++;
            yield break;
        }

        if (aoeCycleStep == 0 || aoeCycleStep == 1)
        {
            aoeCycleStep++;
        }
        else if (aoeCycleStep == 2)
        {
            if (AudioManager.instance != null) AudioManager.instance.PlayCharge();
            ShowCheckerboardWarning();
            aoeCycleStep = 3; // Uyarı verildi, 3'e geçti
        }
        else if (aoeCycleStep == 3)
        {
            // YENİ: Sadece 3'üncü adımda "Patlamaya Hazırım" bayrağını çekiyoruz.
            // TurnManager bunu görüp patlatacak.
            readyToExplodeThisTurn = true; 
        }
    }

    private void TriggerHitSpawn()
    {
        if (isTransitioning) return; // Totem öldüğü sırada spawn yapma
        
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

        int radius = arenaRadius;
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

        Color startColor = new Color(0f, 0.5f, 1f, 0f);   
        Color endColor = new Color(0f, 0.5f, 1f, 0.8f); 

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

    public IEnumerator ExecuteCheckerboardAoE()
    {
        // YENİ: Patladıktan sonra her şeyi anında sıfırla ki takılıp kalmasın
        readyToExplodeThisTurn = false;
        aoeCycleStep = 0; 
        isAoEWarningActive = false;

        List<Vector3Int> cellsToExplode = new List<Vector3Int>(aoeWarningCells);
        aoeWarningCells.Clear();

        if (bossWarningMap != null && cellsToExplode.Count > 0)
        {
            if (AudioManager.instance != null) AudioManager.instance.PlayLightning();
            Color intenseBright = new Color(0.2f, 0.8f, 1f, 1f);

            foreach (var c in cellsToExplode)
            {
                if (bossWarningMap.HasTile(c)) bossWarningMap.SetColor(c, intenseBright);
            }

            yield return new WaitForSeconds(0.1f);

            // DAMAGE VERMEK: Visual başladığında HEMEN ver
            Vector3Int playerCell = TurnManager.instance.player.GetCurrentCellPosition();
            if (cellsToExplode.Contains(playerCell))
            {
                bool dodged = Random.value < RunManager.instance.dodgeChance;
                if (dodged)
                {
                    if (TurnManager.instance.dodgeEffectPrefab != null)
                        Instantiate(TurnManager.instance.dodgeEffectPrefab, TurnManager.instance.player.transform.position, Quaternion.identity);
                }
                else if (RunManager.instance.hasBioBarrier)
                {
                    foreach (var perk in RunManager.instance.activePerks) if (perk is BioBarrierPerk aegis) { aegis.BreakShield(); break; }
                    RunManager.instance.hasBioBarrier = false;
                }
                else
                {
                    TurnManager.instance.player.health.TakeDamage(2);
                }
            }

            float fadeDur = 0.5f; 
            float elapsed = 0f;
            Color startFadeColor = intenseBright;
            Color endFadeColor = new Color(0f, 0.5f, 1f, 0f); 

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

        // Tüm düşmanları ve bossun konumunu al
        Vector3Int playerCell = TurnManager.instance.player.GetCurrentCellPosition();
        Vector3Int bossCell = myEnemyAI.GetCurrentCellPosition();
        List<Vector3Int> occupiedCells = new List<Vector3Int> { playerCell, bossCell };
        foreach (var minion in summonedMinions)
        {
            if (minion != null) occupiedCells.Add(minion.GetCurrentCellPosition());
        }

        // Harita üstünde homojen totem dağılması
        List<Vector3Int> spawnedThisRound = new List<Vector3Int>();
        int radius = arenaRadius;
        int maxAttempts = Mathf.Max(50, countToSummon * 20); // Spawn başarısını artır
        int attempts = 0;

        while (spawnedThisRound.Count < countToSummon && attempts < maxAttempts)
        {
            attempts++;
            
            // Rastgele pozisyon
            Vector3Int cell = new Vector3Int(
                Random.Range(-radius, radius + 1),
                Random.Range(-radius, radius + 1),
                0
            );

            // Geçerli konum mu?
            if (!groundMap.HasTile(cell) || 
                occupiedCells.Contains(cell) || 
                myEnemyAI.Distance(cell, playerCell) < 2f ||  // 3'ten 2'ye indirdik (az daha yakın olabilir)
                LevelGenerator.instance.hazardCells.Contains(cell))
                continue;

            // Diğer bu turda spawn'lanan minions'tan min 2 mesafe?
            bool tooCloseToSpawned = false;
            foreach (var spawnedCell in spawnedThisRound)
            {
                if (myEnemyAI.Distance(cell, spawnedCell) < 2f)
                {
                    tooCloseToSpawned = true;
                    break;
                }
            }
            if (tooCloseToSpawned) continue;

            // Spawn et
            GameObject prefab = Random.value > 0.5f ? LevelGenerator.instance.meleeEnemyPrefab : LevelGenerator.instance.aoeEnemyPrefab;
            Vector3 spawnPos = groundMap.GetCellCenterWorld(cell);
            GameObject minionObj = Instantiate(prefab, spawnPos, Quaternion.identity);

            EnemyAI minionAI = minionObj.GetComponent<EnemyAI>();
            minionAI.groundMap = this.groundMap;

            // Normal bölümdeki gibi random HP hesabı
            float randomMultiplier = Random.Range(0.8f, 1.25f);
            if (Random.value < 0.10f)  // %10 ELITE chance
            {
                randomMultiplier *= 2.0f;
                minionObj.name = "ELITE " + minionObj.name;
            }

            // Boss minionları boss HP'sinin %70'i kadar güçlü olsun (daha zor olsun diye)
            int minionHP = Mathf.RoundToInt(myEnemyAI.health.maxHP * 0.7f * randomMultiplier);
            minionAI.health.maxHP = Mathf.Max(1, minionHP);
            minionAI.health.currentHP = minionAI.health.maxHP;
            minionAI.health.updateHealth();

            summonedMinions.Add(minionAI);
            spawnedThisRound.Add(cell);  // ÖNEMLİ: Loop için count'ı güncelle
            StartCoroutine(minionAI.FadeSpawnCoroutine());
        }

        yield return new WaitForSeconds(0.5f);
        isSummoning = false; 
    }

    private bool totemSequenceRunning = false;

    public void OnTotemDestroyed()
    {
        activeTotems--;
        if (!totemSequenceRunning)
            StartCoroutine(TotemDestroySequence());
    }

    private IEnumerator TotemDestroySequence()
    {
        totemSequenceRunning = true;
        isTransitioning = true;

        // Kısa süre bekle ki aynı frame'deki birden fazla totem ölümü yakalansın
        yield return new WaitForSeconds(0.1f);

        foreach (var minion in summonedMinions)
        {
            if (minion != null && minion.health.currentHP > 0)
            {
                StartCoroutine(minion.FadeDieCoroutine());
            }
        }
        summonedMinions.Clear();

        yield return new WaitForSeconds(0.45f);

        // Normal bölüm sayısı hesapla
        int desiredMinionCount = 2 + (RunManager.instance.currentLevel / 3);
        int countToSpawn = 0;

        if (activeTotems <= 0)
        {
            // Tüm totemler kırılınca: normal sayı kadar spawn et
            isShielded = false;
            StartCoroutine(ShatterShieldVisual());
            previousHP = myEnemyAI.health.currentHP;
            countToSpawn = desiredMinionCount;
        }
        else
        {
            // Hala totemler varsa: sayıyı tamamla veya sadece 1 daha spawn et
            int currentMinionCount = summonedMinions.Count(m => m != null && m.health.currentHP > 0);
            
            if (currentMinionCount < desiredMinionCount)
            {
                // Tamamla
                countToSpawn = desiredMinionCount - currentMinionCount;
            }
            else
            {
                // Sadece 1 tane daha
                countToSpawn = 1;
            }
        }

        yield return StartCoroutine(SummonMinions(countToSpawn));

        isTransitioning = false;
        totemSequenceRunning = false;
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
        isAoEWarningActive = false;
        readyToExplodeThisTurn = false;
        aoeWarningCells.Clear();

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