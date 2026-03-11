using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;

public class PerkListUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public static PerkListUI instance;

    [Header("Referanslar")]
    public GameObject perkListPanel;
    public TMP_Text perkListText; // fallback: ikon yoksa eski sistem
    public TMP_Text buttonText;

    private CanvasGroup panelCanvasGroup;
    private Coroutine fadeCoroutine;
    private const float fadeDuration = 0.15f;
    private const float iconSize = 24f;
    private const float rowSpacing = 2f;

    private readonly List<GameObject> spawnedRows = new List<GameObject>();
    private readonly List<BasePerk> spawnedRowPerks = new List<BasePerk>();

    void Awake()
    {
        instance = this;
    }

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

    public void ForceOpen()
    {
        RefreshPerkList();
        if (perkListPanel != null)
        {
            perkListPanel.SetActive(true);
            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(FadePanel(panelCanvasGroup.alpha, 1f));
        }
    }

    public void ForceClose()
    {
        if (perkListPanel != null)
        {
            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(FadePanel(panelCanvasGroup.alpha, 0f, true));
        }
    }

    public void TriggerShakeForPerk(BasePerk perk)
    {
        if (perkListPanel == null || !perkListPanel.activeSelf) return;
        int idx = spawnedRowPerks.IndexOf(perk);
        if (idx >= 0 && idx < spawnedRows.Count)
            StartCoroutine(ShakeRow(spawnedRows[idx]));
    }

    private System.Collections.IEnumerator ShakeRow(GameObject row)
    {
        if (row == null) yield break;
        RectTransform rt = row.GetComponent<RectTransform>();
        if (rt == null) yield break;

        Vector3 origin = rt.localPosition;
        float duration = 0.4f;
        float elapsed = 0f;
        float magnitude = 5f;
        float frequency = 30f;

        while (elapsed < duration)
        {
            float x = Mathf.Sin(elapsed * frequency) * magnitude * (1f - elapsed / duration);
            rt.localPosition = origin + new Vector3(x, 0f, 0f);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        rt.localPosition = origin;
    }

    private void RefreshPerkList()
    {
        if (perkListPanel == null || RunManager.instance == null) return;

        // Eski satırları temizle
        foreach (var row in spawnedRows) Destroy(row);
        spawnedRows.Clear();
        spawnedRowPerks.Clear();

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
            spawnedRowPerks.Add(p);
        }
    }

    private GameObject CreatePerkRow(BasePerk perk, TMP_FontAsset font)
    {
        // Dış kapsayıcı — dikey: üst satır + açıklama
        GameObject row = new GameObject("PerkRow", typeof(RectTransform));
        var rowVL = row.AddComponent<VerticalLayoutGroup>();
        rowVL.childAlignment = TextAnchor.UpperLeft;
        rowVL.spacing = 0f;
        rowVL.childForceExpandWidth = true;
        rowVL.childForceExpandHeight = false;
        rowVL.padding = new RectOffset(2, 2, 1, 1);
        var rowLE = row.AddComponent<LayoutElement>();
        rowLE.flexibleWidth = 1;
        var rowCSF = row.AddComponent<ContentSizeFitter>();
        rowCSF.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        rowCSF.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        // ── Üst satır: ikon + isim + seviye + toggle ──
        GameObject topRow = new GameObject("TopRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        topRow.transform.SetParent(row.transform, false);
        HorizontalLayoutGroup hlg = topRow.GetComponent<HorizontalLayoutGroup>();
        hlg.spacing = 6f;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        var topLE = topRow.AddComponent<LayoutElement>();
        topLE.preferredHeight = iconSize;
        topLE.flexibleWidth = 1;

        // İkon
        GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        iconObj.transform.SetParent(topRow.transform, false);
        iconObj.GetComponent<RectTransform>().sizeDelta = new Vector2(iconSize, iconSize);
        LayoutElement iconLE = iconObj.AddComponent<LayoutElement>();
        iconLE.minWidth = iconSize; iconLE.preferredWidth = iconSize;
        iconLE.minHeight = iconSize; iconLE.preferredHeight = iconSize;
        Image iconImg = iconObj.GetComponent<Image>();
        if (perk.icon != null) { iconImg.sprite = perk.icon; iconImg.color = perk.isDisabled ? new Color(1,1,1,0.3f) : Color.white; }
        else iconImg.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);

        // Perk adı + seviye
        GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(topRow.transform, false);
        textObj.GetComponent<RectTransform>().sizeDelta = new Vector2(180f, iconSize);
        LayoutElement textLE = textObj.AddComponent<LayoutElement>();
        textLE.preferredWidth = 180f; textLE.preferredHeight = iconSize;
        TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
        string nameColor = perk.isDisabled ? "#666666" : "#FFFFFF";
        tmp.text = $"<color={nameColor}>{perk.perkName}</color>  <color=#AAAAAA>Lv {perk.currentLevel}</color>";
        tmp.fontSize = 13;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.color = Color.white;
        tmp.richText = true;
        tmp.raycastTarget = false;
        if (font != null) tmp.font = font;

        // ON/OFF toggle butonu
        GameObject btnObj = new GameObject("ToggleBtn", typeof(RectTransform), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(topRow.transform, false);
        btnObj.GetComponent<RectTransform>().sizeDelta = new Vector2(40f, 18f);
        LayoutElement btnLE = btnObj.AddComponent<LayoutElement>();
        btnLE.preferredWidth = 40f; btnLE.preferredHeight = 18f;
        Image btnImg = btnObj.GetComponent<Image>();
        btnImg.color = perk.isDisabled ? new Color(0.5f, 0.15f, 0.15f) : new Color(0.15f, 0.45f, 0.15f);

        GameObject lblObj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        lblObj.transform.SetParent(btnObj.transform, false);
        RectTransform lblRT = lblObj.GetComponent<RectTransform>();
        lblRT.anchorMin = Vector2.zero; lblRT.anchorMax = Vector2.one;
        lblRT.offsetMin = lblRT.offsetMax = Vector2.zero;
        TextMeshProUGUI btnLabel = lblObj.GetComponent<TextMeshProUGUI>();
        btnLabel.text = perk.isDisabled ? "OFF" : "ON";
        btnLabel.fontSize = 10;
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

        // ── Açıklama satırı ──
        if (!string.IsNullOrEmpty(perk.description))
        {
            GameObject descObj = new GameObject("Description", typeof(RectTransform), typeof(TextMeshProUGUI));
            descObj.transform.SetParent(row.transform, false);
            TextMeshProUGUI descTmp = descObj.GetComponent<TextMeshProUGUI>();
            descTmp.text = perk.description;
            descTmp.fontSize = 10;
            descTmp.color = new Color(0.65f, 0.65f, 0.65f, 1f);
            descTmp.alignment = TextAlignmentOptions.TopLeft;
            descTmp.enableWordWrapping = true;
            descTmp.raycastTarget = false;
            if (font != null) descTmp.font = font;
            var descLE = descObj.AddComponent<LayoutElement>();
            descLE.flexibleWidth = 1;
            descLE.preferredWidth = 260;
            descObj.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        return row;
    }
}
