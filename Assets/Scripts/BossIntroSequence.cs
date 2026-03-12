using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Boss odası yüklenince çalışır:
/// 1. Input kilitlenir
/// 2. Kamera boss'a zoom'lanır
/// 3. Boss üzerinde popup çıkar
/// 4. Ekran shake
/// 5. Kamera geri çekilir, input açılır
/// </summary>
public class BossIntroSequence : MonoBehaviour
{
    public static BossIntroSequence instance;

    [Header("Popup — Metin")]
    public string roarText          = "ROOAAAR!!";
    public float  popupFontSize     = 44f;
    public Color  popupTextColor    = Color.white;
    public Color  popupBgColor      = Color.black;
    public Color  popupOutlineColor = Color.white;
    public float  popupOutlineSize  = 3f;
    public Vector2 popupSize        = new Vector2(340f, 90f);
    public float  popupOffsetY      = 80f;   // Boss'un ne kadar üstünde çıksın

    [Header("Popup — Animasyon")]
    public float popupFadeOutDuration = 0.3f;
    public float popupPopInDuration   = 0.2f;
    public float popupPopInOvershoot  = 1.12f;

    [Header("Kamera")]
    public float zoomTargetZ      = -6f;
    public float zoomDuration     = 0.5f;
    public float holdDuration     = 2.0f;   // Popup göründükten sonra bekle
    public float zoomBackDuration = 0.4f;
    public float spawnDelay       = 0.4f;   // FadeSpawn bitmesi için bekleme

    [Header("Vacuum Ring Efekti")]
    public GameObject vacuumRingPrefab; // Inspector'dan vacuum prefab'ını ata
    public float ringInterval     = 0.5f;  // Her kaç saniyede bir ring gelsin
    public float ringStartScale   = 0.2f;  // Başlangıç boyutu
    public float ringEndScale     = 3.0f;  // Bitiş boyutu
    public float ringDuration     = 0.45f; // Her ring'in expand+fade süresi

    [Header("Shake")]
    public float shakeDuration  = 0.6f;
    public float shakeMagnitude = 0.18f;

    [Header("Opsiyonel Prefab")]
    public GameObject roarPopupPrefab; // Dolu ise bu kullanılır, boş ise runtime'da oluşturulur

    private Camera           cam;
    private CameraController camController;

    void Awake() { instance = this; }

    void Start()
    {
        cam           = Camera.main;
        camController = FindFirstObjectByType<CameraController>();
    }

    public void PlayIntro(EnemyAI boss)
    {
        StartCoroutine(IntroRoutine(boss));
    }

    private IEnumerator IntroRoutine(EnemyAI boss)
    {
        if (cam == null)           cam           = Camera.main;
        if (camController == null) camController = FindFirstObjectByType<CameraController>();
        if (cam == null)           yield break;

        if (TurnManager.instance != null) TurnManager.instance.isPlayerTurn = false;
        if (camController != null)        camController.enabled = false;

        Vector3 camStart   = cam.transform.position;
        Vector3 bossPos    = boss.transform.position;
        Vector3 camBossPos = new Vector3(bossPos.x, bossPos.y, zoomTargetZ);

        // 1. ZOOM IN + Ring döngüsü başlat
        
        Coroutine ringLoop = StartCoroutine(RingLoopRoutine(boss.transform.position));

        float elapsed = 0f;
        while (elapsed < zoomDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / zoomDuration);
            cam.transform.position = Vector3.Lerp(camStart, camBossPos, t);
            yield return null;
        }
        cam.transform.position = camBossPos;

        // 2. ROAR SES + POPUP
        if (AudioManager.instance != null) AudioManager.instance.PlayBossRoar();
        GameObject popup = SpawnRoarPopup(boss.transform.position);

        // 3. SHAKE
        yield return StartCoroutine(ShakeRoutine(shakeDuration, shakeMagnitude));

        // 4. HOLD
        float holdLeft = holdDuration - shakeDuration;
        if (holdLeft > 0f) yield return new WaitForSeconds(holdLeft);

        StopCoroutine(ringLoop);

        // 5. POPUP FADE OUT
        if (popup != null) StartCoroutine(FadeOutPopup(popup, popupFadeOutDuration));

