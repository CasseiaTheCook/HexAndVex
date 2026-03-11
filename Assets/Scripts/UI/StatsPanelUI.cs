using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class StatsPanelUI : MonoBehaviour
{
    [Header("Stat Rows (Inspector'dan sırayla bağla)")]
    public TMP_Text turnsValue;
    public TMP_Text diceValue;
    public TMP_Text damageValue;
    public TMP_Text killsValue;
    public TMP_Text goldValue;

    [Header("Perks Section")]
    public Transform perksContainer;

    [Header("Font")]
    public TMP_FontAsset starCrushFont;

    private List<GameObject> spawnedRows = new List<GameObject>();

    public void Refresh()
    {
        if (RunManager.instance == null) return;
        var rm = RunManager.instance;

        if (turnsValue)  turnsValue.text  = rm.totalTurnsPlayed.ToString();
        if (diceValue)   diceValue.text   = rm.totalDiceRolled.ToString();
        if (damageValue) damageValue.text = rm.totalDamageDealt.ToString();
        if (killsValue)  killsValue.text  = rm.totalEnemiesKilled.ToString();
        if (goldValue)   goldValue.text   = rm.totalGoldEarned.ToString();

        RefreshPerks(rm);
    }

    private void RefreshPerks(RunManager rm)
    {
        if (perksContainer == null) return;

        foreach (var row in spawnedRows)
            if (row != null) Destroy(row);
        spawnedRows.Clear();

        if (rm.activePerks.Count == 0)
        {
            spawnedRows.Add(CreatePerkRow(null, "No perks yet", 0, false));
            return;
        }

        foreach (var perk in rm.activePerks)
            spawnedRows.Add(CreatePerkRow(perk.icon, perk.perkName, perk.currentLevel, true));
    }

    private GameObject CreatePerkRow(Sprite icon, string name, int level, bool showLevel)
    {
        // Satır kapsayıcısı
        var rowObj = new GameObject("PerkRow", typeof(RectTransform));
        rowObj.transform.SetParent(perksContainer, false);

        var layout = rowObj.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = 6f;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.padding = new RectOffset(4, 4, 2, 2);

        var rowLE = rowObj.AddComponent<LayoutElement>();
        rowLE.preferredHeight = 32;
        rowLE.flexibleWidth = 1;

        // İkon
        var iconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconObj.transform.SetParent(rowObj.transform, false);
        var iconImg = iconObj.GetComponent<Image>();
        iconImg.sprite = icon;
        iconImg.color = icon != null ? Color.white : new Color(1, 1, 1, 0);
        iconImg.preserveAspect = true;
        var iconLE = iconObj.AddComponent<LayoutElement>();
        iconLE.minWidth = 28; iconLE.preferredWidth = 28;
        iconLE.minHeight = 28; iconLE.preferredHeight = 28;
        iconLE.flexibleWidth = 0;

        // Perk adı — LayoutElement ile sabit genişlik
        var nameGo = new GameObject("Name", typeof(RectTransform));
        nameGo.transform.SetParent(rowObj.transform, false);
        var nameTmp = nameGo.AddComponent<TextMeshProUGUI>();
        nameTmp.text = name;
        nameTmp.fontSize = 15;
        nameTmp.color = Color.white;
        nameTmp.alignment = TextAlignmentOptions.MidlineLeft;
        nameTmp.overflowMode = TextOverflowModes.Ellipsis;
        if (starCrushFont != null) nameTmp.font = starCrushFont;
        var nameLE = nameGo.AddComponent<LayoutElement>();
        nameLE.preferredWidth = 180;
        nameLE.flexibleWidth = 1;
        nameLE.minHeight = 28;

        // Seviye etiketi — sabit genişlik, sağda
        if (showLevel)
        {
            var lvlGo = new GameObject("Level", typeof(RectTransform));
            lvlGo.transform.SetParent(rowObj.transform, false);
            var lvlTmp = lvlGo.AddComponent<TextMeshProUGUI>();
            lvlTmp.text = $"Lv {level}";
            lvlTmp.fontSize = 13;
            lvlTmp.color = new Color(0.5f, 1f, 0.5f);
            lvlTmp.alignment = TextAlignmentOptions.MidlineRight;
            if (starCrushFont != null) lvlTmp.font = starCrushFont;
            var lvlLE = lvlGo.AddComponent<LayoutElement>();
            lvlLE.preferredWidth = 44;
            lvlLE.flexibleWidth = 0;
            lvlLE.minHeight = 28;
        }

        return rowObj;
    }
}
