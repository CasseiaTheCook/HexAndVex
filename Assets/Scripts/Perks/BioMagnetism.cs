using UnityEngine;

public class BioMagnetismPerk : BasePerk
{
    public override void ModifyCombat(CombatPayload payload)
    {
        // Alan hasarı gibi çalışsın, her hedefe +2 vursun
        payload.flatBonus += 2;
        TriggerVisualPop();
    }
}