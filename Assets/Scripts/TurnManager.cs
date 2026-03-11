using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.Tilemaps;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.UI;
using System.Linq;

public class TurnManager : MonoBehaviour
{
    public static TurnManager instance;

    [Header("Düşman Uyarı (Warning) Karosu")]
    public Tilemap warningMap;
    public UnityEngine.Tilemaps.TileBase warningTile;

    [Header("Efektler & Prefablar")]
    public GameObject explosionPrefab;
    public GameObject dodgeEffectPrefab;
    public GameObject vacuumVfxPrefab;

    // ========================================================
    // MAYIN PREFABI BURAYA GELECEK
    // ========================================================
    public GameObject phantomMinePrefab;

    [Header("Frag-Mine")]
    public GameObject fragMinePlaceholderPrefab;

    public HexMovement player;
    public Tilemap groundMap;

    [Header("Dinamik Zar UI Sistemi")]
    public GameObject dieUIPrefab;
    public Transform diceUIContainer;
    public TMP_Text totalDamageText;
    public Sprite[] diceSprites;
    public GameObject criticalText;
    public GameObject comboTextObj; // Inspector'dan bağla — TMP_Text içermeli
    public UnityEngine.UI.Image dicePanelBackground; // Inspector'dan bağla, Source Image = None


    [Header("Coin UI")]
    public TMP_Text coinText;
    public Sprite coinSprite;

    [HideInInspector] public bool skipDiceAnim = false;
    [HideInInspector] public bool skipDiceVisuals = false; // fastMode ile kontrol edilir
    private bool isDiceAnimPlaying = false;

    private List<GameObject> spawnedDiceUI = new List<GameObject>();
    public List<EnemyAI> enemies = new List<EnemyAI>();
    [HideInInspector] public bool isLevelClearTriggered = false;
    public bool isPlayerTurn = true;
    public bool hasAttackedThisTurn = false;
    public bool isAttackAnimationPlaying = false;

    [HideInInspector] public bool isNecroShotTargeting = false;
    [HideInInspector] public bool isBombPlacementTargeting = false;
    [HideInInspector] public bool isPhaseShiftTargeting = false;
    [HideInInspector] public bool isThornPlacementTargeting = false;

    // Thorn preview
    private GameObject thornPreviewObj;

    // Thorn turn tracking: cell → kaç tur geçti (3. turda kırılır)
    private readonly Dictionary<Vector3Int, int> thornTurnCounts = new Dictionary<Vector3Int, int>();
    private readonly Dictionary<Vector3Int, Coroutine> thornBlinkCoroutines = new Dictionary<Vector3Int, Coroutine>();

    public bool IsAnyTargetingActive => isNecroShotTargeting || isBombPlacementTargeting || isPhaseShiftTargeting || isThornPlacementTargeting;

    public int hexesMovedThisTurn = 0;

    // MAYIN DEĞİŞKENLERİ
    public Vector3Int activeMineCell = new Vector3Int(-999, -999, -999);
    private GameObject activeMineObj;

    private static readonly Vector3Int[] oddOffsets = { new Vector3Int(+1, 0, 0), new Vector3Int(0, +1, 0), new Vector3Int(-1, +1, 0), new Vector3Int(-1, 0, 0), new Vector3Int(-1, -1, 0), new Vector3Int(0, -1, 0) };
    private static readonly Vector3Int[] evenOffsets = { new Vector3Int(+1, 0, 0), new Vector3Int(+1, +1, 0), new Vector3Int(0, +1, 0), new Vector3Int(-1, 0, 0), new Vector3Int(0, -1, 0), new Vector3Int(+1, -1, 0) };

    public int finalDamage = 0;

    // Combo sistemi
    private int comboCount = 0;
    private Coroutine comboFadeCoroutine;

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);

        if (player == null) player = FindFirstObjectByType<HexMovement>();
        if (groundMap == null) groundMap = GameObject.Find("GroundMap")?.GetComponent<Tilemap>();
        if (totalDamageText == null) totalDamageText = GameObject.Find("TotalDamageText")?.GetComponent<TMP_Text>();
        if (coinText == null) coinText = GameObject.Find("CoinText")?.GetComponent<TMP_Text>();
    }

    void Start()
    {
        HideDiceResults();
        SetupCoinIcon();
        UpdateCoinUI();
        Invoke("StartPlayerTurn", 0.5f);
    }

    void Update()
    {
        if (isDiceAnimPlaying && RunManager.instance != null && RunManager.instance.fastMode)
            skipDiceAnim = true;

        // fastMode aktif ise zarları gizle, pasif ise göster
        if (RunManager.instance != null)
            skipDiceVisuals = RunManager.instance.fastMode;

        // UI üstünde click olup olmadığını kontrol et (pointer ID -1 = mouse)
        if (!(EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(-1)) && (isBombPlacementTargeting || isThornPlacementTargeting) && Input.GetMouseButtonDown(0))
        {
            Vector3 mousePos = Input.mousePosition;
            mousePos.z = Mathf.Abs(Camera.main.transform.position.z);
            Vector3 worldPoint = Camera.main.ScreenToWorldPoint(mousePos);
            worldPoint.z = 0;
            Vector3Int clickedCell = groundMap.WorldToCell(worldPoint);

            if (groundMap.HasTile(clickedCell))
            {
                if (isBombPlacementTargeting) StartCoroutine(ExecuteBombAt(clickedCell));
                else if (isThornPlacementTargeting) ExecuteThornAt(clickedCell);
            }
        }

        // Thorn preview follows mouse during placement
        if (isThornPlacementTargeting && thornPreviewObj != null)
        {
            Vector3 mPos = Input.mousePosition;
            mPos.z = Mathf.Abs(Camera.main.transform.position.z);
            Vector3 wp = Camera.main.ScreenToWorldPoint(mPos);
            wp.z = 0;
            Vector3Int hoverCell = groundMap.WorldToCell(wp);

            if (groundMap.HasTile(hoverCell) && !IsThornCellBlocked(hoverCell))
            {
                Vector3 snapPos = groundMap.GetCellCenterWorld(hoverCell);
                snapPos.z = 0;
                thornPreviewObj.transform.position = snapPos;
                thornPreviewObj.SetActive(true);
            }
            else
            {
                thornPreviewObj.SetActive(false);
            }
        }

#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.F6))
        {
            foreach (var e in new List<EnemyAI>(enemies))
                if (e != null && e.health.currentHP > 0) e.health.TakeDamage(9999);
            enemies.RemoveAll(e => e == null || e.health.currentHP <= 0);
            if (enemies.Count <= 0) { ClearWarningMap(); StartCoroutine(WaitAndTriggerLevelClear()); }
        }
        if (Input.GetKeyDown(KeyCode.F7))
        {
            RunManager.instance.currentGold += 10000;
            UpdateCoinUI();
        }
        if (Input.GetKeyDown(KeyCode.F8)) SpawnDebugAoEEnemies();
        if (Input.GetKeyDown(KeyCode.F9))
        {
            foreach (var e in enemies)
            {
                if (e != null && e.enemyBehavior == EnemyAI.EnemyBehavior.TelegraphAoE && e.health.currentHP > 0 && e.skipTurns <= 0)
                {
                    e.isChargingAttack = true;
                    e.warningCells = e.GetComponent<EnemyAI>().warningCells;
                    Vector3Int playerCell = player.GetCurrentCellPosition();
                    var cells = GetLineOfCells_Debug(e.GetCurrentCellPosition(), playerCell, e.aoeAttackRange, e);
                    e.warningCells = cells;
                    foreach (var wCell in cells) DrawWarningTile(wCell);
                }
            }
        }
#endif
    }

#if UNITY_EDITOR
    private List<Vector3Int> GetLineOfCells_Debug(Vector3Int startCell, Vector3Int targetCell, int length, EnemyAI enemy)
    {
        List<Vector3Int> line = new List<Vector3Int>();
        Vector3Int[] oddOff = { new Vector3Int(+1, 0, 0), new Vector3Int(0, +1, 0), new Vector3Int(-1, +1, 0), new Vector3Int(-1, 0, 0), new Vector3Int(-1, -1, 0), new Vector3Int(0, -1, 0) };
        Vector3Int[] evenOff = { new Vector3Int(+1, 0, 0), new Vector3Int(+1, +1, 0), new Vector3Int(0, +1, 0), new Vector3Int(-1, 0, 0), new Vector3Int(0, -1, 0), new Vector3Int(+1, -1, 0) };
        int bestDir = 0; float minDist = float.MaxValue;
        Vector3Int[] startOffsets = (startCell.y % 2 != 0) ? evenOff : oddOff;
        for (int i = 0; i < 6; i++)
        {
            float d = enemy.Distance(startCell + startOffsets[i], targetCell);
            if (d < minDist) { minDist = d; bestDir = i; }
        }
        Vector3Int cur = startCell;
        for (int i = 0; i < length; i++)
        {
            Vector3Int[] offs = (cur.y % 2 != 0) ? evenOff : oddOff;
            cur += offs[bestDir];
            if (groundMap.HasTile(cur)) line.Add(cur);
        }
        return line;
    }

    private void SpawnDebugAoEEnemies()
    {
        if (LevelGenerator.instance == null || LevelGenerator.instance.aoeEnemyPrefab == null) return;
        Vector3Int playerCell = player.GetCurrentCellPosition();
        Vector3Int[] oddOff = { new Vector3Int(+1, 0, 0), new Vector3Int(0, +1, 0), new Vector3Int(-1, +1, 0), new Vector3Int(-1, 0, 0), new Vector3Int(-1, -1, 0), new Vector3Int(0, -1, 0) };
        Vector3Int[] evenOff = { new Vector3Int(+1, 0, 0), new Vector3Int(+1, +1, 0), new Vector3Int(0, +1, 0), new Vector3Int(-1, 0, 0), new Vector3Int(0, -1, 0), new Vector3Int(+1, -1, 0) };
        Vector3Int[] offsets = (playerCell.y % 2 != 0) ? evenOff : oddOff;
        List<Vector3Int> spawnCells = new List<Vector3Int>();

        int[][] pairs = { new[] { 0, 3 }, new[] { 1, 4 }, new[] { 2, 5 } };
        foreach (var pair in pairs)
        {
            Vector3Int c1 = playerCell + offsets[pair[0]];
            Vector3Int c2 = playerCell + offsets[pair[1]];
            if (groundMap.HasTile(c1) && groundMap.HasTile(c2) && !IsEnemyAtCell(c1) && !IsEnemyAtCell(c2))
            {
                Vector3Int[] off1 = (c1.y % 2 != 0) ? evenOff : oddOff; Vector3Int far1 = c1 + off1[pair[0]];
                Vector3Int[] off2 = (c2.y % 2 != 0) ? evenOff : oddOff; Vector3Int far2 = c2 + off2[pair[1]];
                if (groundMap.HasTile(far1) && groundMap.HasTile(far2) && !IsEnemyAtCell(far1) && !IsEnemyAtCell(far2)) { spawnCells.Add(far1); spawnCells.Add(far2); break; }
                spawnCells.Add(c1); spawnCells.Add(c2); break;
            }
        }
        if (spawnCells.Count < 2) return;
        foreach (var spawnCell in spawnCells)
        {
            Vector3 spawnPos = groundMap.GetCellCenterWorld(spawnCell); spawnPos.z = 0;
            GameObject obj = Instantiate(LevelGenerator.instance.aoeEnemyPrefab, spawnPos, Quaternion.identity);
            EnemyAI ai = obj.GetComponent<EnemyAI>(); ai.groundMap = groundMap;
            ai.health.maxHP = 50; ai.health.currentHP = 50; ai.health.updateHealth();
            RegisterEnemy(ai); StartCoroutine(ai.FadeSpawnCoroutine());
        }
    }
