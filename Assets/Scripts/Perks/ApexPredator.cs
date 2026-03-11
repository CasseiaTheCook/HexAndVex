using UnityEngine;

public class ApexPredatorPerk : BasePerk
{
    public override void ModifyCombat(CombatPayload payload)
    {
        payload.multiplier *= 5.0f; // 6 Katı devasa hasar
        
        // Zarladığın her zar sayısı kadar çarpandan düşer
        float penalty = payload.diceRolls.Count * 1.0f; 
        payload.multiplier -= penalty;
        
        // Çarpanın eksiye veya sıfıra düşmemesini garantiye al (en kötü ihtimal 1x kalır)
        payload.multiplier = Mathf.Max(payload.multiplier, 1.0f);
        
        if (TurnManager.instance != null && !TurnManager.instance.skipDiceVisuals)
            TriggerVisualPop();
    }
}