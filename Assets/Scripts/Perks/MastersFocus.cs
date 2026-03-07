using UnityEngine;

public class MastersFocusPerk : BasePerk
{
    public override void ModifyCombat(CombatPayload payload)
    {
        bool changed = false;
        for (int i = 0; i < payload.diceRolls.Count; i++)
        {
            // Eğer zar 3 veya altındaysa, kesinlikle 4-6 arası bir değer ver
            if (payload.diceRolls[i] <= 3)
            {
                payload.diceRolls[i] = Random.Range(4, 7);
                changed = true;
            }
        }
        if (changed) TriggerVisualPop();
    }
}
