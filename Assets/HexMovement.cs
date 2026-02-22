using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;
using TMPro;

public class HexMovement : MonoBehaviour
{
    // === INSPECTOR ===
    public Tilemap groundMap;
    public Tilemap highlightMap;
    public Tile highlightTile;

    public TMP_Text dietext1;
    public TMP_Text dietext2;

    public HealthScript health;

    // === AYARLAR ===
    private readonly Color CURRENT_POS_COLOR = Color.blue;
    private readonly Color MOVEABLE_COLOR = Color.yellow;
    private const float MOVEMENT_SPEED = 8f;

    // === DEĞİŞKENLER ===
    private Vector3Int currentCellPosition;
    private Vector3 targetWorldPosition;
    private bool isMoving = false;
    private bool isKnockbackMove = false;

    // --- HEX OFFSETLERİ ---
    private static readonly Vector3Int[] oddOffsets = new Vector3Int[]
    {
        new Vector3Int(+1, 0, 0), new Vector3Int(0, +1, 0), new Vector3Int(-1, +1, 0),
        new Vector3Int(-1, 0, 0), new Vector3Int(-1, -1, 0), new Vector3Int(0, -1, 0)
    };

    private static readonly Vector3Int[] evenOffsets = new Vector3Int[]
    {
        new Vector3Int(+1, 0, 0), new Vector3Int(+1, +1, 0), new Vector3Int(0, +1, 0),
        new Vector3Int(-1, 0, 0), new Vector3Int(0, -1, 0), new Vector3Int(+1, -1, 0)
    };

    void Start()
    {
        currentCellPosition = groundMap.WorldToCell(transform.position);
        targetWorldPosition = groundMap.GetCellCenterWorld(currentCellPosition);
        targetWorldPosition.z = 0;
        transform.position = targetWorldPosition;
        UpdateHighlights();

        if (health == null)
            health = GetComponent<HealthScript>();
    }

    void Update()
    {
        HandleMovement();
        // Sadece oyuncu turuysa ve hareket etmiyorsak input al
        if (!isMoving && TurnManager.instance != null && TurnManager.instance.isPlayerTurn)
            HandleMovementInput();
    }

    private void HandleMovementInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 worldPoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            worldPoint.z = 0;
            Vector3Int clickedCell = groundMap.WorldToCell(worldPoint);

            if (IsNeighbor(currentCellPosition, clickedCell) &&
                groundMap.HasTile(clickedCell))
            {
                isKnockbackMove = false; // Normal oyuncu hareketinde sıfırla
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
                    UpdateHighlights();
                }

                // Sadece normal oyuncu hareketi bittiğinde ve geri tepme yoksa TurnManager'a bildir
                if (!isKnockbackMove)
                {
                    // isPlayerTurn kontrolü artık TurnManager'da yapıldığı için doğrudan çağırabiliriz
                    TurnManager.instance.PlayerFinishedMove(currentCellPosition);
                }

                // Geri tepme bitince bayrağı sıfırla. TurnManager WaitUntil ile kontrol ettiği için buraya bildirmeye gerek yok.
                isKnockbackMove = false;
            }
        }
    }

    private void MoveCharacter(Vector3Int targetCell)
    {
        // Hedef hücreyi, düşmanları kontrol ederek belirleme mantığı buraya eklenebilir,
        // ancak mevcut kod komşu hücreye hareket ettiği için şimdilik sade bırakıldı.
        targetWorldPosition = groundMap.GetCellCenterWorld(targetCell);
        targetWorldPosition.z = 0;
        isMoving = true;
    }

    // --- Karakterin Geri Tepme Hareketi ---
    public void StartKnockbackMovement(Vector3Int targetCell)
    {
        if (groundMap.HasTile(targetCell))
        {
            isKnockbackMove = true; // Geri tepme bayrağını ayarla
            currentCellPosition = targetCell; // Hücre pozisyonunu hemen güncelle
            MoveCharacter(targetCell);
            Debug.Log($"    Karakter geri tepme karesine hareket ediyor: {targetCell}");
        }
        else
        {
            Debug.Log("    Karakter geri tepemedi (engel var).");
        }
    }

    // --- Diğer Metodlar ---
    public bool IsMoving()
    {
        return isMoving;
    }

    private bool IsNeighbor(Vector3Int cell1, Vector3Int cell2)
    {
        Vector3Int[] offsets = (cell1.y % 2 != 0) ? evenOffsets : oddOffsets;

        foreach (var off in offsets)
        {
            if (cell1 + off == cell2)
                return true;
        }
        return false;
    }

    private void UpdateHighlights()
    {
        highlightMap.ClearAllTiles();
        HighlightCell(currentCellPosition, CURRENT_POS_COLOR);
        Vector3Int[] offsets = (currentCellPosition.y % 2 != 0) ? evenOffsets : oddOffsets;

        foreach (var off in offsets)
        {
            Vector3Int neighbor = currentCellPosition + off;
            if (groundMap.HasTile(neighbor))
                HighlightCell(neighbor, MOVEABLE_COLOR);
        }
    }

    private void HighlightCell(Vector3Int cell, Color color)
    {
        highlightMap.SetTile(cell, highlightTile);
        highlightMap.SetTileFlags(cell, TileFlags.None);
        highlightMap.SetColor(cell, color);
    }

    public Vector3Int GetCurrentCellPosition()
    {
        return currentCellPosition;
    }
}