using UnityEngine; // Unity motoru için
using System;      // Temel fonksiyonlar için
using System.Collections.Generic; // Listeler için
public class BountyHunterPerk : BasePerk
{
    public override void OnAcquire()
    {
        RunManager.instance.bonusGoldPerKill += 2; // Her düşman 2 altın fazla verir
        TriggerVisualPop();
    }
}