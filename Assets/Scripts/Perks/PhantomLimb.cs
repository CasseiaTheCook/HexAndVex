using UnityEngine;

public class PhantomLimbPerk : BasePerk
{
    void OnEnable()
    {
        maxLevel = 3;
    }

    public override void OnAcquire()
    {
        TriggerVisualPop();
    }
}