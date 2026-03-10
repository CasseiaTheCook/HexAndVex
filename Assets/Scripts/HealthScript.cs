using TMPro;
using UnityEngine;
using System.Collections;

/// <summary>
/// Oyuncu ve düşman için ortak sağlık sistemi.
/// Hasar, iyileştirme, yumuşak renk geçişleri (Damage Flash), yumuşak saydamlaşma ve ölüm animasyonunu yönetir.
/// </summary>
public class HealthScript : MonoBehaviour
{
    [Header("HP Settings")]
    public int maxHP = 3;
    public int currentHP;

    public System.Action OnDeath;
    public System.Action<int> OnDamaged;

    public TMP_Text hptext;

    private SpriteRenderer spriteRenderer;
    private Color originalColor = Color.white;
    private Coroutine flashCoroutine;
    private Coroutine alphaFadeCoroutine; // YENİ: Saydamlığın yavaşça değişmesini sağlayan animasyon
    private bool isDead = false;

    private bool isDeepStunnedAlpha = false;
    [Header("VFX")]
    public GameObject damageTextPrefab; // Hazırladığın prefabı buraya sürükle

    public GameObject deathMenuUI; // Ölme ekranını buraya da sürükle
    void Start()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null) originalColor = spriteRenderer.color;

        if (gameObject.CompareTag("Player"))
        {
            if (RunManager.instance != null)
            {
                maxHP = RunManager.instance.playerMaxHealth;
                currentHP = RunManager.instance.playerCurrentHealth;
            }
        }
        else
        {
            currentHP = maxHP;
        }
        updateHealth();
    }

    public void TakeDamage(int dmg)
    {
        if (isDead) return;

        currentHP -= dmg;

        if (gameObject.CompareTag("Player"))
        {
            RunManager.instance.totalDamageReceived += dmg; // Alınan hasarı kaydet
        }
        
        if (damageTextPrefab != null)
        {
            // Sayıyı tam düşmanın merkezinde oluştur
            GameObject dmgObj = Instantiate(damageTextPrefab, transform.position, Quaternion.identity);

            // Setup fonksiyonunu çağırarak içindeki rakamı yazdır
            dmgObj.GetComponent<DamageNumber>().Setup(dmg);
        }

        EnemyAI enemy = GetComponentInParent<EnemyAI>();
        if (enemy != null)
        {
            enemy.skipTurns = Mathf.Max(enemy.skipTurns, 1);
            enemy.SetStunVisual(true);
        }

        OnDamaged?.Invoke(currentHP);
        updateHealth();

        if (currentHP <= 0)
        {
            Die();
        }
        else
        {
            if (spriteRenderer != null && gameObject.activeInHierarchy)
            {
                if (flashCoroutine != null) StopCoroutine(flashCoroutine);
                flashCoroutine = StartCoroutine(DamageFlash());
            }
        }
    }

    private IEnumerator DamageFlash()
    {
        spriteRenderer.color = Color.red;

        float duration = 0.35f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            // originalColor'ın alpha'sı o sırada değişiyor olsa bile sorunsuz takip eder
            spriteRenderer.color = Color.Lerp(Color.red, originalColor, elapsed / duration);
            yield return null;
        }

        spriteRenderer.color = originalColor;
        flashCoroutine = null;
    }

    // --- YENİ: YUMUŞAK SAYDAMLAŞMA (FADE IN / FADE OUT) ---
    public void SetStunnedAlpha(bool deepStun)
    {
        if (isDeepStunnedAlpha == deepStun || spriteRenderer == null || isDead) return;

        isDeepStunnedAlpha = deepStun;
        float targetAlpha = deepStun ? 0.45f : 1f;

        // Eğer halihazırda bir saydamlaşma animasyonu varsa durdur, yenisini başlat
        if (alphaFadeCoroutine != null) StopCoroutine(alphaFadeCoroutine);
        alphaFadeCoroutine = StartCoroutine(FadeAlphaCoroutine(targetAlpha));
    }

    private IEnumerator FadeAlphaCoroutine(float targetAlpha)
    {
        float startAlpha = originalColor.a;
        float duration = 0.3f; // Saydamlaşma/Belirginleşme hızı (0.3 saniye)
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float newAlpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);

            // Orijinal rengin hafızasını güncelle
            originalColor = new Color(originalColor.r, originalColor.g, originalColor.b, newAlpha);

            // Eğer o an kırmızı parlama YOKSA, yeni saydamlığı direkt uygula
            // (Kırmızı parlama varsa, zaten üstteki DamageFlash bu originalColor'ı kullanıyor)
            if (flashCoroutine == null)
            {
                spriteRenderer.color = originalColor;
            }

            yield return null;
        }

        // Animasyon bitince tam değeri oturt
        originalColor = new Color(originalColor.r, originalColor.g, originalColor.b, targetAlpha);
        if (flashCoroutine == null)
        {
            spriteRenderer.color = originalColor;
        }
    }

    public void Heal(int amount)
    {
        if (isDead) return;
        currentHP = Mathf.Min(currentHP + amount, maxHP);
        updateHealth();
    }

    private void Die()
    {
        isDead = true;
        OnDeath?.Invoke();

        if (hptext != null) hptext.gameObject.SetActive(false);

        if (gameObject.CompareTag("Player"))
        {
            if (deathMenuUI != null)
            {
                deathMenuUI.SetActive(true);
                Time.timeScale = 0f;
            }
            return;
        }

        // Düşman: DeathAnimation ile yok et (TurnManager gold verene kadar hayatta kalır)
        Debug.Log(gameObject.name + " öldü.");
        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(DeathAnimation());
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private IEnumerator DeathAnimation()
    {
        float duration = 0.4f;
        float elapsed = 0f;

        Vector3 startScale = transform.localScale;
        Color startColor = spriteRenderer != null ? spriteRenderer.color : Color.white;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);

            if (spriteRenderer != null)
            {
                spriteRenderer.color = new Color(startColor.r, startColor.g, startColor.b, Mathf.Lerp(startColor.a, 0f, t));
            }

            yield return null;
        }

        Destroy(gameObject);
    }

    public void updateHealth()
    {
        if (hptext != null) hptext.text = currentHP.ToString() + "/" + maxHP;
    }
}
