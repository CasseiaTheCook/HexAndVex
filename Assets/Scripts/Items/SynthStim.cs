using UnityEngine;

[CreateAssetMenu(menuName = "Items/SynthStim", fileName = "SynthStim")]
public class SynthStim : BaseItem
{
    void OnEnable()
    {
        itemName = "Synth-Stim";
        description = "Bu savaşta +1 zar at";
        price = 6;
    }

    public override bool Use()
    {
        if (RunManager.instance == null) return false;

        RunManager.instance.bonusDiceNextCombat += 1;
        return true;
    }
}
