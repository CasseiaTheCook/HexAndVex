using UnityEngine;

/// <summary>
/// Neuro-Stasis Mist (Rare)
/// Any stun you apply to an enemy lasts +1 extra turn (lv1), +2 extra turns (lv3).
/// The bonus is read by HealthScript when a stun is applied.
/// </summary>
public class NeuroStasisMistPerk : BasePerk
{
    void OnEnable()
    {
        perkName    = "Neuro-Stasis Mist";
        description = "Stuns you apply last +1 extra turn. At level 3: +2 extra turns.";
        rarity      = PerkRarity.Rare;
        maxLevel    = 3;
    }

    /// <summary>Returns how many extra stun turns this perk adds.</summary>
    public int GetStunBonus()
    {
        return currentLevel >= 3 ? 2 : 1;
    }
}
