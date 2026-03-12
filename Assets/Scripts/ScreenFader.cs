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
    /*
        private void Start()
        {
            // İlk açılışta fader'ı bul ve aç
            InitializeFader();
            if (faderGroup != null) StartCoroutine(Fade(0f));
        }
    */
    private void Start()
    {
        // İlk açılışta fader'ı bul
        InitializeFader();

        if (faderGroup != null)
        {
            // Sadece oyun ilk başladığında ekranı zorla simsiyah yapıyoruz.
            faderGroup.alpha = 1f;

            // GÜVENLİK KİLİDİ: Eğer OnSceneLoaded da aynı anda tetiklendiyse
            // iki animasyon birbiriyle savaşmasın diye önce her şeyi durduruyoruz.
            StopAllCoroutines();

            // Şimdi siyahı yavaşça aydınlatıp sahneyi göster.
            StartCoroutine(Fade(0f));
        }
    }
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        InitializeFader();
        // EĞER FadeAndLoad ile gelmiyorsak (direkt Play dediysek veya başka bir geçişse)
        if (!isTransitioning)
        {
            // DİKKAT: Burada alpha = 1f YAPMIYORUZ! 
            // Titremeyi engellemek için sildiğin için o pürüzsüz vibe korundu. 
            // Ekran FadeAndLoad ile zaten siyah kalmış olduğu için buradan sakince açılacak.
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

        // Sahnenin tam oturması için 1 saniye siyah ekranda bekle
        yield return new WaitForSecondsRealtime(1f);

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

        // ÇÖZÜM 1: Sadece 1 kare değil, motorun tam uyanması için 
        // animasyona başlamadan önce çok kısa bir süre (0.1 sn) bekliyoruz.
        yield return new WaitForSecondsRealtime(0.1f); 

        float startAlpha = faderGroup.alpha;
        float elapsed = 0f;
        faderGroup.blocksRaycasts = true;

        while (elapsed < fadeDuration)
        {
            // ÇÖZÜM 2: AMORTİSÖR (Zaman Sınırlandırması)
            // Eğer oyun o an ağır bir işlem yapıp donarsa (örneğin 0.2 sn), 
            // bunu 0.033 saniye (yaklaşık 30 FPS adımı) olarak kabul et.
            // Böylece değer asla 1'den 0.8'e atlayamaz, en fazla 0.97'ye düşer ve pürüzsüz iner.
            float safeDeltaTime = Mathf.Min(Time.unscaledDeltaTime, 0.033f);
            
            elapsed += safeDeltaTime;
            
            if (faderGroup != null)
                faderGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / fadeDuration);
            
            yield return null;
        }

        if (faderGroup != null)
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
