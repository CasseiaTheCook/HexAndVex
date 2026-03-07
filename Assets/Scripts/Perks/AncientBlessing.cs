using System.Collections.Generic;
using System.Linq;

public class AncientBlessingPerk : BasePerk
{
    public override void OnAcquire()
    {
        // Kendisi hariç aktif perkleri listele
        var otherPerks = RunManager.instance.activePerks
            .Where(p => p != this)
            .ToList();

        if (otherPerks.Count > 0)
        {
            // Rastgele birini seç ve varsa Upgrade metodunu çalıştır veya seviyesini artır
            int randomIndex = UnityEngine.Random.Range(0, otherPerks.Count);
            otherPerks[randomIndex].level++; 
            
            TriggerVisualPop();
        }
    }
}
