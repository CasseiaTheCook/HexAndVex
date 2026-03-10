using UnityEngine;

public class NeuroAimPerk : BasePerk
{
    // İlk alındığında çalışır (1. Seviye)
    public override void OnAcquire()
    {
        RunManager.instance.criticalChance += 0.25f;
        TriggerVisualPop();
    }

    // YENİ: Kart tekrar seçilirse çalışır (Seviye Atlama)
    public override void Upgrade()
    {
        base.Upgrade(); // Seviyeyi 1 artırır
        
        RunManager.instance.criticalChance += 0.25f; // Her seviyede +%25 Kritik şansı daha!
        TriggerVisualPop();
    }
}