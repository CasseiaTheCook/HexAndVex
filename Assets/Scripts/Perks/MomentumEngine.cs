using UnityEngine;

public class MomentumEnginePerk : BasePerk
{
    public override void OnAcquire()
    {
        priority = 5; 
    }

    public override void ModifyCombat(CombatPayload payload)
    {
        // Şimdilik oyuncunun attığı adım sayısı yerine, kalan hareket hakkını hasara dönüştürelim
        if (RunManager.instance != null)
        {
            int bonus = (RunManager.instance.extraMovesPerTurn - RunManager.instance.remainingMoves) * 2;
            if (bonus > 0)
            {
                payload.flatBonus += bonus;
                TriggerVisualPop();
            }
        }
    }
}