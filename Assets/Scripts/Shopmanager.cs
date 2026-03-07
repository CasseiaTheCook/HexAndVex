using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

public class Shopmanager : MonoBehaviour
{
    public static Shopmanager instance;

    [Header("Panel")]
    public GameObject shopPanel;

    [Header("Perk Listeleri (LevelUpManager ile aynı)")]
    public List<GameObject> commonPerks;
    public List<GameObject> rarePerks;
    public List<GameObject> epicPerks;
    public List<GameObject> legendaryPerks;

    [Header("UI Slotlar (3 adet)")]
    public Button[] buyButtons;          // 3 adet Satın Al butonu
    public TMP_Text[] itemNameTexts;     // Her slot için isim + açıklama
    public TMP_Text[] itemPriceTexts;    // Her slot için fiyat etiketi
    public Image[] soldOutOverlays;      // Satın alındığında üstüne gelen "Satıldı" görseli (opsiyonel)

    [Header("UI Genel")]
    public TMP_Text coinDisplayText;     // Shopun içindeki coin sayacı
    public Button rerollButton;          // Reroll butonu
    public TMP_Text rerollPriceText;     // Reroll fiyatını gösterir

    [Header("Reroll Ayarları")]
    public int rerollBaseCost = 2;       // İlk reroll maliyeti
    public int rerollCostIncrease = 1;   // Her reroll'da artan miktar

    // Dahili durum
    private List<GameObject> currentItems = new List<GameObject>();
    private int[] currentPrices = new int[3];
    private bool[] purchased = new bool[3];
    private int currentRerollCost;
    private int rerollCount = 0;

    void Awake()
    {
        if (instance == null) instance = this;
    }

    void Start()
    {
        if (rerollButton != null)
            rerollButton.onClick.AddListener(TryReroll);

        if (shopPanel != null)
            shopPanel.SetActive(false);
    }

    // Dungeon bitince TurnManager tarafından çağrılır
    public void OpenShop()
    {
        rerollCount = 0;
        currentRerollCost = rerollBaseCost;

        if (shopPanel != null) shopPanel.SetActive(true);
        GenerateShopItems();
        RefreshCoinDisplay();
        RefreshRerollButton();
    }

    // Dükkanı kapatıp oyuna devam et
    public void LeaveShop()
    {
        if (shopPanel != null) shopPanel.SetActive(false);

        // Hâlâ savaşta düşman varsa oyuna dön, yoksa level-up ekranını aç
        if (TurnManager.instance != null && TurnManager.instance.enemies.Count > 0)
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
            Debug.Log($"Reroll için yeterli coin yok! Gerekli: {currentRerollCost}");
            return;
        }

        RunManager.instance.currentGold -= currentRerollCost;
        rerollCount++;
        currentRerollCost = rerollBaseCost + rerollCount * rerollCostIncrease;

        GenerateShopItems();
        RefreshCoinDisplay();
        RefreshRerollButton();
        Debug.Log($"Reroll yapıldı! Sonraki reroll maliyeti: {currentRerollCost}");
    }

    // -------------------------------------------------------
    // Rastgele ürün oluştur
    // -------------------------------------------------------
    private void GenerateShopItems()
    {
        currentItems.Clear();
        purchased = new bool[3];

        for (int i = 0; i < 3; i++)
        {
            GameObject perk = null;
            int safety = 0;
            int price = 3;
            while (perk == null || currentItems.Contains(perk))
            {
                perk = GetRandomPerkByRarity(out price);
                if (++safety > 100) break;
            }
            currentItems.Add(perk);
            currentPrices[i] = price;

            if (perk == null) continue;

            BasePerk script = perk.GetComponent<BasePerk>();
            if (itemNameTexts != null && i < itemNameTexts.Length && itemNameTexts[i] != null)
                itemNameTexts[i].text = script.perkName + "\n<size=70%>" + script.description + "</size>";

            if (itemPriceTexts != null && i < itemPriceTexts.Length && itemPriceTexts[i] != null)
                itemPriceTexts[i].text = currentPrices[i] + " Coin";

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

        RefreshAffordability();
    }

    // -------------------------------------------------------
    // Satın alma
    // -------------------------------------------------------
    public void TryBuy(int index)
    {
        if (purchased[index]) return;
        if (RunManager.instance == null) return;

        int price = currentPrices[index];

        if (RunManager.instance.currentGold < price)
        {
            StartCoroutine(FlashPrice(index));
            Debug.Log($"Yeterli coin yok! Gerekli: {price}, Mevcut: {RunManager.instance.currentGold}");
            return;
        }

        RunManager.instance.currentGold -= price;
        RunManager.instance.AddPerk(currentItems[index]);
        purchased[index] = true;

        Debug.Log($"Satın alındı: {currentItems[index].GetComponent<BasePerk>().perkName} (-{price} coin)");

        if (buyButtons != null && index < buyButtons.Length && buyButtons[index] != null)
            buyButtons[index].interactable = false;

        if (soldOutOverlays != null && index < soldOutOverlays.Length && soldOutOverlays[index] != null)
            soldOutOverlays[index].gameObject.SetActive(true);

        RefreshCoinDisplay();
        RefreshAffordability();
        RefreshRerollButton();
    }

    // -------------------------------------------------------
    // UI Yardımcıları
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
    // Rastgele perk seç (nadirliğe göre fiyat ata)
    // -------------------------------------------------------
    private GameObject GetRandomPerkByRarity(out int price)
    {
        float roll = Random.Range(0f, 100f);

        if (roll < 5f && legendaryPerks != null && legendaryPerks.Count > 0)
        {
            price = Random.Range(14, 21); // 14-20 coin
            return legendaryPerks[Random.Range(0, legendaryPerks.Count)];
        }
        if (roll < 15f && epicPerks != null && epicPerks.Count > 0)
        {
            price = Random.Range(9, 15); // 9-14 coin
            return epicPerks[Random.Range(0, epicPerks.Count)];
        }
        if (roll < 40f && rarePerks != null && rarePerks.Count > 0)
        {
            price = Random.Range(5, 11); // 5-10 coin
            return rarePerks[Random.Range(0, rarePerks.Count)];
        }

        price = Random.Range(3, 7); // 3-6 coin
        if (commonPerks != null && commonPerks.Count > 0)
            return commonPerks[Random.Range(0, commonPerks.Count)];

        price = 3;
        return null;
    }
}

