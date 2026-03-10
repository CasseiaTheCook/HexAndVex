using UnityEngine;

public class BribePerk : BasePerk
{
    public override void OnAcquire()
    {
        // Bu perk alındığında RunManager'a haber ver
        if (RunManager.instance != null)
        {
            //RunManager.instance.hasBribePerk = true; // RunManager'a bu değişkeni eklemelisin!
        }
    }

    // Gerçek dirilme mantığı HealthScript'in TakeDamage kısmında çalışacak:
    // "Eğer HP <= 0 ise VE RunManager.hasBribePerk varsa -> Altını sıfırla, HP'yi fulle, hasBribePerk = false yap."
}