using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

public static class StatsPanelSetupTool
{
    const string FONT_PATH = "Assets/Star Crush SDF.asset";

    [MenuItem("Tools/Setup Death Screen")]
    public static void SetupDeathScreen()
    {
        var pauseManager = Object.FindFirstObjectByType<PauseManager>();
        if (pauseManager == null) { Debug.LogError("PauseManager bulunamadı!"); return; }
        if (pauseManager.deathMenuUI == null) { Debug.LogError("PauseManager.deathMenuUI atanmamış!"); return; }

        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_PATH);
        if (font == null) Debug.LogWarning($"Star Crush SDF bulunamadı: {FONT_PATH}");

        GameObject deathPanel = pauseManager.deathMenuUI;

        // Mevcut içeriği temizle
        var existingStats = deathPanel.transform.Find("StatsPanel");
        if (existingStats != null) Object.DestroyImmediate(existingStats.gameObject);
        var existingBtn = deathPanel.transform.Find("MainMenuButton");
        if (existingBtn != null) Object.DestroyImmediate(existingBtn.gameObject);
        var existingTitle = deathPanel.transform.Find("DeathTitle");
        if (existingTitle != null) Object.DestroyImmediate(existingTitle.gameObject);

        // Arka plan rengi
        var bg = deathPanel.GetComponent<Image>();
        if (bg != null) bg.color = new Color(0f, 0f, 0f, 0.92f);

        // ── "YOU DIED" başlığı ────────────────────────────────────────────
        var deathTitle = CreateTMP(deathPanel.transform, "DeathTitle", "YOU DIED", font, 52, Color.white);
        var dtRt = deathTitle.GetComponent<RectTransform>();
        dtRt.anchorMin = new Vector2(0.5f, 1f);
        dtRt.anchorMax = new Vector2(0.5f, 1f);
        dtRt.pivot = new Vector2(0.5f, 1f);
        dtRt.anchoredPosition = new Vector2(0, -36);
        dtRt.sizeDelta = new Vector2(500, 64);

        // ── Stats paneli ──────────────────────────────────────────────────
        var statsPanel = CreatePanel(deathPanel.transform, "StatsPanel",
            new Vector2(480, 420), new Vector2(0, 30));

        var panelBg = statsPanel.GetComponent<Image>();
        panelBg.color = new Color(0f, 0f, 0f, 1f);
        AddOutline(statsPanel, new Color(0f, 0.020f, 0.047f, 1f));

