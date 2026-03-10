using UnityEngine;

[CreateAssetMenu(menuName = "Items/MutaGen", fileName = "MutaGen")]
public class MutaGen : BaseItem
{
    void OnEnable()
    {
        itemName = "Muta-Gen";
        description = "Restore 2 HP";
        price = 5;
    }

    public override bool Use()
    {
        if (TurnManager.instance?.player?.health == null) return false;
        if (TurnManager.instance.player.health.currentHP >= TurnManager.instance.player.health.maxHP) return false;

        TurnManager.instance.player.health.Heal(2);
        if (RunManager.instance != null)
            RunManager.instance.playerCurrentHealth = Mathf.Min(
                RunManager.instance.playerCurrentHealth + 2, RunManager.instance.playerMaxHealth);
        return true;
    }
}
