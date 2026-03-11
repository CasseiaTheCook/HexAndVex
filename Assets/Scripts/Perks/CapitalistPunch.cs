using UnityEngine;

public class CapitalistPunchPerk : BasePerk
{
    void OnEnable() { maxLevel = 1; }

    public override void ModifyCombat(CombatPayload payload)
    {
        if (RunManager.instance == null) return;

        int bonus = RunManager.instance.currentGold / 10;
        if (bonus > 0)
        {
            for (int i = 0; i < payload.diceRolls.Count; i++)
                payload.diceRolls[i] += bonus;
            if (TurnManager.instance != null && !TurnManager.instance.skipDiceVisuals)
                TriggerVisualPop();
        }
    }
}
