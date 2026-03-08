using UnityEngine;

public class VeteransVitalityPerk : BasePerk
{
    public override void OnAcquire()
    {
        ApplyHealthBoost();
    }

    // YENİ: Kart tekrar seçildiğinde (Upgrade) HEMEN ekstra canı bassın!
    public override void Upgrade()
    {
        base.Upgrade(); // Seviyeyi artır
        ApplyHealthBoost(); // Seviye atladığı an canı ver!
    }

    public override void OnLevelStart()
    {
        ApplyHealthBoost();
    }

    private void ApplyHealthBoost()
    {
        if (RunManager.instance == null) return;

        // Seviyesi ne kadarsa o kadar can versin (Lv 1 = 1 Can, Lv 3 = 3 Can)
        int healAmount = currentLevel;

        RunManager.instance.playerCurrentHealth = Mathf.Min(
            RunManager.instance.playerCurrentHealth + healAmount,
            RunManager.instance.playerMaxHealth
        );

        HexMovement player = TurnManager.instance != null ? TurnManager.instance.player : null;
        if (player != null && player.health != null)
            player.health.Heal(healAmount);

        TriggerVisualPop();
    }
}