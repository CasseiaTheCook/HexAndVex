using UnityEngine;

public class PhantomLimbPerk : BasePerk
{
    public override void OnAcquire()
    {
        // Pasif olarak kalıcı kaçınma şansı verir
        if (RunManager.instance != null)
        {
            RunManager.instance.dodgeChance += 0.20f; // %20 ekstra dodge
        }
        TriggerVisualPop();
    }
}