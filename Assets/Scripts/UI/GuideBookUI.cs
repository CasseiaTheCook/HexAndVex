using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// GuideBook UI kontrolcüsü — sol sidebar + sağ içerik alanı.
/// Sidebar'da tıklanabilir sayfa listesi, sağda scrollable içerik.
/// Editor tool (GuideBookSetupTool) referansları bağlar.
/// </summary>
public class GuideBookUI : MonoBehaviour
{
    [Header("Panel Referansları")]
    public RectTransform bookPanel;
    public CanvasGroup bookCanvasGroup;

    [Header("Sidebar (Sol)")]
    public ScrollRect sidebarScrollRect;
    public RectTransform sidebarContent; // VerticalLayoutGroup — sayfa butonları buraya spawn edilir

    [Header("İçerik Alanı (Sağ)")]
    public TMP_Text titleText;
    public TMP_Text categoryLabel;
    public ScrollRect contentScrollRect;
    public RectTransform contentScrollContent; // VerticalLayoutGroup — bodyText + illustration
    public TMP_Text bodyText;
    public Image illustrationImage;

    [Header("Butonlar")]
    public Button closeButton;

    [Header("Kategori Sekmeleri")]
    public List<Button> categoryButtons = new List<Button>();
    public List<TMP_Text> categoryButtonTexts = new List<TMP_Text>();

    [Header("Kitap İkonu (Sol Alt)")]
    public Button bookIconButton;
    public RectTransform bookIconRect;

    [Header("Font")]
    public TMP_FontAsset customFont;

    [Header("Animasyon")]
    public float animDuration = 0.25f;

    // Dinamik sidebar butonları
    private List<GameObject> sidebarButtonObjects = new List<GameObject>();

    // İlk açılış pulse animasyonu
    private bool hasOpenedOnce = false;
    private Coroutine pulseCoroutine;
    private Coroutine animCoroutine;

    // Kategori isimleri (ALL yok — filtre butonu olarak 5 kategori)
    private static readonly string[] CATEGORIES = { "Combat", "Enemies", "Items", "Perks", "Movement" };

    // Renkler
    private static readonly Color CAT_ACTIVE = new Color(0f, 0.58f, 0.74f, 1f);     // #0093BC
    private static readonly Color CAT_ACTIVE_LIGHT = new Color(0.1f, 0.68f, 0.84f, 1f);
    private static readonly Color CAT_INACTIVE = new Color(0.15f, 0.15f, 0.2f, 1f);
    private static readonly Color SIDEBAR_ACTIVE = new Color(0f, 0.58f, 0.74f, 1f);
    private static readonly Color SIDEBAR_INACTIVE = new Color(0.06f, 0.06f, 0.1f, 1f);

    private bool initialized = false;

    void Awake()
    {
        InitializeIfNeeded();
    }

    void Start()
    {
        InitializeIfNeeded();
    }

    private void InitializeIfNeeded()
    {
        if (initialized) return;
        initialized = true;

        BindToManager();

        // Buton listener'ları
        if (closeButton != null) closeButton.onClick.AddListener(OnCloseClicked);
        if (bookIconButton != null) bookIconButton.onClick.AddListener(OnBookIconClicked);

        // Kategori butonları
        for (int i = 0; i < categoryButtons.Count && i < CATEGORIES.Length; i++)
        {
            int idx = i;
            categoryButtons[i].onClick.AddListener(() => OnCategoryClicked(idx));
        }

        // Başlangıçta kitap kapalı
        if (bookPanel != null)
        {
            bookPanel.gameObject.SetActive(false);
            if (bookCanvasGroup != null) bookCanvasGroup.alpha = 0f;
        }

        // İlk açılış pulse efekti
        if (!hasOpenedOnce && bookIconRect != null)
            pulseCoroutine = StartCoroutine(PulseIconLoop());
    }

    private void BindToManager()
    {
        if (GuideBookManager.instance != null)
            GuideBookManager.instance.bookUI = this;
        else
            StartCoroutine(BindNextFrame());
    }

    private IEnumerator BindNextFrame()
    {
        yield return null;
        if (GuideBookManager.instance != null)
            GuideBookManager.instance.bookUI = this;
    }

    // ── Aç / Kapa ──────────────────────────────────────────

    public void Show()
    {
        if (bookPanel == null) return;

        // Pulse'u durdur
        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
            pulseCoroutine = null;
            if (bookIconRect != null) bookIconRect.localScale = Vector3.one;
        }
        hasOpenedOnce = true;

        bookPanel.gameObject.SetActive(true);
        RefreshPage();
        RefreshSidebar();
        RefreshCategoryButtons();

