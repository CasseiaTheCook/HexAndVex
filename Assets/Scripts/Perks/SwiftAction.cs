public class SwiftActionPerk : BasePerk
{
    public override void OnAcquire()
    {
        RunManager.instance.extraMovesPerTurn += 1;
        TriggerVisualPop();
    }
}