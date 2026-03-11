using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;

    [Header("Ses Klipleri")]
    public AudioClip takeDamageClip;   // Hasar yeme (oyuncu + düşman)
    public AudioClip moveClip;         // Tile hareketi
    public AudioClip hitClip;          // Düşmana vurma
    public AudioClip wallClip;         // Duvara çarpma
    public AudioClip explosionClip;    // Patlama (mayın, fragmine)
    public AudioClip coinClip;         // Coin düşme
    public AudioClip cardClip;         // Perk kartı açılma
    public AudioClip purchaseClip;     // Satın alma
    public AudioClip hammerClip;       // Telegraf saldırı hazırlanma
    public AudioClip lightningClip;    // Şimşek (kullanılmıyor şimdilik)

    [Header("Ses Ayarları")]
    [Range(0f, 1f)] public float masterVolume = 1f;
    [Range(0f, 1f)] public float sfxVolume = 1f;

    private AudioSource audioSource;

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

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
    }

    private void Play(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null || audioSource == null) return;
        audioSource.PlayOneShot(clip, masterVolume * sfxVolume * volumeScale);
    }

    public void PlayTakeDamage()  => Play(takeDamageClip);
    public void PlayMove()        => Play(moveClip, 0.7f);
    public void PlayHit()         => Play(hitClip);
    public void PlayWall()        => Play(wallClip, 0.9f);
    public void PlayExplosion()   => Play(explosionClip);
    public void PlayCoin()        => Play(coinClip, 0.6f);
    public void PlayCard()        => Play(cardClip, 0.8f);
    public void PlayPurchase()    => Play(purchaseClip);
    public void PlayHammer()      => Play(hammerClip, 0.85f);
    public void PlayLightning()   => Play(lightningClip);
}
