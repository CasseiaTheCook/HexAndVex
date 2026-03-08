public class BlastImpactPerk : BasePerk
{
    public override void ModifyCombat(CombatPayload payload)
    {
        payload.triggerExplosion = true; 
        // İpucu: TurnManager.cs içindeki TriggerExplosion metoduna hasar parametresi eklersen 
        // level * 2 gibi yüksek hasarlar vurdurabilirsin.
        TriggerVisualPop();
    }
}
