using UnityEngine;

[CreateAssetMenu(menuName = "Items/NecroShot", fileName = "NecroShot")]
public class NecroShot : BaseItem
{
    void OnEnable()
    {
        itemName = "Necro-Shot";
        description = "Instantly kill any enemy on the map";
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
