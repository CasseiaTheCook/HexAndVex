using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;
using System.Collections.Generic;

public class WarlockEnemyAI : MonoBehaviour
{
    [Header("Saldırı Döngüsü Ayarları")]
    public int idleTurns = 2;         
    public int attackDamage = 2;      

    [Header("Impact VFX")]
    public GameObject impactVFXPrefab;  // Her vuruşta spawn edilecek effect (3 sec sonra silinir)
    public float vfxYOffset = -0.6f;   // Y offset

    [Header("Uyarı Tile (Büyücüye Özel Renk)")]
    public TileBase warningTile;      

    private int currentIdleCounter = 0;
    private int cyclePhase = 0;
    
    private bool isTransitioning = false;
    private int previousHP;

    private List<Vector3Int> attack1WarningCells = new List<Vector3Int>();
    private List<Vector3Int> attack2WarningCells = new List<Vector3Int>();

    private bool readyToExplodeAttack1 = false;
    private bool readyToExplodeAttack2 = false;

    [Header("Animasyon")]
    public Animator animator;

    private EnemyAI myEnemyAI;
    private Tilemap groundMap;
    
    // ========================================================
    // %100 KİŞİSEL HARİTA 
    // ========================================================
    private Tilemap myPersonalMap;

    private static readonly Vector3Int[] oddOffsets = { new Vector3Int(+1, 0, 0), new Vector3Int(0, +1, 0), new Vector3Int(-1, +1, 0), new Vector3Int(-1, 0, 0), new Vector3Int(-1, -1, 0), new Vector3Int(0, -1, 0) };
    private static readonly Vector3Int[] evenOffsets = { new Vector3Int(+1, 0, 0), new Vector3Int(+1, +1, 0), new Vector3Int(0, +1, 0), new Vector3Int(-1, 0, 0), new Vector3Int(0, -1, 0), new Vector3Int(+1, -1, 0) };

    void Start()
    {
        myEnemyAI = GetComponent<EnemyAI>();
        groundMap = myEnemyAI.groundMap;

        if (warningTile == null && myEnemyAI != null) warningTile = myEnemyAI.warningTile;
        if (warningTile == null && TurnManager.instance != null) warningTile = TurnManager.instance.warningTile;

        previousHP = myEnemyAI.health.currentHP;

        // Warlock'ları bul ve index al
        WarlockEnemyAI[] existingWarlocks = FindObjectsByType<WarlockEnemyAI>(FindObjectsSortMode.None);
        int myIndex = System.Array.IndexOf(existingWarlocks, this);

        // ========================================================
        // KİŞİSEL TİLEMAP: warningMap'i Instantiate ile kopyala (layout/orientation birebir aynı olsun)
        // ========================================================
        if (TurnManager.instance != null && TurnManager.instance.warningMap != null)
        {
            GameObject clone = Instantiate(TurnManager.instance.warningMap.gameObject, TurnManager.instance.warningMap.transform.parent);
            clone.name = "Warlock_PersonalMap_" + gameObject.GetInstanceID();
            clone.transform.localPosition = TurnManager.instance.warningMap.transform.localPosition;
            clone.transform.localRotation = TurnManager.instance.warningMap.transform.localRotation;
            clone.transform.localScale = TurnManager.instance.warningMap.transform.localScale;

            myPersonalMap = clone.GetComponent<Tilemap>();
            myPersonalMap.ClearAllTiles();

            TilemapRenderer tr = clone.GetComponent<TilemapRenderer>();
            TilemapRenderer baseTr = TurnManager.instance.warningMap.GetComponent<TilemapRenderer>();
            tr.sortingLayerID = baseTr.sortingLayerID;
            tr.sortingOrder = baseTr.sortingOrder + myIndex + 1;
        }

        // ========================================================
        // DÖNGÜYÜ SENKRONDAN ÇIKAR
        // ========================================================
        currentIdleCounter = -(myIndex * 2); 
        cyclePhase = 0;

        // ========================================================
        // ANİMATÖRÜ İDLEDEN BAŞLAT
        // ========================================================
        if (animator != null)
        {
            animator.SetBool("IsCharging", false);
            animator.SetBool("IsAttacking", false);
        }
    }

