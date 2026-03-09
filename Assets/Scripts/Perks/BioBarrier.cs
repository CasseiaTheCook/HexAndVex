using UnityEngine;
using System.Collections;

public class BioBarrierPerk : BasePerk
{
    [Header("Obje Ayarları")]
    public GameObject shieldPrefab; // Kalkan objen
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
            currentShieldInstance.transform.localPosition = Vector3.zero; // Karakterin tam ortasına oturt
            
            // Kalkan ilk doğduğunda görünürlüğünü normale çek (eğer önceden saydam kaldıysa diye)
            SpriteRenderer[] renderers = currentShieldInstance.GetComponentsInChildren<SpriteRenderer>();
            foreach (var sr in renderers)
            {
                Color c = sr.color;
                c.a = 0.5f; // Normalde kalkanın saydamlığı (istersen 1f yapabilirsin)
                sr.color = c;
            }
        }
    }

    public void BreakShield()
    {
        if (currentShieldInstance != null)
        {
            StartCoroutine(AnimateShieldBreak());
        }
    }

    private IEnumerator AnimateShieldBreak()
    {
        // Kalkan objesinin içindeki tüm resimleri (Sprite) bul
        SpriteRenderer[] renderers = currentShieldInstance.GetComponentsInChildren<SpriteRenderer>();
        
        Vector3 startScale = currentShieldInstance.transform.localScale;
        Vector3 endScale = startScale * 2.5f; // Kalkan kırılırken 2.5 katına çıkarak patlasın
        
        float duration = 0.3f; // Patlama süresi (0.3 saniye, çok tatlı bir hız)
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            // 1. Kalkanı büyüt
            currentShieldInstance.transform.localScale = Vector3.Lerp(startScale, endScale, t);
            
            // 2. Kalkanı aynı anda yavaşça saydamlaştır (Fade Out)
            foreach (var sr in renderers)
            {
                Color c = sr.color;
                c.a = Mathf.Lerp(0.5f, 0f, t); // 0.5 alpha'dan 0'a doğru erit
                sr.color = c;
            }
            
            yield return null;
        }
        
        // Animasyon bitince kalkanı tamamen sil
        Destroy(currentShieldInstance);
        currentShieldInstance = null;
    }
}