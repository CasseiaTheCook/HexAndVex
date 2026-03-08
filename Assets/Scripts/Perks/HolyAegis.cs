using UnityEngine;
using System.Collections;

public class HolyAegisPerk : BasePerk
{
    [Header("Obje Ayarları")]
    public GameObject shieldPrefab; // Kalkan objen (animasyonlu hali)
    private GameObject currentShieldInstance;

    public override void OnAcquire()
    {
        RunManager.instance.hasHolyAegis = true;
        SpawnShield();
        TriggerVisualPop();
    }

    public override void OnLevelStart()
    {
        RunManager.instance.hasHolyAegis = true;
        if (currentShieldInstance == null) SpawnShield();
        TriggerVisualPop();
    }

    private void SpawnShield()
    {
        if (currentShieldInstance != null) Destroy(currentShieldInstance);
        
        // DÜZELTME: Kalkanı UI'a değil, OYUNCUNUN üstüne ekliyoruz!
        if (TurnManager.instance != null && TurnManager.instance.player != null)
        {
            Transform playerTransform = TurnManager.instance.player.transform;
            currentShieldInstance = Instantiate(shieldPrefab, playerTransform.position, Quaternion.identity, playerTransform);
            currentShieldInstance.transform.localPosition = Vector3.zero; // Karakterin tam ortasına oturt
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
        // Kalkan patlarken objeyi yok etmeden önce animasyonu oynat
        Animator anim = currentShieldInstance.GetComponent<Animator>();
        if (anim != null)
        {
            anim.SetTrigger("Break"); 
            yield return new WaitForSeconds(anim.GetCurrentAnimatorStateInfo(0).length);
        }
        else
        {
            // Animator yoksa basit ölçeklenme patlaması
            float elapsed = 0f;
            while (elapsed < 0.2f)
            {
                currentShieldInstance.transform.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 1.5f, elapsed / 0.2f);
                yield return null;
            }
        }
        
        Destroy(currentShieldInstance);
        currentShieldInstance = null;
    }
}