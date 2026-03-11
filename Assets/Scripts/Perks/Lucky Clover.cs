public class LuckyCloverPerk : BasePerk
{
    void OnEnable()
    {
        rarity = PerkRarity.Rare;
        maxLevel = 3;
    }

    public override void OnAcquire()
    {
        RunManager.instance.luckyCloverLevel = currentLevel;
        TriggerVisualPop();
    }

    public override void Upgrade()
    {
        base.Upgrade();
        RunManager.instance.luckyCloverLevel = currentLevel;
        TriggerVisualPop();
    }
}
