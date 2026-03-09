using UnityEngine;

public class MercenarysRestPerk : BasePerk
{
    // YENİ: Kart tekrar seçilirse sadece seviyeyi artır (Matematiği OnSkip içinde halledeceğiz)
    public override void Upgrade()
    {
        base.Upgrade(); 
        TriggerVisualPop();
    }

    public override void OnSkip()
    {
        // Seviye 1'de 3 altın, Seviye 2'de 6 altın, Seviye 3'te 9 altın verir!
        RunManager.instance.currentGold += (1 * currentLevel);
        TriggerVisualPop();
    }
}