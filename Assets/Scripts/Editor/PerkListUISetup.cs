using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

public class PerkListUISetup : EditorWindow
{
    [MenuItem("Tools/Setup Perk List UI")]
    public static void Setup()
    {
        // 0. Eskisini sil (varsa)
        foreach (var old in Object.FindObjectsByType<PerkListUI>(FindObjectsSortMode.None))
            Undo.DestroyObjectImmediate(old.gameObject);

        // 1. MainUi Canvas'ı bul
        Canvas mainCanvas = null;
        foreach (var c in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            if (c.name == "MainUi") { mainCanvas = c; break; }
        }
        if (mainCanvas == null)
            mainCanvas = Object.FindFirstObjectByType<Canvas>();

        if (mainCanvas == null)
        {
            Debug.LogError("Sahnede Canvas bulunamadı!");
            return;
        }

        // Font asset'i yükle
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Star Crush SDF.asset");
        if (font == null)
        {
            Debug.LogWarning("Star Crush SDF font bulunamadı, varsayılan font kullanılacak.");
        }

        // 2. Ana buton objesi (sol üst köşe)
        GameObject buttonObj = new GameObject("PerkListButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(PerkListUI));
        buttonObj.transform.SetParent(mainCanvas.transform, false);

        RectTransform btnRect = buttonObj.GetComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(1, 1);
        btnRect.anchorMax = new Vector2(1, 1);
        btnRect.pivot = new Vector2(1, 1);
        btnRect.anchoredPosition = new Vector2(-15, -15);
        btnRect.sizeDelta = new Vector2(160, 45);

        Image btnImage = buttonObj.GetComponent<Image>();
        btnImage.color = new Color(0.12f, 0.12f, 0.18f, 0.85f);

        // 3. Buton metni
        GameObject btnTextObj = new GameObject("ButtonText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        btnTextObj.transform.SetParent(buttonObj.transform, false);

        RectTransform btnTextRect = btnTextObj.GetComponent<RectTransform>();
        btnTextRect.anchorMin = Vector2.zero;
        btnTextRect.anchorMax = Vector2.one;
        btnTextRect.offsetMin = Vector2.zero;
        btnTextRect.offsetMax = Vector2.zero;

        TextMeshProUGUI btnTMP = btnTextObj.GetComponent<TextMeshProUGUI>();
        btnTMP.text = "PERKS";
        btnTMP.fontSize = 24;
        btnTMP.alignment = TextAlignmentOptions.Center;
        btnTMP.color = new Color(1f, 0.84f, 0f, 1f); // Altın sarısı
        if (font != null) btnTMP.font = font;

        // 4. Perk listesi paneli (hover'da açılacak)
        GameObject panelObj = new GameObject("PerkListPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        panelObj.transform.SetParent(buttonObj.transform, false);

        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1, 1);
        panelRect.anchorMax = new Vector2(1, 1);
        panelRect.pivot = new Vector2(1, 1);
        panelRect.anchoredPosition = new Vector2(0, -50);
        panelRect.sizeDelta = new Vector2(320, 200);

        Image panelImage = panelObj.GetComponent<Image>();
        panelImage.color = new Color(0.08f, 0.08f, 0.14f, 0.92f);

        VerticalLayoutGroup vlg = panelObj.GetComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(14, 14, 10, 10);
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        ContentSizeFitter csf = panelObj.GetComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        // 5. Perk list text
        GameObject listTextObj = new GameObject("PerkListText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        listTextObj.transform.SetParent(panelObj.transform, false);

        TextMeshProUGUI listTMP = listTextObj.GetComponent<TextMeshProUGUI>();
        listTMP.text = "No perks yet.";
        listTMP.fontSize = 18;
        listTMP.alignment = TextAlignmentOptions.TopLeft;
        listTMP.color = Color.white;
        listTMP.richText = true;
        listTMP.enableWordWrapping = true;
        if (font != null) listTMP.font = font;

        // 6. PerkListUI bileşenini bağla
        PerkListUI perkListUI = buttonObj.GetComponent<PerkListUI>();
        perkListUI.perkListPanel = panelObj;
        perkListUI.perkListText = listTMP;
        perkListUI.buttonText = btnTMP;

        panelObj.SetActive(false);

        // Outline ekle
        Outline btnOutline = buttonObj.AddComponent<Outline>();
        btnOutline.effectColor = new Color(1f, 0.84f, 0f, 0.5f);
        btnOutline.effectDistance = new Vector2(1.5f, -1.5f);

        Outline panelOutline = panelObj.AddComponent<Outline>();
        panelOutline.effectColor = new Color(1f, 0.84f, 0f, 0.3f);
        panelOutline.effectDistance = new Vector2(1, -1);

        Selection.activeGameObject = buttonObj;
        Undo.RegisterCreatedObjectUndo(buttonObj, "Create PerkListUI");

        Debug.Log("✅ PerkListUI başarıyla oluşturuldu! Sol üst köşede 'PERKS' butonu eklendi.");
    }
}
