using UnityEngine;
using UnityEditor;
using System.IO;

public static class BombCursorSetup
{
    [MenuItem("Tools/Setup Bomb Cursor")]
    public static void Setup()
    {
        string texturePath = "Assets/Sprites/VFX/BombCursor.png";
        if (!AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath))
        {
            Texture2D tex = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            Color clear = new Color(0, 0, 0, 0);
            Color orange = new Color(1f, 0.5f, 0f, 1f);

            for (int x = 0; x < 32; x++)
                for (int y = 0; y < 32; y++)
                    tex.SetPixel(x, y, clear);

            // Crosshair çizgiler
            for (int i = 0; i < 32; i++)
            {
                if (i >= 13 && i <= 19) continue;
                tex.SetPixel(16, i, orange);
                tex.SetPixel(i, 16, orange);
            }

            // Daire
            DrawCircle(tex, 16, 16, 12, orange);

            tex.Apply();
            byte[] pngBytes = tex.EncodeToPNG();
            Object.DestroyImmediate(tex);

            string dir = Path.GetDirectoryName(texturePath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            File.WriteAllBytes(texturePath, pngBytes);
            AssetDatabase.ImportAsset(texturePath);

            TextureImporter importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Cursor;
                importer.isReadable = true;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.SaveAndReimport();
            }
        }

        BombPlacementCursor existing = Object.FindFirstObjectByType<BombPlacementCursor>();
        if (existing == null)
        {
            GameObject go = new GameObject("BombPlacementCursor");
            BombPlacementCursor cursor = go.AddComponent<BombPlacementCursor>();
            Texture2D loadedTex = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if (loadedTex != null) cursor.cursorTexture = loadedTex;
            Undo.RegisterCreatedObjectUndo(go, "Create BombPlacementCursor");
            Selection.activeGameObject = go;
            Debug.Log("BombPlacementCursor oluşturuldu ve sahneye eklendi.");
        }
        else
        {
            if (existing.cursorTexture == null)
            {
                Texture2D loadedTex = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
                if (loadedTex != null) existing.cursorTexture = loadedTex;
                EditorUtility.SetDirty(existing);
            }
            Debug.Log("BombPlacementCursor zaten sahnede var, texture atandı.");
        }
    }

    private static void DrawCircle(Texture2D tex, int cx, int cy, int radius, Color color)
    {
        int x = radius, y = 0, d = 1 - radius;
        while (x >= y)
        {
            tex.SetPixel(cx + x, cy + y, color);
            tex.SetPixel(cx - x, cy + y, color);
            tex.SetPixel(cx + x, cy - y, color);
            tex.SetPixel(cx - x, cy - y, color);
            tex.SetPixel(cx + y, cy + x, color);
            tex.SetPixel(cx - y, cy + x, color);
            tex.SetPixel(cx + y, cy - x, color);
            tex.SetPixel(cx - y, cy - x, color);
            y++;
            if (d <= 0) d += 2 * y + 1;
            else { x--; d += 2 * (y - x) + 1; }
        }
    }
}
