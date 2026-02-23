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

    // Bu tur sersemlemiş (Stun yemiş) düşmanları tuttuğumuz liste
    private List<EnemyAI> stunnedEnemiesThisTurn = new List<EnemyAI>();

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
    private const float DICE_VISUAL_DELAY = 1.0f;

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);

        // OTOMATİK ATAMALAR (Güvenlik)
        if (player == null) player = FindFirstObjectByType<HexMovement>();

        if (groundMap == null)
        {
            GameObject mapObj = GameObject.Find("GroundMap");
            if (mapObj != null) groundMap = mapObj.GetComponent<Tilemap>();
        }

        if (dietext1 == null)
        {
            GameObject dt1 = GameObject.Find("DieText1");
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
        // Oyun başlar başlamaz düşmanlara hedeflerini kilitlet
        Invoke("LockAllEnemyIntents", 0.5f);
    }

    public void RegisterEnemy(EnemyAI enemy)
    {
        if (!enemies.Contains(enemy)) enemies.Add(enemy);
    }

    // --- HEDEFLERİ KİLİTLEME METODU (OKLAR İÇİN) ---
    public void LockAllEnemyIntents()
    {
        if (player == null || player.health.currentHP <= 0) return;

        Vector3Int pCell = player.GetCurrentCellPosition();

        foreach (var e in enemies)
        {
            if (e != null)
            {
                bool isStunned = stunnedEnemiesThisTurn.Contains(e);
                e.LockNextMove(pCell, isStunned);
            }
        }
    }

    // --- 1. OYUNCUNUN HAMLESİ ---
    // --- 1. OYUNCUNUN HAMLESİ ---
    public void PlayerFinishedMove(Vector3Int playerCell)
    {
        // YENİ: Eskiden burada "if (!isPlayerTurn) return;" yazıyordu. Bunu sildik!
        // Çünkü oyuncu tıkladığı an HexMovement içinde bu değeri bilerek false yaptık.

        StartCoroutine(HandlePlayerPhase(playerCell));
    }

    // --- 2. OYUNCU FAZI (Yürüyüş sonrası anında saldırı) ---
    private IEnumerator HandlePlayerPhase(Vector3Int playerCell)
    {
        List<EnemyAI> adjacentEnemies = GetAdjacentEnemies(player.GetCurrentCellPosition());

        if (adjacentEnemies.Count > 0)
        {
            Debug.Log("🗡️ İLK SALDIRI (FIRST STRIKE)! Oyuncu hamlesini yapıp düşmana vurdu.");
            yield return StartCoroutine(MultiAttack(adjacentEnemies, true));
        }

        // Oyuncu hayattaysa Düşman Fazına geç
        if (player != null && player.health.currentHP > 0)
        {
            StartCoroutine(EnemyPhase());
        }
    }

    // --- 3. DÜŞMAN FAZI ---
    private IEnumerator EnemyPhase()
    {
        yield return new WaitForSeconds(0.2f);

        // Ölmüş düşmanları listeden temizle (Null kontrolü)
        enemies.RemoveAll(e => e == null || e.health.currentHP <= 0);

        // 1. AŞAMA: Tüm düşmanlara KİLİTLENDİKLERİ YERE gitme emrini ver!
        foreach (var e in enemies)
        {
            // Eğer sersemlemediyse (stun yemediyse) kilitli hedefine gider
            if (e != null && !stunnedEnemiesThisTurn.Contains(e))
            {
                e.ExecuteLockedMove();
            }
        }

        // 2. AŞAMA: TÜM düşmanların hareketinin bitmesini bekle
        // Bu sayede spam tıklama yapılsa bile oyunun mantığı bozulmaz
        yield return new WaitUntil(() =>
        {
            foreach (var e in enemies)
            {
                if (e != null && e.IsMoving()) return false;
            }
            return true;
        });

        yield return new WaitForSeconds(0.2f);

        // 3. AŞAMA: Düşmanlar yürüdü ve durdu. PUSU SAVAŞI KONTROLÜ
        List<EnemyAI> adjacentEnemies = GetAdjacentEnemies(player.GetCurrentCellPosition());

        // Sersemlemiş (Stunned) düşmanlar o tur sana vuramaz, listeden çıkarıyoruz
        adjacentEnemies.RemoveAll(e => stunnedEnemiesThisTurn.Contains(e));

        if (adjacentEnemies.Count > 0)
        {
            Debug.Log("🛡️ DÜŞMAN SALDIRISI! Düşmanlar oyuncuya saldırdı.");
            // Düşmanlar başlattığı için playerInitiated = false (Oyuncu hasar alabilir)
            yield return StartCoroutine(MultiAttack(adjacentEnemies, false));
        }

        // 4. AŞAMA: TURU OYUNCUYA DEVRETME
        if (player != null && player.health.currentHP > 0)
        {
            Debug.Log("🔄 Tur bitti. Sıra oyuncuda. Düşmanlar BİR SONRAKİ HAMLELERİNİ planlıyor...");

            // Yeni tura hazırlık: Sersemletmeleri temizle
            stunnedEnemiesThisTurn.Clear();

            // Sırayı oyuncuya ver (Bu sayede HexMovement input almaya başlar)
            isPlayerTurn = true;

            // GÖRSEL GÜNCELLEME: Oyuncunun hareket edeceği kareleri (highlight) tekrar çiz
            player.UpdateHighlights();

            // STRATEJİK GÜNCELLEME: Düşmanlar bir sonraki hamlelerini hesaplayıp okları kilitler
            LockAllEnemyIntents();
        }
    }

    // --- 4. ÇOKLU SAVAŞ & KNOCKBACK (Ortak Metot) ---
    private IEnumerator MultiAttack(List<EnemyAI> targets, bool playerInitiated)
    {
        yield return new WaitForSeconds(COMBAT_START_DELAY);

        int die1 = Random.Range(1, 7);
        int die2 = Random.Range(1, 7);
        int totalDamage = die1 + die2;

        int targetCount = targets.Count;
        int damagePerEnemy = totalDamage / targetCount;

        Debug.Log($"⚔️ SAVAŞ! {targetCount} düşmana karşı toplam {totalDamage} hasar! Düşman başı {damagePerEnemy} hasar.");

        ShowDiceResults(die1, die2);
        yield return new WaitForSeconds(DICE_VISUAL_DELAY);

        // Düşman ölürse objesi silineceği için merkez noktayı önceden kaydediyoruz.
        Vector3Int referenceEnemyPos = targets[0].GetCurrentCellPosition();

        bool anyEnemySurvived = false;
        List<EnemyAI> survivors = new List<EnemyAI>();

        foreach (var enemy in targets)
        {
            if (enemy == null) continue;

            bool enemyWillDie = enemy.health.currentHP <= damagePerEnemy;
            enemy.health.TakeDamage(damagePerEnemy);

            if (enemyWillDie)
            {
                Debug.Log($"💀 Düşman öldü!");
                enemies.Remove(enemy);
            }
            else
            {
                anyEnemySurvived = true;
                survivors.Add(enemy);
            }
        }

        // --- FIRST STRIKE VE HASAR MANTIĞI ---
        if (playerInitiated)
        {
            Debug.Log($"💥 FIRST STRIKE AVANTAJI! Karakterimiz hasar almıyor.");

            // First Strike yiyen ve hayatta kalan tüm düşmanları Sersemlet (Stun)
            foreach (var surv in survivors)
            {
                if (!stunnedEnemiesThisTurn.Contains(surv))
                {
                    stunnedEnemiesThisTurn.Add(surv);
                    Debug.Log($"💫 {surv.gameObject.name} Sersemledi! Bu tur ne yürüyebilecek ne de vurabilecek.");
                }
            }
        }
        else
        {
            // Eğer savaşı düşman başlattıysa ve hayatta kalan varsa karakter hasar alır
            if (anyEnemySurvived)
            {
                Debug.Log($"💥 Düşmanlar saldırdığı için karakterimiz 1 hasar alıyor!");
                player.health.TakeDamage(1);
            }
        }

        if (player == null || player.health.currentHP <= 0)
        {
            HideDiceResults();
            Debug.Log("💀 Oyuncu öldü. Oyun Bitti!");
            yield break;
        }

        // --- HER DURUMDA GERİ TEPME (KNOCKBACK) ---
        Debug.Log("💥 Çarpışma şiddetiyle herkes geri savruluyor!");

        // Hayatta kalan düşmanlar seker
        foreach (var surv in survivors)
        {
            Vector3Int eKb = GetOppositeCell(surv.GetCurrentCellPosition(), player.GetCurrentCellPosition());
            surv.StartKnockbackMovement(eKb);
        }

        // Oyuncu HER HALÜKARDA (düşman ölse bile) ilk hedefin olduğu konumdan zıt yöne seker
        Vector3Int pKb = GetOppositeCell(player.GetCurrentCellPosition(), referenceEnemyPos);
        player.StartKnockbackMovement(pKb);

        // Herkesin sekme hareketinin bitmesini bekle
        yield return new WaitUntil(() =>
        {
            if (player != null && player.IsMoving()) return false;
            foreach (var s in survivors) if (s != null && s.IsMoving()) return false;
            return true;
        });

        HideDiceResults();

        // ZİNCİRLEME REAKSİYON (PINBALL ETKİSİ)
        if (player != null && player.health.currentHP > 0)
        {
            List<EnemyAI> newAdjacents = GetAdjacentEnemies(player.GetCurrentCellPosition());
            if (newAdjacents.Count > 0)
            {
                Debug.Log("⛓️ ZİNCİRLEME REAKSİYON! Karakter çarptığı yeni gruba karşı savaşa giriyor!");
                yield return StartCoroutine(MultiAttack(newAdjacents, playerInitiated));
            }
        }
    }

    // --- YARDIMCI METOTLAR ---

    public bool IsEnemyAtCell(Vector3Int cell)
    {
        foreach (var e in enemies)
        {
            if (e != null && e.health.currentHP > 0 && e.GetCurrentCellPosition() == cell) return true;
        }
        return false;
    }

    private List<EnemyAI> GetAdjacentEnemies(Vector3Int playerCell)
    {
        List<EnemyAI> adjacentList = new List<EnemyAI>();
        foreach (var enemy in enemies)
        {
            if (enemy != null && enemy.health.currentHP > 0)
            {
                if (IsNeighbor(playerCell, enemy.GetCurrentCellPosition()))
                {
                    adjacentList.Add(enemy);
                }
            }
        }
        return adjacentList;
    }

    private Vector3Int GetOppositeCell(Vector3Int centerCell, Vector3Int awayFromCell)
    {
        Vector3Int[] offsets = (centerCell.y % 2 != 0) ? evenOffsets : oddOffsets;
        Vector3Int bestCell = centerCell;
        float maxDist = -1f;

        Vector3 awayWorldPos = groundMap.GetCellCenterWorld(awayFromCell);

        foreach (var off in offsets)
        {
            Vector3Int neighbor = centerCell + off;

            // Harita dışına, oyuncunun veya diğer düşmanların üstüne sekme!
            if (!groundMap.HasTile(neighbor)) continue;
            if (neighbor == player.GetCurrentCellPosition() || IsEnemyAtCell(neighbor)) continue;

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