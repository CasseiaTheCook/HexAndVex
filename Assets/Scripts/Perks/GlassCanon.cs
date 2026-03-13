using UnityEngine;

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
        Debug.Log($"[GlassCanon] Acquired! Max HP set to 3, Current HP: {(TurnManager.instance?.player?.health?.currentHP ?? RunManager.instance.playerCurrentHealth)}");
        TriggerVisualPop();
    }

    public override void ModifyCombat(CombatPayload payload)
    {
        var rm = RunManager.instance;
        if (rm == null) return;

        int maxHP     = rm.playerMaxHealth;
        int currentHP = rm.playerCurrentHealth;
        if (currentHP < 1) currentHP = 1;

        // Glass Cannon: Sağlık ne kadar düşük o kadar yüksek hasar
        // 3/3 HP → 1.0x | 1/3 HP → 3.0x
        float glassCannonMultiplier = (float)maxHP / currentHP;
        payload.multiplier *= glassCannonMultiplier;
        
        Debug.Log($"[GlassCanon] HP: {currentHP}/{maxHP}, Multiplier: {glassCannonMultiplier}x, Final: {payload.multiplier}x");

        TriggerVisualPop();
    }
}
