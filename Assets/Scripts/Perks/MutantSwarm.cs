using UnityEngine;

public class MutantSwarmPerk : BasePerk
{
    // YENİ: Kart tekrar seçilirse seviye artsın
    public override void Upgrade()
    {
        base.Upgrade();
        TriggerVisualPop();
    }

    public override void ModifyCombat(CombatPayload payload)
    {
        // Seviye 1'de %25 (0.25f), Seviye 2'de %50 (0.50f), Seviye 3'te %75 (0.75f) çarpan ekler!
        float bonusPerDie = 0.25f * currentLevel;
        float extraMult = 1.0f + (payload.diceRolls.Count * bonusPerDie);
        
        payload.multiplier *= extraMult;
        TriggerVisualPop();
    }
}
