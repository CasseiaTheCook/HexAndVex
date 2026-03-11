using UnityEngine;
using UnityEditor;

public class GlassCanonPrefabCreator
{
    [MenuItem("Tools/Create GlassCanon Prefab")]
    static void CreatePrefab()
    {
        GameObject go = new GameObject("GlassCanon");
        GlassCanonPerk perk = go.AddComponent<GlassCanonPerk>();
        perk.perkName = "Glass Canon";
        perk.description = "Your maximum health is reduced to 3, but you deal double damage.";
        perk.rarity = PerkRarity.Rare;
        perk.maxLevel = 1;

        string path = "Assets/Prefabs/Perks/GlassCanon.prefab";
        PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);

        Debug.Log("GlassCanon prefab created at " + path);
    }
}
