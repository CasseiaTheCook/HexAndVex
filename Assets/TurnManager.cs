using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.Tilemaps;
using TMPro;
using UnityEngine.UI;

public class TurnManager : MonoBehaviour
{
    public static TurnManager instance;

    public HexMovement player;
    public Tilemap groundMap;

    public TMP_Text dietext1;
    public TMP_Text dietext2;

    public Image dieImage1;
    public Image dieImage2;
    public TMP_Text totalDamageText;
    public Sprite[] diceSprites;

    public List<EnemyAI> enemies = new List<EnemyAI>();
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

        if (dieImage1 == null)
        {
            GameObject img1 = GameObject.Find("DieImage1");
            if (img1 != null) dieImage1 = img1.GetComponent<Image>();
        }

        if (dieImage2 == null)
        {
            GameObject img2 = GameObject.Find("DieImage2");
            if (img2 != null) dieImage2 = img2.GetComponent<Image>();
        }

        if (totalDamageText == null)
        {
            GameObject tdt = GameObject.Find("TotalDamageText");
            if (tdt != null) totalDamageText = tdt.GetComponent<TMP_Text>();
        }
    }

    void Start()
    {
        HideDiceResults();
        Invoke("LockAllEnemyIntents", 0.5f);
    }

    public void RegisterEnemy(EnemyAI enemy)
    {
        if (!enemies.Contains(enemy)) enemies.Add(enemy);
    }

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

    public void PlayerFinishedMove(Vector3Int playerCell)
    {
        StartCoroutine(HandlePlayerPhase(playerCell));
    }

    public void SkipTurn()
    {
        if (!isPlayerTurn) return;

        foreach (var perk in RunManager.instance.activePerks)
        {
            perk.OnSkip(); // Perkleri tetikle
        }

        // Mercenary's Rest altını ekle
        RunManager.instance.currentGold += RunManager.instance.skipBonusGold;

        isPlayerTurn = false;
        StartCoroutine(EnemyPhase()); // Sırayı düşmana ver
    }


    public void TriggerExplosion(Vector3Int centerCell)
    {
        // Hex komşularını al
        Vector3Int[] offsets = (centerCell.y % 2 != 0) ? evenOffsets : oddOffsets;
        foreach (var off in offsets)
        {
            Vector3Int neighbor = centerCell + off;
            foreach (var e in enemies)
            {
                if (e != null && e.GetCurrentCellPosition() == neighbor)
                {
                    e.health.TakeDamage(1); // Komşulara 1 hasar
                }
            }
        }
    }

    // TurnManager içindeki HandlePlayerPhase fonksiyonunu bul ve değiştir:
    private IEnumerator HandlePlayerPhase(Vector3Int playerCell)
    {
        List<EnemyAI> adjacentEnemies = GetAdjacentEnemies(player.GetCurrentCellPosition());

        if (adjacentEnemies.Count > 0)
        {
            yield return StartCoroutine(MultiAttack(adjacentEnemies, true));
        }

        // --- YENİ EKSTRA HAMLE MANTIĞI ---
        if (RunManager.instance.remainingMoves > 0 && player != null && player.health.currentHP > 0 && enemies.Count > 0)
        {
            RunManager.instance.remainingMoves--; // Bir hakkını kullan
            Debug.Log($"⚡ EKSTRA HAMLE! Kalan hak: {RunManager.instance.remainingMoves}");

            // Sırayı düşmana VERME, oyuncuya geri bırak
            isPlayerTurn = true;
            player.UpdateHighlights();
        }
        else
        {
            // Hakkı bittiyse düşman fazına geç
            if (player != null && player.health.currentHP > 0)
            {
                StartCoroutine(EnemyPhase());
            }
        }
    }

    private IEnumerator EnemyPhase()
    {
        yield return new WaitForSeconds(0.2f);
        enemies.RemoveAll(e => e == null || e.health.currentHP <= 0);

        foreach (var e in enemies)
        {
            if (e != null && !stunnedEnemiesThisTurn.Contains(e))
            {
                e.ExecuteLockedMove();
            }
        }

        yield return new WaitUntil(() =>
        {
            foreach (var e in enemies)
            {
                if (e != null && e.IsMoving()) return false;
            }
            return true;
        });

        yield return new WaitForSeconds(0.2f);

        List<EnemyAI> adjacentEnemies = GetAdjacentEnemies(player.GetCurrentCellPosition());
        adjacentEnemies.RemoveAll(e => stunnedEnemiesThisTurn.Contains(e));

        if (adjacentEnemies.Count > 0)
        {
            Debug.Log("🛡️ DÜŞMAN SALDIRISI! Düşmanlar oyuncuya saldırdı.");
            yield return StartCoroutine(MultiAttack(adjacentEnemies, false));
        }

        if (player != null && player.health.currentHP > 0)
        {
            Debug.Log("🔄 Tur bitti. Sıra oyuncuda.");
            stunnedEnemiesThisTurn.Clear();
            isPlayerTurn = true;
            player.UpdateHighlights();
            LockAllEnemyIntents();
        }
    }

    // --- 4. ÇOKLU SAVAŞ & KNOCKBACK (ROGUELIKE SİSTEMİNE ENTEGRE) ---
    private IEnumerator MultiAttack(List<EnemyAI> targets, bool playerInitiated)
    {
        yield return new WaitForSeconds(COMBAT_START_DELAY);

        // 1. ZARLARI AT (RunManager'daki zar sayısına göre dinamik)
        List<int> currentRolls = new List<int>();
        int diceCount = RunManager.instance != null ? RunManager.instance.baseDiceCount : 2;

        for (int i = 0; i < diceCount; i++)
        {
            currentRolls.Add(Random.Range(1, 7));
        }

        // 2. HASAR PAKETİNİ (PAYLOAD) OLUŞTUR
        CombatPayload finalCombatData = new CombatPayload(currentRolls);

        // 3. KRİTİK VURUŞ KONTROLÜ
        if (RunManager.instance != null && Random.value < RunManager.instance.criticalChance)
        {
            finalCombatData.isCriticalHit = true;
            Debug.Log("🎯 KRİTİK VURUŞ!");
        }

        // 4. PERK/JOKER MODİFİYESİ
        if (RunManager.instance != null)
        {
            foreach (BasePerk perk in RunManager.instance.activePerks)
            {
                perk.ModifyCombat(finalCombatData);
            }
        }

        // 5. NİHAİ HASARI HESAPLA
        int totalDamage = finalCombatData.GetFinalDamage();
        int targetCount = targets.Count;
        int damagePerEnemy = totalDamage / targetCount;

        // 6. GÖRSEL ZAR ANİMASYONU
        int displayDie1 = currentRolls.Count > 0 ? currentRolls[0] : 0;
        int displayDie2 = currentRolls.Count > 1 ? currentRolls[1] : 0;
        yield return StartCoroutine(ShowDiceSequence(displayDie1, displayDie2, totalDamage));

        // 7. HASAR UYGULAMA VE PERK TETİKLEME
        Vector3Int referenceEnemyPos = targets[0].GetCurrentCellPosition();
        List<EnemyAI> survivors = new List<EnemyAI>();

        foreach (var enemy in targets)
        {
            if (enemy == null) continue;

            // Düşman bu darbe ile ölecek mi?
            bool enemyWillDie = enemy.health.currentHP <= damagePerEnemy;

            // Hasarı ver
            enemy.health.TakeDamage(damagePerEnemy);

            // --- PATLAMA PERKİ (Blast Impact) ---
            if (finalCombatData.triggerExplosion)
            {
                TriggerExplosion(enemy.GetCurrentCellPosition());
            }

            if (enemyWillDie)
            {
                // --- ALTIN KAZANMA (Bounty Hunter) ---
                // Rastgele 5-10 arası altın + Bounty Hunter bonusu
                int gainedGold = Random.Range(5, 11) + RunManager.instance.bonusGold;
                RunManager.instance.currentGold += gainedGold;
                Debug.Log($"💰 Düşman öldü! {gainedGold} altın kazanıldı. Toplam: {RunManager.instance.currentGold}");

                // Diğer perklerin "OnEnemyKilled" olayını tetikle
                foreach (var perk in RunManager.instance.activePerks)
                {
                    perk.OnEnemyKilled(enemy);
                }

                // Düşman öldüğü için genel listeden siliyoruz
                enemies.Remove(enemy);
            }
            else
            {
                // Hayatta kaldıysa survivors listesine ekle (Knockback için)
                survivors.Add(enemy);
            }
        }

        // 8. TEMİZLİK VE ZAFER KONTROLÜ
        yield return new WaitForSeconds(0.1f);
        enemies.RemoveAll(e => e == null || e.health.currentHP <= 0);

        if (enemies.Count <= 0)
        {
            Debug.Log("🏆 ODA TEMİZLENDİ!");
            HideDiceResults();
            if (LevelUpManager.instance != null) LevelUpManager.instance.ShowLevelUpScreen();
            yield break;
        }

        // --- SAVAŞ DEVAM EDİYORSA ALT SATIRLAR ÇALIŞIR ---

        if (playerInitiated)
        {
            foreach (var surv in survivors)
            {
                if (!stunnedEnemiesThisTurn.Contains(surv))
                {
                    stunnedEnemiesThisTurn.Add(surv);
                }
            }
        }
        else
        {
            if (survivors.Count > 0) // Oyuncu hasar almadan önce koruma perklerini kontrol et
            {
                bool avoidedDamage = false;

                if (RunManager.instance.hasHolyAegis)
                {
                    RunManager.instance.hasHolyAegis = false; // Kalkanı harca
                    avoidedDamage = true;
                    Debug.Log("🛡️ Holy Aegis hasarı engelledi!");
                }
                else if (Random.value < RunManager.instance.dodgeChance)
                {
                    avoidedDamage = true;
                    Debug.Log("🛡️ Knight's Plating hasarı savuşturdu!");
                }



                if (RunManager.instance.hasHolyAegis)
                {
                    RunManager.instance.hasHolyAegis = false; // Kalkanı harca
                    avoidedDamage = true;
                    Debug.Log("🛡️ Holy Aegis hasarı engelledi!");
                }
                else if (Random.value < RunManager.instance.dodgeChance)
                {
                    avoidedDamage = true;
                    Debug.Log("🛡️ Knight's Plating hasarı savuşturdu!");
                }

                if (!avoidedDamage)
                {
                    player.health.TakeDamage(1);
                }
            }
        }

        if (player == null || player.health.currentHP <= 0)
        {
            HideDiceResults();
            yield break;
        }

        // KNOCKBACK SİSTEMİ
        foreach (var surv in survivors)
        {
            Vector3Int eKb = GetOppositeCell(surv.GetCurrentCellPosition(), player.GetCurrentCellPosition());
            surv.StartKnockbackMovement(eKb);
        }

        Vector3Int pKb = GetOppositeCell(player.GetCurrentCellPosition(), referenceEnemyPos);
        player.StartKnockbackMovement(pKb);

        yield return new WaitUntil(() =>
        {
            if (player != null && player.IsMoving()) return false;
            foreach (var s in survivors) if (s != null && s.IsMoving()) return false;
            return true;
        });

        HideDiceResults();

        // ZİNCİRLEME REAKSİYON
        List<EnemyAI> newAdjacents = GetAdjacentEnemies(player.GetCurrentCellPosition());
        if (newAdjacents.Count > 0)
        {
            yield return StartCoroutine(MultiAttack(newAdjacents, playerInitiated));
        }
        else
        {
            // Eğer zincirleme reaksiyon da yoksa sırayı oyuncuya ver
            isPlayerTurn = true;
            player.UpdateHighlights();
            LockAllEnemyIntents();
        }
    }


    // ==========================================
    // --- GÖRSEL ZAR SİSTEMİ (BALATRO EFEKTİ) ---
    // ==========================================
    private IEnumerator ShowDiceSequence(int die1, int die2, int total)
    {
        // 1. AŞAMA: UI Objelerini aktif et, metinleri temizle ve Scale'i sıfırla (güvenlik)
        if (dietext1 != null) { dietext1.gameObject.SetActive(true); dietext1.text = ""; dietext1.transform.localScale = Vector3.one; }
        if (dietext2 != null) { dietext2.gameObject.SetActive(true); dietext2.text = ""; dietext2.transform.localScale = Vector3.one; }
        if (totalDamageText != null) { totalDamageText.gameObject.SetActive(true); totalDamageText.text = ""; totalDamageText.transform.localScale = Vector3.one; }

        if (dieImage1 != null) dieImage1.gameObject.SetActive(true);
        if (dieImage2 != null) dieImage2.gameObject.SetActive(true);

        // 2. AŞAMA: KENDİ ÖZEL ZAR ANİMASYONUNU ÇALIŞTIR
        Animator anim1 = null;
        Animator anim2 = null;

        if (dieImage1 != null) { anim1 = dieImage1.GetComponent<Animator>(); if (anim1 != null) anim1.enabled = true; }
        if (dieImage2 != null) { anim2 = dieImage2.GetComponent<Animator>(); if (anim2 != null) anim2.enabled = true; }

        float rollDuration = 0.5f;
        yield return new WaitForSeconds(rollDuration);

        // 3. AŞAMA: ZARLAR DURUR, SONUÇ SPRITE'I YÜKLENİR
        if (anim1 != null) anim1.enabled = false;
        if (anim2 != null) anim2.enabled = false;

        if (dieImage1 != null && diceSprites != null && die1 > 0 && die1 <= diceSprites.Length)
            dieImage1.sprite = diceSprites[die1 - 1];

        if (dieImage2 != null && diceSprites != null && die2 > 0 && die2 <= diceSprites.Length)
            dieImage2.sprite = diceSprites[die2 - 1];

        // BEKLEME YOK! Direkt 4. AŞAMAYA (Sayılara) geçiyoruz.

        // 4. AŞAMA: ZAR NUMARALARI PATLAYARAK EKRANDA BELİRİR
        if (dietext1 != null)
        {
            dietext1.text = die1.ToString();
            StartCoroutine(TextPopAnimation(dietext1));
        }
        if (dietext2 != null)
        {
            dietext2.text = die2.ToString();
            StartCoroutine(TextPopAnimation(dietext2));
        }

        // *** KISA BİR BEKLEME *** (Sayılar otursun ve gerilim yaratsın)
        yield return new WaitForSeconds(0.6f);

        // 5. AŞAMA: TOPLAM HASAR GÖSTERİLİR (YİNE PATLAYARAK)
        if (totalDamageText != null)
        {
            totalDamageText.text = total.ToString();
            StartCoroutine(TextPopAnimation(totalDamageText));
        }

        // *** SON BEKLEME *** (Hasar vurulmadan önce)
        yield return new WaitForSeconds(1f);
    }

    // "BALATRO" TARZI POP/SLAM EFEKTİ İÇİN YARDIMCI COROUTINE
    private IEnumerator TextPopAnimation(TMP_Text textElement)
    {
        if (textElement == null) yield break;

        Transform t = textElement.transform;

        // Başlangıç boyutu 3 katı büyük!
        Vector3 startScale = new Vector3(3f, 3f, 3f);
        Vector3 endScale = Vector3.one; // Normal boyut (1x)

        float duration = 0.15f; // Çok hızlı bir çarpma hissi için kısa süre
        float elapsed = 0f;

        while (elapsed < duration)
        {
            // Basit bir ease-out (hızlı başlar, yumuşak biter) hesabı
            float tParam = elapsed / duration;
            tParam = 1f - (1f - tParam) * (1f - tParam);

            t.localScale = Vector3.Lerp(startScale, endScale, tParam);

            elapsed += Time.deltaTime; // Her frame (kare) süreyi artır
            yield return null;
        }

        // Döngü bitince her ihtimale karşı boyutu tam 1'e sabitle
        t.localScale = endScale;
    }


    public void HideDiceResults()
    {
        if (dietext1 != null) dietext1.gameObject.SetActive(false);
        if (dietext2 != null) dietext2.gameObject.SetActive(false);
        if (dieImage1 != null) dieImage1.gameObject.SetActive(false);
        if (dieImage2 != null) dieImage2.gameObject.SetActive(false);
        if (totalDamageText != null) totalDamageText.gameObject.SetActive(false);
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
}