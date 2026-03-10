using UnityEngine;

/// <summary>
/// NecroShot hedefleme modunda özel cursor gösterir.
/// cursorTexture alanına istediğiniz PNG'yi atın, hotspot otomatik ortalanır.
/// </summary>
public class NecroShotCursor : MonoBehaviour
{
    public static NecroShotCursor instance;

    [Header("Cursor Görseli (PNG atın)")]
    [Tooltip("Hedefleme sırasında görünecek ikon. İstediğiniz zaman değiştirin.")]
    public Texture2D cursorTexture;

    [Header("Hotspot")]
    [Tooltip("Cursor'un tıklama noktası. (0,0) sol üst köşe. Boş bırakırsanız otomatik ortalanır.")]
    public Vector2 hotspotOverride = Vector2.zero;
    public bool autoCenter = true;

    private bool isCustomCursorActive = false;

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    void Update()
    {
        bool shouldBeActive = TurnManager.instance != null && TurnManager.instance.isNecroShotTargeting;

        if (shouldBeActive && !isCustomCursorActive)
            ActivateCursor();
        else if (!shouldBeActive && isCustomCursorActive)
            DeactivateCursor();
    }

    private void ActivateCursor()
    {
        if (cursorTexture == null) return;
        Vector2 hotspot = autoCenter ? new Vector2(cursorTexture.width / 2f, cursorTexture.height / 2f) : hotspotOverride;
        Cursor.SetCursor(cursorTexture, hotspot, CursorMode.Auto);
        isCustomCursorActive = true;
    }

    private void DeactivateCursor()
    {
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        isCustomCursorActive = false;
    }

    void OnDisable()
    {
        if (isCustomCursorActive)
            DeactivateCursor();
    }
}
