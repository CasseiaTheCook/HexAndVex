using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Secret Perk alındığında tetiklenen 5 fazlı sinematik animasyon.
/// Tamamen kod ile UI oluşturur — prefab/sahne kurulumu gerekmez.
/// Shopmanager.TryBuy → SecretPerkOrb.Use → bu sınıfın Play() metodu çağrılır.
/// </summary>
public class SecretPerkCinematic : MonoBehaviour
{
    public static SecretPerkCinematic instance;

    private Canvas cinematicCanvas;
    private CanvasGroup rootGroup;
    private RectTransform rootRect;

    // Faz elemanları
    private Image blackOverlay;
    private Image flashOverlay;
    private Image perkIconImage;
    private TMP_Text perkNameText;
    private TMP_Text perkSubText;

    // Glitch barları
    private Image[] glitchBars;
    private const int GLITCH_BAR_COUNT = 18;

    // Enerji çizgileri
    private Image[] energyLines;
    private const int ENERGY_LINE_COUNT = 24;

    // Parçacıklar
    private Image[] particles;
    private const int PARTICLE_COUNT = 40;
    private Vector2[] particleVelocities;
    private float[] particleLifetimes;

    private bool isPlaying = false;

    void Awake()
    {
        if (instance == null) instance = this;
        else { Destroy(gameObject); return; }

        BuildUI();
        rootGroup.alpha = 0f;
        rootGroup.gameObject.SetActive(false);
    }

