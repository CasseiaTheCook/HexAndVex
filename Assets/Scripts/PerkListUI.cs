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
    private Coroutine exitDelayCoroutine;
    private bool isMouseOverPanel = false;
    private bool isMouseOverButton = false;
    private const float fadeDuration = 0.15f;
    private const float exitDelay = 0.35f;
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

        // Butonun (bu GameObject) raycast algılama alanını genişlet
        Image buttonImage = GetComponent<Image>();
        if (buttonImage != null)
            buttonImage.raycastPadding = new Vector4(-15, -15, -15, -15);

        if (perkListPanel != null)
        {
            panelCanvasGroup = perkListPanel.GetComponent<CanvasGroup>();
            if (panelCanvasGroup == null)
                panelCanvasGroup = perkListPanel.AddComponent<CanvasGroup>();

            // Panelin raycast algılama alanını genişlet
            Image panelImage = perkListPanel.GetComponent<Image>();
            if (panelImage != null)
                panelImage.raycastPadding = new Vector4(-15, -15, -15, -15);

            panelCanvasGroup.alpha = 0f;
            perkListPanel.SetActive(false);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isMouseOverButton = true;
        if (exitDelayCoroutine != null) { StopCoroutine(exitDelayCoroutine); exitDelayCoroutine = null; }
        RefreshPerkList();
        if (perkListPanel != null)
        {
            perkListPanel.SetActive(true);
            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(FadePanel(panelCanvasGroup != null ? panelCanvasGroup.alpha : 0f, 1f));
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isMouseOverButton = false;
        if (isMouseOverPanel) return;
        if (exitDelayCoroutine != null) StopCoroutine(exitDelayCoroutine);
        exitDelayCoroutine = StartCoroutine(DelayedClose());
    }

    private System.Collections.IEnumerator DelayedClose()
    {
        yield return new WaitForSecondsRealtime(exitDelay);
        if (isMouseOverPanel || isMouseOverButton) yield break;
        if (perkListPanel != null)
        {
            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(FadePanel(panelCanvasGroup.alpha, 0f, true));
        }
    }

    // Panel'in kendi pointer eventleri için çağrılacak metodlar
    public void OnPanelPointerEnter()
    {
        isMouseOverPanel = true;
        if (exitDelayCoroutine != null) { StopCoroutine(exitDelayCoroutine); exitDelayCoroutine = null; }
    }

    public void OnPanelPointerExit()
    {
        isMouseOverPanel = false;
        if (isMouseOverButton) return;
        if (exitDelayCoroutine != null) StopCoroutine(exitDelayCoroutine);
        exitDelayCoroutine = StartCoroutine(DelayedClose());
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

    public void TriggerLevelUpAnimForPerk(BasePerk perk)
    {
        // Menü kapalıysa önce aç
        if (perkListPanel != null && !perkListPanel.activeSelf)
        {
            RefreshPerkList();
            perkListPanel.SetActive(true);
            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(FadePanel(0f, 1f));
            StartCoroutine(LevelUpAnimThenClose(perk));
        }
        else
        {
            // Zaten açıksa direkt anim yap (satırlar refresh edilmiş olmalı)
            int idx = spawnedRowPerks.IndexOf(perk);
            if (idx >= 0 && idx < spawnedRows.Count)
                StartCoroutine(LevelUpAnimRow(spawnedRows[idx], perk));
        }
    }

    private System.Collections.IEnumerator LevelUpAnimThenClose(BasePerk perk)
    {
        yield return new WaitForSeconds(fadeDuration);
        // Satırlar henüz spawn edilmemiş olabilir, refresh ettik
        int idx = spawnedRowPerks.IndexOf(perk);
        if (idx >= 0 && idx < spawnedRows.Count)
            yield return StartCoroutine(LevelUpAnimRow(spawnedRows[idx], perk));
        else
            yield return new WaitForSeconds(1f);
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadePanel(1f, 0f, true));
    }

    private System.Collections.IEnumerator LevelUpAnimRow(GameObject row, BasePerk perk)
    {
        if (row == null) yield break;

        // Level text'ini bul (TopRow > Text objesi)
        TextMeshProUGUI tmp = row.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp == null) yield break;

        // Level yazısını güncelle (perk zaten upgrade edildi)
        string nameColor = GetRarityHex(perk.rarity);
        tmp.text = $"<color={nameColor}>{perk.perkName}</color>  <color=#AAAAAA>Lv {perk.currentLevel}</color>";

        // Scale pop: küçük → büyük → normal
        RectTransform rt = tmp.GetComponent<RectTransform>();
        if (rt == null) yield break;

        Vector3 baseScale = Vector3.one;
        float duration = 0.35f;
        float elapsed = 0f;

        // Önce büyüt
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float scale = Mathf.Lerp(1f, 2.2f, t * (1f - t) * 4f + (t > 0.5f ? 1f - t : t) * 0f);
            // ease out: büyüyüp küçül
            float s = 1f + Mathf.Sin(t * Mathf.PI) * 1.2f;
            rt.localScale = new Vector3(s, s, 1f);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        rt.localScale = baseScale;
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

        // Rarity'e göre sırala: Secret > Legendary > Epic > Rare > Common
        var sorted = new List<BasePerk>(perks);
        sorted.Sort((a, b) => GetRarityOrder(b.rarity).CompareTo(GetRarityOrder(a.rarity)));

        for (int i = 0; i < sorted.Count; i++)
        {
            BasePerk p = sorted[i];
            GameObject row = CreatePerkRow(p, font);
            row.transform.SetParent(perkListPanel.transform, false);
            spawnedRows.Add(row);
            spawnedRowPerks.Add(p);
        }

        // Reroll Stack bilgisi göster
        if (RunManager.instance != null && RunManager.instance.shopRerollStack > 0)
        {
            GameObject stackRow = CreateRerollStackRow(font);
            stackRow.transform.SetParent(perkListPanel.transform, false);
            spawnedRows.Add(stackRow);
        }
    }

    public static string GetRarityHex(PerkRarity rarity)
    {
        return rarity switch
        {
            PerkRarity.Common    => "#FFFFFF",
            PerkRarity.Rare      => "#4DA6FF",
            PerkRarity.Epic      => "#CC44FF",
            PerkRarity.Legendary => "#FFB300",
            PerkRarity.Secret    => "#FF4444",
            _                    => "#FFFFFF"
        };
    }

    private static int GetRarityOrder(PerkRarity rarity)
    {
        return rarity switch
        {
            PerkRarity.Common    => 0,
            PerkRarity.Rare      => 1,
            PerkRarity.Epic      => 2,
            PerkRarity.Legendary => 3,
            PerkRarity.Secret    => 4,
            _                    => 0
        };
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

        // ── Üst satır: ikon + isim + seviye ──
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
        if (perk.icon != null) { iconImg.sprite = perk.icon; iconImg.color = Color.white; }
        else iconImg.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);

        // Perk adı + seviye
        GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(topRow.transform, false);
        textObj.GetComponent<RectTransform>().sizeDelta = new Vector2(230f, iconSize);
        LayoutElement textLE = textObj.AddComponent<LayoutElement>();
        textLE.preferredWidth = 230f; textLE.preferredHeight = iconSize;
        TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
        string nameColor = GetRarityHex(perk.rarity);
        tmp.text = $"<color={nameColor}>{perk.perkName}</color>  <color=#AAAAAA>Lv {perk.currentLevel}</color>";
        tmp.fontSize = 13;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.color = Color.white;
        tmp.richText = true;
        tmp.raycastTarget = false;
        if (font != null) tmp.font = font;

        // ── Açıklama satırı ──
        if (!string.IsNullOrEmpty(perk.description))
        {
            GameObject descObj = new GameObject("Description", typeof(RectTransform), typeof(TextMeshProUGUI));
            descObj.transform.SetParent(row.transform, false);
            TextMeshProUGUI descTmp = descObj.GetComponent<TextMeshProUGUI>();
            descTmp.text = perk.description;
            descTmp.fontSize = 10;
            // Açıklama rengi rarity'e göre (biraz soluk versiyon)
            Color rarityCol;
            ColorUtility.TryParseHtmlString(GetRarityHex(perk.rarity), out rarityCol);
            descTmp.color = Color.Lerp(rarityCol, new Color(0.65f, 0.65f, 0.65f, 1f), 0.45f);
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

    private GameObject CreateRerollStackRow(TMP_FontAsset font)
    {
        int stack = RunManager.instance.shopRerollStack;

        GameObject row = new GameObject("RerollStackRow", typeof(RectTransform));
        var rowVL = row.AddComponent<VerticalLayoutGroup>();
        rowVL.childAlignment = TextAnchor.UpperLeft;
        rowVL.spacing = 0f;
        rowVL.childForceExpandWidth = true;
        rowVL.childForceExpandHeight = false;
        rowVL.padding = new RectOffset(2, 2, 4, 1);
        var rowLE = row.AddComponent<LayoutElement>();
        rowLE.flexibleWidth = 1;
        var rowCSF = row.AddComponent<ContentSizeFitter>();
        rowCSF.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        rowCSF.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        // Separator çizgi
        GameObject sepObj = new GameObject("Separator", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        sepObj.transform.SetParent(row.transform, false);
        sepObj.GetComponent<Image>().color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
        var sepLE = sepObj.AddComponent<LayoutElement>();
        sepLE.preferredHeight = 1f;
        sepLE.flexibleWidth = 1f;

        // Stack text
        GameObject textObj = new GameObject("StackText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(row.transform, false);
        textObj.GetComponent<RectTransform>().sizeDelta = new Vector2(260f, iconSize);
        var textLE = textObj.AddComponent<LayoutElement>();
        textLE.preferredWidth = 260f;
        textLE.preferredHeight = iconSize;
        TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
        tmp.text = $"<color=#FFD933>Genetic Cartel:</color>  <color=#FFFFFF>+{stack}</color> <color=#AAAAAA>tüm zarlara</color>";
        tmp.fontSize = 12;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.color = Color.white;
        tmp.richText = true;
        tmp.raycastTarget = false;
        if (font != null) tmp.font = font;

        return row;
    }
}
