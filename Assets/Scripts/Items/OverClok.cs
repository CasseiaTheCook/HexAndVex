using UnityEngine;

[CreateAssetMenu(menuName = "Items/OverClok", fileName = "OverClok")]
public class OverClok : BaseItem
{
    void OnEnable()
    {
        itemName = "Over-Clok";
        description = "Sıradaki ilk zar toplamının 2 katı hasar ver";
        price = 7;
    }

    public override bool Use()
    {
        if (RunManager.instance == null) return false;

        RunManager.instance.doubleDamageNextCombat = true;
        return true;
    }
}