    private void BuildUI()
    {
        // Ana canvas — her şeyin üstünde
        cinematicCanvas = gameObject.AddComponent<Canvas>();
        cinematicCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        cinematicCanvas.sortingOrder = 999;
        gameObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
        gameObject.AddComponent<GraphicRaycaster>();

        // Root container
        GameObject rootGO = new GameObject("CinematicRoot", typeof(RectTransform), typeof(CanvasGroup));
        rootGO.transform.SetParent(transform, false);
        rootRect = rootGO.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = rootRect.offsetMax = Vector2.zero;
        rootGroup = rootGO.GetComponent<CanvasGroup>();
        rootGroup.blocksRaycasts = true;

        // Siyah arka plan
        blackOverlay = CreateFullscreenImage(rootGO.transform, "BlackOverlay", new Color(0, 0, 0, 0.95f));

        // Glitch barları
        glitchBars = new Image[GLITCH_BAR_COUNT];
        for (int i = 0; i < GLITCH_BAR_COUNT; i++)
        {
            glitchBars[i] = CreateImage(rootGO.transform, $"GlitchBar_{i}", new Color(1f, 0.15f, 0.15f, 0.7f));
            RectTransform rt = glitchBars[i].rectTransform;
            rt.anchorMin = new Vector2(0, 0.5f);
            rt.anchorMax = new Vector2(1, 0.5f);
            rt.sizeDelta = new Vector2(0, 4f);
            rt.anchoredPosition = Vector2.zero;
            glitchBars[i].gameObject.SetActive(false);
        }

        // Enerji çizgileri (ekran kenarından merkeze)
        energyLines = new Image[ENERGY_LINE_COUNT];
        for (int i = 0; i < ENERGY_LINE_COUNT; i++)
        {
            energyLines[i] = CreateImage(rootGO.transform, $"EnergyLine_{i}", new Color(1f, 0.3f, 0.3f, 0.9f));
            RectTransform rt = energyLines[i].rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(3f, 60f);
            rt.anchoredPosition = Vector2.zero;
            energyLines[i].gameObject.SetActive(false);
        }

        // Beyaz flaş
        flashOverlay = CreateFullscreenImage(rootGO.transform, "FlashOverlay", Color.white);
        flashOverlay.gameObject.SetActive(false);

        // Perk ikonu
        GameObject iconGO = new GameObject("PerkIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        iconGO.transform.SetParent(rootGO.transform, false);
        perkIconImage = iconGO.GetComponent<Image>();
        perkIconImage.preserveAspect = true;
        perkIconImage.raycastTarget = false;
        RectTransform iconRT = iconGO.GetComponent<RectTransform>();
        iconRT.anchorMin = iconRT.anchorMax = new Vector2(0.5f, 0.55f);
        iconRT.sizeDelta = new Vector2(128f, 128f);
        iconRT.anchoredPosition = Vector2.zero;
        perkIconImage.color = new Color(1, 1, 1, 0);

        // Perk isim yazısı
        GameObject nameGO = new GameObject("PerkName", typeof(RectTransform), typeof(TextMeshProUGUI));
        nameGO.transform.SetParent(rootGO.transform, false);
        perkNameText = nameGO.GetComponent<TextMeshProUGUI>();
        perkNameText.fontSize = 42;
        perkNameText.alignment = TextAlignmentOptions.Center;
        perkNameText.color = new Color(1f, 0.27f, 0.27f, 1f); // Secret kırmızı
        perkNameText.fontStyle = FontStyles.Bold;
        perkNameText.raycastTarget = false;
        perkNameText.enableWordWrapping = false;
        RectTransform nameRT = nameGO.GetComponent<RectTransform>();
        nameRT.anchorMin = nameRT.anchorMax = new Vector2(0.5f, 0.38f);
        nameRT.sizeDelta = new Vector2(800, 60);
        nameRT.anchoredPosition = Vector2.zero;
        perkNameText.text = "";

        // Alt yazı ("SECRET MUTATION ACQUIRED")
        GameObject subGO = new GameObject("SubText", typeof(RectTransform), typeof(TextMeshProUGUI));
        subGO.transform.SetParent(rootGO.transform, false);
        perkSubText = subGO.GetComponent<TextMeshProUGUI>();
        perkSubText.fontSize = 18;
        perkSubText.alignment = TextAlignmentOptions.Center;
        perkSubText.color = new Color(0.7f, 0.7f, 0.7f, 0f);
        perkSubText.fontStyle = FontStyles.Italic;
        perkSubText.raycastTarget = false;
        perkSubText.text = "SECRET MUTATION ACQUIRED";
        RectTransform subRT = subGO.GetComponent<RectTransform>();
        subRT.anchorMin = subRT.anchorMax = new Vector2(0.5f, 0.30f);
        subRT.sizeDelta = new Vector2(600, 30);
        subRT.anchoredPosition = Vector2.zero;

        // Parçacıklar
        particles = new Image[PARTICLE_COUNT];
        particleVelocities = new Vector2[PARTICLE_COUNT];
        particleLifetimes = new float[PARTICLE_COUNT];
        for (int i = 0; i < PARTICLE_COUNT; i++)
        {
            particles[i] = CreateImage(rootGO.transform, $"Particle_{i}", new Color(1f, 0.3f, 0.1f, 0));
            RectTransform rt = particles[i].rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.55f);
            rt.sizeDelta = new Vector2(6f, 6f);
            rt.anchoredPosition = Vector2.zero;
            particles[i].gameObject.SetActive(false);
        }
    }

