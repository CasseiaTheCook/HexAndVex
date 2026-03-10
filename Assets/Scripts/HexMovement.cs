using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;
using System.Collections.Generic;

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

    private List<Vector3Int> activeHighlightCells = new List<Vector3Int>();
    private Coroutine highlightFadeCoroutine;

    // YENİ: Saydamlık kilidi
    private float targetAlphaValue = 1f;

    private static readonly Vector3Int[] oddOffsets = { new Vector3Int(+1, 0, 0), new Vector3Int(0, +1, 0), new Vector3Int(-1, +1, 0), new Vector3Int(-1, 0, 0), new Vector3Int(-1, -1, 0), new Vector3Int(0, -1, 0) };
    private static readonly Vector3Int[] evenOffsets = { new Vector3Int(+1, 0, 0), new Vector3Int(+1, +1, 0), new Vector3Int(0, +1, 0), new Vector3Int(-1, 0, 0), new Vector3Int(0, -1, 0), new Vector3Int(+1, -1, 0) };

    void Start()
    {
        if (groundMap == null) groundMap = GameObject.Find("GroundMap").GetComponent<Tilemap>();
        if (highlightMap == null) highlightMap = GameObject.Find("HighlightMap").GetComponent<Tilemap>();
        if (health == null) health = GetComponent<HealthScript>();

        if (visualRenderer == null) visualRenderer = GetComponentInChildren<SpriteRenderer>();
        if (animator == null) animator = GetComponentInChildren<Animator>();

        currentCellPosition = groundMap.WorldToCell(transform.position);
        targetWorldPosition = groundMap.GetCellCenterWorld(currentCellPosition);
        targetWorldPosition.z = 0;
        transform.position = targetWorldPosition;

        UpdateHighlights();
    }

    void Update()
    {
        HandleMovement();

        if (!isMoving && TurnManager.instance != null && TurnManager.instance.isPlayerTurn)
        {
            HandleMovementInput();
        }
    }

    void LateUpdate()
    {
        if (visualRenderer != null)
        {
            // TurnManager'dan kontrol et: Bizim sıramız değilse VEYA bu el zaten vurduysak soluklaş
            bool canAttack = false;
            
            if (TurnManager.instance != null)
            {
                canAttack = TurnManager.instance.isPlayerTurn && !TurnManager.instance.hasAttackedThisTurn;
                
                // Kılıç inene kadar parlak kalsın
                if (TurnManager.instance.isAttackAnimationPlaying)
                {
                    canAttack = true; 
                }
            }

            // Gidilecek saydamlığı belirle (Vurabiliyorsa 1f, vuramıyorsa veya sıra onda değilse 0.5f)
            targetAlphaValue = canAttack ? 1f : 0.5f;

            Color c = visualRenderer.color;
            c.a = Mathf.MoveTowards(c.a, targetAlphaValue, 3f * Time.deltaTime);
            visualRenderer.color = c;
        }
    }

    // TurnManager.cs eskiden bu fonksiyonu arıyordu, artık sadece LateUpdate içindeki mantığı destekleyecek kadar bıraktık
    public void SetVisualAlpha(float targetAlpha)
    {
        targetAlphaValue = targetAlpha;
    }

    private void HandleMovementInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 worldPoint = GetMousePositionOnZPlane();
            Vector3Int clickedCell = groundMap.WorldToCell(worldPoint);

            if (IsNeighbor(currentCellPosition, clickedCell) &&
                groundMap.HasTile(clickedCell) &&
                !LevelGenerator.instance.hazardCells.Contains(clickedCell) &&
                !TurnManager.instance.IsEnemyAtCell(clickedCell))
            {
                isKnockbackMove = false;
                TurnManager.instance.isPlayerTurn = false;

                TurnManager.instance.HideAllEnemyIntents();
                ClearHighlights();
                MoveCharacter(clickedCell);
            }
        }
    }

    private Vector3 GetMousePositionOnZPlane()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Plane zPlane = new Plane(Vector3.forward, Vector3.zero); 

        if (zPlane.Raycast(ray, out float distance))
        {
            return ray.GetPoint(distance);
        }

        return Vector3.zero; 
    }

    private void HandleMovement()
    {
        if (isMoving)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                targetWorldPosition,
                MOVEMENT_SPEED * Time.deltaTime
            );

            if (Vector3.Distance(transform.position, targetWorldPosition) < 0.001f)
            {
                transform.position = targetWorldPosition;
                isMoving = false;

                Vector3 checkPos = transform.position;

                Vector3Int newCell = groundMap.WorldToCell(checkPos);
                if (newCell != currentCellPosition)
                {
                    currentCellPosition = newCell;
                }

                FaceCombatTarget();

                if (!isKnockbackMove)
                {
                    TurnManager.instance.PlayerFinishedMove(currentCellPosition);
                }

                isKnockbackMove = false;
            }
        }
    }

    private void MoveCharacter(Vector3Int targetCell)
    {
        targetWorldPosition = groundMap.GetCellCenterWorld(targetCell);
        targetWorldPosition.z = 0;

        if (visualRenderer != null)
        {
            float dx = targetWorldPosition.x - transform.position.x;
            if (Mathf.Abs(dx) > 0.01f)
            {
                visualRenderer.flipX = (dx < 0);
            }
        }

        isMoving = true;
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

                if (Mathf.Abs(dx) > 0.01f && visualRenderer != null)
                {
                    visualRenderer.flipX = (dx < 0);
                }
                break;
            }
        }
    }

    public void StartKnockbackMovement(Vector3Int targetCell)
    {
        if (groundMap.HasTile(targetCell))
        {
            isKnockbackMove = true;
            currentCellPosition = targetCell;
            MoveCharacter(targetCell);
        }
    }

    public bool IsMoving() => isMoving;

    private bool IsNeighbor(Vector3Int cell1, Vector3Int cell2)
    {
        Vector3Int[] offsets = (cell1.y % 2 != 0) ? evenOffsets : oddOffsets;
        foreach (var off in offsets) if (cell1 + off == cell2) return true;
        return false;
    }

    public void UpdateHighlights()
    {
        if (highlightFadeCoroutine != null) StopCoroutine(highlightFadeCoroutine);

        highlightMap.ClearAllTiles();
        activeHighlightCells.Clear();

        Vector3Int[] offsets = (currentCellPosition.y % 2 != 0) ? evenOffsets : oddOffsets;

        foreach (var off in offsets)
        {
            Vector3Int neighbor = currentCellPosition + off;

            if (groundMap.HasTile(neighbor))
            {
                bool isHazard = false;
                if (LevelGenerator.instance != null && LevelGenerator.instance.hazardCells != null)
                {
                    isHazard = LevelGenerator.instance.hazardCells.Contains(neighbor);
                }

                if (!isHazard && !TurnManager.instance.IsEnemyAtCell(neighbor))
                {
                    activeHighlightCells.Add(neighbor);
                }
            }
        }

        if (activeHighlightCells.Count > 0)
        {
            highlightFadeCoroutine = StartCoroutine(FadeHighlightsCoroutine(true, new List<Vector3Int>(activeHighlightCells)));
        }
    }

    public void ClearHighlights()
    {
        if (highlightFadeCoroutine != null) StopCoroutine(highlightFadeCoroutine);

        if (activeHighlightCells.Count > 0)
        {
            highlightFadeCoroutine = StartCoroutine(FadeHighlightsCoroutine(false, new List<Vector3Int>(activeHighlightCells)));
        }

        activeHighlightCells.Clear();
    }

    private IEnumerator FadeHighlightsCoroutine(bool fadeIn, List<Vector3Int> cellsToAnimate)
    {
        float duration = 0.2f;
        float elapsed = 0f;

        float maxAlpha = 0.5f;
        float startAlpha = fadeIn ? 0f : maxAlpha;
        float endAlpha = fadeIn ? maxAlpha : 0f;

        if (fadeIn)
        {
            foreach (var cell in cellsToAnimate)
            {
                highlightMap.SetTile(cell, highlightTile);
                highlightMap.SetTileFlags(cell, TileFlags.None);
                highlightMap.SetColor(cell, new Color(1f, 1f, 1f, 0f));
                highlightMap.RefreshTile(cell);
            }
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float currentAlpha = Mathf.Lerp(startAlpha, endAlpha, elapsed / duration);
            Color currentColor = new Color(1f, 1f, 1f, currentAlpha);

            foreach (var cell in cellsToAnimate)
            {
                if (highlightMap.HasTile(cell)) highlightMap.SetColor(cell, currentColor);
            }
            yield return null;
        }

        Color finalColor = new Color(1f, 1f, 1f, endAlpha);
        foreach (var cell in cellsToAnimate)
        {
            if (highlightMap.HasTile(cell)) highlightMap.SetColor(cell, finalColor);
        }

        if (!fadeIn)
        {
            foreach (var cell in cellsToAnimate) highlightMap.SetTile(cell, null);
        }
    }

    public Vector3Int GetCurrentCellPosition() => currentCellPosition;

    public void TriggerAttackAnimation()
    {
        if (animator != null)
        {
            animator.SetTrigger("Attack"); 
        }
    }
}