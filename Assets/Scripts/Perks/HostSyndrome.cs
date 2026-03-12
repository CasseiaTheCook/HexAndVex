using UnityEngine;

/// <summary>
/// Host Syndrome (Epic) — One-time purchase
/// Gain +1 to each die roll for every enemy currently adjacent to you.
/// </summary>
public class HostSyndromePerk : BasePerk
{
    void OnEnable()
    {
        perkName     = "Host Syndrome";
        description  = "Gain +1 to each die roll for every enemy adjacent to you.";
        rarity       = PerkRarity.Epic;
        maxLevel     = 1;
        isRerollPerk = false;
        priority     = 5;
    }

    public override void ModifyCombat(CombatPayload payload)
    {
        var tm = TurnManager.instance;
        if (tm == null) return;

        int adjacentCount = tm.GetAdjacentEnemies(tm.player.GetCurrentCellPosition()).Count;
        if (adjacentCount <= 0) return;

        for (int i = 0; i < payload.diceRolls.Count; i++)
            payload.diceRolls[i] += adjacentCount;

        TriggerVisualPop();
    }
}
