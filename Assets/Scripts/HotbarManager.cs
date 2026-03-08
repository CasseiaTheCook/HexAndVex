using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum HotbarItemType
{
    None,
    HealthPotion,   // +1 HP
    StrongPotion,   // +2 HP
    GoldBag,        // +6 coin
    EnergyDrink,    // +1 hamle
    BattleSpell     // +%15 kritik
}

public class HotbarManager : MonoBehaviour
{
    public static HotbarManager instance;

    [Header("Hotbar Slotlari")]
    public Button[] hotbarButtons;      // Inspector'da 4 buton ata
    public TMP_Text[] hotbarNameTexts;  // Her slot icin isim etiketi
    public GameObject[] emptyOverlays;  // Slot boskken gosterilen overlay

    public int maxSlots = 4;

    void Awake()
    {
        if (instance == null) instance = this;
    }

    void Start()
    {
        if (hotbarButtons != null)
        {
            for (int i = 0; i < hotbarButtons.Length; i++)
            {
                int idx = i;
                hotbarButtons[i].onClick.RemoveAllListeners();
                hotbarButtons[i].onClick.AddListener(() => UseItem(idx));
            }
        }
        RefreshUI();
    }

    public bool CanAddItem()
    {
        return RunManager.instance != null && RunManager.instance.hotbarItems.Count < maxSlots;
    }

    public bool AddItem(HotbarItemType type)
    {
        if (!CanAddItem()) return false;
        RunManager.instance.hotbarItems.Add(type);
        RefreshUI();
        return true;
    }

    public void UseItem(int index)
    {
        if (RunManager.instance == null) return;
        if (TurnManager.instance != null && !TurnManager.instance.isPlayerTurn) return;
        if (index >= RunManager.instance.hotbarItems.Count) return;

        HotbarItemType type = RunManager.instance.hotbarItems[index];
        ApplyEffect(type);
        RunManager.instance.hotbarItems.RemoveAt(index);
        RefreshUI();
    }

    private void ApplyEffect(HotbarItemType type)
    {
        switch (type)
        {
            case HotbarItemType.HealthPotion:
                RunManager.instance.playerCurrentHealth = Mathf.Min(
                    RunManager.instance.playerCurrentHealth + 1, RunManager.instance.playerMaxHealth);
                TurnManager.instance?.player?.health?.Heal(1);
                break;

            case HotbarItemType.StrongPotion:
                RunManager.instance.playerCurrentHealth = Mathf.Min(
                    RunManager.instance.playerCurrentHealth + 2, RunManager.instance.playerMaxHealth);
                TurnManager.instance?.player?.health?.Heal(2);
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
        Debug.Log($"Hotbar item used: {type}");
    }

    public void RefreshUI()
    {
        if (hotbarButtons == null) return;
        var items = RunManager.instance != null ? RunManager.instance.hotbarItems : null;

        for (int i = 0; i < hotbarButtons.Length; i++)
        {
            bool hasItem = items != null && i < items.Count;

            if (hotbarNameTexts != null && i < hotbarNameTexts.Length && hotbarNameTexts[i] != null)
                hotbarNameTexts[i].text = hasItem ? GetItemName(items[i]) : "";

            if (hotbarButtons[i] != null)
                hotbarButtons[i].interactable = hasItem;

            if (emptyOverlays != null && i < emptyOverlays.Length && emptyOverlays[i] != null)
                emptyOverlays[i].SetActive(!hasItem);
        }
    }

    public static string GetItemName(HotbarItemType type) => type switch
    {
        HotbarItemType.HealthPotion => "Sağlık İksiri",
        HotbarItemType.StrongPotion => "Güçlü İksir",
        HotbarItemType.GoldBag      => "Altın Cüzdan",
        HotbarItemType.EnergyDrink  => "Enerji İçeceği",
        HotbarItemType.BattleSpell  => "Savaş Büyüsü",
        _                           => "?"
    };
}
