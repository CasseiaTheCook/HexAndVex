using UnityEngine;
using System.Collections;
using System;

public class ScreenFader : MonoBehaviour
{
    public static ScreenFader instance;
    public CanvasGroup fadeGroup;
    public float fadeDuration = 0.5f;

    void Awake()
    {
        if (instance == null) 
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        if (fadeGroup != null)
        {
            fadeGroup.alpha = 1f; 
            fadeGroup.blocksRaycasts = true; 
        }
    }

    IEnumerator Start()
    {
        fadeGroup.alpha = 1f; 
        // Zaman dursa bile çalışsın diye Realtime kullanıyoruz!
        yield return new WaitForSecondsRealtime(0.4f); 
        FadeToClear();
    }

    public void FadeToClear()
    {
        StopAllCoroutines(); 
        StartCoroutine(FadeRoutine(fadeGroup.alpha, 0f, null));
    }

    public void FadeAndLoad(Action loadAction)
    {
        StopAllCoroutines(); 
        StartCoroutine(FadeAndLoadRoutine(loadAction));
    }

    private IEnumerator FadeAndLoadRoutine(Action loadAction)
    {
        fadeGroup.blocksRaycasts = true;
        float elapsed = 0f;
        float startAlpha = fadeGroup.alpha; 

        while (elapsed < fadeDuration)
        {
            // TimeScale 0 olsa bile çalışır
            elapsed += Time.unscaledDeltaTime;
            fadeGroup.alpha = Mathf.Lerp(startAlpha, 1f, elapsed / fadeDuration);
            yield return null;
        }
        fadeGroup.alpha = 1f; 

        // EKRAN SİMSİYAHKEN YENİ BÖLÜMÜ YÜKLE
        loadAction?.Invoke();
        
        // Bölüm yüklendikten sonra yine garanti bekleme (Harita dizilsin diye)
        yield return new WaitForSecondsRealtime(0.4f); 

        elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            fadeGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            yield return null;
        }
        fadeGroup.alpha = 0f; 
        fadeGroup.blocksRaycasts = false; 
    }

    private IEnumerator FadeRoutine(float startAlpha, float endAlpha, Action onComplete)
    {
        fadeGroup.blocksRaycasts = true;
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            fadeGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, elapsed / fadeDuration);
            yield return null;
        }
        fadeGroup.alpha = endAlpha;
        if (endAlpha == 0f) fadeGroup.blocksRaycasts = false;
        
        onComplete?.Invoke();
    }
}