using UnityEngine;
using TMPro; // TextMeshPro için bu satır şart!

public class RGBText : MonoBehaviour
{
    public float hiz = 2.0f;        // Renklerin akış hızı
    public float dalgaAraligi = 0.2f; // Harfler arası renk farkı
    
    private TMP_Text textBileseni;
    private Mesh mesh;
    private Vector3[] vertices;
    private Color32[] colors;

    void Start()
    {
        textBileseni = GetComponent<TMP_Text>();
    }

    void Update()
    {
        // Yazının mesh verilerini güncelle (Her karede harflere ulaşmak için)
        textBileseni.ForceMeshUpdate();
        mesh = textBileseni.mesh;
        vertices = mesh.vertices;
        colors = new Color32[vertices.Length];

        // Harf bilgilerini alıyoruz
        TMP_TextInfo textInfo = textBileseni.textInfo;

        for (int i = 0; i < textInfo.characterCount; i++)
        {
            TMP_CharacterInfo charInfo = textInfo.characterInfo[i];

            // Eğer karakter boşluksa veya görünmezse atla
            if (!charInfo.isVisible) continue;

            // Her harf 4 köşeden (vertex) oluşur
            int vertexIndex = charInfo.vertexIndex;

            // Harfin pozisyonuna göre bir renk zamanlaması oluştur
            float offset = charInfo.origin * dalgaAraligi;
            float hue = Mathf.Repeat(Time.time * hiz + offset, 1f);
            Color32 yeniRenk = Color.HSVToRGB(hue, 1f, 1f);

            // Harfin 4 köşesini de aynı renge boya
            colors[vertexIndex + 0] = yeniRenk;
            colors[vertexIndex + 1] = yeniRenk;
            colors[vertexIndex + 2] = yeniRenk;
            colors[vertexIndex + 3] = yeniRenk;
        }

        // Yeni renkleri yazıya uygula
        mesh.colors32 = colors;
        textBileseni.canvasRenderer.SetMesh(mesh);
    }
}
