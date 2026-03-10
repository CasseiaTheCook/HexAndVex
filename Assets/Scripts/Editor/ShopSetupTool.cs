using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

public static class ShopSetupTool
{
    [MenuItem("Tools/Setup Shop")]
    public static void SetupShop()
    {
        CreateItems();
        SetupShopUI();
        Debug.Log("=== Shop kurulumu tamamlandi! ===");
    }

    // ── Item asset'lerini oluştur ──────────────────────────────────────
    static void CreateItems()
    {
        string folder = "Assets/ScriptableObjects/Items";
        if (!AssetDatabase.IsValidFolder("Assets/ScriptableObjects"))
            AssetDatabase.CreateFolder("Assets", "ScriptableObjects");
        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder("Assets/ScriptableObjects", "Items");

        CreateItem<BioVial>(folder, "BioVial", "Bio-Vial", "Restore 1 HP", 3);
        CreateItem<MutaGen>(folder, "MutaGen", "Muta-Gen", "Restore 2 HP", 5);
        CreateItem<NecroShot>(folder, "NecroShot", "Necro-Shot", "Instantly kill any enemy on the map", 10);
        CreateItem<SynthStim>(folder, "SynthStim", "Synth-Stim", "Roll +1 extra die in the next combat", 6);
        CreateItem<GoldLeech>(folder, "GoldLeech", "Gold-Leech", "Next enemy drops 2x gold", 4);
        CreateItem<OverClok>(folder, "OverClok", "Over-Clok", "Deal 2x total dice damage in the next combat", 7);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Item asset'leri hazir.");
    }

    static void CreateItem<T>(string folder, string fileName, string name, string desc, int price) where T : BaseItem
    {
        string path = $"{folder}/{fileName}.asset";
        if (AssetDatabase.LoadAssetAtPath<T>(path) != null) return;
        T item = ScriptableObject.CreateInstance<T>();
        item.itemName = name;
        item.description = desc;
        item.price = price;
        AssetDatabase.CreateAsset(item, path);
    }

    // ── Shop UI'ını kur ────────────────────────────────────────────────
    static void SetupShopUI()
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
        soldTxt.text = "SOLD OUT";
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
        // Eski ayrık RerollButton varsa sil
        Transform existingReroll = mainCanvas.transform.Find("RerollButton");
        if (existingReroll != null)
            Object.DestroyImmediate(existingReroll.gameObject);

        // Ana konteyner — VerticalLayoutGroup
        GameObject panelGO = new GameObject("ShopPanel");
        panelGO.transform.SetParent(mainCanvas.transform, false);
        panelGO.layer = LayerMask.NameToLayer("UI");

