using UnityEngine;

public class OrganPouchPerk : BasePerk
{
    // İlk alındığında çalışır (1. Seviye)
    public override void OnAcquire()
    {
        if (Shopmanager.instance != null)
        {
            Shopmanager.instance.shopSlotCount += 1;
            
            // DÜZELTME: Artık dükkanı resetlemiyor, eskilere dokunmadan 1 tane ekliyor!
            Shopmanager.instance.AddSingleExtraSlot(); 
        }
        TriggerVisualPop();
    }

    // Kart tekrar seçilirse çalışır (2. ve 3. Seviyeler)
    public override void Upgrade()
    {
        base.Upgrade(); 
        
        if (Shopmanager.instance != null)
        {
            Shopmanager.instance.shopSlotCount += 1; 
            
            // DÜZELTME: Artık dükkanı resetlemiyor, eskilere dokunmadan 1 tane ekliyor!
            Shopmanager.instance.AddSingleExtraSlot(); 
        }
        TriggerVisualPop();
    }
}