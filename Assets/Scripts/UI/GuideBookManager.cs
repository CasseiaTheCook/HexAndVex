using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// GuideBook veri ve sayfa yönetimi — singleton.
/// Oyun içinde herhangi bir yerden GuideBookManager.instance.Open() ile açılabilir.
/// Oyunu DURAKLATMAZ — okurken tur devam eder.
/// </summary>
public class GuideBookManager : MonoBehaviour
{
    public static GuideBookManager instance;

    [Header("Veri")]
    public GuideBookData bookData;

    [HideInInspector] public int currentPageIndex = 0;
    [HideInInspector] public bool isOpen = false;

    // Aktif kategori filtresi ("" = hepsini göster, filtre yok)
    [HideInInspector] public string activeCategory = "";

    // Filtrelenmiş sayfa indeksleri (bookData.pages içindeki gerçek indeksler)
    private List<int> filteredIndices = new List<int>();

    // UI referansı — GuideBookUI kendini buraya bağlar
    [HideInInspector] public GuideBookUI bookUI;

    void Awake()
    {
        if (instance == null) instance = this;
        else { Destroy(gameObject); return; }
    }

    void Start()
    {
        // bookUI henüz bağlanmadıysa bul (aynı GameObject'te olabilir)
        if (bookUI == null)
            bookUI = GetComponent<GuideBookUI>();
        if (bookUI == null)
            bookUI = FindFirstObjectByType<GuideBookUI>();

        RebuildFilter();
    }

    // ── Filtre ──────────────────────────────────────────────

    public void SetCategory(string category)
    {
        // Aynı kategoriye tekrar tıklanırsa filtreyi kaldır (toggle)
        if (activeCategory == category && !string.IsNullOrEmpty(category))
            activeCategory = "";
        else
            activeCategory = category;

        RebuildFilter();
        currentPageIndex = 0;
        RefreshAll();
    }

    public void RebuildFilter()
    {
        filteredIndices.Clear();
        if (bookData == null) return;

        for (int i = 0; i < bookData.pages.Count; i++)
        {
            if (string.IsNullOrEmpty(activeCategory) || bookData.pages[i].category == activeCategory)
                filteredIndices.Add(i);
        }
    }

    // ── Sayfa bilgisi ────────────────────────────────────────

    public int FilteredPageCount => filteredIndices.Count;

    public List<int> GetFilteredIndices() => filteredIndices;

    public GuideBookPage GetCurrentPage()
    {
        if (filteredIndices.Count == 0 || bookData == null) return null;
        int clampedIndex = Mathf.Clamp(currentPageIndex, 0, filteredIndices.Count - 1);
        return bookData.pages[filteredIndices[clampedIndex]];
    }

    public GuideBookPage GetPageAt(int dataIndex)
    {
        if (bookData == null || dataIndex < 0 || dataIndex >= bookData.pages.Count) return null;
        return bookData.pages[dataIndex];
    }

    // ── Navigasyon ───────────────────────────────────────────

    /// <summary>
    /// Sidebar'dan tıklanan filtrelenmiş indekse git.
    /// filteredIdx = filteredIndices listesindeki sıra numarası.
    /// </summary>
    public void GoToFilteredIndex(int filteredIdx)
    {
        if (filteredIndices.Count == 0) return;
        currentPageIndex = Mathf.Clamp(filteredIdx, 0, filteredIndices.Count - 1);
        RefreshAll();
        if (AudioManager.instance != null) AudioManager.instance.PlayCard();
    }

    public void GoToPage(int index)
    {
        if (filteredIndices.Count == 0) return;
        currentPageIndex = Mathf.Clamp(index, 0, filteredIndices.Count - 1);
        RefreshAll();
    }

    /// <summary>
    /// Hem içerik hem sidebar'ı güncelle.
    /// </summary>
    public void RefreshAll()
    {
        if (bookUI == null) bookUI = GetComponent<GuideBookUI>();
        if (bookUI == null) bookUI = FindFirstObjectByType<GuideBookUI>();
        if (bookUI == null) return;

        bookUI.RefreshPage();
        bookUI.RefreshSidebar();
        bookUI.RefreshCategoryButtons();
    }

    // ── Aç / Kapa ───────────────────────────────────────────

    public void Open()
    {
        if (isOpen) return;
        isOpen = true;
        currentPageIndex = 0;
        activeCategory = ""; // Açılışta tüm sayfaları göster

        if (bookUI == null) bookUI = GetComponent<GuideBookUI>();
        if (bookUI == null) bookUI = FindFirstObjectByType<GuideBookUI>();

        RebuildFilter();
        if (bookUI != null) bookUI.Show();
        else Debug.LogWarning("GuideBookManager: bookUI bulunamadı!");
    }

    public void Close()
    {
        if (!isOpen) return;
        isOpen = false;
        if (bookUI != null) bookUI.Hide();
    }

    public void Toggle()
    {
        if (isOpen) Close();
        else Open();
    }
}
