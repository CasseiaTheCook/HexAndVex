using UnityEngine;

public class BioMagnetismPerk : BasePerk
{
    public override void OnAcquire()
    {
        priority = 1; // Sıralaması önemli değil, savaş başlamadan özel olarak çağırıyoruz.
    }
}