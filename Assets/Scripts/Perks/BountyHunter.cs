public class BountyHunterPerk : BasePerk
{
    public override void OnAcquire()
    {
        RunManager.instance.bonusGoldPerKill += 2; // Her düşman 2 altın fazla verir
        TriggerVisualPop();
    }
}