        RectTransform panelRT = panelGO.AddComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0f, 1f);
        panelRT.anchorMax = new Vector2(0f, 1f);
        panelRT.pivot     = new Vector2(0f, 1f);
        panelRT.anchoredPosition = new Vector2(10f, -10f);
        panelRT.sizeDelta = new Vector2(280f, 115f);

        VerticalLayoutGroup mainVlg = panelGO.AddComponent<VerticalLayoutGroup>();
        mainVlg.spacing = 4;
        mainVlg.padding = new RectOffset(0, 0, 0, 0);
        mainVlg.childAlignment         = TextAnchor.UpperCenter;
        mainVlg.childControlWidth      = true;
        mainVlg.childControlHeight     = false;
        mainVlg.childForceExpandWidth  = true;
        mainVlg.childForceExpandHeight = false;

        // ── 3a. Üst satır: Slot container (HorizontalLayoutGroup) ──
        GameObject slotRowGO = new GameObject("SlotRow");
        slotRowGO.transform.SetParent(panelGO.transform, false);
        slotRowGO.layer = LayerMask.NameToLayer("UI");
        RectTransform slotRowRT = slotRowGO.AddComponent<RectTransform>();
        slotRowRT.sizeDelta = new Vector2(0f, 65f);
        LayoutElement slotRowLE = slotRowGO.AddComponent<LayoutElement>();
        slotRowLE.preferredHeight = 65f;

        HorizontalLayoutGroup slotHlg = slotRowGO.AddComponent<HorizontalLayoutGroup>();
        slotHlg.spacing = 8;
        slotHlg.padding = new RectOffset(0, 0, 0, 0);
        slotHlg.childAlignment         = TextAnchor.MiddleLeft;
        slotHlg.childControlWidth      = false;
        slotHlg.childControlHeight     = false;
        slotHlg.childForceExpandWidth  = false;
        slotHlg.childForceExpandHeight = false;

        Debug.Log("ShopPanel olusturuldu: MainUi altinda");

        // ── 3b. Alt satır: Reroll (sol %50) + Coin (sag %50) ─────────
        GameObject bottomRowGO = new GameObject("BottomRow");
        bottomRowGO.transform.SetParent(panelGO.transform, false);
        bottomRowGO.layer = LayerMask.NameToLayer("UI");
        RectTransform bottomRowRT = bottomRowGO.AddComponent<RectTransform>();
        bottomRowRT.sizeDelta = new Vector2(0f, 36f);
        LayoutElement bottomRowLE = bottomRowGO.AddComponent<LayoutElement>();
        bottomRowLE.preferredHeight = 36f;

        HorizontalLayoutGroup bottomHlg = bottomRowGO.AddComponent<HorizontalLayoutGroup>();
        bottomHlg.spacing = 12;
        bottomHlg.padding = new RectOffset(0, 0, 0, 0);
        bottomHlg.childAlignment         = TextAnchor.MiddleCenter;
        bottomHlg.childControlWidth      = true;
        bottomHlg.childControlHeight     = true;
        bottomHlg.childForceExpandWidth  = true;
        bottomHlg.childForceExpandHeight = true;

        // ── Reroll butonu (sol %50) ──
        GameObject rerollGO = new GameObject("RerollButton", typeof(RectTransform));
        rerollGO.transform.SetParent(bottomRowGO.transform, false);
        rerollGO.layer = LayerMask.NameToLayer("UI");

        LayoutElement rerollLE = rerollGO.AddComponent<LayoutElement>();
        rerollLE.flexibleWidth = 1.5f;

        Image rerollBg = rerollGO.AddComponent<Image>();
        rerollBg.color = Color.white;
        Button rerollBtn = rerollGO.AddComponent<Button>();
        rerollBtn.targetGraphic = rerollBg;
        ColorBlock rerollCB = rerollBtn.colors;
        rerollCB.normalColor      = new Color32(0x00, 0x05, 0x0C, 0xFF);
        rerollCB.highlightedColor = new Color32(0x00, 0x08, 0x41, 0xFF);
        rerollCB.pressedColor     = new Color32(0x00, 0x93, 0xBC, 0xFF);
        rerollCB.selectedColor    = new Color32(0x00, 0x05, 0x0C, 0xFF);
        rerollCB.disabledColor    = new Color32(0x00, 0x05, 0x0C, 0xFF);
        rerollCB.colorMultiplier  = 1f;
        rerollBtn.colors = rerollCB;

        GameObject rerollTxtGO = new GameObject("RerollPriceText", typeof(RectTransform));
        rerollTxtGO.transform.SetParent(rerollGO.transform, false);
        rerollTxtGO.layer = LayerMask.NameToLayer("UI");
        RectTransform rpRT = rerollTxtGO.GetComponent<RectTransform>();
        rpRT.anchorMin = Vector2.zero; rpRT.anchorMax = Vector2.one;
        rpRT.offsetMin = new Vector2(4f, 2f); rpRT.offsetMax = new Vector2(-24f, -2f);
        TMP_Text rerollPriceTxt = rerollTxtGO.AddComponent<TextMeshProUGUI>();
        rerollPriceTxt.text      = "Reroll: 2";
        rerollPriceTxt.fontSize  = 14;
        rerollPriceTxt.alignment = TextAlignmentOptions.Center;
        rerollPriceTxt.color     = Color.white;
        rerollPriceTxt.raycastTarget = false;

        Debug.Log("Reroll butonu olusturuldu.");

        // ── Coin alanı (sag %50) ──
        GameObject coinAreaGO = new GameObject("CoinArea", typeof(RectTransform));
        coinAreaGO.transform.SetParent(bottomRowGO.transform, false);
        coinAreaGO.layer = LayerMask.NameToLayer("UI");

        LayoutElement coinAreaLE = coinAreaGO.AddComponent<LayoutElement>();
        coinAreaLE.flexibleWidth = 0.5f;

        Image coinAreaBg = coinAreaGO.AddComponent<Image>();
        coinAreaBg.color = new Color32(0x00, 0x05, 0x0C, 0xFF);
        coinAreaBg.raycastTarget = false;

        HorizontalLayoutGroup coinHlg = coinAreaGO.AddComponent<HorizontalLayoutGroup>();
        coinHlg.spacing = 4f;
        coinHlg.padding = new RectOffset(6, 6, 2, 2);
        coinHlg.childAlignment = TextAnchor.MiddleLeft;
        coinHlg.childControlWidth = false;
        coinHlg.childControlHeight = false;
        coinHlg.childForceExpandWidth = false;
        coinHlg.childForceExpandHeight = false;

        // CoinText — Shopmanager bunu referans alacak
        GameObject coinTxtGO = new GameObject("CoinText", typeof(RectTransform));
        coinTxtGO.transform.SetParent(coinAreaGO.transform, false);
        coinTxtGO.layer = LayerMask.NameToLayer("UI");
        RectTransform coinTxtRT = coinTxtGO.GetComponent<RectTransform>();
        coinTxtRT.sizeDelta = new Vector2(60f, 26f);
        TMP_Text coinDisplayTxt = coinTxtGO.AddComponent<TextMeshProUGUI>();
        coinDisplayTxt.text = "0";
        coinDisplayTxt.fontSize = 16;
        coinDisplayTxt.alignment = TextAlignmentOptions.Left;
        coinDisplayTxt.color = new Color(1f, 0.85f, 0.2f, 1f);
        coinDisplayTxt.fontStyle = FontStyles.Bold;
        coinDisplayTxt.raycastTarget = false;

        Debug.Log("Coin alani olusturuldu.");

        // ── 4. ShopManager — eski varsa sil, yenisini yap ─────────────────
        Shopmanager existingMgr = Object.FindFirstObjectByType<Shopmanager>();
        if (existingMgr != null)
            Object.DestroyImmediate(existingMgr.gameObject);

        GameObject mgrGO = new GameObject("ShopManager");
        Shopmanager mgr = mgrGO.AddComponent<Shopmanager>();
        mgr.shopSlotContainer = slotRowRT;
        mgr.shopSlotPrefab    = slotPrefab;
        mgr.shopSlotCount     = 3;
        mgr.rerollButton      = rerollBtn;
        mgr.rerollPriceText   = rerollPriceTxt;
        mgr.coinDisplayText   = coinDisplayTxt;
        mgr.rerollBaseCost    = 2f;
        mgr.rerollMultiplier  = 1.2f;

        // Item pool'u otomatik doldur
        mgr.itemPool.Clear();
        string[] guids = AssetDatabase.FindAssets("t:BaseItem", new[] { "Assets/ScriptableObjects/Items" });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            BaseItem item = AssetDatabase.LoadAssetAtPath<BaseItem>(path);
            if (item != null) mgr.itemPool.Add(item);
        }

        EditorUtility.SetDirty(mgrGO);
        EditorUtility.SetDirty(panelGO);
        EditorUtility.SetDirty(rerollGO);
        Debug.Log($"ShopManager olusturuldu — {mgr.itemPool.Count} item atandı.");

        // Sahneyi kaydet
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Selection.activeGameObject = panelGO;
    }

    private static T FindInScene<T>(string objName) where T : Component
    {
        foreach (var obj in Object.FindObjectsByType<T>(FindObjectsSortMode.None))
            if (obj.name == objName) return obj;
        return null;
    }
}
