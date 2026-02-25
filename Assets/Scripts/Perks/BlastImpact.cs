public class BlastImpactPerk : BasePerk
{
    public override void ModifyCombat(CombatPayload payload)
    {
        payload.triggerExplosion = true; // TurnManager bunu okuyup hasar alan düşmanların etrafında alan hasarı tetikleyecek
        TriggerVisualPop();
    }
}