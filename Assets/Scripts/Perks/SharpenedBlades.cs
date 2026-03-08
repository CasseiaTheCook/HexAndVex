public class SharpenedBladesPerk : BasePerk
{
    public override void ModifyCombat(CombatPayload payload)
    {
        for (int i = 0; i < payload.diceRolls.Count; i++)
        {
            payload.diceRolls[i] += 1;
        }
        TriggerVisualPop();
    }
}