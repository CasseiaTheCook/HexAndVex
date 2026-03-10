using UnityEngine;

public class BribePerk : BasePerk
{
    void OnEnable() { maxLevel = 1; }
    // Ölümcül hasar yenildiğinde bütün altın kaybedilir, oyuncu full HP ile diriltilir ve bu perk kaldırılır.
    // Mantık TurnManager.PlayerTakeDamage içinde çalışıyor.
}