        if (animCoroutine != null) StopCoroutine(animCoroutine);
        animCoroutine = StartCoroutine(AnimateOpen());
    }

    public void Hide()
    {
        if (animCoroutine != null) StopCoroutine(animCoroutine);
        animCoroutine = StartCoroutine(AnimateClose());
    }

    // ── Sidebar (Sol Panel — Sayfa Listesi) ─────────────────

    public void RefreshSidebar()
    {
        var mgr = GuideBookManager.instance;
        if (mgr == null || sidebarContent == null) return;

        // Eski butonları temizle
        foreach (var go in sidebarButtonObjects)
            if (go != null) Destroy(go);
        sidebarButtonObjects.Clear();

        var filteredIndices = mgr.GetFilteredIndices();

        for (int i = 0; i < filteredIndices.Count; i++)
        {
            int filteredIdx = i;
            GuideBookPage page = mgr.GetPageAt(filteredIndices[i]);
            if (page == null) continue;

            bool isActive = (i == mgr.currentPageIndex);

            // Buton GameObject
            GameObject btnGO = new GameObject($"SidebarBtn_{i}", typeof(RectTransform), typeof(Image), typeof(Button));
            btnGO.transform.SetParent(sidebarContent, false);

            RectTransform btnRT = btnGO.GetComponent<RectTransform>();
            LayoutElement le = btnGO.AddComponent<LayoutElement>();
            le.preferredHeight = 34f;
            le.flexibleWidth = 1f;

            Image btnImg = btnGO.GetComponent<Image>();
            btnImg.color = isActive ? SIDEBAR_ACTIVE : SIDEBAR_INACTIVE;

            // ColorBlock — tüm state'leri aynı yap ki Unity override etmesin
            Button btn = btnGO.GetComponent<Button>();
            ColorBlock cb = ColorBlock.defaultColorBlock;
            if (isActive)
            {
                cb.normalColor = SIDEBAR_ACTIVE;
                cb.highlightedColor = CAT_ACTIVE_LIGHT;
                cb.pressedColor = CAT_ACTIVE_LIGHT;
                cb.selectedColor = SIDEBAR_ACTIVE;
                cb.disabledColor = SIDEBAR_ACTIVE;
            }
            else
            {
                cb.normalColor = SIDEBAR_INACTIVE;
                cb.highlightedColor = new Color(0.12f, 0.12f, 0.18f, 1f);
                cb.pressedColor = CAT_ACTIVE;
                cb.selectedColor = SIDEBAR_INACTIVE;
                cb.disabledColor = SIDEBAR_INACTIVE;
            }
            cb.colorMultiplier = 1f;
            btn.colors = cb;

            btn.onClick.AddListener(() =>
            {
                if (GuideBookManager.instance != null)
                    GuideBookManager.instance.GoToFilteredIndex(filteredIdx);
            });

            // Yazı
            GameObject txtGO = new GameObject("Label", typeof(RectTransform));
            txtGO.transform.SetParent(btnGO.transform, false);
            TMP_Text tmp = txtGO.AddComponent<TextMeshProUGUI>();
            if (customFont != null) tmp.font = customFont;
            tmp.fontSize = 14;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.raycastTarget = false;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.overflowMode = TextOverflowModes.Ellipsis;

            // Başlığı kısalt
            string title = page.title;
            if (title.Length > 22) title = title.Substring(0, 19) + "...";
            tmp.text = title;
            tmp.color = isActive ? Color.white : new Color(0.6f, 0.6f, 0.6f, 1f);

            RectTransform txtRT = txtGO.GetComponent<RectTransform>();
            txtRT.anchorMin = Vector2.zero;
            txtRT.anchorMax = Vector2.one;
            txtRT.offsetMin = new Vector2(10f, 0); // Sol padding
            txtRT.offsetMax = new Vector2(-4f, 0);

            sidebarButtonObjects.Add(btnGO);
        }
    }

    // ── Sayfa İçerik Yenileme ───────────────────────────────

    public void RefreshPage()
    {
        var mgr = GuideBookManager.instance;
        if (mgr == null) return;

        GuideBookPage page = mgr.GetCurrentPage();
        if (page == null)
        {
            if (titleText != null) titleText.text = "Empty";
            if (bodyText != null) bodyText.text = "No pages found.";
            if (categoryLabel != null) categoryLabel.text = "";
            if (illustrationImage != null) illustrationImage.gameObject.SetActive(false);
            return;
        }

        if (titleText != null) titleText.text = page.title;
        if (bodyText != null) bodyText.text = page.bodyText;
        if (categoryLabel != null)
            categoryLabel.text = string.IsNullOrEmpty(page.category) ? "" : page.category.ToUpper();

        // İllüstrasyon
        if (illustrationImage != null)
        {
            if (page.illustration != null)
            {
                illustrationImage.gameObject.SetActive(true);
                illustrationImage.sprite = page.illustration;
            }
            else
            {
                illustrationImage.gameObject.SetActive(false);
            }
        }

        // Scroll'u en üste resetle
        if (contentScrollRect != null)
            contentScrollRect.normalizedPosition = new Vector2(0, 1);
    }

    // ── Kategori Butonları ──────────────────────────────────

    public void RefreshCategoryButtons()
    {
        var mgr = GuideBookManager.instance;
        if (mgr == null) return;

        for (int i = 0; i < categoryButtons.Count && i < CATEGORIES.Length; i++)
        {
            bool isActive = mgr.activeCategory == CATEGORIES[i];
            SetCategoryButtonColors(categoryButtons[i], categoryButtonTexts[i], isActive);
        }
    }

    /// <summary>
    /// ColorBlock'un TÜM state'lerini ayarla — Unity'nin hover/press override'ını engelle.
    /// </summary>
    private void SetCategoryButtonColors(Button btn, TMP_Text txt, bool isActive)
    {
        if (btn == null) return;

        ColorBlock cb = ColorBlock.defaultColorBlock;
        cb.colorMultiplier = 1f;

        if (isActive)
        {
            cb.normalColor = CAT_ACTIVE;
            cb.highlightedColor = CAT_ACTIVE_LIGHT;
            cb.pressedColor = CAT_ACTIVE_LIGHT;
            cb.selectedColor = CAT_ACTIVE;
            cb.disabledColor = CAT_ACTIVE;
        }
        else
        {
            cb.normalColor = CAT_INACTIVE;
            cb.highlightedColor = new Color(0.22f, 0.22f, 0.3f, 1f);
            cb.pressedColor = CAT_ACTIVE;
            cb.selectedColor = CAT_INACTIVE;
            cb.disabledColor = CAT_INACTIVE;
        }

        btn.colors = cb;

        if (txt != null)
            txt.color = isActive ? Color.white : new Color(0.6f, 0.6f, 0.6f, 1f);
    }

    // ── Buton Callback'leri ─────────────────────────────────

    private void OnCloseClicked()
    {
        if (GuideBookManager.instance != null) GuideBookManager.instance.Close();
    }

    private void OnBookIconClicked()
    {
        if (GuideBookManager.instance != null) GuideBookManager.instance.Toggle();
        if (AudioManager.instance != null) AudioManager.instance.PlayCard();
    }

    private void OnCategoryClicked(int index)
    {
        if (index < 0 || index >= CATEGORIES.Length) return;
        if (GuideBookManager.instance != null)
            GuideBookManager.instance.SetCategory(CATEGORIES[index]);
        if (AudioManager.instance != null) AudioManager.instance.PlayMove();
    }

    // ── Animasyonlar ────────────────────────────────────────

    private IEnumerator AnimateOpen()
    {
        float elapsed = 0f;
        bookPanel.localScale = new Vector3(0.8f, 0.8f, 1f);
        if (bookCanvasGroup != null) bookCanvasGroup.alpha = 0f;

        while (elapsed < animDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / animDuration);

            float easeT = 1f - Mathf.Pow(1f - t, 3f);
            float s = Mathf.LerpUnclamped(0.8f, 1f, easeT);
            bookPanel.localScale = new Vector3(s, s, 1f);

            if (bookCanvasGroup != null)
                bookCanvasGroup.alpha = Mathf.Clamp01(t * 2f);

            yield return null;
        }

        bookPanel.localScale = Vector3.one;
        if (bookCanvasGroup != null) bookCanvasGroup.alpha = 1f;
    }

    private IEnumerator AnimateClose()
    {
        float elapsed = 0f;

        while (elapsed < animDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / animDuration);

            float easeT = t * t;
            float s = Mathf.Lerp(1f, 0.8f, easeT);
            bookPanel.localScale = new Vector3(s, s, 1f);

            if (bookCanvasGroup != null)
                bookCanvasGroup.alpha = Mathf.Lerp(1f, 0f, easeT);

            yield return null;
        }

        bookPanel.localScale = Vector3.one;
        if (bookCanvasGroup != null) bookCanvasGroup.alpha = 0f;
        bookPanel.gameObject.SetActive(false);
    }

    private IEnumerator PulseIconLoop()
    {
        while (!hasOpenedOnce)
        {
            float t = Mathf.PingPong(Time.unscaledTime * 2f, 1f);
            float s = Mathf.Lerp(1f, 1.15f, t);
            if (bookIconRect != null)
                bookIconRect.localScale = new Vector3(s, s, 1f);
            yield return null;
        }
        if (bookIconRect != null) bookIconRect.localScale = Vector3.one;
    }
}
