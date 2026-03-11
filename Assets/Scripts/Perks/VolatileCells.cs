public class VolatileCellsPerk : BasePerk
{
    void OnEnable()
    {
        maxLevel = 4;
    }

    public override void ModifyCombat(CombatPayload payload)
    {
        payload.triggerExplosion = true;
        payload.explosionDamagePercent = currentLevel * 0.25f; // Lv1=%25, Lv2=%50, Lv3=%75, Lv4=%100
        if (TurnManager.instance != null && !TurnManager.instance.skipDiceVisuals)
            TriggerVisualPop();
    }
}
