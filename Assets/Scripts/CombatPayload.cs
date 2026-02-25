using UnityEngine;
using System.Collections.Generic;
using System.Linq;

// MonoBehaviour'dan miras ALMIYOR!
public class CombatPayload
{
    public List<int> diceRolls = new List<int>();
    
    public int flatBonus = 0;
    public float multiplier = 1.0f;
    
    // Legendary perk'ler için özel kurallar
    public bool isCriticalHit = false;
    public bool triggerExplosion = false; // Blast Impact
    public bool multiplyInsteadOfAdd = false; // Sword Dance

    public CombatPayload(List<int> rolls)
    {
        // Zarların kopyasını alıyoruz
        diceRolls = new List<int>(rolls);
    }

    public int GetFinalDamage()
    {
        float baseDamage = 0f;

        if (multiplyInsteadOfAdd && diceRolls.Count > 0)
        {
            // SWORD DANCE: Zarları birbiriyle çarp
            baseDamage = diceRolls[0];
            for (int i = 1; i < diceRolls.Count; i++)
            {
                baseDamage *= diceRolls[i];
            }
        }
        else
        {
            // NORMAL: Zarları topla
            baseDamage = diceRolls.Sum();
        }

        // Düz hasar bonuslarını ekle ve genel çarpanla çarp
        float total = (baseDamage + flatBonus) * multiplier;

        // Kritik vuruş varsa RunManager'daki çarpanla çarp
        if (isCriticalHit)
        {
            total *= RunManager.instance.criticalDamageMultiplier;
        }

        // Hasarın eksiye düşmemesini ve tam sayı olmasını sağla
        return Mathf.Max(0, Mathf.FloorToInt(total));
    }
}