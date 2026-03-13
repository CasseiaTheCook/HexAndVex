using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class ScaffoldManager : MonoBehaviour
{
    public static ScaffoldManager instance;

    [Header("Scaffold Ayarları")]
    [Tooltip("Üstüne basıldıktan sonra titreme süresi (saniye). Bu süre dolunca çöker.")]
    public float shakeDuration = 1.2f;

    [Tooltip("Çökme animasyonu süresi (saniye).")]
    public float collapseDuration = 0.35f;

    [Tooltip("Scaffold çöktüğünde üstündeki varlığa verilen hasar.")]
    public int collapseDamage = 1;

    // Aktif scaffold'lar: hücre -> coroutine bilgisi
    private Dictionary<Vector3Int, Coroutine> shakeCoroutines = new Dictionary<Vector3Int, Coroutine>();
    // Hangi scaffold'lar aktive edilmiş (basılmış)
    private HashSet<Vector3Int> activatedScaffolds = new HashSet<Vector3Int>();
    // Şu anda çökmekte olan scaffold'lar (tekrar tetikleme önleme)
    private HashSet<Vector3Int> collapsingScaffolds = new HashSet<Vector3Int>();

    void Awake()
    {
        if (instance == null) instance = this;
    }

    /// <summary>
    /// Bu hücre scaffold mı?
    /// </summary>
    public bool IsScaffoldCell(Vector3Int cell)
    {
        return LevelGenerator.instance != null
            && LevelGenerator.instance.scaffoldCells != null
            && LevelGenerator.instance.scaffoldCells.Contains(cell);
    }

    /// <summary>
    /// Bu scaffold şu anda çökmekte mi?
    /// </summary>
    public bool IsCollapsing(Vector3Int cell)
    {
        return collapsingScaffolds.Contains(cell);
    }

    /// <summary>
    /// Bir varlık (oyuncu veya düşman) scaffold hücresine bastığında çağrılır.
    /// Scaffold titremeye başlar ve shakeDuration sonra otomatik çöker.
    /// </summary>
    public void OnEntityEnter(Vector3Int cell)
    {
        if (!IsScaffoldCell(cell)) return;
        if (activatedScaffolds.Contains(cell)) return;
        if (collapsingScaffolds.Contains(cell)) return;

        activatedScaffolds.Add(cell);
        Coroutine shake = StartCoroutine(ShakeAndCollapseCoroutine(cell));
        shakeCoroutines[cell] = shake;
    }

    /// <summary>
    /// Bir varlık scaffold hücresinden ayrıldığında çağrılır.
    /// Eğer scaffold aktif edilmişse hemen çöker.
    /// </summary>
    public void OnEntityLeave(Vector3Int cell)
    {
        if (!activatedScaffolds.Contains(cell)) return;
        if (collapsingScaffolds.Contains(cell)) return;

        // Titreme coroutine'ini durdur
        StopShakeCoroutine(cell);
        activatedScaffolds.Remove(cell);

        // Transform matrisini sıfırla
        ResetTileTransform(cell);

        // Hemen çök
        StartCoroutine(CollapseCoroutine(cell));
    }

    /// <summary>
    /// Titreme + süre dolunca otomatik çökme.
    /// Varlık üstünde kalsa bile shakeDuration sonra çöker.
    /// </summary>
    private IEnumerator ShakeAndCollapseCoroutine(Vector3Int cell)
    {
        Tilemap scaffoldMap = LevelGenerator.instance.scaffoldMap;
        if (scaffoldMap == null) yield break;

        float elapsed = 0f;
        float baseIntensity = 0.02f;
        float speed = 25f;

        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;

            // Titreme yoğunluğu zamanla artar
            float progress = elapsed / shakeDuration;
            float intensity = Mathf.Lerp(baseIntensity, baseIntensity * 3f, progress);
            float currentSpeed = Mathf.Lerp(speed, speed * 2f, progress);

            float ox = Mathf.Sin(elapsed * currentSpeed) * intensity;
            float oy = Mathf.Cos(elapsed * currentSpeed * 1.3f) * intensity * 0.5f;

            if (scaffoldMap.HasTile(cell))
            {
                scaffoldMap.SetTransformMatrix(cell, Matrix4x4.TRS(
                    new Vector3(ox, oy, 0f), Quaternion.identity, Vector3.one));
            }

            yield return null;
        }

        // Süre doldu - scaffold üstünde durulsa bile çöker
        shakeCoroutines.Remove(cell);
        activatedScaffolds.Remove(cell);

        ResetTileTransform(cell);
        StartCoroutine(CollapseCoroutine(cell));
    }

    /// <summary>
    /// Scaffold çökme animasyonu. Tile'ları kaldırır ve üstündeki varlığa hasar verir.
    /// </summary>
    private IEnumerator CollapseCoroutine(Vector3Int cell)
    {
        if (collapsingScaffolds.Contains(cell)) yield break;
        collapsingScaffolds.Add(cell);

        Tilemap scaffoldMap = LevelGenerator.instance.scaffoldMap;
        Tilemap groundMap = LevelGenerator.instance.groundMap;
        Tilemap backgroundMap = LevelGenerator.instance.backgroundMap;

        if (AudioManager.instance != null) AudioManager.instance.PlayWall();

        // Çökme animasyonu
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
        RemoveTile(scaffoldMap, cell);
        RemoveTile(groundMap, cell);
        RemoveTile(backgroundMap, cell);

        // Takip listesinden çıkar
        if (LevelGenerator.instance != null)
            LevelGenerator.instance.scaffoldCells.Remove(cell);

        // Üstünde duran varlığı kontrol et ve düşür
        DamageEntityOnCell(cell);

        collapsingScaffolds.Remove(cell);
    }

    /// <summary>
    /// Çöken scaffold üstündeki oyuncu veya düşmana hasar verir ve güvenli hücreye iter.
    /// </summary>
    private void DamageEntityOnCell(Vector3Int cell)
    {
        if (TurnManager.instance == null) return;

        // Oyuncu bu hücrede mi?
        HexMovement player = TurnManager.instance.player;
        if (player != null && player.GetCurrentCellPosition() == cell)
        {
            player.health.TakeDamage(collapseDamage);

            // Oyuncuyu güvenli komşu hücreye it
            Vector3Int safeCell = FindSafeNeighbor(cell);
            if (safeCell != cell)
            {
                player.StartKnockbackMovement(safeCell);
            }
            return;
        }

        // Düşman bu hücrede mi?
        EnemyAI enemyOnCell = TurnManager.instance.GetEnemyAtCell(cell);
        if (enemyOnCell != null)
        {
            enemyOnCell.health.TakeDamage(collapseDamage);

            if (enemyOnCell.health.currentHP <= 0)
            {
                StartCoroutine(enemyOnCell.FadeDieCoroutine());
            }
            else
            {
                // Düşmanı güvenli komşu hücreye it
                Vector3Int safeCell = FindSafeNeighbor(cell);
                if (safeCell != cell)
                {
                    enemyOnCell.StartKnockbackMovement(safeCell);
                }
            }
        }
    }

    /// <summary>
    /// Verilen hücrenin çevresinde güvenli (ground var, hazard/scaffold değil) bir komşu hücre bulur.
    /// </summary>
    private Vector3Int FindSafeNeighbor(Vector3Int cell)
    {
        Vector3Int[] oddOff = { new Vector3Int(+1, 0, 0), new Vector3Int(0, +1, 0), new Vector3Int(-1, +1, 0), new Vector3Int(-1, 0, 0), new Vector3Int(-1, -1, 0), new Vector3Int(0, -1, 0) };
        Vector3Int[] evenOff = { new Vector3Int(+1, 0, 0), new Vector3Int(+1, +1, 0), new Vector3Int(0, +1, 0), new Vector3Int(-1, 0, 0), new Vector3Int(0, -1, 0), new Vector3Int(+1, -1, 0) };

        Vector3Int[] offsets = (cell.y % 2 != 0) ? evenOff : oddOff;
        Tilemap groundMap = LevelGenerator.instance.groundMap;

        // Önce güvenli hücre ara (hazard değil, scaffold değil, düşman/oyuncu yok)
        foreach (var off in offsets)
        {
            Vector3Int neighbor = cell + off;
            if (groundMap != null && groundMap.HasTile(neighbor)
                && !LevelGenerator.instance.hazardCells.Contains(neighbor)
                && !LevelGenerator.instance.scaffoldCells.Contains(neighbor)
                && !collapsingScaffolds.Contains(neighbor))
            {
                // Üstünde başka entity var mı?
                if (TurnManager.instance.IsEnemyAtCell(neighbor)) continue;
                if (TurnManager.instance.player != null && TurnManager.instance.player.GetCurrentCellPosition() == neighbor) continue;

                return neighbor;
            }
        }

        // Güvenli hücre bulunamadıysa, en azından ground olan herhangi bir komşu
        foreach (var off in offsets)
        {
            Vector3Int neighbor = cell + off;
            if (groundMap != null && groundMap.HasTile(neighbor))
                return neighbor;
        }

        return cell; // Hiç komşu yoksa yerinde kal (edge case)
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

    /// <summary>
    /// Level geçişlerinde tüm scaffold durumunu sıfırla.
    /// </summary>
    public void ClearAll()
    {
        foreach (var coroutine in shakeCoroutines.Values)
            if (coroutine != null) StopCoroutine(coroutine);
        shakeCoroutines.Clear();
        activatedScaffolds.Clear();
        collapsingScaffolds.Clear();
        StopAllCoroutines();
    }
}
