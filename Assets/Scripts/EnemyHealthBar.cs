using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Düşmanların üstünde can barı gösterir.
/// Canvas + UI hierarchy prefab'a Editor Tool ile eklenir, runtime'da oluşturulmaz.
/// Souls tarzı gölge (trail) bar: hasar yendiğinde gri bar yavaşça asıl barı takip eder.
/// </summary>
public class EnemyHealthBar : MonoBehaviour
{
    [Header("Referanslar (Editor Tool tarafından atanır)")]
    public Canvas barCanvas;
    public Image bgImage;
    public Image trailImage;
    public Image fillImage;
    public TextMeshProUGUI hpLabel;

    [Header("Boyut ve Pozisyon")]
    public float barWidth = 0.55f;
    public float barHeight = 0.1f;
    public float offsetY = 0.18f;

    [Header("Renkler")]
    public Color fullColor = new Color(0.2f, 0.8f, 0.2f, 1f);
    public Color midColor = new Color(1f, 0.8f, 0f, 1f);
    public Color lowColor = new Color(0.8f, 0.1f, 0.1f, 1f);

    [Header("Trail Ayarları")]
    public Color trailColor = new Color(0.6f, 0.6f, 0.6f, 0.8f);
    public float trailSpeed = 1.5f;
    public float trailDelay = 0.4f;

    [Header("Eski HP Yazısını Gizle")]
    public bool hideOriginalHPText = true;

    private HealthScript health;
    private RectTransform fillRT;
    private RectTransform trailRT;
    private float trailRatio = 1f;
    private float targetRatio = 1f;
    private float trailTimer = 0f;

    void Start()
    {
        health = GetComponent<HealthScript>();
        if (health == null || fillImage == null) { enabled = false; return; }

        fillRT = fillImage.GetComponent<RectTransform>();
        if (trailImage != null) trailRT = trailImage.GetComponent<RectTransform>();

        // Inspector'daki boyut/pozisyon değerlerini Canvas RectTransform'a uygula
        if (barCanvas != null)
        {
            RectTransform crt = barCanvas.GetComponent<RectTransform>();
            crt.sizeDelta = new Vector2(barWidth, barHeight);
            crt.localPosition = new Vector3(0f, offsetY, 0f);
        }

        // Trail rengini uygula
        if (trailImage != null) trailImage.color = trailColor;

        // Eski HP text'ini gizle
        if (hideOriginalHPText && health.hptext != null)
            health.hptext.gameObject.SetActive(false);

        trailRatio = 1f;
        targetRatio = 1f;
        UpdateBar();
    }

    void Update()
    {
        if (trailRT == null) return;

        // Trail delay bittiyse yavaşça takip et
        if (trailTimer > 0f)
        {
            trailTimer -= Time.deltaTime;
            return;
        }

        if (trailRatio > targetRatio)
        {
            trailRatio = Mathf.MoveTowards(trailRatio, targetRatio, trailSpeed * Time.deltaTime);
            trailRT.anchorMax = new Vector2(trailRatio, 1f);
        }
    }

    public void UpdateBar()
    {
        if (health == null || fillRT == null) return;

        float ratio = Mathf.Clamp01((float)health.currentHP / health.maxHP);

        // Trail: yeni ratio eskisinden düşükse delay başlat
        if (ratio < targetRatio)
            trailTimer = trailDelay;

        targetRatio = ratio;

        // Asıl fill barını hemen güncelle
        fillRT.anchorMax = new Vector2(ratio, 1f);

        // Renk gradyanı: full → mid → low
        if (ratio > 0.5f)
            fillImage.color = Color.Lerp(midColor, fullColor, (ratio - 0.5f) * 2f);
        else
            fillImage.color = Color.Lerp(lowColor, midColor, ratio * 2f);

        if (hpLabel != null)
            hpLabel.text = $"{health.currentHP}/{health.maxHP}";
    }

    public void SetSortingOrder(int order)
    {
        if (barCanvas != null) barCanvas.sortingOrder = order;
    }
}