    void Update()
    {
        if (myEnemyAI == null || myEnemyAI.health.currentHP <= 0) return;

        if (myEnemyAI.health.currentHP < previousHP && !isTransitioning)
        {
            previousHP = myEnemyAI.health.currentHP;
            StartCoroutine(HitAndTeleportSequence());
        }
    }

    void OnDestroy()
    {
        // Warlock ölünce şahsi haritası direkt çöpe gider, başkasına dokunmaz
        if (myPersonalMap != null)
        {
            Destroy(myPersonalMap.gameObject);
        }
    }

    public Vector3Int CalculateFleeMove(Vector3Int playerCell)
    {
        Vector3Int currentCell = myEnemyAI.GetCurrentCellPosition();
        Vector3Int[] offsets = (currentCell.y % 2 != 0) ? evenOffsets : oddOffsets;

        Vector3Int bestCell = currentCell;
        float bestDist = myEnemyAI.Distance(currentCell, playerCell);

        foreach (var off in offsets)
        {
            Vector3Int neighbor = currentCell + off;
            if (!groundMap.HasTile(neighbor)) continue;
            if (TurnManager.instance.IsEnemyAtCell(neighbor)) continue;
            if (TurnManager.instance.player.GetCurrentCellPosition() == neighbor) continue;
            if (LevelGenerator.instance != null && LevelGenerator.instance.hazardCells.Contains(neighbor)) continue;
            // if (LevelGenerator.instance != null && LevelGenerator.instance.scaffoldCells.Contains(neighbor)) continue;

            float dist = myEnemyAI.Distance(neighbor, playerCell);
            if (dist > bestDist)
            {
                bestDist = dist;
                bestCell = neighbor;
            }
        }

        if (bestCell == currentCell)
        {
            List<Vector3Int> safeCells = new List<Vector3Int>();
            foreach (var off in offsets)
            {
                Vector3Int neighbor = currentCell + off;
                if (groundMap.HasTile(neighbor) && !TurnManager.instance.IsEnemyAtCell(neighbor) &&
                    TurnManager.instance.player.GetCurrentCellPosition() != neighbor &&
                    (LevelGenerator.instance == null || !LevelGenerator.instance.hazardCells.Contains(neighbor)))
                {
                    safeCells.Add(neighbor);
                }
            }
            if (safeCells.Count > 0) bestCell = safeCells[Random.Range(0, safeCells.Count)];
        }
        return bestCell;
    }

    public IEnumerator ExecuteWarlockTurn()
    {
        if (myEnemyAI.skipTurns > 0) yield break;
        if (isTransitioning) yield break;

        Vector3Int playerCell = TurnManager.instance.player.GetCurrentCellPosition();

        switch (cyclePhase)
        {
            case 0: 
                // İDLE FAZI: Hazırlanma süresi
                currentIdleCounter++;
                if (animator != null)
                {
                    animator.SetBool("IsCharging", false);
                    animator.SetBool("IsAttacking", false);
                }
                if (currentIdleCounter >= idleTurns)
                {
                    cyclePhase = 1;
                }
                break;

            case 1:
                // SALDIRI 1 ŞARJı
                if (animator != null)
                {
                    animator.SetBool("IsCharging", true);
                    animator.SetBool("IsAttacking", false);
                }
                if (AudioManager.instance != null) AudioManager.instance.PlayCharge();
                
                attack1WarningCells = GetAttack1Cells(playerCell);
                ShowWarningCells(attack1WarningCells, new Color(0.7f, 0.2f, 0.9f, 0f), new Color(0.7f, 0.2f, 0.9f, 1f));
                cyclePhase = 2;
                break;

            case 2:
                // SALDIRI 1 YÜRÜTME
                if (animator != null)
                {
                    animator.SetBool("IsCharging", false);
                    animator.SetBool("IsAttacking", true);
                }
                readyToExplodeAttack1 = true;
                cyclePhase = 3;
                break;

            case 3: 
                // SALDIRI 2 ŞARJı
                if (animator != null)
                {
                    animator.SetBool("IsCharging", true);
                    animator.SetBool("IsAttacking", false);
                }
                if (AudioManager.instance != null) AudioManager.instance.PlayCharge();
                
                attack2WarningCells = GetAttack2Cells(playerCell);
                ShowWarningCells(attack2WarningCells, new Color(0.9f, 0.2f, 0.7f, 0f), new Color(0.9f, 0.2f, 0.7f, 1f));
                cyclePhase = 4;
                break;

            case 4:
                // SALDIRI 2 YÜRÜTME
                if (animator != null)
                {
                    animator.SetBool("IsCharging", false);
                    animator.SetBool("IsAttacking", true);
                }
                readyToExplodeAttack2 = true;
                cyclePhase = 0;
                currentIdleCounter = 0;
                break;
        }
    }

