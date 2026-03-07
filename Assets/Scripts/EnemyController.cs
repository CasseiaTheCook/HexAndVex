using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;
using System.Collections.Generic;

public class EnemyAI : MonoBehaviour
{
    public Tilemap groundMap;
    public HealthScript health;

    [Header("UI Settings")]
    public GameObject intentArrow;
    public GameObject stunEffectObj; // Sersemleme (yıldız) görseli

    // Ok yan/ters bakıyorsa bunu 90, -90 veya 180 yapıp hizala
    public float arrowAngleOffset = 0f;

    // Okların Fade In / Fade Out animasyonları için
    private SpriteRenderer arrowRenderer;
    private Coroutine arrowFadeCoroutine;

    private Vector3Int cell;
    private Vector3 targetWorldPos;
    private bool isMoving = false;
    public bool isBumping = false; // Duvara çarpma animasyonu devrede mi?
    private const float ENEMY_MOVE_SPEED = 5f;

    public Vector3Int lockedTargetCell;
    public bool hasLockedTarget = false;

    // YENİ: Düşmanın kaç tur şokta kalacağını tutan hafıza
    public int skipTurns = 0;

    private static readonly Vector3Int[] oddOffsets = { new Vector3Int(+1, 0, 0), new Vector3Int(0, +1, 0), new Vector3Int(-1, +1, 0), new Vector3Int(-1, 0, 0), new Vector3Int(-1, -1, 0), new Vector3Int(0, -1, 0) };
    private static readonly Vector3Int[] evenOffsets = { new Vector3Int(+1, 0, 0), new Vector3Int(+1, +1, 0), new Vector3Int(0, +1, 0), new Vector3Int(-1, 0, 0), new Vector3Int(0, -1, 0), new Vector3Int(+1, -1, 0) };

    void Awake()
    {
        if (groundMap == null)
        {
            GameObject mapObj = GameObject.Find("GroundMap");
            if (mapObj != null) groundMap = mapObj.GetComponent<Tilemap>();
            else Debug.LogError("HATA: Sahnede 'GroundMap' isminde bir obje bulunamadı!");
        }
    }

    void Start()
    {
        cell = groundMap.WorldToCell(transform.position);

        if (TurnManager.instance != null) TurnManager.instance.RegisterEnemy(this);
        targetWorldPos = groundMap.GetCellCenterWorld(cell);

        SetStunVisual(false);

        // Başlangıçta okun görünmezliğini (Alpha = 0) ayarla
        if (intentArrow != null)
        {
            arrowRenderer = intentArrow.GetComponentInChildren<SpriteRenderer>();
            if (arrowRenderer != null)
            {
                Color c = arrowRenderer.color;
                c.a = 0f;
                arrowRenderer.color = c;
            }
            intentArrow.SetActive(false);
        }
    }

    void Update()
    {
        HandleMovement();

        // Düşman hayatta olduğu sürece şok durumunu her kare kontrol et
        if (health != null && health.currentHP > 0)
        {
            // Adam şokta mı? (skipTurns 0'dan büyük mü?)
            bool isStunned = skipTurns > 0;

            // 1. Yıldızları aç/kapat
            if (stunEffectObj != null)
            {
                if (stunEffectObj.activeSelf != isStunned) stunEffectObj.SetActive(isStunned);
            }

            // 2. KANKA SENİN İSTEDİĞİN YER: Şokta olduğu SÜRECE saydamlaştır!
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

    // --- SERSEMLEME GÖRSELİ ---
    public void SetStunVisual(bool state)
    {
        if (stunEffectObj != null) stunEffectObj.SetActive(state);
    }

    // --- DUVARA ÇARPMA VE ESNEME (BUMP) ANİMASYONU ---
    public void StartWallBump(Vector3 direction)
    {
        StartCoroutine(WallBumpCoroutine(direction));
    }

    private IEnumerator WallBumpCoroutine(Vector3 direction)
    {
        isBumping = true;
        isMoving = true;

        Vector3 originalPos = groundMap.GetCellCenterWorld(cell);
        originalPos.z = 0;

        Vector3 bumpPos = originalPos + (direction * 0.10f);
        float hitSpeed = 4f;
        float returnSpeed = 1f;

        while (Vector3.Distance(transform.position, bumpPos) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(transform.position, bumpPos, hitSpeed * Time.deltaTime);
            yield return null;
        }

        yield return new WaitForSeconds(0.05f); // Tokluk hissi veren Hitstop

        while (Vector3.Distance(transform.position, originalPos) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(transform.position, originalPos, returnSpeed * Time.deltaTime);
            yield return null;
        }

        transform.position = originalPos;
        isBumping = false;
        isMoving = false;
    }

    // --- YUMUŞAK OK ANİMASYONU (FADE IN / FADE OUT) ---
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
        float duration = 0.2f; // Ne kadar sürede eriyip belireceği
        float elapsed = 0f;

        Color c = arrowRenderer.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            c.a = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
            arrowRenderer.color = c;
            yield return null;
        }

        c.a = targetAlpha;
        arrowRenderer.color = c;

        if (!show) intentArrow.SetActive(false);
    }

