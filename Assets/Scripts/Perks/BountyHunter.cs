using UnityEngine; 
using System;      
using System.Collections.Generic; 

public class BountyHunterPerk : BasePerk
{
    // İlk alındığında çalışır (1. Seviye)
    public override void OnAcquire()
    {
        RunManager.instance.bonusGold += 2; // DÜZELTME: Eski kodlarında "bonusGold" olarak geçiyordu, onu kullandım.
        TriggerVisualPop();
    }

    // YENİ: Kart tekrar seçilirse çalışır (2. ve 3. Seviyeler)
    public override void Upgrade()
    {
        base.Upgrade(); // Seviyeyi 1 artırır ve konsola "Seviye atladı" yazar
        
        RunManager.instance.bonusGold += 2; // Her seviyede +2 altın daha eklensin
        TriggerVisualPop();
    }
}