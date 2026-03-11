using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// GuideBook UI'ını sahneye otomatik kurar + starter veri oluşturur.
/// HexAndVex > Setup Guide Book UI menüsünden erişilir.
/// Layout: Sol sidebar (sayfa listesi) + Sağ içerik (scroll edilebilir gövde).
/// </summary>
public class GuideBookSetupTool : EditorWindow
{
    [MenuItem("HexAndVex/Setup Guide Book UI")]
    public static void OpenWindow()
    {
        var win = GetWindow<GuideBookSetupTool>("GuideBook Setup");
        win.minSize = new Vector2(320, 180);
    }

    private void OnGUI()
    {
        GUILayout.Space(10);
        GUILayout.Label("GuideBook Kurulum Aracı", EditorStyles.boldLabel);
        GUILayout.Space(5);
        GUILayout.Label("Sahneye GuideBook Canvas'ını, sol sidebar'ı,\nsağ içerik alanını ve tüm UI elemanlarını kurar.", EditorStyles.wordWrappedLabel);
        GUILayout.Space(15);

        if (GUILayout.Button("Sahneye Kur", GUILayout.Height(36)))
            SetupGuideBook();

        GUILayout.Space(5);
        if (GUILayout.Button("Sadece Veri Dosyası Oluştur", GUILayout.Height(28)))
            CreateDataAsset();
    }

