using UnityEngine;

[CreateAssetMenu(menuName = "Items/NecroShot", fileName = "NecroShot")]
public class NecroShot : BaseItem
{
    void OnEnable()
    {
        itemName = "Necro-Shot";
        description = "Mapteki istediğin düşmanı anında öldür";
        price = 10;
    }

    public override bool Use()
    {
        if (TurnManager.instance == null) return false;

        // Oyuncunun bir düşman seçmesini beklemek için modu aç
        TurnManager.instance.StartNecroShotTargeting();
        return true;
    }
}
