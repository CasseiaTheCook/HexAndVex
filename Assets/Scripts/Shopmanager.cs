using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

public class Shopmanager : MonoBehaviour
{
    public static Shopmanager instance;

    // -------------------------------------------------------
    // Inspector alanlari
    // -------------------------------------------------------
    [Header("Item Havuzu")]
    public List<BaseItem> itemPool = new List<BaseItem>();

    [Header("Shop Slot Sistemi")]
    public Transform shopSlotContainer;
    public GameObject shopSlotPrefab;
    public int shopSlotCount = 3;

    [Header("UI Genel")]
    public TMP_Text coinDisplayText;
    public Button rerollButton;
    public TMP_Text rerollPriceText;

    [Header("Reroll Ayarlari")]
    public float rerollBaseCost = 2f;
    public float rerollMultiplier = 1.5f;

    // -------------------------------------------------------
    // Runtime state
    // -------------------------------------------------------
    private List<ShopSlot> spawnedSlots = new List<ShopSlot>();
    private List<BaseItem> currentItems = new List<BaseItem>();
    private List<bool> purchased = new List<bool>();

    private int rerollCount = 0;
    private int currentRerollCost;

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

        if (shopSlotContainer != null)
        {
            var hlg = shopSlotContainer.GetComponent<HorizontalLayoutGroup>();
            if (hlg == null) hlg = shopSlotContainer.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 8;
            hlg.padding = new RectOffset(4, 4, 4, 4);
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth  = false;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth  = false;
            hlg.childForceExpandHeight = false;
        }

        if (rerollButton != null)
        {
            rerollButton.onClick.RemoveAllListeners();
            rerollButton.onClick.AddListener(TryReroll);
        }

        GenerateShopItems();
    }

    // -------------------------------------------------------
    // Normal tur temizlendi — sadece perk seçim ekranını aç
    // -------------------------------------------------------
    public void OnDungeonCleared()
    {
        RefreshCoinDisplay();

        if (LevelUpManager.instance != null)
            LevelUpManager.instance.ShowLevelUpScreen();
        else
        {
            RunManager.instance.currentLevel++;
            LevelGenerator.instance.GenerateNextLevel();
        }
    }

    // -------------------------------------------------------
    // Boss temizlendi — shopı yenile + perk seçim ekranı
    // -------------------------------------------------------
    public void OnBossCleared()
    {
        rerollCount = 0;
        currentRerollCost = Mathf.RoundToInt(rerollBaseCost);
        GenerateShopItems();
        RefreshCoinDisplay();

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
        currentRerollCost = Mathf.RoundToInt(rerollBaseCost * Mathf.Pow(rerollMultiplier, rerollCount));

        GenerateShopItems();
        RefreshCoinDisplay();
    }

    // -------------------------------------------------------
    // Slot üretimi
    // -------------------------------------------------------
    public void GenerateShopItems()
    {
        foreach (var slot in spawnedSlots)
            if (slot != null) Destroy(slot.gameObject);

        spawnedSlots.Clear();
        currentItems.Clear();
        purchased.Clear();

        if (shopSlotPrefab == null || shopSlotContainer == null) return;

        // Havuzdan rastgele benzersiz seçim
        List<int> usedIndices = new List<int>();

        for (int i = 0; i < shopSlotCount; i++)
        {
            if (itemPool.Count <= usedIndices.Count) break;

            int idx;
            int safety = 0;
            do { idx = Random.Range(0, itemPool.Count); if (++safety > 100) break; }
            while (usedIndices.Contains(idx));
            usedIndices.Add(idx);

            BaseItem item = itemPool[idx];

            GameObject slotGO = Instantiate(shopSlotPrefab, shopSlotContainer);
            slotGO.transform.localScale = Vector3.one;

            RectTransform slotRT = slotGO.GetComponent<RectTransform>();
            if (slotRT != null) slotRT.sizeDelta = new Vector2(65f, 65f);

            var le = slotGO.GetComponent<LayoutElement>();
            if (le == null) le = slotGO.AddComponent<LayoutElement>();
            le.preferredWidth = 65f;
            le.preferredHeight = 65f;
            le.flexibleWidth = 0f;
            le.flexibleHeight = 0f;

            ShopSlot slot = slotGO.GetComponent<ShopSlot>();
            spawnedSlots.Add(slot);
            currentItems.Add(item);
            purchased.Add(false);

            SetupSlot(slot, i, item);
        }

        RefreshCoinDisplay();
    }

    private void SetupSlot(ShopSlot slot, int index, BaseItem item)
    {
        if (slot == null) return;

        slot.tooltipName = item.itemName;
        slot.tooltipDesc = item.description;
        slot.tooltipPrice = item.price;

        if (slot.soldOutOverlay != null)
            slot.soldOutOverlay.SetActive(false);

        if (slot.buyButton != null)
        {
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
    // Satın alma
    // -------------------------------------------------------
    public void TryBuy(int index)
    {
        if (index >= purchased.Count || purchased[index]) return;
        if (RunManager.instance == null) return;

        BaseItem item = currentItems[index];
        if (item == null) return;

        if (RunManager.instance.currentGold < item.price)
        {
            if (index < spawnedSlots.Count && spawnedSlots[index] != null)
                StartCoroutine(FlashText(spawnedSlots[index].priceText));
            return;
        }

        RunManager.instance.currentGold -= item.price;
        item.Use();

        purchased[index] = true;

        if (index < spawnedSlots.Count && spawnedSlots[index] != null)
        {
            if (spawnedSlots[index].buyButton != null)
                spawnedSlots[index].buyButton.interactable = false;
            if (spawnedSlots[index].soldOutOverlay != null)
                spawnedSlots[index].soldOutOverlay.SetActive(true);
        }

        RefreshCoinDisplay();
    }

    // -------------------------------------------------------
    // UI yardımcıları
    // -------------------------------------------------------
    public void RefreshCoinDisplay()
    {
        if (coinDisplayText != null && RunManager.instance != null)
            coinDisplayText.text = RunManager.instance.currentGold.ToString();
        RefreshRerollButton();
        RefreshAffordability();
    }

    public void RefreshAffordability()
    {
        if (RunManager.instance == null) return;
        for (int i = 0; i < spawnedSlots.Count; i++)
        {
            if (purchased[i] || spawnedSlots[i] == null) continue;
            if (spawnedSlots[i].buyButton != null)
                spawnedSlots[i].buyButton.interactable = RunManager.instance.currentGold >= currentItems[i].price;
        }
    }

    private void RefreshRerollButton()
    {
        if (rerollPriceText != null)
            rerollPriceText.text = "Reroll: " + currentRerollCost + " Coin";
        if (rerollButton != null && RunManager.instance != null)
            rerollButton.interactable = RunManager.instance.currentGold >= currentRerollCost;
    }

    private IEnumerator FlashText(TMP_Text t)
    {
        if (t == null) yield break;
        Color orig = t.color;
        t.color = Color.red;
        yield return new WaitForSeconds(0.5f);
        t.color = orig;
    }
}
