using UnityEngine;
using UnityEngine.Tilemaps;

public class EnemyAI : MonoBehaviour
{
    public Tilemap groundMap;
    public HealthScript health;

    [Header("UI Settings")]
    public GameObject intentArrow;

    private Vector3Int cell;
    private Vector3 targetWorldPos;
    private bool isMoving = false;
    private const float ENEMY_MOVE_SPEED = 5f;

    // YENİ: Düşmanın kilitlendiği karar hücresi
    public Vector3Int lockedTargetCell;
    public bool hasLockedTarget = false;

    private static readonly Vector3Int[] oddOffsets = { new Vector3Int(+1, 0, 0), new Vector3Int(0, +1, 0), new Vector3Int(-1, +1, 0), new Vector3Int(-1, 0, 0), new Vector3Int(-1, -1, 0), new Vector3Int(0, -1, 0) };
    private static readonly Vector3Int[] evenOffsets = { new Vector3Int(+1, 0, 0), new Vector3Int(+1, +1, 0), new Vector3Int(0, +1, 0), new Vector3Int(-1, 0, 0), new Vector3Int(0, -1, 0), new Vector3Int(+1, -1, 0) };

    void Awake()
    {
        // Eğer Inspector'dan atanmadıysa otomatik bul
        if (groundMap == null)
        {
            GameObject mapObj = GameObject.Find("GroundMap");
            if (mapObj != null)
            {
                groundMap = mapObj.GetComponent<Tilemap>();
            }
            else
            {
                Debug.LogError("HATA: Sahnede 'GroundMap' isminde bir obje bulunamadı!");
            }
        }
    }
    void Start()
    {
        cell = groundMap.WorldToCell(transform.position);

        // TurnManager'a beni kaydet!
        if (TurnManager.instance != null)
        {
            TurnManager.instance.RegisterEnemy(this);
        }

        targetWorldPos = groundMap.GetCellCenterWorld(cell);
        // ... diğer kodlar
    }

    void Update()
    {
        HandleMovement();
        if (health != null && health.currentHP <= 0) Destroy(gameObject);
    }

    private void HandleMovement()
    {
        if (!isMoving) return;

        transform.position = Vector3.MoveTowards(transform.position, targetWorldPos, ENEMY_MOVE_SPEED * Time.deltaTime);

        if (Vector3.Distance(transform.position, targetWorldPos) < 0.001f)
        {
            transform.position = targetWorldPos;
            isMoving = false;
        }
    }

    // YENİ 1: Tur sana geçmeden önce düşmanın hedefini belirler ve kilitler!
    public void LockNextMove(Vector3Int playerCell, bool isStunned)
    {
        if (intentArrow == null) return;

        // Eğer sersemlediyse, öldüyse veya zaten dibindeyse karar veremez.
        if (isStunned || health.currentHP <= 0 || IsNeighbor(cell, playerCell))
        {
            hasLockedTarget = false;
            intentArrow.SetActive(false);
            return;
        }

        // Oyuncunun ŞU ANKİ yerine göre bir rota çizer
        lockedTargetCell = CalculateNextMove(playerCell);
        
        if (lockedTargetCell != cell)
        {
            hasLockedTarget = true;
            intentArrow.SetActive(true);
            
            Vector3 currentWorldPos = groundMap.GetCellCenterWorld(cell);
            Vector3 nextWorldPos = groundMap.GetCellCenterWorld(lockedTargetCell);
            currentWorldPos.z = 0; nextWorldPos.z = 0;
            
            Vector3 dir = (nextWorldPos - currentWorldPos).normalized;
            
            // Oku gövdesine yapıştırıp, hedefe çevirir
            intentArrow.transform.localPosition = Vector3.zero; 
            intentArrow.transform.right = dir; // Resmin orijinali sağa bakıyorsa. (Değilse up yap)
        }
        else
        {
            hasLockedTarget = false;
            intentArrow.SetActive(false);
        }
    }

    // YENİ 2: Düşmanın sırası gelince KÖRÜ KÖRÜNE kilitli hedefine gider.
    public void ExecuteLockedMove()
    {
        if (isMoving || health.currentHP <= 0) return;

        if (hasLockedTarget)
        {
            // Eğer sen onu knockback ile itmediysen ve hedefi hala komşusuysa/boşsa oraya gider!
            if (IsNeighbor(cell, lockedTargetCell) && 
                !TurnManager.instance.IsEnemyAtCell(lockedTargetCell) && 
                TurnManager.instance.player.GetCurrentCellPosition() != lockedTargetCell)
            {
                cell = lockedTargetCell;
                targetWorldPos = groundMap.GetCellCenterWorld(cell);
                targetWorldPos.z = 0;
                isMoving = true;
            }
            else
            {
                Debug.Log($"💥 {gameObject.name} planladığı yere gidemedi! (Knockback yediği için yönünü kaybetti veya yeri doldu)");
            }
        }
        
        // Hareket bitti, hedefi sıfırla ve oku gizle
        hasLockedTarget = false;
        if (intentArrow != null) intentArrow.SetActive(false);
    }

    public Vector3Int CalculateNextMove(Vector3Int playerCell)
    {
        Vector3Int[] offsets = (cell.y % 2 != 0) ? evenOffsets : oddOffsets;
        float bestDist = float.MaxValue;
        Vector3Int bestMove = cell;

        foreach (var o in offsets)
        {
            Vector3Int n = cell + o;
            
            if (!groundMap.HasTile(n) || n == playerCell) continue; 
            if (TurnManager.instance.IsEnemyAtCell(n)) continue;

            float dist = Distance(n, playerCell);
            if (dist < bestDist)
            {
                bestDist = dist;
                bestMove = n;
            }
        }
        return bestMove;
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
    public bool IsMoving() => isMoving;

    private bool IsNeighbor(Vector3Int cell1, Vector3Int cell2)
    {
        Vector3Int[] offsets = (cell1.y % 2 != 0) ? evenOffsets : oddOffsets;
        foreach (var off in offsets) if (cell1 + off == cell2) return true;
        return false;
    }
}