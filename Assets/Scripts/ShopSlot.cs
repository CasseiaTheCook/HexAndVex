using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Bu component shop slot prefab'ina takinir.
/// Her slot kendi buton, isim, fiyat ve "Satildi" overlay'ini tasir.
/// </summary>
public class ShopSlot : MonoBehaviour
{
    [Header("Slot UI Elemanlari")]
    public Button buyButton;
    public TMP_Text nameText;
    public TMP_Text priceText;
    public GameObject soldOutOverlay;
}
