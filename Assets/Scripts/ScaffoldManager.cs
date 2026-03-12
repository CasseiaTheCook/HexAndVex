using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;
using System.Collections.Generic;

public class ScaffoldManager : MonoBehaviour
{
    public static ScaffoldManager instance;

    // Üstüne basılmış (titremeye başlamış) scaffold hücreleri
    private HashSet<Vector3Int> activatedScaffolds = new HashSet<Vector3Int>();
    private Dictionary<Vector3Int, Coroutine> shakeCoroutines = new Dictionary<Vector3Int, Coroutine>();

    void Awake()
    {
        if (instance == null) instance = this;
    }

    public bool IsScaffoldCell(Vector3Int cell)
    {
        return LevelGenerator.instance != null
            && LevelGenerator.instance.scaffoldCells != null
            && LevelGenerator.instance.scaffoldCells.Contains(cell);
    }

    /// <summary>
    /// Bir varlık (oyuncu veya düşman) bir hücreye bastığında çağrılır.
    /// Scaffold hücresiyse titremeye başlar.
    /// </summary>
    public void OnEntityEnter(Vector3Int cell)
    {
        if (!IsScaffoldCell(cell)) return;
        if (activatedScaffolds.Contains(cell)) return;

        activatedScaffolds.Add(cell);
        Coroutine shake = StartCoroutine(ShakeCoroutine(cell));
        shakeCoroutines[cell] = shake;
    }

    /// <summary>
    /// Bir varlık scaffold hücresinden ayrıldığında çağrılır.
    /// Aktif scaffold ise çöker ve o hexagon boş kalır.
    /// </summary>
    public void OnEntityLeave(Vector3Int cell)
    {
        if (!activatedScaffolds.Contains(cell)) return;

        activatedScaffolds.Remove(cell);

        if (shakeCoroutines.ContainsKey(cell))
        {
            StopCoroutine(shakeCoroutines[cell]);
            shakeCoroutines.Remove(cell);
        }

        // Titreşim matrisini sıfırla
        Tilemap scaffoldMap = LevelGenerator.instance.scaffoldMap;
        if (scaffoldMap != null && scaffoldMap.HasTile(cell))
            scaffoldMap.SetTransformMatrix(cell, Matrix4x4.identity);

        StartCoroutine(CollapseCoroutine(cell));
    }

    private IEnumerator ShakeCoroutine(Vector3Int cell)
    {
        Tilemap scaffoldMap = LevelGenerator.instance.scaffoldMap;
        if (scaffoldMap == null) yield break;

        float intensity = 0.03f;
        float speed = 30f;
        float elapsed = 0f;

        while (true)
        {
            elapsed += Time.deltaTime;
            float ox = Mathf.Sin(elapsed * speed) * intensity;
            float oy = Mathf.Cos(elapsed * speed * 1.3f) * intensity * 0.5f;

            scaffoldMap.SetTransformMatrix(cell, Matrix4x4.TRS(
                new Vector3(ox, oy, 0f), Quaternion.identity, Vector3.one));

            yield return null;
        }
    }

    private IEnumerator CollapseCoroutine(Vector3Int cell)
    {
        Tilemap scaffoldMap = LevelGenerator.instance.scaffoldMap;
        Tilemap groundMap = LevelGenerator.instance.groundMap;
        Tilemap backgroundMap = LevelGenerator.instance.backgroundMap;

        if (AudioManager.instance != null) AudioManager.instance.PlayWall();

        float duration = 0.35f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float scale = Mathf.Lerp(1f, 0f, t);
            float yOff = Mathf.Lerp(0f, -0.5f, t * t);
            Color fadeColor = new Color(1f, 1f, 1f, 1f - t);

            Matrix4x4 matrix = Matrix4x4.TRS(
                new Vector3(0f, yOff, 0f), Quaternion.identity, new Vector3(scale, scale, 1f));

            if (scaffoldMap != null && scaffoldMap.HasTile(cell))
            {
                scaffoldMap.SetTransformMatrix(cell, matrix);
                scaffoldMap.SetColor(cell, fadeColor);
            }

            if (groundMap != null && groundMap.HasTile(cell))
            {
                groundMap.SetTransformMatrix(cell, matrix);
                groundMap.SetColor(cell, fadeColor);
            }

            if (backgroundMap != null && backgroundMap.HasTile(cell))
            {
                backgroundMap.SetTransformMatrix(cell, matrix);
                backgroundMap.SetColor(cell, fadeColor);
            }

            yield return null;
        }

        // Tile'ları tamamen kaldır
        if (scaffoldMap != null)
        {
            scaffoldMap.SetTransformMatrix(cell, Matrix4x4.identity);
            scaffoldMap.SetTile(cell, null);
        }
        if (groundMap != null)
        {
            groundMap.SetTransformMatrix(cell, Matrix4x4.identity);
            groundMap.SetTile(cell, null);
        }
        if (backgroundMap != null)
        {
            backgroundMap.SetTransformMatrix(cell, Matrix4x4.identity);
            backgroundMap.SetTile(cell, null);
        }

        // Takip listesinden çıkar
        if (LevelGenerator.instance != null)
            LevelGenerator.instance.scaffoldCells.Remove(cell);
    }

    /// <summary>
    /// Level geçişlerinde tüm scaffold durumunu sıfırla.
    /// </summary>
    public void ClearAll()
    {
        foreach (var coroutine in shakeCoroutines.Values)
            if (coroutine != null) StopCoroutine(coroutine);
        shakeCoroutines.Clear();
        activatedScaffolds.Clear();
    }
}
