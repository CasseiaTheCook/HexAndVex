public class AdrenalSurgePerk : BasePerk
{
    public override void ModifyCombat(CombatPayload payload)
    {
        payload.multiplier *= 2.0f;
        if (TurnManager.instance != null && !TurnManager.instance.skipDiceVisuals)
            TriggerVisualPop();
    }
}