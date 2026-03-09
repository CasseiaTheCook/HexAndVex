using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;

public class ShopSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Slot UI Elemanlari")]
    public Button buyButton;
    public TMP_FontAsset customFont;
    public GameObject soldOutOverlay;

    // Tooltip verileri (Shopmanager tarafından set edilir)
    [HideInInspector] public string tooltipName;
    [HideInInspector] public string tooltipDesc;
    [HideInInspector] public int tooltipPrice;

    private GameObject tooltipObj;
    private TMP_Text titleText;
    private TMP_Text descText;
    private TMP_Text priceText;
    private CanvasGroup tooltipCanvasGroup;

    private Coroutine fadeCoroutine;

    void Awake()
    {
        // Gerekirse Awake'te initialization yapilabilir
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
        // Eğer zaten varsa sadece aktif et
        if (tooltipObj != null)
        {
            tooltipObj.SetActive(true);
            StartFade(1f); // Fade-in baslat
            return;
        }

        // ==========================================
        // YENİ: TOOLTIP ANA OBJE (DIKDORTGEN PENCERE)
        // ==========================================
        tooltipObj = new GameObject("TooltipWindow", typeof(RectTransform));
        tooltipObj.transform.SetParent(transform, false);
        tooltipObj.layer = gameObject.layer;

        // FADE İÇİN CANVAS GROUP
        tooltipCanvasGroup = tooltipObj.AddComponent<CanvasGroup>();
        tooltipCanvasGroup.alpha = 0f; // Sifirdan basla

        RectTransform ttRT = tooltipObj.GetComponent<RectTransform>();

        // YENİ POZİSYON: Slotun SAĞINA yapışık (Right Center anchored)
        ttRT.anchorMin = new Vector2(1f, 0.5f);
        ttRT.anchorMax = new Vector2(1f, 0.5f);

        // PİVOT: Kendi SOL ORTASINA (Left Center pivot)
        ttRT.pivot = new Vector2(0f, 0.5f);

        // YENİ POZİSYON: Sağa doğru 10 piksel boşluk bırak
        ttRT.anchoredPosition = new Vector2(10f, 0f);

        // YENİ DEVAZA BOYUTLAR: Genişlik 350 piksel (Dikdörtgen olması için)
        ttRT.sizeDelta = new Vector2(350f, 0f); // Yükseklik otomatik

        // ARKAPLAN (Windows Menü Koyu Gri)
        Image bg = tooltipObj.AddComponent<Image>();
        bg.color = new Color(0.1f, 0.1f, 0.1f, 0.98f);
        bg.raycastTarget = false;

        // ContentSizeFitter ile yüksekliği otomatik ayarla
        var fitter = tooltipObj.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // DÜZEN (Vertical Layout Group)
        var vlg = tooltipObj.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(16, 16, 12, 12); // Büyük boşluklar
        vlg.spacing = 6; // Yazılar arası boşluk
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        // ==========================================
        // İÇERİK: BAŞLIK (Devasa)
        // ==========================================
        GameObject titleGO = new GameObject("TooltipTitle", typeof(RectTransform));
        titleGO.transform.SetParent(tooltipObj.transform, false);
        titleGO.layer = gameObject.layer;
        titleText = titleGO.AddComponent<TextMeshProUGUI>();
        if (customFont != null) titleText.font = customFont;
        titleText.fontSize = 18; // BAŞLIK 18 PUNTO!
        titleText.alignment = TextAlignmentOptions.Left; // Windows menüsü gibi Sola Dayalı
        titleText.color = Color.white;
        titleText.fontStyle = FontStyles.Bold;
        titleText.raycastTarget = false;

        // ==========================================
        // İÇERİK: AYIRICI ÇİZGİ (Gray Line)
        // ==========================================
        GameObject lineGO = new GameObject("SeparatorLine", typeof(RectTransform));
        lineGO.transform.SetParent(tooltipObj.transform, false);
        lineGO.layer = gameObject.layer;

        Image lineImg = lineGO.AddComponent<Image>();
        lineImg.color = new Color(0.4f, 0.4f, 0.4f, 0.6f);
        lineImg.raycastTarget = false;

        LayoutElement lineLE = lineGO.AddComponent<LayoutElement>();
        lineLE.minHeight = 2f;
        lineLE.preferredHeight = 2f; // 2 piksel kalınlık

        // ==========================================
        // İÇERİK: AÇIKLAMA (Büyük ve Sola Dayalı)
        // ==========================================
        GameObject descGO = new GameObject("TooltipDescription", typeof(RectTransform));
        descGO.transform.SetParent(tooltipObj.transform, false);
        descGO.layer = gameObject.layer;
        descText = descGO.AddComponent<TextMeshProUGUI>();
        if (customFont != null) descText.font = customFont;
        descText.fontSize = 14; // AÇIKLAMA 14 PUNTO!
        descText.alignment = TextAlignmentOptions.Left; // Windows menüsü gibi Sola Dayalı
        descText.color = new Color(0.9f, 0.9f, 0.9f, 1f); // Hafif kırık beyaz
        descText.raycastTarget = false;
        descText.enableWordWrapping = true;

        // ==========================================
        // İÇERİK: FİYAT (Altına, Renkli ve Büyük)
        // ==========================================
        GameObject priceGO = new GameObject("TooltipPrice", typeof(RectTransform));
        priceGO.transform.SetParent(tooltipObj.transform, false);
        priceGO.layer = gameObject.layer;
        priceText = priceGO.AddComponent<TextMeshProUGUI>();
        if (customFont != null) priceText.font = customFont;
        priceText.fontSize = 16; // FİYAT 16 PUNTO!
        priceText.alignment = TextAlignmentOptions.Right; // Sağ alt köşede dursun
        priceText.color = new Color(1f, 0.85f, 0.2f, 1f); // Parlak Coin Sarısı
        priceText.fontStyle = FontStyles.Bold;
        priceText.raycastTarget = false;

        // Verileri doldur
        titleText.text = tooltipName.ToUpper(); // Başlık hep BÜYÜK HARF
        descText.text = tooltipDesc;
        priceText.text = tooltipPrice + " COIN";

        // Tooltip'i en üste çıkar (diğer slotların üstünde görünsün)
        Canvas tooltipCanvas = tooltipObj.AddComponent<Canvas>();
        tooltipCanvas.overrideSorting = true;
        tooltipCanvas.sortingOrder = 100; // Çok yukarıda
        tooltipObj.AddComponent<GraphicRaycaster>();

        StartFade(1f); // Fade-in baslat
    }

    private void HideTooltip()
    {
        StartFade(0f); // Fade-out baslat
    }

    // ==========================================
    // YENİ: ŞİMŞEK HIZINDA FADE ANİMASYONU
    // ==========================================
    private void StartFade(float targetAlpha)
    {
        if (tooltipCanvasGroup == null) return;

        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);

        // Oyuncu fareyi çok hızlı hareket ettireceği için 
        // Fade hızı ÇOK HIZLI olmalı (0.1sn)
        fadeCoroutine = StartCoroutine(FadeRoutine(targetAlpha, 0.1f));
    }

    private IEnumerator FadeRoutine(float targetAlpha, float duration)
    {
        float startAlpha = tooltipCanvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime; // Oyun durduysa bile çalışsın

            // Linear Fade
            tooltipCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
            yield return null;
        }

        tooltipCanvasGroup.alpha = targetAlpha;

        // Tamamen kapandıysa objeyi de kapat ki performans yemesin
        if (targetAlpha <= 0f && tooltipObj != null)
        {
            tooltipObj.SetActive(false);
        }
    }

    void OnDisable()
    {
        HideTooltip();
    }
}