using UnityEngine;
using System.Collections.Generic;

public class RunManager : MonoBehaviour
{
    public static RunManager instance;

    [Header("Run Progression")]
    public int currentLevel = 1; // Kaçıncı odadayız?

    [Header("Run Stats")]

    public int currentGold = 0;
    public int playerMaxHealth = 3;
    public int playerCurrentHealth = 3;
    public int baseDiceCount = 2;
    public int maxTurns = 1;
    public int collectibleSlots = 3;
    public float armorChance = 0f; // Knight's Plating için (%15 hasar engelleme ihtimali)
    public int bonusGoldPerKill = 0; // Bounty Hunter için

    [Header("Perk Değişkenleri")]
    public int bonusGold = 0;        // Bounty Hunter için
    public float dodgeChance = 0f;   // Knight's Plating için
    public bool hasBioBarrier = false; // Bio-Barrier kalkanı için
    public int skipBonusGold = 0;    // Mercenary's Rest için

    [Header("Combat Stats")]
    public float criticalChance = 0f; // 0.0 to 1.0
    public float criticalDamageMultiplier = 1.5f;

    [Header("Active Perks")]
    public Transform perkUIContainer; // Assign a Horizontal Layout Group UI panel here!
    public List<BasePerk> activePerks = new List<BasePerk>();

    [Header("Item Buff'ları (Tek Kullanımlık)")]
    public int bonusDiceNextCombat = 0;
    public bool doubleGoldNextKill = false;
    public bool doubleDamageNextCombat = false;
    public bool cleaveNextCombat = false;
    public bool surgeBootNextTurn = false;
    [HideInInspector] public bool surgeBootActive = false; // Bu tur 2 hex hareket edebilir mi?

    [Header("Legendary Stats")]
    public int extraMovesPerTurn = 0; // Swift Action ile artacak (Normalde 0)
    public int remainingMoves;       // O tur içindeki kalan hamle hakkı

    [Header("Run Statistics")]
    public int totalEnemiesKilled = 0;
    public int totalDamageDealt = 0;
    public int totalDamageReceived = 0;
    public int totalTurnsPlayed = 0;
    public int totalDiceRolled = 0;
    public int totalGoldEarned = 0;
    void Awake()
    {
        // The legendary Singleton pattern for cross-scene persistence
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);

            // If the perk container is a child of this object, it survives too!
            if (perkUIContainer != null)
                DontDestroyOnLoad(perkUIContainer.root.gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Called when the player selects a perk from the Level Up screen
    public void AddPerk(GameObject perkPrefab)
    {
        BasePerk prefabScript = perkPrefab.GetComponent<BasePerk>();

        // Oyuncunun elinde bu perk tipinden (Örn: ReflexFiberPerk) zaten var mı kontrol et
        BasePerk existingPerk = activePerks.Find(p => p.GetType() == prefabScript.GetType());

        if (existingPerk != null)
        {
            // ZATEN VARSA: Yeni obje yaratma, sadece olanı YÜKSELT!
            existingPerk.Upgrade();
        }
        else
        {
            // İLK DEFA ALINIYORSA: Obje olarak yarat ve listeye ekle
            GameObject newPerkObj = Instantiate(perkPrefab, transform);
            BasePerk newPerk = newPerkObj.GetComponent<BasePerk>();
            activePerks.Add(newPerk);
            newPerk.OnAcquire();
        }
    }
    public string GetStatsSummary()
    {
        return $"Turns Played: {totalTurnsPlayed}\n" +
               $"Dice Rolled: {totalDiceRolled}\n" +
               $"Damage Dealt: {totalDamageDealt}\n" +
               $"Enemies Killed: {totalEnemiesKilled}\n" +
               $"Gold Earned: {totalGoldEarned}";
    }

    public string GetPerksSummary()
    {
        if (activePerks.Count == 0) return "None";
        var sb = new System.Text.StringBuilder();
        foreach (var p in activePerks)
            sb.AppendLine($"{p.perkName}  Lv {p.currentLevel}");
        return sb.ToString().TrimEnd();
    }
}
