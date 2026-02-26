public class VeteransVitalityPerk : BasePerk
{
    public override void OnLevelStart()
    {
        RunManager.instance.playerMaxHealth += 1;
        RunManager.instance.playerCurrentHealth += 1;
        TriggerVisualPop();
    }
}