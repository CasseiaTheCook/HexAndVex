using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class PerkListUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Referanslar")]
    public GameObject perkListPanel;
    public TMP_Text perkListText;
    public TMP_Text buttonText;

    private CanvasGroup panelCanvasGroup;
    private Coroutine fadeCoroutine;
    private const float fadeDuration = 0.15f;

    void Start()
    {
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
        if (perkListText == null || RunManager.instance == null) return;

        var perks = RunManager.instance.activePerks;

        if (perks.Count == 0)
        {
            perkListText.text = "No perks yet.";
            return;
        }

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        for (int i = 0; i < perks.Count; i++)
        {
            BasePerk p = perks[i];
            sb.Append($"<color=#FFD700>{p.perkName}</color>  <color=#AAAAAA>Lv {p.currentLevel}</color>\n");
            sb.Append($"<size=70%>{p.description}</size>");
            if (i < perks.Count - 1) sb.Append("\n\n");
        }

        perkListText.text = sb.ToString();
    }
}
