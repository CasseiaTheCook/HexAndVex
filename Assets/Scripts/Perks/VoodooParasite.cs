using UnityEngine;
using System.Collections.Generic;

public class VoodooParasitePerk : BasePerk
{
    public override void ModifyCombat(CombatPayload payload)
    {
        TriggerVisualPop();
        // Asıl olay hasar vurulduktan sonra TurnManager içinde tetiklenmeli.
        // Ama şimdilik payload üzerinden sadece bir "patlama" trigger'layabiliyoruz.
        payload.triggerExplosion = true; // Blast Impact gibi çalışsın şimdilik.
    }
}