using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class EnemyAI : MonoBehaviour
{
    public Tilemap groundMap;
    public HealthScript health;
    public SpriteRenderer visualRenderer;

    [Header("Animasyon")]
    public Animator animator;

    [Header("VFX")]
    public GameObject hammerImpactVFXPrefab;
    public float vfxDelayBetweenCells = 0.07f;
    public float vfxYOffset = 0.2f;
    public float vfxXOffset = 0f;

    [Header("Düşman Tipi ve Saldırı Ayarları")]
    public EnemyBehavior enemyBehavior = EnemyBehavior.Melee;
    public int aoeAttackRange = 3;
    public int aoeCooldown = 2;
    private int currentCooldown = 0;
    private bool isFirstTurn = true;

    public enum EnemyBehavior { Melee, TelegraphAoE, Totem, Boss, Warlock }

    [Header("AoE Uyarı Ayarları")]
    public Tilemap warningMap;
    public TileBase warningTile;

    [Header("UI Settings")]
    public GameObject intentArrow;
    public Vector2 arrowOffset = Vector2.zero;
    
    public GameObject stunEffectPrefab; 
    private GameObject spawnedStunEffect; 
    private SpriteRenderer stunRenderer; 
    private Coroutine stunFadeCoroutine;
    private bool isStunVisualActive = false; 

    public float arrowAngleOffset = 0f;
    private SpriteRenderer arrowRenderer;
    private Coroutine arrowFadeCoroutine;

    private Vector3Int cell;
    private Vector3 targetWorldPos;
    private bool isMoving = false;
    public bool isBumping = false;
    public bool isFading = false;

    private const float ENEMY_MOVE_SPEED = 5f;

    public Vector3Int lockedTargetCell;
    public bool hasLockedTarget = false;
    public int skipTurns = 0;

    public bool isChargingAttack = false;
    public List<Vector3Int> warningCells = new List<Vector3Int>();

    private static readonly Vector3Int[] oddOffsets = { new Vector3Int(+1, 0, 0), new Vector3Int(0, +1, 0), new Vector3Int(-1, +1, 0), new Vector3Int(-1, 0, 0), new Vector3Int(-1, -1, 0), new Vector3Int(0, -1, 0) };
    private static readonly Vector3Int[] evenOffsets = { new Vector3Int(+1, 0, 0), new Vector3Int(+1, +1, 0), new Vector3Int(0, +1, 0), new Vector3Int(-1, 0, 0), new Vector3Int(0, -1, 0), new Vector3Int(+1, -1, 0) };

    void Awake()
    {
        if (groundMap == null)
        {
            GameObject mapObj = GameObject.Find("GroundMap");
            if (mapObj != null) groundMap = mapObj.GetComponent<Tilemap>();
        }

        if (warningMap == null)
        {
            GameObject warnObj = GameObject.Find("WarningMap");
            if (warnObj != null) warningMap = warnObj.GetComponent<Tilemap>();
        }
    }

    void Start()
    {
        if (visualRenderer == null)
        {
            visualRenderer = GetComponent<SpriteRenderer>();
            if (visualRenderer == null)
            {
                foreach (var sr in GetComponentsInChildren<SpriteRenderer>())
                {
                    if (intentArrow != null && sr.transform.IsChildOf(intentArrow.transform)) continue;
                    if (stunEffectPrefab != null && sr.gameObject == spawnedStunEffect) continue;
                    visualRenderer = sr;
                    break;
                }
            }
        }

        if (GetComponent<Collider2D>() == null)
        {
            var col = gameObject.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
        }

        cell = groundMap.WorldToCell(transform.position);

        if (TurnManager.instance != null) TurnManager.instance.RegisterEnemy(this);
        targetWorldPos = groundMap.GetCellCenterWorld(cell);

        if (stunEffectPrefab != null)
        {
            spawnedStunEffect = Instantiate(stunEffectPrefab, this.transform);
            spawnedStunEffect.transform.localPosition = new Vector3(0f, 0.05f, 0f); 
            stunRenderer = spawnedStunEffect.GetComponent<SpriteRenderer>();
            
            if (stunRenderer != null) 
            {
                Color c = stunRenderer.color; c.a = 0f; stunRenderer.color = c;
            }
            spawnedStunEffect.SetActive(false);
        }

        if (intentArrow != null)
        {
            arrowRenderer = intentArrow.GetComponentInChildren<SpriteRenderer>();
            if (arrowRenderer != null)
            {
                Color c = arrowRenderer.color; c.a = 0f; arrowRenderer.color = c;
            }
            intentArrow.SetActive(false);
        }

        if (health != null)
        {
            health.OnDeath += HandleDeath;
            health.OnDamaged += HandleDamaged;
        }
    }

    private void HandleDamaged(int remainingHP)
    {
        if (animator == null || enemyBehavior != EnemyBehavior.TelegraphAoE) return;
        animator.SetBool("IsCharging", false);
        animator.SetTrigger("GotHit");
    }

    private void HandleDeath()
    {
        isChargingAttack = false;
        if (warningCells.Count > 0) ForceClearWarningCells();

        if (enemyBehavior == EnemyBehavior.Totem && SpawnerBossAI.instance != null)
        {
            SpawnerBossAI.instance.OnTotemDestroyed();
        }
        else if (enemyBehavior == EnemyBehavior.Boss && SpawnerBossAI.instance != null)
        {
            SpawnerBossAI.instance.OnBossDied();
        }
        else if (enemyBehavior == EnemyBehavior.Warlock)
        {
            WarlockEnemyAI warlock = GetComponent<WarlockEnemyAI>();
            if (warlock != null) warlock.OnWarlockDied();
        }
    }

    void OnDestroy()
    {
        if (health != null)
        {
            health.OnDeath -= HandleDeath;
            health.OnDamaged -= HandleDamaged;
        }
    }

    void OnMouseDown()
    {
        if (TurnManager.instance == null) return;
        if (TurnManager.instance.isNecroShotTargeting)
            TurnManager.instance.TryNecroShotKill(this);
        else if (TurnManager.instance.isPhaseShiftTargeting)
            TurnManager.instance.TryPhaseShift(this);
    }

    void Update()
    {
        HandleMovement();

        if (health != null && health.currentHP > 0 && !isFading)
        {
            health.SetStunnedAlpha(skipTurns > 0);
        }

        UpdateSortingOrder();
    }

    private void UpdateSortingOrder()
    {
        int order = 100 + Mathf.RoundToInt(-transform.position.y * 10f);

        SpriteRenderer target = visualRenderer ?? GetComponent<SpriteRenderer>() ?? GetComponentInChildren<SpriteRenderer>();
        if (target != null) target.sortingOrder = order;

        // Ok, stun efekti ve HP text her zaman düşmanın üstünde
        if (arrowRenderer != null) arrowRenderer.sortingOrder = order + 1;
        if (stunRenderer != null) stunRenderer.sortingOrder = order + 2;

        if (health != null && health.hptext != null)
        {
            Canvas hpCanvas = health.hptext.GetComponentInParent<Canvas>();
            if (hpCanvas != null) hpCanvas.sortingOrder = order + 3;
        }

        if (health != null)
        {
            var healthBar = health.GetComponent<EnemyHealthBar>();
            if (healthBar != null) healthBar.SetSortingOrder(order + 4);
        }

        // TelegraphAoE: hemen üstündeki hücrede başka düşman varsa saydamlaş
        if (enemyBehavior == EnemyBehavior.TelegraphAoE && TurnManager.instance != null)
        {
            bool coveredByEnemy = false;

            // Hex grid'de tam üstteki tek hücre: komşular arasından world X farkı en az olan Y+1 hücre
            Vector3Int[] offsets = (cell.y % 2 != 0) ? evenOffsets : oddOffsets;
            Vector3Int directlyAbove = cell;
            float minXDiff = float.MaxValue;
            Vector3 myWorldPos = groundMap.GetCellCenterWorld(cell);
            foreach (var off in offsets)
            {
                Vector3Int neighbor = cell + off;
                if (neighbor.y <= cell.y) continue;
                float xDiff = Mathf.Abs(groundMap.GetCellCenterWorld(neighbor).x - myWorldPos.x);
                if (xDiff < minXDiff) { minXDiff = xDiff; directlyAbove = neighbor; }
            }

            foreach (var e in TurnManager.instance.enemies)
            {
                if (e == null || e == this || e.isFading) continue;
                if (e.GetCurrentCellPosition() == directlyAbove) { coveredByEnemy = true; break; }
            }

            float targetAlpha = coveredByEnemy ? 0.4f : 1f;
            if (target != null)
            {
                Color c = target.color;
                c.a = Mathf.MoveTowards(c.a, targetAlpha, 4f * Time.deltaTime);
                target.color = c;
            }
        }
    }

    private void HandleMovement()
    {
        if (!isMoving || isBumping) return;

        transform.position = Vector3.MoveTowards(transform.position, targetWorldPos, ENEMY_MOVE_SPEED * Time.deltaTime);

        if (Vector3.Distance(transform.position, targetWorldPos) < 0.001f)
        {
            transform.position = targetWorldPos;
            isMoving = false;
        }
    }

    public void TeleportTo(Vector3Int targetCell)
    {
        cell = targetCell;
        targetWorldPos = groundMap.GetCellCenterWorld(cell);
        targetWorldPos.z = 0;
        transform.position = targetWorldPos;
    }

    // ========================================================
    // YENİ: TEK NOKTADAN STUN YÖNETİMİ
    // ========================================================
    public void ApplyStun(int turns, bool showEffect)
    {
        skipTurns = Mathf.Max(skipTurns, turns);
        
        // Eğer ağır stunsak (duvara çarptıysak) efekti aç
        if (showEffect && !isStunVisualActive)
        {
            isStunVisualActive = true;
            if (spawnedStunEffect != null)
            {
                spawnedStunEffect.SetActive(true);
                if (stunFadeCoroutine != null) StopCoroutine(stunFadeCoroutine);
                stunFadeCoroutine = StartCoroutine(FadeStunEffect(1f));
            }
        }
        
        // Saldırı hazırlığındaysa iptal et
        if (isChargingAttack)
        {
            isChargingAttack = false;
            currentCooldown = 0;
            if (animator != null) { animator.SetBool("IsCharging", false); animator.SetTrigger("GotHit"); }
            ForceClearWarningCells();
        }
    }

    // TurnManager her turun sonunda bunu çağırır
    public void DecreaseStunTurn()
    {
        if (skipTurns > 0)
        {
            skipTurns--;
            
            // Eğer süre TAMAMEN SIFIRLANDIYSA efekti kapat
            if (skipTurns <= 0)
            {
                if (isStunVisualActive)
                {
                    isStunVisualActive = false;
                    if (stunFadeCoroutine != null) StopCoroutine(stunFadeCoroutine);
                    stunFadeCoroutine = StartCoroutine(FadeStunEffect(0f));
                }
            }
        }
    }

    // ========================================================
    // HealthScript'in Hata Vermemesi İçin Köprü (Sadece kapatma için kullanılır)
    // ========================================================
    public void SetStunVisual(bool state)
    {
        if (!state && isStunVisualActive)
        {
            isStunVisualActive = false;
            if (spawnedStunEffect != null) spawnedStunEffect.SetActive(false); // Anında kapat
        }
    }

    private IEnumerator FadeStunEffect(float targetAlpha)
    {
        float startAlpha = stunRenderer.color.a;
        float elapsed = 0f;
        float duration = 0.25f; 
        Color c = stunRenderer.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            c.a = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
            stunRenderer.color = c;
            yield return null;
        }

        c.a = targetAlpha;
        stunRenderer.color = c;

        if (targetAlpha <= 0.01f)
        {
            spawnedStunEffect.SetActive(false);
        }
    }

    public void StartWallBump(Vector3 direction)
    {
        if (enemyBehavior == EnemyBehavior.Totem || enemyBehavior == EnemyBehavior.Boss || enemyBehavior == EnemyBehavior.Warlock) return;
        StartCoroutine(WallBumpCoroutine(direction));
    }

    private IEnumerator WallBumpCoroutine(Vector3 direction)
    {
        if (AudioManager.instance != null) AudioManager.instance.PlayWall();
        isBumping = true; isMoving = true;
        Vector3 originalPos = groundMap.GetCellCenterWorld(cell); originalPos.z = 0;
        Vector3 bumpPos = originalPos + (direction * 0.10f);

        while (Vector3.Distance(transform.position, bumpPos) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(transform.position, bumpPos, 4f * Time.deltaTime);
            yield return null;
        }

        yield return new WaitForSeconds(0.05f);

        while (Vector3.Distance(transform.position, originalPos) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(transform.position, originalPos, 1f * Time.deltaTime);
            yield return null;
        }

        transform.position = originalPos;
        isBumping = false; isMoving = false;
    }

    public void SetArrowVisibility(bool show)
    {
        if (intentArrow == null || arrowRenderer == null) return;
        if (show && !hasLockedTarget) return;

        if (arrowFadeCoroutine != null) StopCoroutine(arrowFadeCoroutine);
        arrowFadeCoroutine = StartCoroutine(FadeArrowCoroutine(show));
    }

    private IEnumerator FadeArrowCoroutine(bool show)
    {
        if (show) intentArrow.SetActive(true);

        float targetAlpha = show ? 1f : 0f;
        float startAlpha = arrowRenderer.color.a;
        float elapsed = 0f; Color c = arrowRenderer.color;

        while (elapsed < 0.2f)
        {
            elapsed += Time.deltaTime;
            c.a = Mathf.Lerp(startAlpha, targetAlpha, elapsed / 0.2f);
            arrowRenderer.color = c;
            yield return null;
        }
        c.a = targetAlpha; arrowRenderer.color = c;
        if (!show) intentArrow.SetActive(false);
    }

    public void LockNextMove(Vector3Int playerCell, bool isStunned)
    {
        if (isChargingAttack) ForceClearWarningCells();

        if (enemyBehavior == EnemyBehavior.Totem || enemyBehavior == EnemyBehavior.Boss) return;

        if (enemyBehavior == EnemyBehavior.Warlock)
        {
            WarlockEnemyAI warlockAI = GetComponent<WarlockEnemyAI>();
            if (warlockAI != null && warlockAI.IsInIdlePhase() && !warlockAI.IsTransitioning() && !isStunned && intentArrow != null && arrowRenderer != null)
            {
                Vector3Int fleeTarget = warlockAI.CalculateFleeMove(playerCell);
                if (fleeTarget != cell)
                {
                    hasLockedTarget = true;
                    lockedTargetCell = fleeTarget;
                    Vector3 currentWorldPos = groundMap.GetCellCenterWorld(cell); currentWorldPos.z = 0;
                    Vector3 nextWorldPos = groundMap.GetCellCenterWorld(fleeTarget); nextWorldPos.z = 0;
                    intentArrow.transform.position = currentWorldPos + (Vector3)arrowOffset;
                    Vector3 dir = nextWorldPos - currentWorldPos;
                    float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                    intentArrow.transform.rotation = Quaternion.AngleAxis(angle + arrowAngleOffset, Vector3.forward);
                    SetArrowVisibility(true);
                }
                else
                {
                    hasLockedTarget = false; SetArrowVisibility(false);
                }
            }
            else
            {
                hasLockedTarget = false; SetArrowVisibility(false);
            }
            return;
        }

        if (isStunned || health.currentHP <= 0)
        {
            hasLockedTarget = false; SetArrowVisibility(false); isChargingAttack = false; return;
        }

        if (currentCooldown > 0) currentCooldown--;

        Vector3 playerPos = groundMap.GetCellCenterWorld(playerCell);
        float dxToPlayer = playerPos.x - transform.position.x;
        if (Mathf.Abs(dxToPlayer) > 0.01f && visualRenderer != null)
        {
            visualRenderer.flipX = (dxToPlayer < 0);
        }

        if (enemyBehavior == EnemyBehavior.TelegraphAoE)
        {
            if (!isFirstTurn && currentCooldown <= 0 && Distance(cell, playerCell) <= aoeAttackRange)
            {
                isChargingAttack = true;
                hasLockedTarget = false;
                SetArrowVisibility(false);
                if (AudioManager.instance != null) AudioManager.instance.PlayCharge();
                if (animator != null) animator.SetBool("IsCharging", true);

                warningCells = GetLineOfCells(cell, playerCell, aoeAttackRange);

                if (TurnManager.instance != null)
                {
                    foreach (var wCell in warningCells)
                    {
                        TurnManager.instance.DrawWarningTile(wCell);
                    }
                }

                return;
            }
            else
            {
                isChargingAttack = false;
            }
        }

        if (IsNeighbor(cell, playerCell))
        {
            hasLockedTarget = false; SetArrowVisibility(false); return;
        }

        lockedTargetCell = CalculateNextMove(playerCell);

        if (lockedTargetCell != cell)
        {
            hasLockedTarget = true;
            Vector3 currentWorldPos = groundMap.GetCellCenterWorld(cell);
            Vector3 nextWorldPos = groundMap.GetCellCenterWorld(lockedTargetCell);
            currentWorldPos.z = 0; nextWorldPos.z = 0;

            intentArrow.transform.position = currentWorldPos;
            Vector3 direction = nextWorldPos - currentWorldPos;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            intentArrow.transform.rotation = Quaternion.AngleAxis(angle + arrowAngleOffset, Vector3.forward);

            SetArrowVisibility(true);
        }
        else
        {
            hasLockedTarget = false; SetArrowVisibility(false);
        }
    }

    public void ExecuteLockedMove()
    {
        isFirstTurn = false;

        if (enemyBehavior == EnemyBehavior.Totem) return;

        if (enemyBehavior == EnemyBehavior.Boss)
        {
            SpawnerBossAI bossAI = GetComponent<SpawnerBossAI>();
            if (bossAI != null) StartCoroutine(bossAI.ExecuteBossTurn());
            return;
        }

        if (enemyBehavior == EnemyBehavior.Warlock)
        {
            WarlockEnemyAI warlockAI = GetComponent<WarlockEnemyAI>();
            if (warlockAI != null)
            {
                // Bekleme fazındaysa kaç, değilse sadece saldırı döngüsünü çalıştır
                if (warlockAI.IsInIdlePhase() && !warlockAI.IsTransitioning() && skipTurns <= 0)
                {
                    Vector3Int playerCell = TurnManager.instance.player.GetCurrentCellPosition();
                    Vector3Int fleeTarget = warlockAI.CalculateFleeMove(playerCell);
                    if (fleeTarget != cell)
                    {
                        cell = fleeTarget;
                        targetWorldPos = groundMap.GetCellCenterWorld(cell);
                        targetWorldPos.z = 0;
                        float dx = targetWorldPos.x - transform.position.x;
                        if (Mathf.Abs(dx) > 0.01f && visualRenderer != null) visualRenderer.flipX = (dx < 0);
                        isMoving = true;
                        if (AudioManager.instance != null) AudioManager.instance.PlayMove();
                    }
                }
                StartCoroutine(warlockAI.ExecuteWarlockTurn());
            }
            return;
        }

        if (isMoving || health.currentHP <= 0 || skipTurns > 0)
        {
            hasLockedTarget = false; SetArrowVisibility(false); return;
        }

        if (isChargingAttack) return;

        if (hasLockedTarget)
        {
            if (IsNeighbor(cell, lockedTargetCell) &&
                !TurnManager.instance.IsEnemyAtCell(lockedTargetCell) &&
                TurnManager.instance.player.GetCurrentCellPosition() != lockedTargetCell &&
                (LevelGenerator.instance == null || !LevelGenerator.instance.hazardCells.Contains(lockedTargetCell)))
            {
                cell = lockedTargetCell;
                targetWorldPos = groundMap.GetCellCenterWorld(cell);
                targetWorldPos.z = 0;

                float dx = targetWorldPos.x - transform.position.x;
                if (Mathf.Abs(dx) > 0.01f && visualRenderer != null)
                {
                    visualRenderer.flipX = (dx < 0);
                }

                isMoving = true;
                if (AudioManager.instance != null) AudioManager.instance.PlayMove();
            }
        }

        hasLockedTarget = false;
        SetArrowVisibility(false);
    }

    public IEnumerator FadeSpawnCoroutine()
    {
        isFading = true;
        SpriteRenderer[] allRenderers = GetComponentsInChildren<SpriteRenderer>();

        foreach (var sr in allRenderers) { Color c = sr.color; c.a = 0f; sr.color = c; }

        float duration = 0.5f; float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(0f, 1f, elapsed / duration);
            foreach (var sr in allRenderers) { Color c = sr.color; c.a = alpha; sr.color = c; }
            yield return null;
        }

        foreach (var sr in allRenderers) { Color c = sr.color; c.a = 1f; sr.color = c; }
        isFading = false;
    }

    public IEnumerator FadeOutWithoutDestroy()
    {
        isFading = true;
        SpriteRenderer[] allRenderers = GetComponentsInChildren<SpriteRenderer>();

        float duration = 0.3f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
            foreach (var sr in allRenderers) { Color c = sr.color; c.a = alpha; sr.color = c; }
            yield return null;
        }
        isFading = false;
    }

    public IEnumerator FadeDieCoroutine()
    {
        isFading = true;

        isChargingAttack = false;
        if (warningCells.Count > 0) ForceClearWarningCells();

        health.currentHP = 0;
        skipTurns = 0;
        SetArrowVisibility(false);
        SetStunVisual(false); 
        hasLockedTarget = false;

        SpriteRenderer[] allRenderers = GetComponentsInChildren<SpriteRenderer>();

        float duration = 0.4f; float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
            foreach (var sr in allRenderers) { Color c = sr.color; c.a = alpha; sr.color = c; }
            yield return null;
        }

        if (TurnManager.instance != null) TurnManager.instance.enemies.Remove(this);
        Destroy(gameObject);
    }

    private List<Vector3Int> GetLineOfCells(Vector3Int startCell, Vector3Int targetCell, int length)
    {
        List<Vector3Int> line = new List<Vector3Int>();
        int bestDirIndex = 0;
        float minDist = float.MaxValue;
        Vector3Int[] startOffsets = (startCell.y % 2 != 0) ? evenOffsets : oddOffsets;

        for (int i = 0; i < 6; i++)
        {
            float d = Distance(startCell + startOffsets[i], targetCell);
            if (d < minDist)
            {
                minDist = d;
                bestDirIndex = i;
            }
        }

        Vector3Int currentStep = startCell;
        for (int i = 0; i < length; i++)
        {
            Vector3Int[] currOffsets = (currentStep.y % 2 != 0) ? evenOffsets : oddOffsets;
            currentStep += currOffsets[bestDirIndex];

            if (groundMap.HasTile(currentStep)) line.Add(currentStep);
        }
        return line;
    }

    private bool IsCellTargetedByOtherEnemy(Vector3Int targetCell)
    {
        if (TurnManager.instance == null) return false;
        foreach (var e in TurnManager.instance.enemies)
        {
            if (e != null && e != this && e.health.currentHP > 0 && !e.isFading && e.isChargingAttack)
            {
                if (e.warningCells.Contains(targetCell)) return true;
            }
        }
        return false;
    }

    private void ForceClearWarningCells()
    {
        if (warningMap == null) { warningCells.Clear(); return; }
        StartCoroutine(FadeClearWarningCells(new List<Vector3Int>(warningCells)));
        warningCells.Clear();
    }

    private IEnumerator FadeClearWarningCells(List<Vector3Int> cells)
    {
        float duration = 0.3f;
        float elapsed = 0f;

        // Hücreleri "paylaşılan" ve "sadece bize ait" olarak ayır
        List<Vector3Int> ownCells = new List<Vector3Int>();
        List<Vector3Int> sharedCells = new List<Vector3Int>();
        foreach (var c in cells)
        {
            if (warningMap.HasTile(c))
            {
                if (IsCellTargetedByOtherEnemy(c)) sharedCells.Add(c);
                else ownCells.Add(c);
            }
        }

        // Fade out animasyonu (sadece kendi hücrelerimiz)
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float alpha = Mathf.Lerp(0.65f, 0f, t);
            Color fadeColor = new Color(1f, 1f, 1f, alpha);
            foreach (var c in ownCells)
            {
                if (warningMap.HasTile(c)) warningMap.SetColor(c, fadeColor);
            }
            yield return null;
        }

        foreach (var c in ownCells)
        {
            if (warningMap.HasTile(c)) warningMap.SetTile(c, null);
        }

        // Paylaşılan hücreleri kırmızıya çevir
        foreach (var c in sharedCells)
        {
            if (warningMap.HasTile(c)) warningMap.SetColor(c, new Color(1f, 0.2f, 0.2f, 0.65f));
        }
    }

    public IEnumerator ExecuteAoEAttackCoroutine(HexMovement player)
    {
        if (!isChargingAttack) yield break;

        if (warningMap != null && warningCells.Count > 0)
        {
            HashSet<Vector3Int> sharedCells = new HashSet<Vector3Int>();
            List<Vector3Int> ownCells = new List<Vector3Int>();
            foreach (var c in warningCells)
            {
                if (IsCellTargetedByOtherEnemy(c)) sharedCells.Add(c);
                else ownCells.Add(c);
            }

            Color attackFlashColor = new Color(1f, 0.8f, 0f, 1f);
            if (animator != null)
            {
                animator.SetBool("IsCharging", false);
                animator.SetTrigger("Attack");
            }
            foreach (var c in ownCells)
            {
                if (warningMap.HasTile(c)) warningMap.SetColor(c, attackFlashColor);
            }

            foreach (var c in sharedCells)
            {
                if (warningMap.HasTile(c)) warningMap.SetColor(c, attackFlashColor);
            }

            yield return new WaitForSeconds(0.1f);

            if (hammerImpactVFXPrefab != null)
                StartCoroutine(SpawnImpactVFXSequence(new List<Vector3Int>(warningCells)));


            float fadeDur = 0.4f;
            float elapsed = 0f;
            Color startFadeColor = attackFlashColor;
            Color endFadeColor = new Color(1f, 0.8f, 0f, 0f); 

            while (elapsed < fadeDur)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeDur;
                t = t * t * (3f - 2f * t);

                Color current = Color.Lerp(startFadeColor, endFadeColor, t);

                foreach (var c in ownCells)
                {
                    if (warningMap.HasTile(c)) warningMap.SetColor(c, current);
                }
                
                Color sharedFade = Color.Lerp(startFadeColor, new Color(1f, 0.2f, 0.2f, 0.65f), t);
                foreach (var c in sharedCells)
                {
                    if (warningMap.HasTile(c)) warningMap.SetColor(c, sharedFade);
                }

                yield return null;
            }
            
            foreach (var c in sharedCells)
            {
                 if (warningMap.HasTile(c)) warningMap.SetColor(c, new Color(1f, 0.2f, 0.2f, 0.65f));
            }
        }

        if (warningCells.Contains(player.GetCurrentCellPosition()))
        {
            bool dodged = false;
            if (RunManager.instance != null) dodged = Random.value < RunManager.instance.dodgeChance;

            if (dodged)
            {
                if (AudioManager.instance != null) AudioManager.instance.PlayShieldBreak();
                if (TurnManager.instance != null && TurnManager.instance.dodgeEffectPrefab != null)
                {
                    Instantiate(TurnManager.instance.dodgeEffectPrefab, player.transform.position, Quaternion.identity);
                }
            }
            else if (RunManager.instance.hasBioBarrier)
            {
                foreach (var perk in RunManager.instance.activePerks) if (perk is BioBarrierPerk aegis) { aegis.BreakShield(); break; }
                RunManager.instance.hasBioBarrier = false;
            }
            else player.health.TakeDamage(2);

            Vector3Int pushTarget = TurnManager.instance.GetOppositeCell(player.GetCurrentCellPosition(), cell);
            player.StartKnockbackMovement(pushTarget);
            yield return new WaitUntil(() => !player.IsMoving());
        }

        isChargingAttack = false;
        // Saldırı kendi fade'ini zaten yaptı, kalan tile'ları anında temizle
        if (warningMap != null)
        {
            foreach (var c in warningCells)
            {
                if (warningMap.HasTile(c) && !IsCellTargetedByOtherEnemy(c))
                    warningMap.SetTile(c, null);
            }
        }
        warningCells.Clear();
        currentCooldown = aoeCooldown;
        yield return new WaitForSeconds(0.2f);
    }

    public Vector3Int CalculateNextMove(Vector3Int playerCell)
    {
        Queue<Vector3Int> queue = new Queue<Vector3Int>();
        Dictionary<Vector3Int, Vector3Int> cameFrom = new Dictionary<Vector3Int, Vector3Int>();

        queue.Enqueue(cell); cameFrom[cell] = cell;
        Vector3Int targetNeighbor = playerCell; bool foundPath = false;

        // Yol tamamen tıkalıysa en yakın hücreyi takip etmek için
        Vector3Int closestCell = cell;
        float closestDist = Distance(cell, playerCell);

        while (queue.Count > 0)
        {
            Vector3Int current = queue.Dequeue();
            if (IsNeighbor(current, playerCell)) { targetNeighbor = current; foundPath = true; break; }

            float dist = Distance(current, playerCell);
            if (dist < closestDist) { closestDist = dist; closestCell = current; }

            Vector3Int[] offsets = (current.y % 2 != 0) ? evenOffsets : oddOffsets;
            foreach (var off in offsets)
            {
                Vector3Int next = current + off;
                if (!cameFrom.ContainsKey(next))
                {
                    if (!groundMap.HasTile(next)) continue;
                    if (LevelGenerator.instance != null && LevelGenerator.instance.hazardCells != null && LevelGenerator.instance.hazardCells.Contains(next)) continue;
                    if (TurnManager.instance.IsEnemyAtCell(next) && next != cell) continue;

                    cameFrom[next] = current; queue.Enqueue(next);
                }
            }
        }

        // Tam yol bulunamadıysa, en yakın noktaya doğru git
        Vector3Int destination = foundPath ? targetNeighbor : closestCell;
        if (destination == cell) return cell;

        Vector3Int step = destination;
        while (cameFrom.ContainsKey(step) && cameFrom[step] != cell) step = cameFrom[step];
        return step;
    }

    public void StartKnockbackMovement(Vector3Int targetCell)
    {
        if (enemyBehavior == EnemyBehavior.Totem || enemyBehavior == EnemyBehavior.Boss || enemyBehavior == EnemyBehavior.Warlock) return;

        if (groundMap.HasTile(targetCell))
        {
            cell = targetCell; targetWorldPos = groundMap.GetCellCenterWorld(cell);
            targetWorldPos.z = 0; isMoving = true;
        }
    }

    public float Distance(Vector3Int a, Vector3Int b)
    {
        Vector3Int ac = OffsetToCube(a); Vector3Int bc = OffsetToCube(b);
        return (Mathf.Abs(ac.x - bc.x) + Mathf.Abs(ac.y - bc.y) + Mathf.Abs(ac.z - bc.z)) * 0.5f;
    }

    private Vector3Int OffsetToCube(Vector3Int o)
    {
        int x = o.x - (o.y - (o.y & 1)) / 2; int z = o.y; int y = -x - z;
        return new Vector3Int(x, y, z);
    }

    public Vector3Int GetCurrentCellPosition() => cell;

    public void ForceSetPosition(Vector3Int newCell)
    {
        cell = newCell;
        Tilemap groundMap = TurnManager.instance.groundMap;
        Vector3 worldPos = groundMap.GetCellCenterWorld(newCell);
        worldPos.z = 0;
        transform.position = worldPos;
    }

    public bool IsMoving() => isMoving || isBumping;

    private IEnumerator SpawnImpactVFXSequence(List<Vector3Int> cells)
    {
        foreach (var c in cells)
        {
            Vector3 worldPos = groundMap.GetCellCenterWorld(c);
            worldPos.z = 0f;
            worldPos.x += vfxXOffset;
            worldPos.y += vfxYOffset;
            GameObject vfx = Instantiate(hammerImpactVFXPrefab, worldPos, Quaternion.identity);
            Destroy(vfx, 3f);
            yield return new WaitForSeconds(0.05f);
            AudioManager.instance?.PlayHammer();
            yield return new WaitForSeconds(vfxDelayBetweenCells);
        }
    }

    private bool IsNeighbor(Vector3Int cell1, Vector3Int cell2)
    {
        Vector3Int[] offsets = (cell1.y % 2 != 0) ? evenOffsets : oddOffsets;
        foreach (var off in offsets) if (cell1 + off == cell2) return true;
        return false;
    }
}