using UnityEngine;

public class CapitalistPunchPerk : BasePerk
{
    public override void ModifyCombat(CombatPayload payload)
    {
        if (RunManager.instance != null)
        {
            int currentGold = RunManager.instance.currentGold;
            int bonus = currentGold / 10; // Her 10 altın için 1 hasar

            if (bonus > 0)
            {
                payload.flatBonus += bonus;
                TriggerVisualPop();
            }
        }
    }
}