public class SwiftActionPerk : BasePerk
{
    public override void OnAcquire()
    {
        // Alttan tire kaldırıldı, RunManager'daki isimle eşleşti
        RunManager.instance.extraMovesPerTurn += 1; 
        TriggerVisualPop();
    }
}
