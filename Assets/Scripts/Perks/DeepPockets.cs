public class DeepPocketsPerk : BasePerk
{
    public override void OnAcquire()
    {
        RunManager.instance.collectibleSlots += 1;
        TriggerVisualPop();
    }
}