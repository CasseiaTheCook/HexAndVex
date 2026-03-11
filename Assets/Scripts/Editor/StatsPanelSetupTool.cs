using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

public static class StatsPanelSetupTool
{
    const string FONT_PATH = "Assets/Star Crush SDF.asset";

    // ─────────────────────────────────────────────────────────────────────────
    // DEATH SCREEN
    // ─────────────────────────────────────────────────────────────────────────
    [MenuItem("Tools/Setup Death Screen")]
    public static void SetupDeathScreen()
    {
        var pauseManager = Object.FindFirstObjectByType<PauseManager>();
        if (pauseManager == null) { Debug.LogError("PauseManager bulunamadı!"); return; }
        if (pauseManager.deathMenuUI == null) { Debug.LogError("PauseManager.deathMenuUI atanmamış!"); return; }

        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_PATH);
        if (font == null) Debug.LogWarning($"Star Crush SDF bulunamadı: {FONT_PATH}");

        GameObject deathPanel = pauseManager.deathMenuUI;

        // Temizle
        foreach (string n in new[] { "StatsPanel", "MainMenuButton", "DeathTitle" })
        {
            var t = deathPanel.transform.Find(n);
            if (t != null) Object.DestroyImmediate(t.gameObject);
        }

        // Arka plan
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
            new Vector2(480, 440), new Vector2(0, 20));
        var panelBg = statsPanel.GetComponent<Image>();
        panelBg.color = new Color(0f, 0f, 0f, 1f);
        AddOutline(statsPanel, new Color(0f, 0.020f, 0.047f, 1f));

        // Stat satırları
        var rowsContainer = CreateEmptyRect(statsPanel.transform, "StatRows",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0, -20), new Vector2(420, 180));
        var vl = rowsContainer.AddComponent<VerticalLayoutGroup>();
        vl.spacing = 4f;
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

            var lbl = CreateTMP(row.transform, "Label", labels[i] + ":", font, 15, Color.white);
            lbl.alignment = TextAlignmentOptions.Left;
            var le = lbl.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = 220; le.flexibleWidth = 0;
            lbl.GetComponent<RectTransform>().sizeDelta = new Vector2(220, 28);

            var val = CreateTMP(row.transform, "Value", "0", font, 15, Color.white);
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
        divRt.anchoredPosition = new Vector2(0, -210); divRt.sizeDelta = new Vector2(400, 2);

        // Perks başlığı
        var perksTitle = CreateTMP(statsPanel.transform, "PerksTitle", "— PERKS —", font, 17, Color.white);
        var ptRt = perksTitle.GetComponent<RectTransform>();
        ptRt.anchorMin = new Vector2(0.5f, 1f); ptRt.anchorMax = new Vector2(0.5f, 1f);
        ptRt.pivot = new Vector2(0.5f, 1f);
        ptRt.anchoredPosition = new Vector2(0, -224); ptRt.sizeDelta = new Vector2(400, 24);

        // Perks scroll view
        var (scrollView, perksContent) = CreateScrollView(statsPanel.transform,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0, -252), new Vector2(440, 170));

        var pvl = perksContent.AddComponent<VerticalLayoutGroup>();
        pvl.spacing = 2f;
        pvl.childAlignment = TextAnchor.UpperLeft;
        pvl.childForceExpandWidth = true;
        pvl.childForceExpandHeight = false;
        pvl.padding = new RectOffset(8, 8, 2, 2);
        perksContent.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // StatsPanelUI
        var panelUI = statsPanel.AddComponent<StatsPanelUI>();
        panelUI.turnsValue  = valueTexts[0];
        panelUI.diceValue   = valueTexts[1];
        panelUI.damageValue = valueTexts[2];
        panelUI.killsValue  = valueTexts[3];
        panelUI.goldValue   = valueTexts[4];
        panelUI.perksContainer = perksContent.GetComponent<RectTransform>();
        panelUI.starCrushFont  = font;

        // ── Ana Menü butonu ───────────────────────────────────────────────
        var btnObj = new GameObject("MainMenuButton", typeof(RectTransform), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(deathPanel.transform, false);
        var btnRt = btnObj.GetComponent<RectTransform>();
        btnRt.anchorMin = new Vector2(0.5f, 0f);
        btnRt.anchorMax = new Vector2(0.5f, 0f);
        btnRt.pivot = new Vector2(0.5f, 0f);
        btnRt.anchoredPosition = new Vector2(0, 20);
        btnRt.sizeDelta = new Vector2(260, 48);

        var btnImg = btnObj.GetComponent<Image>();
        btnImg.color = new Color(0f, 0f, 0f, 1f);
        AddOutline(btnObj, new Color(0f, 0.020f, 0.047f, 1f));

        var btnLabel = CreateTMP(btnObj.transform, "Label", "MAIN MENU", font, 20, Color.white);
        var blRt = btnLabel.GetComponent<RectTransform>();
        blRt.anchorMin = Vector2.zero; blRt.anchorMax = Vector2.one;
        blRt.offsetMin = blRt.offsetMax = Vector2.zero;

        var btn = btnObj.GetComponent<Button>();
        btn.targetGraphic = btnImg;
        UnityEditor.Events.UnityEventTools.AddIntPersistentListener(
            btn.onClick, pauseManager.LoadSceneByIndex, 0);

        pauseManager.deathStatsPanelUI = panelUI;
        EditorUtility.SetDirty(pauseManager);
        EditorUtility.SetDirty(deathPanel);
        Debug.Log("Death Screen kurulumu tamamlandı!");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PAUSE MENU
    // ─────────────────────────────────────────────────────────────────────────
    [MenuItem("Tools/Setup Stats Panel (Pause Menu)")]
    public static void SetupStatsPanel()
    {
        var pauseManager = Object.FindFirstObjectByType<PauseManager>();
        if (pauseManager == null) { Debug.LogError("PauseManager bulunamadı!"); return; }
        if (pauseManager.pauseMenuUI == null) { Debug.LogError("PauseManager.pauseMenuUI atanmamış!"); return; }

        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_PATH);
        if (font == null) Debug.LogWarning($"Star Crush SDF bulunamadı: {FONT_PATH}");

        GameObject pausePanel = pauseManager.pauseMenuUI;

        // Temizle
        foreach (string n in new[] { "StatsPanel", "ContinueButton" })
        {
            var t = pausePanel.transform.Find(n);
            if (t != null) Object.DestroyImmediate(t.gameObject);
        }

        // ── Stats paneli ──────────────────────────────────────────────────
        var statsPanel = CreatePanel(pausePanel.transform, "StatsPanel",
            new Vector2(480, 520), new Vector2(0, -30));
        var bg = statsPanel.GetComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 1f);
        AddOutline(statsPanel, new Color(0f, 0.020f, 0.047f, 1f));

        // Başlık
        var title = CreateTMP(statsPanel.transform, "Title", "— STATS —", font, 26, Color.white);
        SetAnchored(title, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0, -24), new Vector2(400, 34));

        // Stat satırları
        var rowsContainer = CreateEmptyRect(statsPanel.transform, "StatRows",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0, -68), new Vector2(420, 190));
        var vl = rowsContainer.AddComponent<VerticalLayoutGroup>();
        vl.spacing = 5f;
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
            row.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 30);

            var lbl = CreateTMP(row.transform, "Label", labels[i] + ":", font, 16, Color.white);
            lbl.alignment = TextAlignmentOptions.Left;
            lbl.GetComponent<RectTransform>().sizeDelta = new Vector2(220, 30);
            var le = lbl.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = 220; le.flexibleWidth = 0;

            var val = CreateTMP(row.transform, "Value", "0", font, 16, Color.white);
            val.fontStyle = FontStyles.Bold;
            val.alignment = TextAlignmentOptions.Right;
            val.GetComponent<RectTransform>().sizeDelta = new Vector2(160, 30);
            var ve = val.gameObject.AddComponent<LayoutElement>();
            ve.preferredWidth = 160; ve.flexibleWidth = 1;

            valueTexts[i] = val;
        }

        // Ayraç
        var divider = new GameObject("Divider", typeof(RectTransform), typeof(Image));
        divider.transform.SetParent(statsPanel.transform, false);
        divider.GetComponent<Image>().color = new Color(0.2f, 0.7f, 1f, 0.25f);
        var divRt = divider.GetComponent<RectTransform>();
        divRt.anchorMin = new Vector2(0.5f, 1f); divRt.anchorMax = new Vector2(0.5f, 1f);
        divRt.pivot = new Vector2(0.5f, 1f);
        divRt.anchoredPosition = new Vector2(0, -274); divRt.sizeDelta = new Vector2(400, 2);

        // Perks başlığı
        var perksTitle = CreateTMP(statsPanel.transform, "PerksTitle", "— PERKS —", font, 18, Color.white);
        SetAnchored(perksTitle, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0, -288), new Vector2(400, 26));

        // Perks scroll view
        var (scrollView, perksContent) = CreateScrollView(statsPanel.transform,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0, -318), new Vector2(440, 185));

        var pvl = perksContent.AddComponent<VerticalLayoutGroup>();
        pvl.spacing = 2f;
        pvl.childAlignment = TextAnchor.UpperLeft;
        pvl.childForceExpandWidth = true;
        pvl.childForceExpandHeight = false;
        pvl.padding = new RectOffset(8, 8, 2, 2);
        perksContent.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // StatsPanelUI
        var panelUI = statsPanel.AddComponent<StatsPanelUI>();
        panelUI.turnsValue  = valueTexts[0];
        panelUI.diceValue   = valueTexts[1];
        panelUI.damageValue = valueTexts[2];
        panelUI.killsValue  = valueTexts[3];
        panelUI.goldValue   = valueTexts[4];
        panelUI.perksContainer = perksContent.GetComponent<RectTransform>();
        panelUI.starCrushFont  = font;

        pauseManager.statsPanelUI = panelUI;

        // ── Continue butonu ───────────────────────────────────────────────
        var contBtn = CreateButton(pausePanel.transform, "ContinueButton", "CONTINUE", font, 20,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0, -8), new Vector2(260, 48));
        UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(
            contBtn.GetComponent<Button>().onClick, pauseManager.Resume);

        EditorUtility.SetDirty(pauseManager);
        EditorUtility.SetDirty(statsPanel);
        Debug.Log("Stats Panel (Pause) kurulumu tamamlandı!");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // YARDIMCI METODLAR
    // ─────────────────────────────────────────────────────────────────────────

    static (GameObject scrollView, GameObject content) CreateScrollView(
        Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size)
    {
        // ScrollView kapsayıcısı
        var svGo = new GameObject("PerksScrollView", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        svGo.transform.SetParent(parent, false);
        var svRt = svGo.GetComponent<RectTransform>();
        svRt.anchorMin = anchorMin; svRt.anchorMax = anchorMax;
        svRt.pivot = new Vector2(0.5f, 1f);
        svRt.anchoredPosition = pos;
        svRt.sizeDelta = size;
        svGo.GetComponent<Image>().color = new Color(0, 0, 0, 0); // şeffaf

        // Viewport
        var vpGo = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        vpGo.transform.SetParent(svGo.transform, false);
        var vpRt = vpGo.GetComponent<RectTransform>();
        vpRt.anchorMin = Vector2.zero; vpRt.anchorMax = Vector2.one;
        vpRt.offsetMin = vpRt.offsetMax = Vector2.zero;
        vpGo.GetComponent<Image>().color = new Color(0, 0, 0, 0.01f); // mask için
        vpGo.GetComponent<Mask>().showMaskGraphic = false;

        // Content
        var contentGo = new GameObject("Content", typeof(RectTransform));
        contentGo.transform.SetParent(vpGo.transform, false);
        var contentRt = contentGo.GetComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0f, 1f); contentRt.anchorMax = new Vector2(1f, 1f);
        contentRt.pivot = new Vector2(0.5f, 1f);
        contentRt.offsetMin = Vector2.zero; contentRt.offsetMax = Vector2.zero;

        // ScrollRect bağla
        var sr = svGo.GetComponent<ScrollRect>();
        sr.content = contentRt;
        sr.viewport = vpRt;
        sr.horizontal = false;
        sr.vertical = true;
        sr.scrollSensitivity = 30f;
        sr.movementType = ScrollRect.MovementType.Clamped;

        return (svGo, contentGo);
    }

    static GameObject CreateButton(Transform parent, string name, string label,
        TMP_FontAsset font, int fontSize,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = pos; rt.sizeDelta = size;

        var img = go.GetComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 1f);
        AddOutline(go, new Color(0f, 0.020f, 0.047f, 1f));

        var btn = go.GetComponent<Button>();
        btn.targetGraphic = img;

        var lbl = CreateTMP(go.transform, "Label", label, font, fontSize, Color.white);
        var lRt = lbl.GetComponent<RectTransform>();
        lRt.anchorMin = Vector2.zero; lRt.anchorMax = Vector2.one;
        lRt.offsetMin = lRt.offsetMax = Vector2.zero;

        return go;
    }

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
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = pos; rt.sizeDelta = size;
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
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = pos; rt.sizeDelta = size;
    }

    static void AddOutline(GameObject go, Color color)
    {
        var outline = go.AddComponent<Outline>();
        outline.effectColor = color;
        outline.effectDistance = new Vector2(1.5f, -1.5f);
    }
}
