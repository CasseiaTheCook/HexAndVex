using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI; // Image bileşeni için
using System.Collections;

public class LogoSceneScript : MonoBehaviour
{
    [Header("Canvas Referansları")]
    public Canvas introCanvas;        // İlk goofy logonun olduğu Canvas
    public Canvas mainCanvas;         // Asıl oyun logosunun olduğu Canvas

    [Header("Görsel Elemanlar (Hierarchy'deki Objelere Göre)")]
    public RectTransform bananaTransform; // 'IntroCanvas' içindeki muz Image objesinin RectTransform'u
    public Transform explosionObject; // Hierarchy'deki 'ExplosionImage' objesinin Transform'u

    [Header("Ses ve Müzik")]
    public AudioSource sfxSource;
    public AudioSource musicSource;
    public AudioClip goofyRunSound;
    public AudioClip explosionSound;

    [Header("Ayarlar")]
    public float rotationSpeed = 1500f; // Muzun dönme hızı
    public float explosionScaleUpDuration = 0.2f; // Patlamanın büyüme süresi
    public float explosionScaleDownDuration = 0.3f; // Patlamanın küçülme ve yok olma süresi
    public float maxExplosionScale = 5.0f; // Patlamanın ulaşacağı maksimum büyüklük
    public float mainLogoDisplayTime = 2f; // Asıl logo ekranda kaç sn duracak?

    private bool isRotating = false;

    void Start()
    {
        // Başlangıç kurulumu: Sadece intro canvas açık
        if (introCanvas != null) introCanvas.enabled = true;
        if (mainCanvas != null) mainCanvas.enabled = false;
        
        // Patlama objesini başlangıçta gizle ve scale'ini 0 yap
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
        // 1. Ekranın açılmasını bekle
        yield return new WaitForSeconds(0.5f);

        // 2. MUZUN DÖNME VE KOŞMA SESİ AŞAMASI
        if (goofyRunSound != null)
        {
            isRotating = true;
            sfxSource.PlayOneShot(goofyRunSound);
            yield return new WaitForSeconds(goofyRunSound.length);
        }

        // 3. PATLAMA VE LOGO REVEAL AŞAMASI
        isRotating = false;
        
        // Patlama başlayınca muz objesini gizle
        if (bananaTransform != null) bananaTransform.gameObject.SetActive(false); 

        if (explosionObject != null)
        {
            // Patlama objesini göster ve sesi çal
            explosionObject.gameObject.SetActive(true);
            if (explosionSound != null) sfxSource.PlayOneShot(explosionSound);

            // a. Patlamayı hızla büyüt
            yield return StartCoroutine(ScaleObject(explosionObject, Vector3.zero, Vector3.one * maxExplosionScale, explosionScaleUpDuration));
            
            // b. TAM PATLAMA BÜYÜDÜĞÜNDE: CANVAS DEĞİŞİMİ
            if (introCanvas != null) introCanvas.enabled = false;
            if (mainCanvas != null) mainCanvas.enabled = true;
            
            // c. Müziği başlat
            if (musicSource != null) musicSource.Play();

            // d. Patlamayı küçültüp yok et
            yield return StartCoroutine(ScaleObject(explosionObject, Vector3.one * maxExplosionScale, Vector3.zero, explosionScaleDownDuration));
            
            explosionObject.gameObject.SetActive(false); // Patlamayı temizle
        }

        // 4. ASIL LOGO EKRANDA DURMA SÜRESİ
        yield return new WaitForSeconds(mainLogoDisplayTime);

        // 5. SAHNE GEÇİŞİ (Build index + 1)
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

    // Objeyi belirli bir sürede scale etmek için yardımcı coroutine
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
