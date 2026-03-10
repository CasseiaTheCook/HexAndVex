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
            elapsed += Time.deltaTime;
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
        // Row: HorizontalLayoutGroup
        GameObject row = new GameObject("PerkRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        HorizontalLayoutGroup hlg = row.GetComponent<HorizontalLayoutGroup>();
        hlg.spacing = 8f;
        hlg.childAlignment = TextAnchor.UpperLeft;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;

        // İkon
        GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        iconObj.transform.SetParent(row.transform, false);
        RectTransform iconRT = iconObj.GetComponent<RectTransform>();
        iconRT.sizeDelta = new Vector2(iconSize, iconSize);
        Image iconImg = iconObj.GetComponent<Image>();
        if (perk.icon != null)
        {
            iconImg.sprite = perk.icon;
            iconImg.color = Color.white;
        }
        else
        {
            // Placeholder: koyu kare
            iconImg.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);
        }

        // Metin
        GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(row.transform, false);
        RectTransform textRT = textObj.GetComponent<RectTransform>();
        textRT.sizeDelta = new Vector2(260f, 0f);
        ContentSizeFitter csf = textObj.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
        tmp.text = $"<color=#FFFFFF>{perk.perkName}</color>  <color=#AAAAAA>Lv {perk.currentLevel}</color>\n<size=70%>{perk.description}</size>";
        tmp.fontSize = 18;
        tmp.alignment = TextAlignmentOptions.TopLeft;
        tmp.color = Color.white;
        tmp.richText = true;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.raycastTarget = false;
        if (font != null) tmp.font = font;

        return row;
    }
}