    private List<Vector3Int> GetAttack1Cells(Vector3Int center)
    {
        List<Vector3Int> cells = new List<Vector3Int> { center };
        Vector3Int[] offsets = (center.y % 2 != 0) ? evenOffsets : oddOffsets;
        int[] attack1Indices = { 2, 4, 0 }; 
        foreach (int i in attack1Indices)
        {
            Vector3Int neighbor = center + offsets[i];
            if (groundMap.HasTile(neighbor)) cells.Add(neighbor);
        }
        return cells;
    }

    private List<Vector3Int> GetAttack2Cells(Vector3Int center)
    {
        List<Vector3Int> cells = new List<Vector3Int> { center };
        Vector3Int[] offsets = (center.y % 2 != 0) ? evenOffsets : oddOffsets;
        int[] attack2Indices = { 1, 3, 5 }; 
        foreach (int i in attack2Indices)
        {
            Vector3Int neighbor = center + offsets[i];
            if (groundMap.HasTile(neighbor)) cells.Add(neighbor);
        }
        return cells;
    }

    private void ShowWarningCells(List<Vector3Int> cells, Color startColor, Color endColor)
    {
        if (myPersonalMap == null) return;
        foreach (var cell in cells) StartCoroutine(SmoothWarlockWarningFadeIn(cell, startColor, endColor));
    }

