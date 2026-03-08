public class SwiftActionPerk : BasePerk
{
    public override void OnAcquire()
    {
        RunManager.instance.extraMovesPerTurn += 1;
        TriggerVisualPop();
    }

    public override void Upgrade()
    {
        base.Upgrade(); // Seviyeyi 1 artırır
        RunManager.instance.extraMovesPerTurn += 1; // Her seviyede 1 hak daha ver!
    }
}