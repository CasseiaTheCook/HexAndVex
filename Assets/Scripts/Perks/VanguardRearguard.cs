public class VanguardRearguardPerk : BasePerk
{
    public override void ModifyCombat(CombatPayload payload)
    {
        if (payload.diceRolls.Count > 0)
        {
            payload.diceRolls[0] += 4; // İlk zar
            
            if (payload.diceRolls.Count > 1)
            {
                payload.diceRolls[payload.diceRolls.Count - 1] += 4; // Son zar
            }
            TriggerVisualPop();
        }
    }
}