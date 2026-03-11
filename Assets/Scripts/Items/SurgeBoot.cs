using UnityEngine;

[CreateAssetMenu(menuName = "Items/SurgeBoot", fileName = "SurgeBoot")]
public class SurgeBoot : BaseItem
{
    void OnEnable()
    {
        itemName = "Surge-Boot";
        description = "Next turn you can move up to 2 hexes instead of 1";
        price = 4;
    }

    public override bool Use()
    {
        if (RunManager.instance == null) return false;
        RunManager.instance.surgeBootActive = true;
        if (TurnManager.instance?.player != null)
            TurnManager.instance.player.UpdateHighlights();
        return true;
    }
}
