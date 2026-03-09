using UnityEngine;

public class ToxinEdgePerk : BasePerk
{
    // YENİ: Kart tekrar seçilirse sadece seviyeyi artır
    public override void Upgrade()
    {
        base.Upgrade();
        TriggerVisualPop();
    }

    public override void ModifyCombat(CombatPayload payload)
    {
        for (int i = 0; i < payload.diceRolls.Count; i++)
        {
            // Zarın değerine direkt yeteneğin seviyesini ekle (Lv 1 ise +1, Lv 3 ise +3 ekler)
            payload.diceRolls[i] += currentLevel;
        }
        TriggerVisualPop();
    }
}