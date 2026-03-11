using UnityEngine;
using System.Linq;

public class DoubleOrNothingPerk : BasePerk
{
    public override void OnAcquire()
    {
        priority = 50; // En son hasar hesaplanırken baksın
    }

    public override void ModifyCombat(CombatPayload payload)
    {
        int total = payload.diceRolls.Sum();

        if (total % 2 == 0)
        {
            // Çiftse ikiye katla
            payload.multiplier *= 2f;
            if (TurnManager.instance != null && !TurnManager.instance.skipDiceVisuals)
                TriggerVisualPop();
        }

    }
}
