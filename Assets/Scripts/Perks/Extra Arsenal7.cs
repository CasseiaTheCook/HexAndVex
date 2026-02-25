public class ExtraArsenalPerk : BasePerk
{
    public override void OnAcquire()
    {
        RunManager.instance.baseDiceCount += 1;
        TriggerVisualPop();
    }
}