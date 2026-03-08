using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

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
    public int rerollBaseCost = 2;
    public int rerollCostIncrease = 1;

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
        currentRerollCost = rerollBaseCost;

        // Container layout'unu ve boyutunu kod üzerinden garantile
        if (shopSlotContainer != null)
        {
            var hlg = shopSlotContainer.GetComponent<HorizontalLayoutGroup>();
            if (hlg == null) hlg = shopSlotContainer.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 10;
            hlg.padding = new RectOffset(8, 8, 8, 8);
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth  = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth  = true;
            hlg.childForceExpandHeight = true;

            var rt = shopSlotContainer as RectTransform;
            if (rt != null) rt.sizeDelta = new Vector2(shopSlotCount * 210f, 140f);
        }

        if (rerollButton != null)
        {
            rerollButton.onClick.RemoveAllListeners();
            rerollButton.onClick.AddListener(TryReroll);
        }

        GenerateShopItems();
    }

    // -------------------------------------------------------
    // Dungeon temizlendi — TurnManager cagirir
    // -------------------------------------------------------
    public void OnDungeonCleared()
    {
        rerollCount = 0;
        currentRerollCost = rerollBaseCost;
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
        if (RunManager.instance == null) return;

        if (RunManager.instance.currentGold < currentRerollCost)
        {
            StartCoroutine(FlashText(rerollPriceText));
            return;
        }

        RunManager.instance.currentGold -= currentRerollCost;
        rerollCount++;
        currentRerollCost = rerollBaseCost + rerollCount * rerollCostIncrease;

        GenerateShopItems();
        RefreshCoinDisplay();
        RefreshRerollButton();
    }

    // -------------------------------------------------------
    // Slot uretimi — tum slotlari yok eder, yenilerini spawn eder
    // -------------------------------------------------------
    private void GenerateShopItems()
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

            // Prefab'da Canvas varsa scale bozuluyor — hepsini temizle
            foreach (var c in slotGO.GetComponents<Canvas>())           Destroy(c);
            foreach (var c in slotGO.GetComponents<CanvasScaler>())      Destroy(c);
            foreach (var c in slotGO.GetComponents<GraphicRaycaster>()) Destroy(c);
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
            slot.nameText.text = itemName + "\n<size=70%>" + desc + "</size>";

        if (slot.priceText != null)
            slot.priceText.text = price + " Coin";

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

        // Item ise hotbar dolu mu kontrol et
        if (slotTypes[index] == SlotType.Item
            && HotbarManager.instance != null
            && !HotbarManager.instance.CanAddItem())
        {
            Debug.Log("Hotbar dolu, item eklenemiyor!");
            return;
        }

        RunManager.instance.currentGold -= price;

        if (slotTypes[index] == SlotType.Item)
        {
            if (HotbarManager.instance != null)
                HotbarManager.instance.AddItem(currentItems[index].itemType);
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
    public void RefreshCoinDisplay()
    {
        if (coinDisplayText != null && RunManager.instance != null)
            coinDisplayText.text = "Coin: " + RunManager.instance.currentGold;
    }

    private void RefreshAffordability()
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
