using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

public enum HotbarItemType
{
    None,
    HealthPotion,
    StrongPotion,
    GoldBag,
    EnergyDrink,
    BattleSpell
}

public class Shopmanager : MonoBehaviour
{
    public static Shopmanager instance;

    // -------------------------------------------------------
    // Ic tipler
    // -------------------------------------------------------
    private enum SlotType { Perk, Item }

    private class ShopItemData
    {
        public string name;
        public string description;
        public int price;
        public HotbarItemType itemType;

        public ShopItemData(string n, string desc, int p, HotbarItemType type)
        { name = n; description = desc; price = p; itemType = type; }
    }

    // -------------------------------------------------------
    // Inspector alanlari
    // -------------------------------------------------------
    [Header("Perk Listeleri")]
    public List<GameObject> commonPerks;
    public List<GameObject> rarePerks;
    public List<GameObject> epicPerks;
    public List<GameObject> legendaryPerks;

    [Header("Shop Slot Sistemi")]
    public Transform shopSlotContainer;   // Horizontal/Vertical Layout Group iceren parent
    public GameObject shopSlotPrefab;     // ShopSlot component tasiran prefab
    public int shopSlotCount = 3;         // Upgrades ile arttirilabilir

    [Header("UI Genel")]
    public TMP_Text coinDisplayText;
    public Button rerollButton;
    public TMP_Text rerollPriceText;

    [Header("Reroll Ayarlari")]
    public float rerollBaseCost = 2f;
    public float rerollMultiplier = 1.2f;

    // -------------------------------------------------------
    // Runtime state
    // -------------------------------------------------------
    private List<ShopSlot> spawnedSlots      = new List<ShopSlot>();
    private List<ShopItemData> currentItems  = new List<ShopItemData>();
    private List<SlotType> slotTypes         = new List<SlotType>();
    private List<int> currentPrices          = new List<int>();
    private List<bool> purchased             = new List<bool>();
    private List<GameObject> perkPrefabs     = new List<GameObject>();

    private int rerollCount = 0;
    private int currentRerollCost;

    // -------------------------------------------------------
    // 5 sabit consumable item havuzu
    // -------------------------------------------------------
    private List<ShopItemData> BuildItemPool() => new List<ShopItemData>
    {
        new ShopItemData("Sağlık İksiri",   "1 can yenile",                     3, HotbarItemType.HealthPotion),
        new ShopItemData("Güçlü İksir",     "2 can yenile",                     5, HotbarItemType.StrongPotion),
        new ShopItemData("Altın Cüzdan",    "+6 coin kazan",                    2, HotbarItemType.GoldBag),
        new ShopItemData("Enerji İçeceği",  "Bu savaşta 1 ekstra hamle",        4, HotbarItemType.EnergyDrink),
        new ShopItemData("Savaş Büyüsü",    "Kritik şans kalıcı +%15",          6, HotbarItemType.BattleSpell)
    };

    // -------------------------------------------------------
    // Unity lifecycle
    // -------------------------------------------------------
    void Awake()
    {
        if (instance == null) instance = this;
    }

    void Start()
    {
        currentRerollCost = Mathf.RoundToInt(rerollBaseCost);

        // Sadece HLG ayarlarini kod uzerinden garantile, pozisyonu editor'a birak
        if (shopSlotContainer != null)
        {
            var hlg = shopSlotContainer.GetComponent<HorizontalLayoutGroup>();
            if (hlg == null) hlg = shopSlotContainer.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 4;
            hlg.padding = new RectOffset(4, 4, 4, 4);
            hlg.childAlignment = TextAnchor.UpperLeft;
            hlg.childControlWidth  = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth  = true;
            hlg.childForceExpandHeight = true;
        }

        if (rerollButton != null)
        {
            rerollButton.onClick.RemoveAllListeners();
            rerollButton.onClick.AddListener(TryReroll);
            Debug.Log("Reroll butonu baglandi.");
        }
        else
        {
            Debug.LogWarning("Shopmanager: rerollButton atanmamis!");
        }

        GenerateShopItems();
    }

    // -------------------------------------------------------
    // Dungeon temizlendi — TurnManager cagirir
    // -------------------------------------------------------
    public void OnDungeonCleared()
    {
        rerollCount = 0;
        currentRerollCost = Mathf.RoundToInt(rerollBaseCost);
        GenerateShopItems();

        if (LevelUpManager.instance != null)
            LevelUpManager.instance.ShowLevelUpScreen();
        else
        {
            RunManager.instance.currentLevel++;
            LevelGenerator.instance.GenerateNextLevel();
        }
    }

