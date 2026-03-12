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

    public void TriggerHitstop(float duration = 1f / 15f)
    {
        // Aktif hitstop varsa iptal et
        if (activeHitstop != null)
            StopCoroutine(activeHitstop);

        // Yeni hitstop başlat
        activeHitstop = StartCoroutine(ApplyHitstop(duration));
    }

    private IEnumerator ApplyHitstop(float duration)
    {
        float originalTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = originalTimeScale;
    }
}
