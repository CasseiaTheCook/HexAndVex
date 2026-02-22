using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.Tilemaps;
using TMPro;

public class TurnManager : MonoBehaviour
{
    public static TurnManager instance;

    // Inspector'dan atama gerektirecek Player referansı
    public HexMovement player;
    public Tilemap groundMap; // Geri tepme için tile kontrolü

    // Zar Metinleri (Inspector'dan atanmalı)
    public TMP_Text dietext1;
    public TMP_Text dietext2;

    private List<EnemyAI> enemies = new List<EnemyAI>();
    public bool isPlayerTurn = true;
    private int enemiesFinishedMove = 0;

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

    private const float KNOCKBACK_DELAY = 0.4f;
    private const float COMBAT_START_DELAY = 0.5f;
    private const float DICE_VISUAL_DELAY = 0.5f;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        HideDiceResults();
    }

    public void RegisterEnemy(EnemyAI enemy)
    {
        enemies.Add(enemy);
    }

    public List<EnemyAI> GetEnemies()
    {
        return enemies;
    }

    public void PlayerFinishedMove(Vector3Int playerCell)
    {
        if (!isPlayerTurn) return;

        isPlayerTurn = false;

        // Temas varsa Attack Coroutine'i başlayıp turu kendi yönetecek.
        // Temas yoksa, EnemyTurn Coroutine'i başlatılmalıdır.
        if (CheckForEnemyContact(playerCell, player)) return;

        // Temas yoksa normal düşman sırasını başlat
        StartCoroutine(EnemyTurn(playerCell));
    }

    public void EnemyFinishedMove(EnemyAI enemy)
    {
        enemiesFinishedMove++;

        // Düşman hareketini bitirdiğinde oyuncuyla temas kontrolü.
        // Temas varsa Attack Coroutine'i başlayıp turu kendi yönetecek.
        if (CheckForPlayerContact(enemy.GetCurrentCellPosition(), enemy))
        {
            return;
        }

        // Temas yoksa veya muharebe bittiğinde (Attack metodunun sonundan çağrıldığında)
        if (enemiesFinishedMove >= enemies.Count)
        {
            Debug.Log("🔄 Tur Sona Erdi. Yeni Tur Başlıyor (Oyuncu Sırası)");
            enemiesFinishedMove = 0;
            isPlayerTurn = true;
        }
    }

    private IEnumerator EnemyTurn(Vector3Int playerCell)
    {
        yield return new WaitForSeconds(0.4f);

        // Ölmüş düşmanları kontrol et ve listeden temizle
        enemies.RemoveAll(e => e == null || e.health.currentHP <= 0);

        // Her düşman hareket etmeye başlar
        foreach (var e in enemies)
        {
            if (e != null)
            {
                e.MoveTowardsPlayer(playerCell);
            }
        }

        // Eğer hiç düşman kalmadıysa, tur döngüsünü bozmamak için direkt oyuncu turunu başlat
        if (enemies.Count == 0)
        {
            Debug.Log("✅ Tüm düşmanlar temizlendi. Yeni Tur Başlıyor (Oyuncu Sırası)");
            enemiesFinishedMove = 0;
            isPlayerTurn = true;
        }
    }

    // --- SAVAŞ MEKANİKLERİ ---

    private bool CheckForEnemyContact(Vector3Int playerCell, HexMovement targetPlayer)
    {
        foreach (var enemy in enemies)
        {
            if (enemy == null || enemy.health.currentHP <= 0) continue;

            Vector3Int enemyCell = enemy.GetCurrentCellPosition();

            if (IsNeighbor(playerCell, enemyCell))
            {
                Debug.Log($"⚔️ **SAVAŞ BAŞLADI!** Karakter Düşman ile temas etti.");
                // Oyuncu saldırdığı için: isPlayerAttacking = true
                StartCoroutine(Attack(targetPlayer, enemy, playerCell, enemyCell, true));
                return true;
            }
        }
        return false;
    }

    private bool CheckForPlayerContact(Vector3Int enemyCell, EnemyAI enemy)
    {
        if (player == null)
        {
            Debug.LogError("TurnManager: Player (HexMovement) referansı atanmamış!");
            return false;
        }

        Vector3Int playerCell = player.GetCurrentCellPosition();

        if (IsNeighbor(enemyCell, playerCell))
        {
            Debug.Log($"⚠️ **SAVAŞ BAŞLADI! (Düşman Saldırısı)** Düşman pozisyonunda durdu ve saldırdı.");
            // Düşman saldırdığı için: isPlayerAttacking = false
            StartCoroutine(Attack(player, enemy, playerCell, enemyCell, false));
            return true;
        }
        return false;
    }

    // Merkezi Saldırı Metodu
    private IEnumerator Attack(HexMovement targetPlayer, EnemyAI enemy, Vector3Int playerCell, Vector3Int enemyCell, bool isPlayerAttacking)
    {
        // 1. ADIM: Temas Sonrası Bekle (0.5 saniye)
        yield return new WaitForSeconds(COMBAT_START_DELAY);

        // 2. ADIM: ZAR ATMA (2d6)
        int die1 = Random.Range(1, 7);
        int die2 = Random.Range(1, 7);
        int totalDamage = die1 + die2;

        Debug.Log($"    Saldıran ({(isPlayerAttacking ? "Karakter" : "Düşman")}) Zar Sonuçları: Zar 1={die1}, Zar 2={die2}. Toplam Hasar: {totalDamage}");

        ShowDiceResults(die1, die2);

        // 3. ADIM: Zar Sonucunu Göstermek İçin Bekle (0.5 saniye)
        yield return new WaitForSeconds(DICE_VISUAL_DELAY);

        // 4. ADIM: HASAR UYGULAMA & GERİ TEPME BAŞLATMA

        HealthScript targetHealth = isPlayerAttacking ? enemy.health : targetPlayer.health;
        HealthScript attackerHealth = isPlayerAttacking ? targetPlayer.health : enemy.health;
        string targetName = isPlayerAttacking ? "Düşman" : "Karakter";
        string attackerName = isPlayerAttacking ? "Karakter" : "Düşman";

        bool willDie = targetHealth.currentHP <= totalDamage;
        targetHealth.TakeDamage(totalDamage);

        if (willDie)
        {
            Debug.Log($"💀 {targetName} Öldü! {attackerName} kazandı.");

            // Eğer düşman öldüyse, listeden çıkar
            if (isPlayerAttacking)
            {
                enemies.Remove(enemy);
            }
        }
        else
        {
            Debug.Log($"💥 {targetName} hayatta kaldı. Geri tepme (Knockback) uygulanıyor ve {attackerName} 1 hasar alıyor.");

            attackerHealth.TakeDamage(1);

            // Geri Tepme (Knockback) hareketini başlat ve bitmesini bekle
            yield return StartCoroutine(ExecuteKnockback(targetPlayer, enemy, playerCell, enemyCell, isPlayerAttacking));
        }

        // 5. ADIM: Zar Görselini Gizle
        HideDiceResults();

        // 6. ADIM: Turu İlerlet
        // Oyuncu saldırdıysa, oyuncu turu bitti (isPlayerTurn = false) ve düşman sırasına geçilmelidir.
        if (isPlayerAttacking)
        {
            StartCoroutine(EnemyTurn(targetPlayer.GetCurrentCellPosition()));
        }
        else // Düşman saldırdıysa, bu sadece bir düşmanın hareketini ve saldırısını bitirdiği anlamına gelir.
        {
            EnemyFinishedMove(enemy);
        }
    }


    // Zar Metinlerini Gösterme Metodu
    public void ShowDiceResults(int die1, int die2)
    {
        if (dietext1 != null && dietext2 != null)
        {
            dietext1.gameObject.SetActive(true);
            dietext2.gameObject.SetActive(true);
            dietext1.text = die1.ToString();
            dietext2.text = die2.ToString();
        }
    }

    // Zar Metinlerini Gizleme Metodu
    public void HideDiceResults()
    {
        if (dietext1 != null && dietext2 != null)
        {
            dietext1.gameObject.SetActive(false);
            dietext2.gameObject.SetActive(false);
        }
    }

    // Geri Tepme Coroutine'i
    private IEnumerator ExecuteKnockback(HexMovement targetPlayer, EnemyAI enemy, Vector3Int playerCell, Vector3Int enemyCell, bool isPlayerAttacking)
    {
        // Geri tepme yönünü belirle
        Vector3Int kbDirection = isPlayerAttacking ? enemyCell - playerCell : playerCell - enemyCell;

        // Geri tepme, saldırılan karakteri saldırgan karakterden uzaklaştırır.
        Vector3Int targetKnockbackCell;
        Vector3Int attackerKnockbackCell;

        if (isPlayerAttacking) // Oyuncu Saldırdı -> Düşman geri tepsin
        {
            targetKnockbackCell = enemyCell + kbDirection;
            attackerKnockbackCell = playerCell; // Oyuncu yerinde kalır

            enemy.StartKnockbackMovement(targetKnockbackCell);
        }
        else // Düşman Saldırdı -> Oyuncu geri tepsin
        {
            targetKnockbackCell = playerCell + kbDirection;
            attackerKnockbackCell = enemyCell; // Düşman yerinde kalır

            targetPlayer.StartKnockbackMovement(targetKnockbackCell);
        }

        // Geri tepme hareketlerinin bitmesini bekle
        yield return new WaitUntil(() => targetPlayer != null && !targetPlayer.IsMoving() && enemy != null && !enemy.IsMoving());

        Debug.Log("💥 Geri tepme hareketleri bitti.");
    }

    // --- YARDIMCI METOTLAR ---

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
}