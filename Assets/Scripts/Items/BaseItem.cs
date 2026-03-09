using UnityEngine;

public abstract class BaseItem : ScriptableObject
{
    public string itemName;
    [TextArea] public string description;
    public int price;
    public Sprite icon;

    /// <summary>
    /// Item kullanıldığında çağrılır. true dönerse item tüketilmiş demektir.
    /// </summary>
    public abstract bool Use();
}
