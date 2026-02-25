public class UnstoppableForcePerk : BasePerk
{
    public override void ModifyCombat(CombatPayload payload)
    {
        // Örnek: Elindeki her zar için %30 ekstra çarpan kazandırır
        float extraMult = 1.0f + (payload.diceRolls.Count * 0.3f);
        payload.multiplier *= extraMult;
        TriggerVisualPop();
    }
}