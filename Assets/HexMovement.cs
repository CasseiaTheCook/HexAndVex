using UnityEngine;
using UnityEngine.Tilemaps;

public class HexMovement : MonoBehaviour
{
    public Tilemap groundMap;
    public Tilemap highlightMap;
    public Tile highlightTile;
    public HealthScript health;

    private readonly Color CURRENT_POS_COLOR = Color.blue;
    private readonly Color MOVEABLE_COLOR = Color.yellow;
    private const float MOVEMENT_SPEED = 8f;

    private Vector3Int currentCellPosition;
    private Vector3 targetWorldPosition;
    private bool isMoving = false;
    private bool isKnockbackMove = false;

    private static readonly Vector3Int[] oddOffsets = { new Vector3Int(+1, 0, 0), new Vector3Int(0, +1, 0), new Vector3Int(-1, +1, 0), new Vector3Int(-1, 0, 0), new Vector3Int(-1, -1, 0), new Vector3Int(0, -1, 0) };
    private static readonly Vector3Int[] evenOffsets = { new Vector3Int(+1, 0, 0), new Vector3Int(+1, +1, 0), new Vector3Int(0, +1, 0), new Vector3Int(-1, 0, 0), new Vector3Int(0, -1, 0), new Vector3Int(+1, -1, 0) };

    void Start()
    {
        if (groundMap == null) groundMap = GameObject.Find("GroundMap").GetComponent<Tilemap>();
        if (highlightMap == null) highlightMap = GameObject.Find("HighlightMap").GetComponent<Tilemap>();
        if (health == null) health = GetComponent<HealthScript>();

        currentCellPosition = groundMap.WorldToCell(transform.position);
        targetWorldPosition = groundMap.GetCellCenterWorld(currentCellPosition);
        targetWorldPosition.z = 0;
        transform.position = targetWorldPosition;
        UpdateHighlights();
    }

    void Update()
    {
        HandleMovement();
        // HexMovement.cs Update içinde:
        if (!isMoving && TurnManager.instance != null && TurnManager.instance.isPlayerTurn)
        {
            HandleMovementInput();
        }
    }

    private void HandleMovementInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 worldPoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            worldPoint.z = 0;
            Vector3Int clickedCell = groundMap.WorldToCell(worldPoint);

            if (IsNeighbor(currentCellPosition, clickedCell) && groundMap.HasTile(clickedCell))
            {
                isKnockbackMove = false;

                // YENİ: Oyuncu bir hamle seçtiği an turu kilitlenir ve sarı kareler silinir!
                TurnManager.instance.isPlayerTurn = false;
                ClearHighlights();

                MoveCharacter(clickedCell);
            }
        }
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

                Vector3Int newCell = groundMap.WorldToCell(transform.position);
                if (newCell != currentCellPosition)
                {
                    currentCellPosition = newCell;
                }

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
        isMoving = true;
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
        highlightMap.ClearAllTiles();
        HighlightCell(currentCellPosition, CURRENT_POS_COLOR);
        Vector3Int[] offsets = (currentCellPosition.y % 2 != 0) ? evenOffsets : oddOffsets;

        foreach (var off in offsets)
        {
            Vector3Int neighbor = currentCellPosition + off;
            if (groundMap.HasTile(neighbor)) HighlightCell(neighbor, MOVEABLE_COLOR);
        }
    }

    public void ClearHighlights()
    {
        highlightMap.ClearAllTiles();
    }

    private void HighlightCell(Vector3Int cell, Color color)
    {
        highlightMap.SetTile(cell, highlightTile);
        highlightMap.SetTileFlags(cell, TileFlags.None);
        highlightMap.SetColor(cell, color);
    }

    public Vector3Int GetCurrentCellPosition() => currentCellPosition;
}