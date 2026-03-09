using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

public static class ShopSetupTool
{
    [MenuItem("Tools/Setup Shop UI")]
    public static void SetupShopUI()
    {
        // ── 1. Ana Canvas'i bul (MainUi veya ilk Canvas) ──────────────────
        Canvas mainCanvas = null;
        foreach (var c in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            if (c.name == "MainUi") { mainCanvas = c; break; }
        }
        if (mainCanvas == null)
            mainCanvas = Object.FindFirstObjectByType<Canvas>();

        if (mainCanvas == null)
        {
            Debug.LogError("Sahneде Canvas bulunamadi!");
            return;
        }

        // ── 2. ShopSlot prefab olustur ────────────────────────────────────
        string prefabPath = "Assets/Prefabs/ShopSlot.prefab";

        // Prefab icin gecici sahne objesi — RectTransform ile beraber olustur
        GameObject slotRoot = new GameObject("ShopSlot", typeof(RectTransform));
        slotRoot.layer = LayerMask.NameToLayer("UI");

        RectTransform slotRT = slotRoot.GetComponent<RectTransform>();
        slotRT.sizeDelta = new Vector2(65f, 65f);

        // Arkaplan image
        Image bg = slotRoot.AddComponent<Image>();
        bg.color = new Color(0.2f, 0.2f, 0.2f, 0.85f);

        // ShopSlot component
        ShopSlot shopSlot = slotRoot.AddComponent<ShopSlot>();

        // -- BuyButton
        GameObject btnGO = new GameObject("BuyButton");
        btnGO.transform.SetParent(slotRoot.transform, false);
        btnGO.layer = LayerMask.NameToLayer("UI");
        RectTransform btnRT = btnGO.AddComponent<RectTransform>();
        btnRT.anchorMin = Vector2.zero;
        btnRT.anchorMax = Vector2.one;
        btnRT.offsetMin = Vector2.zero;
        btnRT.offsetMax = Vector2.zero;
        Image btnImg = btnGO.AddComponent<Image>();
        btnImg.color = new Color(1f, 1f, 1f, 0f); // tamamen seffaf — arkaplan gorunsun
        Button btn = btnGO.AddComponent<Button>();
        shopSlot.buyButton = btn;

        // -- İsim text (hover'da gorunur)
        GameObject nameGO = new GameObject("Isim");
        nameGO.transform.SetParent(slotRoot.transform, false);
        nameGO.layer = LayerMask.NameToLayer("UI");
        RectTransform nameRT = nameGO.AddComponent<RectTransform>();
        nameRT.anchorMin = new Vector2(0f, 0.35f);
        nameRT.anchorMax = new Vector2(1f, 1f);
        nameRT.offsetMin = new Vector2(4f, 4f);
        nameRT.offsetMax = new Vector2(-4f, -4f);
        TMP_Text nameTxt = nameGO.AddComponent<TextMeshProUGUI>();
        nameTxt.text = "Item Adi";
        nameTxt.fontSize = 8;
        nameTxt.alignment = TextAlignmentOptions.Center;
        nameTxt.color = Color.white;

        // -- Fiyat text (hover'da gorunur)
        GameObject priceGO = new GameObject("Fiyat");
        priceGO.transform.SetParent(slotRoot.transform, false);
        priceGO.layer = LayerMask.NameToLayer("UI");
        RectTransform priceRT = priceGO.AddComponent<RectTransform>();
        priceRT.anchorMin = new Vector2(0f, 0f);
        priceRT.anchorMax = new Vector2(1f, 0.35f);
        priceRT.offsetMin = new Vector2(4f, 4f);
        priceRT.offsetMax = new Vector2(-4f, -4f);
        TMP_Text priceTxt = priceGO.AddComponent<TextMeshProUGUI>();
        priceTxt.text = "0 Coin";
        priceTxt.fontSize = 7;
        priceTxt.alignment = TextAlignmentOptions.Center;
        priceTxt.color = new Color(1f, 0.85f, 0.2f);

        // -- SoldOut overlay
        GameObject soldGO = new GameObject("SoldOut");
        soldGO.transform.SetParent(slotRoot.transform, false);
        soldGO.layer = LayerMask.NameToLayer("UI");
        RectTransform soldRT = soldGO.AddComponent<RectTransform>();
        soldRT.anchorMin = Vector2.zero;
        soldRT.anchorMax = Vector2.one;
        soldRT.offsetMin = Vector2.zero;
        soldRT.offsetMax = Vector2.zero;
        Image soldImg = soldGO.AddComponent<Image>();
        soldImg.color = new Color(0f, 0f, 0f, 0.65f);

        // Text icin ayri child GO (Image ve TMP_Text ayni GO'da olamaz)
        GameObject soldTextGO = new GameObject("SoldOutText", typeof(RectTransform));
        soldTextGO.transform.SetParent(soldGO.transform, false);
        soldTextGO.layer = LayerMask.NameToLayer("UI");
        RectTransform soldTextRT = soldTextGO.GetComponent<RectTransform>();
        soldTextRT.anchorMin = Vector2.zero;
        soldTextRT.anchorMax = Vector2.one;
        soldTextRT.offsetMin = Vector2.zero;
        soldTextRT.offsetMax = Vector2.zero;
        TMP_Text soldTxt = soldTextGO.AddComponent<TextMeshProUGUI>();
        soldTxt.text = "SATILDI";
        soldTxt.fontSize = 9;
        soldTxt.fontStyle = FontStyles.Bold;
        soldTxt.alignment = TextAlignmentOptions.Center;
        soldTxt.color = Color.red;
        shopSlot.soldOutOverlay = soldGO;

        // Prefab klasorunu garantile
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");

        // Eski prefab varsa sil
        AssetDatabase.DeleteAsset(prefabPath);

        // Prefab kaydet
        bool success;
        PrefabUtility.SaveAsPrefabAsset(slotRoot, prefabPath, out success);
        Object.DestroyImmediate(slotRoot);

        if (!success)
        {
            Debug.LogError("ShopSlot prefab kaydedilemedi: " + prefabPath);
            return;
        }
        GameObject slotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        Debug.Log("ShopSlot prefab olusturuldu: " + prefabPath);

        // ── 3. ShopPanel — eski varsa sil, yenisini yap ──────────────────
        Transform existingPanel = mainCanvas.transform.Find("ShopPanel");
        if (existingPanel != null)
            Object.DestroyImmediate(existingPanel.gameObject);

        GameObject panelGO = new GameObject("ShopPanel");
        panelGO.transform.SetParent(mainCanvas.transform, false);
        panelGO.layer = LayerMask.NameToLayer("UI");

        RectTransform panelRT = panelGO.AddComponent<RectTransform>();
        // Sol-ust koseye sabitle
        panelRT.anchorMin = new Vector2(0f, 1f);
        panelRT.anchorMax = new Vector2(0f, 1f);
        panelRT.pivot     = new Vector2(0f, 1f);
        panelRT.anchoredPosition = new Vector2(10f, -10f);
        panelRT.sizeDelta = new Vector2(225f, 75f);

        HorizontalLayoutGroup hlg = panelGO.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 4;
        hlg.padding = new RectOffset(4, 4, 4, 4);
        hlg.childAlignment         = TextAnchor.UpperLeft;
        hlg.childControlWidth      = true;
        hlg.childControlHeight     = true;
        hlg.childForceExpandWidth  = true;
        hlg.childForceExpandHeight = true;

        Debug.Log("ShopPanel olusturuldu: MainUi altinda");

        // ── 3b. Reroll Button — ShopPanel'in altinda ──────────────────
        Transform existingReroll = mainCanvas.transform.Find("RerollButton");
        if (existingReroll != null)
            Object.DestroyImmediate(existingReroll.gameObject);

        GameObject rerollGO = new GameObject("RerollButton", typeof(RectTransform));
        rerollGO.transform.SetParent(mainCanvas.transform, false);
        rerollGO.layer = LayerMask.NameToLayer("UI");

        RectTransform rerollRT = rerollGO.GetComponent<RectTransform>();
        rerollRT.anchorMin        = new Vector2(0f, 1f);
        rerollRT.anchorMax        = new Vector2(0f, 1f);
        rerollRT.pivot            = new Vector2(0f, 1f);
        rerollRT.anchoredPosition = new Vector2(10f, -90f);
        rerollRT.sizeDelta        = new Vector2(100f, 28f);

        Image rerollBg = rerollGO.AddComponent<Image>();
        rerollBg.color = Color.white; // ColorBlock kontrol edecek
        Button rerollBtn = rerollGO.AddComponent<Button>();
        rerollBtn.targetGraphic = rerollBg;
        ColorBlock rerollCB = rerollBtn.colors;
        rerollCB.normalColor      = new Color32(0x00, 0x05, 0x0C, 0xFF); // #00050C
        rerollCB.highlightedColor = new Color32(0x00, 0x08, 0x41, 0xFF); // #000841
        rerollCB.pressedColor     = new Color32(0x00, 0x93, 0xBC, 0xFF); // #0093BC
        rerollCB.selectedColor    = new Color32(0x00, 0x05, 0x0C, 0xFF);
        rerollCB.disabledColor    = new Color32(0x00, 0x05, 0x0C, 0xFF);
        rerollCB.colorMultiplier  = 1f;
        rerollBtn.colors = rerollCB;

        // Reroll fiyat texti — butonun icinde
        GameObject rerollTxtGO = new GameObject("RerollPriceText", typeof(RectTransform));
        rerollTxtGO.transform.SetParent(rerollGO.transform, false);
        rerollTxtGO.layer = LayerMask.NameToLayer("UI");
        RectTransform rpRT = rerollTxtGO.GetComponent<RectTransform>();
        rpRT.anchorMin = Vector2.zero; rpRT.anchorMax = Vector2.one;
        rpRT.offsetMin = new Vector2(4f, 2f); rpRT.offsetMax = new Vector2(-4f, -2f);
        TMP_Text rerollPriceTxt = rerollTxtGO.AddComponent<TextMeshProUGUI>();
        rerollPriceTxt.text      = "Reroll: 2 Coin";
        rerollPriceTxt.fontSize  = 10;
        rerollPriceTxt.alignment = TextAlignmentOptions.Center;
        rerollPriceTxt.color     = Color.white;
        rerollPriceTxt.raycastTarget = false;

        Debug.Log("Reroll butonu olusturuldu.");

        // ── 4. ShopManager — eski varsa sil, yenisini yap ─────────────────
        Shopmanager existingMgr = Object.FindFirstObjectByType<Shopmanager>();
        if (existingMgr != null)
            Object.DestroyImmediate(existingMgr.gameObject);

        GameObject mgrGO = new GameObject("ShopManager");
        Shopmanager mgr = mgrGO.AddComponent<Shopmanager>();
        mgr.shopSlotContainer = panelRT;
        mgr.shopSlotPrefab    = slotPrefab;
        mgr.shopSlotCount     = 3;
        mgr.rerollButton      = rerollBtn;
        mgr.rerollPriceText   = rerollPriceTxt;
        mgr.rerollBaseCost    = 2f;
        mgr.rerollMultiplier  = 1.2f;

        // CoinText'i bul ve ata
        TMP_Text coinText = FindInScene<TMP_Text>("CoinText");
        if (coinText != null) mgr.coinDisplayText = coinText;

        EditorUtility.SetDirty(mgrGO);
        EditorUtility.SetDirty(panelGO);
        EditorUtility.SetDirty(rerollGO);
        Debug.Log("ShopManager olusturuldu ve baglandı.");

        // Sahneyi kaydet
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("=== Shop kurulumu tamamlandi! ===");
        Selection.activeGameObject = panelGO;
    }

    private static T FindInScene<T>(string objName) where T : Component
    {
        foreach (var obj in Object.FindObjectsByType<T>(FindObjectsSortMode.None))
            if (obj.name == objName) return obj;
        return null;
    }
}
