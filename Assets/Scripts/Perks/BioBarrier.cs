using UnityEngine;
using System.Collections;

public class BioBarrierPerk : BasePerk
{
    [Header("Obje Ayarları")]
    public GameObject shieldPrefab; // Kalkan objen
    
    // ========================================================
    // YENİ: Kalkanın Y eksenindeki yüksekliği (Inspector'dan ayarla)
    // ========================================================
    public float shieldOffsetY = 0.07f; 
    
    private GameObject currentShieldInstance;

    public override void OnAcquire()
    {
        RunManager.instance.hasBioBarrier = true;
        SpawnShield();
        TriggerVisualPop();
    }

    public override void OnLevelStart()
    {
        RunManager.instance.hasBioBarrier = true;
        if (currentShieldInstance == null) SpawnShield();
        TriggerVisualPop();
    }

    private void SpawnShield()
    {
        if (currentShieldInstance != null) Destroy(currentShieldInstance);
        
        // Kalkanı OYUNCUNUN üstüne ekliyoruz
        if (TurnManager.instance != null && TurnManager.instance.player != null)
        {
            Transform playerTransform = TurnManager.instance.player.transform;
            currentShieldInstance = Instantiate(shieldPrefab, playerTransform.position, Quaternion.identity, playerTransform);
            
            // ========================================================
            // DÜZELTME: Kalkanı karakterin merkezinden offset değeri kadar yukarı taşıyoruz
            // ========================================================
            currentShieldInstance.transform.localPosition = new Vector3(0f, shieldOffsetY, 0f); 
            
            // Prefabın alpha değerini koru, hard code etme
        }
    }

    public void BreakShield()
    {
        if (currentShieldInstance != null)
        {
            if (AudioManager.instance != null) AudioManager.instance.PlayShieldBreak();
            StartCoroutine(AnimateShieldBreak());
        }
    }

    private IEnumerator AnimateShieldBreak()
    {
        SpriteRenderer[] renderers = currentShieldInstance.GetComponentsInChildren<SpriteRenderer>();
        float startAlpha = renderers.Length > 0 ? renderers[0].color.a : 1f;

        Vector3 startScale = currentShieldInstance.transform.localScale;
        Vector3 endScale = startScale * 2.5f;

        float duration = 0.3f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            currentShieldInstance.transform.localScale = Vector3.Lerp(startScale, endScale, t);

            foreach (var sr in renderers)
            {
                Color c = sr.color;
                c.a = Mathf.Lerp(startAlpha, 0f, t);
                sr.color = c;
            }
            
            yield return null;
        }
        
        // Animasyon bitince kalkanı tamamen sil
        Destroy(currentShieldInstance);
        currentShieldInstance = null;
    }
}