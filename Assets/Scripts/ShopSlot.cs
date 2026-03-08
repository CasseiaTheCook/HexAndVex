using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Bu component shop slot prefab'ina takinir.
/// Her slot kendi buton, isim, fiyat ve "Satildi" overlay'ini tasir.
/// nameText yalnizca mouse hover'inda gorunur.
/// </summary>
public class ShopSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Slot UI Elemanlari")]
    public Button buyButton;
    public TMP_Text nameText;      // Hover'da gorunur, otherwise gizli
    public TMP_Text priceText;
    public GameObject soldOutOverlay;

    void Start()
    {
        SetTextsVisible(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        SetTextsVisible(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SetTextsVisible(false);
    }

    private void SetTextsVisible(bool visible)
    {
        if (nameText  != null) nameText.gameObject.SetActive(visible);
        if (priceText != null) priceText.gameObject.SetActive(visible);
    }
}
