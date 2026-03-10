using UnityEngine;

public class RecoilSpringPerk : BasePerk
{
    void OnEnable() { maxLevel = 1; }
    // Saldırıdan sonra oyuncu geri sıçrar. Yeni komşu düşman varsa yeniden saldırır.
    // Mantık TurnManager.MultiAttack içinde çalışıyor.
}
