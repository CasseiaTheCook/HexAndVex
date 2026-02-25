public class MercenarysRestPerk : BasePerk
{
    public override void OnSkip()
    {
        RunManager.instance.currentGold += 3;
        TriggerVisualPop();
    }
}