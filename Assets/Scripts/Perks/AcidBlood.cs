using UnityEngine;

public class AcidBloodPerk : BasePerk
{
    public override void OnEnemyKilled(EnemyAI enemy)
    {
        if (enemy != null && LevelGenerator.instance != null)
        {
            Vector3Int deathCell = enemy.GetCurrentCellPosition();
            if (LevelGenerator.instance.hazardCells.Contains(deathCell))
            {
                // Dikende öldüyse oyuncuyu iyileştir
                if (TurnManager.instance != null && TurnManager.instance.player != null)
                {
                    TurnManager.instance.player.health.Heal(1);
                    TriggerVisualPop();
                }
            }
        }
    }
}