public class VeteransVitalityPerk : BasePerk
{
    public override void OnLevelStart()
    {
        RunManager.instance.playerMaxHealth += 1;
        TurnManager.instance.player.health.Heal(1);
        TriggerVisualPop();
    }
}