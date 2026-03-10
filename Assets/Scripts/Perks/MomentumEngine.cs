using UnityEngine;

public class MomentumEnginePerk : BasePerk
{
    public override void OnAcquire()
    {
        priority = 5; // Hasar çarpanlarından önce, normal zar manipülasyonlarıyla aynı anda eklensin
    }

    public override void ModifyCombat(CombatPayload payload)
    {
        // Eğer TurnManager yoksa veya hiç yürümediysek boşuna yorma
        if (TurnManager.instance == null || TurnManager.instance.hexesMovedThisTurn <= 0) return;

        int stepsTaken = TurnManager.instance.hexesMovedThisTurn;

        // Bütün zarlara atılan adım kadar değer ekle
        for (int i = 0; i < payload.diceRolls.Count; i++)
        {
            payload.diceRolls[i] += stepsTaken;
        }

        // Çalıştığını belli etmek için hoplat
        TriggerVisualPop();
    }
}