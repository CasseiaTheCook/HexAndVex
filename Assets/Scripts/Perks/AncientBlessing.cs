using UnityEngine;
using System.Collections.Generic;

public class AncientBlessingPerk : BasePerk
{
    public override void OnAcquire()
    {
        List<BasePerk> upgradablePerks = new List<BasePerk>();

        foreach (var perk in RunManager.instance.activePerks)
        {
            if (perk != this) upgradablePerks.Add(perk);
        }

        // --- DÜZELTME: Güçlendirecek şey bulamazsa pas geçme, B PLANI'nı devreye sok ---
        if (upgradablePerks.Count > 0)
        {
            int randomIndex = Random.Range(0, upgradablePerks.Count);
            BasePerk selectedPerk = upgradablePerks[randomIndex];
            
            selectedPerk.UpgradePerk();
            Debug.Log($"✨ Kadim Kutsama: {selectedPerk.perkName} güçlendirildi!");
            TriggerVisualPop();
        }
        else
        {
            // B PLANI: Eğer güçlendirilecek yetenek yoksa, oyuncuya anında 1 adet YENİ perk ver!
            // Böylece perk boşa gitmemiş olur.
            Debug.Log("⚠️ Güçlendirilecek yetenek yok, Kadim Kutsama yeni bir yetenek hediye ediyor!");
            
            // Rastgele bir perk seç ve doğrudan ekle
            GameObject randomPerk = LevelUpManager.instance.GetRandomPerkByRarity(false);
            if (randomPerk != null)
            {
                RunManager.instance.AddPerk(randomPerk);
            }
            TriggerVisualPop();
        }
    }
}