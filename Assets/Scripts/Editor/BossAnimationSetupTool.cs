using UnityEngine;
using UnityEditor;

public static class BossAnimationSetupTool
{
    [MenuItem("Tools/Setup Boss Animation Controller")]
    public static void SetupBossAnimationController()
    {
        // Sahnede zaten varsa uyar
        BossIntroSequence existing = Object.FindFirstObjectByType<BossIntroSequence>();
        if (existing != null)
        {
            bool replace = EditorUtility.DisplayDialog(
                "BossIntroSequence Zaten Var",
                $"Sahnede zaten bir BossIntroSequence var: '{existing.gameObject.name}'\nYeni bir tane oluşturmak istiyor musun?",
                "Evet, Yeni Oluştur", "İptal");
            if (!replace) return;
        }

        // Yeni BossAnimationController objesi oluştur
        GameObject go = new GameObject("BossAnimationController");
        BossIntroSequence seq = go.AddComponent<BossIntroSequence>();

        // Seçili yap ve hiyerarşide göster
        Selection.activeGameObject = go;
        EditorGUIUtility.PingObject(go);

        EditorUtility.SetDirty(go);
        Debug.Log("✅ BossAnimationController oluşturuldu. BossIntroSequence Inspector'dan ayarlanabilir.");
    }
}
