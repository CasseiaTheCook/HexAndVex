using UnityEngine;

[CreateAssetMenu(menuName = "Items/HexThorn", fileName = "HexThorn")]
public class HexThorn : BaseItem
{
    void OnEnable()
    {
        itemName = "Hex-Thorn";
        description = "Place a spike trap on any empty hex tile";
        price = 5;
    }

    public override bool Use()
    {
        if (TurnManager.instance == null) return false;
        TurnManager.instance.StartThornPlacement();
        return true;
    }
}
