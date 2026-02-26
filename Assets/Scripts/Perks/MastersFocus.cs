using UnityEngine;

public class MastersFocusPerk : BasePerk
{
    // ModifyCombat yerine direkt RunManager üzerinden bir kontrol saðlayacaðýz
    // Ama sistemi bozmamak için burayý boþ býrakabiliriz veya bir log ekleyebiliriz.
    public override void OnAcquire()
    {
        base.OnAcquire();
        // Bu perk alýndýðýnda RunManager'da bir bool'u aktif edebiliriz 
        // veya TurnManager direkt activePerks listesinde bu scripti arayabilir.
    }
}