    // -------------------------------------------------------
    // Reroll
    // -------------------------------------------------------
    public void TryReroll()
    {
        Debug.Log($"TryReroll cagrildi. gold={RunManager.instance?.currentGold}, cost={currentRerollCost}");
        if (RunManager.instance == null) return;

        if (RunManager.instance.currentGold < currentRerollCost)
        {
            StartCoroutine(FlashText(rerollPriceText));
            return;
        }

        RunManager.instance.currentGold -= currentRerollCost;
        rerollCount++;
        currentRerollCost = Mathf.RoundToInt(rerollBaseCost * Mathf.Pow(rerollMultiplier, rerollCount));

        GenerateShopItems();
        RefreshCoinDisplay();
        RefreshRerollButton();
    }

    // -------------------------------------------------------
    // Slot uretimi — tum slotlari yok eder, yenilerini spawn eder
    // -------------------------------------------------------
    public void GenerateShopItems()
    {
        // Eski slotlari temizle
        foreach (var slot in spawnedSlots)
            if (slot != null) Destroy(slot.gameObject);

        spawnedSlots.Clear();
        currentItems.Clear();
        slotTypes.Clear();
        currentPrices.Clear();
        purchased.Clear();
        perkPrefabs.Clear();

        if (shopSlotPrefab == null || shopSlotContainer == null)
        {
            Debug.LogWarning("Shopmanager: shopSlotPrefab veya shopSlotContainer atanmamis!");
            return;
        }

        List<ShopItemData> itemPool = BuildItemPool();
        List<int> usedItemIndices = new List<int>();

        for (int i = 0; i < shopSlotCount; i++)
        {
            bool pickItem = (Random.value < 0.4f) || AllPerkListsEmpty();

            GameObject slotGO = Instantiate(shopSlotPrefab, shopSlotContainer);

            slotGO.transform.localScale = Vector3.one;

            ShopSlot slot = slotGO.GetComponent<ShopSlot>();
            spawnedSlots.Add(slot);
            purchased.Add(false);

            if (pickItem && itemPool.Count > usedItemIndices.Count)
            {
                int idx;
                int safety = 0;
                do { idx = Random.Range(0, itemPool.Count); if (++safety > 100) break; }
                while (usedItemIndices.Contains(idx));

                usedItemIndices.Add(idx);
                ShopItemData item = itemPool[idx];

                slotTypes.Add(SlotType.Item);
                currentItems.Add(item);
                perkPrefabs.Add(null);
                currentPrices.Add(item.price);

                SetupSlot(slot, i, item.name, item.description, item.price);
            }
            else
            {
                int price = 3;
                GameObject perkPrefab = PickUniquePerk(out price);

                slotTypes.Add(SlotType.Perk);
                currentItems.Add(null);
                perkPrefabs.Add(perkPrefab);
                currentPrices.Add(price);

                if (perkPrefab != null)
                {
                    BasePerk script = perkPrefab.GetComponent<BasePerk>();
                    SetupSlot(slot, i, script.perkName, script.description, price);
                }
                else
                {
                    SetupSlot(slot, i, "Perk yok", "-", price);
                }
            }
        }

        RefreshCoinDisplay();
        RefreshRerollButton();
        RefreshAffordability();
    }

    // Daha once secilmemis benzersiz bir perk prefabi dondurur
    private GameObject PickUniquePerk(out int price)
    {
        GameObject result = null;
        price = 3;
        int safety = 0;
        do
        {
            result = GetRandomPerkByRarity(out price);
            if (++safety > 100) break;
        }
        while (result != null && perkPrefabs.Contains(result));
        return result;
    }

    private void SetupSlot(ShopSlot slot, int index, string itemName, string desc, int price)
    {
        if (slot == null) return;

        if (slot.nameText != null)
        {
            slot.nameText.text = itemName + "\n<size=70%>" + desc + "</size>";
            slot.nameText.raycastTarget = false;
        }

        if (slot.priceText != null)
        {
            slot.priceText.text = price + " Coin";
            slot.priceText.raycastTarget = false;
        }

        if (slot.soldOutOverlay != null)
            slot.soldOutOverlay.SetActive(false);

        if (slot.buyButton != null)
        {
            // Prefab'dan kalan eski etiket metnini temizle
            var btnLabel = slot.buyButton.GetComponentInChildren<TMP_Text>();
            if (btnLabel != null && btnLabel != slot.nameText && btnLabel != slot.priceText)
                btnLabel.text = "";

            int idx = index;
            slot.buyButton.onClick.RemoveAllListeners();
            slot.buyButton.onClick.AddListener(() => TryBuy(idx));
            slot.buyButton.interactable = true;
        }
    }

