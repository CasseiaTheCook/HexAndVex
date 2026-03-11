using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class LogoSceneScript : MonoBehaviour
{
    [Header("Fader (Kararma/Açılma)")]
    public CanvasGroup faderGroup;     // Siyah ekranı kontrol eden CanvasGroup
    public float fadeDuration = 1.0f;  // Kararma hızı

    [Header("Canvas Referansları")]
    public Canvas introCanvas;
    public Canvas mainCanvas;

    [Header("Görsel Elemanlar")]
    public RectTransform bananaTransform;
    public Transform explosionObject;

    [Header("Ses ve Müzik")]
    public AudioSource sfxSource;
    public AudioSource musicSource;
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
        // 1. BAŞLANGIÇ DURUMU (Her şey karanlık ve hazır)
        if (faderGroup != null) faderGroup.alpha = 1f; // Ekran simsiyah başlasın

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
        yield return new WaitForSeconds(3f);
        // 3. MUZUN DÖNME VE KOŞMA SESİ (Fade bittikten sonra başlar)
        if (goofyRunSound != null)
        {
            isRotating = true;
            sfxSource.PlayOneShot(goofyRunSound);
            yield return new WaitForSeconds(goofyRunSound.length);
        }

        // 4. PATLAMA BAŞLANGICI
        isRotating = false;
        if (bananaTransform != null) bananaTransform.gameObject.SetActive(false);

        if (explosionObject != null)
        {
            explosionObject.gameObject.SetActive(true);
            if (explosionSound != null) sfxSource.PlayOneShot(explosionSound);

            yield return StartCoroutine(ScaleObject(explosionObject, Vector3.zero, Vector3.one * maxExplosionScale, explosionScaleUpDuration));

            // LOGO EKRANI GELDİĞİ AN
            if (introCanvas != null) introCanvas.enabled = false;
            if (mainCanvas != null) mainCanvas.enabled = true;

            yield return new WaitForSeconds(0.2f);
            sfxSource.Stop();

            if (musicSource != null) musicSource.Play();

            yield return StartCoroutine(ScaleObject(explosionObject, Vector3.one * maxExplosionScale, Vector3.zero, explosionScaleDownDuration));
            explosionObject.gameObject.SetActive(false);
        }

        // 5. MÜZİĞİN BİTMESİNİ BEKLE
        if (musicSource != null && musicSource.clip != null)
        {
            yield return new WaitWhile(() => musicSource.isPlaying);
        }
        else
        {
            yield return new WaitForSeconds(1f);
        }

        // 6. FADE IN (Ekran Siyaha Gömülüyor)
        ScreenFader.instance.FadeAndLoad(() =>
        {
            SceneManager.LoadScene(1);
        });

        // 7. OTOMATİK SAHNE GEÇİŞİ
    }

    // Kararma ve Açılma için yardımcı coroutine

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
