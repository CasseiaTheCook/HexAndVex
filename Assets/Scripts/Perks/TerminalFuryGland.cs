using UnityEngine;

/// <summary>
/// Terminal Fury Gland (Legendary)
/// Always active. Multiplier = 1 + (missingHP + 1) * 0.5 * level
/// Lv1: fullHP=x1.5, -1hp=x2.0, -2hp=x2.5 ...
/// Lv2: fullHP=x2.0, -1hp=x3.0, -2hp=x4.0 ...
/// Lv3: fullHP=x2.5, -1hp=x4.0, -2hp=x5.5 ...
/// Glass Canon (max 3hp) scales identically relative to max.
/// </summary>
public class TerminalFuryGlandPerk : BasePerk
{
    void OnEnable()
    {
        perkName    = "Terminal Fury Gland";
        description = "Always deal bonus damage. The lower your HP, the stronger the multiplier.";
        rarity      = PerkRarity.Legendary;
        maxLevel    = 3;
        priority    = 15;
    }

    public override void ModifyCombat(CombatPayload payload)
    {
        var rm = RunManager.instance;
        var tm = TurnManager.instance;
        if (rm == null || tm == null || tm.player == null) return;

        int effectiveMax = rm.playerMaxHealth;
        int currentHP   = tm.player.health.currentHP;
        int missingHP   = effectiveMax - currentHP;

        // Always gives a bonus: base from being alive, more from missing HP
        float multiplier = 1f + (missingHP + 1) * 0.5f * currentLevel;
        payload.multiplier *= multiplier;

        TriggerVisualPop();
    }
}