    // ═══════════════════════════════════════════════════════
    //  ANA KURULUM
    // ═══════════════════════════════════════════════════════
    private static void SetupGuideBook()
    {
        // Zaten varsa uyar
        if (Object.FindFirstObjectByType<GuideBookManager>() != null)
        {
            if (!EditorUtility.DisplayDialog("GuideBook Zaten Var",
                "Sahnede zaten bir GuideBookManager bulundu. Yeniden oluşturmak istiyor musun?",
                "Evet, Yeniden Oluştur", "İptal"))
                return;

            var old = Object.FindFirstObjectByType<GuideBookManager>();
            if (old != null) Undo.DestroyObjectImmediate(old.gameObject);
        }

        TMP_FontAsset font = UIStyle.LoadFont();
        GuideBookData dataAsset = CreateDataAsset();

        // ── 1. Ana Canvas ────────────────────────────────────
        GameObject canvasGO = new GameObject("GuideBookCanvas");
        Undo.RegisterCreatedObjectUndo(canvasGO, "Create GuideBook");

        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasGO.AddComponent<GraphicRaycaster>();

        // ── 2. Manager + UI bileşenleri ──────────────────────
        GuideBookManager manager = canvasGO.AddComponent<GuideBookManager>();
        manager.bookData = dataAsset;

        GuideBookUI ui = canvasGO.AddComponent<GuideBookUI>();
        ui.customFont = font;

        // ── 3. Kitap İkonu (Sol Alt) ─────────────────────────
        GameObject iconBtnGO = CreateButton(canvasGO.transform, "BookIconButton", 50f, 50f, font);
        RectTransform iconRT = iconBtnGO.GetComponent<RectTransform>();
        iconRT.anchorMin = iconRT.anchorMax = new Vector2(0, 0);
        iconRT.pivot = new Vector2(0, 0);
        iconRT.anchoredPosition = new Vector2(20f, 20f);

        TMP_Text iconLabel = iconBtnGO.GetComponentInChildren<TMP_Text>();
        if (iconLabel != null)
        {
            iconLabel.text = "?";
            iconLabel.fontSize = 30;
            iconLabel.fontStyle = FontStyles.Bold;
            iconLabel.alignment = TextAlignmentOptions.Center;
        }

        iconBtnGO.GetComponent<Image>().color = UIStyle.BgDark;
        iconBtnGO.GetComponent<Button>().colors = UIStyle.ButtonColors();
        UIStyle.AddOutline(iconBtnGO);

        ui.bookIconButton = iconBtnGO.GetComponent<Button>();
        ui.bookIconRect = iconRT;

        // ── 4. Kitap Paneli (Ana Container) ──────────────────
        // [BookPanel 760x560]
        GameObject panelGO = new GameObject("BookPanel", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        panelGO.transform.SetParent(canvasGO.transform, false);

        RectTransform panelRT = panelGO.GetComponent<RectTransform>();
        panelRT.anchorMin = panelRT.anchorMax = new Vector2(0.5f, 0.5f);
        panelRT.sizeDelta = new Vector2(760f, 560f);
        panelRT.anchoredPosition = Vector2.zero;

        panelGO.GetComponent<Image>().color = new Color(0.02f, 0.02f, 0.05f, 0.97f);
        CanvasGroup panelCG = panelGO.GetComponent<CanvasGroup>();
        panelCG.alpha = 0f;

        UIStyle.AddOutline(panelGO);

        Canvas panelCanvas = panelGO.AddComponent<Canvas>();
        panelCanvas.overrideSorting = true;
        panelCanvas.sortingOrder = 50;
        panelGO.AddComponent<GraphicRaycaster>();

        ui.bookPanel = panelRT;
        ui.bookCanvasGroup = panelCG;

        // ── 5. Üst Bar: Başlık + Kapat Butonu ───────────────
        GameObject topBar = CreateRect(panelGO.transform, "TopBar");
        SetAnchorsStretchTop(topBar, 10f, 10f, 10f, 45f);
        AddHorizontalGroup(topBar, TextAnchor.MiddleCenter, 8f);

        // Başlık
        GameObject titleGO = CreateTMPText(topBar.transform, "TitleText", "GUIDE BOOK", font,
            UIStyle.FontSizeTitle, Color.white, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
        AddLE(titleGO, flexW: 1f, minH: 40f);
        ui.titleText = titleGO.GetComponent<TMP_Text>();

        // Kapat butonu
        GameObject closeBtnGO = CreateButton(topBar.transform, "CloseButton", 40f, 40f, font);
        TMP_Text closeTxt = closeBtnGO.GetComponentInChildren<TMP_Text>();
        closeTxt.text = "X";
        closeTxt.fontSize = 22;
        closeTxt.color = Color.red;
        closeTxt.fontStyle = FontStyles.Bold;
        closeTxt.alignment = TextAlignmentOptions.Center;
        closeBtnGO.GetComponent<Button>().colors = UIStyle.ButtonColors();
        AddLE(closeBtnGO, prefW: 40f, prefH: 40f);
        ui.closeButton = closeBtnGO.GetComponent<Button>();

        // ── 6. Ayırıcı ──────────────────────────────────────
        GameObject div1 = CreateRect(panelGO.transform, "Divider1", typeof(Image));
        SetAnchorsStretchHorizontal(div1, 10f, 10f, 55f, 1.5f);
        div1.GetComponent<Image>().color = UIStyle.DividerColor;
        div1.GetComponent<Image>().raycastTarget = false;

        // ── 7. Kategori Sekmeleri (5 adet, ALL yok) ─────────
        GameObject catBar = CreateRect(panelGO.transform, "CategoryBar");
        SetAnchorsStretchHorizontal(catBar, 10f, 10f, 62f, 32f);
        AddHorizontalGroup(catBar, TextAnchor.MiddleCenter, 4f);

        string[] catLabels = { "COMBAT", "ENEMIES", "ITEMS", "PERKS", "MOVEMENT" };
        ui.categoryButtons = new List<Button>();
        ui.categoryButtonTexts = new List<TMP_Text>();

        for (int i = 0; i < catLabels.Length; i++)
        {
            GameObject catBtnGO = CreateButton(catBar.transform, $"CatBtn_{catLabels[i]}", 0, 30f, font);
            AddLE(catBtnGO, flexW: 1f, prefH: 30f);

            TMP_Text catTxt = catBtnGO.GetComponentInChildren<TMP_Text>();
            catTxt.text = catLabels[i];
            catTxt.fontSize = 12;
            catTxt.alignment = TextAlignmentOptions.Center;
            catTxt.color = new Color(0.6f, 0.6f, 0.6f, 1f);

            // Başlangıçta hepsi inactive (açılışta filtre yok)
            Button catBtn = catBtnGO.GetComponent<Button>();
            ColorBlock inactiveCB = MakeUniformColorBlock(new Color(0.15f, 0.15f, 0.2f, 1f),
                new Color(0.22f, 0.22f, 0.3f, 1f), new Color(0f, 0.58f, 0.74f, 1f));
            catBtn.colors = inactiveCB;

            ui.categoryButtons.Add(catBtn);
            ui.categoryButtonTexts.Add(catTxt);
        }

        // ── 8. Ana Alan: Sidebar + Ayırıcı + İçerik ─────────
        // Top offset = topBar(45) + pad(8) + divider(1.5) + catBar(32) + pad(5) = ~100
        GameObject mainArea = CreateRect(panelGO.transform, "MainArea");
        RectTransform mainAreaRT = mainArea.GetComponent<RectTransform>();
        mainAreaRT.anchorMin = new Vector2(0, 0);
        mainAreaRT.anchorMax = new Vector2(1, 1);
        mainAreaRT.offsetMin = new Vector2(10f, 10f);   // sol, alt
        mainAreaRT.offsetMax = new Vector2(-10f, -100f); // sağ, üst

        AddHorizontalGroup(mainArea, TextAnchor.UpperLeft, 0f, controlW: true, controlH: true,
            forceExpandW: false, forceExpandH: true);

        // ── 8a. Sidebar (Sol — 170px) ────────────────────────
        GameObject sidebarGO = CreateRect(mainArea.transform, "Sidebar", typeof(Image));
        AddLE(sidebarGO, prefW: 170f, flexH: 1f);
        sidebarGO.GetComponent<Image>().color = new Color(0.04f, 0.04f, 0.07f, 1f);

        // ScrollRect — sidebar scroll
        ScrollRect sidebarSR = sidebarGO.AddComponent<ScrollRect>();
        sidebarSR.horizontal = false;
        sidebarSR.vertical = true;
        sidebarSR.movementType = ScrollRect.MovementType.Clamped;
        sidebarSR.scrollSensitivity = 20f;

        // Viewport
        GameObject sidebarViewport = CreateRect(sidebarGO.transform, "Viewport", typeof(Image), typeof(Mask));
        RectTransform vpRT = sidebarViewport.GetComponent<RectTransform>();
        vpRT.anchorMin = Vector2.zero;
        vpRT.anchorMax = Vector2.one;
        vpRT.offsetMin = vpRT.offsetMax = Vector2.zero;
        sidebarViewport.GetComponent<Image>().color = Color.white;
        sidebarViewport.GetComponent<Mask>().showMaskGraphic = false;

        sidebarSR.viewport = vpRT;

        // Content (VerticalLayoutGroup)
        GameObject sidebarContentGO = CreateRect(sidebarViewport.transform, "SidebarContent");
        RectTransform scRT = sidebarContentGO.GetComponent<RectTransform>();
        scRT.anchorMin = new Vector2(0, 1);
        scRT.anchorMax = new Vector2(1, 1);
        scRT.pivot = new Vector2(0.5f, 1f);
        scRT.sizeDelta = new Vector2(0, 0);

        VerticalLayoutGroup sidebarVLG = sidebarContentGO.AddComponent<VerticalLayoutGroup>();
        sidebarVLG.childAlignment = TextAnchor.UpperLeft;
        sidebarVLG.spacing = 2f;
        sidebarVLG.padding = new RectOffset(2, 2, 4, 4);
        sidebarVLG.childForceExpandWidth = true;
        sidebarVLG.childForceExpandHeight = false;
        sidebarVLG.childControlWidth = true;
        sidebarVLG.childControlHeight = true;

        ContentSizeFitter sidebarCSF = sidebarContentGO.AddComponent<ContentSizeFitter>();
        sidebarCSF.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        sidebarCSF.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        sidebarSR.content = scRT;

        ui.sidebarScrollRect = sidebarSR;
        ui.sidebarContent = scRT;

        // ── 8b. Dikey Ayırıcı ────────────────────────────────
        GameObject vertDiv = CreateRect(mainArea.transform, "VerticalDivider", typeof(Image));
        AddLE(vertDiv, prefW: 1.5f, flexH: 1f);
        vertDiv.GetComponent<Image>().color = UIStyle.DividerColor;
        vertDiv.GetComponent<Image>().raycastTarget = false;

        // ── 8c. İçerik Alanı (Sağ — flexible) ───────────────
        GameObject contentArea = CreateRect(mainArea.transform, "ContentArea");
        AddLE(contentArea, flexW: 1f, flexH: 1f);

        VerticalLayoutGroup contentVLG = contentArea.AddComponent<VerticalLayoutGroup>();
        contentVLG.childAlignment = TextAnchor.UpperLeft;
        contentVLG.spacing = 4f;
        contentVLG.padding = new RectOffset(12, 8, 6, 6);
        contentVLG.childForceExpandWidth = true;
        contentVLG.childForceExpandHeight = false;
        contentVLG.childControlWidth = true;
        contentVLG.childControlHeight = true;

        // Kategori label
        GameObject catLabelGO = CreateTMPText(contentArea.transform, "CategoryLabel", "", font,
            12, new Color(0.5f, 0.5f, 0.5f), FontStyles.Italic, TextAlignmentOptions.TopRight);
        AddLE(catLabelGO, prefH: 18f, flexW: 1f);
        ui.categoryLabel = catLabelGO.GetComponent<TMP_Text>();

        // İçerik başlığı (sayfa başlığı burada — üst bar'daki başlık "GUIDE BOOK" sabit kalır)
        // Aslında titleText'i burada gösterelim, üst bar'da sabit "GUIDE BOOK" yazsın
        GameObject pageTitleGO = CreateTMPText(contentArea.transform, "PageTitle", "How to Move", font,
            24, Color.white, FontStyles.Bold, TextAlignmentOptions.TopLeft);
        AddLE(pageTitleGO, prefH: 34f, flexW: 1f);
        // titleText'i sayfa başlığına bağla, üst bar'da sabit başlık kalacak
        ui.titleText = pageTitleGO.GetComponent<TMP_Text>();

        // ── İçerik ScrollRect ────────────────────────────────
        GameObject contentScrollGO = CreateRect(contentArea.transform, "ContentScrollRect");
        AddLE(contentScrollGO, flexW: 1f, flexH: 1f);

        ScrollRect contentSR = contentScrollGO.AddComponent<ScrollRect>();
        contentSR.horizontal = false;
        contentSR.vertical = true;
        contentSR.movementType = ScrollRect.MovementType.Elastic;
        contentSR.scrollSensitivity = 20f;

        // Viewport
        GameObject contentVP = CreateRect(contentScrollGO.transform, "Viewport", typeof(Image), typeof(Mask));
        RectTransform cvpRT = contentVP.GetComponent<RectTransform>();
        cvpRT.anchorMin = Vector2.zero;
        cvpRT.anchorMax = Vector2.one;
        cvpRT.offsetMin = cvpRT.offsetMax = Vector2.zero;
        contentVP.GetComponent<Image>().color = Color.white;
        contentVP.GetComponent<Mask>().showMaskGraphic = false;

        contentSR.viewport = cvpRT;

        // ScrollContent (VerticalLayoutGroup + ContentSizeFitter)
        GameObject scrollContentGO = CreateRect(contentVP.transform, "ScrollContent");
        RectTransform scrollContentRT = scrollContentGO.GetComponent<RectTransform>();
        scrollContentRT.anchorMin = new Vector2(0, 1);
        scrollContentRT.anchorMax = new Vector2(1, 1);
        scrollContentRT.pivot = new Vector2(0.5f, 1f);
        scrollContentRT.sizeDelta = new Vector2(0, 0);

        VerticalLayoutGroup scrollVLG = scrollContentGO.AddComponent<VerticalLayoutGroup>();
        scrollVLG.childAlignment = TextAnchor.UpperLeft;
        scrollVLG.spacing = 8f;
        scrollVLG.padding = new RectOffset(0, 0, 0, 10);
        scrollVLG.childForceExpandWidth = true;
        scrollVLG.childForceExpandHeight = false;
        scrollVLG.childControlWidth = true;
        scrollVLG.childControlHeight = true;

        ContentSizeFitter scrollCSF = scrollContentGO.AddComponent<ContentSizeFitter>();
        scrollCSF.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scrollCSF.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        contentSR.content = scrollContentRT;

        ui.contentScrollRect = contentSR;
        ui.contentScrollContent = scrollContentRT;

        // Body text
        GameObject bodyGO = CreateTMPText(scrollContentGO.transform, "BodyText",
            "Page content goes here...", font, UIStyle.FontSizeMid,
            new Color(0.85f, 0.85f, 0.85f), FontStyles.Normal, TextAlignmentOptions.TopLeft);
        TMP_Text bodyTmp = bodyGO.GetComponent<TMP_Text>();
        bodyTmp.textWrappingMode = TextWrappingModes.Normal;

        ContentSizeFitter bodyCSF = bodyGO.AddComponent<ContentSizeFitter>();
        bodyCSF.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        bodyCSF.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        ui.bodyText = bodyTmp;

        // İllüstrasyon
        GameObject illusGO = new GameObject("Illustration", typeof(RectTransform), typeof(Image));
        illusGO.transform.SetParent(scrollContentGO.transform, false);
        Image illusImg = illusGO.GetComponent<Image>();
        illusImg.preserveAspect = true;
        illusImg.color = Color.white;
        illusImg.raycastTarget = false;
        AddLE(illusGO, prefH: 180f, flexW: 1f);
        illusGO.SetActive(false);
        ui.illustrationImage = illusImg;

        // ── Son Ayarlar ──────────────────────────────────────

        // Üst bar'daki başlığı sabit yap
        Transform topBarTitleTr = topBar.transform.Find("TitleText");
        GameObject topBarTitle = topBarTitleTr != null ? topBarTitleTr.gameObject : null;
        if (topBarTitle != null)
        {
            // Üst bar'ın başlığını sabit "GUIDE BOOK" yap — sayfa başlığı ayrı
            TMP_Text topTitleTmp = topBarTitle.GetComponent<TMP_Text>();
            topTitleTmp.text = "GUIDE BOOK";
        }

        panelGO.SetActive(false);

        Selection.activeGameObject = canvasGO;
        EditorUtility.SetDirty(canvasGO);

        Debug.Log("GuideBook UI sahneye kuruldu! Sol alttaki '?' butonuna tıklayarak kitabı aç.");
    }

    // ═══════════════════════════════════════════════════════
    //  VERİ OLUŞTURMA
    // ═══════════════════════════════════════════════════════
    private static GuideBookData CreateDataAsset()
    {
        string assetPath = "Assets/Data/GuideBookData.asset";
        GuideBookData existing = AssetDatabase.LoadAssetAtPath<GuideBookData>(assetPath);
        if (existing != null) return existing;

        if (!AssetDatabase.IsValidFolder("Assets/Data"))
            AssetDatabase.CreateFolder("Assets", "Data");

        GuideBookData data = ScriptableObject.CreateInstance<GuideBookData>();
        data.pages = new List<GuideBookPage>
        {
            new GuideBookPage
            {
                title = "How to Move", category = "Movement",
                bodyText = "Each turn, you can move to any highlighted hex adjacent to you. Blue hexes show valid moves. Moving into an enemy attacks them — no separate attack button needed. You start with 1 move per turn. Some perks grant extra moves."
            },
            new GuideBookPage
            {
                title = "Dice & Damage", category = "Combat",
                bodyText = "When you attack an enemy by moving into them, dice are rolled to determine damage. You start with 2 dice. More dice = more potential damage. Critical hits multiply your damage. Watch the dice panel — combos and crits are highlighted. Some perks modify dice rolls and damage."
            },
            new GuideBookPage
            {
                title = "Enemy Types", category = "Enemies",
                bodyText = "MELEE: Moves toward you and attacks on contact.\nTELEGRAPH AOE: Shows a warning tile (red hex) before unleashing an area attack. Move out of the highlighted zone!\nTOTEM: Stationary, buffs nearby enemies.\nWARLOCK: Ranged attacker with special abilities.\nBOSS: Powerful enemies with multiple mechanics — read their patterns carefully."
            },
            new GuideBookPage
            {
                title = "Items & Shop", category = "Items",
                bodyText = "After each room, a Shop appears. Spend gold to buy items. Items provide powerful one-time or persistent effects. You can reroll the shop for a cost — rerolling gets more expensive each time. Gold is earned by killing enemies. Some perks increase gold drops."
            },
            new GuideBookPage
            {
                title = "Perks & Level Up", category = "Perks",
                bodyText = "After clearing a room, choose a perk. Perks are passive upgrades that persist for the run. Rarities: Common (grey) → Rare (blue) → Epic (purple) → Legendary (gold) → Secret. Many perks have multiple levels — you can pick the same perk again to upgrade it. Build synergies between perks for powerful combinations."
            }
        };

        AssetDatabase.CreateAsset(data, assetPath);
        AssetDatabase.SaveAssets();
        Debug.Log("GuideBookData oluşturuldu: " + assetPath);
        return data;
    }

    // ═══════════════════════════════════════════════════════
    //  YARDIMCI FONKSİYONLAR
    // ═══════════════════════════════════════════════════════

    private static GameObject CreateRect(Transform parent, string name, params System.Type[] extras)
    {
        var types = new List<System.Type> { typeof(RectTransform) };
        types.AddRange(extras);
        GameObject go = new GameObject(name, types.ToArray());
        go.transform.SetParent(parent, false);
        return go;
    }

    private static GameObject CreateButton(Transform parent, string name, float width, float height, TMP_FontAsset font)
    {
        GameObject btnGO = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        btnGO.transform.SetParent(parent, false);
        btnGO.GetComponent<RectTransform>().sizeDelta = new Vector2(width, height);
        btnGO.GetComponent<Image>().color = UIStyle.BgDark;

        GameObject txtGO = new GameObject("Label", typeof(RectTransform));
        txtGO.transform.SetParent(btnGO.transform, false);
        TMP_Text tmp = txtGO.AddComponent<TextMeshProUGUI>();
        if (font != null) tmp.font = font;
        tmp.text = name;
        tmp.fontSize = UIStyle.FontSizeMid;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;

        RectTransform txtRT = txtGO.GetComponent<RectTransform>();
        txtRT.anchorMin = Vector2.zero;
        txtRT.anchorMax = Vector2.one;
        txtRT.offsetMin = txtRT.offsetMax = Vector2.zero;

        return btnGO;
    }

    private static GameObject CreateTMPText(Transform parent, string name, string text,
        TMP_FontAsset font, int fontSize, Color color, FontStyles style, TextAlignmentOptions alignment)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        TMP_Text tmp = go.AddComponent<TextMeshProUGUI>();
        if (font != null) tmp.font = font;
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.fontStyle = style;
        tmp.alignment = alignment;
        tmp.raycastTarget = false;
        return go;
    }

    /// <summary>
    /// Üste anchor'lanmış, yatay stretch, belirli yükseklik.
    /// </summary>
    private static void SetAnchorsStretchTop(GameObject go, float left, float right, float topOffset, float height)
    {
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.offsetMin = new Vector2(left, 0);
        rt.offsetMax = new Vector2(-right, -topOffset);
        rt.sizeDelta = new Vector2(rt.sizeDelta.x, height);
    }

    /// <summary>
    /// Yatay stretch, üstten belirli offset, belirli yükseklik.
    /// </summary>
    private static void SetAnchorsStretchHorizontal(GameObject go, float left, float right, float topOffset, float height)
    {
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0, -topOffset);
        rt.offsetMin = new Vector2(left, -topOffset - height);
        rt.offsetMax = new Vector2(-right, -topOffset);
    }

