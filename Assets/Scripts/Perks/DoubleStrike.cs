public class DoubleStrikePerk : BasePerk
{
    public override void ModifyCombat(CombatPayload payload)
    {
        payload.multiplier *= 2.0f;
        TriggerVisualPop();
    }
}