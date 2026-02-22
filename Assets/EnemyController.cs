using UnityEngine;
using UnityEngine.Tilemaps;

public class EnemyAI : MonoBehaviour
{
    public Tilemap groundMap;
    public HealthScript health;

    private Vector3Int cell;
    private Vector3 targetWorldPos;
    private bool isMoving = false;
    private const float ENEMY_MOVE_SPEED = 5f;

    private static readonly Vector3Int[] oddOffsets = { new Vector3Int(+1, 0, 0), new Vector3Int(0, +1, 0), new Vector3Int(-1, +1, 0), new Vector3Int(-1, 0, 0), new Vector3Int(-1, -1, 0), new Vector3Int(0, -1, 0) };
    private static readonly Vector3Int[] evenOffsets = { new Vector3Int(+1, 0, 0), new Vector3Int(+1, +1, 0), new Vector3Int(0, +1, 0), new Vector3Int(-1, 0, 0), new Vector3Int(0, -1, 0), new Vector3Int(+1, -1, 0) };

    void Start()
    {
        cell = groundMap.WorldToCell(transform.position);
        TurnManager.instance.RegisterEnemy(this);
        targetWorldPos = groundMap.GetCellCenterWorld(cell);
        targetWorldPos.z = 0;
        transform.position = targetWorldPos;
        if (health == null) health = GetComponent<HealthScript>();
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

    public void MoveTowardsPlayer(Vector3Int playerCell)
    {
        if (isMoving || health.currentHP <= 0) return;

        Vector3Int[] offsets = (cell.y % 2 != 0) ? evenOffsets : oddOffsets;
        float bestDist = float.MaxValue;
        Vector3Int bestMove = cell;

        foreach (var o in offsets)
        {
            Vector3Int n = cell + o;
            // Oyuncunun KENDİ karesine adım atmaya çalışmasını engelle!
            if (!groundMap.HasTile(n) || n == playerCell) continue; 
            
            float dist = Distance(n, playerCell);
            if (dist < bestDist)
            {
                bestDist = dist;
                bestMove = n;
            }
        }

        if (bestMove != cell)
        {
            cell = bestMove;
            targetWorldPos = groundMap.GetCellCenterWorld(cell);
            targetWorldPos.z = 0;
            isMoving = true;
        }
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
}