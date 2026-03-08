using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(LevelUpManager))]
public class LevelUpManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        LevelUpManager manager = (LevelUpManager)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Forced Perk Seçici", EditorStyles.boldLabel);

        // Tüm perk listelerinden bir havuz oluştur
        List<GameObject> allPerks = new List<GameObject>();
        if (manager.commonPerks != null) allPerks.AddRange(manager.commonPerks);
        if (manager.rarePerks != null) allPerks.AddRange(manager.rarePerks);
        if (manager.epicPerks != null) allPerks.AddRange(manager.epicPerks);
        if (manager.legendaryPerks != null) allPerks.AddRange(manager.legendaryPerks);

        // Null'ları temizle
        allPerks.RemoveAll(p => p == null);

        // Dropdown için isim listesi oluştur
        List<string> perkNames = new List<string> { "None" };
        for (int i = 0; i < allPerks.Count; i++)
        {
            BasePerk bp = allPerks[i].GetComponent<BasePerk>();
            string label = bp != null ? bp.perkName : allPerks[i].name;
            perkNames.Add(label);
        }

        // Mevcut seçimin index'ini bul
        int currentIndex = 0;
        if (manager.forcedPerk != null)
        {
            int found = allPerks.IndexOf(manager.forcedPerk);
            if (found >= 0) currentIndex = found + 1; // +1 çünkü 0 = "None"
        }

        int newIndex = EditorGUILayout.Popup("Forced Perk", currentIndex, perkNames.ToArray());

        GameObject newValue = newIndex == 0 ? null : allPerks[newIndex - 1];
        if (manager.forcedPerk != newValue)
        {
            Undo.RecordObject(manager, "Change Forced Perk");
            manager.forcedPerk = newValue;
            EditorUtility.SetDirty(manager);
        }
    }
}
