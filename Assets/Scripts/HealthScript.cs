using TMPro;
using UnityEngine;
using System.Collections; // Coroutine'ler (animasyonlar) için eklendi

/// <summary>
/// Oyuncu ve düşman için ortak sağlık sistemi.
/// Hasar, iyileştirme, yumuşak renk geçişleri (Damage Flash) ve ölüm animasyonunu yönetir.
/// </summary>
public class HealthScript : MonoBehaviour
{
    [Header("HP Settings")]
    public int maxHP = 3;
    public int currentHP;

    public System.Action OnDeath;
    public System.Action<int> OnDamaged;

    public TMP_Text hptext;

    // Görsel efektler için gereken değişkenler
    private SpriteRenderer spriteRenderer;
    private Color originalColor = Color.white;
    private Coroutine flashCoroutine; // Üst üste hasar yediğinde renkler bug'a girmesin diye
    private bool isDead = false; // Ölüm animasyonu iki kere tetiklenmesin diye kilit

    void Start()
    {
        // Karakterin SpriteRenderer'ını bul ve orijinal rengini kaydet
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

        // --- YENİ: HER HASARDA SERSEMLETME ---
        // Eğer bu bir düşman ise, hasar aldığı an skipTurns'ü en az 1 yapıyoruz
        // HealthScript.cs içindeki o kısım tam olarak şöyle olmalı:
        EnemyAI enemy = GetComponentInParent<EnemyAI>();
        if (enemy != null)
        {
            enemy.skipTurns = 1; // Hasar aldı, 1 tur şokta!
            enemy.SetStunVisual(true);
        }
        // -------------------------------------

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

    // --- YENİ: YUMUŞAK KIRMIZI PARLAMA (DAMAGE FLASH) ---
    private IEnumerator DamageFlash()
    {
        // Vurulduğu an TIK diye kırmızı olur (Etki hissi için anlık olmalı)
        spriteRenderer.color = Color.red;

        float duration = 0.35f; // Eski rengine dönme süresi (Fade out hızı)
        float elapsed = 0f;

        // Kırmızıdan orijinal renge yavaşça (Fade out) geçiş yap
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            spriteRenderer.color = Color.Lerp(Color.red, originalColor, elapsed / duration);
            yield return null;
        }

        // Animasyon bitince rengin tam olarak eski haline döndüğünden emin ol
        spriteRenderer.color = originalColor;
    }

    public void Heal(int amount)
    {
        if (isDead) return;
        currentHP = Mathf.Min(currentHP + amount, maxHP);
        updateHealth();
    }

    private void Die()
    {
        isDead = true; // Kilitledik
        Debug.Log($"{gameObject.name} öldü!");
        OnDeath?.Invoke();

        if (hptext != null) hptext.gameObject.SetActive(false); // Can yazısını anında ekrandan sil

        // Anında yok etmek yerine ölüm animasyonunu başlatıyoruz
        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(DeathAnimation());
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // --- YENİ: KÜÇÜLEREK VE SİLİNEREK YOK OLMA (DEATH ANIMATION) ---
    private IEnumerator DeathAnimation()
    {
        float duration = 0.4f; // Objenin eriyip yok olma süresi
        float elapsed = 0f;

        Vector3 startScale = transform.localScale;
        Color startColor = spriteRenderer != null ? spriteRenderer.color : Color.white;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // 1. Ölürken objeyi küçült (Scale'i 0'a doğru çek)
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);

            // 2. Saydamlaşma efekti (Alpha değerini 0'a doğru çekip Fade Out yap)
            if (spriteRenderer != null)
            {
                spriteRenderer.color = new Color(startColor.r, startColor.g, startColor.b, Mathf.Lerp(startColor.a, 0f, t));
            }

            yield return null;
        }

        // Animasyon bittiğinde objeyi sahneden tamamen sil
        Destroy(gameObject);
    }

    public void updateHealth()
    {
        if (hptext != null) hptext.text = currentHP.ToString() + "/" + maxHP;
    }
}