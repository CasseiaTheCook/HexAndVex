using UnityEngine;

[CreateAssetMenu(menuName = "Items/PhaseShift", fileName = "PhaseShift")]
public class PhaseShift : BaseItem
{
    void OnEnable()
    {
        itemName = "Phase-Shift";
        description = "Select an enemy and swap positions with it";
        price = 6;
    }

    public override bool Use()
    {
        if (TurnManager.instance == null) return false;
        TurnManager.instance.StartPhaseShiftTargeting();
        return true;
    }
}
