using UnityEngine;

public class RecoilSpringPerk : BasePerk
{
    public override void ModifyCombat(CombatPayload payload)
    {
        // Vurduktan sonra diğer dövüşte ekstra +1 zar ver (sıçrama hissi yaratmak için momentum kazanıyor)
        if (RunManager.instance != null)
        {
            RunManager.instance.bonusDiceNextCombat += 1;
        }
        TriggerVisualPop();
    }
}