    // --- OK GÖSTERME VE KİLİTLENME ---
    public void LockNextMove(Vector3Int playerCell, bool isStunned)
    {
        if (intentArrow == null) return;

        if (isStunned || health.currentHP <= 0 || IsNeighbor(cell, playerCell))
        {
            hasLockedTarget = false;
            SetArrowVisibility(false);
            return;
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
            hasLockedTarget = false;
            SetArrowVisibility(false);
        }
    }

    public void ExecuteLockedMove()
    {
        // KRİTİK: Eğer ölmüşse veya şoktaysa hareket E-DE-MEZ!
        if (isMoving || health.currentHP <= 0 || skipTurns > 0)
        {
            if (skipTurns > 0) Debug.Log($"{gameObject.name} sarsıldığı için bu tur kilitli!");
            hasLockedTarget = false;
            SetArrowVisibility(false);
            return;
        }

        if (hasLockedTarget)
        {
            if (IsNeighbor(cell, lockedTargetCell) &&
                !TurnManager.instance.IsEnemyAtCell(lockedTargetCell) &&
                TurnManager.instance.player.GetCurrentCellPosition() != lockedTargetCell)
            {
                cell = lockedTargetCell;
                targetWorldPos = groundMap.GetCellCenterWorld(cell);
                targetWorldPos.z = 0;
                isMoving = true;
            }
        }

        hasLockedTarget = false;
        SetArrowVisibility(false); // Hareket başlarken oku usulca gizler
    }

    // --- AKILLI YOL BULMA (BFS) ---
    public Vector3Int CalculateNextMove(Vector3Int playerCell)
    {
        Queue<Vector3Int> queue = new Queue<Vector3Int>();
        Dictionary<Vector3Int, Vector3Int> cameFrom = new Dictionary<Vector3Int, Vector3Int>();

        queue.Enqueue(cell);
        cameFrom[cell] = cell;

        Vector3Int targetNeighbor = playerCell;
        bool foundPath = false;

        while (queue.Count > 0)
        {
            Vector3Int current = queue.Dequeue();

            if (IsNeighbor(current, playerCell))
            {
                targetNeighbor = current;
                foundPath = true;
                break;
            }

            Vector3Int[] offsets = (current.y % 2 != 0) ? evenOffsets : oddOffsets;

            foreach (var off in offsets)
            {
                Vector3Int next = current + off;

                if (!cameFrom.ContainsKey(next))
                {
                    if (!groundMap.HasTile(next)) continue;
                    if (LevelGenerator.instance != null && LevelGenerator.instance.hazardCells != null && LevelGenerator.instance.hazardCells.Contains(next)) continue;
                    if (TurnManager.instance.IsEnemyAtCell(next) && next != cell) continue;

                    cameFrom[next] = current;
                    queue.Enqueue(next);
                }
            }
        }

        if (foundPath)
        {
            Vector3Int step = targetNeighbor;
            while (cameFrom[step] != cell)
            {
                step = cameFrom[step];
            }
            return step;
        }

        return cell;
    }

    public void StartKnockbackMovement(Vector3Int targetCell)
    {
        if (groundMap.HasTile(targetCell))
        {
            cell = targetCell;
            targetWorldPos = groundMap.GetCellCenterWorld(cell);
            targetWorldPos.z = 0;
            isMoving = true;
        }
    }

    private float Distance(Vector3Int a, Vector3Int b)
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