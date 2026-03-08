using UnityEngine;
using UnityEngine.Tilemaps;

public class HexMovement : MonoBehaviour
{
    public Tilemap groundMap;
    public Tilemap highlightMap;
    
    // Unity'nin kendi animasyonlu tile yapısı için (Eğer hata verirse AnimatedTile yerine TileBase yapabilirsin)
    public UnityEngine.Tilemaps.AnimatedTile highlightTile; 
    
    public HealthScript health;

    [Header("Görsel Ayarlar")]
    // YENİ: Karakterin zemine gömük durmaması için yukarı doğru kaydırma miktarı
    // Inspector'dan bu sayıyla oynayıp karakterin tam zemine basmasını sağlayabilirsin.
    public float playerVisualOffsetY = 0.25f; 

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

        // Başlangıç pozisyonunu ayarla
        currentCellPosition = groundMap.WorldToCell(transform.position);
        targetWorldPosition = groundMap.GetCellCenterWorld(currentCellPosition);
        targetWorldPosition.y += playerVisualOffsetY; // Karakteri hafif havaya kaldır
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

    private void HandleMovementInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 worldPoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            worldPoint.z = 0;
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

                // Koordinat bulurken offset'i çıkarıyoruz ki altımızdaki doğru hücreyi saptayalım
                Vector3 checkPos = transform.position;
                checkPos.y -= playerVisualOffsetY;
                
                Vector3Int newCell = groundMap.WorldToCell(checkPos);
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
        
        // YENİ: Karakter hedefe giderken zeminin merkezine değil, biraz üstüne gider
        targetWorldPosition.y += playerVisualOffsetY; 
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
        ClearHighlights();
        
        // DİKKAT: Artık bulunduğumuz kareyi (currentCellPosition) boyamıyoruz!
        
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
                    HighlightCell(neighbor); // Artık renk parametresi yollamıyoruz!
                }
            }
        }
    }

    public void ClearHighlights()
    {
        highlightMap.ClearAllTiles();
    }

    // ==========================================
    // RENK BOYAMASI İPTAL EDİLDİ (Orijinal haliyle çizilir)
    // ==========================================
    private void HighlightCell(Vector3Int cell)
    {
        // Sadece tile'ı koyuyoruz, renk (tint) ayarıyla oynamıyoruz.
        highlightMap.SetTile(cell, highlightTile);
    }

    public Vector3Int GetCurrentCellPosition() => currentCellPosition;
}