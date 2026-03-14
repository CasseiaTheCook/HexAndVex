using UnityEngine;

public class CoinDropService
{
    public int CalculateAndAwardCoins(EnemyAI enemy)
    {
        if (enemy.enemyBehavior == EnemyAI.EnemyBehavior.Totem) return 0;

        RunManager rm = RunManager.instance;
        bool isBossRoom = rm.currentLevel % 5 == 0;
        int coinDrop = 0;

        if (isBossRoom)
        {
            if (enemy.enemyBehavior == EnemyAI.EnemyBehavior.Boss)
                coinDrop = 20;
        }
        else
        {
            coinDrop = Random.Range(1, 4) + rm.bonusGold;
            if (rm.doubleGoldNextKill)
            {
                coinDrop *= 2;
                rm.doubleGoldNextKill = false;
            }
        }

        if (coinDrop > 0)
        {
            rm.currentGold += coinDrop;
            rm.totalGoldEarned += coinDrop;
            if (CoinDropVFX.instance != null)
                CoinDropVFX.instance.SpawnCoins(enemy.transform.position, coinDrop);
        }

        return coinDrop;
    }

    public void ProcessKillRewards(EnemyAI deadEnemy)
    {
        CalculateAndAwardCoins(deadEnemy);
        foreach (var p in RunManager.instance.activePerks)
            p.OnEnemyKilled(deadEnemy);
        RunManager.instance.totalEnemiesKilled++;
    }
}
