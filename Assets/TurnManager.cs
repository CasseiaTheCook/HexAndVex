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
        yield return new WaitForSeconds(COMBAT_START_DELAY);

        List<int> currentRolls = new List<int>();
        int diceCount = RunManager.instance != null ? RunManager.instance.baseDiceCount : 2;

        for (int i = 0; i < diceCount; i++)
        {
            currentRolls.Add(Random.Range(1, 7));
        }

        CombatPayload finalCombatData = new CombatPayload(currentRolls);

        if (RunManager.instance != null && Random.value < RunManager.instance.criticalChance)
        {
            finalCombatData.isCriticalHit = true;
        }

        if (RunManager.instance != null)
        {
            foreach (BasePerk perk in RunManager.instance.activePerks) perk.ModifyCombat(finalCombatData);
        }

        int totalDamage = finalCombatData.GetFinalDamage();
        int targetCount = targets.Count;
        int damagePerEnemy = totalDamage / targetCount;

        // --- YENİ ZAR ÇAĞRISI (Tüm listeyi yolluyoruz) ---
        yield return StartCoroutine(ShowDiceSequence(currentRolls, totalDamage));

        Vector3Int referenceEnemyPos = targets[0].GetCurrentCellPosition();
        List<EnemyAI> survivors = new List<EnemyAI>();

        foreach (var enemy in targets)
        {
            if (enemy == null) continue;

            bool enemyWillDie = enemy.health.currentHP <= damagePerEnemy;
            enemy.health.TakeDamage(damagePerEnemy);

            if (finalCombatData.triggerExplosion) TriggerExplosion(enemy.GetCurrentCellPosition());

            if (enemyWillDie)
            {
                int gainedGold = Random.Range(5, 11) + RunManager.instance.bonusGold;
                RunManager.instance.currentGold += gainedGold;
                foreach (var perk in RunManager.instance.activePerks) perk.OnEnemyKilled(enemy);
                enemies.Remove(enemy);
            }
            else
            {
                survivors.Add(enemy);
            }
        }

        yield return new WaitForSeconds(0.1f);
        enemies.RemoveAll(e => e == null || e.health.currentHP <= 0);

        if (enemies.Count <= 0)
        {
            HideDiceResults();
            if (LevelUpManager.instance != null) LevelUpManager.instance.ShowLevelUpScreen();
            yield break;
        }

        if (playerInitiated)
        {
            foreach (var surv in survivors)
            {
                if (!stunnedEnemiesThisTurn.Contains(surv)) stunnedEnemiesThisTurn.Add(surv);
            }
        }
        else
        {
            if (survivors.Count > 0)
            {
                bool avoidedDamage = false;

                if (RunManager.instance.hasHolyAegis)
                {
                    RunManager.instance.hasHolyAegis = false;
                    avoidedDamage = true;
                }
                else if (Random.value < RunManager.instance.dodgeChance)
                {
                    avoidedDamage = true;
                }

                if (!avoidedDamage) player.health.TakeDamage(1);
            }
        }

        if (player == null || player.health.currentHP <= 0)
        {
            HideDiceResults();
            yield break;
        }

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

        List<EnemyAI> newAdjacents = GetAdjacentEnemies(player.GetCurrentCellPosition());
        if (newAdjacents.Count > 0)
        {
            yield return StartCoroutine(MultiAttack(newAdjacents, playerInitiated));
        }
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
    private IEnumerator ShowDiceSequence(List<int> rolls, int total)
    {
        // 1. Temizlik
        foreach (var die in spawnedDiceUI) Destroy(die);
        spawnedDiceUI.Clear();

        // --- KRİTİK DÜZELTME: Obje gizliyse açıyoruz ---
        if (totalDamageText != null)
        {
            totalDamageText.gameObject.SetActive(true); // Gizliyse görünür yap
            totalDamageText.text = "";
            totalDamageText.transform.localScale = Vector3.one;
        }

        List<Image> dieImages = new List<Image>();
        List<Animator> dieAnimators = new List<Animator>();
        List<TMP_Text> dieTexts = new List<TMP_Text>();

        // Zarları oluştur (Layout Group sayesinde yan yana dizilecekler)
        for (int i = 0; i < rolls.Count; i++)
        {
            GameObject newDie = Instantiate(dieUIPrefab, diceUIContainer);
            spawnedDiceUI.Add(newDie);
            dieImages.Add(newDie.GetComponent<Image>());
            dieAnimators.Add(newDie.GetComponent<Animator>());
            dieTexts.Add(newDie.GetComponentInChildren<TMP_Text>());
        }

        // --- İLK ATIM ---
        yield return new WaitForSeconds(0.4f);
        for (int i = 0; i < rolls.Count; i++)
        {
            if (dieAnimators[i] != null) dieAnimators[i].enabled = false;
            dieImages[i].sprite = diceSprites[rolls[i] - 1];
            dieTexts[i].text = rolls[i].ToString();
            StartCoroutine(TextPopAnimation(dieTexts[i]));
        }

        // --- MASTER'S FOCUS REROLL DÖNGÜSÜ ---
        bool hasMastersFocus = RunManager.instance.activePerks.Exists(p => p is MastersFocusPerk);
        if (hasMastersFocus)
        {
            bool stillHaveLowDice = true;
            int safetyCounter = 0;
            while (stillHaveLowDice && safetyCounter < 10)
            {
                stillHaveLowDice = false;
                List<int> diceToReroll = new List<int>();
                for (int i = 0; i < rolls.Count; i++)
                {
                    if (rolls[i] < 3)
                    {
                        stillHaveLowDice = true;
                        diceToReroll.Add(i);
                        if (dieAnimators[i] != null) dieAnimators[i].enabled = true;
                        dieTexts[i].text = "!";
                    }
                }

                if (stillHaveLowDice)
                {
                    yield return new WaitForSeconds(0.6f);
                    foreach (int index in diceToReroll)
                    {
                        int newRoll = Random.Range(1, 7);
                        rolls[index] = newRoll;
                        if (dieAnimators[index] != null) dieAnimators[index].enabled = false;
                        dieImages[index].sprite = diceSprites[newRoll - 1];
                        dieTexts[index].text = newRoll.ToString();
                        StartCoroutine(TextPopAnimation(dieTexts[index]));
                    }
                    var perk = RunManager.instance.activePerks.Find(p => p is MastersFocusPerk);
                    if (perk != null) perk.TriggerVisualPop();
                    yield return new WaitForSeconds(0.4f);
                }
                safetyCounter++;
            }
        }

        // --- 2. KRİTİK DÜZELTME: REROLL SONRASI TOPLAMI GÜNCELLE ---
        int finalTotal = 0;
        foreach (int r in rolls) finalTotal += r;

        // Eğer RunManager'da damage çarpanı falan varsa (Sword Dance vb.) 
        // total değerini ona göre burada son kez manipüle edebilirsin.
        // Şimdilik MultiAttack'tan gelen total'i kullanıyoruz ama reroll olduysa finalTotal yazalım.

        yield return new WaitForSeconds(0.3f);
        if (totalDamageText != null)
        {
            totalDamageText.text = finalTotal.ToString(); // Reroll yapılmış güncel toplam
            StartCoroutine(TextPopAnimation(totalDamageText));
        }

        yield return new WaitForSeconds(1f);
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
}