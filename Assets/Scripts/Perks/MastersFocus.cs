using UnityEngine;

public class MastersFocusPerk : BasePerk
{
    public override void ModifyCombat(CombatPayload payload)
    {
        bool triggered = false;
        for (int i = 0; i < payload.diceRolls.Count; i++)
        {
            if (payload.diceRolls[i] < 3)
            {
                payload.diceRolls[i] = Random.Range(1, 7); // Yeniden at!
                triggered = true;
            }
        }
        if (triggered) TriggerVisualPop();
    }
}