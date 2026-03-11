public class LetsGoAgainPerk : BasePerk
{
    private void Awake()
    {
        perkName = "Let's Go Again";
        description = "After all perks trigger, they all trigger once more.";
        rarity = PerkRarity.Secret;
        maxLevel = 1;
        priority = 0;
    }

    // Bu perk ModifyCombat ile çalışmaz.
    // TurnManager, nihai hasar hesabı bittikten sonra bu perkin varlığını kontrol eder
    // ve tüm perkleri tekrar tetikler.
    // Detaylar TurnManager içinde uygulanır.
}
