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

    [Header("Düşman Tipi ve Saldırı Ayarları")]
    public EnemyBehavior enemyBehavior = EnemyBehavior.Melee;
    public int aoeAttackRange = 3;
    public int aoeCooldown = 2;
    private int currentCooldown = 0;
    private bool isFirstTurn = true;

    public enum EnemyBehavior { Melee, TelegraphAoE, Totem, Boss }

    [Header("AoE Uyarı Ayarları")]
    public Tilemap warningMap;
    public TileBase warningTile;

    [Header("UI Settings")]
    public GameObject intentArrow;
    
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
        if (visualRenderer == null) visualRenderer = GetComponent<SpriteRenderer>();

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

        if (health != null) health.OnDeath += HandleDeath;
    }

    private void HandleDeath()
    {
        if (isChargingAttack)
        {
            isChargingAttack = false;
            ForceClearWarningCells();
        }

        if (enemyBehavior == EnemyBehavior.Totem && SpawnerBossAI.instance != null)
        {
            SpawnerBossAI.instance.OnTotemDestroyed();
        }
        else if (enemyBehavior == EnemyBehavior.Boss && SpawnerBossAI.instance != null)
        {
            SpawnerBossAI.instance.OnBossDied();
        }
    }

    void OnDestroy()
    {
        if (health != null) health.OnDeath -= HandleDeath;
    }

    void OnMouseDown()
    {
        if (TurnManager.instance != null && TurnManager.instance.isNecroShotTargeting)
            TurnManager.instance.TryNecroShotKill(this);
    }

    void Update()
    {
        HandleMovement();

        if (health != null && health.currentHP > 0 && !isFading)
        {
            // Health barı soluklaşması her türlü sersemletmede çalışsın
            health.SetStunnedAlpha(skipTurns > 0);
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
        if (enemyBehavior == EnemyBehavior.Totem || enemyBehavior == EnemyBehavior.Boss) return;
        StartCoroutine(WallBumpCoroutine(direction));
    }

    private IEnumerator WallBumpCoroutine(Vector3 direction)
    {
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

        if (isMoving || health.currentHP <= 0 || skipTurns > 0)
        {
            hasLockedTarget = false; SetArrowVisibility(false); return;
        }

        if (isChargingAttack) return;

        if (hasLockedTarget)
        {
            if (IsNeighbor(cell, lockedTargetCell) &&
                !TurnManager.instance.IsEnemyAtCell(lockedTargetCell) &&
                TurnManager.instance.player.GetCurrentCellPosition() != lockedTargetCell)
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

        if (isChargingAttack)
        {
            isChargingAttack = false;
            ForceClearWarningCells();
        }

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
        if (SpawnerBossAI.instance != null && SpawnerBossAI.instance.IsCellTargetedByBoss(targetCell)) return true;

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
        if (warningMap == null) return;

        foreach (var c in warningCells)
        {
            if (warningMap.HasTile(c))
            {
                if (!IsCellTargetedByOtherEnemy(c))
                {
                    warningMap.SetTile(c, null);
                }
                else
                {
                    warningMap.SetColor(c, new Color(1f, 0.2f, 0.2f, 0.65f)); 
                }
            }
        }
        warningCells.Clear();
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

            foreach (var c in ownCells)
            {
                if (warningMap.HasTile(c)) warningMap.SetColor(c, attackFlashColor);
            }
            
            foreach (var c in sharedCells)
            {
                if (warningMap.HasTile(c)) warningMap.SetColor(c, attackFlashColor);
            }

            yield return new WaitForSeconds(0.1f);

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
                if (TurnManager.instance != null && TurnManager.instance.dodgeEffectPrefab != null)
                {
                    Instantiate(TurnManager.instance.dodgeEffectPrefab, player.transform.position, Quaternion.identity);
                }
            }
            else if (RunManager.instance.hasBioBarrier) RunManager.instance.hasBioBarrier = false;
            else player.health.TakeDamage(2);

            Vector3Int pushTarget = TurnManager.instance.GetOppositeCell(player.GetCurrentCellPosition(), cell);
            player.StartKnockbackMovement(pushTarget);
            yield return new WaitUntil(() => !player.IsMoving());
        }

        isChargingAttack = false; 
        ForceClearWarningCells();
        currentCooldown = aoeCooldown;
        yield return new WaitForSeconds(0.2f);
    }

    public Vector3Int CalculateNextMove(Vector3Int playerCell)
    {
        Queue<Vector3Int> queue = new Queue<Vector3Int>();
        Dictionary<Vector3Int, Vector3Int> cameFrom = new Dictionary<Vector3Int, Vector3Int>();

        queue.Enqueue(cell); cameFrom[cell] = cell;
        Vector3Int targetNeighbor = playerCell; bool foundPath = false;

        while (queue.Count > 0)
        {
            Vector3Int current = queue.Dequeue();
            if (IsNeighbor(current, playerCell)) { targetNeighbor = current; foundPath = true; break; }

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

        if (foundPath)
        {
            Vector3Int step = targetNeighbor;
            while (cameFrom[step] != cell) step = cameFrom[step];
            return step;
        }
        return cell;
    }

    public void StartKnockbackMovement(Vector3Int targetCell)
    {
        if (enemyBehavior == EnemyBehavior.Totem || enemyBehavior == EnemyBehavior.Boss) return;

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
    public bool IsMoving() => isMoving || isBumping;

    private bool IsNeighbor(Vector3Int cell1, Vector3Int cell2)
    {
        Vector3Int[] offsets = (cell1.y % 2 != 0) ? evenOffsets : oddOffsets;
        foreach (var off in offsets) if (cell1 + off == cell2) return true;
        return false;
    }
}