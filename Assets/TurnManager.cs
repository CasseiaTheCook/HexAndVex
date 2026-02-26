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

    [Header("Dinamik Zar UI Sistemi")]
    public GameObject dieUIPrefab;       // Hazırladığın Zar Prefab'ını buraya at
    public Transform diceUIContainer;    // Horizontal Layout olan paneli buraya at
    public TMP_Text totalDamageText;
    public Sprite[] diceSprites;

    // Sahnede o an var olan görsel zarların listesi
    private List<GameObject> spawnedDiceUI = new List<GameObject>();

    public List<EnemyAI> enemies = new List<EnemyAI>();
    public bool isPlayerTurn = true;

    private List<EnemyAI> stunnedEnemiesThisTurn = new List<EnemyAI>();

    private static readonly Vector3Int[] oddOffsets = { new Vector3Int(+1, 0, 0), new Vector3Int(0, +1, 0), new Vector3Int(-1, +1, 0), new Vector3Int(-1, 0, 0), new Vector3Int(-1, -1, 0), new Vector3Int(0, -1, 0) };
    private static readonly Vector3Int[] evenOffsets = { new Vector3Int(+1, 0, 0), new Vector3Int(+1, +1, 0), new Vector3Int(0, +1, 0), new Vector3Int(-1, 0, 0), new Vector3Int(0, -1, 0), new Vector3Int(+1, -1, 0) };

    private const float COMBAT_START_DELAY = 0.5f;

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);

        if (player == null) player = FindFirstObjectByType<HexMovement>();

        if (groundMap == null)
        {
            GameObject mapObj = GameObject.Find("GroundMap");
            if (mapObj != null) groundMap = mapObj.GetComponent<Tilemap>();
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
        foreach (var perk in RunManager.instance.activePerks) perk.OnSkip();
        RunManager.instance.currentGold += RunManager.instance.skipBonusGold;
        isPlayerTurn = false;
        StartCoroutine(EnemyPhase());
    }

    public void TriggerExplosion(Vector3Int centerCell)
    {
        Vector3Int[] offsets = (centerCell.y % 2 != 0) ? evenOffsets : oddOffsets;
        foreach (var off in offsets)
        {
            Vector3Int neighbor = centerCell + off;
            foreach (var e in enemies)
            {
                if (e != null && e.GetCurrentCellPosition() == neighbor) e.health.TakeDamage(1);
            }
        }
    }

    private IEnumerator HandlePlayerPhase(Vector3Int playerCell)
    {
        List<EnemyAI> adjacentEnemies = GetAdjacentEnemies(player.GetCurrentCellPosition());
        if (adjacentEnemies.Count > 0)
        {
            yield return StartCoroutine(MultiAttack(adjacentEnemies, true));
        }

        if (RunManager.instance.remainingMoves > 0 && player != null && player.health.currentHP > 0 && enemies.Count > 0)
        {
            RunManager.instance.remainingMoves--;
            Debug.Log($"⚡ EKSTRA HAMLE! Kalan hak: {RunManager.instance.remainingMoves}");
            isPlayerTurn = true;
            player.UpdateHighlights();
        }
        else
        {
            if (player != null && player.health.currentHP > 0) StartCoroutine(EnemyPhase());
        }
    }

    private IEnumerator EnemyPhase()
    {
        yield return new WaitForSeconds(0.2f);
        enemies.RemoveAll(e => e == null || e.health.currentHP <= 0);

        foreach (var e in enemies)
        {
            if (e != null && !stunnedEnemiesThisTurn.Contains(e)) e.ExecuteLockedMove();
        }

        yield return new WaitUntil(() =>
        {
            foreach (var e in enemies) if (e != null && e.IsMoving()) return false;
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

    private IEnumerator MultiAttack(List<EnemyAI> targets, bool playerInitiated)
    {
        yield return new WaitForSeconds(0.3f);

        // --- ADIM 1: ZARLARI AT VE GÖSTER ---
        List<int> currentRolls = new List<int>();
        int diceCount = RunManager.instance != null ? RunManager.instance.baseDiceCount : 2;
        for (int i = 0; i < diceCount; i++) currentRolls.Add(Random.Range(1, 7));

        CombatPayload payload = new CombatPayload(currentRolls);

        // Zarları ekrana bas ve Master's Focus reroll işlemlerini bitirene kadar bekle
        yield return StartCoroutine(ShowDiceSequence(currentRolls));

        // --- ADIM 2: ÖNCELİKLİ PERKLER (SWORD DANCE VB.) ---
        // Bazı perkler oyunun temel kuralını (Toplama yerine Çarpma gibi) en başta değiştirmeli
        if (RunManager.instance != null)
        {
            foreach (BasePerk perk in RunManager.instance.activePerks)
            {
                if (perk.GetType().Name == "SwordDancePerk") perk.ModifyCombat(payload);
            }
        }

        // Başlangıç hasarını ekrana "BAM" diye vur
        UpdateTotalDamageDisplay(payload.GetFinalDamage());
        yield return new WaitForSeconds(0.7f);

        // --- ADIM 3: JOKER/PERK SIRALAMASI VE ZAR DEDEKTİFİ (PRIORITY SİSTEMİ) ---
        if (RunManager.instance != null && RunManager.instance.activePerks.Count > 0)
        {
            // İsim arama saçmalığı bitti! Sadece priority numarasına göre küçükten büyüğe diziyoruz.
            List<BasePerk> perksToProcess = new List<BasePerk>(RunManager.instance.activePerks);
            perksToProcess.Sort((a, b) => a.priority.CompareTo(b.priority));

            foreach (BasePerk perk in perksToProcess)
            {
                int beforeTotal = payload.GetFinalDamage();
                perk.ModifyCombat(payload);

                // ZAR DEDEKTİFİ: Vanguard veya Rearguard zarın kendisini mi değiştirdi?
                bool anyDieChanged = false;
                for (int i = 0; i < currentRolls.Count; i++)
                {
                    if (currentRolls[i] != payload.diceRolls[i])
                    {
                        currentRolls[i] = payload.diceRolls[i];
                        AnimateSpecificDie(i, currentRolls[i]); // Zarı BUM yap ve sayısını değiştir
                        anyDieChanged = true;
                        yield return new WaitForSeconds(0.3f);
                    }
                }

                int afterTotal = payload.GetFinalDamage();

                // Eğer hasar arttıysa veya zar değiştiyse Joker kartını parlat ve toplamı güncelle
                if (beforeTotal != afterTotal || anyDieChanged)
                {
                    perk.TriggerVisualPop();
                    UpdateTotalDamageDisplay(afterTotal);
                    yield return new WaitForSeconds(0.6f);
                }
            }
        }

        // --- ADIM 4: KRİTİK VURUŞ ---
        if (Random.value < RunManager.instance.criticalChance)
        {
            payload.isCriticalHit = true;
            UpdateTotalDamageDisplay(payload.GetFinalDamage());
            Debug.Log("🎯 KRİTİK!");
            yield return new WaitForSeconds(0.5f);
        }

        // --- ADIM 5: HASAR UYGULAMA VE ÖLÜM ---
        int finalDamage = payload.GetFinalDamage();
        int damagePerEnemy = finalDamage / targets.Count;
        List<EnemyAI> survivors = new List<EnemyAI>();

        foreach (var enemy in targets)
        {
            if (enemy == null) continue;
            bool dies = enemy.health.currentHP <= damagePerEnemy;
            enemy.health.TakeDamage(damagePerEnemy);

            if (payload.triggerExplosion) TriggerExplosion(enemy.GetCurrentCellPosition());

            if (dies)
            {
                RunManager.instance.currentGold += Random.Range(5, 11) + RunManager.instance.bonusGold;
                foreach (var p in RunManager.instance.activePerks) p.OnEnemyKilled(enemy);
                enemies.Remove(enemy);
            }
            else survivors.Add(enemy);
        }

        yield return new WaitForSeconds(0.2f);
        enemies.RemoveAll(e => e == null || e.health.currentHP <= 0);

        if (enemies.Count <= 0)
        {
            HideDiceResults();
            if (LevelUpManager.instance != null) LevelUpManager.instance.ShowLevelUpScreen();
            yield break;
        }

        // --- ADIM 6: KNOCKBACK ---
        if (survivors.Count > 0)
        {
            foreach (var s in survivors)
            {
                Vector3Int targetCell = GetOppositeCell(s.GetCurrentCellPosition(), player.GetCurrentCellPosition());
                s.StartKnockbackMovement(targetCell);
            }
            Vector3Int playerTarget = GetOppositeCell(player.GetCurrentCellPosition(), targets[0].GetCurrentCellPosition());
            player.StartKnockbackMovement(playerTarget);

            yield return new WaitUntil(() => {
                if (player != null && player.IsMoving()) return false;
                foreach (var s in survivors) if (s != null && s.IsMoving()) return false;
                return true;
            });
        }

        // --- ADIM 7: SIRA KONTROLÜ VE ZİNCİRLEME ---
        if (playerInitiated)
        {
            foreach (var s in survivors) if (!stunnedEnemiesThisTurn.Contains(s)) stunnedEnemiesThisTurn.Add(s);
        }
        else if (survivors.Count > 0)
        {
            bool dodged = RunManager.instance.hasHolyAegis || Random.value < RunManager.instance.dodgeChance;
            if (RunManager.instance.hasHolyAegis) RunManager.instance.hasHolyAegis = false;
            if (!dodged) player.health.TakeDamage(1);
        }

        yield return new WaitForSeconds(0.3f);
        HideDiceResults();

        List<EnemyAI> nextTargets = GetAdjacentEnemies(player.GetCurrentCellPosition());
        if (nextTargets.Count > 0) yield return StartCoroutine(MultiAttack(nextTargets, playerInitiated));
        else
        {
            isPlayerTurn = true;
            player.UpdateHighlights();
            LockAllEnemyIntents();
        }
    }


    // ==========================================
    // --- GÖRSEL ZAR SİSTEMİ (SONSUZ ZAR DESTEĞİ) ---
    // ==========================================
    private IEnumerator ShowDiceSequence(List<int> rolls)
    {
        // 1. Temizlik ve Hazırlık
        foreach (var die in spawnedDiceUI) Destroy(die);
        spawnedDiceUI.Clear();

        if (totalDamageText != null)
        {
            totalDamageText.gameObject.SetActive(true);
            totalDamageText.text = "0";
        }

        List<Image> dieImages = new List<Image>();
        List<Animator> dieAnimators = new List<Animator>();
        List<TMP_Text> dieTexts = new List<TMP_Text>();

        for (int i = 0; i < rolls.Count; i++)
        {
            GameObject newDie = Instantiate(dieUIPrefab, diceUIContainer);
            spawnedDiceUI.Add(newDie);
            dieImages.Add(newDie.GetComponent<Image>());
            dieAnimators.Add(newDie.GetComponent<Animator>());
            dieTexts.Add(newDie.GetComponentInChildren<TMP_Text>());
        }

        // İlk fırlatma animasyonu
        yield return new WaitForSeconds(0.4f);

        // --- MASTER'S FOCUS: REROLL DÖNGÜSÜ ---
        bool hasFocus = RunManager.instance.activePerks.Exists(p => p is MastersFocusPerk);
        if (hasFocus)
        {
            bool stillLow = true;
            int safety = 0;
            while (stillLow && safety < 10)
            {
                stillLow = false;
                List<int> toReroll = new List<int>();
                for (int i = 0; i < rolls.Count; i++)
                {
                    if (rolls[i] < 3) { stillLow = true; toReroll.Add(i); }
                }

                if (stillLow)
                {
                    foreach (int idx in toReroll)
                    {
                        if (dieAnimators[idx] != null) dieAnimators[idx].enabled = true;
                        dieTexts[idx].text = "!";
                    }
                    yield return new WaitForSeconds(0.6f);
                    foreach (int idx in toReroll)
                    {
                        rolls[idx] = Random.Range(1, 7);
                        if (dieAnimators[idx] != null) dieAnimators[idx].enabled = false;
                        dieImages[idx].sprite = diceSprites[rolls[idx] - 1];
                        dieTexts[idx].text = rolls[idx].ToString();
                        StartCoroutine(TextPopAnimation(dieTexts[idx]));
                    }
                    yield return new WaitForSeconds(0.4f);
                }
                safety++;
            }
        }


        // --- SON GÖSTERİM: TÜM ZARLARI SABİTLE ---
        for (int i = 0; i < rolls.Count; i++)
        {
            if (dieAnimators[i] != null) dieAnimators[i].enabled = false;
            dieImages[i].sprite = diceSprites[rolls[i] - 1];
            dieTexts[i].text = rolls[i].ToString();
            StartCoroutine(TextPopAnimation(dieTexts[i]));
        }

        yield return new WaitForSeconds(0.2f);
    }

    private IEnumerator TextPopAnimation(TMP_Text textElement)
    {
        if (textElement == null) yield break;
        Transform t = textElement.transform;
        Vector3 startScale = new Vector3(3f, 3f, 3f);
        Vector3 endScale = Vector3.one;
        float duration = 0.15f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float tParam = elapsed / duration;
            tParam = 1f - (1f - tParam) * (1f - tParam);
            t.localScale = Vector3.Lerp(startScale, endScale, tParam);
            elapsed += Time.deltaTime;
            yield return null;
        }
        t.localScale = endScale;
    }
    // Bu fonksiyon sayı her değiştiğinde (perk tetiklendiğinde) çağrılır
    public void UpdateTotalDamageDisplay(int val)
    {
        if (totalDamageText == null) return;

        // Obje kapalıysa açıyoruz
        totalDamageText.gameObject.SetActive(true);

        // Sayıyı güncelliyoruz
        totalDamageText.text = val.ToString();

        // HAŞİN EFECT: Önceki animasyonu durdurup yenisini başlatıyoruz (BAM etkisi)
        StopCoroutine("TextPopAnimation");
        StartCoroutine(TextPopAnimation(totalDamageText));
    }

    public void HideDiceResults()
    {
        // Savaş bitince veya panel kapatılınca tüm üretilen zarları yok et
        foreach (var die in spawnedDiceUI) Destroy(die);
        spawnedDiceUI.Clear();

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
                if (IsNeighbor(playerCell, enemy.GetCurrentCellPosition())) adjacentList.Add(enemy);
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
        foreach (var off in offsets) if (cell1 + off == cell2) return true;
        return false;
    }
    // İstenilen sıradaki zarı "BUM" diye sarsan fonksiyon
    // İstenilen sıradaki zarı BUM yapıp yeni değerini ekrana basar
    public void AnimateSpecificDie(int index, int newValue)
    {
        if (index >= 0 && index < spawnedDiceUI.Count)
        {
            Transform dieTransform = spawnedDiceUI[index].transform;
            TMP_Text dieText = spawnedDiceUI[index].GetComponentInChildren<TMP_Text>();
            Image dieImage = spawnedDiceUI[index].GetComponent<Image>();

            // Sayıyı ve Görseli Güncelle
            if (dieText != null) dieText.text = newValue.ToString();

            // Zar 6'dan büyükse max sprite'ı göster (hata vermesin diye Clamp yapıyoruz)
            if (dieImage != null && diceSprites != null)
            {
                int spriteIndex = Mathf.Clamp(newValue - 1, 0, diceSprites.Length - 1);
                dieImage.sprite = diceSprites[spriteIndex];
            }

            StartCoroutine(DiePopAnimation(dieTransform));
        }
    }

    private IEnumerator DiePopAnimation(Transform t)
    {
        Vector3 startScale = Vector3.one * 1.6f; // Zar 1.6 kat büyüyüp ekrana vurur
        float dur = 0.2f;
        float elapsed = 0f;

        while (elapsed < dur)
        {
            t.localScale = Vector3.Lerp(startScale, Vector3.one, elapsed / dur);
            elapsed += Time.deltaTime;
            yield return null;
        }
        t.localScale = Vector3.one;
    }
}