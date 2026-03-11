public class SymbioticFuryPerk : BasePerk
{
    // Oyun başladığında veya bu perk alındığında önceliğini çok yüksek yapıyoruz
    // Böylece +1, +2 gibi zar artıran perkler önce çalışır, Sword Dance en son çalışır!
    private void Awake()
    {
        priority = 99; // En sona atar
        rarity = PerkRarity.Secret;
    }

    public override void ModifyCombat(CombatPayload payload)
    {
        payload.multiplyInsteadOfAdd = true;
        
        // Buradaki TriggerVisualPop(); satırını sildik çünkü TurnManager zaten 
        // matematiksel sıçramayı (toplamadan çarpmaya geçişi) algılayıp kendisi patlatacak.
    }
}