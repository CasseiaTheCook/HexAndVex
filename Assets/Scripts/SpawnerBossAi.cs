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
    public GameObject shieldVisual;      // Prefab veya sahne objesi — her ikisi de çalışır
    private GameObject shieldInstance;   // Runtime'da spawn edilen instance

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
        if (shieldVisual != null)
        {
            // Prefab mı sahne objesi mi anlamak için: prefab ise sahnede değildir
            if (shieldVisual.scene.name == null || shieldVisual.scene.name == "")
            {
                // Prefab — instantiate et, boss'a child yap
                shieldInstance = Instantiate(shieldVisual, transform.position, Quaternion.identity, transform);
                shieldInstance.transform.localPosition = Vector3.zero;
            }
            else
            {
                // Sahne objesi — direkt kullan
                shieldInstance = shieldVisual;
                shieldInstance.SetActive(true);
            }
            // Boss kalkanını 3x büyüt ve Y ekseninde offset
            shieldInstance.transform.localScale = Vector3.one * 3f;
            shieldInstance.transform.localPosition = new Vector3(0f, 0.1f, 0f);
            StartCoroutine(ShieldPulseLoop());
        }

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
                
                // Boss kalkanı kırıldığından sonra her hasar anında minion spawn et
                TriggerHitSpawn();
                
                StartCoroutine(HitAndTeleportSequence());
            }
        }
    }

    private IEnumerator HitAndTeleportSequence()
    {
        isTransitioning = true;
        if (AudioManager.instance != null) AudioManager.instance.PlayBossGrunt();

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

        // Boss spawn'laması teleport sonrası (kalkan açıksa)
        if (!isShielded) TriggerHitSpawn();

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
            if (myEnemyAI.animator != null) myEnemyAI.animator.SetBool("IsCharging", true);
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
        if (isShielded) return; // Kalkan açık olsa spawn yapma
        
        summonedMinions.RemoveAll(m => m == null || m.health.currentHP <= 0);
        int currentMinions = summonedMinions.Count;
        int toSpawn = 0;

        if (currentMinions < 3) 
            toSpawn = 3 - currentMinions; 
        else 
            toSpawn = 1; 
        Debug.Log($"[Boss] Hit! Current minions: {currentMinions}, Spawning: {toSpawn}");        StartCoroutine(SummonMinions(toSpawn));
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
        // Charge animasyonunu kapat
        if (myEnemyAI.animator != null) myEnemyAI.animator.SetBool("IsCharging", false);
        
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

        // Tüm düşmanları, bossun konumunu ve totemleri al
        Vector3Int playerCell = TurnManager.instance.player.GetCurrentCellPosition();
        Vector3Int bossCell = myEnemyAI.GetCurrentCellPosition();
        List<Vector3Int> occupiedCells = new List<Vector3Int> { playerCell, bossCell };
        foreach (var e in TurnManager.instance.enemies)
        {
            if (e != null && e.health.currentHP > 0) occupiedCells.Add(e.GetCurrentCellPosition());
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

        bool lastTotem = activeTotems <= 0;

        // Ses + patlama efektleri
        if (lastTotem)
        {
            if (AudioManager.instance != null) AudioManager.instance.PlayLightning();
            // Birkaç patlama boss çevresinde arka arkaya
            if (TurnManager.instance != null && TurnManager.instance.explosionPrefab != null)
            {
                for (int i = 0; i < 4; i++)
                {
                    Vector3 offset = new Vector3(Random.Range(-0.6f, 0.6f), Random.Range(-0.4f, 0.4f), 0f);
                    GameObject fx = Instantiate(TurnManager.instance.explosionPrefab, transform.position + offset, Quaternion.identity);
                    StartCoroutine(FadeAndDestroyExplosion(fx));
                    yield return new WaitForSeconds(0.12f);
                }
            }
            // Güçlü kamera sarsıntısı
            StartCoroutine(CameraShake(0.8f, 0.22f));
        }
        else
        {
            if (AudioManager.instance != null) AudioManager.instance.PlayExplosion();
            if (TurnManager.instance != null && TurnManager.instance.explosionPrefab != null)
            {
                Vector3 offset = new Vector3(Random.Range(-0.4f, 0.4f), Random.Range(-0.3f, 0.3f), 0f);
                GameObject fx = Instantiate(TurnManager.instance.explosionPrefab, transform.position + offset, Quaternion.identity);
                StartCoroutine(FadeAndDestroyExplosion(fx));
            }
            StartCoroutine(CameraShake(0.4f, 0.14f));
        }

        // Minionları öldür
        foreach (var minion in summonedMinions)
        {
            if (minion != null && minion.health.currentHP > 0)
                StartCoroutine(minion.FadeDieCoroutine());
        }
        summonedMinions.Clear();

        // Son totemse biraz daha bekle, epik his için
        yield return new WaitForSeconds(lastTotem ? 1.1f : 0.55f);

        // Normal bölüm sayısı hesapla
        int desiredMinionCount = 2 + (RunManager.instance.currentLevel / 3);
        int countToSpawn = 0;

        if (lastTotem)
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
                countToSpawn = desiredMinionCount - currentMinionCount;
            else
                countToSpawn = 1;
        }

        yield return StartCoroutine(SummonMinions(countToSpawn));

        isTransitioning = false;
        totemSequenceRunning = false;
    }

    private IEnumerator CameraShake(float duration, float magnitude)
    {
        Camera cam = Camera.main;
        if (cam == null) yield break;

        Vector3 origin = cam.transform.position;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float strength = Mathf.Lerp(magnitude, 0f, elapsed / duration);
            cam.transform.position = new Vector3(
                origin.x + Random.Range(-1f, 1f) * strength,
                origin.y + Random.Range(-1f, 1f) * strength,
                origin.z);
            elapsed += Time.deltaTime;
            yield return null;
        }
        cam.transform.position = origin;
    }

    private IEnumerator ShieldPulseLoop()
    {
        if (shieldInstance == null) yield break;
        Vector3 baseScale = shieldInstance.transform.localScale;
        float t = 0f;
        while (shieldInstance != null && isShielded)
        {
            t += Time.deltaTime * 1.2f; // nefes alma hızı
            float pulse = 1f + Mathf.Sin(t * Mathf.PI * 2f) * 0.04f; // ±%4
            shieldInstance.transform.localScale = baseScale * pulse;
            yield return null;
        }
        if (shieldInstance != null)
            shieldInstance.transform.localScale = baseScale;
    }

    private IEnumerator ShatterShieldVisual()
    {
        if (shieldInstance == null) yield break;

        if (AudioManager.instance != null) AudioManager.instance.PlayShieldBreak();

        if (TurnManager.instance != null && TurnManager.instance.explosionPrefab != null)
        {
            GameObject fx = Instantiate(TurnManager.instance.explosionPrefab, transform.position, Quaternion.identity);
            StartCoroutine(FadeAndDestroyExplosion(fx));
        }

        SpriteRenderer[] shieldSrs = shieldInstance.GetComponentsInChildren<SpriteRenderer>();
        Vector3 startScale = shieldInstance.transform.localScale;
        Vector3 targetScale = startScale * 2.5f;

        float duration = 0.3f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            shieldInstance.transform.localScale = Vector3.Lerp(startScale, targetScale, t);

            foreach (var sr in shieldSrs)
            {
                Color c = sr.color;
                c.a = Mathf.Lerp(1f, 0f, t);
                sr.color = c;
            }
            yield return null;
        }

        Destroy(shieldInstance);
        shieldInstance = null;
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