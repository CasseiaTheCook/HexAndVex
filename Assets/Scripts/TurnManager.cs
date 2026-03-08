using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.Tilemaps;
using TMPro;
using UnityEngine.UI;

public class TurnManager : MonoBehaviour
{
    public static TurnManager instance;

    [Header("Düşman Uyarı (Warning) Karosu")]
    public Tilemap warningMap;
    public UnityEngine.Tilemaps.TileBase warningTile;

    [Header("Efektler")]
    public GameObject explosionPrefab; 
    public GameObject dodgeEffectPrefab; 

    public HexMovement player;
    public Tilemap groundMap;

    [Header("Dinamik Zar UI Sistemi")]
    public GameObject dieUIPrefab;
    public Transform diceUIContainer;
    public TMP_Text totalDamageText;
    public Sprite[] diceSprites;
    public GameObject criticalText;

    [Header("Coin UI")]
    public TMP_Text coinText;

    private List<GameObject> spawnedDiceUI = new List<GameObject>();
    public List<EnemyAI> enemies = new List<EnemyAI>();
    public bool isPlayerTurn = true;
    
    public bool hasAttackedThisTurn = false; 

    private static readonly Vector3Int[] oddOffsets = { new Vector3Int(+1, 0, 0), new Vector3Int(0, +1, 0), new Vector3Int(-1, +1, 0), new Vector3Int(-1, 0, 0), new Vector3Int(-1, -1, 0), new Vector3Int(0, -1, 0) };
    private static readonly Vector3Int[] evenOffsets = { new Vector3Int(+1, 0, 0), new Vector3Int(+1, +1, 0), new Vector3Int(0, +1, 0), new Vector3Int(-1, 0, 0), new Vector3Int(0, -1, 0), new Vector3Int(+1, -1, 0) };

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);

        if (player == null) player = FindFirstObjectByType<HexMovement>();

        if (groundMap == null)
        {
            GameObject mapObj = GameObject.Find("GroundMap");
            if (groundMap == null && mapObj != null) groundMap = mapObj.GetComponent<Tilemap>();
        }

        if (totalDamageText == null)
        {
            GameObject tdt = GameObject.Find("TotalDamageText");
            if (totalDamageText == null && tdt != null) totalDamageText = tdt.GetComponent<TMP_Text>();
        }
    }

    void Start()
    {
        HideDiceResults();
        UpdateCoinUI();
        Invoke("StartPlayerTurn", 0.5f);
    }

    public void StartPlayerTurn()
    {
        if (player == null || player.health.currentHP <= 0) return;

        isPlayerTurn = true;
        hasAttackedThisTurn = false; 

        if (RunManager.instance != null)
        {
            RunManager.instance.remainingMoves = RunManager.instance.extraMovesPerTurn;
        }

        player.UpdateHighlights();
        LockAllEnemyIntents();
    }

    public void ClearWarningMap()
    {
        GameObject warningMapObj = GameObject.Find("WarningMap");
        if (warningMapObj != null)
        {
            warningMapObj.GetComponent<Tilemap>().ClearAllTiles();
        }
    }

    public void UpdateCoinUI()
    {
        if (coinText != null && RunManager.instance != null)
            coinText.text = "Coins: " + RunManager.instance.currentGold;
        if (Shopmanager.instance != null)
            Shopmanager.instance.RefreshAffordability();
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
                bool isStunned = e.skipTurns > 0;
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
        UpdateCoinUI();
        
        isPlayerTurn = false;
        if (RunManager.instance != null) RunManager.instance.remainingMoves = 0; 
        
        StartCoroutine(EnemyPhase());
    }

    public void TriggerExplosion(Vector3Int centerCell)
    {
        Vector3 spawnPos = groundMap.GetCellCenterWorld(centerCell);
        spawnPos.z = 0;
        StartCoroutine(AnimateExplosionFX(spawnPos));

        Vector3Int[] offsets = (centerCell.y % 2 != 0) ? evenOffsets : oddOffsets;
        foreach (var off in offsets)
        {
            Vector3Int neighbor = centerCell + off;
            foreach (var e in enemies)
            {
                if (e != null && e.health.currentHP > 0 && e.GetCurrentCellPosition() == neighbor)
                {
                    int explosionDamage = Mathf.Max(1, e.health.maxHP / 2);
                    e.health.TakeDamage(explosionDamage);
                }
            }
        }
    }

    private IEnumerator AnimateExplosionFX(Vector3 pos)
    {
        if (explosionPrefab == null) yield break;

        GameObject fx = Instantiate(explosionPrefab, pos, Quaternion.identity);
        SpriteRenderer[] renderers = fx.GetComponentsInChildren<SpriteRenderer>();

        Vector3 startScale = Vector3.one * 0.5f; 
        Vector3 endScale = Vector3.one * 2f;    

        float duration = 0.15f; 
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            fx.transform.localScale = Vector3.Lerp(startScale, endScale, t);

            foreach (var sr in renderers)
            {
                Color c = sr.color;
                c.a = Mathf.Lerp(0.8f, 0f, t); 
                sr.color = c;
            }

            yield return null;
        }
        Destroy(fx);
    }

    private IEnumerator HandlePlayerPhase(Vector3Int playerCell)
    {
        if (LevelGenerator.instance.hazardCells.Contains(playerCell))
        {
            yield return new WaitForSeconds(0.15f);
            StartCoroutine(FlashHazardTileCoroutine(playerCell));

            if (RunManager.instance.hasHolyAegis)
            {
                foreach (var perk in RunManager.instance.activePerks)
                {
                    if (perk is HolyAegisPerk aegis) { aegis.BreakShield(); break; }
                }
                RunManager.instance.hasHolyAegis = false;
            }
            else
            {
                player.health.TakeDamage(1);
            }

            yield return new WaitForSeconds(0.15f);

            Vector3Int bounceCell = GetSafeNeighbor(playerCell);
            player.StartKnockbackMovement(bounceCell);
            yield return new WaitUntil(() => !player.IsMoving());

            if (RunManager.instance.remainingMoves > 0 && player != null && player.health.currentHP > 0)
            {
                RunManager.instance.remainingMoves--;
                isPlayerTurn = true;
                player.UpdateHighlights();
                
                // ==========================================
                // YENİ: OKLARI GERİ GETİR (Tuzağa basıp sekse bile oklar gelsin)
                // ==========================================
                ShowAllEnemyIntents(); 
            }
            else if (player != null && player.health.currentHP > 0)
            {
                StartCoroutine(EnemyPhase());
            }
            yield break;
        }

        List<EnemyAI> adjacentEnemies = GetAdjacentEnemies(player.GetCurrentCellPosition());
        
        if (adjacentEnemies.Count > 0 && !hasAttackedThisTurn)
        {
            hasAttackedThisTurn = true; 
            yield return StartCoroutine(MultiAttack(adjacentEnemies));
        }
        else if (adjacentEnemies.Count > 0 && hasAttackedThisTurn)
        {
            Debug.Log("🏃 Saldırı hakkı zaten kullanıldı, sadece pozisyon alındı!");
        }

        if (RunManager.instance.remainingMoves > 0 && player != null && player.health.currentHP > 0 && enemies.Count > 0)
        {
            RunManager.instance.remainingMoves--;
            Debug.Log($"⚡ EKSTRA HAMLE! Kalan hak: {RunManager.instance.remainingMoves}");
            isPlayerTurn = true;
            player.UpdateHighlights();

            // ==========================================
            // YENİ: OKLARI GERİ GETİR (Normal hareket veya saldırı sonrası oklar geri gelsin)
            // ==========================================
            ShowAllEnemyIntents(); 
        }
        else
        {
            if (player != null && player.health.currentHP > 0) StartCoroutine(EnemyPhase());
        }
    }

    private Vector3Int GetSafeNeighbor(Vector3Int centerCell)
    {
        Vector3Int[] offsets = (centerCell.y % 2 != 0) ? evenOffsets : oddOffsets;
        foreach (var off in offsets)
        {
            Vector3Int neighbor = centerCell + off;
            if (groundMap.HasTile(neighbor) && !IsEnemyAtCell(neighbor) && !LevelGenerator.instance.hazardCells.Contains(neighbor))
            {
                return neighbor;
            }
        }
        return centerCell;
    }

    private IEnumerator EnemyPhase()
    {
        yield return new WaitForSeconds(0.2f);
        enemies.RemoveAll(e => e == null || e.health.currentHP <= 0);

        foreach (var e in enemies)
        {
            if (e != null && e.skipTurns <= 0) e.ExecuteLockedMove();
        }

        yield return new WaitUntil(() =>
        {
            foreach (var e in enemies) if (e != null && e.IsMoving()) return false;
            return true;
        });

        yield return new WaitForSeconds(0.2f);

        List<EnemyAI> readyToMeleeAttack = new List<EnemyAI>();
        List<EnemyAI> readyToAoEAttack = new List<EnemyAI>();

        foreach (var e in enemies)
        {
            if (e != null && e.skipTurns <= 0)
            {
                if (e.isChargingAttack) readyToAoEAttack.Add(e);
                else if (IsNeighbor(e.GetCurrentCellPosition(), player.GetCurrentCellPosition())) readyToMeleeAttack.Add(e);
            }
        }

        if (readyToAoEAttack.Count > 0)
        {
            foreach (var aoeEnemy in readyToAoEAttack)
            {
                yield return StartCoroutine(aoeEnemy.ExecuteAoEAttackCoroutine(player));
            }
        }

        if (readyToMeleeAttack.Count > 0)
        {
            yield return StartCoroutine(EnemyAttackCoroutine(readyToMeleeAttack));
        }
        else
        {
            EndTurnAndDecreaseStuns();
        }
    }

    public void HideAllEnemyIntents()
    {
        foreach (var e in enemies)
        {
            if (e != null) e.SetArrowVisibility(false);
        }
    }

    // ==========================================
    // YENİ: DÜŞMANIN OKLARINI GERİ ÇAĞIRAN FONKSİYON
    // ==========================================
    public void ShowAllEnemyIntents()
    {
        foreach (var e in enemies)
        {
            // Eğer adam sersemlemiş değilse (skipTurns <= 0) ve okları varsa görünür yap
            if (e != null && e.skipTurns <= 0) e.SetArrowVisibility(true);
        }
    }

    private IEnumerator MultiAttack(List<EnemyAI> targets)
    {
        yield return new WaitForSeconds(0.3f);

        List<int> currentRolls = new List<int>();
        int diceCount = RunManager.instance != null ? RunManager.instance.baseDiceCount : 2;

        int extraDices = 0;
        foreach (var p in RunManager.instance.activePerks)
        {
            if (p is CalculatedAmbushPerk ambushPerk)
            {
                extraDices += ambushPerk.storedExtraDices;
                ambushPerk.storedExtraDices = 0; 
            }
        }

        for (int i = 0; i < (diceCount + extraDices); i++) currentRolls.Add(Random.Range(1, 7));

        CombatPayload payload = new CombatPayload(currentRolls);

        if (RunManager.instance != null && RunManager.instance.activePerks.Exists(p => p.GetType().Name == "SwordDancePerk"))
        {
            payload.multiplyInsteadOfAdd = true;
        }

        yield return StartCoroutine(ShowDiceSequence(currentRolls));

        UpdateTotalDamageDisplay(payload.GetFinalDamage());
        yield return new WaitForSeconds(0.5f);

        if (RunManager.instance != null && RunManager.instance.activePerks.Count > 0)
        {
            List<BasePerk> perksToProcess = new List<BasePerk>(RunManager.instance.activePerks);
            perksToProcess.Sort((a, b) => a.priority.CompareTo(b.priority));

            foreach (BasePerk perk in perksToProcess)
            {
                int beforeTotal = payload.GetFinalDamage();
                perk.ModifyCombat(payload);

                bool anyDieChanged = false;
                List<int> changedIndices = new List<int>();
                for (int i = 0; i < currentRolls.Count; i++)
                {
                    if (currentRolls[i] != payload.diceRolls[i])
                        changedIndices.Add(i);
                }

                if (changedIndices.Count > 0)
                {
                    // Değişen zarlarda "!" göster (re-roll animasyonu)
                    foreach (int idx in changedIndices)
                    {
                        if (idx < spawnedDiceUI.Count)
                        {
                            Animator dieAnim = spawnedDiceUI[idx].GetComponent<Animator>();
                            TMP_Text dieText = spawnedDiceUI[idx].GetComponentInChildren<TMP_Text>();
                            if (dieAnim != null) dieAnim.enabled = true;
                            if (dieText != null) dieText.text = "!";
                        }
                    }
                    yield return new WaitForSeconds(0.5f);

                    // Yeni değerleri göster
                    foreach (int idx in changedIndices)
                    {
                        currentRolls[idx] = payload.diceRolls[idx];
                        if (idx < spawnedDiceUI.Count)
                        {
                            Animator dieAnim = spawnedDiceUI[idx].GetComponent<Animator>();
                            if (dieAnim != null) dieAnim.enabled = false;
                        }
                        AnimateSpecificDie(idx, currentRolls[idx]);
                    }
                    anyDieChanged = true;
                    yield return new WaitForSeconds(0.3f);
                }

                int afterTotal = payload.GetFinalDamage();

                if (beforeTotal != afterTotal || anyDieChanged)
                {
                    perk.TriggerVisualPop();
                    UpdateTotalDamageDisplay(afterTotal);
                    yield return new WaitForSeconds(0.3f);
                }
            }
        }

        if (Random.value < RunManager.instance.criticalChance)
        {
            payload.isCriticalHit = true;
            UpdateTotalDamageDisplay(payload.GetFinalDamage());
            Debug.Log("🎯 KRİTİK!");

            if (criticalText != null) StartCoroutine(CriticalTextPopAnimation());
            yield return new WaitForSeconds(0.5f);
        }

        yield return new WaitForSeconds(0.4f);
        HideDiceResults();

        int finalDamage = payload.GetFinalDamage();
        int damagePerEnemy = finalDamage / targets.Count;

        List<EnemyAI> knockedEnemies = new List<EnemyAI>();
        List<EnemyAI> deadEnemiesThisTurn = new List<EnemyAI>();

        foreach (var enemy in targets)
        {
            if (enemy == null) continue;

            bool dies = enemy.health.currentHP <= damagePerEnemy;
            enemy.health.TakeDamage(damagePerEnemy);

            knockedEnemies.Add(enemy);
            if (dies) deadEnemiesThisTurn.Add(enemy);
        }

        foreach (var e in knockedEnemies)
        {
            if (e == null) continue;

            Vector3Int rawTargetCell = GetRawOppositeCell(e.GetCurrentCellPosition(), player.GetCurrentCellPosition());
            EnemyAI enemyBehind = GetEnemyAtCell(rawTargetCell);

            if (enemyBehind != null)
            {
                e.skipTurns = 2; e.SetStunVisual(true);
                enemyBehind.skipTurns = 2; enemyBehind.SetStunVisual(true);

                Vector3 cPos = groundMap.GetCellCenterWorld(e.GetCurrentCellPosition());
                Vector3 pPos = groundMap.GetCellCenterWorld(player.GetCurrentCellPosition());
                cPos.z = 0; pPos.z = 0;
                Vector3 bumpDir = (cPos - pPos).normalized;

                e.StartWallBump(bumpDir); enemyBehind.StartWallBump(bumpDir);
            }
            else if (!groundMap.HasTile(rawTargetCell) || player.GetCurrentCellPosition() == rawTargetCell)
            {
                e.skipTurns = 2; e.SetStunVisual(true);
                Vector3 cPos = groundMap.GetCellCenterWorld(e.GetCurrentCellPosition());
                Vector3 pPos = groundMap.GetCellCenterWorld(player.GetCurrentCellPosition());
                cPos.z = 0; pPos.z = 0;
                e.StartWallBump((cPos - pPos).normalized);
            }
            else
            {
                e.skipTurns = Mathf.Max(e.skipTurns, 1);
                e.StartKnockbackMovement(rawTargetCell);
            }
        }

        yield return new WaitUntil(() =>
        {
            foreach (var e in knockedEnemies)
                if (e != null && e.IsMoving()) return false;
            return true;
        });

        foreach (var e in knockedEnemies)
        {
            if (e != null && payload.triggerExplosion) TriggerExplosion(e.GetCurrentCellPosition());
        }

        List<EnemyAI> deadFromSpikes = enemies.FindAll(e => e != null && e.health.currentHP <= 0);
        foreach (var deadEnemy in deadFromSpikes)
        {
            int coinDrop = Random.Range(1, 6) + RunManager.instance.bonusGold;
            RunManager.instance.currentGold += coinDrop;
            foreach (var p in RunManager.instance.activePerks) p.OnEnemyKilled(deadEnemy);
        }
        UpdateCoinUI();

        enemies.RemoveAll(e => e == null || e.health.currentHP <= 0);

        if (enemies.Count <= 0)
        {
            ClearWarningMap(); 
            if (Shopmanager.instance != null) Shopmanager.instance.OnDungeonCleared();
            else if (LevelUpManager.instance != null) LevelUpManager.instance.ShowLevelUpScreen();
            yield break;
        }

        List<EnemyAI> spikedEnemies = new List<EnemyAI>();
        bool anyoneBounced = false;

        foreach (var s in knockedEnemies)
        {
            if (s != null && LevelGenerator.instance.hazardCells.Contains(s.GetCurrentCellPosition())) spikedEnemies.Add(s);
        }

        if (spikedEnemies.Count > 0)
        {
            yield return new WaitForSeconds(0.2f);
            foreach (var s in spikedEnemies)
            {
                StartCoroutine(FlashHazardTileCoroutine(s.GetCurrentCellPosition()));
                s.health.TakeDamage(Mathf.Max(1, s.health.maxHP / 2));
            }
            yield return new WaitForSeconds(0.2f);
            foreach (var s in spikedEnemies)
            {
                if (s != null && s.health.currentHP > 0)
                {
                    Vector3Int randomBounceCell = GetRandomSafeNeighbor(s.GetCurrentCellPosition());
                    s.StartKnockbackMovement(randomBounceCell);
                    anyoneBounced = true;
                }
            }
        }

        if (anyoneBounced)
        {
            yield return new WaitUntil(() =>
            {
                foreach (var s in spikedEnemies) if (s != null && s.IsMoving()) return false;
                return true;
            });
        }

        deadFromSpikes = enemies.FindAll(e => e != null && e.health.currentHP <= 0);
        foreach (var deadEnemy in deadFromSpikes)
        {
            int coinDrop = Random.Range(1, 6) + RunManager.instance.bonusGold;
            RunManager.instance.currentGold += coinDrop;
            foreach (var p in RunManager.instance.activePerks) p.OnEnemyKilled(deadEnemy);
        }
        UpdateCoinUI();

        enemies.RemoveAll(e => e == null || e.health.currentHP <= 0);

        if (enemies.Count <= 0)
        {
            ClearWarningMap(); 
            if (Shopmanager.instance != null) Shopmanager.instance.OnDungeonCleared();
            else if (LevelUpManager.instance != null) LevelUpManager.instance.ShowLevelUpScreen();
            yield break;
        }

        yield return new WaitForSeconds(0.2f);

        List<EnemyAI> allAdjacent = GetAdjacentEnemies(player.GetCurrentCellPosition());
        List<EnemyAI> nextTargets = new List<EnemyAI>();

        foreach (var e in allAdjacent)
        {
            if (e != null && e.skipTurns <= 0) nextTargets.Add(e);
        }

        if (nextTargets.Count > 0) yield return StartCoroutine(MultiAttack(nextTargets));
    }

    private IEnumerator EnemyAttackCoroutine(List<EnemyAI> attackers)
    {
        attackers.RemoveAll(a => a.skipTurns > 0);
        if (attackers.Count == 0)
        {
            EndTurnAndDecreaseStuns();
            yield break;
        }

        yield return new WaitForSeconds(0.2f);

        bool dodged = false;
        if (RunManager.instance != null)
        {
            dodged = Random.value < RunManager.instance.dodgeChance;
        }

        if (dodged)
        {
            Debug.Log("🛡️ DODGE! Hasar engellendi ama savruluyorsun!");
            if (dodgeEffectPrefab != null)
            {
                Instantiate(dodgeEffectPrefab, player.transform.position, Quaternion.identity);
            }
        }
        else if (RunManager.instance.hasHolyAegis)
        {
            foreach (var perk in RunManager.instance.activePerks)
            {
                if (perk is HolyAegisPerk aegis)
                {
                    aegis.BreakShield();
                    break;
                }
            }
            RunManager.instance.hasHolyAegis = false;
            Debug.Log("🛡️ Holy Aegis kırıldı!");
        }
        else
        {
            player.health.TakeDamage(1);
        }

        Vector3Int playerOriginalCell = player.GetCurrentCellPosition();
        Vector3Int playerTarget = GetOppositeCell(playerOriginalCell, attackers[0].GetCurrentCellPosition());

        player.StartKnockbackMovement(playerTarget);
        yield return new WaitUntil(() => !player.IsMoving());

        if (LevelGenerator.instance != null && LevelGenerator.instance.hazardCells.Contains(player.GetCurrentCellPosition()))
        {
            yield return new WaitForSeconds(0.4f);

            int spikeDamage = Mathf.Max(1, player.health.maxHP / 2);
            Debug.Log($"🔥 Dikenlere sürüklendin! {spikeDamage} hasar yiyorsun!");

            if (RunManager.instance.hasHolyAegis)
            {
                foreach (var perk in RunManager.instance.activePerks)
                {
                    if (perk is HolyAegisPerk aegis)
                    {
                        aegis.BreakShield();
                        break;
                    }
                }
                RunManager.instance.hasHolyAegis = false;
                Debug.Log("🛡️ Holy Aegis dikenlerde kırıldı!");
            }
            else
            {
                player.health.TakeDamage(1);
            }

            StartCoroutine(FlashHazardTileCoroutine(player.GetCurrentCellPosition()));
            player.StartKnockbackMovement(playerOriginalCell);
            yield return new WaitUntil(() => !player.IsMoving());
        }

        yield return new WaitForSeconds(0.3f);
        EndTurnAndDecreaseStuns();
    }

    private void EndTurnAndDecreaseStuns()
    {
        if (player != null && player.health.currentHP > 0)
        {
            Debug.Log("🔄 Düşman turu bitti. Sersemleme süreleri azalıyor...");
            foreach (var e in enemies)
            {
                if (e != null && e.skipTurns > 0)
                {
                    e.skipTurns--;
                    if (e.skipTurns <= 0) e.SetStunVisual(false);
                }
            }
            StartPlayerTurn();
        }
    }

    public void ResumeAfterShop()
    {
        StartPlayerTurn();
    }

    private IEnumerator ShowDiceSequence(List<int> rolls)
    {
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

        yield return new WaitForSeconds(0.4f);

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

    public void UpdateTotalDamageDisplay(int val)
    {
        if (totalDamageText == null) return;
        totalDamageText.gameObject.SetActive(true);
        totalDamageText.text = val.ToString();
        StopCoroutine("TextPopAnimation");
        StartCoroutine(TextPopAnimation(totalDamageText));
    }

    public void HideDiceResults()
    {
        foreach (var die in spawnedDiceUI) Destroy(die);
        spawnedDiceUI.Clear();
        if (totalDamageText != null) totalDamageText.gameObject.SetActive(false);
        if (criticalText != null) criticalText.gameObject.SetActive(false);
    }

    private IEnumerator CriticalTextPopAnimation()
    {
        if (criticalText == null) yield break;
        criticalText.gameObject.SetActive(true);
        Transform t = criticalText.transform;
        Vector3 startScale = new Vector3(0.2f, 0.2f, 0.2f);
        Vector3 overshootScale = new Vector3(0.6f, 0.6f, 0.6f);
        Vector3 endScale = new Vector3(0.5f, 0.5f, 0.5f);
        float elapsed = 0f;
        float popDuration = 0.1f;
        while (elapsed < popDuration)
        {
            t.localScale = Vector3.Lerp(startScale, overshootScale, elapsed / popDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        elapsed = 0f;
        float settleDuration = 0.1f;
        while (elapsed < settleDuration)
        {
            t.localScale = Vector3.Lerp(overshootScale, endScale, elapsed / settleDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        t.localScale = endScale;
    }

    public bool IsEnemyAtCell(Vector3Int cell)
    {
        foreach (var e in enemies)
        {
            if (e != null && e.health.currentHP > 0 && e.GetCurrentCellPosition() == cell) return true;
        }
        return false;
    }

    public EnemyAI GetEnemyAtCell(Vector3Int cell)
    {
        foreach (var e in enemies)
        {
            if (e != null && e.health.currentHP > 0 && e.GetCurrentCellPosition() == cell) return e;
        }
        return null;
    }

    private Vector3Int GetRawOppositeCell(Vector3Int centerCell, Vector3Int awayFromCell)
    {
        Vector3Int[] offsets = (centerCell.y % 2 != 0) ? evenOffsets : oddOffsets;
        for (int i = 0; i < 6; i++)
        {
            if (centerCell + offsets[i] == awayFromCell)
            {
                int oppositeIndex = (i + 3) % 6;
                return centerCell + offsets[oppositeIndex];
            }
        }
        return centerCell;
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

    public Vector3Int GetOppositeCell(Vector3Int centerCell, Vector3Int awayFromCell)
    {
        Vector3Int[] offsets = (centerCell.y % 2 != 0) ? evenOffsets : oddOffsets;
        for (int i = 0; i < 6; i++)
        {
            if (centerCell + offsets[i] == awayFromCell)
            {
                int oppositeIndex = (i + 3) % 6;
                Vector3Int strictKnockbackCell = centerCell + offsets[oppositeIndex];
                if (!groundMap.HasTile(strictKnockbackCell) || IsEnemyAtCell(strictKnockbackCell) || player.GetCurrentCellPosition() == strictKnockbackCell)
                {
                    return centerCell;
                }
                return strictKnockbackCell;
            }
        }
        return centerCell;
    }

    private bool IsNeighbor(Vector3Int cell1, Vector3Int cell2)
    {
        Vector3Int[] offsets = (cell1.y % 2 != 0) ? evenOffsets : oddOffsets;
        foreach (var off in offsets) if (cell1 + off == cell2) return true;
        return false;
    }

    public void AnimateSpecificDie(int index, int newValue)
    {
        if (index >= 0 && index < spawnedDiceUI.Count)
        {
            Transform dieTransform = spawnedDiceUI[index].transform;
            TMP_Text dieText = spawnedDiceUI[index].GetComponentInChildren<TMP_Text>();
            Image dieImage = spawnedDiceUI[index].GetComponent<Image>();

            if (dieText != null) dieText.text = newValue.ToString();
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
        Vector3 startScale = Vector3.one * 1.6f;
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

    private Vector3Int GetRandomSafeNeighbor(Vector3Int centerCell)
    {
        Vector3Int[] offsets = (centerCell.y % 2 != 0) ? evenOffsets : oddOffsets;
        List<Vector3Int> safeNeighbors = new List<Vector3Int>();

        foreach (var off in offsets)
        {
            Vector3Int neighbor = centerCell + off;
            if (groundMap.HasTile(neighbor) && !IsEnemyAtCell(neighbor) && player.GetCurrentCellPosition() != neighbor && !LevelGenerator.instance.hazardCells.Contains(neighbor))
            {
                safeNeighbors.Add(neighbor);
            }
        }
        if (safeNeighbors.Count > 0) return safeNeighbors[Random.Range(0, safeNeighbors.Count)];
        return centerCell;
    }

    private IEnumerator FlashHazardTileCoroutine(Vector3Int cell)
    {
        if (groundMap == null) yield break;
        groundMap.SetTileFlags(cell, TileFlags.None);
        Color originalColor = Color.white;
        Color flashColor = new Color(1f, 0.4f, 0.4f, 1f);
        float duration = 0.15f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            groundMap.SetColor(cell, Color.Lerp(originalColor, flashColor, elapsed / duration));
            yield return null;
        }
        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            groundMap.SetColor(cell, Color.Lerp(flashColor, originalColor, elapsed / duration));
            yield return null;
        }
        groundMap.SetColor(cell, originalColor);
    }

    public void DrawWarningTile(Vector3Int cell)
    {
        if (warningMap == null || warningTile == null) return;
        StartCoroutine(SmoothWarningFadeIn(cell));
    }

    private IEnumerator SmoothWarningFadeIn(Vector3Int cell)
    {
        warningMap.SetTile(cell, warningTile);
        warningMap.SetTileFlags(cell, TileFlags.None); 

        Color startColor = new Color(1f, 1f, 1f, 0f);   
        Color endColor = new Color(1f, 1f, 1f, 0.5f);   

        warningMap.SetColor(cell, startColor);

        float duration = 0.3f; 
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            t = t * t * (3f - 2f * t);

            warningMap.SetColor(cell, Color.Lerp(startColor, endColor, t));
            yield return null;
        }
        warningMap.SetColor(cell, endColor);
    }
}