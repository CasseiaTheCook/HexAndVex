using UnityEngine;

public class VoodooParasitePerk : BasePerk
{
    void OnEnable() { maxLevel = 4; }
    // Asıl mantık TurnManager.MultiAttack içinde çalışıyor.
    // Her seviyede 1 ek rastgele düşmana aynı hasarı vurur.
}
