using UnityEngine;

public class ParallaksScript : MonoBehaviour
{
    [Header("Ayarlar")]
    public float hareketMiktari = 20f; // Objenin ne kadar kayacağı
    public float yumusaklik = 5f;    // Hareketin ne kadar akıcı olacağı

    private Vector3 baslangicPozisyonu;
    private RectTransform rectTransform;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        baslangicPozisyonu = rectTransform.anchoredPosition;
    }

    void Update()
    {
        // Farenin ekran üzerindeki yerini alıyoruz (-1 ile 1 arasına çekiyoruz)
        float fareX = (Input.mousePosition.x - (Screen.width / 2)) / (Screen.width / 2);
        float fareY = (Input.mousePosition.y - (Screen.height / 2)) / (Screen.height / 2);

        // Hedef pozisyonu hesapla
        Vector3 hedefPoz = new Vector3(
            baslangicPozisyonu.x + (fareX * hareketMiktari),
            baslangicPozisyonu.y + (fareY * hareketMiktari),
            0
        );

        // Objenin pozisyonunu hedef pozisyona doğru yumuşakça kaydır
        rectTransform.anchoredPosition = Vector3.Lerp(rectTransform.anchoredPosition, hedefPoz, Time.deltaTime * yumusaklik);
    }
}
