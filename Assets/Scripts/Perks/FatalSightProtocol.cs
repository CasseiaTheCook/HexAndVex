using UnityEngine;

/// <summary>
/// Fatal Sight Protocol (Legendary)
/// Attacks are always critical hits.
/// criticalChance is converted to extra criticalDamageMultiplier (1:1).
/// </summary>
public class FatalSightProtocolPerk : BasePerk
{
    void OnEnable()
    {
        perkName    = "Fatal Sight Protocol";
        description = "All attacks are Critical Hits. Each 1% Crit Chance converts to +1% Crit Damage.";
        rarity      = PerkRarity.Legendary;
        maxLevel    = 1;
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
            // critChance → critDamage: 1:1 dönüşüm (0.10 critChance = +0.10 critDamage)
            rm.criticalDamageMultiplier += rm.criticalChance;
            rm.criticalChance = 0f;
        }

        payload.isCriticalHit = true;
    }
}