    private Image CreateFullscreenImage(Transform parent, string name, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        Image img = go.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    private Image CreateImage(Transform parent, string name, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        Image img = go.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    // ═══════════════════════════════════════════════════════
    // ANA GİRİŞ NOKTASI — SecretPerkOrb buraya çağırır
    // ═══════════════════════════════════════════════════════
    public void Play(BasePerk perk)
    {
        if (isPlaying) return;
        StartCoroutine(CinematicSequence(perk));
    }

    private IEnumerator CinematicSequence(BasePerk perk)
    {
        isPlaying = true;

        // Hazırlık
        rootGroup.gameObject.SetActive(true);
        rootGroup.alpha = 0f;
        blackOverlay.color = new Color(0, 0, 0, 0);
        flashOverlay.gameObject.SetActive(false);
        perkIconImage.color = new Color(1, 1, 1, 0);
        perkIconImage.rectTransform.localScale = Vector3.zero;
        perkNameText.text = "";
        perkSubText.color = new Color(0.7f, 0.7f, 0.7f, 0f);

        if (perk.icon != null)
            perkIconImage.sprite = perk.icon;

        rootGroup.alpha = 1f;

        // Özel secret perk sesi varsa çal (yoksa her fazın kendi sesleri var)
        if (AudioManager.instance != null) AudioManager.instance.PlaySecretPerk();

        // ╔═══════════════════════════════════════╗
        // ║  FAZ 1: GLITCH DISTORTION (0.8s)      ║
        // ╚═══════════════════════════════════════╝
        yield return StartCoroutine(Phase_Glitch(0.8f));

        // ╔═══════════════════════════════════════╗
        // ║  FAZ 2: ENERGY CONVERGENCE (0.7s)      ║
        // ╚═══════════════════════════════════════╝
        yield return StartCoroutine(Phase_Convergence(0.7f));

        // ╔═══════════════════════════════════════╗
        // ║  FAZ 3: FLASH + ICON REVEAL (0.6s)    ║
        // ╚═══════════════════════════════════════╝
        yield return StartCoroutine(Phase_FlashReveal(0.6f));

        // ╔═══════════════════════════════════════╗
        // ║  FAZ 4: TYPEWRITER + PARTICLES (1.2s)  ║
        // ╚═══════════════════════════════════════╝
        yield return StartCoroutine(Phase_Identity(perk, 1.2f));

        // Dramatik duraklama — oyuncu ne aldığını okusun
        yield return new WaitForSecondsRealtime(1.0f);

        // ╔═══════════════════════════════════════╗
        // ║  FAZ 5: DISSOLUTION (0.6s)             ║
        // ╚═══════════════════════════════════════╝
        yield return StartCoroutine(Phase_Dissolution(0.6f));

        // Temizlik
        rootGroup.alpha = 0f;
        rootGroup.gameObject.SetActive(false);
        CleanupAllElements();
        isPlaying = false;
    }

    // ════════════════════════════════════════════════════════
    //  FAZ 1: GLITCH — Ekran kırılıyor, kırmızı barlar uçuşuyor
    // ════════════════════════════════════════════════════════
    private IEnumerator Phase_Glitch(float duration)
    {
        // Siyah overlay fade in
        float fadeIn = 0.2f;
        float elapsed = 0f;
        while (elapsed < fadeIn)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / fadeIn;
            blackOverlay.color = new Color(0, 0, 0, t * 0.95f);
            yield return null;
        }
        blackOverlay.color = new Color(0, 0, 0, 0.95f);

        // Glitch ses
        if (AudioManager.instance != null) AudioManager.instance.PlayCharge();

        // Glitch barları rastgele patlat
        float glitchTime = duration - fadeIn;
        elapsed = 0f;
        float nextGlitch = 0f;

        while (elapsed < glitchTime)
        {
            elapsed += Time.unscaledDeltaTime;
            float intensity = Mathf.Clamp01(elapsed / glitchTime); // Giderek yoğunlaşır

            if (elapsed >= nextGlitch)
            {
                // Rastgele 2-5 bar aynı anda aktif et
                int barCount = Random.Range(2, Mathf.RoundToInt(Mathf.Lerp(3, 6, intensity)));
                for (int i = 0; i < GLITCH_BAR_COUNT; i++)
                    glitchBars[i].gameObject.SetActive(false);

                for (int b = 0; b < barCount; b++)
                {
                    int idx = Random.Range(0, GLITCH_BAR_COUNT);
                    glitchBars[idx].gameObject.SetActive(true);

                    RectTransform rt = glitchBars[idx].rectTransform;
                    float yPos = Random.Range(-540f, 540f);
                    float height = Random.Range(2f, 12f + intensity * 20f);
                    rt.anchoredPosition = new Vector2(0, yPos);
                    rt.sizeDelta = new Vector2(0, height);

                    // Renk varyasyonu: kırmızı, beyaz, siyah karışımı
                    float colorRoll = Random.value;
                    if (colorRoll < 0.5f)
                        glitchBars[idx].color = new Color(1f, 0.1f, 0.1f, Random.Range(0.3f, 0.8f));
                    else if (colorRoll < 0.8f)
                        glitchBars[idx].color = new Color(1f, 1f, 1f, Random.Range(0.2f, 0.5f));
                    else
                        glitchBars[idx].color = new Color(0f, 0f, 0f, Random.Range(0.5f, 1f));
                }

                // Sonraki glitch frame zamanlayıcısı — hızlanarak
                nextGlitch = elapsed + Random.Range(0.02f, Mathf.Lerp(0.08f, 0.02f, intensity));
            }

            // Ekran sarsıntısı — rootRect'i rastgele kaydır
            float shake = intensity * 8f;
            rootRect.anchoredPosition = new Vector2(
                Random.Range(-shake, shake),
                Random.Range(-shake, shake)
            );

            yield return null;
        }

        // Glitch barlarını kapat
        for (int i = 0; i < GLITCH_BAR_COUNT; i++)
            glitchBars[i].gameObject.SetActive(false);
        rootRect.anchoredPosition = Vector2.zero;
    }

    // ════════════════════════════════════════════════════════
    //  FAZ 2: CONVERGENCE — Enerji çizgileri merkeze akıyor
    // ════════════════════════════════════════════════════════
    private IEnumerator Phase_Convergence(float duration)
    {
        if (AudioManager.instance != null) AudioManager.instance.PlayVacuum();

        // Enerji çizgilerini başlangıç pozisyonlarına koy (ekranın kenarında, merkeze bakan)
        float screenDiag = 800f;
        for (int i = 0; i < ENERGY_LINE_COUNT; i++)
        {
            energyLines[i].gameObject.SetActive(true);

            float angle = (360f / ENERGY_LINE_COUNT) * i + Random.Range(-8f, 8f);
            float rad = angle * Mathf.Deg2Rad;

            RectTransform rt = energyLines[i].rectTransform;
            rt.anchoredPosition = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * screenDiag;
            rt.localRotation = Quaternion.Euler(0, 0, angle - 90f); // merkeze baksın
            rt.sizeDelta = new Vector2(3f, Random.Range(40f, 120f));
            rt.localScale = Vector3.one;

            // Renk: kırmızı-turuncu gradyan
            float hue = Random.Range(0f, 0.08f); // kırmızı-turuncu arası
            energyLines[i].color = Color.HSVToRGB(hue, 1f, 1f);
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;

            // Ease-in-quad: yavaş başlayıp hızlanan
            float easeT = t * t * t;

            for (int i = 0; i < ENERGY_LINE_COUNT; i++)
            {
                float angle = (360f / ENERGY_LINE_COUNT) * i;
                float rad = angle * Mathf.Deg2Rad;

                RectTransform rt = energyLines[i].rectTransform;
                float dist = Mathf.Lerp(screenDiag, 0f, easeT);
                rt.anchoredPosition = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * dist;

                // Çizgi uzunluğu merkeze yaklaştıkça artar
                float h = Mathf.Lerp(60f, 200f, easeT);
                rt.sizeDelta = new Vector2(Mathf.Lerp(3f, 5f, t), h);

                // Parlaklık artışı
                float alpha = Mathf.Lerp(0.5f, 1f, t);
                Color c = energyLines[i].color;
                energyLines[i].color = new Color(c.r, c.g, c.b, alpha);
            }

            // Hafif sarsıntı
            float microShake = t * 3f;
            rootRect.anchoredPosition = new Vector2(
                Random.Range(-microShake, microShake),
                Random.Range(-microShake, microShake)
            );

            yield return null;
        }

        // Enerji çizgilerini kapat
        for (int i = 0; i < ENERGY_LINE_COUNT; i++)
            energyLines[i].gameObject.SetActive(false);
        rootRect.anchoredPosition = Vector2.zero;
    }

    // ════════════════════════════════════════════════════════
    //  FAZ 3: FLASH + REVEAL — Beyaz patlama, ikon ortaya çıkıyor
    // ════════════════════════════════════════════════════════
    private IEnumerator Phase_FlashReveal(float duration)
    {
        // BOOM sesi
        if (AudioManager.instance != null) AudioManager.instance.PlayExplosion();

        // Anlık beyaz flaş
        flashOverlay.gameObject.SetActive(true);
        flashOverlay.color = Color.white;

        // Sarsıntı
        rootRect.anchoredPosition = new Vector2(Random.Range(-15f, 15f), Random.Range(-15f, 15f));
        yield return new WaitForSecondsRealtime(0.05f);
        rootRect.anchoredPosition = Vector2.zero;

        // Flaş sönmesi + İkon spring animasyonu paralel
        float elapsed = 0f;
        float flashDur = 0.3f;
        float springDur = duration;

        // Spring parametreleri
        float springFreq = 12f;
        float springDamp = 4f;

        while (elapsed < springDur)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / springDur;

            // Flaş sönmesi
            if (elapsed < flashDur)
            {
                float ft = elapsed / flashDur;
                flashOverlay.color = new Color(1, 1, 1, Mathf.Lerp(1f, 0f, ft * ft));
            }
            else
            {
                flashOverlay.color = new Color(1, 1, 1, 0);
            }

            // Spring bounce: scale 0 → overshoot → 1
            float spring = 1f - Mathf.Exp(-springDamp * t) * Mathf.Cos(springFreq * t);
            float scale = Mathf.LerpUnclamped(0f, 1f, spring);
            perkIconImage.rectTransform.localScale = new Vector3(scale, scale, 1);

            // İkon alpha
            perkIconImage.color = new Color(1, 1, 1, Mathf.Clamp01(t * 4f));

            yield return null;
        }

        flashOverlay.gameObject.SetActive(false);
        perkIconImage.rectTransform.localScale = Vector3.one;
        perkIconImage.color = Color.white;
    }

