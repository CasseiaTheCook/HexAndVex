using UnityEngine;

public class PhantomLimbPerk : BasePerk
{
    [Header("Mine Ayarları")]
    public Vector3 mineOffset = Vector3.zero;
    
    void OnEnable()
    {
        maxLevel = 3;
    }

    public override void OnAcquire()
    {
        TriggerVisualPop();
    }
}