#endif

    public void StartPlayerTurn()
    {
        if (player == null || player.health.currentHP <= 0) return;

        isPlayerTurn = true;
        hasAttackedThisTurn = false;
        isAttackAnimationPlaying = false;

        if (RunManager.instance != null)
        {
            int moves = RunManager.instance.extraMovesPerTurn;
            RunManager.instance.remainingMoves = moves;

            // Surge Boot: her tur başında sıfırla (Use() anında aktifleştirir)
            RunManager.instance.surgeBootActive = false;
            RunManager.instance.surgeBootNextTurn = false;
        }
        player.UpdateHighlights();
        StartCoroutine(LockIntentsNextFrame());
    }

    // ========================================================
    // YENİ: MAYIN BIRAKMA / YER DEĞİŞTİRME KOMUTU
    // ========================================================
    public void TryDropMine(Vector3Int cell)
    {
        if (activeMineObj != null)
        {
            Destroy(activeMineObj);
        }

        activeMineCell = cell;
        Vector3 pos = groundMap.GetCellCenterWorld(cell);
        pos.z = 0;

        if (phantomMinePrefab != null)
        {
            activeMineObj = Instantiate(phantomMinePrefab, pos, Quaternion.identity);
        }
    }

    public void ClearWarningMap()
    {
        GameObject warningMapObj = GameObject.Find("WarningMap");
        if (warningMapObj != null) warningMapObj.GetComponent<Tilemap>().ClearAllTiles();

        // Warlock uyarı haritasını da temizle
        GameObject warlockWarnObj = GameObject.Find("WarlockWarningMap");
        if (warlockWarnObj != null) warlockWarnObj.GetComponent<Tilemap>().ClearAllTiles();

        if (activeMineObj != null) Destroy(activeMineObj);
        activeMineCell = new Vector3Int(-999, -999, -999);
    }

    public void PlayerTakeDamage(int amt)
    {
        if (player == null || player.health.currentHP <= 0) return;
        ResetCombo();

        if (player.health.currentHP - amt <= 0)
        {
            var bribe = RunManager.instance.activePerks.FirstOrDefault(p => p is BribePerk);
            if (bribe != null)
            {
                RunManager.instance.currentGold = 0; UpdateCoinUI(); player.health.Heal(9999);
                RunManager.instance.activePerks.Remove(bribe); Destroy(bribe.gameObject);

                // DODGE EFEKTİNİ COROUTINE İLE ÇAĞIRIYORUZ (Kalkan Kırılma Animasyonu)
                StartCoroutine(AnimateShieldBreakFX(player.transform.position));
                return;
            }
        }
        player.health.TakeDamage(amt);
    }

    // ========================================================
    // DODGE / BRIBE İÇİN KALKAN KIRILMA EFEKTİ
    // ========================================================
    private IEnumerator AnimateShieldBreakFX(Vector3 pos)
    {
        if (dodgeEffectPrefab == null) yield break;

        GameObject fx = Instantiate(dodgeEffectPrefab, pos, Quaternion.identity);
        SpriteRenderer[] renderers = fx.GetComponentsInChildren<SpriteRenderer>();

        float duration = 0.35f;
        float elapsed = 0f;

        // Küçükten büyüyen bir efekt için scale değerleri
        Vector3 startScale = Vector3.one * 2.4f;
        Vector3 endScale = Vector3.one * 9.0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // Objeyi büyütüyoruz
            fx.transform.localScale = Vector3.Lerp(startScale, endScale, t);

            // Giderek saydamlaştırıyoruz
            foreach (var sr in renderers)
            {
                if (sr != null)
                {
                    Color c = sr.color;
                    c.a = Mathf.Lerp(1f, 0f, t);
                    sr.color = c;
                }
            }
            yield return null;
        }

        Destroy(fx);
    }

    private IEnumerator WaitAndTriggerLevelClear()
    {
        if (isLevelClearTriggered) yield break;
        isLevelClearTriggered = true;

        while (isAttackAnimationPlaying) yield return null;
        if (CoinDropVFX.instance != null) while (CoinDropVFX.instance.activeCoinCount > 0) yield return null;
        yield return new WaitForSeconds(0.3f);

        if (Shopmanager.instance != null)
        {
            bool isBossLevel = RunManager.instance.currentLevel > 0 && RunManager.instance.currentLevel % 5 == 0;
            if (isBossLevel) Shopmanager.instance.OnBossCleared(); else Shopmanager.instance.OnDungeonCleared();
        }
        else if (LevelUpManager.instance != null) LevelUpManager.instance.ShowLevelUpScreen();
    }

    private void SetupCoinIcon()
    {
        if (coinSprite == null) { var vfx = FindFirstObjectByType<CoinDropVFX>(); if (vfx != null) coinSprite = vfx.coinSprite; }
        if (coinText == null || coinSprite == null) return;
        Transform parent = coinText.transform.parent;
        if (parent == null || parent.Find("CoinIcon") != null) return;

        HorizontalLayoutGroup hlg = parent.GetComponent<HorizontalLayoutGroup>();
        if (hlg == null)
        {
            hlg = parent.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 4f; hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = false; hlg.childControlHeight = false;
        }

        GameObject iconGO = new GameObject("CoinIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        iconGO.transform.SetParent(parent, false); iconGO.transform.SetAsFirstSibling();
        RectTransform iconRT = iconGO.GetComponent<RectTransform>(); iconRT.sizeDelta = new Vector2(22f, 22f);
        LayoutElement le = iconGO.AddComponent<LayoutElement>(); le.preferredWidth = 22f; le.preferredHeight = 22f;
        Image img = iconGO.GetComponent<Image>(); img.sprite = coinSprite; img.preserveAspect = true; img.raycastTarget = false;
    }

    public void UpdateCoinUI()
    {
        if (coinText != null && RunManager.instance != null) coinText.text = RunManager.instance.currentGold.ToString();
        if (Shopmanager.instance != null) Shopmanager.instance.RefreshAffordability();
    }

    public void RegisterEnemy(EnemyAI enemy) { if (!enemies.Contains(enemy)) enemies.Add(enemy); }
    public void StartNecroShotTargeting() { isNecroShotTargeting = true; }

    public void TryNecroShotKill(EnemyAI target)
    {
        if (!isNecroShotTargeting || target == null) return;
        if (target.enemyBehavior == EnemyAI.EnemyBehavior.Boss) return;
        isNecroShotTargeting = false;
        if (player != null) player.TriggerAttackAnimation();

        target.health.TakeDamage(9999);
        if (target.enemyBehavior != EnemyAI.EnemyBehavior.Totem)
        {
            bool isBossRoom = RunManager.instance.currentLevel % 5 == 0;
            int coinDrop = 0;
            if (isBossRoom) { if (target.enemyBehavior == EnemyAI.EnemyBehavior.Boss) coinDrop = 20; }
            else { coinDrop = Random.Range(1, 4) + RunManager.instance.bonusGold; if (RunManager.instance.doubleGoldNextKill) { coinDrop *= 2; RunManager.instance.doubleGoldNextKill = false; } }
            if (coinDrop > 0) { RunManager.instance.currentGold += coinDrop; RunManager.instance.totalGoldEarned += coinDrop; if (CoinDropVFX.instance != null) CoinDropVFX.instance.SpawnCoins(target.transform.position, coinDrop); }
        }
        foreach (var p in RunManager.instance.activePerks) if (!p.isDisabled) p.OnEnemyKilled(target);
        UpdateCoinUI();
        enemies.RemoveAll(e => e == null || e.health.currentHP <= 0);
        if (enemies.Count <= 0) { ClearWarningMap(); StartCoroutine(WaitAndTriggerLevelClear()); }
        RunManager.instance.totalEnemiesKilled++; // Öldürme sayısını arttır
    }

    public void StartBombPlacement() { isBombPlacementTargeting = true; }
    private IEnumerator ExecuteBombAt(Vector3Int cell)
    {
        isBombPlacementTargeting = false;

        // Placeholder'ı hemen koy, patlamadan önce göster
        GameObject placeholderObj = null;
        if (fragMinePlaceholderPrefab != null)
        {
            Vector3 placeholderPos = groundMap.GetCellCenterWorld(cell);
            placeholderPos.z = 0;
            placeholderObj = Instantiate(fragMinePlaceholderPrefab, placeholderPos, Quaternion.identity);
        }

        // Zar animasyonunu göster
        int diceCount = RunManager.instance != null ? RunManager.instance.baseDiceCount : 2;
        List<int> rolls = new List<int>();
        for (int i = 0; i < diceCount; i++) rolls.Add(Random.Range(1, 7));
        if (RunManager.instance != null) RunManager.instance.totalDiceRolled += diceCount;

        CombatPayload payload = new CombatPayload(rolls);
        if (RunManager.instance != null && RunManager.instance.activePerks.Exists(p => p.GetType().Name == "SymbioticFuryPerk"))
            payload.multiplyInsteadOfAdd = true;

        isDiceAnimPlaying = true; skipDiceAnim = false;
        if (!skipDiceVisuals)
        {
            yield return StartCoroutine(ShowDiceSequence(rolls));
            UpdateTotalDamageDisplay(payload.GetFinalDamage());
        }

        // Perk zar boost'larını uygula (normal combat ile aynı)
        if (RunManager.instance != null && RunManager.instance.activePerks.Count > 0)
        {
            List<BasePerk> perksToProcess = RunManager.instance.activePerks.FindAll(p => p != null && !p.isDisabled);
            perksToProcess.Sort((a, b) => { int r = b.isRerollPerk.CompareTo(a.isRerollPerk); return r != 0 ? r : a.priority.CompareTo(b.priority); });
            foreach (BasePerk perk in perksToProcess)
            {
                int beforeTotal = payload.GetFinalDamage();
                perk.ModifyCombat(payload);
                bool anyDieChanged = false; List<int> changedIndices = new List<int>();
                for (int i = 0; i < rolls.Count; i++) if (rolls[i] != payload.diceRolls[i]) changedIndices.Add(i);
                if (changedIndices.Count > 0)
                {
                    if (!skipDiceVisuals)
                    {
                        if (perk.isRerollPerk)
                        {
                            foreach (int idx in changedIndices)
                            {
                                if (idx < spawnedDiceUI.Count)
                                {
                                    Animator dieAnim = spawnedDiceUI[idx].GetComponent<Animator>(); TMP_Text dieText = spawnedDiceUI[idx].GetComponentInChildren<TMP_Text>();
                                    if (dieAnim != null) dieAnim.enabled = true; if (dieText != null) dieText.text = "!";
                                }
                            }
                            yield return StartCoroutine(SkippableWait(0.5f));
                            foreach (int idx in changedIndices)
                            {
                                rolls[idx] = payload.diceRolls[idx];
                                if (idx < spawnedDiceUI.Count) { Animator dieAnim = spawnedDiceUI[idx].GetComponent<Animator>(); if (dieAnim != null) dieAnim.enabled = false; }
                                AnimateSpecificDie(idx, rolls[idx]);
                            }
                        }
                        else
                        {
                            foreach (int idx in changedIndices) { rolls[idx] = payload.diceRolls[idx]; AnimateSpecificDie(idx, rolls[idx]); }
                        }
                        anyDieChanged = true; yield return StartCoroutine(SkippableWait(0.3f));
                    }
                    else
                    {
                        foreach (int idx in changedIndices) { rolls[idx] = payload.diceRolls[idx]; }
                        anyDieChanged = true;
                    }
                }
                int afterTotal = payload.GetFinalDamage();
                if (beforeTotal != afterTotal || anyDieChanged)
                {
                    if (!skipDiceVisuals)
                    {
                        perk.TriggerVisualPop(); if (PerkListUI.instance != null) PerkListUI.instance.TriggerShakeForPerk(perk); UpdateTotalDamageDisplay(afterTotal); yield return StartCoroutine(SkippableWait(0.3f));
                    }
                }
            }
        }

        if (!skipDiceVisuals && Random.value < RunManager.instance.criticalChance)
        {
            payload.isCriticalHit = true;
            UpdateTotalDamageDisplay(payload.GetFinalDamage());
            if (criticalText != null) StartCoroutine(CriticalTextPopAnimation());
            yield return StartCoroutine(SkippableWait(0.5f));
        }
        else if (skipDiceVisuals && Random.value < RunManager.instance.criticalChance)
        {
            payload.isCriticalHit = true;
        }

        int totalDamage = payload.GetFinalDamage();
        if (!skipDiceVisuals)
        {
            UpdateTotalDamageDisplay(totalDamage);
            yield return StartCoroutine(SkippableWait(0.6f));
        }
        isDiceAnimPlaying = false; skipDiceAnim = false;

        // Placeholder'ı kaldır ve patlama efektini çal
        if (placeholderObj != null) Destroy(placeholderObj);
        StartCoroutine(AnimateExplosionFX(groundMap.GetCellCenterWorld(cell)));

        HideDiceResults();

        // Patlama alanı: bombadaki hex + 1 mesafe komşular
        Vector3Int[] offsets = (cell.y % 2 != 0) ? evenOffsets : oddOffsets;
        List<Vector3Int> blastCells = new List<Vector3Int> { cell };
        foreach (var off in offsets) blastCells.Add(cell + off);

        // Her düşmana aynı zarı ver (paylaştırma yok)
        List<EnemyAI> hitEnemies = new List<EnemyAI>();
        foreach (var bc in blastCells) { EnemyAI enemy = GetEnemyAtCell(bc); if (enemy != null && !hitEnemies.Contains(enemy)) hitEnemies.Add(enemy); }

        foreach (var enemy in hitEnemies)
        {
            enemy.health.TakeDamage(totalDamage);
            if (enemy.health.currentHP <= 0)
            {
                if (enemy.enemyBehavior != EnemyAI.EnemyBehavior.Totem)
                {
                    bool isBossRoom = RunManager.instance.currentLevel % 5 == 0;
                    int coinDrop = 0;
                    if (isBossRoom) { if (enemy.enemyBehavior == EnemyAI.EnemyBehavior.Boss) coinDrop = 20; }
                    else { coinDrop = Random.Range(1, 4) + RunManager.instance.bonusGold; if (RunManager.instance.doubleGoldNextKill) { coinDrop *= 2; RunManager.instance.doubleGoldNextKill = false; } }
                    if (coinDrop > 0) { RunManager.instance.currentGold += coinDrop; RunManager.instance.totalGoldEarned += coinDrop; if (CoinDropVFX.instance != null) CoinDropVFX.instance.SpawnCoins(enemy.transform.position, coinDrop); }
                }
                foreach (var p in RunManager.instance.activePerks) if (!p.isDisabled) p.OnEnemyKilled(enemy);
                RunManager.instance.totalEnemiesKilled++;
            }
        }

        UpdateCoinUI();
        enemies.RemoveAll(e => e == null || e.health.currentHP <= 0);
        if (enemies.Count <= 0) { ClearWarningMap(); StartCoroutine(WaitAndTriggerLevelClear()); }
        else ShowAllEnemyIntents();
    }

    public void StartPhaseShiftTargeting() { isPhaseShiftTargeting = true; }
    public void TryPhaseShift(EnemyAI target)
    {
        if (!isPhaseShiftTargeting || target == null) return;
        isPhaseShiftTargeting = false;
        StartCoroutine(PhaseShiftCoroutine(target));
    }

    private IEnumerator PhaseShiftCoroutine(EnemyAI target)
    {
        Vector3Int playerCell = player.GetCurrentCellPosition();
        Vector3Int enemyCell = target.GetCurrentCellPosition();

        // Oyuncu küçülür
        float shrinkDur = 0.12f; float elapsed = 0f;
        while (elapsed < shrinkDur)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / shrinkDur;
            player.transform.localScale = Vector3.Lerp(Vector3.one, Vector3.zero, t);
            yield return null;
        }
        player.transform.localScale = Vector3.zero;

        // Konumları değiştir
        player.ForceSetPosition(enemyCell);
        target.ForceSetPosition(playerCell);

        // Oyuncu büyür
        elapsed = 0f;
        while (elapsed < shrinkDur)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / shrinkDur;
            player.transform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, t);
            yield return null;
        }
        player.transform.localScale = Vector3.one;

        // Swap sonrası mayın kontrolü: düşman mayının üstüne geldiyse hemen patlat
        if (activeMineCell.y != -999 && target.GetCurrentCellPosition() == activeMineCell)
        {
            yield return new WaitForSeconds(0.1f);
            var phantomPerk = RunManager.instance.activePerks.Find(p => p is PhantomLimbPerk);
            float mineDamagePercent = phantomPerk != null ? phantomPerk.currentLevel * 0.25f : 0.25f;
            TriggerExplosion(activeMineCell, mineDamagePercent);

            if (target != null && target.health.currentHP > 0)
                target.ApplyStun(2, true);

            if (activeMineObj != null) Destroy(activeMineObj);
            activeMineCell = new Vector3Int(-999, -999, -999);

            List<EnemyAI> mineKills = enemies.FindAll(e => e != null && e.health.currentHP <= 0);
            foreach (var deadEnemy in mineKills)
            {
                if (deadEnemy.enemyBehavior != EnemyAI.EnemyBehavior.Totem)
                {
                    bool isBossRoom = RunManager.instance.currentLevel % 5 == 0;
                    int coinDrop = 0;
                    if (isBossRoom) { if (deadEnemy.enemyBehavior == EnemyAI.EnemyBehavior.Boss) coinDrop = 20; }
                    else { coinDrop = Random.Range(1, 4) + RunManager.instance.bonusGold; if (RunManager.instance.doubleGoldNextKill) { coinDrop *= 2; RunManager.instance.doubleGoldNextKill = false; } }
                    if (coinDrop > 0) { RunManager.instance.currentGold += coinDrop; RunManager.instance.totalGoldEarned += coinDrop; if (CoinDropVFX.instance != null) CoinDropVFX.instance.SpawnCoins(deadEnemy.transform.position, coinDrop); }
                }
                foreach (var p in RunManager.instance.activePerks) p.OnEnemyKilled(deadEnemy);
            }
            enemies.RemoveAll(e => e == null || e.health.currentHP <= 0);
            UpdateCoinUI();

            if (enemies.Count <= 0) { ClearWarningMap(); StartCoroutine(WaitAndTriggerLevelClear()); yield break; }
        }

        // Highlight ve ok'ları yeni konuma göre güncelle
        player.UpdateHighlights();
        LockAllEnemyIntents();
        ShowAllEnemyIntents();
    }

    public void StartThornPlacement()
    {
        isThornPlacementTargeting = true;
        CreateThornPreview();
    }

    private void CreateThornPreview()
    {
        if (thornPreviewObj != null) return;

        Sprite previewSprite = null;
        if (LevelGenerator.instance != null && LevelGenerator.instance.hazardTile is Tile ht && ht.sprite != null)
            previewSprite = ht.sprite;

        if (previewSprite == null) return;

        thornPreviewObj = new GameObject("ThornPreview");
        var sr = thornPreviewObj.AddComponent<SpriteRenderer>();
        sr.sprite = previewSprite;
        sr.color = new Color(1f, 1f, 1f, 0.45f);
        sr.sortingOrder = 10;
        thornPreviewObj.SetActive(false);
    }

    private void DestroyThornPreview()
    {
        if (thornPreviewObj != null) { Destroy(thornPreviewObj); thornPreviewObj = null; }
    }

    private bool IsThornCellBlocked(Vector3Int cell)
    {
        if (player != null && player.GetCurrentCellPosition() == cell) return true;
        if (IsEnemyAtCell(cell)) return true;
        if (LevelGenerator.instance != null && LevelGenerator.instance.hazardCells.Contains(cell)) return true;
        return false;
    }

    private void ExecuteThornAt(Vector3Int cell)
    {
        if (LevelGenerator.instance == null) { isThornPlacementTargeting = false; DestroyThornPreview(); return; }
        if (IsThornCellBlocked(cell)) return;
        isThornPlacementTargeting = false;
        DestroyThornPreview();
        LevelGenerator.instance.hazardCells.Add(cell);
        thornTurnCounts[cell] = 0;
        if (LevelGenerator.instance.hazardMap != null && LevelGenerator.instance.hazardTile != null) LevelGenerator.instance.hazardMap.SetTile(cell, LevelGenerator.instance.hazardTile);
        // Yerleştirilir yerleştirilmez blink başlasın
        thornBlinkCoroutines[cell] = StartCoroutine(BlinkThornTile(cell));
        if (player != null) player.UpdateHighlights();
    }

    private void TickThornTurns()
    {
        if (LevelGenerator.instance == null) return;
        var toRemove = new List<Vector3Int>();

        foreach (var kv in new Dictionary<Vector3Int, int>(thornTurnCounts))
        {
            int newCount = kv.Value + 1;
            thornTurnCounts[kv.Key] = newCount;

            if (newCount >= 3)
            {
                // 3. tur: kırıl
                toRemove.Add(kv.Key);
            }
        }

        foreach (var cell in toRemove)
        {
            // Blink coroutine'i durdur
            if (thornBlinkCoroutines.TryGetValue(cell, out Coroutine co))
            {
                if (co != null) StopCoroutine(co);
                thornBlinkCoroutines.Remove(cell);
            }
            thornTurnCounts.Remove(cell);
            LevelGenerator.instance.hazardCells.Remove(cell);
            if (LevelGenerator.instance.hazardMap != null)
                LevelGenerator.instance.hazardMap.SetTile(cell, null);
        }
    }

    private IEnumerator BlinkThornTile(Vector3Int cell)
    {
        if (LevelGenerator.instance?.hazardMap == null) yield break;
        float elapsed = 0f;
        float blinkSpeed = 8f;
        var tile = LevelGenerator.instance.hazardTile as UnityEngine.Tilemaps.Tile;

        // thornTurnCounts'ta var olduğu sürece yanıp sön
        while (thornTurnCounts.ContainsKey(cell))
        {
            if (LevelGenerator.instance?.hazardMap == null) yield break;
            bool show = Mathf.Sin(elapsed * blinkSpeed) > 0f;
            if (tile != null)
                LevelGenerator.instance.hazardMap.SetTileFlags(cell, UnityEngine.Tilemaps.TileFlags.None);
            LevelGenerator.instance.hazardMap.SetColor(cell, show ? Color.white : new Color(1f, 1f, 1f, 0f));
            elapsed += Time.deltaTime;
            yield return null;
        }
        // Temizlendiğinde rengi sıfırla
        if (LevelGenerator.instance?.hazardMap != null)
            LevelGenerator.instance.hazardMap.SetColor(cell, Color.white);
    }

    public void LockAllEnemyIntents()
    {
        if (player == null || player.health.currentHP <= 0) return;
        Vector3Int pCell = player.GetCurrentCellPosition();
        foreach (var e in enemies) if (e != null) { bool isStunned = e.skipTurns > 0; e.LockNextMove(pCell, isStunned); }
    }

    private IEnumerator SkippableWait(float seconds)
    {
        float elapsed = 0f;
        while (elapsed < seconds)
        {
            if (skipDiceAnim) yield break;
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private IEnumerator LockIntentsNextFrame()
    {
        yield return new WaitForSeconds(0.35f);
        LockAllEnemyIntents();
        ShowAllEnemyIntents();
    }

    public void PlayerFinishedMove(Vector3Int playerCell)
    {
        StartCoroutine(HandlePlayerPhase(playerCell));
    }

    public void SkipTurn()
    {
        if (!isPlayerTurn || IsAnyTargetingActive) return;
        isPlayerTurn = false;
        if (RunManager.instance != null) RunManager.instance.remainingMoves = 0;

        if (player != null)
        {
            player.ClearHighlights();

            if (RunManager.instance != null && RunManager.instance.activePerks.Exists(p => p is PhantomLimbPerk))
            {
                TryDropMine(player.GetCurrentCellPosition());

                var perk = RunManager.instance.activePerks.Find(p => p is PhantomLimbPerk);
                if (perk != null) perk.TriggerVisualPop();
            }
        }

        StartCoroutine(HandleSkipPhase());
    }

    private IEnumerator HandleSkipPhase()
    {
        // OnSkip'i saldırıdan ÖNCE çağır: DormantSpore zarları bu turda kullanılabilsin
        foreach (var perk in RunManager.instance.activePerks) if (!perk.isDisabled) perk.OnSkip();
        RunManager.instance.currentGold += RunManager.instance.skipBonusGold;
        UpdateCoinUI();

        List<EnemyAI> adjacentEnemies = GetAdjacentEnemies(player.GetCurrentCellPosition());
        if (adjacentEnemies.Count > 0 && !hasAttackedThisTurn)
        {
            RunManager.instance.remainingMoves = 0;
            hasAttackedThisTurn = true; isAttackAnimationPlaying = true;
            yield return StartCoroutine(MultiAttack(adjacentEnemies));
        }

        if (enemies.Count <= 0) yield break;
        StartCoroutine(EnemyPhase());
    }

    public void TriggerExplosion(Vector3Int centerCell, float damagePercent = 0.5f, bool includeCenter = true)
    {
        Vector3 spawnPos = groundMap.GetCellCenterWorld(centerCell);
        spawnPos.z = 0;
        StartCoroutine(AnimateExplosionFX(spawnPos));

        Vector3Int[] offsets = (centerCell.y % 2 != 0) ? evenOffsets : oddOffsets;
        List<Vector3Int> cellsToHit = includeCenter ? new List<Vector3Int> { centerCell } : new List<Vector3Int>();
        foreach (var off in offsets) cellsToHit.Add(centerCell + off);

        HashSet<EnemyAI> enemiesToHit = new HashSet<EnemyAI>();
        foreach (var cell in cellsToHit)
        {
            EnemyAI e = GetEnemyAtCell(cell);
            if (e != null && e.health.currentHP > 0) enemiesToHit.Add(e);
        }

        foreach (var e in enemiesToHit)
        {
            if (e == null) continue;
            int explosionDamage = Mathf.Max(1, Mathf.RoundToInt(e.health.maxHP * damagePercent));
            e.health.TakeDamage(explosionDamage);
        }

        enemies.RemoveAll(e => e == null || e.health.currentHP <= 0);
    }

    private IEnumerator AnimateExplosionFX(Vector3 pos)
    {
        if (AudioManager.instance != null) AudioManager.instance.PlayExplosion();
        if (explosionPrefab == null) yield break;
        GameObject fx = Instantiate(explosionPrefab, pos, Quaternion.identity);
        SpriteRenderer[] renderers = fx.GetComponentsInChildren<SpriteRenderer>();
        float duration = 0.15f; float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime; float t = elapsed / duration;
            fx.transform.localScale = Vector3.Lerp(Vector3.one * 1.0f, Vector3.one * 5.2f, t);
            foreach (var sr in renderers) { Color c = sr.color; c.a = Mathf.Lerp(0.8f, 0f, t); sr.color = c; }
            yield return null;
        }
        Destroy(fx);
    }

    private IEnumerator VacuumVFXCoroutine(Vector3 pos)
    {
        if (vacuumVfxPrefab == null) yield break;
        if (AudioManager.instance != null) AudioManager.instance.PlayVacuum();
        GameObject vfx = Instantiate(vacuumVfxPrefab, pos, Quaternion.identity);
        SpriteRenderer[] renderers = vfx.GetComponentsInChildren<SpriteRenderer>();
        float duration = 0.4f; float elapsed = 0f;
        Vector3 startScale = Vector3.one * 12f; Vector3 endScale = Vector3.one * 1.2f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime; float t = elapsed / duration;
            float alpha = t < 0.2f ? Mathf.Lerp(0f, 0.4f, t / 0.2f) : Mathf.Lerp(0.4f, 0f, (t - 0.2f) / 0.8f);
            vfx.transform.localScale = Vector3.Lerp(startScale, endScale, t);
            foreach (var sr in renderers) { if (sr != null) { Color c = sr.color; c.a = alpha; sr.color = c; } }
            yield return null;
        }
        Destroy(vfx);
    }

    private IEnumerator HandlePlayerPhase(Vector3Int playerCell)
    {
        hexesMovedThisTurn++;

        // Surge Boot: hareketten sonra kapat
        if (RunManager.instance != null)
            RunManager.instance.surgeBootActive = false;

        if (LevelGenerator.instance.hazardCells.Contains(playerCell))
        {
            yield return new WaitForSeconds(0.15f);
            StartCoroutine(FlashHazardTileCoroutine(playerCell));
            if (RunManager.instance.hasBioBarrier)
            {
                foreach (var perk in RunManager.instance.activePerks) if (perk is BioBarrierPerk aegis) { aegis.BreakShield(); break; }
                RunManager.instance.hasBioBarrier = false;
            }
            else PlayerTakeDamage(1);

            yield return new WaitForSeconds(0.15f);
            player.StartKnockbackMovement(GetSafeNeighbor(playerCell));
            yield return new WaitUntil(() => !player.IsMoving());

            if (RunManager.instance.remainingMoves > 0 && player != null && player.health.currentHP > 0)
            {
                RunManager.instance.remainingMoves--; isPlayerTurn = true; player.UpdateHighlights(); ShowAllEnemyIntents();
            }
            else if (player != null && player.health.currentHP > 0)
            {
                player.ClearHighlights(); yield return new WaitForSeconds(0.1f); StartCoroutine(EnemyPhase());
            }
            yield break;
        }

        List<EnemyAI> adjacentEnemies = GetAdjacentEnemies(player.GetCurrentCellPosition());
        if (adjacentEnemies.Count > 0 && !hasAttackedThisTurn)
        {
            RunManager.instance.remainingMoves = 0;
            hasAttackedThisTurn = true; isAttackAnimationPlaying = true;
            yield return StartCoroutine(MultiAttack(adjacentEnemies));
        }
        else yield return new WaitForSeconds(0.05f);

        if (RunManager.instance.remainingMoves > 0 && player != null && player.health.currentHP > 0 && enemies.Count > 0)
        {
            RunManager.instance.remainingMoves--; isPlayerTurn = true; player.UpdateHighlights(); ShowAllEnemyIntents();
        }
        else if (player != null && player.health.currentHP > 0)
        {
            player.ClearHighlights(); yield return new WaitForSeconds(0.1f); StartCoroutine(EnemyPhase());
        }
    }

    private Vector3Int GetSafeNeighbor(Vector3Int centerCell)
    {
        Vector3Int[] offsets = (centerCell.y % 2 != 0) ? evenOffsets : oddOffsets;
        foreach (var off in offsets)
        {
            Vector3Int n = centerCell + off;
            if (groundMap.HasTile(n) && !IsEnemyAtCell(n) && !LevelGenerator.instance.hazardCells.Contains(n)) return n;
        }
        return centerCell;
    }

    private IEnumerator EnemyPhase()
    {
        yield return new WaitForSeconds(0.2f);
        enemies.RemoveAll(e => e == null || e.health.currentHP <= 0);

        foreach (var e in enemies) if (e != null && e.skipTurns <= 0) e.ExecuteLockedMove();
        yield return new WaitUntil(() => { foreach (var e in enemies) if (e != null && e.IsMoving()) return false; return true; });
        yield return new WaitForSeconds(0.2f);

        // ========================================================
        // MAYIN KONTROLÜ
        // ========================================================
        if (activeMineCell.y != -999)
        {
            EnemyAI victim = GetEnemyAtCell(activeMineCell);

            if (victim != null && victim.health.currentHP > 0)
            {
                var phantomPerk = RunManager.instance.activePerks.Find(p => p is PhantomLimbPerk);
                float mineDamagePercent = phantomPerk != null ? phantomPerk.currentLevel * 0.25f : 0.25f;
                TriggerExplosion(activeMineCell, mineDamagePercent);

                if (victim != null && victim.health.currentHP > 0)
                    victim.ApplyStun(2, true);

                if (activeMineObj != null) Destroy(activeMineObj);
                activeMineCell = new Vector3Int(-999, -999, -999);

                // Mayınla ölen düşmanlar için coin drop ve perk callback
                List<EnemyAI> mineKills = enemies.FindAll(e => e != null && e.health.currentHP <= 0);
                foreach (var deadEnemy in mineKills)
                {
                    if (deadEnemy.enemyBehavior != EnemyAI.EnemyBehavior.Totem)
                    {
                        int coinDrop = Random.Range(1, 4) + RunManager.instance.bonusGold;
                        if (RunManager.instance.doubleGoldNextKill) { coinDrop *= 2; RunManager.instance.doubleGoldNextKill = false; }
                        RunManager.instance.currentGold += coinDrop; RunManager.instance.totalGoldEarned += coinDrop;
                        if (CoinDropVFX.instance != null) CoinDropVFX.instance.SpawnCoins(deadEnemy.transform.position, coinDrop);
                    }
                    foreach (var p in RunManager.instance.activePerks) p.OnEnemyKilled(deadEnemy);
                }

                enemies.RemoveAll(e => e == null || e.health.currentHP <= 0);
                UpdateCoinUI();

                if (enemies.Count <= 0)
                {
                    ClearWarningMap();
                    StartCoroutine(WaitAndTriggerLevelClear());
                    yield break;
                }
                else
                {
                    var perk = RunManager.instance.activePerks.Find(p => p is PhantomLimbPerk);
                    if (perk != null) perk.TriggerVisualPop();
                }
            }
        }

        List<EnemyAI> readyToBossAttack = new List<EnemyAI>();
        List<EnemyAI> readyToAoEAttack = new List<EnemyAI>();
        List<EnemyAI> readyToMeleeAttack = new List<EnemyAI>();
        List<WarlockEnemyAI> readyWarlockAttack1 = new List<WarlockEnemyAI>();
        List<WarlockEnemyAI> readyWarlockAttack2 = new List<WarlockEnemyAI>();

        foreach (var e in enemies)
        {
            if (e != null && e.skipTurns <= 0)
            {
                if (e.enemyBehavior == EnemyAI.EnemyBehavior.Boss && SpawnerBossAI.instance != null && SpawnerBossAI.instance.readyToExplodeThisTurn) readyToBossAttack.Add(e);
                else if (e.enemyBehavior == EnemyAI.EnemyBehavior.TelegraphAoE && e.isChargingAttack) readyToAoEAttack.Add(e);
                else if (e.enemyBehavior == EnemyAI.EnemyBehavior.Melee && IsNeighbor(e.GetCurrentCellPosition(), player.GetCurrentCellPosition())) readyToMeleeAttack.Add(e);
                else if (e.enemyBehavior == EnemyAI.EnemyBehavior.Warlock)
                {
                    WarlockEnemyAI warlock = e.GetComponent<WarlockEnemyAI>();
                    if (warlock != null)
                    {
                        if (warlock.IsReadyToExplodeAttack1()) readyWarlockAttack1.Add(warlock);
                        if (warlock.IsReadyToExplodeAttack2()) readyWarlockAttack2.Add(warlock);
                    }
                }
            }
        }

        if (readyToBossAttack.Count > 0)
        {
            foreach (var boss in readyToBossAttack) yield return StartCoroutine(SpawnerBossAI.instance.ExecuteCheckerboardAoE());
            yield return new WaitForSeconds(0.2f);
        }

        // Warlock saldırı 1 (artı paterni)
        if (readyWarlockAttack1.Count > 0)
        {
            foreach (var warlock in readyWarlockAttack1) yield return StartCoroutine(warlock.ExecuteAttack1());
            yield return new WaitForSeconds(0.2f);
        }

        // Warlock saldırı 2 (çapraz paterni)
        if (readyWarlockAttack2.Count > 0)
        {
            foreach (var warlock in readyWarlockAttack2) yield return StartCoroutine(warlock.ExecuteAttack2());
            yield return new WaitForSeconds(0.2f);
        }

        // Melee saldırılar AoE'den önce: pozisyon değiştirdiği için önemli
        if (readyToMeleeAttack.Count > 0)
        {
            yield return StartCoroutine(EnemyAttackCoroutine(readyToMeleeAttack));
        }

        if (readyToAoEAttack.Count > 0)
        {
            foreach (var aoeEnemy in readyToAoEAttack) yield return StartCoroutine(aoeEnemy.ExecuteAoEAttackCoroutine(player));
            yield return new WaitForSeconds(0.2f);
        }

        TickThornTurns();
        EndTurnAndDecreaseStuns();
    }

    public void HideAllEnemyIntents() { foreach (var e in enemies) if (e != null) e.SetArrowVisibility(false); }
    public void ShowAllEnemyIntents() { foreach (var e in enemies) if (e != null && e.skipTurns <= 0) e.SetArrowVisibility(true); }

    public void ToggleFastMode()
    {
        if (RunManager.instance != null)
            RunManager.instance.fastMode = !RunManager.instance.fastMode;
    }
    public bool GetFastMode() => RunManager.instance != null && RunManager.instance.fastMode;

    public void ToggleSkipDiceVisuals()
    {
        skipDiceVisuals = !skipDiceVisuals;
    }
    public bool GetSkipDiceVisuals() => skipDiceVisuals;

    private IEnumerator MultiAttack(List<EnemyAI> targets)
    {
        bool hasBioMag = RunManager.instance.activePerks.Exists(p => p is BioMagnetismPerk);
        if (hasBioMag)
        {
            Vector3Int pCell = player.GetCurrentCellPosition();
            List<EnemyAI> pullTargets = new List<EnemyAI>();
            foreach (var e in enemies)
            {
                if (e != null && e.health.currentHP > 0 && DistanceCube(e.GetCurrentCellPosition(), pCell) == 2f) pullTargets.Add(e);
            }
            bool anyonePulled = false;
            if (pullTargets.Count > 0 && vacuumVfxPrefab != null) StartCoroutine(VacuumVFXCoroutine(player.transform.position));

            foreach (var e in pullTargets)
            {
                Vector3Int eCell = e.GetCurrentCellPosition();
                Vector3Int bestPullCell = eCell;
                Vector3Int[] pOffsets = (pCell.y % 2 != 0) ? evenOffsets : oddOffsets;
                foreach (var off in pOffsets)
                {
                    Vector3Int neighborToPlayer = pCell + off;
                    if (IsNeighbor(neighborToPlayer, eCell))
                    {
                        if (groundMap.HasTile(neighborToPlayer) && !IsEnemyAtCell(neighborToPlayer) && (LevelGenerator.instance == null || !LevelGenerator.instance.hazardCells.Contains(neighborToPlayer)))
                        {
                            bestPullCell = neighborToPlayer; break;
                        }
                    }
                }
                if (bestPullCell != eCell) { e.StartKnockbackMovement(bestPullCell); anyonePulled = true; }
            }
            if (anyonePulled)
            {
                yield return new WaitUntil(() => pullTargets.All(e => e == null || !e.IsMoving()));
                yield return new WaitForSeconds(0.1f);
                targets = GetAdjacentEnemies(pCell);
                var perk = RunManager.instance.activePerks.Find(p => p is BioMagnetismPerk);
                if (perk != null) perk.TriggerVisualPop();
            }
        }

        if (!skipDiceVisuals) yield return new WaitForSeconds(0.3f);
        List<int> currentRolls = new List<int>();
        int diceCount = RunManager.instance != null ? RunManager.instance.baseDiceCount : 2;
        int extraDices = 0;
        foreach (var p in RunManager.instance.activePerks) if (p is DormantSporePerk ambushPerk) { extraDices += ambushPerk.storedExtraDices; ambushPerk.storedExtraDices = 0; }
        if (RunManager.instance.bonusDiceNextCombat > 0) { extraDices += RunManager.instance.bonusDiceNextCombat; RunManager.instance.bonusDiceNextCombat = 0; }
        for (int i = 0; i < (diceCount + extraDices); i++) currentRolls.Add(Random.Range(1, 7));
        if (RunManager.instance != null) RunManager.instance.totalDiceRolled += diceCount + extraDices;
        CombatPayload payload = new CombatPayload(currentRolls);
        if (RunManager.instance != null && RunManager.instance.activePerks.Exists(p => p.GetType().Name == "SymbioticFuryPerk")) payload.multiplyInsteadOfAdd = true;
        if (!skipDiceVisuals && PerkListUI.instance != null) PerkListUI.instance.ForceOpen();
        isDiceAnimPlaying = true; skipDiceAnim = false;
        if (!skipDiceVisuals)
        {
            yield return StartCoroutine(ShowDiceSequence(currentRolls));
            UpdateTotalDamageDisplay(payload.GetFinalDamage());
            yield return StartCoroutine(SkippableWait(0.5f));
        }

        if (RunManager.instance != null && RunManager.instance.activePerks.Count > 0)
        {
            List<BasePerk> perksToProcess = RunManager.instance.activePerks.FindAll(p => p != null && !p.isDisabled);
            perksToProcess.Sort((a, b) => { int rerollOrder = b.isRerollPerk.CompareTo(a.isRerollPerk); return rerollOrder != 0 ? rerollOrder : a.priority.CompareTo(b.priority); });
            foreach (BasePerk perk in perksToProcess)
            {
                int beforeTotal = payload.GetFinalDamage(); perk.ModifyCombat(payload);
                bool anyDieChanged = false; List<int> changedIndices = new List<int>();
                for (int i = 0; i < currentRolls.Count; i++) if (currentRolls[i] != payload.diceRolls[i]) changedIndices.Add(i);
                if (changedIndices.Count > 0)
                {
                    if (!skipDiceVisuals)
                    {
                        if (perk.isRerollPerk)
                        {
                            foreach (int idx in changedIndices)
                            {
                                if (idx < spawnedDiceUI.Count)
                                {
                                    Animator dieAnim = spawnedDiceUI[idx].GetComponent<Animator>(); TMP_Text dieText = spawnedDiceUI[idx].GetComponentInChildren<TMP_Text>();
                                    if (dieAnim != null) dieAnim.enabled = true; if (dieText != null) dieText.text = "!";
                                }
                            }
                            yield return StartCoroutine(SkippableWait(0.5f));
                            foreach (int idx in changedIndices)
                            {
                                currentRolls[idx] = payload.diceRolls[idx];
                                if (idx < spawnedDiceUI.Count) { Animator dieAnim = spawnedDiceUI[idx].GetComponent<Animator>(); if (dieAnim != null) dieAnim.enabled = false; }
                                AnimateSpecificDie(idx, currentRolls[idx]);
                            }
                        }
                        else
                        {
                            foreach (int idx in changedIndices) { currentRolls[idx] = payload.diceRolls[idx]; AnimateSpecificDie(idx, currentRolls[idx]); }
                        }
                        anyDieChanged = true; yield return StartCoroutine(SkippableWait(0.3f));
                    }
                    else
                    {
                        foreach (int idx in changedIndices) { currentRolls[idx] = payload.diceRolls[idx]; }
                        anyDieChanged = true;
                    }
                }
                int afterTotal = payload.GetFinalDamage();
                if (beforeTotal != afterTotal || anyDieChanged)
                {
                    if (!skipDiceVisuals)
                    {
                        perk.TriggerVisualPop(); if (PerkListUI.instance != null) PerkListUI.instance.TriggerShakeForPerk(perk); UpdateTotalDamageDisplay(afterTotal); yield return StartCoroutine(SkippableWait(0.3f));
                    }
                }
            }
        }

        if (!skipDiceVisuals && PerkListUI.instance != null) PerkListUI.instance.ForceClose();

        if (!skipDiceVisuals && Random.value < RunManager.instance.criticalChance)
        {
            payload.isCriticalHit = true; UpdateTotalDamageDisplay(payload.GetFinalDamage());
            if (criticalText != null) StartCoroutine(CriticalTextPopAnimation()); yield return StartCoroutine(SkippableWait(0.5f));
        }
        else if (skipDiceVisuals && Random.value < RunManager.instance.criticalChance)
        {
            payload.isCriticalHit = true;
        }

        // OverClok: zar gizlenmeden önce 2x hasarı göster
        int finalDamage = payload.GetFinalDamage();
        if (RunManager.instance.doubleDamageNextCombat)
        {
            finalDamage *= 2;
            RunManager.instance.doubleDamageNextCombat = false;
            if (!skipDiceVisuals)
            {
                UpdateTotalDamageDisplay(finalDamage);
                yield return StartCoroutine(SkippableWait(0.5f));
            }
        }

        if (!skipDiceVisuals)
        {
            yield return StartCoroutine(SkippableWait(0.4f));
            HideDiceResults();
        }
        isDiceAnimPlaying = false; skipDiceAnim = false;

        int damagePerEnemy = 0;
        if (targets.Count > 0)
        {
            if (RunManager.instance.cleaveNextCombat) { damagePerEnemy = finalDamage; RunManager.instance.cleaveNextCombat = false; }
            else { damagePerEnemy = finalDamage / targets.Count; }
        }

        if (player != null) player.TriggerAttackAnimation();
        if (AudioManager.instance != null) AudioManager.instance.PlaySwing();
        yield return new WaitForSeconds(0.3f);
        if (AudioManager.instance != null) AudioManager.instance.PlayHit();
        isAttackAnimationPlaying = false;
        hexesMovedThisTurn = 0;

        List<EnemyAI> knockedEnemies = new List<EnemyAI>(); List<EnemyAI> deadEnemiesThisTurn = new List<EnemyAI>();

        var voodooPerk = RunManager.instance.activePerks.Find(p => p is VoodooParasitePerk) as VoodooParasitePerk;

        foreach (var enemy in targets)
        {
            if (enemy == null) continue;
            bool dies = enemy.health.currentHP <= damagePerEnemy;
            enemy.health.TakeDamage(damagePerEnemy);
            RegisterComboHit();
            knockedEnemies.Add(enemy); if (dies) deadEnemiesThisTurn.Add(enemy);

            // ========================================================
            // VOODOO PARASITE GÜNCELLEMESİ (Canı en çok olana vurma)
            // ========================================================
            if (voodooPerk != null && enemies.Count > 1)
            {
                // Hayatta olanları candan (büyükten küçüğe) sıralayıp liste haline getir
                var others = enemies.Where(e => e != null && e != enemy && e.health.currentHP > 0
                                            && e.enemyBehavior != EnemyAI.EnemyBehavior.Boss)
                                    .OrderByDescending(e => e.health.currentHP)
                                    .ToList();

                int voodooHits = Mathf.Min(voodooPerk.currentLevel, others.Count);
                for (int v = 0; v < voodooHits; v++)
                {
                    others[v].health.TakeDamage(damagePerEnemy);
                }
                if (voodooHits > 0) voodooPerk.TriggerVisualPop();
            }
        }

        foreach (var e in knockedEnemies)
        {
            if (e == null) continue;
            Vector3Int rawTargetCell = GetRawOppositeCell(e.GetCurrentCellPosition(), player.GetCurrentCellPosition());
            EnemyAI enemyBehind = GetEnemyAtCell(rawTargetCell);

            if (enemyBehind != null)
            {
                e.ApplyStun(2, true); enemyBehind.ApplyStun(2, true);
                Vector3 cPos = groundMap.GetCellCenterWorld(e.GetCurrentCellPosition()); Vector3 pPos = groundMap.GetCellCenterWorld(player.GetCurrentCellPosition());
                cPos.z = 0; pPos.z = 0; Vector3 bumpDir = (cPos - pPos).normalized;
                e.StartWallBump(bumpDir); enemyBehind.StartWallBump(bumpDir);
            }
            else if (!groundMap.HasTile(rawTargetCell) || player.GetCurrentCellPosition() == rawTargetCell)
            {
                e.ApplyStun(2, true);
                Vector3 cPos = groundMap.GetCellCenterWorld(e.GetCurrentCellPosition()); Vector3 pPos = groundMap.GetCellCenterWorld(player.GetCurrentCellPosition());
                cPos.z = 0; pPos.z = 0; e.StartWallBump((cPos - pPos).normalized);
            }
            else
            {
                e.ApplyStun(1, false); e.StartKnockbackMovement(rawTargetCell);
            }
        }

        var recoilPerk = RunManager.instance.activePerks.Find(p => p is RecoilSpringPerk) as RecoilSpringPerk;
        bool didRecoil = false;
        if (recoilPerk != null && targets.Count > 0)
        {
            Vector3Int playerOriginal = player.GetCurrentCellPosition();
            Vector3 playerWorld = groundMap.GetCellCenterWorld(playerOriginal); playerWorld.z = 0;

            // Tüm hedeflerin yönlerini topla, ortalama vektör bul
            Vector2 avgDir = Vector2.zero;
            foreach (var t in targets)
            {
                if (t == null) continue;
                Vector3 eWorld = groundMap.GetCellCenterWorld(t.GetCurrentCellPosition()); eWorld.z = 0;
                avgDir += (Vector2)(eWorld - playerWorld).normalized;
            }

            Vector3Int bounceTo = playerOriginal;
            if (avgDir.sqrMagnitude > 0.001f)
            {
                // Ortalama vektörün tam tersine en yakın hex komşusunu bul
                Vector2 recoilDir = -avgDir.normalized;
                Vector3Int[] offsets = (playerOriginal.y % 2 != 0) ? evenOffsets : oddOffsets;
                float bestDot = -2f;
                foreach (var off in offsets)
                {
                    Vector3Int neighbor = playerOriginal + off;
                    if (!groundMap.HasTile(neighbor) || IsEnemyAtCell(neighbor) || neighbor == player.GetCurrentCellPosition()) continue;
                    Vector3 nWorld = groundMap.GetCellCenterWorld(neighbor); nWorld.z = 0;
                    Vector2 nDir = (Vector2)(nWorld - playerWorld).normalized;
                    float dot = Vector2.Dot(recoilDir, nDir);
                    if (dot > bestDot) { bestDot = dot; bounceTo = neighbor; }
                }
            }

            if (bounceTo != playerOriginal)
            {
                recoilPerk.TriggerVisualPop();
                player.StartKnockbackMovement(bounceTo, true);
                didRecoil = true;
            }
        }

        yield return new WaitUntil(() => { foreach (var e in knockedEnemies) if (e != null && e.IsMoving()) return false; if (didRecoil && player.IsMoving()) return false; return true; });

        foreach (var e in knockedEnemies) if (e != null && payload.triggerExplosion) TriggerExplosion(e.GetCurrentCellPosition(), payload.explosionDamagePercent, false);

        List<EnemyAI> deadFromSpikes = enemies.FindAll(e => e != null && e.health.currentHP <= 0);
        foreach (var deadEnemy in deadFromSpikes)
        {
            if (deadEnemy.enemyBehavior != EnemyAI.EnemyBehavior.Totem)
            {
                bool isBossRoom = RunManager.instance.currentLevel % 5 == 0;
                int coinDrop = 0;
                if (isBossRoom) { if (deadEnemy.enemyBehavior == EnemyAI.EnemyBehavior.Boss) coinDrop = 20; }
                else { coinDrop = Random.Range(1, 4) + RunManager.instance.bonusGold; if (RunManager.instance.doubleGoldNextKill) { coinDrop *= 2; RunManager.instance.doubleGoldNextKill = false; } }
                if (coinDrop > 0) { RunManager.instance.currentGold += coinDrop; RunManager.instance.totalGoldEarned += coinDrop; if (CoinDropVFX.instance != null) CoinDropVFX.instance.SpawnCoins(deadEnemy.transform.position, coinDrop); }
            }
            foreach (var p in RunManager.instance.activePerks) if (!p.isDisabled) p.OnEnemyKilled(deadEnemy);
            RunManager.instance.totalEnemiesKilled++;
        }
        UpdateCoinUI(); enemies.RemoveAll(e => e == null || e.health.currentHP <= 0);

        if (enemies.Count <= 0) { ClearWarningMap(); StartCoroutine(WaitAndTriggerLevelClear()); yield break; }

        List<EnemyAI> spikedEnemies = new List<EnemyAI>(); bool anyoneBounced = false;
        foreach (var s in knockedEnemies) if (s != null && LevelGenerator.instance.hazardCells.Contains(s.GetCurrentCellPosition())) spikedEnemies.Add(s);

        if (spikedEnemies.Count > 0)
        {
            yield return new WaitForSeconds(0.2f);
            foreach (var s in spikedEnemies) { StartCoroutine(FlashHazardTileCoroutine(s.GetCurrentCellPosition())); s.health.TakeDamage(Mathf.Max(1, s.health.maxHP / 2)); }

            var acidPerk = RunManager.instance.activePerks.Find(p => p is AcidBloodPerk) as AcidBloodPerk;
            if (acidPerk != null) { player.health.Heal(spikedEnemies.Count * acidPerk.currentLevel); acidPerk.TriggerVisualPop(); }

            yield return new WaitForSeconds(0.2f);
            foreach (var s in spikedEnemies) if (s != null && s.health.currentHP > 0) { Vector3Int randomBounceCell = GetRandomSafeNeighbor(s.GetCurrentCellPosition()); s.StartKnockbackMovement(randomBounceCell); anyoneBounced = true; }
        }

        if (anyoneBounced) yield return new WaitUntil(() => { foreach (var s in spikedEnemies) if (s != null && s.IsMoving()) return false; return true; });

        deadFromSpikes = enemies.FindAll(e => e != null && e.health.currentHP <= 0);
        foreach (var deadEnemy in deadFromSpikes)
        {
            if (deadEnemy.enemyBehavior != EnemyAI.EnemyBehavior.Totem)
            {
                bool isBossRoom = RunManager.instance.currentLevel % 5 == 0;
                int coinDrop = 0;
                if (isBossRoom) { if (deadEnemy.enemyBehavior == EnemyAI.EnemyBehavior.Boss) coinDrop = 20; }
                else { coinDrop = Random.Range(1, 4) + RunManager.instance.bonusGold; if (RunManager.instance.doubleGoldNextKill) { coinDrop *= 2; RunManager.instance.doubleGoldNextKill = false; } }
                if (coinDrop > 0) { RunManager.instance.currentGold += coinDrop; RunManager.instance.totalGoldEarned += coinDrop; if (CoinDropVFX.instance != null) CoinDropVFX.instance.SpawnCoins(deadEnemy.transform.position, coinDrop); }
            }
            foreach (var p in RunManager.instance.activePerks) if (!p.isDisabled) p.OnEnemyKilled(deadEnemy);
            RunManager.instance.totalEnemiesKilled++;
        }
        UpdateCoinUI(); enemies.RemoveAll(e => e == null || e.health.currentHP <= 0);

        if (enemies.Count <= 0) { ClearWarningMap(); StartCoroutine(WaitAndTriggerLevelClear()); yield break; }

        // Recoil Spring: oyuncu spike'a düştüyse hasar + geri itme
        if (didRecoil && LevelGenerator.instance != null && LevelGenerator.instance.hazardCells.Contains(player.GetCurrentCellPosition()))
        {
            yield return new WaitForSeconds(0.15f);
            StartCoroutine(FlashHazardTileCoroutine(player.GetCurrentCellPosition()));
            if (RunManager.instance.hasBioBarrier)
            {
                foreach (var perk in RunManager.instance.activePerks) if (perk is BioBarrierPerk aegis) { aegis.BreakShield(); break; }
                RunManager.instance.hasBioBarrier = false;
            }
            else PlayerTakeDamage(1);

            Vector3Int safeCell = GetSafeNeighbor(player.GetCurrentCellPosition());
            player.StartKnockbackMovement(safeCell);
            yield return new WaitUntil(() => !player.IsMoving());
        }

        if (didRecoil)
        {
            List<EnemyAI> allAdjacent = GetAdjacentEnemies(player.GetCurrentCellPosition());
            List<EnemyAI> nextTargets = allAdjacent.Where(e => e != null && e.health.currentHP > 0).ToList();
            if (nextTargets.Count > 0) yield return StartCoroutine(MultiAttack(nextTargets));
        }
        finalDamage = payload.GetFinalDamage();
        RunManager.instance.totalDamageDealt += finalDamage; // Toplam hasarı ekle
    }

    private IEnumerator EnemyAttackCoroutine(List<EnemyAI> attackers)
    {
        attackers.RemoveAll(a => a.skipTurns > 0);
        if (attackers.Count == 0) { yield break; }

        yield return new WaitForSeconds(0.2f);
        bool dodged = false;
        if (RunManager.instance != null) dodged = Random.value < RunManager.instance.dodgeChance;

        if (dodged)
        {
            // DODGE EFEKTİNİ COROUTINE İLE ÇAĞIRIYORUZ (Kalkan Kırılma Animasyonu)
            StartCoroutine(AnimateShieldBreakFX(player.transform.position));
        }
        else if (RunManager.instance.hasBioBarrier)
        {
            foreach (var perk in RunManager.instance.activePerks) if (perk is BioBarrierPerk aegis) { aegis.BreakShield(); break; }
            RunManager.instance.hasBioBarrier = false;
        }
        else PlayerTakeDamage(1);

        Vector3Int playerOriginalCell = player.GetCurrentCellPosition();
        Vector3Int playerTarget = GetOppositeCell(playerOriginalCell, attackers[0].GetCurrentCellPosition());
        player.StartKnockbackMovement(playerTarget);
        yield return new WaitUntil(() => !player.IsMoving());

        if (LevelGenerator.instance != null && LevelGenerator.instance.hazardCells.Contains(player.GetCurrentCellPosition()))
        {
            yield return new WaitForSeconds(0.4f);
            if (RunManager.instance.hasBioBarrier)
            {
                foreach (var perk in RunManager.instance.activePerks) if (perk is BioBarrierPerk aegis) { aegis.BreakShield(); break; }
                RunManager.instance.hasBioBarrier = false;
            }
            else PlayerTakeDamage(1);

            StartCoroutine(FlashHazardTileCoroutine(player.GetCurrentCellPosition()));
            player.StartKnockbackMovement(playerOriginalCell);
            yield return new WaitUntil(() => !player.IsMoving());
        }
        yield return new WaitForSeconds(0.3f);
    }

    private void EndTurnAndDecreaseStuns()
    {
        if (player != null && player.health.currentHP > 0)
        {
            foreach (var e in enemies)
            {
                if (e != null) e.DecreaseStunTurn();
            }
            StartPlayerTurn();
        }
        RunManager.instance.totalTurnsPlayed++; // Tur sayısını arttır
    }

    public void ResumeAfterShop() { StartPlayerTurn(); }

    private IEnumerator ShowDiceSequence(List<int> rolls)
    {
        // skipDiceVisuals aktif ise zarları tamamen gizle
        if (skipDiceVisuals)
        {
            foreach (var die in spawnedDiceUI) Destroy(die);
            spawnedDiceUI.Clear();
            if (dicePanelBackground != null) dicePanelBackground.gameObject.SetActive(false);
            if (totalDamageText != null) totalDamageText.gameObject.SetActive(false);
            yield break;
        }

        foreach (var die in spawnedDiceUI) Destroy(die); spawnedDiceUI.Clear();
        if (totalDamageText != null)
        {
            totalDamageText.gameObject.SetActive(true); totalDamageText.text = "0";
            Color tc = totalDamageText.color; tc.a = 0f; totalDamageText.color = tc;
        }

        List<CanvasGroup> dieGroups = new List<CanvasGroup>(); List<Animator> dieAnimators = new List<Animator>(); List<TMP_Text> dieTexts = new List<TMP_Text>(); List<Image> dieImages = new List<Image>();
        for (int i = 0; i < rolls.Count; i++)
        {
            GameObject newDie = Instantiate(dieUIPrefab, diceUIContainer); spawnedDiceUI.Add(newDie);
            // CanvasGroup — Animator kendi animasyonunu oynatır, biz sadece alpha'yı yönetiriz
            CanvasGroup cg = newDie.GetComponent<CanvasGroup>(); if (cg == null) cg = newDie.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            // Text'i roll boyunca gizle
            TMP_Text txt = newDie.GetComponentInChildren<TMP_Text>();
            if (txt != null) { Color tc = txt.color; tc.a = 0f; txt.color = tc; txt.text = ""; }
            dieGroups.Add(cg); dieAnimators.Add(newDie.GetComponent<Animator>()); dieTexts.Add(txt); dieImages.Add(newDie.GetComponent<Image>());
        }

        if (dicePanelBackground != null)
        {
            dicePanelBackground.gameObject.SetActive(true);
            float e = 0f; Color c = dicePanelBackground.color; c.a = 0f; dicePanelBackground.color = c;
            while (e < 0.2f && !skipDiceAnim) { e += Time.deltaTime; c.a = Mathf.Lerp(0f, 0.9f, e / 0.2f); dicePanelBackground.color = c; yield return null; }
            c.a = 0.9f; dicePanelBackground.color = c;
        }

        // Animator kendi idle animasyonunu oynatırken CanvasGroup ile fade in
        if (AudioManager.instance != null) AudioManager.instance.PlayDiceRoll();
        float rollTime = 0.4f; float rollElapsed = 0f;
        while (rollElapsed < rollTime && !skipDiceAnim)
        {
            rollElapsed += Time.deltaTime;
            float a = Mathf.Clamp01(rollElapsed / (rollTime * 0.5f));
            foreach (var cg in dieGroups) cg.alpha = a;
            yield return null;
        }
        foreach (var cg in dieGroups) cg.alpha = 1f;

        // Animator'ı kapat, final sprite + text fade in ile göster
        if (AudioManager.instance != null) AudioManager.instance.PlayDiceHit();
        for (int i = 0; i < rolls.Count; i++)
        {
            if (dieAnimators[i] != null) dieAnimators[i].enabled = false;
            dieImages[i].sprite = diceSprites[rolls[i] - 1];
            dieTexts[i].text = rolls[i].ToString();
            if (!skipDiceAnim) StartCoroutine(TextFadeInAndPopDelayed(dieTexts[i], i * 0.08f));
            else { Color tc = dieTexts[i].color; tc.a = 1f; dieTexts[i].color = tc; dieTexts[i].transform.localScale = Vector3.one; }
        }
        if (!skipDiceAnim) yield return new WaitForSeconds(0.2f + rolls.Count * 0.08f);
    }

    private IEnumerator TextFadeInAndPopDelayed(TMP_Text txt, float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);
        yield return StartCoroutine(TextFadeInAndPop(txt));
    }

    private IEnumerator TextFadeInAndPop(TMP_Text txt)
    {
        if (txt == null) yield break;
        if (AudioManager.instance != null) AudioManager.instance.PlayTextEffect();
        float duration = 0.15f; float elapsed = 0f;
        Transform t = txt.transform; Vector3 startScale = new Vector3(2f, 2f, 2f);
        t.localScale = startScale;
        while (elapsed < duration)
        {
            float progress = elapsed / duration;
            float a = Mathf.Lerp(0f, 1f, progress);
            Color c = txt.color; c.a = a; txt.color = c;
            t.localScale = Vector3.Lerp(startScale, Vector3.one, 1f - (1f - progress) * (1f - progress));
            elapsed += Time.deltaTime; yield return null;
        }
        Color fc = txt.color; fc.a = 1f; txt.color = fc;
        t.localScale = Vector3.one;
    }

    private IEnumerator DiceFadeIn(Image img, TMP_Text txt)
    {
        float duration = 0.2f; float elapsed = 0f;
        while (elapsed < duration)
        {
            float a = Mathf.Lerp(0f, 1f, elapsed / duration);
            if (img != null) { Color c = img.color; c.a = a; img.color = c; }
            if (txt != null) { Color c = txt.color; c.a = a; txt.color = c; }
            elapsed += Time.deltaTime; yield return null;
        }
        if (img != null) { Color c = img.color; c.a = 1f; img.color = c; }
        if (txt != null) { Color c = txt.color; c.a = 1f; txt.color = c; }
    }

    private IEnumerator TextPopAnimation(TMP_Text textElement)
    {
        if (textElement == null) yield break;
        if (AudioManager.instance != null) AudioManager.instance.PlayTextEffect();
        Transform t = textElement.transform; Vector3 startScale = new Vector3(3f, 3f, 3f); Vector3 endScale = Vector3.one;
        float duration = 0.15f; float elapsed = 0f;
        while (elapsed < duration)
        {
            float tParam = elapsed / duration; tParam = 1f - (1f - tParam) * (1f - tParam);
            t.localScale = Vector3.Lerp(startScale, endScale, tParam); elapsed += Time.deltaTime; yield return null;
        }
        t.localScale = endScale;
    }

    public void UpdateTotalDamageDisplay(int val)
    {
        if (totalDamageText == null) return;
        totalDamageText.gameObject.SetActive(true); totalDamageText.text = val.ToString();
        StartCoroutine(TotalTextFadeIn(totalDamageText));
        StopCoroutine("TextPopAnimation"); StartCoroutine(TextPopAnimation(totalDamageText));
    }

    private IEnumerator TotalTextFadeIn(TMP_Text txt)
    {
        if (txt == null) yield break;
        float duration = 0.2f; float elapsed = 0f;
        Color startColor = txt.color; startColor.a = 0f; txt.color = startColor;
        while (elapsed < duration)
        {
            Color c = txt.color; c.a = Mathf.Lerp(0f, 1f, elapsed / duration); txt.color = c;
            elapsed += Time.deltaTime; yield return null;
        }
        Color fc = txt.color; fc.a = 1f; txt.color = fc;
    }

    public void HideDiceResults()
    {
        StartCoroutine(FadeOutAndHideDice());
    }

    private IEnumerator FadeOutAndHideDice()
    {
        float duration = 0.25f; float elapsed = 0f;
        // CanvasGroup ile dice fade out
        List<CanvasGroup> dieGroups = new List<CanvasGroup>();
        foreach (var die in spawnedDiceUI)
        {
            if (die == null) continue;
            CanvasGroup cg = die.GetComponent<CanvasGroup>(); if (cg == null) cg = die.AddComponent<CanvasGroup>();
            dieGroups.Add(cg);
        }
        float totalStartA = totalDamageText != null ? totalDamageText.color.a : 0f;
        float critStartA = 0f;
        Image critImage = criticalText != null ? criticalText.GetComponent<Image>() : null;
        TMP_Text critTMP = criticalText != null ? criticalText.GetComponentInChildren<TMP_Text>() : null;
        if (critImage != null) critStartA = critImage.color.a;
        else if (critTMP != null) critStartA = critTMP.color.a;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            foreach (var cg in dieGroups) { if (cg != null) cg.alpha = Mathf.Lerp(1f, 0f, t); }
            if (totalDamageText != null) { Color c = totalDamageText.color; c.a = Mathf.Lerp(totalStartA, 0f, t); totalDamageText.color = c; }
            if (critImage != null) { Color c = critImage.color; c.a = Mathf.Lerp(critStartA, 0f, t); critImage.color = c; }
            if (critTMP != null) { Color c = critTMP.color; c.a = Mathf.Lerp(critStartA, 0f, t); critTMP.color = c; }
            elapsed += Time.deltaTime; yield return null;
        }

        foreach (var die in spawnedDiceUI) { if (die != null) Destroy(die); } spawnedDiceUI.Clear();
        if (totalDamageText != null) totalDamageText.gameObject.SetActive(false);
        if (criticalText != null) criticalText.gameObject.SetActive(false);
        if (dicePanelBackground != null) StartCoroutine(FadeDicePanel());
    }

    private IEnumerator FadeDicePanel()
    {
        if (dicePanelBackground == null) yield break;
        float e = 0f; Color c = dicePanelBackground.color;
        float startA = 0.9f;
        while (e < 0.2f) { e += Time.deltaTime; c.a = Mathf.Lerp(startA, 0f, e / 0.2f); dicePanelBackground.color = c; yield return null; }
        c.a = 0f; dicePanelBackground.color = c;
        dicePanelBackground.gameObject.SetActive(false);
    }

    private IEnumerator CriticalTextPopAnimation()
    {
        if (criticalText == null) yield break;
        criticalText.gameObject.SetActive(true); Transform t = criticalText.transform;
        // Alpha sıfırla
        Image critImg = criticalText.GetComponent<Image>(); TMP_Text critTMP = criticalText.GetComponentInChildren<TMP_Text>();
        if (critImg != null) { Color c = critImg.color; c.a = 0f; critImg.color = c; }
        if (critTMP != null) { Color c = critTMP.color; c.a = 0f; critTMP.color = c; }

        Vector3 startScale = new Vector3(0.2f, 0.2f, 0.2f); Vector3 overshootScale = new Vector3(0.6f, 0.6f, 0.6f); Vector3 endScale = new Vector3(0.5f, 0.5f, 0.5f);
        float elapsed = 0f; float popDuration = 0.1f;
        while (elapsed < popDuration)
        {
            float prog = elapsed / popDuration;
            t.localScale = Vector3.Lerp(startScale, overshootScale, prog);
            if (critImg != null) { Color c = critImg.color; c.a = Mathf.Lerp(0f, 1f, prog); critImg.color = c; }
            if (critTMP != null) { Color c = critTMP.color; c.a = Mathf.Lerp(0f, 1f, prog); critTMP.color = c; }
            elapsed += Time.deltaTime; yield return null;
        }
        elapsed = 0f; float settleDuration = 0.1f;
        while (elapsed < settleDuration) { t.localScale = Vector3.Lerp(overshootScale, endScale, elapsed / settleDuration); elapsed += Time.deltaTime; yield return null; }
        t.localScale = endScale;
        if (critImg != null) { Color c = critImg.color; c.a = 1f; critImg.color = c; }
        if (critTMP != null) { Color c = critTMP.color; c.a = 1f; critTMP.color = c; }
    }

    // ── Combo Sistemi ────────────────────────────────────────────────────
    public void RegisterComboHit()
    {
        comboCount++;
        if (comboCount >= 2) ShowCombo(comboCount);
    }

    public void ResetCombo()
    {
        comboCount = 0;
        if (comboTextObj != null) comboTextObj.SetActive(false);
        if (comboFadeCoroutine != null) { StopCoroutine(comboFadeCoroutine); comboFadeCoroutine = null; }
    }

    private void ShowCombo(int count)
    {
        if (comboTextObj == null) return;
        var tmp = comboTextObj.GetComponentInChildren<TMP_Text>();
        if (tmp != null) tmp.text = $"x{count} COMBO!";
        comboTextObj.SetActive(true);
        if (comboFadeCoroutine != null) StopCoroutine(comboFadeCoroutine);
        comboFadeCoroutine = StartCoroutine(ComboPopAndFade());
    }

    private IEnumerator ComboPopAndFade()
    {
        Transform t = comboTextObj.transform;
        // Pop animasyonu
        Vector3 start = new Vector3(0.3f, 0.3f, 0.3f);
        Vector3 over  = new Vector3(0.65f, 0.65f, 0.65f);
        Vector3 end   = new Vector3(0.5f, 0.5f, 0.5f);
        float elapsed = 0f;
        while (elapsed < 0.1f) { t.localScale = Vector3.Lerp(start, over, elapsed / 0.1f); elapsed += Time.unscaledDeltaTime; yield return null; }
        elapsed = 0f;
        while (elapsed < 0.1f) { t.localScale = Vector3.Lerp(over, end, elapsed / 0.1f); elapsed += Time.unscaledDeltaTime; yield return null; }
        t.localScale = end;

        // 1.2 sn sonra fade out
        yield return new WaitForSecondsRealtime(1.2f);
        var tmp = comboTextObj.GetComponentInChildren<TMP_Text>();
        if (tmp == null) { comboTextObj.SetActive(false); yield break; }
        Color c = tmp.color; float fadeDur = 0.3f; elapsed = 0f;
        while (elapsed < fadeDur)
        {
            elapsed += Time.unscaledDeltaTime;
            c.a = Mathf.Lerp(1f, 0f, elapsed / fadeDur);
            tmp.color = c;
            yield return null;
        }
        c.a = 1f; tmp.color = c;
        comboTextObj.SetActive(false);
    }

    public bool IsEnemyAtCell(Vector3Int cell)
    {
        foreach (var e in enemies) if (e != null && e.health.currentHP > 0 && e.GetCurrentCellPosition() == cell) return true;
        return false;
    }

    public EnemyAI GetEnemyAtCell(Vector3Int cell)
    {
        foreach (var e in enemies) if (e != null && e.health.currentHP > 0 && e.GetCurrentCellPosition() == cell) return e;
        return null;
    }

    private Vector3Int GetRawOppositeCell(Vector3Int centerCell, Vector3Int awayFromCell)
    {
        Vector3Int[] offsets = (centerCell.y % 2 != 0) ? evenOffsets : oddOffsets;
        for (int i = 0; i < 6; i++) if (centerCell + offsets[i] == awayFromCell) return centerCell + offsets[(i + 3) % 6];
        return centerCell;
    }

    private List<EnemyAI> GetAdjacentEnemies(Vector3Int playerCell)
    {
        List<EnemyAI> adjacentList = new List<EnemyAI>();
        foreach (var enemy in enemies) if (enemy != null && enemy.health.currentHP > 0) if (IsNeighbor(playerCell, enemy.GetCurrentCellPosition())) adjacentList.Add(enemy);
        return adjacentList;
    }

    public Vector3Int GetOppositeCell(Vector3Int centerCell, Vector3Int awayFromCell)
    {
        Vector3Int[] offsets = (centerCell.y % 2 != 0) ? evenOffsets : oddOffsets;
        for (int i = 0; i < 6; i++)
        {
            if (centerCell + offsets[i] == awayFromCell)
            {
                int oppositeIndex = (i + 3) % 6; Vector3Int strictKnockbackCell = centerCell + offsets[oppositeIndex];
                if (!groundMap.HasTile(strictKnockbackCell) || IsEnemyAtCell(strictKnockbackCell) || player.GetCurrentCellPosition() == strictKnockbackCell) return centerCell;
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
            Transform dieTransform = spawnedDiceUI[index].transform; TMP_Text dieText = spawnedDiceUI[index].GetComponentInChildren<TMP_Text>(); Image dieImage = spawnedDiceUI[index].GetComponent<Image>();
            if (dieText != null) dieText.text = newValue.ToString();
            if (dieImage != null && diceSprites != null) { int spriteIndex = Mathf.Clamp(newValue - 1, 0, diceSprites.Length - 1); dieImage.sprite = diceSprites[spriteIndex]; }
            StartCoroutine(DiePopAnimation(dieTransform));
        }
    }

    private IEnumerator DiePopAnimation(Transform t)
    {
        Vector3 startScale = Vector3.one * 1.6f; float dur = 0.2f; float elapsed = 0f;
        while (elapsed < dur) { t.localScale = Vector3.Lerp(startScale, Vector3.one, elapsed / dur); elapsed += Time.deltaTime; yield return null; }
        t.localScale = Vector3.one;
    }

    private Vector3Int GetRandomSafeNeighbor(Vector3Int centerCell)
    {
        Vector3Int[] offsets = (centerCell.y % 2 != 0) ? evenOffsets : oddOffsets; List<Vector3Int> safeNeighbors = new List<Vector3Int>();
        foreach (var off in offsets)
        {
            Vector3Int neighbor = centerCell + off;
            if (groundMap.HasTile(neighbor) && !IsEnemyAtCell(neighbor) && player.GetCurrentCellPosition() != neighbor && !LevelGenerator.instance.hazardCells.Contains(neighbor)) safeNeighbors.Add(neighbor);
        }
        if (safeNeighbors.Count > 0) return safeNeighbors[Random.Range(0, safeNeighbors.Count)];
        return centerCell;
    }

    public float DistanceCube(Vector3Int a, Vector3Int b)
    {
        Vector3Int ac = OffsetToCube(a);
        Vector3Int bc = OffsetToCube(b);
        return (Mathf.Abs(ac.x - bc.x) + Mathf.Abs(ac.y - bc.y) + Mathf.Abs(ac.z - bc.z)) * 0.5f;
    }

    private Vector3Int OffsetToCube(Vector3Int o)
    {
        int x = o.x - (o.y - (o.y & 1)) / 2; int z = o.y; int y = -x - z;
        return new Vector3Int(x, y, z);
    }

    private IEnumerator FlashHazardTileCoroutine(Vector3Int cell)
    {
        Tilemap targetMap = LevelGenerator.instance.hazardMap;
        if (targetMap == null) yield break;

        targetMap.SetTileFlags(cell, TileFlags.None);
        Color originalColor = Color.white;
        Color flashColor = new Color(0.2f, 1f, 0.2f, 1f);
        float duration = 0.15f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            targetMap.SetColor(cell, Color.Lerp(originalColor, flashColor, elapsed / duration));
            yield return null;
        }
        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            targetMap.SetColor(cell, Color.Lerp(flashColor, originalColor, elapsed / duration));
            yield return null;
        }
        targetMap.SetColor(cell, originalColor);
    }

    public void DrawWarningTile(Vector3Int cell)
    {
        if (warningMap == null || warningTile == null) return;
        StartCoroutine(SmoothWarningFadeIn(cell));
    }

    private IEnumerator SmoothWarningFadeIn(Vector3Int cell)
    {
        warningMap.SetTile(cell, warningTile); warningMap.SetTileFlags(cell, TileFlags.None);
        Color startColor = new Color(1f, 1f, 1f, 0f); Color endColor = new Color(1f, 1f, 1f, 0.5f);
        warningMap.SetColor(cell, startColor);
        float duration = 0.3f; float elapsed = 0f;
        while (elapsed < duration)
        {
            if (!warningMap.HasTile(cell)) yield break;
            elapsed += Time.deltaTime; float t = elapsed / duration; t = t * t * (3f - 2f * t);
            warningMap.SetColor(cell, Color.Lerp(startColor, endColor, t)); yield return null;
        }
        if (warningMap.HasTile(cell)) warningMap.SetColor(cell, endColor);
    }
}
