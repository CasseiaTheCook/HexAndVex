using UnityEngine;
using System.Collections;
using System;
using UnityEngine.SceneManagement;

public class ScreenFader : MonoBehaviour
{
    public static ScreenFader instance;
    public CanvasGroup faderGroup;
    public float fadeDuration = 1.0f;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // İlk açılışta ekran simsiyah olsun
        if (faderGroup != null)
        {
            faderGroup.alpha = 1f;
            faderGroup.blocksRaycasts = true;
        }
    }

    private void Start()
    {
        // Sahne ilk yüklendiğinde otomatik olarak ekranı aç (Fade Out)
        StartCoroutine(Fade(0f));
    }

    // Sahne geçişleri için dışarıdan bunu çağıracaksın
    public void FadeAndLoad(Action loadAction)
    {
        StopAllCoroutines();
        StartCoroutine(FadeAndLoadSequence(loadAction));
    }

    private IEnumerator FadeAndLoadSequence(Action loadAction)
    {
        // 1. Ekranı karart (siyaha kadar)
        yield return StartCoroutine(Fade(1f));

        // 2. TAM SİYAH KALACAK: 1 saniye kal (sahne yükleme sırasında siyah olsun)
        yield return new WaitForSecondsRealtime(1f);

        // 3. Sahne yükleme işini yap (Action çalıştır) - SİYAH OLDUĞU SIRADA
        loadAction?.Invoke();

        // 4. Yeni sahnede ekranı geri aç
        yield return StartCoroutine(Fade(0f));
    }

    // TAM SENİN İSTEDİĞİN LERP MANTIĞI
    IEnumerator Fade(float targetAlpha)
    {
        if (faderGroup == null) yield break;

        float startAlpha = faderGroup.alpha;
        float elapsed = 0f;

        // Kararma başladığında tıklamaları engelle
        faderGroup.blocksRaycasts = true;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime; // Pause olsa bile çalışması için unscaled
            faderGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / fadeDuration);
            yield return null;
        }

        faderGroup.alpha = targetAlpha;

        // Eğer ekran tamamen açıldıysa (0), arkadaki butonlara tıklanabilsin
        if (targetAlpha <= 0.05f)
        {
            faderGroup.blocksRaycasts = false;
        }
    }
}
