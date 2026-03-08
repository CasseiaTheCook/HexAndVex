using UnityEngine; // Unity motoru için
using System;      // Temel fonksiyonlar için
using System.Collections.Generic; // Listeler için
public class CalculatedAmbushPerk : BasePerk
{
    public int storedExtraDices = 0;

    public override void OnSkip()
    {
        storedExtraDices++;
        Debug.Log($"🎯 Ambush: 1 zar birikti! Toplam: {storedExtraDices}");
        TriggerVisualPop();
    }
    // ModifyCombat artık burada değil, TurnManager'da yönetiliyor!
}