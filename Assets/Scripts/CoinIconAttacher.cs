using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// TMP_Text objesine eklenir, otomatik olarak metnin soluna coin ikonu koyar.
/// Sprite Inspector'dan atanır (VFX/Coin.aseprite).
/// </summary>
[RequireComponent(typeof(TMP_Text))]
public class CoinIconAttacher : MonoBehaviour
{
    public Sprite coinSprite;
    public float iconSize = 20f;
    public float spacing = 4f;

    private Image coinImage;

    void Start()
    {
        Setup();
    }

    public void Setup()
    {
        if (coinSprite == null || coinImage != null) return;

        Transform parent = transform.parent;
        if (parent == null) return;

        // Eğer parent'ta HorizontalLayoutGroup yoksa ekle
        HorizontalLayoutGroup hlg = parent.GetComponent<HorizontalLayoutGroup>();
        if (hlg == null)
        {
            hlg = parent.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = spacing;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
        }

        // Coin icon objesi oluştur
        GameObject iconGO = new GameObject("CoinIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        iconGO.transform.SetParent(parent, false);
        iconGO.transform.SetAsFirstSibling(); // Text'in soluna

        RectTransform iconRT = iconGO.GetComponent<RectTransform>();
        iconRT.sizeDelta = new Vector2(iconSize, iconSize);

        LayoutElement le = iconGO.AddComponent<LayoutElement>();
        le.preferredWidth = iconSize;
        le.preferredHeight = iconSize;

        coinImage = iconGO.GetComponent<Image>();
        coinImage.sprite = coinSprite;
        coinImage.preserveAspect = true;
        coinImage.raycastTarget = false;
    }
}
