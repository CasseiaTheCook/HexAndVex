using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class HexMovement : MonoBehaviour
{
    public Tilemap groundMap;
    public Tilemap highlightMap;

    public UnityEngine.Tilemaps.AnimatedTile highlightTile;

    public HealthScript health;

    [Header("Görsel Ayarlar")]
    public float playerVisualOffsetY = 0.25f;

    public SpriteRenderer visualRenderer;
    
    [Header("Animasyonlar")]
    public Animator animator;

    private const float MOVEMENT_SPEED = 8f;

    private Vector3Int currentCellPosition;
    private Vector3 targetWorldPosition;
    private bool isMoving = false;
    private bool isKnockbackMove = false;
    private bool _preserveFacingNextMove = false;

    // 2-hex hareket için ara nokta bilgisi
    private Dictionary<Vector3Int, Vector3Int> waypointMap = new Dictionary<Vector3Int, Vector3Int>();
    private Vector3Int? currentWaypoint = null;
    private Vector3Int finalTarget;

    private class HighlightData
    {
        public float currentAlpha;
        public float targetAlpha;
        public float fadeSpeed;
    }
    private Dictionary<Vector3Int, HighlightData> highlights = new Dictionary<Vector3Int, HighlightData>();

    private float targetAlphaValue = 1f;

    private Tile frozenDummyTile;

    private static readonly Vector3Int[] oddOffsets = { new Vector3Int(+1, 0, 0), new Vector3Int(0, +1, 0), new Vector3Int(-1, +1, 0), new Vector3Int(-1, 0, 0), new Vector3Int(-1, -1, 0), new Vector3Int(0, -1, 0) };
    private static readonly Vector3Int[] evenOffsets = { new Vector3Int(+1, 0, 0), new Vector3Int(+1, +1, 0), new Vector3Int(0, +1, 0), new Vector3Int(-1, 0, 0), new Vector3Int(0, -1, 0), new Vector3Int(+1, -1, 0) };

    void Start()
    {
        if (groundMap == null) groundMap = GameObject.Find("GroundMap").GetComponent<Tilemap>();
        if (highlightMap == null) highlightMap = GameObject.Find("HighlightMap").GetComponent<Tilemap>();
        if (health == null) health = GetComponent<HealthScript>();

        if (visualRenderer == null) visualRenderer = GetComponentInChildren<SpriteRenderer>();
        if (animator == null) animator = GetComponentInChildren<Animator>();

        frozenDummyTile = ScriptableObject.CreateInstance<Tile>();

        currentCellPosition = groundMap.WorldToCell(transform.position);
        targetWorldPosition = groundMap.GetCellCenterWorld(currentCellPosition);
        targetWorldPosition.z = 0;
        transform.position = targetWorldPosition;

        UpdateHighlights();
    }

    void Update()
    {
        HandleMovement();
        ProcessHighlights();

        if (!isMoving && TurnManager.instance != null && TurnManager.instance.isPlayerTurn && !TurnManager.instance.IsAnyTargetingActive)
        {
            HandleMovementInput();
        }
    }

    void LateUpdate()
    {
        if (visualRenderer != null)
        {
            int order = 100 + Mathf.RoundToInt(-transform.position.y * 10f);
            visualRenderer.sortingOrder = order;

            if (health != null && health.hptext != null)
            {
                Canvas hpCanvas = health.hptext.GetComponentInParent<Canvas>();
                if (hpCanvas != null) hpCanvas.sortingOrder = order + 3;
            }

            bool canAttack = false;
            
            if (TurnManager.instance != null)
            {
                canAttack = TurnManager.instance.isPlayerTurn && !TurnManager.instance.hasAttackedThisTurn;
                if (TurnManager.instance.isAttackAnimationPlaying) canAttack = true; 
            }

            targetAlphaValue = canAttack ? 1f : 0.5f;

            Color c = visualRenderer.color;
            c.a = Mathf.MoveTowards(c.a, targetAlphaValue, 3f * Time.deltaTime);
            visualRenderer.color = c;
        }
    }

    public void SetVisualAlpha(float targetAlpha) { targetAlphaValue = targetAlpha; }

    private void HandleMovementInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 worldPoint = GetMousePositionOnZPlane();
            Vector3Int clickedCell = groundMap.WorldToCell(worldPoint);

            if (highlights.ContainsKey(clickedCell) && highlights[clickedCell].targetAlpha > 0f)
            {
                isKnockbackMove = false;
                TurnManager.instance.isPlayerTurn = false;
                TurnManager.instance.HideAllEnemyIntents();

                Sprite currentFrameSprite = highlightMap.GetSprite(clickedCell);
                if (currentFrameSprite != null)
                {
                    frozenDummyTile.sprite = currentFrameSprite;
                    highlightMap.SetTile(clickedCell, frozenDummyTile);
                    highlightMap.SetTileFlags(clickedCell, TileFlags.None);
                }

                foreach (var kvp in highlights)
                {
                    if (kvp.Key != clickedCell) {
                        kvp.Value.targetAlpha = 0f; kvp.Value.fadeSpeed = 15f;
                    } else {
                        kvp.Value.currentAlpha = 1f; kvp.Value.targetAlpha = 0f; kvp.Value.fadeSpeed = 2.5f;
                    }
                }

                // 2-hex hareket mi kontrol et
                if (waypointMap.ContainsKey(clickedCell))
                {
                    currentWaypoint = waypointMap[clickedCell];
                    finalTarget = clickedCell;
                    MoveCharacter(currentWaypoint.Value);
                }
                else
                {
                    currentWaypoint = null;
                    MoveCharacter(clickedCell);
                }
            }
        }
    }

    private Vector3 GetMousePositionOnZPlane()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Plane zPlane = new Plane(Vector3.forward, Vector3.zero); 
        if (zPlane.Raycast(ray, out float distance)) return ray.GetPoint(distance);
        return Vector3.zero; 
    }

    private void HandleMovement()
    {
        if (isMoving)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetWorldPosition, MOVEMENT_SPEED * Time.deltaTime);

            if (Vector3.Distance(transform.position, targetWorldPosition) < 0.001f)
            {
                transform.position = targetWorldPosition;

                Vector3 checkPos = transform.position;
                Vector3Int newCell = groundMap.WorldToCell(checkPos);
                if (newCell != currentCellPosition) currentCellPosition = newCell;

                // Waypoint varsa: ara noktaya ulaştık, şimdi hedef hücreye devam et
                if (currentWaypoint.HasValue)
                {
                    currentWaypoint = null;
                    MoveCharacter(finalTarget);
                    // Hâlâ isMoving = true, döngü devam edecek
                    return;
                }

                isMoving = false;
                FaceCombatTarget();

                if (!isKnockbackMove) TurnManager.instance.PlayerFinishedMove(currentCellPosition);
                isKnockbackMove = false;
            }
        }
    }

    private void MoveCharacter(Vector3Int targetCell)
    {
        if (AudioManager.instance != null) AudioManager.instance.PlayMove();

        targetWorldPosition = groundMap.GetCellCenterWorld(targetCell);
        targetWorldPosition.z = 0;

        if (!_preserveFacingNextMove && visualRenderer != null)
        {
            float dx = targetWorldPosition.x - transform.position.x;
            if (Mathf.Abs(dx) > 0.01f) visualRenderer.flipX = (dx < 0);
        }
        _preserveFacingNextMove = false;

        isMoving = true;
    }

    public void ForceSetPosition(Vector3Int targetCell)
    {
        currentCellPosition = targetCell;
        targetWorldPosition = groundMap.GetCellCenterWorld(targetCell);
        targetWorldPosition.z = 0;
        transform.position = targetWorldPosition;
        isMoving = false;
        isKnockbackMove = false;
    }

    private void FaceCombatTarget()
    {
        Vector3Int[] offsets = (currentCellPosition.y % 2 != 0) ? evenOffsets : oddOffsets;
        foreach (var off in offsets)
        {
            Vector3Int neighbor = currentCellPosition + off;
            if (TurnManager.instance.IsEnemyAtCell(neighbor))
            {
                Vector3 enemyPos = groundMap.GetCellCenterWorld(neighbor);
                float dx = enemyPos.x - transform.position.x;
                if (Mathf.Abs(dx) > 0.01f && visualRenderer != null) visualRenderer.flipX = (dx < 0);
                break;
            }
        }
    }

    public void StartKnockbackMovement(Vector3Int targetCell, bool preserveFacing = false)
    {
        if (groundMap.HasTile(targetCell))
        {
            _preserveFacingNextMove = preserveFacing;
            isKnockbackMove = true;
            currentCellPosition = targetCell;
            MoveCharacter(targetCell);
        }
    }

    public bool IsMoving() => isMoving;

    public void UpdateHighlights()
    {
        waypointMap.Clear();
        Vector3Int[] offsets = (currentCellPosition.y % 2 != 0) ? evenOffsets : oddOffsets;
        List<Vector3Int> validCells = new List<Vector3Int>();

        // Range-1: komşu hücreler
        foreach (var off in offsets)
        {
            Vector3Int neighbor = currentCellPosition + off;
            if (groundMap.HasTile(neighbor))
            {
                bool isHazard = LevelGenerator.instance != null && LevelGenerator.instance.hazardCells != null && LevelGenerator.instance.hazardCells.Contains(neighbor);
                if (!isHazard && TurnManager.instance != null && !TurnManager.instance.IsEnemyAtCell(neighbor)) validCells.Add(neighbor);
            }
        }

        // Range-2: Surge Boot aktifse, komşuların komşularını da ekle
        if (RunManager.instance != null && RunManager.instance.surgeBootActive)
        {
            List<Vector3Int> range2Cells = new List<Vector3Int>();
            foreach (var mid in validCells)
            {
                Vector3Int[] midOffsets = (mid.y % 2 != 0) ? evenOffsets : oddOffsets;
                foreach (var off2 in midOffsets)
                {
                    Vector3Int far = mid + off2;
                    if (far == currentCellPosition) continue; // Başlangıç noktasına geri dönme
                    if (validCells.Contains(far)) continue;   // Zaten range-1'de var
                    if (range2Cells.Contains(far)) continue;  // Zaten eklendi
                    if (!groundMap.HasTile(far)) continue;
                    bool isHazard = LevelGenerator.instance != null && LevelGenerator.instance.hazardCells != null && LevelGenerator.instance.hazardCells.Contains(far);
                    if (isHazard) continue;
                    if (TurnManager.instance != null && TurnManager.instance.IsEnemyAtCell(far)) continue;

                    range2Cells.Add(far);
                    waypointMap[far] = mid; // Bu hücreye gitmek için mid'den geç
                }
            }
            validCells.AddRange(range2Cells);
        }

        foreach (var cell in validCells)
        {
            highlightMap.SetTile(cell, null);
            highlightMap.SetTile(cell, highlightTile);
            highlightMap.SetTileFlags(cell, TileFlags.None);

            if (!highlights.ContainsKey(cell)) highlights[cell] = new HighlightData { currentAlpha = 0f, targetAlpha = 0.6f, fadeSpeed = 4f };
            else { highlights[cell].targetAlpha = 0.6f; highlights[cell].fadeSpeed = 4f; }

            highlightMap.SetColor(cell, new Color(1f, 1f, 1f, highlights[cell].currentAlpha));
        }

        foreach (var cell in highlights.Keys.ToList())
        {
            if (!validCells.Contains(cell)) { highlights[cell].targetAlpha = 0f; highlights[cell].fadeSpeed = 4f; }
        }
    }

    public void ClearHighlights()
    {
        foreach (var cell in highlights.Keys.ToList()) { highlights[cell].targetAlpha = 0f; highlights[cell].fadeSpeed = 4f; }
    }

    private void ProcessHighlights()
    {
        List<Vector3Int> cellsToRemove = new List<Vector3Int>();
        foreach (var kvp in highlights)
        {
            Vector3Int cell = kvp.Key; HighlightData data = kvp.Value;
            if (data.currentAlpha != data.targetAlpha)
            {
                data.currentAlpha = Mathf.MoveTowards(data.currentAlpha, data.targetAlpha, data.fadeSpeed * Time.deltaTime);
                if (highlightMap.HasTile(cell)) highlightMap.SetColor(cell, new Color(1f, 1f, 1f, data.currentAlpha));
            }
            if (data.currentAlpha <= 0.01f && data.targetAlpha <= 0.01f) { if (highlightMap.HasTile(cell)) highlightMap.SetTile(cell, null); cellsToRemove.Add(cell); }
        }
        foreach (var c in cellsToRemove) highlights.Remove(c);
    }

    public Vector3Int GetCurrentCellPosition() => currentCellPosition;

    public void TriggerAttackAnimation() { if (animator != null) animator.SetTrigger("Attack"); }
}