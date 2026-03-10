using UnityEngine;
using UnityEngine.UI;

public class GifPlayer : MonoBehaviour
{
    [Header("Ayarlar")]
    public Sprite[] kareler;      // Buraya tüm resimleri sırayla sürükle
    public float fps = 10f;       // Saniyede kaç kare dönecek?
    public bool donguOlsun = true; 

    private Image panelResmi;
    private int aktifKareIndex;
    private float zamanlayici;

    void Start()
    {
        panelResmi = GetComponent<Image>();
    }

    void Update()
    {
        if (kareler.Length == 0) return;

        zamanlayici += Time.deltaTime;

        // Bir sonraki kareye geçme zamanı geldi mi?
        if (zamanlayici >= (1f / fps))
        {
            zamanlayici = 0f;
            aktifKareIndex++;

            // Liste biterse başa dön
            if (aktifKareIndex >= kareler.Length)
            {
                if (donguOlsun)
                    aktifKareIndex = 0;
                else
                    aktifKareIndex = kareler.Length - 1;
            }

            // Resmi güncelle
            panelResmi.sprite = kareler[aktifKareIndex];
        }
    }
}
