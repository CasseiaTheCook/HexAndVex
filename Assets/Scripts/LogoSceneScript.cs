using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class LogoSceneScript : MonoBehaviour
{
    [Header("Canvas Referansları")]
    public Canvas introCanvas;        
    public Canvas mainCanvas;         

    [Header("Görsel Elemanlar")]
    public RectTransform bananaTransform; 
    public Transform explosionObject; 

    [Header("Ses ve Müzik")]
    public AudioSource sfxSource;   // Koşma ve Patlama sesleri için
    public AudioSource musicSource; // Asıl logonun müziği için
    public AudioClip goofyRunSound;
    public AudioClip explosionSound;

    [Header("Ayarlar")]
    public float rotationSpeed = 1500f; 
    public float explosionScaleUpDuration = 0.2f; 
    public float explosionScaleDownDuration = 0.3f; 
    public float maxExplosionScale = 5.0f; 

    private bool isRotating = false;

    void Start()
    {
        // Başlangıç kurulumu
        if (introCanvas != null) introCanvas.enabled = true;
        if (mainCanvas != null) mainCanvas.enabled = false;
        
        if (explosionObject != null)
        {
            explosionObject.gameObject.SetActive(false);
            explosionObject.localScale = Vector3.zero;
        }

        StartCoroutine(SplashSequence());
    }

    void Update()
    {
        if (isRotating && bananaTransform != null)
        {
            bananaTransform.Rotate(Vector3.forward * rotationSpeed * Time.deltaTime);
        }
    }

    IEnumerator SplashSequence()
    {
        yield return new WaitForSeconds(0.5f);

        // 1. MUZUN DÖNME VE KOŞMA SESİ
        if (goofyRunSound != null)
        {
            isRotating = true;
            sfxSource.PlayOneShot(goofyRunSound);
            yield return new WaitForSeconds(goofyRunSound.length);
        }

        // 2. PATLAMA BAŞLANGICI
        isRotating = false;
        if (bananaTransform != null) bananaTransform.gameObject.SetActive(false); 

        if (explosionObject != null)
        {
            explosionObject.gameObject.SetActive(true);
            if (explosionSound != null) sfxSource.PlayOneShot(explosionSound);

            // Patlama en büyük haline gelirken büyüme başlar
            yield return StartCoroutine(ScaleObject(explosionObject, Vector3.zero, Vector3.one * maxExplosionScale, explosionScaleUpDuration));
            
            // 3. LOGO EKRANI GELDİĞİ AN (TAM PATLAMA ANI)
            if (introCanvas != null) introCanvas.enabled = false;
            if (mainCanvas != null) mainCanvas.enabled = true;
            
            // "Müzik 0.2 saniye daha çalıp duracak"
            // Burada patlama sesinin kuyruğu veya SFX kanalındaki ses 0.2 saniye daha çalar
            yield return new WaitForSeconds(0.2f);
            sfxSource.Stop(); // Önceki tüm goofy sesleri ve patlama kuyruğunu aniden keser

            // "Logonun kendi müziği çalmaya başlayacak"
            if (musicSource != null) musicSource.Play();

            // Patlamayı küçültüp yok et (Görsel temizlik)
            yield return StartCoroutine(ScaleObject(explosionObject, Vector3.one * maxExplosionScale, Vector3.zero, explosionScaleDownDuration));
            explosionObject.gameObject.SetActive(false); 
        }

        // 4. MÜZİK BİTİNCE SAHNE DEĞİŞECEK
        if (musicSource != null && musicSource.clip != null)
        {
            // Müzik çalmaya devam ettiği sürece burada bekle (Goofy müzik bitene kadar)
            yield return new WaitWhile(() => musicSource.isPlaying);
        }
        else
        {
            // Eğer müzik yoksa güvenlik amacıyla 1 saniye bekle
            yield return new WaitForSeconds(1f);
        }

        // 5. OTOMATİK SONRAKİ SAHNEYE GEÇİŞ (Build index + 1)
        int nextSceneIndex = SceneManager.GetActiveScene().buildIndex + 1;
        
        if (ScreenFader.instance != null)
        {
            ScreenFader.instance.FadeAndLoad(() => {
                SceneManager.LoadScene(nextSceneIndex);
            });
        }
        else
        {
            SceneManager.LoadScene(nextSceneIndex);
        }
    }

    IEnumerator ScaleObject(Transform target, Vector3 startScale, Vector3 endScale, float duration)
    {
        float elapsed = 0f;
        target.localScale = startScale;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            target.localScale = Vector3.Lerp(startScale, endScale, elapsed / duration);
            yield return null;
        }
        target.localScale = endScale;
    }
}
