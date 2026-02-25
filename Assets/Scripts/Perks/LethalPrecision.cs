public class LethalPrecisionPerk : BasePerk
{
    public override void OnAcquire()
    {
        RunManager.instance.criticalChance += 0.25f;
        TriggerVisualPop();
    }
}