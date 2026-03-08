using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;
using System.Collections.Generic;
using System.Linq; // LINQ kütüphanesini listeleri taramak için ekledik

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

    public enum EnemyBehavior { Melee, TelegraphAoE }

    [Header("AoE Uyarı Ayarları")]
    public Tilemap warningMap; 
    public TileBase warningTile; 

    [Header("UI Settings")]
    public GameObject intentArrow;
    public GameObject stunEffectObj; 

    public float arrowAngleOffset = 0f;
    private SpriteRenderer arrowRenderer;
    private Coroutine arrowFadeCoroutine;
    private Coroutine telegraphCoroutine;

    private Vector3Int cell;
    private Vector3 targetWorldPos;
    private bool isMoving = false;
    public bool isBumping = false; 
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

        cell = groundMap.WorldToCell(transform.position);

        if (TurnManager.instance != null) TurnManager.instance.RegisterEnemy(this);
        targetWorldPos = groundMap.GetCellCenterWorld(cell);

        SetStunVisual(false);

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
            SetTelegraphVisuals(false, true); 
            isChargingAttack = false;
            warningCells.Clear();
        }
    }

    void OnDestroy()
    {
        if (health != null) health.OnDeath -= HandleDeath;
    }

    void Update()
    {
        HandleMovement();

        if (health != null && health.currentHP > 0)
        {
            bool isStunned = skipTurns > 0;
            if (stunEffectObj != null)
            {
                if (stunEffectObj.activeSelf != isStunned) stunEffectObj.SetActive(isStunned);
            }
            health.SetStunnedAlpha(isStunned);
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

    public void SetStunVisual(bool state)
    {
        if (stunEffectObj != null) stunEffectObj.SetActive(state);
    }

    public void StartWallBump(Vector3 direction)
    {
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
        if (isChargingAttack) SetTelegraphVisuals(false, false);

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
            if (currentCooldown <= 0 && Distance(cell, playerCell) <= aoeAttackRange)
            {
                isChargingAttack = true; 
                hasLockedTarget = false; 
                SetArrowVisibility(false);

                warningCells = GetLineOfCells(cell, playerCell, aoeAttackRange);
                SetTelegraphVisuals(true, false);
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

    // =======================================================
    // YENİ: KARE BAŞKA DÜŞMAN TARAFINDAN KULLANILIYOR MU KONTROLÜ
    // =======================================================
    private bool IsCellTargetedByOtherEnemy(Vector3Int targetCell)
    {
        if (TurnManager.instance == null) return false;
        foreach (var e in TurnManager.instance.enemies)
        {
            // Eğer diğer düşman hayattaysa, saldırı hazırlığındaysa ve bu kare onun listesinde de varsa
            if (e != null && e != this && e.isChargingAttack && e.warningCells.Contains(targetCell))
            {
                return true; 
            }
        }
        return false;
    }

    private void SetTelegraphVisuals(bool show, bool instantClear)
    {
        if (warningMap == null || warningTile == null) return;
        if (telegraphCoroutine != null) StopCoroutine(telegraphCoroutine);

        List<Vector3Int> cellsToAnimate = new List<Vector3Int>(warningCells);

        if (instantClear && !show)
        {
            foreach (var c in cellsToAnimate)
            {
                // YENİ: Başka düşman kullanmıyorsa sil, kullanıyorsa Kırmızı bırak!
                if (!IsCellTargetedByOtherEnemy(c))
                {
                    warningMap.SetTile(c, null);
                }
                else
                {
                    warningMap.SetColor(c, new Color(1f, 0.2f, 0.2f, 0.65f)); 
                }
            }
            return;
        }

        telegraphCoroutine = StartCoroutine(AnimateTelegraphCoroutine(show, cellsToAnimate));
    }

    private IEnumerator AnimateTelegraphCoroutine(bool show, List<Vector3Int> cells)
    {
        float duration = show ? 0.35f : 0.2f; 
        float elapsed = 0f;

        Color invisibleColor = new Color(1f, 0.2f, 0.2f, 0f);
        Color visibleColor = new Color(1f, 0.2f, 0.2f, 0.65f); 

        Color startColor = show ? invisibleColor : visibleColor;
        Color endColor = show ? visibleColor : invisibleColor;

        if (show)
        {
            foreach (var c in cells)
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
            foreach (var c in cells)
            {
                if (warningMap.HasTile(c)) warningMap.SetColor(c, current);
            }
            yield return null;
        }

        foreach (var c in cells) if (warningMap.HasTile(c)) warningMap.SetColor(c, endColor);

        if (!show)
        {
            foreach (var c in cells)
            {
                // YENİ: Fade Out bittiğinde eğer başkası kullanıyorsa silme, geri kırmızı yap!
                if (!IsCellTargetedByOtherEnemy(c))
                {
                    warningMap.SetTile(c, null);
                }
                else
                {
                    warningMap.SetColor(c, visibleColor);
                }
            }
        }
    }

    public IEnumerator ExecuteAoEAttackCoroutine(HexMovement player)
    {
        if (!isChargingAttack) yield break;

        Debug.Log($"💥 {gameObject.name} Balyozunu Yere Vurdu!");

        if (warningMap != null && warningCells.Count > 0)
        {
            float flashDur = 0.05f;
            float elapsed = 0f;
            Color startRed = new Color(1f, 0.2f, 0.2f, 0.65f);
            Color whiteFlash = new Color(1f, 1f, 1f, 0.9f);

            while(elapsed < flashDur) {
                elapsed += Time.deltaTime;
                Color current = Color.Lerp(startRed, whiteFlash, elapsed / flashDur);
                foreach (var c in warningCells) warningMap.SetColor(c, current);
                yield return null;
            }

            elapsed = 0f;
            float burnDur = 0.15f;
            Color yellowBurn = new Color(1f, 0.8f, 0f, 0.8f);

            while(elapsed < burnDur) {
                elapsed += Time.deltaTime;
                Color current = Color.Lerp(whiteFlash, yellowBurn, elapsed / burnDur);
                foreach (var c in warningCells) warningMap.SetColor(c, current);
                yield return null;
            }
            
            yield return new WaitForSeconds(0.1f); 
        }

        if (warningCells.Contains(player.GetCurrentCellPosition()))
        {
            bool dodged = false;
            if (RunManager.instance != null)
            {
                dodged = RunManager.instance.hasHolyAegis || Random.value < RunManager.instance.dodgeChance;
                if (RunManager.instance.hasHolyAegis) RunManager.instance.hasHolyAegis = false;
            }

            if (!dodged)
            {
                player.health.TakeDamage(2); 
                Debug.Log("🔥 Alan saldırısına yakalandın!");
                
                Vector3Int pushTarget = TurnManager.instance.GetOppositeCell(player.GetCurrentCellPosition(), cell);
                player.StartKnockbackMovement(pushTarget);
                yield return new WaitUntil(() => !player.IsMoving());
            }
            else
            {
                Debug.Log("🛡️ DODGE! Alan saldırısından son anda kaçtın.");
            }
        }

        SetTelegraphVisuals(false, false);
        isChargingAttack = false;
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