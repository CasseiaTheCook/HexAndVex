using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

public class Shopmanager : MonoBehaviour
{
    public static Shopmanager instance;

    // -------------------------------------------------------
    // Dahili item tipi (prefab gerektirmez, anÄ±nda efekt uygular)
    // -------------------------------------------------------
    private enum SlotType { Perk, Item }

    private class ShopItemData
    {
        public string name;
        public string description;
        public int price;
        public HotbarItemType itemType;
        public System.Action onBuy;

        public ShopItemData(string n, string desc, int p, HotbarItemType type, System.Action effect = null)
        { name = n; description = desc; price = p; itemType = type; onBuy = effect; }
    }

    [Header("Panel")]
    public GameObject shopPanel;

    [Header("Perk Listeleri (LevelUpManager ile aynÄ±)")]
    public List<GameObject> commonPerks;
    public List<GameObject> rarePerks;
    public List<GameObject> epicPerks;
    public List<GameObject> legendaryPerks;

    [Header("UI Slotlar (3 adet)")]
    public Button[] buyButtons;
    public TMP_Text[] itemNameTexts;
    public TMP_Text[] itemPriceTexts;
    public Image[] soldOutOverlays;

    [Header("UI Genel")]
    public TMP_Text coinDisplayText;
    public Button rerollButton;
    public TMP_Text rerollPriceText;
    public Button shopButton;            // MainUI'daki Shop aÃ§/kapat butonu

    [Header("Reroll AyarlarÄ±")]
    public int rerollBaseCost = 2;
    public int rerollCostIncrease = 1;

    // Dahili durum
    private List<GameObject> currentPerkItems = new List<GameObject>(); // perk slotlarÄ±
    private ShopItemData[] currentShopItems = new ShopItemData[3];       // item slotlarÄ±
    private SlotType[] slotTypes = new SlotType[3];
    private int[] currentPrices = new int[3];
    private bool[] purchased = new bool[3];
    private int currentRerollCost;
    private int rerollCount = 0;
    private bool openedMidGame = false;

    // 5 sabit consumable item havuzu
    private List<ShopItemData> BuildItemPool()
    {
        return new List<ShopItemData>
        {
            
                new ShopItemData(
    " Sağlık İksiri", "Anında 1 can yenile", 3,
    HotbarItemType.HealthPotion),
    new ShopItemData(
    " Güçlü İksir", "Anında 2 can yenile", 5,
    HotbarItemType.StrongPotion),
    new ShopItemData(
    " Altın Cüzdan", "Anında +6 coin kazan", 2,
    HotbarItemType.GoldBag),
    new ShopItemData(
    " Enerji İçeceği", "Bu savaşta 1 ekstra hamle hakkı", 4,
    HotbarItemType.EnergyDrink),
    new ShopItemData(
    " Savaş Büyüsü", "Kritik vuruş ihtimali kalıcı +%15", 6,
    HotbarItemType.BattleSpell)
        };
    }

    void Awake()
    {
        if (instance == null) instance = this;
    }

    void Start()
    {
        if (rerollButton != null)
        {
            rerollButton.onClick.RemoveAllListeners();
            rerollButton.onClick.AddListener(TryReroll);
        }

        if (shopButton != null)
        {
            shopButton.onClick.RemoveAllListeners();
            shopButton.onClick.AddListener(ToggleShopMidGame);
        }

        if (shopPanel != null)
            shopPanel.SetActive(false);
    }

    // -------------------------------------------------------
    // AÃ§ / Kapat
    // -------------------------------------------------------

    // Dungeon bitti / tur sayÄ±sÄ± doldu â†’ TurnManager Ã§aÄŸÄ±rÄ±r
    public void OpenShop()
    {
        openedMidGame = false;
        rerollCount = 0;
        currentRerollCost = rerollBaseCost;

        if (shopPanel != null) shopPanel.SetActive(true);
        GenerateShopItems();
        RefreshCoinDisplay();
        RefreshRerollButton();
    }

    // ShopButton'a basÄ±nca toggle
    public void ToggleShopMidGame()
    {
        if (shopPanel != null && shopPanel.activeSelf)
        {
            LeaveShop();
            return;
        }

        if (TurnManager.instance != null && !TurnManager.instance.isPlayerTurn) return;

        openedMidGame = true;
        rerollCount = 0;
        currentRerollCost = rerollBaseCost;

        if (TurnManager.instance != null)
            TurnManager.instance.isPlayerTurn = false;

        if (shopPanel != null) shopPanel.SetActive(true);
        GenerateShopItems();
        RefreshCoinDisplay();
        RefreshRerollButton();
    }

