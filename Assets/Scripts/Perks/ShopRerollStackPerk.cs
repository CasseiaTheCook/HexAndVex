using UnityEngine;

public class ShopRerollStackPerk : BasePerk
{
    private int rerollStack = 0;

    public override void OnAcquire()
    {
        perkName = "Reroll Stack";
        description = "Her shop reroll'da +1 stack kazandıkça, her zarında o kadar bonus damage ekle. Güçlü zar stack mekanizması!";
        priority = 30;
        rerollStack = 0;
    }

    public override void OnShopReroll()
    {
        rerollStack++;
        TriggerVisualPop();
        Debug.Log($"[ShopRerollStackPerk] Reroll Stack: {rerollStack}");
    }

    public override void ModifyCombat(CombatPayload payload)
    {
        if (rerollStack <= 0) return;

        // İlk zara stack kadar bonus ekle
        if (payload.diceRolls.Count > 0)
        {
            payload.diceRolls[0] += rerollStack;
            // Eğer 6'yı geçerse, 6'da sabitle
            if (payload.diceRolls[0] > 6)
                payload.diceRolls[0] = 6;

            Debug.Log($"[ShopRerollStackPerk] İlk zara +{rerollStack} eklendi. Yeni değer: {payload.diceRolls[0]}");
        }
    }

    public override void OnLevelStart()
    {
        // Seviye başlarında stackı sıfırla (isteğe göre değiştirebilirsin)
        rerollStack = 0;
        Debug.Log("[ShopRerollStackPerk] Seviye başladı, stack reset edildi.");
    }
}