    // ════════════════════════════════════════════════════════
    //  FAZ 4: IDENTITY — Typewriter + RGB + Parçacıklar
    // ════════════════════════════════════════════════════════
    private IEnumerator Phase_Identity(BasePerk perk, float duration)
    {
        if (AudioManager.instance != null) AudioManager.instance.PlayLightning();

        string fullName = perk.perkName;
        perkNameText.text = "";

        // Typewriter efekti — harf harf yaz
        float typeDelay = Mathf.Min(0.06f, 0.5f / Mathf.Max(1, fullName.Length));
        for (int i = 0; i < fullName.Length; i++)
        {
            perkNameText.text = fullName.Substring(0, i + 1);
            if (AudioManager.instance != null) AudioManager.instance.PlayTextEffect();
            CameraController.ShakeLight();
            yield return new WaitForSecondsRealtime(typeDelay);
        }

        // Alt yazı fade in
        float subFade = 0.3f;
        float subElapsed = 0f;
        while (subElapsed < subFade)
        {
            subElapsed += Time.unscaledDeltaTime;
            float a = Mathf.Clamp01(subElapsed / subFade);
            perkSubText.color = new Color(0.7f, 0.7f, 0.7f, a);
            yield return null;
        }
        perkSubText.color = new Color(0.7f, 0.7f, 0.7f, 1f);

        // Parçacıkları fırlat + RGB renk döngüsü
        for (int i = 0; i < PARTICLE_COUNT; i++)
        {
            particles[i].gameObject.SetActive(true);
            particles[i].rectTransform.anchoredPosition = Vector2.zero;

            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float speed = Random.Range(150f, 500f);
            particleVelocities[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed;
            particleLifetimes[i] = Random.Range(0.5f, 1.0f);

            float size = Random.Range(3f, 10f);
            particles[i].rectTransform.sizeDelta = new Vector2(size, size);
            particles[i].color = Color.HSVToRGB(Random.Range(0f, 0.1f), 1f, 1f);
        }

        float particleTime = duration - (fullName.Length * typeDelay) - subFade;
        if (particleTime < 0.3f) particleTime = 0.3f;
        float pElapsed = 0f;

        while (pElapsed < particleTime)
        {
            pElapsed += Time.unscaledDeltaTime;

            // RGB renk döngüsü perk ismi — kırmızı tonlarında
            float hue = Mathf.Repeat(Time.unscaledTime * 0.5f, 0.12f); // 0-0.12 arası = kırmızı-turuncu
            Color nameCol = Color.HSVToRGB(hue, 1f, 1f);
            perkNameText.color = nameCol;

            // İkon glow pulse
            float glow = 1f + Mathf.Sin(Time.unscaledTime * 8f) * 0.15f;
            perkIconImage.rectTransform.localScale = new Vector3(glow, glow, 1f);

            // Parçacıkları güncelle
            for (int i = 0; i < PARTICLE_COUNT; i++)
            {
                if (!particles[i].gameObject.activeSelf) continue;

                particleLifetimes[i] -= Time.unscaledDeltaTime;
                if (particleLifetimes[i] <= 0f)
                {
                    particles[i].gameObject.SetActive(false);
                    continue;
                }

                RectTransform rt = particles[i].rectTransform;
                rt.anchoredPosition += particleVelocities[i] * Time.unscaledDeltaTime;

                // Yavaşla
                particleVelocities[i] *= (1f - 2f * Time.unscaledDeltaTime);

                // Alpha azalt
                Color c = particles[i].color;
                particles[i].color = new Color(c.r, c.g, c.b, Mathf.Clamp01(particleLifetimes[i] * 2f));
            }

            yield return null;
        }
    }

    // ════════════════════════════════════════════════════════
    //  FAZ 5: DISSOLUTION — Her şey dramatik şekilde dağılıyor
    // ════════════════════════════════════════════════════════
    private IEnumerator Phase_Dissolution(float duration)
    {
        if (AudioManager.instance != null) AudioManager.instance.PlayHammer();

        float elapsed = 0f;

        // Başlangıç değerlerini kaydet
        Vector3 iconStartScale = perkIconImage.rectTransform.localScale;
        Vector2 iconStartPos = perkIconImage.rectTransform.anchoredPosition;
        Vector2 nameStartPos = perkNameText.rectTransform.anchoredPosition;
        Vector2 subStartPos = perkSubText.rectTransform.anchoredPosition;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;

            // Ease-in-cubic: yavaş başla, hızla bitir
            float ease = t * t * t;

            // İkon: büyüyüp sönüyor
            float iconScale = Mathf.Lerp(1f, 2.5f, ease);
            perkIconImage.rectTransform.localScale = new Vector3(iconScale, iconScale, 1f);
            perkIconImage.color = new Color(1, 1, 1, Mathf.Lerp(1f, 0f, ease));

            // İsim: yukarı kayıp sönüyor
            float nameY = Mathf.Lerp(0f, 80f, ease);
            perkNameText.rectTransform.anchoredPosition = nameStartPos + new Vector2(0, nameY);
            Color nc = perkNameText.color;
            perkNameText.color = new Color(nc.r, nc.g, nc.b, Mathf.Lerp(1f, 0f, ease));

            // Alt yazı: aşağı kayıp sönüyor
            float subY = Mathf.Lerp(0f, -50f, ease);
            perkSubText.rectTransform.anchoredPosition = subStartPos + new Vector2(0, subY);
            Color sc = perkSubText.color;
            perkSubText.color = new Color(sc.r, sc.g, sc.b, Mathf.Lerp(1f, 0f, ease));

            // Arka plan sönmesi
            blackOverlay.color = new Color(0, 0, 0, Mathf.Lerp(0.95f, 0f, ease));

            // Son sarsıntı
            if (t < 0.3f)
            {
                float s = (1f - t / 0.3f) * 5f;
                rootRect.anchoredPosition = new Vector2(
                    Random.Range(-s, s), Random.Range(-s, s));
            }
            else
            {
                rootRect.anchoredPosition = Vector2.zero;
            }

            yield return null;
        }

        rootRect.anchoredPosition = Vector2.zero;
    }

    private void CleanupAllElements()
    {
        for (int i = 0; i < GLITCH_BAR_COUNT; i++)
            if (glitchBars[i] != null) glitchBars[i].gameObject.SetActive(false);
        for (int i = 0; i < ENERGY_LINE_COUNT; i++)
            if (energyLines[i] != null) energyLines[i].gameObject.SetActive(false);
        for (int i = 0; i < PARTICLE_COUNT; i++)
            if (particles[i] != null) particles[i].gameObject.SetActive(false);

        if (flashOverlay != null) flashOverlay.gameObject.SetActive(false);
        perkIconImage.color = new Color(1, 1, 1, 0);
        perkIconImage.rectTransform.localScale = Vector3.zero;
        perkNameText.text = "";
        perkNameText.rectTransform.anchoredPosition = Vector2.zero;
        perkSubText.rectTransform.anchoredPosition = Vector2.zero;
        perkSubText.color = new Color(0.7f, 0.7f, 0.7f, 0f);
        blackOverlay.color = new Color(0, 0, 0, 0);
    }
}