    private IEnumerator SmoothWarlockWarningFadeIn(Vector3Int cell, Color startColor, Color endColor)
    {
        myPersonalMap.SetTile(cell, warningTile);
        myPersonalMap.SetTileFlags(cell, TileFlags.None);
        myPersonalMap.SetColor(cell, startColor);

        float duration = 0.3f; float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime; float t = elapsed / duration; t = t * t * (3f - 2f * t);
            myPersonalMap.SetColor(cell, Color.Lerp(startColor, endColor, t));
            yield return null;
        }
        myPersonalMap.SetColor(cell, endColor);
    }

    public IEnumerator ExecuteAttack1()
    {
        readyToExplodeAttack1 = false;
        List<Vector3Int> cellsToExplode = new List<Vector3Int>(attack1WarningCells);
        attack1WarningCells.Clear();
        yield return StartCoroutine(ExplodeWarningCells(cellsToExplode, new Color(0.9f, 0.5f, 1f, 1f)));
        if (animator != null) animator.SetBool("IsAttacking", false);
    }

    public IEnumerator ExecuteAttack2()
    {
        readyToExplodeAttack2 = false;
        List<Vector3Int> cellsToExplode = new List<Vector3Int>(attack2WarningCells);
        attack2WarningCells.Clear();
        yield return StartCoroutine(ExplodeWarningCells(cellsToExplode, new Color(1f, 0.5f, 0.9f, 1f)));
        if (animator != null) animator.SetBool("IsAttacking", false);
    }

    private IEnumerator ExplodeWarningCells(List<Vector3Int> cells, Color flashColor)
    {
        if (myPersonalMap != null && cells.Count > 0)
        {
            // === WAVE 1: Merkez hücre ===
            if (AudioManager.instance != null) AudioManager.instance.PlayLightning();
            Vector3Int centerCell = cells[0];
            if (myPersonalMap.HasTile(centerCell)) myPersonalMap.SetColor(centerCell, flashColor);
            
            yield return new WaitForSeconds(0.1f);
            SpawnImpactEffects(new List<Vector3Int> { centerCell });

            // === Merkez hasarı kontrol et ===
            Vector3Int playerCell = TurnManager.instance.player.GetCurrentCellPosition();
            bool playerHit = playerCell == centerCell;
            
            if (playerHit)
            {
                bool dodged = false;
                if (RunManager.instance != null) dodged = Random.value < RunManager.instance.dodgeChance;

                if (dodged)
                {
                    if (AudioManager.instance != null) AudioManager.instance.PlayShieldBreak();
                    if (TurnManager.instance != null && TurnManager.instance.dodgeEffectPrefab != null)
                        Instantiate(TurnManager.instance.dodgeEffectPrefab, TurnManager.instance.player.transform.position, Quaternion.identity);
                }
                else if (RunManager.instance.hasBioBarrier)
                {
                    foreach (var perk in RunManager.instance.activePerks) if (perk is BioBarrierPerk aegis) { aegis.BreakShield(); break; }
                    RunManager.instance.hasBioBarrier = false;
                }
                else TurnManager.instance.player.health.TakeDamage(attackDamage);

                Vector3Int pushTarget = TurnManager.instance.GetOppositeCell(playerCell, myEnemyAI.GetCurrentCellPosition());
                TurnManager.instance.player.StartKnockbackMovement(pushTarget);
                yield return new WaitUntil(() => !TurnManager.instance.player.IsMoving());
            }

            // === Merkez fade ===
            float fadeDur = 0.3f;
            float elapsed = 0f;
            Color endFadeColor = new Color(flashColor.r, flashColor.g, flashColor.b, 0f);

            while (elapsed < fadeDur)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeDur;
                t = t * t * (3f - 2f * t);
                Color current = Color.Lerp(flashColor, endFadeColor, t);
                if (myPersonalMap.HasTile(centerCell)) myPersonalMap.SetColor(centerCell, current);
                yield return null;
            }
            if (myPersonalMap.HasTile(centerCell)) myPersonalMap.SetTile(centerCell, null);

            // === Kısa bekleme ===
            yield return new WaitForSeconds(0.05f);

            // === WAVE 2: Dış hatlar ===
            if (cells.Count > 1)
            {
                if (AudioManager.instance != null) AudioManager.instance.PlayLightning();
                List<Vector3Int> outerCells = new List<Vector3Int>();
                for (int i = 1; i < cells.Count; i++) outerCells.Add(cells[i]);

                foreach (var c in outerCells) if (myPersonalMap.HasTile(c)) myPersonalMap.SetColor(c, flashColor);
                
                yield return new WaitForSeconds(0.1f);
                SpawnImpactEffects(outerCells);

                // === Dış hatlar hasarı ===
                if (outerCells.Contains(playerCell))
                {
                    bool dodged = false;
                    if (RunManager.instance != null) dodged = Random.value < RunManager.instance.dodgeChance;

                    if (dodged)
                    {
                        if (AudioManager.instance != null) AudioManager.instance.PlayShieldBreak();
                        if (TurnManager.instance != null && TurnManager.instance.dodgeEffectPrefab != null)
                            Instantiate(TurnManager.instance.dodgeEffectPrefab, TurnManager.instance.player.transform.position, Quaternion.identity);
                    }
                    else if (RunManager.instance.hasBioBarrier)
                    {
                        foreach (var perk in RunManager.instance.activePerks) if (perk is BioBarrierPerk aegis) { aegis.BreakShield(); break; }
                        RunManager.instance.hasBioBarrier = false;
                    }
                    else TurnManager.instance.player.health.TakeDamage(attackDamage);

                    Vector3Int pushTarget = TurnManager.instance.GetOppositeCell(playerCell, myEnemyAI.GetCurrentCellPosition());
                    TurnManager.instance.player.StartKnockbackMovement(pushTarget);
                    yield return new WaitUntil(() => !TurnManager.instance.player.IsMoving());
                }

                // === Dış hatlar fade ===
                elapsed = 0f;
                while (elapsed < fadeDur)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / fadeDur;
                    t = t * t * (3f - 2f * t);
                    Color current = Color.Lerp(flashColor, endFadeColor, t);
                    foreach (var c in outerCells) if (myPersonalMap.HasTile(c)) myPersonalMap.SetColor(c, current);
                    yield return null;
                }
                foreach (var c in outerCells) if (myPersonalMap.HasTile(c)) myPersonalMap.SetTile(c, null);
            }
        }
    }

    private IEnumerator HitAndTeleportSequence()
    {
        isTransitioning = true;
        if (AudioManager.instance != null) AudioManager.instance.PlayWarlockGrunt();
        if (animator != null)
        {
            animator.SetBool("IsCharging", false);
            animator.SetBool("IsAttacking", false);
        }
        ClearAllWarnings();
        cyclePhase = 0;
        currentIdleCounter = 0;
        readyToExplodeAttack1 = false;
        readyToExplodeAttack2 = false;

        yield return StartCoroutine(WarlockTeleportFade(1f, 0f, 0.25f));

        List<Vector3Int> farCells = new List<Vector3Int>();
        Vector3Int playerCell = TurnManager.instance.player.GetCurrentCellPosition();
        int radius = LevelGenerator.instance.baseMapRadius + (RunManager.instance.currentLevel / 6) + 2;

        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                Vector3Int c = new Vector3Int(x, y, 0);
                if (groundMap.HasTile(c) && !TurnManager.instance.IsEnemyAtCell(c) &&
                    myEnemyAI.Distance(c, playerCell) >= 4f &&
                    (LevelGenerator.instance == null || !LevelGenerator.instance.hazardCells.Contains(c)))
                {
                    farCells.Add(c);
                }
            }
        }

        if (farCells.Count == 0)
        {
            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    Vector3Int c = new Vector3Int(x, y, 0);
                    if (groundMap.HasTile(c) && !TurnManager.instance.IsEnemyAtCell(c) &&
                        myEnemyAI.Distance(c, playerCell) >= 3f &&
                        (LevelGenerator.instance == null || !LevelGenerator.instance.hazardCells.Contains(c)))
                    {
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

        yield return StartCoroutine(WarlockTeleportFade(0f, 1f, 0.25f));
        isTransitioning = false;
    }

    private IEnumerator WarlockTeleportFade(float startAlpha, float endAlpha, float duration)
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr == null) sr = GetComponentInChildren<SpriteRenderer>();
        if (sr == null) yield break;

        Color c = sr.color; float elapsed = 0f;
        Vector3 normalScale = Vector3.one;
        Vector3 stretchedScale = new Vector3(0.5f, 1.5f, 1f);
        Vector3 startScale = startAlpha > endAlpha ? normalScale : stretchedScale;
        Vector3 endScale = startAlpha > endAlpha ? stretchedScale : normalScale;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime; float t = elapsed / duration;
            c.a = Mathf.Lerp(startAlpha, endAlpha, t); sr.color = c;
            transform.localScale = Vector3.Lerp(startScale, endScale, t);
            yield return null;
        }
        c.a = endAlpha; sr.color = c; transform.localScale = normalScale;
    }

    private void ClearAllWarnings()
    {
        if (myPersonalMap != null) myPersonalMap.ClearAllTiles();
        attack1WarningCells.Clear();
        attack2WarningCells.Clear();
        readyToExplodeAttack1 = false;
        readyToExplodeAttack2 = false;
    }

    public void OnWarlockDied()
    {
        ClearAllWarnings();
        cyclePhase = 0;
        currentIdleCounter = 0;
    }

    private void SpawnImpactEffects(List<Vector3Int> cells)
    {
        if (impactVFXPrefab == null) return;

        // Tüm cell'lere aynı anda effect spawn et
        foreach (var c in cells)
        {
            Vector3 worldPos = groundMap.GetCellCenterWorld(c);
            worldPos.z = 0f;
            worldPos.y += vfxYOffset;  // -0.6 offset
            GameObject vfx = Instantiate(impactVFXPrefab, worldPos, Quaternion.identity);
            Destroy(vfx, 3f);  // 3 saniye sonra sil
        }
    }

    public bool IsReadyToExplodeAttack1() => readyToExplodeAttack1;
    public bool IsReadyToExplodeAttack2() => readyToExplodeAttack2;
    public bool IsInIdlePhase() => cyclePhase == 0;
    public bool IsTransitioning() => isTransitioning;
}