        // Stat satırları kapsayıcısı
        var rowsContainer = CreateEmptyRect(statsPanel.transform, "StatRows",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0, -20), new Vector2(420, 180));
        var vl = rowsContainer.AddComponent<VerticalLayoutGroup>();
        vl.spacing = 6f;
        vl.childAlignment = TextAnchor.UpperLeft;
        vl.childForceExpandWidth = true;
        vl.childForceExpandHeight = false;
        vl.padding = new RectOffset(8, 8, 4, 4);

        string[] labels = { "Turns Played", "Dice Rolled", "Damage Dealt", "Enemies Killed", "Gold Earned" };
        var valueTexts = new TMP_Text[labels.Length];
        for (int i = 0; i < labels.Length; i++)
        {
            var row = CreateEmptyRect(rowsContainer.transform, $"Row_{labels[i]}");
            var hl = row.AddComponent<HorizontalLayoutGroup>();
            hl.childAlignment = TextAnchor.MiddleLeft;
            hl.childForceExpandWidth = true;
            hl.childForceExpandHeight = false;
            row.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 28);

            var lbl = CreateTMP(row.transform, "Label", labels[i] + ":", font, 16, Color.white);
            lbl.alignment = TextAlignmentOptions.Left;
            var le = lbl.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = 220; le.flexibleWidth = 0;
            lbl.GetComponent<RectTransform>().sizeDelta = new Vector2(220, 28);

            var val = CreateTMP(row.transform, "Value", "0", font, 16, Color.white);
            val.fontStyle = FontStyles.Bold;
            val.alignment = TextAlignmentOptions.Right;
            var ve = val.gameObject.AddComponent<LayoutElement>();
            ve.preferredWidth = 160; ve.flexibleWidth = 1;
            val.GetComponent<RectTransform>().sizeDelta = new Vector2(160, 28);

            valueTexts[i] = val;
        }

        // Ayraç
        var divider = new GameObject("Divider", typeof(RectTransform), typeof(Image));
        divider.transform.SetParent(statsPanel.transform, false);
        divider.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.15f);
        var divRt = divider.GetComponent<RectTransform>();
        divRt.anchorMin = new Vector2(0.5f, 1f); divRt.anchorMax = new Vector2(0.5f, 1f);
        divRt.pivot = new Vector2(0.5f, 1f);
        divRt.anchoredPosition = new Vector2(0, -216); divRt.sizeDelta = new Vector2(400, 2);

        // Perks başlığı
        var perksTitle = CreateTMP(statsPanel.transform, "PerksTitle", "— PERKS —", font, 18, Color.white);
        var ptRt = perksTitle.GetComponent<RectTransform>();
        ptRt.anchorMin = new Vector2(0.5f, 1f); ptRt.anchorMax = new Vector2(0.5f, 1f);
        ptRt.pivot = new Vector2(0.5f, 1f);
        ptRt.anchoredPosition = new Vector2(0, -232); ptRt.sizeDelta = new Vector2(400, 26);

        // Perks container
        var perksContainer = CreateEmptyRect(statsPanel.transform, "PerksContainer",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0, -264), new Vector2(420, 140));
        var pvl = perksContainer.AddComponent<VerticalLayoutGroup>();
        pvl.spacing = 4f;
        pvl.childAlignment = TextAnchor.MiddleCenter;
        pvl.childForceExpandWidth = true;
        pvl.childForceExpandHeight = false;
        pvl.padding = new RectOffset(8, 8, 2, 2);
        perksContainer.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // StatsPanelUI component
        var panelUI = statsPanel.AddComponent<StatsPanelUI>();
        panelUI.turnsValue  = valueTexts[0];
        panelUI.diceValue   = valueTexts[1];
        panelUI.damageValue = valueTexts[2];
        panelUI.killsValue  = valueTexts[3];
        panelUI.goldValue   = valueTexts[4];
        panelUI.perksContainer = perksContainer.GetComponent<RectTransform>();
        panelUI.starCrushFont  = font;

        // ── Ana Menü butonu ───────────────────────────────────────────────
        var btnObj = new GameObject("MainMenuButton", typeof(RectTransform), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(deathPanel.transform, false);
        var btnRt = btnObj.GetComponent<RectTransform>();
        btnRt.anchorMin = new Vector2(0.5f, 0f);
        btnRt.anchorMax = new Vector2(0.5f, 0f);
        btnRt.pivot = new Vector2(0.5f, 0f);
        btnRt.anchoredPosition = new Vector2(0, 28);
        btnRt.sizeDelta = new Vector2(260, 52);

        var btnImg = btnObj.GetComponent<Image>();
        btnImg.color = new Color(0f, 0f, 0f, 1f);
        AddOutline(btnObj, new Color(0f, 0.020f, 0.047f, 1f));

        var btnLabel = CreateTMP(btnObj.transform, "Label", "MAIN MENU", font, 22, Color.white);
        var blRt = btnLabel.GetComponent<RectTransform>();
        blRt.anchorMin = Vector2.zero; blRt.anchorMax = Vector2.one;
        blRt.offsetMin = blRt.offsetMax = Vector2.zero;

        // Button onClick → PauseManager.LoadSceneByIndex(0)
        var btn = btnObj.GetComponent<Button>();
        btn.targetGraphic = btnImg;
        var cb = new UnityEngine.Events.UnityEvent();
        UnityEditor.Events.UnityEventTools.AddIntPersistentListener(
            btn.onClick, pauseManager.LoadSceneByIndex, 0);

        // PauseManager'a bağla
        pauseManager.deathStatsPanelUI = panelUI;

        EditorUtility.SetDirty(pauseManager);
        EditorUtility.SetDirty(deathPanel);
        Debug.Log("Death Screen kurulumu tamamlandı!");
    }

    [MenuItem("Tools/Setup Stats Panel (Pause Menu)")]
    public static void SetupStatsPanel()
    {
        // --- Pause menü panelini bul ---
        var pauseManager = Object.FindFirstObjectByType<PauseManager>();
        if (pauseManager == null) { Debug.LogError("PauseManager bulunamadı!"); return; }
        if (pauseManager.pauseMenuUI == null) { Debug.LogError("PauseManager.pauseMenuUI atanmamış!"); return; }

        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_PATH);
        if (font == null) Debug.LogWarning($"Star Crush SDF bulunamadı: {FONT_PATH}");

        GameObject pausePanel = pauseManager.pauseMenuUI;

        // Mevcut StatsPanel varsa sil
        var existing = pausePanel.transform.Find("StatsPanel");
        if (existing != null) { Object.DestroyImmediate(existing.gameObject); }

        // ── Ana stats paneli ──────────────────────────────────────────────
        var statsPanel = CreatePanel(pausePanel.transform, "StatsPanel",
            new Vector2(480, 520), new Vector2(0, -30));

        // Arka plan: #000000, outline: #00050C
        var bg = statsPanel.GetComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 1f);
        AddOutline(statsPanel, new Color(0f, 0.020f, 0.047f, 1f));

        // ── Başlık ────────────────────────────────────────────────────────
        var title = CreateTMP(statsPanel.transform, "Title", "— STATS —",
            font, 28, Color.white);
        SetAnchored(title, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0, -28), new Vector2(400, 36));

        // ── Stat satırları kapsayıcısı ────────────────────────────────────
        var rowsContainer = CreateEmptyRect(statsPanel.transform, "StatRows",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0, -76), new Vector2(420, 200));

        var vl = rowsContainer.AddComponent<VerticalLayoutGroup>();
        vl.spacing = 6f;
        vl.childAlignment = TextAnchor.UpperLeft;
        vl.childForceExpandWidth = true;
        vl.childForceExpandHeight = false;
        vl.padding = new RectOffset(8, 8, 4, 4);

        // Stat satırları
        string[] labels = { "Turns Played", "Dice Rolled", "Damage Dealt", "Enemies Killed", "Gold Earned" };
        Color labelColor = Color.white;
        Color valueColor = Color.white;

        var valueTexts = new TMP_Text[labels.Length];
        for (int i = 0; i < labels.Length; i++)
        {
            var row = CreateEmptyRect(rowsContainer.transform, $"Row_{labels[i]}");
            var hl = row.AddComponent<HorizontalLayoutGroup>();
            hl.childAlignment = TextAnchor.MiddleLeft;
            hl.childForceExpandWidth = true;
            hl.childForceExpandHeight = false;
            row.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 30);

            var lbl = CreateTMP(row.transform, "Label", labels[i] + ":",
                font, 17, labelColor);
            lbl.alignment = TextAlignmentOptions.Left;
            lbl.GetComponent<RectTransform>().sizeDelta = new Vector2(220, 30);
            var le = lbl.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = 220; le.flexibleWidth = 0;

            var val = CreateTMP(row.transform, "Value", "0",
                font, 17, valueColor);
            val.fontStyle = FontStyles.Bold;
            val.alignment = TextAlignmentOptions.Right;
            val.GetComponent<RectTransform>().sizeDelta = new Vector2(160, 30);
            var ve = val.gameObject.AddComponent<LayoutElement>();
            ve.preferredWidth = 160; ve.flexibleWidth = 1;

            valueTexts[i] = val;
        }

        // ── Ayraç ─────────────────────────────────────────────────────────
        var divider = new GameObject("Divider", typeof(RectTransform), typeof(Image));
        divider.transform.SetParent(statsPanel.transform, false);
        divider.GetComponent<Image>().color = new Color(0.2f, 0.7f, 1f, 0.25f);
        var divRt = divider.GetComponent<RectTransform>();
        divRt.anchorMin = new Vector2(0.5f, 1f);
        divRt.anchorMax = new Vector2(0.5f, 1f);
        divRt.pivot = new Vector2(0.5f, 1f);
        divRt.anchoredPosition = new Vector2(0, -288);
        divRt.sizeDelta = new Vector2(400, 2);

        // ── Perks başlığı ─────────────────────────────────────────────────
        var perksTitle = CreateTMP(statsPanel.transform, "PerksTitle", "— PERKS —",
            font, 20, Color.white);
        SetAnchored(perksTitle, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0, -306), new Vector2(400, 28));

        // ── Perks kapsayıcısı (scroll yok, vertical layout) ───────────────
        var perksContainer = CreateEmptyRect(statsPanel.transform, "PerksContainer",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0, -342), new Vector2(420, 160));

        var pvl = perksContainer.AddComponent<VerticalLayoutGroup>();
        pvl.spacing = 4f;
        pvl.childAlignment = TextAnchor.MiddleCenter;
        pvl.childForceExpandWidth = true;
        pvl.childForceExpandHeight = false;
        pvl.padding = new RectOffset(8, 8, 2, 2);
        perksContainer.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // ── StatsPanelUI component'ini ekle ve bağla ──────────────────────
        var panelUI = statsPanel.AddComponent<StatsPanelUI>();
        panelUI.turnsValue  = valueTexts[0];
        panelUI.diceValue   = valueTexts[1];
        panelUI.damageValue = valueTexts[2];
        panelUI.killsValue  = valueTexts[3];
        panelUI.goldValue   = valueTexts[4];
        panelUI.perksContainer = perksContainer.GetComponent<RectTransform>();
        panelUI.starCrushFont  = font;

        // PauseManager'a bağla
        pauseManager.statsPanelUI = panelUI;

        EditorUtility.SetDirty(pauseManager);
        EditorUtility.SetDirty(statsPanel);
        Debug.Log("Stats Panel kurulumu tamamlandı!");
    }

    // ── Yardımcı metodlar ─────────────────────────────────────────────────

    static GameObject CreatePanel(Transform parent, string name, Vector2 size, Vector2 pos)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return go;
    }

    static TextMeshProUGUI CreateTMP(Transform parent, string name, string text,
        TMP_FontAsset font, int size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        if (font != null) tmp.font = font;
        return tmp;
    }

    static GameObject CreateEmptyRect(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return go;
    }

    static GameObject CreateEmptyRect(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    static void SetAnchored(TextMeshProUGUI tmp, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 pos, Vector2 size)
    {
        var rt = tmp.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    static void AddOutline(GameObject go, Color color)
    {
        var outline = go.AddComponent<Outline>();
        outline.effectColor = color;
        outline.effectDistance = new Vector2(1.5f, -1.5f);
    }
}
