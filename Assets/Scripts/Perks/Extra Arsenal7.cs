using UnityEngine;

public class ExtraArsenalPerk : BasePerk
{
    // İlk alındığında çalışır (1. Seviye)
    public override void OnAcquire()
    {
        RunManager.instance.baseDiceCount += 1;
        TriggerVisualPop();
    }

    // YENİ: Kart tekrar seçilirse çalışır (Seviye Atlama)
    public override void Upgrade()
    {
        base.Upgrade(); // Seviyeyi 1 artırır
        
        RunManager.instance.baseDiceCount += 1; // Her seviyede +1 zar daha!
        TriggerVisualPop();
    }
}