    private static void AddHorizontalGroup(GameObject go, TextAnchor alignment, float spacing,
        bool controlW = true, bool controlH = true, bool forceExpandW = false, bool forceExpandH = false)
    {
        HorizontalLayoutGroup hlg = go.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = alignment;
        hlg.spacing = spacing;
        hlg.childControlWidth = controlW;
        hlg.childControlHeight = controlH;
        hlg.childForceExpandWidth = forceExpandW;
        hlg.childForceExpandHeight = forceExpandH;
    }

    private static void AddLE(GameObject go, float prefW = -1, float prefH = -1,
        float flexW = -1, float flexH = -1, float minH = -1, float minW = -1)
    {
        LayoutElement le = go.GetComponent<LayoutElement>();
        if (le == null) le = go.AddComponent<LayoutElement>();
        if (prefW >= 0) le.preferredWidth = prefW;
        if (prefH >= 0) le.preferredHeight = prefH;
        if (flexW >= 0) le.flexibleWidth = flexW;
        if (flexH >= 0) le.flexibleHeight = flexH;
        if (minH >= 0) le.minHeight = minH;
        if (minW >= 0) le.minWidth = minW;
    }

    /// <summary>
    /// Tüm Button state'lerini aynı renkte yapan ColorBlock oluştur.
    /// Unity'nin hover/press override'ını engeller.
    /// </summary>
    private static ColorBlock MakeUniformColorBlock(Color normal, Color highlighted, Color pressed)
    {
        ColorBlock cb = ColorBlock.defaultColorBlock;
        cb.normalColor = normal;
        cb.highlightedColor = highlighted;
        cb.pressedColor = pressed;
        cb.selectedColor = normal;
        cb.disabledColor = normal;
        cb.colorMultiplier = 1f;
        return cb;
    }
}
