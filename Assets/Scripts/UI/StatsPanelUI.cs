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

        if (turnsValue)  turnsValue.text  = FormatWithBest(rm.totalTurnsPlayed,   RunManager.BestTurns);
        if (diceValue)   diceValue.text   = FormatWithBest(rm.totalDiceRolled,    RunManager.BestDice);
        if (damageValue) damageValue.text = FormatWithBest(rm.totalDamageDealt,   RunManager.BestDamage);
        if (killsValue)  killsValue.text  = FormatWithBest(rm.totalEnemiesKilled, RunManager.BestKills);
        if (goldValue)   goldValue.text   = FormatWithBest(rm.totalGoldEarned,    RunManager.BestGold);

        RefreshPerks(rm);
    }

    // Mevcut değer best'i geçiyorsa altın rengi ve "NEW BEST!" göster
    // ★ unicode yerine [BEST] kullan — TMP font embed olmadan box görünür
    private string FormatWithBest(int current, int best)
    {
        if (current > best)
            return $"<color=#FFD700>{current} [BEST]</color>";
        return $"{current}  <color=#888888><size=11>best {best}</size></color>";
    }

    private void RefreshPerks(RunManager rm)
    {
        if (perksContainer == null) return;

        foreach (var row in spawnedRows)
            if (row != null) Destroy(row);
        spawnedRows.Clear();

        if (rm.activePerks.Count == 0)
        {
            spawnedRows.Add(CreatePerkRow(null, "No perks yet", 0, "", false));
            return;
        }

        foreach (var perk in rm.activePerks)
            spawnedRows.Add(CreatePerkRow(perk.icon, perk.perkName, perk.currentLevel, perk.description, true));
    }

    private GameObject CreatePerkRow(Sprite icon, string name, int level, string description, bool showLevel)
    {
        // Satır kapsayıcısı — dikey: icon+isim üstte, açıklama altta
        var rowObj = new GameObject("PerkRow", typeof(RectTransform));
        rowObj.transform.SetParent(perksContainer, false);

        var rowVL = rowObj.AddComponent<VerticalLayoutGroup>();
        rowVL.childAlignment = TextAnchor.UpperLeft;
        rowVL.spacing = 1f;
        rowVL.childForceExpandWidth = true;
        rowVL.childForceExpandHeight = false;
        rowVL.padding = new RectOffset(4, 4, 3, 3);

        var rowLE = rowObj.AddComponent<LayoutElement>();
        rowLE.preferredWidth = 380;
        rowLE.flexibleWidth = 1;
        rowObj.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // ── Üst satır: ikon + isim + seviye ─────────────────────────────
        var topRow = new GameObject("TopRow", typeof(RectTransform));
        topRow.transform.SetParent(rowObj.transform, false);
        var topHL = topRow.AddComponent<HorizontalLayoutGroup>();
        topHL.childAlignment = TextAnchor.MiddleLeft;
        topHL.spacing = 6f;
        topHL.childForceExpandWidth = false;
        topHL.childForceExpandHeight = false;
        var topLE = topRow.AddComponent<LayoutElement>();
        topLE.preferredHeight = 28;
        topLE.flexibleWidth = 1;

        // İkon
        var iconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconObj.transform.SetParent(topRow.transform, false);
        var iconImg = iconObj.GetComponent<Image>();
        iconImg.sprite = icon;
        iconImg.color = icon != null ? Color.white : new Color(1, 1, 1, 0);
        iconImg.preserveAspect = true;
        var iconLE = iconObj.AddComponent<LayoutElement>();
        iconLE.minWidth = 24; iconLE.preferredWidth = 24;
        iconLE.minHeight = 24; iconLE.preferredHeight = 24;
        iconLE.flexibleWidth = 0;

        // Perk adı
        var nameGo = new GameObject("Name", typeof(RectTransform));
        nameGo.transform.SetParent(topRow.transform, false);
        var nameTmp = nameGo.AddComponent<TextMeshProUGUI>();
        nameTmp.text = name;
        nameTmp.fontSize = 14;
        nameTmp.color = Color.white;
        nameTmp.fontStyle = FontStyles.Bold;
        nameTmp.alignment = TextAlignmentOptions.MidlineLeft;
        nameTmp.overflowMode = TextOverflowModes.Ellipsis;
        if (starCrushFont != null) nameTmp.font = starCrushFont;
        var nameLE = nameGo.AddComponent<LayoutElement>();
        nameLE.flexibleWidth = 1;
        nameLE.minHeight = 24;

        // Seviye etiketi
        if (showLevel)
        {
            var lvlGo = new GameObject("Level", typeof(RectTransform));
            lvlGo.transform.SetParent(topRow.transform, false);
            var lvlTmp = lvlGo.AddComponent<TextMeshProUGUI>();
            lvlTmp.text = $"Lv {level}";
            lvlTmp.fontSize = 12;
            lvlTmp.color = new Color(0.5f, 1f, 0.5f);
            lvlTmp.alignment = TextAlignmentOptions.MidlineRight;
            if (starCrushFont != null) lvlTmp.font = starCrushFont;
            var lvlLE = lvlGo.AddComponent<LayoutElement>();
            lvlLE.preferredWidth = 40;
            lvlLE.flexibleWidth = 0;
            lvlLE.minHeight = 24;
        }

        // ── Açıklama satırı ───────────────────────────────────────────────
        if (!string.IsNullOrEmpty(description))
        {
            var descGo = new GameObject("Description", typeof(RectTransform));
            descGo.transform.SetParent(rowObj.transform, false);
            var descTmp = descGo.AddComponent<TextMeshProUGUI>();
            descTmp.text = description;
            descTmp.fontSize = 11;
            descTmp.color = new Color(0.75f, 0.75f, 0.75f, 1f);
            descTmp.alignment = TextAlignmentOptions.TopLeft;
            descTmp.enableWordWrapping = true;
            if (starCrushFont != null) descTmp.font = starCrushFont;
            var descLE = descGo.AddComponent<LayoutElement>();
            descLE.flexibleWidth = 1;
            descLE.preferredWidth = 340;
            descGo.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        return rowObj;
    }
}
