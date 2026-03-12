using UnityEngine;

public class PhantomLimbPerk : BasePerk
{
    // Mine offset hard-coded
    private Vector3 mineOffset = new Vector3(-0.07f, 0f, 0f); // X: 0.5, Y: 0, Z: 0
    
    void OnEnable()
    {
        maxLevel = 3;
    }

    public override void OnAcquire()
    {
        TriggerVisualPop();
    }

    public Vector3 GetMineOffset()
    {
        return mineOffset;
    }
}