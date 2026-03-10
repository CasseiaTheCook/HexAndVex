using UnityEngine;
using UnityEditor;

public class CoinSpriteAssigner
{
    [MenuItem("Tools/Assign Coin Sprites")]
    public static void AssignCoinSprites()
    {
        Sprite coinSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/VFX/Coin.aseprite");
        if (coinSprite == null)
        {
            Debug.LogError("Coin sprite not found at Assets/Sprites/VFX/Coin.aseprite");
            return;
        }

        int count = 0;

        var tm = Object.FindFirstObjectByType<TurnManager>();
        if (tm != null && tm.coinSprite == null)
        {
            tm.coinSprite = coinSprite;
            EditorUtility.SetDirty(tm);
            count++;
        }

        foreach (var slot in Object.FindObjectsByType<ShopSlot>(FindObjectsSortMode.None))
        {
            if (slot.coinSprite == null)
            {
                slot.coinSprite = coinSprite;
                EditorUtility.SetDirty(slot);
                count++;
            }
        }

        foreach (var vfx in Object.FindObjectsByType<CoinDropVFX>(FindObjectsSortMode.None))
        {
            if (vfx.coinSprite == null)
            {
                vfx.coinSprite = coinSprite;
                EditorUtility.SetDirty(vfx);
                count++;
            }
        }

        Debug.Log($"Assigned coin sprite to {count} component(s). Save the scene to keep changes.");
    }
}
