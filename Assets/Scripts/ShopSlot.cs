using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class ShopSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Slot UI Elemanlari")]
    public Button buyButton;
    public TMP_Text nameText;      // Artık kullanılmıyor (kutunun içinde yazı yok)
    public TMP_Text priceText;     // Artık kullanılmıyor (kutunun içinde yazı yok)
    public GameObject soldOutOverlay;

    // Tooltip verileri (Shopmanager tarafından set edilir)
    [HideInInspector] public string tooltipName;
    [HideInInspector] public string tooltipDesc;
    [HideInInspector] public int tooltipPrice;

    private GameObject tooltipObj;
    private TMP_Text tooltipText;

    void Start()
    {
        // Eski kutucuk içi yazıları gizle
        if (nameText != null) nameText.gameObject.SetActive(false);
        if (priceText != null) priceText.gameObject.SetActive(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        ShowTooltip();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        HideTooltip();
    }

    private void ShowTooltip()
    {
        if (tooltipObj != null) { tooltipObj.SetActive(true); return; }

        // Tooltip objesini oluştur — slot'un altında
        tooltipObj = new GameObject("Tooltip", typeof(RectTransform));
        tooltipObj.transform.SetParent(transform, false);
        tooltipObj.layer = gameObject.layer;

        RectTransform ttRT = tooltipObj.GetComponent<RectTransform>();
        // Kutunun alt kenarına bağla, aşağı doğru sark
        ttRT.anchorMin = new Vector2(0.5f, 0f);
        ttRT.anchorMax = new Vector2(0.5f, 0f);
        ttRT.pivot = new Vector2(0.5f, 1f);
        ttRT.anchoredPosition = new Vector2(0f, -4f);
        ttRT.sizeDelta = new Vector2(120f, 0f); // Yükseklik otomatik

        // Arkaplan
        Image bg = tooltipObj.AddComponent<Image>();
        bg.color = new Color(0.12f, 0.12f, 0.12f, 0.92f);
        bg.raycastTarget = false;

        // ContentSizeFitter ile yüksekliği otomatik ayarla
        var fitter = tooltipObj.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Padding için VerticalLayoutGroup
        var vlg = tooltipObj.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(6, 6, 4, 4);
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        // Text
        GameObject textGO = new GameObject("TooltipText", typeof(RectTransform));
        textGO.transform.SetParent(tooltipObj.transform, false);
        textGO.layer = gameObject.layer;
        tooltipText = textGO.AddComponent<TextMeshProUGUI>();
        tooltipText.fontSize = 7;
        tooltipText.alignment = TextAlignmentOptions.Center;
        tooltipText.color = Color.white;
        tooltipText.raycastTarget = false;
        tooltipText.overflowMode = TextOverflowModes.Overflow;
        tooltipText.enableWordWrapping = true;

        // İçerik
        tooltipText.text = $"<b>{tooltipName}</b>\n" +
                           $"<size=6>{tooltipDesc}</size>\n" +
                           $"<color=#FFD933>{tooltipPrice} Coin</color>";

        // Tooltip'i en üste çıkar (diğer slotların üstünde görünsün)
        Canvas tooltipCanvas = tooltipObj.AddComponent<Canvas>();
        tooltipCanvas.overrideSorting = true;
        tooltipCanvas.sortingOrder = 100;
        tooltipObj.AddComponent<GraphicRaycaster>();
    }

    private void HideTooltip()
    {
        if (tooltipObj != null) tooltipObj.SetActive(false);
    }

    void OnDisable()
    {
        HideTooltip();
    }
}
