using UnityEngine;

public class NeuralRebootPerk : BasePerk
{
    void Awake()
    {
        maxLevel = 1;
        isRerollPerk = true;
    }

    public override void OnAcquire()
    {
        base.OnAcquire();
    }

    // 3 veya altı gelen her zarı bir kez yeniden atar
    public override void ModifyCombat(CombatPayload payload)
    {
        for (int i = 0; i < payload.diceRolls.Count; i++)
        {
            if (payload.diceRolls[i] <= 3)
            {
                int oldVal = payload.diceRolls[i];
                payload.diceRolls[i] = Random.Range(1, 7);
                Debug.Log($"NeuralReboot: Zar {i + 1} yeniden atildi: {oldVal} -> {payload.diceRolls[i]}");
            }
        }
    }
}