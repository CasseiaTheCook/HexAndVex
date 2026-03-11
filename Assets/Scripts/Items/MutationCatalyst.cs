using UnityEngine;

[CreateAssetMenu(menuName = "Items/MutationCatalyst", fileName = "MutationCatalyst")]
public class MutationCatalyst : BaseItem
{
    void OnEnable()
    {
        itemName = "Mutation Catalyst";
        description = "Once acquired, you gain the right to reroll perks on the next perk selection screen.";
        price = 6;
    }

    public override bool Use()
    {
        if (RunManager.instance == null) return false;
        RunManager.instance.hasPerkReroll = true;
        return true;
    }
}
