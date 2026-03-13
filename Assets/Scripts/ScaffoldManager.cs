using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;
using System.Collections.Generic;

public class ScaffoldManager : MonoBehaviour
{
    public static ScaffoldManager instance;

    [Header("Scaffold Ayarları")]
    [Tooltip("Çökme animasyonu süresi (saniye).")]
    public float collapseDuration = 0.35f;

    // Aktif scaffold'lar: hücre -> titreşim coroutine
    private Dictionary<Vector3Int, Coroutine> shakeCoroutines = new Dictionary<Vector3Int, Coroutine>();
    // Şu anda çökmekte olan scaffold'lar (tekrar tetikleme önleme)
    private HashSet<Vector3Int> collapsingScaffolds = new HashSet<Vector3Int>();

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

    public bool IsCollapsing(Vector3Int cell)
    {
        return collapsingScaffolds.Contains(cell);
    }

    /// <summary>
    /// Oyuncu scaffold'a bastığında çağrılır. Titreşim başlar.
    /// </summary>
    public void OnEntityEnter(Vector3Int cell)
    {
        if (!IsScaffoldCell(cell)) return;
        if (shakeCoroutines.ContainsKey(cell)) return;
        if (collapsingScaffolds.Contains(cell)) return;

        Coroutine shake = StartCoroutine(ShakeCoroutine(cell));
        shakeCoroutines[cell] = shake;
    }

    /// <summary>
    /// Oyuncu scaffold'dan ayrıldığında çağrılır. Titreşim durur, scaffold düşer.
    /// </summary>
    public void OnEntityLeave(Vector3Int cell)
    {
        if (!shakeCoroutines.ContainsKey(cell)) return;
        if (collapsingScaffolds.Contains(cell)) return;

        StopShakeCoroutine(cell);
        ResetTileTransform(cell);

        HexMovement playerMovement = TurnManager.instance?.player;

        // Oyuncu artık scaffold'ın üstünde değilse, knockback olsun olmasın çök
        bool playerStillOnScaffold = playerMovement != null && playerMovement.GetCurrentCellPosition() == cell;
        
        if (!playerStillOnScaffold)
        {
            StartCoroutine(CollapseCoroutine(cell));
        }
    }

    /// <summary>
    /// Süresiz titreşim. Oyuncu üstünde durduğu sürece devam eder.
    /// </summary>
    private IEnumerator ShakeCoroutine(Vector3Int cell)
    {
        Tilemap scaffoldMap = LevelGenerator.instance.scaffoldMap;
        Tilemap backgroundMap = LevelGenerator.instance.backgroundMap;
        if (scaffoldMap == null) yield break;

        HexMovement player = TurnManager.instance?.player;
        Vector3 originalPlayerPos = player != null ? player.transform.position : Vector3.zero;

        float elapsed = 0f;
        float intensity = 0.005f;
        float speed = 45f;

        while (true)
        {
            // Oyuncu scaffold'dan ayrıldıysa (başka cell'e taşındıysa) shake'i durdur
            if (player != null && player.GetCurrentCellPosition() != cell)
            {
                break;
            }

            elapsed += Time.deltaTime;

            float ox = Mathf.Sin(elapsed * speed) * intensity;
            float oy = Mathf.Cos(elapsed * speed * 1.3f) * intensity * 0.5f;

            Matrix4x4 shakeMatrix = Matrix4x4.TRS(
                new Vector3(ox, oy, 0f), Quaternion.identity, Vector3.one);

            // Üst katmanı titret
            if (scaffoldMap.HasTile(cell))
            {
                scaffoldMap.SetTransformMatrix(cell, shakeMatrix);
            }

            // Alt katmanı (background) da aynı şekilde titret
            if (backgroundMap != null && backgroundMap.HasTile(cell))
            {
                backgroundMap.SetTransformMatrix(cell, shakeMatrix);
            }

            // Oyuncuyu da titret
            if (player != null && player.GetCurrentCellPosition() == cell)
            {
                player.transform.position = originalPlayerPos + new Vector3(ox, oy, 0f);
            }

            yield return null;
        }
    }

    /// <summary>
    /// Scaffold çökme animasyonu. Tile'ları kaldırır.
    /// </summary>
    private IEnumerator CollapseCoroutine(Vector3Int cell)
    {
        if (collapsingScaffolds.Contains(cell)) yield break;
        collapsingScaffolds.Add(cell);

        Tilemap scaffoldMap = LevelGenerator.instance.scaffoldMap;
        Tilemap backgroundMap = LevelGenerator.instance.backgroundMap;

        if (AudioManager.instance != null) AudioManager.instance.PlayWall();

        float elapsed = 0f;
        while (elapsed < collapseDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / collapseDuration;
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
            if (backgroundMap != null && backgroundMap.HasTile(cell))
            {
                backgroundMap.SetTransformMatrix(cell, matrix);
                backgroundMap.SetColor(cell, fadeColor);
            }

            yield return null;
        }

        RemoveTile(scaffoldMap, cell);
        RemoveTile(backgroundMap, cell);

        if (LevelGenerator.instance != null)
            LevelGenerator.instance.scaffoldCells.Remove(cell);

        collapsingScaffolds.Remove(cell);
    }

    private void StopShakeCoroutine(Vector3Int cell)
    {
        if (shakeCoroutines.ContainsKey(cell))
        {
            if (shakeCoroutines[cell] != null)
                StopCoroutine(shakeCoroutines[cell]);
            shakeCoroutines.Remove(cell);
        }
    }

    private void ResetTileTransform(Vector3Int cell)
    {
        Tilemap scaffoldMap = LevelGenerator.instance.scaffoldMap;
        if (scaffoldMap != null && scaffoldMap.HasTile(cell))
            scaffoldMap.SetTransformMatrix(cell, Matrix4x4.identity);

        // Alt katmanın (background) da pozisyonunu sıfırla
        Tilemap backgroundMap = LevelGenerator.instance.backgroundMap;
        if (backgroundMap != null && backgroundMap.HasTile(cell))
            backgroundMap.SetTransformMatrix(cell, Matrix4x4.identity);
    }

    private void RemoveTile(Tilemap map, Vector3Int cell)
    {
        if (map != null && map.HasTile(cell))
        {
            map.SetTransformMatrix(cell, Matrix4x4.identity);
            map.SetColor(cell, Color.white);
            map.SetTile(cell, null);
        }
    }

    public void ClearAll()
    {
        foreach (var coroutine in shakeCoroutines.Values)
            if (coroutine != null) StopCoroutine(coroutine);
        shakeCoroutines.Clear();
        collapsingScaffolds.Clear();
        StopAllCoroutines();
    }
}
