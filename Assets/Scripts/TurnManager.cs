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
    public GameObject criticalText;

    [Header("Coin UI")]
    public TMP_Text coinText; // Coin sayısını göstermek için UI text

    // Sahnede o an var olan görsel zarların listesi
    private List<GameObject> spawnedDiceUI = new List<GameObject>();

    public List<EnemyAI> enemies = new List<EnemyAI>();
    public bool isPlayerTurn = true;

    private List<EnemyAI> stunnedEnemiesThisTurn = new List<EnemyAI>();

    [Header("Shop Turu")]


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
        UpdateCoinUI();
        Invoke("LockAllEnemyIntents", 0.5f);
    }

    public void UpdateCoinUI()
    {
        if (coinText != null && RunManager.instance != null)
            coinText.text = "Coins: " + RunManager.instance.currentGold;
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
                // YENİ: Eğer skipTurns 0'dan büyükse niyetini kilitler, ok çıkaramaz!
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
        // --- YENİ: TUZAK KONTROLÜ (GECİKMELİ VE PARLAMALI) ---
        if (LevelGenerator.instance.hazardCells.Contains(playerCell))
        {
            Debug.Log("🔥 Tuzağa bastın!");

            // 1. Karakter dikene ulaştı, durumu algılaması için çok kısa bekle
            yield return new WaitForSeconds(0.15f);

            // 2. Diken yumuşakça parlasın (Aynı anda hasar yesin)
            StartCoroutine(FlashHazardTileCoroutine(playerCell));
            // Hasar yeme kontrolü (EnemyAttackCoroutine veya Diken kontrolü)
            if (RunManager.instance.hasHolyAegis)
            {
                // Önce kalkanı bul ve patlat!
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

            yield return new WaitForSeconds(0.15f); // Parlamanın bitişini çok az bekle

            // 3. Etrafındaki güvenli bir komşuya can havliyle seker
            Vector3Int bounceCell = GetSafeNeighbor(playerCell);
            player.StartKnockbackMovement(bounceCell);

            // Geri sekme animasyonunun bitmesini bekle
            yield return new WaitUntil(() => !player.IsMoving());

            // Tuzağa bastığı için saldırı yapamaz, sıra direkt düşmana veya diğer hamleye geçer
            if (RunManager.instance.remainingMoves > 0 && player != null && player.health.currentHP > 0)
            {
                RunManager.instance.remainingMoves--;
                isPlayerTurn = true;
                player.UpdateHighlights();
            }
            else if (player != null && player.health.currentHP > 0)
            {
                StartCoroutine(EnemyPhase());
            }
            yield break; // Alt satırlardaki MultiAttack kısmına inmesini engeller
        }

        // --- ESKİ NORMAL SALDIRI KISMI ---
        List<EnemyAI> adjacentEnemies = GetAdjacentEnemies(player.GetCurrentCellPosition());
        if (adjacentEnemies.Count > 0)
        {
            yield return StartCoroutine(MultiAttack(adjacentEnemies));
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

    // --- YARDIMCI METOT: TUZAKTAN SEKECEK GÜVENLİ YER BULMA ---
    // Bunu TurnManager.cs'in en alt kısımlarına (diğer yardımcı metodların yanına) ekle
    private Vector3Int GetSafeNeighbor(Vector3Int centerCell)
    {
        Vector3Int[] offsets = (centerCell.y % 2 != 0) ? evenOffsets : oddOffsets;
        foreach (var off in offsets)
        {
            Vector3Int neighbor = centerCell + off;
            // Zemin var mı? Düşman yok mu? Tuzak değil mi?
            if (groundMap.HasTile(neighbor) && !IsEnemyAtCell(neighbor) && !LevelGenerator.instance.hazardCells.Contains(neighbor))
            {
                return neighbor;
            }
        }
        return centerCell; // Eğer etrafı tamamen sarılıysa olduğu yerde kalır mecbur
    }

    private IEnumerator EnemyPhase()
    {
        yield return new WaitForSeconds(0.2f);
        enemies.RemoveAll(e => e == null || e.health.currentHP <= 0);

        // 1. ADIM: Sadece şokta olmayanları hareket ettir
        foreach (var e in enemies)
        {
            if (e != null && e.skipTurns <= 0)
            {
                e.ExecuteLockedMove();
            }
        }

        yield return new WaitUntil(() =>
        {
            foreach (var e in enemies) if (e != null && e.IsMoving()) return false;
            return true;
        });

        yield return new WaitForSeconds(0.2f);

        // --- 2. ADIM: KRİTİK FİLTRELEME! (Melee ve Alan Saldırısı ayrımı) ---
        List<EnemyAI> readyToMeleeAttack = new List<EnemyAI>();
        List<EnemyAI> readyToAoEAttack = new List<EnemyAI>();

        foreach (var e in enemies)
        {
            if (e != null && e.skipTurns <= 0)
            {
                // Eğer şarj ediyorsa Alan Saldırısı listesine al
                if (e.isChargingAttack)
                {
                    readyToAoEAttack.Add(e);
                }
                // Etrafımızdaysa ve şarj etmiyorsa Melee listesine al
                else if (IsNeighbor(e.GetCurrentCellPosition(), player.GetCurrentCellPosition()))
                {
                    readyToMeleeAttack.Add(e);
                }
            }
        }

        // 3. ADIM: Önce Uzaktan Gelen Alan Saldırıları Patlasın!
        if (readyToAoEAttack.Count > 0)
        {
            foreach (var aoeEnemy in readyToAoEAttack)
            {
                yield return StartCoroutine(aoeEnemy.ExecuteAoEAttackCoroutine(player));
            }
        }

        // 4. ADIM: Sonra Normal Melee (Yakın Çekim) Saldırıları Çalışsın!
        if (readyToMeleeAttack.Count > 0)
        {
            Debug.Log("🛡️ Yakın dövüşçüler saldırıyor...");
            yield return StartCoroutine(EnemyAttackCoroutine(readyToMeleeAttack));
        }
        else
        {
            // Eğer kimse melee atmadıysa turu biz manuel bitiriyoruz
            Debug.Log("Düşmanlar şokta, uzakta veya sadece alan saldırısı yaptı. Tur geçiyor.");
            EndTurnAndDecreaseStuns();
        }
    }
    // YENİ: Oyuncu hamle yaptığı an tüm okları yavaşça siler
    public void HideAllEnemyIntents()
    {
        foreach (var e in enemies)
        {
            if (e != null) e.SetArrowVisibility(false);
        }
    }
    // ARTIK SADECE OYUNCUNUN SALDIRISINI YÖNETİR!
    // ARTIK SADECE OYUNCUNUN SALDIRISINI YÖNETİR!


    // ARTIK SADECE OYUNCUNUN SALDIRISINI YÖNETİR!
    private IEnumerator MultiAttack(List<EnemyAI> targets)
    {
        yield return new WaitForSeconds(0.3f);

        // 1. Zarları at
        List<int> currentRolls = new List<int>();
        int diceCount = RunManager.instance != null ? RunManager.instance.baseDiceCount : 2;

        // --- İŞTE BURADA: PUSU PERK'ÜNÜN BİRİKTİRDİĞİ ZARLARI EKLE! ---
        int extraDices = 0;
        foreach (var p in RunManager.instance.activePerks)
        {
            // Eğer perk "CalculatedAmbush" ise biriken zarlarını al
            if (p is CalculatedAmbushPerk ambushPerk)
            {
                extraDices += ambushPerk.storedExtraDices;
                ambushPerk.storedExtraDices = 0; // Birikenleri sıfırla
            }
        }

        for (int i = 0; i < (diceCount + extraDices); i++) currentRolls.Add(Random.Range(1, 7));

        CombatPayload payload = new CombatPayload(currentRolls);
        // ... (Bundan sonrası senin eski kodunla devam ediyor)

        // ===============================================================
        // YENİ: VİZYONER KONTROL! 
        // Eğer adamda Sword Dance varsa, oyun daha zarları ekrana basmadan 
        // matematiği direkt çarpmaya çevirir. Gereksiz toplamayı atlarız!
        // ===============================================================
        if (RunManager.instance != null && RunManager.instance.activePerks.Exists(p => p.GetType().Name == "SwordDancePerk"))
        {
            payload.multiplyInsteadOfAdd = true;
        }

        yield return StartCoroutine(ShowDiceSequence(currentRolls));

        // Zarlar atıldıktan sonra İLK HASARI ekranda göster
        // (Artık Sword Dance varsa ekranda 16 yerine şak diye 63 yazacak)
        UpdateTotalDamageDisplay(payload.GetFinalDamage());
        yield return new WaitForSeconds(0.5f);

        // 2. PERKLERİ SIRAYLA ÇALIŞTIR
        if (RunManager.instance != null && RunManager.instance.activePerks.Count > 0)
        {
            List<BasePerk> perksToProcess = new List<BasePerk>(RunManager.instance.activePerks);
            perksToProcess.Sort((a, b) => a.priority.CompareTo(b.priority));

            foreach (BasePerk perk in perksToProcess)
            {
                int beforeTotal = payload.GetFinalDamage();
                perk.ModifyCombat(payload);

                bool anyDieChanged = false;
                for (int i = 0; i < currentRolls.Count; i++)
                {
                    if (currentRolls[i] != payload.diceRolls[i])
                    {
                        currentRolls[i] = payload.diceRolls[i];
                        AnimateSpecificDie(i, currentRolls[i]);
                        anyDieChanged = true;
                        yield return new WaitForSeconds(0.3f);
                    }
                }

                int afterTotal = payload.GetFinalDamage();

                // Kanka burası çok kritik: Sword Dance döngüde sırası gelip tekrar çalıştığında,
                // matematik zaten çarpma olduğu için beforeTotal ve afterTotal aynı kalacak.
                // Bu sayede oyun "Aaa sayı değişmedi" deyip gereksiz yere ikinci bir animasyon OYNATMAYACAK!
                if (beforeTotal != afterTotal || anyDieChanged)
                {
                    perk.TriggerVisualPop();
                    UpdateTotalDamageDisplay(afterTotal);
                    yield return new WaitForSeconds(0.3f);
                }
            }
        }

        // ... (Kodun geri kalanı aynı şekilde devam ediyor: Kritik vuruş, hasar uygulama vs.)

        // 3. Kritik Vuruş
        if (Random.value < RunManager.instance.criticalChance)
        {
            payload.isCriticalHit = true;
            UpdateTotalDamageDisplay(payload.GetFinalDamage());
            Debug.Log("🎯 KRİTİK!");

            // YENİ: Animasyonu tam bu saniye başlatıyoruz!
            if (criticalText != null) StartCoroutine(CriticalTextPopAnimation());

            yield return new WaitForSeconds(0.5f);
        }

        // =========================================================
        // YENİ: VURUŞ HİSSİYATI İÇİN ZARLARI AKSİYONDAN ÖNCE GİZLE!
        // Oyuncu toplam hasarı saliselik görür ve ardından ekran temizlenir.
        // =========================================================
        yield return new WaitForSeconds(0.4f);
        HideDiceResults();

        // 4. HASAR UYGULAMA (Patlama ve ödüller uçtuktan sonraya saklanır)
        int finalDamage = payload.GetFinalDamage();
        int damagePerEnemy = finalDamage / targets.Count;

        List<EnemyAI> knockedEnemies = new List<EnemyAI>();
        List<EnemyAI> deadEnemiesThisTurn = new List<EnemyAI>();

        foreach (var enemy in targets)
        {
            if (enemy == null) continue;

            bool dies = enemy.health.currentHP <= damagePerEnemy;
            enemy.health.TakeDamage(damagePerEnemy); // Anında kırmızı yanar veya erimeye başlar

            knockedEnemies.Add(enemy);
            if (dies) deadEnemiesThisTurn.Add(enemy);
        }

        // 5. KNOCKBACK (ÖLÜ YA DA DİRİ TÜM VURULANLARA UYGULANIR)
        foreach (var e in knockedEnemies)
        {
            if (e == null) continue;

            // 1. Düşmanın itileceği o tam arkasındaki kareyi bul
            Vector3Int rawTargetCell = GetRawOppositeCell(e.GetCurrentCellPosition(), player.GetCurrentCellPosition());

            // 2. Arkadaki karede duran bir düşman var mı?
            EnemyAI enemyBehind = GetEnemyAtCell(rawTargetCell);

            if (enemyBehind != null)
            {
                // ==========================================
                // BİLARDO ETKİSİ! Düşman düşmana çarptı!
                // ==========================================
                e.skipTurns = 2;
                e.SetStunVisual(true);

                enemyBehind.skipTurns = 2;
                enemyBehind.SetStunVisual(true);

                // Vuruş yönünü hesapla
                Vector3 cPos = groundMap.GetCellCenterWorld(e.GetCurrentCellPosition());
                Vector3 pPos = groundMap.GetCellCenterWorld(player.GetCurrentCellPosition());
                cPos.z = 0; pPos.z = 0;
                Vector3 bumpDir = (cPos - pPos).normalized;

                // İkisini de aynı yöne doğru hafifçe sars!
                e.StartWallBump(bumpDir);
                enemyBehind.StartWallBump(bumpDir);

                Debug.Log($"🎳 BİLARDO! {e.name}, {enemyBehind.name}'e çarptı! İkisi de şokta!");
            }
            else if (!groundMap.HasTile(rawTargetCell) || player.GetCurrentCellPosition() == rawTargetCell)
            {
                // ==========================================
                // NORMAL DUVARA ÇARPMA
                // ==========================================
                e.skipTurns = 2;
                e.SetStunVisual(true);

                Vector3 cPos = groundMap.GetCellCenterWorld(e.GetCurrentCellPosition());
                Vector3 pPos = groundMap.GetCellCenterWorld(player.GetCurrentCellPosition());
                cPos.z = 0; pPos.z = 0;
                e.StartWallBump((cPos - pPos).normalized);
            }
            else
            {
                // ==========================================
                // ARKASI BOŞ, SEKEREK UÇMA
                // ==========================================
                e.skipTurns = Mathf.Max(e.skipTurns, 1); // Zaten şoktaysa süresi azalmasın diye Max kullanıyoruz
                e.StartKnockbackMovement(rawTargetCell);
            }
        }

        // 6. HAREKET BİTTİ -> PATLAMALARI VE ÖLÜM ÖDÜLLERİNİ GİTTİKLERİ YERDE VER!
        foreach (var e in knockedEnemies)
        {
            // Patlama perk'ü varsa uçtuğu yeni hücrede tetiklenir
            if (e != null && payload.triggerExplosion) TriggerExplosion(e.GetCurrentCellPosition());
        }

        foreach (var deadEnemy in deadEnemiesThisTurn)
        {
            if (deadEnemy != null)
            {
                int coinDrop = Random.Range(1, 6) + RunManager.instance.bonusGold;
                RunManager.instance.currentGold += coinDrop;
                UpdateCoinUI();
                Debug.Log($"💀 {deadEnemy.name} uçarak öldü! +{coinDrop} coin");
                foreach (var p in RunManager.instance.activePerks) p.OnEnemyKilled(deadEnemy);
                enemies.Remove(deadEnemy); // Ana listeden düş
            }
        }

        // Ölüleri bu geçici listelerden tamamen temizle
        knockedEnemies.RemoveAll(e => deadEnemiesThisTurn.Contains(e) || e == null || e.health.currentHP <= 0);
        enemies.RemoveAll(e => e == null || e.health.currentHP <= 0);

        if (enemies.Count <= 0)
        {
            if (LevelUpManager.instance != null) LevelUpManager.instance.ShowLevelUpScreen();
            yield break;
        }

        // 7. DİKEN (HAZARD) KONTROLÜ (BEKLEMELİ VE PARLAMALI)
        List<EnemyAI> spikedEnemies = new List<EnemyAI>();
        bool anyoneBounced = false; // İŞTE BURAYA ALDIK! Artık herkes görebilir.

        foreach (var s in knockedEnemies)
        {
            if (s != null && LevelGenerator.instance.hazardCells.Contains(s.GetCurrentCellPosition()))
            {
                spikedEnemies.Add(s);
            }
        }

        if (spikedEnemies.Count > 0)
        {
            // 1. Ekranda o anı "hissetmek" için bekle
            yield return new WaitForSeconds(0.2f);

            // 2. Dikenler aynı anda parlasın ve adamlar hasar yesin
            foreach (var s in spikedEnemies)
            {
                StartCoroutine(FlashHazardTileCoroutine(s.GetCurrentCellPosition()));
                s.health.TakeDamage(Mathf.Max(1, s.health.maxHP / 2));
            }

            // 3. Hasarın ve parlamanın ekranda kalma süresi
            yield return new WaitForSeconds(0.2f);

            // 4. Hayatta kalanlar dikenden rastgele kaçsın
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

        // Animasyonun bitmesini bekle (Artık hata vermeyecek)
        if (anyoneBounced)
        {
            yield return new WaitUntil(() =>
            {
                foreach (var s in spikedEnemies) if (s != null && s.IsMoving()) return false;
                return true;
            });
            enemies.RemoveAll(e => e == null || e.health.currentHP <= 0);
        }

        if (enemies.Count <= 0)
        {
            if (LevelUpManager.instance != null) LevelUpManager.instance.ShowLevelUpScreen();
            yield break;
        }

        yield return new WaitForSeconds(0.2f);

        // 8. ZİNCİRLEME SALDIRI KONTROLÜ (YENİ MANTIK)
        List<EnemyAI> allAdjacent = GetAdjacentEnemies(player.GetCurrentCellPosition());
        List<EnemyAI> nextTargets = new List<EnemyAI>();

        foreach (var e in allAdjacent)
        {
            // Sadece sersemlememiş düşmanlar zincirlemeye dahil olur
            if (e != null && e.skipTurns <= 0) nextTargets.Add(e);
        }

        // EĞER VURULACAK ADAM KALDIYSA ZİNCİRLEMEYE DEVAM ET
        if (nextTargets.Count > 0)
        {
            yield return StartCoroutine(MultiAttack(nextTargets));
        }

        // DİKKAT: BURADAKİ "else { EndTurnAndDecreaseStuns(); }" KISMINI TAMAMEN SİLDİK!


    }

    // ==========================================
    // YENİ: DÜŞMANLARIN BAŞLATTIĞI SALDIRI
    // ==========================================
    private IEnumerator EnemyAttackCoroutine(List<EnemyAI> attackers)
    {
        // YENİ GÜVENLİK KONTROLÜ: Saldıranlardan şokta olan varsa listeyi temizle!
        attackers.RemoveAll(a => a.skipTurns > 0);
        if (attackers.Count == 0)
        {
            EndTurnAndDecreaseStuns();
            yield break;
        }

        yield return new WaitForSeconds(0.2f);

        // --- EKSİK OLAN VE SENİN "GERİ KALAN KODLAR" DİYE SİLDİĞİN ASIL KISIM BURASIYDI :) ---
        bool dodged = false;
        if (RunManager.instance != null)
        {
            dodged = RunManager.instance.hasHolyAegis || Random.value < RunManager.instance.dodgeChance;
            if (RunManager.instance.hasHolyAegis) RunManager.instance.hasHolyAegis = false;
        }

        if (!dodged)
        {
            // Oyuncu Hasar Alır
            // Hasar yeme kontrolü (EnemyAttackCoroutine veya Diken kontrolü)
            if (RunManager.instance.hasHolyAegis)
            {
                // Önce kalkanı bul ve patlat!
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

            // Oyuncu Geri Seker (Saldıran ilk düşmanın tersine doğru)
            Vector3Int playerOriginalCell = player.GetCurrentCellPosition();
            Vector3Int playerTarget = GetOppositeCell(playerOriginalCell, attackers[0].GetCurrentCellPosition());

            player.StartKnockbackMovement(playerTarget);
            yield return new WaitUntil(() => !player.IsMoving());

            // Oyuncu dikene düştü mü kontrolü
            if (LevelGenerator.instance != null && LevelGenerator.instance.hazardCells.Contains(player.GetCurrentCellPosition()))
            {
                // 1. EKRAN DONMASI (VURGU ARTIRILDI): Oyuncu dikene girdi, tehlikeyi hissetmesi için daha uzun bekle
                yield return new WaitForSeconds(0.4f); // Eski 0.2f değerini 0.4f yaptık, "eyvah" hissi pekişti

                // 2. HASAR HESABI
                int spikeDamage = Mathf.Max(1, player.health.maxHP / 2);
                Debug.Log($"🔥 Dikenlere sürüklendin! {spikeDamage} hasar yiyorsun!");
                // --- ESKİ HALİ ---
                // Hasar yeme kontrolü (EnemyAttackCoroutine veya Diken kontrolü)
                if (RunManager.instance.hasHolyAegis)
                {
                    // Önce kalkanı bul ve patlat!
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

                // 3. PARLAMA VE SEKME TAMAMEN EŞ ZAMANLI!
                // Zemin parlamaya başladığı salise...
                StartCoroutine(FlashHazardTileCoroutine(player.GetCurrentCellPosition()));

                // ...oyuncu da can havliyle oradan fırlıyor! (Aradaki beklemeyi sildik)
                player.StartKnockbackMovement(playerOriginalCell);

                // Geri sekme hareketinin bitmesini bekle
                yield return new WaitUntil(() => !player.IsMoving());
            }
        }
        else
        {
            Debug.Log("🛡️ DODGE! Hasar almadın.");
        }

        yield return new WaitForSeconds(0.3f);

        // --- EN KRİTİK YER: SALDIRI BİTİNCE TURU SANA VEREN KOD ---
        EndTurnAndDecreaseStuns();

        {
            if (player != null && player.health.currentHP > 0)
            {
                Debug.Log("🔄 Düşman turu bitti. Sersemleme süreleri azalıyor...");

                foreach (var e in enemies)
                {
                    if (e != null && e.skipTurns > 0)
                    {
                        e.skipTurns--; // Cezasını 1 tur azalt

                        if (e.skipTurns <= 0)
                        {
                            e.SetStunVisual(false); // Cezası bittiyse kafasındaki yıldızları sil
                        }
                    }
                }

                isPlayerTurn = true;
                player.UpdateHighlights();
                LockAllEnemyIntents();
            }
        }
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
                    if (e.skipTurns <= 0)
                        e.SetStunVisual(false);
                }
            }

            isPlayerTurn = true;
            player.UpdateHighlights();
            LockAllEnemyIntents();
        }
    }

    // Shop kapandıktan sonra Shopmanager.LeaveShop() içinden çağrılır
    public void ResumeAfterShop()
    {
        if (player != null && player.health.currentHP > 0)
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
        if (criticalText != null) criticalText.gameObject.SetActive(false); // YENİ EKLENDİ
    }

    // ==========================================
    // YENİ: KRİTİK YAZISI ANİMASYONU
    // ==========================================
    private IEnumerator CriticalTextPopAnimation()
    {
        if (criticalText == null) yield break;

        criticalText.gameObject.SetActive(true);
        Transform t = criticalText.transform;

        Vector3 startScale = new Vector3(0.2f, 0.2f, 0.2f); // Küçücük başlar
        Vector3 overshootScale = new Vector3(0.6f, 0.6f, 0.6f); // Ekrana doğru devasa patlar
        Vector3 endScale = new Vector3(0.5f, 0.5f, 0.5f); // Normal boyutuna (1,1,1) döner

        float elapsed = 0f;
        float popDuration = 0.1f;

        // 1. AŞAMA: Aniden büyü ve ekrana vur (Overshoot)
        while (elapsed < popDuration)
        {
            t.localScale = Vector3.Lerp(startScale, overshootScale, elapsed / popDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        elapsed = 0f;
        float settleDuration = 0.1f;

        // 2. AŞAMA: Hafifçe küçülüp asıl boyutuna esneyerek otur (Settle)
        while (elapsed < settleDuration)
        {
            t.localScale = Vector3.Lerp(overshootScale, endScale, elapsed / settleDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        t.localScale = endScale;
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

    // Verilen karede duran düşmanı direkt obje olarak döndürür (BİLARDO İÇİN)
    public EnemyAI GetEnemyAtCell(Vector3Int cell)
    {
        foreach (var e in enemies)
        {
            if (e != null && e.health.currentHP > 0 && e.GetCurrentCellPosition() == cell)
                return e;
        }
        return null;
    }

    // Çarpmaları hesaba katmadan DÜMDÜZ arkadaki karenin koordinatını verir
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
        // İŞTE BÜTÜN HATANIN SEBEBİ BURASIYDI! (Senin orijinalindeki gibi düzelttim)
        Vector3Int[] offsets = (centerCell.y % 2 != 0) ? evenOffsets : oddOffsets;

        // Vuruşun tam olarak hangi yönden (0 ile 5 arası hangi index) geldiğini buluyoruz
        for (int i = 0; i < 6; i++)
        {
            if (centerCell + offsets[i] == awayFromCell)
            {
                // Hexagon matematikte tam zıttı HER ZAMAN index'e 3 ekleyip 6'ya bölmektir.
                int oppositeIndex = (i + 3) % 6;

                // İteceğimiz o HEDEF KAREYİ bulduk
                Vector3Int strictKnockbackCell = centerCell + offsets[oppositeIndex];

                // KANKA SENİN DEDİĞİN YER TAM OLARAK BURASI:
                // Sadece ve sadece o hedef karede zemin yoksa veya biri duruyorsa = DUVAR
                if (!groundMap.HasTile(strictKnockbackCell) ||
                    IsEnemyAtCell(strictKnockbackCell) ||
                    player.GetCurrentCellPosition() == strictKnockbackCell)
                {
                    // Arkasında kare yok (veya dolu). Çarpıp aynı karede bitirecek (Stun yiyecek).
                    return centerCell;
                }

                // Arkası temizse o kareye doğru seker.
                return strictKnockbackCell;
            }
        }

        // Eğer komşu değillerse hata vermesin diye olduğu yeri döndürür
        return centerCell;
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
    // YENİ: Dikenden rastgele güvenli bir yöne sekme fonksiyonu
    private Vector3Int GetRandomSafeNeighbor(Vector3Int centerCell)
    {
        Vector3Int[] offsets = (centerCell.y % 2 != 0) ? evenOffsets : oddOffsets;
        List<Vector3Int> safeNeighbors = new List<Vector3Int>();

        foreach (var off in offsets)
        {
            Vector3Int neighbor = centerCell + off;

            // Eğer karede zemin varsa, üstünde düşman yoksa, kendin (oyuncu) yoksan ve DİKEN DEĞİLSE güvenlidir!
            if (groundMap.HasTile(neighbor) &&
                !IsEnemyAtCell(neighbor) &&
                player.GetCurrentCellPosition() != neighbor &&
                !LevelGenerator.instance.hazardCells.Contains(neighbor))
            {
                safeNeighbors.Add(neighbor);
            }
        }

        // Eğer etrafında kaçacak güvenli yer varsa rastgele birini seç
        if (safeNeighbors.Count > 0)
        {
            return safeNeighbors[Random.Range(0, safeNeighbors.Count)];
        }

        // Eğer her yeri kapalıysa mecbur olduğu yerde (dikenin içinde) kalır
        return centerCell;
    }
    // ==========================================
    // YENİ: DİKEN PARLAMA (FLASH) ANİMASYONU
    // ==========================================
    private IEnumerator FlashHazardTileCoroutine(Vector3Int cell)
    {
        if (groundMap == null) yield break;

        // ÇOK ÖNEMLİ: Unity varsayılan olarak Tile renklerini kilitler. Önce o kilidi açıyoruz!
        groundMap.SetTileFlags(cell, TileFlags.None);

        Color originalColor = Color.white;
        Color flashColor = new Color(1f, 0.4f, 0.4f, 1f); // Dikenin parlayacağı kırmızımtırak renk

        float duration = 0.15f; // Parlama hızı
        float elapsed = 0f;

        // 1. Kırmızıya doğru yumuşak geçiş
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            groundMap.SetColor(cell, Color.Lerp(originalColor, flashColor, elapsed / duration));
            yield return null;
        }

        elapsed = 0f;

        // 2. Geri kendi rengine (beyaza) dönüş
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            groundMap.SetColor(cell, Color.Lerp(flashColor, originalColor, elapsed / duration));
            yield return null;
        }

        groundMap.SetColor(cell, originalColor);
    }
}