public class CalculatedAmbushPerk : BasePerk
{
    private int storedExtraDices = 0;

    public override void OnSkip()
    {
        storedExtraDices++;
        TriggerVisualPop(); // Ekranda biriktiğini gösterir
    }

    public override void ModifyCombat(CombatPayload payload)
    {
        if (storedExtraDices > 0)
        {
            for (int i = 0; i < storedExtraDices; i++)
            {
                payload.diceRolls.Add(UnityEngine.Random.Range(1, 7)); // Ekstra zarları ekle
            }
            storedExtraDices = 0; // Birikenleri sıfırla
            TriggerVisualPop();
        }
    }
}