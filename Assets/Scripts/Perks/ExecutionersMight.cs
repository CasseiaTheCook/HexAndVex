using UnityEngine;

public class ExecutionersMightPerk : BasePerk
{
    // İlk alındığında çalışır (1. Seviye)
    public override void OnAcquire()
    {
        RunManager.instance.criticalDamageMultiplier += 0.5f;
        TriggerVisualPop();
    }

    // YENİ: Kart tekrar seçilirse çalışır (Seviye Atlama)
    public override void Upgrade()
    {
        base.Upgrade(); // Seviyeyi 1 artırır
        
        RunManager.instance.criticalDamageMultiplier += 0.5f; // Her seviyede +0.5x daha ekle!
        TriggerVisualPop();
    }
}