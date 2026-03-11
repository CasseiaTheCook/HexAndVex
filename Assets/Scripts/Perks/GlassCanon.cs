public class GlassCanonPerk : BasePerk
{
    void OnEnable()
    {
        rarity = PerkRarity.Rare;
        maxLevel = 1;
    }

    public override void OnAcquire()
    {
        RunManager.instance.playerMaxHealth = 3;
        if (RunManager.instance.playerCurrentHealth > 3)
            RunManager.instance.playerCurrentHealth = 3;
        TriggerVisualPop();
    }

    public override void ModifyCombat(CombatPayload payload)
    {
        if (isDisabled) return;
        payload.multiplier *= 2f;
        TriggerVisualPop();
    }
}
