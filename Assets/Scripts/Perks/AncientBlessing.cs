public class AncientBlessingPerk : BasePerk
{
    public override void OnAcquire()
    {
        if (RunManager.instance.activePerks.Count > 1)
        {
            // Kendisi hariç rastgele bir perk bul ve güçlendir
            // ...
            TriggerVisualPop();
        }
    }
}