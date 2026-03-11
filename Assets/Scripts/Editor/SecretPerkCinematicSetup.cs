using UnityEngine;
using UnityEditor;

public class SecretPerkCinematicSetup
{
    [MenuItem("Tools/Setup Secret Perk Cinematic")]
    public static void Setup()
    {
        // Sahnede zaten varsa tekrar ekleme
        if (Object.FindFirstObjectByType<SecretPerkCinematic>() != null)
        {
            Debug.Log("SecretPerkCinematic zaten sahnede mevcut!");
            return;
        }

        GameObject go = new GameObject("SecretPerkCinematic");
        go.AddComponent<SecretPerkCinematic>();
        Undo.RegisterCreatedObjectUndo(go, "Create SecretPerkCinematic");
        Selection.activeGameObject = go;

        Debug.Log("SecretPerkCinematic sahnede oluşturuldu! Artık secret perk alındığında epik animasyon oynatılacak.");
    }
}
