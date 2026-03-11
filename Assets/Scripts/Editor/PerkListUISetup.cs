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

        TMP_FontAsset font = UIStyle.LoadFont();
        if (font == null) Debug.LogWarning("Star Crush SDF font bulunamadı, varsayılan font kullanılacak.");

        // 2. Ana buton objesi (sol üst köşe)
        GameObject buttonObj = new GameObject("PerkListButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(PerkListUI));
        buttonObj.transform.SetParent(mainCanvas.transform, false);

        // Kendi Canvas'ı olsun ki LevelUpCanvas (sorting 15) üstünde kalsın
        Canvas btnCanvas = buttonObj.AddComponent<Canvas>();
        btnCanvas.overrideSorting = true;
        btnCanvas.sortingOrder = 20;
        buttonObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        RectTransform btnRect = buttonObj.GetComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(1, 1);
        btnRect.anchorMax = new Vector2(1, 1);
        btnRect.pivot = new Vector2(1, 1);
        btnRect.anchoredPosition = new Vector2(-15, -15);
        btnRect.sizeDelta = new Vector2(UIStyle.PerkBtnWidth, UIStyle.PerkBtnHeight);

        Image btnImage = buttonObj.GetComponent<Image>();
        btnImage.color = UIStyle.BgDark;

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
        btnTMP.fontSize = UIStyle.PerkBtnFontSize;
        btnTMP.alignment = TextAlignmentOptions.Center;
        btnTMP.color = UIStyle.TextWhite;
        if (font != null) btnTMP.font = font;

        // 4. Perk listesi paneli (hover'da açılacak)
        GameObject panelObj = new GameObject("PerkListPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        panelObj.transform.SetParent(buttonObj.transform, false);

        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1, 1);
        panelRect.anchorMax = new Vector2(1, 1);
        panelRect.pivot = new Vector2(1, 1);
        panelRect.anchoredPosition = new Vector2(0, -50);
        panelRect.sizeDelta = new Vector2(UIStyle.PerkPanelWidth, 200);

        Image panelImage = panelObj.GetComponent<Image>();
        panelImage.color = UIStyle.BgPanel;

        VerticalLayoutGroup vlg = panelObj.GetComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(10, 10, 10, 10);
        vlg.spacing = 6f;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
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
        listTMP.fontSize = UIStyle.PerkListFontSize;
        listTMP.alignment = TextAlignmentOptions.TopLeft;
        listTMP.color = UIStyle.TextWhite;
        listTMP.richText = true;
        listTMP.enableWordWrapping = true;
        if (font != null) listTMP.font = font;

        // 6. PerkListUI bileşenini bağla
        PerkListUI perkListUI = buttonObj.GetComponent<PerkListUI>();
        perkListUI.perkListPanel = panelObj;
        perkListUI.perkListText = listTMP;
        perkListUI.buttonText = btnTMP;

        panelObj.SetActive(false);

        UIStyle.AddOutline(buttonObj);
        UIStyle.AddOutline(panelObj);

        Selection.activeGameObject = buttonObj;
        Undo.RegisterCreatedObjectUndo(buttonObj, "Create PerkListUI");

        Debug.Log("✅ PerkListUI başarıyla oluşturuldu! Sol üst köşede 'PERKS' butonu eklendi.");
    }
}
