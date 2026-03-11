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

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            // Sahne yüklenmesini dinlemeye başla
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Editörde direkt bu sahneyi başlatırsan çalışması için
        FindAndFadeOut();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Sahne değiştikçe yeni Canvas'ı bul ve aç
        FindAndFadeOut();
    }

    void FindAndFadeOut()
    {
        GameObject obj = GameObject.FindWithTag(faderTag);
        if (obj != null)
        {
            faderGroup = obj.GetComponent<CanvasGroup>();
            // Ekranı önce tam siyah yap, sonra açılış başlasın
            faderGroup.alpha = 1f; 
            StopAllCoroutines();
            StartCoroutine(Fade(0f));
        }
    }

    public void FadeAndLoad(Action loadAction)
    {
        StopAllCoroutines();
        StartCoroutine(FadeAndLoadSequence(loadAction));
    }

    private IEnumerator FadeAndLoadSequence(Action loadAction)
    {
        // Önce ekranı kapat (Siyah yap)
        yield return StartCoroutine(Fade(1f));
        // Sahneyi yükle
        loadAction?.Invoke();
        // Yeni sahne yüklendiğinde OnSceneLoaded otomatik olarak açacak
    }

    // TAM SENİN İSTEDİĞİN LERP DÖNGÜSÜ
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
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}
