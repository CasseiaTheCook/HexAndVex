public class DeepPocketsPerk : BasePerk
{
    public override void OnAcquire()
    {
        if (Shopmanager.instance != null)
        {
            Shopmanager.instance.shopSlotCount += 1;
            Shopmanager.instance.GenerateShopItems();
        }
        TriggerVisualPop();
    }
}