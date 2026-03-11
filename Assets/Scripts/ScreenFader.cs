using UnityEngine;
using System.Collections;
using System;
using UnityEngine.SceneManagement;

public class ScreenFader : MonoBehaviour
{
    public static ScreenFader instance;
    public CanvasGroup faderGroup;
    public float fadeDuration = 1.0f;
    public string faderTag = "FadeCanvas";

    private bool isTransitioning = false;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        // İlk açılışta fader'ı bul ve aç
        InitializeFader();
        if (faderGroup != null) StartCoroutine(Fade(0f));
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        InitializeFader();
        // EĞER FadeAndLoad ile gelmiyorsak (direkt Play dediysek veya başka bir geçişse)
        if (!isTransitioning)
        {
            StartCoroutine(Fade(0f));
        }
    }

    void InitializeFader()
    {
        GameObject obj = GameObject.FindWithTag(faderTag);
        if (obj != null)
        {
            faderGroup = obj.GetComponent<CanvasGroup>();
            // BURADAKİ alpha = 1f SATIRINI SİLDİM!
            // Çünkü ekran zaten ya siyahtır ya da fader çalışıyordur.
            // Durduk yere 1 yaparsan flickering (parlama/kararma) yapar.
        }
    }

    public void FadeAndLoad(Action loadAction)
    {
        if (isTransitioning) return;
        StopAllCoroutines();
        StartCoroutine(FadeAndLoadSequence(loadAction));
    }

    private IEnumerator FadeAndLoadSequence(Action loadAction)
    {
        isTransitioning = true;

        // 1. Ekranı kapat
        yield return StartCoroutine(Fade(1f));

        // 2. Sahneyi yükle
        loadAction?.Invoke();

        // Sahnenin tam oturması için 1 kare bekle
        yield return null;

        // Yeni sahnedeki fader'ı bul
        InitializeFader();

        // 3. Ekranı aç
        if (faderGroup != null)
        {
            yield return StartCoroutine(Fade(0f));
        }

        isTransitioning = false;
    }

    IEnumerator Fade(float targetAlpha)
    {
        if (faderGroup == null) yield break;

        float startAlpha = faderGroup.alpha;
        float elapsed = 0f;
        faderGroup.blocksRaycasts = true;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime; 
            if(faderGroup != null)
                faderGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / fadeDuration);
            yield return null;
        }

        if(faderGroup != null)
        {
            faderGroup.alpha = targetAlpha;
            if (targetAlpha <= 0.05f) faderGroup.blocksRaycasts = false;
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}
