using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// GuideBook UI'ını sahneye otomatik kurar + starter veri oluşturur.
/// HexAndVex > Setup Guide Book UI menüsünden erişilir.
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
        GUILayout.Label("Bu araç sahneye GuideBook Canvas'ını,\nkitap ikonunu ve tüm UI elemanlarını ekler.\nAyrıca örnek veri dosyası oluşturur.", EditorStyles.wordWrappedLabel);
        GUILayout.Space(15);

        if (GUILayout.Button("Sahneye Kur", GUILayout.Height(36)))
        {
            SetupGuideBook();
        }

        GUILayout.Space(5);
        if (GUILayout.Button("Sadece Veri Dosyası Oluştur", GUILayout.Height(28)))
        {
            CreateDataAsset();
        }
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

            // Eskisini sil
            var old = Object.FindFirstObjectByType<GuideBookManager>();
            if (old != null) Undo.DestroyObjectImmediate(old.gameObject);
        }

        TMP_FontAsset font = UIStyle.LoadFont();

        // ── 1. Veri dosyası ──────────────────────────────────
        GuideBookData dataAsset = CreateDataAsset();

        // ── 2. Ana Canvas GameObject ─────────────────────────
        GameObject canvasGO = new GameObject("GuideBookCanvas");
        Undo.RegisterCreatedObjectUndo(canvasGO, "Create GuideBook");

        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasGO.AddComponent<GraphicRaycaster>();

        // ── 3. Manager bileşeni ──────────────────────────────
        GuideBookManager manager = canvasGO.AddComponent<GuideBookManager>();
        manager.bookData = dataAsset;

        // ── 4. UI bileşeni ───────────────────────────────────
        GuideBookUI ui = canvasGO.AddComponent<GuideBookUI>();
        ui.customFont = font;

        // ── 5. Kitap İkonu (Sol Alt) ─────────────────────────
        GameObject iconBtnGO = CreateButton(canvasGO.transform, "BookIconButton", 50f, 50f, font);
        RectTransform iconRT = iconBtnGO.GetComponent<RectTransform>();
        iconRT.anchorMin = new Vector2(0, 0);
        iconRT.anchorMax = new Vector2(0, 0);
        iconRT.pivot = new Vector2(0, 0);
        iconRT.anchoredPosition = new Vector2(20f, 20f);

        // İkon yerine "?" yazısı (sprite olmadan)
        TMP_Text iconLabel = iconBtnGO.GetComponentInChildren<TMP_Text>();
        if (iconLabel != null)
        {
            iconLabel.text = "?";
            iconLabel.fontSize = 30;
            iconLabel.fontStyle = FontStyles.Bold;
            iconLabel.alignment = TextAlignmentOptions.Center;
        }

        Image iconBg = iconBtnGO.GetComponent<Image>();
        if (iconBg != null)
        {
            iconBg.color = UIStyle.BgDark;
        }

        Button iconBtn = iconBtnGO.GetComponent<Button>();
        iconBtn.colors = UIStyle.ButtonColors();

        UIStyle.AddOutline(iconBtnGO);

        ui.bookIconButton = iconBtn;
        ui.bookIconRect = iconRT;

        // ── 6. Kitap Paneli ──────────────────────────────────
        GameObject panelGO = new GameObject("BookPanel", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        panelGO.transform.SetParent(canvasGO.transform, false);

        RectTransform panelRT = panelGO.GetComponent<RectTransform>();
        panelRT.anchorMin = panelRT.anchorMax = new Vector2(0.5f, 0.5f);
        panelRT.sizeDelta = new Vector2(700f, 520f);
        panelRT.anchoredPosition = Vector2.zero;

        Image panelBg = panelGO.GetComponent<Image>();
        panelBg.color = new Color(0.02f, 0.02f, 0.05f, 0.97f);

        CanvasGroup panelCG = panelGO.GetComponent<CanvasGroup>();
        panelCG.alpha = 0f;

        UIStyle.AddOutline(panelGO);

        // Panel Canvas — her şeyin üstünde
        Canvas panelCanvas = panelGO.AddComponent<Canvas>();
        panelCanvas.overrideSorting = true;
        panelCanvas.sortingOrder = 50;
        panelGO.AddComponent<GraphicRaycaster>();

        ui.bookPanel = panelRT;
        ui.bookCanvasGroup = panelCG;

        // ── 7. Panel İçeriği ─────────────────────────────────

        // --- Üst Bar: Başlık + Kapat Butonu ---
        GameObject topBar = CreateHorizontalGroup(panelGO.transform, "TopBar", 680f, 45f);
        RectTransform topBarRT = topBar.GetComponent<RectTransform>();
        topBarRT.anchorMin = new Vector2(0.5f, 1f);
        topBarRT.anchorMax = new Vector2(0.5f, 1f);
        topBarRT.pivot = new Vector2(0.5f, 1f);
        topBarRT.anchoredPosition = new Vector2(0, -8f);

        // Başlık
        GameObject titleGO = CreateTMPText(topBar.transform, "TitleText", "HOW TO MOVE", font,
            UIStyle.FontSizeTitle, Color.white, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
        LayoutElement titleLE = titleGO.AddComponent<LayoutElement>();
        titleLE.flexibleWidth = 1f;
        titleLE.minHeight = 40f;
        ui.titleText = titleGO.GetComponent<TMP_Text>();

        // Kapat butonu
        GameObject closeBtnGO = CreateButton(topBar.transform, "CloseButton", 40f, 40f, font);
        TMP_Text closeTxt = closeBtnGO.GetComponentInChildren<TMP_Text>();
        if (closeTxt != null)
        {
            closeTxt.text = "X";
            closeTxt.fontSize = 22;
            closeTxt.color = Color.red;
            closeTxt.fontStyle = FontStyles.Bold;
            closeTxt.alignment = TextAlignmentOptions.Center;
        }
        Button closeBtn = closeBtnGO.GetComponent<Button>();
        closeBtn.colors = UIStyle.ButtonColors();
        LayoutElement closeLE = closeBtnGO.AddComponent<LayoutElement>();
        closeLE.preferredWidth = 40f;
        closeLE.preferredHeight = 40f;
        ui.closeButton = closeBtn;

        // --- Ayırıcı çizgi ---
        CreateDivider(panelGO.transform, "Divider1", 660f, new Vector2(0, -55f));

        // --- Kategori Sekmeleri ---
        GameObject catBar = CreateHorizontalGroup(panelGO.transform, "CategoryBar", 660f, 32f);
        RectTransform catBarRT = catBar.GetComponent<RectTransform>();
        catBarRT.anchorMin = new Vector2(0.5f, 1f);
        catBarRT.anchorMax = new Vector2(0.5f, 1f);
        catBarRT.pivot = new Vector2(0.5f, 1f);
        catBarRT.anchoredPosition = new Vector2(0, -62f);
        HorizontalLayoutGroup catHLG = catBar.GetComponent<HorizontalLayoutGroup>();
        catHLG.spacing = 4f;

        string[] catLabels = { "ALL", "COMBAT", "ENEMIES", "ITEMS", "PERKS", "MOVEMENT" };
        ui.categoryButtons = new List<Button>();
        ui.categoryButtonTexts = new List<TMP_Text>();

        for (int i = 0; i < catLabels.Length; i++)
        {
            GameObject catBtnGO = CreateButton(catBar.transform, $"CatBtn_{catLabels[i]}", 0, 30f, font);
            LayoutElement catLE = catBtnGO.AddComponent<LayoutElement>();
            catLE.flexibleWidth = 1f;
            catLE.preferredHeight = 30f;

            TMP_Text catTxt = catBtnGO.GetComponentInChildren<TMP_Text>();
            if (catTxt != null)
            {
                catTxt.text = catLabels[i];
                catTxt.fontSize = 12;
                catTxt.alignment = TextAlignmentOptions.Center;
                catTxt.color = i == 0 ? Color.white : new Color(0.6f, 0.6f, 0.6f);
            }

            Image catBtnImg = catBtnGO.GetComponent<Image>();
            if (catBtnImg != null)
                catBtnImg.color = i == 0 ? new Color(0f, 0.58f, 0.74f, 1f) : new Color(0.15f, 0.15f, 0.2f, 1f);

            Button catBtn = catBtnGO.GetComponent<Button>();
            var cb = UIStyle.ButtonColors();
            cb.normalColor = i == 0 ? new Color(0f, 0.58f, 0.74f, 1f) : new Color(0.15f, 0.15f, 0.2f, 1f);
            catBtn.colors = cb;

            ui.categoryButtons.Add(catBtn);
            ui.categoryButtonTexts.Add(catTxt);
        }

        // --- Kategori Label ---
        GameObject catLabelGO = CreateTMPText(panelGO.transform, "CategoryLabel", "MOVEMENT", font,
            12, new Color(0.5f, 0.5f, 0.5f), FontStyles.Italic, TextAlignmentOptions.TopRight);
        RectTransform catLabelRT = catLabelGO.GetComponent<RectTransform>();
        catLabelRT.anchorMin = new Vector2(0.5f, 1f);
        catLabelRT.anchorMax = new Vector2(0.5f, 1f);
        catLabelRT.pivot = new Vector2(0.5f, 1f);
        catLabelRT.sizeDelta = new Vector2(640f, 20f);
        catLabelRT.anchoredPosition = new Vector2(0, -100f);
        ui.categoryLabel = catLabelGO.GetComponent<TMP_Text>();

        // --- İllüstrasyon ---
        GameObject illusGO = new GameObject("Illustration", typeof(RectTransform), typeof(Image));
        illusGO.transform.SetParent(panelGO.transform, false);
        RectTransform illusRT = illusGO.GetComponent<RectTransform>();
        illusRT.anchorMin = new Vector2(0.5f, 1f);
        illusRT.anchorMax = new Vector2(0.5f, 1f);
        illusRT.pivot = new Vector2(0.5f, 1f);
        illusRT.sizeDelta = new Vector2(300f, 150f);
        illusRT.anchoredPosition = new Vector2(0, -125f);
        Image illusImg = illusGO.GetComponent<Image>();
        illusImg.preserveAspect = true;
        illusImg.color = Color.white;
        illusImg.raycastTarget = false;
        illusGO.SetActive(false);
        ui.illustrationImage = illusImg;

        // --- Gövde Metni ---
        GameObject bodyGO = CreateTMPText(panelGO.transform, "BodyText",
            "Page content goes here...", font, UIStyle.FontSizeMid,
            new Color(0.85f, 0.85f, 0.85f), FontStyles.Normal, TextAlignmentOptions.TopLeft);
        RectTransform bodyRT = bodyGO.GetComponent<RectTransform>();
        bodyRT.anchorMin = new Vector2(0.5f, 1f);
        bodyRT.anchorMax = new Vector2(0.5f, 1f);
        bodyRT.pivot = new Vector2(0.5f, 1f);
        bodyRT.sizeDelta = new Vector2(640f, 300f);
        bodyRT.anchoredPosition = new Vector2(0, -130f);
        TMP_Text bodyTmp = bodyGO.GetComponent<TMP_Text>();
        bodyTmp.enableWordWrapping = true;
        bodyTmp.overflowMode = TextOverflowModes.ScrollRect;
        ui.bodyText = bodyTmp;

        // --- Alt Bar: Prev + Sayfa No + Next ---
        GameObject bottomBar = CreateHorizontalGroup(panelGO.transform, "BottomBar", 400f, 40f);
        RectTransform bottomBarRT = bottomBar.GetComponent<RectTransform>();
        bottomBarRT.anchorMin = new Vector2(0.5f, 0f);
        bottomBarRT.anchorMax = new Vector2(0.5f, 0f);
        bottomBarRT.pivot = new Vector2(0.5f, 0f);
        bottomBarRT.anchoredPosition = new Vector2(0, 15f);
        HorizontalLayoutGroup bottomHLG = bottomBar.GetComponent<HorizontalLayoutGroup>();
        bottomHLG.spacing = 10f;
        bottomHLG.childAlignment = TextAnchor.MiddleCenter;

        // Prev butonu
        GameObject prevBtnGO = CreateButton(bottomBar.transform, "PrevButton", 0, 36f, font);
        LayoutElement prevLE = prevBtnGO.AddComponent<LayoutElement>();
        prevLE.preferredWidth = 80f;
        prevLE.preferredHeight = 36f;
        TMP_Text prevTxt = prevBtnGO.GetComponentInChildren<TMP_Text>();
        if (prevTxt != null)
        {
            prevTxt.text = "< PREV";
            prevTxt.fontSize = 16;
            prevTxt.alignment = TextAlignmentOptions.Center;
        }
        Button prevBtn = prevBtnGO.GetComponent<Button>();
        prevBtn.colors = UIStyle.ButtonColors();
        UIStyle.AddOutline(prevBtnGO);
        ui.prevButton = prevBtn;

        // Sayfa numarası
        GameObject pageNumGO = CreateTMPText(bottomBar.transform, "PageNumber", "1 / 5", font,
            UIStyle.FontSizeMid, Color.white, FontStyles.Normal, TextAlignmentOptions.Center);
        LayoutElement pageLE = pageNumGO.AddComponent<LayoutElement>();
        pageLE.preferredWidth = 100f;
        pageLE.preferredHeight = 36f;
        ui.pageNumberText = pageNumGO.GetComponent<TMP_Text>();

        // Next butonu
        GameObject nextBtnGO = CreateButton(bottomBar.transform, "NextButton", 0, 36f, font);
        LayoutElement nextLE = nextBtnGO.AddComponent<LayoutElement>();
        nextLE.preferredWidth = 80f;
        nextLE.preferredHeight = 36f;
        TMP_Text nextTxt = nextBtnGO.GetComponentInChildren<TMP_Text>();
        if (nextTxt != null)
        {
            nextTxt.text = "NEXT >";
            nextTxt.fontSize = 16;
            nextTxt.alignment = TextAlignmentOptions.Center;
        }
        Button nextBtn = nextBtnGO.GetComponent<Button>();
        nextBtn.colors = UIStyle.ButtonColors();
        UIStyle.AddOutline(nextBtnGO);
        ui.nextButton = nextBtn;

        // ── Seçim ve Son Ayarlar ─────────────────────────────
        panelGO.SetActive(false); // Kapalı başlasın

        Selection.activeGameObject = canvasGO;
        EditorUtility.SetDirty(canvasGO);

        Debug.Log("GuideBook UI sahneye kuruldu! Sol alttaki '?' butonuna tıklayarak kitabı açabilirsin.");
    }

    // ═══════════════════════════════════════════════════════
    //  VERİ OLUŞTURMA
    // ═══════════════════════════════════════════════════════
    private static GuideBookData CreateDataAsset()
    {
        // Zaten varsa yeniden oluşturma
        string assetPath = "Assets/Data/GuideBookData.asset";
        GuideBookData existing = AssetDatabase.LoadAssetAtPath<GuideBookData>(assetPath);
        if (existing != null)
        {
            Debug.Log("GuideBookData zaten mevcut: " + assetPath);
            return existing;
        }

        // Klasör yoksa oluştur
        if (!AssetDatabase.IsValidFolder("Assets/Data"))
            AssetDatabase.CreateFolder("Assets", "Data");

        GuideBookData data = ScriptableObject.CreateInstance<GuideBookData>();
        data.pages = new List<GuideBookPage>();

        // Sayfa 1 — Movement
        data.pages.Add(new GuideBookPage
        {
            title = "How to Move",
            category = "Movement",
            bodyText = "Each turn, you can move to any highlighted hex adjacent to you. Blue hexes show valid moves. Moving into an enemy attacks them — no separate attack button needed. You start with 1 move per turn. Some perks grant extra moves."
        });

        // Sayfa 2 — Combat
        data.pages.Add(new GuideBookPage
        {
            title = "Dice & Damage",
            category = "Combat",
            bodyText = "When you attack an enemy by moving into them, dice are rolled to determine damage. You start with 2 dice. More dice = more potential damage. Critical hits multiply your damage. Watch the dice panel — combos and crits are highlighted. Some perks modify dice rolls and damage."
        });

        // Sayfa 3 — Enemies
        data.pages.Add(new GuideBookPage
        {
            title = "Enemy Types",
            category = "Enemies",
            bodyText = "MELEE: Moves toward you and attacks on contact.\nTELEGRAPH AOE: Shows a warning tile (red hex) before unleashing an area attack. Move out of the highlighted zone!\nTOTEM: Stationary, buffs nearby enemies.\nWARLOCK: Ranged attacker with special abilities.\nBOSS: Powerful enemies with multiple mechanics — read their patterns carefully."
        });

        // Sayfa 4 — Items
        data.pages.Add(new GuideBookPage
        {
            title = "Items & Shop",
            category = "Items",
            bodyText = "After each room, a Shop appears. Spend gold to buy items. Items provide powerful one-time or persistent effects. You can reroll the shop for a cost — rerolling gets more expensive each time. Gold is earned by killing enemies. Some perks increase gold drops."
        });

        // Sayfa 5 — Perks
        data.pages.Add(new GuideBookPage
        {
            title = "Perks & Level Up",
            category = "Perks",
            bodyText = "After clearing a room, choose a perk. Perks are passive upgrades that persist for the run. Rarities: Common (grey) → Rare (blue) → Epic (purple) → Legendary (gold) → Secret. Many perks have multiple levels — you can pick the same perk again to upgrade it. Build synergies between perks for powerful combinations."
        });

        AssetDatabase.CreateAsset(data, assetPath);
        AssetDatabase.SaveAssets();
        Debug.Log("GuideBookData oluşturuldu: " + assetPath);

        return data;
    }

    // ═══════════════════════════════════════════════════════
    //  YARDIMCI FONKSİYONLAR
    // ═══════════════════════════════════════════════════════

    private static GameObject CreateButton(Transform parent, string name, float width, float height, TMP_FontAsset font)
    {
        GameObject btnGO = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        btnGO.transform.SetParent(parent, false);

        RectTransform rt = btnGO.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(width, height);

        Image img = btnGO.GetComponent<Image>();
        img.color = UIStyle.BgDark;

        // İçine TMP_Text koy
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

    private static GameObject CreateHorizontalGroup(Transform parent, string name, float width, float height)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(HorizontalLayoutGroup));
        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(width, height);

        HorizontalLayoutGroup hlg = go.GetComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;

        return go;
    }

    private static void CreateDivider(Transform parent, string name, float width, Vector2 position)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(width, 1.5f);
        rt.anchoredPosition = position;

        Image img = go.GetComponent<Image>();
        img.color = UIStyle.DividerColor;
        img.raycastTarget = false;
    }
}
