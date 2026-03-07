using UnityEngine;

public class VeteransVitalityPerk : BasePerk
{
    // İlk alındığında da hemen 1 can ver
    public override void OnAcquire()
    {
        ApplyHealthBoost();
    }

    // Her yeni level başlınca da 1 can ver
    public override void OnLevelStart()
    {
        ApplyHealthBoost();
    }

    private void ApplyHealthBoost()
    {
        if (RunManager.instance == null) return;

        // Sadece mevcut canı 1 artır (max canı değiştirme)
        RunManager.instance.playerCurrentHealth = Mathf.Min(
            RunManager.instance.playerCurrentHealth + 1,
            RunManager.instance.playerMaxHealth
        );

        // Sahnedeki gerçek HealthScript'i de iyileştir
        HexMovement player = TurnManager.instance != null ? TurnManager.instance.player : null;
        if (player != null && player.health != null)
            player.health.Heal(1);

        TriggerVisualPop();
    }
}