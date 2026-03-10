using UnityEngine;
using UnityEditor;
using System.IO;

public static class NecroShotCursorSetup
{
    [MenuItem("Tools/Setup NecroShot Cursor")]
    public static void Setup()
    {
        // Placeholder cursor texture oluştur (32x32 crosshair)
        string texturePath = "Assets/Sprites/VFX/NecroShotCursor.png";
        if (!AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath))
        {
            Texture2D tex = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            Color clear = new Color(0, 0, 0, 0);
            Color red = new Color(1f, 0.2f, 0.2f, 1f);

            // Temizle
            for (int x = 0; x < 32; x++)
                for (int y = 0; y < 32; y++)
                    tex.SetPixel(x, y, clear);

            // Çapraz çizgiler (crosshair)
            for (int i = 0; i < 32; i++)
            {
                if (i >= 12 && i <= 20) continue; // ortada boşluk
                tex.SetPixel(16, i, red);
                tex.SetPixel(i, 16, red);
            }

            // Orta nokta
            tex.SetPixel(16, 16, Color.white);
            tex.SetPixel(15, 16, Color.white);
            tex.SetPixel(16, 15, Color.white);
            tex.SetPixel(17, 16, Color.white);
            tex.SetPixel(16, 17, Color.white);

            // Daire
            DrawCircle(tex, 16, 16, 14, red);

            tex.Apply();
            byte[] pngBytes = tex.EncodeToPNG();
            Object.DestroyImmediate(tex);

            string dir = Path.GetDirectoryName(texturePath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            File.WriteAllBytes(texturePath, pngBytes);
            AssetDatabase.ImportAsset(texturePath);

            // Cursor olarak kullanılabilmesi için Read/Write etkinleştir
            TextureImporter importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Cursor;
                importer.isReadable = true;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.SaveAndReimport();
            }

            Debug.Log("NecroShot placeholder cursor oluşturuldu: " + texturePath);
        }

        // Sahneye NecroShotCursor ekle
        NecroShotCursor existing = Object.FindFirstObjectByType<NecroShotCursor>();
        if (existing == null)
        {
            GameObject go = new GameObject("NecroShotCursor");
            NecroShotCursor cursor = go.AddComponent<NecroShotCursor>();

            Texture2D loadedTex = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if (loadedTex != null) cursor.cursorTexture = loadedTex;

            Undo.RegisterCreatedObjectUndo(go, "Create NecroShotCursor");
            Selection.activeGameObject = go;
            Debug.Log("NecroShotCursor oluşturuldu. cursorTexture alanına istediğiniz PNG'yi atabilirsiniz.");
        }
        else
        {
            if (existing.cursorTexture == null)
            {
                Texture2D loadedTex = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
                if (loadedTex != null) existing.cursorTexture = loadedTex;
                EditorUtility.SetDirty(existing);
            }
            Debug.Log("NecroShotCursor zaten sahnede var.");
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
