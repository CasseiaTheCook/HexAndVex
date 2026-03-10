using UnityEngine;

public class PhantomLimbPerk : BasePerk
{
    void OnEnable()
    {
        maxLevel = 4;
    }

    public override void OnAcquire()
    {
        TriggerVisualPop();
    }
}