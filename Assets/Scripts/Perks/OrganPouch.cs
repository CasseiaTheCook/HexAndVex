using UnityEngine;

public class OrganPouchPerk : BasePerk
{
    // İlk alındığında çalışır (1. Seviye)
    public override void OnAcquire()
    {
        if (Shopmanager.instance != null)
        {
            Shopmanager.instance.shopSlotCount += 1;
            Shopmanager.instance.GenerateShopItems();
        }
        TriggerVisualPop();
    }

    // YENİ: Kart tekrar seçilirse çalışır (2. ve 3. Seviyeler)
    public override void Upgrade()
    {
        base.Upgrade(); // Seviyeyi 1 artır
        
        if (Shopmanager.instance != null)
        {
            Shopmanager.instance.shopSlotCount += 1; // Dükkana 1 slot daha ekle
            Shopmanager.instance.GenerateShopItems(); // Yeni slot boş kalmasın diye dükkanı hemen yenile
        }
        TriggerVisualPop();
    }
}