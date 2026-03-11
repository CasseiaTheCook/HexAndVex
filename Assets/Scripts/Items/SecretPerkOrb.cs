using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Items/SecretPerkOrb", fileName = "SecretPerkOrb")]
public class SecretPerkOrb : BaseItem
{
    [Header("Secret Perk Havuzu")]
    public List<GameObject> secretPerkPool = new List<GameObject>();

    void OnEnable()
    {
        itemName = "??? Orb";
        description = "A mysterious orb pulsing with unknown energy. Grants a secret mutation.";
        price = 8;
    }

    public override bool Use()
    {
        if (RunManager.instance == null || secretPerkPool.Count == 0) return false;

        // Henüz max seviyeye ulaşmamış secret perkleri filtrele
        List<GameObject> available = new List<GameObject>();
        foreach (var perkPrefab in secretPerkPool)
        {
            if (perkPrefab == null) continue;
            BasePerk prefabScript = perkPrefab.GetComponent<BasePerk>();
            if (prefabScript == null) continue;

            BasePerk existing = RunManager.instance.activePerks.Find(p => p.GetType() == prefabScript.GetType());
            if (existing != null && existing.currentLevel >= existing.maxLevel) continue;

            available.Add(perkPrefab);
        }

        if (available.Count == 0) return false;

        // Rastgele bir secret perk seç ve ver
        GameObject chosen = available[Random.Range(0, available.Count)];
        RunManager.instance.AddPerk(chosen);

        return true;
    }
}
