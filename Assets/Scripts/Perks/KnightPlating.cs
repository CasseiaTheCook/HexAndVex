public class KnightsPlatingPerk : BasePerk
{
    public override void OnAcquire()
    {
        RunManager.instance.armorChance += 0.15f; 
        TriggerVisualPop();
    }
}