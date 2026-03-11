using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;

public class PerkListUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Referanslar")]
    public GameObject perkListPanel;
    public TMP_Text perkListText; // fallback: ikon yoksa eski sistem
    public TMP_Text buttonText;

    private CanvasGroup panelCanvasGroup;
    private Coroutine fadeCoroutine;
    private const float fadeDuration = 0.15f;
    private const float iconSize = 36f;
    private const float rowSpacing = 6f;

    private readonly List<GameObject> spawnedRows = new List<GameObject>();

    void Start()
    {
        // Kendi Canvas'ı yoksa ekle — LevelUpCanvas üstünde kalsın
        Canvas ownCanvas = GetComponent<Canvas>();
        if (ownCanvas == null)
        {
            ownCanvas = gameObject.AddComponent<Canvas>();
            ownCanvas.overrideSorting = true;
            ownCanvas.sortingOrder = 20;
            gameObject.AddComponent<GraphicRaycaster>();
        }

        if (perkListPanel != null)
        {
            panelCanvasGroup = perkListPanel.GetComponent<CanvasGroup>();
            if (panelCanvasGroup == null)
                panelCanvasGroup = perkListPanel.AddComponent<CanvasGroup>();

            panelCanvasGroup.alpha = 0f;
            perkListPanel.SetActive(false);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        RefreshPerkList();
        if (perkListPanel != null)
        {
            perkListPanel.SetActive(true);
            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(FadePanel(0f, 1f));
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (perkListPanel != null)
        {
            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(FadePanel(panelCanvasGroup.alpha, 0f, true));
        }
    }

    private System.Collections.IEnumerator FadePanel(float from, float to, bool deactivateOnDone = false)
    {
        float elapsed = 0f;
        panelCanvasGroup.alpha = from;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            panelCanvasGroup.alpha = Mathf.Lerp(from, to, elapsed / fadeDuration);
            yield return null;
        }
        panelCanvasGroup.alpha = to;
        if (deactivateOnDone) perkListPanel.SetActive(false);
    }

    private void RefreshPerkList()
    {
        if (perkListPanel == null || RunManager.instance == null) return;

        // Eski satırları temizle
        foreach (var row in spawnedRows) Destroy(row);
        spawnedRows.Clear();

        var perks = RunManager.instance.activePerks;

        // Eski text'i gizle (artık dinamik satırlar kullanıyoruz)
        if (perkListText != null) perkListText.gameObject.SetActive(false);

        if (perks.Count == 0)
        {
            if (perkListText != null)
            {
                perkListText.gameObject.SetActive(true);
                perkListText.text = "No perks yet.";
            }
            return;
        }

        TMP_FontAsset font = perkListText != null ? perkListText.font : null;

        for (int i = 0; i < perks.Count; i++)
        {
            BasePerk p = perks[i];
            GameObject row = CreatePerkRow(p, font);
            row.transform.SetParent(perkListPanel.transform, false);
            spawnedRows.Add(row);
        }
    }

    private GameObject CreatePerkRow(BasePerk perk, TMP_FontAsset font)
    {
        GameObject row = new GameObject("PerkRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        HorizontalLayoutGroup hlg = row.GetComponent<HorizontalLayoutGroup>();
        hlg.spacing = 8f;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        LayoutElement rowLE = row.AddComponent<LayoutElement>();
        rowLE.preferredHeight = iconSize + rowSpacing;

        // İkon
        GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        iconObj.transform.SetParent(row.transform, false);
        iconObj.GetComponent<RectTransform>().sizeDelta = new Vector2(iconSize, iconSize);
        LayoutElement iconLE = iconObj.AddComponent<LayoutElement>();
        iconLE.minWidth = iconSize; iconLE.preferredWidth = iconSize;
        iconLE.minHeight = iconSize; iconLE.preferredHeight = iconSize;
        Image iconImg = iconObj.GetComponent<Image>();
        if (perk.icon != null) { iconImg.sprite = perk.icon; iconImg.color = perk.isDisabled ? new Color(1,1,1,0.3f) : Color.white; }
        else iconImg.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);

        // Metin
        GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(row.transform, false);
        textObj.GetComponent<RectTransform>().sizeDelta = new Vector2(200f, iconSize);
        LayoutElement textLE = textObj.AddComponent<LayoutElement>();
        textLE.preferredWidth = 200f; textLE.preferredHeight = iconSize;
        TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
        string nameColor = perk.isDisabled ? "#666666" : "#FFFFFF";
        tmp.text = $"<color={nameColor}>{perk.perkName}</color>  <color=#AAAAAA>Lv {perk.currentLevel}</color>";
        tmp.fontSize = 16;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.color = Color.white;
        tmp.richText = true;
        tmp.raycastTarget = false;
        if (font != null) tmp.font = font;

        // ON/OFF toggle butonu
        GameObject btnObj = new GameObject("ToggleBtn", typeof(RectTransform), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(row.transform, false);
        btnObj.GetComponent<RectTransform>().sizeDelta = new Vector2(48f, 22f);
        LayoutElement btnLE = btnObj.AddComponent<LayoutElement>();
        btnLE.preferredWidth = 48f; btnLE.preferredHeight = 22f;
        Image btnImg = btnObj.GetComponent<Image>();
        btnImg.color = perk.isDisabled ? new Color(0.5f, 0.15f, 0.15f) : new Color(0.15f, 0.45f, 0.15f);

        GameObject lblObj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        lblObj.transform.SetParent(btnObj.transform, false);
        RectTransform lblRT = lblObj.GetComponent<RectTransform>();
        lblRT.anchorMin = Vector2.zero; lblRT.anchorMax = Vector2.one;
        lblRT.offsetMin = lblRT.offsetMax = Vector2.zero;
        TextMeshProUGUI btnLabel = lblObj.GetComponent<TextMeshProUGUI>();
        btnLabel.text = perk.isDisabled ? "OFF" : "ON";
        btnLabel.fontSize = 12;
        btnLabel.alignment = TextAlignmentOptions.Center;
        btnLabel.color = Color.white;
        if (font != null) btnLabel.font = font;

        btnObj.GetComponent<Button>().onClick.AddListener(() =>
        {
            perk.SetDisabled(!perk.isDisabled);
            btnLabel.text = perk.isDisabled ? "OFF" : "ON";
            btnImg.color = perk.isDisabled ? new Color(0.5f, 0.15f, 0.15f) : new Color(0.15f, 0.45f, 0.15f);
            if (perk.icon != null) iconImg.color = perk.isDisabled ? new Color(1,1,1,0.3f) : Color.white;
            string nc = perk.isDisabled ? "#666666" : "#FFFFFF";
            tmp.text = $"<color={nc}>{perk.perkName}</color>  <color=#AAAAAA>Lv {perk.currentLevel}</color>";
        });

        return row;
    }
}
