using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

public class Shopmanager : MonoBehaviour
{
    public static Shopmanager instance;

    [Header("Item Havuzu")]
    public List<BaseItem> itemPool = new List<BaseItem>();

    [Header("Secret Item")]
    public BaseItem secretItem; // SecretPerkOrb — %1 şansla shopta çıkar
    [Range(0f, 1f)] public float secretItemChance = 0.01f;

    [Header("Shop Slot Sistemi")]
    public Transform shopSlotContainer;
    public GameObject shopSlotPrefab;
    public int shopSlotCount = 3;

    [Header("UI Genel")]
    public TMP_Text coinDisplayText;
    public Button rerollButton;
    public TMP_Text rerollPriceText;

    [Header("Reroll Ayarlari")]
    public float rerollBaseCost = 10f;
    public float rerollMultiplier = 1.5f;

    private List<ShopSlot> spawnedSlots = new List<ShopSlot>();
    private List<BaseItem> currentItems = new List<BaseItem>();
    private List<bool> purchased = new List<bool>();

    private int rerollCount = 0;
    private int currentRerollCost;

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
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
        }

        if (rerollButton != null)
        {
            rerollButton.onClick.RemoveAllListeners();
            rerollButton.onClick.AddListener(TryReroll);
        }

        SetupCoinIcons();
        GenerateShopItems();
    }

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

    public void TryReroll()
    {
        if (RunManager.instance == null) return;
        if (TurnManager.instance != null && TurnManager.instance.IsAnyTargetingActive) return;

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

        // Tüm aktif perklerin OnShopReroll callback'ini çağır
        if (RunManager.instance != null)
        {
            foreach (var perk in RunManager.instance.activePerks)
            {
                if (perk != null) perk.OnShopReroll();
            }
        }
    }

    public void GenerateShopItems()
    {
        foreach (var slot in spawnedSlots)
            if (slot != null) Destroy(slot.gameObject);

        spawnedSlots.Clear();
        currentItems.Clear();
        purchased.Clear();

        if (shopSlotPrefab == null || shopSlotContainer == null) return;

        List<int> usedIndices = new List<int>();

        for (int i = 0; i < shopSlotCount; i++)
        {
            if (itemPool.Count <= usedIndices.Count) break;

            int idx;
            int safety = 0;
            do {
                idx = Random.Range(0, itemPool.Count);
                if (++safety > 100) break;
            }
            while (usedIndices.Contains(idx) ||
                   (RunManager.instance != null && RunManager.instance.hasPerkReroll && itemPool[idx] is MutationCatalyst));
            usedIndices.Add(idx);

            BaseItem item = itemPool[idx];
            if (item == null) continue;

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

        // %1 şansla secret item slotu ekle
        if (secretItem != null && Random.value < secretItemChance)
        {
            int secretIndex = spawnedSlots.Count;

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
            currentItems.Add(secretItem);
            purchased.Add(false);

            SetupSlot(slot, secretIndex, secretItem);
        }

        RefreshCoinDisplay();
    }

    private void SetupSlot(ShopSlot slot, int index, BaseItem item)
    {
        if (slot == null) return;

        slot.tooltipName = item.itemName;
        slot.tooltipDesc = item.description;
        slot.tooltipPrice = item.price;

        // Item ikonunu göster
        if (slot.itemIconImage != null)
        {
            if (item.icon != null)
            {
                slot.itemIconImage.sprite = item.icon;
                slot.itemIconImage.color = Color.white;
                slot.itemIconImage.enabled = true;
            }
            else
            {
                slot.itemIconImage.enabled = false;
            }
        }

        if (slot.soldOutOverlay != null)
            slot.soldOutOverlay.SetActive(false);

        if (slot.buyButton != null)
        {
            var btnLabel = slot.buyButton.GetComponentInChildren<TMP_Text>();

            // DÜZELTME: Eski nameText ve priceText çöpleri tamamen silindi!
            if (btnLabel != null)
                btnLabel.text = "";

            int idx = index;
            slot.buyButton.onClick.RemoveAllListeners();
            slot.buyButton.onClick.AddListener(() => TryBuy(idx));
            slot.buyButton.interactable = true;
        }
    }

    public void TryBuy(int index)
    {
        if (index >= purchased.Count || purchased[index]) return;
        if (RunManager.instance == null) return;
        if (TurnManager.instance != null && TurnManager.instance.IsAnyTargetingActive) return;

        BaseItem item = currentItems[index];
        if (item == null) return;

        if (RunManager.instance.currentGold < item.price)
        {
            // DÜZELTME: Artık paran yetmediğinde ana paran (coinDisplayText) kırmızı yanacak!
            StartCoroutine(FlashText(coinDisplayText));
            return;
        }

        RunManager.instance.currentGold -= item.price;
        if (AudioManager.instance != null) AudioManager.instance.PlayPurchase();
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
                spawnedSlots[i].buyButton.interactable = currentItems[i] != null && RunManager.instance.currentGold >= currentItems[i].price;
        }
    }

    private void RefreshRerollButton()
    {
        if (rerollPriceText != null)
        {
            rerollPriceText.richText = true;
            rerollPriceText.text = "Reroll  <color=#FFD933>" + currentRerollCost + "</color>";
        }
        if (rerollButton != null && RunManager.instance != null)
            rerollButton.interactable = RunManager.instance.currentGold >= currentRerollCost;
    }

    private void SetupCoinIcons()
    {
        Sprite coinSpr = null;
        if (TurnManager.instance != null && TurnManager.instance.coinSprite != null)
            coinSpr = TurnManager.instance.coinSprite;
        if (coinSpr == null)
        {
            var vfx = FindFirstObjectByType<CoinDropVFX>();
            if (vfx != null) coinSpr = vfx.coinSprite;
        }
        if (coinSpr == null) return;

        // Reroll button coin icon — placed manually, no HLG
        if (rerollPriceText != null)
        {
            Transform rerollParent = rerollPriceText.transform.parent;
            if (rerollParent != null && rerollParent.Find("RerollCoinIcon") == null)
            {
                // Keep existing text anchored/stretched, just add icon as sibling
                GameObject rIconGO = new GameObject("RerollCoinIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                rIconGO.transform.SetParent(rerollParent, false);
                rIconGO.layer = gameObject.layer;
                RectTransform rIconRT = rIconGO.GetComponent<RectTransform>();
                // Anchor to right-center of button
                rIconRT.anchorMin = new Vector2(1f, 0.5f);
                rIconRT.anchorMax = new Vector2(1f, 0.5f);
                rIconRT.pivot = new Vector2(1f, 0.5f);
                rIconRT.anchoredPosition = new Vector2(-6f, 0f);
                rIconRT.sizeDelta = new Vector2(18f, 18f);
                Image rImg = rIconGO.GetComponent<Image>();
                rImg.sprite = coinSpr;
                rImg.preserveAspect = true;
                rImg.raycastTarget = false;
            }
        }
    }

    private IEnumerator FlashText(TMP_Text t)
    {
        if (t == null) yield break;
        Color orig = t.color;
        t.color = Color.red;
        yield return new WaitForSeconds(0.5f);
        t.color = orig;
    }
    // ==========================================
    // YENİ: DÜKKANI SİLMEDEN SADECE 1 YENİ SLOT EKLER (Deep Pockets İçin)
    // ==========================================
    public void AddSingleExtraSlot()
    {
        if (shopSlotPrefab == null || shopSlotContainer == null) return;

        // 1. Dükkanda şu an neler var indekslerini bulalım ki aynısı çıkmasın
        List<int> usedIndices = new List<int>();
        foreach (var currentItem in currentItems)
        {
            if (currentItem != null)
            {
                int indexInPool = itemPool.IndexOf(currentItem);
                if (indexInPool != -1) usedIndices.Add(indexInPool);
            }
        }

        // Havuzda eşya kalmadıysa boşuna ekleme yapma
        if (itemPool.Count <= usedIndices.Count) return;

        // 2. Rastgele ve benzersiz yeni bir eşya seç
        int idx;
        int safety = 0;
        do { idx = Random.Range(0, itemPool.Count); if (++safety > 100) break; }
        while (usedIndices.Contains(idx));

        BaseItem newItem = itemPool[idx];

        // 3. Yeni slotu UI'da yarat ve listeye ekle
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

        // 4. Yeni index'i belirle ve listelere kaydet
        int newIndex = spawnedSlots.Count;
        spawnedSlots.Add(slot);
        currentItems.Add(newItem);
        purchased.Add(false);

        // 5. Slotu kur ve parayı kontrol et
        SetupSlot(slot, newIndex, newItem);
        RefreshAffordability();
    }
}