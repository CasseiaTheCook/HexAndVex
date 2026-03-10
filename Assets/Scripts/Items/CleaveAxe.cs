using UnityEngine;

[CreateAssetMenu(menuName = "Items/CleaveAxe", fileName = "CleaveAxe")]
public class CleaveAxe : BaseItem
{
    void OnEnable()
    {
        itemName = "Cleave-Axe";
        description = "Next attack deals full damage to all adjacent enemies without splitting";
        price = 7;
    }

    public override bool Use()
    {
        if (RunManager.instance == null) return false;
        RunManager.instance.cleaveNextCombat = true;
        return true;
    }
}
