public class SwordDancePerk : BasePerk
{
    public override void ModifyCombat(CombatPayload payload)
    {
        payload.multiplyInsteadOfAdd = true;
        TriggerVisualPop();
    }
}