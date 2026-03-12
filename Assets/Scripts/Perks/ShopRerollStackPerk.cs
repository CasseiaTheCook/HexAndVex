using UnityEngine;

public class ShopRerollStackPerk : BasePerk
{
    public override void OnAcquire()
    {
        perkName = "Genetic Cartel";
        description = GetDescription();
        priority = 30;
    }

    public override void OnShopReroll()
    {
        // Stack artık RunManager'da tutulup TurnManager'da zarlara ekleniyor
        // Sadece açıklama güncelle ve visual feedback ver
        description = GetDescription();
        TriggerVisualPop();
    }

    public override void ModifyCombat(CombatPayload payload)
    {
        // Reroll stack bonusu artık doğrudan zar atılırken ekleniyor (TurnManager)
        // Bu perk artık sadece bilgilendirme amaçlı
    }

    private string GetDescription()
    {
        int stack = RunManager.instance != null ? RunManager.instance.shopRerollStack : 0;
        return $"Her shop reroll'da tüm zarlarına kalıcı +1 bonus. Stack: {stack}";
    }
}
