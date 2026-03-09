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

    void Start()
    {
        if (perkListPanel != null)
            perkListPanel.SetActive(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        RefreshPerkList();
        if (perkListPanel != null)
            perkListPanel.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (perkListPanel != null)
            perkListPanel.SetActive(false);
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
