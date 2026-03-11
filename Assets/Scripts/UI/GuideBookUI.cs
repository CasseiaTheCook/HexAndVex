using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// GuideBook UI kontrolcüsü — kitap panelini açar/kapar, sayfa içeriğini gösterir.
/// Tamamen kod ile UI oluşturur — prefab gerekmez.
/// Editor tool (GuideBookSetupTool) bu scripti sahneye ekler ve referansları bağlar.
/// </summary>
public class GuideBookUI : MonoBehaviour
{
    [Header("Panel Referansları")]
    public RectTransform bookPanel;
    public CanvasGroup bookCanvasGroup;

    [Header("İçerik")]
    public TMP_Text titleText;
    public TMP_Text bodyText;
    public TMP_Text pageNumberText;
    public TMP_Text categoryLabel;
    public Image illustrationImage;

    [Header("Butonlar")]
    public Button prevButton;
    public Button nextButton;
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

    // İlk açılış pulse animasyonu
    private bool hasOpenedOnce = false;
    private Coroutine pulseCoroutine;
    private Coroutine animCoroutine;

    // Kategori isimleri
    private static readonly string[] CATEGORIES = { "", "Combat", "Enemies", "Items", "Perks", "Movement" };
    private static readonly string[] CATEGORY_LABELS = { "ALL", "COMBAT", "ENEMIES", "ITEMS", "PERKS", "MOVEMENT" };

    // Kategori renkleri
    private static readonly Color CAT_ACTIVE = new Color(0f, 0.58f, 0.74f, 1f);   // #0093BC
    private static readonly Color CAT_INACTIVE = new Color(0.15f, 0.15f, 0.2f, 1f);
    private static readonly Color CAT_TEXT_ACTIVE = Color.white;
    private static readonly Color CAT_TEXT_INACTIVE = new Color(0.6f, 0.6f, 0.6f, 1f);

    private bool initialized = false;

    void Awake()
    {
        // Buton listener'ları Awake'te bağla — Start panel kapalıyken çalışmayabilir
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

        // Manager'a kendini bağla — gerekirse bir frame bekle
        BindToManager();

        // Buton listener'ları
        if (prevButton != null) prevButton.onClick.AddListener(OnPrevClicked);
        if (nextButton != null) nextButton.onClick.AddListener(OnNextClicked);
        if (closeButton != null) closeButton.onClick.AddListener(OnCloseClicked);
        if (bookIconButton != null) bookIconButton.onClick.AddListener(OnBookIconClicked);

        // Kategori butonları
        for (int i = 0; i < categoryButtons.Count && i < CATEGORIES.Length; i++)
        {
            int idx = i; // closure capture
            categoryButtons[i].onClick.AddListener(() => OnCategoryClicked(idx));
        }

        // Başlangıçta kitap kapalı — sadece paneli kapat, canvas aktif kalmalı
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
        {
            GuideBookManager.instance.bookUI = this;
        }
        else
        {
            // Aynı GameObject'teyse Awake sırası garanti değil — 1 frame bekle
            StartCoroutine(BindNextFrame());
        }
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
        RefreshCategoryButtons();

        if (animCoroutine != null) StopCoroutine(animCoroutine);
        animCoroutine = StartCoroutine(AnimateOpen());
    }

    public void Hide()
    {
        if (animCoroutine != null) StopCoroutine(animCoroutine);
        animCoroutine = StartCoroutine(AnimateClose());
    }

    // ── Sayfa Yenileme ──────────────────────────────────────

    public void RefreshPage()
    {
        var mgr = GuideBookManager.instance;
        if (mgr == null) return;

        GuideBookPage page = mgr.GetCurrentPage();
        if (page == null)
        {
            if (titleText != null) titleText.text = "Empty";
            if (bodyText != null) bodyText.text = "No pages found.";
            if (pageNumberText != null) pageNumberText.text = "0 / 0";
            if (categoryLabel != null) categoryLabel.text = "";
            if (illustrationImage != null) illustrationImage.gameObject.SetActive(false);
            return;
        }

        if (titleText != null) titleText.text = page.title;
        if (bodyText != null) bodyText.text = page.bodyText;
        if (pageNumberText != null)
            pageNumberText.text = $"{mgr.currentPageIndex + 1} / {mgr.FilteredPageCount}";
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

        // Prev/Next buton durumu
        if (prevButton != null) prevButton.interactable = mgr.FilteredPageCount > 1;
        if (nextButton != null) nextButton.interactable = mgr.FilteredPageCount > 1;
    }

    private void RefreshCategoryButtons()
    {
        var mgr = GuideBookManager.instance;
        if (mgr == null) return;

        for (int i = 0; i < categoryButtons.Count && i < CATEGORIES.Length; i++)
        {
            bool isActive = mgr.activeCategory == CATEGORIES[i];

            // Buton arkaplan rengi
            Image btnImg = categoryButtons[i].GetComponent<Image>();
            if (btnImg != null)
                btnImg.color = isActive ? CAT_ACTIVE : CAT_INACTIVE;

            // Yazı rengi
            if (i < categoryButtonTexts.Count && categoryButtonTexts[i] != null)
                categoryButtonTexts[i].color = isActive ? CAT_TEXT_ACTIVE : CAT_TEXT_INACTIVE;
        }
    }

    // ── Buton Callback'leri ─────────────────────────────────

    private void OnPrevClicked()
    {
        if (GuideBookManager.instance != null) GuideBookManager.instance.PrevPage();
    }

    private void OnNextClicked()
    {
        if (GuideBookManager.instance != null) GuideBookManager.instance.NextPage();
    }

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
        RefreshCategoryButtons();
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

            // Ease-out-back: hafif overshoot
            float easeT = 1f - Mathf.Pow(1f - t, 3f);
            float scaleEase = 1f + (easeT > 0.7f ? (1f - easeT) * 0.1f : easeT * 0.05f);

            float s = Mathf.LerpUnclamped(0.8f, 1f, easeT) * scaleEase;
            bookPanel.localScale = new Vector3(s, s, 1f);

            if (bookCanvasGroup != null)
                bookCanvasGroup.alpha = Mathf.Clamp01(t * 2f); // Hızlı fade in

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

            // Ease-in
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

    // İlk kez oynamaya başlayan oyuncu için kitap ikonu pulse
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
