using UnityEngine;
using UnityEditor;
using System.IO;

public static class ItemAssetCreator
{
    [MenuItem("Tools/Shop Itemlerini Oluştur")]
    public static void CreateAllItems()
    {
        string folder = "Assets/ScriptableObjects/Items";
        if (!AssetDatabase.IsValidFolder("Assets/ScriptableObjects"))
            AssetDatabase.CreateFolder("Assets", "ScriptableObjects");
        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder("Assets/ScriptableObjects", "Items");

        CreateItem<BioVial>(folder, "BioVial", "Bio-Vial", "1 can yenile", 3);
        CreateItem<MutaGen>(folder, "MutaGen", "Muta-Gen", "2 can yenile", 5);
        CreateItem<NecroShot>(folder, "NecroShot", "Necro-Shot", "Mapteki istediğin düşmanı anında öldür", 10);
        CreateItem<SynthStim>(folder, "SynthStim", "Synth-Stim", "Bu savaşta +1 zar at", 6);
        CreateItem<GoldLeech>(folder, "GoldLeech", "Gold-Leech", "Sonraki düşmandan 2 kat gold düşürt", 4);
        CreateItem<OverClok>(folder, "OverClok", "Over-Clok", "Sıradaki ilk zar toplamının 2 katı hasar ver", 7);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Shopmanager'a otomatik ata
        var mgr = Object.FindFirstObjectByType<Shopmanager>();
        if (mgr != null)
        {
            mgr.itemPool.Clear();
            string[] guids = AssetDatabase.FindAssets("t:BaseItem", new[] { folder });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                BaseItem item = AssetDatabase.LoadAssetAtPath<BaseItem>(path);
                if (item != null) mgr.itemPool.Add(item);
            }
            EditorUtility.SetDirty(mgr);
            Debug.Log($"✅ {mgr.itemPool.Count} item Shopmanager'a atandı!");
        }
        else
        {
            Debug.LogWarning("Shopmanager sahnede bulunamadı. Item'leri elle sürükle.");
        }

        Debug.Log($"✅ 6 item asset oluşturuldu: {folder}");
    }

    private static void CreateItem<T>(string folder, string fileName, string name, string desc, int price) where T : BaseItem
    {
        string path = $"{folder}/{fileName}.asset";
        if (AssetDatabase.LoadAssetAtPath<T>(path) != null) return; // Zaten var

        T item = ScriptableObject.CreateInstance<T>();
        item.itemName = name;
        item.description = desc;
        item.price = price;
        AssetDatabase.CreateAsset(item, path);
    }
}
