public class HolyAegisPerk : BasePerk
{
    public override void OnLevelStart()
    {
        RunManager.instance.hasHolyAegis = true; // Kalkanı yenile
        TriggerVisualPop();
    }
}