    public void LeaveShop()
    {
        if (shopPanel != null) shopPanel.SetActive(false);

        if (openedMidGame || (TurnManager.instance != null && TurnManager.instance.enemies.Count > 0))
        {
            TurnManager.instance.ResumeAfterShop();
        }
        else if (LevelUpManager.instance != null)
        {
            LevelUpManager.instance.ShowLevelUpScreen();
        }
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
    // Slot Ã¼retimi (perk + item karÄ±ÅŸÄ±k)
    // -------------------------------------------------------
    private void GenerateShopItems()
    {
        currentPerkItems.Clear();
        purchased = new bool[3];
        List<ShopItemData> itemPool = BuildItemPool();
        List<int> usedItemIndices = new List<int>();

        for (int i = 0; i < 3; i++)
        {
            // %40 ihtimalle item, %60 ihtimalle perk (perk havuzu boÅŸsa item ver)
            bool pickItem = (Random.value < 0.4f) || (AllPerkListsEmpty());

            if (pickItem && itemPool.Count > usedItemIndices.Count)
            {
                // Benzersiz item seÃ§
                int idx;
                int safety = 0;
                do { idx = Random.Range(0, itemPool.Count); if (++safety > 100) break; }
                while (usedItemIndices.Contains(idx));

                usedItemIndices.Add(idx);
                slotTypes[i] = SlotType.Item;
                currentShopItems[i] = itemPool[idx];
                currentPrices[i] = itemPool[idx].price;

                SetSlotUI(i, itemPool[idx].name, itemPool[idx].description, itemPool[idx].price);
            }
            else
            {
                // Benzersiz perk seÃ§
                GameObject perk = null;
                int price = 3;
                int safety = 0;
                while (perk == null || currentPerkItems.Contains(perk))
                {
                    perk = GetRandomPerkByRarity(out price);
                    if (++safety > 100) break;
                }
                currentPerkItems.Add(perk);
                slotTypes[i] = SlotType.Perk;
                currentShopItems[i] = null;
                currentPrices[i] = price;

                if (perk != null)
                {
                    BasePerk script = perk.GetComponent<BasePerk>();
                    SetSlotUI(i, script.perkName, script.description, price);
                }
            }
        }

        RefreshAffordability();
    }

    private void SetSlotUI(int i, string itemName, string description, int price)
    {
        if (itemNameTexts != null && i < itemNameTexts.Length && itemNameTexts[i] != null)
            itemNameTexts[i].text = itemName + "\n<size=70%>" + description + "</size>";

        if (itemPriceTexts != null && i < itemPriceTexts.Length && itemPriceTexts[i] != null)
            itemPriceTexts[i].text = price + " Coin";

        if (buyButtons != null && i < buyButtons.Length && buyButtons[i] != null)
        {
            int idx = i;
            buyButtons[i].onClick.RemoveAllListeners();
            buyButtons[i].onClick.AddListener(() => TryBuy(idx));
            buyButtons[i].interactable = true;
        }

        if (soldOutOverlays != null && i < soldOutOverlays.Length && soldOutOverlays[i] != null)
            soldOutOverlays[i].gameObject.SetActive(false);
    }

    // -------------------------------------------------------
    // SatÄ±n alma
    // -------------------------------------------------------
    public void TryBuy(int index)
    {
        if (purchased[index] || RunManager.instance == null) return;

        int price = currentPrices[index];
        if (RunManager.instance.currentGold < price)
        {
            StartCoroutine(FlashPrice(index));
            return;
        }

        // Item ise hotbar doluluk kontrolu
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
            // Consumable item â†’ anÄ±nda efekti uygula
            if (HotbarManager.instance != null)
                HotbarManager.instance.AddItem(currentShopItems[index].itemType);
            else
                currentShopItems[index]?.onBuy?.Invoke();
            Debug.Log($"Hotbar'a eklendi: {currentShopItems[index]?.name}");
        }
        else
        {
            // Perk â†’ kalÄ±cÄ± ekle
            int perkIdx = 0;
            int remaining = index;
            for (int i = 0; i <= index; i++)
                if (slotTypes[i] == SlotType.Perk) { perkIdx = i; }

            // currentPerkItems iÃ§indeki sÄ±rayÄ± bul
            int perkListIdx = 0;
            int count = -1;
            for (int i = 0; i <= index; i++)
                if (slotTypes[i] == SlotType.Perk) count++;
            perkListIdx = count;

            if (perkListIdx < currentPerkItems.Count && currentPerkItems[perkListIdx] != null)
            {
                RunManager.instance.AddPerk(currentPerkItems[perkListIdx]);
                Debug.Log($"SatÄ±n alÄ±ndÄ±: {currentPerkItems[perkListIdx].GetComponent<BasePerk>().perkName} (-{price} coin)");
            }
        }

        purchased[index] = true;

        if (buyButtons != null && index < buyButtons.Length && buyButtons[index] != null)
            buyButtons[index].interactable = false;

        if (soldOutOverlays != null && index < soldOutOverlays.Length && soldOutOverlays[index] != null)
            soldOutOverlays[index].gameObject.SetActive(true);

        RefreshCoinDisplay();
        RefreshAffordability();
        RefreshRerollButton();
    }

    // -------------------------------------------------------
    // UI YardÄ±mcÄ±larÄ±
    // -------------------------------------------------------
    private void RefreshCoinDisplay()
    {
        if (coinDisplayText != null && RunManager.instance != null)
            coinDisplayText.text = "Coin: " + RunManager.instance.currentGold;
    }

    private void RefreshAffordability()
    {
        if (RunManager.instance == null) return;
        for (int i = 0; i < 3; i++)
        {
            if (purchased[i]) continue;
            if (buyButtons != null && i < buyButtons.Length && buyButtons[i] != null)
                buyButtons[i].interactable = RunManager.instance.currentGold >= currentPrices[i];
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

    private System.Collections.IEnumerator FlashPrice(int index)
    {
        if (itemPriceTexts == null || index >= itemPriceTexts.Length || itemPriceTexts[index] == null) yield break;
        yield return StartCoroutine(FlashText(itemPriceTexts[index]));
    }

    private System.Collections.IEnumerator FlashText(TMP_Text t)
    {
        if (t == null) yield break;
        Color orig = t.color;
        t.color = Color.red;
        yield return new WaitForSeconds(0.5f);
        t.color = orig;
    }

    // -------------------------------------------------------
    // Perk seÃ§imi
    // -------------------------------------------------------
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
