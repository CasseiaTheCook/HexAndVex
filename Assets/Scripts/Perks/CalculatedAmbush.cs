public class CalculatedAmbushPerk : BasePerk
{
    private int storedExtraDices = 0;
    private bool appliedThisBattle = false;

    public override void OnSkip()
    {
        storedExtraDices++;
        TriggerVisualPop();
    }

    public override void OnLevelStart()
    {
        // Yeni odaya geçince saldırı hakkını sıfırla
        appliedThisBattle = false;
    }

    public override void ModifyCombat(CombatPayload payload)
    {
        // Sadece savaşın ilk saldırısında biriken zarları ekle
        if (!appliedThisBattle && storedExtraDices > 0)
        {
            for (int i = 0; i < storedExtraDices; i++)
            {
                payload.diceRolls.Add(UnityEngine.Random.Range(1, 7));
            }
            
            storedExtraDices = 0; // Kullanılanları sıfırla
            appliedThisBattle = true; // Bu savaşta bir kez çalıştı
            TriggerVisualPop();
        }
    }
}
