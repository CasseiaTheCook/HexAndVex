using UnityEngine;
using System.Collections;

/// <summary>
/// Hitstop (frame freeze) yönetimi
/// Aynı anda birden fazla TakeDamage çağrısı olsa bile sadece bir tane hitstop çalışır
/// </summary>
public class HitstopManager : MonoBehaviour
{
    public static HitstopManager instance;
    private Coroutine activeHitstop;

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    private float savedTimeScale = 1f;

    public void TriggerHitstop(float duration = 1f / 15f)
    {
        // Aktif hitstop varsa iptal et ama orijinal timeScale'i koru
        if (activeHitstop != null)
            StopCoroutine(activeHitstop);
        else
            savedTimeScale = Time.timeScale; // Sadece ilk hitstop'ta kaydet

        // Yeni hitstop başlat
        activeHitstop = StartCoroutine(ApplyHitstop(duration));
    }

    private IEnumerator ApplyHitstop(float duration)
    {
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = savedTimeScale;
        activeHitstop = null;
    }
}
