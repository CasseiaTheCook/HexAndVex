using UnityEngine;

[CreateAssetMenu(menuName = "Items/BioVial", fileName = "BioVial")]
public class BioVial : BaseItem
{
    void OnEnable()
    {
        itemName = "Bio-Vial";
        description = "1 can yenile";
        price = 3;
    }

    public override bool Use()
    {
        if (TurnManager.instance?.player?.health == null) return false;
        if (TurnManager.instance.player.health.currentHP >= TurnManager.instance.player.health.maxHP) return false;

        TurnManager.instance.player.health.Heal(1);
        if (RunManager.instance != null)
            RunManager.instance.playerCurrentHealth = Mathf.Min(
                RunManager.instance.playerCurrentHealth + 1, RunManager.instance.playerMaxHealth);
        return true;
    }
}
