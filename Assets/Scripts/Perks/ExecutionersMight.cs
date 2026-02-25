public class ExecutionersMightPerk : BasePerk
{
    public override void OnAcquire()
    {
        RunManager.instance.criticalDamageMultiplier += 0.5f;
        TriggerVisualPop();
    }
}