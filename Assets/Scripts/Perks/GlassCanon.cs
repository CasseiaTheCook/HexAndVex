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

        // Sahnedeki player health component'ini de güncelle
        if (TurnManager.instance != null && TurnManager.instance.player != null)
        {
            var h = TurnManager.instance.player.health;
            h.maxHP = 3;
            if (h.currentHP > 3) h.currentHP = 3;
            h.updateHealth();
        }
        TriggerVisualPop();
    }

    public override void ModifyCombat(CombatPayload payload)
    {
        var rm = RunManager.instance;
        var tm = TurnManager.instance;
        if (rm == null || tm == null || tm.player == null) return;

        int maxHP     = rm.playerMaxHealth;
        int currentHP = tm.player.health.currentHP;
        if (currentHP < 1) currentHP = 1;

        float multiplier = (float)maxHP / currentHP;
        payload.multiplier *= multiplier;

        TriggerVisualPop();
    }
}
