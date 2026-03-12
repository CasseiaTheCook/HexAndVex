using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;

    [Header("Ses Klipleri")]
    public AudioClip takeDamageClip;   // Hasar yeme (oyuncu + düşman)
    public AudioClip moveClip;         // Tile hareketi
    public AudioClip swingClip;        // Saldırı animasyonu başında
    public AudioClip hitClip;          // Düşmana vurma (0.3s sonra)
    public AudioClip wallClip;         // Duvara çarpma
    public AudioClip explosionClip;    // Patlama (mayın, fragmine)
    public AudioClip coinClip;         // Coin düşme
    public AudioClip cardClip;         // Perk kartı açılma
    public AudioClip purchaseClip;     // Satın alma
    public AudioClip chargeClip;       // Telegraf saldırı hazırlanma (charge)
    public AudioClip hammerClip;       // Telegraf saldırı vuruş anı
    public AudioClip lightningClip;    // Boss şimşek vuruş anı
    public AudioClip diceRollClip;     // Zarlar ortaya çıkarken
    public AudioClip diceHitClip;      // Her zar yüzü açılırken
    public AudioClip vacuumClip;       // Vacuum efekti
    public AudioClip textEffectClip;   // Bam bam zar text animasyonu
    public AudioClip secretPerkClip;   // Secret perk sinematik açılış sesi (opsiyonel)
    public AudioClip shieldBreakClip;  // Kalkan kırılma / dodge
    public AudioClip bossRoarClip;     // Boss giriş animasyonu kükreme sesi
    public AudioClip bossGruntClip;    // Boss hasar yeme grunt sesi
    public AudioClip warlockGruntClip; // Warlock hasar yeme grunt sesi

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

    private void Play(AudioClip clip, float volumeScale = 1f, float pitchMin = 1f, float pitchMax = 1f)
    {
        if (clip == null || audioSource == null) return;
        audioSource.pitch = Random.Range(pitchMin, pitchMax);
        audioSource.PlayOneShot(clip, masterVolume * sfxVolume * volumeScale);
        audioSource.pitch = 1f;
    }

    public void PlayTakeDamage()  => Play(takeDamageClip,  1f,   0.9f,  1.1f);
    public void PlayMove()        => Play(moveClip,        0.7f, 0.95f, 1.05f);
    public void PlaySwing()       => Play(swingClip,       1f,   0.9f,  1.1f);
    public void PlayHit()         => Play(hitClip,         1f,   0.85f, 1.15f);
    public void PlayWall()        => Play(wallClip,        0.9f, 0.9f,  1.1f);
    public void PlayExplosion()   => Play(explosionClip,   1f,   0.95f, 1.05f);
    public void PlayCoin()        => Play(coinClip,        0.6f, 0.9f,  1.2f);
    public void PlayCard()        => Play(cardClip,        0.8f, 0.95f, 1.05f);
    public void PlayPurchase()    => Play(purchaseClip,    1f,   0.95f, 1.05f);
    public void PlayCharge()      => Play(chargeClip,      0.9f, 0.9f,  1.1f);
    public void PlayHammer()      => Play(hammerClip,      0.85f,0.9f,  1.1f);
    public void PlayLightning()   => Play(lightningClip);
    public void PlayDiceRoll()    => Play(diceRollClip,    0.9f, 0.9f,  1.1f);
    public void PlayDiceHit()     => Play(diceHitClip,     0.8f, 0.85f, 1.2f);
    public void PlayVacuum()      => Play(vacuumClip,      1f,   0.95f, 1.05f);
    public void PlayTextEffect()  => Play(textEffectClip,  0.75f,0.85f, 1.2f);
    public void PlaySecretPerk()   => Play(secretPerkClip,  1f,   0.95f, 1.05f);
    public void PlayShieldBreak()  => Play(shieldBreakClip, 1f,   0.9f,  1.1f);
    public void PlayBossRoar()     => Play(bossRoarClip != null ? bossRoarClip : lightningClip, 1.2f, 0.7f, 0.8f);
    public void PlayBossGrunt()    => Play(bossGruntClip,    1f, 0.85f, 1.1f);
    public void PlayWarlockGrunt() => Play(warlockGruntClip, 1f, 0.85f, 1.1f);
}
