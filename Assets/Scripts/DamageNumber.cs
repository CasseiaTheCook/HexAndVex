using TMPro;
using UnityEngine;

public class DamageNumber : MonoBehaviour
{
    private TextMeshPro textMesh;
    private float disappearTimer = 0.8f;
    private Color textColor;
    private Vector3 moveVector;

    public void Setup(int damageAmount)
    {
        textMesh = GetComponent<TextMeshPro>();
        textMesh.text = damageAmount.ToString();
        textColor = textMesh.color;

        // --- DAİRESEL FIRLAMA MANTIĞI ---
        // Rastgele bir açı seçiyoruz (0 ile 360 derece arası)
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        
        // Bu açıyı bir yöne çeviriyoruz (x = cos, y = sin)
        float force = 2.5f; // Sayının fırlama hızı
        moveVector = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * force;
        
        // Başlangıçta hafif büyük görünsün (Pop-up etkisi)
        transform.localScale = Vector3.one * 1.5f;
    }

    private void Update()
    {
        // 1. Sayıyı fırlat ve zamanla yavaşlat
        transform.position += moveVector * Time.deltaTime;
        moveVector -= moveVector * 8f * Time.deltaTime; 

        // 2. Boyutu yavaşça normale döndür
        if (transform.localScale.x > 1f)
        {
            transform.localScale -= Vector3.one * 2f * Time.deltaTime;
        }

        // 3. Kaybolma ve şeffaflaşma
        disappearTimer -= Time.deltaTime;
        if (disappearTimer < 0)
        {
            textColor.a -= 4f * Time.deltaTime;
            textMesh.color = textColor;
            if (textColor.a <= 0) Destroy(gameObject);
        }
    }
}
