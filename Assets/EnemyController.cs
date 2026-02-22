using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;

public class EnemyAI : MonoBehaviour
{
    // === INSPECTOR ===
    public Tilemap groundMap;
    public HexMovement player; // Player'a hala ihtiyacımız var (MoveTowardsPlayer)
    public HealthScript health;

    // === DEĞİŞKENLER ===
    private Vector3Int cell;
    private Vector3 targetWorldPos;
    private bool isMoving = false;
    private bool isKnockbackMove = false;

    private const float ENEMY_MOVE_SPEED = 5f;

    // --- HEX OFFSETLERİ ---
    private static readonly Vector3Int[] oddOffsets =
    {
        new Vector3Int(+1, 0, 0), new Vector3Int(0, +1, 0), new Vector3Int(-1, +1, 0),
        new Vector3Int(-1, 0, 0), new Vector3Int(-1, -1, 0), new Vector3Int(0, -1, 0)
    };

    private static readonly Vector3Int[] evenOffsets =
    {
        new Vector3Int(+1, 0, 0), new Vector3Int(+1, +1, 0), new Vector3Int(0, +1, 0),
        new Vector3Int(-1, 0, 0), new Vector3Int(0, -1, 0), new Vector3Int(+1, -1, 0)
    };

    void Start()
    {
        cell = groundMap.WorldToCell(transform.position);
        TurnManager.instance.RegisterEnemy(this);

        targetWorldPos = groundMap.GetCellCenterWorld(cell);
        targetWorldPos.z = 0;
        transform.position = targetWorldPos;

        if (health == null)
            health = GetComponent<HealthScript>();
    }

    void Update()
    {
        HandleMovement();

        // Ölüp ölmediğini kontrol et
        if (health != null && health.currentHP <= 0)
        {
            Destroy(gameObject);
        }
    }

    // --- Hareket İşleme ---
    private void HandleMovement()
    {
        if (!isMoving) return;

        transform.position = Vector3.MoveTowards(
            transform.position,
            targetWorldPos,
            ENEMY_MOVE_SPEED * Time.deltaTime
        );

        if (Vector3.Distance(transform.position, targetWorldPos) < 0.001f)
        {
            transform.position = targetWorldPos;
            isMoving = false;

            // Geri tepme hareketi değilse, düşman hareketini TurnManager'a bildir
            if (!isKnockbackMove)
            {
                // Düşman normal hareketini bitirdiğinde, TurnManager'a bildir
                TurnManager.instance.EnemyFinishedMove(this);
            }

            // Geri tepme bitince bayrağı sıfırla, TurnManager WaitUntil ile kontrol ettiği için buraya bildirmeye gerek yok
            isKnockbackMove = false;
        }
    }

    // --- Geri Tepme Mekanizması ---
    public void StartKnockbackMovement(Vector3Int targetCell)
    {
        if (groundMap.HasTile(targetCell))
        {
            isKnockbackMove = true; // Geri tepme bayrağını ayarla
            cell = targetCell; // Hücre pozisyonunu hemen güncelle
            targetWorldPos = groundMap.GetCellCenterWorld(cell);
            targetWorldPos.z = 0;

            isMoving = true;
            Debug.Log($"    Düşman geri tepme karesine hareket ediyor: {targetCell}");
        }
        else
        {
            Debug.Log("    Düşman geri tepemedi (engel var).");
        }
    }

    // --- Diğer Metodlar ---

    public void MoveTowardsPlayer(Vector3Int playerCell)
    {
        if (isMoving || health.currentHP <= 0) return;

        Vector3Int[] offsets = (cell.y % 2 != 0) ? evenOffsets : oddOffsets;
        float bestDist = float.MaxValue;
        Vector3Int bestMove = cell;

        foreach (var o in offsets)
        {
            Vector3Int n = cell + o;
            if (!groundMap.HasTile(n)) continue;
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
        else
        {
            // Hareket edemiyorsa (sıkışmışsa veya zaten yanındaysa), yine de turu ilerletmek için bildirim yapılmalıdır.
            // Ancak bu, düşmanın hareket etmeye çalıştıktan sonraki CheckForPlayerContact ile yönetilecektir.
            // Eğer hareket etme imkanı yoksa ve oyuncunun yanında duruyorsa, CheckForPlayerContact çağrılacaktır.
            TurnManager.instance.EnemyFinishedMove(this);
        }
    }

    // ... Distance ve OffsetToCube metotları yerinde kalmalı ...
    private float Distance(Vector3Int a, Vector3Int b)
    {
        Vector3Int ac = OffsetToCube(a);
        Vector3Int bc = OffsetToCube(b);
        return (Mathf.Abs(ac.x - bc.x) + Mathf.Abs(ac.y - bc.y) + Mathf.Abs(ac.z - bc.z)) * 0.5f;
    }

    private Vector3Int OffsetToCube(Vector3Int o)
    {
        int x = o.x - (o.y - (o.y & 1)) / 2;
        int z = o.y;
        int y = -x - z;
        return new Vector3Int(x, y, z);
    }
    // ...

    public Vector3Int GetCurrentCellPosition()
    {
        return cell;
    }

    public bool IsMoving()
    {
        return isMoving;
    }
}