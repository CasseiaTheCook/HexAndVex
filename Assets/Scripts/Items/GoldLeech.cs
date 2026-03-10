using UnityEngine;

[CreateAssetMenu(menuName = "Items/GoldLeech", fileName = "GoldLeech")]
public class GoldLeech : BaseItem
{
    void OnEnable()
    {
        itemName = "Gold-Leech";
        description = "Sonraki düşmandan 2 kat gold düşürt";
        price = 4;
    }

    public override bool Use()
    {
        if (RunManager.instance == null) return false;

        RunManager.instance.doubleGoldNextKill = true;
        return true;
    }
}
