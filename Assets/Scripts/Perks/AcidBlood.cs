using UnityEngine;

public class AcidBloodPerk : BasePerk
{
    void OnEnable() { maxLevel = 3; }
    // Düşmanı dikene itmek oyuncuyu iyileştirir. Seviye başına 1 ek can.
    // Mantık TurnManager.MultiAttack içinde çalışıyor.
}