        // 6. ZOOM BACK
        Vector3 camZoomedPos = cam.transform.position;
        elapsed = 0f;
        while (elapsed < zoomBackDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / zoomBackDuration);
            cam.transform.position = Vector3.Lerp(camZoomedPos, camStart, t);
            yield return null;
        }
        cam.transform.position = camStart;

        // 7. OYUNU AÇ
        if (camController != null)        camController.enabled = true;
        if (TurnManager.instance != null) TurnManager.instance.isPlayerTurn = true;
        if (TurnManager.instance != null) TurnManager.instance.player?.UpdateHighlights();
    }

    // ── Shake ───────────────────────────────────────────────────────────────
    private IEnumerator ShakeRoutine(float duration, float magnitude)
    {
        Vector3 origin  = cam.transform.position;
        float   elapsed = 0f;
        while (elapsed < duration)
        {
            float   strength = Mathf.Lerp(magnitude, 0f, elapsed / duration);
            Vector3 offset   = new Vector3(Random.Range(-1f, 1f) * strength,
                                           Random.Range(-1f, 1f) * strength, 0f);
            cam.transform.position = new Vector3(origin.x + offset.x,
                                                 origin.y + offset.y,
                                                 origin.z);
            elapsed += Time.deltaTime;
            yield return null;
        }
        cam.transform.position = origin;
    }

    // ── Roar Popup ──────────────────────────────────────────────────────────
    private GameObject SpawnRoarPopup(Vector3 worldPos)
    {
        if (roarPopupPrefab != null)
            return Instantiate(roarPopupPrefab, worldPos + Vector3.up * 0.8f, Quaternion.identity);

        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return null;

        // Container
        GameObject container = new GameObject("BossRoarPopup", typeof(RectTransform), typeof(CanvasGroup));
        container.transform.SetParent(canvas.transform, false);

        Vector2 screenPos = cam.WorldToScreenPoint(worldPos + Vector3.up * 0.8f);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.GetComponent<RectTransform>(), screenPos,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : cam,
            out Vector2 localPos);

        RectTransform rt = container.GetComponent<RectTransform>();
        rt.localPosition = new Vector3(localPos.x, localPos.y + popupOffsetY, 0f);
        rt.sizeDelta     = popupSize;

        // Arka plan
        GameObject bg    = new GameObject("BG", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(container.transform, false);
        var bgRt         = bg.GetComponent<RectTransform>();
        bgRt.anchorMin   = Vector2.zero; bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin   = bgRt.offsetMax = Vector2.zero;
        bg.GetComponent<Image>().color = popupBgColor;

        var outline             = bg.AddComponent<UnityEngine.UI.Outline>();
        outline.effectColor     = popupOutlineColor;
        outline.effectDistance  = new Vector2(popupOutlineSize, -popupOutlineSize);

        // Metin
        GameObject textGo  = new GameObject("Text", typeof(RectTransform));
        textGo.transform.SetParent(container.transform, false);
        var textRt         = textGo.GetComponent<RectTransform>();
        textRt.anchorMin   = Vector2.zero; textRt.anchorMax = Vector2.one;
        textRt.offsetMin   = new Vector2(12f, 0f);
        textRt.offsetMax   = new Vector2(-12f, 0f);
        var tmp            = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text           = roarText;
        tmp.fontSize       = popupFontSize;
        tmp.fontStyle      = FontStyles.Bold;
        tmp.color          = popupTextColor;
        tmp.alignment      = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = false;

        StartCoroutine(PopIn(container.transform));
        return container;
    }

    private IEnumerator PopIn(Transform t)
    {
        float dur = popupPopInDuration;
        float elapsed = 0f;
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float s = Mathf.LerpUnclamped(0f, popupPopInOvershoot, elapsed / dur);
            t.localScale = new Vector3(s, s, 1f);
            yield return null;
        }
        dur = dur * 0.5f; elapsed = 0f;
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float s = Mathf.Lerp(popupPopInOvershoot, 1f, elapsed / dur);
            t.localScale = new Vector3(s, s, 1f);
            yield return null;
        }
        t.localScale = Vector3.one;
    }

    // ── Vacuum Ring Loop ─────────────────────────────────────────────────────
    private IEnumerator RingLoopRoutine(Vector3 center)
    {
        while (true)
        {
            if (vacuumRingPrefab != null)
                StartCoroutine(SpawnRingCoroutine(center));
            yield return new WaitForSeconds(ringInterval);
        }
    }

    private IEnumerator SpawnRingCoroutine(Vector3 center)
    {
        if (vacuumRingPrefab == null) yield break;

        GameObject ring = Instantiate(vacuumRingPrefab, center, Quaternion.identity);
        SpriteRenderer[] renderers = ring.GetComponentsInChildren<SpriteRenderer>();

        float elapsed = 0f;
        while (elapsed < ringDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / ringDuration;
            float scale = Mathf.Lerp(ringStartScale, ringEndScale, t);
            ring.transform.localScale = new Vector3(scale, scale, 1f);

            float alpha = Mathf.Lerp(1f, 0f, t);
            foreach (var sr in renderers)
            {
                Color c = sr.color;
                c.a = alpha;
                sr.color = c;
            }
            yield return null;
        }
        Destroy(ring);
    }

    private IEnumerator FadeOutPopup(GameObject popup, float duration)
    {
        CanvasGroup cg = popup.GetComponent<CanvasGroup>();
        if (cg == null) { Destroy(popup); yield break; }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed   += Time.deltaTime;
            cg.alpha   = Mathf.Lerp(1f, 0f, elapsed / duration);
            yield return null;
        }
        Destroy(popup);
    }
}
