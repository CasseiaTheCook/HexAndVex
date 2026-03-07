using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;
using System.Collections.Generic;

public class EnemyAI : MonoBehaviour
{
    public Tilemap groundMap;
    public HealthScript health;
    public int skipTurns = 0; // YENİ: Düşmanın kaç tur boyunca kilitli kalacağını tutar

    [Header("UI Settings")]
    public GameObject intentArrow;
    public GameObject stunEffectObj; // YENİ: Kafada dönecek sersemleme görseli
    
    // OK YÖNÜ İNCE AYARI: Ok yan/ters bakıyorsa bunu 90, -90 veya 180 yap.
    public float arrowAngleOffset = 0f; 

    private Vector3Int cell;
    private Vector3 targetWorldPos;
    private bool isMoving = false;
    public bool isBumping = false; // Duvara çarpma animasyonu devrede mi?
    private const float ENEMY_MOVE_SPEED = 5f;

    public Vector3Int lockedTargetCell;
    public bool hasLockedTarget = false;

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

        if (TurnManager.instance != null)
        {
            TurnManager.instance.RegisterEnemy(this);
        }

        targetWorldPos = groundMap.GetCellCenterWorld(cell);
        
        // Oyun başlarken sersemleme efekti gizli olsun
        SetStunVisual(false);
    }

    void Update()
    {
        HandleMovement();
    }

    private void HandleMovement()
    {
        // Eğer hareket etmiyorsa veya "duvara çarpma" animasyonu oynuyorsa normal yürüyüşü durdur
        if (!isMoving || isBumping) return;

        transform.position = Vector3.MoveTowards(transform.position, targetWorldPos, ENEMY_MOVE_SPEED * Time.deltaTime);

        if (Vector3.Distance(transform.position, targetWorldPos) < 0.001f)
        {
            transform.position = targetWorldPos;
            isMoving = false;
        }
    }

    // --- YENİ EKLENEN SERSEMLEME GÖRSELİ KONTROLÜ ---
    public void SetStunVisual(bool state)
    {
        if (stunEffectObj != null) stunEffectObj.SetActive(state);
    }

    // --- YENİ EKLENEN DUVARA ÇARPMA (BUMP) ANİMASYONU ---
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
        
        // YENİ: 0.6f çok fazlaydı, 0.35f ile tam olarak kendi altıgeninin bittiği duvara yapışacak.
        Vector3 bumpPos = originalPos + (direction * 0.10f); 

        // YENİ: Hızları böldük ve yavaşlattık. 
        float hitSpeed = 12f;   // Duvara çarpma hızı (eskiden 20'ydi)
        float returnSpeed = 8f; // Yerine dönme hızı (sersemlediği için daha yavaş dönüyor)
        
        // 1. İleri (Duvarın dibine doğru) savrul
        while (Vector3.Distance(transform.position, bumpPos) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(transform.position, bumpPos, hitSpeed * Time.deltaTime);
            yield return null;
        }

        // YENİ: HİTSTOP EFEKTİ! Duvara yapıştığı an çok ufak bir süre donar. 
        // Vuruşun o "tok" hissini veren en büyük sırdır.
        yield return new WaitForSeconds(0.05f); 

        // 2. Geri (Kendi hücresinin merkezine) sek
        while (Vector3.Distance(transform.position, originalPos) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(transform.position, originalPos, returnSpeed * Time.deltaTime);
            yield return null;
        }

        transform.position = originalPos; 
        isBumping = false;
        isMoving = false; 
    }

    // --- OK GÖSTERME VE KİLİTLENME ---
    public void LockNextMove(Vector3Int playerCell, bool isStunned)
    {
        if (intentArrow == null) return;

        if (isStunned || health.currentHP <= 0 || IsNeighbor(cell, playerCell))
        {
            hasLockedTarget = false;
            intentArrow.SetActive(false);
            return;
        }

        lockedTargetCell = CalculateNextMove(playerCell);

        if (lockedTargetCell != cell)
        {
            hasLockedTarget = true;
            intentArrow.SetActive(true);

            Vector3 currentWorldPos = groundMap.GetCellCenterWorld(cell);
            Vector3 nextWorldPos = groundMap.GetCellCenterWorld(lockedTargetCell);
            currentWorldPos.z = 0; nextWorldPos.z = 0;

            intentArrow.transform.position = currentWorldPos;

            Vector3 direction = nextWorldPos - currentWorldPos;
            
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            intentArrow.transform.rotation = Quaternion.AngleAxis(angle + arrowAngleOffset, Vector3.forward);
        }
        else
        {
            hasLockedTarget = false;
            intentArrow.SetActive(false);
        }
    }

    public void ExecuteLockedMove()
    {
        // YENİ KONTROL: Eğer skipTurns > 0 ise, hiçbir şekilde hareket etme!
        if (isMoving || health.currentHP <= 0 || skipTurns > 0) 
        {
            if (skipTurns > 0) Debug.Log($"{gameObject.name} sarsıldığı için hareket edemiyor!");
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
        if (intentArrow != null) intentArrow.SetActive(false);
    }

    // --- AKILLI YOL BULMA (BFS ALGORİTMASI) ---
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
                    // Zemin yoksa geçemez
                    if (!groundMap.HasTile(next)) continue;
                    
                    // Diken (Hazard) varsa İÇİNE GİREMEZ
                    if (LevelGenerator.instance != null && LevelGenerator.instance.hazardCells != null && LevelGenerator.instance.hazardCells.Contains(next)) continue;
                    
                    // Başka bir düşman oradayken üzerinden geçemez
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

        // Eğer yol yoksa (ada ayrık veya sıkıştıysa) titremek yerine bekler
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
    public bool IsMoving() => isMoving || isBumping; // Animasyon oynarken de hareket ediyor sayılır

    private bool IsNeighbor(Vector3Int cell1, Vector3Int cell2)
    {
        Vector3Int[] offsets = (cell1.y % 2 != 0) ? evenOffsets : oddOffsets;
        foreach (var off in offsets) if (cell1 + off == cell2) return true;
        return false;
    }
}