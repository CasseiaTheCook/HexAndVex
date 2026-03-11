using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using System.Collections.Generic;

public class ButtonColorFixer : EditorWindow
{
    // Renkler UIStyle.cs'den geliyor — değiştirmek için oradan düzenle

    [MenuItem("Tools/Fix Button Colors")]
    public static void FixColors()
    {
        int fixedCount = 0;
        HashSet<GameObject> processed = new HashSet<GameObject>();

        // 1. Button component'i olan objeleri tara
        foreach (var btn in Object.FindObjectsByType<Button>(FindObjectsSortMode.None))
        {
            string objName = btn.gameObject.name.ToLower();
            bool isSkip = HasOnClickMethod(btn, "SkipTurn") || objName.Contains("skip");
            bool isReroll = objName.Contains("reroll");
            bool isPerks = false;

            if (isSkip || isReroll || isPerks)
            {
                ApplyColorBlock(btn);
                processed.Add(btn.gameObject);
                fixedCount++;
                string label = isSkip ? "Skip" : isReroll ? "Reroll" : "Perks";
                Debug.Log($"[ButtonColorFixer] {label} butonu düzeltildi: {btn.gameObject.name}");
            }
        }

        if (fixedCount > 0)
            Debug.Log($"[ButtonColorFixer] Toplam {fixedCount} buton düzeltildi!");
        else
            Debug.LogWarning("[ButtonColorFixer] Sahnede skip veya reroll butonu bulunamadı. Sahne açık mı?");
    }

    static void ApplyColorBlock(Button btn)
    {
        btn.colors = UIStyle.ButtonColors();

        Image img = btn.GetComponent<Image>();
        if (img != null)
        {
            img.color = Color.white;
            EditorUtility.SetDirty(img);
        }

        EditorUtility.SetDirty(btn);
    }

    static bool HasOnClickMethod(Button btn, string methodName)
    {
        int count = btn.onClick.GetPersistentEventCount();
        for (int i = 0; i < count; i++)
        {
            if (btn.onClick.GetPersistentMethodName(i) == methodName)
                return true;
        }
        return false;
    }
}