    // -------------------------------------------------------
    // Satin alma
    // -------------------------------------------------------
    public void TryBuy(int index)
    {
        if (index >= purchased.Count || purchased[index]) return;
        if (RunManager.instance == null) return;

        int price = currentPrices[index];

        if (RunManager.instance.currentGold < price)
        {
            if (index < spawnedSlots.Count && spawnedSlots[index] != null)
                StartCoroutine(FlashText(spawnedSlots[index].priceText));
            return;
        }

        RunManager.instance.currentGold -= price;

        if (slotTypes[index] == SlotType.Item)
        {
            ApplyItemEffect(currentItems[index].itemType);
        }
        else
        {
            GameObject perk = index < perkPrefabs.Count ? perkPrefabs[index] : null;
            if (perk != null)
                RunManager.instance.AddPerk(perk);
        }

        purchased[index] = true;

        if (index < spawnedSlots.Count && spawnedSlots[index] != null)
        {
            if (spawnedSlots[index].buyButton != null)
                spawnedSlots[index].buyButton.interactable = false;
            if (spawnedSlots[index].soldOutOverlay != null)
                spawnedSlots[index].soldOutOverlay.SetActive(true);
        }

        RefreshCoinDisplay();
        RefreshAffordability();
        RefreshRerollButton();
    }

    // -------------------------------------------------------
    // UI yardimcilari
    // -------------------------------------------------------
    // -------------------------------------------------------
    // Item efektlerini aninda uygula
    // -------------------------------------------------------
    private void ApplyItemEffect(HotbarItemType type)
    {
        switch (type)
        {
            case HotbarItemType.HealthPotion:
                if (TurnManager.instance?.player?.health != null)
                    TurnManager.instance.player.health.Heal(1);
                if (RunManager.instance != null)
                    RunManager.instance.playerCurrentHealth = Mathf.Min(
                        RunManager.instance.playerCurrentHealth + 1, RunManager.instance.playerMaxHealth);
                break;
            case HotbarItemType.StrongPotion:
                if (TurnManager.instance?.player?.health != null)
                    TurnManager.instance.player.health.Heal(2);
                if (RunManager.instance != null)
                    RunManager.instance.playerCurrentHealth = Mathf.Min(
                        RunManager.instance.playerCurrentHealth + 2, RunManager.instance.playerMaxHealth);
                break;
            case HotbarItemType.GoldBag:
                RunManager.instance.currentGold += 6;
                TurnManager.instance?.UpdateCoinUI();
                break;
            case HotbarItemType.EnergyDrink:
                RunManager.instance.remainingMoves += 1;
                break;
            case HotbarItemType.BattleSpell:
                RunManager.instance.criticalChance += 0.15f;
                break;
        }
        Debug.Log($"Item kullanildi: {type}");
    }

    public void RefreshCoinDisplay()
    {
        if (coinDisplayText != null && RunManager.instance != null)
            coinDisplayText.text = "Coins: " + RunManager.instance.currentGold;
    }

    public void RefreshAffordability()
    {
        if (RunManager.instance == null) return;
        for (int i = 0; i < spawnedSlots.Count; i++)
        {
            if (purchased[i] || spawnedSlots[i] == null) continue;
            if (spawnedSlots[i].buyButton != null)
                spawnedSlots[i].buyButton.interactable = RunManager.instance.currentGold >= currentPrices[i];
        }
    }

    private void RefreshRerollButton()
    {
        if (rerollPriceText != null)
            rerollPriceText.text = "Reroll: " + currentRerollCost + " Coin";
        if (rerollButton != null && RunManager.instance != null)
            rerollButton.interactable = RunManager.instance.currentGold >= currentRerollCost;
    }

    private bool AllPerkListsEmpty() =>
        (commonPerks == null || commonPerks.Count == 0) &&
        (rarePerks == null || rarePerks.Count == 0) &&
        (epicPerks == null || epicPerks.Count == 0) &&
        (legendaryPerks == null || legendaryPerks.Count == 0);

    private IEnumerator FlashText(TMP_Text t)
    {
        if (t == null) yield break;
        Color orig = t.color;
        t.color = Color.red;
        yield return new WaitForSeconds(0.5f);
        t.color = orig;
    }

    private GameObject GetRandomPerkByRarity(out int price)
    {
        float roll = Random.Range(0f, 100f);

        if (roll < 5f && legendaryPerks != null && legendaryPerks.Count > 0)
        { price = Random.Range(14, 21); return legendaryPerks[Random.Range(0, legendaryPerks.Count)]; }

        if (roll < 15f && epicPerks != null && epicPerks.Count > 0)
        { price = Random.Range(9, 15); return epicPerks[Random.Range(0, epicPerks.Count)]; }

        if (roll < 40f && rarePerks != null && rarePerks.Count > 0)
        { price = Random.Range(5, 11); return rarePerks[Random.Range(0, rarePerks.Count)]; }

        price = Random.Range(3, 7);
        if (commonPerks != null && commonPerks.Count > 0)
            return commonPerks[Random.Range(0, commonPerks.Count)];

        price = 3;
        return null;
    }
}
