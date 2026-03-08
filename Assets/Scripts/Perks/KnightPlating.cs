using UnityEngine;

public class KnightsPlatingPerk : BasePerk
{
    // İlk alındığında çalışır (1. Seviye)
    public override void OnAcquire()
    {
        RunManager.instance.armorChance += 0.15f; 
        TriggerVisualPop();
    }

    // YENİ: Kart tekrar seçilirse çalışır (Seviye Atlama)
    public override void Upgrade()
    {
        base.Upgrade(); // Seviyeyi 1 artırır
        
        RunManager.instance.armorChance += 0.15f; // Her seviyede +%15 Zırh şansı daha!
        TriggerVisualPop();
    }
}