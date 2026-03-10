using UnityEngine;

public class PhantomLimbPerk : BasePerk
{
    public override void OnAcquire()
    {
        // Pasif olarak kalıcı kaçınma şansı verir
        if (RunManager.instance != null)
        {
            
        }
        TriggerVisualPop();
    }
}