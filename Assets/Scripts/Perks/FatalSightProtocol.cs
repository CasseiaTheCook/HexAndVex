using UnityEngine;

/// <summary>
/// Fatal Sight Protocol (Legendary)
/// Attacks are always critical hits.
/// criticalChance is converted to extra criticalDamageMultiplier (×1.5 per level stacking).
/// </summary>
public class FatalSightProtocolPerk : BasePerk
{
    void OnEnable()
    {
        perkName    = "Fatal Sight Protocol";
        description = "All attacks are Critical Hits. Each 1% Crit Chance converts to +1.5% Crit Damage per level.";
        rarity      = PerkRarity.Legendary;
        maxLevel    = 3;
        priority    = 1; // Önce çalışsın, sonraki perkler critHit=true üzerine eklensin
    }

    public override void ModifyCombat(CombatPayload payload)
    {
        var rm = RunManager.instance;
        if (rm == null) return;

        // Her saldırıda: birikmiş critChance varsa dönüştür
        // (sonradan alınan crit perkleri de bu şekilde yakalanır)
        if (rm.criticalChance > 0f)
        {
            // critChance → critDamage: her %1 crit şansı = +1.5% crit hasar, level ile çarpılır
            float bonus = rm.criticalChance * 1.5f * currentLevel;
            rm.criticalDamageMultiplier += bonus;
            rm.criticalChance = 0f;
        }

        payload.isCriticalHit = true;
    }
}
