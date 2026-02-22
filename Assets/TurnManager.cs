using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.Tilemaps;
using TMPro;

public class TurnManager : MonoBehaviour
{
    public static TurnManager instance;

    public HexMovement player;
    public Tilemap groundMap;

    public TMP_Text dietext1;
    public TMP_Text dietext2;

    private List<EnemyAI> enemies = new List<EnemyAI>();
    public bool isPlayerTurn = true;

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

    private const float COMBAT_START_DELAY = 0.5f;
    private const float DICE_VISUAL_DELAY = 0.5f;

void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);

        // OTOMATİK ATAMA (Güvenlik Ağı)
        if (player == null) player = FindFirstObjectByType<HexMovement>(); // Sahnede HexMovement scripti olan objeyi bulur
        
        if (groundMap == null) 
        {
            GameObject mapObj = GameObject.Find("GroundMap");
            if (mapObj != null) groundMap = mapObj.GetComponent<Tilemap>();
        }

        // UI Text'leri isminden bulma (Eğer hiyerarşideki adları farklıysa buraları değiştir)
        if (dietext1 == null) 
        {
            GameObject dt1 = GameObject.Find("DieText1"); // Hiyerarşideki adının tam olarak bu olması lazım
            if (dt1 != null) dietext1 = dt1.GetComponent<TMP_Text>();
        }
        
        if (dietext2 == null) 
        {
            GameObject dt2 = GameObject.Find("DieText2");
            if (dt2 != null) dietext2 = dt2.GetComponent<TMP_Text>();
        }
    }

    void Start()
    {
        HideDiceResults();
    }

    public void RegisterEnemy(EnemyAI enemy)
    {
        if (!enemies.Contains(enemy)) enemies.Add(enemy);
    }

    // Oyuncu hareketini bitirdiğinde HexMovement tarafından çağrılır
    public void PlayerFinishedMove(Vector3Int playerCell)
    {
        if (!isPlayerTurn) return;
        isPlayerTurn = false;

        // Oyuncu bir düşmanın dibine girdiyse savaşı başlat
        if (CheckForEnemyContact(playerCell, true)) return;

        // Temas yoksa düşmanların turu başlar
        StartCoroutine(EnemyTurn(playerCell));
    }

    private IEnumerator EnemyTurn(Vector3Int playerCell)
    {
        yield return new WaitForSeconds(0.2f); // Tur geçişi için ufak bir es

        // Ölmüş düşmanları listeden temizle
        enemies.RemoveAll(e => e == null || e.health.currentHP <= 0);

        foreach (var e in enemies)
        {
            if (e == null) continue;

            // Zaten oyuncunun yanındaysa, hiç hareket etmeden direkt saldır!
            if (IsNeighbor(e.GetCurrentCellPosition(), player.GetCurrentCellPosition()))
            {
                StartCoroutine(Attack(player, e, player.GetCurrentCellPosition(), e.GetCurrentCellPosition(), false));
                yield break; // Savaş başladı, diğer düşmanların turu İPTAL (Tur oyuncuya dönecek)
            }

            // Düşmanı hareket ettir
            e.MoveTowardsPlayer(playerCell);
            
            // Düşmanın hareketi bitene kadar bekle (Race condition'ı engeller)
            yield return new WaitUntil(() => !e.IsMoving());

            // Hareket ettikten sonra oyuncunun dibine girdiyse saldır!
            if (IsNeighbor(e.GetCurrentCellPosition(), player.GetCurrentCellPosition()))
            {
                StartCoroutine(Attack(player, e, player.GetCurrentCellPosition(), e.GetCurrentCellPosition(), false));
                yield break; // Savaş başladı, tur iptal!
            }

            yield return new WaitForSeconds(0.1f); // Düşmanlar arası akıcı görünüm için hafif bekleme
        }

        // Eğer hiç savaş yaşanmadıysa standart olarak turu oyuncuya ver
        Debug.Log("🔄 Düşman turu bitti. Sıra oyuncuda.");
        isPlayerTurn = true;
    }

    // --- SAVAŞ MEKANİKLERİ ---

    private bool CheckForEnemyContact(Vector3Int playerCell, bool isPlayerAttacking)
    {
        foreach (var enemy in enemies)
        {
            if (enemy == null || enemy.health.currentHP <= 0) continue;

            Vector3Int enemyCell = enemy.GetCurrentCellPosition();

            if (IsNeighbor(playerCell, enemyCell))
            {
                Debug.Log($"⚔️ **SAVAŞ BAŞLADI!**");
                StartCoroutine(Attack(player, enemy, playerCell, enemyCell, isPlayerAttacking));
                return true;
            }
        }
        return false;
    }

    private IEnumerator Attack(HexMovement targetPlayer, EnemyAI enemy, Vector3Int playerCell, Vector3Int enemyCell, bool isPlayerAttacking)
    {
        yield return new WaitForSeconds(COMBAT_START_DELAY);

        // ZAR ATMA (2d6)
        int die1 = Random.Range(1, 7);
        int die2 = Random.Range(1, 7);
        int totalDamage = die1 + die2;

        ShowDiceResults(die1, die2);
        yield return new WaitForSeconds(DICE_VISUAL_DELAY);

        HealthScript targetHealth = isPlayerAttacking ? enemy.health : targetPlayer.health;
        HealthScript attackerHealth = isPlayerAttacking ? targetPlayer.health : enemy.health;

        bool willDie = targetHealth.currentHP <= totalDamage;
        targetHealth.TakeDamage(totalDamage);

        if (willDie)
        {
            Debug.Log("💀 Hedef öldü!");
            if (isPlayerAttacking) enemies.Remove(enemy);
        }
        else
        {
            Debug.Log("💥 Hedef hayatta kaldı! İki taraf da geri sekiyor ve saldıran 1 hasar alıyor.");
            attackerHealth.TakeDamage(1);

            // İKİSİNİ DE GERİ TEP
            yield return StartCoroutine(ExecuteKnockback(targetPlayer, enemy, playerCell, enemyCell));
        }

        HideDiceResults();

        // KURAL: Savaş bittiğinde tur HER ZAMAN oyuncuya geçer!
        Debug.Log("🔄 Savaş bitti. Sıra oyuncuda.");
        isPlayerTurn = true;
    }

    private IEnumerator ExecuteKnockback(HexMovement playerObj, EnemyAI enemyObj, Vector3Int pCell, Vector3Int eCell)
    {
        // En güvenli yöntem: Zıt yönü dünya pozisyonu (uzaklık) ile bulmak
        Vector3Int playerKnockbackCell = GetOppositeCell(pCell, eCell);
        Vector3Int enemyKnockbackCell = GetOppositeCell(eCell, pCell);

        playerObj.StartKnockbackMovement(playerKnockbackCell);
        enemyObj.StartKnockbackMovement(enemyKnockbackCell);

        // İkisinin de hareketinin bitmesini bekle
        yield return new WaitUntil(() => 
            (playerObj == null || !playerObj.IsMoving()) && 
            (enemyObj == null || !enemyObj.IsMoving())
        );
    }

    // Hedef hücreden "en uzak" komşuyu (tam zıt yönü) bulur
    private Vector3Int GetOppositeCell(Vector3Int centerCell, Vector3Int awayFromCell)
    {
        Vector3Int[] offsets = (centerCell.y % 2 != 0) ? evenOffsets : oddOffsets;
        Vector3Int bestCell = centerCell;
        float maxDist = -1f;

        Vector3 awayWorldPos = groundMap.GetCellCenterWorld(awayFromCell);

        foreach (var off in offsets)
        {
            Vector3Int neighbor = centerCell + off;
            if (!groundMap.HasTile(neighbor)) continue; // Harita dışına itme

            Vector3 neighborWorldPos = groundMap.GetCellCenterWorld(neighbor);
            float dist = Vector3.Distance(neighborWorldPos, awayWorldPos);

            if (dist > maxDist)
            {
                maxDist = dist;
                bestCell = neighbor;
            }
        }
        return bestCell;
    }

    private bool IsNeighbor(Vector3Int cell1, Vector3Int cell2)
    {
        Vector3Int[] offsets = (cell1.y % 2 != 0) ? evenOffsets : oddOffsets;
        foreach (var off in offsets)
        {
            if (cell1 + off == cell2) return true;
        }
        return false;
    }

    public void ShowDiceResults(int die1, int die2)
    {
        if (dietext1 != null && dietext2 != null)
        {
            dietext1.gameObject.SetActive(true); dietext2.gameObject.SetActive(true);
            dietext1.text = die1.ToString(); dietext2.text = die2.ToString();
        }
    }

    public void HideDiceResults()
    {
        if (dietext1 != null && dietext2 != null)
        {
            dietext1.gameObject.SetActive(false); dietext2.gameObject.SetActive(false);
        }
    }
}